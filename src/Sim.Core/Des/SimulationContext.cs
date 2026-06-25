using Sim.Core.World;

namespace Sim.Core.Des;

public sealed class SimulationContext
{
    public SimulationContext(
        SimClock clock,
        DeterministicRng rng,
        SimEventQueue eventQueue,
        SimEventLog eventLog,
        WorldState worldState)
    {
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Rng = rng ?? throw new ArgumentNullException(nameof(rng));
        EventQueue = eventQueue ?? throw new ArgumentNullException(nameof(eventQueue));
        EventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
        WorldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
    }

    public SimClock Clock { get; internal set; }

    public DeterministicRng Rng { get; }

    public SimEventQueue EventQueue { get; }

    public SimEventLog EventLog { get; }

    public WorldState WorldState { get; internal set; }
}
