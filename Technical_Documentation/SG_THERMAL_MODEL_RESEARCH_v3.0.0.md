# SG Thermal Model Research — v3.0.0 Physics Overhaul
## Compiled: 2026-02-10

---

## 1. CRITICAL FINDING: RHR Role During Heatup

### What Actually Happens During PWR Heatup (Cold Shutdown → HZP)

The RHR system does NOT "bypass" the SG during heatup. The actual sequence from NRC HRTD 19.0 and 5.1 is:

**During Cooldown (power → cold):**
1. **Phase 1 (350°F → 200°F):** SGs + steam dump/AFW cool the RCS (first phase)
2. **Phase 2 (<350°F, <425 psig):** RHR takes over from SGs (second phase of cooldown)
3. RHR cools RCS from 350°F down to <200°F (cold shutdown)
4. RHR maintains cold shutdown temperature indefinitely

**During Heatup (cold → HZP):**
1. RHR is running and maintaining cold shutdown (~100-160°F)
2. Pressurizer bubble is drawn, RCPs are started
3. RHR heat exchangers are **throttled** to allow temperature to rise (not "bypassing" — throttling)
4. At ~160°F: RHR HX throttled to **hold** temperature (cold water addition accident limit)
5. Once all 4 RCPs running: RHR pumps **stopped** — RCPs provide heat source
6. Heatup proceeds at ~50°F/hr from RCP heat (~16 MW) + decay heat
7. SGs act as **passive heat sink** absorbing some of this heat
8. At ~350°F: RHR isolated from RCS, ECCS aligned (Mode 3 entry)
9. At ~220°F: Steam formation in SGs, nitrogen blanket isolated
10. Steam dumps control at 1092 psig (557°F no-load Tavg)

### Key Insight for Our Model
- **RHR does NOT bypass the SG** — it's a separate heat removal system on the primary side
- During heatup, RHR is OFF (stopped after RCPs start). Heat source is RCPs only.
- The SG is a **passive, stagnant heat sink** filled with cold water during heatup
- The SG receives heat via the U-tubes as hot RCS water flows through them
- The SG does NOT need to be "modeled as bypassed" — it just has enormous thermal inertia
- The problem is that our model makes the SG absorb WAY too much heat

### Implication
We do NOT need an RHR system model for the heatup simulation. RHR is already stopped before heatup begins. What we need is a **correct SG passive heat sink model** that realistically captures the very slow warming of a massive, stagnant water column.

---

## 2. SG Heat Transfer Physics During Heatup

### The Physical Setup
- 4 SGs, each containing ~415,000 lb of stagnant cold water (wet layup, 100% WR level)
- RCS hot water (~100°F and rising) flows INSIDE U-tubes, driven by RCPs
- Heat transfers through Inconel 690 tube wall to secondary water OUTSIDE tubes
- Secondary side has NO forced circulation — natural convection only
- Hot water rises to top, cold water stays at bottom → thermal stratification

### Heat Transfer Chain
```
RCS water (forced convection) → Tube wall (conduction) → Secondary water (natural convection)
```

The overall heat transfer coefficient U is:
```
1/U = 1/h_primary + R_wall + 1/h_secondary
```

Where:
- h_primary ≈ 800-3000 BTU/(hr·ft²·°F) — forced convection with RCPs (NOT the bottleneck)
- R_wall = t/(k·A) ≈ negligible — thin Inconel 690, high conductivity
- h_secondary ≈ 20-160 BTU/(hr·ft²·°F) — natural convection on tube exterior (THE bottleneck)

### Churchill-Chu Correlation for Horizontal Cylinder
For a single isolated horizontal tube in quiescent fluid:
```
Nu_D = [0.60 + 0.387·Ra_D^(1/6) / (1 + (0.559/Pr)^(9/16))^(8/27)]²
```

Where:
- Ra_D = Gr·Pr = (g·β·ΔT·D³)/(ν·α)
- D = tube OD = 0.0625 ft
- Properties evaluated at film temperature

**At typical heatup conditions (150°F water, ΔT = 10°F):**
- β ≈ 0.00026 /°F
- ν ≈ 5.0×10⁻⁶ ft²/s
- α ≈ 5.8×10⁻⁶ ft²/s
- Pr ≈ 3.5
- Ra_D ≈ (32.2)(0.00026)(10)(0.0625³)/((5.0e-6)(5.8e-6))
- Ra_D ≈ (32.2)(0.00026)(10)(2.44e-4)/(2.9e-11) ≈ 7.1×10⁵
- Nu ≈ [0.60 + 0.387(7.1e5)^(1/6)/(1+(0.559/3.5)^(9/16))^(8/27)]² 
- Nu ≈ [0.60 + 0.387(9.45)/(1.077)]² ≈ [0.60 + 3.40]² ≈ 16
- h_isolated = Nu·k/D = 16 × 0.38 / 0.0625 ≈ 97 BTU/(hr·ft²·°F)

### Tube Bundle Penalty
In a dense tube bundle (triangular pitch 1.063", tube OD 0.75", gap 0.313"):
- Boundary layers from adjacent tubes overlap
- Convection plumes interfere with each other
- Pitch-to-diameter ratio: P/D = 1.063/0.75 = 1.42
- Published bundle correction factors: 0.3-0.6 for tightly packed bundles
- **Effective h_secondary ≈ 30-60 BTU/(hr·ft²·°F) for stagnant conditions**

### Stratification Effect on Effective Area
This is the crucial physics. Only tube sections IN CONTACT WITH THE THERMOCLINE OR ABOVE IT participate in meaningful heat transfer:

- Hot water from convection rises and sits on top (stable stratification, Ri >> 1)
- Below the thermocline, secondary water is cold and stagnant
- Tubes below the thermocline have very small ΔT to the local secondary water
- **Effective area = only the portion of tube bundle above the thermocline**

### Thermocline Descent Rate
The thermocline descends primarily by:
1. **Thermal conduction through water** — very slow (α_water ≈ 5.8×10⁻⁶ ft²/s = 0.021 ft²/hr)
2. **Conduction through tube metal** — tubes act as thermal wicks (α_inconel ≈ 0.14 ft²/hr, ~7× faster)
3. **Convective transport** — heated plumes from tube surfaces drive local mixing above thermocline

The effective thermal diffusivity with tube metal present is HIGHER than pure water because the Inconel tubes conduct heat downward faster than the water alone. But the dominant mechanism remains the local convective heating at the tube surface, which creates a slowly descending boundary.

**Estimated thermocline descent rate:**
- Pure thermal diffusion: x ~ √(4·α·t) → for α_eff ≈ 0.1 ft²/hr (enhanced by tube conduction):
  - After 1 hr: ~0.6 ft
  - After 4 hr: ~1.3 ft  
  - After 8 hr: ~1.8 ft
- The tube bundle is ~21 ft tall straight section
- Over the ~8 hr of heatup, thermocline descends perhaps 1-3 ft total
- This means **<15% of tube area** participates during the entire heatup from cold

---

## 3. SG Draining During Startup

From NRC HRTD 2.3 and 19.0:
- At ~200°F RCS temp: SG draining commenced via normal blowdown system
- Draining from 100% WR (wet layup) to ~33% ±5% NR (operating level)
- Blowdown system: 150 gpm normal rate through coolers (~120°F) and ion exchangers
- 2,350 gallon blowdown tank
- Water removed from bottom of SG (tubesheet area)
- Draining removes cold water from bottom → may slightly help thermocline
- But primary purpose is chemistry control and establishing operating level

---

## 4. SG Secondary Pressure During Heatup

- Initially: Atmospheric pressure + nitrogen blanket (corrosion protection)
- Nitrogen blanket isolated when steam forms (~220°F)
- At ~220°F: First steam bubbles form at top of secondary (near U-bend)
- MSIVs opened to warm steam lines
- Pressure builds with temperature following saturation curve
- Steam dumps control at 1092 psig setpoint (Tsat = 557°F)
- The simultaneous heating and pressurization occurs naturally as the secondary heats

---

## 5. Expected Realistic Temperature Profile

Based on the physics above:

| Time | T_RCS | T_SG_top | T_SG_bulk | SG_Heat(MW) | RCS Rate |
|------|-------|----------|-----------|-------------|----------|
| Start | 100°F | 100°F | 100°F | 0 | - |
| +1hr | 145°F | 108°F | 101°F | ~1.5 MW | ~45°F/hr |
| +2hr | 190°F | 118°F | 103°F | ~2.5 MW | ~45°F/hr |
| +3hr | 235°F | 132°F | 106°F | ~3.5 MW | ~44°F/hr |
| +4hr | 280°F | 150°F | 112°F | ~4.5 MW | ~43°F/hr |
| +6hr | 365°F | 195°F | 130°F | ~6.0 MW | ~40°F/hr |
| +8hr | 440°F | 260°F | 165°F | ~8.0 MW | ~35°F/hr |
| +9hr | 480°F | 320°F | 210°F | ~10 MW | ~30°F/hr |
| +10hr | 520°F | 390°F | 280°F | ~12 MW | ~25°F/hr |
| End | 557°F | 545°F | 500°F | ~14 MW | ~5°F/hr |

Key features:
1. **Large RCS increase, small SG increase** initially (limited U-bend area)
2. **Growing gap** RCS-SG for first several hours (SG thermal inertia dominates)
3. **Gap starts narrowing** as thermocline descends and more area participates
4. **Final convergence** as steam formation improves heat transfer dramatically
5. **Average heatup rate ~45-50°F/hr** in early phase, slowing as SG catches up
6. **Total SG heat absorption: 2-6 MW** during subcooled phase, rising to 10-14 MW at end

---

## 6. Key Formulas for Implementation

### Effective Heat Transfer Area
```
A_eff = A_total × f_thermocline(z_therm/H_total) × f_bundle_penalty
```
Where:
- z_therm = thermocline position from top (starts at 0, grows to H_total)
- f_thermocline = fraction of tube area above thermocline
- f_bundle_penalty ≈ 0.3-0.5 (tube bundle natural convection penalty)

### Thermocline Position
```
dz/dt = α_eff / z_therm  (diffusion-limited front propagation)
z_therm = √(4 · α_eff · t)
```
Where α_eff ≈ 0.05-0.15 ft²/hr (enhanced thermal diffusivity with tube metal conduction)

### Per-Node Heat Transfer
```
Q_node = h_eff × A_node_eff × (T_rcs - T_node)
```
Where:
- h_eff = U_overall from series resistance model
- A_node_eff = node area × (1 if above thermocline, ~0.02 if below)
- T_node = local secondary temperature at this elevation

### Richardson Number (Stability Check)
```
Ri = g·β·ΔT·L / v²
```
For Ri >> 1: stratification is stable (suppressed mixing)
For Ri < 1: mixing dominates (thermocline breaks down)

During subcooled heatup: Ri ~ 10,000-100,000 (extremely stable stratification)
