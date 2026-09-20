using System.Diagnostics;
using System.Xml.Linq;
using Windows11Optimizer.Models;

namespace Windows11Optimizer.Services;

public sealed class AutorunsService
{
    public const string OfficialPage =
        "https://learn.microsoft.com/sysinternals/downloads/autoruns";

    public string? FindTool()
    {
        var names = new[]
        {
            "Autorunsc64.exe",
            "Autorunsc.exe"
        };

        var appDirectory = AppContext.BaseDirectory;

        var explicitCandidates = new List<string>
        {
            Path.Combine(appDirectory, "Tools", "Autoruns", "Autorunsc64.exe"),
            Path.Combine(appDirectory, "Tools", "Autoruns", "Autorunsc.exe")
        };

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            explicitCandidates.Add(Path.Combine(programFiles, "Sysinternals", "Autorunsc64.exe"));
            explicitCandidates.Add(Path.Combine(programFiles, "Sysinternals", "Autorunsc.exe"));
            explicitCandidates.Add(Path.Combine(programFiles, "SysinternalsSuite", "Autorunsc64.exe"));
            explicitCandidates.Add(Path.Combine(programFiles, "SysinternalsSuite", "Autorunsc.exe"));
        }

        foreach (var candidate in explicitCandidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var folder in pathValue.Split(
                     Path.PathSeparator,
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var name in names)
            {
                try
                {
                    var candidate = Path.Combine(folder.Trim('"'), name);
                    if (File.Exists(candidate))
                        return candidate;
                }
                catch
                {
                    // Una ruta inválida del PATH no debe bloquear la detección.
                }
            }
        }

        return null;
    }

    public string? FindGui()
    {
        var commandLine = FindTool();
        if (commandLine is null)
            return null;

        var directory = Path.GetDirectoryName(commandLine);
        if (string.IsNullOrWhiteSpace(directory))
            return null;

        foreach (var name in new[] { "Autoruns64.exe", "Autoruns.exe" })
        {
            var candidate = Path.Combine(directory, name);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    public async Task<AutorunsAnalysisResult> AnalyzeAsync()
    {
        var tool = FindTool();
        if (tool is null)
        {
            return new AutorunsAnalysisResult
            {
                Available = false,
                Message = "Autoruns no está instalado o no se encontró Autorunsc."
            };
        }

        var psi = new ProcessStartInfo
        {
            FileName = tool,
            Arguments = "-a * -x -m -s",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = psi };

        try
        {
            if (!process.Start())
                throw new InvalidOperationException("No se pudo iniciar Autorunsc.");

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(35));

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }

                return new AutorunsAnalysisResult
                {
                    Available = true,
                    ToolPath = tool,
                    Message =
                        "Autorunsc no terminó a tiempo. Si es la primera vez que usas Autoruns, " +
                        "abre Autoruns una vez y acepta sus términos oficiales; después vuelve a analizar."
                };
            }

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
            {
                return new AutorunsAnalysisResult
                {
                    Available = true,
                    ToolPath = tool,
                    Message = string.IsNullOrWhiteSpace(error)
                        ? $"Autorunsc terminó con código {process.ExitCode}."
                        : error.Trim()
                };
            }

            var entries = ParseXml(output);

            return new AutorunsAnalysisResult
            {
                Available = true,
                ToolPath = tool,
                Entries = entries,
                Message = entries.Count == 0
                    ? "Autoruns no devolvió entradas de terceros."
                    : $"Autoruns detectó {entries.Count} entradas de terceros."
            };
        }
        catch (Exception ex)
        {
            return new AutorunsAnalysisResult
            {
                Available = true,
                ToolPath = tool,
                Message = ex.Message
            };
        }
    }

    private static IReadOnlyList<AutorunsEntry> ParseXml(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return [];

        var xmlStart = output.IndexOf("<?xml", StringComparison.OrdinalIgnoreCase);
        if (xmlStart < 0)
            xmlStart = output.IndexOf("<autoruns", StringComparison.OrdinalIgnoreCase);

        if (xmlStart < 0)
            return [];

        var xml = output[xmlStart..];
        var document = XDocument.Parse(xml, LoadOptions.None);

        var rows = new List<AutorunsEntry>();

        foreach (var item in document.Descendants()
                     .Where(x => x.Name.LocalName.Equals("item", StringComparison.OrdinalIgnoreCase)))
        {
            var entry = Value(item, "entry", "name");
            var imagePath = Value(item, "imagepath", "image path");
            var launchString = Value(item, "launchstring", "launch string");
            var location = Value(item, "location", "entrylocation", "entry location");

            if (string.IsNullOrWhiteSpace(entry) &&
                string.IsNullOrWhiteSpace(imagePath) &&
                string.IsNullOrWhiteSpace(launchString))
            {
                continue;
            }

            rows.Add(new AutorunsEntry
            {
                Location = location,
                Entry = entry,
                Enabled = Value(item, "enabled"),
                Category = Value(item, "category"),
                Description = Value(item, "description"),
                Publisher = Value(item, "publisher"),
                ImagePath = imagePath,
                LaunchString = launchString
            });
        }

        return rows
            .OrderByDescending(x => x.IsMissing)
            .ThenBy(x => x.NameDisplay, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static string Value(XElement item, params string[] names)
    {
        foreach (var element in item.Elements())
        {
            var local = element.Name.LocalName
                .Replace("_", "", StringComparison.Ordinal)
                .Replace(" ", "", StringComparison.Ordinal);

            foreach (var name in names)
            {
                var normalized = name
                    .Replace("_", "", StringComparison.Ordinal)
                    .Replace(" ", "", StringComparison.Ordinal);

                if (local.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                    return element.Value?.Trim() ?? "";
            }
        }

        return "";
    }
}

public sealed class SmartAnalysisService
{
    private readonly SystemMetricsService _metrics;
    private readonly StartupInventoryService _startup;
    private readonly AutorunsService _autoruns;

    public SmartAnalysisService(
        SystemMetricsService metrics,
        StartupInventoryService startup,
        AutorunsService autoruns)
    {
        _metrics = metrics;
        _startup = startup;
        _autoruns = autoruns;
    }

    public async Task<OptimizationSnapshot> CaptureAsync(bool includeAutoruns)
    {
        var metricsTask = _metrics.GetAsync();
        var startupTask = Task.Run(() => _startup.GetEntries());

        AutorunsAnalysisResult? autoruns = null;
        if (includeAutoruns)
            autoruns = await _autoruns.AnalyzeAsync();

        var metrics = await metricsTask;
        var startup = await startupTask;

        return new OptimizationSnapshot
        {
            CapturedAt = DateTime.Now,
            RamPercent = metrics.RamPercent,
            UsedRamGb = metrics.UsedRamGb,
            TotalRamGb = metrics.TotalRamGb,
            ProcessCount = metrics.ProcessCount,
            StartupEntryCount = startup.Count,
            OrphanedStartupCount = startup.Count(x => x.IsOrphaned),
            AutorunsThirdPartyCount = autoruns?.ThirdPartyCount ?? 0,
            AutorunsMissingCount = autoruns?.MissingCount ?? 0,
            AutorunsAvailable = autoruns?.Available ?? false
        };
    }
}
