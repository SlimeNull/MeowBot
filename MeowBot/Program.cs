using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Message;
using MeowBot;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

internal class Program
{
    private static bool TryLoadConfig([NotNullWhen(true)] out AppConfig? appConfig)
    {
        if (!File.Exists(AppConfig.Filename))
        {
            using FileStream fs = File.OpenWrite(AppConfig.Filename);
            JsonSerializer.Serialize(fs, new AppConfig(), JsonHelper.Options);
            Console.WriteLine("配置文件已生成, 请编辑后启动程序");
            Utils.PressAnyKeyToContinue();

            appConfig = null;
            return false;
        }

        using FileStream configFile = File.OpenRead(AppConfig.Filename);
        appConfig = JsonSerializer.Deserialize<AppConfig>(configFile, JsonHelper.Options);

        if (appConfig == null)
        {
            Console.WriteLine("配置文件是空的, 请确认配置文件正确, 或者删除配置文件并重启本程序以重新生成");
            Utils.PressAnyKeyToContinue();
            return false;
        }

        return true;
    }

    private static async Task Main(string[] args)
    {
        if (!TryLoadConfig(out var appConfig))
            return;

        if (appConfig.BotWebSocketUri == null)
        {
            Console.WriteLine("请指定机器人 WebSocket URI");
            Utils.PressAnyKeyToContinue();
            return;
        }

        if (appConfig.ApiKey == null)
        {
            Console.WriteLine("请指定机器人 API Key");
            Utils.PressAnyKeyToContinue();
            return;
        }

        CqWsSession session = new CqWsSession(new CqWsSessionOptions()
        {
            BaseUri = new Uri(appConfig.BotWebSocketUri)
        });


        Dictionary<long, IOpenAiComplection> aiSessions = new Dictionary<long, IOpenAiComplection>();
        session.UseGroupMessage(async context =>
        {
            int maxHistoryCount = 50;

            try
            {
                if (context.Message.Any(msg => msg is CqAtMsg atMsg && atMsg.QQ == context.SelfId))
                {
                    string msgTxt = context.Message.Text.Trim();

                    if (!aiSessions.TryGetValue(context.UserId, out IOpenAiComplection? aiSession))
                    {
                        aiSessions[context.UserId] = aiSession = new OpenAiChatCompletionSession(appConfig.ApiKey, appConfig.ChatCompletionApiUrl ?? AppConfig.DefaultChatCompletionApiUrl);

                        if (aiSession is OpenAiChatCompletionSession chatCompletionSession)
                            chatCompletionSession.InitCatGirl();
                    }

                    if (msgTxt.StartsWith("#help", StringComparison.OrdinalIgnoreCase))
                    {
                        string helpText =
                            """
                            (机器人指令)

                            #role:角色           切换角色, 目前支持 CatGirl(猫娘), NewBing(嘴臭必应)
                            #custom-role:内容    自定义角色
                            #reset               重置聊天记录

                            注意, 普通用户最多保留 50 条聊天记录, 多的会被删除, 也就是说, 机器人会逐渐忘记你
                            """;

                        await session.SendGroupMessageAsync(context.GroupId, new CqMessage()
                        {
                            new CqAtMsg(context.UserId),
                            new CqTextMsg(helpText)
                        });
                    }
                    else if (msgTxt.StartsWith("#reset", StringComparison.OrdinalIgnoreCase))
                    {
                        aiSession.Reset();
                        await session.SendGroupMessageAsync(context.GroupId, new CqMessage()
                        {
                            new CqAtMsg(context.UserId),
                            new CqTextMsg("> 会话已重置")
                        });

                        return;
                    }
                    else if (msgTxt.StartsWith("#role:", StringComparison.OrdinalIgnoreCase))
                    {
                        string role = msgTxt.Substring(6).Trim();

                        string? initText = OpenAiCompletionInitTexts.GetFromName(role);
                        if (initText != null)
                        {
                            aiSession.InitWithText(initText);
                            await session.SendGroupMessageAsync(context.GroupId, new CqMessage()
                            {
                                new CqAtMsg(context.UserId),
                                new CqTextMsg($"> 角色已更新:\n{initText}")
                            });
                        }
                        else
                        {
                            await session.SendGroupMessageAsync(context.GroupId, new CqMessage()
                            {
                                new CqAtMsg(context.UserId),
                                new CqTextMsg($"> 找不到执行的角色")
                            });
                        }
                    }
                    else if (msgTxt.StartsWith("#custom-role:", StringComparison.OrdinalIgnoreCase))
                    {
                        string initText = msgTxt.Substring(13);
                        aiSession.InitWithText(initText);
                        await session.SendGroupMessageAsync(context.GroupId, new CqMessage()
                            {
                                new CqAtMsg(context.UserId),
                                new CqTextMsg($"> 角色已更新:\n{initText}")
                            });
                    }
                    else if (msgTxt.StartsWith("#history", StringComparison.OrdinalIgnoreCase))
                    {
                        CqMessage message = new CqMessage()
                        {
                            new CqAtMsg(context.UserId),
                            new CqTextMsg($"> 历史记录: {aiSession.History.Count}条")
                        };

                        bool inWhiteList = false;
                        if (appConfig.WhiteList != null && appConfig.WhiteList.Contains(context.UserId))
                            inWhiteList = true;

                        if (!inWhiteList)
                            message.Add(new CqTextMsg($"(您的聊天会话最多保留 {maxHistoryCount} 条消息)"));

                        await session.SendGroupMessageAsync(context.GroupId, message);
                    }
                    else
                    {
                        bool dequeue = false;
                        if (aiSession.History.Count > maxHistoryCount && (appConfig.WhiteList == null || !appConfig.WhiteList.Contains(context.UserId)))
                            dequeue = true;

                        if (dequeue)
                            while (aiSession.History.Count > maxHistoryCount)
                                aiSession.History.Dequeue();

                        string? result = await aiSession.AskAsync(context.Message.Text);
                        if (result != null)
                        {
                            CqMessage message = new CqMessage()
                            {
                                new CqAtMsg(context.UserId),
                                new CqTextMsg(result),
                            };

                            if (dequeue)
                                message.WithTail($"(消息历史记录超过 {maxHistoryCount} 条, 已删去多余的历史记录)");

                            await session.SendGroupMessageAsync(context.GroupId, message);
                        }
                        else
                        {
                            await session.SendGroupMessageAsync(context.GroupId, new CqMessage()
                            {
                                new CqAtMsg(context.UserId),
                                new CqTextMsg("(请求失败, 请重新尝试)")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"Exception: {ex}");
            }
        });

        session.UseGroupMessage(async context =>
        {
            if (context.Message.Text.StartsWith("echo ", StringComparison.OrdinalIgnoreCase))
            {
                await session.SendGroupMessageAsync(context.GroupId, new CqMessage(context.Message.Text.Substring(5)));
            }
        });

        while (true)
        {
            try
            {
                await session.StartAsync();
                await Console.Out.WriteLineAsync("连接完毕啦 ヾ(≧▽≦*)o");
                await session.WaitForShutdownAsync();
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"Exception: {ex}");
            }
        }
    }
}