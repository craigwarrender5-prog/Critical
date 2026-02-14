# CRITICAL: Master the Atom — Implementation Plan v0.6.0
# BRS Implementation & Closed-Loop Inventory Correction

**Date:** 2026-02-07
**Triggered by:** Heatup simulation run (Build/HeatupLogs, 25 interval logs, T+0 to T+12.5hr)
**Version type:** Minor (new subsystem, physics behaviour change)
**Previous version:** 0.5.0 (legacy cleanup)
**Updated:** 2026-02-07 — BRS specifications sourced from NRC/FSAR documentation

---

## 1. Problem Statement

The simulation does not model a closed-loop fixed-inventory primary coolant system.
Heatup validation logs show persistent VCT level instability with the level spending
the majority of the simulation outside the normal operating band (40–70%), triggering
repeated divert and alarm conditions that would not occur in this pattern on a real plant.

### 1.1 Evidence from Heatup Logs

| Time (hr) | VCT Level | VCT Status   | Charging | Letdown | Surge Flow | Notes |
|-----------|-----------|-------------|----------|---------|------------|-------|
| 0.50      | 56.0%     | NORMAL       | 75.0     | 76.4    | 1.40       | Solid PZR — balanced, slight rise |
| 2.50      | 61.1%     | NORMAL       | 75.0     | 77.1    | 2.11       | Rising — expansion accumulating |
| 5.00      | 70.5%     | **DIVERTING** | 75.0     | 78.0    | 3.02       | Crossed 70% divert setpoint |
| 7.50      | 71.2%     | **DIVERTING** | 75.0     | 80.6    | 5.57       | Steady divert, VCT Level FAIL |
| 10.00     | 32.7%     | NORMAL       | 64.4     | 0.0     | 1.22       | Post-bubble, letdown ISOLATED, VCT draining fast |
| 12.50     | 84.1%     | **DIVERTING** | 32.0     | 75.0    | 28.15      | 4 RCPs, PI saturated, massive expansion |

**Key observations:**
- VCT level validation **FAILS** at T=5.0hr and never recovers
- At T=12.50hr: PI integral saturated at -600 (max), charging at floor (32 gpm = seal injection only), letdown at 75 gpm, yet VCT still rising to 84% because the 28 gpm surge flow expansion overwhelms the controller
- Total VCT cumulative in: 57,985 gal. Total VCT cumulative out: 56,818 gal. Thousands of gallons have passed through the VCT with no accounting of where diverted water went
- Mass conservation shows PASS because the check tracks VCT+RCS internal transfers, but does **not** verify total system inventory is conserved across the divert boundary

### 1.2 Root Cause Analysis

The CVCS is modelled as an **open-drain system** rather than a **closed-loop recirculating system**:

```
CURRENT MODEL (broken):

  RCS ←→ VCT (via charging/letdown)    ← This part is correct
         VCT → DIVERT → [void]          ← Water permanently lost
         [RWST/RMS] → VCT               ← External makeup replaces it

REAL PLANT (correct):

  RCS ←→ VCT (via charging/letdown)
         VCT → DIVERT → BRS holdup tanks → evaporator processing → BAT / PWST
         BRS processed water → VCT (when level low, as makeup source)
```

**Three specific defects:**

1. **No BRS exists.** When the VCT divert valve (LCV-112A) opens above 70%, the
   diverted letdown flow is subtracted from VCT volume and disappears from the
   simulation. In a real plant, this water enters the BRS recycle holdup tanks where
   it is stored, batch-processed through the boric acid evaporator, and the products
   returned to the BAT (concentrated boric acid) and PWST (clean distillate).

2. **Makeup comes from external sources.** When VCT level drops (e.g., during
   bubble drain or RCP start transients), the auto-makeup system draws from
   the Reactor Makeup System (RMS) or RWST — both modelled as infinite external
   sources. In a real plant during normal heatup, makeup would come from BRS
   processed water or RMS blending (BAT + PWST), not directly from RWST.

3. **No total-system inventory conservation.** The mass conservation check in
   `VCTPhysics.VerifyMassConservation()` only verifies internal consistency of
   VCT+RCS transfers vs. external flows. It does not verify that the total
   system inventory (RCS + PZR + VCT + BRS) remains constant. Water leaks out
   through divert and is silently replaced by RWST, so the check passes despite
   non-physical inventory behaviour.

### 1.3 Consequences

- VCT level oscillates between divert (>70%) and normal/low rather than staying
  in the 40-70% normal band as expected during a well-controlled heatup
- Boron concentration tracking may drift because diverted borated water is lost
  and replaced with RWST water at a different boron concentration
- The PI charging controller saturates trying to compensate for inventory
  imbalances that would not exist on a real plant with a functional BRS
- Operators on a real plant would see stable VCT levels during solid plant
  heatup because the ~1-6 gpm net expansion accumulation is small relative
  to VCT capacity and is managed by the divert/BRS/return cycle

---

## 2. Thermal Expansion Flow Path Analysis

### 2.1 The User's Question

> During heatup, Charging is 75 gpm, Letdown is 75 gpm, and then there is the
> expansion of around 1.3 gpm. Is the 1.3 gpm INCLUDED in the 75 gpm, or IN
> ADDITION TO the 75 gpm?

### 2.2 Answer

The thermal expansion flow is **IN ADDITION TO** the base 75 gpm, and the current
model handles this correctly. Here is why:

During solid plant operations, the CVCS pressure controller (SolidPlantPressure.cs)
maintains RCS pressure by adjusting letdown flow. The controller equation is:

```
letdown = base_letdown (75 gpm) + PI_controller_adjustment
charging = base_charging (75 gpm)  ← constant during solid plant ops
```

When PZR heaters warm the pressurizer water, thermal expansion increases the total
system volume. In a water-solid (incompressible) system, this volume increase
would cause a pressure spike. The PI controller responds by **increasing letdown
above 75 gpm** to bleed off the excess volume and hold pressure in band.

The heatup logs confirm this:

| Time | Letdown | Charging | Delta (expansion) | Surge Flow |
|------|---------|----------|-------------------|------------|
| 0.5hr | 76.4   | 75.0     | 1.4 gpm          | 1.40 gpm   |
| 2.5hr | 77.1   | 75.0     | 2.1 gpm          | 2.11 gpm   |
| 5.0hr | 78.0   | 75.0     | 3.0 gpm          | 3.02 gpm   |
| 7.5hr | 80.6   | 75.0     | 5.6 gpm          | 5.57 gpm   |

The letdown-charging delta matches the surge flow (PZR thermal expansion rate)
almost exactly. The controller is correctly adding the expansion removal on top
of the 75 gpm base.

### 2.3 Measurement Point Matters

The user correctly identified that the answer depends on where you measure:

- **At the charging pump discharge (leaving VCT):** 75.0 gpm. The expansion
  volume is NOT in this measurement. Charging flow is set by the controller
  independent of expansion.

- **At the letdown path (RHR crossconnect exit, entering VCT):** 76.4–80.6 gpm.
  The expansion IS included in this measurement. The total flow leaving the RCS
  through the letdown path includes both the recirculation component (matching
  charging) and the excess removal component (matching expansion).

- **At the surge line (PZR to hot leg):** 1.4–5.6 gpm. This is the pure
  thermal expansion flow rate — the volume displaced by PZR water heating.

The net effect on the VCT is: `inflow = letdown`, `outflow = charging`, so
`net = letdown - charging = expansion rate`. The expansion volume accumulates
in the VCT at ~1-6 gpm.

### 2.4 Why This Causes VCT Problems

Over the solid plant phase (0–8 hours), the cumulative expansion volume is:

```
~2 gpm average × 60 min/hr × 8 hr = ~960 gallons
```

VCT capacity is 4000 gallons. Starting at 55% (2200 gal), adding 960 gallons
reaches ~79% — well above the 70% divert setpoint. This is why the VCT hits
divert by T=5hr and stays there.

On a real plant, this is exactly what happens — the divert valve opens and sends
the excess to BRS holdup tanks. The VCT level stabilises near the divert setpoint
(70%) because the divert valve is proportional (LCV-112A). This part of the model
**is correct**. The problem is that the diverted water has nowhere to go and nowhere
to come back from.

---

## 3. Expected Behaviour (Real Plant Reference)

Per NRC HRTD 4.1 (ML11223A214) and the Callaway FSAR Chapter 11 (ML21195A182):

### 3.1 Solid Plant Phase (0 to ~8 hr)

1. PZR heaters warm pressurizer water at ~40°F/hr
2. Thermal expansion creates ~1-6 gpm excess volume
3. CVCS pressure controller increases letdown to remove excess → **CORRECT**
4. Excess flows to VCT, VCT level rises slowly → **CORRECT**
5. At ~70%, LCV-112A divert valve opens proportionally → **CORRECT**
6. Diverted water enters **BRS recycle holdup tanks** → **NOT MODELLED**
7. VCT level stabilises near 70-72% with divert managing the excess → **PARTIALLY CORRECT** (stabilises but water vanishes)

### 3.2 Bubble Formation / RCP Start Phase (8 to ~10 hr)

1. Bubble forms, PZR level drops from 100% to ~60%
2. CVCS increases charging to restore level → VCT level drops
3. RCPs start → thermal transient → PZR level drops further
4. If VCT drops to makeup setpoint, **BRS processed water** returns to VCT → **NOT MODELLED** (currently draws from RWST)
5. VCT level should recover to normal band via BRS return flow

### 3.3 Full Heatup Phase (10 to ~14 hr)

1. 4 RCPs running, massive heat input (~21 MW RCP heat)
2. RCS heats at up to ~71°F/hr (within 100°F/hr tech spec limit — see Section 9.4)
3. Large thermal expansion → large surge flow (up to ~30 gpm)
4. PZR level rises rapidly as RCS water expands into PZR
5. CVCS reduces charging to minimum (seal injection only) to counteract
6. Excess letdown (75 gpm) minus charging (32 gpm) = 43 gpm net to VCT
7. Divert valve manages VCT level, excess goes to **BRS** → **NOT MODELLED**
8. Total excess volume during full heatup: **~30,000 gallons** (per NRC HRTD 19.0, already in PlantConstants as `HEATUP_EXCESS_VOLUME_GAL`)

The BRS holdup tanks have sufficient capacity for this — 56,000 gallons total
(Callaway FSAR baseline). The BRS processes this water in batches over days
through the boric acid evaporator and returns concentrated boric acid to the
BAT and clean distillate to the PWST for reuse.

---

## 4. Proposed Solution

### 4.1 New Module: BRSPhysics.cs

Create a new GOLD-standard physics module implementing a simplified BRS model.

**Scope:** Buffer tank system with inflow from VCT divert, batch processing via
evaporator, and return paths to BAT/PWST. Not a full radiochemical processing
simulation — just enough to close the inventory loop and provide realistic
mass conservation.

```
Module: BRSPhysics
File:   Assets/Scripts/Physics/BRSPhysics.cs
Type:   Static physics module (same pattern as VCTPhysics)
```

**State structure:**
```csharp
BRSState:
  // --- Holdup Tank State ---
  HoldupVolume_gal        // Current water volume in recycle holdup tanks
  HoldupBoronConc_ppm     // Boron concentration of holdup tank inventory
  HoldupBoronMass_lb      // Boron mass in holdup tanks

  // --- Evaporator Processing State ---
  ProcessingActive         // Bool — evaporator feed pump running
  EvaporatorFeedRate_gpm   // Current feed rate to evaporator (0 to 15 gpm)
  DistillateRate_gpm       // Clean water output rate (≈ feed rate)
  ConcentrateRate_gpm      // Concentrated boric acid output rate (small fraction)

  // --- Processed Inventory (Available for Return) ---
  DistillateAvailable_gal  // Clean water accumulated in monitor tanks
  ConcentrateAvailable_gal // Concentrated boric acid accumulated in BAT
  DistillateBoron_ppm      // Should be ≈ 0 ppm (clean condensate)
  ConcentrateBoron_ppm     // Should be ≈ 7000 ppm (4 wt% boric acid)

  // --- Flow Tracking ---
  InFlow_gpm               // Current inflow from VCT divert (LCV-112A)
  ReturnFlow_gpm           // Current return to VCT/BAT/PWST
  CumulativeIn_gal         // Total received from VCT divert
  CumulativeProcessed_gal  // Total processed through evaporator
  CumulativeDistillate_gal // Total clean water produced
  CumulativeConcentrate_gal// Total boric acid concentrate produced
  CumulativeReturned_gal   // Total returned to plant systems

  // --- Alarms ---
  HoldupHighLevel          // Holdup tanks approaching capacity
  HoldupLowLevel           // Holdup tanks near empty (processing starved)
```

**Key behaviours:**

1. **Receive:** Accept diverted letdown flow from LCV-112A. Track volume and
   boron concentration using mixing equation. Nitrogen cover gas displaced to
   waste gas decay tanks (not modelled — no radiological impact on hydraulics).

2. **Store:** Track volume and boron concentration in holdup tanks. Two tanks
   modelled as single lumped volume (recirculation pump maintains homogeneous
   concentration per NRC HRTD 4.1).

3. **Process (Batch Evaporation):** When holdup volume exceeds a minimum batch
   threshold (e.g., 5000 gal), evaporator feed pump starts. Feed flows through
   evaporator feed ion exchangers → filter → boric acid evaporator. Evaporator
   separates into:
   - **Distillate:** Clean condensate at ≈ 0 ppm boron → monitor tanks
   - **Concentrate:** Boric acid solution at ≈ 7000 ppm (4 wt%) → BAT

   Processing rate: 15 gpm continuous (per Callaway FSAR). Simplified as
   first-order throughput — no thermal dynamics of evaporator.

4. **Return (Makeup Source):** Processed distillate in monitor tanks is available
   as a makeup water source. When VCT auto-makeup triggers, BRS distillate is
   the first-priority source (before RMS external blending or RWST). Return
   flow rate limited to 35 gpm (matches existing AUTO_MAKEUP_FLOW_GPM).

5. **Capacity Limit:** If holdup tanks reach capacity (56,000 gal usable),
   the system can accept no further divert flow. LCV-112A must close, and
   VCT level will rise unchecked — alarm condition. In practice, this would
   never occur during a single heatup (~30,000 gal excess vs. 56,000 gal
   capacity), but the model should handle it for robustness.

**Simplified processing model rationale:**

The real BRS evaporator is a complex thermodynamic device (preheater, stripper
column, evaporator section, absorption tower, condenser, condensate
demineraliser, filter — per NRC HRTD 4.1 Figure 4.1-4). Modelling these
individual stages provides no benefit for the VCT level stability problem.
What matters is:
- Flow in (from VCT divert) — sets holdup tank fill rate
- Processing throughput (15 gpm) — sets how fast holdup drains
- Products (distillate + concentrate) — sets what's available for return
- Time delay — holdup tanks buffer the batch processing cycle

### 4.2 New Constants: PlantConstants.BRS.cs

Create a new PlantConstants partial for BRS parameters. All values sourced
from Callaway FSAR Chapter 11 (ML21195A182) as the baseline 4-loop
Westinghouse design, cross-referenced with NRC HRTD 4.1 (ML11223A214).

```csharp
// ===================================================================
// RECYCLE HOLDUP TANKS
// Source: Callaway FSAR Chapter 11 (ML21195A182), Figure 11.1A-2
//         NRC HRTD 4.1 Section 4.1.2.6 (ML11223A214)
// ===================================================================

/// Recycle holdup tank total capacity in gallons (2 tanks).
/// Source: Callaway FSAR Fig 11.1A-2 — 56,000 gallons total.
/// Catawba UFSAR Table 12-19 confirms similar sizing for 4-loop plant.
BRS_HOLDUP_TOTAL_CAPACITY_GAL = 56000f

/// Usable fraction of holdup tank capacity.
/// Source: Callaway FSAR — "0.8 usable" (80% of total, reserves for
/// nitrogen cover gas ullage and instrument tap margins).
BRS_HOLDUP_USABLE_FRACTION = 0.80f

/// Usable holdup capacity in gallons = 56,000 × 0.80 = 44,800 gal.
/// Derived from above two constants.
BRS_HOLDUP_USABLE_CAPACITY_GAL = 44800f

/// Number of recycle holdup tanks.
/// Source: NRC HRTD 4.1 — "recycle holdup tanks" (plural); Callaway
/// FSAR shows 2 tanks with recirculation pump to transfer between them.
BRS_HOLDUP_TANK_COUNT = 2

/// Holdup tank high-level alarm setpoint (% of usable capacity).
/// Plant-specific. Conservative default.
BRS_HOLDUP_HIGH_LEVEL_PCT = 90f

/// Holdup tank low-level alarm / processing stop setpoint (%).
/// Below this, evaporator feed pump trips to prevent cavitation.
BRS_HOLDUP_LOW_LEVEL_PCT = 10f

/// Minimum holdup volume to start evaporator batch (gallons).
/// Prevents cycling the evaporator on small volumes.
/// Approximate — operator-initiated in real plant.
BRS_EVAPORATOR_MIN_BATCH_GAL = 5000f

// ===================================================================
// BORIC ACID EVAPORATOR
// Source: Callaway FSAR Fig 11.1A-2 — 21,600 gpd processing rate
//         NRC HRTD 4.1 Section 4.1.2.6 — evaporator process description
//         NRC HRTD 15.1 Table 15.1-2 — waste evaporator 1-15 gpm range
// ===================================================================

/// Evaporator processing rate in gpm (continuous operation).
/// Source: Callaway FSAR — 21,600 gpd ÷ 1440 min/day = 15 gpm.
/// NRC HRTD 15.1 Table 15.1-2 confirms 1-15 gpm range for similar
/// Westinghouse 4-loop evaporators (Vogtle, Comanche Peak, McGuire).
BRS_EVAPORATOR_RATE_GPM = 15f

/// Evaporator processing rate in gallons per day.
/// Source: Callaway FSAR Figure 11.1A-2 — 21,600 gpd.
BRS_EVAPORATOR_RATE_GPD = 21600f

/// Time to process one full holdup tank batch at rated capacity (days).
/// Source: Callaway FSAR — Tp = 0.8 × 56,000 / 21,600 = 2.07 days.
BRS_BATCH_PROCESSING_TIME_DAYS = 2.07f

/// Time to fill holdup tanks at normal shim bleed rate (days).
/// Source: Callaway FSAR — Tc = 0.8 × 56,000 / 2,140 = 20.9 days.
/// Note: During heatup, fill rate is much faster (divert flow ≫ shim bleed).
BRS_NORMAL_COLLECTION_TIME_DAYS = 20.9f

/// Evaporator concentrate output boron concentration in ppm.
/// Source: NRC HRTD 4.1 — "concentrated to approximately 4 weight
/// percent (7000 ppm)" of boric acid solution.
BRS_CONCENTRATE_BORON_PPM = 7000f

/// Evaporator distillate output boron concentration in ppm.
/// Source: NRC HRTD 4.1 — condensate passes through demineraliser,
/// essentially pure water. Modelled as 0 ppm.
BRS_DISTILLATE_BORON_PPM = 0f

/// Fraction of evaporator feed that becomes distillate (mass basis).
/// For 2000 ppm input being concentrated to 7000 ppm:
/// Mass balance: F = D + C, F×Cf = D×0 + C×7000
/// C/F = Cf/7000, D/F = 1 - Cf/7000
/// At 2000 ppm input: D/F = 1 - 2000/7000 = 0.714 (71.4% distillate)
/// This is concentration-dependent; calculate dynamically in physics.
/// This constant is for reference only — actual split computed per-step.
BRS_DISTILLATE_FRACTION_REF = 0.714f

// ===================================================================
// MONITOR TANKS (Processed Water Storage)
// Source: Callaway FSAR Fig 11.1A-2 — 2 × monitor tanks
//         NRC HRTD 4.1 — monitor tanks sampled before discharge
// ===================================================================

/// Monitor tank capacity each in gallons.
/// Source: Callaway FSAR Figure 11.1A-2 — 100,000 gallons each.
/// NOTE: For simulation purposes, monitor tanks are modelled as a single
/// lumped "distillate available" volume. The 200,000 gal total capacity
/// far exceeds any single-heatup production, so tank limits are not
/// constraining. Simplified to a running total.
BRS_MONITOR_TANK_CAPACITY_GAL = 100000f
BRS_MONITOR_TANK_COUNT = 2

// ===================================================================
// RETURN FLOW PATHS
// Source: NRC HRTD 4.1 Section 4.1.2.6 — monitor tank discharge paths:
//         (1) Primary water storage tank (PWST)
//         (2) Lake/river discharge (environmental release — not modelled)
//         (3) Holdup tanks (reprocessing — not modelled as separate path)
//         (4) Evaporator condensate demineralisers (polishing — not modelled)
//         (5) Liquid waste system (not modelled)
//
// Concentrate return:
//         Boric acid at ~7000 ppm → concentrates filter → holding tank
//         → boric acid tanks (BAT) if specs met, else → holdup tanks
// ===================================================================

/// Maximum return flow rate from BRS to VCT/plant systems in gpm.
/// This is the flow rate at which processed distillate can be returned
/// to the VCT as makeup water. Matches existing AUTO_MAKEUP_FLOW_GPM
/// (35 gpm) since the RMS blending system is the common delivery path.
/// Source: Engineering judgement — limited by makeup piping/valve capacity.
BRS_RETURN_FLOW_MAX_GPM = 35f

// ===================================================================
// BORIC ACID TANKS (BAT) — Already partially defined in PlantConstants
// Source: NRC HRTD 4.1 — "Each boric acid tank... capacity of 24,228 gal"
//         "concentration of approximately 4 weight percent (7000 ppm)"
// ===================================================================

/// BAT capacity each in gallons.
/// Source: NRC HRTD 4.1 (ML11223A214)
BRS_BAT_CAPACITY_EACH_GAL = 24228f

/// Number of boric acid tanks.
/// Source: NRC HRTD 4.1 — "Two boric acid tanks"
BRS_BAT_COUNT = 2

/// BAT boron concentration in ppm (= BORIC_ACID_CONC, already defined).
/// Cross-reference: PlantConstants.BORIC_ACID_CONC = 7000f
/// No new constant needed — reference existing.

// ===================================================================
// PRIMARY WATER STORAGE TANK (PWST)
// Source: NRC HRTD 4.1 — "This 203,000-gal tank may be filled with
//         water from the plant secondary makeup system or with distillate
//         from the boric acid evaporators."
// ===================================================================

/// PWST capacity in gallons.
/// Source: NRC HRTD 4.1 (ML11223A214)
BRS_PWST_CAPACITY_GAL = 203000f

/// PWST boron concentration in ppm.
/// Demineralised water storage — essentially 0 ppm boron.
BRS_PWST_BORON_PPM = 0f

// ===================================================================
// LCV-112A DIVERT VALVE — Already defined in PlantConstants.CVCS.cs
// Source: NRC HRTD 4.1 Section 4.1.3.1
// Cross-references:
//   VCT_DIVERT_SETPOINT = 70f     (begin-divert level)
//   VCT_DIVERT_PROP_BAND = 20f    (proportional band)
//   VCT_LEVEL_HIGH = 73f          (high alarm → full divert)
// No new constants needed for divert valve — logic already in VCTPhysics.
// ===================================================================

// ===================================================================
// DECONTAMINATION FACTORS (for reference — not used in hydraulic model)
// Source: Callaway FSAR Chapter 11, Figure 11.1A-2
//   System DF with anion bed:
//     Iodine: 10^5, Cs/Rb: 2×10^3, Other: 10^4
//   System DF with mixed bed:
//     Iodine: 10^4, Cs/Rb: 2×10^4, Other: 10^5
// These are provided for future radiochemistry modelling if needed.
// ===================================================================
```

**Existing constants that support BRS (no changes needed):**

| Constant | Value | File | Used By BRS |
|----------|-------|------|-------------|
| `VCT_DIVERT_SETPOINT` | 70% | PlantConstants.CVCS.cs | LCV-112A begin-divert level |
| `VCT_DIVERT_PROP_BAND` | 20% | PlantConstants.CVCS.cs | Proportional valve band |
| `VCT_LEVEL_HIGH` | 73% | PlantConstants.CVCS.cs | Full-divert alarm level |
| `AUTO_MAKEUP_FLOW_GPM` | 35 gpm | PlantConstants.CVCS.cs | Return flow rate cap |
| `BORIC_ACID_CONC` | 7000 ppm | PlantConstants.CVCS.cs | BAT/concentrate reference |
| `BORON_RWST_PPM` | 2600 ppm | PlantConstants.cs | RWST backup source conc |
| `HEATUP_EXCESS_VOLUME_GAL` | 30000 gal | PlantConstants.CVCS.cs | Expected heatup divert volume |

### 4.3 Modifications to VCTPhysics.cs

**Change VCT divert destination:**
- Currently: `DivertFlow_gpm` is subtracted from VCT volume and lost to void
- Proposed: `DivertFlow_gpm` is subtracted from VCT **and the engine coordinates
  transfer to BRS inflow** — VCTPhysics itself does not reference BRSPhysics.
  The engine (HeatupSimEngine.CVCS.cs) reads `state.DivertFlow_gpm` after the
  VCT update and passes it to `BRSPhysics.Update()` as inflow. This preserves
  single-responsibility (G1) — VCTPhysics manages VCT, engine coordinates.

**Change VCT makeup source priority:**
- Currently: Auto-makeup always draws from RMS (infinite source) or RWST
- Proposed: `VCTPhysics.Update()` gains an optional parameter:
  `float brsDistillateAvailable_gal = 0f`
  When auto-makeup triggers and BRS distillate is available, the makeup source
  is flagged as BRS rather than RMS. The boron concentration of makeup water
  changes accordingly (BRS distillate ≈ 0 ppm vs. RMS blended ≈ RCS target ppm).

  New state field:
  ```csharp
  public bool MakeupFromBRS;  // True if current makeup is sourced from BRS
  ```

  Priority order in makeup logic:
  1. BRS distillate (if `brsDistillateAvailable_gal > 0`) — closed-loop reclaim
  2. RMS blending system (BAT + PWST) — normal external makeup (unchanged)
  3. RWST suction (emergency only, at low-low level — unchanged)

**No changes to divert valve logic.** The existing LCV-112A proportional model
in VCTPhysics.Update() is physically correct per NRC HRTD 4.1:
- Proportional opening above `VCT_DIVERT_SETPOINT` (70%)
- Ramp from 0% to 100% divert over `VCT_DIVERT_PROP_BAND` (20%)
- 3% hysteresis on closing
- Divert fraction applied to letdown flow

### 4.4 Modifications to HeatupSimEngine.CVCS.cs

**Add BRS coordination to `UpdateVCT()`:**
```
After VCTPhysics.Update():
  1. Read vctState.DivertFlow_gpm and vctState.DivertActive
  2. If divert active:
     a. Calculate divert volume this timestep = DivertFlow_gpm × dt/60
     b. Call BRSPhysics.ReceiveDivert(ref brsState, divertVolume, vctBoronConc, dt)
  3. Call BRSPhysics.UpdateProcessing(ref brsState, dt)
  4. If VCT auto-makeup active AND brsState.DistillateAvailable_gal > 0:
     a. Calculate makeup volume this timestep
     b. Call BRSPhysics.WithdrawDistillate(ref brsState, makeupVolume)
     c. Flag vctState.MakeupFromBRS = true
  5. Update brsState tracking totals
```

**Add BRS state variable to HeatupSimEngine.cs:**
```csharp
[HideInInspector] public BRSPhysics.BRSState brsState;
```

### 4.5 Modifications to HeatupSimEngine.Init.cs

**Initialise BRS state:**
```csharp
brsState = BRSPhysics.Initialize(rcsBoronConcentration_ppm);
```

BRS starts empty (0 gallons in holdup, 0 gallons processed) at cold shutdown.
Initial boron concentration set to match RCS so that any residual fluid in
piping is at equilibrium. No prior processed water available.

### 4.6 Modifications to HeatupSimEngine.Logging.cs

**Add BRS section to interval log:**
```
  BRS (Boron Recycle System):
    Holdup Volume:       XXXXX / 44800 gal  (XX.X%)
    Holdup Boron:        XXXX ppm
    Evaporator:          RUNNING / IDLE
    Evap Feed Rate:      XX.X gpm
    Distillate Avail:    XXXXX gal
    Concentrate Avail:   XXXXX gal
    Inflow (from VCT):   XX.X gpm
    Return (to VCT):     XX.X gpm
    Cumulative In:       XXXXX gal
    Cumulative Returned: XXXXX gal
```

**Add BRS to history tracking:**
- `brsHoldupHistory` list for graph display
- `brsDistillateHistory` list for processed water tracking

### 4.7 Enhanced Mass Conservation Check

**Add total-system inventory verification:**
```
Total inventory = RCS_water_volume_gal
                + PZR_water_volume_gal
                + VCT_volume_gal
                + BRS_holdup_volume_gal
                + BRS_distillate_available_gal
                + BRS_concentrate_available_gal

Expected change = RWST_additions - CBO_losses
                  (only true external boundary crossings)

Conservation error = |actual_total - (initial_total + expected_change)|
```

This replaces the current partial check with a comprehensive system-wide balance.
The BRS closes the loop so that divert flow is no longer an "external" loss.

---

## 5. BRS Technical Reference (NRC/FSAR Source Data)

### 5.1 BRS System Description

Source: NRC HRTD 4.1 (ML11223A214), Section 4.1.2.6

The Boron Recycle System collects excess borated water resulting from plant
operations that produce a high VCT level. These operations include:

1. **Dilution for core burnup compensation** — as fuel burns, boron is diluted
   to maintain criticality. Excess borated water diverted to BRS.
2. **Load follow operations** — boration/dilution for power changes.
3. **RCS heatup from cold shutdown to hot standby** — thermal expansion displaces
   ~30,000 gallons of excess water.
4. **Refueling operations** — large volume changes during cavity fill/drain.

### 5.2 BRS Flow Path (Real Plant)

Source: NRC HRTD 4.1 (ML11223A214), Section 4.1.2.6, Figure 4.1-3

```
VCT high level
    │
    ▼
LCV-112A (proportional 3-way divert valve)
    │
    ▼
Recycle Holdup Tanks (2 × ~28,000 gal = 56,000 gal total)
    │  ├── Nitrogen cover gas → Waste Gas Decay Tanks
    │  └── Recirculation pump (transfers between tanks for mixing)
    ▼
Evaporator Feed Pump
    │
    ▼
Evaporator Feed Ion Exchangers (pair — removes dissolved gases)
    │
    ▼
Evaporator Feed Filter
    │
    ▼
Boric Acid Evaporator
    ├── Preheater
    ├── Stripper Column (removes dissolved gases → Waste Gas System)
    ├── Evaporator Section (boils water, concentrates boric acid)
    ├── Absorption Tower (removes carryover, reflux to evaporator)
    └── Evaporator Condenser
            │
            ├─── DISTILLATE PATH ──────────────────────────┐
            │    Evaporator Condensate Demineraliser        │
            │    → Filter → Monitor Tanks (2 × 100,000 gal)│
            │              │                                │
            │              ▼ (sample, then discharge to:)   │
            │              ├── PWST (203,000 gal)           │
            │              ├── Lake/River Discharge          │
            │              ├── Holdup Tanks (reprocess)      │
            │              └── Liquid Waste System           │
            │                                               │
            └─── CONCENTRATE PATH ─────────────────────────┐
                 Concentrated boric acid (~4 wt% / 7000 ppm)
                 → Concentrates Filter → Holding Tank
                 → Sample
                 ├── If specs met: → Boric Acid Tanks (BAT)
                 └── If not: → Holdup Tanks (reprocess)
```

### 5.3 LCV-112A Divert Valve Behaviour

Source: NRC HRTD 4.1 (ML11223A214), Section 4.1.3.1

The letdown divert valve LCV-112A is a **three-way valve** controlled by VCT
level. An operator-adjustable setpoint is compared with actual level:

- **Below setpoint:** All letdown flows to VCT (no diversion)
- **Above setpoint:** Begins proportionally diverting letdown to BRS holdup
  tanks. As level error increases, diversion increases.
- **At high alarm:** Full diversion of all letdown to BRS (backup control via
  redundant VCT level transmitter)
- **Decreased VCT inflow** with normal charging outflow → VCT level drops →
  diversion decreases → level stabilises near setpoint

Manual override available from MCB: normal position or full-divert position.

**Current model status:** This proportional valve behaviour is already correctly
implemented in `VCTPhysics.Update()`. No changes needed to the valve logic.

### 5.4 Callaway FSAR BRS Design Parameters

Source: Callaway FSAR Chapter 11 (ML21195A182), Figure 11.1A-2

| Parameter | Value | Unit | Notes |
|-----------|-------|------|-------|
| Holdup tank total capacity | 56,000 | gal | 2 tanks |
| Holdup usable capacity | 44,800 | gal | 80% of total |
| Normal influent rate (shim bleed) | 1,840 | gpd | ~1.3 gpm avg |
| Evaporator processing rate | 21,600 | gpd | 15 gpm continuous |
| Normal collection time (Tc) | 20.9 | days | Fill time at normal shim rate |
| Batch processing time (Tp) | 2.07 | days | Drain time at 15 gpm |
| Monitor tank capacity | 100,000 | gal each | 2 tanks |
| Influent boron (typical) | ~2000 | ppm | Mode 5 RCS concentration |
| Distillate boron | ≈ 0 | ppm | After condensate demineraliser |
| Concentrate boron | ~7000 | ppm | 4 weight percent boric acid |

**Decontamination Factors (with anion bed):**
- Iodine: 10⁵
- Cs/Rb: 2×10³
- Other nuclides: 10⁴

### 5.5 Cross-Plant Comparison

BRS component sizes vary by plant. The following data points confirm that
Callaway values are representative of standard 4-loop Westinghouse designs:

| Plant | Type | Holdup (gal) | Evap Rate | Source |
|-------|------|-------------|-----------|--------|
| Callaway | W 4-Loop | 56,000 | 15 gpm | FSAR Ch.11 (ML21195A182) |
| Catawba | W 4-Loop | ~127,000* | — | UFSAR Table 12-19 (ML19189A302) |
| Vogtle 1&2 | W 4-Loop | — | 1-15 gpm | NRC HRTD 15.1 (ML11223A332) |
| Comanche Peak | W 4-Loop | — | 1-15 gpm | NRC HRTD 15.1 (ML11223A332) |
| McGuire | W 4-Loop | — | 1-15 gpm | NRC HRTD 15.1 (ML11223A332) |

\* Catawba value derived from shielding table dimensions (R=180", H=289"),
may include both units or represent a different tank configuration. Callaway
value (56,000 gal) is used as the baseline since it is directly documented
in gallons with usable fraction specified.

### 5.6 Related Component Data (From NRC HRTD 4.1)

| Component | Value | Source |
|-----------|-------|--------|
| Boric acid tanks (BAT) | 2 × 24,228 gal | NRC HRTD 4.1 |
| BAT concentration | 4 wt% (7000 ppm) | NRC HRTD 4.1 |
| BAT temperature | Room temp > 65°F | NRC HRTD 4.1 (prevent precipitation) |
| Primary Water Storage Tank | 203,000 gal | NRC HRTD 4.1 |
| RWST | ~450,000 gal | PlantConstants.cs |
| VCT | 4,000 gal | PlantConstants.CVCS.cs |
| VCT relief valve | 75 psig → holdup tanks | NRC HRTD 4.1 |
| Letdown orifices | 2 × 75 gpm + 1 × 45 gpm | NRC HRTD 4.1 |
| Ion exchanger admin limit | 127 gpm max | NRC HRTD 4.1 |

---

## 6. Files Affected

| File | Action | Description |
|------|--------|-------------|
| `BRSPhysics.cs` | **NEW** | BRS holdup tanks + evaporator processing physics |
| `PlantConstants.BRS.cs` | **NEW** | BRS system constants (sourced from FSAR) |
| `VCTPhysics.cs` | **MODIFY** | Add BRS-aware makeup source parameter + state field |
| `HeatupSimEngine.cs` | **MODIFY** | Add BRS state field |
| `HeatupSimEngine.CVCS.cs` | **MODIFY** | Coordinate VCT↔BRS flow transfers |
| `HeatupSimEngine.Init.cs` | **MODIFY** | Initialise BRS state |
| `HeatupSimEngine.Logging.cs` | **MODIFY** | Add BRS section to interval logs + history |
| `HeatupValidationVisual.cs` | **MODIFY** | Add BRS display (if space allows) |

---

## 7. GOLD Standard Compliance

### 7.1 New Module: BRSPhysics.cs

Must satisfy all G1-G10 criteria from GOLD_STANDARD_TEMPLATE.md:

- **G1:** Single responsibility — BRS holdup tank and evaporator processing physics
- **G2:** Full header with NRC HRTD 4.1 and Callaway FSAR sources, physics equations, units
- **G3:** N/A (physics module, not engine)
- **G4:** `BRSState` struct for all state (no hidden state, no side effects)
- **G5:** Constants from PlantConstants.BRS.cs only (no magic numbers)
- **G6:** All parameters cite NRC HRTD 4.1 (ML11223A214) or Callaway FSAR (ML21195A182)
- **G7:** `namespace Critical.Physics`
- **G8:** Target < 15 KB (simple module — receive, process, return)
- **G9:** No dead code
- **G10:** No duplication with VCTPhysics

### 7.2 New Constants: PlantConstants.BRS.cs

- **G1:** Single responsibility — BRS constants only
- **G2:** Full header with FSAR/HRTD source citations per constant
- **G5:** Self-contained, no cross-references except existing PlantConstants partials
- **G6:** Every constant has NRC document number and section citation
- **G7:** `namespace Critical.Physics`, partial class `PlantConstants`

### 7.3 Modified Modules

All modified GOLD modules (VCTPhysics, HeatupSimEngine partials) must retain
their GOLD certification. Changes must be reviewed against G1-G10 before
the work is considered complete.

---

## 8. Validation Criteria

### 8.1 VCT Level Stability

During solid plant heatup (0–8 hr), VCT level should:
- Start at 55% (cold shutdown initial condition)
- Rise gradually to ~70% as thermal expansion accumulates
- Stabilise near 70-72% with proportional divert managing excess
- **Never trigger HIGH level alarm (73%)** during steady-state heatup
- Remain within normal band (40-70%) for the majority of the simulation

### 8.2 BRS Inventory Tracking

- BRS holdup volume should increase during divert periods
- BRS should show processing activity (evaporator feed rate > 0 when holdup > min batch)
- BRS cumulative in should approximately equal VCT cumulative divert out
- BRS holdup should never exceed usable capacity (44,800 gal)
- Distillate should accumulate as evaporator runs
- Concentrate should accumulate and be tracked for BAT return

### 8.3 Mass Conservation

- Total system inventory (RCS + PZR + VCT + BRS holdup + BRS distillate + BRS
  concentrate) should remain constant within numerical tolerance (< 10 gal error),
  excluding true external boundary crossings (RWST additions, CBO losses)
- The existing VCT mass conservation check should continue to pass
- A new system-wide conservation check should be added and should pass

### 8.4 Transient Recovery

During bubble formation and RCP start transients:
- VCT should be able to draw from BRS distillate for auto-makeup recovery
- VCT level should recover to normal band without requiring RWST suction
  (unless BRS distillate is exhausted, which should not happen during a single heatup)
- The oscillation pattern (84% → 33% → 84%) should be significantly damped

### 8.5 Boron Tracking

- RCS boron concentration should remain stable during heatup (no unintended
  dilution from switching between BRS distillate and RWST water sources)
- BRS holdup boron should match diverted water concentration
- BRS distillate boron should be ≈ 0 ppm
- BRS concentrate boron should be ≈ 7000 ppm
- When BRS distillate (0 ppm) is used as makeup, VCT boron will decrease —
  this is physically correct (dilution), same as RMS blending on a real plant

### 8.6 Processing Rate Realism

- During the 12.5-hour heatup transient, the evaporator should process a
  small fraction of the total diverted volume:
  - Total divert ≈ 30,000 gal (heatup excess)
  - 12.5 hours × 15 gpm × 60 = 11,250 gal processed (if running continuously)
  - Remaining ≈ 18,750 gal still in holdup at end of simulation
- This is physically correct — the BRS batch-processes over 2+ days, not in
  real-time. The holdup tanks are the buffer.

---

## 9. Implementation Order

1. **PlantConstants.BRS.cs** — Define all BRS constants (no dependencies)
2. **BRSPhysics.cs** — New module with Initialize(), ReceiveDivert(), UpdateProcessing(), WithdrawDistillate()
3. **VCTPhysics.cs** — Add BRS-aware makeup source parameter + MakeupFromBRS state field
4. **HeatupSimEngine.cs** — Add brsState field
5. **HeatupSimEngine.Init.cs** — Initialise BRS
6. **HeatupSimEngine.CVCS.cs** — Wire VCT divert → BRS, BRS return → VCT makeup
7. **HeatupSimEngine.Logging.cs** — Add BRS logging section + history buffer
8. **Validation run** — Full heatup simulation, compare logs to v0.5.0 baseline
9. **CHANGELOG v0.6.0.md** — Document all changes with GOLD certification results

---

## 10. Risks and Considerations

### 10.1 BRS Processing Rate vs. Divert Rate

During peak thermal expansion (4 RCPs, T=12.5hr), the divert flow from VCT is
significant (up to ~40 gpm during transients). The BRS evaporator processing rate
(15 gpm per Callaway FSAR) is lower than peak divert inflow. This means holdup
tanks fill faster than they drain during transients. This is **physically correct**
— the BRS is designed with large holdup capacity (56,000 gal total, 44,800 usable)
precisely for this reason. Processing catches up over hours/days after the
transient. The model should reflect this — BRS is a buffer, not a real-time
processor.

Total heatup excess (~30,000 gal) is well within the 44,800 gal usable holdup
capacity, confirming the design is adequate for a single heatup.

### 10.2 Interaction with Solid Plant Pressure Controller

The SolidPlantPressure.cs CVCS controller adjusts letdown to maintain pressure.
Adding BRS does not change this controller — it only changes where the diverted
water goes after leaving the VCT. No changes needed to SolidPlantPressure.cs.

### 10.3 VCT Divert Valve Behaviour

The current proportional divert valve model (LCV-112A in VCTPhysics.cs) is
physically correct per NRC HRTD 4.1 and should be retained unchanged. The only
change is that the diverted flow now has a destination (BRS holdup tanks) instead
of disappearing into void.

### 10.4 Heatup Rate

The T=12.5hr log shows RCS heatup rate of 71.47°F/hr. Research into NRC Technical
Specifications and P-T curve requirements confirms:

- **100°F/hr** is the standard Tech Spec limit for normal heatup/cooldown
  (protect reactor vessel from thermal stress per ASME Section III Appendix G)
- **50°F/hr** is described in NRC HRTD 19.2.2 as the *expected* rate with all
  4 RCPs providing ~21 MW heat input. It is **not** a Tech Spec limit.
- **71°F/hr** is within the 100°F/hr Tech Spec limit and does not constitute a
  violation.

The previous plan (Section 9.4) incorrectly identified 71°F/hr as exceeding a
"50°F/hr tech spec limit." This has been corrected. The 71°F/hr rate is above
the ~50°F/hr expected value, which may indicate that thermal mass or heat loss
modelling could be tuned, but this is a calibration concern rather than a defect.

**Action:** No code changes for heatup rate in v0.6.0. If the rate needs
adjustment after BRS implementation, it can be addressed as a separate calibration
task in a future patch (thermal mass or heat loss tuning).

### 10.5 Charging Flow Physical Limit from VCT

A secondary consideration: should the charging pump flow be physically limited
when VCT volume approaches zero? Currently, VCT volume clamps at 0 but charging
continues at whatever the PI controller commands. In reality, the charging pump
would lose suction (cavitate) if VCT empties. The RWST suction swap at 5% level
prevents this in most cases, but it could be an issue during rapid transients.
This is a **nice-to-have** for v0.6.0, not a requirement.

### 10.6 BRS Distillate as Makeup — Boron Dilution Concern

When BRS distillate (≈ 0 ppm boron) is returned to the VCT as makeup water,
it will dilute the VCT boron concentration. This is physically correct — on a
real plant, the RMS blending system would blend BAT and PWST water to match the
target RCS boron concentration. In the simplified model, using pure distillate
as makeup is equivalent to a dilution event. For v0.6.0, this is acceptable
because:

1. During heatup, the RCS is subcritical with ~2000 ppm boron and large
   shutdown margin — minor dilution from makeup is not safety-significant
2. The VCT boron tracking already handles mixing from different-concentration
   sources (RWST path uses 2600 ppm, RMS uses blended concentration)
3. A future refinement could model the RMS blender explicitly, mixing BAT
   concentrate (7000 ppm) with PWST distillate (0 ppm) to match target conc.

---

## 11. Reference Documents

### 11.1 NRC Technical Documents (Publicly Available)

| Document | Title | Sections Used |
|----------|-------|---------------|
| ML11223A214 | NRC HRTD Section 4.1 — CVCS | 4.1.2.6 (BRS description), 4.1.3.1 (LCV-112A), Fig 4.1-3 (BRS flow), Fig 4.1-4 (evaporator) |
| ML11223A342 | NRC HRTD Section 19.0/19.2 — Plant Heatup | 19.0 (RHR letdown), 19.2.1 (solid plant), 19.2.2 (bubble formation) |
| ML11223A332 | NRC HRTD Section 15.1 — Liquid Waste | Table 15.1-2 (evaporator capacities, multi-plant comparison) |
| ML21195A182 | Callaway FSAR Chapter 11 — Radwaste | Fig 11.1A-2 (BRS flow rates, holdup tank capacity, processing rates, DFs) |
| ML19189A302 | Catawba UFSAR Chapter 9 — Auxiliary Systems | Table 9-24 (BRS component data), Table 12-19 (BRS component dimensions) |

### 11.2 Existing Project Constants

| File | Relevant Constants |
|------|-------------------|
| PlantConstants.CVCS.cs | VCT levels, divert setpoints, letdown/charging flows, BORIC_ACID_CONC |
| PlantConstants.cs | BORON_RWST_PPM, RCS water volume, RWST capacity |
| PlantConstants.Heatup.cs | MAX_RCS_HEATUP_RATE_F_HR (100°F/hr), TYPICAL_HEATUP_RATE_F_HR (50°F/hr) |

### 11.3 Existing Code (Current Model)

| File | Relevant Logic |
|------|---------------|
| VCTPhysics.cs | LCV-112A proportional divert valve, auto-makeup, RWST suction swap, boron tracking |
| HeatupSimEngine.CVCS.cs | VCT update coordination, charging/letdown flow management |
| SolidPlantPressure.cs | CVCS pressure controller (letdown adjustment for thermal expansion) |
