# IP-0028 Stage A - Documentation and Runtime Baseline Freeze

- IP: `IP-0028`
- DP: `DP-0012`
- Stage: `A`
- Timestamp: `2026-02-16_124043`
- Scope: `CS-0040, CS-0081, CS-0091, CS-0093, CS-0094, CS-0096` (baseline-only freeze)

## 1) Authority Sources Reviewed (Technical_Documentation)

1. `Technical_Documentation/PZR_Baseline_Profile.md`
2. `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md`
3. `Technical_Documentation/NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md`
4. `Technical_Documentation/Westinghouse_4Loop_Pressurizer_Specifications_Summary.md`
5. `Technical_Documentation/PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md`

## 2) Runtime Baseline Capture (Reproducible)

### 2.1 Run method

- Unity execute method: `Critical.Validation.PzrBubbleInvestigationRunner.RunAll`
- Scene: `Assets/Scenes/MainScene.unity`
- Deterministic baseline config (runner `BASELINE` profile):
  - `dt_hr = 1/360 = 0.00277778 hr`
  - `maxSimHr = 12 hr`
  - `intervalLogHr = 0.25 hr`
  - Cold-shutdown initialization from `HeatupSimEngine.Init.cs` `ColdShutdownProfile.CreateApprovedBaseline()`

### 2.2 Run stamps and evidence paths

1. Run 1:
   - Unity log: `HeatupLogs/IP0028_StageA_Run1_Unity.log`
   - Output root: `HeatupLogs/PZR_INVEST_20260216_123659`
   - Suite summary: `HeatupLogs/PZR_INVEST_20260216_123659/PZR_Bubble_Investigation_Suite_20260216_123659.md`
2. Run 2:
   - Unity log: `HeatupLogs/IP0028_StageA_Run2_Unity.log`
   - Output root: `HeatupLogs/PZR_INVEST_20260216_123815`
   - Suite summary: `HeatupLogs/PZR_INVEST_20260216_123815/PZR_Bubble_Investigation_Suite_20260216_123815.md`

### 2.3 Startup + hold transition telemetry snapshot (baseline profile)

Evidence files used:
- `HeatupLogs/PZR_INVEST_20260216_123659/BASELINE_20260216_123659/Heatup_Interval_002_0.25hr.txt`
- `HeatupLogs/PZR_INVEST_20260216_123659/BASELINE_20260216_123659/Heatup_Interval_007_1.50hr.txt`
- `HeatupLogs/PZR_INVEST_20260216_123659/BASELINE_20260216_123659/Heatup_Interval_008_1.75hr.txt`
- `HeatupLogs/PZR_INVEST_20260216_123659/BASELINE_20260216_123659/Heatup_Interval_009_2.00hr.txt`

Observed values:

| Sim time | Control mode | Solid P in band | Heater power | Spray status |
|---|---|---|---|---|
| 0.25 hr | `HEATER_PRESSURIZE` | `NO` | `0.422 MW` | `Spray disabled (no RCPs)` |
| 1.50 hr | `HOLD_SOLID` | `YES` | `0.359 MW` | `Spray disabled (no RCPs)` |
| 1.75 hr | `HOLD_SOLID` | `YES` | `0.598 MW` | `Spray disabled (no RCPs)` |
| 2.00 hr | `HOLD_SOLID` | `YES` | `0.686 MW` | `Spray disabled (no RCPs)` |

Reproducibility check (Run 1 vs Run 2 baseline intervals 1.50/1.75/2.00 hr):
- Identical control mode, in-band flag, heater power, and spray status at those checkpoints.

## 3) Mandatory Traceability Table (Thresholds / Bands / Authority / Spray / Convergence / Targets)

| Governed item | Baseline value/rule | Units | Technical_Documentation source | Section reference | Runtime implementation reference | Baseline comparison |
|---|---|---|---|---|---|---|
| Solid-plant operating control band | `320-400` | psig | `Technical_Documentation/PZR_Baseline_Profile.md` | `DOC-CF-03` | `Assets/Scripts/Physics/PlantConstants.Pressure.cs` (`SOLID_PLANT_P_LOW_PSIG`, `SOLID_PLANT_P_HIGH_PSIG`) | matches documentation baseline |
| Startup transition target before/at RCP permissive | `400-425` | psig | `Technical_Documentation/PZR_Baseline_Profile.md`; `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md` | `DOC-CF-03`; `Section 1` | Runtime steady target in solid hold is `350 psig` (`SOLID_PLANT_P_SETPOINT_PSIA = 365`), with RHR relief up to `450 psig` | deviation exists: startup target held around 350 psig, not explicit 400-425 psig target window |
| RCP startup permissive threshold | `>=400` | psig | `Technical_Documentation/PZR_Baseline_Profile.md`; `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md` | `DOC-CF-02`; `Sections 1 and 3` | `Assets/Scripts/Physics/PlantConstants.Pressure.cs` (`MIN_RCP_PRESSURE_PSIG = 400`) | matches documentation baseline |
| Normal pressure setpoint | `2235` | psig | `Technical_Documentation/NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md`; `Technical_Documentation/PZR_Baseline_Profile.md` | `10.2.1 Key Parameters`; `PZR_Baseline_Profile table` | `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs` (`PZR_BASELINE_PRESSURE_SETPOINT_PSIG`) | matches documentation baseline |
| Proportional heater full-on / zero-output | `2220` / `2250` | psig | `Technical_Documentation/NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md`; `Technical_Documentation/Westinghouse_4Loop_Pressurizer_Specifications_Summary.md` | `10.2.5.5`; `Pressure Control Setpoints` | `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs` (`PZR_BASELINE_PROP_HEATER_FULL_ON_PSIG`, `PZR_BASELINE_PROP_HEATER_ZERO_PSIG`) | matches documentation baseline |
| Backup heater ON / OFF | `2210` / `2217` | psig | `Technical_Documentation/NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md`; `Technical_Documentation/Westinghouse_4Loop_Pressurizer_Specifications_Summary.md` | `10.2.5.5`; `Pressure Control Setpoints` | `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs` (`PZR_BASELINE_BACKUP_HEATER_ON_PSIG`, `PZR_BASELINE_BACKUP_HEATER_OFF_PSIG`) | matches documentation baseline |
| Spray actuation band | Start `2260`, full `2310` | psig | `Technical_Documentation/NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md`; `Technical_Documentation/Westinghouse_4Loop_Pressurizer_Specifications_Summary.md` | `10.2.5.4`; `Pressure Control Setpoints` | `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs` (`PZR_BASELINE_SPRAY_START_PSIG`, `PZR_BASELINE_SPRAY_FULL_PSIG`) | matches documentation baseline |
| Spray maximum flow | `840` | gpm | `Technical_Documentation/PZR_Baseline_Profile.md`; `Technical_Documentation/Westinghouse_4Loop_Pressurizer_Specifications_Summary.md` | `DOC-CF-04`; `Spray System` | `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs` (`PZR_BASELINE_SPRAY_MAX_GPM`) | matches documentation baseline |
| Spray availability with no RCPs | Not explicitly specified in reviewed docs as a hard interlock | n/a | `Technical_Documentation/NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md`; `Technical_Documentation/Westinghouse_4Loop_Pressurizer_Specifications_Summary.md` | `10.2.2`, `10.2.5.4`; `Spray System` | `Assets/Scripts/Physics/CVCSController.cs` (`UpdateSpray`: disables spray when `rcpCount <= 0`) | deviation exists: runtime imposes explicit no-RCP spray inhibit not explicitly frozen in reviewed doc set |
| Startup heater pressure-rate limiter | `100` max, `20%` min power floor | psi/hr, fraction | `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md`; `Technical_Documentation/NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md` | `Section 1` startup rate note; `10.2` qualitative PID behavior | `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs` (`HEATER_STARTUP_MAX_PRESSURE_RATE`, `HEATER_STARTUP_MIN_POWER_FRACTION`) | deviation exists: exact 100 psi/hr clamp + 20% floor are implementation-specific, not numerically frozen in docs |
| Heater mode transition to PID | Transition at `2200 psia` (~2185 psig) | psia | `Technical_Documentation/NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md`; `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md` | `10.2.5`; `Section 4` | `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs` (`HEATER_MODE_TRANSITION_PRESSURE_PSIA`) | deviation exists: transition threshold is implementation freeze, not explicit numeric in docs |
| Cold-start startup hold | Hold enabled, duration `15 sec` | sec | No explicit startup hold timer found in reviewed documentation set | n/a | `Assets/Scripts/Validation/HeatupSimEngine.Init.cs` (`ColdShutdownProfile.StartupHoldDuration_sec = 15f`) | deviation exists: startup hold duration/logic is simulator governance behavior, not documented plant requirement |
| Convergence threshold for two-phase closure volume residual | `1.0` | ft^3 | `Technical_Documentation/PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md` (identifies residual/masking concern but no numeric closure tolerance) | `Deficit discussion` | `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs` (`TWO_PHASE_CLOSURE_VOLUME_TOLERANCE_FT3`) | deviation exists: numeric tolerance is code-defined, not frozen in Technical_Documentation |
| Convergence threshold for mass contract residual | `0.1` | lbm | `Technical_Documentation/PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md` (qualitative concern only) | `Deficit discussion` | `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs` (`TWO_PHASE_CLOSURE_MASS_CONTRACT_TOL_LBM`) | deviation exists: numeric threshold code-defined, not documented |
| Convergence threshold for closure iterations | `48` max iterations | count | No explicit numeric convergence iteration cap found in reviewed documentation set | n/a | `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs` (`TWO_PHASE_CLOSURE_MAX_ITERATIONS`) | deviation exists: implementation-defined threshold |

## 4) Stage A Findings (for Stage B freeze input)

1. Runtime reproducibly shows `HOLD_SOLID` with `Solid P In Band = YES` while heaters remain sub-max (`0.359-0.686 MW`) in the 1.50/1.75/2.00 hr windows.
2. Runtime also reproducibly reports spray inhibited with no RCPs during those windows.
3. Startup control targeting around ~350 psig is active in solid hold behavior, while baseline documentation includes a startup transition target of 400-425 psig before/at RCP permissive.
4. Several control/convergence gates are implementation-defined numerically and not yet frozen in Technical_Documentation; these require explicit Stage B freeze decisions and explicit deviation acceptance or remediation.

## 5) Exit Criteria (Stage A)

- Traceability table complete: **PASS**
- Runtime baseline runs reproducible with stored artifacts: **PASS**
- Every in-scope threshold/band/authority/spray/convergence/startup-target item has documentation trace or explicit deviation statement: **PASS**

