using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Enums;
using BasketPdfStats.Core.Identity;
using BasketPdfStats.Core.Models;
using BasketPdfStats.Core.Ocr;
using BasketPdfStats.Core.Pipeline;
using BasketPdfStats.Infrastructure.Configuration;
using BasketPdfStats.Infrastructure.Pipeline;
using BasketPdfStats.Ocr.Mock;

namespace BasketPdfStats.Tests;

public sealed class MultiEnginePipelineTests
{
    [Fact]
    public async Task Failed_engine_does_not_block_successful_mock_engine()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var uploadFolder = Path.Combine(root, "Uploads");
            Directory.CreateDirectory(uploadFolder);
            var pdf = Path.Combine(uploadFolder, "sample.pdf");
            await File.WriteAllBytesAsync(pdf, [9, 8, 7, 6]);

            var pipeline = new PdfProcessingPipeline(options, [new FailedOcrEngine(), new MockOcrEngine()]);
            var result = await pipeline.ProcessPdfAsync(pdf);

            Assert.NotEqual(FileProcessingStatus.Failed, result.ProcessedFile.Status);
            Assert.Contains(result.OcrRuns, x => x.Engine == "FailedOcr" && x.Status == OcrRunStatus.Failed);
            Assert.Contains(result.OcrRuns, x => x.Engine == "MockOcr" && x.Status == OcrRunStatus.Success);
            Assert.True(File.Exists(result.ProcessedFile.OutputJsonPath));
            Assert.True(File.Exists(result.ProcessedFile.FinalPath));
            Assert.Equal(pdf, result.ProcessedFile.FinalPath);
            Assert.True(File.Exists(pdf));
            Assert.False(ContainsPdf(options.ProcessedPath));
            Assert.Empty(Directory.EnumerateFiles(options.WorkingPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Failed_processing_preserves_original_selected_pdf()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var uploadFolder = Path.Combine(root, "Uploads");
            Directory.CreateDirectory(uploadFolder);
            var pdf = Path.Combine(uploadFolder, "failed.pdf");
            await File.WriteAllBytesAsync(pdf, [9, 8, 7, 6]);

            var pipeline = new PdfProcessingPipeline(options, [new FailedOcrEngine()]);
            var result = await pipeline.ProcessPdfAsync(pdf);

            Assert.Equal(FileProcessingStatus.Failed, result.ProcessedFile.Status);
            Assert.Equal(pdf, result.ProcessedFile.FinalPath);
            Assert.True(File.Exists(pdf));
            Assert.False(ContainsPdf(options.ErrorPath));
            Assert.Empty(Directory.EnumerateFiles(options.WorkingPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Match_context_is_passed_to_engines()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var uploadFolder = Path.Combine(root, "Uploads");
            Directory.CreateDirectory(uploadFolder);
            var pdf = Path.Combine(uploadFolder, "ctx.pdf");
            await File.WriteAllBytesAsync(pdf, [1, 2, 3, 4]);

            var capturing = new CapturingOcrEngine();
            var pipeline = new PdfProcessingPipeline(options, [capturing]);
            var context = new OcrMatchContext { MatchId = 42 };

            await pipeline.ProcessPdfAsync(pdf, new OcrRunSelection { UseTesseract = true, UsePaddle = true }, context);

            Assert.True(capturing.Captured);
            Assert.NotNull(capturing.CapturedContext);
            Assert.Equal(42, capturing.CapturedContext!.MatchId);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Match_context_is_null_when_not_provided()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var uploadFolder = Path.Combine(root, "Uploads");
            Directory.CreateDirectory(uploadFolder);
            var pdf = Path.Combine(uploadFolder, "noctx.pdf");
            await File.WriteAllBytesAsync(pdf, [1, 2, 3, 4]);

            var capturing = new CapturingOcrEngine();
            var pipeline = new PdfProcessingPipeline(options, [capturing]);

            await pipeline.ProcessPdfAsync(pdf);

            Assert.True(capturing.Captured);
            Assert.Null(capturing.CapturedContext);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Pipeline_realigns_home_away_and_sets_flag_on_inversion()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var uploadFolder = Path.Combine(root, "Uploads");
            Directory.CreateDirectory(uploadFolder);
            var pdf = Path.Combine(uploadFolder, "inv.pdf");
            await File.WriteAllBytesAsync(pdf, [1, 2, 3, 4]);

            // OCR legge Home=Pall, Away=Virtus; il DB dice il contrario → inversione.
            var engine = new TeamResultOcrEngine(homeName: "Pall", awayName: "Virtus", finalScore: "52-44");
            var pipeline = new PdfProcessingPipeline(options, [engine]);
            var context = new OcrMatchContext
            {
                MatchId = 1,
                HomeTeam = new OcrContextTeam { TeamId = 1, Name = "Virtus" },
                AwayTeam = new OcrContextTeam { TeamId = 2, Name = "Pall" },
            };

            var result = await pipeline.ProcessPdfAsync(pdf, new OcrRunSelection { UseTesseract = true, UsePaddle = true }, context);

            Assert.True(result.Reconciliation?.SideInversionApplied);
            Assert.Equal("Virtus", result.Teams.Single(t => t.Side == "Home").Name);
            Assert.Equal("44-52", result.Game.FinalScore);
            Assert.Equal("player:Away:jersey:5", result.Players.Single().EntityId);
            Assert.Contains(result.Validation.Warnings, w => w.RuleId == "team.sideInversionApplied");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Pipeline_adds_team_mismatch_warning_when_teams_do_not_match()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var uploadFolder = Path.Combine(root, "Uploads");
            Directory.CreateDirectory(uploadFolder);
            var pdf = Path.Combine(uploadFolder, "mismatch.pdf");
            await File.WriteAllBytesAsync(pdf, [1, 2, 3, 4]);

            var engine = new TeamResultOcrEngine(homeName: "Lakers", awayName: "Celtics", finalScore: "80-70");
            var pipeline = new PdfProcessingPipeline(options, [engine]);
            var context = new OcrMatchContext
            {
                MatchId = 1,
                HomeTeam = new OcrContextTeam { TeamId = 1, Name = "Virtus" },
                AwayTeam = new OcrContextTeam { TeamId = 2, Name = "Pall" },
            };

            var result = await pipeline.ProcessPdfAsync(pdf, new OcrRunSelection { UseTesseract = true, UsePaddle = true }, context);

            Assert.Contains(result.Validation.Warnings, w => w.RuleId == "team.mismatch");
            Assert.False(result.Reconciliation?.SideInversionApplied ?? false);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Pipeline_sets_review_required_status_on_player_conflict()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var uploadFolder = Path.Combine(root, "Uploads");
            Directory.CreateDirectory(uploadFolder);
            var pdf = Path.Combine(uploadFolder, "review.pdf");
            await File.WriteAllBytesAsync(pdf, [1, 2, 3, 4]);

            // OCR: squadre coerenti col DB, ma un giocatore Home ha nome incompatibile col jersey.
            var engine = new RosterResultOcrEngine();
            var pipeline = new PdfProcessingPipeline(options, [engine]);
            var context = new OcrMatchContext
            {
                MatchId = 1,
                HomeTeam = new OcrContextTeam
                {
                    TeamId = 1,
                    Name = "Virtus",
                    Players = [new OcrRosterPlayer { PlayerId = 1, TeamId = 1, FirstName = "Mario", LastName = "Rossi", JerseyNumber = 5 }],
                },
                AwayTeam = new OcrContextTeam { TeamId = 2, Name = "Pall", Players = [] },
            };

            var result = await pipeline.ProcessPdfAsync(pdf, new OcrRunSelection { UseTesseract = true, UsePaddle = true }, context);

            Assert.Equal(FileProcessingStatus.CompletedWithReviewRequired, result.ProcessedFile.Status);
            Assert.Contains(result.IdentityReview, i => i.Reason == IdentityReviewReason.Conflict);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Mismatch_with_decider_abort_skips_remaining_channels()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var uploadFolder = Path.Combine(root, "Uploads");
            Directory.CreateDirectory(uploadFolder);
            var pdf = Path.Combine(uploadFolder, "abort.pdf");
            await File.WriteAllBytesAsync(pdf, [1, 2, 3, 4]);

            var ch1 = new TeamResultOcrEngine(homeName: "Lakers", awayName: "Celtics", finalScore: "80-70");
            var laterChannel = new TrackingOcrEngine(OcrStrategyNames.PaddleTableRows);
            var decider = new StubDecider(continueProcessing: false); // interrompi (consigliato)
            var pipeline = new PdfProcessingPipeline(options, [ch1, laterChannel], teamMismatchDecider: decider);
            var context = new OcrMatchContext
            {
                MatchId = 1,
                HomeTeam = new OcrContextTeam { TeamId = 1, Name = "Virtus" },
                AwayTeam = new OcrContextTeam { TeamId = 2, Name = "Pall" },
            };

            var result = await pipeline.ProcessPdfAsync(pdf, new OcrRunSelection { UseTesseract = true, UsePaddle = true }, context);

            Assert.True(decider.WasAsked);
            Assert.False(laterChannel.WasCalled); // CH2–CH4 saltati
            Assert.Contains(result.Validation.Warnings, w => w.RuleId == "ocr.processingAbortedByUser");
            Assert.Contains(result.Validation.Warnings, w => w.RuleId == "team.mismatch");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Mismatch_with_decider_continue_runs_remaining_channels()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var uploadFolder = Path.Combine(root, "Uploads");
            Directory.CreateDirectory(uploadFolder);
            var pdf = Path.Combine(uploadFolder, "continue.pdf");
            await File.WriteAllBytesAsync(pdf, [1, 2, 3, 4]);

            var ch1 = new TeamResultOcrEngine(homeName: "Lakers", awayName: "Celtics", finalScore: "80-70");
            var laterChannel = new TrackingOcrEngine(OcrStrategyNames.PaddleTableRows);
            var decider = new StubDecider(continueProcessing: true); // completa comunque
            var pipeline = new PdfProcessingPipeline(options, [ch1, laterChannel], teamMismatchDecider: decider);
            var context = new OcrMatchContext
            {
                MatchId = 1,
                HomeTeam = new OcrContextTeam { TeamId = 1, Name = "Virtus" },
                AwayTeam = new OcrContextTeam { TeamId = 2, Name = "Pall" },
            };

            var result = await pipeline.ProcessPdfAsync(pdf, new OcrRunSelection { UseTesseract = true, UsePaddle = true }, context);

            Assert.True(decider.WasAsked);
            Assert.True(laterChannel.WasCalled); // CH2–CH4 eseguiti
            Assert.DoesNotContain(result.Validation.Warnings, w => w.RuleId == "ocr.processingAbortedByUser");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Matching_teams_do_not_invoke_decider_and_run_all_channels()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var uploadFolder = Path.Combine(root, "Uploads");
            Directory.CreateDirectory(uploadFolder);
            var pdf = Path.Combine(uploadFolder, "ok.pdf");
            await File.WriteAllBytesAsync(pdf, [1, 2, 3, 4]);

            var ch1 = new TeamResultOcrEngine(homeName: "Virtus", awayName: "Pall", finalScore: "80-70");
            var laterChannel = new TrackingOcrEngine(OcrStrategyNames.PaddleTableRows);
            var decider = new StubDecider(continueProcessing: false);
            var pipeline = new PdfProcessingPipeline(options, [ch1, laterChannel], teamMismatchDecider: decider);
            var context = new OcrMatchContext
            {
                MatchId = 1,
                HomeTeam = new OcrContextTeam { TeamId = 1, Name = "Virtus" },
                AwayTeam = new OcrContextTeam { TeamId = 2, Name = "Pall" },
            };

            var result = await pipeline.ProcessPdfAsync(pdf, new OcrRunSelection { UseTesseract = true, UsePaddle = true }, context);

            Assert.False(decider.WasAsked); // squadre coerenti → nessuna popup
            Assert.True(laterChannel.WasCalled);
            Assert.DoesNotContain(result.Validation.Warnings, w => w.RuleId == "ocr.processingAbortedByUser");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Pipeline_removes_ocr_diagnostic_warnings_in_normal_mode()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var uploadFolder = Path.Combine(root, "Uploads");
            Directory.CreateDirectory(uploadFolder);
            var pdf = Path.Combine(uploadFolder, "noise.pdf");
            await File.WriteAllBytesAsync(pdf, [1, 2, 3, 4]);

            // Due provider con valore in conflitto → il reconciler genera ocr.reconciliation.conflict,
            // che la pipeline deve rimuovere in modalità normale.
            var pipeline = new PdfProcessingPipeline(options,
            [
                new StatSetOcrEngine(OcrStrategyNames.TesseractFullPage, ("points", 12)),
                new StatSetOcrEngine(OcrStrategyNames.TesseractLayoutCrops, ("points", 14)),
            ]);

            var result = await pipeline.ProcessPdfAsync(pdf);

            Assert.DoesNotContain(result.Validation.Warnings, w => w.RuleId.StartsWith("ocr.reconciliation", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(result.Validation.Warnings, w => w.RuleId.StartsWith("ocr.", StringComparison.OrdinalIgnoreCase));
            var stat = Assert.Single(result.Stats);
            Assert.Empty(stat.Warnings);
            Assert.Equal(StatValidationStatus.Validato, stat.Status);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Pipeline_keeps_math_warning_and_emits_clean_json()
    {
        var root = Path.Combine(Path.GetTempPath(), "BasketPdfStatsTests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = new RuntimeOptions { RuntimeRoot = root };
            var uploadFolder = Path.Combine(root, "Uploads");
            Directory.CreateDirectory(uploadFolder);
            var pdf = Path.Combine(uploadFolder, "math.pdf");
            await File.WriteAllBytesAsync(pdf, [1, 2, 3, 4]);

            // points=20 ma 2*5+3*2+1=17 → math.pointsFormula deve restare.
            var pipeline = new PdfProcessingPipeline(options,
            [
                new StatSetOcrEngine(OcrStrategyNames.TesseractFullPage,
                    ("points", 20), ("twoPoints.made", 5), ("threePoints.made", 2), ("freeThrows.made", 1)),
            ]);

            var result = await pipeline.ProcessPdfAsync(pdf);

            Assert.Contains(result.Validation.Warnings, w => w.RuleId == "math.pointsFormula");

            var json = await File.ReadAllTextAsync(result.ProcessedFile.OutputJsonPath!);
            Assert.Contains("math.pointsFormula", json);
            Assert.DoesNotContain("ocr.reconciliation", json);
            Assert.DoesNotContain("providerValues", json);
            Assert.DoesNotContain("\"candidates\"", json); // [JsonIgnore]: chiave assente
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static bool ContainsPdf(string folder) =>
        Directory.Exists(folder) && Directory.EnumerateFiles(folder, "*.pdf").Any();

    private sealed class StatSetOcrEngine : IOcrEngine
    {
        private readonly (string Key, int Value)[] _stats;
        public StatSetOcrEngine(string engineName, params (string, int)[] stats)
        {
            EngineName = engineName;
            _stats = stats;
        }

        public string EngineName { get; }

        public Task<ProcessingResult> ProcessAsync(OcrProcessingRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProcessingResult
            {
                OcrRuns = [new OcrRunResult { Engine = EngineName, Status = OcrRunStatus.Success }],
                Stats = _stats.Select(s => new StatValue
                {
                    Scope = StatScope.Team,
                    Side = "Home",
                    EntityId = "team:Home",
                    StatKey = s.Key,
                    FieldId = $"team:Home:{s.Key}",
                    Value = s.Value,
                    Score = 0.9,
                    Status = StatValidationStatus.Validato,
                }).ToList(),
            });
        }
    }

    private sealed class StubDecider : ITeamMismatchProcessingDecider
    {
        private readonly bool _continue;
        public StubDecider(bool continueProcessing) => _continue = continueProcessing;
        public bool WasAsked { get; private set; }

        public bool ShouldContinueProcessing(TeamMismatchPrompt prompt)
        {
            WasAsked = true;
            return _continue;
        }
    }

    private sealed class TrackingOcrEngine : IOcrEngine
    {
        public TrackingOcrEngine(string engineName) => EngineName = engineName;
        public string EngineName { get; }
        public bool WasCalled { get; private set; }

        public Task<ProcessingResult> ProcessAsync(OcrProcessingRequest request, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(new ProcessingResult
            {
                OcrRuns = [new OcrRunResult { Engine = EngineName, Status = OcrRunStatus.Success }],
            });
        }
    }

    private sealed class RosterResultOcrEngine : IOcrEngine
    {
        public string EngineName => OcrStrategyNames.TesseractFullPage;

        public Task<ProcessingResult> ProcessAsync(OcrProcessingRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProcessingResult
            {
                OcrRuns = [new OcrRunResult { Engine = EngineName, Status = OcrRunStatus.Success }],
                Teams =
                [
                    new TeamStats { Side = "Home", TeamId = "team:Home", Name = "Virtus" },
                    new TeamStats { Side = "Away", TeamId = "team:Away", Name = "Pall" },
                ],
                Players =
                [
                    new PlayerStats { EntityId = "player:Home:jersey:5", TeamId = "team:Home", Side = "Home", Number = "5", FullName = "Giovanni Sconosciuto" },
                ],
                Game = new GameStats { HomeTeamId = "team:Home", AwayTeamId = "team:Away" },
            });
        }
    }

    private sealed class TeamResultOcrEngine : IOcrEngine
    {
        private readonly string _homeName;
        private readonly string _awayName;
        private readonly string _finalScore;

        public TeamResultOcrEngine(string homeName, string awayName, string finalScore)
        {
            _homeName = homeName;
            _awayName = awayName;
            _finalScore = finalScore;
        }

        public string EngineName => OcrStrategyNames.TesseractFullPage;

        public Task<ProcessingResult> ProcessAsync(OcrProcessingRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProcessingResult
            {
                OcrRuns = [new OcrRunResult { Engine = EngineName, Status = OcrRunStatus.Success }],
                Teams =
                [
                    new TeamStats { Side = "Home", TeamId = "team:Home", Name = _homeName },
                    new TeamStats { Side = "Away", TeamId = "team:Away", Name = _awayName },
                ],
                Players =
                [
                    new PlayerStats { EntityId = "player:Home:jersey:5", TeamId = "team:Home", Side = "Home" },
                ],
                Game = new GameStats
                {
                    HomeTeamId = "team:Home",
                    AwayTeamId = "team:Away",
                    FinalScore = _finalScore,
                },
            });
        }
    }

    private sealed class CapturingOcrEngine : IOcrEngine
    {
        public bool Captured { get; private set; }
        public OcrMatchContext? CapturedContext { get; private set; }

        // Nome TesseractFullPage così da superare il filtro di selezione del pipeline.
        public string EngineName => OcrStrategyNames.TesseractFullPage;

        public Task<ProcessingResult> ProcessAsync(OcrProcessingRequest request, CancellationToken cancellationToken = default)
        {
            Captured = true;
            CapturedContext = request.MatchContext;
            return Task.FromResult(new ProcessingResult
            {
                OcrRuns = [new OcrRunResult { Engine = EngineName, Status = OcrRunStatus.Success }]
            });
        }
    }

    private sealed class FailedOcrEngine : IOcrEngine
    {
        public string EngineName => "FailedOcr";

        public Task<ProcessingResult> ProcessAsync(OcrProcessingRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProcessingResult
            {
                OcrRuns =
                [
                    new OcrRunResult
                    {
                        Engine = EngineName,
                        Status = OcrRunStatus.Failed,
                        Error = "simulated failure"
                    }
                ]
            });
        }
    }
}
