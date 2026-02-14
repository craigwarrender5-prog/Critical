# Post-Stabilization Triage — v5.4.1 Baseline
## Critical: Master the Atom -- NSSS Simulator

**Triage Date:** 2026-02-13
**Baseline Version:** v5.4.1 (Complete)
**Triage Scope:** SG system, mass conservation, VCT, RCP transients, secondary boundary
**Template:** `FUTURE_FEATURE_TEMPLATE.md` v1.0
**Governing Document:** `PROJECT_CONSTITUTION.md`

---

## Version Baseline Notice

**v5.4.1 is the current active baseline** (completed 2026-02-13). All forward-looking references in this document are baselined against v5.4.1. The v5.4.0 release established the canonical mass ledger architecture and staged implementation plan, but several residual items from its original scope remain unresolved. Those residuals are captured in FF-05 and targeted for v5.4.2.0 (the next Phase 0 patch). References to "Phase 0 exit" throughout this document mean completion of FF-05/v5.4.2.0, not v5.4.0.

---

## Triage Methodology

This document captures all known physics and architectural issues identified through static code review of the v5.4.1 codebase. Each issue is:

1. **Classified** as: Critical Physics Blocker / Architectural Debt / Performance-Scaling Concern / Observability-UX Concern
2. **Prioritized** per the template scale (`Critical (future)` / `High` / `Medium` / `Low`)
3. **Assessed for dependencies** against the existing roadmap
4. **Dispositioned** as either a standalone feature or absorbed into an existing roadmap item

Issues that logically belong to a single architectural correction are grouped under one feature entry to prevent fragmented patching.

**Scope control:** This document does NOT reprioritize the existing roadmap, propose code changes, or expand the Phase 0/1 boundary. It documents what exists for future reference.

---

## Issue Inventory Summary

| # | Feature Entry | Classification | Priority | Disposition | Target Version |
|---|---------------|---------------|----------|-------------|----------------|
| FF-01 | SG Energy Accounting & Boiling Fidelity | Critical Physics Blocker | High | Absorb into v5.6.0.0 | v5.6.0.0 |
| FF-02 | SG Regime Transition Robustness | Architectural Debt | Medium | Absorb into v5.6.0.0 | v5.6.0.0 |
| FF-03 | SG Pressure Rate Realism & Steam Inventory Consistency | Critical Physics Blocker | High | Absorb into v5.6.0.0 | v5.6.0.0 |
| FF-04 | SG Constants Calibration & Sensitivity Audit | Performance/Scaling Concern | Medium | Standalone | v5.6.1.0 |
| FF-05 | Primary Mass Conservation Tightening | Critical Physics Blocker | Critical (future) | Standalone — Phase 0 residual | v5.4.2.0 |
| FF-06 | VCT Conservation & Flow Boundary Audit | Architectural Debt | Medium | Standalone — Phase 0 | v5.4.4.0 |
| FF-07 | RCP Startup Transient Fidelity | Critical Physics Blocker | High | Standalone — Phase 0 | v5.4.3.0 |
| FF-08 | Secondary System Boundary Completeness | Architectural Debt | Medium | Absorb into v5.6.0.0 | v5.6.0.0 |
| FF-09 | CVCS Actuator Dynamics & Control Coupling | Architectural Debt | Medium | Absorb into Architecture Hardening | v5.7.0.0 |
| FF-10 | Regime Dispatch Hardening | Architectural Debt | Medium | Absorb into Architecture Hardening | v5.7.0.0 |

---

# FF-01: SG Energy Accounting & Boiling Fidelity

## Status

`Planned`

## Priority

`High`

## Classification

**Critical Physics Blocker** — Energy imbalances in the SG model propagate into incorrect primary heatup rates, incorrect steam production, and incorrect secondary pressure evolution.

## Motivation

The SG multi-node thermal model (SGMultiNodeThermal.cs) has several energy accounting gaps that prevent accurate heat balance tracking. While the v5.1.0 saturation-tracking pressure model was a major improvement, the underlying energy split between sensible and latent heat has inconsistencies that accumulate over multi-hour simulations.

## Problem Statement

Four specific energy accounting issues exist:

### 1. Delta-Q Clamp Rescales Boiling/Sensible Split Proportionally
**Location:** `SGMultiNodeThermal.cs:1186-1200`

When the 5 MW/timestep delta-Q clamp fires, it rescales `totalQ_boiling_BTUhr` proportionally with the total Q reduction. This assumes boiling and sensible contributions are uniform across all nodes. If one node spiked (causing the clamp) while others were stable, the proportional rescaling incorrectly penalizes all nodes equally, breaking per-node energy balance.

### 2. Steam Line Condensation Energy Removed from Steam Budget but Not from Node Temperatures
**Location:** `SGMultiNodeThermal.cs:1233-1272`

Energy diverted to steam line warming (`Q_condensation`) is subtracted from `totalQ_boiling_BTUhr` (reducing steam production), but boiling nodes remain clamped to T_sat. The energy that warmed the steam line is effectively "free" — the secondary water doesn't cool, but less steam is produced. Over a full heatup this can accumulate to ~2-5% steam production undercount.

### 3. Simultaneous Draining and Boiling Mass Removal Uncoupled
**Location:** `SGMultiNodeThermal.cs:1309-1343, 1284-1288`

Both the draining model and the boiling model independently decrement `SecondaryWaterMass_lb` in the same timestep without cross-validation. If draining is active during early boiling (200-220 degF overlap), total mass removal per timestep is not checked against available inventory at current density.

### 4. Inter-Node Conduction Not Logged in Diagnostics
**Location:** `SGMultiNodeThermal.cs:1052-1071, 1120`

Inter-node mixing heat is correctly applied to node temperatures but is not stored in any diagnostic field. Forensic logging loses ~10-15% of energy movement between nodes, making heat balance audits incomplete.

## Scope

| Goal | Description |
|------|-------------|
| Per-node energy clamp | Replace proportional rescale with per-node clamp that preserves individual node energy budgets |
| Condensation energy coupling | Ensure energy diverted to steam line warming is properly debited from the secondary water energy balance |
| Drain+boil mutual exclusion check | Add validation that total mass removal per timestep does not exceed available water at current conditions |
| Mixing heat diagnostics | Add `NodeMixingHeat[]` diagnostic array for forensic logging |

## Non-Goals

- No changes to the fundamental 5-node architecture
- No changes to the pressure model (covered in FF-03)
- No changes to HTC correlations (covered in FF-04)

## Technical Considerations

- **System boundaries:** SGMultiNodeThermal.cs only. No changes to engine dispatch.
- **Data ownership:** SG state struct owns all secondary-side energy fields.
- **Determinism requirements:** Must produce identical results to current code when clamp does not fire (i.e., only diverges in edge cases where current code is incorrect).
- **Performance impact:** Negligible. One additional array allocation at init.
- **Testing strategy:** Compare total energy balance (Q_primary_out = Q_sensible + Q_latent + Q_condensation + Q_mixing) before and after. Target: balance within 0.1% per timestep.

## Risks

| Risk | Mitigation |
|------|------------|
| Per-node clamp introduces oscillation at clamp boundary | Use smooth clamping (min of node Q and budget) rather than hard cutoff |
| Condensation coupling changes SG pressure trajectory | Validate against NRC HRTD heatup rate targets (45-55 degF/hr) |

## Dependencies

- Phase 0 mass conservation residual work complete (v5.4.2.0 — see FF-05)
- v5.4.1 complete (current baseline)

## Acceptance Criteria

- [ ] Per-timestep energy balance closes within 0.1%: Q_primary_out = Q_sensible + Q_latent + Q_condensation + Q_drain
- [ ] No steam production undercount from condensation diversion
- [ ] No mass removal exceeding available inventory in any timestep
- [ ] Mixing heat visible in forensic logs

## Notes

- This feature groups 4 related energy-accounting issues under one entry to prevent fragmented patching.
- Implementation should be a single coordinated change to the SG thermal model's energy bookkeeping.
- **Disposition:** Absorbed into v5.6.0.0 (SG Energy, Pressure & Secondary Boundary Corrections). Consolidated with FF-02, FF-03, and FF-08 under a single SG physics correction release.

---

# FF-02: SG Regime Transition Robustness

## Status

`Planned`

## Priority

`Medium`

## Classification

**Architectural Debt** — Current regime transition logic has edge cases that can cause oscillation or unrealistic state jumps. Not blocking current heatup validation but will cause issues in cooldown and transient scenarios.

## Motivation

The SG model transitions between Subcooled, Boiling, and SteamDump regimes using threshold-based logic with known gaps: instant blend reset on cooldown, no hysteresis on nitrogen isolation, and boiling onset detection using two inconsistent criteria.

## Problem Statement

Three regime transition issues exist:

### 1. Regime Blend Instant Reset on Cooldown
**Location:** `SGMultiNodeThermal.cs:943-955`

When a node drops below T_sat, `NodeRegimeBlend` resets instantly to 0.0 instead of ramping down. The code comment acknowledges this: "instant reset on cooldown; reverse ramp deferred to v5.2.0." If a node oscillates around T_sat (e.g., during inter-node mixing transients), the HTC and area calculations chatter between boiling and subcooled values every timestep.

### 2. Nitrogen Isolation Is Irreversible
**Location:** `SGMultiNodeThermal.cs:1870-1874`

Once `NitrogenIsolated` is set to true (at T_rcs >= 220 degF), there is no code path to reset it to false. For heatup-only simulation, this is correct. For cooldown scenarios (Phase 3: v5.7.0), the nitrogen blanket must be re-established when temperature drops below steaming conditions.

### 3. Boiling Onset Detection Inconsistency
**Location:** `SGMultiNodeThermal.cs:860-881`

Per-node boiling uses `NodeTemperatures[i] >= SaturationTemp_F` (inclusive), but regime determination uses `MaxSuperheat_F > 0f` (exclusive). If a node is exactly at T_sat due to floating-point arithmetic, `NodeBoiling[i]` is true but the regime may stay Subcooled if `MaxSuperheat_F` rounds to exactly 0.0.

## Scope

| Goal | Description |
|------|-------------|
| Reverse blend ramp | Implement downward ramp for NodeRegimeBlend when node drops below T_sat (matching the upward 60-second ramp) |
| N2 isolation hysteresis | Add cooldown path for NitrogenIsolated with temperature hysteresis (e.g., isolate at 220 degF, re-establish at 200 degF) |
| Consistent boiling criterion | Unify regime detection to use the same comparison operator as per-node boiling |

## Non-Goals

- No changes to the regime enum or addition of new regimes
- No changes to the SteamDump transition logic (handled by HZP)

## Technical Considerations

- **System boundaries:** SGMultiNodeThermal.cs only.
- **Performance impact:** Negligible.
- **Testing strategy:** Inject oscillating T_rcs near T_sat; verify no regime chattering. Verify N2 flag transitions correctly during cooldown ramp.

## Risks

| Risk | Mitigation |
|------|------------|
| Reverse ramp delays boiling termination unrealistically | Tune ramp-down rate independently of ramp-up; real boiling has thermal inertia |
| N2 hysteresis creates pressure discontinuity | Ensure N2 re-establishment does not create sudden pressure floor change |

## Dependencies

- None blocking. Can be implemented independently.
- N2 cooldown path is prerequisite for v5.8.0.0 (Cooldown Procedures).

## Acceptance Criteria

- [ ] No regime chattering when node temperature oscillates within 1 degF of T_sat
- [ ] Blend ramp-down completes in configurable time (default: 60 seconds, matching ramp-up)
- [ ] N2 isolation correctly re-establishes during cooldown scenarios
- [ ] Boiling onset detection produces identical results for threshold and superheat criteria

## Notes

- **Disposition:** Absorbed into v5.6.0.0 (SG Energy, Pressure & Secondary Boundary Corrections). All three issues affect the SG regime logic and should be addressed as a coordinated change with FF-01/03/08.
- The reverse blend ramp was explicitly deferred from v5.2.0; this entry formalizes that deferral.

---

# FF-03: SG Pressure Rate Realism & Steam Inventory Consistency

## Status

`Planned`

## Priority

`High`

## Classification

**Critical Physics Blocker** — SG secondary pressure behavior determines T_sat, which controls the primary-to-secondary heat transfer rate. Incorrect pressure dynamics propagate into incorrect heatup rates and incorrect steam production.

## Motivation

The v5.1.0 direct saturation tracking model eliminated the artificial 200 psi/hr rate limiter, which was correct. However, direct saturation tracking allows arbitrarily large pressure jumps per timestep. Additionally, the steam inventory model (for isolated SG scenarios) uses ideal gas law, which diverges from steam table saturation properties at operating pressures.

## Problem Statement

Three pressure-related issues exist:

### 1. No Rate Limiting on Saturation-Tracked Pressure
**Location:** `SGMultiNodeThermal.cs:1913-1929`

Pressure is set to `P_sat(T_hottest)` every timestep. If `T_hottest` jumps 10 degF in one timestep (plausible during RCP ramp with 10-second dt), pressure can jump ~100 psi instantaneously. Real SG pressure changes are rate-limited by steam generation kinetics and steam space compressibility.

### 2. Ideal Gas Law vs. Steam Tables for Inventory Pressure
**Location:** `SGMultiNodeThermal.cs:1437-1468`, `PlantConstants.SG.cs:892-905`

The steam inventory model uses `P = mRT/V` (ideal gas law) for isolated SG pressure. Steam at 17-1200 psia is NOT ideal; the compressibility factor Z ranges from ~0.95 to ~0.85 across this range. The ideal gas law overestimates pressure by 5-15% compared to steam tables. No cross-validation between inventory pressure and saturation tracking exists.

### 3. Steam Space Volume Upper Bound Missing
**Location:** `SGMultiNodeThermal.cs:1400-1416`

Steam space volume is clamped to >= 0 but has no upper bound. If water density calculation returns an anomalous large value (e.g., near-critical conditions), `V_water` becomes very small and `V_steam` can exceed `V_total`, violating SG geometry.

## Scope

| Goal | Description |
|------|-------------|
| Physical pressure rate limit | Add dP/dt limit based on steam generation rate and steam space compressibility (not an arbitrary cap) |
| Steam compressibility correction | Apply compressibility factor Z(P,T) to inventory pressure calculation, or replace ideal gas with steam table interpolation |
| Volume geometry enforcement | Clamp V_steam to [0, V_total] and V_water to [0, V_total] with V_water + V_steam = V_total |

## Non-Goals

- No replacement of the saturation tracking model (it is thermodynamically correct for open-system boiling)
- No changes to the SteamDump pressure cap logic

## Technical Considerations

- **System boundaries:** SGMultiNodeThermal.cs and PlantConstants.SG.cs.
- **Data ownership:** SG state struct owns pressure and volume fields.
- **Determinism requirements:** Pressure trajectory must be smooth (no single-step jumps > 50 psi during normal heatup).
- **Testing strategy:** Run full heatup and verify dP/dt profile is monotonically increasing and smooth. Cross-check inventory pressure against saturation pressure at same conditions.

## Risks

| Risk | Mitigation |
|------|------------|
| Physical rate limit is too restrictive during rapid transients | Make limit a function of steam generation rate, not a fixed cap |
| Compressibility correction changes pressure trajectory | Validate against NRC HRTD expected pressure profile during heatup |

## Dependencies

- Phase 0 mass conservation residual work complete (v5.4.2.0 — see FF-05)
- FF-01 (energy accounting) should be complete first, as pressure depends on correct steam production

## Acceptance Criteria

- [ ] No single-timestep pressure jump > 50 psi during normal heatup (45-55 degF/hr)
- [ ] Inventory pressure matches saturation pressure within 5% at same conditions
- [ ] V_water + V_steam = V_total at all times (geometry conservation)
- [ ] Pressure trajectory during heatup is smooth and monotonically increasing

## Notes

- **Disposition:** Absorbed into v5.6.0.0 (SG Energy, Pressure & Secondary Boundary Corrections). Consolidated with FF-01/02/08 under a single SG physics correction release.
- The ideal gas divergence is most significant at high pressures (>800 psia) where the simulator approaches steam dump conditions. At low pressures (<100 psia), ideal gas is acceptable.

---

# FF-04: SG Constants Calibration & Sensitivity Audit

## Status

`Research`

## Priority

`Medium`

## Classification

**Performance/Scaling Concern** — Several SG constants are tuning parameters without rigorous physical basis. Sensitivity to these parameters is unknown, making validation against plant data unreliable.

## Motivation

The SG thermal model relies on several empirical constants that were tuned during v4.3.0 to produce aggregate correct behavior (30-60 degF primary-secondary delta-T, 45-55 degF/hr heatup rate). Individual constants lack validation against experimental or FSAR data.

## Problem Statement

Four parameter groups need calibration review:

### 1. Thermocline Alpha (0.08 ft2/hr)
**Location:** `PlantConstants.SG.cs:430-446`

Composite estimate combining water thermal diffusivity (0.021) and Inconel tube effects (0.14). Controls thermocline descent rate and therefore active tube area during subcooled phase. Sensitivity: +/-25% change in alpha shifts active area fraction by ~15%.

### 2. Bundle Penalty Factor (0.55)
**Location:** `PlantConstants.SG.cs:448-469`

Accounts for reduced natural convection in dense tube bundle. Increased from 0.40 to 0.55 in v4.3.0 via qualitative argument. Real bundle penalty depends on tube pitch, gap width, and Rayleigh number.

### 3. Boiling HTC Range (500-700 BTU/hr-ft2-degF)
**Location:** `PlantConstants.SG.cs:757-766`, `SGMultiNodeThermal.cs:1724-1748`

Full nucleate boiling at 1100 psia typically produces 2000-5000 BTU/hr-ft2-degF. The 700 value accounts for "bundle effects" and "partial coverage" as ad-hoc corrections. These corrections have not been validated against SG-specific boiling data.

### 4. Node Effectiveness Factors
**Location:** `PlantConstants.SG.cs:330-370`

Five-element array of effectiveness factors uniformly increased in v4.3.0 (top: 0.40->0.70, bottom: 0.01->0.02). These control the fraction of each node's tube area that participates in heat transfer. No individual node validation against CFD or experimental data.

## Scope

| Goal | Description |
|------|-------------|
| Parameter sensitivity analysis | Determine which constants most affect heatup rate, delta-T, and pressure trajectory |
| Document calibration basis | For each constant, document the physical basis, source, uncertainty range, and sensitivity |
| Identify plant-data validation targets | List which parameters could be validated against FSAR startup data if available |

## Non-Goals

- No code changes in this feature (research/documentation only)
- No new physics models
- No parameter optimization (that follows from the research)

## Technical Considerations

- **Testing strategy:** Parametric sweeps with +/-25% variation on each constant, measuring heatup rate, delta-T, and pressure trajectory as outputs.

## Risks

| Risk | Mitigation |
|------|------------|
| Sensitivity analysis reveals fundamental model inadequacy | Document finding and assess whether architectural changes are needed |
| Parameters are highly correlated, making individual calibration impossible | Use orthogonal sweep design to separate effects |

## Dependencies

- FF-01 (energy accounting) should be resolved first so sensitivity analysis reflects correct energy balance
- No blocking dependencies

## Acceptance Criteria

- [ ] Sensitivity matrix produced for all 4 parameter groups
- [ ] Each constant has documented physical basis with uncertainty range
- [ ] Validation targets identified for any available plant data

## Notes

- **Disposition:** Standalone research item. Results feed into FF-01 and FF-03 implementations.
- This is explicitly a documentation/research feature. No code changes.

---

# FF-05: Primary Mass Conservation Tightening

## Status

`Implemented — Pending Validation` (4 of 5 items delivered in v5.4.2.0; Issue #5 deferred)

## Priority

`Critical (future)`

## Deferral Notice

**FF-05 Issue #5 (CoupledThermo V×ρ intermediate estimate)** deferred from v5.4.2.0 to **v5.7.0.0** (Architecture Hardening).

**Reason:** The CoupledThermo partition logic (`CoupledThermo.cs:237-259`) uses an intermediate V×ρ estimate to split mass between RCS and PZR. Correcting this requires a solver partition redesign that exceeds the conservation-tightening scope of v5.4.2.0. The issue does not affect mass conservation totals (RCS is computed as the ledger remainder), but it can perturb PZR volume partitioning at regime transitions. This is an architecture concern, not a conservation violation, and is correctly targeted at the Architecture Hardening phase.

## Classification

**Critical Physics Blocker** — Mass conservation violations propagate into every downstream calculation. The v5.3.0 through v5.4.1 work established the canonical mass ledger, but code review reveals remaining gaps in the transfer semantics and regime transitions.

## Motivation

Despite the canonical mass ledger infrastructure, several code paths still violate conservation-by-construction principles. These are residuals from iterative patching and represent the highest-priority physics corrections remaining.

## Problem Statement

Five mass conservation gaps identified:

### 1. Surge Mass Transfer Uses Post-Step Density
**Location:** `SolidPlantPressure.cs:632-635`

During solid-plant surge flow, `surgeMass_lb = dV_pzr * rho_pzr_post` uses the NEW (post-heating) density instead of the density at which water actually left the PZR. During rapid pressurization (dP/dt > 100 psi/hr), the error can exceed 10 lbm per step.

### 2. Bubble Formation Recalculates Mass from Volume
**Location:** `PressurizerPhysics.cs:152-153`

`FormBubble()` sets `WaterMass = WaterVolume * rhoWater` and `SteamMass = SteamVolume * rhoSteam`, which overwrites the conserved mass values. If rho lookup differs from the values used during solid-plant tracking, ~100 lbm can appear or vanish at the transition.

### 3. Steam Reconciliation During DRAIN Creates Mass
**Location:** `HeatupSimEngine.BubbleFormation.cs:415-417`

After the DRAIN phase, steam mass is reconciled as `PZRSteamMass = PZRSteamVolume * rhoSteam`. This overwrites the mass established by energy-based phase change, potentially creating mass from volume-based computation. The forensic audit then flags a "VIOLATION" that is self-inflicted.

### 4. Canonical Mass Ledger Initialization Fragility
**Location:** `HeatupSimEngine.Init.cs:342-360`, `RCSHeatup.cs:162-165`

`TotalPrimaryMass_lb` is initialized once at simulation start. If the first physics timestep produces slightly different component masses (due to rho lookup differences between Init and first Step), the ledger baseline is already wrong. No warm-up tolerance exists.

### 5. CoupledThermo Canonical Mode V-rho Estimate Feeds PZR Volume
**Location:** `CoupledThermo.cs:237-259`

In canonical mode, an intermediate V-rho estimate (`M_RCS_est`) is used to compute PZR water volume before RCS is computed as the conservation remainder. If the estimate is slightly off, PZR volumes are perturbed, causing spurious mass redistribution at regime transitions.

## Scope

| Goal | Description |
|------|-------------|
| Transfer density audit | All inter-component mass transfers use density at transfer conditions, not post-step density |
| FormBubble mass preservation | FormBubble preserves total PZR mass exactly; volumes derived from mass, not the reverse |
| Eliminate volume-based mass reconciliation | No code path computes mass from V*rho after the canonical ledger is established |
| Init-to-first-step consistency | First timestep conservation check uses tolerance window or ledger is updated to match actual first-step state |
| CoupledThermo intermediate estimate audit | Verify or eliminate V-rho intermediate estimate in canonical mode |

## Non-Goals

- No changes to the ledger architecture (it is correct in design)
- No changes to boundary flow accounting (CVCS net, relief)
- No changes to logging or display

## Technical Considerations

- **System boundaries:** SolidPlantPressure.cs, PressurizerPhysics.cs, HeatupSimEngine.BubbleFormation.cs, HeatupSimEngine.Init.cs, CoupledThermo.cs
- **Data ownership:** `TotalPrimaryMass_lb` is the single source of truth. All component masses are derived from or constrained by this value.
- **Determinism requirements:** Changes must produce identical or tighter conservation metrics.
- **Testing strategy:** Full heatup run comparing mass error at every interval. Target: max transient error < 50 lbm (currently ~3,755 lbm during DRAIN spike). Steady-state error < 0.05%.

## Risks

| Risk | Mitigation |
|------|------------|
| Changing FormBubble mass preservation alters two-phase entry conditions | Validate bubble formation level, pressure, and temperature against current baseline |
| Removing V-rho reconciliation causes volume drift | Derive volumes from mass and density after every step; verify PZR level matches expected program |

## Dependencies

- These issues were originally scoped for v5.4.0 Stages 1-3 but remain unresolved as of v5.4.1. They form the core scope of the next Phase 0 patch (v5.4.2.0).
- No new external dependencies

## Acceptance Criteria

- [ ] No code path computes mass from V*rho after ledger initialization
- [ ] FormBubble preserves total PZR mass within 0.01 lbm
- [ ] Surge transfer uses density at transfer conditions
- [ ] Max transient mass error < 50 lbm during DRAIN
- [ ] Steady-state mass error < 0.05% over 8-hour simulation
- [ ] First-timestep conservation check passes

## Notes

- **Disposition:** Issues #1-#4 implemented in v5.4.2.0 (pending runtime validation). Issue #5 (CoupledThermo V×ρ intermediate estimate) deferred to v5.7.0.0 — requires solver partition redesign beyond conservation-tightening scope.
- **Implementation Plan:** `Updates/IMPLEMENTATION_PLAN_v5.4.2.0.md`

---

# FF-06: VCT Conservation & Flow Boundary Audit

## Status

`Planned`

## Priority

`Medium`

## Classification

**Architectural Debt** — VCT tracking uses gallon-based accounting that diverges from the mass-based canonical ledger. Flow boundary conversions use incorrect density assumptions. These do not break simulation stability but accumulate 30-50 gallon errors over multi-hour runs.

## Motivation

The VCT subsystem tracks inventory in gallons while the primary system uses mass (lbm). The conversion between these units happens at several boundaries with inconsistent density assumptions, creating phantom conservation errors.

## Problem Statement

Four VCT-related issues exist:

### 1. Gallon-Based RCS Change Tracking
**Location:** `HeatupSimEngine.CVCS.cs:208-210`

RCS mass changes are converted to gallons using `rho_rcs` at current conditions for VCT tracking. Over a heatup where density changes ~10%, the same mass delta registers as different gallon amounts, accumulating ~30-50 gallon errors in `CumulativeRCSChange_gal`.

### 2. External Flow Boundary Uses Fixed VCT Density
**Location:** `HeatupSimEngine.CVCS.cs:292-298`

The system-wide mass conservation check converts external gallon flows to mass using `rhoVCT` (at 100 degF). Water from the RWST (90 degF) or BRS has different density. The conversion should use source-condition density.

### 3. Divert Valve Has No Actuator Dynamics
**Location:** `VCTPhysics.cs:179-197`

The divert valve position responds instantaneously to level changes. Real proportional valves (LCV-112A) have ~10-20 second travel time. During charging transients, the model diverts excess flow immediately, causing VCT to stabilize 1-2% lower than the real plant.

### 4. BRS Makeup Shortfall Not Fed Back to VCT
**Location:** `HeatupSimEngine.CVCS.cs:256-268`

If BRS distillate is depleted during makeup, `WithdrawDistillate()` returns less than requested, but VCT isn't informed of the shortfall. The VCT level control assumes full makeup is available, potentially causing level to drop unexpectedly when BRS depletes.

## Scope

| Goal | Description |
|------|-------------|
| Mass-based VCT tracking | Convert VCT cumulative tracking to mass (lbm) as primary unit; derive gallons for display only |
| Density-correct boundary flows | Use source-condition density for all external flow mass conversions |
| Divert valve dynamics | Add first-order lag to divert valve position with configurable time constant (~15 seconds) |
| BRS depletion feedback | When BRS returns less than requested, reduce effective makeup rate in VCT state |

## Non-Goals

- No changes to VCT level control setpoints
- No changes to BRS physics model
- No changes to charging/letdown flow models

## Technical Considerations

- **System boundaries:** VCTPhysics.cs, HeatupSimEngine.CVCS.cs, BRSPhysics.cs (read-only for shortfall detection)
- **Performance impact:** Negligible.
- **Testing strategy:** Full heatup run comparing VCT level vs. expected trajectory. Target: VCT cumulative error < 10 gallons over 8 hours.

## Risks

| Risk | Mitigation |
|------|------------|
| Mass-based tracking changes VCT level display behavior | Verify display gallons match previous behavior through density conversion |
| Divert valve lag causes VCT level excursions | Tune time constant conservatively; add high-level alarm if level exceeds 80% |

## Dependencies

- FF-05 (primary mass conservation) should be resolved first, as VCT tracking depends on correct RCS mass accounting
- No blocking dependencies

## Acceptance Criteria

- [ ] VCT cumulative conservation error < 10 gallons over 8-hour heatup
- [ ] External flow mass conversion uses source-condition density
- [ ] Divert valve shows realistic lag during transients
- [ ] BRS depletion correctly reduces effective makeup rate

## Notes

- **Disposition:** Standalone — v5.4.4.0 (Phase 0). Physics correction within Core Physics Stabilization domain, depends on FF-05 (v5.4.2.0).
- Issues #1 and #2 are directly related to the v5.4.0 Issue #5 (VCT Conservation Error Growth ~1,600-1,700 gal). This entry captures the specific code-level root causes.

---

# FF-07: RCP Startup Transient Fidelity

## Status

`Planned`

## Priority

`High`

## Classification

**Critical Physics Blocker** — RCP startup is the most operationally significant transient in the heatup sequence. Incorrect modeling of the RCP-induced PZR response creates false alarms and masks real mass conservation errors.

## Motivation

When RCPs start, the primary system transitions from stagnant to forced-circulation in 30-90 seconds. This creates rapid thermal mixing, PZR level transients (expansion/contraction), and CVCS control challenges. The current model has several gaps that affect the fidelity of this critical transient.

## Problem Statement

Five RCP startup issues exist:

### 1. No Thermal Mixing Model for RCP Inlet Temperature
**Location:** `RCSHeatup.cs:218-221`

RCP heat is distributed uniformly across the RCS using bulk T_avg. In reality, cold-leg water enters the pump at T_cold and exits at T_hot (30 degF higher). The surge line carries hot-leg water to the PZR. Using bulk T_avg underestimates surge-line temperature and PZR level rise by ~2-3%.

### 2. PI Controller Pre-Seeding May Over-Seed
**Location:** `HeatupSimEngine.cs:806-808`

At RCP start, the PI controller integral is pre-seeded based on current conditions using the NEW rcpCount (post-start). If PZR level is already near setpoint, the pre-seed introduces an artificial overshoot component that takes several control cycles to bleed down.

### 3. Regime Blend Alpha Threshold Has No Hysteresis
**Location:** `HeatupSimEngine.cs:1006`

The boundary between Regime 1 (isolated) and Regime 2 (blended) uses a hard threshold `alpha < 0.001f`. If alpha hovers at this boundary, the regime oscillates frame-by-frame between isolated and coupled physics, causing discontinuous state jumps.

### 4. Linear Pressure-Temperature Blending Violates Thermodynamic Consistency
**Location:** `HeatupSimEngine.cs:1219-1235`

In Regime 2 (RCPs ramping), temperature and pressure from isolated and coupled paths are linearly blended: `T = T_iso * (1-alpha) + T_coupled * alpha`. The blended P-T state may not lie on any thermodynamic equilibrium surface, creating transient violations that the next timestep must correct.

### 5. Effective RCP Ramp Doesn't Account for Non-Sequential Starts
**Location:** `HeatupSimEngine.cs:794-809`

The `rcpStartTimes[]` array assumes pumps start in order (0, 1, 2, 3). If pumps start non-sequentially, the ramp profile calculation may compress or misalign the contribution from individual pumps.

## Scope

| Goal | Description |
|------|-------------|
| Hot-leg/cold-leg split | Model T_hot and T_cold separately during RCP ramp; use T_hot for surge line temperature |
| PI pre-seed review | Validate pre-seed magnitude against actual level error at RCP start; cap pre-seed to prevent overshoot |
| Regime boundary hysteresis | Add hysteresis band to alpha threshold (enter Regime 2 at alpha > 0.002, exit at alpha < 0.0005) |
| Thermodynamically consistent blending | Blend in energy/enthalpy space rather than P-T space, then derive P and T from blended enthalpy |
| Non-sequential RCP support | Make ramp profile calculation robust to arbitrary pump start order |

## Non-Goals

- No changes to RCP pump curves or NPSH modeling
- No changes to seal injection flow models
- No changes to the 4-pump-per-unit assumption

## Technical Considerations

- **System boundaries:** RCSHeatup.cs, HeatupSimEngine.cs (regime dispatch), CVCSController.cs (pre-seed).
- **Data ownership:** RCS temperature split (T_hot, T_cold) would be new state fields in SystemState.
- **Performance impact:** Minor. Additional state fields and one extra temperature calculation per step.
- **Testing strategy:** Compare PZR level transient at RCP start against expected plant response: level should rise 3-5% due to thermal expansion, stabilize within 2 minutes, no overshoot > 8%.

## Risks

| Risk | Mitigation |
|------|------------|
| T_hot/T_cold split introduces new state coupling | Use simple T_hot = T_avg + delta/2, T_cold = T_avg - delta/2 where delta is from heat balance |
| Enthalpy blending is more complex than P-T blending | Can be implemented incrementally; start with hysteresis on current P-T blend |
| Non-sequential RCP logic is more complex | May not be needed if simulation always starts pumps sequentially; defer if so |

## Dependencies

- Phase 0 mass conservation residual work complete (v5.4.2.0 — see FF-05)
- FF-05 (primary mass conservation) resolves the DRAIN spike that currently masks RCP transient errors

## Acceptance Criteria

- [ ] PZR level rise on RCP start matches expected 3-5% (not 0% or 10%)
- [ ] No regime chattering at alpha = 0.001 threshold
- [ ] PI controller does not overshoot level setpoint by more than 2% after pre-seed
- [ ] Blended P-T state is thermodynamically consistent (P = P_sat(T) during two-phase)
- [ ] Four RCPs starting in any order produce correct cumulative ramp

## Notes

- **Disposition:** Standalone — v5.4.3.0 (Phase 0). Physics-critical RCP transient corrections within Core Physics Stabilization domain.
- Issue #3 (alpha threshold) and #4 (P-T blending) are related to the Architecture Hardening future feature (regime dispatch via state machine). However, they are physics-critical and should not wait for the full refactor.
- The original v5.4.0 Issue #4 (PZR Level Spike on RCP Start) is a subset of this feature entry.

---

# FF-08: Secondary System Boundary Completeness

## Status

`Planned`

## Priority

`Medium`

## Classification

**Architectural Debt** — The secondary system (SG shell side, steam lines, MSIVs) is modeled as boundary conditions rather than a conserved volume. This limits the simulator's ability to model closed-system scenarios, cooldown, and accident conditions.

## Motivation

The current SG model treats the secondary side as either an open system (steam vented through MSIVs) or a simplified closed system (ideal gas inventory). Several boundary conditions are incomplete: SG draining is declared but not implemented, SG secondary mass is opaque to the engine, and level instrumentation doesn't account for two-phase density.

## Problem Statement

Four secondary boundary gaps exist:

### 1. SG Draining State Variables Not Implemented
**Location:** `HeatupSimEngine.cs:154-160`

Five SG draining state variables are declared (`sgDrainingActive`, `sgDrainingComplete`, `sgDrainingRate_gpm`, `sgTotalMassDrained_lb`, `sgSecondaryMass_lb`) but are never assigned in any code path. They default to false/0 and are displayed in logs as such.

The SG model (SGMultiNodeThermal) has its own internal draining model (lines 1309-1343), but the engine never reads or exposes this state. The operator cannot observe SG draining progress through the engine's public fields.

### 2. SG Secondary Mass Opaque to Engine
**Location:** `HeatupSimEngine.cs:154-160`, `SGMultiNodeThermal.cs` internal state

The engine declares `sgSecondaryMass_lb` but never assigns it. The actual SG secondary water mass lives in `sgMultiNodeState.SecondaryWaterMass_lb`, which is internal to the multi-node model. System-wide mass audits cannot include SG secondary inventory without reaching into the SG model's internal state.

### 3. Level Instrumentation Doesn't Account for Two-Phase Density
**Location:** `SGMultiNodeThermal.cs:1366-1380`

Wide Range and Narrow Range level are computed as direct mass fractions. Real SG level instruments measure differential pressure, which depends on the density of the water column (collapsed level). During steaming, the effective water density decreases, and indicated level reads lower than actual mass fraction. The code acknowledges this limitation.

### 4. Steam Line Model Lacks Condensate Return Path
**Location:** `SGMultiNodeThermal.cs:1233-1272`

Steam line warming removes energy from the steam budget (reducing steam production), but the condensed steam is not returned to the SG secondary water inventory. In reality, steam condenses on cold pipe walls and drains back to the SG through steam line drains. This is a mass sink in the current model.

## Scope

| Goal | Description |
|------|-------------|
| Wire SG draining to engine | Copy SG draining state from multi-node model to engine public fields every timestep |
| Expose SG secondary mass | Add `sgSecondaryMass_lb = sgMultiNodeState.SecondaryWaterMass_lb` assignment to engine update |
| Collapsed level model | Implement collapsed-level calculation for WR/NR that accounts for void fraction and density changes |
| Condensate return | Add condensate return path from steam line to SG secondary mass |

## Non-Goals

- No new SG instrumentation (e.g., no steam flow transmitter)
- No feedwater system modeling (Phase 3 scope)
- No main steam isolation logic (Phase 3 scope)

## Technical Considerations

- **System boundaries:** HeatupSimEngine.cs (wiring), SGMultiNodeThermal.cs (condensate return, level model).
- **Performance impact:** Negligible.
- **Testing strategy:** Verify SG secondary mass is visible in all logging outputs. Verify collapsed level reads lower than mass-fraction level during steaming.

## Risks

| Risk | Mitigation |
|------|------------|
| Collapsed level model requires void fraction correlation | Use Zuber-Findlay or simplified drift-flux for SG tube bundle geometry |
| Condensate return changes secondary mass trajectory | Validate total secondary mass at end of heatup against expected wet-layup values |

## Dependencies

- FF-01 (SG energy accounting) should be resolved first, as condensate return depends on correct energy balance
- No blocking dependencies

## Acceptance Criteria

- [ ] Engine public fields reflect actual SG draining state at all times
- [ ] `sgSecondaryMass_lb` matches `sgMultiNodeState.SecondaryWaterMass_lb` within 0.1 lbm
- [ ] WR/NR level accounts for two-phase density during boiling
- [ ] Steam line condensate is returned to SG secondary mass inventory

## Notes

- **Disposition:** Absorbed into v5.6.0.0 (SG Energy, Pressure & Secondary Boundary Corrections). Consolidated with FF-01/02/03 under a single SG physics correction release.
- Issues #1 and #2 are simple wiring fixes that could be done immediately but are grouped here to avoid fragmented patching.
- Issue #3 (collapsed level) is needed before v5.8.0.0 (Cooldown Procedures) where level indication accuracy matters.

---

# FF-09: CVCS Actuator Dynamics & Control Coupling

## Status

`Planned`

## Priority

`Medium`

## Classification

**Architectural Debt** — The CVCS controller and SolidPlantPressure module both contain pressure/level control logic that can interfere when both execute in the same timestep. Actuator dynamics are partially implemented but inconsistent across modules.

## Motivation

The v5.4.1 release added pressure filter, lag, and slew-rate limiter to the CVCS controller. However, the underlying architectural issue remains: two modules (CVCSController and SolidPlantPressure) both adjust letdown/charging flows based on pressure and level errors, with no explicit coordination.

## Problem Statement

Three control coupling issues exist:

### 1. Dual Pressure Control Systems
**Location:** `CVCSController.cs:335-556`, `SolidPlantPressure.cs:498-556`

During solid-plant operations, CVCSController maintains PZR level via charging modulation while SolidPlantPressure maintains pressure via letdown adjustment. These are physically coupled (changing letdown affects level, changing charging affects pressure) but are computed independently. If both fire corrections in the same timestep, the net effect can be contradictory, causing 20-30 second oscillation.

### 2. Seal Injection Step Change on RCP Count Change
**Location:** `CVCSController.cs:371-380`

When RCP count changes, seal injection demand changes by 8 gpm per pump instantaneously. The PI controller's integral term still holds the old correction, causing a transient level excursion of 2-5% that takes 10-20 seconds to correct.

### 3. Letdown Orifice Configuration Not Under Controller
**Location:** `CVCSController.cs:344-346`

The number of open letdown orifices (`num75Open`, `open45`) is passed into the controller from the engine/operator but is not computed by the controller itself. If the operator configuration is stale, the controller compensates by adjusting charging, creating a parasitic control loop.

## Scope

| Goal | Description |
|------|-------------|
| Unified control authority | During each regime, exactly one module owns pressure control and one owns level control; no overlap |
| Seal injection ramp | Ramp seal injection changes over ~5 seconds instead of step change |
| Letdown orifice awareness | Controller can request orifice configuration changes based on flow demand |

## Non-Goals

- No new control algorithms (PI tuning remains)
- No automatic orifice valve sequencing (operator decision)

## Technical Considerations

- **System boundaries:** CVCSController.cs, SolidPlantPressure.cs, HeatupSimEngine.cs (dispatch)
- **Testing strategy:** Inject RCP count change; verify PZR level transient stays within 3% of setpoint.

## Risks

| Risk | Mitigation |
|------|------------|
| Unified authority changes control response characteristics | Validate against current baseline before and after |
| Orifice awareness adds complexity to controller | Keep as advisory output, not automatic action |

## Dependencies

- Architecture Hardening (FUTURE_ARCHITECTURE_ITEMS.md Item 1) — the module boundary definition is where this should be resolved
- Phase 0 mass conservation residual work complete (v5.4.2.0 — see FF-05)

## Acceptance Criteria

- [ ] No timestep where both CVCSController and SolidPlantPressure modify the same flow variable
- [ ] RCP count change causes < 3% PZR level excursion
- [ ] Controller correctly adapts to orifice configuration

## Notes

- **Disposition:** Absorb into v5.7.0.0 — Architecture Hardening (FUTURE_ARCHITECTURE_ITEMS.md Item 1). The dual-control issue is a symptom of unclear module boundaries, which is the core problem that item addresses. Fixing it in isolation would be a band-aid.

---

# FF-10: Regime Dispatch Hardening

## Status

`Planned`

## Priority

`Medium`

## Classification

**Architectural Debt** — The main simulation loop uses nested boolean chains for regime dispatch, magic number thresholds, duplicate state flags, and fragile inter-step state communication. These create maintenance risk and edge-case bugs.

## Motivation

The simulation engine dispatches to different physics paths based on `solidPressurizer`, `bubbleFormed`, `bubblePreDrainPhase`, `bubblePhase` enum, `rcpCount`, `alpha`, and `regime` index. Several of these are redundant (e.g., `bubblePreDrainPhase` vs. `bubblePhase`), and the boundaries between regimes use hard-coded thresholds without hysteresis.

## Problem Statement

Four dispatch issues exist:

### 1. Duplicate Phase Flags
**Location:** `HeatupSimEngine.BubbleFormation.cs:84-93, 134, 288`

`bubblePreDrainPhase` is a separate boolean that duplicates information in the `bubblePhase` enum. CVCS dispatch checks the boolean (CVCS.cs:74-78) instead of the enum. If these become out of sync, CVCS flow control diverges from the state machine.

### 2. Magic Number Alpha Threshold
**Location:** `HeatupSimEngine.cs:1006`

`alpha < 0.001f` is the boundary between Regime 1 and Regime 2 with no documented physical basis and no hysteresis. (Also covered in FF-07 Issue #3.)

### 3. PZR Volume Blending Uses Incremental Delta Without Volume Reconciliation
**Location:** `HeatupSimEngine.cs:1219-1235`

In Regime 2, PZR water volume is computed by blending incremental deltas from isolated and coupled paths. The initial volume is never reconciled from mass/density, so errors from the previous timestep's regime propagate forward.

### 4. regime3CVCSPreApplied Flag Is Fragile
**Location:** `HeatupSimEngine.cs:427, 1166-1177`, `HeatupSimEngine.CVCS.cs:179-183`

The flag `regime3CVCSPreApplied` gates whether `UpdateRCSInventory()` applies CVCS changes (or skips them because they were pre-applied to the solver). The flag is reset unconditionally every timestep. If execution order changes, the skip could fire incorrectly.

## Scope

| Goal | Description |
|------|-------------|
| Eliminate duplicate phase flags | Remove `bubblePreDrainPhase`; derive all gating from `bubblePhase` enum |
| Document alpha threshold | Add comment with physical basis or make threshold a PlantConstants value |
| Volume reconciliation at regime entry | At each regime transition, reconcile PZR volume from mass and density |
| CVCS pre-application audit | Replace boolean flag with explicit enum state (CVCS_NOT_APPLIED / CVCS_PRE_APPLIED / CVCS_APPLIED) |

## Non-Goals

- No full state machine refactor (that is Architecture Hardening Item 1)
- No new regimes
- No changes to physics calculations

## Technical Considerations

- **System boundaries:** HeatupSimEngine.cs, HeatupSimEngine.BubbleFormation.cs, HeatupSimEngine.CVCS.cs
- **Testing strategy:** Full heatup regression test. Verify identical interval logs before and after dispatch changes.

## Risks

| Risk | Mitigation |
|------|------------|
| Removing boolean flag changes CVCS behavior during bubble formation | Test bubble formation sequence in isolation; verify identical CVCS flows |
| Volume reconciliation at regime entry causes mass discontinuity | Reconciliation must preserve canonical mass; only volumes change |

## Dependencies

- Architecture Hardening (FUTURE_ARCHITECTURE_ITEMS.md Item 1) — this is a subset of the regime state machine refactor
- No blocking dependencies for the immediate cleanup items

## Acceptance Criteria

- [ ] `bubblePreDrainPhase` removed; all dispatch uses `bubblePhase` enum
- [ ] Alpha threshold documented with physical basis or made configurable
- [ ] PZR volume is reconciled from mass at every regime transition
- [ ] CVCS pre-application state is explicit (no boolean flag)
- [ ] Full heatup regression test produces identical results

## Notes

- **Disposition:** Absorb into v5.7.0.0 — Architecture Hardening (FUTURE_ARCHITECTURE_ITEMS.md Item 1). Items #1 and #4 are quick cleanup that could be done immediately, but they are part of the larger regime dispatch refactor and should not be done piecemeal.
- Item #2 (alpha threshold) could be addressed in v5.4.3.0 / FF-07 (RCP transient fidelity) if that feature ships first.

---

# Cross-Reference Matrix

## Issue-to-Feature Mapping

| Source Issue | Feature Entry | Rationale |
|-------------|---------------|-----------|
| SG delta-Q clamp rescale | FF-01 | Energy accounting |
| SG condensation energy gap | FF-01 | Energy accounting |
| SG drain+boil mass removal | FF-01 | Energy accounting |
| SG mixing heat not logged | FF-01 | Energy accounting (diagnostics) |
| SG blend instant reset | FF-02 | Regime transition |
| SG N2 isolation irreversible | FF-02 | Regime transition |
| SG boiling onset inconsistency | FF-02 | Regime transition |
| SG pressure rate unlimited | FF-03 | Pressure realism |
| SG ideal gas vs steam tables | FF-03 | Pressure realism |
| SG steam volume upper bound | FF-03 | Pressure realism |
| SG thermocline alpha | FF-04 | Constants calibration |
| SG bundle penalty factor | FF-04 | Constants calibration |
| SG boiling HTC range | FF-04 | Constants calibration |
| SG node effectiveness factors | FF-04 | Constants calibration |
| Surge mass density error | FF-05 | Mass conservation |
| FormBubble mass overwrite | FF-05 | Mass conservation |
| DRAIN steam reconciliation | FF-05 | Mass conservation |
| Init-to-first-step gap | FF-05 | Mass conservation |
| CoupledThermo V-rho estimate | FF-05 | Mass conservation |
| VCT gallon-based tracking | FF-06 | VCT boundary |
| VCT external flow density | FF-06 | VCT boundary |
| VCT divert valve dynamics | FF-06 | VCT boundary |
| BRS depletion feedback | FF-06 | VCT boundary |
| RCP thermal mixing model | FF-07 | RCP transient |
| PI pre-seed overshoot | FF-07 | RCP transient |
| Alpha threshold hysteresis | FF-07 | RCP transient |
| P-T blend consistency | FF-07 | RCP transient |
| Non-sequential RCP starts | FF-07 | RCP transient |
| SG draining not wired | FF-08 | Secondary boundary |
| SG mass opaque to engine | FF-08 | Secondary boundary |
| SG collapsed level model | FF-08 | Secondary boundary |
| SG condensate return path | FF-08 | Secondary boundary |
| Dual pressure control | FF-09 | Control coupling |
| Seal injection step change | FF-09 | Control coupling |
| Letdown orifice awareness | FF-09 | Control coupling |
| Duplicate phase flags | FF-10 | Dispatch hardening |
| Alpha magic number | FF-10 | Dispatch hardening |
| PZR volume unreconciled | FF-10 | Dispatch hardening |
| CVCS pre-applied flag | FF-10 | Dispatch hardening |

## Roadmap Absorption Map

| Feature Entry | Disposition | Target Version | Rationale |
|---------------|-----------|----------------|-----------|
| FF-01 | Absorb | v5.6.0.0 | SG energy accounting — consolidated SG physics correction |
| FF-02 | Absorb | v5.6.0.0 | SG regime transitions — same SG code, same release |
| FF-03 | Absorb | v5.6.0.0 | SG pressure realism — consolidated SG physics correction |
| FF-04 | Standalone | v5.6.1.0 | SG constants research — follows SG corrections, no code changes |
| FF-05 | Standalone | v5.4.2.0 | Mass conservation residuals — Phase 0 exit gate |
| FF-06 | Standalone | v5.4.4.0 | VCT boundary audit — Phase 0, depends on FF-05 |
| FF-07 | Standalone | v5.4.3.0 | RCP transient fidelity — Phase 0, high priority |
| FF-08 | Absorb | v5.6.0.0 | Secondary boundary — SG-scoped, consolidated |
| FF-09 | Absorb | v5.7.0.0 | Control coupling — Architecture Hardening (Item 1) |
| FF-10 | Absorb | v5.7.0.0 | Dispatch hardening — Architecture Hardening (Item 1) |

## Dependency Graph

```
v5.4.1 (current baseline)
    |
    +-- v5.4.2.0 / FF-05 (mass conservation)
    |       |
    |       +-- v5.4.3.0 / FF-07 (RCP transients) -- depends on FF-05
    |       |
    |       +-- v5.4.4.0 / FF-06 (VCT boundary) -- depends on FF-05
    |
    +-- [Phase 0 Complete]
            |
            +-- v5.5.0.0 (Validation Dashboard)
            +-- v5.5.1.0 (Scenario Selector)
            +-- v5.5.2.0 (200°F Hold)
            +-- v5.5.3.0 (Help System)
            |
            +-- [Phase 1 Complete]
                    |
                    +-- v5.6.0.0 / FF-01+02+03+08 (SG corrections)
                    |       |
                    |       +-- v5.6.1.0 / FF-04 (SG calibration)
                    |
                    +-- v5.6.2.0–v5.6.9.0 (NSSS physics expansion)
                    |
                    +-- [Phase 2 Complete]
                            |
                            +-- v5.7.0.0 / FF-09+10 (Architecture Hardening)
                            |       |
                            |       +-- v5.7.1.0 (Multicore / Performance)
                            |
                            +-- v5.8.0.0 (Cooldown & AFW)
                            |       |
                            |       +-- v5.8.1.0 (Steam Dump Modes)
                            |
                            +-- [Phase 3 Complete]
                                    |
                                    +-- v5.9.0.0 (Power Ascension)
                                    |
                                    +-- [Phase 4 Complete]
                                            |
                                            +-- v6.0.0.0 (Full NSSS Realism)
```

---

# Document Metadata

| Field | Value |
|-------|-------|
| Created | 2026-02-13 |
| Author | Triage pass (static code review) |
| Conformance | FUTURE_FEATURE_TEMPLATE.md v1.0 |
| Scope | Documentation only — no code changes |
| Files Reviewed | SGMultiNodeThermal.cs, PlantConstants.SG.cs, SGForensics.cs, VCTPhysics.cs, PressurizerPhysics.cs, SolidPlantPressure.cs, CVCSController.cs, CoupledThermo.cs, RCSHeatup.cs, PlantConstants.Pressure.cs, PlantConstants.CVCS.cs, HeatupSimEngine.cs, HeatupSimEngine.Init.cs, HeatupSimEngine.CVCS.cs, HeatupSimEngine.BubbleFormation.cs, HeatupSimEngine.HZP.cs, HeatupSimEngine.Logging.cs |

---

*End of Triage Document*
