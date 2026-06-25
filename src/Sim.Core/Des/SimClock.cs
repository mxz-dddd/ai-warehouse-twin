using Sim.Core.Domain;

namespace Sim.Core.Des;

public sealed class SimClock
{
    public SimClock(long initialTimeMs = 0)
    {
        if (initialTimeMs < 0)
        {
            throw new DomainRuleViolationException(
                $"Simulation clock initial time cannot be negative. InitialTimeMs: {initialTimeMs}.");
        }

        NowMs = initialTimeMs;
    }

    public long NowMs { get; }

    public SimClock AdvanceTo(long targetTimeMs)
    {
        if (targetTimeMs < NowMs)
        {
            throw new DomainRuleViolationException(
                $"Simulation clock cannot move backwards. CurrentTimeMs: {NowMs}, TargetTimeMs: {targetTimeMs}.");
        }

        return new SimClock(targetTimeMs);
    }
}
