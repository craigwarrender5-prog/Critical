# CRITICAL: Master the Atom — Implementation Plan v0.8.0
# SG Secondary Side Thermal Mass Integration & RCS Heatup Rate Correction

**Version:** 0.8.0 (Minor — new physics subsystem, changes heatup behavior)
**Previous Version:** 0.7.0 (Dashboard UI redesign)
**Date:** 2026-02-07
**Classification:** Physics Correction — Realism Critical

---

## 1. PROBLEM STATEMENT

### 1.1 Observed Symptom

During heatup validation testing, the RCS heatup rate reaches **71°F/hr** once all 4 RCPs are running at full power (Logs #023–#024, T+12.0hr to T+12.5hr). The validation check `RCS Rate <=50F/hr` has been **FAILING since T+11.0hr** (Log #021, 51.22°F/hr) and escalates to 72.42°F/hr by T+12.0hr.

The Tech Spec limit logged in the validation is 50°F/hr, which represents the *expected* operator-controlled heatup rate per NRC ML11223A342 Section 19.2.2. The *hard* Tech Spec limit for reactor vessel thermal stress protection is **100°F/hr**.

### 1.2 How Serious Is 71°F/hr in a Real Westinghouse 4-Loop PWR?

| Threshold | Rate | Significance |
|-----------|------|-------------|
| Normal operational target | ~50°F/hr | Per NRC HRTD 19.2.2 — "approximately 50°F per hour" with 4 RCPs |
| Our simulation | 71°F/hr | 42% above expected; see root cause below |
| Tech Spec HARD limit | 100°F/hr | Maximum allowable per Tech Specs — protects reactor vessel from brittle fracture and thermal stress |
| Immediate safety concern | >100°F/hr | Would violate Tech Specs; potential for excessive thermal stress on RPV nozzles and beltline |

**Assessment: 71°F/hr is NOT a safety violation** (below the 100°F/hr hard limit), but it IS **unrealistically fast** compared to real plant heatup operations. A real plant with 4 RCPs running achieves approximately 50°F/hr because the steam generators act as a massive thermal sink that absorbs a significant portion of the RCP mechanical heat input. Our simulator is missing this heat sink entirely.

### 1.3 Root Cause Analysis

The current thermal model in `RCSHeatup.BulkHeatupStep()` and `PlantConstants.GetTotalHeatCapacity()` computes heat capacity as:

```
totalHeatCap = RCS water (m × Cp) + RCS metal (2,200,000 lb × 0.12 BTU/lb·°F)
```

**What's included in `RCS_METAL_MASS = 2,200,000 lb`:**
- Reactor Vessel: 800,000 lb
- 4× SG primary side (tube bundle metal, channelheads): portion of 4 × 700,000 lb
- RCS piping: 400,000 lb
- Pressurizer wall mass: 200,000 lb

**What's MISSING — the SG secondary side:**
The steam generators during cold shutdown are in **wet layup** — filled to 100% with water. Per NRC HRTD 19.2.2 initial conditions: *"Steam generators filled to wet-layup (100%)"*. This means each SG contains:

| Component | Mass per SG | Total (4 SGs) | Cp | Heat Capacity |
|-----------|------------|---------------|-----|--------------|
| Secondary shell/shroud metal | ~200,000 lb | 800,000 lb | 0.12 BTU/lb·°F | 96,000 BTU/°F |
| Secondary water (wet layup) | ~415,000 lb | 1,660,000 lb | ~1.0 BTU/lb·°F | ~1,660,000 BTU/°F |
| **TOTAL MISSING** | | **2,460,000 lb** | | **~1,756,000 BTU/°F** |

The SG tube bundle is the interface: hot primary water flows through tubes, transferring heat to the secondary side water surrounding them. During heatup, RCP heat raises the primary temperature, but that heat conducts through the SG tube walls into the secondary water, which must ALSO be heated.

### 1.4 Quantitative Impact

**Current model heat capacity** (from `GetTotalHeatCapacity` at ~200°F, 800 psia):
- RCS water: ~11,500 ft³ × ~57 lb/ft³ × ~1.0 BTU/lb·°F ≈ 655,500 BTU/°F
- RCS metal: 2,200,000 lb × 0.12 ≈ 264,000 BTU/°F
- **Total: ~919,500 BTU/°F**

**With SG secondary side added:**
- SG secondary metal: 800,000 lb × 0.12 = 96,000 BTU/°F
- SG secondary water: 1,660,000 lb × 1.0 = 1,660,000 BTU/°F
- **New total: ~2,675,500 BTU/°F**

**Ratio: 2,675,500 / 919,500 ≈ 2.91×**

The thermal mass nearly **triples** when the SG secondary side is properly included.

**Predicted corrected heatup rate:**
- Current: ~71°F/hr with 4 RCPs (~22 MW net)
- Corrected: ~71 / 2.91 ≈ **24°F/hr** (too slow!)

However, this is an overcorrection because the SG secondary side is not perfectly thermally coupled to the primary. The coupling depends on the SG heat transfer coefficient and area, and there is a time constant for the secondary to respond. The effective coupling fraction varies:

- **At low temperatures (<200°F):** Natural convection on secondary side is weak, coupling ~30-50%
- **With RCPs running, moderate temps:** Forced convection dramatically improves coupling to ~60-80%
- **At higher temps (>350°F):** Near-full coupling ~80-95% as secondary approaches boiling

A properly modeled dynamic coupling should yield **~45-55°F/hr** with 4 RCPs — matching the NRC HRTD expected rate.

### 1.5 Turbine and Balance of Plant

The turbine and main steam system are **NOT relevant during heatup**. During cold shutdown to hot standby:
- Turbine is isolated (main steam isolation valves closed until Mode 3)
- No steam is being generated (secondary side is subcooled)
- Condensate/feedwater systems are not in service
- The main condenser is not acting as a heat sink

The turbine adds no thermal mass during heatup and does NOT need to be modeled for this correction. It only becomes relevant at power operation (Mode 1/2), which is beyond the current simulator scope.

---

## 2. EXPECTATIONS

### 2.1 Success Criteria

| Metric | Current | Target | Source |
|--------|---------|--------|--------|
| RCS heatup rate with 4 RCPs | 71°F/hr | 45-55°F/hr | NRC HRTD 19.2.2: "approximately 50°F/hr" |
| Time from RCPs to 557°F | ~6.5 hr | ~9-11 hr | Consistent with ~50°F/hr from 100°F |
| Validation check `RCS Rate <=50F/hr` | FAIL at T+11hr | PASS (or marginal PASS ~50-52°F/hr) | Tech Spec compliance |
| Heatup rate with 1-2 RCPs | ~8-35°F/hr | ~5-25°F/hr | Proportionally reduced |
| PZR isolated heating rate | ~43°F/hr | UNCHANGED | SG secondary unaffected during isolated PZR heating |
| Phase 1 (solid PZR) behavior | -0.2°F/hr RCS | UNCHANGED | No RCPs, no forced SG coupling |
| Mass conservation | PASS | PASS | No new mass flows introduced |

### 2.2 What Must NOT Change

- **PZR isolated heating physics** — heaters heating a solid/water-filled pressurizer before bubble formation has nothing to do with SG secondary side
- **Bubble formation state machine** — purely a PZR phenomenon
- **CVCS flow control** — letdown/charging balance unaffected
- **Solid plant pressure control** — operates before RCPs start
- **BRS physics** — boron recycle is independent
- **All existing GOLD standard certifications**

---

## 3. PROPOSED FIX — Staged Implementation

### Stage 1: SG Secondary Side Thermal Mass Model (New Physics)

**New file: `SGSecondaryThermal.cs`** in `Assets/Scripts/Physics/`

A lumped-parameter model of the 4 steam generator secondary sides:

```
Core State:
  - T_sg_secondary: Average SG secondary water temperature (°F)
  - SG_secondary_heat_capacity: Dynamic function of temperature

Heat Transfer Model:
  Q_primary_to_secondary = U × A × (T_rcs - T_sg_secondary)
  
  Where:
    U = Overall heat transfer coefficient (BTU/hr·ft²·°F)
    A = Total SG tube area = 4 × 55,000 ft² = 220,000 ft²
    
  U varies with conditions:
    - No RCPs: U ≈ 5-15 BTU/hr·ft²·°F (natural convection only)
    - RCPs running: U ≈ 150-250 BTU/hr·ft²·°F (forced primary, natural secondary)
    - At power: U ≈ 250-350 BTU/hr·ft²·°F (forced both sides, boiling)
    
  During heatup (no secondary flow), secondary side is natural convection only.

Temperature Update:
  dT_sg/dt = Q_primary_to_secondary / C_sg_secondary
  
  Where C_sg_secondary = (M_metal × Cp_steel + M_water × Cp_water(T))
```

This is a **single lumped node** for all 4 SGs combined — appropriate for a training simulator where all 4 loops are symmetric. The key physics is the **heat sink effect**: energy going into the SG secondary is energy NOT raising the RCS temperature.

**Constants to add to `PlantConstants.Heatup.cs`:**
```csharp
// SG Secondary Side During Heatup
public const float SG_SECONDARY_TOTAL_METAL_MASS_LB = 800000f;  // 4 × 200,000 lb
public const float SG_SECONDARY_TOTAL_WATER_MASS_LB = 1660000f; // 4 × 415,000 lb
public const float SG_TUBE_AREA_TOTAL_FT2 = 220000f;            // 4 × 55,000 ft²
public const float SG_HTC_NO_FLOW = 10f;      // BTU/hr·ft²·°F  (natural convection)
public const float SG_HTC_FORCED_PRIMARY = 200f; // BTU/hr·ft²·°F (RCPs running)
```

Note: The constants `SG_MASS_EACH_LB`, `SG_SECONDARY_METAL_MASS_LB`, `SG_SECONDARY_WATER_MASS_LB`, and `SG_AREA_EACH` already exist in `PlantConstants.cs` and `PlantConstants.Heatup.cs`. The new constants aggregate these for the combined 4-SG model and add the heat transfer coefficients.

### Stage 2: Integration into Heatup Engine

**Modify `RCSHeatup.BulkHeatupStep()`** to subtract SG secondary heat absorption:

```
Current:  dT_rcs = Q_net / (C_rcs + C_pzr)
Proposed: dT_rcs = (Q_net - Q_to_sg_secondary) / (C_rcs + C_pzr)
```

The SG secondary temperature tracks separately and is updated each timestep.

**Modify `HeatupSimEngine.cs`** to:
1. Initialize `T_sg_secondary` to match `startTemperature` (both start at ~100°F in cold shutdown — thermal equilibrium)
2. Pass SG secondary state to `BulkHeatupStep` and `IsolatedHeatingStep`
3. Expose `T_sg_secondary` as a public field for the dashboard
4. Log `T_sg_secondary` in heatup validation logs

### Stage 3: Dashboard Integration

**Modify `HeatupValidationVisual` partials** to:
1. Add T_sg gauge to the temperature group
2. Add T_sg to the TEMPS trend graph
3. Log T_sg in the validation log output

### Stage 4: Validation & Testing

- Run full heatup simulation cold shutdown → hot standby
- Verify heatup rate ~45-55°F/hr with 4 RCPs
- Verify SG secondary temperature tracks ~10-20°F behind RCS (realistic lag)
- Verify Phase 1 (PZR isolated heating) is unaffected
- Verify mass conservation still passes
- Verify all existing validation tests pass
- Tune HTC values if necessary to match expected ~50°F/hr rate

---

## 4. FILES AFFECTED

| File | Change Type | Description |
|------|------------|-------------|
| **NEW: `SGSecondaryThermal.cs`** | Create | SG secondary side lumped thermal model |
| `PlantConstants.Heatup.cs` | Modify | Add SG HTC constants for heatup conditions |
| `RCSHeatup.cs` | Modify | Accept SG heat sink in BulkHeatupStep |
| `HeatupSimEngine.cs` | Modify | Initialize/track SG secondary state, pass to physics |
| `HeatupSimEngine.Init.cs` | Modify | Initialize T_sg_secondary |
| `HeatupSimEngine.Logging.cs` | Modify | Log T_sg_secondary in validation output |
| `HeatupValidationVisual.Gauges.cs` | Modify | Add SG secondary temperature gauge |
| `HeatupValidationVisual.Graphs.cs` | Modify | Add T_sg to temperature trend graph |

**Files NOT changed:** ThermalMass.cs, CoupledThermo.cs, SolidPlantPressure.cs, PressurizerPhysics.cs, BubbleFormation partial, CVCS partial, Alarms partial, BRSPhysics.cs, all PlantConstants files except Heatup, all Reactor/ modules.

---

## 5. RISK ASSESSMENT

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Over-damping (rate too slow, <40°F/hr) | Medium | HTC values are tunable; start conservative, adjust |
| Under-damping (rate still >60°F/hr) | Low | Increase HTC or coupling fraction |
| Phase 1 regression (PZR heating affected) | Very Low | SG coupling with no RCPs is near-zero by design |
| CoupledThermo convergence issues | Low | SG heat sink reduces ΔT per step, improving convergence |
| Mass conservation error | Very Low | No new mass flows; only energy redistribution |
| Dashboard display issues | Low | T_sg is just another temperature reading |

---

## 6. SUMMARY

The 71°F/hr heatup rate is caused by the simulator ignoring the massive thermal sink of the steam generator secondary side water (~1.66 million lb of water in wet layup). This is the single largest thermal mass in the entire primary + secondary system combined, and its omission causes the RCS to heat nearly 50% faster than a real Westinghouse plant.

The steam generator secondary side thermal model is a **straightforward, well-understood heat transfer problem** (lumped-parameter HX model) that requires no exotic physics. It is absolutely feasible to implement now and is, in fact, necessary for any realistic heatup simulation.

The **turbine does NOT need to be modeled** for this correction — it is isolated during heatup and adds no thermal mass to the system.

**Recommendation: Proceed with v0.8.0 implementation (Stages 1-4).**
