# Changelog v5.1.0 — SG Pressure–Saturation Coupling Fix

**Date:** 2026-02-12  
**Version:** 5.1.0  
**Type:** MINOR — Physics Model Correction  
**Implementation Plan:** IMPLEMENTATION_PLAN_v5.1.0.md  

---

## Summary

Replaces the artificial 200 psi/hr secondary pressure rate limiter with three physically correct mechanisms: direct saturation tracking, steam line condensation energy damping, and wall superheat boiling driver. Resolves the runaway SG heat sink that caused multi-MW heat absorption spikes and RCS cooldown during late heatup.

**Root cause:** The rate clamp decoupled secondary pressure from the thermodynamic state, holding T_sat artificially low while boiling HTCs activated. This created an unphysical driving ΔT (T_rcs − T_sat) of hundreds of °F, producing 27+ MW heat absorption that exceeded the 21 MW RCP heat input and reversed the heatup.

**Fix:** Pressure now tracks the saturation curve directly (`P_secondary = P_sat(T_hottest)`), creating a self-regulating negative feedback loop. Physical damping from steam line condensation provides realistic early-boiling pressure lag. Wall superheat reduces the boiling driving force from `(T_rcs − T_sat)` to `(T_wall − T_sat)`, preventing exaggerated heat transfer.

---

## Changes

### Stage 1 — Remove Artificial Rate Limit & Implement Saturation Tracking

#### `SGMultiNodeThermal.cs`

##### Removed
- **`MAX_PRESSURE_RATE_PSI_HR` constant** (200 psi/hr) — artificial rate clamp that decoupled pressure from thermodynamic state

##### Changed
- **`UpdateSecondaryPressure()` boiling branch** — complete rewrite. During boiling regime, secondary pressure is now set directly as `P_secondary = P_sat(T_hottest_boiling_node)`. Rate-limiting logic (`maxDeltaP`, `deltaP` clamping) removed entirely. Pressure still clamped to physical limits: N₂ blanket floor, steam dump cap (1092 psig), safety valve ceiling.

##### Added
- **Validation Test 16** — verifies pressure equals `P_sat(350°F)` within 5 psia after one timestep (confirms instantaneous saturation tracking)

#### `PlantConstants.SG.cs`

##### Removed
- **`SG_MAX_PRESSURE_RATE_PSI_HR` constant** (200f) — replaced by comment documenting removal rationale and reference to v5.1.0 implementation plan

---

### Stage 2 — Saturation Tracking Hardening & Diagnostics

#### `SGMultiNodeThermal.cs`

##### Added
- **Boiling→subcooled reversion guard** in `UpdateSecondaryPressure()` — prevents pressure from snapping back to 17 psia when T_hottest transiently dips below T_sat mid-heatup. Once pressure has risen above the N₂ blanket value, the boiling branch continues tracking `P_sat(T_hottest)` downward smoothly. Pressure only returns to N₂ blanket when it naturally falls to that level.
- **Saturation coupling diagnostics** in `GetDiagnosticString()` — new log line: `v5.1.0: T_hottest=XXX°F | P_sat(T_hot)=XXX psig | ΔT_driving(T_rcs-T_sat)=XX.X°F`
- **Validation Test 22** — verifies reversion guard: after boiling at ~135 psia, dropping T_hottest 2°F below T_sat does not snap pressure to 17 psia

##### Changed
- **Stale comments updated** — `UpdateSecondaryPressure()` method doc and inline comments updated to reflect saturation tracking replacing rate-limited model

---

### Stage 3 — Steam Line Warming Model (Physical Rate Damping)

#### `PlantConstants.SG.cs`

##### Added
- **`#region Steam Line Warming Model (v5.1.0 Stage 3)`** with 3 new constants:

| Constant | Value | Unit | Basis |
|----------|-------|------|-------|
| `SG_STEAM_LINE_METAL_MASS_LB` | 120,000 | lb | 4 × 30" Sch 80 lines × 100 ft + MSIVs |
| `SG_STEAM_LINE_CP` | 0.12 | BTU/(lb·°F) | A106 Grade B carbon steel |
| `SG_STEAM_LINE_UA` | 2,200,000 | BTU/(hr·°F) | Film condensation on ~3,140 ft² interior area |

#### `SGMultiNodeThermal.cs`

##### Added
- **`SteamLineTempF` state field** on `SGMultiNodeState` — tracks average steam line piping temperature. Initialized at simulation start temperature (same as RCS).
- **`SteamLineCondensationRate_BTUhr` state field** on `SGMultiNodeState` — diagnostic: current condensation heat rate from steam to metal.
- **Steam line initialization** in `Initialize()` — `SteamLineTempF = initialTempF`, condensation rate = 0.
- **Section 8c: Steam Line Condensation Energy Sink** in `Update()` — 65-line physics block inserted between delta clamp (Section 8b) and steam production accounting. Physics:
  - `Q_condensation = UA × (T_sat − T_steamLine)`
  - Steam line warms: `dT/dt = Q_condensation / (M_metal × Cp)`
  - Capped at T_sat (cannot exceed steam temperature)
  - Condensation capped at 95% of available boiling energy (stability)
  - Subtracted from `totalQ_boiling_BTUhr` before steam production
  - **Never modifies pressure directly** — only reduces boiling energy budget
- **Steam line diagnostic line** in `GetDiagnosticString()` — `SteamLine: T=XXX.X°F | Q_cond=X.XX MW`
- **Validation Test 23** — steam line warms during boiling (50 steps), rises above initial, stays below T_sat
- **Validation Test 24** — steam line initialization correctness (T = initialTempF, rate = 0)

##### Changed
- **`UpdateSecondaryPressure()` doc comments** — updated to reference steam line condensation as physical damping mechanism
- **Removed-rate-clamp comment** in `PlantConstants.SG.cs` — updated to reference Stage 3

---

### Stage 4 — Wall Superheat Boiling Driver

#### `PlantConstants.SG.cs`

##### Added
- **`#region Wall Superheat Model (v5.1.0 Stage 4)`** with 1 new constant:

| Constant | Value | Unit | Basis |
|----------|-------|------|-------|
| `SG_PRIMARY_FORCED_CONVECTION_HTC` | 1,500 | BTU/(hr·ft²·°F) | Dittus-Boelter at Re ~400k, referenced to outer tube area, conservative for fouling/maldistribution |

#### `SGMultiNodeThermal.cs`

##### Changed
- **Section 5 per-node boiling ΔT** — boiling driving force changed from `(T_rcs − T_sat)` to `(T_wall − T_sat)`. Wall temperature computed algebraically per node:
  ```
  T_wall = T_rcs - Q_node_prev / (h_primary × A_node)
  Clamped: T_sat ≤ T_wall ≤ T_rcs
  ```
  Uses previous timestep Q to avoid implicit coupling. The `drivingT_boil` variable is set such that `deltaTNode = T_wall − T_sat` during fully boiling conditions, blended with subcooled driving T via the existing regime blend ramp.
- **Section 5 comment header** — updated to document wall superheat physics
- **`Update()` method doc comment** — updated to reference all 4 v5.1.0 stages

##### Added
- **`ComputeDiagWallTemp()` private method** — computes T_wall for the hottest boiling node (diagnostic use by `GetDiagnosticString()`)
- **Wall temperature diagnostic line** in `GetDiagnosticString()` — `Wall: T_wall=XXX.X°F | ΔT_wall(Tw-Tsat)=XX.X°F`
- **Validation Test 25** — T_wall bounds during boiling: `T_sat ≤ T_wall ≤ T_rcs`, with warning if no wall drop detected
- **Validation Test 26** — `SG_PRIMARY_FORCED_CONVECTION_HTC` exists and is positive
- **Updated validation pass message** — `"v5.1.0 — saturation + steam line + wall superheat"`

---

## Files Modified

| File | Stages | Nature |
|------|--------|--------|
| `SGMultiNodeThermal.cs` | 1, 2, 3, 4 | Core physics: pressure model rewrite, steam line warming, wall superheat, diagnostics, 10 new validation tests (16–26) |
| `PlantConstants.SG.cs` | 1, 3, 4 | Constants: removed rate clamp, added 3 steam line constants, added 1 primary HTC constant |

---

## Validation Tests Added

| Test | Stage | Description |
|------|-------|-------------|
| 16 | 1 | Pressure tracks P_sat(350°F) within 5 psia (saturation tracking) |
| 17 | — | Energy balance: steam produced during boiling (pre-existing, renumbered) |
| 18 | — | Pressure caps at steam dump setpoint (pre-existing, renumbered) |
| 19 | — | T_sat at steam dump = ~557°F (pre-existing, renumbered) |
| 20 | — | Regime blend ramp (pre-existing, renumbered) |
| 21 | — | Delta clamp ≤5 MW/step (pre-existing, renumbered) |
| 22 | 2 | Reversion guard: pressure does not snap to 17 psia on transient T_hottest dip |
| 23 | 3 | Steam line warms during boiling, stays below T_sat |
| 24 | 3 | Steam line initialization (T = initial, rate = 0) |
| 25 | 4 | Wall temperature bounded: T_sat ≤ T_wall ≤ T_rcs |
| 26 | 4 | Primary HTC constant exists and is positive |

---

## Physics Summary

### Before v5.1.0
```
P_secondary: rate-limited at 200 psi/hr (artificial)
Boiling ΔT: T_rcs - T_sat (bulk primary, overpredicts)
Damping: none (rate clamp was only mechanism)
Result: 27+ MW SG absorption → RCS cooldown at -16°F/hr
```

### After v5.1.0
```
P_secondary = P_sat(T_hottest)     [thermodynamically exact]
Boiling ΔT  = T_wall - T_sat      [wall superheat, physically correct]
Damping     = steam line condensation [energy sink, self-regulating]

Feedback loop:
  Higher T_hottest → higher P_secondary → higher T_sat
  → smaller (T_wall - T_sat) → less boiling → slower T rise

Early boiling: cold steam lines absorb energy → damped pressure rise
Late boiling:  warm steam lines → pure saturation tracking
```

---

## Not Addressed (Deferred)

| Issue | Reason | Target |
|-------|--------|--------|
| Per-SG pressure modeling | Lumped 4-SG model sufficient for Phase 0 | v5.4.7 |
| Secondary mass conservation tracking | Requires feedwater system for closure | v5.4.4 |
| SG level impact on boiling area | Requires secondary mass/level model | v5.4.4 |
| Distributed steam line model (per-line) | Lumped model sufficient for Phase 0 | v6.0.0 |
| Reverse regime blend ramp (boiling→subcooled) | Instant reset acceptable for heatup | v5.2.0 |

---

*Changelog created: 2026-02-12*  
*Implementation Plan: IMPLEMENTATION_PLAN_v5.1.0.md*
