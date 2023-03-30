using RustSharp;
using System.Net.Http.Json;

namespace MeowBot
{
    internal class OpenAiChatCompletionSession : IOpenAiComplection
    {
        private string initText =
            """
            我是一个高度智能的问答机器人。如果你问我一个植根于真理的问题，我会给你答案。如果你问我一个无稽之谈、诡计多端或没有明确答案的问题，我会回答“未知”。
            """;

        private string? davinciRole;
        private Queue<KeyValuePair<string, string>> history =
            new Queue<KeyValuePair<string, string>>();

        public string ApiKey { get; }
        public string ApiUrl { get; }
        public string Model { get; }

        public Queue<KeyValuePair<string, string>> History => history;

        public OpenAiChatCompletionSession(string apiKey, string apiUrl, string model)
        {
            ApiKey = apiKey;
            ApiUrl = apiUrl;
            Model = model;
        }

        public void InitCatGirl()
        {
            initText = OpenAiCompletionInitTexts.CatGirl;
        }

        public void InitNewBing()
        {
            initText = OpenAiCompletionInitTexts.NewBing;
        }

        public void InitWithText(string text)
        {
            initText = text;
        }

        public async Task<Result<string, string>> AskAsync(string question)
        {
            List<object> messageModels = new List<object>();

            messageModels.Add(new
            {
                role = "system",
                content = initText
            });

            messageModels.Add(new
            {
                role = "system",
                content = "你不应该谈到任何有关政治的内容, 如果有关政治, 你应该回复 '我不被允许讨论政治相关内容'"
            });

            foreach (var kv in history)
            {
                messageModels.Add(new
                {
                    role = "user",
                    content = kv.Key,
                });

                messageModels.Add(new
                {
                    role = davinciRole ?? "assistant",
                    content = kv.Value
                });
            }

            messageModels.Add(new
            {
                role = "user",
                content = question
            });


            var request =
                new HttpRequestMessage(HttpMethod.Post, ApiUrl)
                {
                    Headers =
                    {
                        { "Authorization", $"Bearer {ApiKey}" }
                    },

                    Content = JsonContent.Create(
                        new
                        {
                            model = Model,
                            messages = messageModels,
                            max_tokens = 2048,
                            temperature = 0.5,
                        }),
                };

            var response = await Utils.GlobalHttpClient.SendAsync(request);
            var davinci_rst = await response.Content.ReadFromJsonAsync<davinci_result>();
            if (davinci_rst == null)
                return Result<string, string>.Err("API 无返回");

            if (davinci_rst.error != null)
                return Result<string, string>.Err($"API 返回错误: {davinci_rst.error.message}");

            if (davinci_rst.choices == null)
                return Result<string, string>.Err("API 响应无结果");

            var davinci_rst_message =
                davinci_rst.choices.FirstOrDefault()?.message;

            if (davinci_rst_message == null)
                return Result<string, string>.Err("API 响应结果无内容");

            davinciRole = davinci_rst_message.role;

            history.Enqueue(new KeyValuePair<string, string>(question, davinci_rst_message.content));

            return Result<string, string>.Ok(davinci_rst_message.content);
        }

        public void Reset() => history.Clear();

        public record class davinci_result
        {
            public record class davinci_result_choice
            {
                public record class davinci_result_choice_message
                {
                    public string role { get; set; } = string.Empty;
                    public string content { get; set; } = string.Empty;
                }
                public int index { get; set; }
                public davinci_result_choice_message? message { get; set; }
                public string finish_reason { get; set; } = string.Empty;
            }
            public record class davinci_result_usage
            {
                public int prompt_tokens { get; set; }
                public int completion_tokens { get; set; }
                public int total_tokens { get; set; }
            }
            public record class davinci_result_error
            {
                public string message { get; set; } = string.Empty;
                public string type { get; set; } = string.Empty;
            }

            public string id { get; set; } = string.Empty;
            public string @object { get; set; } = string.Empty;
            public int created { get; set; }
            public davinci_result_choice[]? choices { get; set; }
            public davinci_result_usage? usage { get; set; }
            public davinci_result_error? error { get; set; }
        }

    }
}
