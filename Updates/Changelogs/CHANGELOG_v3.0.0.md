# Changelog v3.0.0 — SG Thermal Model Physics Overhaul + RHR System Model

**Version:** 3.0.0  
**Date:** 2026-02-10  
**Classification:** MAJOR — Physics Architecture Rewrite  
**Implementation Plan:** IMPLEMENTATION_PLAN_v3.0.0.md  

---

## Summary

Complete replacement of the SG secondary-side heat transfer model and addition of the RHR thermal system model. The v2.x SG circulation-onset model was fundamentally incorrect — it treated stable thermal stratification (hot on top, cold on bottom) as a trigger for natural circulation, causing SG heat absorption of 14-19 MW (physically impossible) and crashing heatup rate to ~26°F/hr. Replaced with a physically correct thermocline-based stratification model. Also added a complete RHR thermal model (previously missing) to represent the heat source during early heatup before RCP start.

---

## New Files

### PlantConstants.RHR.cs
- **Path:** `Assets/Scripts/Physics/PlantConstants.RHR.cs`
- **Type:** Constants file (partial class of PlantConstants)
- **GOLD:** Yes
- **Purpose:** 19 RHR system constants across 6 regions (Pump, Heat Exchanger, Operating Modes, Fluid Properties, System Control, CVCS Integration)
- **Key constants:** `RHR_PUMP_HEAT_MW_TOTAL = 1.0`, `RHR_HX_UA_TOTAL = 40,000`, `RHR_SUCTION_VALVE_AUTO_CLOSE_PSIG = 585`, `RHR_HX_BYPASS_FRACTION_HEATUP = 0.85`

### RHRSystem.cs
- **Path:** `Assets/Scripts/Physics/RHRSystem.cs`
- **Type:** Physics module
- **GOLD:** Yes
- **Purpose:** Complete RHR thermal physics — pump heat, UA-LMTD counter-flow HX model, HX bypass control, isolation ramp, auto-isolation interlock, CVCS letdown path
- **Architecture:** Static class with `RHRMode` enum (Standby/Cooling/Heatup/Isolating), `RHRState` struct (18 fields), `RHRResult` struct (7 fields)
- **Public API:** `Initialize()`, `InitializeStandby()`, `Update()`, `BeginIsolation()`, `SetHXBypass()`, `GetDiagnosticString()`, `ValidateModel()`
- **Validation:** 8 tests (all passing)

---

## Modified Files (GOLD)

### PlantConstants.SG.cs
- **Change type:** Constants added and deprecated
- **Deprecated (3 constants, commented with explanation):**
  - `SG_CIRC_ONSET_DELTA_T_F = 30f` — wrong: stratification is stable, not a circulation trigger
  - `SG_CIRC_FULL_DELTA_T_F = 80f` — not applicable to thermocline model
  - `SG_CIRC_FULL_EFFECTIVENESS = 0.70f` — caused artificial mixing at 50,000 BTU/(hr·°F) UA
- **Added — Thermocline Stratification Model (7 constants):**
  - `SG_THERMOCLINE_ALPHA_EFF = 0.08 ft²/hr` — effective thermal diffusivity for thermocline descent
  - `SG_BUNDLE_PENALTY_FACTOR = 0.40` — tube bundle natural convection penalty (P/D = 1.42)
  - `SG_THERMOCLINE_TRANSITION_FT = 1.5 ft` — thermocline gradient width
  - `SG_BELOW_THERMOCLINE_EFF = 0.02` — residual effectiveness below thermocline
  - `SG_BOILING_ONSET_TEMP_F = 220°F` — nucleate boiling onset (atmospheric + N₂ blanket)
  - `SG_BOILING_HTC_MULTIPLIER = 5.0` — HTC enhancement when boiling active
  - `SG_UBEND_AREA_FRACTION = 0.12` — initial active area fraction (U-bend only)
- **Added — SG Draining Model (3 constants):**
  - `SG_DRAINING_START_TEMP_F = 200°F`
  - `SG_DRAINING_RATE_GPM = 150 gpm`
  - `SG_DRAINING_TARGET_MASS_FRAC = 0.55`
- **Retained unchanged:** All tube geometry (14 constants), HTC constants (4), node effectiveness arrays (3), node area/mass fractions (3)

### SGMultiNodeThermal.cs
- **Change type:** Core physics rewrite
- **Removed:**
  - `CalculateCirculationFraction()` — root cause of the physics error
  - `INTERNODE_UA_CIRCULATING = 50,000` — far too aggressive
  - All references to deprecated `SG_CIRC_*` constants
  - Dependency on `SG_HTC_NO_FLOW` (undefined — pre-existing compile issue)
- **Added to state struct (4 fields):**
  - `ThermoclineHeight_ft` — thermocline position from tubesheet (0-24 ft)
  - `ActiveAreaFraction` — fraction of tube area above thermocline
  - `ElapsedHeatupTime_hr` — drives thermocline descent
  - `BoilingActive` — boiling onset flag
- **Added to result struct (3 fields):**
  - `ThermoclineHeight_ft`, `ActiveAreaFraction`, `BoilingActive`
- **Deprecated (API compatibility, always 0/false):**
  - `CirculationFraction`, `CirculationActive` in both structs
- **Rewritten methods:**
  - `Update()` — 8-step physics: thermocline advance → boiling check → per-node HTC → thermocline-based area → Q = h × A_eff × ΔT × BundlePenalty → inter-node conduction → temp update → outputs
  - `GetNodeHTC()` — stagnant NC + boiling enhancement, no circFrac dependency
  - `GetNodeEffectiveAreaFraction()` — thermocline-based with transition zone interpolation
  - `GetDiagnosticString()` — shows thermocline height, ABOVE/BELOW/TRANS per node
- **Validation tests:** 10 tests (up from 8) — added thermocline descent, boiling onset, 2-hr energy accumulation
- **New private constants:**
  - `HTC_NO_RCPS = 8` — replaces undefined `SG_HTC_NO_FLOW`
  - `INTERNODE_UA_BOILING = 5,000` — replaces 50,000 circulating value
  - `NODE_HEIGHT_FT` — computed from total height / node count

### HeatupSimEngine.cs
- **Change type:** Integration (state fields, physics dispatch)
- **Added state fields (8):**
  - `sgThermoclineHeight`, `sgActiveAreaFraction`, `sgBoilingActive` (SG thermocline display)
  - `rhrState`, `rhrNetHeat_MW`, `rhrHXRemoval_MW`, `rhrPumpHeat_MW`, `rhrActive`, `rhrModeString` (RHR display)
- **Deprecated annotations:** `sgCirculationFraction`, `sgCirculationActive`
- **Step 1C (new):** RHR System Update every timestep — calls `RHRSystem.Update()`, triggers isolation on RCP start
- **Regime 1:** Added SG model update (display state), RHR heat contribution to RCS in solid plant and isolated heating
- **Regime 2 & 3:** Added thermocline display fields (`sgThermoclineHeight`, `sgActiveAreaFraction`, `sgBoilingActive`) to SG state sync
- **Header:** Added `RHRSystem` to physics modules list

### HeatupSimEngine.Init.cs
- **Change type:** Initialization additions
- **Added:** Thermocline display field initialization (`sgThermoclineHeight`, `sgActiveAreaFraction`, `sgBoilingActive`)
- **Added:** RHR system initialization — cold shutdown → `RHRSystem.Initialize()` (heatup mode), warm start → `RHRSystem.InitializeStandby()`
- **Added:** RHR display field initialization (`rhrNetHeat_MW`, `rhrHXRemoval_MW`, `rhrPumpHeat_MW`, `rhrActive`, `rhrModeString`)

---

## Physics Model Changes

### Before (v2.x): Circulation-Onset Model
- SG effectiveness driven by `CirculationFraction` (0→1 based on top-bottom ΔT)
- Circulation triggered at ΔT > 30°F, full at 80°F
- Inter-node UA ramped from 500 → 50,000 BTU/(hr·°F) — caused artificial bulk heating
- All tube area counted with no bundle penalty
- Result: SG absorbed 14-19 MW, heatup rate crashed to ~26°F/hr

### After (v3.0.0): Thermocline Model
- Thermocline boundary descends via thermal diffusion: z = H - √(4·α_eff·t)
- Only tubes above thermocline participate in heat transfer
- Initial active area: 12% (U-bend), slowly expanding to ~15-25% over 8-hour heatup
- Bundle penalty factor: 0.40 (dense tube bundle, P/D = 1.42)
- Inter-node UA: 500 stagnant, 5,000 near boiling zone only
- Boiling onset at 220°F enhances HTC by 5× in affected nodes
- Result: SG absorbs 0.5-3 MW subcooled, 4-10 MW with boiling → ~50°F/hr heatup

### RHR System (new)
- RHR operating during Mode 5 (cold shutdown) before RCP start
- Heat balance: pump heat (~1 MW) minus HX removal (0.1-0.5 MW bypassed) = net ~0.5-1 MW heating
- Auto-isolates at 585 psig or when RCPs start
- 5-minute flow ramp during isolation prevents thermal transients
- UA-LMTD counter-flow HX model with 2-iteration convergence

---

## Validation Targets

| Parameter | v2.x Actual | v3.0.0 Target | Source |
|-----------|-------------|---------------|--------|
| Heatup rate (4 RCPs) | 26°F/hr (locked) | 45-55°F/hr | NRC HRTD 19.2.2 |
| SG absorption (subcooled) | 14-19 MW | 0.5-3 MW | Thermodynamic analysis |
| SG absorption (boiling) | N/A | 4-10 MW | Nucleate boiling HTC |
| RHR net heat (heatup) | 0 MW (missing) | 0.5-1.5 MW | NRC HRTD 5.1 |
| Thermocline descent | N/A | 1-3 ft over 8 hr | Thermal diffusion |
| RCS-SG_top ΔT | 7°F (locked) | 5-15°F (growing) | Heat transfer physics |

---

## Research Documents Created

| Document | Path | Purpose |
|----------|------|---------|
| SG Thermal Model Research v3.0.0 | `Technical_Documentation/SG_THERMAL_MODEL_RESEARCH_v3.0.0.md` | SG thermocline physics, stratification analysis, Churchill-Chu correlation |
| RHR System Research v3.0.0 | `Technical_Documentation/RHR_SYSTEM_RESEARCH_v3.0.0.md` | RHR system design, HX sizing, operating modes, Byron exam data |
| SG Model Research Handoff | `Technical_Documentation/SG_MODEL_RESEARCH_HANDOFF.md` | Analysis handoff from research to implementation |

---

## Unaddressed Issues

| Issue | Reason | Planned |
|-------|--------|---------|
| SG draining model not integrated | Constants defined but integration requires SGMultiNodeThermal mass tracking updates | v3.1.0 |
| Per-SG level instrumentation | Requires SG draining + mass balance model | v3.1.0 |
| Grashof-based HTC scaling | Temperature factor retained as linear (adequate); Grashof improves accuracy | v3.1.0 |
| RHR letdown flow integration with CVCS | `LetdownFlow_gpm` tracked in RHRState but not wired to CVCS controller | v3.1.0 |
| Dashboard/GUI updates for new fields | `sgThermoclineHeight`, `sgActiveAreaFraction`, `sgBoilingActive`, RHR fields need display | v3.1.0 |
| HeatupSimEngine.Logging.cs updates | Log files should include RHR state and thermocline data | v3.1.0 |
| CCW system thermal model | RHR HX shell-side uses constant CCW inlet temp (85°F) | v1.3.0 per roadmap |

---

## Breaking Changes

- `SGMultiNodeState.CirculationFraction` — always returns 0 (was 0-1). Retained for API compatibility.
- `SGMultiNodeState.CirculationActive` — always returns false. Retained for API compatibility.
- `SGMultiNodeResult.CirculationFraction` / `CirculationActive` — same deprecation.
- Any code that branched on `CirculationActive == true` will never take that branch.
- `CalculateCirculationFraction()` removed — any external caller will fail to compile.

---

## Sources

- NRC HRTD 5.1 (ML11223A219) — Residual Heat Removal System
- NRC HRTD 19.0 (ML11223A342) — Plant Operations (heatup/cooldown)
- NRC HRTD 2.3 — Steam Generator Design
- NRC Bulletin 88-11 — Thermal Stratification in PWR Systems
- NUREG/CR-5426 — SG Natural Circulation Phenomena
- Byron NRC Exam 2019 (ML20054A571) — RHR HX Bypass Valve Operations
- Incropera & DeWitt — Fundamentals of Heat and Mass Transfer (Ch. 5, 9, 10)
- Churchill-Chu correlation for horizontal cylinders
- WCAP-8530 / WCAP-12700 — Westinghouse Model F SG Design
