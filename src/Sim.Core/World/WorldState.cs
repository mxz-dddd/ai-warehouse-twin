using Sim.Core.Domain;

namespace Sim.Core.World;

public sealed record EntityPose
{
    public EntityPose(string entityId, long xMm, long yMm, long zMm, string status)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new DomainRuleViolationException("EntityPose EntityId cannot be empty.");
        }

        EntityId = entityId;
        XMm = xMm;
        YMm = yMm;
        ZMm = zMm;
        Status = status ?? string.Empty;
    }

    public string EntityId { get; }

    public long XMm { get; }

    public long YMm { get; }

    public long ZMm { get; }

    public string Status { get; }
}

public sealed class WorldState
{
    private readonly IReadOnlyDictionary<string, EntityPose> _entities;

    public WorldState(long timeMs)
        : this(timeMs, new Dictionary<string, EntityPose>())
    {
    }

    public WorldState(long timeMs, IReadOnlyDictionary<string, EntityPose> entities)
    {
        if (timeMs < 0)
        {
            throw new DomainRuleViolationException(
                $"WorldState time cannot be negative. TimeMs: {timeMs}.");
        }

        ArgumentNullException.ThrowIfNull(entities);

        TimeMs = timeMs;
        _entities = new Dictionary<string, EntityPose>(entities);
    }

    public long TimeMs { get; }

    public IReadOnlyDictionary<string, EntityPose> Entities => _entities;

    public WorldState WithTime(long timeMs)
    {
        return new WorldState(timeMs, _entities);
    }

    public WorldState UpsertEntity(EntityPose pose)
    {
        ArgumentNullException.ThrowIfNull(pose);

        var entities = new Dictionary<string, EntityPose>(_entities)
        {
            [pose.EntityId] = pose,
        };

        return new WorldState(TimeMs, entities);
    }
}
