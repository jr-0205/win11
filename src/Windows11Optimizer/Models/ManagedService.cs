namespace Windows11Optimizer.Models;

public sealed class ManagedService
{
    public string Group { get; init; } = "";
    public string Name { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string State { get; init; } = "Unknown";
    public string StartMode { get; init; } = "Unknown";
    public string Policy { get; init; } = "";
}
