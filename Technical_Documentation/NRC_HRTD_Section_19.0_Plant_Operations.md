# NRC HRTD Section 19.0 — Plant Operations

**Source:** https://www.nrc.gov/docs/ML1122/ML11223A342.pdf  
**Retrieved:** 2026-02-17  
**Revision:** Rev 0400

---

## Overview

This document describes plant operations during startup, shutdown, and power operations for Westinghouse PWR plants. Critical for simulator development of heatup procedures.

---

## 19.2 Plant Heatup

### 19.2.1 Initial Conditions

For startup, the following conditions exist:
- RCS pressure maintained between **320 and 400 psig** (solid plant operation)
- Boron concentration sufficient for ≥1% Δk/k shutdown margin
- **Pressurizer is solid** (no steam bubble)
- Reactor coolant pumps secured
- Decay heat removed by RHR system
- Steam generators in wet layup (100% level)
- **Mode 5 (Cold Shutdown):** T_avg < 200°F, Keff < 0.99

### 19.2.2 Operations

#### Bubble Formation Procedure

> "Steam bubble formation in the pressurizer is accomplished by heating the water volume of the pressurizer with the pressurizer heaters. Concurrently, charging and letdown flows are adjusted to maintain the pressure within the RCS between 320 and 400 psig."

> "When the temperature in the pressurizer reaches **428°F to 448°F** (saturation temperature for an RCS pressure between 320 psig and 400 psig), charging flow is reduced to form the steam bubble."

> "As the steam bubble forms, RCS letdown is increased, and the charging flow is maintained constant. The difference between these flow rates causes the level in the pressurizer to decrease, and **operators lower the level to 25 percent.**"

#### Post-Bubble Operations

> "With a steam bubble established in the pressurizer, RCS pressure is controlled by pressurizer heater and spray valve operation."

> "Once a bubble is drawn in the pressurizer and the pressure in the RCS has reached 320 psig, the reactor coolant pumps are started."

> "With all four RCPs operating and the RHR system secured, the reactor coolant begins to heat up at a rate of **approximately 50°F per hour.**"

#### Heatup Progression

> "As a result of the heatup and the draining of the pressurizer, approximately one-third of the reactor coolant system volume (30,000 gallons) is diverted to the holdup tanks through the chemical and volume control system."

> "The primary plant heatup is terminated by automatic actuation of the steam dumps (in steam pressure control) when the pressure inside the steam header pressure reaches 1092 psig. The RCS temperature remains constant at 557°F..."

---

## Appendix 19-1: Plant Startup from Cold Shutdown

### Initial Conditions

1. **Cold Shutdown - MODE 5:**
   - Keff < 0.99
   - 0% power
   - T_avg < 200°F
2. RCS: solid
3. RCS Temperature: 150-160°F
4. RCS Pressure: **320-400 psig**
5. Steam Generators: wet layup (100% wide-range level)
6. Secondary Systems: shutdown

### A. Heatup from COLD SHUTDOWN to HOT SHUTDOWN (MODE 5 to MODE 4)

> **Step 5: Establish a pressurizer steam bubble by:**
> a. Increasing pressurizer temperature using pressurizer heaters.
> b. Adjust charging and letdown flow to maintain pressurizer pressure at approximately 320-400 psig while reducing pressurizer level.
> c. **As pressurizer temperature approaches 428°F (saturation temperature for 320 psig), reduce pressurizer level toward 25%.**

> **Step 11:** When pressurizer level is at the **no-load operating level (25%)**, place the pressurizer level control system in automatic.

### B. Heatup from HOT SHUTDOWN to HOT STANDBY (MODE 4 to MODE 3)

> **Step 8:** Establish HOT STANDBY conditions of 557°F T_avg.

---

## Critical Data for Simulator Development

### Pressurizer Level During Heatup

| Phase | Level Setpoint | Temperature | Notes |
|-------|---------------|-------------|-------|
| Solid plant (before bubble) | 100% (solid) | < 428°F | No steam space |
| Bubble formation | Lower toward 25% | 428-448°F | At saturation temp |
| **Post-bubble heatup** | **25%** | 200-557°F | No-load program |
| Hot Standby (Mode 3) | 25% | 557°F | No-load T_avg |
| At-power | 25% → 61.5% | 557-584.7°F | Power escalation |

### Key Setpoints

| Parameter | Value | Source |
|-----------|-------|--------|
| Solid plant pressure band | 320-400 psig | Section 19.2.1 |
| Bubble formation temperature | 428-448°F | Section 19.2.2 |
| Post-bubble level target | **25%** | Appendix 19-1 Step 11 |
| Heatup rate with 4 RCPs | ~50°F/hr | Section 19.2.2 |
| Steam dump actuation (heatup termination) | 1092 psig | Section 19.2.2 |
| No-load T_avg | 557°F | Section 19.2.2 |

### Mode Definitions

| Mode | Name | Temperature | Keff |
|------|------|-------------|------|
| 5 | Cold Shutdown | < 200°F | < 0.99 |
| 4 | Hot Shutdown | 200-350°F | < 0.99 |
| 3 | Hot Standby | > 350°F | < 0.99 |
| 2 | Startup | Any | ≥ 0.99, ≤5% power |
| 1 | Power Operation | Any | ≥ 0.99, >5% power |

---

## CRITICAL FINDING: Heatup Level Program

**The NRC documentation does NOT support a heatup level program that ramps from 25% to 60%.**

Per NRC HRTD 19.0:
- Level is lowered to **25%** during bubble formation (~428°F)
- Level is maintained at **25% (no-load operating level)** throughout heatup
- Level control system is placed in automatic at **25%** level
- The 25% → 61.5% ramp only occurs during power escalation (557°F → 584.7°F)

**Implication for Simulator:**
The "heatup level program" in PlantConstants.Pressurizer.cs that ramps from 25% at 200°F to 60% at 557°F is **not supported by NRC documentation** and should be removed or corrected.

---

## References

- NRC HRTD Section 4.1 — Chemical and Volume Control System
- NRC HRTD Section 5.1 — Residual Heat Removal System
- NRC HRTD Section 10.2 — Pressurizer Pressure Control
- NRC HRTD Section 10.3 — Pressurizer Level Control

---

*Document retrieved and formatted 2026-02-17*
