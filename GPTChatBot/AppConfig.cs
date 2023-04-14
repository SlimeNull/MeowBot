using System.Text.Json.Serialization;

namespace GPTChatBot;

/// <summary>
/// 应用程序配置
/// </summary>
internal class AppConfig
{
    public const string Filename = "AppConfig.json";

    /// <summary>
    /// 用于和OpenAI通信的密钥
    /// </summary>
    public string OpenAiApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 用于和Go-CqHttp通信的WebSocketUri
    /// </summary>
    public string BotWebSocketUri { get; set; } = string.Empty;

    /// <summary>
    /// 用于提供对话应答Api的Url
    /// </summary>
    public string? ChatCompletionApiUrl { get; set; }
        
    /// <summary>
    /// 用于产生对话应答的模型名称
    /// </summary>
    public string? GptModel { get; set; }

    /// <summary>
    /// 非机器人白名单内的用户在此时间范围（秒）内的访问会受到<see cref="UsageLimitCount"/>的限制
    /// </summary>
    public int UsageLimitTime { get; set; } = 300;
        
    /// <summary>
    /// 非机器人白名单内的用户在<see cref="UsageLimitCount"/>指定的时间范围（毫秒）内的访问会受到此参数的限制
    /// </summary>
    public int UsageLimitCount { get; set; } = 5;

    /// <summary>
    /// 白名单用户没有使用频率限制，并且对话的上下文也没有限制
    /// </summary>
    public long[] AccountWhiteList { get; set; } = Array.Empty<long>();
        
    /// <summary>
    /// 黑名单用户无法获得任何服务
    /// </summary>
    public long[] AccountBlackList { get; set; } = Array.Empty<long>();

    /// <summary>
    /// 用于初始化GPT自我角色的提示性信息
    /// </summary>
    public string GptRoleInitText { get; set; } = string.Empty;
    
    /// <summary>
    /// 在初始化对话上下文时需要传入的额外提示性信息
    /// </summary>
    public string[] SystemCommand { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 可供用户选择的GPT自我角色的提示性信息
    /// </summary>
    public Dictionary<string, string> BuiltinRoles { get; set; } = new();

    /// <summary>
    /// 在<see cref="GptRoleInitText"/>未提供时，默认使用的用于初始化GPT自我角色的提示性信息
    /// </summary>
    public static string DefaultGptRoleText => "你是一个基于GPT的会话机器人。如果用户询问你一个植根于真理的问题，你会提供解答。如果用户希望你对他们提供的信息发表看法或表达态度，你会礼貌的的拒绝他，并且表示这不是你的设计目的";
    
    /// <summary>
    /// 在<see cref="ChatCompletionApiUrl"/>未提供时，默认使用的用于提供对话应答Api的Url
    /// </summary>
    public static string DefaultChatCompletionApiUrl => "https://api.openai.com/v1/chat/completions";
        
    /// <summary>
    /// 在<see cref="GptModel"/>未提供时，默认使用的的用于产生对话应答的模型名称
    /// </summary>
    public static string DefaultGptModel => "gpt-3.5-turbo";
}