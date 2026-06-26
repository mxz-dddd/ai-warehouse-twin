using Sim.Core.Domain;

namespace Sim.Core.Processes.EachPick;

public sealed record EachPickProcessParameters
{
    public EachPickProcessParameters(
        long toteBindDurationMs,
        long travelToStationDurationMs,
        long pickServiceDurationMs,
        long moveToStagingDurationMs)
    {
        if (toteBindDurationMs < 0)
        {
            throw new DomainRuleViolationException(
                $"ToteBindDurationMs cannot be negative. ToteBindDurationMs: {toteBindDurationMs}.");
        }

        if (travelToStationDurationMs < 0)
        {
            throw new DomainRuleViolationException(
                $"TravelToStationDurationMs cannot be negative. TravelToStationDurationMs: {travelToStationDurationMs}.");
        }

        if (pickServiceDurationMs < 0)
        {
            throw new DomainRuleViolationException(
                $"PickServiceDurationMs cannot be negative. PickServiceDurationMs: {pickServiceDurationMs}.");
        }

        if (moveToStagingDurationMs < 0)
        {
            throw new DomainRuleViolationException(
                $"MoveToStagingDurationMs cannot be negative. MoveToStagingDurationMs: {moveToStagingDurationMs}.");
        }

        ToteBindDurationMs = toteBindDurationMs;
        TravelToStationDurationMs = travelToStationDurationMs;
        PickServiceDurationMs = pickServiceDurationMs;
        MoveToStagingDurationMs = moveToStagingDurationMs;
    }

    public long ToteBindDurationMs { get; }

    public long TravelToStationDurationMs { get; }

    public long PickServiceDurationMs { get; }

    public long MoveToStagingDurationMs { get; }
}
