using System.Text.Json.Serialization;

namespace Soenneker.SignalR.Dtos.Envelope;

/// <summary>
/// Represents a standardized message envelope used in SignalR communication,
/// containing a type identifier and a JSON-encoded payload.
/// </summary>
public sealed record SignalREnvelope
{
    /// <summary>
    /// Gets or sets the type of the message, used to identify the nature or intent of the payload.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// Gets or sets the serialized payload of the message, typically a JSON string.
    /// This may be <c>null</c> for messages that carry no payload.
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>Stable event identity for durable deduplication. Optional for legacy messages.</summary>
    [JsonPropertyName("messageId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MessageId { get; set; }

    /// <summary>Authorized replay stream identity. Sequence numbers are scoped to this stream.</summary>
    [JsonPropertyName("streamId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StreamId { get; set; }

    /// <summary>Contiguous, server-assigned sequence starting at one. Zero denotes an empty checkpoint.</summary>
    [JsonPropertyName("sequence"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Sequence { get; set; }

    /// <summary>Positive payload schema version. Required by the opt-in replay protocol.</summary>
    [JsonPropertyName("schemaVersion"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SchemaVersion { get; set; }
}