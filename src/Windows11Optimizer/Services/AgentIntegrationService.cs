using System.Diagnostics;

namespace Windows11Optimizer.Services;

public sealed class AgentIntegrationService
{
    public string AgentPath =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Windows11Optimizer.Agent.exe");

    public bool IsAvailable => File.Exists(AgentPath);

    public void OpenMiniFocus() => StartAgent("--focus");

    public void OpenSettings() => StartAgent("--settings");

    public void StartAgent() => StartAgent("--agent");

    private void StartAgent(string arguments)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(
                "No se encontró Windows11Optimizer.Agent.exe junto a la aplicación.");
        }

        Process.Start(new ProcessStartInfo(
            AgentPath,
            arguments)
        {
            UseShellExecute = true,
            WorkingDirectory = AppContext.BaseDirectory
        });
    }
}
