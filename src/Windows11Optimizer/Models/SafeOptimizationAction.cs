namespace Windows11Optimizer.Models;

public sealed class SafeActionDefinition
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Category { get; init; } = "";
    public string Description { get; init; } = "";
    public string Impact { get; init; } = "";
    public string Risk { get; init; } = "Bajo";
    public string Revert { get; init; } = "";
    public bool CanApply { get; init; }
    public bool AiSelectable { get; init; }
}

public sealed class AiRecommendation
{
    public string ActionId { get; init; } = "";
    public string Reason { get; init; } = "";
    public string Confidence { get; init; } = "medium";
}

public sealed class AiExaminationResult
{
    public string Summary { get; init; } = "";
    public IReadOnlyList<AiRecommendation> Recommendations { get; init; } =
        Array.Empty<AiRecommendation>();
}

public sealed class AiRecommendationRow
{
    public string ActionId { get; init; } = "";
    public string Title { get; init; } = "";
    public string Category { get; init; } = "";
    public string Reason { get; init; } = "";
    public string Confidence { get; init; } = "";
    public string Impact { get; init; } = "";
    public string Risk { get; init; } = "";
    public string Revert { get; init; } = "";
    public bool CanApply { get; init; }
}

public sealed class SystemAssessmentSnapshot
{
    public DateTime CapturedAt { get; init; }
    public double RamPercent { get; init; }
    public double UsedRamGb { get; init; }
    public double TotalRamGb { get; init; }
    public int ProcessCount { get; init; }
    public int StartupEntryCount { get; init; }
    public int OrphanedStartupCount { get; init; }
    public int InstalledAppCount { get; init; }
    public IReadOnlyList<SystemAssessmentProcess> TopProcesses { get; init; } =
        Array.Empty<SystemAssessmentProcess>();
    public IReadOnlyDictionary<string, string> SafeComponentStates { get; init; } =
        new Dictionary<string, string>();

    public object ToPrivacySafePayload() => new
    {
        capturedAt = CapturedAt,
        ramPercent = RamPercent,
        usedRamGb = UsedRamGb,
        totalRamGb = TotalRamGb,
        processCount = ProcessCount,
        startupEntryCount = StartupEntryCount,
        orphanedStartupCount = OrphanedStartupCount,
        installedAppCount = InstalledAppCount,
        topProcesses = TopProcesses,
        safeComponentStates = SafeComponentStates
    };
}

public sealed class SystemAssessmentProcess
{
    public string Name { get; init; } = "";
    public double WorkingSetMb { get; init; }
}
