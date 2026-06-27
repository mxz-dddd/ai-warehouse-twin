using Sim.Core.World;

namespace Sim.Core.Scenarios;

public sealed class WarehouseScenarioRunner
{
    public WarehouseRunResult Run(WarehouseScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var inboundResult = scenario.InboundScenario is null
            ? null
            : new InboundScenarioRunner().Run(scenario.InboundScenario);

        var outboundResult = scenario.OutboundScenario is null
            ? null
            : new OutboundScenarioRunner().Run(scenario.OutboundScenario);

        var eachPickResult = scenario.EachPickScenario is null
            ? null
            : new EachPickScenarioRunner().Run(scenario.EachPickScenario);

        var childRuns = new List<ChildRun>();

        if (inboundResult is not null)
        {
            childRuns.Add(new ChildRun(
                "inbound",
                inboundResult.StartedAtMs,
                inboundResult.FinishedAtMs,
                inboundResult.EventLogText,
                inboundResult.FinalWorldState));
        }

        if (outboundResult is not null)
        {
            childRuns.Add(new ChildRun(
                "outbound",
                outboundResult.StartedAtMs,
                outboundResult.FinishedAtMs,
                outboundResult.EventLogText,
                outboundResult.FinalWorldState));
        }

        if (eachPickResult is not null)
        {
            childRuns.Add(new ChildRun(
                "each_pick",
                eachPickResult.StartedAtMs,
                eachPickResult.FinishedAtMs,
                eachPickResult.EventLogText,
                eachPickResult.FinalWorldState));
        }

        var startedAtMs = childRuns.Min(run => run.StartedAtMs);
        var finishedAtMs = childRuns.Max(run => run.FinishedAtMs);

        return new WarehouseRunResult(
            scenario.ScenarioId,
            scenario.Seed,
            inboundResult,
            outboundResult,
            eachPickResult,
            startedAtMs,
            finishedAtMs,
            ToWarehouseEventLog(childRuns),
            MergeWorldStates(finishedAtMs, childRuns));
    }

    private static string ToWarehouseEventLog(IEnumerable<ChildRun> childRuns)
    {
        var lines = new List<string>();

        foreach (var childRun in childRuns)
        {
            if (string.IsNullOrWhiteSpace(childRun.EventLogText))
            {
                continue;
            }

            foreach (var rawLine in childRun.EventLogText.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                if (line.Length == 0)
                {
                    continue;
                }

                lines.Add($"{childRun.FlowName}|{line}");
            }
        }

        return string.Join("\n", lines);
    }

    private static WorldState MergeWorldStates(long finishedAtMs, IEnumerable<ChildRun> childRuns)
    {
        var entities = new Dictionary<string, EntityPose>();

        foreach (var childRun in childRuns)
        {
            foreach (var entity in childRun.FinalWorldState.Entities)
            {
                entities[entity.Key] = entity.Value;
            }
        }

        return new WorldState(finishedAtMs, entities);
    }

    private sealed record ChildRun(
        string FlowName,
        long StartedAtMs,
        long FinishedAtMs,
        string EventLogText,
        WorldState FinalWorldState);
}
