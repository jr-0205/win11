namespace Windows11Optimizer.Profiles;

public static class SafeProfile
{
    public static readonly string[] VmwareCoreServices =
    [
        "VMAuthdService",
        "VMnetDHCP",
        "VMware NAT Service"
    ];

    public static readonly string[] VmwareOptionalServices =
    [
        "VMUSBArbService",
        "VMwareAutostartService"
    ];

    public static readonly string[] VmwareServices =
        VmwareCoreServices
            .Concat(VmwareOptionalServices)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static readonly string[] DockerOnDemandServices =
    [
        "com.docker.service"
    ];

    // Componentes de Windows usados por WSL2, Docker, Sandbox y Hyper-V.
    // No se fuerzan a Manual si ya tienen una configuración válida.
    // Solo se reparan cuando aparecen como Disabled.
    public static readonly string[] WindowsVirtualizationRepairServices =
    [
        "WslService",
        "LxssManager",
        "vmcompute",
        "hns",
        "HvHost",
        "vmms",
        "CmService"
    ];

    public static readonly string[] AcerOnDemandServices =
    [
        "AcerCCAgentSvis",
        "AcerDIAgentSvis",
        "AcerEZSvc"
    ];

    public static readonly string[] AcerProtectedServices =
    [
        "AcerDeviceEnablingServiceV2",
        "AcerQAAgentSvis",
        "ASMSvc",
        "AcerServiceSvc"
    ];

    // Se analizan, pero la optimización inteligente no las deshabilita.
    public static readonly string[] AcerManagedTasks =
    [
        "\\DelayStartCareCenter2",
        "\\DelayStartDeviceInfo2"
    ];

    public static IEnumerable<string> IntelligentOnDemandServices =>
        AcerOnDemandServices
            .Concat(VmwareServices)
            .Concat(DockerOnDemandServices)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<string> AllModifiedServices =>
        IntelligentOnDemandServices
            .Concat(WindowsVirtualizationRepairServices)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<string> UserManageableServices =>
        IntelligentOnDemandServices;

    public static bool IsUserManageableService(string name) =>
        UserManageableServices.Contains(name, StringComparer.OrdinalIgnoreCase);

    public static bool IsProtectedService(string name) =>
        AcerProtectedServices.Contains(name, StringComparer.OrdinalIgnoreCase);

    public static bool IsVmwareCoreService(string name) =>
        VmwareCoreServices.Contains(name, StringComparer.OrdinalIgnoreCase);

    // La experiencia simplificada no deja servicios aprobados en Disabled.
    public static bool CanDisableService(string name) => false;
}
