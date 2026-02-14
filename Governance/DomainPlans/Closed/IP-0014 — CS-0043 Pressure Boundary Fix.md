> LEGACY IMPLEMENTATION ARTIFACT — archived during Constitution v1.0 governance re-baseline.
> Historical record preserved. No current execution authority.

# IP-0014 â€” CS-0043 Pressure Boundary Corrective Fix
## Pressurizer Energy Path Correction for Two-Phase Regime 1

**Issue:** CS-0043 (Critical)
**Version:** v0.3.2.0
**Date:** 2026-02-14
**Authorization:** `AUTHORIZED TO IMPLEMENT: CS-0043 corrective fix`
**Approach:** Option A (Minimal) â€” Bypass IsolatedHeatingStep PZR branch during two-phase

---

## 1. Problem Summary

During Regime 1 (bubble exists, no RCPs), heater energy is consumed twice:
1. As sensible heat dT in `IsolatedHeatingStep` (capped and discarded by T_sat)
2. As latent heat steam generation in `UpdateDrainPhase` (applied to mass transfer)

The T_sat cap in `IsolatedHeatingStep` combined with the Psat(T_pzr) override at HeatupSimEngine.cs:1143 creates a ratchet: any conduction/insulation loss below T_sat causes P_new < P_old, and T_sat(P_new) < T_sat(P_old), locking in monotonic decline.

## 2. Approach Selection: Option A (Minimal)

The investigation report recommended Option B (ThreeRegionUpdate). After code review, Option A is the better choice for this patch because:

- **ThreeRegionUpdate has incompatible inputs**: It requires `surgeFlow_gpm`, `sprayFlow_gpm`, `sprayTemp_F`, and `heaterPower_kW` with its own thermal lag tracking. Regime 1 does not have forced surge/spray flow. Adapting it would require significant interface changes â€” scope creep.
- **The root cause is clearly double-counting**: The simplest correct fix is to ensure heater energy is applied once, not twice. During two-phase, `UpdateDrainPhase` already has the correct latent heat path. `IsolatedHeatingStep` should not also consume that energy.
- **Option A directly eliminates the failure mechanism**: Skip PZR dT in `IsolatedHeatingStep` when two-phase is active. Set T_pzr = T_sat(P) directly (saturation lock). Remove the Psat override ratchet. Let `UpdateDrainPhase` be the sole energy consumer for PZR heating during DRAIN.

Option B/C remain valid future improvements and can be filed as deferred items.

## 3. Implementation Stages

### Stage 1: Modify `IsolatedHeatingStep` â€” Add Two-Phase Bypass

**File:** `Assets/Scripts/Physics/RCSHeatup.cs`

Add a `bool twoPhaseActive` parameter to `IsolatedHeatingStep`. When true:
- Skip PZR sensible heat computation (dT = Q/mCp) entirely
- Set `result.T_pzr = T_sat(pressure)` (PZR water locked to saturation)
- Set `result.Pressure = pressure` (no thermal expansion dP â€” pressure set elsewhere)
- Set `result.SurgeFlow = 0f` (no thermal expansion surge during two-phase)
- RCS temperature update proceeds normally (surge line conduction still active)

The subcooled path (twoPhaseActive = false) is completely unchanged.

### Stage 2: Update Regime 1 Caller â€” Pass Two-Phase Flag & Remove Psat Override

**File:** `Assets/Scripts/Validation/HeatupSimEngine.cs`

At the Regime 1 call site (~line 1124):
1. Compute `twoPhaseActive` flag (same condition already at line 1138-1140)
2. Pass it to `IsolatedHeatingStep`
3. **Remove** the Psat override block (lines 1134-1144) â€” it is no longer needed because:
   - When two-phase, `IsolatedHeatingStep` returns T_pzr = T_sat(P) and P unchanged
   - Pressure during DRAIN is governed by the saturation lock: T_pzr = T_sat(P), so P = Psat(T_pzr) = P (identity, stable)
   - Pressure changes come from `UpdateDrainPhase` steam generation effects, applied through mass balance
4. After removing the Psat override, add a saturation temperature lock: during two-phase, pressure should reflect steam mass balance. Since UpdateDrainPhase does mass-conserving phase change but doesn't set pressure directly, and we need pressure stability during DRAIN, we maintain P = Psat(T_pzr) but now T_pzr = T_sat(P_current) each step â€” this is the identity (no ratchet) since IsolatedHeatingStep no longer drags T_pzr below T_sat.

**Key insight:** The ratchet was caused by conduction/insulation losses pulling T_pzr below T_sat(P_old) inside IsolatedHeatingStep. With the bypass, T_pzr is directly set to T_sat(P), so Psat(T_sat(P)) = P. No ratchet.

### Stage 3: Eliminate Double Energy Application in DRAIN

**File:** `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs`

`UpdateDrainPhase()` (line 339) computes:
```
heaterPower_BTU_sec = pzrHeaterPower * PlantConstants.MW_TO_BTU_SEC
steamGenRate_lb_sec = heaterPower_BTU_sec / h_fg
```

This is now the **sole** consumer of heater energy during two-phase. No changes needed to UpdateDrainPhase itself â€” it already does the right thing. The fix is entirely upstream (Stage 1 & 2 prevent IsolatedHeatingStep from also consuming the energy).

**However**, conduction and insulation losses from PZR should reduce the effective heater power available for steam generation. Currently these losses are computed in IsolatedHeatingStep but with the bypass, they are skipped for PZR. We should subtract PZR conduction and insulation losses from the heater power used for steam generation in UpdateDrainPhase:

```
netHeaterPower = heaterPower - conductionLoss - insulationLoss
steamGenRate = max(0, netHeaterPower / h_fg)
```

This ensures energy conservation: heater power minus losses = energy available for steam generation.

### Stage 4: Update Regime 2 Call Site (If Applicable)

**File:** `Assets/Scripts/Validation/HeatupSimEngine.cs`

Regime 2 (~line 1224) also calls `IsolatedHeatingStep`. In Regime 2, RCPs are running and bubble always exists â†’ pass `twoPhaseActive = true` for the isolated contribution. This is consistent â€” the isolated path's PZR energy is handled by ThreeRegionUpdate in the coupled path anyway.

**Decision:** Pass `twoPhaseActive: false` for Regime 2 isolated call to avoid changing Regime 2 behavior. Regime 2's pressure is set by the coupled solver, not the isolated result. The isolated result is blended by (1-Î±) which goes to zero. Minimal risk.

## 4. Version Rationale

**v0.3.2.0** â€” This is a Patch (not Revision) because it corrects a critical regime logic error in the existing bubble formation domain. It fixes the energy routing architecture within the established domain, but does not introduce a new subsystem boundary.

## 5. Files Modified

| File | Change |
|------|--------|
| `RCSHeatup.cs` | Add `twoPhaseActive` parameter; bypass PZR dT when true |
| `HeatupSimEngine.cs` | Pass two-phase flag; remove Psat override ratchet |
| `HeatupSimEngine.BubbleFormation.cs` | Subtract PZR conduction/insulation losses from heater power for steam gen |

## 6. Files NOT Modified

| File | Reason |
|------|--------|
| `PressurizerPhysics.cs` | `TwoPhaseHeatingUpdate` not called during Regime 1; no change needed |
| `PlantConstants.Pressurizer.cs` | No new constants needed |
| `SolidPlantPressure.cs` | Pre-bubble code path, unaffected |
| `CVCSController.cs` | CVCS flow balance unaffected |

## 7. Risks

| Risk | Mitigation |
|------|------------|
| Regime 0 regression | Two-phase bypass guarded by `twoPhaseActive` flag; Regime 0 never sets this |
| Regime 2/3 regression | Not changing Regime 2 IsolatedHeatingStep behavior; Regime 3 doesn't call it |
| Pressure unstable during DRAIN | T_pzr locked to T_sat(P) makes Psat(T_pzr) = P (identity); net conduction/insulation losses handled by reduced steam gen rate, pressure naturally stable |
| CS-0036 (DRAIN duration) | May improve â€” correct energy routing means correct steam gen rate |

## 8. Validation Criteria

After fix, Stage E long-run should show:
1. Pressure stable or slowly rising during DRAIN (not collapsing)
2. T_pzr tracking T_sat(P) during two-phase
3. Mass conservation maintained (system mass delta < 0.1 lbm/step)
4. DRAIN completes within reasonable time (~30-60 min)
5. No regression in Regime 0 (pre-bubble behavior identical)

## 9. Artifacts Required

- [x] Implementation Plan (this document)
- [ ] CHANGELOG_v0.3.2.0.md
- [ ] Forensics documentation (post-validation)
- [ ] ISSUE_REGISTRY.md update (CS-0043 status â†’ In Progress)

