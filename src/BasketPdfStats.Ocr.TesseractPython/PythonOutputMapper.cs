using System.Globalization;
using System.Text.Json;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Normalization;
using BasketPdfStats.Core.Ocr;

namespace BasketPdfStats.Ocr.TesseractPython;

public sealed class PythonOutputMapper
{
    private const string EngineName = OcrStrategyNames.TesseractFullPage;

    public ProcessingResult Map(JsonDocument document, OcrProcessingRequest request)
    {
        var root = document.RootElement;
        var reliability = GetString(root, "affidabilita", "livello_globale");
        var confidence = ConfidenceFromReliability(reliability);
        var result = new ProcessingResult
        {
            Game = MapGame(root, request)
        };
        var context = new MappingContext();

        MapTeamsAndPlayers(root, result, confidence, context);
        MapResultStats(root, result, confidence);
        MapComparatives(root, result, confidence);
        MapValidation(root, result, context);
        if (result.Validation.Status == FileProcessingStatus.Pending)
        {
            result.Validation.Status = result.Validation.Warnings.Count == 0
                ? FileProcessingStatus.CompletedValidated
                : FileProcessingStatus.CompletedWithWarnings;
        }

        return result;
    }

    private static GameStats MapGame(JsonElement root, OcrProcessingRequest request)
    {
        var gameId = $"game:{ShortHash(request.DocumentHash)}";
        var game = new GameStats
        {
            GameId = gameId,
            Competition = GetString(root, "partita", "competizione"),
            Venue = GetString(root, "partita", "campo"),
            Date = GetString(root, "partita", "data"),
            Time = GetString(root, "partita", "ora"),
            Number = GetNumberAsString(root, "partita", "numero_gara"),
            Spectators = GetInt(root, "partita", "spettatori"),
            Duration = GetString(root, "partita", "durata_gara"),
            ReportCreated = GetString(root, "partita", "report_creato"),
            HomeTeamId = CanonicalEntityKeyBuilder.Team("Home"),
            AwayTeamId = CanonicalEntityKeyBuilder.Team("Away"),
            FinalScore = GetString(root, "risultato", "punteggio_finale")
        };

        if (TryGet(root, out var referees, "partita", "arbitri") && referees.ValueKind == JsonValueKind.Array)
        {
            game.Referees = referees.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToList();
        }

        if (TryGet(root, out var periods, "risultato", "periodi") && periods.ValueKind == JsonValueKind.Array)
        {
            foreach (var period in periods.EnumerateArray())
            {
                game.Periods.Add(new PeriodScore
                {
                    Period = GetString(period, "label") ?? GetNumberAsString(period, "numero") ?? string.Empty,
                    Home = GetInt(period, "squadra_casa"),
                    Away = GetInt(period, "squadra_ospite")
                });
            }
        }

        return game;
    }

    private static void MapTeamsAndPlayers(JsonElement root, ProcessingResult result, double confidence, MappingContext context)
    {
        if (!TryGet(root, out var teamsElement, "squadre") || teamsElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var index = 0;
        foreach (var teamElement in teamsElement.EnumerateArray())
        {
            var side = index == 0 ? "Home" : "Away";
            var teamName = GetString(teamElement, "nome");
            var teamAbbreviation = GetString(teamElement, "abbreviazione");
            var teamId = CanonicalEntityKeyBuilder.Team(side);
            var team = new TeamStats
            {
                TeamId = teamId,
                Side = side,
                Name = teamName,
                Abbreviation = teamAbbreviation,
                Coach = GetString(teamElement, "allenatore"),
                AssistantCoach = GetString(teamElement, "vice_allenatore")
            };
            result.Teams.Add(team);

            if (TryGet(teamElement, out var totals, "totali_squadra"))
            {
                AddStatsFromStatsObject(result.Stats, StatScope.Team, teamId, side, teamId, totals, confidence, context, TeamTotalsPath(teamName));
            }

            if (TryGet(teamElement, out var players, "giocatori") && players.ValueKind == JsonValueKind.Array)
            {
                var row = 0;
                var playerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var playerElement in players.EnumerateArray())
                {
                    row++;
                    var playerName = GetString(playerElement, "nome_completo");
                    var rawNumber = GetString(playerElement, "numero");
                    if (CanonicalEntityKeyBuilder.IsSpecialPlayerRow(rawNumber, playerName))
                    {
                        continue;
                    }

                    var identity = CanonicalEntityKeyBuilder.Player(side, rawNumber, playerName, row);
                    if (!playerIds.Add(identity.EntityId))
                    {
                        var duplicateEntityId = identity.EntityId;
                        var fallback = CanonicalEntityKeyBuilder.Player(side, null, playerName, row);
                        identity = new CanonicalPlayerIdentity(fallback.EntityId, identity.Jersey, identity.Starter);
                        if (!playerIds.Add(identity.EntityId))
                        {
                            fallback = CanonicalEntityKeyBuilder.Player(side, null, null, row);
                            identity = new CanonicalPlayerIdentity(fallback.EntityId, identity.Jersey, identity.Starter);
                            playerIds.Add(identity.EntityId);
                        }

                        result.Validation.Warnings.Add(new ValidationWarning
                        {
                            RuleId = "ocr.duplicateJerseyDetected",
                            Severity = "Warning",
                            Message = $"Duplicate player entityId '{duplicateEntityId}' for {side}; player row {row} uses fallback entityId '{identity.EntityId}'."
                        });
                    }

                    var playerId = identity.EntityId;
                    result.Players.Add(new PlayerStats
                    {
                        EntityId = playerId,
                        SourceEntityId = $"player:{teamAbbreviation ?? side}:row:{row}",
                        TeamId = teamId,
                        Side = side,
                        Number = identity.Jersey ?? rawNumber,
                        FullName = playerName,
                        FirstName = GetString(playerElement, "nome"),
                        LastName = GetString(playerElement, "cognome"),
                        Starter = identity.Starter,
                        Captain = GetBool(playerElement, "capitano"),
                        DidNotPlay = GetBool(playerElement, "non_entrato")
                    });
                    AddStatsFromStatsObject(result.Stats, StatScope.Player, playerId, side, playerId, playerElement, confidence, context, PlayerPath(teamName, playerName));
                }
            }

            index++;
        }
    }

    private static void MapResultStats(JsonElement root, ProcessingResult result, double confidence)
    {
        var gameId = result.Game.GameId;
        AddSimpleStat(result.Stats, StatScope.Result, gameId, "Home", "points", "result.home.points", GetInt(root, "risultato", "squadra_casa", "punti_finali"), confidence);
        AddSimpleStat(result.Stats, StatScope.Result, gameId, "Away", "points", "result.away.points", GetInt(root, "risultato", "squadra_ospite", "punti_finali"), confidence);

        foreach (var period in result.Game.Periods)
        {
            AddSimpleStat(result.Stats, StatScope.Result, gameId, "Home", "period.points", $"result.period.{period.Period}.home", period.Home, confidence);
            AddSimpleStat(result.Stats, StatScope.Result, gameId, "Away", "period.points", $"result.period.{period.Period}.away", period.Away, confidence);
        }
    }

    private static void MapComparatives(JsonElement root, ProcessingResult result, double confidence)
    {
        if (!TryGet(root, out var comp, "statistiche_comparative"))
        {
            return;
        }

        var gameId = result.Game.GameId;
        AddSidePair(result.Stats, gameId, "comparative.pointsFromTurnovers", "comparative.pointsFromTurnovers", comp, "punti_da_palle_perse", confidence);
        AddSidePair(result.Stats, gameId, "comparative.pointsInThePaint", "comparative.pointsInThePaint", comp, "punti_in_area", confidence);
        AddSidePair(result.Stats, gameId, "comparative.secondChancePoints", "comparative.secondChancePoints", comp, "punti_da_secondi_tiri", confidence);
        AddSidePair(result.Stats, gameId, "comparative.fastBreakPoints", "comparative.fastBreakPoints", comp, "punti_contropiede", confidence);
        AddSidePair(result.Stats, gameId, "comparative.fastBreakPointsFromTurnovers", "comparative.fastBreakPointsFromTurnovers", comp, "fast_break_points_from_turnovers", confidence);
        AddSidePair(result.Stats, gameId, "comparative.benchPoints", "comparative.benchPoints", comp, "punti_panchina", confidence);
        AddSidePair(result.Stats, gameId, "comparative.largestLead", "comparative.largestLead", comp, "massimo_vantaggio", confidence);
        AddSidePair(result.Stats, gameId, "comparative.biggestRun", "comparative.biggestRun", comp, "massimo_parziale", confidence);
        AddSidePair(result.Stats, gameId, "comparative.pointsPerPossession", "comparative.pointsPerPossession", comp, "punti_per_possesso", confidence);
        AddSidePair(result.Stats, gameId, "comparative.timeInLead", "comparative.timeInLead", comp, "tempo_in_vantaggio", confidence);
        AddSimpleStat(result.Stats, StatScope.Comparative, gameId, null, "comparative.leadChanges", "comparative.leadChanges", GetInt(comp, "cambi_guida"), confidence);
        AddSimpleStat(result.Stats, StatScope.Comparative, gameId, null, "comparative.timesTied", "comparative.timesTied", GetInt(comp, "parita"), confidence);
    }

    private static void AddStatsFromStatsObject(
        List<StatValue> stats,
        StatScope scope,
        string entityId,
        string? side,
        string fieldPrefix,
        JsonElement source,
        double confidence,
        MappingContext context,
        string? warningPathPrefix)
    {
        AddShot(stats, scope, entityId, side, fieldPrefix, "fieldGoals", "tiri_dal_campo", source, confidence, context, warningPathPrefix);
        AddShot(stats, scope, entityId, side, fieldPrefix, "twoPoints", "tiri_da_2", source, confidence, context, warningPathPrefix);
        AddShot(stats, scope, entityId, side, fieldPrefix, "threePoints", "tiri_da_3", source, confidence, context, warningPathPrefix);
        AddShot(stats, scope, entityId, side, fieldPrefix, "freeThrows", "tiri_liberi", source, confidence, context, warningPathPrefix);

        if (TryGet(source, out var rebounds, "rimbalzi"))
        {
            AddSimpleStat(stats, scope, entityId, side, "rebounds.offensive", $"{fieldPrefix}:rebounds.offensive", GetInt(rebounds, "offensivi"), confidence, context: context, warningPaths: [PathJoin(warningPathPrefix, "rimbalzi"), PathJoin(warningPathPrefix, "rimbalzi.offensivi")]);
            AddSimpleStat(stats, scope, entityId, side, "rebounds.defensive", $"{fieldPrefix}:rebounds.defensive", GetInt(rebounds, "difensivi"), confidence, context: context, warningPaths: [PathJoin(warningPathPrefix, "rimbalzi"), PathJoin(warningPathPrefix, "rimbalzi.difensivi")]);
            AddSimpleStat(stats, scope, entityId, side, "rebounds.total", $"{fieldPrefix}:rebounds.total", GetInt(rebounds, "totali"), confidence, context: context, warningPaths: [PathJoin(warningPathPrefix, "rimbalzi"), PathJoin(warningPathPrefix, "rimbalzi.totali")]);
        }

        if (TryGet(source, out var fouls, "falli"))
        {
            AddSimpleStat(stats, scope, entityId, side, "fouls.committed", $"{fieldPrefix}:fouls.committed", GetInt(fouls, "commessi"), confidence, context: context, warningPaths: [PathJoin(warningPathPrefix, "falli"), PathJoin(warningPathPrefix, "falli.commessi")]);
            AddSimpleStat(stats, scope, entityId, side, "fouls.drawn", $"{fieldPrefix}:fouls.drawn", GetInt(fouls, "subiti"), confidence, context: context, warningPaths: [PathJoin(warningPathPrefix, "falli"), PathJoin(warningPathPrefix, "falli.subiti")]);
        }

        AddSimpleStat(stats, scope, entityId, side, "assists", $"{fieldPrefix}:assists", GetInt(source, "assist"), confidence, context: context, warningPaths: [PathJoin(warningPathPrefix, "assist")]);
        AddSimpleStat(stats, scope, entityId, side, "turnovers", $"{fieldPrefix}:turnovers", GetInt(source, "palle_perse"), confidence, context: context, warningPaths: [PathJoin(warningPathPrefix, "palle_perse")]);
        AddSimpleStat(stats, scope, entityId, side, "steals", $"{fieldPrefix}:steals", GetInt(source, "palle_recuperate"), confidence, context: context, warningPaths: [PathJoin(warningPathPrefix, "palle_recuperate")]);
        AddSimpleStat(stats, scope, entityId, side, "blocks", $"{fieldPrefix}:blocks", GetInt(source, "stoppate_date"), confidence, context: context, warningPaths: [PathJoin(warningPathPrefix, "stoppate_date")]);
        AddSimpleStat(stats, scope, entityId, side, "plusMinus", $"{fieldPrefix}:plusMinus", GetInt(source, "plus_minus"), confidence, context: context, warningPaths: [PathJoin(warningPathPrefix, "plus_minus")]);
        AddSimpleStat(stats, scope, entityId, side, "evaluation", $"{fieldPrefix}:evaluation", GetInt(source, "valutazione") ?? GetInt(source, "efficiency"), confidence, context: context, warningPaths: [PathJoin(warningPathPrefix, "valutazione"), PathJoin(warningPathPrefix, "efficiency")]);
        AddSimpleStat(stats, scope, entityId, side, "points", $"{fieldPrefix}:points", GetInt(source, "punti"), confidence, context: context, warningPaths: [PathJoin(warningPathPrefix, "punti")]);

        if (TryGet(source, out var minutes, "minuti"))
        {
            var value = GetMinutesValue(minutes, out var raw);
            AddSimpleStat(stats, scope, entityId, side, "minutes", $"{fieldPrefix}:minutes", value, confidence, raw, context, [PathJoin(warningPathPrefix, "minuti")]);
        }
    }

    private static void AddShot(
        List<StatValue> stats,
        StatScope scope,
        string entityId,
        string? side,
        string fieldPrefix,
        string canonicalPrefix,
        string pythonKey,
        JsonElement source,
        double confidence,
        MappingContext context,
        string? warningPathPrefix)
    {
        if (!TryGet(source, out var shot, pythonKey))
        {
            return;
        }

        var made = GetInt(shot, "realizzati");
        var attempted = GetInt(shot, "tentati");
        var pct = GetDouble(shot, "percentuale");
        var rawRatio = made is null && attempted is null ? JsonRaw(shot) : $"{made}/{attempted}";
        var shotPath = PathJoin(warningPathPrefix, pythonKey);
        AddSimpleStat(stats, scope, entityId, side, $"{canonicalPrefix}.made", $"{fieldPrefix}:{canonicalPrefix}.made", made, confidence, rawRatio, context, [shotPath, PathJoin(shotPath, "realizzati")]);
        AddSimpleStat(stats, scope, entityId, side, $"{canonicalPrefix}.attempted", $"{fieldPrefix}:{canonicalPrefix}.attempted", attempted, confidence, rawRatio, context, [shotPath, PathJoin(shotPath, "tentati")]);
        AddSimpleStat(stats, scope, entityId, side, $"{canonicalPrefix}.percentage", $"{fieldPrefix}:{canonicalPrefix}.percentage", pct, confidence, pct?.ToString("0.0", CultureInfo.InvariantCulture), context, [shotPath, PathJoin(shotPath, "percentuale")]);
    }

    private static void AddSidePair(List<StatValue> stats, string gameId, string statKey, string fieldPrefix, JsonElement comp, string pythonKey, double confidence)
    {
        if (!TryGet(comp, out var pair, pythonKey))
        {
            return;
        }

        AddSimpleStat(stats, StatScope.Comparative, gameId, "Home", statKey, $"{fieldPrefix}.home", GetBestScalar(pair, "squadra_casa"), confidence);
        AddSimpleStat(stats, StatScope.Comparative, gameId, "Away", statKey, $"{fieldPrefix}.away", GetBestScalar(pair, "squadra_ospite"), confidence);
    }

    private static StatValue? AddSimpleStat(
        List<StatValue> stats,
        StatScope scope,
        string entityId,
        string? side,
        string statKey,
        string fieldId,
        object? value,
        double confidence,
        string? raw = null,
        MappingContext? context = null,
        IReadOnlyCollection<string?>? warningPaths = null)
    {
        if (value is null)
        {
            return null;
        }

        var stat = new StatValue
        {
            Scope = scope,
            EntityId = entityId,
            Side = side,
            StatKey = statKey,
            FieldId = fieldId,
            Value = value,
            Status = confidence >= 0.8 ? StatValidationStatus.Validato : StatValidationStatus.NonValidato,
            Score = confidence,
            Candidates =
            [
                new OcrCandidate
                {
                    Engine = EngineName,
                    FieldId = fieldId,
                    StatKey = statKey,
                    SourceFieldId = fieldId,
                    SourceEntityId = entityId,
                    Raw = raw ?? Convert.ToString(value, CultureInfo.InvariantCulture),
                    Normalized = value,
                    Confidence = confidence,
                    Status = OcrRunStatus.Success
                }
            ]
        };
        stats.Add(stat);

        if (context is not null && warningPaths is not null)
        {
            foreach (var warningPath in warningPaths.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                context.Register(warningPath!, stat);
            }
        }

        return stat;
    }

    private static void MapValidation(JsonElement root, ProcessingResult result, MappingContext context)
    {
        var reliability = GetString(root, "affidabilita", "livello_globale");
        result.Validation.Status = string.Equals(reliability, "alta", StringComparison.OrdinalIgnoreCase)
            ? FileProcessingStatus.CompletedValidated
            : FileProcessingStatus.CompletedWithWarnings;

        if (!TryGet(root, out var warnings, "affidabilita", "warnings") || warnings.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var warningElement in warnings.EnumerateArray())
        {
            var rawField = GetString(warningElement, "campo") ?? "python.warning";
            var rawMessage = GetString(warningElement, "motivo") ?? JsonRaw(warningElement);
            var targets = context.Resolve(rawField).ToArray();
            var warning = new ValidationWarning
            {
                RuleId = ClassifyOcrWarning(rawField, rawMessage),
                Severity = string.Equals(GetString(warningElement, "affidabilita"), "bassa", StringComparison.OrdinalIgnoreCase) ? "Error" : "Warning",
                FieldId = targets.FirstOrDefault()?.FieldId ?? rawField,
                FieldIds = targets.Select(x => x.FieldId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Message = $"{rawField}: {rawMessage}"
            };
            result.Validation.Warnings.Add(warning);

            foreach (var target in targets)
            {
                target.Warnings.Add(warning);
                target.Score = Math.Min(target.Score, 0.60);
                target.Status = StatValidationStatus.NonValidato;
            }
        }
    }

    private static string ClassifyOcrWarning(string rawField, string message)
    {
        if (message.Contains("Percentuale OCR incoerente", StringComparison.OrdinalIgnoreCase) ||
            rawField.EndsWith(".percentuale", StringComparison.OrdinalIgnoreCase))
        {
            return "ocr.percentageRecalculated";
        }

        if (message.Contains("Rapporto tiri stimato", StringComparison.OrdinalIgnoreCase))
        {
            return "ocr.shotRatioEstimated";
        }

        if (message.Contains("Tiri dal campo OCR incoerenti", StringComparison.OrdinalIgnoreCase))
        {
            return "ocr.fieldGoalsReconciled";
        }

        if (message.Contains("Totale squadra mancante", StringComparison.OrdinalIgnoreCase))
        {
            return "ocr.teamTotalReconstructed";
        }

        return "ocr.warning";
    }

    private static object? GetBestScalar(JsonElement parent, string property)
    {
        if (!TryGet(parent, out var value, property))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var i) => i,
            JsonValueKind.Number when value.TryGetDouble(out var d) => Math.Round(d, 3),
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Object => GetInt(value, "punti") ?? (object?)JsonRaw(value),
            _ => null
        };
    }

    private static object? GetMinutesValue(JsonElement minutes, out string? raw)
    {
        raw = GetString(minutes, "raw");
        if (!string.IsNullOrWhiteSpace(raw))
        {
            if (raw.Equals("N.E.", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("NE", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (TryParseMinutes(raw, out _))
            {
                return raw;
            }
        }

        var secondi = GetInt(minutes, "secondi");
        if (secondi is null)
        {
            return null;
        }

        var mm = secondi.Value / 60;
        var ss = secondi.Value % 60;
        return $"{mm:D2}:{ss:D2}";
    }

    private static bool TryParseMinutes(string value, out int seconds)
    {
        seconds = 0;
        var parts = value.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var secondsPart) ||
            secondsPart < 0 ||
            secondsPart >= 60)
        {
            return false;
        }

        seconds = minutes * 60 + secondsPart;
        return true;
    }

    private static string? TeamTotalsPath(string? teamName)
    {
        return string.IsNullOrWhiteSpace(teamName) ? null : $"squadre.{teamName}.totali_squadra";
    }

    private static string? PlayerPath(string? teamName, string? playerName)
    {
        return string.IsNullOrWhiteSpace(teamName) || string.IsNullOrWhiteSpace(playerName)
            ? null
            : $"squadre.{teamName}.giocatori.{playerName}";
    }

    private static string? PathJoin(string? prefix, string suffix)
    {
        return string.IsNullOrWhiteSpace(prefix) ? null : $"{prefix}.{suffix}";
    }

    private static double ConfidenceFromReliability(string? reliability)
    {
        return reliability?.ToLowerInvariant() switch
        {
            "alta" => 0.90,
            "media" => 0.65,
            "bassa" => 0.35,
            _ => 0.60
        };
    }

    private static bool TryGet(JsonElement element, out JsonElement value, params string[] path)
    {
        value = element;
        foreach (var part in path)
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(part, out value))
            {
                value = default;
                return false;
            }
        }

        return true;
    }

    private static string? GetString(JsonElement element, params string[] path)
    {
        return TryGet(element, out var value, path) && value.ValueKind != JsonValueKind.Null
            ? value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString()
            : null;
    }

    private static int? GetInt(JsonElement element, params string[] path)
    {
        if (!TryGet(element, out var value, path) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
            ? number
            : null;
    }

    private static double? GetDouble(JsonElement element, params string[] path)
    {
        if (!TryGet(element, out var value, path) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
        {
            return Math.Round(number, 1);
        }

        return value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
            ? Math.Round(number, 1)
            : null;
    }

    private static bool GetBool(JsonElement element, params string[] path)
    {
        return TryGet(element, out var value, path) && value.ValueKind == JsonValueKind.True;
    }

    private static string? GetNumberAsString(JsonElement element, params string[] path)
    {
        var intValue = GetInt(element, path);
        return intValue?.ToString(CultureInfo.InvariantCulture);
    }

    private static string JsonRaw(JsonElement element) => element.GetRawText();

    private static string ShortHash(string documentHash)
    {
        var normalized = documentHash.Replace("sha256:", string.Empty, StringComparison.OrdinalIgnoreCase);
        return normalized.Length <= 12 ? normalized : normalized[..12];
    }

    private sealed class MappingContext
    {
        private readonly Dictionary<string, List<StatValue>> _warningPathMap = new(StringComparer.OrdinalIgnoreCase);

        public void Register(string warningPath, StatValue stat)
        {
            if (!_warningPathMap.TryGetValue(warningPath, out var stats))
            {
                stats = [];
                _warningPathMap[warningPath] = stats;
            }

            if (!stats.Contains(stat))
            {
                stats.Add(stat);
            }
        }

        public IReadOnlyList<StatValue> Resolve(string warningPath)
        {
            return _warningPathMap.TryGetValue(warningPath, out var stats)
                ? stats
                : [];
        }
    }
}
