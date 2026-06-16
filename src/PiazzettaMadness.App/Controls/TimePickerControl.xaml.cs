using System.Windows;
using System.Windows.Controls;

namespace PiazzettaMadness.App.Controls;

public partial class TimePickerControl : UserControl
{
    private bool _updating;

    public TimePickerControl()
    {
        InitializeComponent();
        HourCombo.ItemsSource = new[] { "--" }.Concat(Enumerable.Range(0, 24).Select(value => value.ToString("00")));
        MinuteCombo.ItemsSource = new[] { "--" }.Concat(Enumerable.Range(0, 60).Select(value => value.ToString("00")));
        Clear();
    }

    public TimeSpan? SelectedTime
    {
        get
        {
            if (HourCombo.SelectedItem is not string hourText ||
                MinuteCombo.SelectedItem is not string minuteText ||
                !int.TryParse(hourText, out var hour) ||
                !int.TryParse(minuteText, out var minute))
            {
                return null;
            }

            return new TimeSpan(hour, minute, 0);
        }
        set
        {
            _updating = true;
            if (value is null)
            {
                HourCombo.SelectedIndex = 0;
                MinuteCombo.SelectedIndex = 0;
            }
            else
            {
                HourCombo.SelectedItem = value.Value.Hours.ToString("00");
                MinuteCombo.SelectedItem = value.Value.Minutes.ToString("00");
            }
            _updating = false;
        }
    }

    public bool IsEmpty => HourCombo.SelectedIndex <= 0 && MinuteCombo.SelectedIndex <= 0;

    public bool IsComplete => HourCombo.SelectedIndex > 0 && MinuteCombo.SelectedIndex > 0;

    public void Clear() => SelectedTime = null;

    private void TimePart_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating)
        {
            return;
        }

        if (HourCombo.SelectedIndex <= 0 && MinuteCombo.SelectedIndex > 0)
        {
            HourCombo.SelectedItem = "00";
        }
        else if (MinuteCombo.SelectedIndex <= 0 && HourCombo.SelectedIndex > 0)
        {
            MinuteCombo.SelectedItem = "00";
        }
    }
}
