# PWR Startup State Sequence — Cold Shutdown to Hot Standby

**Purpose:** Integrated state machine defining system configurations during plant heatup  
**Created:** 2026-02-17  
**Basis:** NRC HRTD Sections 19.0, 11.2, 7.1, 2.3; synthesized into canonical sequence
**Authoritative Startup Boundary Reference:** `Startup_Boundary_and_SteamDump_Authoritative_Spec.md`

**Supersession Note:** If any statement in this file conflicts with the authoritative startup-boundary specification, the authoritative specification governs.

---

## 1. STATE DEFINITIONS

### 1.1 Primary States (Mode-Based)

| State | NRC Mode | T_RCS Range | P_RCS Range | Keff | Description |
|-------|----------|-------------|-------------|------|-------------|
| **S0** | Mode 5 | <200°F | 320-450 psig | <0.99 | Cold Shutdown (Solid) |
| **S1** | Mode 5 | <200°F | 320-450 psig | <0.99 | Cold Shutdown (Bubble) |
| **S2** | Mode 5 | 160-200°F | 320-2235 psig | <0.99 | RCPs Running, Pre-Mode 4 |
| **S3** | Mode 4 | 200-350°F | Variable | <0.99 | Hot Shutdown |
| **S4** | Mode 3 | ≥350°F | ~2235 psig | <0.99 | Hot Standby |
| **S5** | Mode 3 | 557°F | 2235 psig | <0.99 | No-Load Tavg (Steady) |

### 1.2 Secondary States (SG-Based)

| State | T_RCS | SG Level | SG Pressure | Steam Path | Description |
|-------|-------|----------|-------------|------------|-------------|
| **SG0** | <160°F | 100% WR | N₂ (~2-6 psig) | None | Wet Layup |
| **SG1** | 160-200°F | 100%→33% NR | N₂ (~2-6 psig) | None→MSIV bypass | Draining |
| **SG2** | 200-220°F | ~33% NR | N₂→Atm | MSIVs closed, bypass open | Pre-Boiling |
| **SG3** | 220-350°F | ~33% NR | 0-130 psig | Bypass warming, then MSIV transition | Early Boiling |
| **SG4** | 350-557°F | ~33% NR | 130-1092 psig | MSIVs open, bypass closed | Full Boiling |
| **SG5** | 557°F | ~33% NR | 1092 psig | Steam dumps (if C-9 and P-12 permissive) | Controlled |

---

## 2. COMPLETE STATE TRANSITION DIAGRAM

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                              PWR STARTUP STATE MACHINE                                   │
└─────────────────────────────────────────────────────────────────────────────────────────┘

  ┌─────────┐
  │   S0    │  COLD SHUTDOWN (SOLID)
  │ Mode 5  │  T_RCS: 100-160°F, P_RCS: 320-400 psig
  │ SG0     │  PZR: Solid, RCPs: OFF, RHR: Running
  │         │  SG: Wet Layup (100% WR), N₂ blanket
  └────┬────┘
       │ Energize PZR heaters
       │ Draw steam bubble
       │ PZR level → 25%
       ▼
  ┌─────────┐
  │   S1    │  COLD SHUTDOWN (BUBBLE)
  │ Mode 5  │  T_RCS: 150-160°F, P_RCS: 320-400 psig
  │ SG0     │  PZR: Bubble @ 428-448°F, RCPs: OFF
  │         │  SG: Still Wet Layup
  └────┬────┘
       │ Start RCPs (one at a time)
       │ Verify seal injection
       │ RHR flow adjusted
       ▼
  ┌─────────┐
  │   S2    │  RCPS RUNNING, PRE-MODE 4
  │ Mode 5  │  T_RCS: 160°F (hold), P_RCS: 320+ psig
  │ SG0→SG1 │  All 4 RCPs running
  │         │  Begin SG draining (100% WR → 33% NR)
  │         │  Open MSIV bypass valves for line warming
  └────┬────┘
       │ Stop RHR pumps
       │ Allow heatup (~50°F/hr)
       │ Continue SG draining
       │ T_RCS crosses 200°F
       ▼
  ┌─────────┐
  │   S3a   │  HOT SHUTDOWN (EARLY)
  │ Mode 4  │  T_RCS: 200-220°F
  │ SG2     │  SG draining complete (~33% NR)
  │         │  MSIVs closed, bypass open, N₂ isolated
  │         │  H₂ blanket established in VCT
  └────┬────┘
       │ Steam formation begins (~220°F)
       │ SG secondary pressurizes
       │ Steam line warming progresses
       ▼
  ┌─────────┐
  │   S3b   │  HOT SHUTDOWN (MID)
  │ Mode 4  │  T_RCS: 220-350°F
  │ SG3     │  SG pressure: 0-130 psig
  │         │  Steam lines warming
  │         │  Condensate draining via traps
  └────┬────┘
       │ T_RCS crosses 350°F
       │ RHR isolated from RCS
       │ ECCS aligned
       │ Containment spray aligned
       ▼
  ┌─────────┐
  │   S4    │  HOT STANDBY (HEATUP)
  │ Mode 3  │  T_RCS: 350-557°F
  │ SG4     │  P_RCS: Increasing toward 2235 psig
  │         │  SG pressure: 130-1092 psig
  │         │  Letdown via CVCS normal orifices
  │         │  SIT valves open at 1925 psig
  └────┬────┘
       │ SG pressure reaches 1092 psig
       │ Steam dumps actuate (pressure mode)
       │ T_RCS stabilizes at 557°F
       │ P_RCS reaches 2235 psig
       ▼
  ┌─────────┐
  │   S5    │  HOT STANDBY (STEADY)
  │ Mode 3  │  T_RCS: 557°F (no-load Tavg)
  │ SG5     │  P_RCS: 2235 psig
  │         │  SG pressure: 1092 psig (dump controlled)
  │         │  Ready for reactor startup
  └─────────┘
```

---

## 3. DETAILED STATE CONFIGURATIONS

### 3.1 State S0 — Cold Shutdown (Solid)

| System | Configuration |
|--------|---------------|
| **RCS** | Solid (no bubble), 320-400 psig, 100-160°F |
| **Pressurizer** | Solid (100% water), PORVs in UNBLOCK for cold overpressure protection |
| **RCPs** | All OFF |
| **RHR** | Running for decay heat removal, HCV-128 open |
| **CVCS** | Letdown via RHR, charging to maintain pressure |
| **SG Primary** | Stagnant |
| **SG Secondary** | Wet layup (100% WR), N₂ blanket (2-6 psig) |
| **MSIVs** | CLOSED |
| **Steam Dumps** | OFF |
| **Main FW** | OFF |
| **Turbine** | On turning gear |

### 3.2 State S1 — Cold Shutdown (Bubble)

| System | Configuration |
|--------|---------------|
| **RCS** | 320-400 psig, 150-160°F |
| **Pressurizer** | Steam bubble formed, level at 25%, T_PZR at 428-448°F |
| **RCPs** | All OFF (preparing to start) |
| **RHR** | Running, flow adjusted to maintain temp |
| **CVCS** | Normal letdown/charging alignment |
| **SG Primary** | Stagnant |
| **SG Secondary** | Still wet layup |
| **MSIVs** | CLOSED |

**Transition to S2:** Start RCPs one at a time, verify no air in U-tubes

### 3.3 State S2 — RCPs Running, Pre-Mode 4

| System | Configuration |
|--------|---------------|
| **RCS** | 320+ psig, held at 160°F |
| **Pressurizer** | Bubble established, heaters/sprays controlling |
| **RCPs** | All 4 RUNNING |
| **RHR** | Pumps STOPPED (RCPs provide flow) |
| **CVCS** | Normal alignment, letdown increasing for heatup |
| **SG Primary** | Forced flow through U-tubes |
| **SG Secondary** | Draining from 100% WR to 33% NR |
| **MSIVs / Bypass** | MSIVs CLOSED, MSIV bypass OPEN (line warming) |
| **Blowdown** | In service for draining |

**Key Actions:**
- SG draining via blowdown system
- Open MSIV bypass valves with MSIVs closed to begin steam line warming
- Adjust CVCS for thermal expansion

### 3.4 State S3a — Hot Shutdown (Early, 200-220°F)

| System | Configuration |
|--------|---------------|
| **RCS** | Pressurizing, heatup at ~50°F/hr |
| **Pressurizer** | Level increasing with expansion |
| **SG Secondary** | Level at ~33% NR, draining complete |
| **N₂ to SGs** | ISOLATED |
| **VCT** | H₂ blanket established |
| **MSIVs / Bypass** | MSIVs CLOSED, MSIV bypass OPEN |
| **Steam Lines** | Cold, beginning to warm |

**Mode 4 Entry Checklist triggered at 200°F**

### 3.5 State S3b — Hot Shutdown (Mid, 220-350°F)

| System | Configuration |
|--------|---------------|
| **RCS** | 220-350°F, pressure increasing |
| **SG Secondary** | BOILING begins at ~220°F |
| **SG Pressure** | Rising: 0 → ~130 psig |
| **Steam Lines** | Warming (condensate draining via traps) |
| **MSIVs / Bypass** | Bypass OPEN, then MSIVs OPENING as dP equalizes |
| **Steam Dumps** | OFF (not yet at setpoint) |
| **RHR** | Still aligned to RCS (emergency backup) |

**Key Physics:**
- Steam formation in SG secondary
- Pressure tracks saturation curve
- Steam line condensation acts as natural damper

### 3.6 State S4 — Hot Standby (Heatup, 350-557°F)

| System | Configuration |
|--------|---------------|
| **RCS** | 350-557°F, P_RCS → 2235 psig |
| **RHR** | ISOLATED from RCS (ECCS lineup) |
| **CVCS** | All letdown via normal orifices (max 120 gpm) |
| **SG Pressure** | Rising: 130 → 1092 psig |
| **Steam Lines** | Fully warmed |
| **MSIVs / Bypass** | MSIVs OPEN, MSIV bypass CLOSED |
| **SITs** | Outlet valves OPEN at 1925 psig |
| **Containment Spray** | Aligned for operation |

**Mode 3 Entry Checklist triggered at 350°F**

### 3.7 State S5 — Hot Standby (Steady, 557°F)

| System | Configuration |
|--------|---------------|
| **RCS** | T_avg = 557°F, P = 2235 psig |
| **Pressurizer** | Auto control (heaters/sprays) |
| **SG Secondary** | T_sat = 557°F, P = 1092 psig |
| **Steam Dumps** | MODULATING only if C-9 true and P-12 not blocking (or bypassed) |
| **MSIVs / Bypass** | MSIVs OPEN, MSIV bypass CLOSED |
| **Main FW Pump** | On turning gear (ready) |
| **Turbine** | Warmed, ready for startup |

**Ready for reactor criticality**

---

## 4. VALVE STATE MATRIX

### 4.1 Main Steam System Valves

| Valve | S0 | S1 | S2 | S3a | S3b | S4 | S5 |
|-------|----|----|----|----|----|----|-----|
| MSIVs | CLOSED | CLOSED | CLOSED | CLOSED | OPENING | OPEN | OPEN |
| MSIV Bypass | CLOSED | CLOSED | OPEN | OPEN | OPEN/CLOSING | CLOSED | CLOSED |
| Steam Dumps | OFF | OFF | OFF | OFF | OFF | ARMED/CLOSED* | MODULATING* |
| Atm Relief | AUTO | AUTO | AUTO | AUTO | AUTO | AUTO | AUTO |
| Safety Valves | AUTO | AUTO | AUTO | AUTO | AUTO | AUTO | AUTO |
| Steam Line Drains | CLOSED | CLOSED | OPEN | OPEN | OPEN | CLOSED | CLOSED |

* Steam dump availability requires `C-9` condenser-available and `P-12` not blocking (or deliberate bypass).

### 4.2 Feedwater System Valves

| Valve | S0 | S1 | S2 | S3a | S3b | S4 | S5 |
|-------|----|----|----|----|----|----|-----|
| Main FW Iso | CLOSED | CLOSED | CLOSED | CLOSED | CLOSED | CLOSED | CLOSED |
| Main FW Reg | CLOSED | CLOSED | CLOSED | CLOSED | CLOSED | CLOSED | CLOSED |
| AFW | STANDBY | STANDBY | STANDBY | READY | READY | READY | READY |

### 4.3 SG Blowdown Valves

| Valve | S0 | S1 | S2 | S3a | S3b | S4 | S5 |
|-------|----|----|----|----|----|----|-----|
| Bottom Blowdown | CLOSED | CLOSED | OPEN* | OPEN* | CLOSED | NORMAL | NORMAL |
| Surface Blowdown | CLOSED | CLOSED | OPEN* | OPEN* | CLOSED | NORMAL | NORMAL |
| To Circ Water | CLOSED | CLOSED | OPEN | OPEN | CLOSED | CLOSED | CLOSED |

*Open for SG draining during transition

### 4.4 RHR System Valves

| Valve | S0 | S1 | S2 | S3a | S3b | S4 | S5 |
|-------|----|----|----|----|----|----|-----|
| RHR Suction | OPEN | OPEN | OPEN | OPEN | OPEN | CLOSED | CLOSED |
| RHR Discharge | OPEN | OPEN | OPEN | OPEN | OPEN | CLOSED | CLOSED |
| RHR to CVCS (HCV-128) | OPEN | OPEN | THROTTLED | THROTTLED | THROTTLED | CLOSED | CLOSED |

---

## 5. TIMING PROFILE

### 5.1 Nominal Transition Times

| Transition | Duration | Cumulative | Key Activities |
|------------|----------|------------|----------------|
| S0 → S1 | 2-4 hr | 0-4 hr | Bubble formation, PZR heatup |
| S1 → S2 | 0.5-1 hr | 4-5 hr | RCP startup sequence |
| S2 → S3a | 0.5-1 hr | 5-6 hr | Hold at 160°F, begin SG drain |
| S3a → S3b | 0.5 hr | 6-6.5 hr | Cross 220°F, steam formation |
| S3b → S4 | 2.5-3 hr | 6.5-9.5 hr | 220→350°F at 50°F/hr |
| S4 → S5 | 4-5 hr | 9.5-14.5 hr | 350→557°F at 50°F/hr |

**Total Cold-to-Hot Standby: 10-15 hours (typical)**

### 5.2 Heatup Rate by Phase

| Phase | T_RCS Range | Nominal Rate | Limit |
|-------|-------------|--------------|-------|
| Pre-Mode 4 | 160-200°F | 50°F/hr | 100°F/hr |
| Mode 4 | 200-350°F | 50°F/hr | 100°F/hr |
| Mode 3 (early) | 350-500°F | 50°F/hr | 100°F/hr |
| Mode 3 (late) | 500-557°F | Decreasing | Steam dumps limit |

---

## 6. PROTECTION SYSTEM STATUS BY STATE

| State | SI Block | Steam Line Iso Block | Source Range | Rod Control |
|-------|----------|---------------------|--------------|-------------|
| S0-S2 | BLOCKED (P-11 not reset) | BLOCKED | ACTIVE | MANUAL |
| S3 | BLOCKED until P>1915 psig | BLOCKED until T>553°F | ACTIVE | MANUAL |
| S4 | AUTO (unblocked >1915 psig) | AUTO (>553°F) | BLOCKED >P-6 | MANUAL |
| S5 | ACTIVE | ACTIVE | BLOCKED | MANUAL |

### 6.1 Steam Dump Bridge States (Authoritative)

| Bridge State | Entry Conditions | Behavior |
|--------------|------------------|----------|
| Dumps Unavailable | `!C-9` OR (`P-12` active and not bypassed) | Dump valves forced closed; SG pressure follows thermodynamics and boundary-state mass balance |
| Dumps Armed (Closed) | `C-9` true AND (`P-12` not blocking OR bypassed) AND steam-pressure mode selected | Controller armed; valves remain closed while pressure error is within deadband |
| Dumps Modulating | Armed state plus SG pressure > (setpoint + deadband) | Dump valves modulate to hold steam pressure near setpoint |

`S4` and `S5` steam-dump behavior must be interpreted through this bridge-state logic.

---

## 7. IMPLEMENTATION NOTES

### 7.1 State Machine Logic

```csharp
public enum StartupState
{
    S0_ColdShutdownSolid,
    S1_ColdShutdownBubble,
    S2_RCPsRunning,
    S3a_HotShutdownEarly,
    S3b_HotShutdownMid,
    S4_HotStandbyHeatup,
    S5_HotStandbySteady
}

public StartupState GetCurrentState(float T_RCS, float P_RCS, bool pzrBubble, bool rcpsRunning, bool rhrIsolated)
{
    if (T_RCS >= 557f && P_RCS >= 2235f) return StartupState.S5_HotStandbySteady;
    if (T_RCS >= 350f && rhrIsolated) return StartupState.S4_HotStandbyHeatup;
    if (T_RCS >= 220f && T_RCS < 350f) return StartupState.S3b_HotShutdownMid;
    if (T_RCS >= 200f && T_RCS < 220f) return StartupState.S3a_HotShutdownEarly;
    if (rcpsRunning && T_RCS < 200f) return StartupState.S2_RCPsRunning;
    if (pzrBubble && !rcpsRunning) return StartupState.S1_ColdShutdownBubble;
    return StartupState.S0_ColdShutdownSolid;
}
```

### 7.2 Secondary State Derivation

```csharp
public SGState GetSGState(float T_RCS, float sgLevel_WR, float sgPressure_psig)
{
    if (sgPressure_psig >= 1090f) return SGState.SG5_Controlled;
    if (T_RCS >= 350f) return SGState.SG4_FullBoiling;
    if (T_RCS >= 220f) return SGState.SG3_EarlyBoiling;
    if (T_RCS >= 200f) return SGState.SG2_PreBoiling;
    if (sgLevel_WR < 50f) return SGState.SG1_Draining;
    return SGState.SG0_WetLayup;
}
```

---

## 8. REFERENCES

1. NRC HRTD Section 19.0 - Plant Operations (ML11223A342)
2. NRC HRTD Section 11.2 - Steam Dump Control System (ML11223A294)
3. NRC HRTD Section 7.1 - Main Steam Supply System (ML11221A146)
4. NRC HRTD Section 2.3 - Steam Generators (ML11251A016)
5. SG_Startup_Pressure_Bridge_Specification.md (internal)
6. Startup_Boundary_and_SteamDump_Authoritative_Spec.md (internal)
