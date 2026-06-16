using System.Windows;
using System.Windows.Controls;
using PiazzettaMadness.App.Data;

namespace PiazzettaMadness.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DatabaseInitializer.Initialize();
        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(ConfigureFormWindow));
        base.OnStartup(e);
    }

    private static void ConfigureFormWindow(object sender, RoutedEventArgs e)
    {
        if (sender is not Window window ||
            window.GetType().Namespace != "PiazzettaMadness.App.Forms" ||
            window.Content is not Grid root)
        {
            return;
        }

        window.MaxHeight = Math.Max(360, SystemParameters.WorkArea.Height - 48);
        window.MaxWidth = Math.Max(520, SystemParameters.WorkArea.Width - 48);
        window.ResizeMode = ResizeMode.CanResizeWithGrip;

        var body = root.Children
            .OfType<UIElement>()
            .FirstOrDefault(child => Grid.GetRow(child) == 1);

        if (body is null || body is ScrollViewer)
        {
            return;
        }

        root.Children.Remove(body);
        var scrollViewer = new ScrollViewer
        {
            Content = body,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = true
        };
        Grid.SetRow(scrollViewer, 1);
        Grid.SetColumn(scrollViewer, Grid.GetColumn(body));
        Grid.SetColumnSpan(scrollViewer, Grid.GetColumnSpan(body));
        root.Children.Add(scrollViewer);
    }
}
