using System.Windows;
using System.Windows.Controls;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App.Forms;

public partial class ThreePointEntryFormWindow : Window
{
    private readonly IReadOnlyList<Team> _teams;
    private readonly IReadOnlyList<TeamRoster> _rosters;
    private readonly IReadOnlyList<Player> _players;

    public ThreePointEntryFormWindow(
        ThreePointContestEntry entry,
        IReadOnlyList<CompetitionEvent> events,
        IReadOnlyList<Team> teams,
        IReadOnlyList<TeamRoster> rosters,
        IReadOnlyList<Player> players,
        bool isNew)
    {
        Entry = entry;
        _teams = teams;
        _rosters = rosters;
        _players = players;
        InitializeComponent();
        TitleText.Text = isNew ? "Nuovo partecipante" : "Modifica partecipante";
        EventCombo.ItemsSource = events;
        EventCombo.SelectedItem = events.FirstOrDefault(x => x.Id == entry.CompetitionEventId) ?? events.FirstOrDefault();
        TeamCombo.SelectedItem = teams.FirstOrDefault(x => x.Id == entry.TeamId);
        SeedOrderBox.Text = entry.SeedOrder?.ToString() ?? "";
        LoadTeams();
        LoadPlayers();
    }

    public ThreePointContestEntry Entry { get; }

    private void EventCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadTeams();
        LoadPlayers();
    }

    private void TeamCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadPlayers();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (EventCombo.SelectedItem is not CompetitionEvent competitionEvent)
        {
            ShowValidation("Seleziona un evento.");
            return;
        }

        if (TeamCombo.SelectedItem is not Team team)
        {
            ShowValidation("Seleziona una squadra.");
            return;
        }

        if (PlayerCombo.SelectedItem is not PlayerOption player)
        {
            ShowValidation("Seleziona un giocatore.");
            return;
        }

        int? seedOrder = null;
        if (!string.IsNullOrWhiteSpace(SeedOrderBox.Text))
        {
            if (!int.TryParse(SeedOrderBox.Text.Trim(), out var parsed) || parsed <= 0)
            {
                ShowValidation("L'ordine di tiro deve essere un numero positivo.");
                return;
            }

            seedOrder = parsed;
        }

        Entry.CompetitionEventId = competitionEvent.Id;
        Entry.TeamId = team.Id;
        Entry.PlayerId = player.Id;
        Entry.SeedOrder = seedOrder;
        DialogResult = true;
    }

    private void LoadTeams()
    {
        if (EventCombo.SelectedItem is not CompetitionEvent competitionEvent)
        {
            TeamCombo.ItemsSource = Array.Empty<Team>();
            return;
        }

        TeamCombo.ItemsSource = _teams
            .Where(team => team.EditionId == competitionEvent.EditionId)
            .OrderBy(team => team.Name)
            .ToList();

        TeamCombo.SelectedItem ??= _teams.FirstOrDefault(team => team.Id == Entry.TeamId);
    }

    private void LoadPlayers()
    {
        if (TeamCombo.SelectedItem is not Team team)
        {
            PlayerCombo.ItemsSource = Array.Empty<PlayerOption>();
            return;
        }

        var rosterPlayerIds = _rosters
            .Where(roster => roster.TeamId == team.Id && roster.IsActive)
            .Select(roster => roster.PlayerId)
            .ToHashSet();

        var options = _players
            .Where(player => rosterPlayerIds.Contains(player.Id))
            .OrderBy(player => player.LastName)
            .ThenBy(player => player.FirstName)
            .Select(player => new PlayerOption(player.Id, $"{player.LastName} {player.FirstName}".Trim()))
            .ToList();

        PlayerCombo.ItemsSource = options;
        PlayerCombo.SelectedItem = options.FirstOrDefault(player => player.Id == Entry.PlayerId);
    }

    private static void ShowValidation(string message)
    {
        MessageBox.Show(message, "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public sealed class PlayerOption
    {
        public PlayerOption(int id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public int Id { get; }
        public string DisplayName { get; }
    }
}
