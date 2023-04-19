using System.Text;
using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;
using RustSharp;

namespace MeowBot;

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

        var cqGroupMessageSender = context.Sender;
        await OnMessageReceived
        (
            context.Message.Text,
            aiSessionStorages,
            appConfig,
            cqGroupMessageSender.UserId,
            cqGroupMessageSender.Nickname,
            messageText => session.SendGroupMessageAsync
            (
                context.GroupId,
                new()
                {
                    new CqAtMsg(context.UserId),
                    new CqTextMsg("\n" + messageText)
                }
            )
        );
    }

    /// <summary>
    /// 当在私聊中收到信息时调用
    /// </summary>
    /// <param name="context">群消息上下文</param>
    /// <param name="aiSessionStorages">所有用户的OpenAI会话上下文存储信息</param>
    /// <param name="appConfig">应用程序设置</param>
    /// <param name="session">QQBot的Socket会话</param>
    private static async Task OnPrivateMessageReceived(CqPrivateMessagePostContext context, Dictionary<long, AiCompletionSessionStorage> aiSessionStorages, AppConfig appConfig, CqWsSession session)
    {
        var cqMessageSender = context.Sender;
        await OnMessageReceived(context.Message.Text,
            aiSessionStorages,
            appConfig,
            cqMessageSender.UserId,
            cqMessageSender.Nickname,
            messageText => session.SendPrivateMessageAsync
            (
                cqMessageSender.UserId,
                new()
                {
                    new CqTextMsg(messageText)
                }
            )
        );
    }
}