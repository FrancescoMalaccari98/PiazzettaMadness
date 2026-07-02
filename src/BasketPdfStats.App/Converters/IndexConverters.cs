using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BasketPdfStats.App.Converters;

/// <summary>
/// Visibile quando l'indice corrente (int) è uguale al parametro. Usato per mostrare il pannello
/// della scheda selezionata (tab in alto che pilotano un singolo indice).
/// </summary>
public sealed class IndexToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Matches(value, parameter) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static bool Matches(object? value, object? parameter) =>
        value is int index && TryParse(parameter, out var target) && index == target;

    internal static bool TryParse(object? parameter, out int target) =>
        int.TryParse(parameter?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out target);
}

/// <summary>
/// True quando l'indice corrente è uguale al parametro. Per i RadioButton-scheda nell'header:
/// selezionando il radio si imposta l'indice (ConvertBack), e l'indice attivo evidenzia il radio.
/// </summary>
public sealed class IndexToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int index && IndexToVisibilityConverter.TryParse(parameter, out var target) && index == target;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && IndexToVisibilityConverter.TryParse(parameter, out var target))
        {
            return target;
        }

        return System.Windows.Data.Binding.DoNothing;
    }
}
