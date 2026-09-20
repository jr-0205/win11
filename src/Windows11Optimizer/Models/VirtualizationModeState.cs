namespace Windows11Optimizer.Models;

public sealed class VirtualizationModeState
{
    public string HypervisorLaunchType { get; init; } = "Auto";
    public bool IsExplicitlyConfigured { get; init; }
    public bool HypervisorPresentNow { get; init; }
    public bool PendingRestart { get; init; }

    public int? VbsStatus { get; init; }
    public bool? MemoryIntegrityRunning { get; init; }

    public bool IsConfiguredForVmware =>
        HypervisorLaunchType.Equals("Off", StringComparison.OrdinalIgnoreCase);

    public bool IsConfiguredForNormal => !IsConfiguredForVmware;

    public string ConfiguredModeLabel =>
        IsConfiguredForVmware ? "Modo VMware" : "Modo normal";

    public string CurrentHypervisorLabel =>
        HypervisorPresentNow
            ? "Hipervisor de Windows activo"
            : "Hipervisor de Windows no activo";

    public string VbsLabel => VbsStatus switch
    {
        2 => "VBS en ejecución",
        1 => "VBS configurado, no activo",
        0 => "VBS no habilitado",
        _ => "VBS no disponible"
    };

    public string MemoryIntegrityLabel => MemoryIntegrityRunning switch
    {
        true => "Integridad de memoria activa",
        false => "Integridad de memoria no activa",
        null => "Integridad de memoria no disponible"
    };

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
