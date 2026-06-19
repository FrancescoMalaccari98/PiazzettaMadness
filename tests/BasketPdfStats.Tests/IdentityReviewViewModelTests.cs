using BasketPdfStats.App.ViewModels;
using BasketPdfStats.Core.Database;
using BasketPdfStats.Core.Identity;

namespace BasketPdfStats.Tests;

public sealed class IdentityReviewViewModelTests
{
    private static OcrMatchContext Context() => new()
    {
        MatchId = 1,
        HomeTeam = new OcrContextTeam
        {
            TeamId = 1,
            Name = "Home",
            Players = [new OcrRosterPlayer { PlayerId = 1, TeamId = 1, FirstName = "Mario", LastName = "Rossi", JerseyNumber = 5 }],
        },
        AwayTeam = new OcrContextTeam
        {
            TeamId = 2,
            Name = "Away",
            Players = [new OcrRosterPlayer { PlayerId = 2, TeamId = 2, FirstName = "Luca", LastName = "Bianchi", JerseyNumber = 7 }],
        },
    };

    [Fact]
    public void Candidates_come_only_from_the_correct_side()
    {
        var items = new List<IdentityReviewItem>
        {
            new() { Side = "Away", Reason = IdentityReviewReason.Conflict, OcrJersey = "7", OcrName = "X" },
        };

        var vm = new IdentityReviewViewModel(items, Context());
        var row = Assert.Single(vm.Rows);

        Assert.All(row.Candidates, c => Assert.Equal(2, c.TeamId));
        Assert.DoesNotContain(row.Candidates, c => c.PlayerId == 1); // nessun giocatore della squadra opposta
    }

    [Fact]
    public void Conflict_blocks_export_until_resolved_with_a_candidate()
    {
        var items = new List<IdentityReviewItem>
        {
            new() { Side = "Away", Reason = IdentityReviewReason.Conflict, OcrJersey = "7", OcrName = "X" },
        };
        var vm = new IdentityReviewViewModel(items, Context());
        var row = vm.Rows.Single();

        Assert.False(vm.CanExport);
        Assert.Equal(1, vm.OpenBlockingCount);

        // Senza candidato selezionato la conferma non risolve.
        row.SelectedCandidate = null;
        row.Confirm();
        Assert.False(row.Resolved);
        Assert.False(vm.CanExport);

        // Con candidato selezionato la conferma risolve e sblocca l'export.
        row.SelectedCandidate = row.Candidates.Single();
        row.Confirm();
        Assert.True(row.Resolved);
        Assert.True(vm.CanExport);
        Assert.Equal(0, vm.OpenBlockingCount);
    }

    [Fact]
    public void Not_in_pdf_row_can_be_confirmed_as_acknowledged()
    {
        var items = new List<IdentityReviewItem>
        {
            new() { Side = "Home", Reason = IdentityReviewReason.NotInPdf, CandidatePlayerId = 1, CandidateName = "Mario Rossi" },
        };
        var vm = new IdentityReviewViewModel(items, Context());
        var row = vm.Rows.Single();

        Assert.False(vm.CanExport);
        row.Confirm();
        Assert.True(row.Resolved);
        Assert.True(vm.CanExport);
    }

    [Fact]
    public void Non_blocking_items_do_not_block_export()
    {
        var items = new List<IdentityReviewItem>
        {
            new() { Side = "Home", Reason = IdentityReviewReason.ProbableMatch, CandidatePlayerId = 1, CandidateName = "Mario Rossi", ConfidenceScore = 0.8 },
            new() { Side = "Away", Reason = IdentityReviewReason.NumberNotFound, OcrJersey = "99" },
        };
        var vm = new IdentityReviewViewModel(items, Context());

        Assert.True(vm.CanExport);
        Assert.Equal(0, vm.OpenBlockingCount);
    }

    [Fact]
    public void Confirm_command_is_disabled_for_conflict_without_candidate()
    {
        var items = new List<IdentityReviewItem>
        {
            new() { Side = "Home", Reason = IdentityReviewReason.Conflict, OcrJersey = "5", OcrName = "Sbagliato" },
        };
        var vm = new IdentityReviewViewModel(items, Context());
        var row = vm.Rows.Single();

        Assert.False(row.ConfirmCommand.CanExecute(null));

        row.SelectedCandidate = row.Candidates.Single();
        Assert.True(row.ConfirmCommand.CanExecute(null));
    }
}
