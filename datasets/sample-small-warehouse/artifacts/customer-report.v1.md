# AI Warehouse Twin Report

## Run Summary

- Scenario: sample-small-warehouse
- Finished at: 410 ms
- Completed receipts: 1
- Completed outbound orders: 1
- Completed each-pick orders: 1

## Artifact Handoff

- Layout resources: 2
- Position timeline entries: 6
- Event log entries: 13

## A/B Comparison Summary

- Baseline: sample-small-warehouse
- Candidate: sample-small-warehouse-candidate

| Metric | Baseline | Candidate | Delta | Delta % | Direction |
|---|---:|---:|---:|---:|---|
| finished_at_ms | 410 | 360 | -50 | -12.195 | decrease |
| total_work_item_throughput_per_hour | 27000 | 30857.143 | 3857.143 | 14.286 | increase |

## Notes

- This report is generated from deterministic simulation artifacts.
- The comparison table reports objective numeric deltas only.
- Layout coordinates are deterministic baseline positions, not a full warehouse map.
