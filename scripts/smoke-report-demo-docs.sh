#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

grep -n "render-report" README.md >/dev/null
grep -n "customer-report.v1.md" README.md >/dev/null
grep -n "scripts/smoke-customer-report.sh" README.md >/dev/null
grep -n "scripts/smoke-report-demo-docs.sh" README.md >/dev/null

grep -n "Customer report demo" docs/customer-report-demo.md >/dev/null
grep -n "RunArtifact" docs/customer-report-demo.md >/dev/null
grep -n "ComparisonArtifact" docs/customer-report-demo.md >/dev/null
grep -n "AI Warehouse Twin Report" docs/customer-report-demo.md >/dev/null
grep -n "Artifact Handoff" docs/customer-report-demo.md >/dev/null
grep -n "total_work_item_throughput_per_hour" docs/customer-report-demo.md >/dev/null

grep -n "smoke-customer-report.sh" docs/each-pick.md >/dev/null
grep -n "smoke-report-demo-docs.sh" docs/each-pick.md >/dev/null

echo "smoke-report-demo-docs PASS"
