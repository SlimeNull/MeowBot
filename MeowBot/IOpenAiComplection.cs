namespace MeowBot
{
    internal interface IOpenAiComplection
    {
        Task<string?> AskAsync(string content);
    }
}