using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App.Forms;

public partial class TeamFormWindow : Window
{
    private static readonly Regex HexColorRegex = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".bmp"
    };

    public TeamFormWindow(Team team, bool isNew)
    {
        Team = team;
        InitializeComponent();
        TitleText.Text = isNew ? "Nuova squadra" : "Modifica squadra";
        NameBox.Text = team.Name;
        ShortNameBox.Text = team.ShortName;
        PrimaryColorBox.Text = team.PrimaryColor;
        SecondaryColorBox.Text = team.SecondaryColor;
        LogoPathBox.Text = team.LogoPath;
        UpdateColorPreviews();
        UpdateLogoPreview();
    }

    public Team Team { get; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        var shortName = ShortNameBox.Text.Trim();
        var primaryColor = PrimaryColorBox.Text.Trim();
        var secondaryColor = SecondaryColorBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowValidation("Il nome squadra e obbligatorio.");
            return;
        }

        if (string.IsNullOrWhiteSpace(shortName))
        {
            ShowValidation("Il nome breve e obbligatorio per il tabellone.");
            return;
        }

        if (!IsValidOptionalColor(primaryColor) || !IsValidOptionalColor(secondaryColor))
        {
            ShowValidation("I colori devono essere in formato hex, per esempio #2563eb.");
            return;
        }

        var logoPath = EmptyToNull(LogoPathBox.Text);
        if (!IsValidOptionalImagePath(logoPath))
        {
            ShowValidation("Il logo deve essere un file immagine esistente: PNG, JPG, JPEG, WEBP o BMP.");
            return;
        }

        Team.Name = name;
        Team.ShortName = shortName;
        Team.PrimaryColor = string.IsNullOrWhiteSpace(primaryColor) ? null : primaryColor;
        Team.SecondaryColor = string.IsNullOrWhiteSpace(secondaryColor) ? null : secondaryColor;
        Team.LogoPath = logoPath;

        DialogResult = true;
    }

    private void BrowseLogo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleziona logo squadra",
            Filter = "Immagini (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Tutti i file (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            LogoPathBox.Text = dialog.FileName;
            UpdateLogoPreview();
        }
    }

    private void ClearLogo_Click(object sender, RoutedEventArgs e)
    {
        LogoPathBox.Clear();
        UpdateLogoPreview();
    }

    private void PickPrimaryColor_Click(object sender, RoutedEventArgs e) => PickColor(PrimaryColorBox);

    private void PickSecondaryColor_Click(object sender, RoutedEventArgs e) => PickColor(SecondaryColorBox);

    private void PickColor(TextBox target)
    {
        using var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            AnyColor = true
        };

        if (TryParseHexColor(target.Text, out var currentColor))
        {
            dialog.Color = System.Drawing.Color.FromArgb(currentColor.R, currentColor.G, currentColor.B);
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            target.Text = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        }
    }

    private void ColorBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateColorPreviews();
    }

    private void UpdateColorPreviews()
    {
        if (PrimaryColorPreview is null || SecondaryColorPreview is null)
        {
            return;
        }

        PrimaryColorPreview.Background = CreateColorBrush(PrimaryColorBox.Text);
        SecondaryColorPreview.Background = CreateColorBrush(SecondaryColorBox.Text);
    }

    private static Brush CreateColorBrush(string value)
    {
        return TryParseHexColor(value, out var color)
            ? new SolidColorBrush(color)
            : Brushes.Transparent;
    }

    private static bool TryParseHexColor(string value, out Color color)
    {
        var normalized = value.Trim();
        var match = HexColorRegex.Match(normalized);
        if (!match.Success)
        {
            color = default;
            return false;
        }

        color = Color.FromRgb(
            Convert.ToByte(normalized.Substring(1, 2), 16),
            Convert.ToByte(normalized.Substring(3, 2), 16),
            Convert.ToByte(normalized.Substring(5, 2), 16));
        return true;
    }

    private static bool IsValidOptionalColor(string value)
    {
        return string.IsNullOrWhiteSpace(value) || HexColorRegex.IsMatch(value);
    }

    private static string? EmptyToNull(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static bool IsValidOptionalImagePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            || File.Exists(path)
            && ImageExtensions.Contains(Path.GetExtension(path));
    }

    private void UpdateLogoPreview()
    {
        LogoPreview.Source = CreateImageSource(LogoPathBox.Text);
    }

    private static BitmapImage? CreateImageSource(string path)
    {
        if (!IsValidOptionalImagePath(path) || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private static void ShowValidation(string message)
    {
        MessageBox.Show(message, "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
