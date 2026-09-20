namespace Windows11Optimizer.Models;

public sealed class InstalledApp
{
    public string DisplayName { get; init; } = "";
    public string DisplayVersion { get; init; } = "";
    public string Publisher { get; init; } = "";
    public string InstallLocation { get; init; } = "";
    public string UninstallString { get; init; } = "";
    public string QuietUninstallString { get; init; } = "";
    public string RegistryHive { get; init; } = "";
    public string RegistryPath { get; init; } = "";
    public bool IsSystemComponent { get; init; }
    public bool WindowsInstaller { get; init; }

    public bool CanUninstall =>
        !IsSystemComponent &&
        !string.IsNullOrWhiteSpace(UninstallString);

    public string VersionDisplay =>
        string.IsNullOrWhiteSpace(DisplayVersion)
            ? "—"
            : DisplayVersion;

    public string PublisherDisplay =>
        string.IsNullOrWhiteSpace(Publisher)
            ? "Editor no indicado"
            : Publisher;
}

public sealed class ApplicationExplanation
{
    public string Summary { get; init; } = "";
    public string Purpose { get; init; } = "";
    public string StartupAdvice { get; init; } = "";
    public string UninstallCaution { get; init; } = "";

    public string DisplayText =>
        $"Qué es: {Summary}{Environment.NewLine}{Environment.NewLine}" +
        $"Para qué sirve: {Purpose}{Environment.NewLine}{Environment.NewLine}" +
        $"Inicio de Windows: {StartupAdvice}{Environment.NewLine}{Environment.NewLine}" +
        $"Antes de desinstalar: {UninstallCaution}";
}
