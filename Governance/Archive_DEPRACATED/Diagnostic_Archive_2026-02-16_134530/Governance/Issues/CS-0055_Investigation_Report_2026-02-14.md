# CS-0055 Preliminary Investigation Report

- Issue ID: `CS-0055`
- Date: `2026-02-14`
- Investigation State: `Preliminary`
- Assigned Domain Plan: `DP-0001`
- Severity: `CRITICAL`
- Remediation Authorization: `NO REMEDIATION AUTHORIZED.`

## Observation
RCS bulk temperature rises during PZR heater operation while no RCPs are running.

## Expected Behavior
If PZR heating is truly isolated and no valid transport path exists, RCS bulk temperature should not rise.

## Repro Conditions and Evidence
- `HeatupLogs/Heatup_Interval_005_1.00hr.txt:26` shows `RCS Heat Rate: 3.10 F/hr`
- `HeatupLogs/Heatup_Interval_005_1.00hr.txt:76` shows `RCPs Running: 0 / 4`
- `HeatupLogs/Heatup_Interval_005_1.00hr.txt:125` shows `PZR Heaters: 1.80 MW`
- `HeatupLogs/Heatup_Interval_005_1.00hr.txt:129` shows `Net Plant Heat: 2.68 MW`
- `HeatupLogs/Heatup_Interval_007_1.50hr.txt:26` shows `RCS Heat Rate: 3.08 F/hr` with `RCPs Running: 0 / 4`
- `HeatupLogs/Heatup_Interval_012_2.75hr.txt:26` shows `RCS Heat Rate: 3.05 F/hr` with `RCPs Running: 0 / 4`

## Heat-Path Trace
1. PZR to RCS natural-convection path exists:
   - `Assets/Scripts/Physics/RCSHeatup.cs:320` computes `SurgeLineHeatTransfer_MW(T_pzr, T_rcs, pressure)`
   - `Assets/Scripts/Physics/RCSHeatup.cs:387` applies conduction/loss result to `T_rcs`
2. RHR thermal path exists in no-RCP regime:
   - `Assets/Scripts/Validation/HeatupSimEngine.cs:1125` updates RHR every step
   - `Assets/Scripts/Validation/HeatupSimEngine.cs:1310` applies `rhrNetHeat_MW` directly to `T_rcs` in Regime 1
   - `Assets/Scripts/Validation/HeatupSimEngine.cs:1752` includes `rhrNetHeat_MW` in `netPlantHeat_MW`
3. RHR starts active in cold-start conditions:
   - `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:153` initializes `RHRSystem.Initialize(...)`
   - `Assets/Scripts/Physics/RHRSystem.cs:274` initializes RHR in `Heatup` mode with pumps online

## Mass/Flow Path Trace
1. RHR hydraulic coupling is enabled when suction valves are open and flow is non-zero:
   - `Assets/Scripts/Physics/RHRSystem.cs:523`
2. Cold-start letdown path is explicitly via RHR crossconnect:
   - `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:366`
   - `Assets/Scripts/Physics/CVCSController.cs:506`
   - `Assets/Scripts/Physics/CVCSController.cs:510`

## Control/Visibility Couplings
1. RHR isolation starts only after RCP start:
   - `Assets/Scripts/Validation/HeatupSimEngine.cs:1135`
2. Heat source presentation can mask the RHR contribution:
   - `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1124` reports `Gross Heat Input` as RCP + PZR heater
   - `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1128` reports `Net Plant Heat` including RHR net heat

## Quantified Finding
Using logged values from intervals `005-012`, implied RHR net heat contribution is approximately `0.96-0.99 MW` while `RCPs Running = 0/4`, consistent with observed RCS heat-rate rise.

## Refined Failure Mode
Primary Failure Mode:
RHR net heat (and surge-line transfer) is applied directly to the bulk RCS temperature state (`T_rcs`) as if the entire RCS inventory is fully mixed, even when:

- No RCPs are running
- Only natural convection exists
- Effective circulation mass should be limited
- Ambient/plant losses compete with localized heating

This results in the entire RCS mass responding thermally to heat inputs that, in a no-RCP regime, would realistically affect only a limited coupled volume over the timestep.

## Physical Model Concern
- Heat application scope does not reflect effective mixed mass.
- No-RCP regime lacks a transport gating modifier.
- Ambient/loss modeling does not prevent unrealistic bulk rise.
- Thermal inertia of full RCS mass is being bypassed by lumped-state update.

## Preliminary Findings and Hypotheses
- Most probable: RCS warming is primarily driven by active RHR pump heat path, not by RCPs.
- Secondary contribution: PZR-to-RCS surge-line natural convection contributes additional positive heat.
- RHR running itself is not inherently a defect (it is a normal decay-heat-removal condition); the defect is the bulk thermal state update model under limited-circulation conditions.

## Risk Classification
- Classification: `CRITICAL`
- Rationale: this can invalidate operator interpretation and governance decisions for isolated-heating scenarios by hiding active heat-path assumptions.

## Domain Assignment Rationale
Reassigned to `DP-0001` (`Primary Thermodynamics`) because the refined primary failure mode is thermal-hydraulic state application in the no-RCP regime (effective mixed-mass and transport gating), not primarily an operator-interface or scenario-labeling concern.

## Additional Defects
No additional distinct defect IDs opened from this preliminary pass.

## Authorization
NO REMEDIATION AUTHORIZED.
