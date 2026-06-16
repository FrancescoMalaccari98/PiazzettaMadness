using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using PiazzettaMadness.App.Controls;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App.Forms;

public partial class CompetitionEventFormWindow : Window
{
    public CompetitionEventFormWindow(CompetitionEvent competitionEvent, IReadOnlyList<Edition> editions, bool isNew)
    {
        CompetitionEvent = competitionEvent;
        InitializeComponent();
        TitleText.Text = isNew ? "Nuovo evento" : "Modifica evento";
        EditionCombo.ItemsSource = editions;
        EditionCombo.SelectedItem = editions.FirstOrDefault(x => x.Id == competitionEvent.EditionId) ?? editions.FirstOrDefault();
        NameBox.Text = competitionEvent.Name;
        StatusCombo.Text = string.IsNullOrWhiteSpace(competitionEvent.Status) ? "Scheduled" : competitionEvent.Status;
        ReadDateTime(competitionEvent.ScheduledStartAt, StartDatePicker, StartTimePicker);
        ReadDateTime(competitionEvent.ScheduledEndAt, EndDatePicker, EndTimePicker);
    }

    public CompetitionEvent CompetitionEvent { get; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (EditionCombo.SelectedItem is not Edition edition)
        {
            ShowValidation("Seleziona una edizione.");
            return;
        }

        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowValidation("Il nome evento e obbligatorio.");
            return;
        }

        if (!TryBuildDateTime(StartDatePicker, StartTimePicker, out var startAt) ||
            !TryBuildDateTime(EndDatePicker, EndTimePicker, out var endAt))
        {
            ShowValidation("Seleziona una data e un orario completi oppure lascia entrambi vuoti.");
            return;
        }

        CompetitionEvent.EditionId = edition.Id;
        CompetitionEvent.EventType = "ThreePointContest";
        CompetitionEvent.Name = name;
        CompetitionEvent.ScheduledStartAt = startAt;
        CompetitionEvent.ScheduledEndAt = endAt;
        CompetitionEvent.Status = StatusCombo.Text;
        DialogResult = true;
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

    private static void ShowValidation(string message)
    {
        MessageBox.Show(message, "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
