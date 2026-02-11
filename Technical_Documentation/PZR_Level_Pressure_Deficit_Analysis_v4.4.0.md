# Technical Analysis: Simulator vs. Real Westinghouse PWR — Root Cause Deficit Analysis

**Date:** 2026-02-11  
**Version:** 4.4.0-ANALYSIS  
**Purpose:** Identify specific engineering deficits between our simulator and a real Westinghouse 4-Loop PWR that cause the pressurizer level/pressure runaway during heatup

---

## Methodology

This analysis compares our simulator code (`CVCSController.cs`, `HeatupSimEngine.cs`, `CoupledThermo.cs`, `PlantConstants.CVCS.cs`) against three authoritative NRC HRTD documents:

- **NRC HRTD Section 4.1** — Chemical and Volume Control System (ML11223A214)
- **NRC HRTD Section 10.2** — Pressurizer Pressure Control System (ML11223A287)
- **NRC HRTD Section 10.3** — Pressurizer Level Control System (ML11223A290)
- **NRC HRTD Section 19.0** — Plant Operations / Heatup Procedure (ML11223A342)

Each deficit identified below references the specific NRC text and the specific line of simulator code where the discrepancy exists.

---

## DEFICIT 1: Charging Flow is the ONLY Control Actuator — Letdown is Uncontrolled

### What the Real Plant Does (NRC HRTD 10.3, Section 10.3.4.1):

> "Since the letdown flow is fixed, the inventory of the reactor coolant system is maintained by varying the charging flow."

And from NRC HRTD 4.1, Section 4.1.2.4:

> "The charging flow rate is determined by the pressurizer level control system. Control of the flow rate from the reciprocating pump is accomplished by varying the speed of the pump. With a centrifugal charging pump operating, the charging flow rate is controlled by varying the position of a modulating valve (FCV-121)."

**Key insight:** In the real plant, letdown IS essentially constant (75 gpm through one orifice at normal operating pressure). The level controller ONLY modulates charging via FCV-121. This is correct and our simulator matches this design.

### What Our Simulator Does:

`CVCSController.Update()`: PI controller modulates `ChargingFlow` only. Letdown is calculated from `PlantConstants.CalculateTotalLetdownFlow()` based on orifice sizing and ΔP. **This matches the real plant.**

### So Why Does the Real Plant NOT Have This Problem?

Because in a real plant, at 2235 psig with one 75-gpm orifice open, letdown is 75 gpm and charging (with seal injection) is 87 gpm. The 12 gpm excess matches the seal leakoff (12 gpm back to VCT). The flows balance. If thermal expansion causes a level rise, charging is reduced below 87 gpm (toward minimum of seal injection only = 32 gpm). At minimum charging of 32 gpm vs. 75 gpm letdown, the net RCS drain rate is 43 gpm — which is MORE than enough to offset any thermal expansion surge.

**The real issue is not the controller authority. The real issue is WHY our simulator's charging floor (32 gpm) with letdown of 60-78 gpm (net = -28 to -46 gpm drain) is insufficient, when in a real plant -43 gpm net drain easily controls level.**

### The Actual Deficit: Surge Flow Is Being Computed Independently of CVCS Mass Balance

In our simulator, the `CoupledThermo.SolveEquilibrium()` solver computes PZR water volume from mass conservation: **it treats thermal expansion as directly moving mass into the PZR, regardless of what the CVCS is doing.** The surge flow of ~23 gpm INTO the PZR is a physics result, but the CVCS net drain of -43 gpm OUT of the RCS is handled in a separate code path (`UpdateRCSInventory()`).

In a real plant, these are the SAME water. When the CVCS removes 43 gpm net from the RCS letdown line (at the cold leg), that mass is GONE from the primary loop. The thermal expansion still occurs, but the total RCS+PZR mass is lower because 43 gpm was drained. The PZR level rises LESS because there's less total mass.

**In our simulator, the CoupledThermo solver doesn't know about the CVCS drain. It computes PZR level based on a fixed total mass for that timestep, THEN the CVCS adjusts mass afterward. The solver sees a full-mass system every time, so it always computes maximum PZR level from expansion.**

**This is the primary engineering deficit.** It's not a cosmetic tuning issue — it's a fundamental architectural error in how mass accounting works between the CoupledThermo solver and the CVCS flow system.

---

## DEFICIT 2: Real Plant Has Three Letdown Orifices — We Model Only One

### What the Real Plant Does (NRC HRTD 4.1, Section 4.1.3.1):

> "Three orifices are provided to control letdown flow. Two of these orifices pass 75 gpm each at an RCS pressure of 2235 psig. The third orifice is rated at 45 gpm. Normally one of the two 75-gpm orifices is in service. If extra purification flow is desired, or additional letdown flow for boron concentration changes is required, the 45-gpm orifice may be placed in service."

And from NRC HRTD 19.0, Section B (MODE 4 to MODE 3 heatup):

> "As the RCS pressure increases, maintain letdown flow at a maximum of 120 gpm by increasing the setting of the backpressure regulator (PCV-131) until pressure reaches 350 psig and then by closing the letdown orifice isolation valves as necessary."

**This means during heatup, MULTIPLE orifices may be open simultaneously.** The 120 gpm maximum corresponds to the administrative limit from ion exchanger flow limits. During the final heatup phase (350°F to 557°F), the operator manages orifice lineup to maintain adequate letdown as pressure rises.

### What Our Simulator Does:

`PlantConstants.CalculateTotalLetdownFlow()` — hardcoded `numOrificesOpen = 1`. The calling code in `CVCSController.Update()` always passes 1.

At 2235 psig (2249 psia): `K × sqrt(2235 - 340) = 1.723 × sqrt(1895) = 75 gpm`. This is correct for ONE orifice.

At intermediate pressures during heatup (e.g., 1000 psig): `1.723 × sqrt(1000 - 340) = 1.723 × sqrt(660) = 44.3 gpm`. Only 44 gpm letdown with one orifice.

### Why This Matters:

During heatup from 350°F to 557°F, pressure rises from ~400 psig to 2235 psig. With only ONE orifice, letdown varies from ~0 to 75 gpm. But the operator in a real plant would have 2 orifices open during the lower-pressure phase, giving up to 88 gpm at 1000 psig (44 × 2). At 687 psig (our T+14hr condition): one orifice gives `1.723 × sqrt(687-340) = 32 gpm`, but two orifices give 64 gpm.

**Our simulator never opens the second orifice.** This means during the critical mid-heatup pressurization phase, our letdown is 30-50% lower than a real plant would have, reducing the CVCS's ability to drain thermal expansion volume.

### Impact on the Failure:

At T+14hr: Our log shows letdown = 56 gpm (at P = 687 psia = 672 psig, single orifice). With two orifices, letdown would be 112 gpm. Net CVCS drain with two orifices: -(112 - 32) = -80 gpm vs. the actual -24 gpm. That -80 gpm would overwhelm the ~23 gpm surge flow easily.

---

## DEFICIT 3: No Pressurizer Spray System During Heatup Pressurization

### What the Real Plant Does (NRC HRTD 10.2, Section 10.2.2):

> "As the pressure in the pressurizer increases above its normal setpoint, the master controller decreases the output of the proportional heaters. If the pressure continues to increase, the master controller output modulates the spray valves open."

And from NRC HRTD 10.2, Section 10.2.5.4:

> "The nominal setpoints (corresponding to the proportional output of the controller only) are 2260 psig (25 psig above setpoint) for spray valves to start opening and 2310 psig (75 psig above setpoint) for spray valves to fully open."

And from NRC HRTD 19.0, Section 19.2.2:

> "The pressurizer heaters and sprays are placed in automatic control when the pressure reaches the normal operating value of 2235 psig."

### What Our Simulator Does:

No spray model exists. During heatup pressurization, the only pressure control is the heater rate-feedback in `PRESSURIZE_AUTO` mode, which reduces heater power but never reduces it to zero (20% minimum floor). There is no mechanism to **reduce** pressure once it exceeds 2235 psig.

### Why This Matters:

In a real plant, once pressure reaches 2235 psig, the spray system becomes the primary means of preventing overpressure. Spray condenses steam, directly reducing pressure. The proportional + spray system can handle the normal thermal expansion transient easily. Without spray, our simulator has NO active pressure reduction mechanism above 2235 psig — only the passive effect of steam compression.

**At T+17hr with P = 1611 psia (1596 psig) and rising at 826 psi/hr, a real plant would:**
1. Heaters would be fully de-energized above 2250 psig (they already are at min in our sim, but would be fully OFF)
2. Spray would begin opening at 2260 psig
3. Full spray at 2310 psig would condense ~100 ft³/hr of steam, directly controlling pressure

**Our simulator has none of this.** The pressure overshoots 2235 psig with no resistance.

---

## DEFICIT 4: Heater Mode Transition Never Occurs — PRESSURIZE_AUTO Runs Forever

### What the Real Plant Does (NRC HRTD 19.0, Section 19.2.2):

> "The pressurizer heaters and sprays are placed in automatic control when the pressure reaches the normal operating value of 2235 psig."

This describes a discrete operational handoff: during heatup, the operator manages heater power manually or with a simple rate-control scheme. When pressure reaches 2235 psig, the operator **places heaters in automatic**, which means transferring control to the master pressure controller (PID at 2235 psig setpoint, per NRC HRTD 10.2).

### What Our Simulator Does:

The `PRESSURIZE_AUTO` mode runs from bubble formation through the entire heatup. It never transitions to `AUTOMATIC_PID`. The `PRESSURIZE_AUTO` mode has:
- A pressure-rate feedback that reduces power when `dP/dt > 100 psi/hr`
- A minimum power floor of 20% (`HEATER_STARTUP_MIN_POWER_FRACTION = 0.2`)
- No absolute pressure setpoint or ceiling

### Why This Matters:

The 20% minimum floor means heaters ALWAYS add 0.36 MW regardless of pressure. At T+17:47 with P = 2264 psia (2249 psig, ABOVE the operating setpoint), heaters are still on at 20%. In a real plant, the master pressure controller would have heaters at 0% (fully de-energized) because pressure is 14 psig above setpoint. The PID controller biases to zero heater output above ~2250 psig.

**The AUTOMATIC_PID mode already exists in our code** (`CVCSController.CalculateHeaterState`, case `HeaterMode.AUTOMATIC_PID`). It even has the correct backup heater cutoff logic at 2250 psig (`HEATER_PROP_CUTOFF_PSIG`). The full PID controller with proper staging exists in `UpdateHeaterPID()`. **The code just never gets called** because no mode transition is triggered.

---

## DEFICIT 5: Excess Letdown Not Available During Heatup

### What the Real Plant Does (NRC HRTD 4.1, Section 4.1.2.5):

> "Certain plant evolutions, such as RCS heatup or the inoperability of the normal letdown path, may require the use of the excess letdown. At low RCS pressures, when the letdown orifices do not pass their design flows, the excess letdown may be placed in service. Placing excess letdown in service assists the normal letdown system in removing the expansion volume due to the RCS heatup."

### What Our Simulator Does:

No excess letdown path is modeled. During low-pressure heatup (below ~500 psig), the orifice path produces very little flow (30-40 gpm with one orifice), and the RHR crossconnect provides 75 gpm below 350°F but is isolated above 350°F. In the gap between 350°F (RHR isolation) and ~500°F (where orifice flow becomes adequate), the plant relies on orifice flow alone — which may be insufficient.

### Why This Matters:

The excess letdown provides an additional 20 gpm at normal operating pressure. During heatup at low pressures, it provides less but still supplements the normal letdown. This is specifically identified by the NRC document as being used during heatup.

---

## DEFICIT 6: PCV-131 (Backpressure Regulator) Not Modeled — Affects Letdown Below 350 psig

### What the Real Plant Does (NRC HRTD 19.0, Section 19.2.1):

> "The control room operator manipulates CVCS backpressure regulating valve PCV-131 in either automatic or manual to vary the rate of letdown from the RCS."

And from NRC HRTD 4.1, Section 4.1.3.1:

> "A PID controller receives an input from a pressure transmitter downstream of the letdown heat exchanger and compares this pressure with an adjustable setpoint (normally 340 psig). The controller modulates the pressure control valve to maintain letdown system pressure at setpoint."

### What Our Simulator Does:

`PlantConstants.CalculateTotalLetdownFlow()` — below 350°F, it returns `RHR_CROSSCONNECT_FLOW_GPM = 75 gpm`. Above 350°F, it calculates orifice flow based on `K × sqrt(P_rcs - 340)`. The backpressure regulator is assumed to maintain 340 psig downstream.

### The Gap:

The real PCV-131 is operator-adjustable. During heatup, the operator can increase the PCV-131 setpoint to increase letdown flow at lower pressures. From NRC HRTD 19.0 Appendix B: "increasing the setting of the backpressure regulator (PCV-131) until pressure reaches 350 psig." This means the operator LOWERS the backpressure (increasing ΔP across the orifice) to maintain letdown during the transition from RHR to orifice path.

Our model uses a fixed 340 psig backpressure. If the real operator reduces this to 200 psig during the transition, orifice flow at 700 psig would be: `1.723 × sqrt(686 - 200) = 1.723 × sqrt(486) = 38 gpm` vs. `1.723 × sqrt(686 - 340) = 32 gpm`. Small difference, but it compounds with the orifice count deficit.

**Lower priority deficit** — the orifice count (Deficit 2) matters more.

---

## DEFICIT 7: Level Program Values During Heatup May Be Wrong

### What the Real Plant Does (NRC HRTD 10.3, Section 10.3.4.1):

> "The pressurizer low level setpoint of 25% is selected to prevent the pressurizer from emptying following a reactor trip."
> "The pressurizer high level setpoint of 61.5% is derived from the natural expansion of the reactor coolant when the coolant is heated up from the no-load to the full power Tavg (557°F to 584.7°F)."

**These 25% and 61.5% are the AT-POWER level program limits** (from 557°F to 584.7°F). The level program at Tavg = 557°F is 25%. At Tavg = 584.7°F, it's 61.5%.

### What Our Simulator Does:

`PlantConstants.Pressurizer.cs` defines a HEATUP level program:
- 25% at T=200°F → 60% at T=557°F (linear)

This heatup program is separate from the at-power program and is used by `GetPZRLevelSetpointUnified()`.

### Analysis:

At T+14hr (T_avg = 357°F): setpoint = 25% + (60%-25%) × (357-200)/(557-200) = 25% + 35% × 0.44 = 40.4%. Our log shows setpoint = 40.4%. This seems reasonable for a heatup program.

At T+17hr (T_avg = 466°F): setpoint = 25% + 35% × (466-200)/357 = 25% + 35% × 0.745 = 51.0%. Log shows 51.0%.

**The question is: does a real plant actually use 51% at T_avg = 466°F?** The NRC HRTD doesn't give a specific heatup level program — the 25%-61.5% program is for at-power Tavg changes. During heatup, the operator likely targets a lower setpoint to leave room for expansion. But this is a secondary concern.

---

## SUMMARY: Priority-Ranked Deficits

| # | Deficit | Engineering Impact | Priority |
|---|---------|-------------------|----------|
| 1 | **CoupledThermo solver doesn't account for CVCS mass drain** | Primary cause of runaway — solver computes PZR level from stale total mass | **CRITICAL** |
| 2 | **Only one letdown orifice modeled (should be 2-3)** | Letdown 30-50% too low during mid-heatup pressurization | **CRITICAL** |
| 4 | **No heater mode transition to AUTOMATIC_PID** | Heaters stuck at 20% minimum above 2235 psig | **HIGH** |
| 3 | **No pressurizer spray during heatup** | No active pressure reduction above 2235 psig | **HIGH** (included in v4.4.0 Stage 4) |
| 5 | **No excess letdown path** | Missing 20 gpm supplemental letdown during heatup | **MEDIUM** |
| 6 | **PCV-131 backpressure regulator not adjustable** | Minor letdown shortfall during RHR→orifice transition | **LOW** |
| 7 | **Heatup level program not validated against plant data** | Potentially incorrect setpoints, secondary concern | **LOW** |

---

## What a Corrected Implementation Plan Should Address

Based on this analysis, the original implementation plan (v4.4.0) had some correct ideas but was treating symptoms rather than root causes. Here is what actually needs to happen:

### Fix 1 (CRITICAL): Integrate CVCS mass drain into CoupledThermo solver

The `SolveEquilibrium()` call must receive the **current** total mass after CVCS flows have been applied, not the stale mass from the previous step. This means moving the `UpdateRCSInventory()` mass adjustment to BEFORE the solver call, or passing the net CVCS flow as an input to the solver.

**This is the #1 fix.** Without it, no amount of controller tuning will work because the solver will always compute maximum PZR level from expansion.

### Fix 2 (CRITICAL): Add orifice lineup management during heatup

The real plant has 3 orifices (75+75+45 gpm). During heatup, the operator opens orifices as needed to maintain adequate letdown. Our simulator should:
- Track which orifices are open (initially 1×75 gpm)
- Open the 45 gpm orifice when PZR level exceeds setpoint by >5% (operator action simulation)
- Close orifices if letdown approaches 120 gpm max (ion exchanger limit)
- Pass `numOrificesOpen` to `CalculateTotalLetdownFlow()`

### Fix 3 (HIGH): Add heater mode transition

When pressure reaches ~2200 psia (approaching operating band), transition from `PRESSURIZE_AUTO` to `AUTOMATIC_PID`. The `UpdateHeaterPID()` code already exists and is fully functional. Just need the trigger.

### Fix 4 (HIGH — INCLUDED IN v4.4.0): Pressurizer spray model

Included in v4.4.0 Stage 4 to complete the master pressure controller triad (heaters / spray / PID). This ensures we have the full pressure control system in place and won't need to revisit this area. The spray system is essential for both heatup completion and future at-power operations.

### Fix 5 (MEDIUM — FUTURE): Excess letdown path

Add as a future enhancement. The normal letdown with 2-3 orifices should be sufficient.

---

## References

1. NRC HRTD Section 4.1, "Chemical and Volume Control System," Rev 1208, ML11223A214
2. NRC HRTD Section 10.2, "Pressurizer Pressure Control System," Rev 1208, ML11223A287
3. NRC HRTD Section 10.3, "Pressurizer Level Control System," Rev 0502, ML11223A290
4. NRC HRTD Section 19.0, "Plant Operations," Rev 0400, ML11223A342
