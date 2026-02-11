# CRITICAL: Heatup Simulation — PZR Bubble Formation & RCP Startup Bug Analysis

## Session Handover Summary — 2026-02-06

**Transcripts available at:**
- `/mnt/transcripts/2026-02-06-15-53-58-rcp-startup-pzr-vct-bugs-fixed.txt` (original bug investigation)
- `/mnt/transcripts/2026-02-06-15-57-49-pzr-bubble-formation-research.txt` (NRC research & bubble physics)

---

## CONTEXT

During heatup simulation testing, a cascade failure was observed starting at RCP #1 start (T+8:51 sim time). The heatup runs cleanly from T+0 through PZR heater energization and bubble formation (Logs #000–#016, all PASS on mass conservation) but fails catastrophically when the first RCP starts.

**Key log data points showing the failure:**

| Parameter | Log #016 (T+8:30, Pre-RCP) | Log #017 (T+9:00, Post-RCP) |
|-----------|---------------------------|----------------------------|
| PZR Level | 25.00% | 10.24% |
| VCT Level | 70.1% | 54.2% |
| Letdown | 75.0 gpm (RHR XCONN) | 0.0 gpm (ISOLATED) |
| Charging | 75.0 gpm | 82.3 gpm |
| Mass Cons Err | 1.46 gal (PASS) | 649.25 gal (FAIL) |
| RCS Pressure | 753.5 psia | 985.5 psia |
| PZR Heaters | 1.80 MW | 0.00 MW (tripped) |

---

## FOUR BUGS IDENTIFIED

### BUG #1 (ROOT CAUSE): Instant PZR Bubble Formation Transition

**Location:** `HeatupSimEngine.cs` line ~625 (bubble formation trigger)

**Problem:** When `solidPlantState.BubbleFormed` triggers, the code **instantly** sets PZR from 100% water to 25% water / 75% steam in a single timestep:

```csharp
// Current code — WRONG: instant transition
float targetLevel = PlantConstants.PZR_LEVEL_AFTER_BUBBLE;
physicsState.PZRWaterVolume = PlantConstants.PZR_TOTAL_VOLUME * targetLevel / 100f;
physicsState.PZRSteamVolume = PlantConstants.PZR_TOTAL_VOLUME - physicsState.PZRWaterVolume;
```

**Reality per NRC HRTD 19.2.2:** Bubble formation is a multi-phase, operator-controlled process taking 30–60 minutes. It is NOT a flash event. The actual procedure is:

1. **Pre-condition:** PZR is 100% solid water at ~100°F. CVCS charging and letdown already in service at 75 gpm via RHR cross-connect (HCV-128).
2. **Heaters bring PZR to Tsat:** 1.8 MW heaters raise PZR water from ~100°F to ~435°F (Tsat at ~350 psig). Takes roughly 3–4 hours at 80–100°F/hr PZR heatup rate (NRC mandates max 100°F/hr).
3. **Operators increase letdown / reduce charging:** This creates a net outflow from the RCS, causing the PZR level to drop. As pressure drops slightly, water at the liquid surface (already at Tsat) begins gently boiling — **not flashing**. Steam fills the void being created by the drain. The diagnostic signature is "continuing decrease in level without a consequent decrease in pressure" (NRC HRTD 2.1).
4. **Controlled drain to 25%:** Letdown increased to ~120 gpm, charging held at ~75 gpm = ~45 gpm net outflow. PZR drains from ~100% down to 25% level over ~30-60 min. Heaters maintain pressure by supplying latent heat of vaporization.
5. **Stabilization:** CVCS rebalanced, level control transferred to automatic, pressure stabilized.
6. **Pressure increase:** Heaters continue raising pressure to ≥320 psig for RCP NPSH requirements before RCPs can start.

**Impact:** The instant transition is the foundational cause of all downstream bugs. It changes 75% of PZR volume from water to steam in zero time, creating/destroying mass and giving no time for CVCS equilibrium.

### BUG #2: Missing VCT Mass Conservation Tracking

**Location:** `HeatupSimEngine.StepSimulation()` line ~1150

**Problem:** When RCPs are running, the code does not call `VCTPhysics.AccumulateRCSChange()` after the CVCS controller update. CVCS drives 82.3 gpm net charging into RCS (letdown isolated, only charging active), but this inventory change is never accumulated in VCT state. Mass conservation error explodes from 1.46 gal to 649 gal.

**Fix:** Add `VCTPhysics.AccumulateRCSChange()` call in the RCP-running branch, matching the pattern used in the no-RCP branch.

### BUG #3: Excessive PZR Level Drop from CoupledThermo at RCP Start

**Location:** `CoupledThermo.cs` `SolveEquilibrium()` method

**Problem:** When RCP #1 starts at T+9:00hr, PZR level crashes from 25% to ~9.4% essentially instantly. Physics: RCP adds 5.25 MW heat → RCS heatup rate jumps from 0.21°F/hr to 23.79°F/hr → pressure rises 753→985 psia → steam in PZR compresses → level drops. However, 25%→9.4% in minutes at cold conditions (~100°F RCS, ~540°F PZR) is unrealistically aggressive. The CoupledThermo solver may be over-computing the steam compression effect.

**Note:** This bug may be partially or fully resolved by fixing Bug #1, since a proper gradual bubble formation would establish CVCS equilibrium before RCPs start.

### BUG #4: CVCS Controller Unprepared for RCP Start Transition

**Location:** `CVCSController.cs` initialization at mode transitions

**Problem:** CVCS PI controller initialized at bubble formation with zero integral error. When RCP starts and PZR level plummets, controller faces 14.8% level error but integral needs time to wind up. With letdown isolated (triggered by <17% level interlock), base charging is only seal injection (8 gpm/RCP) plus PI corrections. Controller eventually reaches 82.3 gpm but PZR has already drained significantly.

**Fix:** Pre-seed the CVCS PI integral at RCP start transition to match the expected charging rate for the new configuration.

---

## THE FAILURE CASCADE (How Bugs Interact)

1. RCP #1 starts → adds 5.25 MW heat to cold RCS
2. Rapid pressure rise (437→985 psia) → steam compression + thermal expansion
3. PZR level crashes 25%→9.4% (too aggressive, Bug #3)
4. PZR level hits 17% → letdown isolates, heaters trip (safety interlock)
5. With letdown at 0 gpm, VCT receives only seal return (~3 gpm/RCP) but supplies charging at 78+ gpm → net VCT drain of ~80 gpm
6. VCT drains from 70% to 10% in ~30 min
7. VCT hits 25% → auto makeup starts (35 gpm), but can't keep up
8. VCT hits 17% → VCT LOW LEVEL alarm
9. PZR can't recover because PI controller fights massive level error with no letdown baseline (Bug #4)
10. Mass conservation error unchecked (Bug #2) → 649 gal error

---

## PROPOSED SOLUTION: Multi-Phase Bubble Formation

Replace the instant PZR transition with a new `BUBBLE_FORMATION` simulation phase spanning ~30–60 minutes of sim time:

### Phase 1: Bubble Detection (~5 min sim time)
- PZR water temperature reaches Tsat
- First tiny steam bubbles form at heater surfaces
- Vapor space temperature instruments start rising toward Tsat
- Small pressure perturbation as first steam displaces water
- PZR level starts dropping slightly without corresponding pressure drop (diagnostic signature)

### Phase 2: Bubble Verification (~5 min sim time)
- Operators close PORVs and test with auxiliary spray
- If steam bubble exists, aux spray causes rapid pressure decrease (confirming compressible gas, not solid water)

### Phase 3: Controlled Drain (~20–40 min sim time)
- Letdown increased from 75 gpm to ~120 gpm
- Charging held at ~75 gpm (or reduced slightly)
- Net outflow ~45 gpm drains PZR from ~100% toward 25% level
- Heaters supply latent heat of vaporization to maintain pressure
- Steam forms continuously at liquid surface to fill growing void
- Drained water flows through letdown to VCT/holdup tanks (tracked for mass conservation)
- Pressure maintained in 320–400 psig band by heater energy balance

### Phase 4: Stabilization (~10 min sim time)
- Level reaches 25%, CVCS rebalanced (letdown returned to ~75 gpm)
- PZR heaters maintaining pressure at target
- CVCS level control transferred to automatic
- PI controller initialized with equilibrium state (fixes Bug #4)

### Phase 5: Pressure Increase (before RCPs)
- Heaters continue raising pressure to ≥320 psig for RCP NPSH requirements
- Only then can RCPs start — this ensures CVCS is fully stable before the RCP thermal transient

---

## FILES TO MODIFY

| File | Changes |
|------|---------|
| `HeatupSimEngine.cs` | Replace instant bubble transition (~line 625) with multi-phase BUBBLE_FORMATION state machine; Add VCT mass conservation call in RCP branch (~line 1150) |
| `CoupledThermo.cs` | Review `SolveEquilibrium()` for excessive steam compression at RCP start; may self-resolve with gradual bubble formation |
| `CVCSController.cs` | Add PI integral pre-seeding at RCP start transition; Add letdown ramp-up logic for bubble formation drain phase |
| `VCTPhysics.cs` | Ensure `AccumulateRCSChange()` properly tracks drain-down volume during bubble formation |
| `PlantConstants.cs` | May need new constants for bubble formation drain rates, letdown ramp targets |

---

## KEY NRC REFERENCES

- **NRC HRTD ML11223A342** — Westinghouse Technology Systems Manual Section 19.0 Plant Operations (Section 19.2.2 covers bubble formation procedure)
- **NRC HRTD ML11251A014** — Section 2.1 Reactor Coolant System (alternate drain-down procedure description)
- **NRC HRTD CVCS Section 4.1** — ML11223A214 (CVCS flow balance, letdown orifices, charging pump operation)
- **NRC HRTD Section 5** — ML11251A019 (letdown flow control during startup, back-pressure regulation to prevent flashing)

---

## KEY PHYSICS NOTES

- **Bubble forms by controlled drain, NOT by flashing.** Operators increase letdown/decrease charging to create void. Water at Tsat gently boils at the surface to fill the void. Steam formation is the consequence of the drain, not the cause.
- **Letdown back-pressure valves (PCV-131) maintain 460 psig** downstream of the letdown heat exchanger to prevent flashing in the letdown piping (Tsat at 460 psig = 458°F > max letdown temp of 450°F).
- **Max letdown flow is 128 gpm** (limited to prevent total outflow from exceeding max charging of 132 gpm with all three pumps).
- **PZR heater demand increases significantly during bubble formation** — they must supply latent heat of vaporization (~850 BTU/lb at these conditions) on top of temperature maintenance.
- **The instant 100%→25% PZR snap is the root cause** — it changes 75% of PZR volume in zero time, creating a mass discontinuity. A proper 30–60 min drain gives CVCS time to track the change and establish equilibrium before RCP start.

---

## STATUS

**Research complete. Ready to begin implementation.**

Recommended approach: Fix Bug #1 (bubble formation transition) first, as it is the foundational issue. Then verify if Bugs #3 and #4 are resolved by the proper transition sequence. Bug #2 (missing VCT mass conservation call) should be a quick independent fix.
