using Sim.Core.Domain;

namespace Sim.Core.Des;

public sealed class SimEventQueue
{
    private readonly PriorityQueue<QueuedEvent, QueuePriority> _queue = new();
    private long _nextSequence;

    public int Count => _queue.Count;

    public void Enqueue(ISimEvent simEvent)
    {
        ArgumentNullException.ThrowIfNull(simEvent);

        if (simEvent.OccursAtMs < 0)
        {
            throw new DomainRuleViolationException(
                $"Simulation event cannot occur at a negative time. EventId: {simEvent.EventId}, OccursAtMs: {simEvent.OccursAtMs}.");
        }

        var sequence = _nextSequence++;
        _queue.Enqueue(
            new QueuedEvent(simEvent, sequence),
            new QueuePriority(simEvent.OccursAtMs, sequence));
    }

    public bool TryPeek(out ISimEvent? simEvent)
    {
        if (_queue.TryPeek(out var queuedEvent, out _))
        {
            simEvent = queuedEvent.SimEvent;
            return true;
        }

        simEvent = null;
        return false;
    }

    public bool TryDequeue(out ISimEvent? simEvent)
    {
        if (_queue.TryDequeue(out var queuedEvent, out _))
        {
            simEvent = queuedEvent.SimEvent;
            return true;
        }

        simEvent = null;
        return false;
    }

    private sealed record QueuedEvent(ISimEvent SimEvent, long Sequence);

    private readonly record struct QueuePriority(long OccursAtMs, long Sequence)
        : IComparable<QueuePriority>
    {
        public int CompareTo(QueuePriority other)
        {
            var timeComparison = OccursAtMs.CompareTo(other.OccursAtMs);
            if (timeComparison != 0)
            {
                return timeComparison;
            }

            return Sequence.CompareTo(other.Sequence);
        }
    }
}
