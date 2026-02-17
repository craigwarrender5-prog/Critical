# CS-0109 Investigation Report (2026-02-17_190515)

- Issue ID: `CS-0109`
- Title: `PZR Mode 5 pre-heater pressurization: validate net +1 gpm charging imbalance vs documented expectations`
- Initial Status at Creation: `INVESTIGATING`
- Investigation Completed: `2026-02-17T19:05:15Z`
- Recommended Next Status: `READY`
- Assigned Domain Plan: `DP-0012 - Pressurizer & Startup Control`
- Assigned IP: `None (diagnostic-to-resolution CS; corrective scope only)`

## Governance / Scope

This CS is **diagnostic-to-resolution**. No implementation is authorized under this CS. If corrective action is required, create a dedicated IP before code changes.

## 1) Context

Mode 5 startup pre-heater pressurization is currently observed with approximately `+1 gpm` net charging imbalance while the pressurizer remains water-solid. This CS validates whether:

1. The imbalance magnitude,
2. The mechanism (controller-driven vs fixed bias), and
3. The resulting pressure ramp behavior

match documented expectations in `Technical_Documentation/`.

## 2) Technical Documentation Review

### Documented Mode 5 / solid-plant expectations

1. `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:12-16`
   - Mode 5 cold shutdown initial state includes solid pressurizer (no steam space).
2. `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:21`
   - Startup sequence: pressure increased by charging > letdown to 400-425 psig; then RCPs started and heaters energized.
3. `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:25`
   - Explicit guidance: net `+20-40 gpm` yielding approximately `50-100 psi/hr` during initial pressurization.
4. `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:43-47`
   - In solid configuration, pressure is controlled by charging/letdown imbalance.

### Additional CVCS/pressure-control references

1. `Technical_Documentation/NRC_HRTD_Section_4.1_Chemical_Volume_Control_System.md:602-610`
   - Letdown through RHR/CVCS path during cold shutdown.
2. `Technical_Documentation/NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md:45-47`
   - Heater function and pressure-control architecture context.

## 3) Existing Log Analysis (`Build/HeatupLogs`)

### Mode 5 pre-heater window examined

- `Build/HeatupLogs/Heatup_Interval_001_0.00hr.txt`
- `Build/HeatupLogs/Heatup_Interval_002_0.25hr.txt`
- `Build/HeatupLogs/Heatup_Interval_004_0.50hr.txt`
- `Build/HeatupLogs/Heatup_Interval_006_0.75hr.txt`
- `Build/HeatupLogs/Heatup_Interval_010_1.25hr.txt`
- `Build/HeatupLogs/Heatup_Interval_012_1.50hr.txt`
- `Build/HeatupLogs/Heatup_Interval_014_1.75hr.txt`
- `Build/HeatupLogs/Heatup_Interval_020_2.50hr.txt`
- `Build/HeatupLogs/Heatup_Interval_098_12.25hr.txt`

### Flow evidence

1. Initial balanced condition:
   - `Build/HeatupLogs/Heatup_Interval_001_0.00hr.txt:38,41,47`
   - Charging `75.0 gpm`, letdown `75.0 gpm`, net `0.0 gpm`.
2. Early pressurization:
   - `Build/HeatupLogs/Heatup_Interval_002_0.25hr.txt:38,41,47`
   - Charging `75.0`, letdown `74.0`, net `+1.0 gpm`.
3. Similar pattern persists through early rise:
   - `Build/HeatupLogs/Heatup_Interval_004_0.50hr.txt:47` (`+1.0 gpm`)
   - `Build/HeatupLogs/Heatup_Interval_010_1.25hr.txt:47` (`+1.0 gpm`)
   - `Build/HeatupLogs/Heatup_Interval_012_1.50hr.txt:47` (`+1.1 gpm`)
4. Net flow does not remain fixed; sign reversals occur:
   - `Build/HeatupLogs/Heatup_Interval_014_1.75hr.txt:47` (`-1.6 gpm`)
   - `Build/HeatupLogs/Heatup_Interval_020_2.50hr.txt:47` (`+0.3 gpm`).

### Pressure ramp evidence (derived from logged pressure-rate and pressure)

1. Logged rates in early rise are around `176-184 psi/hr`:
   - `Build/HeatupLogs/Heatup_Interval_002_0.25hr.txt:27`
   - `Build/HeatupLogs/Heatup_Interval_004_0.50hr.txt:27`
   - `Build/HeatupLogs/Heatup_Interval_010_1.25hr.txt:27`
   - `Build/HeatupLogs/Heatup_Interval_012_1.50hr.txt:27`
2. Equivalent slope is about `2.94-3.07 psi/min`.
3. Time-to-target bands from logs:
   - `>= 320 psig`: `Build/HeatupLogs/Heatup_Interval_010_1.25hr.txt:30`
   - `>= 350 psig`: `Build/HeatupLogs/Heatup_Interval_012_1.50hr.txt:30`
   - `>= 400 psig`: `Build/HeatupLogs/Heatup_Interval_098_12.25hr.txt:30`.

### Water-solid state confirmation

- `Build/HeatupLogs/Heatup_Interval_001_0.00hr.txt:78`
- `Build/HeatupLogs/Heatup_Interval_002_0.25hr.txt:78`
- `Build/HeatupLogs/Heatup_Interval_020_2.50hr.txt:78`

All show `Solid Pressurizer: YES` in analyzed window.

## 4) Code Trace

### Charging/letdown command origin

1. Engine sets equal solid-base flows (`75/75`) before controller trim:
   - `Assets/Scripts/Validation/HeatupSimEngine.cs:1561-1567`
2. Solid-plant outputs mapped back to displayed charging/letdown:
   - `Assets/Scripts/Validation/HeatupSimEngine.cs:1594-1595`

### +1 gpm mechanism determination

1. Not a fixed hard-coded net bias in log space; base starts balanced (`75/75`).
2. Controller trim authority in `HEATER_PRESSURIZE` is explicitly limited to +/-`1.0 gpm`:
   - `Assets/Scripts/Physics/SolidPlantPressure.cs:232-238`
   - `Assets/Scripts/Physics/SolidPlantPressure.cs:628-631`
3. Letdown is adjusted while charging remains base value:
   - `Assets/Scripts/Physics/SolidPlantPressure.cs:683-685`

Conclusion on mechanism: `~+1 gpm` is **controller-driven emergent behavior under a hard trim cap**, not a standalone fixed constant offset in the engine startup configuration.

### Water-solid compliance model

1. Pressure update uses compressibility relation `dP = dV_net / (V_total * kappa)`:
   - `Assets/Scripts/Physics/SolidPlantPressure.cs:717-723`
2. Compressibility obtained from thermophysical model:
   - `Assets/Scripts/Physics/SolidPlantPressure.cs:722`
   - `Assets/Scripts/Physics/SolidPlantPressure.cs:773`

## 5) Disposition

**Disposition: INCONSISTENT with Technical_Documentation**

Primary mismatches:

1. Documented pre-heater mechanism magnitude (`+20-40 gpm`, `~50-100 psi/hr`) is not matched by observed `~+1 gpm` envelope and `~176-184 psi/hr` rise (`Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:25` vs logs above).
2. Heaters are already active early in the solid pre-heater period (`Build/HeatupLogs/Heatup_Interval_002_0.25hr.txt:68`), while documentation sequence states pressurize first, then energize heaters (`Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:21,31-34`).

## 6) Root Cause and Corrective Scope (No Implementation)

### Root cause

1. Solid-plant flow trim authority is intentionally capped to +/-`1 gpm` in `HEATER_PRESSURIZE`, preventing documented larger flow-imbalance behavior.
2. Heater control participates during early solid pressurization, so observed ramp is not purely CVCS-imbalance-driven.

### Required corrected behavior

1. Mode 5 pre-heater pressurization should follow documented mechanism and pace envelope for this stage:
   - CVCS imbalance-dominant rise consistent with documented startup references, and
   - heater enable sequencing aligned to documented startup transition point.

### Affected modules/files

- `Assets/Scripts/Physics/SolidPlantPressure.cs`
- `Assets/Scripts/Validation/HeatupSimEngine.cs`
- `Assets/Scripts/Physics/CVCSController.cs` (heater participation interactions)

### Proposed remediation direction (for subsequent IP)

1. Add explicit Mode 5 pre-heater pressurization policy boundary:
   - parameterized target imbalance/rate envelope aligned with documentation,
   - clear handoff condition to heater-led stage.
2. Separate CVCS pre-heater control objective from heater pressure-rate limiter objective to avoid mixed mechanisms in the pre-heater stage.
3. If runtime behavior is intentionally different from references, update technical documentation with explicit rationale and expected quantitative envelope.

## 7) Recommended Next Step

Keep `CS-0109` open as `READY` and create an IP to implement the corrective scope above.
