[![](https://img.shields.io/nuget/v/soenneker.signalr.dtos.envelope.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.signalr.dtos.envelope/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.signalr.dtos.envelope/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.signalr.dtos.envelope/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.signalr.dtos.envelope.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.signalr.dtos.envelope/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.signalr.dtos.envelope/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.signalr.dtos.envelope/actions/workflows/codeql.yml)

# Soenneker.SignalR.Dtos.Envelope

A two-field SignalR message envelope containing a type discriminator and an optional serialized payload string.

## Installation

```bash
dotnet add package Soenneker.SignalR.Dtos.Envelope
```

## Create an envelope

```csharp
using System.Text.Json;
using Soenneker.SignalR.Dtos.Envelope;

var payload = new OrderUpdated
{
    OrderId = "order-123",
    Status = "shipped"
};

var envelope = new SignalREnvelope
{
    Type = "order.updated.v1",
    Payload = JsonSerializer.Serialize(payload)
};

await hubConnection.SendAsync(
    "Message",
    envelope,
    cancellationToken);
```

`Payload` is a string, not a `JsonElement` or arbitrary object. When the envelope itself is serialized as JSON, a JSON payload is therefore nested as an escaped string:

```json
{
  "type": "order.updated.v1",
  "payload": "{\"orderId\":\"order-123\",\"status\":\"shipped\"}"
}
```

Use `Payload = null` for a type-only message.

## Consume an envelope

Route on a stable type value, then deserialize the payload into the corresponding contract:

```csharp
switch (envelope.Type)
{
    case "order.updated.v1" when envelope.Payload is not null:
        OrderUpdated? update =
            JsonSerializer.Deserialize<OrderUpdated>(envelope.Payload);
        break;
}
```

System.Text.Json annotations map the properties to `type` and `payload`. The DTO does not validate either field, enforce a type registry, serialize payload objects for you, or protect against unknown type values. Validate untrusted payload size and content before deserializing it.

`SignalREnvelope` is a mutable record. Record equality compares the current `Type` and `Payload` strings; mutating either property changes equality and hash-code results, so do not mutate an instance while it is used as a dictionary key or set member.

## Optional durable replay protocol

Legacy messages still serialize with only `type` and `payload`. Opt-in events additionally carry `messageId`, `streamId`, `sequence`, and `schemaVersion`. Sequence numbers are contiguous within one authorized stream and begin at one. A checkpoint of zero represents an empty consumer.

```csharp
using Soenneker.SignalR.Dtos.Envelope.Replay;

ISignalRReplaySynchronizer replay = new SignalRReplaySynchronizer(
    persistentServerSource, transactionalConsumer,
    new SignalRReplayOptions { PageSize = 256, MaxPages = 1000 });

await replay.Synchronize(streamId, cancellationToken);
```

`ISignalRReplaySource` supplies ordered history pages. If retention removed required history, return `SnapshotRequired`; the synchronizer fetches an authoritative snapshot, commits its checkpoint, then replays newer events. Unsupported schemas, sequence gaps, malformed pages, and exhausted page budgets fail explicitly. Already committed progress remains available for a later call.

Implement `ISignalRReplayConsumer` using your application database: event effects, message-ID deduplication, and checkpoint advancement must commit atomically. Snapshot replacement must compare the expected checkpoint in the same transaction. A concurrent writer causes a conflict instead of allowing an older snapshot to replace newer state. Use this path for both connection restoration and coalesced live notifications; do not independently apply live payloads around a snapshot reload.

`ISignalRReplayJournal` defines durable server append/replay behavior. Append must assign sequence numbers transactionally and return the same event for an identical retried message ID. `ISignalRCommandHandler` defines atomic server-side command idempotency: scope command IDs by authenticated principal/tenant, persist outcomes with domain changes, reject conflicting reuse, and retain IDs throughout the retry horizon. Use an outbox for external effects.

These are storage integration contracts, not an in-memory durability claim. Supply persistent journal and consumer implementations appropriate to your database; authorize stream access on every server request. The library does not provide a database adapter or guarantee exactly-once effects across external systems.
