# Inventory Audit Discrepancy Report

**Version:** 1.0
**Date:** 2026-02-13
**Scope:** Explain why INVENTORY AUDIT reports primary masses as 0 lbm while MASS INVENTORY shows correct non-zero values, and why Mass Conservation is marked FAIL.

---

## 1. Log Sections Involved

Two independent mass-reporting sections appear in each interval log:

| Log Section | Writer Location | Purpose |
|---|---|---|
| **MASS INVENTORY** | `Logging.cs:1160–1163` | Quick-look display of RCS and PZR water mass |
| **INVENTORY AUDIT** | `Logging.cs:1217–1243` | Comprehensive mass balance with conservation check |

They are written by the same `SaveIntervalLog()` method but draw from **different source variables**.

---

## 2. Source Variables — MASS INVENTORY

```
Logging.cs:1161  →  rcsWaterMass              (engine instance field)
Logging.cs:1163  →  pzrWaterVolume * density   (V×ρ recalc from engine fields)
```

These engine instance fields (`rcsWaterMass`, `pzrWaterVolume`) are updated every timestep:

- **Solid regime (Regime 1, solid branch):**
  `HeatupSimEngine.cs:1025–1026` — `physicsState.RCSWaterMass += massChange_lb; rcsWaterMass = physicsState.RCSWaterMass;`
  `pzrWaterVolume` stays at `PZR_TOTAL_VOLUME` (100% water-solid).

- **Post-bubble (Regime 1, isolated branch):**
  `HeatupSimEngine.cs:1395` — `rcsWaterMass = physicsState.RCSWaterMass;`

- **Regime 2/3:**
  Same sync at line 1395 after CoupledThermo solver updates `physicsState`.

**Result:** MASS INVENTORY always shows valid, non-zero masses.

---

## 3. Source Variables — INVENTORY AUDIT

The audit uses a regime-aware branch (`Logging.cs:303–321`):

```csharp
// Line 303
if (solidPressurizer && !bubbleFormed)
{
    // SOLID branch (lines 305–310)
    inventoryAudit.PZR_Water_Mass_lbm = physicsState.PZRWaterMassSolid;
    inventoryAudit.RCS_Mass_lbm       = physicsState.TotalPrimaryMassSolid
                                       - physicsState.PZRWaterMassSolid;
    inventoryAudit.PZR_Steam_Mass_lbm = 0f;
    inventoryAudit.AuditMassSource    = "CANONICAL_SOLID";
}
else
{
    // TWO-PHASE branch (lines 314–320)
    inventoryAudit.RCS_Mass_lbm       = physicsState.RCSWaterMass;
    inventoryAudit.PZR_Water_Mass_lbm = physicsState.PZRWaterMass;
    inventoryAudit.PZR_Steam_Mass_lbm = physicsState.PZRSteamMass;
    inventoryAudit.AuditMassSource    = "CANONICAL_TWO_PHASE";
}
```

---

## 4. Discrepancy Cause — Root Cause A: Solid Regime (0 lbm from start)

### The Orphaned Fields

The SOLID branch reads two `SystemState` fields:

| Field | Declared at | Comment says "maintained by" | Actually written by |
|---|---|---|---|
| `TotalPrimaryMassSolid` | `CoupledThermo.cs:760` | SolidPlantPressure via CVCS boundary flow | **Nobody** |
| `PZRWaterMassSolid` | `CoupledThermo.cs:761` | SolidPlantPressure via surge transfer | **Nobody** |

**Evidence:** A codebase-wide search for assignment patterns `TotalPrimaryMassSolid =` and `PZRWaterMassSolid =` returns zero results outside of comments and test descriptions.

These fields were introduced in the v5.0.2 architecture as canonical mass trackers for solid-ops, but the write-side implementation was never completed:
- `SolidPlantPressure.cs` maintains its own `SolidPlantState.PzrWaterMass` field, but this is never transferred to `physicsState.PZRWaterMassSolid`.
- No code computes or assigns `physicsState.TotalPrimaryMassSolid`.
- As C# `struct` fields, they default-initialize to `0f`.

### Consequence

During solid ops (`solidPressurizer=true`, `bubbleFormed=false`):
- `inventoryAudit.PZR_Water_Mass_lbm = 0`
- `inventoryAudit.RCS_Mass_lbm = 0 - 0 = 0`
- `inventoryAudit.Total_Mass_lbm = 0 + 0 + 0 + VCT + BRS ≈ VCT + BRS only`

The conservation check (line 394) compares this near-zero total against `Initial_Total_Mass_lbm` (which was ALSO captured as near-zero at init time via the same broken path). So during pure solid ops the error stays small (~0), because **both** initial and current are wrong by the same amount.

The conservation FAIL triggers later, when the branch switches (Root Cause B).

---

## 5. Discrepancy Cause — Root Cause B: Solid→Two-Phase Transition

### The Premature Branch Flip

When the physics module detects a bubble (`BubbleFormation.cs:107`):

```csharp
solidPressurizer = false;
// bubbleFormed stays FALSE until drain completes — gates RCP starts
```

This creates a transitional state where `solidPressurizer=false` AND `bubbleFormed=false`. The audit condition on line 303:

```csharp
if (solidPressurizer && !bubbleFormed)  // FALSE — solidPressurizer is now false
```

...falls through to the ELSE (two-phase) branch. The two-phase branch reads:
- `physicsState.RCSWaterMass` — valid (was maintained during solid ops at line 1025)
- `physicsState.PZRWaterMass` — set once at detection (`BubbleFormation.cs:122`)
- `physicsState.PZRSteamMass` — set to 0 at detection (`BubbleFormation.cs:124`)

### The Stale Values Problem

During DETECTION and VERIFICATION phases, the engine follows the solid physics branch (`HeatupSimEngine.cs:993`) because `bubblePreDrainPhase=true`. The solid branch calls `SolidPlantPressure.Update()` and reads back T/P, but does **not** update `physicsState.PZRWaterMass` or `physicsState.PZRSteamMass` each timestep. These values become increasingly stale.

During DRAIN phase, the values ARE properly maintained (`BubbleFormation.cs:391–397, 412–417`).

### The Conservation Cliff

The real conservation FAIL happens because `Initial_Total_Mass_lbm` was captured during initialization when the solid branch was active (both orphaned fields were 0), giving:

```
Initial_Total_Mass_lbm ≈ 0 + 0 + 0 + VCT_mass + BRS_mass ≈ ~100,173 lbm
```

After the branch flip to two-phase, the audit suddenly reads real mass values:

```
Total_Mass_lbm ≈ 712,000 + 111,000 + 0 + VCT + BRS ≈ ~924,000 lbm
```

The conservation error jumps to ~824,000 lbm instantly, triggering the alarm.

---

## 6. Why MASS INVENTORY Shows Correct Values

The MASS INVENTORY section (`Logging.cs:1160–1163`) never changed during the v5.0.3 refactor. It still reads:

```csharp
rcsWaterMass                    // engine field, updated every tick (line 1025, 1395)
pzrWaterVolume * pzrWaterDensity // V×ρ from engine fields, also updated every tick
```

These engine instance fields are populated by every physics regime, every timestep. They have no dependency on the orphaned `SystemState` canonical fields.

---

## 7. Summary: Side-by-Side Comparison

| Attribute | MASS INVENTORY | INVENTORY AUDIT (Solid) | INVENTORY AUDIT (Two-Phase) |
|---|---|---|---|
| RCS source | `rcsWaterMass` (engine field) | `physicsState.TotalPrimaryMassSolid - PZRWaterMassSolid` | `physicsState.RCSWaterMass` |
| PZR source | `pzrWaterVolume * ρ` (engine calc) | `physicsState.PZRWaterMassSolid` | `physicsState.PZRWaterMass` |
| Updated each tick? | Yes | **No — fields never written** | Partial (DRAIN yes, DETECT/VERIFY stale) |
| Shows zeros? | Never | **Always** | No (has init values from line 174/122) |

---

## Root Cause

**Two unfinished field implementations from v5.0.2/v5.0.3:**

1. `physicsState.TotalPrimaryMassSolid` and `physicsState.PZRWaterMassSolid` were declared in `SystemState` (CoupledThermo.cs:760–761) with architectural comments describing their role, but no code ever assigns values to them. They default to `0f`.

2. The v5.0.3 refactor of `UpdateInventoryAudit()` changed the solid-ops branch from V×ρ recalculation (which worked) to reading these canonical fields (which are always zero). The comment on line 290–291 explicitly says "rcsWaterDensity, pzrWaterDensity, pzrSteamDensity removed — no longer needed after eliminating V×ρ recalculation" — but the replacement writes were never implemented.

3. A secondary issue: `physicsState.TotalPrimaryMass_lb` (v5.3.0 canonical ledger, CoupledThermo.cs:768) is similarly declared but never written. This affects the `UpdatePrimaryMassLedgerDiagnostics()` method (Logging.cs:470–519), which reports `primaryMassLedger_lb = 0` throughout the run.

---

## Minimal Patch Plan

**Option A — Populate the canonical fields (completes the v5.0.2 architecture):**

1. **In `InitializeColdShutdown()` (Init.cs), after line 165:**
   ```
   physicsState.TotalPrimaryMassSolid = totalSystemMass;  // loops + PZR
   physicsState.PZRWaterMassSolid = pzrWaterMass;
   ```

2. **In the Regime 1 solid branch (`HeatupSimEngine.cs`), after line 1026:**
   ```
   physicsState.PZRWaterMassSolid = pzrWaterVolume * WaterProperties.WaterDensity(T_pzr, pressure);
   physicsState.TotalPrimaryMassSolid = physicsState.RCSWaterMass + physicsState.PZRWaterMassSolid;
   ```

3. **In `InitializeColdShutdown()` (Init.cs), after line 165:**
   ```
   physicsState.TotalPrimaryMass_lb = totalSystemMass;
   physicsState.InitialPrimaryMass_lb = totalSystemMass;
   ```

4. **In `InitializeWarmStart()` (Init.cs), after line 233:**
   ```
   physicsState.TotalPrimaryMass_lb = totalSystemMass;
   physicsState.InitialPrimaryMass_lb = totalSystemMass;
   ```

**Option B — Revert to V×ρ in the audit (quick fix, abandons canonical architecture):**

1. Replace lines 303–321 of `UpdateInventoryAudit()` with:
   ```
   float rcsRho = WaterProperties.WaterDensity(T_rcs, pressure);
   float pzrRho = WaterProperties.WaterDensity(T_pzr, pressure);
   float steamRho = solidPressurizer ? 0f : WaterProperties.SaturatedSteamDensity(pressure);
   inventoryAudit.RCS_Mass_lbm = rcsWaterMass;  // engine field, always current
   inventoryAudit.PZR_Water_Mass_lbm = pzrWaterVolume * pzrRho;
   inventoryAudit.PZR_Steam_Mass_lbm = pzrSteamVolume * steamRho;
   inventoryAudit.AuditMassSource = solidPressurizer ? "ENGINE_SOLID" : "ENGINE_TWO_PHASE";
   ```

**Recommendation:** Option A is preferred — it completes the intended architecture and ensures the canonical fields are available for the v5.5.0 telemetry layer. Option B is the fastest fix but leaves three dead fields in `SystemState` and would need to be re-done for v5.5.0 anyway.

---

## How to Validate in Logs

After applying the patch, re-run the simulation from cold shutdown and verify:

- [ ] **Interval 001 (0.25 hr):** INVENTORY AUDIT → `RCS Mass` shows ~712,000 lbm (not 0)
- [ ] **Interval 001 (0.25 hr):** INVENTORY AUDIT → `PZR Water Mass` shows ~111,000 lbm (not 0)
- [ ] **Interval 001 (0.25 hr):** INVENTORY AUDIT → `Mass Source` shows `CANONICAL_SOLID`
- [ ] **Interval 001 (0.25 hr):** INVENTORY AUDIT → `Initial Mass` ≈ `TOTAL MASS` ≈ 924,000 lbm
- [ ] **Interval 001 (0.25 hr):** INVENTORY AUDIT → `Conservation Error` < 100 lbm
- [ ] **All intervals before bubble:** MASS INVENTORY and INVENTORY AUDIT primary masses agree within 1%
- [ ] **At bubble detection:** `Mass Source` transitions from `CANONICAL_SOLID` to `CANONICAL_TWO_PHASE`
- [ ] **At bubble detection:** Conservation Error does not spike > 500 lbm (no cliff)
- [ ] **Through DRAIN phase:** INVENTORY AUDIT → `PZR Water Mass` decreases smoothly from ~111,000 to ~28,000 lbm
- [ ] **v5.0.2 MASS CONSERVATION section:** `TotalPrimaryMass` shows ~824,000 lb (not 0)
- [ ] **VALIDATION STATUS:** `Mass Conservation: PASS` from start of run

---

## Files Referenced

| File | Lines | Role |
|---|---|---|
| `HeatupSimEngine.Logging.cs` | 261–436 | `UpdateInventoryAudit()` — the broken consumer |
| `HeatupSimEngine.Logging.cs` | 1160–1163 | `MASS INVENTORY` log writer (correct) |
| `HeatupSimEngine.Logging.cs` | 1217–1243 | `INVENTORY AUDIT` log writer (displays broken data) |
| `HeatupSimEngine.Logging.cs` | 470–519 | `UpdatePrimaryMassLedgerDiagnostics()` (also affected) |
| `CoupledThermo.cs` | 760–761 | Orphaned field declarations |
| `CoupledThermo.cs` | 768 | `TotalPrimaryMass_lb` — also orphaned |
| `HeatupSimEngine.Init.cs` | 146–198 | Cold shutdown init (patch target) |
| `HeatupSimEngine.Init.cs` | 206–258 | Warm start init (patch target) |
| `HeatupSimEngine.cs` | 993–1041 | Regime 1 solid branch (patch target) |
| `HeatupSimEngine.BubbleFormation.cs` | 107–134 | Bubble detection transition point |
| `SolidPlantPressure.cs` | 37–80 | `SolidPlantState` — has its own mass fields, unlinked |
