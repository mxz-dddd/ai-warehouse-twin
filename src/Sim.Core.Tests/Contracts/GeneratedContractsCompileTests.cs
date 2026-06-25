using WarehouseTwin.Contracts;
using Xunit;

namespace Sim.Core.Tests.Contracts;

public sealed class GeneratedContractsCompileTests
{
    [Fact]
    public void DomainEvent_GeneratedContractType_IsReferenceable()
    {
        Assert.Equal("DomainEvent", typeof(DomainEvent).Name);
    }

    [Fact]
    public void DomainEvent_GeneratedContractType_CanBeCreated()
    {
        var domainEvent = new DomainEvent(
            event_id: "evt-001",
            warehouse_id: "wh-001",
            occurred_at: "2026-06-26T00:00:00Z",
            source: "SIM",
            event_type: "InventoryMoved",
            correlation_id: "corr-001",
            task_id: "task-001",
            actor_id: "worker-001",
            asset_id: "asset-001",
            sku_id: "sku-001",
            quantity: 1m,
            from_location: "loc-a",
            to_location: "loc-b",
            position: new { x_mm = 1, y_mm = 2, z_mm = 3 },
            confidence: 1m,
            idempotency_key: "idem-001");

        Assert.Equal("evt-001", domainEvent.event_id);
    }
}
