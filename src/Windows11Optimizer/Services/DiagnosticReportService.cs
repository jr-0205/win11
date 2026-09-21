using System.Diagnostics;
using System.Management;
using System.Text;
using Windows11Optimizer.Profiles;

namespace Windows11Optimizer.Services;

public sealed class DiagnosticReportService
{
    private readonly SystemMetricsService _metrics;
    private readonly WindowsServiceManager _services;
    private readonly StartupInventoryService _startup;

    public DiagnosticReportService(SystemMetricsService metrics, WindowsServiceManager services, StartupInventoryService startup)
    {
        _metrics = metrics;
        _services = services;
        _startup = startup;
    }

    public async Task<string> ExportToDesktopAsync()
    {
        var m = await _metrics.GetAsync();
        var sb = new StringBuilder();
        sb.AppendLine("WINDOWS 11 OPTIMIZER - DIAGNOSTICO");
        sb.AppendLine($"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine($"CPU actual: {m.CpuPercent:N1} %");
        sb.AppendLine($"RAM: {m.UsedRamGb:N2} / {m.TotalRamGb:N2} GB ({m.RamPercent:N1} %)");
        sb.AppendLine($"Procesos: {m.ProcessCount}");
        sb.AppendLine();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Caption, Version, BuildNumber FROM Win32_OperatingSystem");
            using var results = searcher.Get();
            var os = results.Cast<ManagementObject>().FirstOrDefault();
            if (os is not null)
                sb.AppendLine($"SO: {os["Caption"]} | Version {os["Version"]} | Build {os["BuildNumber"]}");
        }
        catch { }

        sb.AppendLine();
        sb.AppendLine("SERVICIOS ADMINISTRADOS");
        foreach (var name in SafeProfile.VmwareServices)
        {
            var info = _services.GetInfo(name, "VMware", "Bajo demanda");
            if (info is not null) sb.AppendLine($"{info.Name}: {info.State} / {info.StartMode}");
        }
        foreach (var name in SafeProfile.AcerOnDemandServices)
        {
            var info = _services.GetInfo(name, "Fabricante", "Bajo demanda");
            if (info is not null) sb.AppendLine($"{info.Name}: {info.State} / {info.StartMode}");
        }

        sb.AppendLine();
        sb.AppendLine("TOP PROCESOS POR WORKING SET");
        foreach (var p in Process.GetProcesses()
                     .OrderByDescending(p => SafeWorkingSet(p))
                     .Take(15))
        {
            sb.AppendLine($"{p.ProcessName,-28} {SafeWorkingSet(p) / 1024d / 1024d,8:N1} MB");
            p.Dispose();
        }

        sb.AppendLine();
        sb.AppendLine("INICIO REGISTRADO");
        foreach (var entry in _startup.GetEntries())
            sb.AppendLine($"{entry.Name} | {entry.Source} | {entry.Command}");

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var path = Path.Combine(desktop, $"Windows11Optimizer_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private static long SafeWorkingSet(Process process)
    {
        try { return process.WorkingSet64; }
        catch { return 0; }
    }
}
