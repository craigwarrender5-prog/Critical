---
Issue: CS-0050
Title: Persistent Plant-Wide Mass Conservation Imbalance (~10,000 gal class) Across Multiple Intervals
Severity: Critical
Status: Preliminary Investigation Complete - Awaiting Authorization
Date: 2026-02-14
Mode: SPEC/DRAFT
---

# CS-0050 Preliminary Investigation Report

## 1. Registered Observation
- Objective behavior:
  - Long-duration plant-wide conservation imbalance persists on the order of ~10,000 gallons class.
  - Error persists across multiple intervals instead of converging toward zero.
  - Indicates systemic accounting/boundary defect class.

## 2. Governing Checks
- Physical law: mass conservation across plant inventory and external boundaries.
- Conservation rule: cumulative inventory closure must remain bounded over time.
- Control logic: validation thresholds require bounded conservation error, not persistent growth.

## 3. Expected vs Simulated
- Expected behavior:
  - Cumulative error remains bounded near threshold-scale and does not trend as large persistent drift.
- Simulated behavior (evidence):
  - Multi-interval inventory losses reported at `-10,470` and `-8,752` gallons.
  - Larger cumulative mismatch analysis documents `13,200` gallons overcounting class.
  - IP-0015 Stage E rerun at 18.00 hr shows conservation criterion failure with `Max mass error observed: 40407.19 lbm`.
  - Evidence: `Documentation/PWR_Heatup_Simulation_Analysis_Report.md:116`, `Documentation/PWR_Heatup_Simulation_Analysis_Report.md:119-120`, `Documentation/PWR_Heatup_Simulation_Analysis_Report.md:171`.
  - Evidence: `Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_123200.md`, `HeatupLogs/Heatup_Report_20260214_123205.txt`.
  - Related currently tracked defects: `Updates/ISSUE_REGISTRY.md:1363` and `Updates/ISSUE_REGISTRY.md:1424-1426`.

## 4. Boundary and Control State Comparison
- Boundary/control states observed:
  - Inventory closure terms do not close over long windows.
  - Post-IP-0015 Stage E rerun shows SG startup metrics passing while conservation still fails, indicating residual conservation-domain defect class.
  - Error behavior is persistent and cumulative, not single-interval noise.
- Code-path corroboration (non-destructive inspection):
  - Conservation equation and diagnostic threshold in `Assets/Scripts/Physics/VCTPhysics.cs:346-373`.

## 5. Minimal Non-Destructive Probes Performed
- Probe A: reviewed multi-interval conservation evidence table in the 20-hour analysis report.
- Probe B: reviewed canonical issue-register entries for related CVCS/VCT and inventory drift findings.
- Probe C: inspected VCT conservation equation implementation and thresholds in read-only mode.

## 6. Domain Assignment
Evidence indicates origin within Mass & Energy Conservation (conservation audit integrity), not CVCS control-path logic alone.

## 7. Severity Assignment
- Severity: Critical.
- Evidence basis:
  - Persistent large cumulative imbalance indicates conservation-law violation class, not bounded drift.

## 8. Routing Correction Note
- Previous domain categorization was incorrect (was CVCS).
- Reclassified on 2026-02-14 to Mass & Energy Conservation (DP-0005).

## 9. Constraints
- No fix design performed.
- No code modifications performed.
