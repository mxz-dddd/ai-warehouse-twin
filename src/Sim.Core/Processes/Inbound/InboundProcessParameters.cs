using Sim.Core.Domain;

namespace Sim.Core.Processes.Inbound;

public sealed record InboundProcessParameters
{
    public InboundProcessParameters(
        long unloadDurationMs,
        long putawayTravelDurationMs,
        long putawayServiceDurationMs)
    {
        if (unloadDurationMs < 0)
        {
            throw new DomainRuleViolationException(
                $"UnloadDurationMs cannot be negative. UnloadDurationMs: {unloadDurationMs}.");
        }

        if (putawayTravelDurationMs < 0)
        {
            throw new DomainRuleViolationException(
                $"PutawayTravelDurationMs cannot be negative. PutawayTravelDurationMs: {putawayTravelDurationMs}.");
        }

        if (putawayServiceDurationMs < 0)
        {
            throw new DomainRuleViolationException(
                $"PutawayServiceDurationMs cannot be negative. PutawayServiceDurationMs: {putawayServiceDurationMs}.");
        }

        UnloadDurationMs = unloadDurationMs;
        PutawayTravelDurationMs = putawayTravelDurationMs;
        PutawayServiceDurationMs = putawayServiceDurationMs;
    }

    public long UnloadDurationMs { get; }

    public long PutawayTravelDurationMs { get; }

    public long PutawayServiceDurationMs { get; }

    public long PutawayTotalDurationMs => PutawayTravelDurationMs + PutawayServiceDurationMs;
}
