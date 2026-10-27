using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SignalR.Dtos.Envelope.Replay;

/// <summary>
/// Persistent server event journal. Implement alongside the application's database transaction/outbox.
/// The same event identity must return the same committed event on retry; conflicting content must be rejected.
/// Sequence allocation, append, and domain effects must be atomic or joined through a durable outbox.
/// </summary>
public interface ISignalRReplayJournal : ISignalRReplaySource
{
    /// <summary>Durably appends a versioned event, assigning the next contiguous stream sequence before acknowledging success.</summary>
    ValueTask<SignalREnvelope> Append(string streamId, string messageId, string type, int schemaVersion, string? payload, CancellationToken cancellationToken);
}
