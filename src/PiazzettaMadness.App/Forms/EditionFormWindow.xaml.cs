using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App.Forms;

public partial class EditionFormWindow : Window
{
    public EditionFormWindow(Edition edition, IReadOnlyList<Tournament> tournaments, bool isNew)
    {
        Edition = edition;
        InitializeComponent();
        TitleText.Text = isNew ? "Nuova edizione" : "Modifica edizione";
        TournamentCombo.ItemsSource = tournaments;
        TournamentCombo.SelectedItem = tournaments.FirstOrDefault(x => x.Id == edition.TournamentId) ?? tournaments.FirstOrDefault();
        NameBox.Text = edition.Name;
        YearBox.Text = edition.Year == 0 ? DateTime.Now.Year.ToString(CultureInfo.InvariantCulture) : edition.Year.ToString(CultureInfo.InvariantCulture);
        StatusCombo.Text = string.IsNullOrWhiteSpace(edition.Status) ? "Draft" : edition.Status;

        if (DateTime.TryParseExact(edition.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
        {
            StartDatePicker.SelectedDate = start;
        }

        if (DateTime.TryParseExact(edition.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            EndDatePicker.SelectedDate = end;
        }
    }

    public Edition Edition { get; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (TournamentCombo.SelectedItem is not Tournament tournament)
        {
            ShowValidation("Seleziona un torneo.");
            return;
        }

        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowValidation("Il nome edizione e obbligatorio.");
            return;
        }

        if (!int.TryParse(YearBox.Text.Trim(), out var year) || year < 2000 || year > 2100)
        {
            ShowValidation("Inserisci un anno valido.");
            return;
        }

        if (!TryReadDate(StartDatePicker, out var startDate) || !TryReadDate(EndDatePicker, out var endDate))
        {
            ShowValidation("Inserisci date valide oppure lascia i campi vuoti.");
            return;
        }

        if (startDate is not null && endDate is not null && string.CompareOrdinal(startDate, endDate) > 0)
        {
            ShowValidation("La data inizio non puo essere successiva alla data fine.");
            return;
        }

        Edition.TournamentId = tournament.Id;
        Edition.Name = name;
        Edition.Year = year;
        Edition.StartDate = startDate;
        Edition.EndDate = endDate;
        Edition.Status = StatusCombo.Text;
        DialogResult = true;
    }

    private void DatePicker_DateValidationError(object? sender, DatePickerDateValidationErrorEventArgs e)
    {
        e.ThrowException = false;
        ShowValidation("Inserisci una data valida oppure lascia il campo vuoto.");
    }

    private static bool TryReadDate(DatePicker picker, out string? value)
    {
        var text = picker.Text.Trim();
        value = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (!DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed))
        {
            return false;
        }

        value = parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
    }

    private static void ShowValidation(string message)
    {
        MessageBox.Show(message, "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
