using System.Windows;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App.Forms;

public partial class TournamentFormWindow : Window
{
    public TournamentFormWindow(Tournament tournament, bool isNew)
    {
        Tournament = tournament;
        InitializeComponent();
        TitleText.Text = isNew ? "Nuovo torneo" : "Modifica torneo";
        NameBox.Text = tournament.Name;
        DescriptionBox.Text = tournament.Description;
    }

    public Tournament Tournament { get; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Il nome torneo e obbligatorio.", "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Tournament.Name = name;
        Tournament.Description = EmptyToNull(DescriptionBox.Text);
        DialogResult = true;
    }

    private static string? EmptyToNull(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
