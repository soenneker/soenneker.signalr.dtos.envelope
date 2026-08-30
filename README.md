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

Both System.Text.Json and Newtonsoft.Json annotations map the properties to `type` and `payload`. The DTO does not validate either field, enforce a type registry, serialize payload objects for you, or protect against unknown type values. Validate untrusted payload size and content before deserializing it.

`SignalREnvelope` is a mutable record. Record equality compares the current `Type` and `Payload` strings; mutating either property changes equality and hash-code results, so do not mutate an instance while it is used as a dictionary key or set member.
