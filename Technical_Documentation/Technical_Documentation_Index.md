# Technical Documentation Index

**Last Updated:** 2026-02-14  
**Purpose:** Unified index of all technical documentation supporting Critical: Master the Atom simulator development.

---

## Quick Reference

| Category | Document Count | Coverage |
|----------|----------------|----------|
| NRC HRTD Sections | 13 retrieved | Core systems, controls, protection |
| Research Documents | 4 | SG thermal physics, RHR system |
| Reference Compilations | 2 | Startup/pressurization, PZR analysis |
| Meta-Documentation | 3 | Indexes, summaries, analysis |

---

## NRC HRTD Section Documents

### Reactor Core and Primary Systems

| Document | Tags | Description | Priority |
|----------|------|-------------|----------|
| `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` | `[RCS]` `[PZR]` `[RCP]` `[SG]` | RCS design parameters, RCPs, pressurizer, SG specs, P-T limits | HIGH |
| `NRC_HRTD_Section_5.1_*` | `[RHR]` `[COOLDOWN]` | RHR system design (referenced in RHR_SYSTEM_RESEARCH) | HIGH |

### Engineered Safety Features

| Document | Tags | Description | Priority |
|----------|------|-------------|----------|
| `NRC_HRTD_Section_5.7_Auxiliary_Feedwater_System.md` | `[AFW]` `[SAFETY]` `[DECAY_HEAT]` | AFW pumps, start signals, water supplies, PRA insights | MEDIUM |

### Secondary Systems

| Document | Tags | Description | Priority |
|----------|------|-------------|----------|
| `NRC_HRTD_Section_7.1_Main_Auxiliary_Steam.md` | `[STEAM]` `[PORV]` `[MSIV]` `[AFW]` | Main steam design, PORVs, safety valves, flow restrictors | HIGH |

### Control Systems

| Document | Tags | Description | Priority |
|----------|------|-------------|----------|
| `NRC_HRTD_Section_8.1_Rod_Control_System.md` | `[RODS]` `[CONTROL]` `[CRDM]` | CRDM design, bank sequencing, auto/manual control | HIGH |
| `NRC_HRTD_Section_11.2_Steam_Dump_Control.md` | `[STEAM_DUMP]` `[CONTROL]` `[HZP]` | Steam pressure/T_avg modes, arming, interlocks | **CRITICAL** |
| `NRC_HRTD_Startup_Pressurization_Reference.md` | `[PZR]` `[STARTUP]` `[CVCS]` | Consolidated pressurization from Sections 4.1, 10.2, 10.3, 17.0, 19.0 | HIGH |

### Instrumentation

| Document | Tags | Description | Priority |
|----------|------|-------------|----------|
| `NRC_HRTD_Section_9.1_Excore_Nuclear_Instrumentation.md` | `[NI]` `[DETECTORS]` `[STARTUP]` | SR/IR/PR detectors, trips, permissives, calibration | **CRITICAL** |

### Protection Systems

| Document | Tags | Description | Priority |
|----------|------|-------------|----------|
| `NRC_HRTD_Section_12.2_Reactor_Protection_System.md` | `[RPS]` `[TRIPS]` `[PERMISSIVES]` `[INTERLOCKS]` | All trips, OTΔT/OPΔT, P-n permissives, C-n interlocks | **CRITICAL** |

### Plant Operations

| Document | Tags | Description | Priority |
|----------|------|-------------|----------|
| `NRC_HRTD_Section_19.0_*` | `[OPERATIONS]` `[HEATUP]` `[STARTUP]` | Plant operations procedures (referenced inline) | HIGH |

---

## Research Documents

| Document | Tags | Description | Version |
|----------|------|-------------|---------|
| `RHR_SYSTEM_RESEARCH_v3.0.0.md` | `[RHR]` `[PHYSICS]` `[HEATUP]` | RHR system modeling, pump specs, heat exchangers, pump heat contribution | v3.0.0 |
| `SG_THERMAL_MODEL_RESEARCH_v3.0.0.md` | `[SG]` `[PHYSICS]` `[STRATIFICATION]` | SG thermal physics, thermocline behavior, Churchill-Chu correlations, Richardson number analysis | v3.0.0 |
| `SG_Secondary_Pressurization_During_Heatup_Research.md` | `[SG]` `[PRESSURE]` `[HEATUP]` | SG pressurization from nitrogen blanket to steam | N/A |
| `SG_MODEL_RESEARCH_HANDOFF.md` | `[SG]` `[LEGACY]` | Earlier SG research (may be superseded by v3.0.0) | N/A |

---

## Reference Compilations

| Document | Tags | Description |
|----------|------|-------------|
| `NRC_HRTD_Startup_Pressurization_Reference.md` | `[PZR]` `[CVCS]` `[STARTUP]` | Consolidated reference from multiple NRC sections on solid plant pressurization, bubble formation, and pressure/level control |
| `PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md` | `[PZR]` `[IMPLEMENTATION]` | Implementation-specific analysis for v4.4.0 |

---

## Meta-Documentation

| Document | Purpose |
|----------|---------|
| `NRC_REFERENCE_SOURCES.md` | Master tracking of all NRC sources with URLs, retrieval dates, status |
| `Technical_Documentation_Index.md` | This document — unified index with tags and descriptions |
| `Technical_Documentation_Summary_2026-02-14.md` | Session summary of documents added 2026-02-14 |
| `Documentation_Analysis_2026-02-14.md` | Gap analysis and organizational recommendations |

---

## Documents by Development Phase

### Phase 0: Cold Shutdown → RHR Exit (Current)
| Document | Relevance |
|----------|-----------|
| `RHR_SYSTEM_RESEARCH_v3.0.0.md` | RHR operation, pump heat, HX bypass |
| `SG_THERMAL_MODEL_RESEARCH_v3.0.0.md` | SG thermal inertia, stratification physics |
| `NRC_HRTD_Startup_Pressurization_Reference.md` | Solid plant ops, bubble formation |
| `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` | RCS parameters, RCP specs |

### Phase 1: Approach to Criticality
| Document | Relevance |
|----------|-----------|
| `NRC_HRTD_Section_9.1_Excore_Nuclear_Instrumentation.md` | SR/IR detector operation, 1/M plots, SUR |
| `NRC_HRTD_Section_12.2_Reactor_Protection_System.md` | SR trip, P-6 permissive |
| `NRC_HRTD_Section_8.1_Rod_Control_System.md` | Rod withdrawal, bank sequencing |

### Phase 2: HZP Stabilization
| Document | Relevance |
|----------|-----------|
| `NRC_HRTD_Section_11.2_Steam_Dump_Control.md` | Steam pressure mode, 557°F T_avg control |
| `NRC_HRTD_Section_9.1_Excore_Nuclear_Instrumentation.md` | IR/PR transition, P-10 permissive |
| `NRC_HRTD_Section_7.1_Main_Auxiliary_Steam.md` | Steam system at no-load |

### Phase 3: Power Operations
| Document | Relevance |
|----------|-----------|
| `NRC_HRTD_Section_12.2_Reactor_Protection_System.md` | OTΔT/OPΔT, all at-power trips |
| `NRC_HRTD_Section_8.1_Rod_Control_System.md` | Automatic rod control |
| `NRC_HRTD_Section_11.2_Steam_Dump_Control.md` | T_avg mode, load rejection |

### Future: Cooldown / Shutdown
| Document | Relevance |
|----------|-----------|
| `NRC_HRTD_Section_5.7_Auxiliary_Feedwater_System.md` | AFW for decay heat removal |
| `RHR_SYSTEM_RESEARCH_v3.0.0.md` | RHR entry, cooldown to cold shutdown |

---

## Documents by Tag

### `[RCS]` — Reactor Coolant System
- `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md`

### `[PZR]` — Pressurizer
- `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md`
- `NRC_HRTD_Startup_Pressurization_Reference.md`
- `PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md`

### `[SG]` — Steam Generators
- `NRC_HRTD_Section_3.2_Reactor_Coolant_System.md`
- `SG_THERMAL_MODEL_RESEARCH_v3.0.0.md`
- `SG_Secondary_Pressurization_During_Heatup_Research.md`
- `SG_MODEL_RESEARCH_HANDOFF.md`

### `[RHR]` — Residual Heat Removal
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

### `[NI]` — Nuclear Instrumentation
- `NRC_HRTD_Section_9.1_Excore_Nuclear_Instrumentation.md`

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

---

## Critical Setpoints Quick Reference

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
|-----------|-------|--------|
| SR trip | 10⁵ cps | Section 9.1 |
| P-6 (SR block) | 10⁻¹⁰ A IR | Section 9.1 |
| IR trip | 25% equivalent | Section 9.1 |
| C-1 rod stop | 20% equivalent | Section 9.1 |
| P-10 (nuclear at-power) | 10% PR | Section 9.1 |
| PR trip low | 25% | Section 12.2 |
| PR trip high | 109% | Section 12.2 |
| C-2 rod stop | 103% | Section 12.2 |
| Rate trips | ±5%/2 sec | Section 12.2 |

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
