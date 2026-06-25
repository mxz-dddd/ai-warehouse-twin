using Sim.Core.Des;
using Sim.Core.Domain;
using Sim.Core.World;
using Xunit;

namespace Sim.Core.Tests.Des;

public sealed class SimulatorTests
{
    [Fact]
    public void RunUntil_ExecutesEventsBeforeAndAtUntilTime()
    {
        var executed = new List<string>();
        var context = CreateContext(seed: 1);
        context.EventQueue.Enqueue(new RecordingEvent("before", 10, executed));
        context.EventQueue.Enqueue(new RecordingEvent("at", 20, executed));

        var count = new Simulator(context).RunUntil(20);

        Assert.Equal(2, count);
        Assert.Equal(["before", "at"], executed);
        Assert.Equal(20, context.Clock.NowMs);
        Assert.Equal(20, context.WorldState.TimeMs);
    }

    [Fact]
    public void RunUntil_DoesNotExecuteFutureEvent_AndLeavesItQueued()
    {
        var executed = new List<string>();
        var context = CreateContext(seed: 1);
        context.EventQueue.Enqueue(new RecordingEvent("now", 10, executed));
        context.EventQueue.Enqueue(new RecordingEvent("future", 30, executed));

        var count = new Simulator(context).RunUntil(20);

        Assert.Equal(1, count);
        Assert.Equal(["now"], executed);
        Assert.Equal(1, context.EventQueue.Count);
        Assert.True(context.EventQueue.TryPeek(out var future));
        Assert.Equal("future", future!.EventId);
    }

    [Fact]
    public void RunUntil_ExecutesEventsInTimeOrder()
    {
        var executed = new List<string>();
        var context = CreateContext(seed: 1);
        context.EventQueue.Enqueue(new RecordingEvent("later", 30, executed));
        context.EventQueue.Enqueue(new RecordingEvent("earlier", 10, executed));
        context.EventQueue.Enqueue(new RecordingEvent("middle", 20, executed));

        new Simulator(context).RunUntil(30);

        Assert.Equal(["earlier", "middle", "later"], executed);
    }

    [Fact]
    public void RunUntil_ExecutesSameTimeEventsInEnqueueOrder()
    {
        var executed = new List<string>();
        var context = CreateContext(seed: 1);
        context.EventQueue.Enqueue(new RecordingEvent("first", 10, executed));
        context.EventQueue.Enqueue(new RecordingEvent("second", 10, executed));
        context.EventQueue.Enqueue(new RecordingEvent("third", 10, executed));

        new Simulator(context).RunUntil(10);

        Assert.Equal(["first", "second", "third"], executed);
    }

    [Fact]
    public void RunUntil_AllowsEventToEnqueueFollowUpEvent()
    {
        var executed = new List<string>();
        var context = CreateContext(seed: 1);
        context.EventQueue.Enqueue(new EnqueueingEvent("first", 10, executed, "follow-up", 15));

        var count = new Simulator(context).RunUntil(15);

        Assert.Equal(2, count);
        Assert.Equal(["first", "follow-up"], executed);
    }

    [Fact]
    public void RunUntil_WritesStableEventLog()
    {
        var executed = new List<string>();
        var context = CreateContext(seed: 1);
        context.EventQueue.Enqueue(new RecordingEvent("first", 10, executed));
        context.EventQueue.Enqueue(new RecordingEvent("second", 20, executed));

        new Simulator(context).RunUntil(20);

        Assert.Equal(
            "0|10|first|Recording\n1|20|second|Recording",
            context.EventLog.ToDeterministicText());
    }

    [Fact]
    public void RunUntil_ProducesIdenticalLog_ForSameSeedAndInput()
    {
        var firstLog = RunSeededScenario(seed: 123);
        var secondLog = RunSeededScenario(seed: 123);

        Assert.Equal(firstLog, secondLog);
    }

    [Fact]
    public void RunUntil_Throws_WhenMaxEventsExceeded()
    {
        var executed = new List<string>();
        var context = CreateContext(seed: 1);
        context.EventQueue.Enqueue(new SelfEnqueueingEvent("loop", 10, executed));

        Assert.Throws<DomainRuleViolationException>(() => new Simulator(context).RunUntil(10, maxEvents: 3));
    }

    private static string RunSeededScenario(int seed)
    {
        var executed = new List<string>();
        var context = CreateContext(seed);
        var rng = new DeterministicRng(seed);
        context.EventQueue.Enqueue(new RecordingEvent($"evt-{rng.NextInt(1, 100)}", 10, executed));
        context.EventQueue.Enqueue(new RecordingEvent($"evt-{rng.NextInt(1, 100)}", 20, executed));

        new Simulator(context).RunUntil(20);
        return context.EventLog.ToDeterministicText();
    }

    private static SimulationContext CreateContext(int seed)
    {
        return new SimulationContext(
            new SimClock(),
            new DeterministicRng(seed),
            new SimEventQueue(),
            new SimEventLog(),
            new WorldState(0));
    }

    private sealed record RecordingEvent(
        string EventId,
        long OccursAtMs,
        List<string> Executed) : ISimEvent
    {
        public string EventType => "Recording";

        public void Execute(SimulationContext context)
        {
            Executed.Add(EventId);
        }
    }

    private sealed record EnqueueingEvent(
        string EventId,
        long OccursAtMs,
        List<string> Executed,
        string FollowUpEventId,
        long FollowUpOccursAtMs) : ISimEvent
    {
        public string EventType => "Enqueueing";

        public void Execute(SimulationContext context)
        {
            Executed.Add(EventId);
            context.EventQueue.Enqueue(new RecordingEvent(FollowUpEventId, FollowUpOccursAtMs, Executed));
        }
    }

    private sealed record SelfEnqueueingEvent(
        string EventId,
        long OccursAtMs,
        List<string> Executed) : ISimEvent
    {
        public string EventType => "SelfEnqueueing";

        public void Execute(SimulationContext context)
        {
            Executed.Add(EventId);
            context.EventQueue.Enqueue(this);
        }
    }
}
