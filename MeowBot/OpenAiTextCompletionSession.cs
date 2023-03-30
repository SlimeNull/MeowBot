using RustSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace MeowBot
{
    internal class OpenAiTextCompletionSession : IOpenAiComplection
    {
        private string initText =
            """
            我是一个高度智能的问答机器人。如果你问我一个植根于真理的问题，我会给你答案。如果你问我一个无稽之谈、诡计多端或没有明确答案的问题，我会回答“未知”。
            """;

        private Queue<KeyValuePair<string, string>> history = 
            new Queue<KeyValuePair<string, string>>();

        public string ApiKey { get; }
        public string ApiUrl { get; }
        public Queue<KeyValuePair<string, string>> History => history;

        public OpenAiTextCompletionSession(string apiKey, string apiUrl)
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

        public async Task<Result<string, string>> AskAsync(string question)
        {
            StringBuilder historyTextBuilder = new StringBuilder();
            foreach (var kv in history)
            {
                historyTextBuilder.AppendLine($"Q: {kv.Key}");
                historyTextBuilder.AppendLine($"A: {kv.Value}");
                historyTextBuilder.AppendLine();
            }

            string prompt = $"""
                {initText}
                你不应该谈到任何有关政治的内容, 如果有关政治, 你应该回复 '我不被允许讨论政治相关内容'

                {historyTextBuilder.ToString()}

                Q: {question}
                A: 
                """;


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
                            model = "text-davinci-003",
                            prompt = prompt,
                            max_tokens = 2048,
                            temperature = 0.5,
                        }),
                };


            var response = await Utils.GlobalHttpClient.SendAsync(request);
            var davinci_rst = await response.Content.ReadFromJsonAsync<davinci_result>();

            if (davinci_rst == null)
                return Result<string, string>.Err("API 无返回");

            var davinci_rst_txt =
                davinci_rst.choices.FirstOrDefault()?.text;

            if (davinci_rst_txt == null)
                return Result<string, string>.Err("API 返回无内容");

            history.Enqueue(new KeyValuePair<string, string>(question, davinci_rst_txt));
            return Result<string, string>.Ok(davinci_rst_txt);
        }

        public void Reset() => history.Clear();
        Task<Result<string, string>> IOpenAiComplection.AskAsync(string content) => throw new NotImplementedException();

        public class davinci_result
        {
            public class davinci_result_choice
            {
                public string text { get; set; }
                public int index { get; set; }
                public object logprobs { get; set; }
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
            public string model { get; set; }
            public davinci_result_choice[] choices { get; set; }
            public davinci_result_usage usage { get; set; }
        }
    }
}
