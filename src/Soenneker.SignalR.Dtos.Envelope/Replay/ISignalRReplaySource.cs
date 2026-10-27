using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SignalR.Dtos.Envelope.Replay;

/// <summary>
/// Server-backed replay access. Implementations must authorize every stream request, return immutable
/// ordered pages from persistent history, and return SnapshotRequired when retention removed required events.
/// A snapshot must describe application state at exactly its returned sequence, with subsequent events replayable.
/// SignalR notifications should trigger replay; they are not the authoritative history.
/// </summary>
public interface ISignalRReplaySource
{
    /// <summary>Reads at most limit events strictly after the checkpoint. A page with HasMore must make progress.</summary>
    ValueTask<SignalRReplayBatch> ReadAfter(SignalRReplayCheckpoint checkpoint, int limit, CancellationToken cancellationToken);
    /// <summary>Reads an authoritative snapshot with a consistent checkpoint.</summary>
    ValueTask<SignalRReplaySnapshot> ReadSnapshot(string streamId, CancellationToken cancellationToken);
}
