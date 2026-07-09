using System.Windows;
using Microsoft.Win32;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App.Forms;

public partial class MerchandiseItemFormWindow : Window
{
    public MerchandiseItemFormWindow(MerchandiseItem item, bool isNew)
    {
        Item = item;
        InitializeComponent();
        TitleText.Text = isNew ? "Nuovo articolo merchandising" : "Modifica articolo merchandising";
        NameBox.Text = item.Name;
        DescriptionBox.Text = item.Description;
        ImagePathBox.Text = item.ImagePath;
        IsActiveCheck.IsChecked = item.IsActive;
        SortOrderBox.Text = item.SortOrder.ToString();
        PriceBox.Text = item.Price?.ToString("0.##");
        UpdatePreview();
    }

    public MerchandiseItem Item { get; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowValidation("Il nome articolo è obbligatorio.");
            return;
        }

        if (!int.TryParse(SortOrderBox.Text.Trim(), out var sortOrder))
        {
            ShowValidation("L'ordine deve essere un numero intero.");
            return;
        }

        decimal? price = null;
        var priceText = PriceBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(priceText))
        {
            if (!decimal.TryParse(priceText, out var parsedPrice) || parsedPrice < 0)
            {
                ShowValidation("Il prezzo deve essere un numero positivo.");
                return;
            }

            price = parsedPrice;
        }

        var imagePath = EmptyToNull(ImagePathBox.Text);
        if (!ImageAssetStore.IsValidOptionalImagePath(imagePath))
        {
            ShowValidation("L'immagine deve essere un file esistente: PNG, JPG, JPEG, WEBP o BMP.");
            return;
        }

        Item.Name = name;
        Item.Description = EmptyToNull(DescriptionBox.Text);
        Item.Price = price;
        Item.ImagePath = imagePath;
        Item.IsActive = IsActiveCheck.IsChecked == true;
        Item.SortOrder = sortOrder;
        DialogResult = true;
    }

    private void BrowseImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleziona immagine merchandising",
            Filter = "Immagini (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Tutti i file (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            ImagePathBox.Text = ImageAssetStore.Import(dialog.FileName, "merchandising");
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
        ImagePreview.Source = ImageAssetStore.CreateImageSource(ImagePathBox.Text);
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

