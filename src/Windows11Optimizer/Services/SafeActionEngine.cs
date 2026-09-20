using System.Text.Json;
using Microsoft.Win32;
using Windows11Optimizer.Models;
using Windows11Optimizer.Profiles;

namespace Windows11Optimizer.Services;

public sealed class SafeActionEngine
{
    private readonly WindowsServiceManager _services;
    private readonly WinUtilNativeService _winUtil;

    private readonly string _directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Windows11Optimizer");

    private string BackupFile =>
        Path.Combine(_directory, "safe-action-backup.json");

    public SafeActionEngine(
        WindowsServiceManager services,
        WinUtilNativeService winUtil)
    {
        _services = services;
        _winUtil = winUtil;
    }

    public string GetState(string id) => id switch
    {
        "acer.on_demand" => GetAcerState(),
        "windows.transparency_off" => GetDwordState(
            @"SoftwareMicrosoftWindowsCurrentVersionThemesPersonalize",
            "EnableTransparency",
            0),
        "explorer.show_extensions" => _winUtil.GetState("WPFToggleShowExt"),
        "explorer.show_hidden" => _winUtil.GetState("WPFToggleHiddenFiles"),
        "taskbar.end_task" => _winUtil.GetState("WPFTweaksEndTaskOnTaskbar"),
        "startup.review_orphans" => "Revisión manual",
        _ => "No disponible"
    };

    public bool CanApply(string id) =>
        SafeActionCatalog.Find(id)?.CanApply == true;

    public void Apply(string id)
    {
        var definition = SafeActionCatalog.Find(id)
            ?? throw new InvalidOperationException("Ajuste desconocido.");

        if (!definition.CanApply)
        {
            throw new InvalidOperationException(
                "Este elemento es una recomendación de revisión y no se ejecuta automáticamente.");
        }

        switch (id)
        {
            case "acer.on_demand":
                ApplyAcerOnDemand();
                break;

            case "windows.transparency_off":
                ApplyDword(
                    id,
                    @"SoftwareMicrosoftWindowsCurrentVersionThemesPersonalize",
                    "EnableTransparency",
                    0);
                break;

            case "explorer.show_extensions":
                _winUtil.Apply("WPFToggleShowExt");
                break;

            case "explorer.show_hidden":
                _winUtil.Apply("WPFToggleHiddenFiles");
                break;

            case "taskbar.end_task":
                _winUtil.Apply("WPFTweaksEndTaskOnTaskbar");
                break;

            default:
                throw new InvalidOperationException(
                    "Este ajuste todavía no tiene un motor local validado.");
        }
    }

    public void Revert(string id)
    {
        switch (id)
        {
            case "acer.on_demand":
                RevertAcerOnDemand();
                break;

            case "windows.transparency_off":
                RevertRegistryAction(id);
                break;

            case "explorer.show_extensions":
                _winUtil.Restore("WPFToggleShowExt");
                break;

            case "explorer.show_hidden":
                _winUtil.Restore("WPFToggleHiddenFiles");
                break;

            case "taskbar.end_task":
                _winUtil.Restore("WPFTweaksEndTaskOnTaskbar");
                break;

            default:
                throw new InvalidOperationException(
                    "No existe un estado guardado para deshacer este ajuste.");
        }
    }

    public bool HasBackup(string id)
    {
        if (id is "explorer.show_extensions" or
            "explorer.show_hidden" or
            "taskbar.end_task")
        {
            var winUtilId = id switch
            {
                "explorer.show_extensions" => "WPFToggleShowExt",
                "explorer.show_hidden" => "WPFToggleHiddenFiles",
                _ => "WPFTweaksEndTaskOnTaskbar"
            };

            return _winUtil.HasBackup(winUtilId);
        }

        return LoadBackups().ContainsKey(id);
    }

    private string GetAcerState()
    {
        var installed = 0;
        var automatic = 0;

        foreach (var name in SafeProfile.AcerOnDemandServices)
        {
            var info = _services.GetInfo(name, "", "");
            if (info is null)
                continue;

            installed++;

            if (info.StartMode.Equals("Auto", StringComparison.OrdinalIgnoreCase) ||
                info.StartMode.Equals("Automatic", StringComparison.OrdinalIgnoreCase))
            {
                automatic++;
            }
        }

        if (installed == 0)
            return "No instalado";

        return automatic == 0
            ? "Aplicado"
            : $"{automatic} se inician con Windows";
    }

    private void ApplyAcerOnDemand()
    {
        var backups = LoadBackups();

        if (!backups.ContainsKey("acer.on_demand"))
        {
            var backup = new SafeActionBackup
            {
                Id = "acer.on_demand"
            };

            foreach (var name in SafeProfile.AcerOnDemandServices)
            {
                var info = _services.GetInfo(name, "", "");
                if (info is null)
                    continue;

                backup.Services.Add(new SafeActionServiceBackup
                {
                    Name = name,
                    StartMode = info.StartMode,
                    WasRunning = info.State.Equals(
                        "Running",
                        StringComparison.OrdinalIgnoreCase)
                });
            }

            backups[backup.Id] = backup;
            SaveBackups(backups);
        }

        foreach (var name in SafeProfile.AcerOnDemandServices)
        {
            var info = _services.GetInfo(name, "", "");
            if (info is null)
                continue;

            _services.SetStartupManualAsync(name).GetAwaiter().GetResult();
        }
    }

    private void RevertAcerOnDemand()
    {
        var backups = LoadBackups();

        if (!backups.TryGetValue("acer.on_demand", out var backup))
        {
            throw new InvalidOperationException(
                "No existe una copia previa de este ajuste.");
        }

        foreach (var item in backup.Services)
        {
            _services.RestoreAsync(
                    item.Name,
                    item.StartMode,
                    item.WasRunning)
                .GetAwaiter()
                .GetResult();
        }

        backups.Remove("acer.on_demand");
        SaveBackups(backups);
    }

    private static string GetDwordState(
        string path,
        string name,
        int expected)
    {
        using var key = Registry.CurrentUser.OpenSubKey(path, false);
        var value = key?.GetValue(name);

        return value is not null && Convert.ToInt32(value) == expected
            ? "Aplicado"
            : "Disponible";
    }

    private void ApplyDword(
        string id,
        string path,
        string name,
        int value)
    {
        var backups = LoadBackups();

        if (!backups.ContainsKey(id))
        {
            using var currentKey = Registry.CurrentUser.OpenSubKey(path, false);
            var current = currentKey?.GetValue(name);

            backups[id] = new SafeActionBackup
            {
                Id = id,
                RegistryValues =
                [
                    new SafeActionRegistryBackup
                    {
                        Path = path,
                        Name = name,
                        Existed = current is not null,
                        PreviousDword =
                            current is null ? 0 : Convert.ToInt32(current)
                    }
                ]
            };

            SaveBackups(backups);
        }

        using var key = Registry.CurrentUser.CreateSubKey(path, writable: true)
            ?? throw new InvalidOperationException(
                $"No se pudo abrir HKCU\{path}.");

        key.SetValue(name, value, RegistryValueKind.DWord);
    }

    private void RevertRegistryAction(string id)
    {
        var backups = LoadBackups();

        if (!backups.TryGetValue(id, out var backup))
        {
            throw new InvalidOperationException(
                "No existe una copia previa de este ajuste.");
        }

        foreach (var value in backup.RegistryValues)
        {
            using var key = Registry.CurrentUser.CreateSubKey(
                value.Path,
                writable: true)
                ?? throw new InvalidOperationException(
                    $"No se pudo abrir HKCU\{value.Path}.");

            if (value.Existed)
            {
                key.SetValue(
                    value.Name,
                    value.PreviousDword,
                    RegistryValueKind.DWord);
            }
            else
            {
                key.DeleteValue(value.Name, throwOnMissingValue: false);
            }
        }

        backups.Remove(id);
        SaveBackups(backups);
    }

    private Dictionary<string, SafeActionBackup> LoadBackups()
    {
        try
        {
            if (!File.Exists(BackupFile))
            {
                return new Dictionary<string, SafeActionBackup>(
                    StringComparer.OrdinalIgnoreCase);
            }

            var data = JsonSerializer.Deserialize<
                Dictionary<string, SafeActionBackup>>(
                    File.ReadAllText(BackupFile))
                ?? new Dictionary<string, SafeActionBackup>();

            return new Dictionary<string, SafeActionBackup>(
                data,
                StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, SafeActionBackup>(
                StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SaveBackups(
        Dictionary<string, SafeActionBackup> backups)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            BackupFile,
            JsonSerializer.Serialize(
                backups,
                new JsonSerializerOptions { WriteIndented = true }));
    }
}

public sealed class SafeActionBackup
{
    public string Id { get; set; } = "";
    public List<SafeActionRegistryBackup> RegistryValues { get; set; } = [];
    public List<SafeActionServiceBackup> Services { get; set; } = [];
}

public sealed class SafeActionRegistryBackup
{
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Existed { get; set; }
    public int PreviousDword { get; set; }
}

public sealed class SafeActionServiceBackup
{
    public string Name { get; set; } = "";
    public string StartMode { get; set; } = "";
    public bool WasRunning { get; set; }
}
