---
Issue: CS-0049
Title: Pressurizer Does Not Recover Pressure in Two-Phase Condition Under Heater Pressurize Mode
Severity: Critical
Status: Preliminary Investigation Complete - Awaiting Authorization
Date: 2026-02-14
Mode: SPEC/DRAFT
---

# CS-0049 Preliminary Investigation Report

## 1. Registered Observation
- Objective behavior:
  - Pressurizer remains on saturation-temperature behavior in two-phase operation.
  - Pressure does not recover under heater pressurize mode.
  - Pressure may continue decreasing rather than stabilizing/rising.

## 2. Governing Checks
- Physical law: two-phase pressurizer energy addition should support stable or rising pressure under active heaters.
- Conservation rule: pressure trend must be thermodynamically coherent with heater-driven phase behavior.
- Control logic: pressurize mode intent requires non-decreasing pressure response in this phase window.

## 3. Expected vs Simulated
- Expected behavior:
  - During two-phase pressurize operation, pressure stabilizes or rises with heater input.
- Simulated behavior (evidence):
  - DRAIN-phase pressure collapses from ~368 psia toward ~154 psia despite 1.8 MW heaters.
  - Two-phase table confirms continued decline across intervals with BUBBLE_AUTO active.
  - Evidence: `Updates/Issues/CS-0043_Investigation_Report.md:18` and `Updates/Issues/CS-0043_Investigation_Report.md:107-112`.

## 4. Boundary and Control State Comparison
- Boundary/control states observed:
  - Heaters are active in bubble-formation control mode.
  - Pressure response remains negative through monitored two-phase intervals.
- Code-path corroboration (non-destructive inspection):
  - Two-phase steam-generation path in `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs:339-353`.
  - Two-phase temperature/pressure handling branch in `Assets/Scripts/Physics/RCSHeatup.cs:337-341`.

## 5. Minimal Non-Destructive Probes Performed
- Probe A: reviewed validated two-phase interval evidence from CS-0043 investigation.
- Probe B: inspected two-phase pressure/energy control path in read-only mode.
- Probe C: compared expected pressurize-mode pressure direction to observed pressure trajectory.

## 6. Domain Assignment
Evidence indicates origin within Pressurizer & Two-Phase Physics.

## 7. Severity Assignment
- Severity: Critical.
- Evidence basis:
  - Behavior is physically inconsistent for pressurize mode and directly blocks startup progression.

## 8. Constraints
- No fix design performed.
- No code modifications performed.
