using System.Globalization;
using System.Windows;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App.Forms;

public partial class GroupFormWindow : Window
{
    public GroupFormWindow(TournamentGroup group, IReadOnlyList<Edition> editions, bool isNew)
    {
        Group = group;
        InitializeComponent();
        TitleText.Text = isNew ? "Nuovo girone" : "Modifica girone";
        EditionCombo.ItemsSource = editions;
        EditionCombo.SelectedItem = editions.FirstOrDefault(x => x.Id == group.EditionId) ?? editions.FirstOrDefault();
        NameBox.Text = group.Name;
        CodeBox.Text = group.Code;
        SortOrderBox.Text = group.SortOrder.ToString(CultureInfo.InvariantCulture);
    }

    public TournamentGroup Group { get; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (EditionCombo.SelectedItem is not Edition edition)
        {
            ShowValidation("Seleziona una edizione.");
            return;
        }

        var name = NameBox.Text.Trim();
        var code = CodeBox.Text.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowValidation("Il nome girone e obbligatorio.");
            return;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            ShowValidation("Il codice girone e obbligatorio.");
            return;
        }

        if (!int.TryParse(SortOrderBox.Text.Trim(), out var sortOrder))
        {
            ShowValidation("L'ordinamento deve essere un numero intero.");
            return;
        }

        Group.EditionId = edition.Id;
        Group.Name = name;
        Group.Code = code;
        Group.SortOrder = sortOrder;
        DialogResult = true;
    }

    private static void ShowValidation(string message)
    {
        MessageBox.Show(message, "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
