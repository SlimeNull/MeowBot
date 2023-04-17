using System.Text;
using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;
using RustSharp;

namespace GPTChatBot;
internal static partial class Program
{
    /// <summary>
    /// 当在群组中收到信息时调用
    /// </summary>
    /// <param name="context">群消息上下文</param>
    /// <param name="aiSessionStorages">所有用户的OpenAI会话上下文存储信息</param>
    /// <param name="appConfig">应用程序设置</param>
    /// <param name="session">QQBot的Socket会话</param>
    private static async Task OnGroupMessageReceived(CqGroupMessagePostContext context, Dictionary<long, AiCompletionSessionStorage> aiSessionStorages, AppConfig appConfig, CqWsSession session)
    {
        // 仅在自己被@的时候做出反应
        if (!context.Message.Any(msg => msg is CqAtMsg atMsg && atMsg.Target == context.SelfId))
        {
            return;
        }

        // 获得当前消息文本
        var msgTxt = context.Message.Text.Trim();

        // 为新用户创建OpenAI会话
        if (!aiSessionStorages.TryGetValue(context.UserId, out var userAiSessionStorage))
        {
            aiSessionStorages[context.UserId] = userAiSessionStorage = new(
                new OpenAiChatCompletionSession
                (
                    appConfig.OpenAiApiKey,
                    string.IsNullOrWhiteSpace(appConfig.ChatCompletionApiUrl) ? AppConfig.DefaultChatCompletionApiUrl : appConfig.ChatCompletionApiUrl,
                    string.IsNullOrWhiteSpace(appConfig.GptModel) ? AppConfig.DefaultGptModel : appConfig.GptModel,
                    string.IsNullOrWhiteSpace(appConfig.GptRoleInitText) ? AppConfig.DefaultGptRoleText : appConfig.GptRoleInitText,
                    appConfig
                )
            );

            await Console.Out.WriteLineAsync($"> 为用户 {context.Sender.Nickname}({context.UserId}) 创建OpenAI会话信息");
        }

        // 用户流量管理
        if (!appConfig.AccountWhiteList.Contains(context.UserId) && userAiSessionStorage.GetUsageCountInLastDuration(TimeSpan.FromSeconds(appConfig.UsageLimitTime)) >= appConfig.UsageLimitCount)
        {
            var helpText = $"(你不在机器人白名单内, {appConfig.UsageLimitTime}秒内仅允许使用{appConfig.UsageLimitCount}次.)";
            await session.SendGroupMessageAsync(context.GroupId, new()
            {
                new CqAtMsg(context.UserId),
                new CqTextMsg(helpText)
            });
            await Console.Out.WriteLineAsync($"> 已拒绝用户 {context.Sender.Nickname}({context.UserId}) 的OpenAI会话请求（流量管理）");
        }
        // 用户命令处理 & GptAPI调用
        else if (!await HandlePotentialUserCommands(msgTxt, context, userAiSessionStorage, session, appConfig))
        {
            var dequeue = userAiSessionStorage.Session.History.Count > MaxHistoryCount && !appConfig.AccountWhiteList.Contains(context.UserId);

            if (dequeue)
            {
                while (userAiSessionStorage.Session.History.Count > MaxHistoryCount)
                {
                    userAiSessionStorage.Session.History.Dequeue();
                }
                await Console.Out.WriteLineAsync($"> 已裁剪用户 {context.Sender.Nickname}({context.UserId}) 的多余对话上下文信息");
            }
            try
            {
                var result = await userAiSessionStorage.Session.AskAsync(context.Message.Text);

                switch (result)
                {
                    case OkResult<string, string> okResult:
                        var message = new CqMessage()
                        {
                            new CqAtMsg(context.UserId),
                            new CqTextMsg(okResult.Value),
                        };

                        if (dequeue) message.WithTail("(已裁剪对话上下文)");

                        await session.SendGroupMessageAsync(context.GroupId, message);

                        userAiSessionStorage.RecordApiUsage();
                        await Console.Out.WriteLineAsync($"> 已回应用户 {context.Sender.Nickname}({context.UserId}) 的OpenAI会话");
                        break;
                    case ErrResult<string, string> errResult:
                        await session.SendGroupMessageAsync(context.GroupId, new()
                        {
                            new CqAtMsg(context.UserId),
                            new CqTextMsg($"请求失败，请重新尝试，你也可以使用 #reset 重置机器人\n{errResult.Value}")
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                await session.SendGroupMessageAsync(context.GroupId, new()
                {
                    new CqAtMsg(context.UserId),
                    new CqTextMsg($"请求失败，请重新尝试，你也可以使用 #reset 重置机器人\n{ex.Data}")
                });

                await Console.Out.WriteLineAsync($"Exception: {ex}");
            }
        }
    }

    /// <summary>
    /// 检查用户输入的文本是否是给定的命令
    /// </summary>
    /// <param name="msgTxt">用户输入的文本</param>
    /// <param name="context">群消息上下文</param>
    /// <param name="aiSession">当前用户的OpenAI会话上下文存储信息</param>
    /// <param name="appConfig">应用程序设置</param>
    /// <param name="session">QQBot的Socket会话</param>
    /// <returns></returns>
    private static async Task<bool> HandlePotentialUserCommands(string msgTxt, CqGroupMessagePostContext context, AiCompletionSessionStorage aiSession, CqWsSession session, AppConfig appConfig)
    {
        if (!msgTxt.StartsWith("#", StringComparison.OrdinalIgnoreCase)) return false;

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
                
                await session.SendGroupMessageAsync(context.GroupId, new()
                {
                    new CqAtMsg(context.UserId),
                    new CqTextMsg(sb.ToString())
                });

                break;
            case var _ when msgTxt.StartsWith("#reset"):

                aiSession.Session.Reset();
                await session.SendGroupMessageAsync(context.GroupId, new()
                {
                    new CqAtMsg(context.UserId),
                    new CqTextMsg("会话已重置")
                });

                break;
            case var _ when msgTxt.StartsWith("#temperature:"):

                var potentialTemperature = msgTxt[13..].Trim();
                if (!float.TryParse(potentialTemperature, out var validFloatValue) || validFloatValue is < 0 or > 1)
                {
                    await session.SendGroupMessageAsync(context.GroupId, new()
                    {
                        new CqAtMsg(context.UserId),
                        new CqTextMsg($"无法将({potentialTemperature})识别为范围在(0 ~ 1)中的浮点数！")
                    }); 
                    break;
                }

                aiSession.Session.UpdateChatBotTemperature(validFloatValue);
                await session.SendGroupMessageAsync(context.GroupId, new()
                {
                    new CqAtMsg(context.UserId),
                    new CqTextMsg($"温度已更新: {validFloatValue:N2}")
                });

                break;
            case var _ when msgTxt.StartsWith("#role:"):

                var role = msgTxt[6..].Trim();
                if (appConfig.BuiltinRoles.TryGetValue(role, out var gptRoleCommand))
                {
                    aiSession.Session.UpdateChatBotRole(gptRoleCommand);
                    await session.SendGroupMessageAsync(context.GroupId, new()
                    {
                        new CqAtMsg(context.UserId),
                        new CqTextMsg($"角色已更新: {role}")
                    });
                }
                else
                {
                    await session.SendGroupMessageAsync(context.GroupId, new()
                    {
                        new CqAtMsg(context.UserId),
                        new CqTextMsg($"找不到所选的角色")
                    });
                }

                break;
            case var _ when msgTxt.StartsWith("#custom-role:"):
                
                gptRoleCommand = msgTxt[13..];
                aiSession.Session.UpdateChatBotRole(gptRoleCommand);
                await session.SendGroupMessageAsync(context.GroupId, new()
                {
                    new CqAtMsg(context.UserId),
                    new CqTextMsg($"角色已更新")
                });
                aiSession.Session.Reset();

                break;
            case var _ when msgTxt.StartsWith("#history"):
                
                var message = new CqMessage()
                {
                    new CqAtMsg(context.UserId),
                    new CqTextMsg($"历史记录：{aiSession.Session.History.Count}条")
                };

                var inWhiteList = appConfig.AccountWhiteList.Contains(context.UserId);

                if (!inWhiteList) message.Add(new CqTextMsg($"(您的聊天会话最多保留 {MaxHistoryCount} 条消息)"));

                await session.SendGroupMessageAsync(context.GroupId, message);
                
                break;
            default:
                return false;
        }
        return true;
    }
}