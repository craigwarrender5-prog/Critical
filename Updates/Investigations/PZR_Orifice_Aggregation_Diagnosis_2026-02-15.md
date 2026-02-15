# PZR Orifice Aggregation Diagnosis (2026-02-15)

## Scope
- Objective: determine whether PZR/CVCS letdown orifices are being treated as aggregated (3-as-1), multiplied by count incorrectly, opened simultaneously, or double-applied.
- Constraint: diagnostic only (no functional fix in this report).

## Run Stamp and Reproduction
- Investigation run stamp: `HeatupLogs/PZR_INVEST_20260215_174056`
- Runner/method: `Critical.Validation.PzrBubbleInvestigationRunner.RunAll` (`Assets/Scripts/UI/Editor/PzrBubbleInvestigationRunner.cs:55`, `Assets/Scripts/UI/Editor/PzrBubbleInvestigationRunner.cs:56`)
- Configured bracket runs:
  - `BASELINE` (`dt=1/360 hr`)
  - `SMALLER_TIMESTEP` (`dt=1/720 hr`)
  - `NO_AMBIENT_CLAMP` (`DisableAmbientPressureFloor=true`)
  (`Assets/Scripts/UI/Editor/PzrBubbleInvestigationRunner.cs:63`, `Assets/Scripts/UI/Editor/PzrBubbleInvestigationRunner.cs:83`)
- Orifice diagnostics were explicitly enabled and sampled every 25 ticks (`Assets/Scripts/UI/Editor/PzrBubbleInvestigationRunner.cs:135`, `Assets/Scripts/UI/Editor/PzrBubbleInvestigationRunner.cs:136`).

## Call Chain (Demand -> Selection -> Total Flow -> Mass Application)

### A) Non-DRAIN two-phase path
1. Orifice lineup/state selection:
   - `UpdateOrificeLineup()` adjusts `orifice75Count`/`orifice45Open` from level-error thresholds (`Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:355`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:371`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:379`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:395`, `Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:404`)
2. Total letdown flow calculation:
   - Engine path calls `PlantConstants.CalculateTotalLetdownFlow(..., num75Open: orifice75Count, open45: orifice45Open)` (`Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs:87`)
   - Controller path also calls same API (`Assets/Scripts/Physics/CVCSController.cs:344`)
   - `CalculateTotalLetdownFlow` uses lineup model when `num75Open >= 0` (`Assets/Scripts/Physics/PlantConstants.CVCS.cs:561`)
   - Lineup model sums open 75-gpm and optional 45-gpm contributions, capped by ion-exchanger limit (`Assets/Scripts/Physics/PlantConstants.CVCS.cs:522`, `Assets/Scripts/Physics/PlantConstants.CVCS.cs:528`, `Assets/Scripts/Physics/PlantConstants.CVCS.cs:529`, `Assets/Scripts/Physics/PlantConstants.CVCS.cs:533`)
3. Application to mass/ledger:
   - Primary boundary event computed/applied in `ApplyPrimaryBoundaryFlowPBOC` (`Assets/Scripts/Validation/HeatupSimEngine.cs:2424`)
   - Mass flow terms are derived from `chargingFlow` and `letdownFlow` (`Assets/Scripts/Validation/HeatupSimEngine.cs:2353`, `Assets/Scripts/Validation/HeatupSimEngine.cs:2354`)

### B) DRAIN path (bubble formation)
1. Demand/setpoint path:
   - `UpdateDrainPhase()` calls `ResolveDrainCvcsPolicy(...)` (`Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:366`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:417`)
   - `ResolveDrainCvcsPolicy` directly sets `letdown_gpm = Lerp(75..120)` from level fraction (`Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:605`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:607`)
2. Orifice state derivation in DRAIN:
   - If `letdown_gpm >= 100`: force `2x75 + 1x45`
   - Else if `>= 85`: force `1x75 + 1x45`
   - Else: `1x75`
   (`Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:610`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:616`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:623`)
3. Flow applied to inventory:
   - `dm_cvcsDrain_lbm = netOutflow_gpm * ...` and applied as PZR water removal / RCS addition (`Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:423`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:446`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:447`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:448`)
   - Then standard PBOC boundary application still runs each tick (`Assets/Scripts/Validation/HeatupSimEngine.cs:2424`)

## Anti-Pattern Scan Results

### 1) Multiplication by orifice count
- Match found in lineup model:
  - `flow75 = num75Open * ORIFICE_FLOW_COEFF_75 * sqrtDP` (`Assets/Scripts/Physics/PlantConstants.CVCS.cs:528`)
- Assessment:
  - This is expected for two identical 75-gpm parallel orifices when `num75Open` is truly open count.
  - Not by itself evidence of bug.

### 2) Sum of all capacities regardless of open/closed
- No direct pattern found of unconditional `75 + 75 + 45` active flow usage.
- Active lineup model conditionally includes 45-gpm term via `open45` and 75-gpm term via `num75Open` (`Assets/Scripts/Physics/PlantConstants.CVCS.cs:528`, `Assets/Scripts/Physics/PlantConstants.CVCS.cs:529`).

### 3) "Enable letdown" implies "open all"
- No literal `if (letdownEnabled) open all` found.
- Functional equivalent found in DRAIN policy:
  - Entering DRAIN at high level sets `letdown_gpm` near max envelope, which immediately forces all three open (`Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:607`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:610`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:612`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:613`).

### 4) Available vs open confusion
- No direct misuse found of available-count constants in active flow application.
- Available constants exist (`LETDOWN_ORIFICE_75GPM_COUNT`, `LETDOWN_ORIFICE_45GPM_COUNT`) but are not used as active open-state substitutes in the investigated path (`Assets/Scripts/Physics/PlantConstants.CVCS.cs:410`, `Assets/Scripts/Physics/PlantConstants.CVCS.cs:416`).

### 5) Default init sets openCount = 3
- Not found.
- Default lineup is one 75-gpm open (`Assets/Scripts/Validation/HeatupSimEngine.cs:674`, `Assets/Scripts/Validation/HeatupSimEngine.cs:675`).

### 6) Double-application (per-orifice + aggregate)
- No direct per-orifice-plus-aggregate duplicate removal found.
- DRAIN does use a two-step pattern:
  - internal PZR->RCS transfer by `netOutflow_gpm` (`Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:423`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:446`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:448`)
  - plus global PBOC boundary application (`Assets/Scripts/Validation/HeatupSimEngine.cs:2424`)
- This is an architecture choice; not a direct "orifice counted twice" pattern.

## Instrumentation Evidence Around Cliff Event

### Before cliff (pre-DRAIN, baseline interval log)
- `Sim Time: 8.25h`, `Bubble Phase: VERIFICATION`, `PZR Level: 100.0%`, `Letdown Flow: 79.7 gpm`, lineup `1x75` (`HeatupLogs/PZR_INVEST_20260215_174056/BASELINE_20260215_174056/Heatup_Interval_034_8.25hr.txt:5`, `HeatupLogs/PZR_INVEST_20260215_174056/BASELINE_20260215_174056/Heatup_Interval_034_8.25hr.txt:31`, `HeatupLogs/PZR_INVEST_20260215_174056/BASELINE_20260215_174056/Heatup_Interval_034_8.25hr.txt:41`, `HeatupLogs/PZR_INVEST_20260215_174056/BASELINE_20260215_174056/Heatup_Interval_034_8.25hr.txt:44`, `HeatupLogs/PZR_INVEST_20260215_174056/BASELINE_20260215_174056/Heatup_Interval_034_8.25hr.txt:65`)

### Cliff begins (first DRAIN step)
- First DRAIN orifice diagnostic line:
  - `sim_hr=8.2664`, `open_orifice_count=3`, `letdown_total_gpm=120.000`, `charging_gpm=0.000`, `pzr_level_before_pct=100.000`, `pzr_level_after_pct=98.736`, `reason=ORIFICE_CHANGE`
  (`HeatupLogs/PZR_INVEST_20260215_174056/BASELINE_20260215_174056/PZR_Orifice_Diagnostics_BASELINE.log:1`)

### Immediately after
- Still 3 open at `sim_hr=8.3331`, with `letdown_total_gpm=105.417`, and level already down to ~75% (`HeatupLogs/PZR_INVEST_20260215_174056/BASELINE_20260215_174056/PZR_Orifice_Diagnostics_BASELINE.log:2`)
- First reduction to 2 open at `sim_hr=8.3720` (`HeatupLogs/PZR_INVEST_20260215_174056/BASELINE_20260215_174056/PZR_Orifice_Diagnostics_BASELINE.log:3`)

### Bracket correlation
- Smaller timestep reproduces same initial 3-open behavior at DRAIN entry (`open_orifice_count=3`, `letdown_total_gpm=120`) (`HeatupLogs/PZR_INVEST_20260215_174056/SMALLER_TIMESTEP_20260215_174059/PZR_Orifice_Diagnostics_SMALLER_TIMESTEP.log:1`)
- Smaller timestep additionally shows first samples where computed per-orifice hydraulic contributions are `0.000` while commanded letdown remains `120.000` (pressure-dependent flow not governing commanded DRAIN letdown at that instant) (`HeatupLogs/PZR_INVEST_20260215_174056/SMALLER_TIMESTEP_20260215_174059/PZR_Orifice_Diagnostics_SMALLER_TIMESTEP.log:1`)
- No-ambient-clamp run matches baseline DRAIN lineup behavior (`HeatupLogs/PZR_INVEST_20260215_174056/NO_AMBIENT_CLAMP_20260215_174102/PZR_Orifice_Diagnostics_NO_AMBIENT_CLAMP.log:1`)

## Requested Checks (Explicit)
- Timestep where level cliff begins: `sim_hr=8.2664` (first DRAIN step) (`HeatupLogs/PZR_INVEST_20260215_174056/BASELINE_20260215_174056/PZR_Orifice_Diagnostics_BASELINE.log:1`)
- Did `open_orifice_count` jump 1 -> 3: **Yes**, inferred from pre-DRAIN interval lineup (`1x75`) to first DRAIN diagnostics (`3 open`) (`HeatupLogs/PZR_INVEST_20260215_174056/BASELINE_20260215_174056/Heatup_Interval_034_8.25hr.txt:44`, `HeatupLogs/PZR_INVEST_20260215_174056/BASELINE_20260215_174056/PZR_Orifice_Diagnostics_BASELINE.log:1`)
- Did `letdown_total_gpm` jump by ~+120 or ~+195 in one step: **No**.
  - Observed transition: ~`79.7` -> `120.0` gpm (about `+40.3` gpm) at DRAIN entry (`HeatupLogs/PZR_INVEST_20260215_174056/BASELINE_20260215_174056/Heatup_Interval_034_8.25hr.txt:41`, `HeatupLogs/PZR_INVEST_20260215_174056/BASELINE_20260215_174056/PZR_Orifice_Diagnostics_BASELINE.log:1`)
  - No `195 gpm` evidence found in diagnostics.

## Conclusion
**CONFIRMED: Code treats the three orifices as a single commanded letdown path during DRAIN, with lineup states derived from that commanded total (not vice versa).**

Exact mechanism:
- DRAIN path computes a direct aggregate letdown command from level (`75..120 gpm`) (`Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:607`).
- It then forces orifice lineup bands from that command (`Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:610`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:616`, `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:623`).
- The applied mass removal uses this aggregate `netOutflow_gpm` directly (`Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:423`), rather than deriving total flow from per-orifice hydraulic state in DRAIN.

Implication:
- The observed event is not primarily "sum all capacities regardless of open flags" in the non-DRAIN lineup solver.
- It is a DRAIN-state aggregate-flow control behavior that can present as 3-as-1, including an immediate 1->3 lineup shift at DRAIN entry.
