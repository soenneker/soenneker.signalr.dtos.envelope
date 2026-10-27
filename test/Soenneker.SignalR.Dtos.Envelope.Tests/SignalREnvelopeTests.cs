using System.Text.Json;
using System.Threading.Tasks;

namespace Soenneker.SignalR.Dtos.Envelope.Tests;

public sealed class SignalREnvelopeTests
{
    [Test]
    public async ValueTask Legacy_wire_shape_is_preserved_without_replay_metadata()
    {
        var envelope = new SignalREnvelope { Type = "updated", Payload = "data" };
        string json = JsonSerializer.Serialize(envelope);
        await Assert.That(json).IsEqualTo("{\"type\":\"updated\",\"payload\":\"data\"}");
        var restored = JsonSerializer.Deserialize<SignalREnvelope>(json)!;
        await Assert.That(restored.MessageId == null && restored.Sequence == null).IsTrue();
    }

    [Test]
    public async ValueTask Replay_metadata_round_trips()
    {
        var envelope = new SignalREnvelope { Type = "updated", MessageId = "one", StreamId = "orders", Sequence = 1, SchemaVersion = 2 };
        var restored = JsonSerializer.Deserialize<SignalREnvelope>(JsonSerializer.Serialize(envelope));
        await Assert.That(restored).IsEqualTo(envelope);
    }
}
