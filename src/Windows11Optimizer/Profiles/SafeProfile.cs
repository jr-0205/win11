namespace Windows11Optimizer.Profiles;

public static class SafeProfile
{
    // Servicios necesarios para una sesión VMware típica.
    public static readonly string[] VmwareCoreServices =
    [
        "VMAuthdService",
        "VMnetDHCP",
        "VMware NAT Service"
    ];

    // Solo es necesario para pasar dispositivos USB físicos a una VM.
    public static readonly string[] VmwareUsbServices =
    [
        "VMUSBArbService"
    ];

    // Solo es necesario si el usuario configuró VMs para arrancar automáticamente.
    public static readonly string[] VmwareAutostartServices =
    [
        "VMwareAutostartService"
    ];

    public static readonly string[] VmwareServices =
        VmwareCoreServices
            .Concat(VmwareUsbServices)
            .Concat(VmwareAutostartServices)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

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

    public static readonly string[] AcerManagedTasks =
    [
        "\\DelayStartCareCenter2",
        "\\DelayStartDeviceInfo2"
    ];

    public static IEnumerable<string> AllModifiedServices =>
        VmwareServices
            .Concat(AcerOnDemandServices)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<string> UserManageableServices => AllModifiedServices;

    public static bool IsUserManageableService(string name) =>
        UserManageableServices.Contains(name, StringComparer.OrdinalIgnoreCase);

    public static bool IsProtectedService(string name) =>
        AcerProtectedServices.Contains(name, StringComparer.OrdinalIgnoreCase);

    public static bool IsVmwareCoreService(string name) =>
        VmwareCoreServices.Contains(name, StringComparer.OrdinalIgnoreCase);

    public static bool IsVmwareUsbService(string name) =>
        VmwareUsbServices.Contains(name, StringComparer.OrdinalIgnoreCase);

    public static bool IsVmwareAutostartService(string name) =>
        VmwareAutostartServices.Contains(name, StringComparer.OrdinalIgnoreCase);

    // Solo los servicios aprobados explícitamente pueden quedar Disabled.
    public static bool CanDisableService(string name) =>
        IsUserManageableService(name);
}
