# IP-0030 Parameter Output Traceability Report (HeatupSimEngine-Primary)

Date: 2026-02-16
Plan Source: `Governance/ImplementationPlans/IP-0030 - DP-0008 - Validation Dashboard Complete Redesign.md:79`
Parameter Source Scope: `Governance/ImplementationPlans/IP-0030 - DP-0008 - Validation Dashboard Complete Redesign.md:89`

## Output Channels Traced
- Engine state fields and updates in `Assets/Scripts/Validation/HeatupSimEngine*.cs`
- Engine trend history buffers via `AddHistory()` in `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:847`
- Interval/final textual outputs via `SaveIntervalLog()` and `SaveReport()` in `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:955` and `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1346`
- Alarm edge/event output via `LogEvent()` in `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:649`

## IP-0030 Required Monitoring Parameters (Section 5)
5.1 Global Simulation Health:
- Sim time (s/hr), sim rate, paused/running
- Fixed/variable timestep, current dt
- Integration stability flags (divergence, clamp hits, NaN/Inf)
- Mass conservation error (instant and cumulative)
- Energy conservation error (instant and cumulative)
- Total heat added, total heat removed, net heat

5.2 Reactor and Core:
- Reactor power (% and MWt)
- Decay heat (if modeled separately)
- Core inlet temp, core outlet temp
- Tcold, Thot, Tavg
- Core delta-T
- Core flow
- Core heat generation rate
- Reactivity total and breakdown (rod worth/position, boron worth, moderator feedback, Doppler feedback)

5.3 RCS (Primary Loops):
- RCS pressure
- RCS total mass/inventory
- Bulk average density
- Subcooling margin
- Per-loop A/B/C/D hot leg temp, cold leg temp, loop delta-T, loop mass flow, SG primary inlet/outlet temps
- RCP status, speed, torque/amps (if available), pump head
- Natural circulation estimate (RCPs off)
- Void fraction outside PZR

5.4 Pressurizer (PZR):
- PZR pressure
- PZR level, liquid mass, steam mass (or volumes)
- Liquid and steam temperatures
- Saturation temperature at pressure
- Subcooling/superheat in PZR regions
- Heater mode/state
- Heater demand and actual output
- Heater groups active
- Heater ramp rate and limiter flags
- Spray valve position, spray flow, spray inlet temperature
- Surge line flow (signed), temperature, delta-P
- Pressure error and level error
- Net energy into PZR

5.5 CVCS, VCT, and BRS:
- Charging pump status, total flow, to-RCS flow, charging temperature, charging pressure
- Letdown total flow, per-orifice states/flows, letdown temperature, letdown pressure
- Net CVCS flow and integrated mass effect
- VCT level, mass/volume, temperature, pressure, gas blanket pressure
- VCT inflow/outflow rates
- VCT heater/cooling status (if modeled)
- Charging suction margin / NPSH indicator (if modeled)
- BRS tank level, concentration, transfer/makeup status, BRS-to-VCT flow, BRS-to-RCS/CVCS flow, boron mass balance (if available)

5.6 RHR:
- Suction source, discharge destination, key isolation valve states
- Pump status, flow, line pressure/delta-P
- HX inlet/outlet temperatures
- Heat removed by RHR
- Interlocks and permissive states
- Minimum flow protection status

5.7 Steam Generators (Per SG/Loop):
- Secondary pressure
- SG level (narrow/wide where available)
- Feedwater flow
- Steam flow
- Steam dump/relief position or flow
- Secondary temperature (steam or saturation)
- Primary-to-secondary heat transfer
- Blowdown flow

5.8 Safety, Limits, and Alarms:
- High/low RCS pressure alarms
- High/low PZR level alarms
- Heater inhibited reasons
- Spray inhibited reasons
- RHR unavailable reasons
- Max dP/dt exceeded
- Max dT/dt exceeded
- Conservation error threshold exceeded

5.9 Always-On Trends:
- RCS pressure
- PZR pressure
- PZR level
- Heater demand vs actual output
- Spray flow
- Charging, letdown, net CVCS
- Tavg, Thot, Tcold, core delta-T
- SG pressure (per loop where available)
- Mass and energy conservation errors

## 5.1 Global Simulation Health
Produced:
- Sim time, wall time, sim rate, run state
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:149`, `Assets/Scripts/Validation/HeatupSimEngine.cs:165`, `Assets/Scripts/Validation/HeatupSimEngine.cs:938`, `Assets/Scripts/Validation/HeatupSimEngine.cs:972`, `Assets/Scripts/Validation/HeatupSimEngine.cs:981`, `Assets/Scripts/Validation/HeatupSimEngine.cs:983`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:964`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:965`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:966`
- Fixed timestep and current dt (deterministic)
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:678`, `Assets/Scripts/Validation/HeatupSimEngine.cs:948`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:971`
- Stability telemetry (partial)
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:2035`, `Assets/Scripts/Validation/HeatupSimEngine.cs:2040`, `Assets/Scripts/Validation/HeatupSimEngine.cs:265`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:1079`, `Assets/Scripts/Validation/HeatupSimEngine.cs:2217`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1052`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1055`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1195`
- Mass conservation error (instant and cumulative)
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:303`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:391`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:404`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:467`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1246`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1249`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1394`
- Energy conservation error (instant and cumulative)
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:2217`, `Assets/Scripts/Validation/HeatupSimEngine.cs:2249`, `Assets/Scripts/Validation/HeatupSimEngine.cs:2256`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1136`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1138`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1462`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1464`
- Total heat added/removed/net heat
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:2015`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1129`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1133`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1460`

## 5.2 Reactor and Core
Produced or derivable in HeatupSimEngine:
- Tcold, Thot, Tavg
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:173`, `Assets/Scripts/Validation/HeatupSimEngine.cs:174`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1936`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1944`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:975`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:976`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:977`
- Core delta-T (derived as `T_hot - T_cold`)
  - Producer inputs: `Assets/Scripts/Validation/HeatupSimEngine.cs:1936`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1937`
  - Output path: derived in consumers, no dedicated engine field
- Core flow (approximate from RCP contribution)
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1141` (RCP contribution update)
  - Output: per-pump flow fractions in `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1067`

Unavailable in HeatupSimEngine:
- Reactor power (%/MWt)
- Decay heat
- Core heat generation rate
- Reactivity total and reactivity breakdown (rods/boron/moderator/Doppler)

## 5.3 RCS (Primary Loops)
Produced:
- RCS pressure
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1431`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1534`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1679`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1861`, `Assets/Scripts/Validation/HeatupSimEngine.cs:2190`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:989`
- RCS total mass/inventory
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1954`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:296`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:467`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:415`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1211`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1411`
- Subcooling margin
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1952`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:982`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1235`
- RCP status (partial)
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1123`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1141`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1059`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1070`
- Natural-circulation indication (derived)
  - Producer inputs: `Assets/Scripts/Validation/HeatupSimEngine.cs:154`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:761`
  - Output: regime string in `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1061`

Unavailable or not modeled at required granularity:
- Bulk average density as a dedicated monitored output
- Per-loop A/B/C/D hot-leg/cold-leg temperatures
- Per-loop delta-T
- Per-loop mass flow
- SG primary inlet/outlet temperatures per loop
- RCP speed, torque/amps, pump head
- Void fraction outside PZR

## 5.4 Pressurizer (PZR)
Produced:
- PZR pressure, level, water/steam volumes
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1706`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1895`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:989`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:990`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:991`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:992`
- PZR water/steam mass (derived from volumes/density)
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1212`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1213`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1213`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1284`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1285`
- Saturation temperature and bulk subcooling
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1951`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1952`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:980`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:982`
- Heater mode/state, demand/output
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1184`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1217`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1220`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1230`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1013`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1016`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1023`
- Spray valve position and spray flow
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1271`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1272`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1273`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1026`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1027`
- Surge line flow (signed)
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1434`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1535`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1683`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1862`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:993`
- Pressure error and level error (partial)
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1451`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1018`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:363`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1018`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1043`
- PZR energy telemetry (partial)
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:263`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1056`

Unavailable or partial only:
- Separate liquid temperature vs steam temperature regions
- PZR liquid-region subcooling and steam-region superheat as separate monitored channels
- Heater groups active at full group-level detail
- Heater limiter flags as explicit monitored booleans
- Spray inlet temperature
- Surge line temperature
- Surge line delta-P
- Explicit net-energy-into-PZR channel (only aggregate enthalpy/state diagnostics are present)

## 5.5 CVCS, VCT, and BRS
Produced:
- Charging status, charging flow, charging-to-RCS flow
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:134`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:146`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:437`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:997`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:998`
- Letdown total flow and letdown path state
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:87`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:135`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:433`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1000`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1001`
- Per-orifice lineup state
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:371`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:379`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:413`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1004`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1005`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1006`
- Net CVCS flow and integrated mass effect
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:303`, `Assets/Scripts/Validation/HeatupSimEngine.cs:2522`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1007`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1246`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1394`
- VCT level/volume/boron/net flow and inflow-outflow cumulative checks
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:325`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:321`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1089`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1090`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1091`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1092`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1121`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1122`
- BRS level/chemistry/process/flows and boron tracking
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:333`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1100`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1103`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1104`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1108`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1109`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1116`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1117`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1118`

Unavailable or partial only:
- Charging temperature and charging pressure
- Letdown temperature and letdown pressure
- VCT temperature
- VCT pressure / gas blanket pressure
- VCT heater/cooling status
- Charging suction margin or NPSH indicator
- Explicit BRS-to-RCS channel (BRS return is tracked to VCT path)

## 5.6 RHR
Produced:
- RHR mode and active state
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1303`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1304`
- RHR net heat and HX removal
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1300`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1301`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1302`

Unavailable or partial only:
- Suction source and discharge destination as explicit aligned lineups
- Isolation valve states
- Pump flow, line pressure, line delta-P
- HX inlet/outlet temperatures by leg
- Interlocks/permissive states
- Minimum-flow protection status

## 5.7 Steam Generators (per SG/loop)
Produced (aggregate, not per-loop):
- Secondary pressure
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1370`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1723`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1877`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1164`
- SG level (wide and narrow)
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1382`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1383`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1889`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1890`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1205`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1206`
- Secondary temperature and saturation
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1361`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1371`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1160`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1165`
- Primary-to-secondary heat transfer
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.cs:1362`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1713`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1867`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1161`
- Steam production (cumulative mass, not flow)
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:1207`

Unavailable:
- Per-SG/per-loop isolation for the above channels
- Feedwater flow
- Steam flow rate to turbine
- Steam dump or relief position/flow per SG loop
- Blowdown flow

## 5.8 Safety, Limits, and Alarms
Produced:
- High/low RCS pressure alarms and high/low PZR level alarms
  - Producers: `Assets/Scripts/Physics/AlarmManager.cs:115`, `Assets/Scripts/Physics/AlarmManager.cs:120`, `Assets/Scripts/Physics/AlarmManager.cs:135`, `Assets/Scripts/Physics/AlarmManager.cs:138`
  - Engine mapping/output: `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs:262`, `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs:263`, `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs:266`, `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs:267`, `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs:95`, `Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs:113`
- Conservation threshold exceeded
  - Producers: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:404`
  - Output: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:410`

Unavailable or partial only:
- Heater inhibited reasons (explicit reason codes)
- Spray inhibited reasons (explicit reason codes)
- RHR unavailable reasons (explicit reason codes)
- Explicit max dP/dt exceeded flag (only validation check text)
- Explicit max dT/dt exceeded flag (only validation check text)

## 5.9 Always-On Trends (from engine perspective)
Produced histories:
- RCS pressure, PZR level, charging, letdown, surge, temperatures, subcooling
  - History declarations: `Assets/Scripts/Validation/HeatupSimEngine.cs:624`, `Assets/Scripts/Validation/HeatupSimEngine.cs:626`, `Assets/Scripts/Validation/HeatupSimEngine.cs:629`, `Assets/Scripts/Validation/HeatupSimEngine.cs:630`, `Assets/Scripts/Validation/HeatupSimEngine.cs:632`, `Assets/Scripts/Validation/HeatupSimEngine.cs:633`, `Assets/Scripts/Validation/HeatupSimEngine.cs:634`, `Assets/Scripts/Validation/HeatupSimEngine.cs:640`, `Assets/Scripts/Validation/HeatupSimEngine.cs:642`
  - Population: `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:850`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:852`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:855`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:856`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:858`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:859`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:860`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:866`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:868`
- Spray flow history exists
  - Declarations/population: `Assets/Scripts/Validation/HeatupSimEngine.cs:651`, `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs:877`

Missing required trend channels or incomplete trend coverage:
- Heater demand vs actual output trend pair is incomplete (PID output history exists, heater power history buffer does not)
- Net CVCS trend is not stored as a dedicated history channel
- SG pressure trend history is not stored as a dedicated history channel
- Mass conservation error trend and energy conservation error trend histories are not stored as dedicated channels
- Spray flow history is produced in engine but has no traced graph consumer in the current dashboard path

## Notes
- This repo state is pre-IP-0030 implementation (`git` head message shows "Pre IP0030").
- This report intentionally uses `HeatupSimEngine*` as the primary producer authority, with AlarmManager references where `HeatupSimEngine` delegates alarm setpoint evaluation.
