# SG Secondary Pressure Bridge Specification — Startup (220°F to 1092 psig)

**Status:** CANONICAL INTERNAL SPECIFICATION  
**Created:** 2026-02-17  
**Purpose:** Define expected SG secondary pressure behavior during the "bridge period" from steam formation (~220°F) to steam dump actuation (1092 psig), filling the gap in authoritative external documentation.

---

## 1. DOCUMENTATION GAP ACKNOWLEDGMENT

### 1.1 What External Sources Provide

| Source | Coverage | Gap |
|--------|----------|-----|
| NRC HRTD 19.0 | Heatup rate (50°F/hr), steam dump endpoint (1092 psig), steam formation at ~220°F | No pressure trajectory between 220°F and dump actuation |
| NRC HRTD 11.2 | Steam dump interlocks, pressure setpoints, controller behavior | Only covers post-activation; no pre-dump pressure source behavior |
| NRC HRTD 7.1 | MSIV/bypass valve functions, steam line warming description | No integrated state sequence with valve positions |
| NRC HRTD 2.3 | SG design parameters, operating characteristics | Steady-state operation; no startup transient details |

### 1.2 What External Sources Do NOT Provide

1. **Expected SG pressure values** at specific RCS temperatures during heatup
2. **Mass balance rules** for steam production vs. condensation/venting before dump actuation
3. **State transition sequence** defining which valves are open when during line warming
4. **Approach ΔT contract** specifying expected primary-to-secondary temperature difference during heatup

### 1.3 Basis for This Specification

This specification is derived from:
- **Thermodynamic first principles** (saturated system behavior)
- **Heat transfer physics** (approach ΔT requirements)
- **Procedural statements** in NRC HRTD documents
- **Engineering judgment** consistent with real plant behavior

---

## 2. SG SECONDARY PRESSURE MODEL

### 2.1 Fundamental Relationship

During the boiling phase (>220°F RCS), the SG secondary operates as a **saturated system**:

```
P_secondary = P_sat(T_secondary_bulk)
T_secondary_bulk = T_sat(P_secondary)
```

This is thermodynamically mandated — a boiling liquid in equilibrium with its vapor follows the saturation curve.

### 2.2 Approach Temperature Definition

The SG secondary temperature lags the RCS by the **approach ΔT**:

```
ΔT_approach = T_RCS - T_sat(P_secondary)
```

The approach ΔT is determined by the heat transfer rate and available UA:

```
Q_SG = UA_eff × ΔT_approach
```

Where:
- Q_SG = Heat transferred to SG secondary
- UA_eff = Effective heat transfer coefficient × area (varies with flow regime)
- ΔT_approach = Primary-to-secondary temperature difference

### 2.3 Expected Approach ΔT Range

| Phase | RCS Temp Range | Expected ΔT_approach | Basis |
|-------|----------------|----------------------|-------|
| Pre-boiling | <220°F | N/A (subcooled) | SG secondary not pressurizing |
| Early boiling | 220-280°F | 30-50°F | Limited effective area (stratified), steam line warming damping |
| Mid heatup | 280-400°F | 20-35°F | More tube area participating, lines warmed |
| Late heatup | 400-557°F | 15-25°F | Full bundle participation, approaching steady-state |

---

## 3. STARTUP PRESSURE TRAJECTORY

### 3.1 Complete Timeline with Pressure Values

| Time (hr) | T_RCS (°F) | P_secondary (psig) | T_sat (°F) | ΔT_approach (°F) | State / Event |
|-----------|------------|-------------------|------------|------------------|---------------|
| — | 100-160 | ~2-6 (N₂) | N/A | N/A | Mode 5. Wet layup. MSIVs closed. |
| 0.0 | 160 | ~2-6 (N₂) | N/A | N/A | RCPs started. RHR maintaining temp. |
| 0.5 | 160 | ~2-6 (N₂) | N/A | N/A | All 4 RCPs running. RHR stopped. |
| 1.0 | 185 | ~2-6 (N₂) | N/A | N/A | Heatup at ~50°F/hr. SG top warming. |
| 1.5 | 200 | ~2-6 (N₂) | N/A | N/A | **Mode 4 entry.** SG draining starts. MSIVs open. |
| 2.0 | 220 | ~0-5 | 212-227 | N/A→~40 | **Steam formation.** N₂ isolated. Boiling onset. |
| 2.5 | 245 | ~15 | ~250 | ~45 | Steam line warming. Condensate draining. |
| 3.0 | 270 | ~27 | ~259 | ~41 | Pressure tracking saturation curve. |
| 3.5 | 295 | ~52 | ~284 | ~41 | Approach ΔT stabilizing. |
| 4.0 | 320 | ~75 | ~307 | ~38 | Lines mostly warmed. |
| 4.5 | 345 | ~105 | ~328 | ~37 | Approaching Mode 3 entry. |
| 5.0 | 350 | ~115 | ~338 | ~37 | **Mode 3 entry.** RHR isolated. ECCS aligned. |
| 5.5 | 375 | ~155 | ~361 | ~34 | All letdown via CVCS normal orifices. |
| 6.0 | 400 | ~200 | ~382 | ~33 | Leak rate test conditions. |
| 6.5 | 425 | ~260 | ~405 | ~30 | Pressure rising on saturation curve. |
| 7.0 | 450 | ~330 | ~429 | ~29 | Condenser vacuum established. |
| 7.5 | 475 | ~415 | ~452 | ~28 | Gland seals available. |
| 8.0 | 500 | ~520 | ~476 | ~27 | |
| 8.5 | 525 | ~650 | ~500 | ~26 | Approaching steam dump setpoint. |
| 9.0 | 545 | ~830 | ~520 | ~25 | |
| 9.3 | 557 | ~990 | ~541 | ~22 | Near steam dump actuation. |
| 9.5 | 557 | **1092** | **557** | **0** | **Steam dumps actuate.** Heatup terminated. |
| Steady | 557 | 1092 | 557 | 0 | Hot standby. Dumps controlling. |

### 3.2 Graphical Representation

```
SG Secondary Pressure vs RCS Temperature (Heatup)
═══════════════════════════════════════════════════════════════════════════════

P_sec (psig)
  │
1200 ├─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─●  Steam Dump
  │                                                        ╱    Setpoint
1000 ├                                                    ╱     (1092 psig)
  │                                                  ╱
 800 ├                                              ╱
  │                                            ╱
 600 ├                                        ╱
  │                                      ╱
 400 ├                                ╱
  │                             ╱
 200 ├                       ╱
  │                    ╱
 100 ├               ╱  ← Mode 3 Entry (350°F)
  │            ╱
  50 ├       ╱
  │     ╱
   0 ├●──╱──────────────────────────────────────────────────────────────────────
     N₂  │              │              │              │              │
    100  150   200   250   300   350   400   450   500   550   600
         ↑     ↑                ↑
      RCPs  Steam            RHR                              T_RCS (°F)
      Start  Forms          Isolated
            (220°F)         (350°F)

Legend:
  ●────  SG Secondary Pressure Trajectory
  - - -  Steam Dump Setpoint (1092 psig)

Note: Pressure rises along saturation curve with approach ΔT of 20-45°F
```

### 3.3 Approach ΔT vs RCS Temperature

```
Approach ΔT (°F)
  │
 50 ├     ●
  │      ╲
 45 ├       ●──●
  │            ╲
 40 ├              ●──●
  │                    ╲
 35 ├                      ●──●
  │                            ╲
 30 ├                              ●──●──●
  │                                        ╲
 25 ├                                          ●──●
  │                                                ╲
 20 ├                                                  ●
  │
 15 ├
  │
 10 ├
  │
  5 ├
  │
  0 ├─────────────────────────────────────────────────────●  (At dump actuation)
    │         │         │         │         │         │
   200      250       300       350       400       450       500       557
                                                              T_RCS (°F)

Notes:
- High ΔT early due to limited tube participation and steam line condensation
- ΔT decreases as more tube area participates and lines warm
- At dump actuation, ΔT → 0 as secondary reaches RCS no-load temp
```

---

## 4. STATE TRANSITION SEQUENCE

### 4.1 Steam Line Warming States

| State | T_RCS | MSIVs | Bypass Valves | Steam Dumps | Steam Flow Path |
|-------|-------|-------|---------------|-------------|-----------------|
| **S0: Cold** | <200°F | CLOSED | CLOSED | OFF | None (N₂ blanket) |
| **S1: Draining** | ~200°F | OPEN | CLOSED | OFF | Steam to lines; condensate drains |
| **S2: Warming** | 200-350°F | OPEN | CLOSED | OFF | Steam warms lines; trapped condensate evacuates |
| **S3: Warmed** | >350°F | OPEN | CLOSED | OFF (pending) | Lines at temperature; steam accumulates |
| **S4: Controlled** | 557°F | OPEN | CLOSED | ON (pressure) | Steam dumps modulate to hold 1092 psig |

### 4.2 Mass Balance During Each State

| State | Steam Production | Steam Removal | Net Effect |
|-------|------------------|---------------|------------|
| **S0** | None | None | N₂ blanket pressure only |
| **S1** | Begins | Condensation on cold lines + drain traps | Pressure rise damped |
| **S2** | Increasing | Decreasing condensation (lines warming) | Pressure accelerates |
| **S3** | Stable | Minimal (lines at temp) | Pressure approaches setpoint |
| **S4** | Controlled | Dump valves modulate | Pressure held at 1092 psig |

---

## 5. PHYSICS BASIS

### 5.1 Self-Limiting Feedback Loop

The following feedback mechanism prevents runaway heat absorption:

```
More boiling → more steam production → pressure rises → T_sat rises
→ driving ΔT (T_tube - T_sat) decreases → boiling rate decreases
→ system reaches quasi-equilibrium at each timestep
```

**Critical insight:** The boiling HTC can be high (500-10,000 BTU/hr·ft²·°F), but the driving ΔT for boiling is (T_wall - T_sat), NOT (T_RCS - T_sat). As pressure rises, T_sat rises, and the wall superheat shrinks dramatically.

### 5.2 Steam Line Warming as Natural Damper

During early boiling (220-350°F), cold steam lines act as a massive condenser:

1. Steam contacts cold pipe walls (~100°F ambient initially)
2. Steam condenses, releasing latent heat to pipe metal
3. Condensation removes steam from SG, limiting pressure rise
4. As pipe metal warms, condensation rate decreases
5. Eventually lines fully warmed; steam flows freely to dump valves

This effect naturally dampens the initial pressure rise rate.

### 5.3 Energy Balance

At any point during heatup:

```
Q_produced (RCPs + decay) = Q_RCS_heatup + Q_SG + Q_losses
```

Where Q_SG adjusts via the approach ΔT to balance the equation.

If the RCS tries to heat faster than the SG can absorb, approach ΔT increases, increasing Q_SG, restoring balance.

---

## 6. IMPLEMENTATION GUIDANCE

### 6.1 Pressure Calculation

**Recommended approach:** Set P_secondary = P_sat(T_secondary_bulk) every timestep.

This is physically correct for a saturated boiling system with open vent paths (MSIVs open).

### 6.2 Steam Line Warming Effect

Model as a ΔT offset during early boiling:

```csharp
float steamLineWarmingFactor = Mathf.Clamp01((T_RCS - 220f) / (350f - 220f));
float effectiveApproachDeltaT = baseApproachDeltaT + (1f - steamLineWarmingFactor) * 15f;
```

This adds 0-15°F extra approach ΔT during the warming phase.

### 6.3 Avoid Rate-Limiting Pressure

**DO NOT** use a psi/hr rate limiter on pressure during boiling phase. The secondary pressure must track the saturation curve. Rate limiting creates the bug identified in SG_Secondary_Pressurization_During_Heatup_Research.md.

### 6.4 Boiling HTC Coupling

If using instantaneous pressure tracking, the boiling HTC must see the correct T_sat:

```csharp
float T_sat_secondary = SteamTables.GetSaturationTemperature(P_secondary);
float drivingDeltaT = T_tube_wall - T_sat_secondary;  // NOT (T_RCS - T_sat)
float Q_boiling = h_boiling * A_eff * drivingDeltaT;
```

---

## 7. VALIDATION CRITERIA

### 7.1 Acceptance Criteria

| Parameter | Acceptable Range | Failure Mode |
|-----------|------------------|--------------|
| Approach ΔT at 300°F | 30-45°F | Model overdriving SG heat absorption |
| Approach ΔT at 500°F | 20-30°F | Heat transfer not scaling properly |
| Time to reach 350°F | 3-4 hours from RCP start | Rate too fast or too slow |
| Time to reach 557°F | 8-10 hours from RCP start | Consistent with 50°F/hr nominal |
| Pressure at Mode 3 entry | 100-130 psig | Pressure tracking saturation |
| Final heatup rate | Approaches 0°F/hr at 557°F | Steam dumps controlling |

### 7.2 Warning Signs of Model Failure

1. **Approach ΔT < 15°F during heatup** → SG absorbing too much heat
2. **Approach ΔT > 60°F** → SG not participating enough
3. **Pressure rate > 500 psi/hr during early boiling** → Missing steam line damping
4. **Pressure rate < 50 psi/hr during mid heatup** → Over-damped pressure model
5. **RCS temperature crashes** → Boiling HTC activated without pressure tracking

---

## 8. CROSS-REFERENCE: RELATED DOCUMENTS

| Document | Relationship |
|----------|--------------|
| `SG_Secondary_Pressurization_During_Heatup_Research.md` | Detailed research basis for this spec |
| `NRC_HRTD_Section_19.0_Plant_Operations.md` | Heatup procedure source |
| `NRC_HRTD_Section_11.2_Steam_Dump_Control.md` | Steam dump controller details |
| `NRC_HRTD_Section_2.3_Steam_Generators.md` | SG design parameters |
| `NRC_HRTD_Section_7.1_Main_Auxiliary_Steam.md` | Steam line valve configurations |
| `SG_THERMAL_MODEL_RESEARCH_v3.0.0.md` | Three-regime thermal model |

---

## 9. VERSION HISTORY

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-02-17 | Initial specification created to fill documentation gap |
