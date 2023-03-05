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
        public Queue<KeyValuePair<string, string>> History => history;

        public OpenAiChatCompletionSession(string apiKey, string apiUrl)
        {
            ApiKey = apiKey;
            ApiUrl = apiUrl;
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

        public async Task<string?> AskAsync(string question)
        {
            List<object> messageModels = new List<object>();

            messageModels.Add(new
            {
                role = "system",
                content = initText
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
                    role = davinciRole ?? "you",
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
                            model = "gpt-3.5-turbo-0301",
                            messages = messageModels,
                            max_tokens = 2048,
                            temperature = 0.5,
                        }),
                };

            var response = await Utils.GlobalHttpClient.SendAsync(request);
            var davinci_rst = await response.Content.ReadFromJsonAsync<davinci_result>();
            if (davinci_rst == null)
                return null;

            var davinci_rst_message =
                davinci_rst.choices.FirstOrDefault()?.message;

            if (davinci_rst_message == null)
                return null;

            davinciRole = davinci_rst_message.role;

            history.Enqueue(new KeyValuePair<string, string>(question, davinci_rst_message.content));

            return davinci_rst_message.content;
        }

        public void Reset() => history.Clear();

        public class davinci_result
        {
            public class davinci_result_choice
            {
                public class davinci_result_choice_message
                {
                    public string role { get; set; }
                    public string content { get; set; }
                }
                public int index { get; set; }
                public davinci_result_choice_message message { get; set; }
                public string finish_reason { get; set; }
            }
            public class davinci_result_usage
            {
                public int prompt_tokens { get; set; }
                public int completion_tokens { get; set; }
                public int total_tokens { get; set; }
            }
            public string id { get; set; }
            public string @object { get; set; }
            public int created { get; set; }
            public davinci_result_choice[] choices { get; set; }
            public davinci_result_usage usage { get; set; }
        }

    }
}
