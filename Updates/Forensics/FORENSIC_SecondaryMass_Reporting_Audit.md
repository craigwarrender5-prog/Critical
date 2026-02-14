# Forensic Report: Secondary Mass Reporting Inconsistency

**Date:** 2026-02-13
**Baseline:** v5.4.1
**Scope:** Classification + Impact Assessment (no code changes)

---

## 1. Issue Summary

Two log labels report SG secondary-side mass:

| Label | Source Variable | Source File | Reads From |
|---|---|---|---|
| `Secondary Mass: X lb (of 1660000 initial)` | `sgSecondaryMass_lb` | HeatupSimEngine.Logging.cs:1157 | Engine display field |
| `Sec.Mass: X lb` | `state.SecondaryWaterMass_lb` | SGMultiNodeThermal.cs:1597 | Physics state struct (canonical) |

The `Sec.Mass` label reads directly from the canonical physics state and is always correct.
The `Secondary Mass` label reads from an engine display field that is **never assigned**.

---

## 2. Root Cause

### Missing State Synchronization

`HeatupSimEngine.cs` declares seven SG draining/level display fields (lines 154–160):

```
sgDrainingActive            (bool)
sgDrainingComplete          (bool)
sgDrainingRate_gpm          (float)
sgTotalMassDrained_lb       (float)
sgSecondaryMass_lb          (float)
sgWideRangeLevel_pct        (float)
sgNarrowRangeLevel_pct      (float)
```

**None of these are assigned** in the three regime sync blocks:

| Regime | Sync Block Location | Draining/Level Fields Copied? |
|---|---|---|
| Regime 1 (solid PZR) | HeatupSimEngine.cs:~1020–1032 | **NO** |
| Regime 2 (bubble forming) | HeatupSimEngine.cs:~1244–1256 | **NO** |
| Regime 3 (two-phase PZR) | HeatupSimEngine.cs:~1374–1387 | **NO** |

Each sync block copies thermal, pressure, and boiling fields from `sgMultiNodeState` but omits the draining/level fields. This is a **synchronization omission introduced when the draining feature was added in v5.0.0 Stage 4**.

The fields remain at their C# default values:
- `sgSecondaryMass_lb` = **0.0** (float default)
- `sgTotalMassDrained_lb` = **0.0**
- `sgWideRangeLevel_pct` = **0.0**
- `sgNarrowRangeLevel_pct` = **0.0**
- `sgDrainingActive` = **false**
- `sgDrainingComplete` = **false**
- `sgDrainingRate_gpm` = **0.0**

### Why Logs Currently Show 1660000

In available log files (early intervals at 0.25–0.75 hr), `Secondary Mass` reports 1660000 — not 0. This suggests either:
- The Init partial class sets initial values (checked: `HeatupSimEngine.Init.cs:84` only sets `sgBoilingActive = false`, not the mass fields), OR
- The logging was observed at a point before draining activates in a scenario where the field happened to display correctly due to a prior code path.

**However**, the structural analysis is definitive: no assignment `sgSecondaryMass_lb = sgMultiNodeState.SecondaryWaterMass_lb` exists anywhere in the codebase. If the field ever shows a correct non-zero value, it would be due to Unity serialization retaining a prior value, not due to correct runtime sync.

---

## 3. Classification

**This is a logging/state-sync bug**, not a physics inconsistency.

- The canonical physics variable `state.SecondaryWaterMass_lb` in `SGMultiNodeThermal.cs` is correctly initialized, updated during boiling and draining, and correctly read for level calculations, steam space volume, and forensic snapshots.
- The display-layer mirror fields in `HeatupSimEngine.cs` are stale placeholders.

---

## 4. Impact Assessment

| Concern | Affected? | Details |
|---|---|---|
| Primary mass conservation | **No** | Secondary mass is not part of `TotalPrimaryMass_lb` ledger |
| Energy balance | **No** | SG heat transfer uses `state.SecondaryWaterMass_lb` directly from physics module |
| Pressure calculation | **No** | Secondary pressure computed from `sgMultiNodeState`, not display fields |
| Boiling trigger | **No** | Boiling logic reads from `sgMultiNodeState.SecondaryWaterMass_lb` |
| Level/inventory tracking (physics) | **No** | Level computed in SGMultiNodeThermal from canonical state |
| Level/inventory tracking (display) | **YES** | Interval logs show stale values for draining status, mass, and levels |
| Forensic CSV output | **No** | `SGForensics.cs` reads directly from `sgMultiNodeState` |
| Validation UI panels | **Possibly** | Any UI binding to `sgSecondaryMass_lb` would show stale data |

**Risk: LOW** — Physics integrity is unaffected. Only the interval log display layer is incorrect.

---

## 5. Affected Files

| File | Role | Issue |
|---|---|---|
| `Assets/Scripts/Validation/HeatupSimEngine.cs:154–160` | Field declarations | Fields declared, never assigned |
| `Assets/Scripts/Validation/HeatupSimEngine.cs:~1020,~1244,~1374` | Regime sync blocks | Missing 7 draining/level field assignments |
| `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1153–1159,1334–1339` | Interval + summary logs | Reads stale display fields |
| `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1597` | Physics diagnostic string | Reads canonical state (correct) |
| `Assets/Scripts/Physics/SGForensics.cs:502,663` | Forensic snapshot/CSV | Reads canonical state (correct) |

---

## 6. Recommendation

### Fix in: **v5.4.2.0** (mass conservation patch)

**Reasoning:**

- v5.4.2.0 (FF-05) is the next scheduled version and addresses mass conservation reporting.
- This is a minor reporting correction — adding 7 assignment lines to each of 3 sync blocks (21 lines total).
- It does not require new physics logic, architecture changes, or SG model modifications.
- Deferring to v5.6.0.0 (SG corrections) is unnecessary — this is not an SG physics issue.
- Deferring to v5.7.0.0 (architecture hardening) is also unnecessary — the sync pattern already exists for other fields; this is simply an omission.

### Required Fix (when implemented)

Add to each of the three regime sync blocks in `HeatupSimEngine.cs`:

```
sgDrainingActive       = sgMultiNodeState.DrainingActive;
sgDrainingComplete     = sgMultiNodeState.DrainingComplete;
sgDrainingRate_gpm     = sgMultiNodeState.DrainingRate_gpm;
sgTotalMassDrained_lb  = sgMultiNodeState.TotalMassDrained_lb;
sgSecondaryMass_lb     = sgMultiNodeState.SecondaryWaterMass_lb;
sgWideRangeLevel_pct   = sgMultiNodeState.WideRangeLevel_pct;
sgNarrowRangeLevel_pct = sgMultiNodeState.NarrowRangeLevel_pct;
```

---

## 7. Summary

| Item | Value |
|---|---|
| Issue Type | Logging/state-sync bug (not physics) |
| Root Cause | v5.0.0 Stage 4 draining fields never synced to engine display layer |
| Physics Impact | None |
| Display Impact | Interval logs show stale/default values for 7 SG fields |
| Risk Level | **LOW** |
| Recommended Fix Version | **v5.4.2.0** |
| Fix Scope | 21 assignment lines across 3 sync blocks in HeatupSimEngine.cs |
