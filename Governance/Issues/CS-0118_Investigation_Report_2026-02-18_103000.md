# CS-0118: Validation Dashboard Missing Condenser/Feedwater System Parameters

**Date:** 2026-02-18  
**Status:** OPEN  
**Priority:** Medium  
**Category:** Dashboard / Telemetry Gap  

---

## Summary

The Condenser and Feedwater systems (IP-0046, CS-0115/CS-0116) have been fully modeled in the physics layer with comprehensive state structs (`CondenserState`, `FeedwaterState`) and integrated into `HeatupSimEngine`. However, `ValidationDashboard.Snapshot.cs` does not capture any of these new parameters, meaning the dashboard cannot display condenser vacuum, C-9 interlock status, hotwell/CST levels, feedwater pump states, or startup permissive status.

---

## Analysis

### New Parameters Available in HeatupSimEngine

**Condenser System (from IP-0046 Stage F):**
| Field | Type | Description |
|-------|------|-------------|
| `condenserState` | `CondenserState` | Full condenser state struct |
| `condenserVacuum_inHg` | `float` | Current vacuum reading (0-29.92 in. Hg) |
| `condenserBackpressure_psia` | `float` | Condenser backpressure |
| `condenserC9Available` | `bool` | C-9 "Condenser Available" interlock |
| `condenserPulldownPhase` | `string` | Vacuum pulldown phase label |

**Feedwater System (from IP-0046 Stage G):**
| Field | Type | Description |
|-------|------|-------------|
| `feedwaterState` | `FeedwaterState` | Full feedwater state struct |
| `hotwellLevel_pct` | `float` | Hotwell level percentage |
| `cstLevel_pct` | `float` | CST level percentage |
| `feedwaterReturnFlow_lbhr` | `float` | Total return flow to SGs |

**Startup Permissives (from IP-0046 Stage H):**
| Field | Type | Description |
|-------|------|-------------|
| `permissiveState` | `PermissiveState` | Full permissive state struct |
| `steamDumpBridgeState` | `string` | Bridge FSM state |
| `steamDumpPermitted` | `bool` | Final permissive authority |
| `permissiveStatusMessage` | `string` | Human-readable status |

**Orchestration Tracking (CS-0116):**
| Field | Type | Description |
|-------|------|-------------|
| `condenserStartupCommanded` | `bool` | CW + vacuum pulldown initiated |
| `p12BypassCommanded` | `bool` | P-12 bypass engaged |
| `condenserStartupTime_hr` | `float` | Time of condenser startup command |

### Current State of ValidationDashboard.Snapshot.cs

The `DashboardSnapshot.CaptureFrom()` method captures extensive RCS, PZR, CVCS, SG, RHR, and BRS parameters but **zero** Condenser/Feedwater fields. The dashboard has no visibility into:

1. Condenser vacuum establishment progress
2. C-9 interlock satisfaction (critical for steam dump operation)
3. P-12 bypass status
4. Hotwell and CST inventory
5. Feedwater/AFW pump operating status
6. Steam dump permissive chain

---

## Impact

- **Operator Training Gap:** Trainees cannot observe condenser vacuum pulldown sequence or verify C-9/P-12 permissives
- **Steam Dump Validation:** Cannot confirm why steam dumps are blocked or enabled
- **Secondary Inventory:** No visibility into CST depletion during extended AFW operation
- **Startup Procedure Fidelity:** Missing key procedural checkpoints (condenser startup, P-12 bypass)

---

## Tab Architecture Analysis

### Current Dashboard Tab Structure

| Index | Tab Name | Content | Density |
|-------|----------|---------|---------|
| 0 | OVERVIEW | 60+ parameters, 5-column layout | High |
| 1 | RCS | RCS Primary details | Medium |
| 2 | PZR | Pressurizer details | Medium |
| 3 | CVCS | CVCS/VCT details | Medium |
| 4 | SG/RHR | SG secondary + RHR + HZP (3 columns: 30%/30%/40%) | High |
| 5 | SYSTEMS | BRS, Mass Balance, Diagnostics | Medium |
| 6 | GRAPHS | Strip chart trends | Low |
| 7 | LOG | Event log and annunciators | Medium |

### SG/RHR Tab Current Layout (Tab 4)

```
┌─────────────────┬─────────────────┬─────────────────────────────────┐
│ STEAM GENERATOR │      RHR        │          TRENDS                 │
│      (30%)      │     (30%)       │           (40%)                 │
│                 │                 │                                 │
│   [LARGE ARC]   │   RHR STATUS    │  ┌─────────────────────────┐   │
│   SG PRESSURE   │   ACTIVE ●      │  │     SG PRESSURE         │   │
│                 │   MODE ───      │  └─────────────────────────┘   │
│   T_SAT  ───    │                 │  ┌─────────────────────────┐   │
│   T_BULK ───    │   [ARC]         │  │     RCS PRESSURE        │   │
│                 │   RHR HEAT      │  └─────────────────────────┘   │
│   SG HEAT ───   │                 │  ┌─────────────────────────┐   │
│   BOILING ●     │   COOLING ───   │  │     T_AVG               │   │
│   DUMP ●        │   HEATING ───   │  └─────────────────────────┘   │
│                 │                 │  ┌─────────────────────────┐   │
│   T_HOT ───     │   HZP PROGRESS  │  │     NET HEAT            │   │
│   T_COLD ───    │   ═══════════   │  └─────────────────────────┘   │
│   CORE ΔT ───   │   HZP READY ●   │                               │
└─────────────────┴─────────────────┴─────────────────────────────────┘
```

### New Condenser/Feedwater Parameters Requiring Display

**Condenser (5-7 items):**
- Vacuum arc gauge (major visual element)
- Backpressure readout
- CW Pumps running indicator
- Pulldown phase label
- C-9 interlock LED (critical)
- Air ejectors status

**Feedwater (8-10 items):**
- Hotwell level (bar gauge)
- CST level (arc gauge - critical for Tech Spec)
- Condensate pumps running
- MFP running
- AFW motor pumps running
- AFW turbine pump LED
- Return flow readout
- CST below Tech Spec alarm

**Permissives (4 items):**
- Steam dump permitted LED
- P-12 bypass LED
- Bridge state label
- Permissive status message

**Total: ~20 new display elements**

### Options Evaluated

#### Option A: Squeeze into Existing SG/RHR Tab
- **Approach:** Add Condenser/FW as 4th column, compress existing columns
- **Result:** 25%/20%/20%/35% column split
- **Problems:**
  - Columns become too narrow for arc gauges
  - Information density exceeds usability threshold
  - Sparkline column would shrink significantly
- **Verdict:** ❌ Not recommended

#### Option B: Add New "COND/FW" Tab (Tab 8)
- **Approach:** Create dedicated Tab 8 for Condenser and Feedwater
- **Layout:** 3-column (CONDENSER 35% / FEEDWATER 35% / TRENDS 30%)
- **Pros:**
  - Maximum display real estate
  - Clear separation of concerns
  - No disruption to existing tabs
- **Cons:**
  - 9 tabs may crowd the tab bar
  - Separates operationally-related systems (SG ↔ Condenser ↔ Feedwater)
- **Verdict:** ⚠️ Viable but not optimal

#### Option C: Rename SG/RHR to "SECONDARY" and Restructure (Recommended)
- **Approach:** Consolidate all secondary-side heat removal systems into one comprehensive tab
- **New Tab Name:** "SECONDARY" (or "SEC/COND" or "HEAT SINK")
- **Layout:** 4-column (SG 25% / COND/FW 25% / RHR 20% / TRENDS 30%)
- **Pros:**
  - Groups entire heat removal chain: SG → Steam Dump → Condenser → Feedwater → SG
  - Matches operator mental model during startup
  - No additional tab
  - Operationally logical grouping
- **Cons:**
  - Columns narrower than current (but still usable)
  - Requires restructuring existing tab code
- **Verdict:** ✅ **Recommended**

### Recommended Tab 4 Layout ("SECONDARY")

```
┌────────────┬──────────────┬────────────┬────────────────────────┐
│     SG     │   COND/FW    │    RHR     │        TRENDS          │
│   (25%)    │    (25%)     │   (20%)    │         (30%)          │
│            │              │            │                        │
│ [ARC]      │ CONDENSER    │ RHR STATUS │ ┌────────────────────┐ │
│ SG PRESS   │ [ARC]        │ ACTIVE ●   │ │   SG PRESSURE      │ │
│            │ VACUUM       │ MODE ───   │ └────────────────────┘ │
│ T_SAT ──   │ CW PUMPS ──  │            │ ┌────────────────────┐ │
│ T_BULK ──  │ PHASE ───    │ [ARC]      │ │   COND VACUUM      │ │
│            │ C-9 ●        │ RHR HEAT   │ └────────────────────┘ │
│ SG Q ──    │ P-12 BYP ●   │            │ ┌────────────────────┐ │
│ BOILING ●  │              │ NET Q ──   │ │   CST LEVEL        │ │
│ DUMP ●     │ FEEDWATER    │            │ └────────────────────┘ │
│            │ [BAR]        │ HZP        │ ┌────────────────────┐ │
│ PRIMARY    │ HOTWELL      │ PROGRESS   │ │   T_AVG            │ │
│ T_HOT ──   │ [ARC]        │ ═════════  │ └────────────────────┘ │
│ T_COLD ──  │ CST LEVEL    │ HZP RDY ●  │                        │
│            │ CST SPEC ●   │            │                        │
│            │ AFW ●        │ PLANT MODE │                        │
│            │ RETURN ──    │ ─────────  │                        │
└────────────┴──────────────┴────────────┴────────────────────────┘
```

---

## Recommended Fix (Updated)

### Phase 1: Snapshot Extension
Add the following fields to `DashboardSnapshot`:

```csharp
// Condenser
public float CondenserVacuum_inHg;
public float CondenserBackpressure_psia;
public bool CondenserC9Available;
public string CondenserPulldownPhase;
public int CondenserCWPumpsRunning;

// Feedwater
public float HotwellLevel_pct;
public float CSTLevel_pct;
public float FeedwaterReturnFlow_lbhr;
public int CondensatePumpsRunning;
public int AFWMotorPumpsRunning;
public bool AFWTurbinePumpRunning;
public bool FeedwaterAvailable;
public bool CSTBelowTechSpec;

// Permissives
public bool SteamDumpPermitted;
public string SteamDumpBridgeState;
public bool P12Bypassed;
public bool CondenserStartupCommanded;
```

### Phase 2: Tab Restructure
- **Rename** `SGRHRTab.cs` to `SecondaryTab.cs`
- **Rename** tab label from "SG/RHR" to "SECONDARY"
- **Restructure** to 4-column layout:
  - Column 1 (25%): SG parameters (condensed from current)
  - Column 2 (25%): Condenser + Feedwater (new)
  - Column 3 (20%): RHR + HZP (condensed from current)
  - Column 4 (30%): Trends/Sparklines

### Phase 3: Annunciator Integration
- Add C-9 interlock status to annunciator panel
- Add CST Tech Spec alarm to annunciator panel
- Add P-12 bypass indicator

### Phase 4: Sparkline Integration
- Add CST level trend (index TBD)
- Add Condenser vacuum trend (index TBD)

---

## Files Requiring Modification

1. `Assets/Scripts/Validation/ValidationDashboard.Snapshot.cs` — Add capture fields
2. `Assets/Scripts/Validation/ValidationDashboard.cs` — Update tab label array
3. `Assets/Scripts/Validation/Tabs/SGRHRTab.cs` — Rename and restructure to `SecondaryTab.cs`
4. `Assets/Scripts/Validation/ValidationDashboard.Annunciators.cs` — Add C-9/CST annunciators
5. `Assets/Scripts/Validation/ValidationDashboard.Sparklines.cs` — Add new sparkline channels

---

## References

- **IP-0046:** Condenser/Feedwater Architecture Implementation
- **CS-0115:** Condenser/Feedwater Module Investigation
- **CS-0116:** Condenser Startup Orchestration
- **NRC HRTD Section 11.2:** Steam Dump Control System (C-9/P-12)
- **NRC HRTD Section 7.2:** Condensate and Feedwater System

---

*Investigation by: Claude (2026-02-18)*
