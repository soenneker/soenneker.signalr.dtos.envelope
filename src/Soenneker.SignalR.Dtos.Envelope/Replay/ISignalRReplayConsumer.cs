using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SignalR.Dtos.Envelope.Replay;

/// <summary>
/// Application-specific durable transaction boundary. Persist state, message deduplication, and checkpoint
/// together in one transaction. All writers, including live delivery and snapshot replacement, must use this
/// boundary. Compare the expected checkpoint atomically to prevent stale replay or snapshots overwriting newer state.
/// Retain deduplication identities for the application's replay/idempotency horizon. No in-memory implementation
/// can provide durability across process loss. Effects outside the transaction require an outbox/idempotency key.
/// </summary>
public interface ISignalRReplayConsumer
{
    /// <summary>Returns the persisted stream position, or sequence zero for a new stream.</summary>
    ValueTask<SignalRReplayCheckpoint> ReadCheckpoint(string streamId, CancellationToken cancellationToken);
    /// <summary>
    /// Atomically applies the next event, records its MessageId, and advances the checkpoint if expected still matches.
    /// Return false without side effects on a concurrent change. Reject a reused message ID at a different sequence.
    /// </summary>
    ValueTask<bool> TryApply(SignalRReplayCheckpoint expected, SignalREnvelope message, CancellationToken cancellationToken);
    /// <summary>
    /// Atomically replaces state and checkpoint if expected still matches. Return false without side effects on conflict.
    /// Preserve deduplication records required by the application's idempotency horizon.
    /// </summary>
    ValueTask<bool> TryReplace(SignalRReplayCheckpoint expected, SignalRReplaySnapshot snapshot, CancellationToken cancellationToken);
}
