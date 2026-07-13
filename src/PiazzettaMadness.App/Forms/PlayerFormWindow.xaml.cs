using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App.Forms;

public partial class PlayerFormWindow : Window
{
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
        PhotoPathBox.Text = player.ConsolePhotoPath;
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
        if (!ImageAssetStore.IsValidOptionalImagePath(photoPath))
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
        Player.ConsolePhotoPath = photoPath;

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
            PhotoPathBox.Text = ImageAssetStore.Import(dialog.FileName, "players");
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

    private void UpdatePhotoPreview()
    {
        PhotoPreview.Source = ImageAssetStore.CreateImageSource(PhotoPathBox.Text);
    }

    private static void ShowValidation(string message)
    {
        MessageBox.Show(message, "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
