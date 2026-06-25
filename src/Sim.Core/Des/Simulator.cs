using Sim.Core.Domain;

namespace Sim.Core.Des;

public sealed class Simulator
{
    public Simulator(SimulationContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public SimulationContext Context { get; }

    public int RunUntil(long untilMs, int maxEvents = 100_000)
    {
        if (untilMs < Context.Clock.NowMs)
        {
            throw new DomainRuleViolationException(
                $"Simulator cannot run until a time before the current clock. CurrentTimeMs: {Context.Clock.NowMs}, UntilMs: {untilMs}.");
        }

        if (maxEvents <= 0)
        {
            throw new DomainRuleViolationException(
                $"Simulator maxEvents must be greater than zero. MaxEvents: {maxEvents}.");
        }

        var executed = 0;
        while (Context.EventQueue.TryPeek(out var nextEvent) && nextEvent is not null)
        {
            if (nextEvent.OccursAtMs > untilMs)
            {
                break;
            }

            if (executed >= maxEvents)
            {
                throw new DomainRuleViolationException(
                    $"Simulator exceeded maxEvents before reaching untilMs. MaxEvents: {maxEvents}, UntilMs: {untilMs}.");
            }

            Context.EventQueue.TryDequeue(out var simEvent);
            if (simEvent is null)
            {
                break;
            }

            Context.Clock = Context.Clock.AdvanceTo(simEvent.OccursAtMs);
            simEvent.Execute(Context);
            Context.EventLog.Append(Context.Clock.NowMs, simEvent.EventId, simEvent.EventType);
            Context.WorldState = Context.WorldState.WithTime(Context.Clock.NowMs);
            executed++;
        }

        return executed;
    }
}
