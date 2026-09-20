namespace Windows11Optimizer.Models;

public sealed class VirtualizationModeState
{
    public string HypervisorLaunchType { get; init; } = "Auto";
    public bool IsExplicitlyConfigured { get; init; }
    public bool HypervisorPresentNow { get; init; }
    public bool PendingRestart { get; init; }

    public bool IsConfiguredForVmware =>
        HypervisorLaunchType.Equals("Off", StringComparison.OrdinalIgnoreCase);

    public bool IsConfiguredForNormal => !IsConfiguredForVmware;

    public string ConfiguredModeLabel =>
        IsConfiguredForVmware ? "Modo VMware" : "Modo normal";

    public string CurrentHypervisorLabel =>
        HypervisorPresentNow
            ? "Hipervisor de Windows activo"
            : "Hipervisor de Windows no activo";

    public string RestartLabel =>
        PendingRestart
            ? "Reinicio necesario para aplicar el cambio"
            : "No hay reinicio pendiente";
}

public sealed class VirtualizationModeBackup
{
    public DateTime CreatedAt { get; set; }
    public bool HadExplicitValue { get; set; }
    public string OriginalValue { get; set; } = "Auto";
}

public sealed class VirtualizationPendingChange
{
    public DateTime CreatedAt { get; set; }
    public DateTime BootTimeUtc { get; set; }
    public string RequestedMode { get; set; } = "Auto";
}
