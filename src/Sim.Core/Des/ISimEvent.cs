namespace Sim.Core.Des;

public interface ISimEvent
{
    string EventId { get; }

    long OccursAtMs { get; }

    string EventType { get; }

    void Execute(SimulationContext context);
}
