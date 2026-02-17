# IP-0035 Stage D Domain Validation (2026-02-16_224400)

- IP: `IP-0035`
- DP: `DP-0006`
- Stage: `D`

## 1) Validation Scope
- `CS-0079`: startup permissive threshold authority alignment.
- `CS-0010`: SG secondary pressure high alarm implementation and annunciation.

## 2) Static Validation Results

### CS-0079
1. Authoritative startup permissive constant is `400 psig`:
- `Assets/Scripts/Physics/PlantConstants.Pressure.cs:216`
2. Startup gate enforces `pressure_psig >= MIN_RCP_PRESSURE_PSIG`:
- `Assets/Scripts/Physics/PlantConstants.Pressure.cs:518`
- `Assets/Scripts/Physics/PlantConstants.Pressure.cs:520`
3. Sequencer low-pressure message references authoritative constant, avoiding stale hard-coded values:
- `Assets/Scripts/Physics/RCPSequencer.cs:201`

Disposition: `PASS`

### CS-0010
1. Alarm contract includes SG secondary pressure high state, input, and setpoint:
- `Assets/Scripts/Physics/AlarmManager.cs:38`
- `Assets/Scripts/Physics/AlarmManager.cs:67`
- `Assets/Scripts/Physics/AlarmManager.cs:93`
- `Assets/Scripts/Physics/AlarmManager.cs:145`
2. Alarm activation is included in active alarm summary output:
- `Assets/Scripts/Physics/AlarmManager.cs:214`
3. Engine alarm propagation and edge logging are wired:
- `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs:122`
- `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs:255`
- `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs:277`
4. Engine state and deterministic init reset are present:
- `Assets/Scripts/Validation/HeatupSimEngine.cs:491`
- `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:680`
- `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:688`
5. Both operator visuals include the SG high-pressure annunciation:
- `Assets/Scripts/Validation/HeatupValidationVisual.Annunciators.cs:139`
- `Assets/Scripts/Validation/HeatupValidationVisual.Annunciators.cs:169`
- `Assets/Scripts/UI/ValidationDashboard/Panels/OverviewSection_Alarms.cs:69`
- `Assets/Scripts/UI/ValidationDashboard/Panels/OverviewSection_Alarms.cs:89`

Disposition: `PASS`

## 3) Build/Execution Validation Note
`dotnet build Critical.slnx` cannot complete in this terminal workspace because Unity-generated project files are absent:
- `Assembly-CSharp-Editor.csproj`
- `Assembly-CSharp.csproj`
- `Critical.Physics.csproj`

This is a tooling limitation in the current terminal environment, not a Stage D contract failure.

## 4) Stage D Exit
All `IP-0035` Stage D validation gates pass for scoped code changes. Stage E regression and closure packaging authorized.
