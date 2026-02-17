# CS-0110 Investigation Report (2026-02-17_190515)

- Issue ID: `CS-0110`
- Title: `PZR heater startup behavior after pressurization: validate ramp and control vs documented expectations`
- Initial Status at Creation: `INVESTIGATING`
- Investigation Completed: `2026-02-17T19:05:15Z`
- Recommended Next Status: `CLOSED`
- Assigned Domain Plan: `DP-0012 - Pressurizer & Startup Control`
- Resolution Type Recommendation: `CLOSE_NO_CODE`

## Governance / Scope

This CS is **diagnostic-to-resolution**. No implementation is authorized under this CS. If inconsistency is found, a new IP is required before code changes.

## 1) Context

Claim under review: after pressurization, PZR heaters do not ramp to expected/full power.

This CS validates heater permissives, mode/state gating, demand-to-applied power behavior, and startup ramp profile against technical documentation and runtime logs.

## 2) Technical Documentation Review

### Heater startup/control references

1. `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:21,31-34`
   - Startup sequencing includes heater energization after reaching 400-425 psig band.
2. `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:95-97`
   - Automatic heater/spray PID control placed in service at normal operating pressure (`2235 psig`), not during low-pressure heatup.
3. `Technical_Documentation/NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md:65-76`
   - Heater/spray control philosophy (modulation and sequence around setpoint).
4. `Technical_Documentation/NRC_HRTD_Section_10.3_Pressurizer_Level_Control.md:164-170,183-184`
   - Low-level heater cutoff interlock at 17% level.

## 3) Existing Log Analysis (`Build/HeatupLogs`)

### Transition window evaluated

- `Build/HeatupLogs/Heatup_Interval_094_11.75hr.txt`
- `Build/HeatupLogs/Heatup_Interval_096_12.00hr.txt`
- `Build/HeatupLogs/Heatup_Interval_098_12.25hr.txt`
- `Build/HeatupLogs/Heatup_Interval_100_12.50hr.txt`
- `Build/HeatupLogs/Heatup_Interval_102_12.75hr.txt`
- `Build/HeatupLogs/Heatup_Interval_104_13.00hr.txt`
- `Build/HeatupLogs/Heatup_Interval_106_13.25hr.txt`
- `Build/HeatupLogs/Heatup_Interval_110_13.75hr.txt`

### Heater demand vs applied behavior

1. During bubble-formation auto phase, heater runs full with no limiter:
   - `Build/HeatupLogs/Heatup_Interval_096_12.00hr.txt:52,55,56,68`
   - `Build/HeatupLogs/Heatup_Interval_098_12.25hr.txt:52,55,56,68`
   - `Build/HeatupLogs/Heatup_Interval_100_12.50hr.txt:52,55,56,68`
2. Limiter-driven modulation is visible where pressure-rate clamp applies:
   - `Build/HeatupLogs/Heatup_Interval_104_13.00hr.txt:55-56,68`
   - `Build/HeatupLogs/Heatup_Interval_106_13.25hr.txt:55-56,68`
3. Heaters return to full power later in pressurize-auto when limiter clears:
   - `Build/HeatupLogs/Heatup_Interval_110_13.75hr.txt:55,68`

### Permissive/inhibit indicators in logs

1. Hold/permissive instrumentation available and transitions observed:
   - `Build/HeatupLogs/Heatup_Interval_001_0.00hr.txt:53,55,58` (hold locked)
   - `Build/HeatupLogs/Heatup_Interval_002_0.25hr.txt:53,58` (auto, hold released)
2. Bubble-phase context during full-power ramp:
   - `Build/HeatupLogs/Heatup_Interval_098_12.25hr.txt:79-80`

### Exact "ramp-fail" window

No "expected ramp fails to occur" window was found in analyzed logs.
Observed behavior shows full-power heater operation in the expected transition region and clamp-driven modulation where pressure-rate limits are active.

## 4) Code Trace

### Heater demand and clamping logic

1. Startup/bubble and pressurize auto modes share pressure-rate-limited demand with minimum floor:
   - `Assets/Scripts/Physics/CVCSController.cs:652-700` (`BUBBLE_FORMATION_AUTO`)
   - `Assets/Scripts/Physics/CVCSController.cs:707-751` (`PRESSURIZE_AUTO`)
2. Startup pressure-rate / min-floor constants:
   - `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs:251` (`HEATER_STARTUP_MAX_PRESSURE_RATE = 100`)
   - `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs:259` (`HEATER_STARTUP_MIN_POWER_FRACTION = 0.2`)

### Mode/state gating

1. Bubble-phase forces heater mode to bubble auto, then returns to pressurize auto on phase transition:
   - `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:365-366`
   - `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:263-264`
2. AUTO PID transition gate is high-pressure (`2200 psia`) and therefore not expected in low-pressure startup window:
   - `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs:347`
   - `Assets/Scripts/Validation/HeatupSimEngine.cs:1285-1289`
3. Hold and interlock authority paths are explicitly handled:
   - `Assets/Scripts/Validation/HeatupSimEngine.cs:1308-1337`
   - `Assets/Scripts/Validation/HeatupSimEngine.cs:1362-1369`

## 5) Disposition

**Disposition: CONSISTENT with Technical_Documentation**

Evidence supports that:

1. Heater ramp does occur after pressurization transition (full-power in bubble auto phase).
2. Reduced heater power windows are explained by documented/implemented pressure-rate limiting and mode logic, not missing heater authority.
3. PID not engaging in this window is expected because pressure is far below the documented/implemented automatic PID entry band.

## 6) Resolution

No corrective implementation scope is required from this CS.

Recommended closure metadata:

- Status: `CLOSED`
- Resolution type: `CLOSE_NO_CODE`
- Rationale: behavior matches current documentation + implementation architecture in analyzed startup window.

## 7) Residual Note

If desired, documentation can be clarified to explicitly distinguish:

1. full-power segments during bubble-formation auto,
2. pressure-rate-limited modulation during pressurize auto,
3. high-pressure PID engagement threshold behavior.

This is optional documentation hardening, not a defect fix.
