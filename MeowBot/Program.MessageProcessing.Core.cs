using System.Diagnostics;
using System.Text;
using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;
using OpenAI.Chat;
using OpenAI.Models;
using RustSharp;

namespace MeowBot;

internal static partial class Program
{
    /// <summary>
    /// 处理收到的消息
    /// </summary>
    /// <param name="incomingMessage">用户通过群组或私聊向Bot发送的消息</param>
    /// <param name="aiSessionStorages">所有用户的OpenAI会话上下文存储信息</param>
    /// <param name="appConfig">应用程序设置</param>
    /// <param name="userId">发送消息的用户的ID</param>
    /// <param name="userNickname">发送消息的用户的昵称</param>
    /// <param name="sendMessageCallback">回复消息的回调</param>
    private static async Task OnMessageReceived(string incomingMessage, Dictionary<long, AiCompletionSessionStorage> aiSessionStorages, AppConfig appConfig, long userId, string userNickname, Func<string, Task> sendMessageCallback)
    {
        // 获得当前消息文本
        var msgTxt = incomingMessage.Trim();

        // 为新用户创建OpenAI会话
        if (!aiSessionStorages.TryGetValue(userId, out var aiSession))
        {
            aiSessionStorages[userId] = aiSession = new(
                new OpenAI.OpenAIClient(
                    new OpenAI.OpenAIAuthentication(appConfig.OpenAiApiKey),
                    new OpenAI.OpenAIClientSettings(appConfig.ApiHost ?? AppConfig.DefaultApiHost))
            )
            {
                SystemMessage = AppConfig.MeowBotRoleText,
                Temperature = .5
            };

            await Console.Out.WriteLineAsync($"> 为用户 {userNickname}({userId}) 创建OpenAI会话信息");
        }

        // 用户流量管理
        if (!appConfig.AccountWhiteList.Contains(userId) && aiSession.GetUsageCountInLastDuration(TimeSpan.FromSeconds(appConfig.UsageLimitTime)) >= appConfig.UsageLimitCount)
        {
            var helpText = $"(你不在机器人白名单内, {appConfig.UsageLimitTime}秒内仅允许使用{appConfig.UsageLimitCount}次.)";
            await sendMessageCallback.Invoke(helpText);
            await Console.Out.WriteLineAsync($"> 已拒绝用户 {userNickname}({userId}) 的OpenAI会话请求（流量管理）");
        }
        // 用户命令处理 & GptAPI调用
        else if (!await HandlePotentialUserCommands(msgTxt, aiSession, appConfig, userId, sendMessageCallback))
        {
            Debug.WriteLine("Message Received");

            var dequeue = aiSession.ChatHistory.Count > MaxHistoryCount && !appConfig.AccountWhiteList.Contains(userId);

            if (dequeue)
            {
                while (aiSession.ChatHistory.Count > MaxHistoryCount)
                {
                    aiSession.ChatHistory.Dequeue();
                }
                await Console.Out.WriteLineAsync($"> 已裁剪用户 {userNickname}({userId}) 的多余对话上下文信息");
            }
            try
            {
                List<Message> messagesToRequest =
                    new List<Message>();

                messagesToRequest.Add(
                    new Message(Role.System, aiSession.SystemMessage));

                foreach (var sysmsg in appConfig.SystemCommand)
                    messagesToRequest.Add(new Message(Role.System, sysmsg));

                foreach (var msg in aiSession.ChatHistory)
                    messagesToRequest.Add(msg);

                try
                {
                    Message ask = new Message(Role.User, msgTxt);
                    messagesToRequest.Add(ask);

                    var result = await aiSession.Client.ChatEndpoint.GetCompletionAsync(
                        new ChatRequest(messagesToRequest, model: Model.GPT3_5_Turbo, aiSession.Temperature));

                    Debug.WriteLine("OpenAI OK");

                    Message answer = result.Choices.First().Message;
                    var openAiResult = new StringBuilder(answer.Content);

                    if (dequeue)
                        openAiResult.Append(" (已裁剪对话上下文)");

                    await sendMessageCallback.Invoke(openAiResult.ToString());

                    aiSession.ChatHistory.Enqueue(ask);
                    aiSession.ChatHistory.Enqueue(answer);
                    aiSession.RecordApiUsage();

                    await Console.Out.WriteLineAsync($"> 已回应用户 {userNickname}({userId}) 的OpenAI会话");
                    Debug.WriteLine("GoCqHttp OK");
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("This model's maximum context length is "))
                    {
                        await sendMessageCallback.Invoke($"请求失败，对话上下文可能过长，请使用 #reset 重置机器人\n{ex.Message}");
                    }
                    else
                    {
                        await sendMessageCallback.Invoke($"请求失败，请重新尝试，你也可以使用 #reset 重置机器人\n{ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                await sendMessageCallback.Invoke($"请求失败，请重新尝试，你也可以使用 #reset 重置机器人\n{ex.Data}");
                var tempColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Magenta;
                await Console.Out.WriteLineAsync($"Exception: {ex}");
                Console.ForegroundColor = tempColor;
            }
        }
    }

    /// <summary>
    /// 检查用户输入的文本是否是给定的命令
    /// </summary>
    /// <param name="msgTxt">用户输入的文本</param>
    /// <param name="aiSession">当前用户的OpenAI会话上下文存储信息</param>
    /// <param name="appConfig">应用程序设置</param>
    /// <param name="userId">发送消息的用户ID</param>
    /// <param name="sendMessageCallback">执行消息发送动作的回调</param>
    /// <returns></returns>
    private static async Task<bool> HandlePotentialUserCommands(string msgTxt,
        AiCompletionSessionStorage aiSession,
        AppConfig appConfig,
        long userId,
        Func<string, Task> sendMessageCallback)
    {
        if (!msgTxt.StartsWith("#", StringComparison.OrdinalIgnoreCase))
            return false;

        switch (msgTxt)
        {
            case var _ when msgTxt.StartsWith("#help"):
                var helpText =
                    $"""
                    操作指令：
                    ----------------------------------
                    #temperature:<设置AI的应答气温(0~1)，越高的值会带来越随机的结果，反之则会带来越确定以及集中的结果>
                    #role:<切换一个的GPT角色预设>
                    #custom-role:<通过传入用于初始化GPT自我角色的提示性信息来自定义角色性格>
                    #reset:重置聊天对话的上下文信息
                    ----------------------------------
                    ！注意, 普通用户最多记忆{MaxHistoryCount}条聊天对话的上下文信息
                    ----------------------------------
                    以下是所有可用的GPT角色预设：

                    """;

                var sb = new StringBuilder(helpText);
                foreach (var builtinRolesKey in appConfig.BuiltinRoles.Keys)
                {
                    sb.AppendLine($"\t{builtinRolesKey}");
                }

                await sendMessageCallback.Invoke(sb.ToString());
                break;
            case var _ when msgTxt.StartsWith("#reset"):

                aiSession.ChatHistory.Clear();
                await sendMessageCallback.Invoke("会话已重置");

                break;
            case var _ when msgTxt.StartsWith("#temperature:"):

                var potentialTemperature = msgTxt[13..].Trim();
                if (!double.TryParse(potentialTemperature, out var validFloatValue) || validFloatValue is < 0 or > 1)
                {
                    await sendMessageCallback.Invoke($"无法将({potentialTemperature})识别为范围在(0 ~ 1)中的浮点数！");
                    break;
                }

                aiSession.Temperature = validFloatValue;
                await sendMessageCallback.Invoke($"会话温度已更新: {validFloatValue:N2}");

                break;
            case var _ when msgTxt.StartsWith("#role:"):

                var role = msgTxt[6..].Trim();
                if (appConfig.BuiltinRoles.TryGetValue(role, out var gptRoleSystemMessage))
                {
                    aiSession.SystemMessage = gptRoleSystemMessage;
                    await sendMessageCallback.Invoke($"会话角色已更新: {role}");
                }
                else
                {
                    await sendMessageCallback.Invoke($"找不到所选的角色");
                }

                break;
            case var _ when msgTxt.StartsWith("#custom-role:"):

                gptRoleSystemMessage = msgTxt[13..];
                aiSession.SystemMessage = gptRoleSystemMessage;
                await sendMessageCallback.Invoke($"自定义角色已更新");
                aiSession.ChatHistory.Clear();

                break;
            case var _ when msgTxt.StartsWith("#history"):

                await sendMessageCallback.Invoke($"历史记录：{aiSession.ChatHistory.Count}条");
                var inWhiteList = appConfig.AccountWhiteList.Contains(userId);
                if (!inWhiteList)
                    await sendMessageCallback.Invoke($"(您的聊天会话最多保留 {MaxHistoryCount} 条消息)");

                break;
            default:
                return false;
        }

        return true;
    }

}