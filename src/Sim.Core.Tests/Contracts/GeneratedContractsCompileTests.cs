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

    [Fact]
    public void MovementArtifact_GeneratedContractType_IsReferenceable()
    {
        Assert.Equal("MovementArtifact", typeof(MovementArtifact).Name);
    }

    [Fact]
    public void MovementArtifact_GeneratedContractType_CanBeCreated()
    {
        var movementArtifact = new MovementArtifact(
            schema_version: "movement-artifact.v1",
            artifact_kind: "warehouse-movement",
            scenario_id: "scenario-001",
            run_id: "run-001",
            seed: 20240627,
            source_run_artifact: "run-artifact.v1.json",
            warehouse_graph: new
            {
                nodes = new object[0],
                edges = new object[0]
            },
            actors: new object[0],
            movement_events: new object[0],
            route_segments: new object[0],
            provenance: new
            {
                movement_generator_version = "0.0.0",
                graph_source = "fixture",
                movement_enabled = true,
                deterministic_generation_policy = "test"
            });

        Assert.Equal("movement-artifact.v1", movementArtifact.schema_version);
        Assert.Equal("warehouse-movement", movementArtifact.artifact_kind);
        Assert.Equal("scenario-001", movementArtifact.scenario_id);
    }
}
