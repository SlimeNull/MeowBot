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
            try
            {
                if (context.Message.Any(msg => msg is CqAtMsg atMsg && atMsg.QQ == context.SelfId))
                {
                    if (!aiSessions.TryGetValue(context.UserId, out IOpenAiComplection? aiSession))
                        aiSessions[context.UserId] = aiSession = new OpenAiChatCompletionSession(appConfig.ApiKey);

                    if (aiSession is OpenAiChatCompletionSession chatCompletionSession)
                        chatCompletionSession.InitCatGirl();

                    string? result = await aiSession.AskAsync(context.Message.Text);
                    if (result != null)
                    {
                        await session.SendGroupMessageAsync(context.GroupId, new CqMessage()
                        {
                            new CqAtMsg(context.UserId),
                            new CqTextMsg(result)
                        });
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
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"Exception: {ex}");
            }
        });

        while (true)
        {
            try
            {
                await session.StartAsync();
                await Console.Out.WriteLineAsync("连接完毕啦");
                await session.WaitForShutdownAsync();
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"Exception: {ex}");
            }
        }
    }
}