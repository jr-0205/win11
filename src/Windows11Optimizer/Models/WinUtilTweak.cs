namespace Windows11Optimizer.Models;

public sealed class WinUtilTweak
{
    public string Id { get; init; } = "";
    public string Content { get; init; } = "";
    public string Description { get; init; } = "";
    public string Category { get; init; } = "";
    public string Panel { get; init; } = "";
    public string Risk { get; init; } = "Revisión";
    public string Presets { get; init; } = "";
    public int RegistryActions { get; init; }
    public int ServiceActions { get; init; }
    public int ScriptActions { get; init; }
    public int ScheduledTaskActions { get; init; }

    public string Actions =>
        $"Reg:{RegistryActions} · Svc:{ServiceActions} · Script:{ScriptActions} · Task:{ScheduledTaskActions}";
}

public sealed class WinUtilCatalogResult
{
    public string Version { get; init; } = "";
    public string Commit { get; init; } = "";
    public DateTime LoadedAt { get; init; }
    public bool FromCache { get; init; }
    public IReadOnlyList<WinUtilTweak> Tweaks { get; init; } = Array.Empty<WinUtilTweak>();
}
