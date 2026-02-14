# UPDATE v1.1.0.0 — Bug #1 + Bug #4: Multi-Phase Bubble Formation & PI Pre-Seeding

**Date:** 2026-02-06  
**Version:** 1.1.0.0  
**Type:** Major Build (structural change to bubble formation sequence)  
**Backwards Compatible:** No — bubble formation now takes ~60 sim-minutes instead of being instant  
**References:**
- HANDOVER_PZR_BubbleFormation_and_RCP_Bugs.md — Bug #1 (Root Cause), Bug #4
- NRC ML11223A342 Section 19.2.2 — Bubble formation procedure
- NRC ML11251A014 Section 2.1 — Alternate drain-down description
- NRC ML11223A214 Section 4.1 — CVCS flow balance

---

## Summary

Replaced the instant single-timestep PZR snap (100% → 25% level) with a realistic multi-phase bubble formation state machine that takes approximately 60 sim-minutes. This was the root cause bug (#1) that cascaded into mass conservation failure (#2), excessive level drop at RCP start (#3), and CVCS controller inability to recover (#4).

## Root Cause

The original code at HeatupSimEngine.cs line ~808 instantly set:
- `PZRWaterVolume = 1800 × 0.25 = 450 ft³` (removed 1350 ft³ in one timestep)
- `PZRSteamVolume = 1350 ft³` (appeared from nothing)
- `totalSystemMass` recalculated (mass discontinuity)

This violated mass conservation, gave the CVCS no time to equilibrate, and left the PI controller with zero integral when RCPs started.

## Changes

### PlantConstants.cs (7 new constants)

Added bubble formation drain procedure constants in the Bubble Formation region:

| Constant | Value | Source |
|----------|-------|--------|
| `BUBBLE_DRAIN_LETDOWN_GPM` | 120 gpm | NRC HRTD 19.2.2 |
| `BUBBLE_DRAIN_CHARGING_GPM` | 75 gpm | NRC HRTD 19.2.2 |
| `BUBBLE_PHASE_DETECTION_HR` | 5/60 hr (5 min) | NRC HRTD 2.1 |
| `BUBBLE_PHASE_VERIFY_HR` | 5/60 hr (5 min) | NRC HRTD 19.2.2 |
| `BUBBLE_PHASE_DRAIN_HR` | 40/60 hr (40 min) | NRC HRTD 19.2.2 |
| `BUBBLE_PHASE_STABILIZE_HR` | 10/60 hr (10 min) | Procedural |
| `BUBBLE_DRAIN_MIN_PRESSURE_PSIA` | 334.7 psia | NRC HRTD 19.2.2 |

### HeatupSimEngine.cs (Structural Changes)

**Added:** `BubbleFormationPhase` enum with 7 states:
- `NONE` → `DETECTION` → `VERIFICATION` → `DRAIN` → `STABILIZE` → `PRESSURIZE` → `COMPLETE`

**Added:** State tracking fields:
- `bubblePhase` — current phase
- `bubblePhaseStartTime` — timestamp for phase progression
- `bubbleDrainStartLevel` — starting level for progress display

**Replaced:** Instant bubble snap block (~48 lines) with:
1. **Detection trigger** (~35 lines): Sets `solidPressurizer = false`, enters DETECTION, but keeps `bubbleFormed = false` to gate RCP starts
2. **State machine** (~200 lines): Runs each timestep before CVCS section, manages all 5 active phases

**Modified:** CVCS flow control section:
- Added `bubbleDrainActive` flag for DRAIN phase CVCS override
- Letdown set to 120 gpm, charging held at 75 gpm during drain
- PI controller bypassed during drain, initialized when drain completes

**Key design decisions:**
- `bubbleFormed` only set TRUE at COMPLETE phase → RCPSequencer won't start RCPs during drain
- `bubbleFormationTime` set at COMPLETE → RCP countdown starts after full stabilization
- IsolatedHeatingStep continues running during all bubble phases (handles T/P physics)
- State machine handles volume changes independently (IsolatedHeatingStep doesn't modify volumes)

### CVCSController.cs (Bug #4 Fix)

**Added:** `PreSeedForRCPStart()` method (~40 lines):
- Pre-loads PI integral with ~5 gpm extra charging bias
- Provides immediate recovery capacity for RCP thermal transient
- Called at bubble formation COMPLETE, before RCPs can start
- Updates level setpoint from program for current temperature

### Drain Phase Physics

Each timestep during DRAIN:
1. Net outflow = 120 - 75 = 45 gpm → converted to ft³/s
2. Drain volume = net_outflow × dt removed from PZR water
3. Steam fills void at saturation density (steam forms at liquid surface, not by flashing)
4. Heaters supply latent heat of vaporization (~850 BTU/lb at 350 psig)
5. Total system mass updated (mass is leaving via letdown)
6. PZR level checked against 25% target

### Phase Progression Timeline (typical)

| Time | Phase | Key Events |
|------|-------|------------|
| T+0 | DETECTION | Bubble detected, instruments confirming |
| T+5 min | VERIFICATION | Aux spray test, confirm compressible gas |
| T+10 min | DRAIN | Letdown 120 gpm, charging 75 gpm, net -45 gpm |
| T+50 min | STABILIZE | Level at 25%, CVCS rebalancing, PI initialized |
| T+60 min | PRESSURIZE | Heaters raising P to ≥320 psig |
| T+~65 min | COMPLETE | `bubbleFormed = true`, RCPs permitted |

## Impact

- Bubble formation now takes ~60 sim-minutes (was instant)
- CVCS has time to equilibrate before RCP thermal transient
- Mass conservation maintained throughout (no discontinuity)
- PI controller pre-seeded for RCP start (Bug #4)
- RCP start delayed by ~60 min compared to previous behavior
- All existing physics modules (IsolatedHeatingStep, CoupledThermo) unchanged

## Files Changed

| File | Lines Changed | Description |
|------|--------------|-------------|
| `PlantConstants.cs` | +67 | 7 new bubble drain constants with documentation |
| `HeatupSimEngine.cs` | +201 | Enum, state fields, state machine, CVCS overrides |
| `CVCSController.cs` | +43 | PreSeedForRCPStart method |
| `UPDATE-v1.1.0.0.md` | new | This changelog |

## Verification Notes

- Bug #3 (excessive PZR level drop at RCP start) should self-resolve: CVCS is now fully equilibrated with 25% level established gradually, PI integral pre-seeded
- Mass conservation error from Bug #2 fix (v1.0.5.1) will now track properly through the drain phase
- Simulation total heatup time increases by ~60 min due to drain procedure
- Test suite may need updated expected timing values for RCP start events
