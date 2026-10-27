namespace Soenneker.SignalR.Dtos.Envelope.Replay;

/// <summary>A durable consumer position for one authorized stream. Store it atomically with application state.</summary>
public sealed record SignalRReplayCheckpoint(string StreamId, long Sequence);
