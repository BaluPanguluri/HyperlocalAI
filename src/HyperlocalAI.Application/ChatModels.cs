namespace HyperlocalAI.Application;

public sealed record ChatRequest(
    string Message,
    TenantContext Tenant,
    /// <summary>When true, skips web engagement metrics (WhatsApp records its own).</summary>
    bool SkipEngagementRecording = false);

/// <summary>Why snippet-only mode was used instead of the full LLM (for UI hints).</summary>
public enum LlmUnavailableReason
{
    None = 0,
    /// <summary>OpenAI/Azure selected but credentials or deployment settings are incomplete.</summary>
    CloudCredentialsMissing,
    /// <summary>Ollama selected but the local chat service could not be initialized (endpoint, model id).</summary>
    LocalOllamaNotReady,
    /// <summary>The model was configured but the completion call failed.</summary>
    ModelInvocationFailed,
}

public sealed record ChatReply(
    string Answer,
    bool UsedKnowledgeBase,
    bool DemoModeWithoutLlm,
    IReadOnlyList<string>? Sources,
    LlmUnavailableReason UnavailableReason = LlmUnavailableReason.None);
