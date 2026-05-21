namespace HyperlocalAI.Infrastructure;

public sealed class HyperlocalAIOptions
{
    public const string SectionName = "LocalMindAI";

    /// <summary>
    /// Chat backend: <c>OpenAI</c>, <c>AzureOpenAI</c>, or <c>Ollama</c> (local demos; no API key).
    /// Add more providers by extending <see cref="SemanticKernelChatService"/> kernel setup.
    /// </summary>
    public string Provider { get; set; } = "OpenAI";

    /// <summary>
    /// Model or deployment id: OpenAI model name, Azure deployment name, or Ollama model tag (e.g. llama3.2).
    /// </summary>
    public string ChatModelId { get; set; } = "gpt-4o-mini";

    public string EmbeddingModelId { get; set; } = "text-embedding-3-small";

    /// <summary>OpenAI API key (or Azure key when Provider is AzureOpenAI). Ignored for Ollama.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>Ollama server base URL (default http://127.0.0.1:11434). Uses native <c>/api/chat</c>; 127.0.0.1 avoids IPv6 localhost issues on Windows.</summary>
    public string OllamaEndpoint { get; set; } = "http://127.0.0.1:11434";

    public string? OrganizationId { get; set; }

    /// <summary>Azure OpenAI resource endpoint, e.g. https://your-resource.openai.azure.com/</summary>
    public string? AzureEndpoint { get; set; }

    /// <summary>Azure OpenAI deployment name for chat.</summary>
    public string? AzureChatDeploymentName { get; set; }

    /// <summary>Azure OpenAI deployment name for embeddings.</summary>
    public string? AzureEmbeddingDeploymentName { get; set; }
}
