using System.Diagnostics;
using System.Text;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;

namespace MeowBot.Services.OpenAi
{
    internal class OpenAiChatService : AiChatServiceBase
    {
        /// <summary>
        /// OpenAi服务下，非白名单用户能够拥有的最大会话上下文数量
        /// </summary>
        private const int MaxHistoryCount = 50;

        
        public static bool CheckConfig(AppConfig config, out string? reason)
        {
            if (string.IsNullOrWhiteSpace(config.OpenAiApiKey))
            {
                reason = $"请指定机器人 API Key({nameof(config.OpenAiApiKey)})";
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>
        /// 用户配置的对话温度
        /// </summary>
        private double Temperature { get; set; }
        
        /// <summary>
        /// 用户配置的系统指令
        /// </summary>
        private string SystemMessage { get; set; }

        
        private readonly OpenAIClient m_Client;
        private readonly List<Message> m_MessageListBuffer = new();
        private readonly Queue<Message> m_ChatHistory = new();
        private readonly AppConfig m_Config;

        internal override async Task<Exception?> AskAsync(AskCommandArgsModel askCommandArgs, Func<string, bool, Task> sendMessageCallback)
        {
            var msgTxt = askCommandArgs.MsgTxt;
            var userNickname = askCommandArgs.UserNickname;
            var userId = askCommandArgs.UserId;
            
            var hasContextTrimmed = m_ChatHistory.Count > MaxHistoryCount && !m_Config.AccountWhiteList.Contains(userId);

            if (hasContextTrimmed)
            {
                while (m_ChatHistory.Count > MaxHistoryCount)
                {
                    m_ChatHistory.Dequeue();
                }
                await Console.Out.WriteLineAsync($"> 已裁剪用户 {userNickname}({userId}) 的多余对话上下文信息");
            }

            m_MessageListBuffer.Clear();
            m_MessageListBuffer.Add(new(Role.System, SystemMessage));
            m_MessageListBuffer.AddRange(AppConfig.SystemCommand.Select(sysMsg => new Message(Role.System, sysMsg)));
            m_MessageListBuffer.AddRange(m_ChatHistory);

            try
            {
                var ask = new Message(Role.User, msgTxt);
                m_MessageListBuffer.Add(ask);

                var result = await m_Client.ChatEndpoint.GetCompletionAsync(new(m_MessageListBuffer, model: Model.GPT3_5_Turbo, Temperature));

                Debug.WriteLine("OpenAI OK");

                var answer = result.Choices[0].Message;
                var openAiResult = new StringBuilder(answer.Content);

                if (hasContextTrimmed)
                    openAiResult.Append(" (已裁剪对话上下文)");

                m_ChatHistory.Enqueue(ask);
                m_ChatHistory.Enqueue(answer);

                Debug.WriteLine("GoCqHttp OK");
                await sendMessageCallback(openAiResult.ToString(), true);
                return null;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("This model's maximum context length is "))
                {
                    await sendMessageCallback($"请求失败，对话上下文超过了模型支持的长度，请使用 #reset 重置机器人\n{ex.Message}", true);
                    return null;
                }
                else
                {
                    return ex;
                }
            }
        }

        public override async Task<bool> HandlePotentialUserCommands(string msgTxt, AppConfig appConfig, long userId, Func<string, bool, Task> sendMessageCallback)
        {
            switch (msgTxt)
            {
                case "#help":

                    var chatGptHelpText =
                        $"""
                        ChatGPT 操作指令：
                        ----------------------------------
                        #temperature:<设置AI的应答气温(0~1)，越高的值会带来越随机的结果，反之则会带来越确定以及集中的结果，并重置对话>
                        #role:<切换一个的GPT角色预设，并重置对话>
                        #custom-role:<通过传入用于初始化GPT自我角色的提示性信息来自定义角色性格，并重置对话>
                        #reset:重置聊天对话的上下文信息
                        #history:检查当前已产生的历史记录数量
                        ----------------------------------
                        ！注意, 普通用户最多记忆{MaxHistoryCount}条聊天对话的上下文信息
                        ----------------------------------
                        以下是所有可用的GPT角色预设：

                        """;


                    StringBuilder sb = new(chatGptHelpText);
                    foreach (var builtinRolesKey in appConfig.BuiltinRoles.Keys)
                    {
                        sb.AppendLine($"\t{builtinRolesKey}");
                    }

                    await sendMessageCallback.Invoke(sb.ToString(), true);
                    break;
                case "#reset":

                    m_ChatHistory.Clear();
                    await sendMessageCallback.Invoke("> ChatGPT: 会话已重置", true);

                    break;
                case var _ when msgTxt.StartsWith("#temperature:"):

                    var potentialTemperature = msgTxt[13..].Trim();
                    if (!double.TryParse(potentialTemperature, out var validFloatValue) || validFloatValue is < 0 or > 1)
                    {
                        await sendMessageCallback.Invoke($"> ChatGPT: 无法将({potentialTemperature})识别为范围在(0 ~ 1)中的浮点数！", true);
                        break;
                    }

                    Temperature = validFloatValue;
                    m_ChatHistory.Clear();
                    await sendMessageCallback.Invoke($"> ChatGPT: 会话温度已更新: {validFloatValue:N2}", true);

                    break;
                case var _ when msgTxt.StartsWith("#role:"):

                    var role = msgTxt[6..].Trim();
                    if (appConfig.BuiltinRoles.TryGetValue(role, out var gptRoleSystemMessage))
                    {
                        SystemMessage = gptRoleSystemMessage;
                        m_ChatHistory.Clear();
                        await sendMessageCallback.Invoke($"> ChatGPT: 会话角色已更新: {role}", true);
                    }
                    else
                    {
                        await sendMessageCallback.Invoke($"> ChatGPT: 找不到所选的角色", true);
                    }

                    break;
                case var _ when msgTxt.StartsWith("#custom-role:"):

                    gptRoleSystemMessage = msgTxt[13..];
                    SystemMessage = gptRoleSystemMessage;
                    m_ChatHistory.Clear();
                    await sendMessageCallback.Invoke($"> ChatGPT: 自定义角色已更新", true);

                    break;
                case "#history":

                    await sendMessageCallback.Invoke($"> ChatGPT: 历史记录：{m_ChatHistory.Count}条", true);
                    var inWhiteList = appConfig.AccountWhiteList.Contains(userId);
                    if (!inWhiteList)
                        await sendMessageCallback.Invoke($"> ChatGPT: (您的聊天会话最多保留 {MaxHistoryCount} 条消息)", false);

                    break;
                default:
                    return false;
            }

            return true;
        }

        public override ValueTask DisposeAsync()
        {
            // m_Client.Dispose();
            return ValueTask.CompletedTask;
        }


        public OpenAiChatService(AppConfig config) : base(config)
        {
            m_Config = config;
            m_Client = new(new(config.OpenAiApiKey), new(config.ApiHost ?? AppConfig.DefaultApiHost));
            SystemMessage = string.IsNullOrWhiteSpace(config.GptRoleInitText) ? AppConfig.MeowBotRoleText : config.GptRoleInitText;
            Temperature = .5;
        }
    }
}