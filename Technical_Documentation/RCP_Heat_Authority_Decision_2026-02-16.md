# RCP Heat Authority Decision (2026-02-16)

## Purpose
Resolve Baseline A numeric authority conflict for reactor coolant pump (RCP) heat input (`CS-0083`).

## Conflicting Baseline Inputs
1. `Technical_Documentation/NRC_HRTD_Section_3.2_Reactor_Coolant_System.md` and `Technical_Documentation/NRC_REFERENCE_SOURCES.md` cite approximately `6 MW` per RCP in cold water.
2. `Technical_Documentation/RHR_SYSTEM_RESEARCH_v3.0.0.md` includes `5.25 MW` per RCP (`21 MW` total for 4 RCPs) as a historical model value.

## Authority Precedence Decision
1. **Primary normative authority for cold-water startup RCP heat**: `~6 MW per RCP` (`~24 MW total` with 4 RCPs) from the NRC RCS reference set.
2. `RHR_SYSTEM_RESEARCH_v3.0.0.md` values are treated as **historical implementation assumptions**, not normative baseline authority.

## Implementation Scope Impact
1. This decision resolves documentation precedence ambiguity (`CS-0083`).
2. Runtime constant alignment work remains explicitly tracked in `CS-0080` under `IP-0036`.
3. Until `IP-0036` completion, runtime values may remain temporarily divergent from this authority decision; that divergence is tracked and intentional.

## Traceability
1. Audit finding: `Technical_Documentation/Conformance_Audit_Report_2026-02-15.md` (F-005).
2. Governing IP: `Governance/ImplementationPlans/IP-0032/IP-0032.md`.
