# Phase2 Honesty Notes

This document is the required honesty skeleton for Phase2 demos. Do not remove these notes from demo preparation, customer-facing rehearsal, PR descriptions, or release readiness checks.

## Required Claims Boundary

Phase2 is an explainable dynamic warehouse digital twin demo built from deterministic simulation and committed artifacts.

It is not:

- production WMS data;
- live telemetry;
- sensor-calibrated movement;
- audited operational KPI reporting;
- a promise of real-world optimization gain;
- a physical equipment control system.

## Data Source Honesty

Demo data comes from deterministic simulation and artifact files. It is appropriate for showing the flow, UI seams, repeatability, and explainability of the system.

It must not be presented as:

- real customer WMS records;
- real worker or forklift telemetry;
- measured warehouse travel paths;
- production inventory or order truth;
- guaranteed operational performance.

Recommended wording:

```text
这是可解释的动态仓库数字孪生 Demo。
当前用于演示流程和接口闭环，不等同于生产 WMS 数据。
```

## KPI Honesty

KPI values shown in the demo come from demo artifacts or simulation summaries. They do not represent a real-world operating commitment.

Allowed wording:

```text
这些 KPI 来自 demo artifact / deterministic simulation summary，用于解释流程和对比方式。
```

Disallowed wording:

```text
这些 KPI 证明真实仓库会达到同样表现。
```

If a KPI field is missing, zero, or not meaningful for the current artifact, the UI and narration should explain that the value is pending better simulation or integration data.

## Route And Movement Honesty

If a route fallback is used, it must be marked clearly:

```text
Fallback route used; not claimed as a real simulated route.
```

Chinese demo note:

```text
使用 deterministic fallback route 时，不宣称真实仿真路线。
```

Do not describe fallback route output as:

- real WMS route;
- measured route;
- sensor route;
- physically validated route;
- optimized travel path.

## A/B Honesty

A/B comparison is a deterministic demo artifact comparison unless a future task explicitly integrates validated production evidence.

If A/B improvement is `0`, missing, or not meaningful, display:

```text
优化差异待进一步仿真迭代
```

Do not hide zero improvement. Do not convert zero improvement into invented business savings.

## UI Honesty Notes Must Stay Visible

Do not delete honesty prompts from:

- ABComparePanel notes;
- layout or route evidence labels;
- demo guide FAQ;
- release readiness notes;
- PR descriptions for demo-facing changes.

Changing wording is allowed only if it preserves the same or stronger truth boundary.

## External Demo Talk Track

Recommended customer-safe wording:

```text
这是可解释的动态仓库数字孪生 Demo。
当前用于演示流程和接口闭环，不等同于生产 WMS 数据。
```

Longer version:

```text
这个 Demo 展示了我们如何把仓库结构、资源、路径、KPI 和 A/B 对比放进一个可解释的动态数字孪生界面。当前数据来自 deterministic simulation 和 demo artifact，适合验证流程闭环与产品方向；它还不是生产 WMS 数据或真实运营承诺。
```

## Review Checklist

Before an external or leadership demo:

- Confirm no one is calling demo artifacts real WMS data.
- Confirm KPI wording says demo artifact / simulation summary.
- Confirm fallback routes are labeled when used.
- Confirm zero A/B improvement displays `优化差异待进一步仿真迭代`.
- Confirm UI honesty labels remain visible.
- Confirm known issues are disclosed in the QA checklist.

## Future Update Rule

If a future milestone integrates real WMS, calibrated telemetry, or production validation, update this file with:

- source system name;
- validation method;
- data freshness;
- privacy boundary;
- confidence level;
- remaining limitations.

Until then, keep Phase2 claims inside the deterministic demo boundary.
