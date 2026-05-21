namespace HyperlocalAI.Application;

public interface IHyperlocalChatService
{
    Task<ChatReply> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
