# Future Enhancements Roadmap
## Critical: Master the Atom — PWR Heatup Simulation

**Document Version:** 2.1  
**Last Updated:** 2026-02-11  
**Maintainer:** Project Development Team

---

## Overview

This document tracks planned enhancements for the Critical: Master the Atom PWR simulation, organized by priority. Each enhancement includes technical scope, dependencies, and estimated effort.

All completed features have been consolidated into the Completed Enhancements section at the bottom.

---

## Priority-Ordered Pending Work

### PRIORITY 1 — Validation Dashboard Redesign (Multi-Tab Layout)

**Status:** PLANNED  
**Added:** 2026-02-11  
**Estimated Effort:** 12-16 hours  

**Problem:** The current `HeatupValidationVisual` dashboard has grown significantly with the addition of SG multi-node thermal data, RHR system state, spray system, orifice lineup, heater PID details, and multiple history graphs. All of this is squeezed into a single IMGUI screen, making it difficult to read at a glance. With SG/RHR needing their own display, and future systems (CCW, excess letdown) on the horizon, a redesign is overdue.

**Proposed Architecture: Multi-Tab Dashboard**

| Tab | Contents | Priority |
|-----|----------|----------|
| **Tab 1 — Overview** | T_avg, T_hot, T_cold, Pressure, PZR Level, Subcooling, Heatup Rate, Pressure Rate, Physics Regime, Sim Time, Speed Controls. Arc gauges + key history graphs (Temp, Pressure, PZR Level). Critical alarms. | PRIMARY — always-visible "at a glance" |
| **Tab 2 — PZR / Pressure Control** | PZR detail: heater mode, PID output/error, spray valve/flow/ΔT/condensation, PORV/SV state, steam bubble mass/volume, level program setpoint. Bidirectional surge flow gauge. PZR history graphs. | HIGH |
| **Tab 3 — CVCS / Inventory** | Charging/letdown flows, orifice lineup, VCT level/boron, BRS status, seal injection breakdown, mass conservation audit, inventory history graphs. | HIGH |
| **Tab 4 — SG / RHR** | SG multi-node thermal state, per-node temps, stratification ΔT, SG secondary pressure/T_sat/boiling intensity, steam dump state. RHR mode/flow/HX temps/isolation status. SG and RHR history graphs. | HIGH |
| **Tab 5 — RCP / Electrical** | Per-pump status (flow fraction, heat, ramp time), total RCP heat, grid load breakdown, cumulative energy. | MEDIUM |
| **Tab 6 — Event Log** | Full scrollable event log with severity filtering. | MEDIUM |
| **Tab 7 — Validation** | All PASS/FAIL checks from v0.9.6, v1.1.0, v4.4.0. Inventory audit detail. Conservation error tracking. | LOW (debugging) |

**Design Principles:**
- Keep arc gauges and bidirectional gauges — they work well
- Keep history graphs — essential for validating behavior over time
- Tab 1 should be sufficient for normal monitoring (the "operator's eye")
- Deeper tabs for system-specific investigation
- Unity IMGUI `GUILayout.Toolbar()` or similar tab selector at top
- Each tab rendered by its own method in the appropriate partial class
- Consider keyboard shortcuts for tab switching (Ctrl+1 through Ctrl+7)

**Dependencies:** None — purely visual reorganization of existing data.

---

### PRIORITY 2 — Scenario Selector (Initial Conditions Framework)

**Status:** PLANNED  
**Added:** 2026-02-11  
**Estimated Effort:** 8-12 hours

**Description:** Currently the simulation always starts from Cold Shutdown (Mode 5, ~150°F, solid pressurizer). A real training simulator offers multiple starting scenarios. This feature adds a scenario selection menu that initializes the simulation at different plant states.

**Proposed Scenarios:**

| Scenario | Mode | T_avg | Pressure | PZR State | RCPs | Key State |
|----------|------|-------|----------|-----------|------|-----------|
| Cold Shutdown | 5 | 150°F | 100-400 psig | Solid | 0 | RHR cooling, CVCS aligned |
| Mid-Heatup (Pre-Bubble) | 5→4 | 250°F | Solid ~600 psia | Solid | 1-2 | RCPs ramping, heaters pressurizing |
| Post-Bubble Formation | 4 | 350°F | 800-1000 psia | Steam bubble | 2-3 | Bubble formed, draining to setpoint |
| Hot Shutdown | 4 | 350°F | 2235 psig | Normal | 3-4 | Pressure at NOP, ready for Mode 3 |
| Hot Standby (HZP) | 3 | 557°F | 2235 psig | Normal | 4 | Steam dump active, ready for criticality |

**Technical Scope:**
- `ScenarioDefinition` struct/ScriptableObject with all initial state fields
- `HeatupSimEngine.LoadScenario(ScenarioDefinition)` method
- Scenario selection UI (pre-simulation menu or dropdown)
- Must set: all temperatures, pressure, PZR level/mass/phase, RCP states, CVCS alignment, heater mode, SG secondary state, VCT/BRS state, boron concentration, bubble formation flags
- Validation: each scenario must pass all PASS/FAIL checks at t=0

**Dependencies:** None for the framework. Some scenarios (Hot Standby) depend on HZP stabilization working correctly (v1.1.0 — already complete).

---

### PRIORITY 3 — Heatup Temperature Hold at ~200°F (Mode 5→4 Transition)

**Status:** PLANNED — RESEARCH CONFIRMED  
**Added:** 2026-02-11  
**Estimated Effort:** 4-6 hours

**Background Research:**

Your authority is correct. Per NRC HRTD Section 19.0 (Plant Operations) and standard Westinghouse PWR startup procedures, there is a procedural hold during heatup in the vicinity of the Mode 5→Mode 4 boundary (200°F T_avg). The specific temperature varies by plant — some hold at 180°F, some at 200°F — depending on plant-specific Tech Spec requirements. The purpose of this hold is to:

1. **RCS leak rate verification** — Operators stabilize temperature and monitor pressurizer level trend to confirm no abnormal leakage before continuing to higher temperatures where leak consequences are more severe
2. **RHR system alignment** — Final checks on RHR configuration before securing SDC and transitioning to RCP-only heatup
3. **Safety Injection accumulator alignment** — SIT outlet valves are opened before RCS pressure exceeds accumulator cover gas pressure (~650 psig, which corresponds to roughly 300 psia RCS)
4. **Technical Specification compliance** — Mode change requires verification of all Mode 4 LCO requirements (operable equipment, boron concentration, shutdown margin)
5. **Instrument/surveillance checks** — Various surveillance requirements that must be completed before exceeding 200°F

The NRC HRTD Section 19.0 also references a separate hold/check at >400°F and 2235 psig for the formal RCS leak rate test (required only if the RCS has been opened for refueling). Some plants also do a brief stabilization at ~350°F when starting the 3rd/4th RCP.

**Implementation Approach for HeatupSimEngine:**

The simulation currently runs continuously from cold shutdown to HZP. To model the temperature hold:

- Add a `HeatupHoldState` enum: `NORMAL_HEATUP`, `APPROACHING_HOLD`, `HOLDING`, `HOLD_COMPLETE`
- Define `HOLD_TEMPERATURE_F = 200f` (configurable per scenario)
- Define `HOLD_DURATION_HR` (e.g., 0.5-1.0 hours sim time — represents operator checks)
- When `T_avg` approaches hold temperature: reduce heatup rate to zero by adjusting RHR HX bypass or pausing RCP start sequence
- During hold: maintain temperature stable, log "MODE 5→4 TRANSITION HOLD" event, run simulated checks (leak rate trending, SIT alignment verification)
- After hold duration: resume normal heatup, log "HOLD COMPLETE — ENTERING MODE 4"
- Display hold status in dashboard (Tab 1 overview)
- Allow user to skip hold via time acceleration (or auto-complete in fast modes)

**This also supports the Scenario Selector** — a "Mid-Heatup" scenario could start just after this hold is complete.

---

### PRIORITY 4 — In-Game Help System (F1 Key)

**Status:** PLANNED  
**Added:** 2026-02-11  
**Estimated Effort:** 8-12 hours

**Description:** A context-sensitive help overlay accessible via F1 that provides gameplay instructions, scenario objectives, and nuclear engineering reference material. Essential for making the simulation accessible to players who aren't nuclear engineers.

**Proposed GUI Type:** Overlay panel (not a separate screen) — semi-transparent dark background with text content, dismissible with F1 or Escape. Should not pause simulation but could offer a "pause while reading" toggle.

**Content Structure:**

| Section | Content |
|---------|---------|
| **Quick Reference** | Keyboard shortcuts, screen navigation (F1-F8), time controls |
| **Current Scenario** | Active scenario name, objectives, completion criteria, next procedural step |
| **Procedure Guide** | Step-by-step heatup procedure with current step highlighted (e.g., "Step 5: Start RCP #2 when T_avg > 160°F and pressure > 300 psig") |
| **System Glossary** | Brief explanations of PZR, CVCS, RCP, SG, RHR, etc. |
| **Alarm Reference** | What each annunciator alarm means and typical operator response |
| **P-T Limits** | Visual P-T curve showing current operating point vs limits |

**GUI Considerations:**
- Unity IMGUI or UI Toolkit panel (consistent with existing visual style)
- Tabbed or scrollable sections within the help panel
- Context-aware: if viewing Screen 3 (PZR), help defaults to PZR system section
- Procedure guide updates automatically based on simulation state
- Could include simple diagrams (system schematics as sprites/textures)

**Dependencies:** Scenario Selector (Priority 2) for scenario-specific objectives. Can be implemented in phases — keyboard shortcuts and glossary first, contextual procedure guide later.

---

### PRIORITY 5 — Operator Screen Data Gaps (Placeholder Resolution)

**Status:** OPEN  
**Reason:** Multiple operator screens display "---" (PLACEHOLDER) for parameters that lack backing physics models or ScreenDataBridge getters. This undermines realism and operator training fidelity.

**Resolution Approach:**
1. **Phase 1 (HIGH):** PORV/SV valve state, seal injection, boration/dilution flows (core NSSS parameters)
2. **Phase 2 (MEDIUM):** VCT thermal/pressure models, CCP pump curves, boron worth calculation, letdown HX thermal model
3. **Phase 3 (LOW):** Dynamic setpoints, regenerative HX, demineralizer flow, charging temperature
4. Each new ScreenDataBridge getter must follow the established NaN-placeholder convention
5. Each resolved placeholder must be verified against Westinghouse 4-Loop PWR operating data

#### Screen 3 — Pressurizer (5 placeholders remaining)

| Parameter | Priority | Required Physics | Notes |
|-----------|----------|-----------------|-------|
| ~~Spray Flow (gpm)~~ | ~~HIGH~~ | ~~Spray model~~ | **RESOLVED v4.4.0** — wire `sprayFlow_GPM` to ScreenDataBridge |
| PORV State (discrete) | HIGH | PORV valve model with open/close state tracking | |
| Safety Valve State (discrete) | HIGH | SV model with open/close state tracking | |
| Backup Heater Status (discrete) | MEDIUM | Separate proportional vs backup heater tracking | **Partially available** — `heaterPIDState.BackupOn` exists from v4.4.0 |
| Dynamic Pressure Setpoint | LOW | Pressure setpoint controller (currently fixed 2235) | |
| Dynamic Level Setpoint | LOW | Level setpoint controller (currently fixed 60%) | `pzrLevelSetpointDisplay` already computed |

#### Screen 4 — CVCS (10 placeholders)

| Parameter | Priority | Required Physics |
|-----------|----------|-----------------|
| Seal Injection Flow (gpm) | HIGH | Seal injection model (CCP discharge split) |
| Boration Flow (gpm) | HIGH | Boration/dilution flow controller |
| Dilution Flow (gpm) | HIGH | Boration/dilution flow controller |
| Boron Worth (pcm) | MEDIUM | Boron reactivity worth calculation |
| VCT Temperature (°F) | MEDIUM | VCT thermal model |
| VCT Pressure (psig) | MEDIUM | VCT gas space model (H₂ overpressure) |
| CCP Discharge Pressure (psig) | MEDIUM | CCP pump curve model |
| Letdown Temperature (°F) | MEDIUM | Letdown HX thermal model |
| Charging Temperature (°F) | LOW | Regenerative HX / VCT outlet thermal model |
| Purification Flow (gpm) | LOW | Demineralizer flow model |

#### Screen 5 — Steam Generators (6 placeholders)

| Parameter | Priority | Required Physics |
|-----------|----------|-----------------|
| SG-A/B/C/D Level (%) | HIGH | Per-SG level instrumentation model |
| Feedwater Flow (Mlbm/hr) | HIGH | Feedwater system model |
| Steam Flow (Mlbm/hr) | HIGH | Steam flow model (main steam lines, MSIVs) |
| Per-SG Primary Temps | MEDIUM | Per-loop thermal-hydraulic model (currently lumped) |
| Per-SG Steam Pressure | MEDIUM | Per-SG secondary pressure model (currently lumped) |
| SG Blowdown | LOW | SG blowdown system model |

#### Screen 6 — Turbine-Generator (15 placeholders)

Entirely dependent on turbine-generator system model (Power Ascension scope). All parameters blocked until that system exists.

#### Screen 7 — Secondary Systems (13 placeholders)

Mostly dependent on feedwater/condensate system models (Power Ascension scope). Steam dump controls and main steam pressure are live.

#### Screen 8 — Auxiliary Systems (16 placeholders)

RHR parameters now have physics backing from v3.0.0 — need wiring to ScreenDataBridge. CCW/SW systems not yet modeled.

**Immediate wins (wire existing data):**
- Spray Flow → `sprayFlow_GPM` (v4.4.0)
- Backup Heater Status → `heaterPIDState.BackupOn` (v4.4.0)
- Dynamic Level Setpoint → `pzrLevelSetpointDisplay` (already computed)
- RHR parameters (Screen 8) → `rhrState` fields (v3.0.0)

---

### PRIORITY 6 — Reactor Core Data Validation

**Status:** OPEN  
**Added:** 2026-02-08

| Issue | Expected | Found | Impact |
|-------|----------|-------|--------|
| Total RCCAs | 53 | 68 | Incorrect rod worth, incorrect bank positions |
| Shutdown Bank D | 4 assemblies | 8 | Overstated shutdown margin |
| Control Bank D | 9 assemblies | 12 | Incorrect rod insertion reactivity |
| Control Bank C | 9 assemblies | 16 | Incorrect rod insertion reactivity |
| Fuel-only assemblies | 140 | 125 | Incorrect power distribution |

**Resolution:** Validate `CoreMapData` against WCAP-10965 or equivalent Westinghouse reference. Correct assembly assignments and bank membership. Revalidate all rod worth calculations after correction.

---

### PRIORITY 7 — v1.2.0: BRS Enhancements & Remaining Mass Conservation Work

**Status:** Scoped  
**Estimated Effort:** 12-15 hours total

| ID | Enhancement | Priority | Effort | Notes |
|----|-------------|----------|--------|-------|
| 1.2.0 | Mass conservation — `rcsWaterMass` stale during solid operations | MEDIUM | 6-8 hr | Audit resolved in v2.0.10 via state-based calc, but flow-integrated field still stale. Broader refactor needed. |
| 1.2.1 | BRS Evaporator thermal model | MEDIUM | 4 hr | Currently displayed as 15 gpm but not modeled |
| 1.2.2 | BRS Distillate Return to VCT | MEDIUM | 2 hr | Depends on 1.2.1 |
| 1.2.3 | BRS Boric Acid Concentration Tracking | MEDIUM | 3 hr | Depends on 1.2.1 |
| 1.2.4 | VCT Auto-Makeup from BRS Distillate | LOW | 2 hr | Depends on 1.2.2 |

---

### PRIORITY 8 — v4.6.0: Excess Letdown Path Model

**Status:** PLANNED  
**Added:** 2026-02-11 (from v4.4.0 Deficit Analysis #5)  
**Estimated Effort:** 4-6 hours

Per NRC HRTD 4.1 Section 4.1.2.5: excess letdown provides supplemental letdown during heatup (when orifice flow is low) or when normal letdown is unavailable. Capacity ~20 gpm at normal operating pressure via 1-inch line from Loop 3 cold leg through excess letdown HX.

**Scope:** Excess letdown HX model, isolation valves, divert valve (to CVCS or RCDT), operator control interface.

---

### PRIORITY 9 — v1.3.0: Remaining Thermal System Models

**Status:** Partially delivered (RHR in v3.0.0)

| ID | Enhancement | Priority | Effort | Status |
|----|-------------|----------|--------|--------|
| 1.3.2 | CCW System Model | MEDIUM | 6 hr | Planned — required for realistic RHR and letdown HX operation |
| 1.3.3 | RHR Letdown Thermal Path wiring to CVCS | MEDIUM | 4 hr | Constants exist in v3.0.0, CVCS wiring deferred |

---

### PRIORITY 10 — v1.5.0: Heat Exchanger Refinements

**Status:** Conceptual  
**Estimated Effort:** 14 hours total

| ID | Enhancement | Priority | Dependencies | Effort |
|----|-------------|----------|--------------|--------|
| 1.5.1 | Letdown Heat Exchanger Model | MEDIUM | CCW (1.3.2) | 4 hr |
| 1.5.2 | Regenerative Heat Exchanger | LOW | None | 4 hr |
| 1.5.3 | Seal Injection Heat Exchanger | LOW | CCW (1.3.2) | 3 hr |
| 1.5.4 | Temperature-Dependent SG HTC (Grashof-based) | LOW | None | 3 hr |

---

### PRIORITY 11 — Active Operator SG Pressure Management

**Status:** PLANNED FOR FUTURE RELEASE  
**Added:** 2026-02-11 (from v4.3.0 Unaddressed Issue #7)

Operators actively manage SG secondary pressure by stepping it up to track primary temperature using atmospheric dump valves, turbine bypass, and steam line drains. The v4.3.0 model uses quasi-static equilibrium which is valid but doesn't capture active operator intervention.

**Scope:** ADV control model, operator-initiated secondary pressure adjustments, steam line warming procedures. Requires operator interaction framework.

---

### PRIORITY 12 — v2.0.0: Cooldown Procedures

**Status:** Conceptual  
**Estimated Effort:** 33 hours total  
**Dependencies:** v1.3.x thermal system models

| ID | Enhancement | Priority | Effort |
|----|-------------|----------|--------|
| 2.0.1 | Controlled Cooldown Sequence (HZP → Cold Shutdown) | HIGH | 12 hr |
| 2.0.2 | RCP Sequential Shutdown | HIGH | 4 hr |
| 2.0.3 | RHR Entry Procedures | HIGH | 6 hr |
| 2.0.4 | Cooldown Rate Limiting | MEDIUM | 3 hr |
| 2.0.5 | AFW System Model | MEDIUM | 8 hr |

---

### PRIORITY 13 — v2.5.0: Advanced Steam Dump Modes

**Status:** Conceptual  
**Dependencies:** v1.1.0 (complete)

| ID | Enhancement | Priority | Effort |
|----|-------------|----------|--------|
| 2.5.1 | T_avg Control Mode | MEDIUM | 4 hr |
| 2.5.2 | Load Rejection Mode | MEDIUM | 4 hr |
| 2.5.3 | Atmospheric Dump Valves | LOW | 3 hr |
| 2.5.4 | Steam Dump Interlock Logic | LOW | 2 hr |

---

### PRIORITY 14 — Power Ascension & Turbine-Generator

**Status:** Future Scope  
**Dependencies:** v2.x completion  
**Estimated Effort:** 72+ hours

| ID | Enhancement | Priority | Effort |
|----|-------------|----------|--------|
| 3.0.1 | Enhanced Reactor Kinetics | HIGH | 20+ hr |
| 3.0.2 | Automatic Rod Control Enhancement | HIGH | 12 hr |
| 3.0.3 | Power Range Instrumentation | HIGH | 16 hr |
| 3.0.4 | Turbine Generator Model | HIGH | 16 hr |
| 3.0.5 | Grid Synchronization | MEDIUM | 8 hr |

**Note:** This unlocks Screen 6 (Turbine-Generator) and Screen 7 (Secondary Systems) placeholder resolution.

---

### Visual/UI Deferred Items

| ID | Enhancement | Target | Origin |
|----|-------------|--------|--------|
| 4.0.D1 | Per-bank rod position tracking in ReactorController | Next physics pass | v4.0.0 |
| 4.0.D2 | Rod step demand vs actual position mismatch indicators | Next physics pass | v4.0.0 |
| 4.0.D3 | Gauge analog dial needles | Cosmetic pass | v4.0.0 |
| 4.0.D4 | Screens 2-8 visual overhaul (Blender panel treatment) | Per-screen | v4.0.0 |
| 4.1.D1 | Analog dial needles on gauges | Cosmetic pass | v4.1.0 |
| 4.1.D2 | CRT scanline overlay shader | Cosmetic pass | v4.1.0 |
| 4.1.D3 | Per-assembly fuel temperature gradient in core cells | v5.0.0 (physics req) | v4.1.0 |
| 4.1.D4 | Screens 2-8 TMP visual upgrade | Per-screen | v4.1.0 |
| 4.2.2.D1 | Screens 2-8 annunciator alarm panels | Per-screen | v4.2.2 |
| 4.2.2.D2 | Advanced alarm management (first-out, reflash, priority queue) | Future | v4.2.2 |
| 4.2.2.D3 | AUTO ROD CONTROL tile binding | Next physics pass | v4.2.2 |
| 4.2.2.D4 | RCS T-H bridge to ReactorController | Future | v4.2.2 |
| 4.2.2.D5 | Bottom panel GUI boundary alignment pass | Cosmetic pass | v4.2.2 |

---

### Technical Debt

| Item | Priority | Description | Target |
|------|----------|-------------|--------|
| HeaterMode cleanup | LOW | Consolidate heater mode enums | Next refactor |
| Physics module tests | MEDIUM | Add unit tests for all physics modules | Ongoing |
| PlantConstants validation | LOW | Add ValidateConstants() coverage for new constants | Ongoing |
| Memory optimization | LOW | Profile and optimize per-frame allocations | Future |
| `rcsWaterMass` stale during solid ops | MEDIUM | Flow-integrated field stale when `UpdateRCSInventory()` guarded off | v1.2.0 |

---

## Completed Enhancements

| Version | Description | Date |
|---------|-------------|------|
| v0.7.0 | Heatup Simulation Core (Cold Shutdown → HZP, bubble formation, RCP starts, CVCS/BRS) | 2026-02 |
| v0.8.0 | SG Secondary Thermal Mass | 2026-02 |
| v1.0.0 | Reactor Operator GUI (core map, 17 gauges, rod control, alarms) | 2026-02-07 |
| v1.1.0 | HZP Stabilization & Reactor Operations Handoff (Steam dump, HZP controller, PID heaters, inventory audit, handoff) | 2026-02 |
| v1.3.0 | RHR Heat Exchanger Model (partial — delivered in v3.0.0) | 2026-02-10 |
| v2.0.10 | Inventory audit state-based mass calculation fix | 2026-02 |
| v3.0.0 | SG Thermal Model Physics Overhaul + RHR System Model | 2026-02-10 |
| v4.0.0 | Reactor Operator Screen Visual Overhaul (Blender panels, Screen 1) | 2026-02-11 |
| v4.1.0 | Mosaic Board Visual Upgrade (TMP fonts, glow materials, fill bars, sprites) | 2026-02-11 |
| v4.2.2 | Bottom Panel Layout Fix & Annunciator-Style Alarm Panel | 2026-02-11 |
| v4.3.0 | SG Secondary Pressure Model, Dynamic Boiling, HTC Recalibration | 2026-02-11 |
| v4.4.0 | PZR Level/Pressure Control Fix (CVCS mass drain integration, 3-orifice lineup, heater PID transition, spray system, validation logging) | 2026-02-11 |
| v4.5.0 | Pressurizer Spray System Model (merged into v4.4.0 Stage 4) | 2026-02-11 |

---

## References

- NRC HRTD Series (Sections 4.1, 5.1, 6.1, 10.2, 10.3, 11.2, 19.0)
- Westinghouse 4-Loop PWR Technical Specifications
- Implementation Plans (v0.4.0 through v4.4.0)
- Heatup Simulation Logs and Analysis Reports

---

## Document Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2026-02-08 | 1.0 | Initial roadmap |
| 2026-02-08 | 1.1 | Moved Steam Dump and HZP Stabilization to v1.1.0 |
| 2026-02-10 | 1.2 | Added v3.0.0 plan, operator screen placeholder gaps, reactor core validation |
| 2026-02-10 | 1.3 | v3.0.0 IMPLEMENTED. Updated roadmap, aux systems table, v1.3.0 status. |
| 2026-02-11 | 1.4 | v4.1.0 IMPLEMENTED. |
| 2026-02-11 | 1.5 | Added v4.3.0, future SG pressure management. |
| 2026-02-11 | 1.6 | v4.2.2 IMPLEMENTED. |
| 2026-02-11 | 1.7 | Added v4.5.0 and v4.6.0 from v4.4.0 deficit analysis. |
| 2026-02-11 | 1.8 | v4.5.0 merged into v4.4.0 Stage 4. |
| 2026-02-11 | 2.0 | Major cleanup: v4.4.0 COMPLETE. Removed all completed features from pending. Reorganized remaining work into single priority-ordered list. |
| 2026-02-11 | 2.1 | **Added 4 new features:** Dashboard Redesign (P1), Scenario Selector (P2), Temperature Hold ~200°F (P3), In-Game Help F1 (P4). Renumbered all subsequent priorities. Confirmed 200°F hold from NRC HRTD 19.0. |
