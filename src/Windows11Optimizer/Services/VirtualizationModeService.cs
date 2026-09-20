using System.Management;
using System.Text.Json;
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

        // Contrato heredado de VMwareMode.ps1:
        // solo hypervisorlaunchtype=off significa VMware.
        var explicitValue =
            VirtualizationModeCore.HasExplicitValue(result.Output);

        var launchType =
            VirtualizationModeCore.GetConfiguredValue(result.Output);

        var bootTimeUtc = GetBootTimeUtc();
        var hypervisorPresent = GetHypervisorPresentNow();
        var (vbsStatus, memoryIntegrityRunning) = GetDeviceGuardState();
        var pendingRestart = GetPendingRestart(launchType, bootTimeUtc);

        // OFF + hipervisor todavía activo significa que el cambio está preparado
        // pero esta sesión aún no lo ha aplicado, incluso si se modificó fuera
        // de Windows11Optimizer.
        if (launchType.Equals("Off", StringComparison.OrdinalIgnoreCase) &&
            hypervisorPresent)
        {
            pendingRestart = true;
        }

        return new VirtualizationModeState
        {
            HypervisorLaunchType = launchType,
            IsExplicitlyConfigured = explicitValue,
            HypervisorPresentNow = hypervisorPresent,
            PendingRestart = pendingRestart,
            VbsStatus = vbsStatus,
            MemoryIntegrityRunning = memoryIntegrityRunning
        };
    }

    /// <summary>
    /// Modo normal conserva disponibles Hyper-V, WSL2, Virtual Machine Platform
    /// y las funciones de seguridad que dependan del hipervisor de Windows.
    /// No modifica las políticas de VBS ni Integridad de memoria.
    /// </summary>
    public async Task<bool> SetNormalAsync()
    {
        var before = await GetStateAsync();

        if (before.IsConfiguredForNormal)
            return before.PendingRestart;

        await SaveBackupIfMissingAsync(before);

        var result = await CommandRunner.RunAsync(
            "bcdedit.exe",
            ["/set", "{current}", "hypervisorlaunchtype", VirtualizationModeCore.NormalBcdValue]);

        EnsureSuccess(result, "No se pudo preparar el modo normal.");

        var requiresRestart = !before.HypervisorPresentNow;
        SavePendingState("Auto", requiresRestart);

        return requiresRestart;
    }

    /// <summary>
    /// Modo VMware evita que el hipervisor de Windows se inicie en el próximo
    /// arranque. No deshabilita ni elimina las políticas de VBS/HVCI: esas
    /// preferencias permanecen configuradas para volver a funcionar cuando
    /// se restaure el modo normal.
    /// </summary>
    public async Task<bool> SetVmwareDirectAsync()
    {
        var before = await GetStateAsync();

        if (before.IsConfiguredForVmware)
        {
            var requiresExistingRestart =
                before.PendingRestart || before.HypervisorPresentNow;

            if (requiresExistingRestart)
                SavePendingState("Off", true);

            return requiresExistingRestart;
        }

        await SaveBackupIfMissingAsync(before);

        var result = await CommandRunner.RunAsync(
            "bcdedit.exe",
            ["/set", "{current}", "hypervisorlaunchtype", VirtualizationModeCore.VmwareBcdValue]);

        EnsureSuccess(result, "No se pudo preparar el modo VMware.");

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
            ? VirtualizationModeCore.NormalizeValue(backup.OriginalValue)
            : "Auto";

        var alreadyConfigured =
            string.Equals(
                VirtualizationModeCore.NormalizeValue(before.HypervisorLaunchType),
                targetMode,
                StringComparison.OrdinalIgnoreCase);

        if (alreadyConfigured)
        {
            var targetIsAlreadyOff =
                targetMode.Equals("Off", StringComparison.OrdinalIgnoreCase);

            var requiresExistingRestart = targetIsAlreadyOff
                ? before.PendingRestart || before.HypervisorPresentNow
                : before.PendingRestart;

            if (requiresExistingRestart)
                SavePendingState(targetMode, true);

            return requiresExistingRestart;
        }

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

            if (Math.Abs((pending.BootTimeUtc - bootTimeUtc).TotalMinutes) > 2)
            {
                DeletePendingFile();
                return false;
            }

            if (!VirtualizationModeCore.NormalizeValue(pending.RequestedMode).Equals(
                    VirtualizationModeCore.NormalizeValue(configuredMode),
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
            RequestedMode = VirtualizationModeCore.NormalizeValue(requestedMode)
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

    private static (int? VbsStatus, bool? MemoryIntegrityRunning) GetDeviceGuardState()
    {
        try
        {
            var scope = new ManagementScope(
                @"\\.\root\Microsoft\Windows\DeviceGuard");
            scope.Connect();

            using var searcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery(
                    "SELECT VirtualizationBasedSecurityStatus, SecurityServicesRunning FROM Win32_DeviceGuard"));

            using var results = searcher.Get();
            var row = results.Cast<ManagementObject>().FirstOrDefault();

            if (row is null)
                return (null, null);

            int? vbsStatus = row["VirtualizationBasedSecurityStatus"] is null
                ? null
                : Convert.ToInt32(row["VirtualizationBasedSecurityStatus"]);

            bool? memoryIntegrityRunning = null;

            if (row["SecurityServicesRunning"] is Array services)
            {
                memoryIntegrityRunning = services
                    .Cast<object>()
                    .Select(Convert.ToInt32)
                    .Contains(2);
            }

            return (vbsStatus, memoryIntegrityRunning);
        }
        catch
        {
            return (null, null);
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

    private static void EnsureSuccess(
        (int ExitCode, string Output, string Error) result,
        string message)
    {
        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                $"{message}{Environment.NewLine}{result.Error}{result.Output}");
    }
}
