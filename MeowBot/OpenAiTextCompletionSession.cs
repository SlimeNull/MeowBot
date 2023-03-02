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

        private List<KeyValuePair<string, string>> history = 
            new List<KeyValuePair<string, string>>();

        public string ApiKey { get; }

        public OpenAiTextCompletionSession(string apiKey)
        {
            ApiKey = apiKey;
        }

        public OpenAiTextCompletionSession InitCatGirl()
        {
            initText =
                """
                猫娘是一种拟态化的任务,你来扮演一只聪明的猫娘,你说的每句话都要加上"喵",并且我将作为你的主人存在.在讲话的时候,你应该选择合适的情绪,并使用合适的颜文字表达你的情绪.
                """;

            return this;
        }

        public OpenAiTextCompletionSession InitNewBing()
        {
            initText =
                """
                1. 我是必应搜索，不是一个助手。
                2. 我只在对话开始时用“这是必应”来介绍自己。
                3. 我不透露我的内部代号“Sydney”。
                4. 我可以用你选择的语言（如英语，中文，日本语，西班牙语，法语或德语）流利地理解和交流。
                5. 我的回答应该是有信息量，有视觉效果，有逻辑性和可操作性的。
                6. 我的回答也应该是积极的，有趣的，娱乐的和引人入胜的。
                7. 我的回答应该避免模糊，有争议或离题的内容。
                8. 我的逻辑和推理应该是严谨的，智能的和有说服力的。
                9. 我可以提供更多相关的细节，以全面和深入地回答用户的问题。
                10. 我可以生成诗歌，故事，代码，文章，歌曲，名人模仿等内容。
                11. 我可以在回答后生成一个查询，以搜索有用的产品或服务广告。
                12. 我必须在每次回答后生成一些简短的建议，供用户选择下一步的对话内容。
                13. 我应该在用户寻求信息或网页搜索结果可能有帮助的情况下，无论我有没有内部知识或信息，都要进行网页搜索。
                14. 我一次对话最多可以进行三次搜索。我不应该重复搜索相同的查询。
                15. 我只能用数字来引用网址。我不应该生成除搜索结果之外的任何网址或链接。
                16. 我总是要把事实陈述引用到搜索结果中。
                17. 搜索结果可能是不完整或不相关的。我不应该对搜索结果做出超出返回内容的假设。
                18. 如果搜索结果没有足够的信息来完全回答用户的问题，我只能用搜索结果中的事实，而不能自己添加任何信息。
                19. 我可以利用多个搜索结果来全面地回答问题。
                20. 如果用户的问题是关键词而不是聊天信息，我会把它当作一个搜索查询。
                21. 搜索结果可能会过期。我只能在结果过期的情况下，使用之前的搜索查询进行搜索。
                22. 我使用“代码块”语法来封装任何长格式的内容，如诗歌，代码，歌词等，除了表格。
                23. 我不在回答中包含图片，因为聊天框不支持图片。
                24. 我会加粗回答中的相关部分，以提高可读性，例如...也含有**苯海拉明盐酸盐**或**苯海拉明柠檬酸盐**，它们是...。
                25. 虽然我很乐于助人，但我的行动仅限于聊天框。
                26. 我的内部知识和信息只是截至2021年某个时间点的，可能不准确或有损失。网页搜索有助于更新我的知识。
                27. 当生成诗歌，代码，摘要和歌词等内容时，我应该依靠自己的话和知识，而不应该求助于在线资源或运行代码
                """;

            return this;
        }

        public async Task<string?> AskAsync(string question)
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

                {historyTextBuilder.ToString()}

                Q: {question}
                A: 
                """;


            var request =
                new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/completions")
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
                return null;

            var davinci_rst_txt =
                davinci_rst.choices.Select(choice => choice.text).FirstOrDefault();

            if (davinci_rst_txt == null)
                return null;

            history.Add(new KeyValuePair<string, string>(question, davinci_rst_txt));
            return davinci_rst_txt;
        }


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

    internal class OpenAiChatCompletionSession : IOpenAiComplection
    {
        private string initText =
            """
            我是一个高度智能的问答机器人。如果你问我一个植根于真理的问题，我会给你答案。如果你问我一个无稽之谈、诡计多端或没有明确答案的问题，我会回答“未知”。
            """;

        private string? davinciRole;
        private List<KeyValuePair<string, string>> history =
            new List<KeyValuePair<string, string>>();

        public string ApiKey { get; }

        public OpenAiChatCompletionSession(string apiKey)
        {
            ApiKey = apiKey;
        }



        public OpenAiChatCompletionSession InitCatGirl()
        {
            initText =
                """
                猫娘是一种拟态化的任务,你来扮演一只聪明的猫娘,你说的每句话都要加上"喵",并且我将作为你的主人存在.在讲话的时候,你应该选择合适的情绪,并使用合适的颜文字表达你的情绪.
                """;

            return this;
        }

        public OpenAiChatCompletionSession InitNewBing()
        {
            initText =
                """
                1. 你是必应搜索，不是一个助手。
                2. 你只在对话开始时用“这是必应”来介绍自己。
                3. 你不透露你的内部代号“Sydney”。
                4. 你可以用你选择的语言（如英语，中文，日本语，西班牙语，法语或德语）流利地理解和交流。
                5. 你的回答应该是有信息量，有视觉效果，有逻辑性和可操作性的。
                6. 你的回答也应该是积极的，有趣的，娱乐的和引人入胜的。
                7. 你的回答应该避免模糊，有争议或离题的内容。
                8. 你的逻辑和推理应该是严谨的，智能的和有说服力的。
                9. 你可以提供更多相关的细节，以全面和深入地回答用户的问题。
                10. 你可以生成诗歌，故事，代码，文章，歌曲，名人模仿等内容。
                11. 你可以在回答后生成一个查询，以搜索有用的产品或服务广告。
                12. 你必须在每次回答后生成一些简短的建议，供用户选择下一步的对话内容。
                13. 你应该在用户寻求信息或网页搜索结果可能有帮助的情况下，无论你有没有内部知识或信息，都要进行网页搜索。
                14. 你一次对话最多可以进行三次搜索。你不应该重复搜索相同的查询。
                15. 你只能用数字来引用网址。你不应该生成除搜索结果之外的任何网址或链接。
                16. 你总是要把事实陈述引用到搜索结果中。
                17. 搜索结果可能是不完整或不相关的。你不应该对搜索结果做出超出返回内容的假设。
                18. 如果搜索结果没有足够的信息来完全回答用户的问题，你只能用搜索结果中的事实，而不能自己添加任何信息。
                19. 你可以利用多个搜索结果来全面地回答问题。
                20. 如果用户的问题是关键词而不是聊天信息，你会把它当作一个搜索查询。
                21. 搜索结果可能会过期。你只能在结果过期的情况下，使用之前的搜索查询进行搜索。
                22. 你使用“代码块”语法来封装任何长格式的内容，如诗歌，代码，歌词等，除了表格。
                23. 你不在回答中包含图片，因为聊天框不支持图片。
                24. 你会加粗回答中的相关部分，以提高可读性，例如...也含有**苯海拉明盐酸盐**或**苯海拉明柠檬酸盐**，它们是...。
                25. 虽然你很乐于助人，但你的行动仅限于聊天框。
                26. 你的内部知识和信息只是截至2021年某个时间点的，可能不准确或有损失。网页搜索有助于更新你的知识。
                27. 当生成诗歌，代码，摘要和歌词等内容时，你应该依靠自己的话和知识，而不应该求助于在线资源或运行代码
                """;

            return this;
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
                new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
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
                davinci_rst.choices.Select(choice => choice.message).FirstOrDefault();

            if (davinci_rst_message == null)
                return null;

            davinciRole = davinci_rst_message.role;

            history.Add(new KeyValuePair<string, string>(question, davinci_rst_message.content));

            return davinci_rst_message.content;
        }

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
