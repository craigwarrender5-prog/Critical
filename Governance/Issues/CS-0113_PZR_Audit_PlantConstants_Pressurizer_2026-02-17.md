# CS-0113: Pressurizer System Audit — PlantConstants.Pressurizer.cs

**Created:** 2026-02-17  
**Status:** OPEN  
**Priority:** Medium  
**Category:** Audit Finding / Parameter Validation  
**File:** `Assets/Scripts/Physics/PlantConstants.Pressurizer.cs`

---

## Summary

Comprehensive audit of PlantConstants.Pressurizer.cs against NRC HRTD and Westinghouse 4-Loop PWR specifications. File is marked GOLD STANDARD. Most parameters verified correct; several minor discrepancies and missing parameters identified.

---

## Discrepancies Found

### D1: Spray Bypass Flow (Minor)
- **Parameter:** `SPRAY_BYPASS_FLOW_GPM`
- **Code Value:** 1.5 gpm
- **Documentation Value:** 2 gpm total (1 gpm per valve × 2 valves)
- **Source:** Westinghouse_4Loop_Pressurizer_Specifications_Summary.md
- **Action:** Correct to 2.0 gpm

### D2: Proportional Heater Comment (Cosmetic)
- **Location:** Comment for `HEATER_POWER_PROP`
- **Code Comment:** "2 banks × 150 kW = 300 kW"
- **Actual Configuration:** 1 bank (Bank C) × 414 kW = 414 kW
- **Source:** Westinghouse Specs Table
- **Action:** Update comment

### D3: Backup Heater Comment (Cosmetic)
- **Location:** Comment for `HEATER_POWER_BACKUP`
- **Code Comment:** "5 banks × 300 kW"
- **Actual Configuration:** 3 banks (A+B+D) × 460 kW = 1380 kW
- **Source:** Westinghouse Specs Table
- **Action:** Update comment

---

## Parameters Without Verified Citations

| Parameter | Code Value | Notes | Priority |
|-----------|------------|-------|----------|
| `PZR_WALL_MASS` | 200,000 lb | No citation found | Medium |
| `PZR_WALL_AREA` | 600 ft² | No citation found | Medium |
| `HEATER_TAU` | 20 sec | Engineering judgment | Low |
| `MAX_PZR_SPRAY_DELTA_T` | 320°F | No specific NRC cite | Medium |
| `SPRAY_TEMP` | 558°F | Reasonable but no cite | Low |
| `SPRAY_EFFICIENCY` | 0.85 | Engineering estimate | Low |
| `PZR_STEAM_MIN` | 50 ft³ | No citation | Low |
| `PZR_WATER_MAX` | 1750 ft³ | No citation | Low |

**Recommendation:** Research authoritative sources for PZR_WALL_MASS, PZR_WALL_AREA, and MAX_PZR_SPRAY_DELTA_T. Add to Technical_Documentation if found.

---

## Missing Parameters (Per NRC HRTD 10.3)

| Parameter | Value | Function | Priority |
|-----------|-------|----------|----------|
| Low level isolation | 17% | Heater cutoff, letdown isolation | **High** |
| High level alarm | 70% | Alarm only | Low |
| High level trip | 92% | Reactor trip (2/3 coincidence) | Medium |
| Backup heater anticipatory | Program + 5% | Level-based heater energization | Medium |
| High pressure trip | 2385 psig | Reactor trip (2/4 coincidence) | Medium |
| Low pressure trip | 1865 psig | DNB protection (at-power) | Low |
| Code safety lift | 2485 psig | Final overpressure protection | Low |

---

## Level Program Discontinuity (Documentation)

The code correctly implements two separate level programs:
- **Heatup:** 25%→60% over 200→557°F
- **At-Power:** 25%→61.5% over 557→584.7°F

At 557°F, switching from heatup (60%) to at-power (25%) creates a 35% step. This is **procedurally correct** per NRC HRTD 10.3 (Mode 3 entry resets to no-load reference).

**Recommendation:** Add explicit comment in `GetPZRLevelSetpointUnified()` explaining the procedural basis for this discontinuity.

---

## Verified Correct Parameters

All major control setpoints verified against NRC HRTD 10.2/10.3:
- Total volume: 1800 ft³ ✓
- Height: 52.75 ft ✓
- Heater power: 1794 kW total (414 prop + 1380 backup) ✓
- Spray flow: 840 gpm max ✓
- Pressure setpoint: 2235 psig ✓
- All heater/spray band setpoints ✓
- Level program parameters ✓

---

## Recommended Actions

| # | Action | Severity | Effort |
|---|--------|----------|--------|
| 1 | Correct `SPRAY_BYPASS_FLOW_GPM` to 2.0 gpm | Minor | Trivial |
| 2 | Fix heater bank comments (D2, D3) | Cosmetic | Trivial |
| 3 | Add `PZR_LOW_LEVEL_ISOLATION_PERCENT = 17f` | Medium | Trivial |
| 4 | Add `PZR_HIGH_LEVEL_TRIP_PERCENT = 92f` | Medium | Trivial |
| 5 | Research and cite PZR_WALL_MASS, PZR_WALL_AREA | Medium | Moderate |
| 6 | Add comment explaining 557°F level discontinuity | Low | Trivial |

---

## References

- NRC HRTD 10.2 — Pressurizer Pressure Control
- NRC HRTD 10.3 — Pressurizer Level Control
- NRC HRTD 6.1 — Pressurizer Heaters
- Westinghouse_4Loop_Pressurizer_Specifications_Summary.md
- NRC_HRTD_Section_10.3_Pressurizer_Level_Control.md

---

## Disposition

- [ ] Corrections implemented
- [ ] GOLD status maintained
- [ ] Changelog updated
