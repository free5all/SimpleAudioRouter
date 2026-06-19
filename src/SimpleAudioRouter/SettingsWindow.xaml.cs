using System.Windows;

namespace SimpleAudioRouter;

public partial class SettingsWindow : Window
{
    public bool StartWithWindows => StartWithWindowsCheckBox.IsChecked == true;

    public bool StartMinimized => StartMinimizedCheckBox.IsChecked == true;

    public bool MinimizeToTrayOnClose => MinimizeToTrayCheckBox.IsChecked == true;

    public SettingsWindow(bool startWithWindows, bool startMinimized, bool minimizeToTrayOnClose)
    {
        InitializeComponent();
        StartWithWindowsCheckBox.IsChecked = startWithWindows;
        StartMinimizedCheckBox.IsChecked = startMinimized;
        MinimizeToTrayCheckBox.IsChecked = minimizeToTrayOnClose;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
