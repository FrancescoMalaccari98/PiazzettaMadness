using System.Windows;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App.Forms;

public partial class GroupTeamFormWindow : Window
{
    public GroupTeamFormWindow(GroupTeam groupTeam, IReadOnlyList<GroupOption> groups, IReadOnlyList<Team> teams, bool isNew)
    {
        GroupTeam = groupTeam;
        InitializeComponent();
        TitleText.Text = isNew ? "Nuova assegnazione" : "Modifica assegnazione";
        GroupCombo.ItemsSource = groups;
        TeamCombo.ItemsSource = teams;
        GroupCombo.SelectedItem = groups.FirstOrDefault(x => x.Id == groupTeam.GroupId) ?? groups.FirstOrDefault();
        TeamCombo.SelectedItem = teams.FirstOrDefault(x => x.Id == groupTeam.TeamId) ?? teams.FirstOrDefault();
        SeedLabelBox.Text = groupTeam.SeedLabel;
    }

    public GroupTeam GroupTeam { get; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (GroupCombo.SelectedItem is not GroupOption group)
        {
            ShowValidation("Seleziona un girone.");
            return;
        }

        if (TeamCombo.SelectedItem is not Team team)
        {
            ShowValidation("Seleziona una squadra.");
            return;
        }

        GroupTeam.GroupId = group.Id;
        GroupTeam.TeamId = team.Id;
        GroupTeam.SeedLabel = EmptyToNull(SeedLabelBox.Text.ToUpperInvariant());
        DialogResult = true;
    }

    private static string? EmptyToNull(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static void ShowValidation(string message)
    {
        MessageBox.Show(message, "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public sealed class GroupOption
    {
        public GroupOption(int id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public int Id { get; }
        public string DisplayName { get; }
    }
}
