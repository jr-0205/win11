using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Windows11Optimizer.Agent;

internal enum AgentNotificationTone
{
    Info,
    Success,
    Warning,
    Error
}

internal sealed class FluentNotificationForm : Form
{
    private readonly System.Windows.Forms.Timer _lifetimeTimer = new();
    private readonly Stopwatch _lifetime = Stopwatch.StartNew();

    private FluentNotificationForm(
        string title,
        string message,
        AgentNotificationTone tone)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Width = 370;
        Height = 118;
        BackColor = Color.FromArgb(20, 23, 29);
        ForeColor = Color.FromArgb(245, 247, 250);
        Opacity = 0.98;
        Padding = new Padding(0);

        var accent = new Panel
        {
            Dock = DockStyle.Left,
            Width = 5,
            BackColor = ToneColor(tone)
        };

        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 13, 14, 12),
            BackColor = BackColor
        };

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 25,
            AutoEllipsis = true,
            Font = new Font(
                "Segoe UI",
                10.5f,
                FontStyle.Bold),
            ForeColor = ForeColor,
            BackColor = Color.Transparent
        };

        var messageLabel = new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Font = new Font(
                "Segoe UI",
                9f,
                FontStyle.Regular),
            ForeColor = Color.FromArgb(184, 191, 202),
            BackColor = Color.Transparent
        };

        contentPanel.Controls.Add(messageLabel);
        contentPanel.Controls.Add(titleLabel);
        Controls.Add(contentPanel);
        Controls.Add(accent);

        Click += (_, _) => Close();
        contentPanel.Click += (_, _) => Close();
        titleLabel.Click += (_, _) => Close();
        messageLabel.Click += (_, _) => Close();

        Shown += (_, _) =>
        {
            ApplyRoundedRegion();
            PositionNearNotificationArea();
        };

        _lifetimeTimer.Interval = 50;
        _lifetimeTimer.Tick += (_, _) =>
        {
            if (_lifetime.ElapsedMilliseconds < 3400)
                return;

            Opacity = Math.Max(0, Opacity - 0.08);

            if (Opacity > 0.02)
                return;

            _lifetimeTimer.Stop();
            Close();
        };
        _lifetimeTimer.Start();

        FormClosed += (_, _) =>
        {
            _lifetimeTimer.Stop();
            _lifetimeTimer.Dispose();
        };
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WsExToolWindow = 0x00000080;
            const int WsExNoActivate = 0x08000000;

            var cp = base.CreateParams;
            cp.ExStyle |= WsExToolWindow | WsExNoActivate;
            return cp;
        }
    }

    public static void ShowNotification(
        string title,
        string message,
        AgentNotificationTone tone = AgentNotificationTone.Info)
    {
        var popup = new FluentNotificationForm(title, message, tone);
        popup.Show();
    }

    private void PositionNearNotificationArea()
    {
        var screen = Screen.PrimaryScreen;
        if (screen is null)
            return;

        var area = screen.WorkingArea;
        Location = new Point(
            area.Right - Width - 18,
            area.Bottom - Height - 18);
    }

    private void ApplyRoundedRegion()
    {
        const int radius = 20;

        var handle = CreateRoundRectRgn(
            0,
            0,
            Width + 1,
            Height + 1,
            radius,
            radius);

        if (handle == IntPtr.Zero)
            return;

        try
        {
            Region = Region.FromHrgn(handle);
        }
        finally
        {
            DeleteObject(handle);
        }
    }

    private static Color ToneColor(AgentNotificationTone tone) => tone switch
    {
        AgentNotificationTone.Success => Color.FromArgb(46, 166, 106),
        AgentNotificationTone.Warning => Color.FromArgb(194, 138, 36),
        AgentNotificationTone.Error => Color.FromArgb(217, 92, 102),
        _ => Color.FromArgb(94, 161, 255)
    };

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(
        int left,
        int top,
        int right,
        int bottom,
        int widthEllipse,
        int heightEllipse);
}
