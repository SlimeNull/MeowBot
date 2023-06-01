using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace MeowBot.Services.NewBing;

internal class NewBingChatService : AiChatServiceBase
{
    private readonly HubConnection m_HubConnection;
    private readonly HttpClient m_HttpClient = new();
    private readonly string m_NewBingCookie;
    private readonly byte[] m_TraceIdBuffer = new byte[16];

    private readonly JsonSerializerOptions m_MessageResponseSerializerOptions = new()
    {
        TypeInfoResolver = MessageResponseJsonSerializerContext.Default
    };

    private string? m_ConversationId;
    private string? m_ClientId;
    private string? m_ConversationSignature;
    private bool m_IsStartOfSession;
    private bool m_HasBingStartTyping;
    private bool m_HasBingSpoken;
    private uint m_ChatRound;
    private uint m_MaxChatRound;
    private bool m_Disconnected;
    private MessageResponse? m_LastBingResponse;
    private Func<string, bool, Task>? m_SendMessageCallback;
    private Exception? m_PendingException;
    private BingChatCommand.BingStyle m_BingStyle;

    public static bool CheckConfig(AppConfig config, out string? reason)
    {
        if (string.IsNullOrWhiteSpace(config.NewBingCookie))
        {
            reason = $"请指定NewBing Cookie({nameof(config.NewBingCookie)}, 'U_'开头的Cookie)";
            return false;
        }

        reason = null;
        return true;
    }

    public NewBingChatService(AppConfig config) : base(config)
    {
        m_NewBingCookie = config.NewBingCookie;
        m_BingStyle = BingChatCommand.BingStyle.Imaginative;
        m_HubConnection = new HubConnectionBuilder()
            .WithUrl("https://sydney.bing.com/sydney/ChatHub",
                HttpTransportType.WebSockets,
                options => { options.SkipNegotiation = true; })
            .Build();
        m_HubConnection.On<JsonDocument>("update", OnChatHubMessageUpdate);
    }

    private void OnChatHubMessageUpdate(JsonDocument jsonDocument)
    {
        try
        {
            m_LastBingResponse = jsonDocument.Deserialize<MessageResponse>(m_MessageResponseSerializerOptions);
        }
        catch (Exception e)
        {
            m_PendingException = new InvalidOperationException("无法解析NewBing的消息信息", e);
            return;
        }

        if (m_LastBingResponse == null)
        {
            m_PendingException = new NullReferenceException("无法解析NewBing的消息信息");
            return;
        }

        if (m_SendMessageCallback == null)
        {
            m_PendingException = new InvalidOperationException("内部流程错误（未提供发送信息的途径）");
            return;
        }

        if (m_LastBingResponse.Throttling != null)
        {
            m_ChatRound = m_LastBingResponse.Throttling.NumUserMessagesInConversation;
            m_MaxChatRound = m_LastBingResponse.Throttling.MaxNumUserMessagesInConversation;
        }

        if (m_LastBingResponse.Messages == null || m_LastBingResponse.Messages.Length == 0) return;

        var botMessage = m_LastBingResponse.Messages[0];

        if (!string.IsNullOrWhiteSpace(botMessage.SpokenText) && !m_HasBingSpoken)
        {
            m_HasBingSpoken = true;
            m_SendMessageCallback(botMessage.SpokenText, false);
            return;
        }

        if (!string.IsNullOrWhiteSpace(botMessage.Text) && !m_HasBingStartTyping && !botMessage.Text.Equals("Generating answers for you..."))
        {
            m_HasBingStartTyping = true;
            m_HasBingSpoken = true;
            // m_SendMessageCallback("NewBing 正在输入...", false);
        }
    }

    public override Task StartServiceAsync() => Task.CompletedTask;

    private async Task<Exception?> InitializeConnection()
    {
        if(!m_Disconnected) return null;

        try
        {
            await m_HubConnection.StartAsync();
            m_Disconnected = false;
        }
        catch (Exception e)
        {
            return new InvalidOperationException($"> 无法初始化Bing连接: {e.Message}");
        }

        m_IsStartOfSession = true;
        m_HasBingStartTyping = false;
        m_HasBingSpoken = false;
        m_ChatRound = 0;

        m_HubConnection.Closed += exception =>
        {
            if (exception != null)
            {
                Console.WriteLine("> 远端Bing服务器已关闭连接，下次会话时将进行重连！");
                Console.WriteLine($"> {exception.Message}");
            }
            else
            {
                Console.WriteLine("> Bing服务器已连接已结束，下次会话时将进行重连！");
            }

            m_Disconnected = true;
            return Task.CompletedTask;
        };

        return null;
    }

    internal override async Task<Exception?> AskAsync(AskCommandArgsModel askCommandArgs, Func<string, bool, Task> sendMessageCallback)
    {
        try
        {
            var exception = await InitializeConnection();
            if (exception != null) return exception;
        }
        catch (Exception e)
        {
            return e;
        }

        m_SendMessageCallback = sendMessageCallback;
        if (m_IsStartOfSession)
        {
            var result = await InitConversation();
            if (result != null) return result;
        }

        var param = new BingChatCommand(GenerateTraceId(),
            m_IsStartOfSession,
            new("zh-CN",
                "zh-CN",
                "CN",
                "",
                "Chat",
                askCommandArgs.MsgTxt),
            m_ConversationSignature!,
            new(m_ClientId!),
            m_ConversationId!,
            m_BingStyle);

        m_IsStartOfSession = false;
        m_HasBingSpoken = false;
        m_HasBingStartTyping = false;

        await foreach (var _ in m_HubConnection.StreamAsync<object>("chat", param))
        {
        }

        if (m_PendingException != null)
        {
            m_PendingException = null;
            m_IsStartOfSession = true;
            return m_PendingException;
        }

        if (m_LastBingResponse?.Messages == null || m_LastBingResponse.Messages.Length == 0)
        {
            await sendMessageCallback("> Bing未提供回复，下一次将建立新的对话。", true);
            m_IsStartOfSession = true;
            return null;
        }

        var message = m_LastBingResponse.Messages[0];
        await sendMessageCallback(FormatBingChatMessage(message, m_ChatRound, m_MaxChatRound), true);
        m_LastBingResponse = null;

        if (message.SuggestedResponses == null || message.SuggestedResponses.Length == 0)
        {
            await sendMessageCallback("> 此轮会话被Bing结束，下一次将建立新的对话。", false);
            m_IsStartOfSession = true;
        }

        return null;
    }

    public override async Task<bool> HandlePotentialUserCommands(string msgTxt, AppConfig appConfig, long userId, Func<string, bool, Task> sendMessageCallback)
    {
        switch (msgTxt)
        {
            case "#help":

                const string newBingHelpText = $"""
                        NewBing 操作指令：
                        ----------------------------------
                        #style:<切换NewBing的应答风格，并重置对话>
                        #reset:重置聊天对话的上下文信息
                        ----------------------------------
                        ！注意, NewBing会对最大对话长度进行限制！
                        ----------------------------------
                        以下是所有可用的NewBing的应答风格：
                            平衡
                            创造
                            精准
                        """;

                await sendMessageCallback.Invoke(newBingHelpText, false);
                break;
            case var _ when msgTxt.StartsWith("#style:"):
                var style = msgTxt[7..].Trim();
                switch (style)
                {
                    case "平衡":
                        m_BingStyle = BingChatCommand.BingStyle.Balanced;
                        m_IsStartOfSession = true;
                        await sendMessageCallback($"> NewBing: 已更新为 {style} 风格", true);
                        break;
                    case "创造":
                        m_BingStyle = BingChatCommand.BingStyle.Imaginative;
                        m_IsStartOfSession = true;
                        await sendMessageCallback($"> NewBing: 已更新为 {style} 风格", true);
                        break;
                    case "精准":
                        m_BingStyle = BingChatCommand.BingStyle.Precise;
                        m_IsStartOfSession = true;
                        await sendMessageCallback($"> NewBing: 已更新为 {style} 风格", true);
                        break;
                    default:
                        await sendMessageCallback($"> NewBing: 无法找到 {style} 风格", true);
                        break;
                }

                break;
            case "#reset":
                m_IsStartOfSession = true;
                await sendMessageCallback.Invoke("> NewBing: 会话已重置", true);
                break;
            default:
                return false;
        }

        return true;
    }

    public override async ValueTask DisposeAsync()
    {
        m_HttpClient.Dispose();
        await m_HubConnection.DisposeAsync();
    }

    private static string FormatBingChatMessage(Message message, uint chatRound, uint maxChatRound)
    {
        if (string.IsNullOrWhiteSpace(message.Text)) return $"> Bing未提供回答。";

        var responseText = new StringBuilder(message.Text.TrimEnd('\n'));
        responseText.AppendLine();

        if (message.SuggestedResponses != null && message.SuggestedResponses.Length != 0)
        {
            responseText.AppendLine("-----");

            var suggestedResponsesLength = message.SuggestedResponses.Length;
            for (var index = 0; index < suggestedResponsesLength; index++)
            {
                var suggestedResponse = message.SuggestedResponses[index];
                responseText.AppendLine(suggestedResponse.Text);
            }
        }

        if (message.SourceAttributions != null && message.SourceAttributions.Length != 0)
        {
            responseText.AppendLine("-----");

            responseText.Replace("[^", "[");
            responseText.Replace("^]", "]");

            var sourceAttributionsLength = message.SourceAttributions.Length;
            for (var index = 0; index < sourceAttributionsLength; index++)
            {
                var sourceAttribution = message.SourceAttributions[index];
                responseText.Append($"[{index + 1}] ");
                var url = sourceAttribution.SeeMoreUrl.ToString();
                responseText.AppendLine(url);
            }
        }

        responseText.AppendLine("-----");
        responseText.AppendLine($"对话轮次：({chatRound}/{maxChatRound})");

        return responseText.ToString().TrimEnd('\n');
    }

    private async Task<Exception?> InitConversation()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.bing.com/turing/conversation/create");
        request.Headers.Add("Cookie", $"_U={m_NewBingCookie}");

        HttpResponseMessage result;
        try
        {
            result = await m_HttpClient.SendAsync(request);
        }
        catch (Exception e)
        {
            return new InvalidOperationException("无法发送NewBing的会话创建请求", e);
        }

        var conversationCreationJson = await result.Content.ReadAsStringAsync();

        BingChatConversationCreationModel? bingChatConversationCreateModel;
        try
        {
            bingChatConversationCreateModel = JsonSerializer.Deserialize<BingChatConversationCreationModel>(conversationCreationJson);
        }
        catch (Exception e)
        {
            return new InvalidOperationException("无法解析NewBing的会话创建请求", e);
        }

        if (bingChatConversationCreateModel == null)
        {
            return new InvalidOperationException("无法解释BingChat服务器返回的结果");
        }

        if (bingChatConversationCreateModel.Result.Value != "Success")
        {
            return new InvalidOperationException("BingChat服务器拒绝了服务请求");
        }

        (m_ConversationId, m_ClientId, m_ConversationSignature) = bingChatConversationCreateModel;
        return null;
    }

    private string GenerateTraceId()
    {
        Random.Shared.NextBytes(m_TraceIdBuffer);
        return Convert.ToHexString(m_TraceIdBuffer);
    }
}