using Sim.Core.Domain;

namespace Sim.Core.Processes.Outbound;

public sealed record OutboundProcessParameters
{
    public OutboundProcessParameters(
        long pickTravelDurationMs,
        long pickServiceDurationMs,
        long stageDurationMs,
        long loadDurationMs)
    {
        if (pickTravelDurationMs < 0)
        {
            throw new DomainRuleViolationException(
                $"PickTravelDurationMs cannot be negative. PickTravelDurationMs: {pickTravelDurationMs}.");
        }

        if (pickServiceDurationMs < 0)
        {
            throw new DomainRuleViolationException(
                $"PickServiceDurationMs cannot be negative. PickServiceDurationMs: {pickServiceDurationMs}.");
        }

        if (stageDurationMs < 0)
        {
            throw new DomainRuleViolationException(
                $"StageDurationMs cannot be negative. StageDurationMs: {stageDurationMs}.");
        }

        if (loadDurationMs < 0)
        {
            throw new DomainRuleViolationException(
                $"LoadDurationMs cannot be negative. LoadDurationMs: {loadDurationMs}.");
        }

        PickTravelDurationMs = pickTravelDurationMs;
        PickServiceDurationMs = pickServiceDurationMs;
        StageDurationMs = stageDurationMs;
        LoadDurationMs = loadDurationMs;
    }

    public long PickTravelDurationMs { get; }

    public long PickServiceDurationMs { get; }

    public long StageDurationMs { get; }

    public long LoadDurationMs { get; }

    public long PickTotalDurationMs => PickTravelDurationMs + PickServiceDurationMs;

    public long LoadTotalDurationMs => StageDurationMs + LoadDurationMs;
}
