using System.Diagnostics;
using Microsoft.Win32;

namespace Windows11Optimizer.Services;

public sealed class AgentIntegrationService
{
    private const string RunValueName = "Windows11OptimizerAgent";

    public string AgentPath =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Windows11Optimizer.Agent.exe");

    public bool IsAvailable => File.Exists(AgentPath);

    public void EnsureStartup()
    {
        if (!IsAvailable)
            return;

        using var key = Registry.CurrentUser.CreateSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            writable: true);

        if (key is null)
            return;

        key.SetValue(
            RunValueName,
            $""{AgentPath}" --agent",
            RegistryValueKind.String);
    }

    public void OpenMiniFocus()
    {
        StartAgent("--focus");
    }

    public void OpenSettings()
    {
        StartAgent("--settings");
    }

    public void StartAgent()
    {
        StartAgent("--agent");
    }

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
