using System.Text.Json;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Core.Stats;
using BasketPdfStats.Ocr.TesseractPython;

namespace BasketPdfStats.Tests;

public sealed class PythonOutputMapperTests
{
    [Fact]
    public void Maps_python_json_to_normalized_processing_result()
    {
        using var document = JsonDocument.Parse("""
        {
          "metadata": { "nome_file": "sample.pdf", "numero_pagine": 1 },
          "partita": {
            "competizione": "Piazzetta Madness",
            "campo": "Piazzetta Verde",
            "data": "venerdi 29 maggio 2026",
            "ora": "21:00",
            "arbitri": ["A", "B"]
          },
          "risultato": {
            "squadra_casa": { "nome": "Miami Spritz", "abbreviazione": "MIA", "punti_finali": 48 },
            "squadra_ospite": { "nome": "Philadelphia 70Sexers", "abbreviazione": "PHI", "punti_finali": 26 },
            "punteggio_finale": "48-26",
            "periodi": [
              { "numero": 1, "label": "Q1", "squadra_casa": 29, "squadra_ospite": 17 }
            ]
          },
          "squadre": [
            {
              "tipo": "casa",
              "nome": "Miami Spritz",
              "abbreviazione": "MIA",
              "giocatori": [
                {
                  "numero": "7",
                  "nome_completo": "Raoul PIERGENTILI",
                  "nome": "Raoul",
                  "cognome": "PIERGENTILI",
                  "capitano": false,
                  "non_entrato": false,
                  "minuti": { "raw": "20:00", "secondi": 1200 },
                  "punti": 10,
                  "tiri_dal_campo": { "realizzati": 5, "tentati": 10, "percentuale": 50.0 }
                }
              ],
              "totali_squadra": {
                "minuti": { "raw": "40:00", "secondi": 2400 },
                "tiri_dal_campo": { "realizzati": 19, "tentati": 42, "percentuale": 45.2 },
                "tiri_da_2": { "realizzati": 13, "tentati": 23, "percentuale": 56.5 },
                "tiri_da_3": { "realizzati": 6, "tentati": 19, "percentuale": 31.6 },
                "tiri_liberi": { "realizzati": 4, "tentati": 13, "percentuale": 30.8 },
                "rimbalzi": { "offensivi": 8, "difensivi": 21, "totali": 29 },
                "assist": 3,
                "palle_perse": 8,
                "palle_recuperate": 7,
                "stoppate_date": 1,
                "falli": { "commessi": 7, "subiti": 10 },
                "plus_minus": 12,
                "valutazione": 51,
                "punti": 48
              }
            }
          ],
          "statistiche_comparative": {
            "punti_da_palle_perse": { "squadra_casa": 11, "squadra_ospite": 3 },
            "cambi_guida": 1,
            "parita": 0
          },
          "affidabilita": {
            "livello_globale": "alta",
            "warnings": []
          }
        }
        """);

        var mapper = new PythonOutputMapper();
        var result = mapper.Map(document, new OcrProcessingRequest
        {
            OriginalFileName = "sample.pdf",
            DocumentHash = "sha256:abcdef123456"
        });

        Assert.Equal("Piazzetta Madness", result.Game.Competition);
        Assert.Equal("48-26", result.Game.FinalScore);
        Assert.Contains(result.Teams, x => x.TeamId == "team:Home");
        Assert.Contains(result.Players, x => x.PlayerId == "player:Home:jersey:7" && x.Number == "7");
        Assert.Contains(result.Stats, x => x.FieldId == "team:Home:points" && x.Value?.ToString() == "48");
        Assert.Contains(result.Stats, x => x.FieldId == "team:Home:evaluation" && x.StatKey == "evaluation" && x.Value?.ToString() == "51");
        Assert.Contains(result.Stats, x => x.StatKey == "period.points" && x.FieldId == "result.period.Q1.home");
        Assert.DoesNotContain(result.Stats, stat =>
            stat.EntityId.StartsWith("team:MIA", StringComparison.OrdinalIgnoreCase) ||
            stat.EntityId.StartsWith("player:MIA:", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(stat.StatKey, "efficiency", StringComparison.OrdinalIgnoreCase));
        Assert.All(result.Stats, stat => Assert.True(StatKeyRegistry.Contains(stat.StatKey), $"Missing StatKeyRegistry entry: {stat.StatKey}"));
        Assert.Contains(result.Stats.SelectMany(x => x.Candidates), x => x.Engine == OcrStrategyNames.TesseractFullPage && x.Status == OcrRunStatus.Success);
        Assert.All(result.Players, player =>
            Assert.Contains(result.Stats, stat => stat.Scope == StatScope.Player && stat.EntityId == player.EntityId));
    }

    [Fact]
    public void Maps_legacy_efficiency_to_canonical_evaluation()
    {
        using var document = JsonDocument.Parse("""
        {
          "partita": {},
          "risultato": { "squadra_casa": {}, "squadra_ospite": {}, "periodi": [] },
          "squadre": [
            {
              "nome": "Home",
              "totali_squadra": { "efficiency": 17 }
            }
          ],
          "statistiche_comparative": {},
          "affidabilita": { "livello_globale": "alta", "warnings": [] }
        }
        """);

        var result = new PythonOutputMapper().Map(document, new OcrProcessingRequest { DocumentHash = "sha256:abc" });
        var evaluation = Assert.Single(result.Stats);

        Assert.Equal("evaluation", evaluation.StatKey);
        Assert.Equal("team:Home:evaluation", evaluation.FieldId);
        Assert.Equal(17, evaluation.Value);
        Assert.DoesNotContain(result.Stats, stat => string.Equals(stat.StatKey, "efficiency", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Uses_name_fallback_when_jersey_numbers_are_duplicated()
    {
        using var document = JsonDocument.Parse("""
        {
          "partita": {},
          "risultato": {
            "squadra_casa": { "abbreviazione": "SPR", "punti_finali": 6 },
            "squadra_ospite": { "abbreviazione": "AWY", "punti_finali": 0 },
            "periodi": []
          },
          "squadre": [
            {
              "nome": "Saluta Andonio Spurs",
              "abbreviazione": "SPR",
              "giocatori": [
                { "numero": "4", "nome_completo": "Federico POMPOZZI", "minuti": { "raw": "10:00", "secondi": 600 }, "punti": 2 },
                { "numero": "4", "nome_completo": "Fabio RICCI", "minuti": { "raw": "11:00", "secondi": 660 }, "punti": 4 }
              ]
            }
          ],
          "statistiche_comparative": {},
          "affidabilita": { "livello_globale": "alta", "warnings": [] }
        }
        """);

        var result = new PythonOutputMapper().Map(document, new OcrProcessingRequest
        {
            OriginalFileName = "sample.pdf",
            DocumentHash = "sha256:abcdef123456"
        });

        Assert.Contains(result.Players, x => x.PlayerId == "player:Home:jersey:4" && x.Number == "4" && x.FullName == "Federico POMPOZZI");
        Assert.Contains(result.Players, x => x.PlayerId == "player:Home:name:fabio-ricci" && x.Number == "4" && x.FullName == "Fabio RICCI");
        Assert.DoesNotContain(result.Stats.GroupBy(x => x.FieldId), x => x.Count() > 1);
        Assert.Contains(result.Validation.Warnings, warning => warning.RuleId == "ocr.duplicateJerseyDetected");
    }

    [Fact]
    public void Maps_starter_marker_and_skips_special_player_rows()
    {
        using var document = JsonDocument.Parse("""
        {
          "partita": {},
          "risultato": { "squadra_casa": { "abbreviazione": "MIA" }, "squadra_ospite": { "abbreviazione": "SPR" }, "periodi": [] },
          "squadre": [
            {
              "nome": "Miami Spritz",
              "abbreviazione": "MIA",
              "giocatori": [
                { "numero": "*7", "nome_completo": "Starter Player", "punti": 5 },
                { "numero": "Squadra/Allenatore", "punti": 2 },
                { "nome_completo": "Totali", "punti": 7 }
              ],
              "totali_squadra": { "punti": 7 }
            }
          ],
          "statistiche_comparative": {},
          "affidabilita": { "livello_globale": "alta", "warnings": [] }
        }
        """);

        var result = new PythonOutputMapper().Map(document, new OcrProcessingRequest { DocumentHash = "sha256:abc" });

        var player = Assert.Single(result.Players);
        Assert.Equal("player:Home:jersey:7", player.PlayerId);
        Assert.Equal(player.PlayerId, player.EntityId);
        Assert.Equal("player:MIA:row:1", player.SourceEntityId);
        Assert.Equal("7", player.Number);
        Assert.True(player.Starter);
        Assert.Contains(result.Stats, stat => stat.FieldId == "team:Home:points");
        Assert.Contains(result.Stats, stat => stat.Scope == StatScope.Player && stat.EntityId == player.EntityId);
        Assert.DoesNotContain(result.Players, candidate => candidate.FullName == "Totali" || candidate.Number == "Squadra/Allenatore");
    }

    [Theory]
    [InlineData("19:45")]
    [InlineData("150:00")]
    public void Minutes_value_is_raw_mmss_string(string rawMinutes)
    {
        using var document = JsonDocument.Parse($$"""
        {
          "partita": {},
          "risultato": { "squadra_casa": { "abbreviazione": "MIA" }, "squadra_ospite": { "abbreviazione": "SPR" }, "periodi": [] },
          "squadre": [
            {
              "nome": "Miami Spritz",
              "abbreviazione": "MIA",
              "giocatori": [
                { "numero": "7", "nome_completo": "Player One", "minuti": { "raw": "{{rawMinutes}}" }, "punti": 1 }
              ]
            }
          ],
          "statistiche_comparative": {},
          "affidabilita": { "livello_globale": "alta", "warnings": [] }
        }
        """);

        var result = new PythonOutputMapper().Map(document, new OcrProcessingRequest { DocumentHash = "sha256:abc" });
        var minutes = Assert.Single(result.Stats, x => x.StatKey == "minutes");

        Assert.Equal(rawMinutes, minutes.Value as string);
        Assert.Equal(rawMinutes, minutes.Candidates[0].Raw);
    }

    [Fact]
    public void Skips_minutes_stat_when_player_did_not_play()
    {
        using var document = JsonDocument.Parse("""
        {
          "partita": {},
          "risultato": { "squadra_casa": { "abbreviazione": "MIA" }, "squadra_ospite": { "abbreviazione": "SPR" }, "periodi": [] },
          "squadre": [
            {
              "nome": "Miami Spritz",
              "abbreviazione": "MIA",
              "giocatori": [
                { "numero": "3", "nome_completo": "Bench Player", "non_entrato": true, "minuti": { "raw": "N.E." } }
              ]
            }
          ],
          "statistiche_comparative": {},
          "affidabilita": { "livello_globale": "alta", "warnings": [] }
        }
        """);

        var result = new PythonOutputMapper().Map(document, new OcrProcessingRequest { DocumentHash = "sha256:abc" });

        Assert.DoesNotContain(result.Stats, x => x.StatKey == "minutes");
    }

    [Fact]
    public void Links_raw_warning_to_canonical_stat_value_field_id_when_possible()
    {
        using var document = JsonDocument.Parse("""
        {
          "partita": {},
          "risultato": { "squadra_casa": { "abbreviazione": "MIA" }, "squadra_ospite": { "abbreviazione": "SPR" }, "periodi": [] },
          "squadre": [
            {
              "nome": "Miami Spritz",
              "abbreviazione": "MIA",
              "totali_squadra": {
                "tiri_da_2": { "realizzati": 4, "tentati": 8, "percentuale": 50.0 }
              }
            }
          ],
          "statistiche_comparative": {},
          "affidabilita": {
            "livello_globale": "media",
            "warnings": [
              {
                "campo": "squadre.Miami Spritz.totali_squadra.tiri_da_2.percentuale",
                "motivo": "Percentuale calcolata.",
                "affidabilita": "media"
              }
            ]
          }
        }
        """);

        var result = new PythonOutputMapper().Map(document, new OcrProcessingRequest { DocumentHash = "sha256:abc" });
        var stat = Assert.Single(result.Stats, x => x.FieldId == "team:Home:twoPoints.percentage");
        var warning = Assert.Single(stat.Warnings);

        Assert.Equal("ocr.percentageRecalculated", warning.RuleId);
        Assert.Equal("team:Home:twoPoints.percentage", warning.FieldId);
        Assert.Contains("team:Home:twoPoints.percentage", warning.FieldIds);
        Assert.Contains(result.Validation.Warnings, x => x.FieldId == "team:Home:twoPoints.percentage");
    }

    [Fact]
    public void Classifies_shot_ratio_warning_with_stable_rule_id()
    {
        using var document = JsonDocument.Parse("""
        {
          "partita": {},
          "risultato": { "squadra_casa": { "abbreviazione": "MIA" }, "squadra_ospite": { "abbreviazione": "SPR" }, "periodi": [] },
          "squadre": [
            {
              "nome": "Miami Spritz",
              "abbreviazione": "MIA",
              "giocatori": [
                {
                  "numero": "7",
                  "nome_completo": "Raoul PIERGENTILI",
                  "tiri_dal_campo": { "realizzati": 5, "tentati": 10, "percentuale": 50.0 }
                }
              ]
            }
          ],
          "statistiche_comparative": {},
          "affidabilita": {
            "livello_globale": "media",
            "warnings": [
              {
                "campo": "squadre.Miami Spritz.giocatori.Raoul PIERGENTILI.tiri_dal_campo",
                "motivo": "Rapporto tiri stimato/corretto da OCR senza separatore '/' o poco leggibile.",
                "affidabilita": "media"
              }
            ]
          }
        }
        """);

        var result = new PythonOutputMapper().Map(document, new OcrProcessingRequest { DocumentHash = "sha256:abc" });
        var warning = Assert.Single(result.Validation.Warnings);

        Assert.Equal("ocr.shotRatioEstimated", warning.RuleId);
        Assert.Contains("squadre.Miami Spritz.giocatori.Raoul PIERGENTILI.tiri_dal_campo", warning.Message);
        Assert.Contains("player:Home:jersey:7:fieldGoals.made", warning.FieldIds);
    }

    [Fact]
    public void Classifies_field_goal_reconciliation_warning_with_stable_rule_id()
    {
        using var document = JsonDocument.Parse("""
        {
          "partita": {},
          "risultato": { "squadra_casa": { "abbreviazione": "MIA" }, "squadra_ospite": { "abbreviazione": "SPR" }, "periodi": [] },
          "squadre": [
            {
              "nome": "Miami Spritz",
              "abbreviazione": "MIA",
              "totali_squadra": {
                "tiri_dal_campo": { "realizzati": 5, "tentati": 10, "percentuale": 50.0 }
              }
            }
          ],
          "statistiche_comparative": {},
          "affidabilita": {
            "livello_globale": "media",
            "warnings": [
              {
                "campo": "squadre.Miami Spritz.totali_squadra.tiri_dal_campo",
                "motivo": "Tiri dal campo OCR incoerenti con 2P+3P: usata somma delle colonne 2P e 3P.",
                "affidabilita": "media"
              }
            ]
          }
        }
        """);

        var result = new PythonOutputMapper().Map(document, new OcrProcessingRequest { DocumentHash = "sha256:abc" });

        Assert.Contains(result.Validation.Warnings, x => x.RuleId == "ocr.fieldGoalsReconciled");
    }
}
