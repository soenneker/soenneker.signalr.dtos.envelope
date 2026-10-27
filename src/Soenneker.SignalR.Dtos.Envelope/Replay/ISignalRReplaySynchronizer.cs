using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SignalR.Dtos.Envelope.Replay;

/// <summary>Reconciles durable application state from ordered server history and consistent snapshots.</summary>
public interface ISignalRReplaySynchronizer
{
    /// <summary>
    /// Replays until caught up at an observed server position. Rejects gaps, unsupported schemas, and oversized pages.
    /// A retention gap uses a snapshot, then resumes replay. Concurrent consumer writes fail the compare-and-swap
    /// without overwriting newer state; callers may retry. Cancellation or failure preserves already committed progress.
    /// The page budget bounds continuous producers and malformed sources; exhausting it throws with progress retained.
    /// Use for both ConnectionRestored and coalesced live notifications, rather than applying live payloads independently.
    /// </summary>
    ValueTask<SignalRReplayCheckpoint> Synchronize(string streamId, CancellationToken cancellationToken = default);
}
