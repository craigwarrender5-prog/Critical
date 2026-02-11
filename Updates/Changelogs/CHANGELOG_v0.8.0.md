# Changelog v0.8.0 — SG Secondary Side Thermal Mass Model

**Date:** 2026-02-07  
**Type:** Minor Release (Feature Addition)  
**Scope:** Physics, Engine, Dashboard

---

## Summary

Implements the Steam Generator Secondary Side Thermal Mass Model to correct RCS heatup rate calculation. The previous simulator showed ~71°F/hr heatup with 4 RCPs vs the expected ~50°F/hr per NRC HRTD 19.2.2. The root cause was the absence of the SG secondary side as a heat sink during heatup operations.

The SG secondary side represents the single largest thermal mass in the system:
- ~1.66 million lb of water (wet layup at cold shutdown)
- ~800,000 lb of metal (SG tubes, shell, internals)
- Combined heat capacity ~2.5 MBTU/°F

During heatup, this massive heat sink absorbs approximately 40% of RCP heat input, significantly slowing the RCS temperature rise to realistic values.

---

## Changes by Stage

### Stage 1: SGSecondaryThermal.cs Physics Module

**File:** `Assets/Scripts/Physics/SGSecondaryThermal.cs`

- Created lumped-parameter thermal model for all 4 SG secondary sides combined
- Heat transfer equation: Q = U × A × ΔT
- Heat transfer coefficient varies with RCP status:
  - No RCPs: 10 BTU/(hr·ft²·°F) — natural convection only
  - RCPs running: 200 BTU/(hr·ft²·°F) — forced primary convection
- All constants sourced from PlantConstants (no private constants)
- Public interface:
  - `GetSecondaryHeatCapacity(T, P)` — returns total heat capacity (BTU/°F)
  - `CalculateHeatTransfer(T_rcs, T_sg, rcps)` — returns Q (BTU/hr)
  - `UpdateSecondaryTemperature(...)` — lumped capacitance integration
  - `InitializeSecondaryTemperature(T_rcs)` — thermal equilibrium at startup
  - Diagnostic methods: `GetThermalMassContribution`, `GetOperatingRegime`, `GetCurrentHTC`

**Constants verified in PlantConstants.Heatup.cs:**
| Constant | Value | Description |
|----------|-------|-------------|
| `SG_SECONDARY_TOTAL_METAL_MASS_LB` | 800,000 | Total SG metal mass (all 4 SGs) |
| `SG_SECONDARY_TOTAL_WATER_MASS_LB` | 1,660,000 | Total secondary water (wet layup) |
| `SG_TUBE_AREA_TOTAL_FT2` | 220,000 | Total tube surface area (all 4 SGs) |
| `SG_HTC_NO_FLOW` | 10 | Natural convection HTC |
| `SG_HTC_FORCED_PRIMARY` | 200 | Forced convection HTC |
| `STEEL_CP` | 0.12 | Steel specific heat (BTU/lb·°F) |

### Stage 2: Engine Integration

**File:** `Assets/Scripts/Physics/RCSHeatup.cs`
- `BulkHeatupStep()` now accepts `T_sg_secondary` parameter
- Calculates heat transfer to SG secondary via `SGSecondaryThermal.CalculateHeatTransfer()`
- Subtracts SG heat absorption from RCS net heat before temperature integration
- Returns updated `T_sg_secondary` and `sgHeatTransfer_MW` in `BulkHeatupResult` struct

**File:** `Assets/Scripts/Validation/HeatupSimEngine.cs`
- Added public fields: `T_sg_secondary`, `sgHeatTransfer_MW`
- Added history buffer: `tSgSecondaryHistory`
- SG secondary temperature passed to physics in Regime 2 (blended) and Regime 3 (coupled)
- SG secondary state updated from physics result each timestep

**File:** `Assets/Scripts/Validation/HeatupSimEngine.Init.cs`
- `T_sg_secondary` initialized via `SGSecondaryThermal.InitializeSecondaryTemperature()`
- Starts at thermal equilibrium with RCS (same temperature at cold shutdown)

**File:** `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs`
- Added `T_SG_SEC` to TEMPERATURES section in interval logs
- Added `RCS-SG Delta` (thermal lag indicator)
- Added `SG Secondary Loss` to HEAT SOURCES section
- Added dedicated `SG SECONDARY SIDE` log section with:
  - Temperature and thermal lag
  - Heat transfer rate (MW and MBTU/hr)
  - Heat capacity
  - Operating regime and HTC value
- Added `tSgSecondaryHistory` to `AddHistory()` and `ClearHistoryAndEvents()`

### Stage 3: Dashboard Integration

**File:** `Assets/Scripts/Validation/HeatupValidationVisual.Gauges.cs`
- Added `RCS-SG ΔT` mini bar to TEMPERATURES gauge group
- Color-coded indicator:
  - Cyan: Normal thermal lag (5-30°F when RCPs running)
  - Amber: Warning if thermal lag >30°F or <5°F
  - Gray: Inactive when no RCPs running
- Updated `TEMP_GROUP_H` constant for additional bar height

**File:** `Assets/Scripts/Validation/HeatupValidationVisual.Graphs.cs`
- Added `T_SG_SEC` trace (cyan, `_cTrace6`) to TEMPS trend graph
- Now displays 4 temperature traces: T_RCS, T_HOT, T_COLD, T_SG_SEC
- Updated file header comments to document new trace and history buffer

---

## Physics Model Details

### Lumped-Parameter Representation

All 4 SG secondary sides combined into single thermal node:
- Simplification valid because all SGs operate identically during heatup
- Secondary side in wet layup (100% filled with water) during cold shutdown
- No steam generation modeled (secondary remains subcooled)

### Heat Transfer Model

```
Q = U × A × (T_rcs - T_sg_secondary)

Where:
  Q = Heat transfer rate (BTU/hr)
  U = Overall heat transfer coefficient (BTU/hr·ft²·°F)
  A = Total tube surface area = 220,000 ft² (all 4 SGs)
  ΔT = Temperature difference primary to secondary
```

### Heat Transfer Coefficient Selection

| Condition | HTC (BTU/hr·ft²·°F) | Rationale |
|-----------|---------------------|-----------|
| No RCPs | 10 | Natural convection on primary side |
| RCPs Running | 200 | Forced convection from primary flow |

### Temperature Integration

```
dT_sg/dt = Q / C_sg

Where:
  C_sg = M_metal × Cp_steel + M_water × Cp_water(T)
       ≈ 800,000 × 0.12 + 1,660,000 × 1.0
       ≈ 1,756,000 BTU/°F at low temperature
```

Forward Euler integration used (stable with large thermal mass and small timesteps).

---

## Validation Targets

Per implementation plan IMPL_PLAN_v0.8.0:

| Parameter | Previous | Target | Source |
|-----------|----------|--------|--------|
| RCS heatup rate (4 RCPs) | ~71°F/hr | 45-55°F/hr | NRC HRTD 19.2.2 |
| SG secondary thermal lag | N/A | 10-20°F | Realistic coupling |
| Heat sink effect | 0% | ~40% of RCP heat | Energy balance |

---

## Files Modified

| File | Type | Changes |
|------|------|---------|
| `Assets/Scripts/Physics/SGSecondaryThermal.cs` | New | Physics module for SG secondary thermal model |
| `Assets/Scripts/Physics/RCSHeatup.cs` | Modified | Added T_sg_secondary parameter and heat sink calculation |
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | Modified | Added T_sg fields and history buffer |
| `Assets/Scripts/Validation/HeatupSimEngine.Init.cs` | Modified | Added T_sg_secondary initialization |
| `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs` | Modified | Added comprehensive SG secondary logging |
| `Assets/Scripts/Validation/HeatupValidationVisual.Gauges.cs` | Modified | Added RCS-SG ΔT gauge |
| `Assets/Scripts/Validation/HeatupValidationVisual.Graphs.cs` | Modified | Added T_SG_SEC trace to temperature graph |

---

## References

- NRC HRTD 19.2.2: Reactor Coolant System Heatup
- NRC HRTD 6.1: Steam Generator Design and Operation
- Westinghouse 4-Loop PWR Technical Specifications
- Westinghouse Model F Steam Generator Specifications
- Implementation Plan: `Updates and Changelog/IMPL_PLAN_v0.8.0.md`

---

## Notes

- Stage 4 (Validation Testing and HTC Tuning) deferred pending simulation runs
- HTC values may require adjustment based on validation results
- SG secondary model only active during heatup (Modes 5→3)
- Model assumes wet layup secondary side (standard cold shutdown configuration)
