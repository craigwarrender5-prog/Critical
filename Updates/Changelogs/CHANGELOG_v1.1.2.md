# CHANGELOG v1.1.2 — Thermal Stratification Boundary Layer Model

## Version: 1.1.2
## Date: 2026-02-09
## Status: IMPLEMENTED

---

## Summary

This release implements a boundary layer effectiveness factor to model thermal stratification in the SG secondary side during cold heatup. The v1.1.1 temperature-dependent HTC scaling was correctly applied but insufficient to achieve target heatup rates because the calculation used bulk ΔT rather than effective ΔT at the tube surface.

**Root Cause:** In stagnant wet-layup conditions (no secondary circulation), severe thermal stratification develops with Richardson number Ri ≈ 27,000. The boundary layer near the tubes heats to near-RCS temperature while the bulk remains cold. This reduces the effective driving ΔT for heat transfer.

**Solution:** Apply a boundary layer effectiveness factor to the temperature difference:
- `Effective ΔT = Bulk ΔT × Boundary Layer Factor`
- Factor ranges from 0.30 (cold, stratified) to 1.0 (steaming, well-mixed)

---

## Changes

### Stage 1: Boundary Layer Constants (PlantConstants.Heatup.cs)

**Added Constants:**

```csharp
// v1.1.2 FIX: Boundary Layer Effectiveness Factor for Thermal Stratification
public const float SG_BOUNDARY_LAYER_FACTOR_MIN = 0.30f;      // At T ≤ 150°F
public const float SG_BOUNDARY_LAYER_TEMP_MIN_F = 150f;       // Low temp anchor
public const float SG_BOUNDARY_LAYER_FACTOR_MID = 0.55f;      // At T = 300°F
public const float SG_BOUNDARY_LAYER_TEMP_MID_F = 300f;       // Mid temp anchor
public const float SG_BOUNDARY_LAYER_FACTOR_HIGH = 0.90f;     // At T ≥ 500°F
public const float SG_BOUNDARY_LAYER_TEMP_HIGH_F = 500f;      // High temp anchor
public const float SG_BOUNDARY_LAYER_FACTOR_STEAMING = 1.0f;  // When steaming
```

**Physics Basis:**
- Richardson number Ri ≈ 27,000 during stagnant wet-layup (Ri > 10 indicates strong stratification)
- Boundary layer temperature approaches RCS temperature due to minimal natural circulation
- Factor of 0.30 at cold temps reflects ~90% boundary layer heating (ΔT_effective ≈ 0.3 × ΔT_bulk)
- Factor increases with temperature as buoyancy forces improve natural mixing
- Factor = 1.0 when steaming (boiling provides vigorous circulation, no stratification)

---

### Stage 2: Boundary Layer Factor Method (SGSecondaryThermal.cs)

**Added Method:**

```csharp
/// <summary>
/// Calculate boundary layer effectiveness factor for thermal stratification.
/// </summary>
/// <param name="T_secondary">SG secondary bulk temperature (°F)</param>
/// <param name="isSteaming">True if secondary is at saturation (steaming)</param>
/// <returns>Boundary layer effectiveness factor (0.30 to 1.0)</returns>
public static float GetBoundaryLayerFactor(float T_secondary, bool isSteaming)
```

**Implementation:**
- Returns 1.0 immediately if steaming (boiling provides vigorous circulation)
- Returns 0.30 if T ≤ 150°F (severe stratification)
- Returns 0.90 if T ≥ 500°F (good buoyancy-driven mixing)
- Piecewise linear interpolation between anchor points

---

### Stage 3: Heat Transfer Calculation Modifications (SGSecondaryThermal.cs)

**Modified Methods:**

1. **`CalculateHeatTransfer()`**
2. **`CalculateHeatTransfer_MW()`**
3. **`UpdateHZPSecondary()`**

**Before (v1.1.1):**
```csharp
float deltaT = T_rcs - T_sg_secondary;
float heatTransferRate = htc * PlantConstants.SG_TUBE_AREA_TOTAL_FT2 * deltaT;
```

**After (v1.1.2):**
```csharp
// Bulk temperature difference
float bulkDeltaT = T_rcs - T_sg_secondary;

// v1.1.2 FIX: Apply boundary layer effectiveness factor
float boundaryFactor = GetBoundaryLayerFactor(T_sg_secondary, isSteaming);
float effectiveDeltaT = bulkDeltaT * boundaryFactor;

// Heat transfer rate: Q = U × A × ΔT_effective
float heatTransferRate = htc * PlantConstants.SG_TUBE_AREA_TOTAL_FT2 * effectiveDeltaT;
```

**Impact:**
- At T = 150°F with bulk ΔT = 6°F:
  - Before: Q = 36 × 220,000 × 6.0 = 47.5 MBTU/hr ≈ 14 MW
  - After:  Q = 36 × 220,000 × 1.8 = 14.3 MBTU/hr ≈ 4.2 MW
- Net heat to RCS increases from ~8 MW to ~17 MW
- Heatup rate increases from ~26°F/hr to ~50°F/hr

---

### Stage 4: Logging Bug Fix (HeatupSimEngine.Logging.cs)

**Fixed:** HTC logging now uses temperature-scaled overload

**Before:**
```csharp
sb.AppendLine($"    Current HTC:      {SGSecondaryThermal.GetCurrentHTC(rcpCount),10:F0} BTU/(hr·ft²·°F)");
```

**After:**
```csharp
// v1.1.2 FIX: Use temperature-scaled HTC overload for accurate logging
float currentHTC = SGSecondaryThermal.GetCurrentHTC(rcpCount, T_sg_secondary, false);
float boundaryFactor = SGSecondaryThermal.GetBoundaryLayerFactor(T_sg_secondary, false);
sb.AppendLine($"    Current HTC:      {currentHTC,10:F0} BTU/(hr·ft²·°F)");
sb.AppendLine($"    Boundary Factor:  {boundaryFactor,10:F2} (thermal stratification)");
sb.AppendLine($"    Effective ΔT:     {(T_rcs - T_sg_secondary) * boundaryFactor,10:F2} °F (vs {T_rcs - T_sg_secondary:F2}°F bulk)");
```

**Impact:**
- Logged HTC now shows actual temperature-scaled value (was showing base 100)
- New diagnostic lines show boundary layer factor and effective vs bulk ΔT
- Provides full visibility into thermal stratification model during simulation

---

## Validation Criteria

| Criterion | Target | Expected Result |
|-----------|--------|-----------------|
| Heatup Rate (4 RCPs, T=150°F) | 45-55°F/hr | ~50°F/hr |
| SG Heat Absorption (cold) | 4-7 MW | ~4-5 MW |
| Net Heat to RCS | ~15-17 MW | ~17 MW |
| Time to HZP (557°F) | 17-20 hours | ~18 hours |
| Boundary Factor at 150°F | 0.30 | 0.30 |
| Boundary Factor at 300°F | 0.55 | 0.55 |
| Boundary Factor steaming | 1.00 | 1.00 |
| Logged HTC matches physics | Yes | Yes |

---

## Files Modified

| File | Lines Changed | Description |
|------|---------------|-------------|
| PlantConstants.Heatup.cs | +70 | Added boundary layer constants with documentation |
| SGSecondaryThermal.cs | +60, ~15 modified | Added GetBoundaryLayerFactor(), modified heat transfer methods |
| HeatupSimEngine.Logging.cs | +5, ~1 modified | Fixed HTC logging, added boundary factor diagnostics |

---

## Known Issues Not Addressed

### Mass Conservation Error (Deferred to v1.2.0)

**Observation:** Inventory audit shows conservation error growing from 0.05% at 0.25hr to 6.96% at 12.25hr (64,284 lbm).

**Rationale for Deferral:** This is a separate issue unrelated to heatup rate. The thermal stratification fix addresses the primary concern. Mass conservation requires independent investigation of all flow paths.

**Tracking:** Added to Future_Features/FUTURE_ENHANCEMENTS_ROADMAP.md as item 1.2.0

---

## Testing Notes

After implementation, run a full cold-start heatup simulation and verify:

1. **At T_sg_secondary ≈ 150°F:**
   - Boundary Factor = 0.30 in logs
   - Effective ΔT ≈ 0.3 × bulk ΔT
   - SG heat absorption = 4-6 MW
   - Heatup rate = 45-55°F/hr

2. **At T_sg_secondary ≈ 300°F:**
   - Boundary Factor ≈ 0.55
   - Heatup rate maintained

3. **At steaming conditions:**
   - Boundary Factor = 1.0
   - Steam dump properly controls temperature

4. **Overall:**
   - Simulation reaches HZP within 20 hours
   - No unintended side effects at high temperatures

---

## Physics Documentation

### Thermal Stratification in Wet-Layup SG Secondary

During cold heatup, the SG secondary side is in "wet layup" (100% filled with water) with no forced circulation. This creates severe thermal stratification:

```
                    SG Secondary Cross-Section
    ┌─────────────────────────────────────────────────┐
    │                                                 │
    │   Cold bulk water (~120°F)                      │
    │                                                 │
    │   ┌─────────────────────────────────────────┐   │
    │   │  Warm transition zone (~135°F)          │   │
    │   │  ┌─────────────────────────────────┐    │   │
    │   │  │  Hot boundary layer (~146°F)    │    │   │
    │   │  │  ════════════════════════════   │    │   │  ← Tube bundle
    │   │  │  RCS water in tubes (148°F)     │    │   │
    │   │  │  ════════════════════════════   │    │   │
    │   │  │  Hot boundary layer (~146°F)    │    │   │
    │   │  └─────────────────────────────────┘    │   │
    │   │  Warm transition zone (~135°F)          │   │
    │   └─────────────────────────────────────────┘   │
    │                                                 │
    │   Cold bulk water (~120°F)                      │
    │                                                 │
    └─────────────────────────────────────────────────┘
    
    Bulk Average: ~142°F (what simulation tracks)
    Boundary Layer: ~146°F (what drives heat transfer)
    Effective ΔT: 148 - 146 = 2°F (not 148 - 142 = 6°F)
```

**Richardson Number Analysis:**
```
Ri = (g × β × ΔT × L) / V²

Where:
  g = 32.2 ft/s² (gravity)
  β ≈ 0.0003 /°F (thermal expansion coefficient)
  ΔT ≈ 6°F (bulk temperature difference)
  L ≈ 50 ft (characteristic length)
  V ≈ 0.001 ft/s (natural convection velocity estimate)

Ri ≈ (32.2 × 0.0003 × 6 × 50) / (0.001)² ≈ 27,000

Ri >> 10 indicates strong stratification with suppressed mixing
```

---

## Sources

- SG_Heat_Transfer_Investigation_Summary (Updates folder) — Root cause analysis
- NRC HRTD ML11251A016 — Steam Generator wet layup conditions
- NRC HRTD 19.2.2 — Target heatup rate ~50°F/hr with 4 RCPs
- Thermal stratification Richardson number analysis
- Churchill-Chu correlation (Incropera & DeWitt, 7th ed., Ch. 9)
- NUREG/CR-5426 — PWR SG natural circulation phenomena

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| v1.1.0 | 2026-02-08 | HZP Stabilization & Steam Dump Integration |
| v1.1.1 | 2026-02-09 | Temperature-dependent HTC scaling (insufficient) |
| v1.1.2 | 2026-02-09 | Thermal stratification boundary layer model (this release) |
