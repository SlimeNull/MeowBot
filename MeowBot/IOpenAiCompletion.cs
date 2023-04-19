using RustSharp;

namespace MeowBot;

internal interface IOpenAiCompletion
{
    void UpdateChatBotTemperature(float newTemperature);
    void UpdateChatBotRole(string roleText);
    void Reset();
    Queue<KeyValuePair<string, string>> History { get; }
    Task<Result<string, string>> AskAsync(string content);
}