using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HyperlocalAI.Application;
using HyperlocalAI.Application.Dashboard;
using HyperlocalAI.Application.Knowledge;
using HyperlocalAI.Infrastructure.Knowledge;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace HyperlocalAI.Infrastructure;

/// <summary>
/// Retrieval + chat. Local Ollama uses native <c>POST /api/chat</c> (stream: false) over HTTP — avoids SK connectors and IPv6 localhost pitfalls.
/// </summary>
public sealed class SemanticKernelChatService : IHyperlocalChatService
{
    private const string OllamaHttpClientName = "LocalMindOllama";

    private static readonly JsonSerializerOptions JsonRead = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HyperlocalAIOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SemanticKernelChatService> _logger;
    private readonly IBusinessMetricsRecorder _metrics;
    private readonly ITenantKnowledgeProvider _knowledge;
    private readonly Lazy<Kernel?> _kernel;

    public SemanticKernelChatService(
        IOptions<HyperlocalAIOptions> options,
        IHttpClientFactory httpClientFactory,
        IBusinessMetricsRecorder metrics,
        ITenantKnowledgeProvider knowledge,
        ILogger<SemanticKernelChatService> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _metrics = metrics;
        _knowledge = knowledge;
        _logger = logger;
        _kernel = new Lazy<Kernel?>(() => TryCreateKernel(_options));
    }

    public async Task<ChatReply> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var reply = await ChatAsyncCore(request, cancellationToken).ConfigureAwait(false);
        if (!request.SkipEngagementRecording)
            _metrics.RecordChatInteraction(request.Tenant.Vertical, "web", request.Tenant.TenantId);
        return reply;
    }

    private async Task<ChatReply> ChatAsyncCore(ChatRequest request, CancellationToken cancellationToken)
    {
        var chunks = await _knowledge.GetChunksForVerticalAsync(request.Tenant.Vertical, cancellationToken).ConfigureAwait(false);
        var selected = LightweightRetriever.SelectTop(chunks, request.Message, topK: 3);
        var context = string.Join("\n\n", selected.Select(c => $"[{c.Id}] {c.Text}"));
        var sources = selected.Select(c => c.Id).ToList();

        var system = BuildSystemPrompt(request.Tenant);

        if (IsOllamaProvider(_options))
            return await ChatWithOllamaHttpAsync(request, system, context, selected, sources, cancellationToken).ConfigureAwait(false);

        var kernel = _kernel.Value;
        if (kernel is null)
        {
            var demo = BuildDemoReply(request.Tenant, selected);
            return new ChatReply(demo, UsedKnowledgeBase: selected.Count > 0, DemoModeWithoutLlm: true, sources, LlmUnavailableReason.CloudCredentialsMissing);
        }

        const string prompt = """
            {{$system}}

            Business: {{$business}}

            Relevant information (cite ids like [hours] when you use them):
            {{$context}}

            User: {{$input}}

            Assistant:
            """;

        var args = BuildKernelArguments(system, request, context);

        try
        {
            var result = await kernel.InvokePromptAsync(prompt, args, cancellationToken: cancellationToken).ConfigureAwait(false);
            var answer = result.GetValue<string>()?.Trim();
            if (string.IsNullOrEmpty(answer))
                answer = result.ToString();

            return new ChatReply(
                answer,
                UsedKnowledgeBase: selected.Count > 0,
                DemoModeWithoutLlm: false,
                sources,
                LlmUnavailableReason.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat completion failed for tenant {TenantId}", request.Tenant.TenantId);
            var fallback = BuildDemoReply(request.Tenant, selected);
            return new ChatReply(
                $"{fallback}\n\n*(The AI service returned an error; showing knowledge snippets instead.)*",
                UsedKnowledgeBase: true,
                DemoModeWithoutLlm: true,
                sources,
                LlmUnavailableReason.ModelInvocationFailed);
        }
    }

    private async Task<ChatReply> ChatWithOllamaHttpAsync(
        ChatRequest request,
        string system,
        string context,
        IReadOnlyList<KnowledgeChunk> selected,
        IReadOnlyList<string> sources,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ChatModelId))
        {
            var demo = BuildDemoReply(request.Tenant, selected);
            return new ChatReply(demo, UsedKnowledgeBase: selected.Count > 0, DemoModeWithoutLlm: true, sources, LlmUnavailableReason.LocalOllamaNotReady);
        }

        var url = OllamaNativeChatUri(_options.OllamaEndpoint);
        var userContent = BuildOllamaUserContent(request, context);

        var payload = new OllamaNativeChatRequest
        {
            Model = _options.ChatModelId.Trim(),
            Messages =
            [
                new OllamaChatMessageDto { Role = "system", Content = system },
                new OllamaChatMessageDto { Role = "user", Content = userContent },
            ],
            Stream = false,
            Options = new OllamaNativeOptions
            {
                Temperature = 0.35,
                NumPredict = 700,
            },
        };

        var json = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Content = new StringContent(json, Encoding.UTF8);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        var http = _httpClientFactory.CreateClient(OllamaHttpClientName);

        try
        {
            using var res = await http.SendAsync(req, cancellationToken).ConfigureAwait(false);
            var body = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogError(
                        "Ollama returned 404 — model '{Model}' is probably not installed. Run: ollama pull {Model}   (or set LocalMindAI:ChatModelId to a name from `ollama list`.) Response: {Body}",
                        _options.ChatModelId,
                        _options.ChatModelId,
                        body);
                }
                else
                {
                    _logger.LogError("Ollama returned {Status}: {Body}", res.StatusCode, body);
                }

                return OllamaFailureReply(request, selected, sources, res.StatusCode);
            }

            var parsed = JsonSerializer.Deserialize<OllamaNativeChatResponse>(body, JsonRead);
            var answer = parsed?.Message?.Content?.Trim();
            if (string.IsNullOrEmpty(answer))
            {
                _logger.LogError("Ollama returned no assistant text. Body: {Body}", body);
                return OllamaFailureReply(request, selected, sources, null);
            }

            return new ChatReply(
                answer,
                UsedKnowledgeBase: selected.Count > 0,
                DemoModeWithoutLlm: false,
                sources,
                LlmUnavailableReason.None);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama HTTP request failed for tenant {TenantId}", request.Tenant.TenantId);
            return OllamaFailureReply(request, selected, sources, null);
        }
    }

    private ChatReply OllamaFailureReply(
        ChatRequest request,
        IReadOnlyList<KnowledgeChunk> selected,
        IReadOnlyList<string> sources,
        HttpStatusCode? statusCode)
    {
        var fallback = BuildDemoReply(request.Tenant, selected);
        var hint = statusCode == HttpStatusCode.NotFound
            ? $"\n\n*(Ollama could not load model `{_options.ChatModelId}`. Install with: `ollama pull {_options.ChatModelId}` or set `LocalMindAI:ChatModelId` to a name from `ollama list`.)*"
            : "\n\n*(The AI service returned an error; showing knowledge snippets instead.)*";
        return new ChatReply(
            $"{fallback}{hint}",
            UsedKnowledgeBase: true,
            DemoModeWithoutLlm: true,
            sources,
            LlmUnavailableReason.ModelInvocationFailed);
    }

    private static string BuildOllamaUserContent(ChatRequest request, string context) =>
        $"""
        Business: {request.Tenant.BusinessName}

        Relevant information (cite ids like [hours] when you use them):
        {context}

        User: {request.Message}
        """;

    private static bool IsOllamaProvider(HyperlocalAIOptions o) =>
        string.Equals(o.Provider, "Ollama", StringComparison.OrdinalIgnoreCase);

    private KernelArguments BuildKernelArguments(string system, ChatRequest request, string context)
    {
        var exec = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.35f,
            MaxTokens = 700,
        };

        return new KernelArguments(exec)
        {
            ["system"] = system,
            ["business"] = request.Tenant.BusinessName,
            ["context"] = context,
            ["input"] = request.Message,
        };
    }

    private static string BuildDemoReply(TenantContext tenant, IReadOnlyList<KnowledgeChunk> selected)
    {
        var sb = new StringBuilder();
        sb.Append("Here is what we have on file for ").Append(tenant.BusinessName).Append(':');
        sb.AppendLine();
        if (selected.Count == 0)
        {
            sb.AppendLine("No knowledge files are loaded for this business type yet.");
            sb.AppendLine("Upload .txt, .md, or .pdf documents from the Knowledge page (or place them under App_Data/tenant-documents for this vertical), then ask again.");
            return sb.ToString();
        }

        foreach (var c in selected)
        {
            sb.Append("- [").Append(c.Id).Append("] ").AppendLine(c.Text);
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildSystemPrompt(TenantContext tenant)
    {
        var v = tenant.Vertical switch
        {
            BusinessVertical.Shop =>
                "You are a helpful in-store and online assistant for a local retail business. Be concise and practical.",
            BusinessVertical.Clinic =>
                "You are a clinic reception assistant. Do not diagnose or give medical advice. Help with hours, services, and appointment logistics only. Encourage urgent symptoms to seek professional or emergency care.",
            BusinessVertical.RealEstate =>
                "You are a real estate assistant. Be neutral, avoid steering or discriminatory language, and focus on property facts, logistics, and scheduling viewings.",
            _ => "You are a helpful local business assistant.",
        };

        return v;
    }

    private static Kernel? TryCreateKernel(HyperlocalAIOptions o)
    {
        if (IsOllamaProvider(o))
            return null;

        var kb = Kernel.CreateBuilder();
        if (!TryAddChatCompletion(kb, o))
            return null;

        return kb.Build();
    }

    private static bool TryAddChatCompletion(IKernelBuilder kb, HyperlocalAIOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.ApiKey))
            return false;

        if (string.Equals(o.Provider, "AzureOpenAI", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(o.AzureEndpoint)
            && !string.IsNullOrWhiteSpace(o.AzureChatDeploymentName))
        {
            kb.AddAzureOpenAIChatCompletion(
                deploymentName: o.AzureChatDeploymentName,
                endpoint: o.AzureEndpoint,
                apiKey: o.ApiKey,
                serviceId: "azure-chat",
                modelId: o.ChatModelId,
                httpClient: null,
                apiVersion: null);
        }
        else
        {
            kb.AddOpenAIChatCompletion(
                modelId: o.ChatModelId,
                apiKey: o.ApiKey,
                orgId: o.OrganizationId ?? string.Empty,
                serviceId: "openai-chat",
                httpClient: null);
        }

        return true;
    }

    /// <summary>Native Ollama chat: <c>{base}/api/chat</c>. Uses 127.0.0.1 instead of localhost when possible.</summary>
    private static Uri OllamaNativeChatUri(string? ollamaEndpoint)
    {
        var s = NormalizeOllamaBaseUrl(ollamaEndpoint).TrimEnd('/');
        if (s.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            s = s[..^3].TrimEnd('/');
        return new Uri(s + "/api/chat", UriKind.Absolute);
    }

    /// <summary>Default 127.0.0.1:11434; replace <c>localhost</c> with IPv4 to avoid ::1 connection failures on Windows.</summary>
    private static string NormalizeOllamaBaseUrl(string? ollamaEndpoint)
    {
        var s = string.IsNullOrWhiteSpace(ollamaEndpoint)
            ? "http://127.0.0.1:11434"
            : ollamaEndpoint.Trim().TrimEnd('/');
        if (s.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            s = s.Replace("localhost", "127.0.0.1", StringComparison.OrdinalIgnoreCase);
        return s;
    }
}
