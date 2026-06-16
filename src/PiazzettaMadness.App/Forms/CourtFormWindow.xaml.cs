using System.Windows;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App.Forms;

public partial class CourtFormWindow : Window
{
    public CourtFormWindow(Court court, IReadOnlyList<Edition> editions, bool isNew)
    {
        Court = court;
        InitializeComponent();
        TitleText.Text = isNew ? "Nuovo campo" : "Modifica campo";
        EditionCombo.ItemsSource = editions;
        EditionCombo.SelectedItem = editions.FirstOrDefault(x => x.Id == court.EditionId) ?? editions.FirstOrDefault();
        NameBox.Text = court.Name;
        LocationBox.Text = court.Location;
    }

    public Court Court { get; }

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
            ShowValidation("Il nome campo e obbligatorio.");
            return;
        }

        Court.EditionId = edition.Id;
        Court.Name = name;
        Court.Location = EmptyToNull(LocationBox.Text);
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
}
