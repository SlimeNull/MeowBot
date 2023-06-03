﻿using EleCho.GoCqHttpSdk;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using MeowBot.Services;
using MeowBot.Services.NewBing;
using MeowBot.Services.OpenAi;

namespace MeowBot;

internal static partial class Program
{
    private delegate bool CheckAppConfigDelegate(AppConfig config, out string? failReason);

    /// <summary>
    /// 应用程序主循环
    /// </summary>
    private static async Task Main()
    {
        if (!TryLoadConfig(out var appConfig))
            return;

        if (string.IsNullOrWhiteSpace(appConfig.BotWebSocketUri))
        {
            Console.WriteLine("请指定机器人 WebSocket URI");
            Utils.PressAnyKeyToContinue();
            return;
        }


        #region 服务初始化

#warning TODO: 可能需要想个方法解耦所有的服务

        if (!OpenAiChatService.CheckConfig(appConfig, out var failReason))
        {
            Console.WriteLine(failReason);
            Utils.PressAnyKeyToContinue();
            return;
        }

        DefaultChatServiceProvider.Register(config => new OpenAiChatService(config));

        if (appConfig.NewBingSupport)
        {
            if (!NewBingChatService.CheckConfig(appConfig, out failReason))
            {
                Console.WriteLine(failReason);
                Utils.PressAnyKeyToContinue();
                return;
            }

            if (appConfig.DefaultToNewBing)
            {
                DefaultChatServiceProvider.Register(config => new NewBingChatService(config));
            }
        }

        #endregion

        #region QQBot初始化

        var session = BuildSession(appConfig);

        #endregion

        // 配置异常处理
        AppDomain.CurrentDomain.UnhandledException += (_, exception) =>
        {
            Console.WriteLine("出现了不可预知的异常");
            if (exception.ExceptionObject is not Exception ex) return;
            Console.WriteLine(ex);
            Console.WriteLine();
        };
        // QQBot生命周期维持
        await MainSession(session, appConfig);
    }

    private static CqWsSession BuildSession(AppConfig appConfig)
    {
        // 初始化QQBot会话
        var session = new CqWsSession(new() { BaseUri = new(appConfig.BotWebSocketUri) });

        // 配置群组消息黑名单拦截
        session.UseGroupMessage(async (context, next) =>
        {
            if (appConfig.AccountBlackList.Contains(context.UserId)) return;
            await next.Invoke();
        });

        // 配置私聊消息白名单过滤
        session.UsePrivateMessage(async (context, next) =>
        {
            if (!appConfig.AccountWhiteList.Contains(context.UserId) &&
                !appConfig.AccountPrivateMessageList.Contains(context.UserId))
                return;
            await next.Invoke();
        });

        var aiSessions = new Dictionary<long, AiCompletionSessionStorage>();

        // 配置群消息处理
        session.UseGroupMessage(async context =>
        {
            try
            {
                await OnGroupMessageReceived(context, aiSessions, appConfig, session);
            }
            catch (Exception ex)
            {
                await Utils.WriteLineColoredAsync($"Exception: {ex}", ConsoleColor.Magenta);
            }
        });

        // 配置私聊消息处理
        session.UsePrivateMessage(async context =>
        {
            try
            {
                await OnPrivateMessageReceived(context, aiSessions, appConfig, session);
            }
            catch (Exception ex)
            {
                await Utils.WriteLineColoredAsync($"Exception: {ex}", ConsoleColor.Magenta);
            }
        });

        // 配置群邀请处理
        session.UseGroupRequest(async context =>
        {
            if (appConfig.AccountWhiteList.Contains(context.UserId))
                await session.ApproveGroupRequestAsync(context.Flag, context.GroupRequestType);
            else
                await session.RejectGroupRequestAsync(context.Flag, context.GroupRequestType, string.Empty);
        });

        // 配置加好友处理
        session.UseFriendRequest(async context =>
        {
            if (appConfig.AccountWhiteList.Contains(context.UserId) || appConfig.AccountPrivateMessageList.Contains(context.UserId))
                await session.ApproveFriendRequestAsync(context.Flag, null);
            else
                await session.RejectFriendRequestAsync(context.Flag);
        });
        return session;
    }

    /// <summary>
    /// 加载应用程序配置信息
    /// </summary>
    /// <param name="appConfig">加载完成的应用程序配置信息</param>
    /// <returns>在加载失败的情况下在应用程序位置生成新的配置信息并输出提示信息，并且返回false</returns>
    private static bool TryLoadConfig([NotNullWhen(true)] out AppConfig? appConfig)
    {
        var serializerOptions = new JsonSerializerOptions()
        {
            TypeInfoResolver = AppConfigJsonSerializerContext.Default,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
            IgnoreReadOnlyProperties = true
        };

        if (!File.Exists(AppConfig.Filename))
        {
            using var fs = File.OpenWrite(AppConfig.Filename);
            JsonSerializer.Serialize(fs, new AppConfig(), serializerOptions);

            Console.WriteLine("配置文件已生成, 请编辑后启动程序");
            Utils.PressAnyKeyToContinue();

            appConfig = null;
            return false;
        }

        using var configFile = File.OpenRead(AppConfig.Filename);
        appConfig = JsonSerializer.Deserialize<AppConfig>(configFile, serializerOptions);

        if (appConfig != null) return true;

        Console.WriteLine("配置文件是空的, 请确认配置文件正确, 或者删除配置文件并重启本程序以重新生成");
        Utils.PressAnyKeyToContinue();
        return false;
    }

    /// <summary>
    /// 启动与Go-CqHttp的Socket会话，并在退出时自动重启
    /// </summary>
    /// <param name="session">当前Go-CqHttp的WebSocket会话</param>
    /// <param name="appConfig">应用程序配置</param>
    private static async Task MainSession(CqWsSession session, AppConfig appConfig)
    {
        while (true)
        {
            const int oneSecondMillisecondsDelay = 1000;
            try
            {
                await session.StartAsync();
                await Console.Out.WriteLineAsync("连接完成");
                await Console.Out.WriteLineAsync($"------------------");
                await Console.Out.WriteLineAsync($"GPT信息：");
                await Console.Out.WriteLineAsync($"\t模型: {appConfig.GptModel ?? AppConfig.DefaultGptModel}");
                await Console.Out.WriteLineAsync($"\tAPI 主机: {appConfig.ApiHost ?? AppConfig.DefaultApiHost}");
                await Console.Out.WriteLineAsync($"\t普通用户的使用频率限制在: {appConfig.UsageLimitTime}秒/{appConfig.UsageLimitCount}次");
                await Console.Out.WriteLineAsync($"------------------");
                await Console.Out.WriteLineAsync($"NewBing支持：{appConfig.NewBingSupport}");
                await Console.Out.WriteLineAsync($"------------------");
                await Console.Out.WriteLineAsync("用户白名单:");
                await Console.Out.WriteLineAsync(string.Join("\n", appConfig.AccountWhiteList.Select(x => $"\t{x}")));
                await Console.Out.WriteLineAsync($"------------------");
                await Console.Out.WriteLineAsync("私聊白名单:");
                await Console.Out.WriteLineAsync(string.Join("\n", appConfig.AccountPrivateMessageList.Select(x => $"\t{x}")));
                await Console.Out.WriteLineAsync($"------------------");
                await Console.Out.WriteLineAsync("用户黑名单:");
                await Console.Out.WriteLineAsync(string.Join("\n", appConfig.AccountBlackList.Select(x => $"\t{x}")));
                await Console.Out.WriteLineAsync($"------------------");
                
                await session.WaitForShutdownAsync();
                session.Dispose();
                session = BuildSession(appConfig);
                
                await Console.Out.WriteLineAsync($"------------------");
                await Console.Out.WriteLineAsync("连接已结束... 5s 后重连");
                await Task.Delay(oneSecondMillisecondsDelay);
                await Console.Out.WriteLineAsync("连接已结束... 4s 后重连");
                await Task.Delay(oneSecondMillisecondsDelay);
                await Console.Out.WriteLineAsync("连接已结束... 3s 后重连");
                await Task.Delay(oneSecondMillisecondsDelay);
                await Console.Out.WriteLineAsync("连接已结束... 2s 后重连");
                await Task.Delay(oneSecondMillisecondsDelay);
                await Console.Out.WriteLineAsync("连接已结束... 1s 后重连");
                await Task.Delay(oneSecondMillisecondsDelay);
                await Console.Out.WriteLineAsync("正在重连...");
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"{ex}");
                await Console.Out.WriteLineAsync("连接已结束... 2s 后重连");
                await Task.Delay(oneSecondMillisecondsDelay);
                await Console.Out.WriteLineAsync("连接已结束... 1s 后重连");
                await Task.Delay(oneSecondMillisecondsDelay);
            }
        }
    }
}