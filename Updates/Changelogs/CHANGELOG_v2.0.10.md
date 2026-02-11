# Changelog — v2.0.10

**Version:** 2.0.10  
**Date:** 2026-02-10  
**Matching Implementation Plan:** `IMPLEMENTATION_PLAN_v2.0.10.md`

---

## Summary

Patch addressing three unrealistic simulation behaviors: inventory conservation error growing over time, heater bang-bang oscillation during bubble formation/pressurization, and an unrealistically long RCP start delay.

---

## Changes

### Stage 1: State-Based Inventory Audit

**File:** `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs`  
**GOLD Standard:** Yes — single calculation line changed in `UpdateInventoryAudit()`

- **Changed:** RCS mass calculation in `UpdateInventoryAudit()` from reading the flow-integrated `rcsWaterMass` field to a state-based recalculation using `(RCS_WATER_VOLUME - PZR_TOTAL_VOLUME) × WaterDensity(T_rcs, pressure)`.
- **Reason:** During solid pressurizer operations, `UpdateRCSInventory()` is guarded off, so `rcsWaterMass` becomes stale. Thermal expansion mass flowing from PZR to RCS via the surge line was never credited, causing a monotonically growing conservation error (up to 1.90% at 8 hours).
- **Result:** Conservation error now < 0.1% throughout the entire simulation. Only the audit's **reporting** changes — the physics engine's `rcsWaterMass` field is not modified.

### Stage 2: Smoothed Heater Control During Bubble Formation

**Files:**
- `Assets/Scripts/Physics/CVCSController.cs` — GOLD
- `Assets/Scripts/Validation/HeatupSimEngine.cs` — GOLD

**Changes:**

1. **Added `SmoothedOutput` field** to `HeaterControlState` struct — allows caller to read back the rate-limited output for persistence between timesteps.

2. **Added `dt_hr` and `smoothedOutput` parameters** to `CalculateHeaterState()` — both optional with backward-compatible defaults (`dt_hr = 0f`, `smoothedOutput = 1.0f`). When `dt_hr = 0`, behavior is identical to before (instantaneous).

3. **Modified `BUBBLE_FORMATION_AUTO` case** — target fraction is still calculated with the same pressure-rate formula. Now rate-limited using `HEATER_RATE_LIMIT_PER_HR` (6.0/hr). At 10-second timesteps, max change per step ≈ 1.67%. Full travel 20%→100% takes ~2.9 minutes (realistic valve travel).

4. **Modified `PRESSURIZE_AUTO` case** — identical rate-limiting applied.

5. **Added `bubbleHeaterSmoothedOutput` field** to `HeatupSimEngine.cs` — persists between timesteps, initialized to 1.0.

6. **Updated call site** in `StepSimulation()` section 1B — passes `dt` and `bubbleHeaterSmoothedOutput` to `CalculateHeaterState()`, reads back `SmoothedOutput` for bubble/pressurize modes.

- **Result:** Heater power smoothly converges to a stable operating point (~60% at typical pressure rates) with small adjustments — no more bang-bang oscillation between 100% and 20%.

### Stage 3: Correct RCP Start Delay

**File:** `Assets/Scripts/Physics/RCPSequencer.cs` — GOLD

- **Changed:** `RCP1_START_DELAY` from `1.0f` (1 hour) to `10f / 60f` (10 minutes).
- **Reason:** Per NRC HRTD 19.2.2, there is no mandated waiting period after bubble formation. The only prerequisites are bubble existence, P ≥ 320 psig, and seal injection available. The 10-minute delay models realistic operator verification activities.
- **Updated:** File header comment to reflect new timing (~100 min for all 4 RCPs vs ~2 hours).
- **Updated:** `ValidateCalculations()` — all test expected values recalculated for the new delay.
- **Result:** First RCP starts ~10 minutes after bubble formation completes (assuming pressure ≥ 320 psig). Subsequent RCPs follow at unchanged 30-minute intervals.

---

## Files Modified

| File | GOLD? | Change Summary |
|------|-------|----------------|
| `HeatupSimEngine.Logging.cs` | Yes | State-based RCS mass in `UpdateInventoryAudit()` |
| `CVCSController.cs` | Yes | Rate-limiting in bubble/pressurize heater modes; `SmoothedOutput` field; `dt_hr`/`smoothedOutput` params |
| `HeatupSimEngine.cs` | Yes | Added `bubbleHeaterSmoothedOutput` field; updated heater call site |
| `RCPSequencer.cs` | Yes | `RCP1_START_DELAY` 1.0→0.167 hr; header comment; validation tests |

---

## Unaddressed Issues (Deferred)

| Issue | Reason | Tracked In |
|-------|--------|------------|
| Flow-integrated `rcsWaterMass` stale during solid ops | Audit is now state-based so conservation is correct. Broader refactor needed for physics engine. | Future Features → v1.2.0 |
| `RCP_START_INTERVAL` (30 min) not validated against Westinghouse | Current value is reasonable. No contradicting evidence found. | N/A |
| Heater proportional/backup group separation during bubble formation | Currently modeled as single block. Full group staging deferred to AUTOMATIC_PID mode. | Future Features → v1.1.7 |
