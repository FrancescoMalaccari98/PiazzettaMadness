using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BasketPdfStats.App.Converters;

/// <summary>
/// Converte una stringa in <see cref="Visibility"/>: <c>Visible</c> se non vuota, altrimenti
/// <c>Collapsed</c>. Usato per nascondere blocchi di avviso quando il testo è assente.
/// </summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
