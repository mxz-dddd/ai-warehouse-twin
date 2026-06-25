using Sim.Core.Des;
using Sim.Core.Domain;
using Xunit;

namespace Sim.Core.Tests.Des;

public sealed class SimEventQueueTests
{
    [Fact]
    public void TryDequeue_ReturnsEarlierTimeFirst()
    {
        var queue = new SimEventQueue();
        queue.Enqueue(new NoOpEvent("later", 20));
        queue.Enqueue(new NoOpEvent("earlier", 10));

        Assert.True(queue.TryDequeue(out var first));
        Assert.Equal("earlier", first!.EventId);
    }

    [Fact]
    public void TryDequeue_ReturnsSameTimeEventsInEnqueueOrder()
    {
        var queue = new SimEventQueue();
        queue.Enqueue(new NoOpEvent("first", 10));
        queue.Enqueue(new NoOpEvent("second", 10));
        queue.Enqueue(new NoOpEvent("third", 10));

        Assert.True(queue.TryDequeue(out var first));
        Assert.True(queue.TryDequeue(out var second));
        Assert.True(queue.TryDequeue(out var third));

        Assert.Equal("first", first!.EventId);
        Assert.Equal("second", second!.EventId);
        Assert.Equal("third", third!.EventId);
    }

    [Fact]
    public void TryDequeue_ReturnsFalse_ForEmptyQueue()
    {
        var queue = new SimEventQueue();

        Assert.False(queue.TryDequeue(out var simEvent));
        Assert.Null(simEvent);
    }

    [Fact]
    public void Enqueue_Throws_ForNullEvent()
    {
        var queue = new SimEventQueue();

        Assert.Throws<ArgumentNullException>(() => queue.Enqueue(null!));
    }

    [Fact]
    public void Enqueue_Throws_ForNegativeEventTime()
    {
        var queue = new SimEventQueue();

        Assert.Throws<DomainRuleViolationException>(() => queue.Enqueue(new NoOpEvent("bad", -1)));
    }

    [Fact]
    public void TryPeek_DoesNotRemoveEvent()
    {
        var queue = new SimEventQueue();
        queue.Enqueue(new NoOpEvent("evt", 10));

        Assert.True(queue.TryPeek(out var peeked));
        Assert.Equal("evt", peeked!.EventId);
        Assert.Equal(1, queue.Count);

        Assert.True(queue.TryDequeue(out var dequeued));
        Assert.Equal("evt", dequeued!.EventId);
        Assert.Equal(0, queue.Count);
    }

    private sealed record NoOpEvent(string EventId, long OccursAtMs) : ISimEvent
    {
        public string EventType => "NoOp";

        public void Execute(SimulationContext context)
        {
        }
    }
}
