using System.Text.Json;
using Microsoft.Win32;

namespace Windows11Optimizer.Agent;

public sealed class AgentSettings
{
    public string MiniFocusHotkey { get; set; } = "Ctrl+Alt+Space";
    public string FullUiHotkey { get; set; } = "Ctrl+Alt+O";
    public bool StartWithWindows { get; set; } = true;
}

public sealed class AgentSettingsService
{
    private const string RunValueName = "Windows11OptimizerAgent";

    private readonly string _directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Windows11Optimizer");

    private string SettingsFile => Path.Combine(_directory, "agent-settings.json");

    public AgentSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFile))
                return new AgentSettings();

            return JsonSerializer.Deserialize<AgentSettings>(
                       File.ReadAllText(SettingsFile))
                   ?? new AgentSettings();
        }
        catch
        {
            return new AgentSettings();
        }
    }

    public void Save(AgentSettings settings)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            SettingsFile,
            JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions { WriteIndented = true }));

        UpdateStartup(settings.StartWithWindows);
    }

    public void UpdateStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            writable: true);

        if (key is null)
            return;

        if (!enabled)
        {
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
            return;
        }

        var exe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe))
            return;

        key.SetValue(
            RunValueName,
            $""{exe}" --agent",
            RegistryValueKind.String);
    }
}
