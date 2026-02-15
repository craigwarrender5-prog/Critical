# RCS Pressure-Temperature Limits and Steam Tables — Reference Documentation

**Purpose:** Consolidated reference for P-T limit curves, steam tables, and thermodynamic properties for simulator validation  
**Date:** 2026-02-15  
**Status:** Reference documentation for future implementation phases

---

## Current Implementation Status

### ✅ What We Have

#### 1. **NIST-Validated Steam Tables Implementation**
**Location:** `Assets/Scripts/Physics/WaterProperties.cs`

**Coverage:**
- Saturation temperature from pressure (1 - 3200 psia)
- Saturation pressure from temperature (100 - 700°F)
- Latent heat of vaporization (hfg)
- Liquid water density with pressure correction
- Saturated liquid and steam enthalpy
- Saturated steam density
- Subcooled liquid properties
- Superheated steam properties

**Validation:**
- Source: NIST Chemistry WebBook (E.W. Lemmon, M.O. McLinden, D.G. Friend)
- Multi-range polynomial fits for optimal accuracy
- Extended range for cold shutdown (1 psia, 100°F) and LOCA scenarios
- Pressure range: 1 - 3200 psia
- Temperature range: 100 - 700°F

**Accuracy:**
- Saturation temperature: ±1°F from 14.7-3000 psia
- Saturation pressure: ±1% from 212-700°F
- Latent heat: ±5% across full range (±1% in PWR operating range)
- Liquid density: ±2.3% across 100-653°F

**Key Methods:**
```csharp
WaterProperties.SaturationTemperature(float pressure_psia)
WaterProperties.SaturationPressure(float temp_F)
WaterProperties.LatentHeat(float pressure_psia)
WaterProperties.WaterDensity(float temp_F, float pressure_psia)
WaterProperties.SaturatedLiquidEnthalpy(float pressure_psia)
WaterProperties.SaturatedSteamEnthalpy(float pressure_psia)
WaterProperties.SaturatedSteamDensity(float pressure_psia)
```

#### 2. **Two-Phase Steam Thermodynamics**
**Location:** `Assets/Scripts/Physics/SteamThermodynamics.cs`

**Coverage:**
- Steam quality calculations (x = 0 to 1)
- Void fraction with slip ratio correlations
- Two-phase mixture properties (density, enthalpy, specific volume)
- Phase state determination
- Pressurizer-specific calculations
- Phase change energetics

**Key Methods:**
```csharp
SteamThermodynamics.SteamQuality(enthalpy, pressure)
SteamThermodynamics.VoidFraction(quality, pressure)
SteamThermodynamics.TwoPhaseDensity(quality, pressure)
SteamThermodynamics.TwoPhaseEnthalpy(quality, pressure)
SteamThermodynamics.PressurizerMasses(waterVol, steamVol, pressure, out water, out steam)
SteamThermodynamics.EvaporationEnergy(mass, pressure)
```

**Correlations:**
- Chisholm slip ratio: S = (ρf/ρg)^0.25 for vertical upward flow
- Homogeneous equilibrium model for mixture density
- Quality from enthalpy: x = (h - hf) / hfg

#### 3. **Validation Functions**
Both `WaterProperties.cs` and `SteamThermodynamics.cs` include built-in validation methods:
- `WaterProperties.ValidateProperties()` — Tests steam tables against NIST benchmarks
- `SteamThermodynamics.ValidateCalculations()` — Tests two-phase correlations

---

## ❌ What We DON'T Have

### 1. **Plant-Specific P-T Limit Curves**

**What's Missing:**
- Reactor vessel heatup limit curves (as function of EFPY)
- Reactor vessel cooldown limit curves (as function of EFPY)
- Criticality limit curves
- Inservice leak test and hydrostatic test limits
- Cold Overpressure Protection System (COPS) limits

**Why We Need It:**
- Required for operator guidance during Mode 4 → Mode 3 transition
- Required for RHR isolation permissives (< 425 psig, ≤ 350°F)
- Required for COPS PORV setpoint determination
- Required for criticality permissive (P-12)

**What's Available (Not Yet Implemented):**
From NRC HRTD Section 3.2 (ML11223A213):
- Generic Westinghouse P-T limit curves (Figures 3.2-26, 3.2-27)
- Description of methodology per ASME Code Section XI, Appendix G
- 10 CFR 50, Appendix G requirements

**Example Plant-Specific Data Available:**
- Vogtle Unit 1 PTLR (ML14112A519) — 36 EFPY limits with data tables
- Contains numerical P-T curve values for implementation

### 2. **Detailed Steam Tables for UI/Documentation**

**What's Missing:**
- Tabular steam table reference (like standard engineering handbooks)
- Graphical P-T, T-S, P-H, T-H diagrams
- Printable/exportable steam table for operator reference
- Two-phase dome visualization on T-S or P-H diagrams

**Why We Need It:**
- Operator training and reference
- Documentation validation
- Educational content for users
- Debugging and verification during development

**What Could Be Generated:**
Our existing `WaterProperties.cs` and `SteamThermodynamics.cs` can generate:
- Saturation tables (P, T, hf, hg, hfg, vf, vg, ρf, ρg)
- Two-phase mixture properties at various qualities
- Subcooled liquid tables
- Superheated steam tables

### 3. **P-T-V Relationship for Pressurizer**

**What's Missing:**
- Integrated P-T-V surface for pressurizer steam space
- Volume-pressure-temperature relationship during compression/expansion
- Phase diagram showing operational region relative to saturation dome

**Why We Need It:**
- Critical for pressurizer pressure control simulation
- Validates steam space compressibility model
- Shows relationship between level changes and pressure changes
- Demonstrates "going solid" condition

**What Could Be Generated:**
Using existing thermodynamic functions:
- Calculate water volume at various levels (0-100%)
- Calculate steam volume (1800 ft³ - water volume)
- Calculate pressure for given steam mass and volume
- Generate 3D surface: P = f(T, V_steam)

---

## Available Reference Data

### 1. **Vogtle Unit 1 P-T Limits (Example Implementation)**

Source: ML14112A519 — Vogtle Unit 1 PTLR Revision 5 (36 EFPY)

#### Heatup Limits (60°F/hr rate, excerpt)
| Temperature (°F) | Pressure (psig) |
|-----------------|-----------------|
| 60 | 747 |
| 100 | 796 |
| 150 | 1163 |
| 200 | 2231 |
| 210 | — |

#### Cooldown Limits (Steady-State, excerpt)
| Temperature (°F) | Pressure (psig) |
|-----------------|-----------------|
| 60 | 747 |
| 100 | 918 |
| 150 | 1452 |
| 180 | 2146 |

#### COPS PORV Setpoints
| Temperature (°F) | PORV Setpoint (psig) |
|-----------------|---------------------|
| 70 | 612 |
| 90 | 612 |
| 140 | 642 |
| 201 | 760 |
| 350 | 760 |

**Key Parameters:**
- Limiting Material: Intermediate Shell Plate B8805-2
- Limiting ART at 36 EFPY: 110°F (1/4T), 95°F (3/4T)
- Maximum heatup rate: 100°F/hr
- Maximum cooldown rate: 100°F/hr
- Minimum boltup temperature: 60°F
- COPS arming temperature: ≤ 220°F

### 2. **Generic Westinghouse P-T Limits (NRC HRTD)**

Source: NRC HRTD Section 3.2.6.2 (ML11223A213)

**Methodology:**
- ASME Boiler and Pressure Vessel Code, Section XI, Appendix G
- 10 CFR 50, Appendix G
- Linear elastic fracture mechanics
- Nil-ductility transition temperature (NDT) based
- Charpy V-notch 30 ft-lb criterion

**Key Concepts:**
- Reference Temperature (RTndt): Temperature below which brittle fracture may occur
- Adjusted Reference Temperature (ART): RTndt adjusted for neutron fluence
- Pressure-Temperature curves define acceptable operation region
- Heatup: Inner wall compressive stress reduces total stress
- Cooldown: Inner wall tensile stress increases total stress
- Cooldown limits more restrictive than heatup limits

**Criticality Limit:**
- Defines minimum temperature for achieving criticality
- Provides additional margin during power production
- Based on inservice hydrostatic test temperature

### 3. **Steam Table Benchmarks (NIST Validated)**

#### Saturation Properties at Key Pressures

| Pressure (psia) | Temp (°F) | hf (BTU/lb) | hg (BTU/lb) | hfg (BTU/lb) | ρf (lb/ft³) | ρg (lb/ft³) |
|----------------|-----------|-------------|-------------|--------------|-------------|-------------|
| 14.7 (1 atm) | 212.0 | 180 | 1150 | 970 | 59.8 | 0.037 |
| 100 | 327.8 | 298 | 1187 | 889 | 56.6 | 0.245 |
| 400 | 444.6 | 424 | 1204 | 780 | 52.4 | 1.00 |
| 1000 | 544.6 | 542 | 1191 | 649 | 47.2 | 2.56 |
| 2235 (PWR) | 652.9 | 671 | 1061 | 390 | 38.9 | 7.75 |
| 2500 | 668.1 | 696 | 1006 | 310 | 36.5 | 9.62 |
| 3200 (critical) | 705.1 | ~900 | ~900 | 0 | ~20 | ~20 |

**Key Observations:**
- Latent heat decreases with pressure (hfg → 0 at critical point)
- Liquid density decreases with temperature
- Steam density increases with pressure
- At 2235 psig (PWR operating): Factor of ~5 density difference (38.9/7.75)

#### PWR Operating Range Detail (2200-2500 psia)

| P (psia) | T (°F) | hf | hg | hfg | ρf | ρg | vf | vg |
|----------|--------|----|----|-----|----|----|----|----|
| 2200 | 651 | 668 | 1066 | 398 | 39.2 | 7.59 | 0.0255 | 0.132 |
| 2235 | 653 | 671 | 1061 | 390 | 38.9 | 7.75 | 0.0257 | 0.129 |
| 2300 | 657 | 677 | 1052 | 375 | 38.5 | 8.06 | 0.0260 | 0.124 |
| 2400 | 662 | 686 | 1037 | 351 | 37.9 | 8.58 | 0.0264 | 0.117 |
| 2500 | 668 | 696 | 1006 | 310 | 36.5 | 9.62 | 0.0274 | 0.104 |

---

## Implementation Recommendations

### Phase 1: Documentation (Current Need)

**Priority: MEDIUM**  
**Effort: 2-3 hours**

Create visual P-T limit reference documentation:

1. **Export Steam Table as CSV/Markdown**
   - Use existing `WaterProperties.cs` to generate tables
   - Create script to output saturation properties 1-3200 psia
   - Format as markdown table for `Technical_Documentation` folder
   - Include both saturation and two-phase properties

2. **Generate P-T Limit Curves Documentation**
   - Copy Vogtle Unit 1 example curves to documentation
   - Create markdown file with limit curve values
   - Document methodology and basis
   - Reference ASME Code requirements

3. **Create Quick Reference Charts**
   - Key saturation temperatures (400, 1000, 2235 psia)
   - Pressurizer operating region on T-S diagram
   - RCS heatup/cooldown paths on P-T curve

**Deliverables:**
- `Steam_Tables_NIST_Validated.md` — Full saturation property tables
- `RCS_Pressure_Temperature_Limits.md` — P-T curve methodology and example data
- `Pressurizer_PTV_Relationships.md` — Pressurizer-specific thermodynamics

### Phase 2: Interactive Visualization (Future)

**Priority: LOW**  
**Effort: 8-12 hours**

Create interactive P-T diagrams for UI:

1. **Steam Tables UI Panel**
   - Searchable/filterable table
   - Interpolation for intermediate values
   - Export function

2. **P-T Limit Curve Overlay**
   - Real-time RCS state on P-T diagram
   - Heatup/cooldown rate indicator
   - Distance to limit visualization
   - Alarm/caution regions

3. **Pressurizer P-V-T Surface**
   - 3D visualization of steam space behavior
   - Current operating point
   - Animation of level/pressure changes

### Phase 3: Plant-Specific P-T Limits (Future)

**Priority: MEDIUM (required before Mode 3)**  
**Effort: 4-6 hours**

Implement actual P-T limit curves:

1. **Select Reference Plant**
   - Use generic Westinghouse limits OR
   - License Vogtle/similar plant PTLR data OR
   - Synthesize representative 4-loop limits

2. **Implement P-T Limit Checking**
   - `PTLimitChecker.cs` module
   - Real-time validation during heatup/cooldown
   - Alarm generation if approaching limits
   - Interlock with RHR isolation (< 425 psig)

3. **COPS Integration**
   - PORV setpoint curve implementation
   - Arming temperature logic (≤ 220°F)
   - Protection against cold overpressure

---

## Technical Notes

### P-T Limit Curve Methodology

**Per ASME Code Section XI, Appendix G:**

The allowable pressure (P) at temperature (T) is determined by:

```
K_I = (C_1 / t^0.25) × sqrt(a)
```

Where:
- K_I = stress intensity factor
- C_1 = constant based on material and crack geometry
- t = vessel wall thickness
- a = postulated flaw size

The pressure-temperature relationship ensures:
```
K_I < K_Ic(T)
```

Where K_Ic(T) is the fracture toughness at temperature T.

**Temperature Effects:**
- **Heatup:** Inner wall sees compression (favorable), outer wall tension
  - Compression from thermal gradient REDUCES pressure stress
  - Can tolerate HIGHER pressure at given temperature
  
- **Cooldown:** Inner wall sees tension (unfavorable), outer wall compression
  - Tension from thermal gradient ADDS to pressure stress
  - Must maintain LOWER pressure at given temperature

**Neutron Embrittlement:**
- RTndt increases with neutron fluence (EFPY)
- ΔRTndt calculated per Regulatory Guide 1.99, Rev. 2
- Curves become more restrictive with vessel age
- Surveillance capsule data updates predictions

### Pressurizer P-T-V Relationships

For pressurizer steam space at saturation conditions:

**Ideal Gas Approximation (valid at PWR pressures):**
```
P × V = m × R × T

Where:
- P = pressure (psia)
- V = steam volume (ft³)
- m = steam mass (lb)
- R = gas constant for steam (85.78 ft·lbf/(lb·°R))
- T = absolute temperature (°R = °F + 459.67)
```

**More Accurate (using steam tables):**
```
ρ_g = f(P, T_sat)  [from WaterProperties.SaturatedSteamDensity]
m = ρ_g × V
```

**Compression/Expansion:**
- Compression (insurge): V decreases → ρ increases → P increases
- Expansion (outsurge): V increases → ρ decreases → P decreases
- Factor of ~6: Boiling 1 ft³ water → 6 ft³ steam (at constant P)

**Pressurizer Example (2235 psig):**
- Total volume: 1800 ft³
- Level: 60% → Water: 1080 ft³, Steam: 720 ft³
- T_sat: 653°F
- ρ_g: 7.75 lb/ft³
- Steam mass: 720 × 7.75 = 5580 lb

If level increases to 70% (surge of 180 ft³ water):
- New steam volume: 540 ft³
- Same steam mass: 5580 lb
- New steam density: 5580 / 540 = 10.3 lb/ft³
- New pressure: ~2550 psia (from density-pressure relationship)

---

## References

### Steam Tables
1. NIST Chemistry WebBook — E.W. Lemmon, M.O. McLinden, D.G. Friend
   - URL: https://webbook.nist.gov/chemistry/fluid/
   - Standard reference for water/steam properties

2. ASME Steam Tables (9th Edition)
   - Published by ASME, 2014
   - Industry standard reference

3. Keenan, Keyes, Hill, and Moore — "Steam Tables (SI Units)"
   - Classic reference work

### P-T Limits
4. NRC HRTD Section 3.2 — Reactor Coolant System, Rev 1203 (ML11223A213)
   - Generic Westinghouse P-T limit curves
   - Figures 3.2-26 (Heatup) and 3.2-27 (Cooldown)

5. 10 CFR 50, Appendix G — "Fracture Toughness Requirements"
   - Federal regulation governing P-T limits
   - URL: https://www.nrc.gov/reading-rm/doc-collections/cfr/part050/part050-appg.html

6. ASME Boiler and Pressure Vessel Code, Section XI, Appendix G
   - "Fracture Toughness Criteria for Protection Against Failure"
   - Industry standard methodology

7. Regulatory Guide 1.99, Revision 2 — "Radiation Embrittlement of Reactor Vessel Materials"
   - NRC guidance on neutron fluence effects
   - URL: https://www.nrc.gov/reading-rm/doc-collections/reg-guides/

8. WCAP-14040-A, Rev. 4 — "Methodology Used to Develop Cold Overpressure Mitigating System Setpoints and RCS Heatup and Cooldown Limit Curves"
   - Westinghouse proprietary methodology
   - NRC approved

9. Vogtle Unit 1 PTLR Revision 5 (ML14112A519)
   - Example plant-specific implementation
   - Contains actual numerical P-T curve data

### Implemented Code
10. `Assets/Scripts/Physics/WaterProperties.cs`
    - NIST-validated steam table implementation
    - Polynomial fits for saturation properties

11. `Assets/Scripts/Physics/SteamThermodynamics.cs`
    - Two-phase flow correlations
    - Pressurizer-specific calculations

---

## Conclusion

### Current State: **ADEQUATE for Phase 0**

Our steam table implementation is **robust and well-validated** for current needs:
- NIST-validated thermodynamic properties
- Full saturation property coverage
- Two-phase flow calculations
- Extended range for all operating modes

### Gaps: **Not Critical Yet**

Missing P-T limit curves and documentation are **not blockers for current development**:
- Not needed until Mode 4 → Mode 3 transition
- Can be added when implementing:
  - RHR isolation permissives
  - COPS functionality
  - Operator training features

### Recommendation: **Document Now, Implement Later**

**Immediate Action (This Session):**
1. ✅ Create this reference document
2. Generate steam table markdown from existing code
3. Document P-T limit methodology

**Future Action (Pre-Mode 3):**
1. Implement plant-specific P-T limit curves
2. Add P-T limit checking to simulator
3. Create visual P-T diagram overlay
4. Integrate COPS PORV setpoints

---

*Document created 2026-02-15*  
*Status: Reference documentation complete*  
*Implementation: Deferred to future phase*
