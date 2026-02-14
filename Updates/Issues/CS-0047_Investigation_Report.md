---
Issue: CS-0047
Title: Heat-up Progression Stalls During Intended Startup Heat Addition
Severity: Critical
Status: Preliminary Investigation Complete - Awaiting Authorization
Date: 2026-02-14
Mode: SPEC/DRAFT
---

# CS-0047 Preliminary Investigation Report

## 1. Registered Observation
- Objective behavior:
  - RCS heat-up progression stalls despite active heat sources.
  - Net plant heat addition trends to zero or negative in intended heat-up phase.
  - Startup progression is blocked.

## 2. Governing Checks
- Physical law: first-law plant energy balance (`Q_net = Q_sources - Q_sinks`).
- Conservation rule: sustained heat-up requires positive net primary-side energy.
- Control logic: startup heat-up sequence expects monotonic positive temperature trend while heat sources are active.

## 3. Expected vs Simulated
- Expected behavior:
  - Positive net heat through startup heat-up.
  - Rising RCS temperature trend until downstream hold limits are reached.
- Simulated behavior (evidence):
  - SG side absorbs ~27 MW while available source heat is ~22.8 MW; RCS trend becomes negative (`-177 F/hr`).
  - Evidence: `Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md:13`.
  - Supporting note: runaway SG sink and net cooldown are also documented in `Updates/Archive/IMPLEMENTATION_PLAN_v5.1.0.md:37-38`.

## 4. Boundary and Control State Comparison
- Boundary/control states observed:
  - Heat source terms remain active.
  - SG secondary-side sink term dominates net balance.
- Code-path corroboration (non-destructive inspection):
  - Net heat path explicitly subtracts SG removal from gross heat in `Assets/Scripts/Physics/RCSHeatup.cs:131-136`.

## 5. Minimal Non-Destructive Probes Performed
- Probe A: reviewed validated evidence summary for heat-rate collapse (`SG_Secondary_Pressurization_During_Heatup_Research.md`).
- Probe B: verified governing net-heat computation path (`RCSHeatup.cs`).
- Probe C: compared expected startup heat-up intent against observed negative heat-rate evidence.

## 6. Domain Assignment
Evidence indicates origin within Steam Generator Secondary Physics.

## 7. Severity Assignment
- Severity: Critical.
- Evidence basis:
  - The observed behavior blocks startup/heat-up progression.
  - Net heat trending negative during active heat addition is incompatible with intended startup trajectory.

## 8. Constraints
- No fix design performed.
- No code modifications performed.
