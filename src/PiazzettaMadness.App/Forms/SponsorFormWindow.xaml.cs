using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App.Forms;

public partial class SponsorFormWindow : Window
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".bmp"
    };

    public SponsorFormWindow(Sponsor sponsor, bool isNew)
    {
        Sponsor = sponsor;
        InitializeComponent();
        TitleText.Text = isNew ? "Nuovo sponsor" : "Modifica sponsor";
        NameBox.Text = sponsor.Name;
        DescriptionBox.Text = sponsor.Description;
        ImagePathBox.Text = sponsor.ImagePath;
        IsActiveCheck.IsChecked = sponsor.IsActive;
        SortOrderBox.Text = sponsor.SortOrder.ToString();
        UpdatePreview();
    }

    public Sponsor Sponsor { get; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowValidation("Il nome sponsor e obbligatorio.");
            return;
        }

        if (!int.TryParse(SortOrderBox.Text.Trim(), out var sortOrder))
        {
            ShowValidation("L'ordine deve essere un numero intero.");
            return;
        }

        var imagePath = EmptyToNull(ImagePathBox.Text);
        if (!IsValidOptionalImagePath(imagePath))
        {
            ShowValidation("L'immagine deve essere un file esistente: PNG, JPG, JPEG, WEBP o BMP.");
            return;
        }

        Sponsor.Name = name;
        Sponsor.Description = EmptyToNull(DescriptionBox.Text);
        Sponsor.ImagePath = imagePath;
        Sponsor.IsActive = IsActiveCheck.IsChecked == true;
        Sponsor.SortOrder = sortOrder;
        DialogResult = true;
    }

    private void BrowseImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleziona immagine sponsor",
            Filter = "Immagini (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Tutti i file (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            ImagePathBox.Text = dialog.FileName;
            UpdatePreview();
        }
    }

    private void ClearImage_Click(object sender, RoutedEventArgs e)
    {
        ImagePathBox.Clear();
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        ImagePreview.Source = CreateImageSource(ImagePathBox.Text);
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

    private static void ShowValidation(string message)
    {
        MessageBox.Show(message, "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
