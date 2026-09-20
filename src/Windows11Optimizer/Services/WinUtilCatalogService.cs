using System.Net;
using System.Net.Http;
using System.Text.Json;
using Windows11Optimizer.Models;

namespace Windows11Optimizer.Services;

/// <summary>
/// Integración de solo lectura con Chris Titus Tech WinUtil.
/// Nunca descarga ni ejecuta scripts PowerShell. Solo lee JSON de configuración
/// desde un commit fijado del repositorio oficial.
/// </summary>
public sealed class WinUtilCatalogService
{
    public const string Version = "26.08.19";
    public const string Commit = "086aecf4b7d165f9fd1822049435c418a48e7cba";
    public const string RepositoryUrl = "https://github.com/ChrisTitusTech/winutil";

    private static readonly Uri TweaksUri =
        new($"https://raw.githubusercontent.com/ChrisTitusTech/winutil/{Commit}/config/tweaks.json");

    private static readonly Uri PresetsUri =
        new($"https://raw.githubusercontent.com/ChrisTitusTech/winutil/{Commit}/config/preset.json");

    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;
    private readonly string _tweaksCache;
    private readonly string _presetsCache;

    public WinUtilCatalogService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Windows11Optimizer/0.2");

        _cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Windows11Optimizer",
            "winutil",
            Version);

        _tweaksCache = Path.Combine(_cacheDirectory, "tweaks.json");
        _presetsCache = Path.Combine(_cacheDirectory, "preset.json");
    }

    public async Task<WinUtilCatalogResult> LoadAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_cacheDirectory);

        string tweaksJson;
        string presetsJson;
        bool fromCache = false;

        if (!forceRefresh && File.Exists(_tweaksCache) && File.Exists(_presetsCache))
        {
            tweaksJson = await File.ReadAllTextAsync(_tweaksCache, cancellationToken);
            presetsJson = await File.ReadAllTextAsync(_presetsCache, cancellationToken);
            fromCache = true;
        }
        else
        {
            try
            {
                var tweaksTask = DownloadTextAsync(TweaksUri, cancellationToken);
                var presetsTask = DownloadTextAsync(PresetsUri, cancellationToken);

                await Task.WhenAll(tweaksTask, presetsTask);

                tweaksJson = tweaksTask.Result;
                presetsJson = presetsTask.Result;

                // WinUtil 26.08.19 contiene bloques PowerShell multilínea dentro de
                // cadenas JSON. Los saltos de línea literales no son JSON estricto,
                // así que normalizamos solo caracteres de control dentro de strings.
                using (ParseWinUtilJson(tweaksJson)) { }
                using (JsonDocument.Parse(presetsJson)) { }

                await File.WriteAllTextAsync(_tweaksCache, tweaksJson, cancellationToken);
                await File.WriteAllTextAsync(_presetsCache, presetsJson, cancellationToken);
            }
            catch when (File.Exists(_tweaksCache) && File.Exists(_presetsCache))
            {
                tweaksJson = await File.ReadAllTextAsync(_tweaksCache, cancellationToken);
                presetsJson = await File.ReadAllTextAsync(_presetsCache, cancellationToken);
                fromCache = true;
            }
        }

        return new WinUtilCatalogResult
        {
            Version = Version,
            Commit = Commit,
            LoadedAt = DateTime.Now,
            FromCache = fromCache,
            Tweaks = ParseCatalog(tweaksJson, presetsJson)
        };
    }

    private async Task<string> DownloadTextAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException(
                $"WinUtil devolvió HTTP {(int)response.StatusCode} para {uri}.");

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null &&
            !mediaType.Contains("text", StringComparison.OrdinalIgnoreCase) &&
            !mediaType.Contains("json", StringComparison.OrdinalIgnoreCase) &&
            !mediaType.Contains("octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Tipo de contenido inesperado desde WinUtil: {mediaType}");
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static IReadOnlyList<WinUtilTweak> ParseCatalog(
        string tweaksJson,
        string presetsJson)
    {
        var presetMembership = ParsePresetMembership(presetsJson);
        var rows = new List<WinUtilTweak>();

        using var tweaksDoc = ParseWinUtilJson(tweaksJson);

        foreach (var property in tweaksDoc.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
                continue;

            var obj = property.Value;
            var content = GetString(obj, "Content");
            var description = GetString(obj, "Description");
            var category = GetString(obj, "category");
            var panel = GetString(obj, "panel");

            var registryCount = CountArray(obj, "registry");
            var serviceCount = CountArray(obj, "service");
            var scriptCount = CountArray(obj, "InvokeScript");
            var taskCount =
                CountArray(obj, "scheduledtask") +
                CountArray(obj, "ScheduledTask") +
                CountArray(obj, "task");

            var presets = presetMembership.TryGetValue(property.Name, out var names)
                ? string.Join(", ", names.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                : "";

            rows.Add(new WinUtilTweak
            {
                Id = property.Name,
                Content = string.IsNullOrWhiteSpace(content) ? property.Name : content,
                Description = description,
                Category = category,
                Panel = panel,
                Presets = presets,
                RegistryActions = registryCount,
                ServiceActions = serviceCount,
                ScriptActions = scriptCount,
                ScheduledTaskActions = taskCount,
                Risk = ClassifyRisk(
                    property.Name,
                    content,
                    category,
                    registryCount,
                    serviceCount,
                    scriptCount)
            });
        }

        return rows
            .OrderBy(x => RiskOrder(x.Risk))
            .ThenBy(x => x.Category, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x.Content, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static JsonDocument ParseWinUtilJson(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            var normalized = NormalizeControlCharactersInsideStrings(json);
            return JsonDocument.Parse(normalized);
        }
    }

    /// <summary>
    /// Convierte únicamente caracteres de control literales que aparecen dentro
    /// de strings JSON a sus secuencias escapadas equivalentes. No interpreta,
    /// ejecuta ni modifica el contenido lógico de los scripts de WinUtil.
    /// </summary>
    private static string NormalizeControlCharactersInsideStrings(string input)
    {
        var output = new StringBuilder(input.Length + 256);
        var insideString = false;
        var escaped = false;

        foreach (var ch in input)
        {
            if (!insideString)
            {
                output.Append(ch);

                if (ch == '"')
                {
                    insideString = true;
                    escaped = false;
                }

                continue;
            }

            if (escaped)
            {
                output.Append(ch);
                escaped = false;
                continue;
            }

            if (ch == '\\')
            {
                output.Append(ch);
                escaped = true;
                continue;
            }

            if (ch == '"')
            {
                output.Append(ch);
                insideString = false;
                continue;
            }

            switch (ch)
            {
                case '\r':
                    output.Append("\\r");
                    break;
                case '\n':
                    output.Append("\\n");
                    break;
                case '\t':
                    output.Append("\\t");
                    break;
                default:
                    if (ch < 0x20)
                        output.Append($"\\u{(int)ch:X4}");
                    else
                        output.Append(ch);
                    break;
            }
        }

        return output.ToString();
    }

    private static Dictionary<string, HashSet<string>> ParsePresetMembership(string json)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(json);

        foreach (var preset in doc.RootElement.EnumerateObject())
        {
            if (preset.Value.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var item in preset.Value.EnumerateArray())
            {
                var id = item.GetString();
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (!result.TryGetValue(id, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    result[id] = set;
                }

                set.Add(preset.Name);
            }
        }

        return result;
    }

    private static string ClassifyRisk(
        string id,
        string content,
        string category,
        int registryCount,
        int serviceCount,
        int scriptCount)
    {
        var text = $"{id} {content} {category}".ToLowerInvariant();

        string[] highRiskTerms =
        [
            "advanced",
            "caution",
            "removeedge",
            "remove edge",
            "bitlocker",
            "defender",
            "windows update",
            "windowsai",
            "windows ai",
            "onedrive",
            "appx"
        ];

        if (highRiskTerms.Any(text.Contains))
            return "Alto";

        if (scriptCount > 0 || serviceCount > 0)
            return "Revisión";

        if (registryCount > 0)
            return "Medio";

        return "Informativo";
    }

    private static int RiskOrder(string risk) => risk switch
    {
        "Informativo" => 0,
        "Medio" => 1,
        "Revisión" => 2,
        "Alto" => 3,
        _ => 4
    };

    private static string GetString(JsonElement obj, string name)
    {
        foreach (var property in obj.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString() ?? "";
            }
        }

        return "";
    }

    private static int CountArray(JsonElement obj, string name)
    {
        foreach (var property in obj.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.Array)
            {
                return property.Value.GetArrayLength();
            }
        }

        return 0;
    }
}
