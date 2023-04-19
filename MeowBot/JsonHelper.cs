using System.Text.Json.Serialization;

namespace MeowBot;

/// <summary>
/// 提供AOT的序列化服务
/// </summary>
[JsonSerializable(typeof(AppConfig))]
[JsonSourceGenerationOptions(WriteIndented = true, IgnoreReadOnlyProperties = true)]
internal partial class AppConfigJsonSerializerContext : JsonSerializerContext { }