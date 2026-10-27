using System.Collections.Generic;

namespace Soenneker.SignalR.Dtos.Envelope.Replay;

/// <summary>Bounds one synchronization operation and defines understood payload schemas. Copied at construction.</summary>
public sealed class SignalRReplayOptions
{
    public int PageSize { get; set; } = 256;
    public int MaxPages { get; set; } = 1000;
    public ISet<int> SupportedSchemaVersions { get; set; } = new HashSet<int> { 1 };
}
