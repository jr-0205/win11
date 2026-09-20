namespace Windows11Optimizer.Models;

public sealed class AutorunsEntry
{
    public string Location { get; init; } = "";
    public string Entry { get; init; } = "";
    public string Enabled { get; init; } = "";
    public string Category { get; init; } = "";
    public string Description { get; init; } = "";
    public string Publisher { get; init; } = "";
    public string ImagePath { get; init; } = "";
    public string LaunchString { get; init; } = "";

    public bool IsMissing =>
        ImagePath.Contains("File not found", StringComparison.OrdinalIgnoreCase) ||
        LaunchString.Contains("File not found", StringComparison.OrdinalIgnoreCase);

    public string StatusDisplay =>
        IsMissing
            ? "Archivo no encontrado"
            : string.Equals(Enabled, "Enabled", StringComparison.OrdinalIgnoreCase)
                ? "Activo"
                : string.IsNullOrWhiteSpace(Enabled)
                    ? "Detectado"
                    : Enabled;

    public string NameDisplay =>
        string.IsNullOrWhiteSpace(Entry) ? "(Sin nombre)" : Entry;

    public string SourceDisplay =>
        string.IsNullOrWhiteSpace(Category) ? Location : Category;
}

public sealed class AutorunsAnalysisResult
{
    public bool Available { get; init; }
    public string ToolPath { get; init; } = "";
    public string Message { get; init; } = "";
    public IReadOnlyList<AutorunsEntry> Entries { get; init; } = Array.Empty<AutorunsEntry>();

    public int ThirdPartyCount => Entries.Count;
    public int MissingCount => Entries.Count(x => x.IsMissing);
}

public sealed class OptimizationSnapshot
{
    public DateTime CapturedAt { get; init; }
    public double RamPercent { get; init; }
    public double UsedRamGb { get; init; }
    public double TotalRamGb { get; init; }
    public int ProcessCount { get; init; }
    public int StartupEntryCount { get; init; }
    public int OrphanedStartupCount { get; init; }
    public int AutorunsThirdPartyCount { get; init; }
    public int AutorunsMissingCount { get; init; }
    public bool AutorunsAvailable { get; init; }
}
