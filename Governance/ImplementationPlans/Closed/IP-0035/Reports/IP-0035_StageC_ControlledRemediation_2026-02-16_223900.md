# IP-0035 Stage C Controlled Remediation (2026-02-16_223900)

- IP: `IP-0035`
- DP: `DP-0006`
- Stage: `C`

## 1) Implemented Changes

### CS-0079: startup permissive alignment to 400 psig
1. Pressure-constant contract and startup gate documentation aligned to `400 psig`:
- `Assets/Scripts/Physics/PlantConstants.Pressure.cs:216`
- `Assets/Scripts/Physics/PlantConstants.Pressure.cs:518`
2. RCP sequencer low-pressure status message now references the authoritative constant:
- `Assets/Scripts/Physics/RCPSequencer.cs:201`
3. Startup permissive intent comments aligned in dependent startup/control paths:
- `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:67`
- `Assets/Scripts/Physics/CVCSController.cs:77`
- `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs:237`
- `Assets/Scripts/Physics/RHRSystem.cs:39`

### CS-0010: SG secondary pressure high alarm implementation
1. Added SG secondary pressure high alarm state, input, and threshold logic:
- `Assets/Scripts/Physics/AlarmManager.cs:38`
- `Assets/Scripts/Physics/AlarmManager.cs:67`
- `Assets/Scripts/Physics/AlarmManager.cs:93`
- `Assets/Scripts/Physics/AlarmManager.cs:145`
2. Added alarm summary token for active alarm list:
- `Assets/Scripts/Physics/AlarmManager.cs:214`
3. Added engine-level alarm state and alarm-edge logging path:
- `Assets/Scripts/Validation/HeatupSimEngine.cs:491`
- `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs:39`
- `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs:122`
- `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs:255`
- `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs:277`
4. Added deterministic initialization resets:
- `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:680`
- `Assets/Scripts/Validation/HeatupSimEngine.Init.cs:688`
5. Added SG alarm annunciation to both visual surfaces:
- `Assets/Scripts/Validation/HeatupValidationVisual.Annunciators.cs:59`
- `Assets/Scripts/Validation/HeatupValidationVisual.Annunciators.cs:139`
- `Assets/Scripts/Validation/HeatupValidationVisual.Annunciators.cs:169`
- `Assets/Scripts/UI/ValidationDashboard/Panels/OverviewSection_Alarms.cs:35`
- `Assets/Scripts/UI/ValidationDashboard/Panels/OverviewSection_Alarms.cs:69`
- `Assets/Scripts/UI/ValidationDashboard/Panels/OverviewSection_Alarms.cs:89`

## 2) Stage C Exit
Stage C remediation is complete for `CS-0079` and `CS-0010`. Stage D validation authorized.
