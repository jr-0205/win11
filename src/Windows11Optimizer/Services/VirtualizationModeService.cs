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

        var match = Regex.Match(
            result.Output,
            @"(?im)^s*hypervisorlaunchtypes+(?<value>S+)s*$");

        var explicitValue = match.Success;
        var launchType = explicitValue
            ? match.Groups["value"].Value
            : "Auto";

        return new VirtualizationModeState
        {
            HypervisorLaunchType = launchType,
            IsExplicitlyConfigured = explicitValue,
            HypervisorPresentNow = GetHypervisorPresentNow()
        };
    }

    public async Task SetNormalAsync()
    {
        await SaveBackupIfMissingAsync();

        var result = await CommandRunner.RunAsync(
            "bcdedit.exe",
            ["/set", "{current}", "hypervisorlaunchtype", "auto"]);

        EnsureSuccess(result, "No se pudo activar el inicio normal del hipervisor.");
    }

    public async Task SetVmwareDirectAsync()
    {
        await SaveBackupIfMissingAsync();

        var result = await CommandRunner.RunAsync(
            "bcdedit.exe",
            ["/set", "{current}", "hypervisorlaunchtype", "off"]);

        EnsureSuccess(result, "No se pudo desactivar el inicio del hipervisor.");
    }

    public async Task RestoreAsync()
    {
        if (!BackupExists)
            throw new InvalidOperationException(
                "No existe un backup de virtualización creado por Windows11Optimizer.");

        var backup = JsonSerializer.Deserialize<VirtualizationModeBackup>(
            await File.ReadAllTextAsync(BackupFile));

        if (backup is null)
            throw new InvalidOperationException(
                "El backup de virtualización no se pudo leer.");

        (int ExitCode, string Output, string Error) result;

        if (!backup.HadExplicitValue)
        {
            result = await CommandRunner.RunAsync(
                "bcdedit.exe",
                ["/deletevalue", "{current}", "hypervisorlaunchtype"]);
        }
        else
        {
            var value = backup.OriginalValue.Equals(
                "Off",
                StringComparison.OrdinalIgnoreCase)
                ? "off"
                : "auto";

            result = await CommandRunner.RunAsync(
                "bcdedit.exe",
                ["/set", "{current}", "hypervisorlaunchtype", value]);
        }

        EnsureSuccess(result, "No se pudo restaurar la configuración original del hipervisor.");
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

    private async Task SaveBackupIfMissingAsync()
    {
        if (BackupExists)
            return;

        var state = await GetStateAsync();

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

    private static void EnsureSuccess(
        (int ExitCode, string Output, string Error) result,
        string message)
    {
        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                $"{message}{Environment.NewLine}{result.Error}{result.Output}");
    }
}
