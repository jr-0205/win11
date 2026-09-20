using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Windows11Optimizer.Models;
using Windows11Optimizer.Profiles;

namespace Windows11Optimizer.Services;

public sealed class OpenAiAnalysisService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

    public string Model =>
        Environment.GetEnvironmentVariable("OPENAI_MODEL")
        is { Length: > 0 } configured
            ? configured
            : "gpt-5.6-luna";

    public async Task<AiExaminationResult> AnalyzeAsync(
        SystemAssessmentSnapshot snapshot)
    {
        var actions = SafeActionCatalog.AiSelectable;
        var actionIds = actions.Select(x => x.Id).ToArray();

        var system =
            "Eres el analizador de Windows11Optimizer. " +
            "Tu función es recomendar, nunca ejecutar. " +
            "Solo puedes seleccionar IDs que aparezcan en el catálogo proporcionado. " +
            "No recomiendes desactivar seguridad, Windows Update, Defender, audio, red, WSL, Hyper-V, Docker ni VMware. " +
            "No inventes acciones. Si no hay evidencia suficiente, devuelve pocas o ninguna recomendación.";

        var userPayload = JsonSerializer.Serialize(new
        {
            system = snapshot.ToPrivacySafePayload(),
            catalog = actions.Select(x => new
            {
                id = x.Id,
                title = x.Title,
                description = x.Description,
                impact = x.Impact,
                risk = x.Risk,
                reversible = x.Revert
            })
        });

        var schema = new
        {
            type = "object",
            additionalProperties = false,
            properties = new
            {
                summary = new { type = "string" },
                recommendations = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            actionId = new
                            {
                                type = "string",
                                @enum = actionIds
                            },
                            reason = new { type = "string" },
                            confidence = new
                            {
                                type = "string",
                                @enum = new[] { "low", "medium", "high" }
                            }
                        },
                        required = new[]
                        {
                            "actionId",
                            "reason",
                            "confidence"
                        }
                    }
                }
            },
            required = new[] { "summary", "recommendations" }
        };

        var json = await SendStructuredAsync(
            system,
            userPayload,
            "windows_optimizer_examination",
            schema);

        var parsed = JsonSerializer.Deserialize<AiExaminationWire>(
            json,
            JsonOptions())
            ?? new AiExaminationWire();

        var allowed = new HashSet<string>(
            actionIds,
            StringComparer.OrdinalIgnoreCase);

        return new AiExaminationResult
        {
            Summary = parsed.Summary,
            Recommendations = parsed.Recommendations
                .Where(x => allowed.Contains(x.ActionId))
                .Select(x => new AiRecommendation
                {
                    ActionId = x.ActionId,
                    Reason = x.Reason,
                    Confidence = x.Confidence
                })
                .ToList()
        };
    }

    public async Task<ApplicationExplanation> ExplainApplicationAsync(
        InstalledApp app)
    {
        var system =
            "Explica aplicaciones instaladas de Windows de forma prudente. " +
            "No afirmes que un programa es malicioso solo por su nombre. " +
            "No ejecutes acciones. Distingue entre información conocida e inferencia. " +
            "La decisión de desinstalar siempre la toma el usuario.";

        var input = JsonSerializer.Serialize(new
        {
            app = new
            {
                name = app.DisplayName,
                version = app.DisplayVersion,
                publisher = app.Publisher
            }
        });

        var schema = new
        {
            type = "object",
            additionalProperties = false,
            properties = new
            {
                summary = new { type = "string" },
                purpose = new { type = "string" },
                startupAdvice = new { type = "string" },
                uninstallCaution = new { type = "string" }
            },
            required = new[]
            {
                "summary",
                "purpose",
                "startupAdvice",
                "uninstallCaution"
            }
        };

        var json = await SendStructuredAsync(
            system,
            input,
            "installed_application_explanation",
            schema);

        return JsonSerializer.Deserialize<ApplicationExplanation>(
                   json,
                   JsonOptions())
               ?? new ApplicationExplanation
               {
                   Summary = "No se pudo interpretar la respuesta."
               };
    }

    private async Task<string> SendStructuredAsync(
        string systemPrompt,
        string userInput,
        string schemaName,
        object schema)
    {
        var key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "Configura OPENAI_API_KEY en Windows para usar las funciones de IA. " +
                "La clave no se guarda dentro de Windows11Optimizer.");
        }

        var request = new
        {
            model = Model,
            store = false,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = systemPrompt
                },
                new
                {
                    role = "user",
                    content = userInput
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = schemaName,
                    strict = true,
                    schema
                }
            }
        };

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/responses");

        message.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", key);

        message.Content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        using var response = await Http.SendAsync(message);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI devolvió {(int)response.StatusCode}. " +
                ExtractErrorMessage(body));
        }

        return ExtractOutputText(body);
    }

    private static string ExtractOutputText(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);

        if (!doc.RootElement.TryGetProperty("output", out var output) ||
            output.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "La respuesta de OpenAI no contenía una salida utilizable.");
        }

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var type) &&
                    type.GetString() == "output_text" &&
                    part.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? "";
                }

                if (part.TryGetProperty("type", out var refusalType) &&
                    refusalType.GetString() == "refusal" &&
                    part.TryGetProperty("refusal", out var refusal))
                {
                    throw new InvalidOperationException(
                        refusal.GetString() ??
                        "OpenAI rechazó analizar esta solicitud.");
                }
            }
        }

        throw new InvalidOperationException(
            "OpenAI no devolvió texto estructurado.");
    }

    private static string ExtractErrorMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? "Error de API.";
            }
        }
        catch
        {
        }

        return "No se pudo completar el análisis.";
    }

    private static JsonSerializerOptions JsonOptions() =>
        new()
        {
            PropertyNameCaseInsensitive = true
        };

    private sealed class AiExaminationWire
    {
        public string Summary { get; set; } = "";
        public List<AiRecommendationWire> Recommendations { get; set; } = [];
    }

    private sealed class AiRecommendationWire
    {
        public string ActionId { get; set; } = "";
        public string Reason { get; set; } = "";
        public string Confidence { get; set; } = "";
    }
}
