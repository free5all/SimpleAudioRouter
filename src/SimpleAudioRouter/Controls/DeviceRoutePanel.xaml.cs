using System.Windows;
using System.Windows.Controls;
using SimpleAudioRouter.Core.Audio;

namespace SimpleAudioRouter.Controls;

public partial class DeviceRoutePanel : System.Windows.Controls.UserControl
{
    private const double DefaultSliderValue = 100;

    public static readonly DependencyProperty InputLabelLeftProperty =
        DependencyProperty.Register(nameof(InputLabelLeft), typeof(string), typeof(DeviceRoutePanel),
            new PropertyMetadata("→ Out L"));

    public static readonly DependencyProperty InputLabelRightProperty =
        DependencyProperty.Register(nameof(InputLabelRight), typeof(string), typeof(DeviceRoutePanel),
            new PropertyMetadata("→ Out R"));

    private bool _suppressEvents;

    public event EventHandler? GainsChanged;

    public DeviceRoutePanel()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshUi();
    }

    public string InputLabelLeft
    {
        get => (string)GetValue(InputLabelLeftProperty);
        set => SetValue(InputLabelLeftProperty, value);
    }

    public string InputLabelRight
    {
        get => (string)GetValue(InputLabelRightProperty);
        set => SetValue(InputLabelRightProperty, value);
    }

    public void SetGains(DeviceRouteGains gains)
    {
        _suppressEvents = true;
        try
        {
            ToOutputLeftSlider.Value = GainToSlider(gains.ToOutputLeft);
            ToOutputRightSlider.Value = GainToSlider(gains.ToOutputRight);
            RefreshUi();
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    public DeviceRouteGains GetGains() => new()
    {
        ToOutputLeft = SliderToGain(ToOutputLeftSlider.Value),
        ToOutputRight = SliderToGain(ToOutputRightSlider.Value),
    };

    public void SetOutputLevels(float left, float right)
    {
        OutLeftMeter.Value = ToMeter(left);
        OutRightMeter.Value = ToMeter(right);
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        RefreshUi();
        if (_suppressEvents)
            return;

        GainsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ResetLeftButton_Click(object sender, RoutedEventArgs e) =>
        ResetSlider(ToOutputLeftSlider);

    private void ResetRightButton_Click(object sender, RoutedEventArgs e) =>
        ResetSlider(ToOutputRightSlider);

    private void ResetSlider(Slider slider)
    {
        if (IsAtDefault(slider.Value))
            return;

        slider.Value = DefaultSliderValue;
    }

    private void RefreshUi()
    {
        ToOutputLeftValue.Text = $"{SliderToGain(ToOutputLeftSlider.Value):0%}";
        ToOutputRightValue.Text = $"{SliderToGain(ToOutputRightSlider.Value):0%}";
        ResetLeftButton.IsEnabled = !IsAtDefault(ToOutputLeftSlider.Value);
        ResetRightButton.IsEnabled = !IsAtDefault(ToOutputRightSlider.Value);
    }

    private static bool IsAtDefault(double sliderValue) =>
        Math.Abs(sliderValue - DefaultSliderValue) < 0.1;

    private static int GainToSlider(float gain) => (int)Math.Clamp(MathF.Round(gain * 100f), 0f, 200f);

    private static float SliderToGain(double value) => (float)(value / 100.0);

    private static double ToMeter(float peak) => Math.Clamp(peak * 100f, 0f, 100f);
}
