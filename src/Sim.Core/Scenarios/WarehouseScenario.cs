using Sim.Core.Domain;

namespace Sim.Core.Scenarios;

public sealed record WarehouseScenario
{
    public WarehouseScenario(
        string scenarioId,
        int seed,
        InboundScenario? inboundScenario,
        OutboundScenario? outboundScenario,
        EachPickScenario? eachPickScenario)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new DomainRuleViolationException("WarehouseScenario ScenarioId cannot be empty.");
        }

        if (inboundScenario is null &&
            outboundScenario is null &&
            eachPickScenario is null)
        {
            throw new DomainRuleViolationException(
                "WarehouseScenario requires at least one child scenario.");
        }

        ScenarioId = scenarioId;
        Seed = seed;
        InboundScenario = inboundScenario;
        OutboundScenario = outboundScenario;
        EachPickScenario = eachPickScenario;
    }

    public string ScenarioId { get; }

    public int Seed { get; }

    public InboundScenario? InboundScenario { get; }

    public OutboundScenario? OutboundScenario { get; }

    public EachPickScenario? EachPickScenario { get; }
}
