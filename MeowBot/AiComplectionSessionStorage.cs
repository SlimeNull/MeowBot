using MeowBot;

class AiComplectionSessionStorage
{
    public AiComplectionSessionStorage(IOpenAiComplection Session)
    {
        this.Session = Session;
        this.UsageHistory = new Queue<DateTime>();
    }

    public IOpenAiComplection Session { get; }
    public Queue<DateTime> UsageHistory { get; set; }

    public void AddUsage()
    {
        UsageHistory.Enqueue(DateTime.Now);
    }

    public void EnsureUsageHistoryCapacity(int capcity)
    {
        while (UsageHistory.Count > capcity)
            UsageHistory.Dequeue();
    }

    public int GetUsageCountInLastDuration(TimeSpan duration)
    {
        DateTime end = DateTime.Now;
        DateTime start = end - duration;

        int count = 0;
        foreach (DateTime time in UsageHistory)
        {
            if (time >= start && time <= end)
                count++;
        }

        return count;
    }
}