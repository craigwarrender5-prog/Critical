# STAGE 3C — VALIDATION COVERAGE GAPS BY SUBSYSTEM
## Recovery Audit 2026-02-13

---

## Purpose

For each subsystem, this document answers: **"What would catch it if this subsystem goes wrong?"** by cross-referencing Stage 2's VALIDATION_MAP against the Stage 3A mass authority findings.

---

## 1. CoupledThermo Solver

### Existing Checks
- **CTEST-001 through CTEST-007:** 7 static validation tests (10°F response, mass conservation, volume conservation, convergence, steam minimum, heatup range). Called from Phase1TestRunner only.
- **Runtime:** Convergence warning logged if solver doesn't converge (HeatupSimEngine.cs:1421).
- **DEBUG only:** Conservation identity check in canonical mode path (CoupledThermo.cs:264-272) — dead code since legacy mode is always taken.

### Gaps
| Gap | Severity | What's Missing |
|-----|----------|----------------|
| **No runtime mass conservation check** | CRITICAL | The solver runs in LEGACY mode (V×ρ), but nobody checks whether `M_total_out ≈ M_total_in` after each solve. The static test (CTEST-003) only runs at steady-state conditions. |
| **No convergence rate monitoring** | MEDIUM | `IterationsUsed` is set but never logged or checked at runtime (except the non-convergence warning). High iteration counts could indicate approaching instability. |
| **Static tests don't cover heatup trajectory** | MEDIUM | CTEST-007 tests one point (300°F/400 psia). No test covers the full heatup arc (100°F → 547°F, 15 psia → 2235 psia) with varying RCP counts. |

### Suggested Minimal Validation
1. **Runtime mass delta check:** After each `SolveEquilibrium` call, compute `|M_total_out - M_total_in|`. Log WARNING if > 10 lbm, ALARM if > 100 lbm. Location: RCSHeatup.cs:162 (after solver call).
2. **Iteration count logging:** Log if `IterationsUsed > 30` (approaching MAX_ITERATIONS=50).

---

## 2. RCS / Primary Coolant System

### Existing Checks
- **CHECK-001 (massError_lbm):** System-level conservation (CVCS.cs:300). Thresholds: 100/500 lbm.
- **CHECK-003 (InventoryAudit):** Full system inventory with cumulative flow tracking. Threshold: 500 lbm / 0.5%.
- **ALM-05/06:** Pressure Low (350 psia) / High (2300 psia).
- **ALM-07/08/09:** Subcooling margin (30°F / 15°F / 0°F).
- **UI-03:** Heatup rate ≤ 50°F/hr.
- **UI-06:** Pressure rate < 200 psi/hr.

### Gaps
| Gap | Severity | What's Missing |
|-----|----------|----------------|
| **No RCS-only mass conservation** | HIGH | `massError_lbm` is system-level (includes VCT+BRS). No check isolates the primary system. `UpdatePrimaryMassLedgerDiagnostics()` would fill this gap but is dead code. |
| **No RCS volume consistency check** | LOW | RCS volume is fixed at `PlantConstants.RCS_WATER_VOLUME`. Nobody checks if the solver's output volume differs from this constant. |

### Suggested Minimal Validation
1. Resurrect `UpdatePrimaryMassLedgerDiagnostics()` per Stage 3B.

---

## 3. Pressurizer (PZR)

### Existing Checks
- **ALM-01/02:** PZR Level Low (20%) / High (85%). Suppressed during solid ops.
- **ALM-03:** Steam Bubble OK (level ∈ 5-95%).
- **UI-05:** PZR Level In Band (± 15% from setpoint).
- **CTEST-006:** Steam space minimum clamp (static test).
- **Guard 6F:** Low-level letdown isolation.
- **Guard 6B:** Heater mode transition at ~2200 psia.

### Gaps
| Gap | Severity | What's Missing |
|-----|----------|----------------|
| **No PZR mass balance check** | HIGH | PZR water + steam mass should equal `TotalPrimaryMass_lb - RCSWaterMass`. With ledger frozen (ISSUE-0002), this identity is not enforced or checked. |
| **No PZR volume ≤ total check** | LOW | `PZRWaterVolume + PZRSteamVolume` should always equal `PZR_TOTAL_VOLUME`. The solver enforces this, but no runtime assertion verifies it. |
| **No surge flow conservation** | MEDIUM | Surge flow is computed as a display value but nobody checks if surge flow × dt matches PZR volume change. |

### Suggested Minimal Validation
1. **PZR volume identity:** Assert `|PZRWaterVolume + PZRSteamVolume - PZR_TOTAL_VOLUME| < 0.01 ft³` each timestep.
2. **PZR mass subset:** Assert `PZRWaterMass + PZRSteamMass ≤ TotalPrimaryMass_lb` (or component sum if ledger is stale).

---

## 4. CVCS (Chemical & Volume Control System)

### Existing Checks
- **CHECK-001 (massError_lbm):** Captures CVCS's net effect on system mass.
- **CHECK-004 (VCT Conservation):** `|ΔV_vct + ΔV_rcs - externalNet| < threshold` (10/50 gal).
- **CHECK-008:** Double-count guard flag (`regime3CVCSPreApplied`).
- **UI-07:** Seal Injection OK.
- **UI-08:** Letdown Not Isolated.

### Gaps
| Gap | Severity | What's Missing |
|-----|----------|----------------|
| **CVCS pre-applied mass not tracked in ledger** | CRITICAL | CVCS writes to `RCSWaterMass` at lines 1182/1344, but `TotalPrimaryMass_lb` is never updated. If canonical mode were active, this would break conservation. See ISSUE-0001/0002. |
| **No charging/letdown flow rate validation** | LOW | Charging and letdown flow rates are set by the PI controller. No bounds check prevents physically impossible flow rates (e.g., > pump capacity). |
| **CBO loss not cross-checked** | LOW | CBO (Controlled Bleed-Off) loss is applied to the inventory audit's flow tracking (Logging.cs:376) but it's not clear if it's actually subtracted from the system mass anywhere in the physics. |

### Suggested Minimal Validation
1. When ISSUE-0001 is fixed: ensure CVCS pre-application updates the ledger, not just RCSWaterMass.
2. Flow rate bounds: assert `chargingFlow ≤ PlantConstants.MAX_CHARGING_GPM`.

---

## 5. VCT (Volume Control Tank)

### Existing Checks
- **CHECK-004:** VCT Conservation Cross-Check (VCTPhysics.cs:346-389). Threshold: 100 gal triggers diagnostic.
- **ALM-14:** VCT Level Low / High (edge-detected).
- **UI-02:** VCT Flow Imbalance (10/50 gal three-state).
- **UI-09:** VCT Level In Normal Band.

### Gaps
| Gap | Severity | What's Missing |
|-----|----------|----------------|
| **VCT conservation during solid ops** | MEDIUM | VCTPhysics.cs:386-389 notes "SUSPECT: rcsChange≈0 but vctChange large. Possibly in solid ops." During solid ops, RCS mass changes are tracked via `TotalPrimaryMassSolid`, but the VCT verifier reads `CumulativeRCSChange_gal` which may not be updated in solid regime. |
| **No VCT mass (lbm) check** | LOW | VCT conservation is in gallons, not lbm. If water density assumption (100°F atmospheric) is wrong, the lbm conversion in the inventory audit (Logging.cs:324-325) would drift from the gallon-based check. |

### Suggested Minimal Validation
1. Cross-check: VCT mass in inventory audit (Logging.cs:325) should equal VCT volume × assumed density. Log if assumed density deviates from actual by > 5%.

---

## 6. SG (Steam Generator — Multi-Node Thermal)

### Existing Checks
- **AT-10:** SG Isolated Boiling Pressure Rise Test (acceptance test, formula-only).
- Various display fields synced from `sgMultiNodeState` (lines 1387-1411).
- SG boiling/draining flags displayed.

### Gaps
| Gap | Severity | What's Missing |
|-----|----------|----------------|
| **No SG secondary energy balance** | HIGH | No check validates that SG heat removal (MW) is consistent with primary-side temperature change. The SG model could output arbitrary heat removal and nobody would catch it until T_rcs goes unrealistic. |
| **No SG mass conservation** | MEDIUM | SG secondary water mass and draining mass are displayed but not checked against initial conditions. |
| **No SG pressure bounds** | MEDIUM | SG secondary pressure is computed (sgSecondaryPressure_psia) but no alarm exists for excessive pressure (safety valve setpoint). |
| **AT-10 is formula-only** | MEDIUM | The acceptance test validates the concept but doesn't run the actual SG model. |

### Suggested Minimal Validation
1. **SG heat removal sanity:** Assert `sgHeatTransfer_MW ≥ 0` and `sgHeatTransfer_MW ≤ grossHeatInput × 2` (can't remove more than input + stored energy).
2. **SG pressure alarm:** Add alarm if `sgSecondaryPressure_psia > SG_SAFETY_VALVE_SETPOINT` (typically ~1085 psia).
3. **SG mass conservation:** Track `initialSecondaryMass + cumMakeup - cumDrained - cumBoiloff` vs current mass.

---

## 7. BRS (Boron Recycle System)

### Existing Checks
- **Guard 6D:** Holdup capacity clamp (BRSPhysics.cs:189).
- **Guard 6E:** Boron concentration guard (BRSPhysics.cs:260).
- Alarm flags in BRS state (BRSPhysics.cs:87-88), but no alarm edge detection in Alarms.cs.

### Gaps
| Gap | Severity | What's Missing |
|-----|----------|----------------|
| **No BRS alarms in AlarmManager** | LOW | BRS alarm flags exist in the state struct but are never checked by `AlarmManager.CheckAlarms()`. |
| **No BRS mass conservation** | LOW | BRS tracks holdup + distillate + concentrate volumes but nobody checks if they sum correctly against cumulative in/out flows. |

### Suggested Minimal Validation
1. Add BRS holdup high-level alarm to AlarmManager (if holdup > tank capacity × 90%).

---

## 8. RHR (Residual Heat Removal)

### Existing Checks
- **Guard 6C:** RHR isolation on RCP start (HeatupSimEngine.cs:972-976).
- RHR heat addition to T_rcs in Regime 1 (lines 1117-1121).

### Gaps
| Gap | Severity | What's Missing |
|-----|----------|----------------|
| **No RHR isolation pressure interlock** | MEDIUM | Real RHR systems isolate at ~350 psia (RCS pressure interlock). The code isolates on RCP count, not pressure. |
| **No RHR heat balance check** | LOW | RHR pump heat and HX removal are applied to T_rcs but never validated against expected values. |

### Suggested Minimal Validation
1. Add pressure-based RHR isolation check: if `pressure > 350 psia && RHR active → WARNING`.

---

## 9. Instrumentation (RVLIS, SMM)

### Existing Checks
- **ALM-10:** RVLIS Level Low (90%, full range valid).
- **ALM-07/08/09:** SMM checks (30°F / 15°F / 0°F).
- **UI-10:** RVLIS Level OK.
- RVLIS calculation delegates to `RVLISPhysics.Calculate()`.

### Gaps
| Gap | Severity | What's Missing |
|-----|----------|----------------|
| **RVLIS depends on RCSWaterMass** | MEDIUM | `RVLISPhysics.Calculate(physicsState.RCSWaterMass, ...)` (Alarms.cs:220). If RCSWaterMass has a V×ρ error (from LEGACY solver mode), RVLIS will report incorrectly. This is the AT-07 concern. |
| **No cross-check of RVLIS vs PZR level** | LOW | RVLIS and PZR level should be correlated (both reflect RCS inventory). No check validates they move together. |

### Suggested Minimal Validation
1. After ISSUE-0001 fix: verify RVLIS uses canonical RCSWaterMass (remainder from ledger).

---

## 10. Controls & State Machines

### Existing Checks
- **Guard 6B:** Heater mode transition (PRESSURIZE_AUTO → AUTOMATIC_PID at ~2200 psia).
- **Guard 6G:** Bubble formation 7-phase state machine with transition guards.
- **Guard 6H:** Regime selection (α-based).
- **Guard 6I:** Sim time budget cap (5 sim-minutes/frame).

### Gaps
| Gap | Severity | What's Missing |
|-----|----------|----------------|
| **No backward transition detection** | LOW | The bubble formation state machine should only go forward (NONE→DETECTION→...→COMPLETE). No guard prevents backward transitions if state is corrupted. |
| **No regime transition logging** | MEDIUM | When α changes between regimes (0→blended→1), there's no event logged. Mode transitions are logged but regime transitions aren't. |

### Suggested Minimal Validation
1. Log regime transitions: "REGIME CHANGE: {old} → {new} (α = {value})".

---

## 11. Acceptance Tests (Cross-Cutting)

### Existing: 10 tests in AcceptanceTests_v5_4_0.cs

### Critical Gap
| Gap | Severity | What's Missing |
|-----|----------|----------------|
| **All tests are formula-only** | HIGH | Every test passes by validating calculation correctness, not by running the actual simulation. Each has a "REQUIRES SIMULATION" note. No end-to-end test harness exists. |

### Suggested Minimal Runtime Harness

The acceptance tests could be made real by:

1. **Embedding test points in the simulation loop.** At specific simTime milestones, record state snapshots. At simulation end, compare against AT criteria.
2. **Minimal approach:** Run normal heatup, at the end check:
   - AT-02: `|TotalPrimaryMass_lb_final - TotalPrimaryMass_lb_initial| < 60 lb` (if balanced CVCS)
   - AT-03: Record mass at bubble formation transition
   - AT-08: Track max PZR level Δ per timestep throughout
3. **Integration with existing logging:** The interval log (every 15 min) already records mass conservation. Parse the log file post-run to check acceptance criteria.

---

## SUMMARY: PRIORITY GAP RANKING

| Priority | Gap | Subsystem | ISSUE |
|----------|-----|-----------|-------|
| 1 | Canonical mass mode never activated | CoupledThermo/Engine | ISSUE-0001 |
| 2 | Ledger freezes in Regime 2/3 | Engine | ISSUE-0002 |
| 3 | Primary mass diagnostic dead | Engine/Logging | ISSUE-0002 (resurrect) |
| 4 | Boundary accumulators never incremented | SystemState | ISSUE-0003 |
| 5 | No runtime solver mass conservation | CoupledThermo | New |
| 6 | No SG energy balance validation | SG | New |
| 7 | Acceptance tests formula-only | Tests | Existing AT note |
| 8 | CVCS pre-applied mass not ledger-tracked | CVCS | ISSUE-0001 corollary |
| 9 | No PZR mass balance check | PZR | New |
| 10 | SG pressure alarm missing | SG | New |
