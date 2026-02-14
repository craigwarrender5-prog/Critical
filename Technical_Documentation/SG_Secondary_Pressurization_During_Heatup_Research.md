# SG Secondary Pressurization During Heatup — Research & Findings Summary

**Compiled:** 2026-02-12  
**Purpose:** Consolidated NRC/Westinghouse findings on SG secondary pressure evolution during PWR cold startup heatup, to support diagnosis and fix of the boiling-onset pressure tracking failure in SGMultiNodeThermal.cs  
**Sources:** NRC HRTD Sections 2.3, 8.0, 17.0, 19.0; SG_THERMAL_MODEL_RESEARCH_v3.0.0; SG_HEATUP_BREAKTHROUGH_HANDOFF; existing Technical_Documentation

---

## 1. EXECUTIVE SUMMARY OF FINDINGS

### 1.1 The Bug (Current Model Behavior)

During simulated heatup, the SG secondary remains pinned near atmospheric pressure (17 psia / nitrogen blanket) even after boiling onset. When the SG top node crosses T_sat(17 psia) ≈ 219.4°F, the model instantly activates full boiling HTC (h = 500 BTU/hr·ft²·°F) and full effective area (0.25). With RCS at 302.8°F, this creates a driving ΔT of 83.4°F, producing ~27 MW of heat absorption — far exceeding the ~22.8 MW available from RCPs and heaters. The RCS crashes at -177°F/hr.

The root cause is that the secondary pressure rate limiter (200 psi/hr = 0.56 psi per 10-second timestep) cannot keep pace with the instantaneous activation of boiling heat transfer coefficients. In a real plant, pressure rise and boiling HTC are tightly coupled through a self-limiting feedback loop.

### 1.2 What Actually Happens in a Real Plant

From NRC HRTD 17.0, 19.0, and 8.0, the SG secondary pressure evolution during heatup follows this sequence:

1. **Cold shutdown (Mode 5):** SGs in wet layup at 100% WR level, nitrogen blanket at ~2-6 psig. MSIVs closed. Secondary is a sealed, stagnant water inventory.

2. **~200°F RCS:** SG draining begins via blowdown system (100% WR → 33±5% NR). MSIVs opened to begin steam line warming. Nitrogen supply to SGs isolated at ~220°F when steam forms.

3. **~220°F RCS:** First steam formation at tube surfaces. This is nucleate boiling at very low pressure. The steam produced begins displacing nitrogen through the now-open MSIVs into the steam lines. Initially, much of this steam condenses on cold steam line surfaces — this is the "steam line warming" process.

4. **220°F → 350°F RCS:** As more heat transfers to the secondary, more steam is produced. Secondary pressure rises naturally following the saturation curve. Steam lines continue warming. Condensate drains via steam line drain traps and drain trap bypasses. The SG secondary temperature is governed by T_sat(P_secondary), and pressure tracks the hottest secondary temperature.

5. **~350°F RCS (Mode 3 entry):** RHR isolated. By this point, steam lines are partially warmed. SG pressure is at roughly P_sat(T_secondary) which lags RCS by the tube ΔT (typically 20-50°F approach). So at RCS = 350°F, secondary might be ~300-330°F, corresponding to roughly 52-90 psig.

6. **~125 psig SG pressure:** Main and feed pump turbine gland seals established. Condenser vacuum drawn. This enables the steam dump system to function.

7. **Pressure continues rising** as RCS temperature increases. The SG secondary pressure-temperature relationship follows the saturation curve with the secondary tracking below RCS by the heat exchanger approach ΔT.

8. **1092 psig SG pressure (T_sat = 557°F):** Steam dumps actuate in steam pressure mode. This terminates the heatup — any excess RCP heat is dumped to the condenser as steam. RCS stabilizes at no-load T_avg = 557°F.

### 1.3 The Critical Self-Limiting Feedback Loop

In a real SG, the following feedback mechanism prevents runaway heat absorption:

```
More boiling → more steam production → pressure rises → T_sat rises
→ driving ΔT (T_tube - T_sat) decreases → boiling rate decreases
→ system reaches quasi-equilibrium at each timestep
```

**This is the fundamental physics the model is missing.** The boiling HTC can be high (500-10,000 BTU/hr·ft²·°F), but the driving ΔT for boiling is (T_wall - T_sat), NOT (T_RCS - T_sat). As pressure rises, T_sat rises, and the wall superheat shrinks dramatically. The system self-regulates.

---

## 2. NRC HRTD DOCUMENTATION — KEY EXCERPTS AND ANALYSIS

### 2.1 NRC HRTD Section 19.0 — Plant Operations (ML11223A342)

**Initial conditions (Section 19.2.1):**
> "The steam generators are in 'wet layup' (filled to the 100% level with water), and all secondary systems are secured with the exception of one circulating water pump."

**SG draining and steam formation (Section 19.2.2):**
> "As the reactor coolant temperature approaches 200°F, steam generator draining is commenced through the normal blowdown system."

> "At approximately 220°F RCS temperature, steam formation begins in the steam generators. The nitrogen supply to the steam generators, having been applied to prevent or minimize corrosion inside the steam generators, is now isolated."

**Steam line warming (Section 19.2.2):**
> "Warming of the steam lines is initiated by opening the main steam isolation valves (MSIVs), thereby admitting steam to the individual steam lines up to the main turbine stop valves. The steam lines are thus heated as the coolant is heated. Condensate that forms in the steam lines is drained to the condenser via steam line drain traps and drain trap bypasses."

**Heatup termination (Section 19.2.2):**
> "The primary plant heatup is terminated by automatic actuation of the steam dumps (in steam pressure control) when the pressure inside the steam header pressure reaches 1092 psig. The RCS temperature remains constant at 557°F, the steam dumps removing any excess energy that would tend to drive the RCS temperature higher."

**Appendix 19-1 — Startup Checklist:**
- Step 2: "Begin establishing steam generator water levels to 33 ± 5% narrow-range indication."
- Step 15: "Open main steam isolation valves and warm main steam lines."

### 2.2 NRC HRTD Section 17.0 — Plant Operations (ML023040268)

**Steam line warming sequence:**
> "The main and auxiliary steam lines are warmed as steam is available during the plant heatup. Main steam isolation valves are opened initially as heatup begins."

**Steam dump engagement:**
> "The steam dump system, operating in pressure control mode, will dump steam to the main condenser when steam pressure reaches a predetermined setpoint (normally 1,005 psig which is saturation pressure for the 547°F no-load reactor coolant system temperature)."

**NOTE:** Chapter 17 cites 1,005 psig / 547°F while Chapter 19 cites 1,092 psig / 557°F. This reflects different Westinghouse plant designs (different no-load T_avg setpoints). Our model uses 1092 psig / 557°F per the 4-loop design.

**Pressure evolution during heatup (Section 17.2.2):**
> "After the residual heat removal system is isolated from the reactor coolant system, system pressure is allowed to increase as the pressurizer temperature increases."

This confirms that during the heatup phase from ~350°F to no-load conditions, pressure rises naturally — there is no active pressure control until the steam dump setpoint is reached.

### 2.3 NRC HRTD Section 8.0 — Steam Dump and Bypass Control System (ML11251A024)

**During Startup (Section 8.3.4):**
> "Before the reactor can be taken critical, the RCS is heated up to no load temperature by the operation of the reactor coolant pumps. As the pumps add heat energy to the steam generators, steam pressure increases. When steam pressure tries to exceed the SDBCS setpoint of 900 psia, an error signal will be generated. The error signal will cause the bypass valves to open and will maintain steam pressure at 900 psia."

**NOTE:** Section 8 describes a CE (Combustion Engineering) design with a 900 psia setpoint (different from the Westinghouse 1092 psig). However, the operating principle is identical: SG pressure rises naturally during heatup until it hits the steam dump setpoint, at which point the bypass valves open to cap pressure and temperature.

**Key physics confirmation:** This explicitly states "steam pressure increases" as the RCPs add heat — the secondary DOES pressurize during heatup, tracking the saturation curve, until it reaches the steam dump setpoint.

### 2.4 NRC HRTD Section 2.3 — Steam Generators (ML11251A016)

**SG Pressure instrumentation:**
> "The steam generator pressure signals are also used in the asymmetrical steam generator transient circuit. This circuit compares each steam generator pressure and provides a signal to the Thermal Margin Low Pressure (TMLP) RPS trip circuitry."

**Blowdown system:**
> "During normal operations, the flow from the tank is routed through coolers that reduce the temperature to a value that is compatible with ion exchanger operation (~120°F)... The circulating water discharge would be used during steam generator draining activities."

---

## 3. SG SECONDARY PRESSURE PHYSICS — DETAILED ANALYSIS

### 3.1 Pressure-Temperature Relationship During Heatup

The SG secondary operates as a saturated system once boiling begins. This means:

```
P_secondary = P_sat(T_secondary_bulk)
T_secondary_bulk = T_sat(P_secondary)
```

The secondary temperature (and therefore pressure) is determined by the energy balance:

```
Q_in (from primary via tubes) = Q_out (steam vented through MSIVs + steam line condensation losses)
```

During early boiling (220-350°F), much of the steam produced condenses on cold steam lines. This acts as a natural pressure damper — the steam lines are a large cold metal mass that absorbs latent heat. As the steam lines warm, less condensation occurs, and pressure rises faster.

### 3.2 Approach Temperature (Primary-Secondary ΔT)

At any point during heatup, the SG operates with an approach ΔT:

```
ΔT_approach = T_RCS - T_sat(P_secondary)
```

This approach ΔT is what drives heat transfer. In real plant operation:
- At low power (heatup from RCPs only): ΔT_approach ≈ 20-50°F
- At full power (3,411 MWt): ΔT_approach ≈ 50-60°F
- The approach ΔT is determined by the heat transfer rate needed and the available UA

During heatup, the approach ΔT adjusts itself to transfer exactly the amount of heat being produced by the RCPs (minus RCS heatup and losses). If Q_SG tries to exceed available heat, RCS cools slightly, reducing ΔT, which reduces Q_SG — a stable feedback loop.

### 3.3 Why the Model Fails — Timescale Mismatch

In the current model:
- **Boiling HTC activation:** INSTANTANEOUS when node temperature crosses T_sat
- **Pressure response:** Rate-limited to 200 psi/hr (0.56 psi per 10-second timestep)

This creates a massive timescale mismatch. The boiling HTC sees T_sat(17 psia) = 219.4°F while RCS is at 302.8°F, giving ΔT = 83.4°F. But in reality, the secondary pressure would have already risen to something much higher (tracking the secondary temperature), giving a much smaller ΔT.

**Example of correct behavior at RCS = 302.8°F:**
- If secondary is at ~270°F (reasonable 30°F approach): P_secondary ≈ 27 psig
- T_sat(27 psig) = 270°F
- Driving ΔT for boiling = T_wall - T_sat ≈ 5-15°F (wall is between RCS and T_sat)
- Q_SG with 15°F wall superheat is vastly less than Q_SG with 83.4°F driving ΔT

### 3.4 Steam Line Warming — Natural Pressure Damper

An important physical effect not modeled: the cold steam lines act as a massive condenser during early heatup. When MSIVs open and steam enters the lines:

1. Steam contacts cold pipe walls (ambient temperature, ~100°F)
2. Steam condenses, releasing latent heat to the pipe metal
3. This condensation removes steam from the SG, limiting pressure rise
4. As pipe metal warms, condensation rate decreases
5. Eventually the steam lines are fully warmed and steam flows freely to condenser

This effect naturally dampens the initial pressure rise and prevents the kind of instant pressure spike that would occur in a sealed vessel. The NRC HRTD explicitly describes steam line drain traps handling this condensate.

### 3.5 Steam Dump Setpoint Variations

| Source | Setpoint | No-Load T_avg | Plant Type |
|--------|----------|---------------|------------|
| NRC HRTD 17.0 | 1,005 psig (T_sat=547°F) | 547°F | Westinghouse (older) |
| NRC HRTD 19.0 | 1,092 psig (T_sat=557°F) | 557°F | Westinghouse (4-loop) |
| NRC HRTD 8.0 | 900 psia (T_sat=532°F) | 532°F | CE design |

Our model uses 1092 psig / 557°F, consistent with the Westinghouse 4-loop design in NRC HRTD 19.0.

---

## 4. COMPLETE HEATUP TIMELINE WITH SG PRESSURE EVOLUTION

Based on NRC HRTD 17.0 and 19.0, synthesized with physics analysis:

| Time Est. | T_RCS (°F) | SG P_sec (approx) | T_sat_sec (°F) | Event / Action |
|-----------|------------|-------------------|----------------|----------------|
| 0.0 hr | 100-160 | ~2-6 psig (N₂) | N/A (subcooled) | Cold shutdown. SGs in wet layup. MSIVs closed. |
| ~0.5 hr | 160 | ~2-6 psig | N/A | RCPs started. RHR throttled. Hold at 160°F. |
| ~1.0 hr | 160 | ~2-6 psig | N/A | All 4 RCPs running. RHR pumps stopped. |
| ~1.5 hr | ~185 | ~2-6 psig | N/A | Heatup at ~50°F/hr. SG top node warming. |
| ~2.0 hr | ~200 | ~2-6 psig | N/A | SG draining begins via blowdown. Mode 4 entry. |
| ~2.0 hr | ~200 | ~2-6 psig | N/A | MSIVs opened. Steam lines begin warming. |
| ~2.5 hr | ~220 | ~0 psig | ~212°F | Steam formation in SGs. N₂ supply isolated. |
| ~2.5 hr | ~220 | ~0-5 psig | ~212-227°F | Boiling begins at tube surfaces. Steam enters lines. |
| ~3.0 hr | ~245 | ~10-15 psig | ~240-250°F | Steam line warming in progress. Condensate draining. |
| ~4.0 hr | ~295 | ~45-55 psig | ~280-290°F | Pressure tracking RCS with 20-40°F approach ΔT. |
| ~5.0 hr | ~345 | ~100-120 psig | ~325-340°F | Approaching Mode 3 entry. RHR isolated at 350°F. |
| ~5.0 hr | ~350 | ~105-130 psig | ~328-345°F | **Mode 3 entry.** RHR isolated. ECCS aligned. |
| ~5.5 hr | ~375 | ~155-180 psig | ~360-375°F | All letdown via CVCS normal orifices. |
| ~6.0 hr | ~400 | ~215-250 psig | ~385-400°F | Leak rate test conditions approaching. |
| ~6.5 hr | ~430 | ~295-340 psig | ~415-430°F | Pressure rising steadily on saturation curve. |
| ~7.0 hr | ~460 | ~400-440 psig | ~445-455°F | |
| ~7.5 hr | ~490 | ~510-560 psig | ~475-490°F | Gland seals established. Condenser vacuum drawn. |
| ~8.0 hr | ~520 | ~660-720 psig | ~505-515°F | |
| ~8.5 hr | ~547 | ~850-950 psig | ~530-545°F | Approaching steam dump setpoint. |
| ~9.0 hr | ~557 | **1092 psig** | **557°F** | **Steam dumps actuate.** Heatup terminated. |
| Steady | 557 | 1092 psig | 557°F | Hot standby. Steam dumps maintain pressure. |

**Key observation:** The approach ΔT (T_RCS - T_sat_secondary) remains in the range of 10-40°F throughout the heatup. It is NEVER 83°F as the current model produces.

---

## 5. IMPLICATIONS FOR THE MODEL FIX

### 5.1 What Must Change

The fundamental fix is to ensure that **SG secondary pressure tracks the secondary temperature on the saturation curve**, not lag behind it by hundreds of psi due to an arbitrary rate limiter. The self-limiting feedback loop must be present:

1. **Pressure must couple to temperature:** P_secondary = P_sat(T_secondary_hottest_node) — this is the defining relationship for a saturated boiling system.

2. **Boiling driving ΔT must use wall superheat, not bulk ΔT:** The driving force for nucleate boiling is (T_wall - T_sat), not (T_RCS - T_sat). The wall temperature sits between T_RCS and T_sat, determined by the primary-side and wall thermal resistances.

3. **Steam line warming provides initial damping:** During the first ~1-2 hours of boiling (220-350°F), cold steam lines absorb much of the produced steam via condensation, naturally damping the pressure rise rate. After lines warm, pressure responds more quickly.

4. **The 200 psi/hr rate limiter is physically wrong for boiling onset:** In a sealed vessel with boiling, pressure responds on the timescale of steam production — seconds, not hours. The rate limiter may have been intended to model steam line warming effects, but it's far too slow.

### 5.2 What Should NOT Change

- The three-regime model (Subcooled → Boiling → SteamDump) is fundamentally correct
- The boiling HTC values (500-700 BTU/hr·ft²·°F) are in the right range for nucleate boiling at SG pressures
- The steam dump setpoint of 1092 psig is correct per NRC HRTD 19.0
- The subcooled phase model is not implicated in this failure

### 5.3 Recommended Fix Approaches

**Option A — Instantaneous Pressure Equilibrium (Simplest, Recommended):**
Set P_secondary = P_sat(T_hottest_node) every timestep. This is physically correct for a saturated boiling system with open vent paths (MSIVs open, steam lines connected). The only rate-limiting effect is steam line warming during early boiling, which can be modeled as a ΔT offset rather than a pressure rate limit.

**Option B — Energy-Balance Pressure Model:**
Calculate steam production rate from Q_SG and h_fg, then compute pressure from steam accumulation minus steam removal (via condensation in lines + steam dump). More physically rigorous but significantly more complex.

**Option C — Coupled Boiling HTC with Pressure (Intermediate):**
Keep the rate-limited pressure but ramp the boiling HTC proportional to secondary pressure. At 17 psia, h_boiling ≈ 0 (subcooled). At higher pressures, h_boiling ramps to full nucleate boiling value. This couples the HTC to pressure evolution, preventing the instant activation problem.

---

## 6. CROSS-REFERENCE: DISCREPANCY IN STEAM DUMP SETPOINTS

Two NRC HRTD chapters cite different steam dump setpoints:

| Parameter | NRC HRTD 17.0 | NRC HRTD 19.0 |
|-----------|---------------|---------------|
| Steam dump setpoint | 1,005 psig | 1,092 psig |
| No-load T_avg | 547°F | 557°F |
| PZR auto control | 2,235 psig | 2,235 psig |
| SG level target | 50% NR | 33±5% NR |

These represent different vintage Westinghouse 4-loop designs with different no-load T_avg programs. Chapter 17 is Rev 0198 (older), Chapter 19 is Rev 0109/0400 (newer). Our model uses the Ch 19 values (1092 psig, 557°F), which align with standard Westinghouse 4-loop specifications.

---

## 7. REFERENCES

1. NRC HRTD Section 19.0 — Plant Operations (ML11223A342) — Primary source for heatup procedure
2. NRC HRTD Section 17.0 — Plant Operations (ML023040268) — Complementary heatup procedure (older revision)
3. NRC HRTD Section 8.0 — Steam Dump and Bypass Control System (ML11251A024) — Steam dump operation during startup
4. NRC HRTD Section 2.3 — Steam Generators (ML11251A016) — SG design and instrumentation
5. NRC HRTD Section 10.2 — Pressurizer Pressure Control (ML11223A287) — Referenced in existing documentation
6. NRC HRTD Section 10.3 — Pressurizer Level Control (ML11223A290) — Referenced in existing documentation
7. SG_THERMAL_MODEL_RESEARCH_v3.0.0.md — Internal research document
8. SG_HEATUP_BREAKTHROUGH_HANDOFF.md — Three-regime model derivation
9. NRC_HRTD_Startup_Pressurization_Reference.md — Existing Technical Documentation
