namespace Windows11Optimizer.Profiles;

public static class SafeProfile
{
    public static readonly string[] VmwareServices =
    [
        "VMnetDHCP",
        "VMware NAT Service"
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

    public static readonly string[] AcerManagedTasks =
    [
        "\\DelayStartCareCenter2",
        "\\DelayStartDeviceInfo2"
    ];

    public static IEnumerable<string> AllModifiedServices =>
        VmwareServices.Concat(AcerOnDemandServices).Distinct(StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<string> UserManageableServices => AllModifiedServices;

    public static bool IsUserManageableService(string name) =>
        UserManageableServices.Contains(name, StringComparer.OrdinalIgnoreCase);

    public static bool IsProtectedService(string name) =>
        AcerProtectedServices.Contains(name, StringComparer.OrdinalIgnoreCase);

    // Solo los servicios bajo demanda aprobados pueden quedar Disabled.
    public static bool CanDisableService(string name) =>
        IsUserManageableService(name);
}
