# AI Warehouse Twin Report

## Run Summary

- Scenario: sample-small-warehouse
- Finished at: 220 ms
- Completed receipts: 1
- Completed outbound orders: 1
- Completed each-pick orders: 1

## Artifact Handoff

- Layout resources: 4
- Position timeline entries: 12
- Event log entries: 10

## A/B Comparison Summary

- Baseline: sample-small-warehouse
- Candidate: sample-small-warehouse-candidate

| Metric | Baseline | Candidate | Delta | Delta % | Direction |
|---|---:|---:|---:|---:|---|
| finished_at_ms | 220 | 210 | -10 | -4.545 | decrease |
| total_work_item_throughput_per_hour | 51428.571 | 54000 | 2571.429 | 5 | increase |

## Notes

- This report is generated from deterministic simulation artifacts.
- The comparison table reports objective numeric deltas only.
- Layout coordinates are deterministic baseline positions, not a full warehouse map.
