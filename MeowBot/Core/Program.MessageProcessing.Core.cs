using System.Diagnostics;
using MeowBot.Services;
using MeowBot.Services.NewBing;
using MeowBot.Services.OpenAi;

namespace MeowBot;

internal static partial class Program
{
    /// <summary>
    /// 处理收到的消息
    /// </summary>
    /// <param name="incomingMessage">用户通过群组或私聊向Bot发送的消息</param>
    /// <param name="aiSessionStorages">所有用户的AI会话服务会话上下文存储信息</param>
    /// <param name="appConfig">应用程序设置</param>
    /// <param name="userId">发送消息的用户的ID</param>
    /// <param name="userNickname">发送消息的用户的昵称</param>
    /// <param name="sendMessageCallback">回复消息的回调(信息，是否要At用户)</param>
    private static async Task OnMessageReceived(string incomingMessage, IDictionary<long, AiCompletionSessionStorage> aiSessionStorages, AppConfig appConfig, long userId, string userNickname, Func<string, bool, Task> sendMessageCallback)
    {
        // 获得当前消息文本
        var msgTxt = incomingMessage.Trim();

        // 为新用户创建会话
        if (!aiSessionStorages.TryGetValue(userId, out var aiSession))
        {
            var aiChatServiceProductionResult = await DefaultChatServiceProvider.Instantiate(appConfig);
            if (aiChatServiceProductionResult.IsErr)
            {
                await sendMessageCallback($"创建AI会话时产生错误：{aiChatServiceProductionResult.UnwrapErr().Message}", true);
                return;
            }

            var aiChatService = aiChatServiceProductionResult.Unwrap();
            aiSessionStorages[userId] = aiSession = new(aiChatService);

            await Console.Out.WriteLineAsync($"> 为用户 {userNickname}({userId}) 创建会话信息");
        }

        // 用户流量管理
        if (!appConfig.AccountWhiteList.Contains(userId) && aiSession.GetUsageCountInLastDuration(TimeSpan.FromSeconds(appConfig.UsageLimitTime)) >= appConfig.UsageLimitCount)
        {
            var helpText = $"(你不在白名单内, {appConfig.UsageLimitTime}秒内仅允许使用{appConfig.UsageLimitCount}次.)";
            await sendMessageCallback.Invoke(helpText, true);
            await Console.Out.WriteLineAsync($"> 已拒绝用户 {userNickname}({userId}) 的AI会话请求（流量管理）");
        }
        // 用户命令处理 & API调用
        else if (!await HandlePotentialUserCommands(msgTxt, aiSession, appConfig, userId, sendMessageCallback))
        {
            await Console.Out.WriteLineAsync($"> 正在回应用户 {userNickname}({userId}) 的会话：{msgTxt}");

            aiSession.RecordApiUsage();
            
            try
            {
                var message = await aiSession.Service.AskAsync(new(msgTxt, userNickname, userId), sendMessageCallback);
                if (message == null)
                {
                    await Console.Out.WriteLineAsync($"> 已回应用户 {userNickname}({userId}) 的会话");
                    Debug.WriteLine("GoCqHttp OK");
                }
                else
                {
                    throw message;
                }
            }
            catch (Exception ex)
            {
                await sendMessageCallback.Invoke($"在执行请求时发生意外错误，请重新尝试或使用 #reset 重置机器人\n!> {ex.Message}", true);
                await Utils.WriteLineColoredAsync($"Exception: {ex}", ConsoleColor.Magenta);
            }
        }
    }


    /// <summary>
    /// 检查用户输入的文本是否是给定的命令，并且回复对应的命令
    /// </summary>
    /// <param name="msgTxt">用户输入的文本</param>
    /// <param name="aiSession">当前用户的AI会话上下文存储信息</param>
    /// <param name="appConfig">应用程序设置</param>
    /// <param name="userId">发送消息的用户ID</param>
    /// <param name="sendMessageCallback">执行消息发送动作的回调</param>
    /// <returns>用户输入的文本是否被作为命令成功处理？</returns>
    private static async Task<bool> HandlePotentialUserCommands(string msgTxt,
        AiCompletionSessionStorage aiSession,
        AppConfig appConfig,
        long userId,
        Func<string, bool, Task> sendMessageCallback)
    {
        if (!msgTxt.StartsWith("#", StringComparison.OrdinalIgnoreCase))
            return false;


#warning TODO: 可能需要想个方法解耦所有的服务
        if (appConfig.NewBingSupport)
        {
            switch (msgTxt)
            {
                case "#help":
                    await sendMessageCallback($"""
                        系统 操作指令：
                        ----------------------------------
                        #chat:NewBing <切换到基于微软的NewBing对话/查询服务>
                        #chat:GPT <切换到基于OpenAi的GPT对话服务>
                        """, true);
                    break;
                case "#chat:NewBing":
                    aiSession.Service = new NewBingChatService(appConfig);
                    await aiSession.Service.StartServiceAsync();
                    await sendMessageCallback($"聊天服务已切换到：NewBing", true);
                    return true;
                case "#chat:GPT":
                    aiSession.Service = new OpenAiChatService(appConfig);
                    await aiSession.Service.StartServiceAsync();
                    await sendMessageCallback($"聊天服务已切换到：GPT", true);
                    return true;
            }
        }


        var isProcessed = await aiSession.Service.HandlePotentialUserCommands(msgTxt, appConfig, userId, sendMessageCallback);

        if (!isProcessed)
        {
            await sendMessageCallback($"{msgTxt}\n不是一个有效的命令", true);
        }
        
        return true;
    }

}