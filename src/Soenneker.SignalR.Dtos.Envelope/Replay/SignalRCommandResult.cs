namespace Soenneker.SignalR.Dtos.Envelope.Replay;

/// <summary>The durably recorded outcome returned for both initial execution and retries of a command.</summary>
public sealed record SignalRCommandResult(string CommandId, int SchemaVersion, string? Payload);
