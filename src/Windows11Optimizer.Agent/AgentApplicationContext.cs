using System.Diagnostics;
using System.Threading;
using Windows11Optimizer.Core;

namespace Windows11Optimizer.Agent;

internal sealed class AgentApplicationContext : ApplicationContext
{
    public const string ShowFocusEventName =
        @"Local\Windows11Optimizer.Agent.ShowFocus";

    public const string ShowSettingsEventName =
        @"Local\Windows11Optimizer.Agent.ShowSettings";

    private readonly NotifyIcon _tray = new();
    private readonly FocusBoostService _focus = new();
    private readonly AgentSettingsService _settingsService = new();
    private readonly GlobalHotkeyWindow _hotkeys = new();
    private readonly EventWaitHandle _showFocusEvent;
    private readonly EventWaitHandle _showSettingsEvent;
    private readonly RegisteredWaitHandle _showFocusWait;
    private readonly RegisteredWaitHandle _showSettingsWait;
    private readonly Control _uiDispatcher = new();
    private readonly System.Windows.Forms.Timer _focusWatchTimer = new();

    private RegisteredHotkeys _registered;
    private AgentSettings _settings;
    private MiniFocusForm? _mini;
    private AgentSettingsForm? _settingsForm;

    public AgentApplicationContext()
    {
        _settings = _settingsService.Load();
        _settingsService.UpdateStartup(_settings.StartWithWindows);

        _showFocusEvent = new EventWaitHandle(
            false,
            EventResetMode.AutoReset,
            ShowFocusEventName);

        _showSettingsEvent = new EventWaitHandle(
            false,
            EventResetMode.AutoReset,
            ShowSettingsEventName);

        // Fuerza un HWND creado en el hilo principal para poder volver
        // siempre al hilo WinForms desde señales/hotkeys asíncronos.
        _ = _uiDispatcher.Handle;

        _showFocusWait = ThreadPool.RegisterWaitForSingleObject(
            _showFocusEvent,
            (_, _) => BeginUi(ShowMiniFocus),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);

        _showSettingsWait = ThreadPool.RegisterWaitForSingleObject(
            _showSettingsEvent,
            (_, _) => BeginUi(ShowSettings),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);

        _tray.Icon = System.Drawing.SystemIcons.Application;
        _tray.Text = "Windows11Optimizer Agent";
        _tray.Visible = true;
        _tray.DoubleClick += (_, _) => ShowMiniFocus();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Mini Focus Boost", null, (_, _) => ShowMiniFocus());
        menu.Items.Add("Abrir Windows11Optimizer", null, (_, _) => OpenFullUi());
        menu.Items.Add("Configurar hotkeys", null, (_, _) => ShowSettings());
        menu.Items.Add("Detener Focus Boost", null, (_, _) => StopFocus());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Salir del agente", null, (_, _) => ExitAgent());
        AgentVisualTheme.Apply(menu);
        _tray.ContextMenuStrip = menu;

        _focusWatchTimer.Interval = 2000;
        _focusWatchTimer.Tick += (_, _) =>
        {
            try
            {
                _focus.GetStatus();
            }
            catch
            {
                // El siguiente tick vuelve a comprobar el estado.
            }
        };
        _focusWatchTimer.Start();

        var applied = ApplyHotkeys(_settings);
        if (!applied.Success)
        {
            FluentNotificationForm.ShowNotification(
                "Hotkeys no disponibles",
                applied.Message,
                AgentNotificationTone.Warning);
        }
    }

    public static void SignalExistingInstance(string[] args)
    {
        if (args.Any(x => x.Equals("--agent", StringComparison.OrdinalIgnoreCase)))
            return;

        var eventName = args.Any(x => x.Equals("--settings", StringComparison.OrdinalIgnoreCase))
            ? ShowSettingsEventName
            : ShowFocusEventName;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                using var signal = EventWaitHandle.OpenExisting(eventName);
                signal.Set();
                return;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                Thread.Sleep(100);
            }
            catch
            {
                return;
            }
        }
    }

    private void BeginUi(Action action)
    {
        try
        {
            if (_uiDispatcher.IsDisposed)
                return;

            if (_uiDispatcher.InvokeRequired)
            {
                _uiDispatcher.BeginInvoke(action);
                return;
            }

            action();
        }
        catch
        {
            // Un fallo visual no debe cerrar el agente.
        }
    }

    private void ShowMiniFocus()
    {
        if (_mini is null || _mini.IsDisposed)
        {
            _mini = new MiniFocusForm(_focus);
            _mini.FormClosed += (_, _) => _mini = null;
        }

        _mini.Show();
        _mini.WindowState = FormWindowState.Normal;
        _mini.BringToFront();
        _mini.Activate();
    }

    private void ShowSettings()
    {
        if (_settingsForm is not null && !_settingsForm.IsDisposed)
        {
            _settingsForm.BringToFront();
            return;
        }

        _settingsForm = new AgentSettingsForm(
            _settings,
            settings =>
            {
                var result = ApplyHotkeys(settings);
                if (!result.Success)
                    return result;

                _settings = settings;
                _settingsService.Save(settings);
                return (true, "Configuración guardada.");
            });

        _settingsForm.FormClosed += (_, _) => _settingsForm = null;
        _settingsForm.Show();
    }

    private (bool Success, string Message) ApplyHotkeys(AgentSettings settings)
    {
        var previousSettings = _settings;
        var hadPreviousRegistration =
            _registered.Mini > 0 || _registered.Full > 0;

        UnregisterHotkeys();

        if (TryRegisterHotkeyPair(
                settings,
                out var registered,
                out var error))
        {
            _registered = registered;

            return (
                true,
                $"Hotkeys activos: {settings.MiniFocusHotkey} y {settings.FullUiHotkey}.");
        }

        if (hadPreviousRegistration &&
            TryRegisterHotkeyPair(
                previousSettings,
                out var restored,
                out _))
        {
            _registered = restored;
            return (
                false,
                error + " Se conservaron las combinaciones anteriores.");
        }

        return (false, error);
    }

    private bool TryRegisterHotkeyPair(
        AgentSettings settings,
        out RegisteredHotkeys registered,
        out string error)
    {
        registered = default;
        error = "";

        if (!_hotkeys.TryRegister(
                settings.MiniFocusHotkey,
                ShowMiniFocus,
                out var miniId,
                out var miniError))
        {
            error = miniError;
            return false;
        }

        if (!_hotkeys.TryRegister(
                settings.FullUiHotkey,
                OpenFullUi,
                out var fullId,
                out var fullError))
        {
            _hotkeys.Unregister(miniId);
            error = fullError;
            return false;
        }

        registered = new RegisteredHotkeys(miniId, fullId);
        return true;
    }

    private void UnregisterHotkeys()
    {
        _hotkeys.Unregister(_registered.Mini);
        _hotkeys.Unregister(_registered.Full);
        _registered = default;
    }

    private void OpenFullUi()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Windows11Optimizer.exe");

        if (!File.Exists(path))
        {
            FluentNotificationForm.ShowNotification(
                "Windows11Optimizer",
                "No se encontró Windows11Optimizer.exe junto al agente.",
                AgentNotificationTone.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                Verb = "runas"
            });
        }
        catch
        {
            // El usuario puede cancelar el UAC.
        }
    }

    private void StopFocus()
    {
        try
        {
            _focus.Restore();
            FluentNotificationForm.ShowNotification(
                "Focus Boost",
                "Estado restaurado.",
                AgentNotificationTone.Success);
        }
        catch (Exception ex)
        {
            FluentNotificationForm.ShowNotification(
                "Focus Boost",
                ex.Message,
                AgentNotificationTone.Warning);
        }
    }

    private void ExitAgent()
    {
        try
        {
            _focus.Restore();
        }
        catch
        {
            // La sesión persistida permite reintentar la restauración después.
        }

        _tray.Visible = false;
        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        UnregisterHotkeys();
        _showFocusWait.Unregister(null);
        _showSettingsWait.Unregister(null);
        _focusWatchTimer.Stop();
        _focusWatchTimer.Dispose();
        _hotkeys.Dispose();
        _focus.Dispose();
        _showFocusEvent.Dispose();
        _showSettingsEvent.Dispose();
        _uiDispatcher.Dispose();
        _tray.Dispose();
        base.ExitThreadCore();
    }

    private readonly record struct RegisteredHotkeys(int Mini, int Full);
}
