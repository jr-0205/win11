namespace Windows11Optimizer.Models;

public sealed class WinUtilTweak
{
    public string Id { get; init; } = "";
    public string Content { get; init; } = "";
    public string Description { get; init; } = "";
    public string Category { get; init; } = "";
    public string Panel { get; init; } = "";

    // Datos originales de WinUtil conservados para auditoría/diagnóstico.
    public string OriginalContent { get; init; } = "";
    public string OriginalDescription { get; init; } = "";
    public string OriginalCategory { get; init; } = "";
    public string Risk { get; init; } = "Revisión";
    public string Presets { get; init; } = "";
    public int RegistryActions { get; init; }
    public int ServiceActions { get; init; }
    public int ScriptActions { get; init; }
    public int ScheduledTaskActions { get; init; }

    public string Actions =>
        $"Registro: {RegistryActions} · Servicios: {ServiceActions} · Scripts: {ScriptActions} · Tareas: {ScheduledTaskActions}";
}

public sealed class WinUtilCatalogResult
{
    public string Version { get; init; } = "";
    public string Commit { get; init; } = "";
    public DateTime LoadedAt { get; init; }
    public bool FromCache { get; init; }
    public IReadOnlyList<WinUtilTweak> Tweaks { get; init; } = Array.Empty<WinUtilTweak>();
}
