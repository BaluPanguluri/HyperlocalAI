using System.Text.Json.Serialization;

namespace HyperlocalAI.Infrastructure;

/// <summary>Ollama native <c>POST /api/chat</c> (stream: false). More reliable than the OpenAI shim on some setups.</summary>
internal sealed class OllamaNativeChatRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("messages")]
    public required IReadOnlyList<OllamaChatMessageDto> Messages { get; init; }

    [JsonPropertyName("stream")]
    public bool Stream { get; init; }

    [JsonPropertyName("options")]
    public OllamaNativeOptions? Options { get; init; }
}

internal sealed class OllamaNativeOptions
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; init; }

    [JsonPropertyName("num_predict")]
    public int NumPredict { get; init; }
}

internal sealed class OllamaChatMessageDto
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

internal sealed class OllamaNativeChatResponse
{
    [JsonPropertyName("message")]
    public OllamaChatMessageDto? Message { get; init; }
}
