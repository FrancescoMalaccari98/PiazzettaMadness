using System.Windows;
using System.Windows.Input;
using PiazzettaMadness.App.Data;
using PiazzettaMadness.App.Live;

namespace PiazzettaMadness.App;

public partial class ScoreboardWindow : Window
{
    private readonly ScoreboardBroadcaster _broadcaster;
    private bool _isFullScreen;

    public ScoreboardWindow(ScoreboardBroadcaster broadcaster, int displayId)
    {
        _broadcaster = broadcaster;
        DisplayId = displayId;
        InitializeComponent();
        Title = $"Piazzetta Madness - Tabellone {DisplayId}";
        Loaded += OnLoaded;
        Closed += OnClosed;
        PreviewKeyDown += OnKeyDown;
    }

    public int DisplayId { get; }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ScoreboardView.EnsureCoreWebView2Async();
        _broadcaster.Register(DisplayId, $"Tabellone {DisplayId}", ScoreboardView);
        ScoreboardView.Source = new Uri(AppPaths.ScoreboardIndexPath);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _broadcaster.Unregister(DisplayId);
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (_isFullScreen)
            {
                ExitFullScreen();
                e.Handled = true;
            }

            return;
        }

        if (e.Key == Key.F11)
        {
            ToggleFullScreen();
            e.Handled = true;
        }
    }

    private void ToggleFullScreen()
    {
        if (_isFullScreen)
        {
            ExitFullScreen();
        }
        else
        {
            EnterFullScreen();
        }
    }

    private void EnterFullScreen()
    {
        _isFullScreen = true;
        Cursor = Cursors.None;
        Topmost = true;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowState = WindowState.Maximized;
    }

    private void ExitFullScreen()
    {
        _isFullScreen = false;
        Cursor = Cursors.Arrow;
        Topmost = false;
        WindowStyle = WindowStyle.SingleBorderWindow;
        ResizeMode = ResizeMode.CanResize;
        WindowState = WindowState.Normal;
    }
}
