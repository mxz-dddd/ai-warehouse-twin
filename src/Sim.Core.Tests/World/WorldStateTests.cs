using Sim.Core.Domain;
using Sim.Core.World;
using Xunit;

namespace Sim.Core.Tests.World;

public sealed class WorldStateTests
{
    [Fact]
    public void Constructor_CreatesEmptyWorldState()
    {
        var worldState = new WorldState(0);

        Assert.Equal(0, worldState.TimeMs);
        Assert.Empty(worldState.Entities);
    }

    [Fact]
    public void Constructor_Throws_ForNegativeTime()
    {
        Assert.Throws<DomainRuleViolationException>(() => new WorldState(-1));
    }

    [Fact]
    public void UpsertEntity_AddsEntity()
    {
        var worldState = new WorldState(0)
            .UpsertEntity(new EntityPose("forklift-1", 1, 2, 3, "Idle"));

        Assert.True(worldState.Entities.ContainsKey("forklift-1"));
    }

    [Fact]
    public void UpsertEntity_ReplacesExistingEntity()
    {
        var worldState = new WorldState(0)
            .UpsertEntity(new EntityPose("forklift-1", 1, 2, 3, "Idle"))
            .UpsertEntity(new EntityPose("forklift-1", 4, 5, 6, "Moving"));

        Assert.Equal(4, worldState.Entities["forklift-1"].XMm);
        Assert.Equal("Moving", worldState.Entities["forklift-1"].Status);
    }

    [Fact]
    public void WithTime_ReturnsNewInstance()
    {
        var original = new WorldState(10);
        var updated = original.WithTime(20);

        Assert.NotSame(original, updated);
        Assert.Equal(10, original.TimeMs);
        Assert.Equal(20, updated.TimeMs);
    }

    [Fact]
    public void EntityPose_Throws_ForEmptyEntityId()
    {
        Assert.Throws<DomainRuleViolationException>(() => new EntityPose("", 0, 0, 0, "Idle"));
    }
}
