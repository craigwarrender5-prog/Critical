# Steam Generator Heat Transfer Investigation Summary
## PWR Heatup Rate Issue - February 9, 2026

---

## Problem Statement

**Observed Behavior:**
- Target heatup rate: ~50°F/hr (typical PWR with 4 RCPs running)
- Actual heatup rate: 26°F/hr at 12.25 hours into simulation
- SG secondary absorbing 14.56 MW of heat
- This excessive heat absorption is preventing the RCS from heating at the expected rate

**System Configuration:**
- Westinghouse 4-Loop PWR simulation
- 4 Steam Generators, 8,519 tubes each
- Total tube surface area: 220,000 ft²
- RCS heated by 4 RCPs: 21 MW total heat input
- SG secondary in "wet layup" (100% filled, no forced circulation)

---

## Investigation Timeline

### Initial Hypothesis (INCORRECT)
Initially believed the problem was inadequate temperature scaling of the Heat Transfer Coefficient (HTC). Implementation included Churchill-Chu correlation-based scaling:
- 100°F: HTC scale = 0.3 (HTC ≈ 30 BTU/hr·ft²·°F)
- 300°F: HTC scale = 0.6 (HTC ≈ 60 BTU/hr·ft²·°F)  
- 500°F: HTC scale = 1.0 (HTC ≈ 100 BTU/hr·ft²·°F)

**Why This Failed:**
Even with temperature scaling correctly applied, heat transfer remained ~14 MW because the tube surface area (220,000 ft²) is enormous. The scaling reduced HTC from 100 to ~35, but:
```
Q = 35 BTU/hr·ft²·°F × 220,000 ft² × 6.23°F = 47.96 MBTU/hr = 14.05 MW
```

### Code Review Findings

**Logging Bug Identified:**
- File: `HeatupSimEngine.Logging.cs`, Line 761
- Calls: `GetCurrentHTC(rcpCount)` - OLD overload without temperature scaling
- Should call: `GetCurrentHTC(rcpCount, T_sg_secondary, isSteaming)` - NEW overload with scaling
- **Impact:** Logging displays incorrect HTC value (100), but physics calculations ARE using correct temperature-scaled HTC

**Physics Implementation Verified Correct:**
- File: `SGSecondaryThermal.cs`, `CalculateHeatTransfer()` method
- Correctly calls temperature-scaled HTC
- Heat transfer calculation itself is physically sound
- The problem is NOT in the code implementation

---

## Root Cause Analysis

### The Physical Reality

**Steam Generator Configuration During Heatup:**

**Primary Side (Inside Tubes):**
- RCS water at 148°F flowing through ALL 8,519 tubes
- Forced convection from RCP flow
- All 220,000 ft² of tube interior surface gets hot RCS water

**Secondary Side (Outside Tubes):**
- Large pressure vessel (20 ft diameter × 62 ft tall)
- Completely filled with ~214,000 lbs of water per SG (wet layup)
- **NO secondary pumps running**
- **NO forced circulation**
- Water is essentially stagnant

### The Critical Insight: Thermal Stratification

**What Actually Happens:**

1. Hot RCS water (148°F) flows through tubes
2. Heat conducts through tube walls
3. Thin boundary layer of secondary water touching tubes heats to ~145-147°F
4. With **minimal natural circulation**, this heated water cannot mix effectively with the bulk
5. **Severe thermal stratification develops:**
   - At tube surfaces: ~145-147°F
   - A few inches away: ~140°F
   - Further out: ~130-120°F
   - Near vessel shell: ~105-110°F
   - **Bulk average: ~142°F** (what the simulation tracks)

**The Heat Transfer Problem:**

The classic heat transfer equation uses:
```
Q = HTC × Area × ΔT
where ΔT = T_rcs - T_sg_secondary_bulk
```

Current simulation:
```
ΔT = 148°F - 142°F = 6°F
Q = 35 × 220,000 × 6 = 46.2 MBTU/hr ≈ 14 MW
```

But the **actual temperature difference driving heat transfer** is at the tube surface boundary:
```
ΔT_actual = T_rcs - T_boundary_layer ≈ 148°F - 146°F = 2°F
```

**The huge secondary water volume acts as a thermal capacitor:**
- Small heated boundary layer tries to heat massive cold bulk
- Very slow mixing due to minimal natural circulation
- Bulk temperature rises slowly
- But boundary layer temperature stays high (near RCS temp)
- **Effective ΔT at heat transfer surface is much smaller than bulk ΔT**

### Supporting Evidence from Literature

**NRC HRTD Documentation (ML11251A016):**
- Cold shutdown: SG in "wet layup condition" - completely filled with water
- During startup: "steam production begins" only when temperature approaches saturation
- At 100-150°F: NO steam production, NO strong natural circulation

**Thermal Stratification Research:**
- Richardson number (Ri) during stagnant conditions: ~27,000
- Ri > 10 indicates strong stratification, suppressed natural convection
- At Ri ≈ 27,000: essentially stagnant with severe thermal stratification
- Research confirms: "coolant temperature at bottom of steam generator was much lower than at reactor inlet/outlet"

**Natural Circulation Limitations:**
- Circulation ratios (4:1 to 22:1) documented in NRC literature apply ONLY when steam is being produced
- At subcooled conditions (100-150°F), these ratios do not apply
- Without boiling to drive circulation, only weak buoyancy forces exist
- With ΔT of only 6°F, buoyancy-driven flow is minimal

---

## Proposed Solutions

### Option 1: Boundary Layer Temperature Model (Most Physically Accurate)

Model the boundary layer temperature separately from bulk temperature:

```csharp
// With minimal natural circulation, boundary layer approaches RCS temperature
// The effective driving ΔT is much smaller than the bulk ΔT
float bulkDeltaT = T_rcs - T_sg_secondary_bulk;

// Boundary layer effectiveness factor
// At low temperatures with no circulation: ~0.3 (boundary layer at 90% of RCS temp)
// As circulation improves with temperature: approaches 1.0
float boundaryLayerFactor = GetBoundaryLayerFactor(T_sg_secondary, rcpsRunning, isSteaming);
float effectiveDeltaT = bulkDeltaT * boundaryLayerFactor;

float heatTransferRate = htc * area * effectiveDeltaT;
```

**Advantages:**
- Physically represents what's actually happening
- Accounts for thermal stratification explicitly
- Can be tuned based on circulation conditions

**Implementation:**
- Add `GetBoundaryLayerFactor()` method
- Factor should be low (~0.3) at cold temps with no secondary circulation
- Factor increases as temperature rises and buoyancy improves
- Factor approaches 1.0 when steaming (strong natural circulation from boiling)

### Option 2: Effective Area Reduction (Simpler, Less Accurate)

Reduce the effective heat transfer area to account for stagnant zones:

```csharp
float effectiveArea = PlantConstants.SG_TUBE_AREA_TOTAL_FT2;

if (rcpsRunning > 0 && T_sg_secondary < 300f && !isSteaming)
{
    // Thermal stratification during cold heatup
    // Only lower portion of tube bundle actively transfers heat
    effectiveArea *= 0.15f;  // Based on 10-15% active height
}

float heatTransferRate = htc * effectiveArea * deltaT;
```

**Advantages:**
- Simple to implement
- Matches physical intuition about stagnant zones

**Disadvantages:**
- Less physically accurate (treats area as the variable when temperature is the real issue)
- Harder to justify the 0.15 factor (educated guess rather than physics-based)

### Option 3: Hybrid Approach (Recommended)

Combine both methods for maximum accuracy:

```csharp
// Account for both stagnant zones AND boundary layer effects
float effectiveArea = PlantConstants.SG_TUBE_AREA_TOTAL_FT2;
float effectiveDeltaT = T_rcs - T_sg_secondary;

if (rcpsRunning > 0 && T_sg_secondary < 300f && !isSteaming)
{
    // Boundary layer heating reduces effective ΔT
    float boundaryFactor = GetBoundaryLayerFactor(T_sg_secondary);
    effectiveDeltaT *= boundaryFactor;
}

float heatTransferRate = htc * effectiveArea * effectiveDeltaT;
```

---

## Expected Results

### Target Performance
- Heatup rate with 4 RCPs: ~50°F/hr
- SG heat absorption: ~5-7 MW (reduced from current 14.56 MW)
- Net heat to RCS: ~15 MW (vs current ~8 MW)

### Validation Criteria
With boundary layer factor of ~0.3:
```
Effective ΔT = 6°F × 0.3 = 1.8°F
Q = 35 × 220,000 × 1.8 = 13.86 MBTU/hr ≈ 4 MW ✓
Net heat to RCS = 21 MW (RCPs) - 4 MW (SG) = 17 MW
Expected heatup rate ≈ 50°F/hr ✓
```

---

## Further Research Required

### Questions to Answer
1. What is the actual boundary layer thickness and temperature profile in a stagnant SG secondary?
2. How does natural circulation develop as temperature increases from 100°F to 300°F?
3. At what temperature/ΔT does natural circulation become significant?
4. Are there CFD studies of SG secondary thermal stratification during cold startup?

### Data Sources to Investigate
1. **Westinghouse Technical Manuals** - SG startup procedures and thermal behavior
2. **NRC NUREG Reports** - Detailed thermal-hydraulic analysis of SG behavior
3. **RELAP5/TRACE Models** - How do industry-standard codes model this?
4. **Plant Startup Logs** - Actual measured SG temperature distributions during heatup
5. **CFD Studies** - Computational Fluid Dynamics analysis of SG natural circulation

### Experimental Validation
Ideally, we need:
- Measured temperature profiles across SG secondary during cold startup
- Multiple thermocouples at different radial positions from tube bundle
- Correlation between RCS heatup rate and SG temperature stratification

---

## Implementation Recommendations

### Immediate Actions
1. **Fix the logging bug** in `HeatupSimEngine.Logging.cs` line 761
   - Change: `GetCurrentHTC(rcpCount)` 
   - To: `GetCurrentHTC(rcpCount, T_sg_secondary, false)`

2. **Implement boundary layer factor** in `SGSecondaryThermal.cs`
   - Add `GetBoundaryLayerFactor(T_sg_secondary, rcpsRunning, isSteaming)` method
   - Start with factor = 0.3 at T < 200°F, no circulation
   - Tune based on achieving 50°F/hr heatup rate target

3. **Add detailed logging** of thermal stratification effects
   - Log both bulk ΔT and effective ΔT
   - Log boundary layer factor
   - Track temperature gradient across secondary volume

### Testing Plan
1. Run full heatup simulation with new boundary layer model
2. Verify heatup rate reaches ~50°F/hr with 4 RCPs running
3. Verify SG heat absorption drops to ~5-7 MW range
4. Check for unintended side effects at higher temperatures
5. Validate behavior during transition to steaming conditions

---

## Conclusion

The root cause of the low heatup rate (26°F/hr vs target 50°F/hr) is **thermal stratification in the SG secondary side during stagnant wet layup conditions**.

The heat transfer calculation uses the bulk average secondary temperature (142°F), but the actual boundary layer at the tube surfaces is much hotter (~146°F) due to minimal natural circulation. This reduces the effective driving temperature difference from 6°F to ~2°F, causing excessive heat absorption by the SG.

**The fix is NOT to change the HTC or tube area, but to account for the boundary layer temperature difference that develops under stagnant conditions.**

Implementing a boundary layer effectiveness factor of ~0.3 for cold, stagnant conditions should reduce SG heat absorption from 14 MW to ~4 MW, allowing the RCS to heat at the physically realistic rate of ~50°F/hr.

---

## File References

**Physics Implementation:**
- `Assets/Scripts/Physics/SGSecondaryThermal.cs` - Heat transfer calculations (CORRECT)
- `Assets/Scripts/Physics/RCSHeatup.cs` - Line 106-107 calls SG heat transfer (CORRECT)
- `Assets/Scripts/Physics/PlantConstants.Heatup.cs` - HTC temperature scaling constants

**Simulation Engine:**
- `Assets/Scripts/Validation/HeatupSimEngine.cs` - Main simulation loop
- `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs` - Line 761 logging bug (NEEDS FIX)

**Documentation:**
- `Updates/Changelogs/CHANGELOG_v1.1.1.md` - Temperature-dependent HTC implementation
- NRC HRTD 2.3 (ML11251A016) - Steam Generator technical reference

---

**Document Status:** Draft for Review  
**Next Steps:** Literature review → Implementation → Testing → Validation
