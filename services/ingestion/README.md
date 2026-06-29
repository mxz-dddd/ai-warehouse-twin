# Ingestion Service

Minimal Python ingestion tooling for converting local warehouse export fixtures into
`scenario.json` plus `data-quality-report.md`.

## Commands

Run from this directory, or pass `--repo-root` when running from elsewhere:

```bash
python -m ingestion print-schema-info --repo-root ../..
python -m ingestion csv-to-scenario \
  --input ../../datasets/ingestion-cases/csv-basic/input \
  --output /tmp/ingestion-csv-out \
  --repo-root ../..
python -m ingestion mock-wms-to-scenario \
  --input ../../datasets/ingestion-cases/mock-wms-basic/input \
  --output /tmp/ingestion-mock-wms-out \
  --repo-root ../..
```

From the repository root, the smoke check runs linting, typing, tests, valid golden
comparisons, invalid-case checks, and byte-stability checks:

```bash
bash scripts/smoke-ingestion.sh
```

CSV input is supported in this phase. Mock WMS uses local JSON payloads only.
Real WMS integration is reserved for INGEST-006.
