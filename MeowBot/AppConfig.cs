using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowBot
{
    internal class AppConfig
    {
        public const string Filename = "AppConfig.json";

        public string? ApiKey { get; set; }
        public string? BotWebSocketUri { get; set; }

        public string? ChatCompletionApiUrl { get; set; }
        public string? TextCompletionApiUrl { get; set; }

        public static string DefaultChatCompletionApiUrl { get; } = "https://api.openai.com/v1/chat/completions";

        public long[]? WhiteList { get; set; }
    }
}
