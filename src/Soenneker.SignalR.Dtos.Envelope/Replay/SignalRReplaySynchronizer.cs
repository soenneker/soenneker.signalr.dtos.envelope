using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.SignalR.Dtos.Envelope.Replay;

public sealed class SignalRReplaySynchronizer : ISignalRReplaySynchronizer
{
    private readonly ISignalRReplaySource _source;
    private readonly ISignalRReplayConsumer _consumer;
    private readonly HashSet<int> _schemas;
    private readonly int _pageSize;
    private readonly int _maxPages;

    public SignalRReplaySynchronizer(ISignalRReplaySource source, ISignalRReplayConsumer consumer, SignalRReplayOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(consumer);
        options ??= new SignalRReplayOptions();
        if (options.PageSize is < 1 or > 10000 || options.MaxPages is < 1 or > 100000)
            throw new ArgumentOutOfRangeException(nameof(options));
        ArgumentNullException.ThrowIfNull(options.SupportedSchemaVersions);
        _schemas = new HashSet<int>(options.SupportedSchemaVersions);
        if (_schemas.Count == 0 || _schemas.Contains(0) || System.Linq.Enumerable.Any(_schemas, version => version < 0))
            throw new ArgumentException("At least one positive schema version is required.", nameof(options));
        _source = source; _consumer = consumer; _pageSize = options.PageSize; _maxPages = options.MaxPages;
    }

    public async ValueTask<SignalRReplayCheckpoint> Synchronize(string streamId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        cancellationToken.ThrowIfCancellationRequested();
        SignalRReplayCheckpoint checkpoint = await _consumer.ReadCheckpoint(streamId, cancellationToken).ConfigureAwait(false);
        if (checkpoint == null) throw new InvalidDataException("Missing consumer checkpoint.");
        ValidatePosition(streamId, checkpoint.StreamId, checkpoint.Sequence);
        bool replaced = false;
        for (int page = 0; page < _maxPages; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SignalRReplayBatch batch = await _source.ReadAfter(checkpoint, _pageSize, cancellationToken).ConfigureAwait(false);
            if (batch == null || batch.Messages == null || batch.Messages.Count > _pageSize) throw new InvalidDataException("Invalid replay page size.");
            if (batch.SnapshotRequired)
            {
                if (replaced) throw new InvalidDataException("History expired again during synchronization; retry with a fresh snapshot.");
                SignalRReplaySnapshot snapshot = await _source.ReadSnapshot(streamId, cancellationToken).ConfigureAwait(false);
                if (snapshot == null) throw new InvalidDataException("Missing replay snapshot.");
                ValidatePosition(streamId, snapshot.StreamId, snapshot.Sequence);
                ValidateSchema(snapshot.SchemaVersion);
                if (snapshot.Sequence < checkpoint.Sequence) throw new InvalidDataException("Snapshot would move the checkpoint backwards.");
                cancellationToken.ThrowIfCancellationRequested();
                if (!await _consumer.TryReplace(checkpoint, snapshot, cancellationToken).ConfigureAwait(false)) throw Conflict();
                checkpoint = new(streamId, snapshot.Sequence);
                replaced = true;
                continue;
            }
            long before = checkpoint.Sequence;
            foreach (SignalREnvelope supplied in batch.Messages)
            {
                // The wire DTO is mutable; isolate validation/application from subsequent source mutations.
                if (supplied == null) throw new InvalidDataException("Missing replay event.");
                SignalREnvelope message = supplied with { };
                if (string.IsNullOrWhiteSpace(message.MessageId) || string.IsNullOrWhiteSpace(message.Type) ||
                    message.Sequence == null || message.SchemaVersion == null)
                    throw new InvalidDataException("Replay requires an event identity, type, sequence, and schema version.");
                ValidatePosition(streamId, message.StreamId, message.Sequence.Value);
                ValidateSchema(message.SchemaVersion.Value);
                if (checkpoint.Sequence == long.MaxValue || message.Sequence != checkpoint.Sequence + 1)
                    throw new InvalidDataException("Replay events must be contiguous and strictly ordered after the checkpoint.");
                cancellationToken.ThrowIfCancellationRequested();
                if (!await _consumer.TryApply(checkpoint, message, cancellationToken).ConfigureAwait(false)) throw Conflict();
                checkpoint = new(streamId, message.Sequence.Value);
            }
            if (!batch.HasMore) return checkpoint;
            if (before == checkpoint.Sequence) throw new InvalidDataException("Replay page did not advance the checkpoint.");
        }
        throw new InvalidOperationException("Replay page budget exhausted; retry from the persisted checkpoint.");
    }

    private static void ValidatePosition(string requested, string? actual, long sequence)
    {
        if (!string.Equals(requested, actual, StringComparison.Ordinal) || sequence < 0)
            throw new InvalidDataException("Invalid stream or sequence in replay response.");
    }
    private void ValidateSchema(int schema)
    {
        if (!_schemas.Contains(schema)) throw new NotSupportedException($"Unsupported replay schema version {schema}.");
    }
    private static InvalidOperationException Conflict() => new("Application state changed concurrently; retry from its durable checkpoint.");
}
