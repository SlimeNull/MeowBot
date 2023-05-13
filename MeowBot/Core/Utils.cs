using System.Text.Json.Serialization;

namespace MeowBot;

internal static class Utils
{
    // public static readonly HttpClient GlobalHttpClient = new();
    public static void PressAnyKeyToContinue()
    {
        Console.WriteLine("Press any key to continue");
        Console.ReadKey(true);
    }

    public static async Task WriteLineColoredAsync(string text, ConsoleColor color)
    {
        var tempColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        await Console.Out.WriteLineAsync(text);
        Console.ForegroundColor = tempColor;
    }
}

/// <summary>
/// 提供AOT的序列化服务
/// </summary>
[JsonSerializable(typeof(AppConfig))]
[JsonSourceGenerationOptions(WriteIndented = true, IgnoreReadOnlyProperties = true)]
internal partial class AppConfigJsonSerializerContext : JsonSerializerContext { }