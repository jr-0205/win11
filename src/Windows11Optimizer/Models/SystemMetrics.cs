namespace Windows11Optimizer.Models;

public sealed class SystemMetrics
{
    public double CpuPercent { get; init; }
    public double RamPercent { get; init; }
    public double UsedRamGb { get; init; }
    public double TotalRamGb { get; init; }
    public int ProcessCount { get; init; }
}
