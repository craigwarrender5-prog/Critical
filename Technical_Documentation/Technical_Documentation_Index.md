# Technical Documentation Index

**Last Updated:** 2026-02-16  
**Purpose:** Unified index of all technical documentation supporting Critical: Master the Atom simulator development.

---

## Quick Reference

| Category | Document Count | Coverage |
|----------|----------------|----------|
| NRC HRTD Sections | 16 | Core systems, controls, protection, instrumentation, ESFAS, feedwater, pressurizer |
| Research Documents | 4 | SG thermal physics, RHR system |
| Reference Compilations | 4 | Startup/pressurization, PZR analysis, specifications, authority decisions |
| Meta-Documentation | 3 | Indexes, summaries, analysis |

---

## NRC HRTD Section Documents

### Reactor Vessel and Core

| Document | Tags | Description | Priority |
|----------|------|-------------|----------|
| `NRC_HRTD_Section_3.1_Reactor_Vessel_and_Internals.md` | `[VESSEL]` `[CORE]` `[FUEL]` `[CRDM]` | Reactor vessel design, internals, fuel assemblies, CRDMs, core flow paths | HIGH |
| `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` | `[RCS]` `[PZR]` `[RCP]` `[SG]` | **NEW 2026-02-15** — Detailed RCS design, pressurizer specifications (1800 ft³, 1794 kW heaters, 840 gpm spray), RCP seals, SG design, P-T limits | **CRITICAL** |

### Engineered Safety Features

| Document | Tags | Description | Priority |
|----------|------|-------------|----------|
| `NRC_HRTD_Section_5.1_Residual_Heat_Removal_System.md` | `[RHR]` `[COOLDOWN]` `[ECCS]` | Complete RHR system design, cooldown, solid plant ops, ECCS function | HIGH |
| `NRC_HRTD_Section_5.7_Auxiliary_Feedwater_System.md` | `[AFW]` `[SAFETY]` `[DECAY_HEAT]` | AFW pumps, start signals, water supplies, PRA insights | MEDIUM |

### Secondary Systems

| Document | Tags | Description | Priority |
|----------|------|-------------|----------|
| `NRC_HRTD_Section_7.1_Main_Auxiliary_Steam.md` | `[STEAM]` `[PORV]` `[MSIV]` `[AFW]` | Main steam design, PORVs, safety valves, flow restrictors | HIGH |
| `NRC_HRTD_Section_7.2_Condensate_and_Feedwater_System.md` | `[FEEDWATER]` `[CONDENSATE]` `[MFP]` `[HEATERS]` | MFW pumps, heaters, condensate, isolation, chemistry control | MEDIUM |

### Control Systems

| Document | Tags | Description | Priority |
|----------|------|-------------|----------|
| `NRC_HRTD_Section_8.1_Rod_Control_System.md` | `[RODS]` `[CONTROL]` `[CRDM]` | CRDM design, bank sequencing, auto/manual control | HIGH |
| `NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md` | `[PZR]` `[PRESSURE]` `[CONTROL]` `[PORV]` | Heater banks, spray valves, PORVs, cold overpressure protection | **CRITICAL** |
| `NRC_HRTD_Section_10.3_Pressurizer_Level_Control.md` | `[PZR]` `[LEVEL]` `[CONTROL]` `[CVCS]` | Level program, charging flow control, low level interlocks | **CRITICAL** |
| `NRC_HRTD_Section_11.2_Steam_Dump_Control.md` | `[STEAM_DUMP]` `[CONTROL]` `[HZP]` | Steam pressure/T_avg modes, arming, interlocks | **CRITICAL** |
| `NRC_HRTD_Startup_Pressurization_Reference.md` | `[PZR]` `[STARTUP]` `[CVCS]` | Consolidated pressurization from Sections 4.1, 10.2, 10.3, 17.0, 19.0 | HIGH |

### Instrumentation

| Document | Tags | Description | Priority |
|----------|------|-------------|----------|
| `NRC_HRTD_Section_9.1_Excore_Nuclear_Instrumentation.md` | `[NI]` `[DETECTORS]` `[STARTUP]` | SR/IR/PR detectors, trips, permissives, calibration | **CRITICAL** |
| `NRC_HRTD_Section_9.2_Incore_Instrumentation.md` | `[INCORE]` `[CET]` `[FLUX_MAP]` | Core-exit thermocouples, movable fission chambers, flux mapping | HIGH |
| `NRC_HRTD_Section_10.1_Reactor_Coolant_Instrumentation.md` | `[RCS_INST]` `[RVLIS]` `[SMM]` `[RTD]` | RTDs, T_avg/ΔT, RVLIS, Subcooling Margin Monitor, flow measurement | **CRITICAL** |

### Protection Systems

| Document | Tags | Description | Priority |
|----------|------|-------------|----------|
| `NRC_HRTD_Section_12.2_Reactor_Protection_System.md` | `[RPS]` `[TRIPS]` `[PERMISSIVES]` `[INTERLOCKS]` | All trips, OTΔT/OPΔT, P-n permissives, C-n interlocks | **CRITICAL** |
| `NRC_HRTD_Section_12.3_ESFAS.md` | `[ESFAS]` `[SI]` `[CONTAINMENT]` `[SAFETY]` | Safety injection, containment isolation, steam/FW isolation, AFW actuation | **CRITICAL** |

---

## Research Documents

| Document | Tags | Description | Version |
|----------|------|-------------|---------|
| `RHR_SYSTEM_RESEARCH_v3.0.0.md` | `[RHR]` `[PHYSICS]` `[HEATUP]` | RHR system modeling, pump specs, heat exchangers, pump heat contribution | v3.0.0 |
| `SG_THERMAL_MODEL_RESEARCH_v3.0.0.md` | `[SG]` `[PHYSICS]` `[STRATIFICATION]` | SG thermal physics, thermocline behavior, Churchill-Chu correlations, Richardson number analysis | v3.0.0 |
| `SG_Secondary_Pressurization_During_Heatup_Research.md` | `[SG]` `[PRESSURE]` `[HEATUP]` | SG pressurization from nitrogen blanket to steam | N/A |
| `SG_MODEL_RESEARCH_HANDOFF.md` | `[SG]` `[LEGACY]` | Earlier SG research (may be superseded by v3.0.0) | N/A |

---

## Reference Compilations & Specifications

| Document | Tags | Description |
|----------|------|-------------|
| `NRC_HRTD_Startup_Pressurization_Reference.md` | `[PZR]` `[CVCS]` `[STARTUP]` | Consolidated reference from multiple NRC sections on solid plant pressurization, bubble formation, and pressure/level control |
| `PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md` | `[PZR]` `[IMPLEMENTATION]` | Implementation-specific analysis for v4.4.0 |
| `Westinghouse_4Loop_Pressurizer_Specifications_Summary.md` | `[PZR]` `[SPECS]` | **NEW 2026-02-15** — Quick reference: 1800 ft³ volume, 1794 kW heaters (414 kW proportional + 1380 kW backup), 840 gpm spray, level program equations, all control setpoints |
| `RCP_Heat_Authority_Decision_2026-02-16.md` | `[RCP]` `[AUTHORITY]` `[GOVERNANCE]` | **NEW 2026-02-16** - Declares authoritative RCP heat precedence for Baseline A (`CS-0083`) |

---

## Meta-Documentation

| Document | Purpose |
|----------|---------|
| `NRC_REFERENCE_SOURCES.md` | Master tracking of all NRC sources with URLs, retrieval dates, status |
| `Technical_Documentation_Index.md` | This document — unified index with tags and descriptions |
| `Archive/Technical_Documentation_Summary_2026-02-14.md` | Session summary of documents added 2026-02-14 |
| `Archive/Documentation_Analysis_2026-02-14.md` | Gap analysis and organizational recommendations |

---

## Documents by Development Phase

### Phase 0: Cold Shutdown → RHR Exit (Current)
| Document | Relevance |
|----------|-----------|
| `RHR_SYSTEM_RESEARCH_v3.0.0.md` | RHR operation, pump heat, HX bypass |
| `NRC_HRTD_Section_5.1_Residual_Heat_Removal_System.md` | Complete RHR system reference |
| `SG_THERMAL_MODEL_RESEARCH_v3.0.0.md` | SG thermal inertia, stratification physics |
| `NRC_HRTD_Startup_Pressurization_Reference.md` | Solid plant ops, bubble formation |
| `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` | **NEW** — RCS parameters, pressurizer specs, RCP details |
| `Westinghouse_4Loop_Pressurizer_Specifications_Summary.md` | **NEW** — Quick ref for pressurizer implementation |
| `NRC_HRTD_Section_10.1_Reactor_Coolant_Instrumentation.md` | RTDs, RVLIS, SMM for operator interface |

### Phase 1: Approach to Criticality
| Document | Relevance |
|----------|-----------|
| `NRC_HRTD_Section_9.1_Excore_Nuclear_Instrumentation.md` | SR/IR detector operation, 1/M plots, SUR |
| `NRC_HRTD_Section_9.2_Incore_Instrumentation.md` | Core-exit thermocouples, flux mapping |
| `NRC_HRTD_Section_12.2_Reactor_Protection_System.md` | SR trip, P-6 permissive |
| `NRC_HRTD_Section_8.1_Rod_Control_System.md` | Rod withdrawal, bank sequencing |
| `NRC_HRTD_Section_3.1_Reactor_Vessel_and_Internals.md` | Core geometry, fuel assemblies, CRDMs |

### Phase 2: HZP Stabilization
| Document | Relevance |
|----------|-----------|
| `NRC_HRTD_Section_11.2_Steam_Dump_Control.md` | Steam pressure mode, 557°F T_avg control |
| `NRC_HRTD_Section_9.1_Excore_Nuclear_Instrumentation.md` | IR/PR transition, P-10 permissive |
| `NRC_HRTD_Section_7.1_Main_Auxiliary_Steam.md` | Steam system at no-load |
| `NRC_HRTD_Section_10.1_Reactor_Coolant_Instrumentation.md` | T_avg/ΔT indication, SMM |

### Phase 3: Power Operations
| Document | Relevance |
|----------|-----------|
| `NRC_HRTD_Section_12.2_Reactor_Protection_System.md` | OTΔT/OPΔT, all at-power trips |
| `NRC_HRTD_Section_8.1_Rod_Control_System.md` | Automatic rod control |
| `NRC_HRTD_Section_11.2_Steam_Dump_Control.md` | T_avg mode, load rejection |
| `NRC_HRTD_Section_9.2_Incore_Instrumentation.md` | Flux mapping, incore-excore calibration |

### Future: Cooldown / Shutdown
| Document | Relevance |
|----------|-----------|
| `NRC_HRTD_Section_5.7_Auxiliary_Feedwater_System.md` | AFW for decay heat removal |
| `NRC_HRTD_Section_5.1_Residual_Heat_Removal_System.md` | RHR entry, cooldown to cold shutdown |
| `RHR_SYSTEM_RESEARCH_v3.0.0.md` | RHR cooldown modeling |

---

## Documents by Tag

### `[RCS]` — Reactor Coolant System
- `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` **NEW**

### `[RCS_INST]` — RCS Process Instrumentation
- `NRC_HRTD_Section_10.1_Reactor_Coolant_Instrumentation.md`

### `[RVLIS]` — Reactor Vessel Level Indication
- `NRC_HRTD_Section_10.1_Reactor_Coolant_Instrumentation.md`

### `[SMM]` — Subcooling Margin Monitor
- `NRC_HRTD_Section_10.1_Reactor_Coolant_Instrumentation.md`

### `[RTD]` — Resistance Temperature Detectors
- `NRC_HRTD_Section_10.1_Reactor_Coolant_Instrumentation.md`

### `[VESSEL]` — Reactor Vessel
- `NRC_HRTD_Section_3.1_Reactor_Vessel_and_Internals.md`

### `[CORE]` — Reactor Core
- `NRC_HRTD_Section_3.1_Reactor_Vessel_and_Internals.md`

### `[FUEL]` — Fuel Assemblies
- `NRC_HRTD_Section_3.1_Reactor_Vessel_and_Internals.md`

### `[CRDM]` — Control Rod Drive Mechanisms
- `NRC_HRTD_Section_3.1_Reactor_Vessel_and_Internals.md`
- `NRC_HRTD_Section_8.1_Rod_Control_System.md`

### `[PZR]` — Pressurizer
- `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` **NEW** — Detailed specs
- `NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md`
- `NRC_HRTD_Section_10.3_Pressurizer_Level_Control.md`
- `NRC_HRTD_Startup_Pressurization_Reference.md`
- `PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md`
- `Westinghouse_4Loop_Pressurizer_Specifications_Summary.md` **NEW** — Quick reference

### `[SPECS]` — Equipment Specifications
- `Westinghouse_4Loop_Pressurizer_Specifications_Summary.md` **NEW**

### `[RCP]` — Reactor Coolant Pumps
- `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` **NEW** — Pump seals, flywheel, bearing cooling

### `[SG]` — Steam Generators
- `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` **NEW** — Model 51 SG design
- `SG_THERMAL_MODEL_RESEARCH_v3.0.0.md`
- `SG_Secondary_Pressurization_During_Heatup_Research.md`
- `SG_MODEL_RESEARCH_HANDOFF.md`

### `[RHR]` — Residual Heat Removal
- `NRC_HRTD_Section_5.1_Residual_Heat_Removal_System.md`
- `RHR_SYSTEM_RESEARCH_v3.0.0.md`

### `[CVCS]` — Chemical and Volume Control
- `NRC_HRTD_Startup_Pressurization_Reference.md`

### `[STEAM]` — Steam Systems
- `NRC_HRTD_Section_7.1_Main_Auxiliary_Steam.md`
- `NRC_HRTD_Section_11.2_Steam_Dump_Control.md`

### `[AFW]` — Auxiliary Feedwater
- `NRC_HRTD_Section_5.7_Auxiliary_Feedwater_System.md`
- `NRC_HRTD_Section_7.1_Main_Auxiliary_Steam.md`

### `[RODS]` — Rod Control
- `NRC_HRTD_Section_8.1_Rod_Control_System.md`

### `[NI]` — Nuclear Instrumentation (Excore)
- `NRC_HRTD_Section_9.1_Excore_Nuclear_Instrumentation.md`

### `[INCORE]` — Incore Instrumentation
- `NRC_HRTD_Section_9.2_Incore_Instrumentation.md`

### `[CET]` — Core Exit Thermocouples
- `NRC_HRTD_Section_9.2_Incore_Instrumentation.md`
- `NRC_HRTD_Section_10.1_Reactor_Coolant_Instrumentation.md`

### `[FLUX_MAP]` — Flux Mapping
- `NRC_HRTD_Section_9.2_Incore_Instrumentation.md`

### `[RPS]` — Reactor Protection System
- `NRC_HRTD_Section_12.2_Reactor_Protection_System.md`

### `[TRIPS]` — Reactor Trips
- `NRC_HRTD_Section_12.2_Reactor_Protection_System.md`

### `[PERMISSIVES]` — Protection Permissives
- `NRC_HRTD_Section_9.1_Excore_Nuclear_Instrumentation.md`
- `NRC_HRTD_Section_12.2_Reactor_Protection_System.md`

### `[INTERLOCKS]` — Control Interlocks
- `NRC_HRTD_Section_11.2_Steam_Dump_Control.md`
- `NRC_HRTD_Section_12.2_Reactor_Protection_System.md`

### `[PHYSICS]` — Physics/Thermal Modeling
- `RHR_SYSTEM_RESEARCH_v3.0.0.md`
- `SG_THERMAL_MODEL_RESEARCH_v3.0.0.md`

### `[HEATUP]` — Heatup Operations
- `RHR_SYSTEM_RESEARCH_v3.0.0.md`
- `SG_THERMAL_MODEL_RESEARCH_v3.0.0.md`
- `SG_Secondary_Pressurization_During_Heatup_Research.md`
- `NRC_HRTD_Startup_Pressurization_Reference.md`

### `[HZP]` — Hot Zero Power
- `NRC_HRTD_Section_11.2_Steam_Dump_Control.md`

### `[STARTUP]` — Startup Operations
- `NRC_HRTD_Section_9.1_Excore_Nuclear_Instrumentation.md`
- `NRC_HRTD_Startup_Pressurization_Reference.md`

### `[ECCS]` — Emergency Core Cooling
- `NRC_HRTD_Section_5.1_Residual_Heat_Removal_System.md`

### `[COOLDOWN]` — Plant Cooldown
- `NRC_HRTD_Section_5.1_Residual_Heat_Removal_System.md`

### `[FEEDWATER]` — Main Feedwater System
- `NRC_HRTD_Section_7.2_Condensate_and_Feedwater_System.md`

### `[CONDENSATE]` — Condensate System
- `NRC_HRTD_Section_7.2_Condensate_and_Feedwater_System.md`

### `[MFP]` — Main Feedwater Pumps
- `NRC_HRTD_Section_7.2_Condensate_and_Feedwater_System.md`

### `[HEATERS]` — Feedwater Heaters
- `NRC_HRTD_Section_7.2_Condensate_and_Feedwater_System.md`

### `[ESFAS]` — Engineered Safety Features Actuation
- `NRC_HRTD_Section_12.3_ESFAS.md`

### `[SI]` — Safety Injection
- `NRC_HRTD_Section_12.3_ESFAS.md`

### `[CONTAINMENT]` — Containment Systems
- `NRC_HRTD_Section_12.3_ESFAS.md`

### `[PRESSURE]` — Pressure Control
- `NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md`
- `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` **NEW**

### `[LEVEL]` — Level Control
- `NRC_HRTD_Section_10.3_Pressurizer_Level_Control.md`

### `[CONTROL]` — Control Systems
- `NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md`
- `NRC_HRTD_Section_10.3_Pressurizer_Level_Control.md`

### `[PORV]` — Power-Operated Relief Valves
- `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` **NEW** — Detailed PORV specs
- `NRC_HRTD_Section_10.2_Pressurizer_Pressure_Control.md`

---

## Critical Setpoints Quick Reference

### Pressurizer Design Parameters (NEW)
| Parameter | Value | Source |
|-----------|-------|--------|
| **Total Volume** | 1800 ft³ | Section 3.2 |
| **Total Heater Capacity** | 1794 kW | Section 3.2 |
| **Proportional Heaters (Bank C)** | 414 kW (18 heaters) | Section 3.2 |
| **Backup Heaters (A, B, D)** | 1380 kW (60 heaters) | Section 3.2 |
| **Maximum Spray Flow** | 840 gpm | Section 3.2 |
| **Continuous Bypass Spray** | 1 gpm per valve | Section 3.2 |
| **Design Pressure** | 2500 psig | Section 3.2 |
| **Code Safety Valves** | 2485 psig (3 valves) | Section 3.2 |

### Pressure Setpoints
| Parameter | Value | Source |
|-----------|-------|--------|
| Normal operating pressure | 2235 psig | Section 10.2 |
| PZR heater band | 2220-2250 psig | Section 10.2 |
| Spray start | 2260 psig | Section 10.2 |
| Spray full open | 2310 psig | Section 10.2 |
| PORV setpoint | 2335 psig | Section 10.2 |
| High pressure trip | 2385 psig | Section 12.2 |
| Low pressure trip | 1865 psig | Section 12.2 |
| Low pressure SI | 1807 psig | Section 12.2 |
| RHR entry | ≤425 psig | Section 5.1 |
| RHR auto-close | 585 psig | Section 5.1 |
| Steam dump setpoint (no-load) | 1092 psig | Section 11.2 |
| SG PORV | 1125 psig | Section 7.1 |
| SG safety valves | 1170-1230 psig | Section 7.1 |

### Temperature Setpoints
| Parameter | Value | Source |
|-----------|-------|--------|
| No-load T_avg | 557°F | Section 11.2 |
| Full power T_avg | 584.7°F | Section 12.2 |
| P-12 low-low T_avg | 553°F | Section 11.2/12.2 |
| RHR entry | ≤350°F | Section 5.1 |
| CRDM fans required | >350°F | Section 3.1 |

### Level Setpoints
| Parameter | Value | Source |
|-----------|-------|--------|
| PZR level at no-load (557°F) | 25% | Section 10.3 |
| PZR level at full power (584.7°F) | 61.5% | Section 10.3 |
| PZR low level isolation | 17% | Section 10.3 |
| PZR high level trip | 92% | Section 12.2 |
| SG low-low level trip | 11.5% | Section 12.2 |

### Nuclear Instrumentation
| Parameter | Value | Source |
|-----------|-------|-------|
| SR trip | 10⁵ cps | Section 9.1 |
| P-6 (SR block) | 10⁻¹⁰ A IR | Section 9.1 |
| IR trip | 25% equivalent | Section 9.1 |
| C-1 rod stop | 20% equivalent | Section 9.1 |
| P-10 (nuclear at-power) | 10% PR | Section 9.1 |
| PR trip low | 25% | Section 12.2 |
| PR trip high | 109% | Section 12.2 |
| C-2 rod stop | 103% | Section 12.2 |
| Rate trips | ±5%/2 sec | Section 12.2 |

### Instrumentation Ranges
| Instrument | Range | Source |
|------------|-------|--------|
| Narrow-range Tc RTD | 510-630°F | Section 10.1 |
| Narrow-range Th RTD | 530-650°F | Section 10.1 |
| Wide-range RTD | 0-700°F | Section 10.1 |
| Calculated T_avg | 530-650°F | Section 10.1 |
| Calculated ΔT | 0-150% | Section 10.1 |
| PZR pressure (narrow) | 1700-2500 psig | Section 10.1 |
| RCS pressure (wide) | 0-3000 psig | Section 10.1 |
| SMM low margin alarm | 15°F | Section 10.1 |
| SMM no margin alarm | 0°F | Section 10.1 |

### Core Geometry
| Parameter | Value | Source |
|-----------|-------|--------|
| Fuel assemblies | 193 | Section 3.1 |
| Fuel rods per assembly | 264 | Section 3.1 |
| Rod array | 17×17 | Section 3.1 |
| Guide thimbles per assembly | 24 | Section 3.1 |
| RCCAs | 53 | Section 3.1 |
| CRDM step size | 5/8 inch | Section 3.1 |
| Rod travel | 144 inches | Section 3.1 |
| Core bypass flow | 6.5% | Section 3.1 |

---

## Document Maintenance

### Adding New Documents
1. Create document in `Technical_Documentation/` folder
2. Add entry to appropriate section in this index
3. Add tags to "Documents by Tag" section
4. Update `NRC_REFERENCE_SOURCES.md` if NRC source
5. Update phase relevance if applicable

### Archiving Superseded Documents
1. Move to `Technical_Documentation/Archive/`
2. Add "SUPERSEDED BY" note to top of archived document
3. Remove from active sections of this index
4. Keep reference in Archive section if needed

### Version Control
- Research documents use semantic versioning (vX.Y.Z)
- Implementation-specific documents include implementation version
- NRC documents reference NRC revision numbers

---

## Recent Updates

### 2026-02-15
**Added:**
- `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` — Comprehensive RCS and pressurizer specifications from NRC HRTD
- `Westinghouse_4Loop_Pressurizer_Specifications_Summary.md` — Quick reference for pressurizer implementation

**Key Specifications Retrieved:**
- Pressurizer volume: 1800 ft³
- Heater capacity: 1794 kW total (414 kW proportional + 1380 kW backup in banks A, B, D)
- Spray capacity: 840 gpm maximum
- PORV setpoints and interlock logic
- Code safety valve specifications
- Complete pressure control setpoint tables
- Level control program equations
- RCP seal design and cooling requirements
- Steam generator Model 51 specifications

---
