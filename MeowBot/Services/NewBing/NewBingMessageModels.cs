using System.Runtime.Serialization;
using System.Text.Json.Serialization;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable StringLiteralTypo
#pragma warning disable CS8618

namespace MeowBot.Services.NewBing;

[JsonSerializable(typeof(MessageResponse))]
public partial class MessageResponseJsonSerializerContext : JsonSerializerContext
{
}

[DataContract]
public class MessageResponse
{
    [JsonPropertyName("requestId")] public Guid RequestId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("throttling")]
    public Throttling? Throttling { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("messages")]
    public Message[]? Messages { get; set; }
}

[DataContract]
public class Message
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("hiddenText")]
    public string? HiddenText { get; set; }

    [JsonPropertyName("author")] public string Author { get; set; }

    [JsonPropertyName("createdAt")] public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("timestamp")] public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("messageId")] public Guid MessageId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("messageType")]
    public string MessageType { get; set; }

    [JsonPropertyName("offense")] public string Offense { get; set; }

    [JsonPropertyName("adaptiveCards")] public AdaptiveCard[] AdaptiveCards { get; set; }

    [JsonPropertyName("feedback")] public Feedback Feedback { get; set; }

    [JsonPropertyName("contentOrigin")] public string ContentOrigin { get; set; }

    [JsonPropertyName("privacy")] public object Privacy { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("spokenText")]
    public string SpokenText { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("sourceAttributions")]
    public SourceAttribution[]? SourceAttributions { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("suggestedResponses")]
    public SuggestedResponse[]? SuggestedResponses { get; set; }
}

[DataContract]
public class AdaptiveCard
{
    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("version")] public string Version { get; set; }

    [JsonPropertyName("body")] public Body[] Body { get; set; }
}

[DataContract]
public class Body
{
    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("inlines")]
    public Inline[] Inlines { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("wrap")]
    public bool? Wrap { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("size")]
    public string Size { get; set; }
}

[DataContract]
public class Inline
{
    [JsonPropertyName("type")] public string Type { get; set; }

    [JsonPropertyName("isSubtle")] public bool IsSubtle { get; set; }

    [JsonPropertyName("italic")] public bool Italic { get; set; }

    [JsonPropertyName("text")] public string Text { get; set; }
}

[DataContract]
public class Feedback
{
    [JsonPropertyName("tag")] public object Tag { get; set; }

    [JsonPropertyName("updatedOn")] public object UpdatedOn { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; }
}

[DataContract]
public class SourceAttribution
{
    [JsonPropertyName("providerDisplayName")]
    public string ProviderDisplayName { get; set; }

    [JsonPropertyName("seeMoreUrl")] public Uri SeeMoreUrl { get; set; }

    [JsonPropertyName("searchQuery")] public string SearchQuery { get; set; }
}

[DataContract]
public class SuggestedResponse
{
    [JsonPropertyName("text")] public string Text { get; set; }

    [JsonPropertyName("author")] public string Author { get; set; }

    [JsonPropertyName("createdAt")] public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("timestamp")] public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("messageId")] public Guid MessageId { get; set; }

    [JsonPropertyName("messageType")] public string MessageType { get; set; }

    [JsonPropertyName("offense")] public string Offense { get; set; }

    [JsonPropertyName("feedback")] public Feedback Feedback { get; set; }

    [JsonPropertyName("contentOrigin")] public string ContentOrigin { get; set; }

    [JsonPropertyName("privacy")] public object Privacy { get; set; }
}

[DataContract]
public class Throttling
{
    [JsonPropertyName("maxNumUserMessagesInConversation")]
    public uint MaxNumUserMessagesInConversation { get; set; }

    [JsonPropertyName("numUserMessagesInConversation")]
    public uint NumUserMessagesInConversation { get; set; }
}

public class BingChatConversationCreationModel
{
    [JsonPropertyName("conversationId")] public string ConversationId { get; set; }

    [JsonPropertyName("clientId")] public string ClientId { get; set; }

    [JsonPropertyName("conversationSignature")]
    public string ConversationSignature { get; set; }

    [JsonPropertyName("result")] public ResultInfo Result { get; set; }

    public DateTime CreatedDateTime { get; }

    [JsonConstructor]
    public BingChatConversationCreationModel(string conversationId, string clientId, string conversationSignature, ResultInfo result)
    {
        CreatedDateTime = DateTime.Now;
        ConversationId = conversationId;
        ClientId = clientId;
        ConversationSignature = conversationSignature;
        Result = result;
    }

    public void Deconstruct(out string conversationId, out string clientId, out string conversationSignature)
    {
        conversationId = ConversationId;
        clientId = ClientId;
        conversationSignature = ConversationSignature;
    }

    public class ResultInfo
    {
        [JsonPropertyName("value")] public string Value { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; }

        [JsonConstructor]
        public ResultInfo(string value, string message)
        {
            Value = value;
            Message = message;
        }
    }
}

public class BingChatCommand
{
    public enum BingStyle
    {
        Imaginative,
        Balanced,
        Precise
    }

    [JsonPropertyName("source")] public string Source { get; set; }
    [JsonPropertyName("optionSets")] public string[] OptionSets { get; set; }

    [JsonPropertyName("allowedMessageTypes")]
    public string[] AllowedMessageTypes { get; set; }

    [JsonPropertyName("sliceIds")] public string[] SliceIds { get; set; }
    [JsonPropertyName("traceId")] public string TraceId { get; set; }
    [JsonPropertyName("isStartOfSession")] public bool IsStartOfSession { get; set; }
    [JsonPropertyName("message")] public BingChatMessage Message { get; set; }

    [JsonPropertyName("conversationSignature")]
    public string ConversationSignature { get; set; }

    [JsonPropertyName("participant")] public BingChatParticipant Participant { get; set; }
    [JsonPropertyName("conversationId")] public string ConversationId { get; set; }

    public BingChatCommand(string traceId, bool isStartOfSession, BingChatMessage message, string conversationSignature, BingChatParticipant participant, string conversationId, BingStyle style)
    {
        TraceId = traceId;
        IsStartOfSession = isStartOfSession;
        Message = message;
        ConversationSignature = conversationSignature;
        Participant = participant;
        ConversationId = conversationId;
        Source = "cib";
        switch (style)
        {
            case BingStyle.Imaginative:
                OptionSets = new[]
                {
                    "nlu_direct_response_filter",
                    "deepleo",
                    "disable_emoji_spoken_text",
                    "responsible_ai_policy_235",
                    "enablemm",
                    "h3imaginative",
                    "dv3sugg",
                    "clgalileo",
                    "gencontentv3"
                };
                break;
            case BingStyle.Balanced:
                OptionSets = new[]
                {
                    "nlu_direct_response_filter",
                    "deepleo",
                    "disable_emoji_spoken_text",
                    "responsible_ai_policy_235",
                    "enablemm",
                    "galileo",
                    "dv3sugg"
                };
                break;
            case BingStyle.Precise:
                OptionSets = new[]
                {
                    "nlu_direct_response_filter",
                    "deepleo",
                    "disable_emoji_spoken_text",
                    "responsible_ai_policy_235",
                    "enablemm",
                    "h3precise",
                    "dv3sugg",
                    "clgalileo",
                    "gencontentv3"
                };
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(style), style, null);
        }

        AllowedMessageTypes = new[]
        {
            "ActionRequest",
            "Chat",
            "Context",
            "InternalSearchQuery",
            "InternalSearchResult",
            "Disengaged",
            "InternalLoaderMessage",
            "Progress",
            "RenderCardRequest",
            "AdsQuery",
            "SemanticSerp",
            "GenerateContentQuery",
            "SearchQuery"
        };
    }
}

public class BingChatMessage
{
    [JsonPropertyName("locale")] public string Locale { get; set; }
    [JsonPropertyName("market")] public string Market { get; set; }
    [JsonPropertyName("region")] public string Region { get; set; }
    [JsonPropertyName("location")] public string Location { get; set; }
    [JsonPropertyName("author")] public string Author { get; set; }
    [JsonPropertyName("inputMethod")] public string InputMethod { get; set; }
    [JsonPropertyName("messageType")] public string MessageType { get; set; }
    [JsonPropertyName("text")] public string Text { get; set; }

    [JsonConstructor]
    public BingChatMessage(string locale, string market, string region, string location, string messageType, string text)
    {
        Author = "user";
        InputMethod = "Keyboard";
        Locale = locale;
        Market = market;
        Region = region;
        Location = location;
        MessageType = messageType;
        Text = text;
    }
}

public class BingChatParticipant
{
    [JsonInclude] public string Id { get; }

    public BingChatParticipant(string id)
    {
        this.Id = id;
    }
}