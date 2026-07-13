using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using PiazzettaMadness.App.Controls;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App.Forms;

public partial class MatchFormWindow : Window
{
    private readonly IReadOnlyList<TournamentGroup> _groups;
    private readonly IReadOnlyList<Court> _courts;
    private readonly IReadOnlyList<Team> _teams;

    public MatchFormWindow(
        Match match,
        MatchTeam? homeTeam,
        MatchTeam? awayTeam,
        IReadOnlyList<Edition> editions,
        IReadOnlyList<TournamentGroup> groups,
        IReadOnlyList<Court> courts,
        IReadOnlyList<Team> teams,
        bool isNew)
    {
        Match = match;
        HomeMatchTeam = homeTeam ?? new MatchTeam { Side = "Home" };
        AwayMatchTeam = awayTeam ?? new MatchTeam { Side = "Away" };
        _groups = groups;
        _courts = courts;
        _teams = teams;

        InitializeComponent();
        TitleText.Text = isNew ? "Nuova partita" : "Modifica partita";
        EditionCombo.ItemsSource = editions;
        EditionCombo.SelectedItem = editions.FirstOrDefault(x => x.Id == match.EditionId) ?? editions.FirstOrDefault();
        NameBox.Text = match.Name;
        PhaseCombo.Text = string.IsNullOrWhiteSpace(match.Phase) ? "GroupStage" : match.Phase;
        StatusCombo.Text = string.IsNullOrWhiteSpace(match.Status) ? "Scheduled" : match.Status;
        PeriodDurationBox.Text = (match.PeriodDurationMs / 60000).ToString(CultureInfo.InvariantCulture);
        ShotClockBox.Text = (match.ShotClockMs / 1000).ToString(CultureInfo.InvariantCulture);
        MaxScoreEnabledBox.IsChecked = match.MaxScoreEnabled;
        MaxScoreBox.Text = match.MaxScore?.ToString(CultureInfo.InvariantCulture) ?? "";
        LoadEditionScopedCombos();
        GroupCombo.SelectedItem = _groups.FirstOrDefault(x => x.Id == match.GroupId);
        CourtCombo.SelectedItem = _courts.FirstOrDefault(x => x.Id == match.CourtId);
        HomeTeamCombo.SelectedItem = _teams.FirstOrDefault(x => x.Id == HomeMatchTeam.TeamId);
        AwayTeamCombo.SelectedItem = _teams.FirstOrDefault(x => x.Id == AwayMatchTeam.TeamId);
        ReadDateTime(match.ScheduledStartAt, StartDatePicker, StartTimePicker);
        ReadDateTime(match.ScheduledEndAt, EndDatePicker, EndTimePicker);
    }

    public Match Match { get; }
    public MatchTeam HomeMatchTeam { get; }
    public MatchTeam AwayMatchTeam { get; }

    private void EditionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadEditionScopedCombos();
    }

    private void ClearHomeTeam_Click(object sender, RoutedEventArgs e)
    {
        HomeTeamCombo.SelectedItem = null;
    }

    private void ClearAwayTeam_Click(object sender, RoutedEventArgs e)
    {
        AwayTeamCombo.SelectedItem = null;
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (EditionCombo.SelectedItem is not Edition edition)
        {
            ShowValidation("Seleziona una edizione.");
            return;
        }

        var homeTeam = HomeTeamCombo.SelectedItem as Team;
        var awayTeam = AwayTeamCombo.SelectedItem as Team;
        if (homeTeam is not null && awayTeam is not null && homeTeam.Id == awayTeam.Id)
        {
            ShowValidation("Squadra casa e squadra ospite devono essere diverse.");
            return;
        }

        if (!int.TryParse(PeriodDurationBox.Text.Trim(), out var periodMinutes) || periodMinutes <= 0)
        {
            ShowValidation("La durata del tempo deve essere un numero di minuti positivo.");
            return;
        }

        if (!int.TryParse(ShotClockBox.Text.Trim(), out var shotClockSeconds) || shotClockSeconds <= 0)
        {
            ShowValidation("Lo shot clock deve essere un numero di secondi positivo.");
            return;
        }

        int? maxScore = null;
        if (MaxScoreEnabledBox.IsChecked == true)
        {
            if (!int.TryParse(MaxScoreBox.Text.Trim(), out var parsedMaxScore) || parsedMaxScore <= 0)
            {
                ShowValidation("Il limite punti deve essere un numero positivo oppure disabilitato.");
                return;
            }

            maxScore = parsedMaxScore;
        }

        if (!TryBuildDateTime(StartDatePicker, StartTimePicker, out var startAt) ||
            !TryBuildDateTime(EndDatePicker, EndTimePicker, out var endAt))
        {
            ShowValidation("Seleziona una data e un orario completi oppure lascia entrambi vuoti.");
            return;
        }

        if (startAt is not null && endAt is not null && string.CompareOrdinal(startAt, endAt) > 0)
        {
            ShowValidation("L'inizio previsto non puo essere successivo alla fine prevista.");
            return;
        }

        Match.EditionId = edition.Id;
        Match.GroupId = (GroupCombo.SelectedItem as TournamentGroup)?.Id;
        Match.CourtId = (CourtCombo.SelectedItem as Court)?.Id;
        Match.Name = EmptyToNull(NameBox.Text);
        Match.Phase = PhaseCombo.Text;
        Match.Status = StatusCombo.Text;
        Match.ScheduledStartAt = startAt;
        Match.ScheduledEndAt = endAt;
        Match.PeriodDurationMs = periodMinutes * 60000;
        Match.ShotClockMs = shotClockSeconds * 1000;
        Match.MaxScoreEnabled = MaxScoreEnabledBox.IsChecked == true;
        Match.MaxScore = maxScore;

        HomeMatchTeam.TeamId = homeTeam?.Id ?? 0;
        HomeMatchTeam.Side = "Home";
        AwayMatchTeam.TeamId = awayTeam?.Id ?? 0;
        AwayMatchTeam.Side = "Away";
        DialogResult = true;
    }

    private void LoadEditionScopedCombos()
    {
        if (EditionCombo.SelectedItem is not Edition edition)
        {
            return;
        }

        GroupCombo.ItemsSource = _groups.Where(x => x.EditionId == edition.Id).OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToList();
        CourtCombo.ItemsSource = _courts.Where(x => x.EditionId == edition.Id).OrderBy(x => x.Name).ToList();
        HomeTeamCombo.ItemsSource = _teams.Where(x => x.EditionId == edition.Id).OrderBy(x => x.Name).ToList();
        AwayTeamCombo.ItemsSource = _teams.Where(x => x.EditionId == edition.Id).OrderBy(x => x.Name).ToList();
    }

    private void DatePicker_DateValidationError(object? sender, DatePickerDateValidationErrorEventArgs e)
    {
        e.ThrowException = false;
        ShowValidation("Inserisci una data valida oppure lascia il campo vuoto.");
    }

    private static void ReadDateTime(string? value, DatePicker datePicker, TimePickerControl timePicker)
    {
        if (!DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dateTime))
        {
            return;
        }

        datePicker.SelectedDate = dateTime.Date;
        timePicker.SelectedTime = dateTime.TimeOfDay;
    }

    private static bool TryBuildDateTime(DatePicker datePicker, TimePickerControl timePicker, out string? value)
    {
        value = null;
        var dateText = datePicker.Text.Trim();

        if (string.IsNullOrWhiteSpace(dateText) && timePicker.IsEmpty)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(dateText) || !timePicker.IsComplete)
        {
            return false;
        }

        if (!DateTime.TryParse(dateText, CultureInfo.CurrentCulture, DateTimeStyles.None, out var date))
        {
            return false;
        }

        if (timePicker.SelectedTime is not { } time)
        {
            return false;
        }

        value = date.Date.Add(time).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        return true;
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
}


