using System.Globalization;
using RustSharp;
using System.Net.Http.Json;

namespace MeowBot;

/// <summary>
/// 一个逐用户的OpenAI对话，用于为一位用户提供包含上下文的GPT对话服务
/// </summary>
internal partial class OpenAiChatCompletionSession : IOpenAiCompletion
{
    /// <summary>
    /// 描述用户当前使用的GPT角色提示信息
    /// </summary>
    private string m_RoleText;
    
    /// <summary>
    /// 描述用户当前设定的GPT温度信息
    /// </summary>
    private float m_Temperature;
    
    private string? m_DavinciRole;

    private readonly AppConfig m_AppConfig;
    
    /// <summary>
    /// 用户和GPT的对话历史上下文信息
    /// </summary>
    private readonly Queue<KeyValuePair<string, string>> m_DialogHistory = new();

    private readonly string m_ApiKey;
    private readonly string m_ApiUrl;
    private readonly string m_Model;

    /// <summary>
    /// 用户和GPT的对话历史上下文信息
    /// </summary>

    public Queue<KeyValuePair<string, string>> History => m_DialogHistory;

    public OpenAiChatCompletionSession(string apiKey, string apiUrl, string model, string roleText, AppConfig appConfig)
    {
        m_ApiKey = apiKey;
        m_ApiUrl = apiUrl;
        m_Model = model;
        m_RoleText = roleText;
        m_AppConfig = appConfig;
        m_Temperature = 0.5f;
    }

    /// <summary>
    /// 更新当前用户使用的GPT温度信息
    /// </summary>
    /// <param name="roleText">新的GPT温度信息</param>
    public void UpdateChatBotRole(string roleText) => m_RoleText = roleText;

    /// <summary>
    /// 更新当前用户使用的GPT角色提示信息
    /// </summary>
    /// <param name="newTemperature"></param>
    public void UpdateChatBotTemperature(float newTemperature) => m_Temperature = newTemperature;
    
    /// <summary>
    /// 调用OpenAI服务器并且返回结果
    /// </summary>
    /// <param name="question">用户输入的文本</param>
    /// <returns>结果</returns>
    public async Task<Result<string, string>> AskAsync(string question)
    {
        var messageModels = new List<object>
        {
            new
            {
                role = "system",
                content = m_RoleText
            },
            new
            {
                role = "system",
                content = $"当前时间为: {DateTime.Now:U}，当用户想你询问涉及到当前时间的情况时，优先将此时间转换为{CultureInfo.CurrentCulture.DisplayName}区域的当地时间并且使用。"
            }
        };

        foreach (var systemCommand in m_AppConfig.SystemCommand)
        {
            messageModels.Add(new { role = "system", content = systemCommand });
        }

        foreach (var kv in m_DialogHistory)
        {
            messageModels.Add(new
            {
                role = "user",
                content = kv.Key,
            });

            messageModels.Add(new
            {
                role = m_DavinciRole ?? "assistant",
                content = kv.Value
            });
        }

        messageModels.Add(new
        {
            role = "user",
            content = question
        });


        var request =
            new HttpRequestMessage(HttpMethod.Post, m_ApiUrl)
            {
                Headers =
                {
                    { "Authorization", $"Bearer {m_ApiKey}" }
                },

                Content = JsonContent.Create(
                    new
                    {
                        model = m_Model,
                        messages = messageModels,
                        max_tokens = 2048,
                        temperature = m_Temperature,
                    }),
            };

        var response = await Utils.GlobalHttpClient.SendAsync(request);
        var davinciRst = await response.Content.ReadFromJsonAsync<davinci_result>();
        
        if (davinciRst == null) return Result<string, string>.Err("API 无返回");
        if (davinciRst.error != null) return Result<string, string>.Err($"API 返回错误: {davinciRst.error.message}");
        if (davinciRst.choices == null) return Result<string, string>.Err("API 响应无结果");
        
        var davinciRstMessage = davinciRst.choices.FirstOrDefault()?.message;
        
        if (davinciRstMessage == null) return Result<string, string>.Err("API 响应结果无内容");
        
        m_DavinciRole = davinciRstMessage.role;
        m_DialogHistory.Enqueue(new(question, davinciRstMessage.content));
        await Console.Out.WriteLineAsync($"> \tOpen AI API 已回应，消耗({davinciRst.usage?.total_tokens})token");
        return Result<string, string>.Ok(davinciRstMessage.content);
    }

    public void Reset() => m_DialogHistory.Clear();
}