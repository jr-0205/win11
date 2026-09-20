using System.Text.RegularExpressions;

namespace Windows11Optimizer.Services;

/// <summary>
/// Contrato base heredado de VMwareMode.ps1.
/// Esta clase no administra servicios, tareas, Docker, WSL ni seguridad.
/// Su única responsabilidad es traducir el valor BCD del hipervisor.
/// </summary>
public static class VirtualizationModeCore
{
    public const string NormalBcdValue = "auto";
    public const string VmwareBcdValue = "off";

    private static readonly Regex VmwareModeRegex = new(
        @"(?im)^\s*hypervisorlaunchtype\s+off\s*$",
        RegexOptions.Compiled);

    private static readonly Regex ExplicitValueRegex = new(
        @"(?im)^\s*hypervisorlaunchtype\s+(?<value>\S+)\s*$",
        RegexOptions.Compiled);

    /// <summary>
    /// Igual que VMwareMode.ps1: solo 'off' significa VMware.
    /// Cualquier otro valor o la ausencia del elemento se considera Normal.
    /// </summary>
    public static bool IsVmwareMode(string bcdOutput) =>
        VmwareModeRegex.IsMatch(bcdOutput ?? "");

    public static bool HasExplicitValue(string bcdOutput) =>
        ExplicitValueRegex.IsMatch(bcdOutput ?? "");

    public static string GetConfiguredValue(string bcdOutput) =>
        IsVmwareMode(bcdOutput) ? "Off" : "Auto";

    public static string NormalizeValue(string value) =>
        value.Equals("Off", StringComparison.OrdinalIgnoreCase)
            ? "Off"
            : "Auto";
}
