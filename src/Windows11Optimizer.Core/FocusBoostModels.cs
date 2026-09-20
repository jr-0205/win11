using System.Diagnostics;

namespace Windows11Optimizer.Core;

public sealed class FocusProcessInfo
{
    public int Pid { get; init; }
    public string Name { get; init; } = "";
    public string WindowTitle { get; init; } = "";
    public double WorkingSetMb { get; init; }
    public string Priority { get; init; } = "";
    public bool IsAllowedTarget { get; init; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(WindowTitle)
            ? Name
            : $"{Name} — {WindowTitle}";
}

public sealed class FocusPriorityChange
{
    public int Pid { get; set; }
    public string ProcessName { get; set; } = "";
    public DateTime StartTimeUtc { get; set; }
    public string OriginalPriority { get; set; } = ProcessPriorityClass.Normal.ToString();
}

public sealed class FocusBoostSession
{
    public DateTime StartedAt { get; set; }
    public int TargetPid { get; set; }
    public string TargetName { get; set; } = "";
    public DateTime TargetStartTimeUtc { get; set; }
    public string TargetOriginalPriority { get; set; } = ProcessPriorityClass.Normal.ToString();
    public bool TargetPriorityChanged { get; set; }
    public List<FocusPriorityChange> BackgroundChanges { get; set; } = [];
}

public sealed class FocusBoostStatus
{
    public bool IsActive { get; init; }
    public string TargetName { get; init; } = "";
    public int TargetPid { get; init; }
    public DateTime? StartedAt { get; init; }

    public string DisplayText =>
        IsActive
            ? $"Activo para {TargetName} (PID {TargetPid})"
            : "Focus Boost inactivo";
}
