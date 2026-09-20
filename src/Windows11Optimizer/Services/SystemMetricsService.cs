using System.Diagnostics;
using System.Management;
using Windows11Optimizer.Models;

namespace Windows11Optimizer.Services;

public sealed class SystemMetricsService
{
    public Task<SystemMetrics> GetAsync() => Task.Run(() =>
    {
        double cpu = 0;
        double totalGb = 0;
        double freeGb = 0;

        try
        {
            using var cpuSearcher = new ManagementObjectSearcher(
                "SELECT PercentProcessorTime FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name='_Total'");
            using var cpuResults = cpuSearcher.Get();
            var row = cpuResults.Cast<ManagementObject>().FirstOrDefault();
            if (row?["PercentProcessorTime"] is not null)
                cpu = Convert.ToDouble(row["PercentProcessorTime"]);
        }
        catch { }

        try
        {
            using var ramSearcher = new ManagementObjectSearcher(
                "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            using var ramResults = ramSearcher.Get();
            var row = ramResults.Cast<ManagementObject>().FirstOrDefault();
            if (row is not null)
            {
                totalGb = Convert.ToDouble(row["TotalVisibleMemorySize"]) / 1024d / 1024d;
                freeGb = Convert.ToDouble(row["FreePhysicalMemory"]) / 1024d / 1024d;
            }
        }
        catch { }

        var usedGb = Math.Max(0, totalGb - freeGb);
        var ramPct = totalGb > 0 ? usedGb / totalGb * 100d : 0;

        return new SystemMetrics
        {
            CpuPercent = cpu,
            RamPercent = ramPct,
            UsedRamGb = usedGb,
            TotalRamGb = totalGb,
            ProcessCount = Process.GetProcesses().Length
        };
    });
}
