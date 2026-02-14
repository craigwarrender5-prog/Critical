# Implementation Plan v5.0.3 — Inventory Audit Reconciliation Patch

**Version:** v5.0.3 (PATCH)
**Date:** 2026-02-12
**Changelog:** `Critical\Updates\Changelogs\CHANGELOG_v5.0.3.md`

---

## 1. Executive Summary

The Inventory Audit v1.1.0 reports a false mass conservation error of ~189,000 lbm (20.5%) beginning at the solid-to-two-phase pressurizer transition (~8.5 hr). The canonical v5.0.2 mass conservation system (`TotalPrimaryMassSolid`) and the VCT conservation tracking both remain correct throughout the simulation. The error is isolated to the `UpdateInventoryAudit()` method in `HeatupSimEngine.Logging.cs`, which falls back to an independent volume × density recalculation for RCS mass after bubble formation. This recalculation uses a fixed geometric RCS volume constant that does not account for the temperature-dependent density changes tracked by the canonical system's `RCSWaterMass` field. The result is a large phantom mass loss in the audit that does not correspond to any real mass movement.

This patch eliminates the false discrepancy by making the Inventory Audit consume the canonical mass values already maintained by the physics engine, rather than recomputing them independently.

---

## 2. Root Cause Analysis

### 2.1 The Defective Code Path

In `HeatupSimEngine.Logging.cs`, the method `UpdateInventoryAudit()` contains a branching mass calculation:

```csharp
if (solidPressurizer && !bubbleFormed)
{
    // CORRECT: Uses canonical v5.0.2 fields
    inventoryAudit.PZR_Water_Mass_lbm = physicsState.PZRWaterMassSolid;
    inventoryAudit.RCS_Mass_lbm = physicsState.TotalPrimaryMassSolid
                                - physicsState.PZRWaterMassSolid;
    inventoryAudit.PZR_Steam_Mass_lbm = 0f;
}
else
{
    // DEFECTIVE: Recomputes from V×ρ using fixed volume constant
    float rcsLoopVolume_ft3 = PlantConstants.RCS_WATER_VOLUME - PlantConstants.PZR_TOTAL_VOLUME;
    float rcsWaterDensity_audit = WaterProperties.WaterDensity(T_rcs, pressure);
    inventoryAudit.RCS_Mass_lbm = rcsLoopVolume_ft3 * rcsWaterDensity_audit;
    inventoryAudit.PZR_Water_Mass_lbm = pzrWaterVolume * pzrWaterDensity;
    inventoryAudit.PZR_Steam_Mass_lbm = pzrSteamVolume * pzrSteamDensity;
}
```

### 2.2 Why the V×ρ Calculation Is Wrong

The `else` branch computes RCS loop mass as:

```
RCS_Mass = (PlantConstants.RCS_WATER_VOLUME - PlantConstants.PZR_TOTAL_VOLUME) × ρ(T_rcs, P)
```

This treats the RCS loop volume as the fixed geometric constant `RCS_WATER_VOLUME` minus the fixed pressurizer volume. However, the actual RCS water mass tracked by the physics engine (`physicsState.RCSWaterMass`) is modified each timestep by CVCS boundary flows (charging, letdown, seal injection, seal return, CBO losses). By the time bubble formation occurs at ~8.5 hr, cumulative CVCS net flow has removed significant mass from the primary system. The V×ρ recalculation ignores all of this, effectively "reinventing" a full-volume mass that was never present.

### 2.3 Quantitative Confirmation

From the logs at 8.50 hr:

| Source | RCS Mass (lbm) |
|--------|----------------|
| Audit V×ρ recalculation | 594,733 |
| Engine `rcsWaterMass` (from `physicsState.RCSWaterMass`) | 701,179 |
| Difference | 106,446 |

Combined with the PZR mass discrepancy (audit 93,853 vs canonical 105,340 = -11,487 lbm), the total audit gap is ~118,000 lbm. The remaining ~6,000 lbm gap between 118,000 and the reported 124,000 is attributable to the audit's PZR V×ρ recalculation also diverging from the conserved PZR mass as density/volume relationships shift at the phase transition.

### 2.4 Why the v5.0.2 Canonical Values Are Correct

The v5.0.2 system maintains `TotalPrimaryMassSolid` as a boundary-only accumulator: it starts at the initial primary mass and is modified exclusively by CVCS net flow each timestep. `PZRWaterMassSolid` is updated exclusively by surge transfer. `RCSWaterMass` on the `physicsState` struct is similarly maintained by CVCS boundary flow application in all three physics regimes. These values are correct by construction — they track actual mass movement, not geometric approximations.

### 2.5 Secondary Issue: v5.0.2 Fields Stop Logging After Bubble Formation

The `SaveIntervalLog()` method only outputs the v5.0.2 conservation block when `solidPressurizer && !bubbleFormed`. The canonical `TotalPrimaryMassSolid` field continues to be maintained during solid ops but is never updated after bubble formation. However, `physicsState.RCSWaterMass` is updated in all three regimes by CVCS pre-application. This field, combined with the engine's `pzrWaterVolume` / `pzrSteamVolume` and their corresponding densities (already computed by the CoupledThermo solver and stored on `physicsState`), provides the authoritative primary mass throughout the full simulation.

---

## 3. Design Constraints

1. The v5.0.2 canonical conservation system (`TotalPrimaryMassSolid`, `PZRWaterMassSolid`, `physicsState.RCSWaterMass`) must not be modified.
2. No changes to SG, RCP, CVCS, PZR, BubbleFormation, or draining logic.
3. No changes to `SolidPlantPressure.cs`, `PressurizerPhysics.cs`, `CoupledThermo.cs`, or `RCSHeatup.cs`.
4. No changes to `HeatupSimEngine.cs` (main engine), `HeatupSimEngine.Init.cs`, `HeatupSimEngine.BubbleFormation.cs`, `HeatupSimEngine.CVCS.cs`, `HeatupSimEngine.Alarms.cs`, or `HeatupSimEngine.HZP.cs`.
5. No new physics features, systems, or state variables.
6. The `InventoryAuditState` struct may be extended with new fields for diagnostics but no existing fields may be removed.
7. Thermal behavior, pressure behavior, CVCS flows, SG model, RCP heat balance, and all other simulation physics must remain byte-identical.
8. All validation visual display code that reads `InventoryAuditState` fields must continue to compile and function.

---

## 4. Proposed Correction Strategy

### 4.1 Core Fix

Replace the defective V×ρ branch with canonical regime-aware mass source selection. Retain regime-aware canonical selection rather than assuming `physicsState.PZRWaterMass` is valid in solid ops.

During solid ops (`solidPressurizer && !bubbleFormed`), `physicsState.PZRWaterMass` is not actively maintained — only `physicsState.PZRWaterMassSolid` is canonical (updated exclusively by surge transfer in `SolidPlantPressure`). Conversely, after bubble formation, `PZRWaterMassSolid` is no longer updated — `physicsState.PZRWaterMass` becomes the canonical PZR mass (maintained by the CoupledThermo solver in Regimes 2/3). The audit must select the correct canonical source for each regime:

**Solid ops** (`solidPressurizer && !bubbleFormed`):
- **PZR water mass:** `physicsState.PZRWaterMassSolid` (surge-conserved, canonical)
- **PZR steam mass:** `0f` (no steam space exists)
- **RCS loop mass:** `physicsState.TotalPrimaryMassSolid - physicsState.PZRWaterMassSolid` (derived, conservation guaranteed by construction)

**Two-phase ops** (after bubble formation):
- **PZR water mass:** `physicsState.PZRWaterMass` (maintained by CoupledThermo solver)
- **PZR steam mass:** `physicsState.PZRSteamMass` (maintained by CoupledThermo solver)
- **RCS loop mass:** `physicsState.RCSWaterMass` (maintained by CVCS boundary flow pre-application in all three regimes)

This eliminates the V×ρ recalculation entirely in both branches. The audit becomes a pure consumer of canonical state, selecting the correct authoritative field for each operating regime.

### 4.2 Audit Source Field

Add a `string AuditMassSource` field to `InventoryAuditState` to log which source path was used (for future diagnostics). This will always read `"CANONICAL"` after the fix.

### 4.3 Interval Log Enhancement

Continue logging the existing `MASS INVENTORY` block and the `INVENTORY AUDIT` block. Add a one-line source annotation to the audit block indicating the mass source (`CANONICAL (physicsState)`).

---

## 5. Stage Breakdown

### Stage 1: Fix `UpdateInventoryAudit()` Mass Calculation

**File:** `HeatupSimEngine.Logging.cs`

**Actions:**

1. Retain the `if (solidPressurizer && !bubbleFormed)` / `else` branch structure in `UpdateInventoryAudit()`, but replace the mass source in **both** branches with canonical regime-aware fields (eliminating all V×ρ recalculation).
2. **Solid ops branch** (`solidPressurizer && !bubbleFormed`) — already correct, no change needed:
   - `inventoryAudit.PZR_Water_Mass_lbm = physicsState.PZRWaterMassSolid;`
   - `inventoryAudit.RCS_Mass_lbm = physicsState.TotalPrimaryMassSolid - physicsState.PZRWaterMassSolid;`
   - `inventoryAudit.PZR_Steam_Mass_lbm = 0f;`
3. **Two-phase branch** (`else`) — replace V×ρ recalculation with canonical fields:
   - `inventoryAudit.RCS_Mass_lbm = physicsState.RCSWaterMass;` (replaces `rcsLoopVolume_ft3 * rcsWaterDensity_audit`)
   - `inventoryAudit.PZR_Water_Mass_lbm = physicsState.PZRWaterMass;` (replaces `pzrWaterVolume * pzrWaterDensity`)
   - `inventoryAudit.PZR_Steam_Mass_lbm = physicsState.PZRSteamMass;` (replaces `pzrSteamVolume * pzrSteamDensity`)
4. Add `AuditMassSource` string field to `InventoryAuditState` struct (default `"CANONICAL"`).
5. Set `inventoryAudit.AuditMassSource` to `"CANONICAL_SOLID"` in the solid ops branch and `"CANONICAL_TWO_PHASE"` in the two-phase branch.
6. Remove the now-unused local variables `rcsLoopVolume_ft3`, `rcsWaterDensity_audit`, and the duplicate `pzrWaterDensity`/`pzrSteamDensity` locals that were only used by the defective V×ρ recalculation.
7. Retain the VCT and BRS mass calculations unchanged (these are correct — they use their own independent state).

**Does not touch:** Any file other than `HeatupSimEngine.Logging.cs`. No physics modules, no engine dispatch, no visual code.

### Stage 2: Update Interval Log Annotations

**File:** `HeatupSimEngine.Logging.cs`

**Actions:**

1. In `SaveIntervalLog()`, within the `INVENTORY AUDIT` log block, add a source annotation line after the `TOTAL MASS` line:
   ```
   Mass Source:        {inventoryAudit.AuditMassSource}
   ```
   This will display `CANONICAL_SOLID` during solid ops and `CANONICAL_TWO_PHASE` after bubble formation.
2. In the `CONSERVATION CHECK` sub-block, no changes needed — the error calculation already uses `inventoryAudit.Total_Mass_lbm` vs `inventoryAudit.Expected_Total_Mass_lbm`, which will now be correct.

**Does not touch:** Any file other than `HeatupSimEngine.Logging.cs`. No changes to the v5.0.2 solid-ops forensics block or the `MASS INVENTORY` display block.

### Stage 3: Validation Build and Test

**Actions:**

1. Build the project. Confirm zero compile errors.
2. Run the heatup simulation from cold shutdown through at least 12 hr sim time (past bubble formation at ~8.89 hr and through RCP start).
3. Validate all criteria in Section 6 against the interval logs.
4. Confirm no changes to thermal, pressure, CVCS, SG, or RCP behavior by comparing key thermal/pressure/flow values at matching timestamps against the v5.0.2 baseline logs.

---

## 6. Validation Criteria

| # | Criterion | Threshold | Method |
|---|-----------|-----------|--------|
| V1 | Inventory Audit conservation error | ≤ 0.1% throughout 0–16 hr | Read `Conservation_Error_pct` from every interval log |
| V2 | No discontinuity at bubble formation | Audit `Total_Mass_lbm` changes by < 500 lbm between 8.50 hr and 9.00 hr logs | Compare consecutive interval logs spanning bubble formation |
| V3 | VCT conservation unchanged | `Conservation Err` in MASS CONSERVATION block identical (±0.01 gal) to v5.0.2 baseline at matching timestamps | Compare against v5.0.2 baseline logs |
| V4 | Thermal behavior unchanged | `T_avg`, `T_pzr`, `T_rcs` identical (±0.01°F) to v5.0.2 baseline at matching timestamps | Compare against v5.0.2 baseline logs |
| V5 | SG behavior unchanged | `T_sg_secondary`, `sgHeatTransfer_MW`, boiling state identical to v5.0.2 baseline | Compare against v5.0.2 baseline logs |
| V6 | CVCS behavior unchanged | `chargingFlow`, `letdownFlow`, `surgeFlow`, `vctState.Level_percent` identical to v5.0.2 baseline | Compare against v5.0.2 baseline logs |
| V7 | RCP heat balance unchanged | `rcpHeat`, `effectiveRCPHeat`, `rcpCount` identical to v5.0.2 baseline | Compare against v5.0.2 baseline logs |
| V8 | No new compile warnings | Zero new warnings beyond existing baseline | Build output |
| V9 | Audit `Conservation_Alarm` never triggers | `Alarm: NO` in all interval logs 0–16 hr | Read every interval log |
| V10 | Audit mass source annotated | `Mass Source: CANONICAL_SOLID` or `CANONICAL_TWO_PHASE` present in every interval log (regime-appropriate) | Read every interval log |

**Note on V3–V7:** Since this patch modifies only the audit's mass *reading* path (not any physics calculation), thermal/pressure/flow values should be bit-identical to the v5.0.2 run. Any deviation indicates an unintended side effect and is a FAIL.

---

## 7. Files Modified

| File | Change Type | Description |
|------|-------------|-------------|
| `Assets\Scripts\Validation\HeatupSimEngine.Logging.cs` | MODIFY | Fix `UpdateInventoryAudit()` mass source; add `AuditMassSource` field to `InventoryAuditState`; update `SaveIntervalLog()` annotation |

**No other files are modified.**

---

## 8. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| `physicsState.RCSWaterMass` is stale or uninitialized at audit time | LOW | HIGH | `RCSWaterMass` is updated every timestep in all three regimes before `UpdateInventoryAudit()` is called (Step 8 in `StepSimulation()`). Verified by code inspection of call order. |
| `physicsState.PZRWaterMass` / `PZRSteamMass` incorrect during solid ops | N/A | N/A | **Mitigated by design.** The regime-aware branch structure explicitly uses `PZRWaterMassSolid` and `TotalPrimaryMassSolid` during solid ops, and `PZRWaterMass` / `PZRSteamMass` / `RCSWaterMass` only during two-phase ops. Each field is read only in the regime where it is canonically maintained. |
| Display code in `HeatupValidationVisual.TabPressurizer.cs` or similar reads removed fields | VERY LOW | LOW | No fields are removed from `InventoryAuditState`. Only one field is added (`AuditMassSource`). All existing display code continues to compile. |
| Audit total mass jumps at regime transitions | LOW | MEDIUM | Since all three regimes now feed the same `physicsState` fields, and CVCS pre-application ensures mass continuity, this should not occur. Validation criterion V2 explicitly checks for this. |

---

## 9. Unaddressed Issues

| Issue | Reason | Disposition |
|-------|--------|-------------|
| `TotalPrimaryMassSolid` not updated after bubble formation | The solid-ops canonical field is only relevant during solid ops. After bubble formation, `physicsState.RCSWaterMass` + `physicsState.PZRWaterMass` + `physicsState.PZRSteamMass` is the canonical primary mass. No change needed. | Not applicable to this patch. |
| v5.0.2 forensics block not logged after bubble formation | The `SaveIntervalLog()` block guarded by `if (solidPressurizer && !bubbleFormed)` only shows during solid ops. This is cosmetic — the audit block provides equivalent information post-bubble. | Planned for future release. Added to Future_Features. |
| RCS heatup rate exceeds 50°F/hr post-RCP start (54.8°F/hr at 12 hr) | Separate issue. Not related to mass accounting. May require heatup rate check suppression during RCP start transient or RCP start throttling. | Planned for future release. Added to Future_Features. |
| SG heat sink causes RCS cooldown at 15.5 hr (-16°F/hr) | Separate issue. Not related to mass accounting. May require SG drain schedule tuning relative to RCP heat input. | Planned for future release. Added to Future_Features. |
| VCT Level validation shows persistent FAIL during divert | VCT divert is correct behavior during thermal expansion. The validation check threshold may need adjustment. | Out of scope. Not related to mass accounting. |
| PZR Level ±10% validation FAIL during solid ops | By design — solid pressurizer holds at 100% vs 25% setpoint. This is expected. | Not a bug. No change needed. |
| `physicsState.PZRWaterMass` fidelity during solid ops Regime 1 | N/A | **Mitigated by design.** The regime-aware branch explicitly avoids reading `physicsState.PZRWaterMass` during solid ops. The solid ops branch reads `PZRWaterMassSolid` (canonical). No guard needed. |

---

## 10. Versioning Justification

**v5.0.3** — PATCH level increment.

- No new features.
- No API changes.
- No physics behavior changes.
- Fixes a reporting/diagnostic bug in the Inventory Audit that produces false alarms.
- The `InventoryAuditState` struct gains one new field (`AuditMassSource`) which is additive and non-breaking.
- All existing consumers of `InventoryAuditState` continue to function without modification.

---

Awaiting Stage 1 implementation approval.
