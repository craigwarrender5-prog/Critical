# Changelog - Multi-Node Steam Generator Thermal Model

**Version:** 1.3.0  
**Date:** 2026-02-10  
**Type:** Major Feature — Physics Model Replacement

---

## [1.3.0] - 2026-02-10

### Summary

Replaces the lumped-parameter SG secondary thermal model (SGSecondaryThermal.cs) with a vertically-stratified multi-node model (SGMultiNodeThermal.cs) to accurately capture thermal stratification in stagnant wet-layup steam generators during RCS heatup. This resolves the fundamental root cause of excessive SG heat absorption (~14 MW) that limited heatup rates to ~23-27°F/hr instead of the expected 40-50°F/hr for a real Westinghouse 4-Loop PWR.

**Root Cause:** The lumped model treated the entire 220,000 ft² tube surface as thermally active, requiring artificial correction factors (HTC scaling × boundary layer factor) that could not adequately reduce heat transfer. Even with both factors at 0.30, the enormous surface area dominated: Q = 100 × 0.3 × 220,000 × 21 × 0.3 ≈ 14 MW.

**Physical Reality:** In a stagnant wet-layup SG secondary (~415,000 lb water per SG, no forced circulation), the Richardson number is ~27,000 (>> 10 = strongly stratified). Only the top fraction of tubes (near the U-bend apex) contacts convective cells; the rest is insulated by cold stagnant water. The multi-node model captures this by dividing the secondary into N vertical nodes with position-dependent effectiveness factors.

**Expected Results:**
- Early heatup (100-200°F): SG absorbs 3-5 MW → heatup rate ~45-55°F/hr
- Mid heatup (200-350°F): SG absorbs 5-10 MW as stratification develops
- Late heatup (350-500°F): Circulation onset, SG absorbs 10-15 MW
- Energy balance: RCS heat input = SG absorption + insulation losses + RCS temp rise

---

### Added

#### SGMultiNodeThermal.cs (`Assets/Scripts/Physics/SGMultiNodeThermal.cs`) — ~680 lines — NEW

Multi-node vertically-stratified SG secondary side thermal model.

- **Architecture:** N vertical nodes (default 5) from tubesheet (bottom) to U-bend apex (top), each with independent temperature, area fraction, mass fraction, and effectiveness factor
- **State Struct (SGMultiNodeState):** NodeTemperatures[], NodeHeatRates[], NodeHTCs[], NodeEffectiveAreaFractions[], TotalHeatAbsorption_MW, BulkAverageTemp_F, TopNodeTemp_F, BottomNodeTemp_F, StratificationDeltaT_F, CirculationFraction, CirculationActive
- **Result Struct (SGMultiNodeResult):** TotalHeatRemoval_MW, BulkAverageTemp_F, TopNodeTemp_F, RCS_SG_DeltaT_F, CirculationFraction, CirculationActive
- **Public API:**
  - `Initialize(initialTempF)` → SGMultiNodeState
  - `Update(ref state, T_rcs, rcpsRunning, pressurePsia, dt_hr)` → SGMultiNodeResult
  - `GetDiagnosticString(state, T_rcs)` → string
- **Physics Sequence (per timestep):**
  1. Calculate circulation state from node temperature profile (smooth cosine interpolation)
  2. Calculate per-node effective HTC and area fraction
  3. Calculate per-node heat transfer: Q_i = h_i × A_eff_i × (T_rcs − T_node_i)
  4. Apply inter-node mixing/conduction (UA = 500 stagnant, 50,000 circulating BTU/(hr·°F))
  5. Update node temperatures from energy balance
  6. Sum total heat removal for RCS energy balance
- **Circulation Onset:** Cosine interpolation from 0→1 between ΔT(top-bottom) = 30°F (onset) and 80°F (full)
- **Temperature Efficiency Factor:** 0.5 at 100°F → 1.0 at 400°F (accounts for viscosity/β effects on Rayleigh number)
- **Validation Tests (ValidateModel()):** 7 test cases covering initialization, stratification development, heat rate bounds, area/mass fraction conservation, circulation fraction behavior

**Sources:** Churchill-Chu correlation (Incropera & DeWitt Ch. 9), WCAP-8530, NRC HRTD ML11223A213, NUREG/CR-5426, NRC Bulletin 88-11

---

#### PlantConstants.SG.cs (`Assets/Scripts/Physics/PlantConstants.SG.cs`) — ~450 lines — NEW

Partial class extension of PlantConstants with SG tube bundle and multi-node thermal model design parameters.

- **Tube Geometry:** 5,626 tubes/SG, 0.75" OD, 0.043" wall, Inconel 690, 21 ft straight leg + 3 ft U-bend average
- **Heat Transfer Area:** 55,000 ft²/SG (4 SGs = 220,000 ft² total)
- **Secondary Inventory:** ~415,000 lb water per SG in wet layup
- **Multi-Node Parameters (5 nodes default):**
  - Area fractions: [0.25, 0.20, 0.20, 0.20, 0.15] (top to bottom)
  - Mass fractions: [0.15, 0.20, 0.25, 0.25, 0.15] (top to bottom)
  - Stagnant effectiveness: [0.40, 0.15, 0.05, 0.02, 0.01] (top active, bottom insulated)
- **HTC Values:** Stagnant 50, Active NC 200, Full Circulation 400 BTU/(hr·ft²·°F)
- **Circulation Onset:** ΔT threshold 30°F, full at 80°F

**Sources:** WCAP-8530 / WCAP-12700 (SG design report), NRC HRTD ML11223A213 Section 5.0, NRC HRTD ML11251A016 (wet layup), NUREG/CR-5426 (natural circulation phenomena), NRC Bulletin 88-11 (thermal stratification)

---

### Changed

#### RCSHeatup.cs (`Assets/Scripts/Physics/RCSHeatup.cs`) — MODIFIED

- **BulkHeatupResult struct:** Added fields `SG_TopNodeTemp_F`, `SG_CirculationFraction`, `SG_CirculationActive`
- **BulkHeatupStep() signature changed:**
  - OLD: `float T_sg_secondary` (input temp; model calculated Q internally via SGSecondaryThermal)
  - NEW: `float sgHeatRemoval_MW = 0f, float T_sg_bulk = 0f` (Q computed externally by SGMultiNodeThermal, passed in as input)
- **Removed:** Internal `SGSecondaryThermal.CalculateHeatTransfer()` call from BulkHeatupStep
- **Energy balance:** SG heat removal is now subtracted directly from net heat input as an external parameter

#### HeatupSimEngine.cs (`Assets/Scripts/Validation/HeatupSimEngine.cs`) — MODIFIED

- **New public state fields:** `sgMultiNodeState` (SGMultiNodeState), `sgTopNodeTemp`, `sgBottomNodeTemp`, `sgStratificationDeltaT`, `sgCirculationFraction`, `sgCirculationActive`
- **REGIME 2 (RCPs Ramping):** Now calls `SGMultiNodeThermal.Update()` before `BulkHeatupStep()`, passes `sgResult.TotalHeatRemoval_MW` and `sgMultiNodeState.BulkAverageTemp_F` to physics. Updates all SG display fields from multi-node state.
- **REGIME 3 (All RCPs Running):** Same pattern — calls `SGMultiNodeThermal.Update()` before `BulkHeatupStep()`, passes MW result. Replaces old `heatupResult.T_sg_secondary` / `heatupResult.SGHeatTransfer_MW` readback.

#### HeatupSimEngine.Init.cs (`Assets/Scripts/Validation/HeatupSimEngine.Init.cs`) — MODIFIED

- **InitializeSimulation():** Added `sgMultiNodeState = SGMultiNodeThermal.Initialize(startTemperature)` and initialization of all new SG display state fields to starting temperature / zero / false.

#### HeatupSimEngine.Logging.cs (`Assets/Scripts/Validation/HeatupSimEngine.Logging.cs`) — MODIFIED

- **SaveIntervalLog() SG section:** Replaced v0.8.0 lumped-model logging (SGSecondaryThermal HTC, boundary factor, operating regime) with v1.3.0 multi-node data: bulk avg temp, top/bottom node temps, stratification ΔT, circulation fraction, circulation state, per-node diagnostic string.

---

### Architectural Notes

**Decoupled SG Model:** The multi-node SG model is updated externally by the engine *before* calling `RCSHeatup.BulkHeatupStep()`. The engine passes the computed SG heat removal (MW) as an input parameter to the RCS physics step. This is cleaner than the v0.8.0 approach where SG heat transfer was computed inside `BulkHeatupStep()` — the new approach maintains single responsibility (G1) and allows the SG model to evolve independently.

**SGSecondaryThermal.cs retained:** The old lumped model is not deleted. It is still used by the HZP systems (HeatupSimEngine.HZP.cs) for steaming detection and steam dump calculations at near-HZP conditions where forced secondary circulation exists and lumped-parameter modeling is appropriate.

**No HZP Changes:** The HZP stabilization system (v1.1.0) continues to use SGSecondaryThermal for steaming detection. The multi-node model is for heatup-phase SG heat absorption only. Future work may unify these models.

---

### GOLD Certification Checklist

#### SGMultiNodeThermal.cs
| # | Criterion | Status |
|---|-----------|--------|
| G1 | Single Responsibility | ✅ SG multi-node thermal physics only |
| G2 | Documented Header | ✅ Purpose, physics, sources, units, architecture |
| G3 | No Inline Physics in Engines | ✅ Engine calls Update(), no internal physics |
| G4 | Result Structs | ✅ SGMultiNodeState, SGMultiNodeResult |
| G5 | Constants from PlantConstants | ✅ All values from PlantConstants.SG |
| G6 | Cited Sources | ✅ WCAP-8530, NRC HRTD, NUREG/CR-5426, Incropera & DeWitt |
| G7 | Namespace Critical.Physics | ✅ |
| G8 | File Size Reasonable | ✅ ~680 lines |
| G9 | No Dead Code | ✅ |
| G10 | No Duplication | ✅ |

#### PlantConstants.SG.cs
| # | Criterion | Status |
|---|-----------|--------|
| G1 | Single Responsibility | ✅ SG design constants only |
| G2 | Documented Header | ✅ Domain, sources, units |
| G3 | No Inline Physics in Engines | ✅ Constants only, no calculations |
| G4 | Result Structs | ✅ N/A (constants file) |
| G5 | Constants from PlantConstants | ✅ Is PlantConstants partial |
| G6 | Cited Sources | ✅ All values cite WCAP/NRC/Incropera sources |
| G7 | Namespace Critical.Physics | ✅ |
| G8 | File Size Reasonable | ✅ ~450 lines |
| G9 | No Dead Code | ✅ |
| G10 | No Duplication | ✅ |

---

### File Summary

| File | Status | Lines | Purpose |
|------|--------|-------|---------|
| SGMultiNodeThermal.cs | NEW | ~680 | Multi-node SG thermal physics |
| PlantConstants.SG.cs | NEW | ~450 | SG design constants |
| RCSHeatup.cs | MODIFIED | — | Signature change: SG Q as input |
| HeatupSimEngine.cs | MODIFIED | — | Regime 2 & 3 SG integration |
| HeatupSimEngine.Init.cs | MODIFIED | — | Multi-node state initialization |
| HeatupSimEngine.Logging.cs | MODIFIED | — | Interval log SG section update |

---

### References

| Document | Identifier | Used For |
|----------|-----------|----------|
| Westinghouse SG Design Report | WCAP-8530 / WCAP-12700 | Tube count, geometry, dimensions |
| NRC HRTD Steam Generators | ML11223A213 Section 5.0 | SG thermal-hydraulic data |
| NRC HRTD SG Wet Layup | ML11251A016 | Secondary water inventory |
| NRC SG Natural Circulation | NUREG/CR-5426 | NC onset criteria, stratification |
| NRC Thermal Stratification | Bulletin 88-11 | Stratification in PWR systems |
| Heat Transfer Textbook | Incropera & DeWitt, 7th ed., Ch. 9 | Churchill-Chu correlation |
| NRC Heatup Operations | ML11223A342 Section 19.2.2 | ~50°F/hr heatup rate target |

---

**End of Changelog**
