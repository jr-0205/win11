using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Windows11Optimizer.Agent;

internal sealed class GlobalHotkeyWindow : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

    private readonly Dictionary<int, Action> _callbacks = [];
    private int _nextId = 100;

    public GlobalHotkeyWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "Windows11Optimizer.Agent.Hotkeys",
            Parent = new IntPtr(-3)
        });
    }

    public bool TryRegister(
        string shortcut,
        Action callback,
        out int registrationId,
        out string error)
    {
        registrationId = 0;
        error = "";

        if (!TryParseShortcut(shortcut, out var modifiers, out var key, out error))
            return false;

        var id = _nextId++;

        if (!RegisterHotKey(Handle, id, modifiers, (uint)key))
        {
            error =
                $"La combinación {shortcut} ya está ocupada por otra aplicación o por Windows.";
            return false;
        }

        _callbacks[id] = callback;
        registrationId = id;
        return true;
    }

    public void Unregister(int id)
    {
        if (id <= 0)
            return;

        UnregisterHotKey(Handle, id);
        _callbacks.Remove(id);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey)
        {
            var id = m.WParam.ToInt32();
            if (_callbacks.TryGetValue(id, out var callback))
            {
                callback();
                return;
            }
        }

        base.WndProc(ref m);
    }

    private static bool TryParseShortcut(
        string shortcut,
        out uint modifiers,
        out Keys key,
        out string error)
    {
        modifiers = 0;
        key = Keys.None;
        error = "";

        var parts = shortcut
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < 2)
        {
            error = "Usa un formato como Ctrl+Alt+Space.";
            return false;
        }

        foreach (var raw in parts[..^1])
        {
            switch (raw.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= ModControl;
                    break;

                case "alt":
                    modifiers |= ModAlt;
                    break;

                case "shift":
                    modifiers |= ModShift;
                    break;

                case "win":
                case "windows":
                    modifiers |= ModWin;
                    break;

                default:
                    error = $"Modificador no reconocido: {raw}.";
                    return false;
            }
        }

        if (!Enum.TryParse(parts[^1], ignoreCase: true, out key) ||
            key == Keys.None ||
            key is Keys.ControlKey or Keys.Menu or Keys.ShiftKey)
        {
            error = $"Tecla no reconocida: {parts[^1]}.";
            return false;
        }

        return true;
    }

    public void Dispose()
    {
        foreach (var id in _callbacks.Keys.ToArray())
            Unregister(id);

        if (Handle != IntPtr.Zero)
            DestroyHandle();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);
}
