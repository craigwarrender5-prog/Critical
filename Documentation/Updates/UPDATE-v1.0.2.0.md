# UPDATE v1.0.2.0 — Fix PZR Level Alarms During Solid Plant Operations

**Date:** 2026-02-06
**Version:** 1.0.2.0
**Type:** Bug Fix
**Backwards Compatible:** Yes (new field has default value `false` which preserves existing behavior)

---

## Summary

Fixed false PZR LEVEL HIGH alarm firing continuously during solid pressurizer operations (Mode 5 cold shutdown, pre-bubble formation). The alarm correctly fires during two-phase operations but was not suppressed during solid plant mode where 100% PZR level is the intended operating condition.

---

## Root Cause

`AlarmManager.CheckAlarms()` evaluated PZR level alarms unconditionally against the 85% high / 20% low setpoints. During solid plant operations per NRC HRTD 19.2.1, the pressurizer is intentionally water-solid (100% water, 0% steam) — pressure is controlled by CVCS charging/letdown balance rather than steam bubble compression. The AlarmInputs struct lacked a `SolidPressurizer` flag, so the alarm module had no way to distinguish solid ops from two-phase ops.

## Real Plant Reference

Per NRC HRTD 19.2.1: During solid plant operations, PZR level alarms are blocked or have setpoints adjusted above 100% by the plant control system. Level alarms only become active after steam bubble formation when the plant transitions to two-phase pressure control.

---

## Files Modified

| File | Change |
|------|--------|
| `Assets/Scripts/Physics/AlarmManager.cs` | Added `SolidPressurizer` field to `AlarmInputs` struct; suppress PZR Level Low and PZR Level High alarms when `SolidPressurizer == true` |
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | Pass `solidPressurizer` state to `AlarmInputs` when calling `AlarmManager.CheckAlarms()` |

---

## Detailed Changes

### AlarmManager.cs
- **AlarmInputs struct**: Added `public bool SolidPressurizer` field — true during solid plant ops (PZR 100% water, no bubble)
- **PZR Level Low**: Now `!inputs.SolidPressurizer && (inputs.PZRLevel < 20%)` — suppressed during solid ops where level is always 100%
- **PZR Level High**: Now `!inputs.SolidPressurizer && (inputs.PZRLevel > 85%)` — suppressed during solid ops where 100% is the correct operating level

### HeatupSimEngine.cs
- **UpdateAnnunciators()**: Added `SolidPressurizer = solidPressurizer` to the `AlarmInputs` struct construction passed to `AlarmManager.CheckAlarms()`

---

## Validation

- All existing alarm setpoints unchanged for two-phase operations
- New `SolidPressurizer` field defaults to `false`, preserving backward compatibility for any callers that don't set it
- No physics module changes — all GOLD STANDARD modules unaffected
- Alarm suppression automatically lifts when `solidPressurizer` transitions to `false` at bubble formation

---

## Physics Verification (from heatup logs)

All 14 interval logs (T+0.5hr through T+7.0hr) confirmed:
- Simulation physics tracking correctly throughout
- T_rcs rising ~5°F/hr (heater conduction only, no RCPs)
- T_pzr rising ~5-40°F/hr (decreasing as delta-T with RCS equilibrates)
- Pressure held at 365 psia by solid plant controller (in-band)
- VCT level stable/rising (normal CVCS operation)
- Mass conservation error: 0.00–0.01 gal
- All validation checks: PASS
