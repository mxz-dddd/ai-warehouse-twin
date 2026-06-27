using Sim.Core.Domain;
using Sim.Core.Scenarios.Inventory;
using Xunit;

namespace Sim.Core.Tests.Scenarios;

public sealed class WarehouseInventoryLedgerTests
{
    [Fact]
    public void InventoryLedger_AddAndRemove_MaintainsSkuBalance()
    {
        var ledger = new WarehouseInventoryLedger();

        ledger.Add("SKU-A", 5m);
        ledger.Add("SKU-A", 7m);
        ledger.Remove("SKU-A", 8m);

        Assert.Equal(4m, ledger.GetAvailable("SKU-A"));
    }

    [Fact]
    public void InventoryLedger_RemoveMoreThanAvailable_ThrowsDomainRuleViolation()
    {
        var ledger = new WarehouseInventoryLedger();
        ledger.Add("SKU-A", 2m);

        Assert.Throws<DomainRuleViolationException>(
            () => ledger.Remove("SKU-A", 3m));
        Assert.Equal(2m, ledger.GetAvailable("SKU-A"));
    }

    [Fact]
    public void InventoryLedger_Snapshot_IsDeterministic()
    {
        var ledger = new WarehouseInventoryLedger();
        ledger.Add("SKU-C", 3m);
        ledger.Add("SKU-A", 1m);
        ledger.Add("SKU-B", 2m);

        var first = ledger.Snapshot();
        var second = ledger.Snapshot();

        Assert.Equal(
            new[] { "SKU-A", "SKU-B", "SKU-C" },
            first.Keys.ToArray());
        Assert.Equal(
            first.ToArray(),
            second.ToArray());
    }

    [Fact]
    public void WarehouseInventoryConservation_InboundOutboundEachPick_MatchesExpectedBalance()
    {
        var ledger = new WarehouseInventoryLedger();

        ledger.Add("SKU-A", 5m);
        ledger.Add("SKU-A", 7m);
        ledger.Remove("SKU-A", 8m);
        ledger.Remove("SKU-A", 4m);

        Assert.Equal(0m, ledger.GetAvailable("SKU-A"));
        Assert.Equal(5m + 7m - 8m - 4m, ledger.GetAvailable("SKU-A"));
        Assert.All(ledger.Snapshot().Values, quantity => Assert.True(quantity >= 0m));
    }
}
