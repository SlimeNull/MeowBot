using RustSharp;

namespace MeowBot
{
    internal interface IOpenAiComplection
    {
        void InitWithText(string text);
        void Reset();
        Queue<KeyValuePair<string, string>> History { get; }
        Task<Result<string, string>> AskAsync(string content);
    }
}