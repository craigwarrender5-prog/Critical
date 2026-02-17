# CS-0051 Investigation Report

## Issue Metadata
| Field | Value |
|---|---|
| Issue ID | CS-0051 |
| Title | Stage E Mass Conservation Discontinuity at 8.25 hr during Solid->Two-Phase Handoff |
| Domain | DP-0005 (Mass & Energy Conservation) |
| Severity | CRITICAL |
| Status | Open - Investigating |
| Date Opened | 2026-02-14 |

## Executive Summary
Stage E validation remains failed on conservation (`max mass error 40407.19 lbm`) even with SG startup criteria passing (`Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_123200.md:21,24-30`).

The first major discontinuity occurs at `8.25 hr`, where inventory audit absolute error jumps from `48.6 lbm` at `8.00 hr` to `17447.2 lbm` at `8.25 hr` (`HeatupLogs/Heatup_Interval_033_8.00hr.txt:237`, `HeatupLogs/Heatup_Interval_034_8.25hr.txt:222`). The dominant bucket delta is pressurizer water mass dropping from `111416 lbm` to `93912 lbm` (about `17.5 klbm`) across the same interval (`HeatupLogs/Heatup_Interval_033_8.00hr.txt:216`, `HeatupLogs/Heatup_Interval_034_8.25hr.txt:201`).

This profile indicates an authority/regime-transition conservation violation (step discontinuity), not slow numerical drift.

## Transition Invariant (Explicit)
At any regime transition, `TotalTrackedMass_before == TotalTrackedMass_after` must hold exactly within floating tolerance.

## Evidence and Reproducible Investigation
### 1) Transition moment and mode change
- At 8.25 hr, logs show transition context:
  - `Solid Pressurizer: NO`, `Bubble Phase: DETECTION` (`HeatupLogs/Heatup_Interval_034_8.25hr.txt:57-59`)
  - Audit mass source switches to `CANONICAL_TWO_PHASE` (`HeatupLogs/Heatup_Interval_034_8.25hr.txt:207`)
- Previous interval uses `CANONICAL_SOLID` (`HeatupLogs/Heatup_Interval_033_8.00hr.txt:222`).

### 2) Regime-transition code path and overwrite pattern
- Bubble detection flips authority:
  - `solidPressurizer = false;` (`Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:107`)
  - `bubblePhase = BubbleFormationPhase.DETECTION;` (`Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:132`)
- In the same block, PZR water mass is overwritten from geometry/state:
  - `physicsState.PZRWaterMass = physicsState.PZRWaterVolume * rhoWater;` (`Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:122`)

### 3) Compensating transfer check
- At this DETECTION handoff block, there is no equal/opposite mass transfer to `physicsState.RCSWaterMass` or canonical ledger.
- A conservation-preserving transfer exists later in DRAIN (`physicsState.PZRWaterMass -= dm_cvcsActual; physicsState.RCSWaterMass += dm_cvcsActual;`) but not at DETECTION handoff (`Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:404-407`).

### 4) Source-of-truth / canonical sync path relevant to handoff
- Solid-side canonical tracking stores:
  - `physicsState.PZRWaterMassSolid = physicsState.PZRWaterMass;`
  - `physicsState.TotalPrimaryMassSolid = physicsState.RCSWaterMass + physicsState.PZRWaterMassSolid;`
  (`Assets/Scripts/Validation/HeatupSimEngine.cs:1097-1098`)
- Logging switches branch by `solidPressurizer`/bubble state:
  - Solid branch (`CANONICAL_SOLID`) reads `PZRWaterMassSolid` (`Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:303-311`)
  - Two-phase branch (`CANONICAL_TWO_PHASE`) reads `physicsState.PZRWaterMass` (`Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:312-320`)

### 5) Causal chain (current evidence)
`solidPressurizer` flips false at detection -> audit authority switches to two-phase branch -> PZR mass is overwritten by `Vxrho` at handoff -> no equal/opposite transfer or ledger reconciliation at that handoff tick -> instantaneous mass loss appears in inventory audit -> Stage E conservation divergence escalates.

## Why This Is DP-0005
- The observed failure is conservation-law closure at regime handoff, not SG boundary behavior itself.
- The defect class is authority ownership and canonical mass continuity across state transition.
- Therefore domain assignment is `DP-0005 (Mass & Energy Conservation)`.

## Non-Implementation Correction Class (Invariant)
Regime transition must preserve total tracked mass exactly at handoff: any state overwrite must be paired with an equal/opposite transfer or explicit canonical ledger reconciliation in the same tick.


