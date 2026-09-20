namespace Windows11Optimizer.Models;

public sealed class StartupEntry
{
    public string Name { get; init; } = "";
    public string Command { get; init; } = "";
    public string Source { get; init; } = "";

    public string Kind { get; init; } = "";
    public string RegistryHive { get; init; } = "";
    public string RegistryPath { get; init; } = "";
    public string RegistryValueName { get; init; } = "";
    public string StartupFilePath { get; init; } = "";

    public string TargetPath { get; init; } = "";
    public bool TargetCanBeVerified { get; init; }
    public bool TargetExists { get; init; }
    public bool IsOrphaned => TargetCanBeVerified && !TargetExists;

    public bool CanRemoveSafely =>
        IsOrphaned &&
        Kind.Equals("Registry", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(RegistryPath);

    public string StatusDisplay => IsOrphaned
        ? "Huérfana: programa no encontrado"
        : TargetCanBeVerified
            ? "Programa encontrado"
            : Kind.Equals("StartupFolder", StringComparison.OrdinalIgnoreCase)
                ? "Entrada de carpeta Inicio"
                : "No se pudo verificar";

    public string TargetDisplay =>
        string.IsNullOrWhiteSpace(TargetPath)
            ? Command
            : TargetPath;
}

public sealed class StartupEntryBackup
{
    public DateTime RemovedAt { get; set; }
    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public string RegistryHive { get; set; } = "";
    public string RegistryPath { get; set; } = "";
    public string RegistryValueName { get; set; } = "";
    public bool Restored { get; set; }
}
