using System.Text.Json.Serialization;

namespace GPTChatBot;

/// <summary>
/// 提供AOT的序列化服务
/// </summary>
[JsonSerializable(typeof(AppConfig))]
[JsonSourceGenerationOptions(WriteIndented = true, IgnoreReadOnlyProperties = true)]
public partial class AppConfigJsonSerializerContext : JsonSerializerContext { }