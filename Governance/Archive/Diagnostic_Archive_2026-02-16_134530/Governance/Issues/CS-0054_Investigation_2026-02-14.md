# CS-0054 Preliminary Investigation Report

- Issue ID: `CS-0054`
- Date: `2026-02-14`
- Domain: `DP-0003`
- Trigger Evidence: `Updates/Issues/IP-0018_StageE_Validation_2026-02-14_190400.md`
- Stage E Symptom: `stageE_DynamicPressureFlatline3Count = 5`

## Investigation Scope
Determine whether CS-0054 flatline failures are:
- real SG pressure-response physics defects, or
- measurement/windowing artifacts in Stage E acceptance evaluation.

## Captured Failing Windows (Required)
Extracted from instrumented Stage E evidence (`IP-0018_StageE_Validation_2026-02-14_190400.md`).

| Window (k..k+2) | Time (hr) | P[k], P[k+1], P[k+2] psia | dP step1 / step2 psia | dP total psia | PrimaryHeatInput_MW (k..k+2) | State |
|---|---|---|---|---:|---|---|
| 50..52 | 12.500, 12.750, 13.000 | 17.1359, 17.1538, 17.1758 | +0.0178 / +0.0221 | +0.0399 | 1.139, 1.383, 1.722 | OPEN_PREHEAT |
| 51..53 | 12.750, 13.000, 13.250 | 17.1538, 17.1758, 17.2029 | +0.0221 / +0.0271 | +0.0491 | 1.383, 1.722, 2.109 | OPEN_PREHEAT |
| 52..54 | 13.000, 13.250, 13.500 | 17.1758, 17.2029, 17.2361 | +0.0271 / +0.0332 | +0.0603 | 1.722, 2.109, 2.584 | OPEN_PREHEAT |
| 53..55 | 13.250, 13.500, 13.750 | 17.2029, 17.2361, 17.2766 | +0.0332 / +0.0405 | +0.0737 | 2.109, 2.584, 3.157 | OPEN_PREHEAT |
| 54..56 | 13.500, 13.750, 14.000 | 17.2361, 17.2766, 17.3255 | +0.0405 / +0.0489 | +0.0894 | 2.584, 3.157, 3.778 | OPEN_PREHEAT |

## Root-Cause Analysis (Preliminary)
### Finding 1: Not a rounding artifact
- Flatline evaluation uses full-precision sampled values (`Mathf.Abs(dpTotal) <= 0.1f`), not log-rounded `0.1 psia` text.
- Reference: `Assets/Scripts/UI/Editor/IP0018StageERunner.cs:311`, `Assets/Scripts/UI/Editor/IP0018StageERunner.cs:328`.
- Evidence windows show non-zero pressure increases each interval; totals are below threshold, not rounded-to-zero.

### Finding 2: Flatline windows occur exclusively in `OPEN_PREHEAT`
- All 5 failing windows are before pressurize/hold/isolated heatup and are tagged `OPEN_PREHEAT`.
- `OPEN_PREHEAT` deliberately keeps SG boundary open (`ShouldIsolateSGBoundary() -> false`).
- References: `Assets/Scripts/Validation/HeatupSimEngine.cs:2129`, `Assets/Scripts/Validation/HeatupSimEngine.cs:2131`.

### Finding 3: Stage E flatline counter currently has no startup-state filter
- Dynamic flatline counting runs on any 3 active-heating intervals and does not require `ISOLATED_HEATUP`.
- References: `Assets/Scripts/Validation/HeatupSimEngine.cs:2298`, `Assets/Scripts/Validation/HeatupSimEngine.cs:2351`, `Assets/Scripts/Validation/HeatupSimEngine.cs:2377`.

### Finding 4: CS-0009 energy telemetry now reuses `stageE_PrimaryHeatInput_MW` as SG heat
- Energy package sets `stageE_PrimaryHeatInput_MW = max(0, sgHeatTransfer_MW)` for CS-0009 accounting.
- This makes dynamic "active heat" gating sensitive to SG-side heat during `OPEN_PREHEAT`, which increases the number of windows considered active before pressurization.
- Reference: `Assets/Scripts/Validation/HeatupSimEngine.cs:1801`.

## Preliminary Conclusion
Most likely causal mechanism is **measurement/windowing scope mismatch**, not missing SG pressure physics coupling:
- Failures occur in `OPEN_PREHEAT`, where near-atmospheric slow pressure rise is expected by startup design.
- Pressure is increasing, but below the Stage E flatline band over 3 intervals.
- Dynamic flatline logic currently counts those startup windows anyway.

## Corrective Fix Options (Minimum 2)
1. **Option A (recommended, minimal): state-aware flatline counting**
- Change flatline counting to evaluate only windows in `ISOLATED_HEATUP` (or explicitly non-`OPEN_PREHEAT` startup states) while `PrimaryHeatInput_MW > 1.0`.
- Rationale: aligns measurement scope with true dynamic-coupling heatup segment.
- Risk: low-moderate. Must verify no blind spot for genuine post-pressurization flatlines.

2. **Option B: separate dynamic-primary metric from CS-0009 energy primary metric**
- Keep CS-0009 artifact fields unchanged, but introduce a dedicated dynamic gate signal (e.g., true primary heat source proxy) for CS-0020/CS-0054 checks.
- Rationale: avoids cross-coupling acceptance gates that use different physical meanings.
- Risk: moderate. Requires careful audit of Stage E reporting consistency and regression docs.

3. **Option C: physics-side pressure ramp tuning in `OPEN_PREHEAT`**
- Increase early preheat pressure growth to exceed `0.1 psia` over 3 intervals.
- Rationale: forces current metric to pass without changing window scope.
- Risk: high. Alters startup thermodynamics and can regress CS-0017/CS-0019 behavior.

## Selected Fix Direction
- Proceed with **Option A** first (smallest reversible fix, lowest regression risk), then rerun Stage A-E.
