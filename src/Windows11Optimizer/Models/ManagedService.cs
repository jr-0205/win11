namespace Windows11Optimizer.Models;

public sealed class ManagedService
{
    public string Group { get; init; } = "";
    public string Name { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string State { get; init; } = "Unknown";
    public string StartMode { get; init; } = "Unknown";
    public string Policy { get; init; } = "";

    public string StateDisplay => State.ToLowerInvariant() switch
    {
        "running" => "En ejecución",
        "stopped" => "Detenido",
        "start pending" or "startpending" => "Iniciando…",
        "stop pending" or "stoppending" => "Deteniendo…",
        "paused" => "Pausado",
        _ => State
    };

    public string StartModeDisplay => StartMode.ToLowerInvariant() switch
    {
        "auto" or "automatic" => "Inicia con Windows",
        "manual" => "Solo cuando se necesita",
        "disabled" => "No inicia automáticamente",
        _ => StartMode
    };

    public string PolicyDisplay => Policy switch
    {
        "Bajo demanda" => "Seguro para usar solo cuando se necesite",
        "Protegido / conservar" => "Protegido: recomendamos no modificarlo",
        _ => Policy
    };
}
