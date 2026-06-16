using System.Windows;
using System.Windows.Controls;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App.Forms;

public partial class ForfeitFormWindow : Window
{
    private readonly IReadOnlyList<MatchTeam> _matchTeams;
    private readonly IReadOnlyList<Team> _teams;

    public ForfeitFormWindow(ForfeitResult result, IReadOnlyList<MatchOption> matches, IReadOnlyList<MatchTeam> matchTeams, IReadOnlyList<Team> teams, bool isNew)
    {
        Result = result;
        _matchTeams = matchTeams;
        _teams = teams;
        InitializeComponent();
        TitleText.Text = isNew ? "Nuovo risultato a tavolino" : "Modifica risultato a tavolino";
        MatchCombo.ItemsSource = matches;
        MatchCombo.SelectedItem = matches.FirstOrDefault(x => x.Id == result.MatchId) ?? matches.FirstOrDefault();
        HomeScoreBox.Text = result.HomeAssignedScore.ToString();
        AwayScoreBox.Text = result.AwayAssignedScore.ToString();
        ReasonCombo.Text = string.IsNullOrWhiteSpace(result.Reason) ? "OrganizerDecision" : result.Reason;
        NotesBox.Text = result.Notes;
        LoadWinnerTeams();
    }

    public ForfeitResult Result { get; }

    private void MatchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadWinnerTeams();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (MatchCombo.SelectedItem is not MatchOption match)
        {
            ShowValidation("Seleziona una partita.");
            return;
        }

        if (WinnerCombo.SelectedItem is not Team winner)
        {
            ShowValidation("Seleziona la squadra vincente.");
            return;
        }

        if (!int.TryParse(HomeScoreBox.Text.Trim(), out var homeScore) || homeScore < 0 ||
            !int.TryParse(AwayScoreBox.Text.Trim(), out var awayScore) || awayScore < 0)
        {
            ShowValidation("I punteggi devono essere numeri non negativi.");
            return;
        }

        var sideTeams = _matchTeams.Where(x => x.MatchId == match.Id).ToList();
        var loser = sideTeams.FirstOrDefault(x => x.TeamId != winner.Id);

        Result.MatchId = match.Id;
        Result.WinningTeamId = winner.Id;
        Result.LosingTeamId = loser?.TeamId;
        Result.HomeAssignedScore = homeScore;
        Result.AwayAssignedScore = awayScore;
        Result.Reason = ReasonCombo.Text;
        Result.Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();
        DialogResult = true;
    }

    private void LoadWinnerTeams()
    {
        if (MatchCombo.SelectedItem is not MatchOption match)
        {
            WinnerCombo.ItemsSource = Array.Empty<Team>();
            return;
        }

        var teamIds = _matchTeams.Where(x => x.MatchId == match.Id).Select(x => x.TeamId).ToHashSet();
        var options = _teams.Where(x => teamIds.Contains(x.Id)).OrderBy(x => x.Name).ToList();
        WinnerCombo.ItemsSource = options;
        WinnerCombo.SelectedItem = options.FirstOrDefault(x => x.Id == Result.WinningTeamId) ?? options.FirstOrDefault();
    }

    private static void ShowValidation(string message)
    {
        MessageBox.Show(message, "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public sealed class MatchOption
    {
        public MatchOption(int id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public int Id { get; }
        public string DisplayName { get; }
    }
}
