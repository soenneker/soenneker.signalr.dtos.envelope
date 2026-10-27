using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.SignalR.Dtos.Envelope.Replay;

namespace Soenneker.SignalR.Dtos.Envelope.Tests;

internal sealed class ReplayTestStore : ISignalRReplayConsumer, ISignalRReplaySource
{
    private readonly object _gate = new();
    public long Sequence;
    public List<string> Applied { get; } = [];
    public HashSet<string> Identities { get; } = [];
    public Queue<SignalRReplayBatch> Pages { get; } = new();
    public SignalRReplaySnapshot Snapshot { get; set; } = new("orders", 10, 1, "snapshot");
    public bool ConflictOnSnapshot;
    public bool FailNextApply;
    public int Snapshots;
    public ValueTask<SignalRReplayCheckpoint> ReadCheckpoint(string streamId, CancellationToken cancellationToken)
    {
        lock (_gate) return ValueTask.FromResult(new SignalRReplayCheckpoint(streamId, Sequence));
    }
    public ValueTask<SignalRReplayBatch> ReadAfter(SignalRReplayCheckpoint checkpoint, int limit, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Pages.Dequeue());
    public ValueTask<SignalRReplaySnapshot> ReadSnapshot(string streamId, CancellationToken cancellationToken) => ValueTask.FromResult(Snapshot);
    public ValueTask<bool> TryApply(SignalRReplayCheckpoint expected, SignalREnvelope message, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (FailNextApply) { FailNextApply = false; throw new InvalidOperationException("Transaction rolled back"); }
            if (Sequence != expected.Sequence) return ValueTask.FromResult(false);
            if (!Identities.Add(message.MessageId!)) throw new InvalidOperationException("Reused event identity");
            Applied.Add(message.Payload!);
            Sequence = message.Sequence!.Value;
            return ValueTask.FromResult(true);
        }
    }
    public ValueTask<bool> TryReplace(SignalRReplayCheckpoint expected, SignalRReplaySnapshot snapshot, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (ConflictOnSnapshot) { Sequence = snapshot.Sequence + 1; return ValueTask.FromResult(false); }
            if (Sequence != expected.Sequence) return ValueTask.FromResult(false);
            Sequence = snapshot.Sequence; Snapshots++; Applied.Clear(); Applied.Add(snapshot.Payload!);
            return ValueTask.FromResult(true);
        }
    }
}
