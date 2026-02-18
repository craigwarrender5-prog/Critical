# CS-0122 Investigation Report (2026-02-18_131500)

**Title:** RCS temperature incorrectly rising during solid PZR heatup with no RCP flow  
**Severity:** HIGH  
**Domain:** Primary Thermodynamics  
**Status:** READY  
**Created:** 2026-02-18T11:30:00Z  
**Updated:** 2026-02-18T13:15:00Z  
**Assigned DP:** DP-0001

---

## 1. Problem Summary

During Mode 5 solid-PZR heatup with `rcpCount == 0`, RCS bulk temperature rises from PZR heater activity. This is materially above expected no-forced-flow coupling behavior and distorts startup thermal progression.

---

## 2. Code Trace Findings

### 2.1 No-RCP transport floor guarantees non-zero PZR->RCS coupling

In engine constants, no-RCP natural transport floor is hard-coded:
- `Assets/Scripts/Validation/HeatupSimEngine.cs:853-854`
  - `NO_RCP_TRANSPORT_GAIN = 18f`
  - `NO_RCP_NATURAL_FLOOR = 0.08f`

Transport-factor computation enforces a minimum of `0.08` whenever RCPs are off:
- `Assets/Scripts/Validation/HeatupSimEngine.cs:2866-2881`

### 2.2 Solid regime passes transport factor directly into solid-plant model

Regime-1 solid path computes and forwards this factor to solid-plant pressure update:
- `Assets/Scripts/Validation/HeatupSimEngine.cs:1705-1724`

### 2.3 Solid-plant model applies surge-line conduction to RCS temperature every step

Solid model computes surge-line heat transfer scaled by transport factor:
- `Assets/Scripts/Physics/SolidPlantPressure.cs:458-460`

Then applies that heat into RCS bulk temperature update:
- `Assets/Scripts/Physics/SolidPlantPressure.cs:500-503`

Net effect: with heaters on and `T_pzr > T_rcs`, non-zero conducted heat is forced into RCS even without RCP flow due to the hard floor.

---

## 3. Root Cause

No-RCP thermal coupling is intentionally forced by a fixed minimum transport factor (`0.08`), and that factor is directly used in solid-plant surge-line conduction into RCS bulk temperature. This over-couples PZR heater energy into the RCS during no-forced-flow conditions.

---

## 4. Secondary Consequences

1. Artificial RCS warming increases thermal expansion signal in solid-plant pressure control path.
2. Pressure-rate limiting side effects can clamp heater output earlier than expected.
3. Startup heatup timing and pressure progression evidence become less trustworthy for adjacent DP-0012 acceptance work.

---

## 5. Disposition

**Disposition: READY (full investigation complete).**

The issue has a clear, code-verified mechanism and should be remediated via DP-0001 implementation work before downstream startup-policy validation is finalized.

---

## 6. Proposed Resolution Direction

1. Rework no-RCP transport-factor policy to avoid unconditional minimum coupling.
2. Gate PZR->RCS bulk transfer by physically justified circulation criteria (not fixed floor alone).
3. Recalibrate no-RCP surge-line coupling envelope against expected Mode 5 behavior.
4. Revalidate thermal trajectories with RHR and no-RCP conditions after fix.

---

## 7. Acceptance Criteria

1. With `RCPs OFF`, PZR heater activity does not drive bulk RCS temperature rise beyond defined no-flow coupling envelope.
2. With `RCPs ON`, forced-flow coupling still produces expected bulk temperature response.
3. RHR/no-RCP behavior remains stable and regression-free.
4. Updated evidence confirms improved fidelity of pre-RCP thermal progression.

---

## 8. Affected Files

- `Assets/Scripts/Validation/HeatupSimEngine.cs`
- `Assets/Scripts/Physics/SolidPlantPressure.cs`
- `Assets/Scripts/Physics/HeatTransfer.cs`
- `Assets/Scripts/Physics/LoopThermodynamics.cs`

---

## 9. Tags

- `Primary-Thermodynamics`
- `PZR-RCS-Coupling`
- `Thermal-Isolation`
- `No-RCP-Flow`
- `Physics-Violation`
- `High-Priority`
- `User-Request-2026-02-18`
