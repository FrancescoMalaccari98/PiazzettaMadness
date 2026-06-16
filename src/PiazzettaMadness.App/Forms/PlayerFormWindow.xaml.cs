using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App.Forms;

public partial class PlayerFormWindow : Window
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".bmp"
    };

    public PlayerFormWindow(Player player, bool isNew)
    {
        Player = player;
        InitializeComponent();
        TitleText.Text = isNew ? "Nuovo giocatore" : "Modifica giocatore";
        FirstNameBox.Text = player.FirstName;
        LastNameBox.Text = player.LastName;
        NicknameBox.Text = player.Nickname;
        FiscalCodeBox.Text = player.FiscalCode;
        AddressBox.Text = player.Address;
        PhoneNumberBox.Text = player.PhoneNumber;
        EmailBox.Text = player.Email;
        PhotoPathBox.Text = player.PhotoPath;
        UpdatePhotoPreview();

        if (DateTime.TryParseExact(player.BirthDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            BirthDatePicker.SelectedDate = date;
        }
    }

    public Player Player { get; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var firstName = FirstNameBox.Text.Trim();
        var lastName = LastNameBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(firstName))
        {
            ShowValidation("Il nome e obbligatorio.");
            return;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            ShowValidation("Il cognome e obbligatorio.");
            return;
        }

        var email = EmailBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
        {
            ShowValidation("Inserisci un indirizzo email valido.");
            return;
        }

        var birthDateText = BirthDatePicker.Text.Trim();
        string? birthDate = null;
        if (!string.IsNullOrWhiteSpace(birthDateText))
        {
            if (!DateTime.TryParse(birthDateText, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsedBirthDate))
            {
                ShowValidation("Inserisci una data di nascita valida oppure lascia il campo vuoto.");
                return;
            }

            birthDate = parsedBirthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        var photoPath = EmptyToNull(PhotoPathBox.Text);
        if (!IsValidOptionalImagePath(photoPath))
        {
            ShowValidation("La foto deve essere un file immagine esistente: PNG, JPG, JPEG, WEBP o BMP.");
            return;
        }

        Player.FirstName = firstName;
        Player.LastName = lastName;
        Player.Nickname = EmptyToNull(NicknameBox.Text);
        Player.FiscalCode = EmptyToNull(FiscalCodeBox.Text.ToUpperInvariant());
        Player.Address = EmptyToNull(AddressBox.Text);
        Player.PhoneNumber = EmptyToNull(PhoneNumberBox.Text);
        Player.Email = EmptyToNull(email);
        Player.BirthDate = birthDate;
        Player.PhotoPath = photoPath;

        DialogResult = true;
    }

    private void BrowsePhoto_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleziona foto giocatore",
            Filter = "Immagini (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Tutti i file (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            PhotoPathBox.Text = dialog.FileName;
            UpdatePhotoPreview();
        }
    }

    private void ClearPhoto_Click(object sender, RoutedEventArgs e)
    {
        PhotoPathBox.Clear();
        UpdatePhotoPreview();
    }

    private void BirthDatePicker_DateValidationError(object? sender, DatePickerDateValidationErrorEventArgs e)
    {
        e.ThrowException = false;
        ShowValidation("Inserisci una data valida oppure lascia il campo vuoto.");
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

    private void UpdatePhotoPreview()
    {
        PhotoPreview.Source = CreateImageSource(PhotoPathBox.Text);
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
