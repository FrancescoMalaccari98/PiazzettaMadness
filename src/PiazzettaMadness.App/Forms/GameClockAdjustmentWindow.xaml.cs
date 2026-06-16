using System.Windows;
using System.Windows.Input;

namespace PiazzettaMadness.App.Forms;

public partial class GameClockAdjustmentWindow : Window
{
    public GameClockAdjustmentWindow(int currentDurationMs)
    {
        InitializeComponent();

        var totalSeconds = (int)Math.Ceiling(Math.Max(0, currentDurationMs) / 1000d);
        MinutesTextBox.Text = (totalSeconds / 60).ToString();
        SecondsTextBox.Text = (totalSeconds % 60).ToString("00");
        MinutesTextBox.SelectAll();
        MinutesTextBox.Focus();
    }

    public int SelectedDurationMs { get; private set; } = 720000;

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(MinutesTextBox.Text, out var minutes) || minutes is < 0 or > 99)
        {
            ValidationText.Text = "I minuti devono essere compresi tra 0 e 99.";
            return;
        }

        if (!int.TryParse(SecondsTextBox.Text, out var seconds) || seconds is < 0 or > 59)
        {
            ValidationText.Text = "I secondi devono essere compresi tra 0 e 59.";
            return;
        }

        SelectedDurationMs = checked(((minutes * 60) + seconds) * 1000);
        DialogResult = true;
    }

    private void NumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text.Any(character => !char.IsDigit(character));
    }
}
