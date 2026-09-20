using System.Diagnostics;
using Windows11Optimizer.Models;
using Windows11Optimizer.Profiles;

namespace Windows11Optimizer.Services;

public sealed class SystemAssessmentService
{
    private readonly SystemMetricsService _metrics;
    private readonly StartupInventoryService _startup;
    private readonly InstalledAppService _apps;
    private readonly SafeActionEngine _actions;

    public SystemAssessmentService(
        SystemMetricsService metrics,
        StartupInventoryService startup,
        InstalledAppService apps,
        SafeActionEngine actions)
    {
        _metrics = metrics;
        _startup = startup;
        _apps = apps;
        _actions = actions;
    }

    public async Task<SystemAssessmentSnapshot> CaptureAsync()
    {
        var metrics = await _metrics.GetAsync();
        var startup = _startup.GetEntries();
        var apps = _apps.GetInstalledApps();

        var topProcesses = new List<SystemAssessmentProcess>();

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    topProcesses.Add(new SystemAssessmentProcess
                    {
                        Name = process.ProcessName,
                        WorkingSetMb =
                            process.WorkingSet64 / 1024d / 1024d
                    });
                }
                catch
                {
                    // Procesos protegidos se omiten.
                }
            }
        }

        var states = SafeActionCatalog.AiSelectable.ToDictionary(
            x => x.Id,
            x => _actions.GetState(x.Id),
            StringComparer.OrdinalIgnoreCase);

        return new SystemAssessmentSnapshot
        {
            CapturedAt = DateTime.Now,
            RamPercent = metrics.RamPercent,
            UsedRamGb = metrics.UsedRamGb,
            TotalRamGb = metrics.TotalRamGb,
            ProcessCount = metrics.ProcessCount,
            StartupEntryCount = startup.Count,
            OrphanedStartupCount = startup.Count(x => x.IsOrphaned),
            InstalledAppCount = apps.Count,
            TopProcesses = topProcesses
                .OrderByDescending(x => x.WorkingSetMb)
                .Take(12)
                .ToList(),
            SafeComponentStates = states
        };
    }
}
