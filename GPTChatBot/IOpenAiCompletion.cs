using RustSharp;

namespace GPTChatBot;

internal interface IOpenAiCompletion
{
    void UpdateChatBotRole(string roleText);
    void Reset();
    Queue<KeyValuePair<string, string>> History { get; }
    Task<Result<string, string>> AskAsync(string content);
}