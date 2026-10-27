namespace Soenneker.SignalR.Dtos.Envelope.Replay;

/// <summary>Authoritative state and its exact stream position, captured consistently by the server.</summary>
public sealed record SignalRReplaySnapshot(string StreamId, long Sequence, int SchemaVersion, string? Payload);
