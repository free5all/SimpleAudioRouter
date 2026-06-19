using System.Drawing;
using System.Windows.Forms;

namespace SimpleAudioRouter;

internal sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private TrayIconState _currentState = TrayIconState.Idle;

    public TrayService(Action onShow, Action onSettings, Action onExit)
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = AppIconHelper.GetTrayIcon(TrayIconState.Idle),
            Text = AppInfo.ProductName,
            Visible = true,
        };

        _menu = new ContextMenuStrip();
        ApplyMenuTheme();
        RebuildItems(onShow, onSettings, onExit);

        ThemeManager.ThemeChanged += OnThemeChanged;

        _notifyIcon.ContextMenuStrip = _menu;
        _notifyIcon.DoubleClick += (_, _) => onShow();
    }

    public void SetStatus(string status, TrayIconState state)
    {
        _currentState = state;
        _notifyIcon.Text = Truncate($"{AppInfo.ProductName} — {status}", 63);
        _notifyIcon.Icon = AppIconHelper.GetTrayIcon(state);
    }

    public void ShowTrayNotificationOnce()
    {
        _notifyIcon.BalloonTipTitle = AppInfo.ProductName;
        _notifyIcon.BalloonTipText = "Running in the tray. Double-click the icon to reopen.";
        _notifyIcon.BalloonTipIcon = _currentState switch
        {
            TrayIconState.Error => ToolTipIcon.Error,
            TrayIconState.Warning => ToolTipIcon.Warning,
            TrayIconState.Routing => ToolTipIcon.Info,
            _ => ToolTipIcon.Info,
        };
        _notifyIcon.ShowBalloonTip(4000);
    }

    public void Dispose()
    {
        ThemeManager.ThemeChanged -= OnThemeChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
    }

    private void OnThemeChanged() => ApplyMenuTheme();

    private void ApplyMenuTheme()
    {
        _menu.Renderer = ThemeManager.IsDarkMode ? new DarkTrayRenderer() : new ToolStripProfessionalRenderer();
        _menu.ShowImageMargin = false;
        _menu.Font = SystemFonts.MessageBoxFont;

        if (ThemeManager.IsDarkMode)
        {
            _menu.BackColor = TrayPalette.DarkSurface;
            _menu.ForeColor = TrayPalette.DarkText;
        }
        else
        {
            _menu.BackColor = SystemColors.Menu;
            _menu.ForeColor = SystemColors.MenuText;
        }
    }

    private void RebuildItems(Action onShow, Action onSettings, Action onExit)
    {
        _menu.Items.Clear();
        _menu.Items.Add(CreateItem("Open", onShow));
        _menu.Items.Add(CreateItem("Settings…", onSettings));
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(CreateItem("Exit", onExit));
    }

    private ToolStripMenuItem CreateItem(string text, Action action)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += (_, _) => action();
        return item;
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";

    private static class TrayPalette
    {
        public static readonly Color DarkSurface = Color.FromArgb(30, 30, 46);
        public static readonly Color DarkHover = Color.FromArgb(49, 50, 68);
        public static readonly Color DarkBorder = Color.FromArgb(69, 71, 90);
        public static readonly Color DarkText = Color.FromArgb(205, 214, 244);
    }

    private sealed class DarkTrayRenderer : ToolStripProfessionalRenderer
    {
        public DarkTrayRenderer() : base(new DarkTrayColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            var color = e.Item.Selected ? TrayPalette.DarkHover : TrayPalette.DarkSurface;
            using var brush = new SolidBrush(color);
            e.Graphics.FillRectangle(brush, bounds);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            var y = bounds.Height / 2;
            using var pen = new Pen(TrayPalette.DarkBorder);
            e.Graphics.DrawLine(pen, 6, y, bounds.Width - 6, y);
        }
    }

    private sealed class DarkTrayColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => TrayPalette.DarkHover;
        public override Color MenuItemSelectedGradientBegin => TrayPalette.DarkHover;
        public override Color MenuItemSelectedGradientEnd => TrayPalette.DarkHover;
        public override Color MenuItemBorder => TrayPalette.DarkBorder;
        public override Color MenuBorder => TrayPalette.DarkBorder;
        public override Color ToolStripDropDownBackground => TrayPalette.DarkSurface;
        public override Color ImageMarginGradientBegin => TrayPalette.DarkSurface;
        public override Color ImageMarginGradientMiddle => TrayPalette.DarkSurface;
        public override Color ImageMarginGradientEnd => TrayPalette.DarkSurface;
        public override Color SeparatorDark => TrayPalette.DarkBorder;
        public override Color SeparatorLight => TrayPalette.DarkBorder;
    }
}
