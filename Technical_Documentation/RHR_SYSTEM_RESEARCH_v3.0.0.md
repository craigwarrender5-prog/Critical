# RHR System Technical Research — v3.0.0

**Date:** 2026-02-10  
**Purpose:** Technical reference for RHR system modeling in heatup simulation  
**Primary Source:** NRC HRTD 5.1 — Residual Heat Removal System (ML11223A219)  
**Secondary Sources:** NRC HRTD 19.0 (ML11223A342), NRC HRTD 5.2, Byron/Braidwood UFSAR, Wikipedia RHR article, OPEN100 reference design

---

## 1. System Overview

The Westinghouse 4-Loop PWR RHR system consists of:
- **2 RHR pumps** (Train A and Train B) — vertical centrifugal, mechanical seals
- **2 RHR heat exchangers** (HX-A and HX-B) — shell and U-tube type
- Associated piping, valves, and instrumentation
- Each train is independently powered from separate vital electrical buses

### Flow Path (Shutdown Cooling Mode)
- **Suction:** Hot leg of Loop 4 → through series isolation valves (8701 & 8702) → RHR pumps
- **Discharge:** RHR pumps → tube side of RHR HX → return to each cold leg of RCS
- **Cooling:** Component Cooling Water (CCW) on shell side of RHR HX

### Three Functions
1. **Shutdown cooling** — Remove decay heat during second phase of cooldown (350°F → <200°F)
2. **ECCS low-pressure injection** — Inject borated water from RWST during LOCA
3. **Refueling water transfer** — Between RWST and refueling cavity

---

## 2. Operating Limits and Interlocks

### Entry Conditions (RHR placed in service)
- RCS pressure ≤ **425 psig** (suction valve interlock — prevents opening above this)
- RCS T_avg ≤ **350°F** (per NRC HRTD 19.0 — Mode 4 boundary)
- Placed in service approximately **4 hours after reactor shutdown** (per NRC HRTD 5.1)

### Automatic Isolation
- Suction valves **automatically close** when RCS pressure increases to approximately **585 psig**
- This protects RHR piping (design pressure ~600 psig) from RCS overpressurization

### Relief Valve Protection
- Suction line relief valve — sized for combined flow of all charging pumps
- Discharge line relief valves — sized for maximum backleakage through check valves
- RHR suction relief valve setpoint: **450 psig** (per PlantConstants.Pressure.cs already in code)

---

## 3. Component Specifications (Westinghouse 4-Loop)

### RHR Pumps
- **Type:** Vertical, single-stage centrifugal
- **Quantity:** 2 (one per train)
- **Design flow rate:** ~3,000 gpm each (typical W 4-loop; some designs up to 4,500 gpm)
- **Minimum flow:** 500 gpm (min-flow bypass valve opens below this)
- **Min-flow bypass closes:** Above 1,000 gpm
- **Seal cooling:** Component cooling water
- **Motor:** Powered from vital buses, auto-transfer to emergency diesel on LOOP
- **Pump heat:** ~0.5 MW each (estimated from motor size, ~400-700 HP typical)

### RHR Heat Exchangers
- **Type:** Shell and U-tube
- **Quantity:** 2 (one per train)
- **Tube side:** Reactor coolant (borated water)
- **Shell side:** Component cooling water
- **Design basis:** Heat load and ΔT at 20 hours post-shutdown (worst case — minimum ΔT)
- **Design performance:** Cool RCS from 350°F to 140°F in 16 hours (both trains)
- **Single train capability:** Can cool plant, but extended time required

### Derived HX Parameters (Engineering Estimates)
Based on the design requirement to cool from 350°F to 140°F in 16 hours with both trains:

**Heat load calculation:**
- RCS water mass: ~525,000 lbm (4-loop)
- RCS metal mass (effective): ~400,000 lbm steel equivalent
- Total thermal mass: ~525,000 × 1.0 + 400,000 × 0.12 ≈ 573,000 BTU/°F
- Temperature drop: 350°F - 140°F = 210°F
- Energy to remove: 573,000 × 210 = ~120 × 10⁶ BTU
- Plus decay heat over 16 hours: ~1% power × 3411 MWth × 3.412 × 10⁶ BTU/MWh × 16hr ≈ average ~30 × 10⁶ BTU
- Total energy: ~150 × 10⁶ BTU over 16 hours
- Average heat removal rate: ~9.4 × 10⁶ BTU/hr per both trains (~4.7 × 10⁶ per train)

**UA estimate per HX:**
- At design conditions (RCS ~250°F average, CCW ~95°F inlet):
- LMTD ≈ ~120-140°F average over cooldown
- UA per HX ≈ Q / LMTD ≈ 4.7 × 10⁶ / 130 ≈ **36,000 BTU/(hr·°F) per HX**
- Total both trains: ~72,000 BTU/(hr·°F)
- Note: This is a reasonable estimate; actual UA varies with flow and temperature

### Component Cooling Water (CCW) Interface
- **CCW supply temperature:** 85-95°F typical (varies by plant, season, service water temp)
- **CCW flow to each RHR HX:** ~3,000-5,000 gpm (design flow, constant during cooldown)
- **CCW return temperature:** Varies — at 350°F RCS start, CCW outlet may reach ~130-150°F
- **CCW heat rejection:** Via CCW heat exchangers to Essential Service Water (ESW) → ultimate heat sink

---

## 4. RHR During Heatup (Critical for v3.0.0 Scope)

### Starting Condition: Cold Shutdown (Mode 5, T_avg < 200°F)
Per NRC HRTD 19.0 Section 19.2.1:
- RHR system is **in service** providing decay heat removal
- RHR provides forced circulation through core
- Letdown is via **RHR-to-CVCS cross-connect valve HCV-128** (fully open)
- Letdown flow: ~75 gpm through HCV-128 → PCV-131 (throttled)
- RCS pressure controlled by CVCS charging/letdown balance (solid plant, 320-400 psig)

### During Heatup (T < 350°F, P < 425 psig)
- RHR remains in service providing forced circulation
- **RHR HX outlet valve is throttled** to reduce heat removal → allow heatup
- Heat source during early heatup is RHR pump energy (~1 MW total for both pumps)
- The RHR HX can be **partially or fully bypassed** to maximize heat input
- HX bypass valve (HCV-618) is manually adjusted to maintain constant RHR flow while varying HX cooling

### RHR Isolation Sequence (During Heatup)
Per NRC HRTD 19.0:
1. As RCS temperature approaches 350°F and conditions allow RCP start:
   - First RCP is started (requires P ≥ 320 psig, bubble exists)
   - RCP provides forced flow and ~5.25 MW heat per pump
2. After all RCPs started and running:
   - **RHR system is isolated from RCS** (suction valves 8701/8702 closed)
   - RHR aligned to ECCS standby lineup (at-power configuration)
   - All letdown now through **normal CVCS letdown orifices** (not HCV-128)
   - Backpressure valve PCV-131 adjusted for normal letdown pressure (350 psig)
3. Once RHR isolated:
   - Pressure increases freely (no longer limited by RHR design pressure)
   - Heat removal transitions to Steam Generators
   - Accumulators isolated at 1925 psig
   - Normal two-phase pressurizer operations begin

### Key Timing (NRC HRTD 19.0 Heatup Procedure)
- Cold shutdown start: ~100°F
- RHR throttled/bypassed to allow heatup
- Hold at ~160°F (cold water addition accident limit)
- Bubble formation at ~230°F in pressurizer
- RCP start: ~320 psig, after bubble established
- RHR isolation: After RCPs running, T_avg approaching 350°F
- Transition to SG heat sink

---

## 5. Solid Plant Operations (RHR Role)

Per NRC HRTD 5.1 Section 5.1.4.2:
- RHR circulates coolant from Loop 4 hot leg to all cold legs
- System is water-solid (no bubble in pressurizer)
- Pressure controlled by CVCS charging/letdown balance
- RHR flow diverted partially to CVCS via HCV-128 for cleanup and pressure control
- VCT acts as surge/buffer volume

### Pressure Control During Solid Plant
- Charging rate: set manually via HCV-182
- Letdown rate: controlled by PCV-131 (auto or manual)
- If charging > letdown → pressure rises
- If letdown > charging → pressure drops
- Overpressure protection: CVCS letdown relief valve, RHR suction/discharge relief valves

---

## 6. Modeling Requirements for v3.0.0

### What Must Be Modeled
1. **RHR forced circulation** — provides flow through core during cold shutdown
2. **RHR HX heat removal** — Q = UA × LMTD, with throttle valve control
3. **RHR HX bypass** — operator control to reduce/eliminate cooling for heatup
4. **RHR pump heat input** — ~0.5 MW per pump (adds heat to RCS)
5. **RHR-to-CVCS letdown path** — HCV-128 cross-connect for letdown during solid plant
6. **RHR isolation sequence** — secure RHR when transitioning to RCPs/SGs
7. **Interlock logic** — suction valves cannot open above 425 psig, auto-close at 585 psig

### What Can Be Simplified
- CCW temperature can be assumed constant at ~95°F (modeling full CCW system deferred)
- RHR pump curves not needed — assume constant flow at design rate when running
- Single lumped model for both trains (effective = 2× single train parameters)
- Relief valve physics not needed yet (just enforce pressure limits)
- ECCS/LOCA functions not needed for heatup scope

### Key Parameters for PlantConstants.RHR.cs

| Parameter | Value | Source |
|-----------|-------|--------|
| RHR pump count | 2 | NRC HRTD 5.1 |
| RHR pump flow (each) | 3,000 gpm | W 4-loop typical |
| RHR pump flow (total, both trains) | 6,000 gpm | Derived |
| RHR pump heat (each) | 0.5 MW | Engineering estimate |
| RHR pump heat (total) | 1.0 MW | Derived |
| RHR HX UA (each) | 36,000 BTU/(hr·°F) | Derived from design cooldown |
| RHR HX UA (total, both trains) | 72,000 BTU/(hr·°F) | Derived |
| CCW inlet temperature | 95°F | Typical design value |
| CCW flow per HX | 4,000 gpm | Design estimate |
| RHR suction valve interlock (open) | ≤ 425 psig | NRC HRTD 5.1 |
| RHR suction valve interlock (auto-close) | 585 psig | NRC HRTD 5.1 |
| RHR design pressure | 600 psig | NRC HRTD 5.1 |
| RHR relief valve setpoint | 450 psig | PlantConstants.Pressure.cs (existing) |
| RHR entry temperature | 350°F | NRC HRTD 5.1 / existing code |
| RHR min-flow bypass open | < 500 gpm | NRC HRTD 5.1 |
| RHR min-flow bypass close | > 1,000 gpm | NRC HRTD 5.1 |
| HCV-128 letdown flow (typical) | 75 gpm | NRC HRTD 19.0 |

---

## 7. RHR Role During Heatup — Thermal Impact

### Before RCP Start (Cold Shutdown → ~320 psig/230°F)
Heat balance:
- **Heat IN:** RHR pump heat (~1.0 MW) + decay heat (~0.5-2 MW at cold shutdown days after trip)
- **Heat OUT:** RHR HX removal (throttled) + insulation losses (~0.2-0.5 MW)
- **Net:** Operator controls heatup rate by throttling RHR HX outlet valve
- If HX fully bypassed: heatup rate = (Q_pump + Q_decay - Q_losses) / thermal_mass
- If HX fully engaged: system cools (normal cooldown mode)

### Typical heatup rate with RHR only (before RCPs):
- Net heat input ~1-2 MW with HX mostly bypassed
- RCS thermal mass ~573,000 BTU/°F
- Rate = (1.5 MW × 3.412 × 10⁶ BTU/MWh) / 573,000 BTU/°F ≈ **~9°F/hr**
- This is MUCH slower than the 45-50°F/hr after RCPs start
- This matches plant experience: early heatup with RHR only is slow

### After RCP Start
- RCPs add ~21 MW heat
- RHR still running briefly but being isolated
- Heatup rate jumps to ~45-50°F/hr
- RHR isolation is the transition point to SG-dominated heat sink

---

## 8. References

1. NRC HRTD 5.1 — Residual Heat Removal System (ML11223A219) — **FULL TEXT RETRIEVED**
2. NRC HRTD 19.0 — Plant Operations (ML11223A342) — **FULL TEXT PREVIOUSLY RETRIEVED**
3. NRC HRTD 5.2 — Emergency Core Cooling Systems (ML11223A220) — Partial
4. Byron/Braidwood UFSAR Chapter 5 — Referenced for plant-specific data
5. Wikipedia — Residual Heat Removal System — General reference
6. OPEN100 — Open-source PWR design — Design pressure/flow reference
7. Byron NRC Exam 2019 (ML20054A571) — Confirms RHR HX bypass valve operation during heatup
