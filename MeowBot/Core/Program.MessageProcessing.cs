using System.Collections.Concurrent;
using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;

namespace MeowBot;

internal static partial class Program
{
    /// <summary>
    /// 用于存储聊天异步应答服务的字典
    /// </summary>
    private static readonly ConcurrentDictionary<long, TaskChain> UserMessageProcessingQueue = new();

    private static void QueueMessage(long senderId, Func<Task> taskFactory)
    {
        var queue = UserMessageProcessingQueue.GetOrAdd(senderId, _ => new());
        queue.Enqueue(taskFactory);
    }


    /// <summary>
    /// 当在群组中收到信息时调用
    /// </summary>
    /// <param name="context">群消息上下文</param>
    /// <param name="aiSessionStorages">所有用户的AI会话服务会话上下文存储信息</param>
    /// <param name="appConfig">应用程序设置</param>
    /// <param name="session">QQBot的Socket会话</param>
    private static async Task OnGroupMessageReceived(CqGroupMessagePostContext context, IDictionary<long, AiCompletionSessionStorage> aiSessionStorages, AppConfig appConfig, CqWsSession session)
    {
        // 仅在自己被@的时候做出反应
        if (!context.Message.Any(msg => msg is CqAtMsg atMsg && atMsg.Target == context.SelfId))
        {
            return;// Task.CompletedTask;
        }

        var cqGroupMessageSender = context.Sender;

        // QueueMessage(cqGroupMessageSender.UserId, () =>
            await OnMessageReceived
            (
                context.Message.Text,
                aiSessionStorages,
                appConfig,
                cqGroupMessageSender.UserId,
                cqGroupMessageSender.Nickname,
                (messageText, atUser) =>
                {
                    if (atUser)
                    {
                        return session.SendGroupMessageAsync(context.GroupId,
                            new()
                            {
                                new CqAtMsg(context.UserId),
                                new CqTextMsg("\n" + messageText)
                            });
                    }

                    return session.SendGroupMessageAsync(context.GroupId,
                        new()
                        {
                            new CqTextMsg(messageText)
                        });
                }
            );
            // );
            // return Task.CompletedTask;
    }

    /// <summary>
    /// 当在私聊中收到信息时调用
    /// </summary>
    /// <param name="context">群消息上下文</param>
    /// <param name="aiSessionStorages">所有用户的AI会话服务会话上下文存储信息</param>
    /// <param name="appConfig">应用程序设置</param>
    /// <param name="session">QQBot的Socket会话</param>
    private static async Task OnPrivateMessageReceived(CqPrivateMessagePostContext context, IDictionary<long, AiCompletionSessionStorage> aiSessionStorages, AppConfig appConfig, CqWsSession session)
    {
        var cqMessageSender = context.Sender;

        // QueueMessage(cqMessageSender.UserId, () =>
            await OnMessageReceived(context.Message.Text,
                aiSessionStorages,
                appConfig,
                cqMessageSender.UserId,
                cqMessageSender.Nickname,
                (messageText, _) => session.SendPrivateMessageAsync
                (
                    cqMessageSender.UserId,
                    new()
                    {
                        new CqTextMsg(messageText)
                    }
                )
            );
        // );

        // return Task.CompletedTask;
    }
}

public class TaskChain
{
    private readonly Queue<Func<Task>> m_Chain = new();
    private bool m_IsExecutingTask;

    public void Enqueue(Func<Task> taskFactory)
    {
        m_Chain.Enqueue(taskFactory);
        if (!m_IsExecutingTask)
        {
            _ = ExecuteTaskChain();
        }
    }

    private async Task ExecuteTaskChain()
    {
        m_IsExecutingTask = true;
        while (m_Chain.TryDequeue(out var taskFactory))
        {
            try
            {
                await taskFactory();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        m_IsExecutingTask = false;
    }
}