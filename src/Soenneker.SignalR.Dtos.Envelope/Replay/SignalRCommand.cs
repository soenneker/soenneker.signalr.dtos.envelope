namespace Soenneker.SignalR.Dtos.Envelope.Replay;

/// <summary>A retryable application command. Reuse CommandId for retries of identical content; scope identity by authenticated principal.</summary>
public sealed record SignalRCommand(string CommandId, string Type, int SchemaVersion, string? Payload);
