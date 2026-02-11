# NRC HRTD — PWR Startup Pressurization & Post-Bubble Control Reference

**Compiled:** 2026-02-11  
**Purpose:** Reference document for v5.0.0 implementation — consolidated NRC HRTD findings on RCS initial pressurization, post-bubble pressure/level control philosophy, and startup sequence.  
**Sources:** NRC HRTD Sections 4.1, 10.2, 10.3, 17.0, 19.0

---

## 1. Initial RCS Pressurization (NRC HRTD 17.0 — ML023040268)

### Starting Conditions
- Mode 5, Cold Shutdown
- T_avg = 120°F
- Pressurizer pressure = 50–100 psig  
- Pressurizer SOLID (100% water, no steam space)
- Boron concentration sufficient for 10% shutdown margin
- RCPs OFF
- RHR in service for decay heat removal and letdown

### Pressurization Procedure
> "Pressure is increased by maintaining charging flow greater than letdown flow. When pressure is stable between 400 and 425 psig, the reactor coolant pumps are started to begin reactor coolant system heatup. Pressurizer heaters are energized to begin pressurizer heatup."

- Charging via CCP from VCT/RWST
- Letdown via RHR cross-connect (HCV-128) and PCV-131
- Net +20–40 gpm yields ~50–100 psi/hr pressurization rate (solid water is nearly incompressible — small volume additions cause large pressure changes)
- Target: 400–425 psig
- Must stay below 425 psig (RHR suction limit)
- Cold overpressure protection setpoint provides backup

### Key Sequencing
1. Pressurize to 400–425 psig via charging > letdown
2. Start RCPs (per Ch. 17: BEFORE bubble; per Sec. 19.0: AFTER bubble)
3. Energize PZR heaters
4. Heat PZR to T_sat (450°F at 400 psig)
5. Draw bubble by maximizing letdown, minimizing charging

---

## 2. Solid Plant Pressure Control (NRC HRTD 19.0 — ML11223A342)

> "the RCS pressure is being maintained between 320 and 400 psig"

> "CVCS by one of the centrifugal charging pumps. If the charging and the letdown flows are equal, RCS pressure remains constant. Any imbalance between charging and letdown causes the pressure in the RCS to increase or decrease."

> "While the plant is in this configuration, HCV-128 (the letdown isolation valve from the RHR system) is fully open. The control room operator manipulates CVCS backpressure regulating valve PCV-131 in either automatic or manual to vary the rate of letdown from the RCS."

- Pressure controlled purely by charging/letdown balance (no steam space)
- Additional letdown via CVCS orifices 8149A/B/C available but low flow at low RCS pressure

---

## 3. Bubble Formation (NRC HRTD 17.0 & 19.0)

### NRC HRTD 17.0:
> "When pressurizer temperature reaches saturation temperature for the pressure being maintained (450°F for 400 psig), a pressurizer bubble is established. Reactor coolant system temperature is approximately 250-300°F. The bubble is established by maximizing letdown and minimizing charging flow. This will cause the pressurizer level to decrease. System pressure will be maintained at 400 psig as the saturated pressurizer water flashes to steam."

### NRC HRTD 19.0:
> "Prior to starting the reactor coolant pumps, a steam bubble is established in the pressurizer. This ensures that a surge volume is available in the pressurizer for water expansion caused by the heatup of the coolant."

> "Once a bubble is drawn in the pressurizer and the pressure in the RCS has reached 320 psig, the reactor coolant pumps are started."

### Key Details:
- Max letdown (~120 gpm via RHR cross-tie), min charging (~20 gpm)  
- Level drops 100% → ~25%
- CCP started when level < 80% (NRC HRTD 19.0)
- Confirm bubble via aux spray test (rapid P drop proves compressible steam)
- Pressure maintained at 400 psig throughout drain (water flashing to steam maintains pressure)

---

## 4. Post-Bubble Pressure Control (NRC HRTD 10.2 — ML11223A287)

### Control System Architecture
- Master PID controller: Proportional + Integral + Derivative
- Input: error between actual PZR pressure and variable setpoint (normally 2235 psig)
- Setpoint span: 1700–2500 psig

### Component Actuation Sequence (from 2235 psig setpoint):

| Condition | Action |
|-----------|--------|
| P < 2210 psig (-25) | Backup heaters ON |
| P < 2235 psig | Proportional heaters increase output |
| P = 2235 psig | Proportional heaters compensate for bypass spray + ambient losses |
| P > 2260 psig (+25) | Spray valves start opening |
| P > 2310 psig (+75) | Spray valves fully open |
| P > 2335 psig (+100) | PORVs open |
| P > 2385 psig (+150) | High pressure reactor trip |
| P < 1865 psig | Low pressure reactor trip (above P-7 only) |
| P < 1807 psig | Safety injection actuation |

### During Heatup (Post-Bubble, Pre-2235 psig):
> "After the residual heat removal system is isolated from the reactor coolant system, system pressure is allowed to increase as the pressurizer temperature increases."

> "Pressurizer heaters and sprays are placed in automatic control when the pressure reaches the normal operating value of 2235 psig."

**Key insight:** Pressure is NOT controlled to 2235 psig during heatup. It rises naturally following T_sat as PZR heats. The PID controller only engages at 2235 psig.

### Steady-State Operation:
- Proportional heaters compensate for ~1 gpm bypass spray flow and ambient heat losses
- Many plants run backup heaters manually to promote spray flow for PZR boron mixing
- PORVs have interlock: require two independent pressure channels sensing ≥2335 psig

---

## 5. Post-Bubble Level Control (NRC HRTD 10.3 — ML11223A290)

### Level Program
- Programmed as function of auctioneered high T_avg
- 25% at no-load T_avg (547–557°F)  
- 61.5% at full-power T_avg (584.7°F)
- Follows natural expansion characteristics of reactor coolant

### Control Method
- Letdown is CONSTANT at 75 gpm (one 75-gpm orifice in service)
- Charging flow varies via PI controller:
  - If CCP (centrifugal): FCV-121 flow control valve modulates
  - If PD pump: pump speed varies
- Master level controller: PI (proportional + integral) — prevents reaction to small perturbations while eliminating steady-state errors

### Level Setpoint Limits
- Minimum: 25% (prevents PZR emptying after reactor trip; ensures 10% step load increase doesn't uncover heaters)
- Maximum: 61.5% (prevents going solid after turbine trip from 100% without direct reactor trip)

### Special Interlocks
- Low level (17%): Isolate letdown, turn off ALL heaters (protects heaters from steam exposure)
- High level (70%): High level alarm, redundant letdown isolation, heater cutoff
- High level (92%): Reactor trip (2/3 logic) — prevents going solid, protects safety valves from liquid discharge

### Level +5% Above Setpoint → Backup Heaters ON
- Anticipatory signal: large insurge (from load decrease) brings cool water into PZR
- This eventually LOWERS pressure as cool water mixes
- Energizing backup heaters offsets the expected pressure reduction

### During Heatup:
> "When pressurizer level, as read on the hot calibrated channels, indicates the no-load programmed setpoint, charging flow is placed in automatic. As system heatup continues, pressurizer level will try to increase due to coolant expansion. Pressurizer level control will compensate by reducing charging flow."

**Thermal expansion inventory removal:**
- During heatup from 120°F to 557°F, ~30,000 gallons of water expand out of the RCS
- This water goes: RCS → PZR (level rises) → Level controller reduces charging → VCT level rises → VCT high level → Divert to holdup tanks → BRS processing

---

## 6. CVCS Flow Balance at Normal Operations (NRC HRTD 4.1 — ML11223A214)

| Path | Flow | Notes |
|------|------|-------|
| Total CCP discharge | 87 gpm | Split between charging and seal injection |
| Normal charging to RCS | 55 gpm | Via CV-8146 to Loop 1 cold leg |
| Seal injection to RCPs | 32 gpm | 8 gpm per RCP (4 pumps) |
| RCP seal return to RCS | 20 gpm | 5 gpm per RCP via hydraulic chambers |
| RCP seal leakoff to CVCS | 12 gpm | 3 gpm per RCP via seal return line |
| Normal letdown from RCS | 75 gpm | One 75-gpm orifice in service |
| **Net RCS flow** | **0 gpm** | 55 + 20 = 75 (balanced) |

### Orifice Lineup
- Two 75-gpm orifices at 2235 psig RCS pressure
- One 45-gpm orifice
- Normal: one 75-gpm orifice in service
- Maximum letdown: 120 gpm (to prevent exceeding 132 gpm max charging)

### During Cold Shutdown / Heatup
- Letdown via RHR cross-connect (HCV-128 fully open, PCV-131 throttled)
- Normal orifices have extremely low flow at low RCS pressure
- Seal injection not active until RCPs running (no seal injection during solid plant ops)

---

## 7. Key Timing Milestones During Heatup (NRC HRTD 19.0)

| Event | Temperature | Pressure | Action |
|-------|-------------|----------|--------|
| SG drain | ~200°F T_rcs | — | Drain SGs through blowdown system |
| Oxygen spec | ≤250°F | — | Must be in spec before exceeding 250°F |
| H₂ blanket | ~200°F | — | Establish H₂ in VCT (purge N₂) |
| SG steam formation | ~220°F T_rcs | — | N₂ supply to SGs isolated |
| Mode 4 → Mode 3 | 350°F | — | RHR isolated, ECCS lineup, normal letdown orifices |
| SIT valves open | — | 1000 psig | Open cold leg accumulator discharge valves |
| Accumulator alignment | — | 1925 psig | Full ECCS equipment check |
| Heater/spray AUTO | — | 2235 psig | PID pressure control engaged |
| Steam dump actuation | — | 1092 psig (SG) | Caps RCS T_avg at 547–557°F |
| Leak rate test | >400°F | 2235 psig | Required if RCS opened for refueling |
