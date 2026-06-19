using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace SimpleAudioRouter;

internal sealed class TrayService : IDisposable
{
    private static readonly Color TextPrimary = Color.FromArgb(205, 214, 244);
    private static readonly Color TextMuted = Color.FromArgb(147, 153, 178);
    private static readonly Color Border = Color.FromArgb(69, 71, 90);

    private readonly NotifyIcon _notifyIcon;
    private readonly Action _onShow;
    private readonly Action _onSettings;
    private readonly Action _onExit;
    private TrayIconState _currentState = TrayIconState.Idle;

    public TrayService(Action onShow, Action onSettings, Action onExit)
    {
        _onShow = onShow;
        _onSettings = onSettings;
        _onExit = onExit;

        _notifyIcon = new NotifyIcon
        {
            Icon = AppIconHelper.GetTrayIcon(TrayIconState.Idle),
            Text = "SimpleAudioRouter — Idle",
            Visible = true,
        };

        var menu = new ContextMenuStrip
        {
            Renderer = new DarkTrayMenuRenderer(),
            ShowImageMargin = false,
            BackColor = DarkTrayColorTable.Surface,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9f),
            Padding = new Padding(0, 0, 0, 4),
        };

        menu.Items.Add(CreateTitleHeader());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateItem("Open window", (_, _) => _onShow()));
        menu.Items.Add(CreateItem("Settings…", (_, _) => _onSettings()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateItem("Exit", (_, _) => _onExit()));

        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => _onShow();
    }

    public void SetStatus(string status, TrayIconState state)
    {
        _currentState = state;
        _notifyIcon.Text = Truncate($"SimpleAudioRouter — {status}", 63);
        _notifyIcon.Icon = AppIconHelper.GetTrayIcon(state);
    }

    public void ShowTrayNotificationOnce()
    {
        _notifyIcon.BalloonTipTitle = "SimpleAudioRouter";
        _notifyIcon.BalloonTipText = "Running in the tray. Double-click the icon to reopen.";
        _notifyIcon.BalloonTipIcon = _currentState switch
        {
            TrayIconState.Error => ToolTipIcon.Error,
            TrayIconState.Warning => ToolTipIcon.Warning,
            TrayIconState.Routing => ToolTipIcon.Info,
            _ => ToolTipIcon.Info,
        };
        _notifyIcon.ShowBalloonTip(3000);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private static TrayTitleHeader CreateTitleHeader()
    {
        using var icon = AppIconHelper.GetAppIcon();
        using var source = icon.ToBitmap();
        var image = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(image))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(source, new Rectangle(0, 0, 16, 16));
        }

        return new TrayTitleHeader
        {
            Text = "SimpleAudioRouter",
            Image = image,
            ImageScaling = ToolStripItemImageScaling.None,
            Font = CreateTitleFont(),
            ForeColor = TextPrimary,
            Margin = Padding.Empty,
            Padding = new Padding(8, 0, 8, 0),
            AutoSize = false,
            Height = 32,
            Width = 180,
        };
    }

    private static Font CreateTitleFont()
    {
        try
        {
            return new Font("Segoe UI Semibold", 13f, FontStyle.Regular, GraphicsUnit.Point);
        }
        catch
        {
            return new Font("Segoe UI", 13f, FontStyle.Bold, GraphicsUnit.Point);
        }
    }

    private static ToolStripMenuItem CreateItem(string text, EventHandler? onClick)
    {
        var item = new ToolStripMenuItem(text)
        {
            ForeColor = TextPrimary,
            BackColor = DarkTrayColorTable.Surface,
            Padding = new Padding(8, 4, 8, 4),
        };

        if (onClick is not null)
            item.Click += onClick;

        return item;
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";

    private sealed class TrayTitleHeader : ToolStripLabel;

    private sealed class DarkTrayMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkTrayMenuRenderer() : base(new DarkTrayColorTable()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item is TrayTitleHeader)
            {
                e.TextColor = TextPrimary;
                var g = e.Graphics;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                const int iconSize = 16;
                const int gap = 8;
                var iconY = e.Item.ContentRectangle.Top + (e.Item.ContentRectangle.Height - iconSize) / 2;
                var textY = e.Item.ContentRectangle.Top + (e.Item.ContentRectangle.Height - e.TextFont.Height) / 2;

                if (e.Item.Image is not null)
                {
                    g.DrawImage(e.Item.Image, e.Item.ContentRectangle.Left, iconY, iconSize, iconSize);
                }

                using var brush = new SolidBrush(TextPrimary);
                var textX = e.Item.ContentRectangle.Left + iconSize + gap;
                g.DrawString(e.Item.Text, e.TextFont, brush, textX, textY);
                return;
            }

            e.TextColor = e.Item.Enabled ? TextPrimary : TextMuted;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item is TrayTitleHeader)
            {
                var bounds = new Rectangle(Point.Empty, e.Item.Size);
                using var brush = new SolidBrush(DarkTrayColorTable.Surface);
                e.Graphics.FillRectangle(brush, bounds);

                using var pen = new Pen(Border);
                e.Graphics.DrawLine(pen, 0, bounds.Bottom - 1, bounds.Width, bounds.Bottom - 1);
                return;
            }

            var itemBounds = new Rectangle(Point.Empty, e.Item.Size);
            using var itemBrush = new SolidBrush(e.Item.Selected ? DarkTrayColorTable.SurfaceRaised : DarkTrayColorTable.Surface);
            e.Graphics.FillRectangle(itemBrush, itemBounds);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            var y = bounds.Height / 2;
            using var pen = new Pen(Border);
            e.Graphics.DrawLine(pen, 8, y, bounds.Width - 8, y);
        }
    }

    private sealed class DarkTrayColorTable : ProfessionalColorTable
    {
        internal static Color Surface => Color.FromArgb(30, 30, 46);
        internal static Color SurfaceRaised => Color.FromArgb(49, 50, 68);

        public override Color MenuItemSelected => SurfaceRaised;
        public override Color MenuItemSelectedGradientBegin => SurfaceRaised;
        public override Color MenuItemSelectedGradientEnd => SurfaceRaised;
        public override Color MenuItemBorder => Border;
        public override Color MenuBorder => Border;
        public override Color ToolStripDropDownBackground => Surface;
        public override Color ImageMarginGradientBegin => Surface;
        public override Color ImageMarginGradientMiddle => Surface;
        public override Color ImageMarginGradientEnd => Surface;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
        public override Color ToolStripGradientBegin => Surface;
        public override Color ToolStripGradientMiddle => Surface;
        public override Color ToolStripGradientEnd => Surface;
    }
}
