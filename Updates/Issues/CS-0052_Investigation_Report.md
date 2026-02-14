# CS-0052 Investigation Report

## Header
- Title: Post-RTCC long-run mass divergence starting 8.50 hr
- Domain: DP-0005 - Mass & Energy Conservation
- Severity: CRITICAL (Stage E fail)
- Status: Investigating
- Primary Evidence: `Updates/Issues/IP-0016_StageE_Validation_2026-02-14_133520.md`
- First Failing Interval: `HeatupLogs/Heatup_Interval_035_8.50hr.txt`

## Scope
This report localizes the first post-RTCC divergence event (8.25 -> 8.50 hr) and identifies correction class only. No physics implementation is included.

## A) Delta-Of-Delta Bucket Comparison At Onset (034 -> 035)

### Interval Snapshots
- 8.25 hr (`HeatupLogs/Heatup_Interval_034_8.25hr.txt`):
  - Total Mass: `924213 lbm`
  - Expected Mass: `924156 lbm`
  - Error: `56.8 lbm`
  - Plant External Net: `0.0 gal`
- 8.50 hr (`HeatupLogs/Heatup_Interval_035_8.50hr.txt`):
  - Total Mass: `927825 lbm`
  - Expected Mass: `924156 lbm`
  - Error: `3668.7 lbm`
  - Plant External Net: `0.0 gal`

### Bucket Delta Table (8.25 -> 8.50 hr)
| Bucket | 8.25 hr (lbm) | 8.50 hr (lbm) | Delta (lbm) | Contribution vs net +3612 lbm |
|---|---:|---:|---:|---:|
| RCS | 718939 | 721704 | +2765 | +76.6% |
| PZR water | 93912 | 90161 | -3751 | -103.8% |
| PZR steam | 0 | 703 | +703 | +19.5% |
| VCT | 23742 | 26201 | +2459 | +68.1% |
| BRS | 87620 | 89056 | +1436 | +39.8% |
| **Total** | 924213 | 927825 | **+3612** | 100% |

### Contribution Ranking
By absolute delta magnitude:
1. PZR water: `-3751 lbm`
2. RCS: `+2765 lbm`
3. VCT: `+2459 lbm`
4. BRS: `+1436 lbm`
5. PZR steam: `+703 lbm`

### Localization Statement
The divergence is not caused by external boundary terms at onset (`Plant External Net = 0`).
The majority positive growth is internal (`RCS + VCT + BRS = +6660 lbm`) and is only partially offset by `PZR water -3751 lbm`.
Primary culprit term: **missing component-space primary boundary outflow (RCS letdown decrement) while VCT/BRS inflows are still counted**.

## B) Reconcile The Two Conservation Equations

### Equation 1 (step-level / Stage E metric)
`massError_lbm = | totalSystemMass_lbm - initialSystemMass_lbm - externalNetMass_lbm |`
- Code: `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:291`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:296`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:298`

### Equation 2 (interval inventory audit)
`Expected = Initial + externalNetMass_lbm`
`Conservation_Error_lbm = | Total - Expected |`
- Code: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:388`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:391`

### Result
At 8.50 hr they are aligned (rounding-only difference):
- `Mass Conservation: FAIL (3669 lbm)`
- `Interval Conservation: FAIL (3668.7 lbm, 0.397%)`
Source: `HeatupLogs/Heatup_Interval_035_8.50hr.txt`

Conclusion: **No equation-form mismatch**. Both paths use the same external net authority term and fail together.

## C) Boundary Flow Accounting Audit (externalNetMass)

### Units/Sign
- External in/out tracked in gallons and converted once to mass:
  - `externalNetMass_lbm = (externalNetGal / FT3_TO_GAL) * rhoVCT`
  - Code: `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:296`
- Sign convention:
  - `externalInStep_gal` positive
  - `externalOutStep_gal` positive
  - `plantExternalNet_gal = in - out`
  - Code: `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:265`-`Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:269`

### Increment Sites / Double Apply
- Incremented in `UpdateVCT(...)` once per simulation step.
- `UpdateVCT(...)` called once from `UpdateCVCSFlows(...)`.
  - Code: `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:158`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:211`
- At onset interval, external net stays zero, so the jump is not external-boundary-driven.

### Session Reset
- Reset at initialization:
  - `plantExternalIn_gal = 0f`
  - `plantExternalOut_gal = 0f`
  - `plantExternalNet_gal = 0f`
  - `externalNetMass_lbm = 0f`
  - Code: `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:87`-`Assets/Scripts/Validation/HeatupSimEngine.Init.cs:89`, `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:412`

Conclusion: **externalNetMass path is coherent and not the onset source**.

## D) VCT-Focused Audit

### How VCT enters totals
- VCT mass in plant conservation uses `vctState.Volume_gal` with fixed VCT density:
  - `vctMass = (vctState.Volume_gal / FT3_TO_GAL) * rhoVCT`
  - Code: `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:286`
- Same conceptual approach in interval audit path:
  - Code: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs` (inventory update mass calculation)

### VCT internal update behavior
- VCT volume update is flow-based:
  - `flowIn = letdown + sealReturn + makeup`
  - `flowOut = charging + divert + cbo`
  - `deltaVolume = netFlow * dt`
  - Code: `Assets/Scripts/Physics/VCTPhysics.cs:228`-`Assets/Scripts/Physics/VCTPhysics.cs:235`

### Diagnostic mismatch noted in run log
- `VCTPhysics.VerifyMassConservation` uses a different external boundary definition (`seal return + makeup` in, `divert + cbo` out):
  - Code: `Assets/Scripts/Physics/VCTPhysics.cs:250`-`Assets/Scripts/Physics/VCTPhysics.cs:251`, `Assets/Scripts/Physics/VCTPhysics.cs:361`-`Assets/Scripts/Physics/VCTPhysics.cs:362`
- Stage E tail (`HeatupLogs/Unity_StageE_IP0016_final.log`) shows large VCT diagnostic mismatch while plant external net is separately near zero.

### VCT Verdict
**VCT mass itself is not the primary wrong term at onset.**
The stronger defect is: VCT/BRS masses are being increased from letdown/divert path while the corresponding RCS component outflow is not consistently applied in the component-authority equation used by conservation totals.

## E) Time/Order Defect Check (Post-RTCC)
Observed order in same tick (Regime 1 two-phase):
1. Ledger boundary mutation applied in regime block:
   - `ApplyPrimaryBoundaryFlowToLedger(...)`
   - Code: `Assets/Scripts/Validation/HeatupSimEngine.cs:1197`
2. Bubble DRAIN updates component masses and adds CVCS drain transfer:
   - `physicsState.RCSWaterMass += dm_cvcsActual`
   - Code: `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:440`
3. `UpdateRCSInventory(...)` returns early (no component boundary correction):
   - Code: `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:177`-`Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:189`
4. `UpdateVCT(...)` applies letdown/charging flows to VCT/BRS state and then conservation audit snapshots are computed:
   - Code: `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:211`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:291`-`Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:299`

Defect pattern: **time/order + authority split**. Ledger boundary mutation and component/audit mutation paths are not using a single synchronized primary-boundary authority in Regime 1 two-phase.

## 3) Classification
Primary class:
- **Time/order issue** (same-tick sequencing allows boundary math to be ledger-only while audit uses components)

Secondary class (supported by evidence):
- **Missing term** (component-space RCS letdown outflow counterpart missing when VCT/BRS inflows are counted)

Not supported as primary cause at onset:
- unit conversion error
- wrong sign on external net
- RTCC authority-swap discontinuity recurrence

## 4) Proposed Correction Class (No Implementation)

### Invariant
For every tick:
`(RCS + PZRw + PZRs + VCT + BRS)_t - (RCS + PZRw + PZRs + VCT + BRS)_0 = ExternalNetMass_t`
with a single canonical ownership/order for primary boundary flow application.

### Minimal correction approach
- Consolidate Regime 1 two-phase boundary handling so the **same boundary flow event** updates:
  - primary component authority used by audit totals, and
  - ledger authority,
  exactly once per tick and before conservation snapshot.
- Keep one explicit mapping table for each boundary term (internal vs external) and reuse it in both step-level and interval-level conservation pipelines.

### Risks / regressions to watch
- Reintroducing CVCS double-count during DRAIN or STABILIZE.
- Breaking RTCC pass at 8.25 hr while fixing post-8.50 drift.
- Divergence between VCT diagnostic equation and plant-wide conservation equation if boundary definitions remain split.

### Overlap
- Direct overlap with `CS-0050` (plant-wide accounting closure).
- Uses `CS-0051` as resolved prerequisite (transition boundary continuity now passing).

## Evidence Excerpts (short)
- `HeatupLogs/Heatup_Interval_035_8.50hr.txt`:
  - `Mass Conservation:FAIL (3669 lbm)`
  - `Interval Conservation:FAIL (3668.7 lbm, 0.397%)`
  - `Plant External Net: 0.0 gal`
- `HeatupLogs/Heatup_Interval_036_8.75hr.txt`:
  - `Interval Conservation:FAIL (12972.5 lbm, 1.404%)`
- `HeatupLogs/Unity_StageE_IP0016_final.log`:
  - repeated `VCT_CONS_DIAG` mismatches with large residuals, consistent with unsynchronized CVCS/VCT accounting paths.

## Recommendation
Proceed with an IP-0016 follow-on correction limited to accounting ownership/order in Regime 1 two-phase and harmonize VCT diagnostic boundary definitions with plant-wide conservation boundaries.

## Appendix A) Pre-Fix Flow / Mutation Map (For PBOC)
This appendix captures the pre-implementation ownership split that produced CS-0052.

| Boundary Subflow / Term | Computed At | Component Mutation Site(s) | Ledger / Accumulator Mutation Site(s) | Audit Consumption Site(s) | Diagnostic Consumption Site(s) | Pre-Fix Mismatch |
|---|---|---|---|---|---|---|
| Primary net boundary (`chargingToPrimary - primaryOutflow`) | `Assets/Scripts/Validation/HeatupSimEngine.cs:1706` | `Assets/Scripts/Validation/HeatupSimEngine.cs:1723` (`ApplyPrimaryBoundaryFlowToRcs`) | `Assets/Scripts/Validation/HeatupSimEngine.cs:1738` (`ApplyPrimaryBoundaryFlowToLedger`) | Step metric in `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:291`-`299`; interval metric in `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:388`-`391` | R1 drift warning in `Assets/Scripts/Validation/HeatupSimEngine.cs:1634`-`1640` | Regime 1/2/3 callsites use ledger-only pre-apply path while component path can be skipped |
| Regime 1 two-phase pre-apply callsite | `Assets/Scripts/Validation/HeatupSimEngine.cs:1197` | none at this callsite | ledger updated here | later audited from component masses | R1 drift log lines in Unity run log | Ledger decremented/incremented before audit, but component counterpart not guaranteed |
| `UpdateRCSInventory(...)` guard / early return | `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:171` | return path prevents component correction (`177`-`191`) | none | audit still reads component masses | contributes to R1 drift diagnostics | Early return can suppress required paired component term |
| VCT update path (letdown/charging/divert/makeup/cbo) | `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:211` + `Assets/Scripts/Physics/VCTPhysics.cs:157` | VCT/BRS state changes via `VCTPhysics.Update` and `BRSPhysics.*` | plant external accumulators in `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:265`-`269` | VCT/BRS masses in `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:286`-`291`; interval audit in `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:323`-`337` | VCT diag equation in `Assets/Scripts/Physics/VCTPhysics.cs:342`-`365` | VCT/BRS terms counted while primary component counterpart can be missing under pre-fix order split |
| BubbleFormation DRAIN transfer (`dm_cvcsActual`) | `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:415`-`440` | `PZRWaterMass -= dm_cvcsActual`, `RCSWaterMass += dm_cvcsActual` | none (internal transfer) | component totals consumed by both conservation equations | drain audit logs and VCT RCS accumulator in `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:447`-`449` | Internal transfer valid by itself, but combined with ledger-only boundary path creates structural drift |
| External boundary mass term (`externalNetMass_lbm`) | `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:265`-`296` | none directly | plant external in/out/net cumulative | step equation `...CVCS.cs:298`; interval expected mass `...Logging.cs:388` | VCT diag currently uses separate external definition in `Assets/Scripts/Physics/VCTPhysics.cs:362` | Not onset culprit at 8.50 hr, but diagnostic boundary mismatch adds ambiguity |
