using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SimpleAudioRouter.Controls;

public partial class TitleBar : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TitleBar),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.Register(nameof(IconSource), typeof(ImageSource), typeof(TitleBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ShowMinimizeProperty =
        DependencyProperty.Register(nameof(ShowMinimize), typeof(bool), typeof(TitleBar),
            new PropertyMetadata(true));

    public static readonly DependencyProperty MenuContentProperty =
        DependencyProperty.Register(nameof(MenuContent), typeof(object), typeof(TitleBar),
            new PropertyMetadata(null));

    public static readonly RoutedEvent CloseClickEvent =
        EventManager.RegisterRoutedEvent(
            nameof(CloseClick),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(TitleBar));

    public event RoutedEventHandler CloseClick
    {
        add => AddHandler(CloseClickEvent, value);
        remove => RemoveHandler(CloseClickEvent, value);
    }

    public TitleBar()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (IconSource is null)
                IconSource = AppIconHelper.GetAppImageSource();
        };
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public ImageSource? IconSource
    {
        get => (ImageSource?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public bool ShowMinimize
    {
        get => (bool)GetValue(ShowMinimizeProperty);
        set => SetValue(ShowMinimizeProperty, value);
    }

    public object? MenuContent
    {
        get => GetValue(MenuContentProperty);
        set => SetValue(MenuContentProperty, value);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && ShowMinimize)
        {
            if (Window.GetWindow(this) is { } window)
                window.WindowState = WindowState.Minimized;
            return;
        }

        Window.GetWindow(this)?.DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is { } window)
            window.WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        var args = new RoutedEventArgs(CloseClickEvent, this);
        RaiseEvent(args);
        if (args.Handled)
            return;

        Window.GetWindow(this)?.Close();
    }
}
