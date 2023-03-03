namespace MeowBot
{
    internal interface IOpenAiComplection
    {
        void InitWithText(string text);
        void Reset();
        Queue<KeyValuePair<string, string>> History { get; }
        Task<string?> AskAsync(string content);
    }
}