# DP-0005 Conservation Forensic Report - 2026-02-14

## Scope
- Active domain: DP-0005 (Mass & Energy Conservation)
- Trigger evidence:
  - Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_123200.md
  - Updates/Issues/CS-0050_Investigation_Report.md
- Objective: localize the source of the ~40,407 lbm Stage E conservation divergence (max massError_lbm in Stage E evidence).

## Phase 1 - Divergence Localization

### 1) Threshold Crossing Intervals (Cumulative Mass Error)
Using interval inventory-audit error (Error (absolute)): 

| Threshold | First Exceeded At | Interval File | Error At Crossing |
|---|---:|---|---:|
| 100 lbm | 8.25 hr | Heatup_Interval_034_8.25hr.txt | 17447.2 lbm |
| 1,000 lbm | 8.25 hr | Heatup_Interval_034_8.25hr.txt | 17447.2 lbm |
| 10,000 lbm | 8.25 hr | Heatup_Interval_034_8.25hr.txt | 17447.2 lbm |

Divergence onset timestamp (first 1,000 lbm crossing interval): 14/02/2026 12:32:03 p.m. at Sim Time = 8.25 hr.

### 2) Cumulative Mass Error vs Time (Tabulated)
| Sim Time (hr) | Inventory Audit Error (lbm) | Validation massError_lbm (lbm) |
|---:|---:|---:|
| 0.00 | 0.0 | 0 |
| 0.25 | 0.4 | 0 |
| 0.50 | 0.8 | 1 |
| 0.75 | 1.1 | 1 |
| 1.00 | 0.8 | 1 |
| 1.25 | 0.7 | 1 |
| 1.50 | 0.3 | 0 |
| 1.75 | 0.1 | 0 |
| 2.00 | 0.3 | 0 |
| 2.25 | 0.5 | 0 |
| 2.50 | 0.9 | 1 |
| 2.75 | 1.4 | 1 |
| 3.00 | 2.2 | 2 |
| 3.25 | 2.8 | 3 |
| 3.50 | 3.6 | 4 |
| 3.75 | 4.6 | 5 |
| 4.00 | 6.0 | 6 |
| 4.25 | 7.4 | 7 |
| 4.50 | 8.9 | 9 |
| 4.75 | 10.3 | 10 |
| 5.00 | 12.1 | 12 |
| 5.25 | 14.1 | 14 |
| 5.50 | 16.0 | 16 |
| 5.75 | 17.9 | 18 |
| 6.00 | 20.1 | 20 |
| 6.25 | 22.7 | 23 |
| 6.50 | 25.4 | 25 |
| 6.75 | 28.4 | 28 |
| 7.00 | 31.4 | 31 |
| 7.25 | 34.4 | 34 |
| 7.50 | 37.6 | 38 |
| 7.75 | 42.2 | 42 |
| 8.00 | 48.6 | 49 |
| 8.25 | 17447.2 | 17447 |
| 8.50 | 13835.3 | 13835 |
| 8.75 | 4531.4 | 4531 |
| 9.00 | 2407.8 | 2408 |
| 9.25 | 6254.1 | 6254 |
| 9.50 | 10100.8 | 10101 |
| 9.75 | 13947.6 | 13948 |
| 10.00 | 17794.4 | 17794 |
| 10.25 | 21641.3 | 21641 |
| 10.50 | 25488.5 | 25489 |
| 10.75 | 29292.9 | 29293 |
| 11.00 | 33140.2 | 33140 |
| 11.25 | 36987.6 | 36988 |
| 11.50 | 39881.9 | 39882 |
| 11.75 | 40111.4 | 40111 |
| 12.00 | 19879.9 | 19524 |
| 12.25 | 20359.4 | 19631 |
| 12.50 | 21180.5 | 19725 |
| 12.75 | 22045.0 | 19845 |
| 13.00 | 23258.0 | 19958 |
| 13.25 | 24521.3 | 20104 |
| 13.50 | 26119.9 | 20230 |
| 13.75 | 27762.1 | 20383 |
| 14.00 | 29420.6 | 20553 |
| 14.25 | 31085.3 | 20729 |
| 14.50 | 32759.1 | 20914 |
| 14.75 | 34443.2 | 21109 |
| 15.00 | 36139.2 | 21316 |
| 15.25 | 37848.7 | 21536 |
| 15.50 | 39573.5 | 21772 |
| 15.75 | 41315.4 | 22025 |
| 16.00 | 43073.7 | 22294 |
| 16.25 | 44845.8 | 22578 |
| 16.50 | 46634.0 | 22877 |
| 16.75 | 48440.2 | 23194 |
| 17.00 | 50267.3 | 23532 |
| 17.25 | 52120.9 | 23897 |
| 17.50 | 54008.3 | 24295 |
| 17.75 | 55950.3 | 24748 |
| 18.00 | 57944.6 | 25254 |

Notes: peak interval inventory-audit error = 57944.6 lbm at 18.00 hr (Heatup_Interval_073_18.00hr.txt).
- Stage E step-level evidence records max massError_lbm = 40407.19 lbm (higher than interval snapshots due finer timestep sampling).

### 3) Bucket Breakdown at First >=1,000 lbm Deviation (8.25 hr)
Source: Heatup_Interval_034_8.25hr.txt

| Bucket | Mass at 8.25 hr |
|---|---:|
| RCS liquid mass | 701435 lbm |
| RCS steam mass | Not explicitly tracked as separate bucket in interval audit (N/A in this model path) |
| SG liquid mass | 1660000 lbm |
| SG steam inventory | 0.0 lb |
| Pressurizer liquid mass | 93912 lbm |
| Pressurizer steam mass | 0 lbm |
| VCT mass | 23742 lbm |
| CVCS line inventory | Not tracked as explicit mass bucket |
| BRS mass (other tracked subsystem) | 87620 lbm |

State at onset:
- Solid Pressurizer:        NO
- Bubble Phase:      DETECTION
- Mass Source:        CANONICAL_TWO_PHASE

### 4) Sum(tracked buckets) vs Canonical Total
- Sum tracked buckets (RCS + PZRw + PZRs + VCT + BRS) = 906709 lbm
- Canonical expected total (Expected Mass) = 924156 lbm
- Delta = -17447.0 lbm (absolute error 17447.0 lbm)

Onset jump from prior interval (8.00 -> 8.25 hr):
- TOTAL MASS: 924204 -> 906709 lbm (delta -17495 lbm)
- Expected Mass: 924156 -> 924156 lbm (delta 0 lbm)
- Error (absolute): 48.6 -> 17447.0 lbm
- Largest component jump: PZR Water Mass 111416 -> 93912 lbm (delta -17504 lbm)

## Phase 2 - Audit Equation Inspection

### A) Global Conservation Summation Equations (Code)
1. Inventory audit total mass (used for interval/report Error (absolute)): Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:333-337
   - Total_Mass_lbm = RCS_Mass_lbm + PZR_Water_Mass_lbm + PZR_Steam_Mass_lbm + VCT_Mass_lbm + BRS_Mass_lbm
2. Inventory audit expected mass baseline: Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:385-394
   - Expected_Total_Mass_lbm = Initial_Total_Mass_lbm + netExternalMass_lbm
   - netExternalMass_lbm comes from (Cumulative_Makeup_gal - Cumulative_CBOLoss_gal)
3. Validation massError_lbm equation (used by Stage E runner): Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:294-305
   - totalSystemMass_lbm = rcsMass + pzrWaterMassNow + pzrSteamMassNow + vctMass + brsMass
   - massError_lbm = |totalSystemMass_lbm - initialSystemMass_lbm - externalNetMass_lbm|

### B) Terms Included vs Excluded
Included terms:
- RCS liquid mass
- Pressurizer liquid mass
- Pressurizer steam mass
- VCT mass
- BRS mass
- External boundary adjustment terms (makeup/loss variants per path)
Excluded terms:
- SG secondary liquid mass
- SG steam inventory mass
- Explicit CVCS line/piping inventory mass
- Other non-bucketed subsystem hold-up volumes

### C) Identified Defective Term and Failure Mechanism
Primary defective term (transition overwrite):
- Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:122
- physicsState.PZRWaterMass = physicsState.PZRWaterVolume * rhoWater;
Conservation defect at onset:
- During DETECTION entry, solidPressurizer is flipped false (HeatupSimEngine.BubbleFormation.cs:107), then PZR mass is overwritten using V*rho (line 122) without compensating transfer to RCS or ledger.
- This creates an instantaneous mass drop (~17.5 klbm), matching the first large error jump at 8.25 hr.
Supporting synchronization defect:
- Solid-path canonical sync uses stale source at Assets/Scripts/Validation/HeatupSimEngine.cs:1097-1098 (PZRWaterMassSolid mirrors physicsState.PZRWaterMass).
- Solid module actively evolves PZR mass via surge transfer at Assets/Scripts/Physics/SolidPlantPressure.cs:781 (state.PzrWaterMass -= surgeMass_lb).
- This mismatch sets up a discontinuous source swap at the solid -> two-phase handoff.
Classification by requested categories:
- Missing term: missing compensating +delta mass transfer to RCS/ledger when PZR mass is overwritten at DETECTION start.
- Incorrect sign: not primary at onset.
- Double-counted term: not primary at onset.
- Time/order issue: source-authority switch occurs in the same transition tick before conserved mass handoff.

## Minimal Correction Proposal (No Implementation)
1. Enforce conservation-preserving handoff at solid -> DETECTION transition.
   - Replace direct V*rho overwrite with mass-authority transfer semantics.
   - Any required PZR mass re-normalization must be paired with equal/opposite transfer to RCS (or canonical ledger), not destruction.
2. Align solid canonical source before transition.
   - Use solid module canonical mass (solidPlantState.PzrWaterMass) as handoff source of truth instead of stale physicsState.PZRWaterMass mirror.
3. Keep equation audit scope explicit.
   - Continue tracking SG mass separately unless conservation baseline is formally expanded; if expanded, include SG buckets and boundary terms symmetrically in both actual and expected equations.

No fixes were implemented in this phase.
