namespace Windows11Optimizer.Models;

public sealed class VirtualizationModeState
{
    public string HypervisorLaunchType { get; init; } = "Auto";
    public bool IsExplicitlyConfigured { get; init; }
    public bool HypervisorPresentNow { get; init; }
    public string BootModeLabel =>
        HypervisorLaunchType.Equals("Off", StringComparison.OrdinalIgnoreCase)
            ? "VMware directo / hipervisor Windows OFF"
            : "Normal / hipervisor Windows AUTO";
}

public sealed class VirtualizationModeBackup
{
    public DateTime CreatedAt { get; set; }
    public bool HadExplicitValue { get; set; }
    public string OriginalValue { get; set; } = "Auto";
}
