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
}