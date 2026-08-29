[![](https://img.shields.io/nuget/v/soenneker.signalr.dtos.envelope.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.signalr.dtos.envelope/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.signalr.dtos.envelope/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.signalr.dtos.envelope/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.signalr.dtos.envelope.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.signalr.dtos.envelope/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.signalr.dtos.envelope/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.signalr.dtos.envelope/actions/workflows/codeql.yml)

# Soenneker.SignalR.Dtos.Envelope

Represents a standardized message envelope used in SignalR communication, containing a type identifier and a JSON-encoded payload.

## Install

```bash
dotnet add package Soenneker.SignalR.Dtos.Envelope
```

## What you get

- `SignalREnvelope` — Represents a standardized message envelope used in SignalR communication, containing a type identifier and a JSON-encoded payload.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `SignalREnvelope.Type` | Gets or sets the type of the message, used to identify the nature or intent of the payload. | Gets or sets the type of the message, used to identify the nature or intent of the payload. |
| `SignalREnvelope.Payload` | Gets or sets the serialized payload of the message, typically a JSON string. This may be `null` for messages that carry no payload. | Gets or sets the serialized payload of the message, typically a JSON string. This may be `null` for messages that carry no payload. |
