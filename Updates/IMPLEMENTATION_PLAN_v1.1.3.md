# Implementation Plan v1.1.3 — SG Secondary Thermal Mass Participation Model

## Version: 1.1.3
## Date: 2026-02-09
## Status: DRAFT - AWAITING APPROVAL

---

## Problem Summary

### Observed Behavior (from heatup log at T=20 hours)
| Parameter | Value | Expected |
|-----------|-------|----------|
| Heatup rate | 23.81°F/hr | 45-55°F/hr |
| T_RCS | 344.77°F | - |
| T_SG_Secondary | 339.41°F | ~310-320°F |
| T_RCS - T_SG (gap) | 5.36°F | 15-30°F |
| Heat to SG | 14.53 MW | 4-6 MW |
| Net Heat to RCS | 7.44 MW | ~15-17 MW |

### Previous Fixes Applied
1. **v1.1.1**: Temperature-dependent HTC scaling (Churchill-Chu) ✓
2. **v1.1.2**: Boundary layer thermal stratification factor ✓

Both fixes are correctly implemented and working as designed. However, **the heatup rate is still only ~24°F/hr instead of 50°F/hr**.

### Root Cause Analysis

The fundamental problem is that the **SG secondary temperature is tracking too closely to the RCS temperature**:

| Time (hr) | T_RCS (°F) | T_SG (°F) | Gap (°F) | SG Heat Rate (°F/hr) | RCS Heat Rate (°F/hr) |
|-----------|------------|-----------|----------|----------------------|----------------------|
| 10.0 | 101.46 | 100.10 | 1.35 | ~1.5 | 7.92 |
| 15.0 | 223.91 | 211.62 | 12.29 | ~22 | 24.19 |
| 20.0 | 344.77 | 339.41 | 5.36 | ~25 | 23.81 |
| 22.25 | 397.87 | 393.88 | 3.99 | ~24 | 23.37 |

**Key Observations:**
1. The temperature gap CLOSES over time (from 12°F at 15hr to 4°F at 22hr)
2. SG secondary heating rate (~25°F/hr) is almost equal to RCS rate (~24°F/hr)
3. At this rate, SG cannot act as a proper heat sink

### Why This Happens

The current model assumes:
- All 1,660,000 lb of SG secondary water + 800,000 lb of metal participates uniformly in heat transfer
- SG heat capacity ≈ 2.0 MBTU/°F
- With 50 MBTU/hr heat input: ΔT/Δt = 50/2.0 = **25°F/hr**

**This is physically unrealistic.** In reality:

1. **Thermal Stratification**: Only the water near the tube bundle heats up quickly. Water at the bottom and around the shell periphery remains much colder.

2. **Metal Mass Distribution**: The 800,000 lb of SG metal includes:
   - Tube bundle: ~300,000 lb (heats with water)
   - Shell/wrapper: ~300,000 lb (heats slowly from inside out)  
   - Support structures: ~200,000 lb (minimal contact with hot water)

3. **Lumped Parameter Limitation**: The current model treats the entire secondary as a single lumped mass at uniform temperature. Reality has a gradient.

### The Physics We Should Model

**Effective Thermal Mass Participation**:
- At low temperatures (cold startup): Only ~30-40% of the total SG secondary thermal mass participates effectively
- As temperature rises and natural convection improves: Participation increases
- At steaming conditions: ~90-100% participation due to vigorous boiling circulation

This is analogous to how we model boundary layer ΔT reduction - the same physical phenomenon (stratification) also reduces effective thermal mass participation.

---

## Expectations (Correct Realistic Behavior)

| Parameter | Current | Target | Basis |
|-----------|---------|--------|-------|
| Heatup rate (4 RCPs) | 24°F/hr | 45-55°F/hr | NRC HRTD 19.2.2 |
| SG heat absorption | 14.5 MW | 4-7 MW | Heat balance |
| SG-RCS temperature gap | 4-6°F | 15-30°F | Thermal lag |
| Time to HZP | >24 hr | 17-20 hr | Industry typical |
| SG heating rate | 25°F/hr | 8-15°F/hr | Must be slower than RCS |

### Validation Calculation

With 35% thermal mass participation at low temperature:
```
Effective SG heat capacity = 2.0 MBTU/°F × 0.35 = 0.7 MBTU/°F
SG temperature rise = 50 MBTU/hr / 0.7 MBTU/°F = ~71°F/hr (from heat absorbed)
But boundary layer factor limits heat absorbed to: 50 × 0.3 = 15 MBTU/hr
Actual SG rise = 15 / 0.7 = ~21°F/hr

Wait - this still gives fast SG heating...
```

Actually, the real solution is that **less heat should be transferred to the SG in the first place**. Let me recalculate:

With proper modeling:
- Target net heat to RCS: 15-17 MW
- RCP input: 21 MW
- Insulation loss: ~1 MW
- **Target SG absorption: 21 - 17 - 1 = 3-4 MW**

Current SG absorption is 14.5 MW - this is **~4× too high**.

The boundary layer factor at 340°F is ~0.62 (interpolated).
To get 4 MW instead of 14.5 MW, we need total reduction factor of: 4/14.5 = 0.28

Since boundary layer gives 0.62, and we need 0.28 total:
**We need an additional factor of 0.28/0.62 = 0.45**

This is the **thermal mass participation factor** - it effectively reduces the heat transfer coefficient further because a smaller portion of the secondary is actively participating in heat exchange.

---

## Proposed Fix: Thermal Mass Participation Factor

### Concept

Add a **thermal mass participation factor** that multiplies with the existing boundary layer factor. This factor represents:
1. Thermal stratification limiting which water contacts tubes
2. Metal mass thermal lag (shell heats slower than tubes)
3. Stagnant regions not participating in convection

### Implementation

The participation factor varies with temperature:
- T ≤ 150°F: 0.35 (severe stratification, only tube bundle region active)
- T = 300°F: 0.50 (moderate mixing, more volume participating)
- T ≥ 500°F: 0.80 (good natural circulation)
- Steaming: 1.0 (boiling ensures full participation)

**Combined Effect:**
```
Effective Heat Transfer = HTC × Area × Bulk_ΔT × Boundary_Factor × Participation_Factor
```

At T=340°F with current numbers:
```
HTC = 68 BTU/(hr·ft²·°F)
Area = 220,000 ft²
Bulk_ΔT = 5.36°F (but this will increase with fix)
Boundary_Factor = 0.62
Participation_Factor ≈ 0.52 (interpolated at 340°F)
Combined Factor = 0.62 × 0.52 = 0.32

Heat Transfer = 68 × 220,000 × 20°F × 0.32 = 96 MBTU/hr = 28 MW
```

Wait, this assumes a larger ΔT. Let me model this properly...

### Self-Consistent Solution

The correct approach requires solving for the equilibrium where:
1. RCS heats at ~50°F/hr from net heat
2. SG heats slower, creating a temperature gap
3. Heat flow to SG equals what's needed for that temperature gap

Let me derive the equilibrium:

**RCS Energy Balance:**
```
Q_RCP - Q_SG - Q_loss = (dT_rcs/dt) × C_rcs
21 MW - Q_SG - 1 MW = 50°F/hr × (2.9×10⁶ lb × 1 BTU/lb·°F) / 3,412,142 BTU/MW·hr
20 - Q_SG = 42.5 MW
Q_SG = -22.5 MW  ← Impossible! (negative)
```

This shows 21 MW input cannot achieve 50°F/hr if SG absorbs any heat. Let me recalculate with correct units...

**Correct calculation:**
```
RCS heat capacity = 2.9×10⁶ lb × 1.0 BTU/lb·°F = 2.9×10⁶ BTU/°F
To heat at 50°F/hr: Q_net = 50 × 2.9×10⁶ = 145×10⁶ BTU/hr = 42.5 MW

Available: 21 MW RCP - 1 MW loss = 20 MW
```

**This is the fundamental issue!** 21 MW cannot produce 50°F/hr with the given thermal mass!

Let me check the NRC data again... The NRC HRTD says "approximately 50°F/hr" with 4 RCPs. But our thermal mass calculation gives only 24°F/hr max theoretical...

### Revisiting Thermal Mass

Let me check PlantConstants:
- RCS_METAL_MASS = 2,200,000 lb
- RCS_WATER_MASS (calculated from volume × density) ≈ 700,000 lb at low temp

Total RCS mass = 2.9×10⁶ lb
Cp_steel = 0.12 BTU/lb·°F
Cp_water = 1.0 BTU/lb·°F

**Correct RCS heat capacity:**
```
C_rcs = 2.2×10⁶ × 0.12 + 0.7×10⁶ × 1.0 = 0.264×10⁶ + 0.7×10⁶ = 0.964×10⁶ BTU/°F
```

**Recalculate heatup rate:**
```
With 20 MW net heat:
Q = 20 × 3.412×10⁶ BTU/hr = 68.2×10⁶ BTU/hr
dT/dt = 68.2×10⁶ / 0.964×10⁶ = 70.7°F/hr
```

This is TOO FAST if no SG heat sink! The SG is supposed to absorb some heat.

With 50°F/hr target:
```
Q_net = 50 × 0.964×10⁶ = 48.2×10⁶ BTU/hr = 14.1 MW
```

So SG should absorb: 20 - 14.1 = **5.9 MW** ← This matches our target!

### The Actual Problem

The simulation is showing 14.5 MW to SG when it should be ~6 MW.

The gap issue: With 6 MW to SG and 2.0 MBTU/°F SG heat capacity:
- SG heating rate = 6 × 3.412 / 2.0 = 10.2°F/hr
- This is less than RCS rate (50°F/hr), so gap should INCREASE

But currently SG is getting 14.5 MW:
- SG heating rate = 14.5 × 3.412 / 2.0 = 24.8°F/hr
- Almost same as RCS (24°F/hr), so gap stays small

**The boundary layer factor of 0.62 is not enough!**

We need total factor = 6/14.5 × 0.62 = 0.26 (instead of 0.62)

This means participation factor should be: 0.26/0.62 = **0.42**

---

## Revised Proposed Fix

### Option A: Reduce Boundary Layer Factor Constants (Simple)

The v1.1.2 boundary layer factors are too high. Reduce them:

| Temperature | Current Factor | Proposed Factor |
|-------------|----------------|-----------------|
| ≤150°F | 0.30 | 0.15 |
| 300°F | 0.55 | 0.25 |
| 500°F | 0.90 | 0.50 |
| Steaming | 1.00 | 1.00 |

### Option B: Add Participation Factor (More Physical)

Keep boundary layer factor, add separate participation factor.
**Combined effect = Boundary × Participation**

| Temperature | Boundary | Participation | Combined |
|-------------|----------|---------------|----------|
| ≤150°F | 0.30 | 0.50 | 0.15 |
| 300°F | 0.55 | 0.45 | 0.25 |
| 500°F | 0.90 | 0.55 | 0.50 |
| Steaming | 1.00 | 1.00 | 1.00 |

### Recommended Approach: Option A

Option A is simpler and achieves the same physical effect. The boundary layer factor already represents "effective heat transfer reduction due to stratification" - we just need to reduce it further.

The physics justification: The current factors were estimated based on boundary layer temperature alone, but did not fully account for:
1. Severe thermal stratification in stagnant water
2. Large dead zones away from tube bundle
3. Thermal resistance of tube support plates
4. Shell thermal lag

---

## Implementation Stages

### Stage 1: Update Boundary Layer Constants in PlantConstants.Heatup.cs

Change constants from:
```csharp
public const float SG_BOUNDARY_LAYER_FACTOR_MIN = 0.30f;
public const float SG_BOUNDARY_LAYER_FACTOR_MID = 0.55f;
public const float SG_BOUNDARY_LAYER_FACTOR_HIGH = 0.90f;
```

To:
```csharp
public const float SG_BOUNDARY_LAYER_FACTOR_MIN = 0.15f;
public const float SG_BOUNDARY_LAYER_FACTOR_MID = 0.25f;
public const float SG_BOUNDARY_LAYER_FACTOR_HIGH = 0.50f;
```

**Rationale:**
- Reduced by ~50% to account for thermal mass participation effects
- Combined with existing HTC scaling, achieves target ~6 MW heat absorption
- Steaming factor remains 1.0 (boiling ensures full mixing)

### Stage 2: Validation Testing

Run full simulation and verify:
1. Heatup rate 45-55°F/hr at T=200-400°F
2. SG heat absorption 4-7 MW
3. T_RCS - T_SG gap increases to 15-30°F
4. Time to HZP = 17-20 hours

---

## Unaddressed Issues

### Mass Conservation Error
The inventory audit shows growing mass error. This is unrelated to heatup rate and should be investigated separately in v1.2.0.

---

## Files to Modify

| File | Stage | Changes |
|------|-------|---------|
| PlantConstants.Heatup.cs | 1 | Reduce boundary layer constants |

---

## Validation Criteria

| Criterion | Target | Method |
|-----------|--------|--------|
| Heatup rate (4 RCPs, T=200°F) | 45-55°F/hr | Check log |
| SG heat absorption | 4-7 MW | Check log |
| T_RCS - T_SG gap at 20hr | 15-30°F | Check log |
| Time to HZP (557°F) | 17-20 hours | Run simulation |
| Boundary factor at 150°F | 0.15 | Check log |
| Boundary factor at 300°F | 0.25 | Check log |

---

## Approval

**Prepared by:** Claude (AI Assistant)  
**Date:** 2026-02-09  
**Status:** AWAITING USER APPROVAL

Proceed with implementation? [YES/NO]
