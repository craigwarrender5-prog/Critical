---
Issue: CS-0048
Title: Steam Generator Secondary Stays Near Atmospheric and Behaves as Constant Heat Sink
Severity: Critical
Status: Preliminary Investigation Complete - Awaiting Authorization
Date: 2026-02-14
Mode: SPEC/DRAFT
---

# CS-0048 Preliminary Investigation Report

## 1. Registered Observation
- Objective behavior:
  - SG secondary pressure remains near atmospheric while heat is added.
  - SG boiling removes essentially all primary heat input.
  - SG pressure/temperature rise required for startup is prevented.

## 2. Governing Checks
- Physical law: boiling system pressure-temperature coupling along saturation relation.
- Conservation rule: SG heat sink should remain bounded by source heat and state feedback.
- Control logic: startup sequence requires SG secondary pressure and saturation temperature progression.

## 3. Expected vs Simulated
- Expected behavior:
  - As steam forms, SG pressure rises and saturation temperature increases.
  - Heat removal self-regulates as pressure and Tsat increase.
- Simulated behavior (evidence):
  - SG secondary remains pinned near ~17 psia (atmospheric/N2 blanket class) after boiling onset.
  - Boiling heat removal reaches ~27 MW vs ~22.8 MW available source heat, driving primary cooldown.
  - Evidence: `Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md:13`.
  - Additional SG state example: `HeatupLogs/Heatup_Interval_001_0.00hr.txt:155-160` shows low-pressure/N2 OPEN-state reporting.

## 4. Boundary and Control State Comparison
- Boundary/control states observed:
  - Pressure floor behavior persists around initial SG pressure region.
  - OPEN-state boiling sink behavior remains active while startup heat is applied.
- Code-path corroboration (non-destructive inspection):
  - Initial and minimum SG pressure constraints in `Assets/Scripts/Physics/PlantConstants.SG.cs:663` and `Assets/Scripts/Physics/PlantConstants.SG.cs:790`.
  - Pressure-branch behavior in `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1900-1938`.

## 5. Minimal Non-Destructive Probes Performed
- Probe A: reviewed SG pressure/heat evidence summary document.
- Probe B: reviewed interval-log SG secondary state lines.
- Probe C: inspected SG pressure-floor and pressure-update code path only (read-only).

## 6. Domain Assignment
Evidence indicates origin within Steam Generator Secondary Physics.

## 7. Severity Assignment
- Severity: Critical.
- Evidence basis:
  - Physically inconsistent SG pressure/boiling response creates a dominant sink and blocks startup heat-up progression.

## 8. Constraints
- No fix design performed.
- No code modifications performed.
