using System.Management;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows11Optimizer.Models;

namespace Windows11Optimizer.Services;

public sealed class VirtualizationModeService
{
    private readonly string _directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Windows11Optimizer");

    private string BackupFile =>
        Path.Combine(_directory, "virtualization-backup.json");

    private string PendingFile =>
        Path.Combine(_directory, "virtualization-pending.json");

    public bool BackupExists => File.Exists(BackupFile);

    public async Task<VirtualizationModeState> GetStateAsync()
    {
        var result = await CommandRunner.RunAsync(
            "bcdedit.exe",
            ["/enum", "{current}"]);

        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                "No se pudo consultar la configuración de arranque. " +
                result.Error + result.Output);

        // El nombre del elemento BCD es estable aunque Windows esté en español.
        var match = Regex.Match(
            result.Output,
            @"(?im)^\s*hypervisorlaunchtype\s+(?<value>\S+)\s*$");

        var explicitValue = match.Success;
        var launchType = explicitValue
            ? NormalizeMode(match.Groups["value"].Value)
            : "Auto";

        var bootTimeUtc = GetBootTimeUtc();
        var pendingRestart = GetPendingRestart(launchType, bootTimeUtc);

        return new VirtualizationModeState
        {
            HypervisorLaunchType = launchType,
            IsExplicitlyConfigured = explicitValue,
            HypervisorPresentNow = GetHypervisorPresentNow(),
            PendingRestart = pendingRestart
        };
    }

    /// <summary>
    /// Devuelve true únicamente cuando el cambio necesita un reinicio para
    /// modificar el estado efectivo de esta sesión.
    /// </summary>
    public async Task<bool> SetNormalAsync()
    {
        var before = await GetStateAsync();

        if (before.IsConfiguredForNormal)
            return before.PendingRestart;

        await SaveBackupIfMissingAsync(before);

        var result = await CommandRunner.RunAsync(
            "bcdedit.exe",
            ["/set", "{current}", "hypervisorlaunchtype", "auto"]);

        EnsureSuccess(result, "No se pudo preparar el modo normal.");

        // Si el hipervisor ya está activo, el equipo ya está funcionalmente
        // en modo normal y no hace falta reiniciar por este cambio.
        var requiresRestart = !before.HypervisorPresentNow;
        SavePendingState("Auto", requiresRestart);

        return requiresRestart;
    }

    /// <summary>
    /// Devuelve true únicamente cuando el hipervisor de Windows sigue activo
    /// y por tanto hace falta reiniciar para pasar al modo VMware.
    /// </summary>
    public async Task<bool> SetVmwareDirectAsync()
    {
        var before = await GetStateAsync();

        if (before.IsConfiguredForVmware)
            return before.PendingRestart;

        await SaveBackupIfMissingAsync(before);

        var result = await CommandRunner.RunAsync(
            "bcdedit.exe",
            ["/set", "{current}", "hypervisorlaunchtype", "off"]);

        EnsureSuccess(result, "No se pudo preparar el modo VMware.");

        // Si el hipervisor no está activo en esta sesión, VMware ya puede
        // trabajar sin él; no forzamos un reinicio innecesario.
        var requiresRestart = before.HypervisorPresentNow;
        SavePendingState("Off", requiresRestart);

        return requiresRestart;
    }

    public async Task<bool> RestoreAsync()
    {
        if (!BackupExists)
            throw new InvalidOperationException(
                "No existe una copia de seguridad de virtualización creada por Windows11Optimizer.");

        var backup = JsonSerializer.Deserialize<VirtualizationModeBackup>(
            await File.ReadAllTextAsync(BackupFile));

        if (backup is null)
            throw new InvalidOperationException(
                "La copia de seguridad de virtualización no se pudo leer.");

        var before = await GetStateAsync();
        var targetMode = backup.HadExplicitValue
            ? NormalizeMode(backup.OriginalValue)
            : "Auto";

        var alreadyConfigured =
            string.Equals(
                NormalizeMode(before.HypervisorLaunchType),
                targetMode,
                StringComparison.OrdinalIgnoreCase);

        if (alreadyConfigured)
            return before.PendingRestart;

        (int ExitCode, string Output, string Error) result;

        if (!backup.HadExplicitValue)
        {
            result = await CommandRunner.RunAsync(
                "bcdedit.exe",
                ["/deletevalue", "{current}", "hypervisorlaunchtype"]);
        }
        else
        {
            result = await CommandRunner.RunAsync(
                "bcdedit.exe",
                ["/set", "{current}", "hypervisorlaunchtype", targetMode.ToLowerInvariant()]);
        }

        EnsureSuccess(result, "No se pudo restaurar la configuración original.");

        var targetIsOff = targetMode.Equals("Off", StringComparison.OrdinalIgnoreCase);
        var requiresRestart = targetIsOff
            ? before.HypervisorPresentNow
            : !before.HypervisorPresentNow;

        SavePendingState(targetMode, requiresRestart);
        return requiresRestart;
    }

    public static async Task RestartWindowsAsync()
    {
        var result = await CommandRunner.RunAsync(
            "shutdown.exe",
            ["/r", "/t", "0"]);

        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                "Windows no aceptó la solicitud de reinicio. " +
                result.Error + result.Output);
    }

    private async Task SaveBackupIfMissingAsync(VirtualizationModeState? knownState = null)
    {
        if (BackupExists)
            return;

        var state = knownState ?? await GetStateAsync();

        Directory.CreateDirectory(_directory);

        var backup = new VirtualizationModeBackup
        {
            CreatedAt = DateTime.Now,
            HadExplicitValue = state.IsExplicitlyConfigured,
            OriginalValue = state.HypervisorLaunchType
        };

        var json = JsonSerializer.Serialize(
            backup,
            new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(BackupFile, json);
    }

    private bool GetPendingRestart(string configuredMode, DateTime bootTimeUtc)
    {
        try
        {
            if (!File.Exists(PendingFile))
                return false;

            var pending = JsonSerializer.Deserialize<VirtualizationPendingChange>(
                File.ReadAllText(PendingFile));

            if (pending is null)
            {
                DeletePendingFile();
                return false;
            }

            // Si Windows ya reinició desde que se solicitó el cambio,
            // la solicitud pendiente ya fue consumida.
            if (Math.Abs((pending.BootTimeUtc - bootTimeUtc).TotalMinutes) > 2)
            {
                DeletePendingFile();
                return false;
            }

            // Si otra herramienta cambió BCD después de nosotros, no mostramos
            // un reinicio pendiente que ya no corresponde.
            if (!NormalizeMode(pending.RequestedMode).Equals(
                    NormalizeMode(configuredMode),
                    StringComparison.OrdinalIgnoreCase))
            {
                DeletePendingFile();
                return false;
            }

            return true;
        }
        catch
        {
            DeletePendingFile();
            return false;
        }
    }

    private void SavePendingState(string requestedMode, bool requiresRestart)
    {
        Directory.CreateDirectory(_directory);

        if (!requiresRestart)
        {
            DeletePendingFile();
            return;
        }

        var pending = new VirtualizationPendingChange
        {
            CreatedAt = DateTime.Now,
            BootTimeUtc = GetBootTimeUtc(),
            RequestedMode = NormalizeMode(requestedMode)
        };

        File.WriteAllText(
            PendingFile,
            JsonSerializer.Serialize(
                pending,
                new JsonSerializerOptions { WriteIndented = true }));
    }

    private void DeletePendingFile()
    {
        try
        {
            if (File.Exists(PendingFile))
                File.Delete(PendingFile);
        }
        catch
        {
            // Un fallo al limpiar el marcador no debe bloquear la app.
        }
    }

    private static bool GetHypervisorPresentNow()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT HypervisorPresent FROM Win32_ComputerSystem");

            using var results = searcher.Get();
            var row = results.Cast<ManagementObject>().FirstOrDefault();

            return row?["HypervisorPresent"] is bool value && value;
        }
        catch
        {
            return false;
        }
    }

    private static DateTime GetBootTimeUtc()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT LastBootUpTime FROM Win32_OperatingSystem");

            using var results = searcher.Get();
            var row = results.Cast<ManagementObject>().FirstOrDefault();
            var raw = Convert.ToString(row?["LastBootUpTime"]);

            if (!string.IsNullOrWhiteSpace(raw))
                return ManagementDateTimeConverter.ToDateTime(raw).ToUniversalTime();
        }
        catch
        {
            // Fallback abajo.
        }

        return DateTime.UtcNow - TimeSpan.FromMilliseconds(Environment.TickCount64);
    }

    private static string NormalizeMode(string value) =>
        value.Equals("Off", StringComparison.OrdinalIgnoreCase)
            ? "Off"
            : "Auto";

    private static void EnsureSuccess(
        (int ExitCode, string Output, string Error) result,
        string message)
    {
        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                $"{message}{Environment.NewLine}{result.Error}{result.Output}");
    }
}
