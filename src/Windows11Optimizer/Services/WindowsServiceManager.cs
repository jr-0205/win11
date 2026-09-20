using System.Management;
using System.ServiceProcess;
using Windows11Optimizer.Models;

namespace Windows11Optimizer.Services;

public sealed class WindowsServiceManager
{
    public ManagedService? GetInfo(string name, string group, string policy)
    {
        var escaped = name.Replace("'", "''");
        using var searcher = new ManagementObjectSearcher(
            $"SELECT Name, DisplayName, State, StartMode FROM Win32_Service WHERE Name='{escaped}'");
        using var results = searcher.Get();
        var row = results.Cast<ManagementObject>().FirstOrDefault();
        if (row is null) return null;

        return new ManagedService
        {
            Group = group,
            Name = Convert.ToString(row["Name"]) ?? name,
            DisplayName = Convert.ToString(row["DisplayName"]) ?? name,
            State = Convert.ToString(row["State"]) ?? "Unknown",
            StartMode = Convert.ToString(row["StartMode"]) ?? "Unknown",
            Policy = policy
        };
    }

    public Task SetStartupManualAsync(string name) =>
        ConfigureStartAsync(name, "demand");

    public Task SetStartupAutomaticAsync(string name) =>
        ConfigureStartAsync(name, "auto");

    public Task SetStartupDisabledAsync(string name) =>
        ConfigureStartAsync(name, "disabled");

    public async Task StartAsync(string name)
    {
        using var controller = new ServiceController(name);
        controller.Refresh();
        if (controller.Status == ServiceControllerStatus.Running) return;

        controller.Start();
        await Task.Run(() =>
            controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(12)));
    }

    public async Task StopAsync(string name)
    {
        using var controller = new ServiceController(name);
        controller.Refresh();

        if (controller.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending)
            return;

        if (!controller.CanStop)
            throw new InvalidOperationException($"El servicio '{name}' no permite detenerse en este momento.");

        controller.Stop();
        await Task.Run(() =>
            controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(12)));
    }

    public async Task SetManualAndStartAsync(string name)
    {
        await SetStartupManualAsync(name);
        await StartAsync(name);
    }

    public async Task StopAndDisableAsync(string name)
    {
        try
        {
            using var controller = new ServiceController(name);
            controller.Refresh();
            if (controller.Status is not ServiceControllerStatus.Stopped and not ServiceControllerStatus.StopPending)
            {
                controller.Stop();
                await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(12)));
            }
        }
        catch (InvalidOperationException)
        {
            return;
        }

        await SetStartupDisabledAsync(name);
    }

    public async Task RestoreAsync(string name, string startMode, bool shouldRun)
    {
        var scMode = startMode.ToLowerInvariant() switch
        {
            "auto" or "automatic" => "auto",
            "disabled" => "disabled",
            _ => "demand"
        };

        await ConfigureStartAsync(name, scMode);

        try
        {
            using var controller = new ServiceController(name);
            controller.Refresh();

            if (shouldRun && controller.Status != ServiceControllerStatus.Running)
            {
                controller.Start();
                await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(12)));
            }
            else if (!shouldRun && controller.Status is not ServiceControllerStatus.Stopped and not ServiceControllerStatus.StopPending)
            {
                controller.Stop();
                await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(12)));
            }
        }
        catch (InvalidOperationException) { }
    }

    private static async Task ConfigureStartAsync(string name, string mode)
    {
        var result = await CommandRunner.RunAsync("sc.exe", ["config", name, "start=", mode]);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"sc.exe no pudo configurar '{name}': {result.Error}{result.Output}");
    }
}
