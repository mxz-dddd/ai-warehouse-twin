using Sim.Core.Domain;
using Xunit;

namespace Sim.Core.Tests.Domain;

public sealed class InventoryStateMachineTests
{
    public static TheoryData<InventoryStatus, InventoryStatus> LegalTransitions => new()
    {
        { InventoryStatus.Expected, InventoryStatus.Received },
        { InventoryStatus.Received, InventoryStatus.QcHold },
        { InventoryStatus.Received, InventoryStatus.Available },
        { InventoryStatus.QcHold, InventoryStatus.Available },
        { InventoryStatus.Available, InventoryStatus.Allocated },
        { InventoryStatus.Allocated, InventoryStatus.Picking },
        { InventoryStatus.Picking, InventoryStatus.Picked },
        { InventoryStatus.Picked, InventoryStatus.Consolidating },
        { InventoryStatus.Consolidating, InventoryStatus.Staged },
        { InventoryStatus.Staged, InventoryStatus.Loaded },
        { InventoryStatus.Loaded, InventoryStatus.Shipped },
    };

    public static TheoryData<InventoryStatus, InventoryStatus> IllegalTransitions => new()
    {
        { InventoryStatus.Available, InventoryStatus.Shipped },
        { InventoryStatus.Expected, InventoryStatus.Available },
        { InventoryStatus.Loaded, InventoryStatus.Available },
    };

    [Theory]
    [MemberData(nameof(LegalTransitions))]
    public void CanTransition_ReturnsTrue_ForLegalTransitions(InventoryStatus from, InventoryStatus to)
    {
        Assert.True(InventoryStateMachine.CanTransition(from, to));
    }

    [Theory]
    [MemberData(nameof(IllegalTransitions))]
    public void CanTransition_ReturnsFalse_ForIllegalTransitions(InventoryStatus from, InventoryStatus to)
    {
        Assert.False(InventoryStateMachine.CanTransition(from, to));
    }

    [Fact]
    public void EnsureCanTransition_Throws_ForIllegalTransition()
    {
        var ex = Assert.Throws<DomainRuleViolationException>(
            () => InventoryStateMachine.EnsureCanTransition(InventoryStatus.Available, InventoryStatus.Shipped));

        Assert.Contains("Available", ex.Message);
        Assert.Contains("Shipped", ex.Message);
    }

    [Fact]
    public void CanTransition_ReturnsFalse_ForSameStatusTransition()
    {
        Assert.False(InventoryStateMachine.CanTransition(InventoryStatus.Available, InventoryStatus.Available));
    }
}
