using System.Windows;
using System.Windows.Controls;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App.Forms;

public partial class ThreePointRoundFormWindow : Window
{
    public ThreePointRoundFormWindow(ThreePointContestRound round, IReadOnlyList<EntryOption> entries, bool isNew)
    {
        Round = round;
        InitializeComponent();
        TitleText.Text = isNew ? "Nuova prova" : "Modifica prova";
        EntryCombo.ItemsSource = entries;
        EntryCombo.SelectedItem = entries.FirstOrDefault(x => x.Id == round.EntryId) ?? entries.FirstOrDefault();
        RoundNumberBox.Text = round.RoundNumber == 0 ? "1" : round.RoundNumber.ToString();
        RoundTypeCombo.SelectedItem = RoundTypeCombo.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(x => string.Equals(x.Tag?.ToString(), round.RoundType, StringComparison.OrdinalIgnoreCase))
            ?? RoundTypeCombo.Items[0];
        Station1Box.Text = round.Station1Score.ToString();
        Station2Box.Text = round.Station2Score.ToString();
        Station3Box.Text = round.Station3Score.ToString();
        Station4Box.Text = round.Station4Score.ToString();
        Station5Box.Text = round.Station5Score.ToString();
    }

    public ThreePointContestRound Round { get; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (EntryCombo.SelectedItem is not EntryOption entry)
        {
            ShowValidation("Seleziona un partecipante.");
            return;
        }

        if (!ReadNonNegative(RoundNumberBox.Text, "numero round", out var roundNumber) || roundNumber <= 0 ||
            !ReadNonNegative(Station1Box.Text, "postazione 1", out var s1) ||
            !ReadNonNegative(Station2Box.Text, "postazione 2", out var s2) ||
            !ReadNonNegative(Station3Box.Text, "postazione 3", out var s3) ||
            !ReadNonNegative(Station4Box.Text, "postazione 4", out var s4) ||
            !ReadNonNegative(Station5Box.Text, "postazione 5", out var s5))
        {
            return;
        }

        Round.EntryId = entry.Id;
        Round.RoundNumber = roundNumber;
        Round.RoundType = (RoundTypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Qualification";
        Round.Station1Score = s1;
        Round.Station2Score = s2;
        Round.Station3Score = s3;
        Round.Station4Score = s4;
        Round.Station5Score = s5;
        Round.TotalScore = s1 + s2 + s3 + s4 + s5;
        DialogResult = true;
    }

    private static bool ReadNonNegative(string text, string label, out int value)
    {
        if (!int.TryParse(text.Trim(), out value) || value < 0)
        {
            ShowValidation($"Il campo {label} deve essere un numero non negativo.");
            return false;
        }

        return true;
    }

    private static void ShowValidation(string message)
    {
        MessageBox.Show(message, "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public sealed class EntryOption
    {
        public EntryOption(int id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public int Id { get; }
        public string DisplayName { get; }
    }
}
