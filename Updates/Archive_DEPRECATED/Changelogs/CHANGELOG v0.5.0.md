# CRITICAL: Master the Atom — Changelog

## [0.5.0] — 2026-02-07

### Overview
Phase 3 legacy cleanup per REFACTORING_PLAN_v2.md. Removes all dead code identified
in the v2 audit: the obsolete `HeatupValidation` class (marked `[Obsolete]` since
v0.1.0, 19 KB of inline physics superseded by HeatupSimEngine) and the deprecated
`HeatTransfer.SurgeLineHTC()` method (replaced by the stratified natural convection
model in v0.1.0). Zero external references to either — confirmed by global codebase
grep across all 46 source files.

**Version type:** Minor (structural cleanup, no physics or behaviour changes)
**Refactoring plan:** `Updates and Changelog/REFACTORING_PLAN_v2.md` — Phase 3

---

### 3.1 — Delete HeatupValidation.cs (MANUAL ACTION REQUIRED)

**Status:** `[System.Obsolete]` since v0.1.0. Contains inline physics inconsistent
with GOLD standard modules. 19 KB of dead code. Zero external references.

**Audit results:**
- Global grep for `HeatupValidation` (class name, not `HeatupValidationVisual`)
  across all 46 `.cs` files found references ONLY in:
  - `HeatupValidation.cs` itself (self-referential)
  - `HeatupSimEngine.cs` lines 9, 45 — **comments** mentioning the companion
    `HeatupValidationVisual.cs`, not the obsolete class
- No `using` statements, no instantiation, no method calls, no type references
- `HeatupValidationVisual.cs` is the active GUI companion — **NOT** affected

**Action required:** Manually delete from Unity project:
```
Assets/Scripts/Validation/HeatupValidation.cs
Assets/Scripts/Validation/HeatupValidation.cs.meta
```

**Impact:** Zero. No code references the class. Unity will regenerate assembly
without it. No compile errors expected.

---

### 3.2 — Remove Deprecated HeatTransfer.SurgeLineHTC()

**Method removed:**
```csharp
[Obsolete("Use SurgeLineEffectiveUA() for surge line. Churchill-Chu overpredicts " +
           "stratified flow per NRC Bulletin 88-11.")]
public static float SurgeLineHTC(float T_hot_F, float T_cold_F, float pressure_psia)
```

**Background:** The original Churchill-Chu full-pipe natural convection correlation
was replaced in v0.1.0 by the stratified natural convection model (`SurgeLineEffectiveUA()`
and `SurgeLineHeatTransfer_BTU_hr()`). The old method was retained with `[Obsolete]`
for backward compatibility. The v2 audit confirmed zero callers across all source files.

**Audit results:**
- Global grep for `SurgeLineHTC` across all 46 `.cs` files: only the definition in
  `HeatTransfer.cs` line 564. Zero callers anywhere in the project.
- Replacement methods `SurgeLineEffectiveUA()` and `SurgeLineHeatTransfer_BTU_hr()`
  are the active API, used by `RCSHeatup.cs`, `SolidPlantPressure.cs`, and
  `HeatupSimEngine.cs`.

**What was removed (23 lines):**
- XML documentation block (6 lines)
- `[Obsolete]` attribute (2 lines)
- Method signature and body (15 lines)

**What was preserved:**
- `NusseltNaturalConvection()` — retained for SG natural circulation and containment
  heat transfer (documented as "NOT used by surge line model")
- `RayleighNumber()` — retained for same reason
- `GrashofNumber()` — retained for same reason
- `#region Legacy Natural Convection` / `#endregion` — region structure intact
- All 16 validation tests in `ValidateCalculations()` — unchanged, none tested
  `SurgeLineHTC()`

**Impact:** Zero. No callers existed. The `[Obsolete]` attribute was generating a
compile-time warning for any future accidental use — that warning pathway is now
removed along with the dead code.

---

### Files Modified

| File | Change | Size Before | Size After |
|------|--------|-------------|------------|
| `HeatTransfer.cs` | Removed `SurgeLineHTC()` (23 lines) | 29.1 KB | 28.0 KB |

### Files Deleted (MANUAL — Phase 3.1)

| File | Size | Reason |
|------|------|--------|
| `HeatupValidation.cs` | 19 KB | Dead code, `[Obsolete]` since v0.1.0 |
| `HeatupValidation.cs.meta` | 59 B | Unity metadata for deleted file |

---

### GOLD Certification — HeatTransfer.cs

```
Module: HeatTransfer
File:   Assets/Scripts/Physics/HeatTransfer.cs
Date:   2026-02-07

[X] G1  — Single responsibility (heat transfer calculations)
[X] G2  — Header block complete (v1.0.3.0 stratified model documented)
[X] G3  — N/A (static physics module, not an engine)
[X] G4  — Returns float values, no state mutation
[X] G5  — Constants from PlantConstants + module-private calibration constants
[X] G6  — NRC Bulletin 88-11, NRC IN 88-80, NUREG/CR-5757 cited
[X] G7  — namespace Critical.Physics
[X] G8  — 28.0 KB (well within 30 KB target)
[X] G9  — No dead code (removed last [Obsolete] method)
[X] G10 — No duplication

Status: GOLD ✅
```

No re-certification needed for other modules — no other files were modified.

---

### Verification Checklist

- [ ] Delete `HeatupValidation.cs` + `.meta` from Unity
- [ ] Compile in Unity — expect 0 errors, 0 new warnings
- [ ] Run Phase 1 test suite (156 tests) — all pass
- [ ] Run Phase 2 test suite (85 tests) — all pass
- [ ] Run Heatup Integration tests (9 tests) — all pass
- [ ] Confirm `HeatTransfer.ValidateCalculations()` passes (16 tests)
- [ ] Confirm no `[Obsolete]` attributes remain in codebase

---

### Refactoring Plan Progress

| Phase | Description | Status |
|-------|-------------|--------|
| 1 | HeatupSimEngine decomposition (6 partials) | ✅ v0.1.0 |
| 2 | PlantConstants consolidation (7 partials) | ✅ v0.3.0 |
| **3** | **Legacy cleanup (dead code removal)** | **✅ v0.5.0** |
| 4 | HeatupValidationVisual decomposition | Pending |
| 5 | Test infrastructure (TestBase) | Pending |
| 6 | Near-GOLD elevation (split borderline files) | Pending |
