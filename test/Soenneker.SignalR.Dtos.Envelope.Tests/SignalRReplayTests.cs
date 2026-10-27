using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.SignalR.Dtos.Envelope.Replay;

namespace Soenneker.SignalR.Dtos.Envelope.Tests;

public sealed class SignalRReplayTests
{
    private static SignalREnvelope Event(long sequence, string? id = null, int schema = 1) => new()
    {
        StreamId = "orders", Sequence = sequence, MessageId = id ?? $"event-{sequence}",
        SchemaVersion = schema, Type = "order.updated", Payload = $"value-{sequence}"
    };

    [Test]
    public async ValueTask Replay_commits_in_order_and_new_synchronizer_resumes_checkpoint(CancellationToken cancellationToken)
    {
        var store = new ReplayTestStore();
        store.Pages.Enqueue(new([Event(1), Event(2)], HasMore: true));
        store.Pages.Enqueue(new([Event(3)]));
        var replay = new SignalRReplaySynchronizer(store, store);
        await Assert.That((await replay.Synchronize("orders", cancellationToken)).Sequence).IsEqualTo(3);
        store.Pages.Enqueue(new([]));
        await new SignalRReplaySynchronizer(store, store).Synchronize("orders", cancellationToken);
        await Assert.That(store.Applied.Count).IsEqualTo(3);
    }

    [Test]
    public async ValueTask Retention_gap_replaces_snapshot_then_replays_newer_events(CancellationToken cancellationToken)
    {
        var store = new ReplayTestStore();
        store.Pages.Enqueue(new([], SnapshotRequired: true));
        store.Pages.Enqueue(new([Event(11)]));
        var replay = new SignalRReplaySynchronizer(store, store);
        await Assert.That((await replay.Synchronize("orders", cancellationToken)).Sequence).IsEqualTo(11);
        await Assert.That(store.Snapshots).IsEqualTo(1);
        await Assert.That(store.Applied[0]).IsEqualTo("snapshot");
        await Assert.That(store.Applied[1]).IsEqualTo("value-11");
    }

    [Test]
    public async ValueTask Concurrent_progress_prevents_stale_snapshot_overwrite(CancellationToken cancellationToken)
    {
        var store = new ReplayTestStore { ConflictOnSnapshot = true };
        store.Pages.Enqueue(new([], SnapshotRequired: true));
        var replay = new SignalRReplaySynchronizer(store, store);
        await Assert.That(async () => await replay.Synchronize("orders", cancellationToken)).Throws<InvalidOperationException>();
        await Assert.That(store.Snapshots).IsEqualTo(0);
        await Assert.That(store.Sequence).IsEqualTo(11);
    }

    [Test]
    public async ValueTask Unknown_schema_and_sequence_gap_do_not_advance_checkpoint(CancellationToken cancellationToken)
    {
        var store = new ReplayTestStore();
        var replay = new SignalRReplaySynchronizer(store, store);
        store.Pages.Enqueue(new([Event(1, schema: 9)]));
        await Assert.That(async () => await replay.Synchronize("orders", cancellationToken)).Throws<NotSupportedException>();
        store.Pages.Enqueue(new([Event(2)]));
        await Assert.That(async () => await replay.Synchronize("orders", cancellationToken)).Throws<InvalidDataException>();
        await Assert.That(store.Sequence).IsEqualTo(0);
    }

    [Test]
    public async ValueTask Failed_transaction_retries_without_losing_or_duplicating_event(CancellationToken cancellationToken)
    {
        var store = new ReplayTestStore { FailNextApply = true };
        var replay = new SignalRReplaySynchronizer(store, store);
        store.Pages.Enqueue(new([Event(1)]));
        await Assert.That(async () => await replay.Synchronize("orders", cancellationToken)).Throws<InvalidOperationException>();
        await Assert.That(store.Sequence).IsEqualTo(0);
        store.Pages.Enqueue(new([Event(1)]));
        await replay.Synchronize("orders", cancellationToken);
        await Assert.That(store.Applied.Count).IsEqualTo(1);
    }

    [Test]
    public async ValueTask Reused_identity_is_rejected_by_transaction_boundary(CancellationToken cancellationToken)
    {
        var store = new ReplayTestStore();
        store.Pages.Enqueue(new([Event(1, "same"), Event(2, "same")]));
        var replay = new SignalRReplaySynchronizer(store, store);
        await Assert.That(async () => await replay.Synchronize("orders", cancellationToken)).Throws<InvalidOperationException>();
        await Assert.That(store.Sequence).IsEqualTo(1);
        await Assert.That(store.Applied.Count).IsEqualTo(1);
    }

    [Test]
    public async ValueTask Endless_empty_pages_and_oversized_pages_are_rejected(CancellationToken cancellationToken)
    {
        var store = new ReplayTestStore();
        var replay = new SignalRReplaySynchronizer(store, store, new() { PageSize = 1 });
        store.Pages.Enqueue(new([], HasMore: true));
        await Assert.That(async () => await replay.Synchronize("orders", cancellationToken)).Throws<InvalidDataException>();
        store.Pages.Enqueue(new([Event(1), Event(2)]));
        await Assert.That(async () => await replay.Synchronize("orders", cancellationToken)).Throws<InvalidDataException>();
        await Assert.That(store.Sequence).IsEqualTo(0);
    }

    [Test]
    public async ValueTask Page_budget_retains_progress_for_next_call(CancellationToken cancellationToken)
    {
        var store = new ReplayTestStore();
        var replay = new SignalRReplaySynchronizer(store, store, new() { MaxPages = 1 });
        store.Pages.Enqueue(new([Event(1)], HasMore: true));
        await Assert.That(async () => await replay.Synchronize("orders", cancellationToken)).Throws<InvalidOperationException>();
        await Assert.That(store.Sequence).IsEqualTo(1);
        store.Pages.Enqueue(new([Event(2)]));
        await Assert.That((await replay.Synchronize("orders", cancellationToken)).Sequence).IsEqualTo(2);
    }
}
