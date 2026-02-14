# Update Summary: Phase D Validation Integration & Logging Changes
**Date:** 2026-02-06  
**Update ID:** UPDATE-001  
**Scope:** HeatupSimEngine, HeatupValidationVisual, Phase1TestRunner

---

## Changes Made

### 1. Log Output Directory — AppData → Project Root

**File:** `Assets/Scripts/Validation/HeatupSimEngine.cs` (line 294)

**Before:**
```csharp
logPath = Path.Combine(Application.persistentDataPath, "HeatupLogs");
```

**After:**
```csharp
logPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "HeatupLogs");
```

**Rationale:** `Application.persistentDataPath` resolves to `%APPDATA%/LocalLow/DefaultCompany/Critical/` which is inaccessible to external tools. The new path resolves to `<project_root>/HeatupLogs/` (e.g. `C:\Users\craig\Projects\Critical\HeatupLogs\`), making logs accessible for automated analysis, external review, and tool-assisted validation.

**Impact:** Existing logs in AppData are unaffected. New runs will write to the project root. The `HeatupLogs/` folder is outside `Assets/` so Unity will not attempt to import the log files.

---

### 2. Log Interval — 15 Minutes → 30 Minutes

**File:** `Assets/Scripts/Validation/HeatupSimEngine.cs` (line 567)

**Before:**
```csharp
// Save detailed log file every 15 sim-minutes
if (logTimer >= 0.25f)
```

**After:**
```csharp
// Save detailed log file every 30 sim-minutes
if (logTimer >= 0.5f)
```

**File:** `Assets/Scripts/Validation/HeatupValidationVisual.cs` (header comment)  
Updated comment from "15 sim-minutes" to "30 sim-minutes" for consistency.

**Rationale:** The heatup simulation runs for 10-20+ hours of sim time. At 15-minute intervals, a full run generates 40-80+ log files. At 30-minute intervals, this drops to 20-40 files while still capturing meaningful "over time" trends at each log snapshot. The 5-minute history buffer in the trend graphs continues to provide high-resolution visual data; the interval log files serve a different purpose — capturing detailed plant state snapshots for post-run analysis.

**Impact:** Fewer log files per run. Each log still contains the same comprehensive plant state data (temperatures, pressures, CVCS flows, VCT state, mass conservation, validation checks). No data fidelity loss for trend analysis since the 5-minute history buffer is unchanged.

---

### 3. Heatup Integration Tests Wired into Phase 1 Test Runner

**File:** `Assets/Scripts/Tests/Phase1TestRunner.cs`

**Changes:**
- Added `RunHeatupIntegrationTests()` method that calls `HeatupIntegrationTests.RunAllTests()`
- Wired into `RunAllTests()` sequence after `RunIntegrationTests()`
- Updated header comment: 112 → 121 tests (112 original + 9 heatup integration)

**Test Suite (9 tests, HINT-01 through HINT-09):**

| Test ID | Target Bug | What It Verifies |
|---------|-----------|-----------------|
| HINT-01 | Phase A (mass frozen) | RCS mass decreases when letdown > charging |
| HINT-02 | Phase A (mass frozen) | Mass change proportional to cumulative CVCS flow |
| HINT-03 | Phase B (surge zero) | Surge flow non-zero during solid plant PZR heating |
| HINT-04 | Phase B (surge zero) | Surge heat and surge flow direction consistency |
| HINT-05 | Phase C (tautological) | Conservation check detects deliberate mass violation |
| HINT-06 | Phase C (tautological) | Conservation error near zero with balanced flows |
| HINT-07 | Phase C (tautological) | CumulativeRCSChange tracks AccumulateRCSChange inputs |
| HINT-08 | All phases | Full cross-system mass balance over 200 steps |
| HINT-09 | Phase A (mass frozen) | RCS mass not identical after solid plant ops |

**Rationale:** These tests fill the integration gap identified during Phase D validation. All existing Phase 1 tests were unit-level (does Parameter X match Westinghouse spec Y?). The HINT tests verify cross-module coupling — that state changes in SolidPlantPressure actually propagate through HeatupSimEngine to VCTPhysics. This is the class of bug that unit tests structurally cannot catch.

**Test Runner Sequence (Phase1TestRunner.RunAllTests):**
1. PlantConstants (5 tests)
2. WaterProperties (18 tests)
3. SteamThermodynamics (10 tests)
4. ThermalMass (6 tests)
5. ThermalExpansion (6 tests)
6. HeatTransfer (6 tests)
7. FluidFlow (8 tests)
8. ReactorKinetics (10 tests)
9. PressurizerPhysics (24 tests)
10. CoupledThermo (12 tests)
11. Integration Tests (7 tests)
12. **Heatup Integration Tests (9 tests)** ← NEW

**Total: 121 tests**

---

## Files Modified

| File | Change |
|------|--------|
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | Log path, log interval |
| `Assets/Scripts/Validation/HeatupValidationVisual.cs` | Header comment |
| `Assets/Scripts/Tests/Phase1TestRunner.cs` | Added heatup integration test runner |

## Files Unchanged (GOLD Standard Maintained)

No physics module source code was modified. All changes are to infrastructure (logging path/interval) and test wiring. The following modules remain untouched:

- PlantConstants.cs, WaterProperties.cs, SteamThermodynamics.cs
- ThermalMass.cs, ThermalExpansion.cs, HeatTransfer.cs
- FluidFlow.cs, ReactorKinetics.cs, PressurizerPhysics.cs
- CoupledThermo.cs, VCTPhysics.cs, SolidPlantPressure.cs
- CVCSController.cs, RCSHeatup.cs

## Validation

All changes are non-destructive. The log path change and interval change affect runtime output only. The test wiring adds coverage without modifying any existing test or physics code. Run Phase1TestRunner to verify all 121 tests pass.
