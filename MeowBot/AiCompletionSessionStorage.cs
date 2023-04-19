using MeowBot;

/// <summary>
/// 用于存储用户的OpenAi对话信息的数据结构
/// </summary>
internal class AiCompletionSessionStorage
{
    public AiCompletionSessionStorage(IOpenAiCompletion session)
    {
        Session = session;
        UsageHistory = new();
    }

    /// <summary>
    /// 用户的OpenAi对话上下文
    /// </summary>
    public IOpenAiCompletion Session { get; }
    /// <summary>
    /// 存储用户的上一次访问时间，以及访问频率
    /// </summary>
    private Queue<DateTime> UsageHistory { get; }

    /// <summary>
    /// 记录用户在当前时间点使用了该API
    /// </summary>
    public void RecordApiUsage()
    {
        UsageHistory.Enqueue(DateTime.Now);
    }

    /// <summary>
    /// 裁剪掉早于给定时间点的API
    /// </summary>
    /// <param name="start">丢弃用户在此时间段之前的访问计数</param>
    private void Trim(in DateTime start)
    {
        while (UsageHistory.Count > 0 && UsageHistory.Peek() < start)
        {
            UsageHistory.Dequeue();
        }
    }

    /// <summary>
    /// 获得用户在给定时间段（当前时间-<paramref name="duration"/>>到当前之间）之间访问OpenAiAPI的总计数
    /// </summary>
    /// <param name="duration">时间段</param>
    /// <returns>用户在给定时间段之间访问OpenAiAPI的总计数</returns>
    public int GetUsageCountInLastDuration(TimeSpan duration)
    {
        var end = DateTime.Now;
        var start = end - duration;
        Trim(start);
        return UsageHistory.Count(time => time >= start && time <= end);
    }
}