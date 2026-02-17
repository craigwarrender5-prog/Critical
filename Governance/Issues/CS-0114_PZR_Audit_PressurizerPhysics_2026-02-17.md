# CS-0114: Pressurizer System Audit — PressurizerPhysics.cs

**Created:** 2026-02-17  
**Status:** OPEN  
**Priority:** Medium  
**Category:** Audit Finding / Physics Validation  
**File:** `Assets/Scripts/Physics/PressurizerPhysics.cs`

---

## Summary

Comprehensive audit of PressurizerPhysics.cs thermal-hydraulic calculations. File implements solid plant operations, bubble formation, three-region model, and phase change physics. Not currently GOLD STANDARD certified. Core physics structure is sound; several engineering estimates lack citations.

---

## Physics Verification

### Verified Correct
- Solid PZR thermal balance (heaters, surge line, ambient)
- Bubble formation trigger (T_water >= T_sat - 0.5°F)
- Mass conservation (v5.0.2/v5.4.2.0 fixes)
- Heater thermal lag model (first-order, tau=20s)
- Spray condensation (85% efficiency, latent heat basis)
- Three-region mass balance structure

### Technical Issues

#### P1: Constant dTsat/dP (Medium)
- **Location:** `FlashEvaporationRate()` line 373
- **Current:** `float dTsatdP = 0.04f; // °F/psi`
- **Issue:** This derivative varies significantly with pressure:
  - At 365 psia: dTsat/dP ≈ 0.085 °F/psi
  - At 2250 psia: dTsat/dP ≈ 0.025 °F/psi
- **Impact:** Flash rate calculation error up to 2x at pressure extremes
- **Fix:** Replace with `WaterProperties.dTsatdP(pressure)` or lookup table

#### P2: Ambient Heat Loss Inconsistency (Minor)
- **Location:** `SolidPressurizerUpdate()` line 77
- **Current:** Uses "50 kW at reference ΔT" calculation
- **PlantConstants:** `AMBIENT_HEAT_LOSS_KW = 42.5 kW`
- **Fix:** Use PlantConstants value for consistency

---

## Uncited Engineering Estimates

These values appear reasonable but lack authoritative citations:

| Parameter | Value | Location | Priority |
|-----------|-------|----------|----------|
| Flash efficiency | 0.80 | FlashEvaporationRate() | Low |
| Max flash rate | 1%/sec of water mass | FlashEvaporationRate() | Low |
| Steam-wall HTC | 50 BTU/hr-ft²-°F | UpdateWallTemperature() | Low |
| Liquid-wall HTC | 200 BTU/hr-ft²-°F | UpdateWallTemperature() | Low |
| Rainout tau | 5 seconds | RainoutRate() | Low |
| Rainout subcool divisor | 100°F | RainoutRate() | Low |
| Max rainout | 10%/sec of steam mass | RainoutRate() | Low |
| Two-phase damping | 0.5 | TwoPhaseHeatingUpdate() | Low (documented) |

**Recommendation:** Add Technical_Documentation/Pressurizer_Physics_Engineering_Basis.md documenting the rationale for these values.

---

## External References to Verify

The following constants/methods are referenced but defined in other files. Verify existence and correctness:

### PlantConstants References
- `P_SPRAY_ON` — SprayFlowDemand()
- `P_SPRAY_FULL` — SprayFlowDemand()
- `P_HEATERS_ON` — HeaterPowerDemand()
- `P_HEATERS_OFF` — HeaterPowerDemand()
- `T_COLD` — UpdateWallTemperature()
- `GPM_TO_FT3_SEC` — Multiple locations
- `KW_TO_BTU_SEC` — Multiple locations
- `MW_TO_BTU_SEC` — SolidPressurizerUpdate()

### Other Module References
- `HeatTransfer.SurgeLineHeatTransfer_MW()` — SolidPressurizerUpdate()
- `HeatTransfer.CondensingHTC()` — WallCondensationRate()
- `ThermalExpansion.PressureChangeFromTemp()` — TwoPhaseHeatingUpdate()
- `ThermalExpansion.ExpansionCoefficient()` — TwoPhaseHeatingUpdate()
- `ThermalMass.PressurizerWallHeatCapacity()` — Multiple locations
- `WaterProperties.*` — Multiple steam table lookups

---

## Code Quality

### Strengths
- Comprehensive validation suite (14 tests)
- Clear documentation of physics gaps addressed (#2-8)
- Proper solid vs. two-phase state handling
- Explicit mass conservation enforcement (v5.0.2, v5.4.2.0)

### Weaknesses
- Not GOLD STANDARD certified
- Multiple hardcoded "magic numbers"
- Pressure-dependent derivative approximated as constant

---

## Recommended Actions

| # | Action | Severity | Effort |
|---|--------|----------|--------|
| 1 | Replace `dTsatdP = 0.04f` with pressure-dependent calculation | Medium | Moderate |
| 2 | Reconcile ambient loss with PlantConstants.AMBIENT_HEAT_LOSS_KW | Minor | Trivial |
| 3 | Create engineering basis document for uncited estimates | Low | Moderate |
| 4 | Verify all external PlantConstants/module references exist | Medium | Low |
| 5 | Consider GOLD STANDARD certification after fixes | Medium | N/A |

---

## Validation Test Coverage

The file includes 14 validation tests covering:
- Flash evaporation (positive during depressurization, zero during pressurization)
- Spray condensation (range 5-50 lb/sec)
- Heater steam rate (range 1-10 lb/sec)
- Heater lag response (63% at 1τ, 95% at 3τ)
- Wall condensation (positive when wall cold)
- Rainout (positive when steam subcooled)
- Solid PZR initialization (correct volumes/masses)
- Bubble formation trigger (T_sat detection)
- FormBubble transition (creates steam space)
- SolidPressurizerUpdate mass invariance (v5.0.2)

---

## References

- NRC HRTD 19.2.1 — Solid Plant Operations
- NRC HRTD 19.2.2 — Bubble Formation Procedure
- Steam Tables (pressure-dependent properties)

---

## Disposition

- [ ] P1: dTsat/dP pressure dependency implemented
- [ ] P2: Ambient loss reconciled
- [ ] External references verified
- [ ] Engineering basis documented
- [ ] GOLD status evaluated
