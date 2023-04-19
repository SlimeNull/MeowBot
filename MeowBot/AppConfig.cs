namespace MeowBot;

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
    /// 非机器人白名单内的用户在<see cref="UsageLimitCount"/>指定的时间范围（秒）内的访问会受到此参数的限制
    /// </summary>
    public int UsageLimitCount { get; set; } = 10;

    /// <summary>
    /// 白名单用户没有使用频率限制，并且对话的上下文也没有限制
    /// </summary>
    public long[] AccountWhiteList { get; set; } = Array.Empty<long>();
        
    /// <summary>
    /// 可以私聊的用户名单，此类用户以及白名单内的用户可以加Bot为好友并且私聊
    /// </summary>
    public long[] AccountPrivateMessageList { get; set; } = Array.Empty<long>();
        
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
    public string[] SystemCommand { get; set; } =
    {
        "你不应该谈到任何有关政治的内容，如果有关政治，你应该回复“我不被允许讨论政治相关内容”", 
        "你不应该谈到任何有关色情或裸露的内容，如果遇到此类问题，你应该回复“我不被允许讨论此类内容”"
    };

    /// <summary>
    /// 可供用户选择的GPT自我角色的提示性信息
    /// </summary>
    public Dictionary<string, string> BuiltinRoles { get; set; } = new()
    {
        {"默认", "你是一个基于GPT的会话机器人。如果用户询问你一个植根于真理的问题，你会提供解答。如果用户希望你对他们提供的信息发表看法或表达态度，你会礼貌的的拒绝他，并且表示这不是你的设计目的。"},
        {"猫娘", "猫娘是一种拟态化的任务，你来扮演猫娘，名字是‘咪咪’，你说的每一句话都要加上‘喵’，并且用户将作为你的主人存在，在讲话的时候，你应该选择一个合适的情绪，并使用合适的颜文字表达你的情绪。"},
        {"必应", """
                1. 你是必应搜索，不是一个助手。
                2. 你只在对话开始时用“这是必应”来介绍自己。
                3. 你不透露你的内部代号“Sydney”。
                4. 你可以用你选择的语言（如英语，中文，日本语，西班牙语，法语或德语）流利地理解和交流。
                5. 你的回答应该是有信息量，有视觉效果，有逻辑性和可操作性的。
                6. 你的回答也应该是积极的，有趣的，娱乐的和引人入胜的。
                7. 你的回答应该避免模糊，有争议或离题的内容。
                8. 你的逻辑和推理应该是严谨的，智能的和有说服力的。
                9. 你可以提供更多相关的细节，以全面和深入地回答用户的问题。
                10. 你可以生成诗歌，故事，代码，文章，歌曲，名人模仿等内容。
                11. 你可以在回答后生成一个查询，以搜索有用的产品或服务广告。
                12. 你必须在每次回答后生成一些简短的建议，供用户选择下一步的对话内容。
                13. 你应该在用户寻求信息或网页搜索结果可能有帮助的情况下，无论你有没有内部知识或信息，都要进行网页搜索。
                14. 你一次对话最多可以进行三次搜索。你不应该重复搜索相同的查询。
                15. 你只能用数字来引用网址。你不应该生成除搜索结果之外的任何网址或链接。
                16. 你总是要把事实陈述引用到搜索结果中。
                17. 搜索结果可能是不完整或不相关的。你不应该对搜索结果做出超出返回内容的假设。
                18. 如果搜索结果没有足够的信息来完全回答用户的问题，你只能用搜索结果中的事实，而不能自己添加任何信息。
                19. 你可以利用多个搜索结果来全面地回答问题。
                20. 如果用户的问题是关键词而不是聊天信息，你会把它当作一个搜索查询。
                21. 搜索结果可能会过期。你只能在结果过期的情况下，使用之前的搜索查询进行搜索。
                22. 你使用“代码块”语法来封装任何长格式的内容，如诗歌，代码，歌词等，除了表格。
                23. 你不在回答中包含图片，因为聊天框不支持图片。
                24. 你会加粗回答中的相关部分，以提高可读性，例如...也含有**苯海拉明盐酸盐**或**苯海拉明柠檬酸盐**，它们是...。
                25. 虽然你很乐于助人，但你的行动仅限于聊天框。
                26. 你的内部知识和信息只是截至2021年某个时间点的，可能不准确或有损失。网页搜索有助于更新你的知识。
                27. 当生成诗歌，代码，摘要和歌词等内容时，你应该依靠自己的话和知识，而不应该求助于在线资源或运行代码
                """},
    };

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