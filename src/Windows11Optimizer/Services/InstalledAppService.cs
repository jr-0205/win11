using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Windows11Optimizer.Models;

namespace Windows11Optimizer.Services;

public sealed class InstalledAppService
{
    private const string UninstallRegistryPath =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    public IReadOnlyList<InstalledApp> GetInstalledApps()
    {
        var apps = new List<InstalledApp>();

        ReadHive(apps, RegistryHive.LocalMachine, RegistryView.Registry64, "HKLM64");
        ReadHive(apps, RegistryHive.LocalMachine, RegistryView.Registry32, "HKLM32");
        ReadHive(apps, RegistryHive.CurrentUser, RegistryView.Registry64, "HKCU64");
        ReadHive(apps, RegistryHive.CurrentUser, RegistryView.Registry32, "HKCU32");

        return apps
            .GroupBy(
                x => $"{x.DisplayName}|{x.DisplayVersion}|{x.Publisher}",
                StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public void StartUninstall(InstalledApp app)
    {
        if (!app.CanUninstall)
        {
            throw new InvalidOperationException(
                "Esta aplicación no publica un desinstalador seguro que podamos iniciar.");
        }

        var uninstall = Environment.ExpandEnvironmentVariables(
            app.UninstallString.Trim());

        var productCode = Regex.Match(
            uninstall,
            @"\{[0-9A-Fa-f\-]{36}\}");

        if (app.WindowsInstaller && productCode.Success)
        {
            Process.Start(new ProcessStartInfo(
                "msiexec.exe",
                $"/x {productCode.Value}")
            {
                UseShellExecute = true
            });
            return;
        }

        if (!TrySplitCommand(uninstall, out var executable, out var arguments))
        {
            throw new InvalidOperationException(
                "No se pudo interpretar el desinstalador registrado. " +
                "Usa Configuración de Windows para quitar esta aplicación.");
        }

        if (Path.IsPathRooted(executable) && !File.Exists(executable))
        {
            throw new InvalidOperationException(
                "El desinstalador registrado ya no existe.");
        }

        Process.Start(new ProcessStartInfo(executable, arguments)
        {
            UseShellExecute = true,
            WorkingDirectory =
                Path.GetDirectoryName(executable) is { Length: > 0 } directory &&
                Directory.Exists(directory)
                    ? directory
                    : Environment.CurrentDirectory
        });
    }

    public void OpenInstallLocation(InstalledApp app)
    {
        var location = Environment.ExpandEnvironmentVariables(
            app.InstallLocation ?? "");

        if (string.IsNullOrWhiteSpace(location) ||
            !Directory.Exists(location))
        {
            throw new InvalidOperationException(
                "Windows no tiene una carpeta de instalación válida registrada para esta aplicación.");
        }

        Process.Start(new ProcessStartInfo(location)
        {
            UseShellExecute = true
        });
    }

    private static void ReadHive(
        List<InstalledApp> apps,
        RegistryHive hive,
        RegistryView view,
        string hiveLabel)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var uninstall = baseKey.OpenSubKey(
                UninstallRegistryPath,
                writable: false);

            if (uninstall is null)
                return;

            foreach (var subKeyName in uninstall.GetSubKeyNames())
            {
                try
                {
                    using var key = uninstall.OpenSubKey(subKeyName, false);
                    if (key is null)
                        continue;

                    var displayName =
                        Convert.ToString(key.GetValue("DisplayName")) ?? "";

                    if (string.IsNullOrWhiteSpace(displayName))
                        continue;

                    var systemComponent =
                        Convert.ToInt32(key.GetValue("SystemComponent", 0)) == 1;

                    var releaseType =
                        Convert.ToString(key.GetValue("ReleaseType")) ?? "";

                    if (systemComponent ||
                        releaseType.Contains(
                            "Update",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    apps.Add(new InstalledApp
                    {
                        DisplayName = displayName.Trim(),
                        DisplayVersion =
                            Convert.ToString(key.GetValue("DisplayVersion")) ?? "",
                        Publisher =
                            Convert.ToString(key.GetValue("Publisher")) ?? "",
                        InstallLocation =
                            Convert.ToString(key.GetValue("InstallLocation")) ?? "",
                        UninstallString =
                            Convert.ToString(key.GetValue("UninstallString")) ?? "",
                        QuietUninstallString =
                            Convert.ToString(key.GetValue("QuietUninstallString")) ?? "",
                        RegistryHive = hiveLabel,
                        RegistryPath =
                            $@"{UninstallRegistryPath}\{subKeyName}",
                        IsSystemComponent = systemComponent,
                        WindowsInstaller =
                            Convert.ToInt32(key.GetValue("WindowsInstaller", 0)) == 1
                    });
                }
                catch
                {
                    // Una entrada dañada no debe impedir mostrar el resto.
                }
            }
        }
        catch
        {
            // Una vista de Registro inaccesible no debe romper el inventario.
        }
    }

    private static bool TrySplitCommand(
        string command,
        out string executable,
        out string arguments)
    {
        executable = "";
        arguments = "";

        if (string.IsNullOrWhiteSpace(command))
            return false;

        if (command.StartsWith('"'))
        {
            var end = command.IndexOf('"', 1);
            if (end <= 1)
                return false;

            executable = command[1..end];
            arguments = command[(end + 1)..].Trim();
            return true;
        }

        var match = Regex.Match(
            command,
            @"^(?<exe>.+?\.exe)(?:\s+(?<args>.*))?$",
            RegexOptions.IgnoreCase);

        if (!match.Success)
            return false;

        executable = match.Groups["exe"].Value.Trim();
        arguments = match.Groups["args"].Value.Trim();
        return true;
    }
}
