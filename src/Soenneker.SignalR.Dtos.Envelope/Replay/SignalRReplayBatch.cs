using System.Collections.Generic;

namespace Soenneker.SignalR.Dtos.Envelope.Replay;

/// <summary>An ordered page after a requested checkpoint. SnapshotRequired means retained history cannot bridge the gap.</summary>
public sealed record SignalRReplayBatch(IReadOnlyList<SignalREnvelope> Messages, bool HasMore = false, bool SnapshotRequired = false);
