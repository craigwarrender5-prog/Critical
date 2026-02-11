# Volume Control Tank (VCT) Specification Document

## PWR Simulator - Physics Integration Requirement

**Document Version:** 1.0  
**Date:** February 2026  
**Status:** Specification for Future Implementation  
**Reference:** Westinghouse 4-Loop PWR (3411 MWt)

---

## 1. Executive Summary

### 1.1 The Problem

Our current CVCS model displays letdown and charging flows but **does not track where the water goes**. During cold shutdown validation testing, the model showed:

- Letdown: 75 gpm (water leaving RCS)
- Charging: 0 gpm (no water returning to RCS)
- **Net: -75 gpm from RCS**

Yet RCS inventory remained constant. This is physically impossible - the water must go somewhere. The missing component is the **Volume Control Tank (VCT)**.

### 1.2 Why It Matters

The VCT is the closed-loop buffer that:
- Receives all letdown flow
- Supplies all charging pump suction
- Maintains RCS inventory balance
- Enables boration/dilution operations
- Provides hydrogen addition to coolant

Without VCT modeling, our CVCS flows are cosmetic - they don't actually affect system mass balance. This breaks the physics chain:

```
RCS ←→ Pressurizer ←→ CVCS ←→ [VCT - MISSING] ←→ Makeup Systems
```

### 1.3 Solution Approach

Implement VCT as an **automatic subsystem** within the physics engine. The VCT will:
- Track water inventory (level)
- Enforce mass conservation between letdown and charging
- Automatically manage overflow (divert to BRS) and underflow (makeup from RWST)
- Integrate with existing CoupledThermo and pressurizer level control

---

## 2. VCT System Description

### 2.1 Function

The Volume Control Tank (VCT) is a stainless steel tank that serves as the central reservoir for the Chemical and Volume Control System (CVCS). It provides:

1. **Suction source for charging pumps** - Provides adequate NPSH and stable suction
2. **Surge capacity** - Absorbs letdown/charging imbalances
3. **Gas/liquid interface** - Hydrogen overpressure dissolves into coolant for oxygen scavenging
4. **Chemistry interface** - Receives purified letdown, supplies charging

### 2.2 Physical Location in CVCS Flow Path

```
RCS Cold Leg (Loop 3)
        │
        ▼ Letdown (75 gpm normal)
┌───────────────────┐
│ Regenerative HX   │ (cools letdown, heats charging)
└───────────────────┘
        │
        ▼
┌───────────────────┐
│ Letdown Orifices  │ (pressure reduction)
└───────────────────┘
        │
        ▼
┌───────────────────┐
│ Letdown HX        │ (final cooling via CCW)
└───────────────────┘
        │
        ▼
┌───────────────────┐
│ Ion Exchangers    │ (purification)
└───────────────────┘
        │
        ▼
┌───────────────────┐
│ Letdown Filter    │
└───────────────────┘
        │
        ▼ LCV-112A (divert valve)
        │
   ┌────┴────┐
   │         │
   ▼         ▼
[VCT]    [BRS Holdup Tanks]
   │      (on high VCT level)
   │
   ▼ LCV-112B/C (outlet valves)
   │
┌───────────────────┐
│ Charging Pumps    │ ◄── Alternate: RWST (on low-low VCT level)
└───────────────────┘
        │
        ▼
   ┌────┴────┐
   │         │
   ▼         ▼
[RCP Seals] [Normal Charging]
 32 gpm      55 gpm
   │         │
   └────┬────┘
        │
        ▼
┌───────────────────┐
│ Regenerative HX   │
└───────────────────┘
        │
        ▼
    RCS Cold Leg
```

### 2.3 Design Parameters

| Parameter | Value | Notes |
|-----------|-------|-------|
| Total Capacity | ~400-600 ft³ (~3,000-4,500 gal) | Plant-specific |
| Design Pressure | 75 psig | Relief valve setpoint |
| Operating Pressure | 15-30 psig | Hydrogen overpressure |
| Normal Level | 40-70% | Operator-adjustable setpoint |
| Material | Stainless Steel | Corrosion resistance |
| Cover Gas | Hydrogen (N₂ during shutdown) | O₂ scavenging |

---

## 3. VCT Level Control Logic

### 3.1 Level Setpoints and Automatic Actions

The VCT has multiple level setpoints that trigger automatic actions:

| Level | Setpoint | Action | Purpose |
|-------|----------|--------|---------|
| **High-High** | ~90% | Alarm | Operator alert |
| **High** | ~73% | LCV-112A diverts to BRS | Prevent overflow |
| **Begin Divert** | ~65% | LCV-112A proportional | Smooth control |
| **Normal Band** | 40-65% | All flow to VCT | Normal operation |
| **Auto Makeup Start** | ~25% | Reactor Makeup System starts | Restore level |
| **Auto Makeup Stop** | ~50% | Reactor Makeup System stops | Level restored |
| **Low** | ~17% | LCV-112B/C close (VCT isolated) | Protect pumps |
| **Low-Low** | ~5% | LCV-112D/E open (RWST suction) | Emergency supply |

### 3.2 Flow Balance Equation

The VCT level rate of change is:

```
dV_vct/dt = Q_letdown + Q_seal_return + Q_makeup - Q_charging - Q_divert
```

Where:
- `Q_letdown` = Letdown flow into VCT (gpm)
- `Q_seal_return` = RCP seal return flow (gpm) - normally ~12 gpm total
- `Q_makeup` = Automatic makeup flow (gpm) - when level is low
- `Q_charging` = Charging pump suction (gpm)
- `Q_divert` = Flow diverted to BRS (gpm) - when level is high

### 3.3 Normal Flow Balance

**At Power (Normal Operation):**

| Flow | Value | Direction |
|------|-------|-----------|
| Letdown | 75 gpm | INTO VCT |
| Seal Return | 12 gpm | INTO VCT |
| **Total IN** | **87 gpm** | |
| Charging (seals) | 32 gpm | OUT of VCT |
| Charging (normal) | 55 gpm | OUT of VCT |
| **Total OUT** | **87 gpm** | |
| **Net** | **0 gpm** | Balanced |

*Note: Small imbalance (~1 gpm) due to controlled bleed-off requires periodic makeup*

**Cold Shutdown (Solid Pressurizer):**

| Flow | Value | Direction |
|------|-------|-----------|
| Letdown via RHR | 75 gpm | INTO VCT |
| Seal Return | 0 gpm | RCPs secured |
| **Total IN** | **75 gpm** | |
| Charging | 0-75 gpm | Depends on PZR level |
| **Total OUT** | **0-75 gpm** | |
| **Net** | **Variable** | Must be managed |

---

## 4. Operating Mode Configurations

### 4.1 Mode 5 - Cold Shutdown (Solid Pressurizer)

**Initial Condition:**
- Pressurizer: 100% level (solid, water-filled)
- RCPs: Secured (no seal injection needed)
- RHR: Operating for decay heat removal
- Letdown: 75 gpm via HCV-128 (RHR-to-CVCS crossconnect)

**CVCS Configuration:**
```
Letdown:  75 gpm (via RHR crossconnect for purification)
Charging: 0 gpm (PZR at 100%, no inventory addition needed)
Net:      +75 gpm INTO VCT
```

**VCT Response:**
- Level rises at ~75 gpm
- At High Level setpoint → LCV-112A diverts to BRS
- System reaches equilibrium with all letdown diverted

**Alternative - Balanced Purification:**
```
Letdown:  75 gpm
Charging: 75 gpm (recirculating for purification only)
Net:      0 gpm (balanced)
```

This maintains stable VCT level while still achieving purification.

### 4.2 Mode 5 - Cold Shutdown (Bubble Formation)

**Trigger:** Pressurizer heaters energized, level drops below 80%

**CVCS Configuration:**
```
Letdown:  75 gpm (continuous)
Charging: Started to maintain PZR level constant
Net:      Controlled by PZR level control
```

**VCT Response:**
- Charging increases to compensate for steam formation
- VCT level decreases as charging outflow increases
- Auto makeup may activate if level drops too low

### 4.3 Modes 1-2 - Power Operation

**CVCS Configuration:**
```
Letdown:  75 gpm (constant for purification)
Charging: 87 gpm (55 normal + 32 seal injection)
Seal Return: 12 gpm (to VCT or charging pump suction)
Net:      ~0 gpm (small loss to bleed-off)
```

**VCT Response:**
- Level slowly decreases due to bleed-off losses
- Auto makeup periodically restores level
- Level band: typically 40-70%

---

## 5. Integration with Existing Physics Modules

### 5.1 Interface Points

The VCT module must interface with:

| Module | Interface | Data Exchange |
|--------|-----------|---------------|
| **CoupledThermo** | Mass conservation | Net flow affects RCS inventory |
| **PressurizerPhysics** | Level control | Charging flow responds to PZR level |
| **CVCS (current)** | Flow values | Letdown/charging flow rates |
| **RHR** | Cold shutdown | Letdown via HCV-128 crossconnect |
| **Makeup System** | Level control | Auto makeup on low level |

### 5.2 Mass Conservation Chain

```
┌─────────────────────────────────────────────────────────────┐
│                    MASS CONSERVATION                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  RCS Mass Change = Charging_in - Letdown_out - Leakage     │
│                                                             │
│  VCT Mass Change = Letdown_in + SealReturn + Makeup        │
│                    - Charging_out - Divert                  │
│                                                             │
│  Constraint: Sum of all mass changes = 0 (closed system)   │
│              OR = External_makeup - External_losses         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 5.3 Required State Variables

The VCT module needs to track:

```csharp
// VCT State
float vctLevel_percent;        // 0-100%
float vctVolume_gal;           // Current water volume
float vctVolume_max_gal;       // Tank capacity (~4000 gal)

// Flow rates (gpm)
float letdownFlow_gpm;         // INTO VCT from CVCS
float sealReturnFlow_gpm;      // INTO VCT from RCP seals
float makeupFlow_gpm;          // INTO VCT from makeup system
float chargingFlow_gpm;        // OUT of VCT to charging pumps
float divertFlow_gpm;          // OUT of VCT to BRS (high level)

// Automatic control states
bool autoMakeupActive;         // Makeup system running
bool divertActive;             // Diverting to BRS
bool rwstSuctionActive;        // Emergency RWST suction
```

---

## 6. Automatic Operation Logic

### 6.1 For Initial Implementation

The VCT will operate automatically without operator intervention. The physics engine will:

1. **Track VCT level** based on flow imbalance
2. **Auto-balance flows** during steady-state to maintain stable level
3. **Divert excess** to BRS when level exceeds setpoint
4. **Activate makeup** when level drops below setpoint
5. **Swap to RWST** on low-low level (emergency)

### 6.2 Simplified Automatic Mode

For the initial physics implementation, use this simplified logic:

```
IF operating_mode == COLD_SHUTDOWN AND pressurizer_solid:
    IF purification_only:
        # Balance flows for stable VCT
        charging_flow = letdown_flow
        vct_level = CONSTANT
    ELSE:
        # Bubble formation - PZR level control active
        charging_flow = f(pzr_level_error)
        vct_level = f(letdown - charging)
        
ELSE IF operating_mode == POWER_OPERATION:
    # Normal flow balance with PZR level control
    letdown_flow = 75 gpm (constant)
    charging_flow = f(pzr_level_program)
    vct_level = f(flow_imbalance)
    
# Level control automation
IF vct_level > HIGH_SETPOINT:
    divert_to_brs = letdown_flow  # All letdown to BRS
ELSE IF vct_level < LOW_SETPOINT:
    activate_makeup()
ELSE IF vct_level < LOWLOW_SETPOINT:
    swap_to_rwst_suction()
```

### 6.3 Mass Conservation Enforcement

**Critical:** The physics engine must enforce:

```
RCS_mass_change = charging_to_rcs - letdown_from_rcs

# This MUST equal:
VCT_mass_change = letdown_to_vct - charging_from_vct + makeup - divert

# Conservation check:
ABS(RCS_mass_change + VCT_mass_change - external_flows) < TOLERANCE
```

---

## 7. Validation Criteria

### 7.1 Steady-State Tests

| Test | Condition | Expected Result |
|------|-----------|-----------------|
| VCT-01 | Balanced flows (75/75) | VCT level stable |
| VCT-02 | Letdown > Charging | VCT level rises |
| VCT-03 | Letdown < Charging | VCT level falls |
| VCT-04 | High level reached | Divert activates |
| VCT-05 | Low level reached | Makeup activates |

### 7.2 Transient Tests

| Test | Condition | Expected Result |
|------|-----------|-----------------|
| VCT-06 | Cold shutdown purification | Flows balanced, level stable |
| VCT-07 | Bubble formation start | Charging starts, VCT draws down |
| VCT-08 | Heatup expansion | Letdown increases, VCT level rises |
| VCT-09 | Mass conservation check | Error < 0.1% over transient |

### 7.3 Integration Tests

| Test | Condition | Expected Result |
|------|-----------|-----------------|
| VCT-10 | PZR level low | Charging increases, VCT draws down |
| VCT-11 | PZR level high | Charging decreases, VCT fills |
| VCT-12 | RCS heatup 100→350°F | Proper expansion accommodation |

---

## 8. Implementation Phases

### Phase 1: Physics Constants (Current)
- Define VCT capacity, setpoints, flow limits
- Add to PlantConstants.cs

### Phase 2: State Tracking (Next)
- Add VCT level as tracked state variable
- Implement flow balance equation
- Enforce mass conservation

### Phase 3: Automatic Control (Future)
- Implement level control logic
- Add divert/makeup automation
- Integrate with PZR level control

### Phase 4: Operator Interface (Later)
- Manual override capability
- VCT level indication
- Flow rate displays
- Alarm annunciation

---

## 9. Summary

The VCT is a critical missing component in our CVCS model. Without it:

- **Mass is not conserved** - water appears/disappears without tracking
- **Flow displays are cosmetic** - they don't affect the physics
- **Startup scenarios are incomplete** - can't properly model inventory management

With VCT implementation:

- **Closed mass balance** - every gallon is tracked
- **Realistic CVCS operation** - flows have consequences
- **Proper transient response** - heatup/cooldown inventory changes work correctly
- **Foundation for boration/dilution** - future chemistry control

The VCT should be implemented as an automatic subsystem in the physics engine, with manual override capability added later for operator training scenarios.

---

## 10. Boron Concentration Tracking

### 10.1 Why Boron Tracking is Critical

Boron-10 is a strong neutron absorber used as the primary chemical shim for reactivity control. Without boron tracking:

- Cannot model reactor startup (criticality approach)
- Cannot model shutdown margin verification
- MTC calculations are incomplete (MTC varies with boron!)
- Xenon transient compensation impossible
- Load following operations cannot be simulated

**Boron is the bridge between CVCS operations and reactor physics.**

### 10.2 Effect on Fluid Properties

| Property | Effect of Boron (0-2000 ppm) | Impact on T-H Model |
|----------|------------------------------|---------------------|
| Density | +0.12% at 2000 ppm | **Negligible** |
| Specific Heat | <0.1% change | **Negligible** |
| Thermal Conductivity | <0.1% change | **Negligible** |
| Viscosity | ~1% increase | **Negligible** |
| Saturation Temperature | No change | **None** |

**Conclusion: No changes needed to WaterProperties.cs or thermal-hydraulic calculations.**

Boron's effect on fluid properties is within calculation tolerances. The existing Phase 1 thermal-hydraulics model remains valid regardless of boron concentration.

### 10.3 Effect on Reactor Physics (CRITICAL)

| Parameter | Boron Effect | Magnitude |
|-----------|--------------|-----------|
| Reactivity | Δρ = -Worth × Δppm | 8-10 pcm/ppm |
| MTC | Varies with [B] | +5 to -70 pcm/°C |
| Shutdown Margin | Determines subcriticality | ~2000 ppm required cold |
| Critical Boron | Determines criticality point | 0-1500 ppm depending on burnup |

### 10.4 Boron Concentration Reference Values

From PWR Master Development Plan v5:

| Condition | Boron (ppm) | Notes |
|-----------|-------------|-------|
| Cold Shutdown (BOL) | ~2000 | Maximum shutdown margin |
| HZP Critical (BOL) | ~1500 | Just critical, zero power |
| HFP (BOL) | ~1200 | Full power, equilibrium Xe |
| HFP (MOL) | ~600 | Mid-cycle |
| HFP (EOL) | <100 | Near zero, maximum burnup |
| RWST | 2300-2700 | Emergency injection source |
| Boric Acid Tanks | ~7000 | Concentrated makeup source |

### 10.5 Boron Worth Variation

| Core Life | Boron Worth | Primary Reason |
|-----------|-------------|----------------|
| BOL | ~10 pcm/ppm | Fresh fuel, high U-235 |
| EOL | ~8 pcm/ppm | Pu-240 buildup, spectrum hardening |

### 10.6 MTC Dependence on Boron (Critical for Phase 2)

The Moderator Temperature Coefficient varies **strongly** with boron concentration:

```
MTC = MTC_base + MTC_boron_coefficient × [Boron]
```

| Boron (ppm) | MTC at 200°F | MTC at 550°F | Notes |
|-------------|--------------|--------------|-------|
| 1500 (BOL) | +5 pcm/°F | -10 pcm/°F | Slightly positive at low T! |
| 1000 | -5 pcm/°F | -20 pcm/°F | |
| 500 | -15 pcm/°F | -30 pcm/°F | |
| 100 (EOL) | -25 pcm/°F | -45 pcm/°F | Strongly negative |

**This is why reactor behavior changes through the fuel cycle!**

At BOL with high boron:
- Positive MTC at low temperature requires careful startup procedures
- Tech Specs limit power until MTC becomes negative

At EOL with low boron:
- Strongly negative MTC provides robust inherent safety
- Load following is easier (more stable feedback)

### 10.7 Boron Tracking State Variables

The following must be tracked for each location:

```csharp
// Boron concentrations (ppm)
float boronConc_RCS_ppm;           // Primary system concentration
float boronConc_VCT_ppm;           // VCT concentration
float boronConc_charging_ppm;      // What's being injected
float boronConc_letdown_ppm;       // What's being removed (= RCS)

// Source concentrations (constants or slowly varying)
float boronConc_BAT_ppm = 7000f;   // Boric Acid Tanks
float boronConc_RWST_ppm = 2500f;  // Refueling Water Storage Tank
float boronConc_makeup_ppm;        // Blended makeup concentration

// Derived values
float boronWorth_pcm_per_ppm;      // Varies with burnup
float reactivity_boron_pcm;        // Contribution to total reactivity
```

### 10.8 Boron Mass Balance Equation

Boron concentration changes through dilution/concentration:

```
d([B]_RCS × M_RCS)/dt = [B]_charging × Q_charging - [B]_RCS × Q_letdown

Simplified (assuming M_RCS constant):

d[B]_RCS/dt = (Q_charging/V_RCS) × ([B]_charging - [B]_RCS)
```

Where:
- `[B]` = Boron concentration (ppm)
- `M_RCS` = RCS water mass (lb)
- `V_RCS` = RCS water volume (gal)
- `Q` = Flow rate (gpm)

**Key insight:** Boron changes are SLOW due to the large RCS volume (~86,000 gallons). At 75 gpm letdown/charging, complete turnover takes ~19 hours.

### 10.9 Boration/Dilution Operations

**Boration (increasing [B]):**
```
Source: Boric Acid Tanks (7000 ppm)
Path: BAT → Blender → VCT or Charging Pump Suction → RCS
Rate: Limited by blender capacity (~10 gpm boric acid)
Time: ~30 min to add 100 ppm to RCS
```

**Dilution (decreasing [B]):**
```
Source: Primary Water Storage Tank (0 ppm)
Path: PWST → Blender → VCT → Charging → RCS
      (Letdown removes borated water to BRS)
Rate: Limited by letdown capacity (75-120 gpm)
Time: ~2-4 hours to remove 100 ppm from RCS
```

**Emergency Boration:**
```
Source: Boric Acid Tanks via emergency path
Path: BAT → Emergency Boration Valve → Charging Pump Suction → RCS
Rate: Full charging pump capacity with concentrated boric acid
Use: ATWS, stuck rod, inadvertent dilution
```

### 10.10 VCT Role in Boron Control

The VCT is the **mixing point** for boron control:

```
                    ┌─────────────────┐
Letdown ──────────►│                 │
([B] = RCS conc)   │                 │
                   │      VCT        │──────► Charging
Makeup ───────────►│   (mixing)      │        ([B] = VCT conc)
([B] = blended)    │                 │
                   └─────────────────┘
                          │
                          ▼
                   To BRS (divert)
```

VCT concentration depends on:
- Incoming letdown concentration (= RCS)
- Incoming makeup concentration (0 to 7000 ppm, blended)
- Mixing dynamics in tank

For most operations, VCT concentration ≈ RCS concentration (letdown dominates).

### 10.11 Integration with Phase 2 Reactor Physics

**Required coupling for reactor model:**

```
┌────────────────────────────────────────────────────────────────┐
│                     CVCS/Boron Tracking                        │
│  [B]_RCS updated based on charging/letdown flow and conc.     │
└────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────┐
│                     Reactivity Calculation                      │
│  ρ_boron = -BoronWorth × [B]_RCS                               │
│  MTC = f([B]_RCS, T_moderator)                                 │
│  ρ_total = ρ_boron + ρ_Doppler + ρ_MTC + ρ_rods + ρ_Xe + ...  │
└────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────┐
│                     Point Kinetics                              │
│  dn/dt = (ρ - β)/Λ × n + Σλᵢcᵢ                                │
│  Power = n × (Energy per fission)                              │
└────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────┐
│                     Thermal-Hydraulics                          │
│  Q = Power × (1 - losses)                                      │
│  T_fuel, T_moderator updated                                   │
│  (Feeds back to MTC, Doppler)                                  │
└────────────────────────────────────────────────────────────────┘
```

### 10.12 Boron Validation Criteria

| Test | Condition | Expected Result |
|------|-----------|-----------------|
| B-01 | Steady state, no makeup | [B] constant |
| B-02 | Dilution at 75 gpm, 0 ppm makeup | [B] decreases ~4 ppm/hr |
| B-03 | Boration at 10 gpm, 7000 ppm | [B] increases ~50 ppm/hr |
| B-04 | Emergency boration | [B] increases rapidly |
| B-05 | Mass balance check | Boron mass conserved ±0.1% |

---

## 11. Dashboard Display Requirements

### 11.1 VCT Panel

| Parameter | Display | Units | Normal Range |
|-----------|---------|-------|--------------|
| VCT Level | Bar graph + digital | % | 40-70% |
| VCT Pressure | Digital | psig | 15-30 |
| VCT Temperature | Digital | °F | 70-120 |
| Letdown Flow | Digital | gpm | 0-120 |
| Charging Flow | Digital | gpm | 0-150 |
| Net Flow | Digital + trend | gpm | ±10 |
| Divert Status | Indicator | ON/OFF | OFF |
| Makeup Status | Indicator | ON/OFF | OFF |
| RWST Suction | Indicator | ON/OFF | OFF |

### 11.2 Boron Tracking Panel

| Parameter | Display | Units | Typical Range |
|-----------|---------|-------|---------------|
| RCS Boron | Large digital | ppm | 0-2000 |
| VCT Boron | Digital | ppm | 0-2000 |
| Charging Boron | Digital | ppm | 0-7000 |
| BAT Level | Bar graph | % | 50-100% |
| Boration Rate | Digital | ppm/hr | 0-200 |
| Dilution Rate | Digital | ppm/hr | 0-50 |
| Boron Worth | Digital | pcm/ppm | 8-10 |
| ρ_boron | Digital | pcm | -20000 to 0 |

### 11.3 Alarm Setpoints

| Alarm | Setpoint | Priority |
|-------|----------|----------|
| VCT Level High-High | 90% | WARNING |
| VCT Level High | 73% | ADVISORY |
| VCT Level Low | 25% | WARNING |
| VCT Level Low-Low | 17% | ALARM |
| VCT Pressure High | 70 psig | WARNING |
| VCT Pressure Low | 10 psig | WARNING |
| Boron Conc Dilution | Tech Spec limit | ALARM |
| Charging/Letdown Mismatch | >20 gpm | ADVISORY |

### 11.4 Trend Displays

The following should have trend capability (strip chart or graph):

1. VCT Level vs Time
2. RCS Boron Concentration vs Time
3. Charging Flow vs Letdown Flow
4. Net CVCS Flow vs Time

---

## 12. Implementation Priority

### Immediate (Phase 1 Completion)
1. ✅ Thermal-hydraulics (complete)
2. ✅ Pressurizer physics (complete)
3. ⬜ VCT level tracking (this document)
4. ⬜ CVCS flow balance enforcement

### Phase 2 Prerequisites
1. ⬜ Boron concentration tracking
2. ⬜ Boron mass balance
3. ⬜ MTC = f(boron, temperature)
4. ⬜ Boron reactivity worth

### Phase 2 Reactor Physics
1. ⬜ Point kinetics with delayed neutrons
2. ⬜ Doppler feedback
3. ⬜ Full MTC feedback
4. ⬜ Control rod worth
5. ⬜ Xenon/Samarium tracking

---

## 13. Summary of Missing Components

| Component | Current State | Impact | Priority |
|-----------|---------------|--------|----------|
| **VCT Level** | Not modeled | Mass not conserved | HIGH |
| **VCT Pressure** | Not modeled | NPSH not verified | MEDIUM |
| **Boron [RCS]** | Not modeled | No reactivity calc | HIGH (Phase 2) |
| **Boron [VCT]** | Not modeled | No boration/dilution | HIGH (Phase 2) |
| **MTC(boron)** | Not modeled | Incomplete feedback | HIGH (Phase 2) |
| **Boron Worth** | Constant only | Burnup not tracked | MEDIUM |

### Key Physics Gaps Addressed by This Document:

1. **Gap #14: VCT inventory tracking** - Water mass balance incomplete
2. **Gap #15: Boron concentration tracking** - Required for Phase 2 reactor physics
3. **Gap #16: MTC boron dependence** - Feedback coefficient varies with chemistry
4. **Gap #17: Boration/dilution dynamics** - Cannot model reactivity control operations

---

## References

1. NRC Westinghouse Technology Manual, Section 4.1 - Chemical and Volume Control System (ML11223A214)
2. NRC Westinghouse Technology Manual, Section 19.0 - Plant Operations (ML11223A342)
3. NRC CE CVCS Training Materials (ML11251A019)
4. PWR Master Development Plan v5.0

---

*Document prepared for PWR Simulator development*  
*Physics validation required before implementation*
