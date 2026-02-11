# CRITICAL: Master the Atom — Changelog

## [0.4.1] — 2026-02-07

### Overview
Patch fix for compile error CS0200 introduced in v0.4.0 Issue #3 (Regime 2 blended
physics). A single line attempted to assign to a read-only computed property on the
`SystemState` struct. No physics, behaviour, or output changes — the removed
assignment was redundant.

**Version type:** Patch (compile error fix, no behaviour change)

---

### Fix — Remove Assignment to Read-Only Computed Property `SystemState.PZRLevel`

**Error:** `CS0200: Property or indexer 'SystemState.PZRLevel' cannot be assigned to
-- it is read only` at HeatupSimEngine.cs line 675.

**Root cause:** In v0.4.0 Issue #3, the Regime 2 blended physics block was added with
a state synchronisation section that updates `physicsState` fields after blending.
One line attempted to write directly to `SystemState.PZRLevel`:

```csharp
physicsState.PZRLevel = pzrLevel;   // CS0200 — PZRLevel is computed
```

However, `PZRLevel` is defined as a read-only computed property on `SystemState`
(in `CoupledThermo.cs`):

```csharp
public float PZRLevel => PZRWaterVolume / PlantConstants.PZR_TOTAL_VOLUME * 100f;
```

The assignment was redundant because `PZRWaterVolume` is set on the immediately
preceding line, and the computed property automatically yields the correct level
from it. Both expressions are algebraically identical:

- Assignment: `pzrLevel = pzrWaterVolume / PlantConstants.PZR_TOTAL_VOLUME * 100f`
- Property: `PZRLevel => PZRWaterVolume / PlantConstants.PZR_TOTAL_VOLUME * 100f`

**Fix:** Removed the single redundant assignment line. Added a comment documenting
why `PZRLevel` does not need explicit assignment.

**Impact:** Zero. The computed property already returned the correct value from the
`PZRWaterVolume` field set on the preceding line. No physics output, simulation
behaviour, or downstream consumer is affected.

---

### Files Modified

| File | Change | Size |
|------|--------|------|
| `HeatupSimEngine.cs` | Removed `physicsState.PZRLevel = pzrLevel;` in Regime 2 sync block (line 675) | ~35.2 KB (unchanged) |

**No files created or deleted.** Single-line removal in one file.

---

### GOLD Certification — HeatupSimEngine.cs

```
Module: HeatupSimEngine (partial class, 6 files)
Files: HeatupSimEngine.cs, .Init.cs, .BubbleFormation.cs, .CVCS.cs, .Alarms.cs, .Logging.cs
Date: 2026-02-07

[X] G1  — Single responsibility per file
[X] G2  — Header block complete
[X] G3  — No inline physics in engine
[X] G4  — Result/state structs for inter-module communication
[X] G5  — Constants from PlantConstants
[X] G6  — NRC/Westinghouse values cited
[X] G7  — Correct namespace
[~] G8  — 35.2 KB (target exceeded, hard limit respected — unchanged from v0.4.0)
[X] G9  — No dead code (removed redundant assignment)
[X] G10 — No duplication

Status: [~] GOLD (G8 advisory — pre-existing, unchanged from v0.4.0)
```

No re-certification needed for other modules — no other files modified.
