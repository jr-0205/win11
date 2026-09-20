using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace Windows11Optimizer.Services;

public sealed class WinUtilNativeService
{
    private readonly string _directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Windows11Optimizer");

    private string BackupFile =>
        Path.Combine(_directory, "winutil-native-backup.json");

    private static readonly Dictionary<string, RegistryChange[]> Supported =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["WPFToggleShowExt"] =
            [
                new(
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    "HideFileExt",
                    0)
            ],
            ["WPFToggleHiddenFiles"] =
            [
                new(
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    "Hidden",
                    1)
            ],
            ["WPFTweaksEndTaskOnTaskbar"] =
            [
                new(
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings",
                    "TaskbarEndTask",
                    1)
            ],
            ["WPFToggleTaskbarSearch"] =
            [
                new(
                    @"Software\Microsoft\Windows\CurrentVersion\Search",
                    "SearchboxTaskbarMode",
                    1)
            ],
            ["WPFToggleDarkMode"] =
            [
                new(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme",
                    0),
                new(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "SystemUsesLightTheme",
                    0)
            ]
        };

    public bool Supports(string id) => Supported.ContainsKey(id);

    public IReadOnlyCollection<string> SupportedIds => Supported.Keys;

    public string GetSupportText(string id) => Supports(id)
        ? "Aplicable desde Windows11Optimizer"
        : "Solo consulta por ahora";

    public string GetState(string id)
    {
        if (!Supported.TryGetValue(id, out var changes))
            return "Solo consulta";

        foreach (var change in changes)
        {
            using var key = Registry.CurrentUser.OpenSubKey(change.Path, writable: false);
            var value = key?.GetValue(change.Name);

            if (value is null || Convert.ToInt32(value) != change.Value)
                return "Disponible";
        }

        return "Aplicado";
    }

    public void Apply(string id)
    {
        if (!Supported.TryGetValue(id, out var changes))
            throw new InvalidOperationException(
                "Esta opción todavía no está portada de forma segura a Windows11Optimizer.");

        var backups = LoadBackups();

        if (!backups.ContainsKey(id))
        {
            backups[id] = changes
                .Select(Capture)
                .ToList();

            SaveBackups(backups);
        }

        foreach (var change in changes)
        {
            using var key = Registry.CurrentUser.CreateSubKey(change.Path, writable: true)
                ?? throw new InvalidOperationException(
                    $"No se pudo abrir HKCU\\{change.Path}.");

            key.SetValue(change.Name, change.Value, RegistryValueKind.DWord);
        }

        NotifyWindows();
    }

    public void Restore(string id)
    {
        var backups = LoadBackups();

        if (!backups.TryGetValue(id, out var values) || values.Count == 0)
            throw new InvalidOperationException(
                "No existe una copia previa para restaurar esta opción.");

        foreach (var backup in values)
        {
            using var key = Registry.CurrentUser.CreateSubKey(backup.Path, writable: true)
                ?? throw new InvalidOperationException(
                    $"No se pudo abrir HKCU\\{backup.Path}.");

            if (backup.Existed)
            {
                key.SetValue(
                    backup.Name,
                    backup.PreviousValue,
                    RegistryValueKind.DWord);
            }
            else
            {
                key.DeleteValue(backup.Name, throwOnMissingValue: false);
            }
        }

        NotifyWindows();
    }

    public bool HasBackup(string id)
    {
        var backups = LoadBackups();
        return backups.TryGetValue(id, out var values) && values.Count > 0;
    }

    private static RegistryValueBackup Capture(RegistryChange change)
    {
        using var key = Registry.CurrentUser.OpenSubKey(change.Path, writable: false);
        var current = key?.GetValue(change.Name);

        return new RegistryValueBackup
        {
            Path = change.Path,
            Name = change.Name,
            Existed = current is not null,
            PreviousValue = current is null ? 0 : Convert.ToInt32(current)
        };
    }

    private Dictionary<string, List<RegistryValueBackup>> LoadBackups()
    {
        try
        {
            if (!File.Exists(BackupFile))
            {
                return new Dictionary<string, List<RegistryValueBackup>>(
                    StringComparer.OrdinalIgnoreCase);
            }

            var data = JsonSerializer.Deserialize<
                Dictionary<string, List<RegistryValueBackup>>>(
                    File.ReadAllText(BackupFile))
                ?? new Dictionary<string, List<RegistryValueBackup>>();

            return new Dictionary<string, List<RegistryValueBackup>>(
                data,
                StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, List<RegistryValueBackup>>(
                StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SaveBackups(
        Dictionary<string, List<RegistryValueBackup>> backups)
    {
        Directory.CreateDirectory(_directory);

        File.WriteAllText(
            BackupFile,
            JsonSerializer.Serialize(
                backups,
                new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void NotifyWindows()
    {
        try
        {
            SendMessageTimeout(
                new IntPtr(0xffff),
                0x001A,
                IntPtr.Zero,
                "Environment",
                0x0002,
                1000,
                out _);

            SendMessageTimeout(
                new IntPtr(0xffff),
                0x001A,
                IntPtr.Zero,
                "ImmersiveColorSet",
                0x0002,
                1000,
                out _);
        }
        catch
        {
            // El cambio queda escrito aunque Windows no refresque la UI al instante.
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        string? lParam,
        uint flags,
        uint timeout,
        out IntPtr result);

    private sealed record RegistryChange(
        string Path,
        string Name,
        int Value);

    private sealed class RegistryValueBackup
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Existed { get; set; }
        public int PreviousValue { get; set; }
    }
}
