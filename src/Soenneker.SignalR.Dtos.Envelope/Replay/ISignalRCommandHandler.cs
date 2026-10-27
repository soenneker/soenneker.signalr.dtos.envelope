using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SignalR.Dtos.Envelope.Replay;

/// <summary>
/// Server-side idempotency boundary. Authorize the command, scope CommandId by principal/tenant,
/// and atomically persist its result with domain effects and outgoing events. Concurrent or repeated
/// identical commands return the stored result; reusing an ID with different content must fail.
/// Define retention so clients never retry beyond the deduplication horizon. External effects need an outbox.
/// </summary>
public interface ISignalRCommandHandler
{
    /// <summary>Executes once within the durable idempotency horizon and returns the persisted outcome on retry.</summary>
    ValueTask<SignalRCommandResult> Execute(SignalRCommand command, CancellationToken cancellationToken);
}
