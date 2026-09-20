using System.Xml.Linq;

namespace Windows11Optimizer.Services;

public sealed class ScheduledTaskManager
{
    public async Task<bool?> IsEnabledAsync(string taskName)
    {
        var result = await CommandRunner.RunAsync("schtasks.exe", ["/Query", "/TN", taskName, "/XML"]);
        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.Output)) return null;

        try
        {
            var doc = XDocument.Parse(result.Output);
            XNamespace ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
            var enabled = doc.Descendants(ns + "Enabled").FirstOrDefault()?.Value;
            return !string.Equals(enabled, "false", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return null;
        }
    }

    public Task DisableAsync(string taskName) => ChangeAsync(taskName, "/Disable");
    public Task EnableAsync(string taskName) => ChangeAsync(taskName, "/Enable");

    private static async Task ChangeAsync(string taskName, string option)
    {
        var result = await CommandRunner.RunAsync("schtasks.exe", ["/Change", "/TN", taskName, option]);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"No se pudo modificar la tarea '{taskName}': {result.Error}{result.Output}");
    }
}
