---
Identifier: DP-0001
Domain (Canonical): Primary Thermodynamics
Status: Open
Linked Issues: CS-0021, CS-0033, CS-0038, CS-0022, CS-0023, CS-0031, CS-0034
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# DP-0001 - Primary Thermodynamics

## A) Domain Summary
- Canonical Domain: Primary Thermodynamics
- DP Status: Open
- Total CS Count in Domain: 7

## B) Severity Distribution
| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 3 |
| Medium | 4 |
| Low | 0 |

## C) Ordered Issue Backlog
| CS ID | Title | Severity | Status | Blocking Dependency | Validation Outcome |
|---|---|---|---|---|---|
| CS-0021 | Solid-regime pressure decoupled from mass change | High | Assigned | - | Resolved v0.2.0.0 â€” CVCS transport delay (60s ring buffer, read-before-write) prevents same-step cancellation. PressureRate > 0 for 100% of HEATER_PRESSURIZE steps. HOLD_SOLID oscillation 11-14 psi P-P (naturally damping). Mass conservation 0.02 lbm at 5 hr. |
| CS-0033 | RCS bulk temperature rise with RCPs OFF and no confirmed forced flow | High | Assigned | - | Pass â€” Finding A resolved v0.3.1.1 (flow-coupled pump heat). Finding B deferred to CS-0034. Finding C deferred to CS-0035. |
| CS-0038 | PZR level spike on RCP start (single-frame transient) | High | Assigned | - | Not Tested |
| CS-0022 | Early pressurization response mismatched with controller actuation | Medium | Assigned | - | Resolved v0.2.0.0 â€” Anti-windup inhibits integral accumulation when actuator saturated (HEATER_PRESSURIZE clamp, slew limiter) or dead-time gap > 0.5 gpm. Transport delay ensures +/-1 gpm bias arrives 60s late, producing modest contribution rather than same-step amplification. Pressurization rate now thermal-expansion-dominated. |
| CS-0023 | Surge flow trend rises while pressure remains flat | Medium | Assigned | - | Resolved v0.2.0.0 â€” No code change to surge flow required (calculation was correct). SurgePressureConsistent diagnostic added: 100% consistency in both HEATER_PRESSURIZE (260/260 steps) and HOLD_SOLID (2712/2712 steps). Resolved naturally by CS-0021 transport delay fix. |
| CS-0031 | RCS heat rate escalation after RCP start may be numerically aggressive | Medium | Assigned | - | Not Tested |
| CS-0034 | No equilibrium ceiling for RCS temperature in Regime 0/1 (pre-RCP) | Medium | Assigned | - | Not Tested |

## D) Execution Readiness Indicator
**READY FOR AUTHORIZATION**

No blocking Critical issues unresolved outside domain.

## E) Notes / Investigation Links
- Prior IP references:
  - IP-0002 â€” Primary Solid Regime â€” Phase A
  - IP-0002 â€” Primary Solid Regime â€” Phase C (verification only)
  - IP-0004 â€” RCS Energy Balance and Regime Transition
  - IP-0005 â€” RCP Thermal Inertia
- Validation evidence references:
  - CS-0021: Resolved v0.2.0.0 â€” CVCS transport delay (60s ring buffer, read-before-write) prevents same-step cancellation. PressureRate > 0 for 100% of HEATER_PRESSURIZE steps. HOLD_SOLID oscillation 11-14 psi P-P (naturally damping). Mass conservation 0.02 lbm at 5 hr.
  - CS-0022: Resolved v0.2.0.0 â€” Anti-windup inhibits integral accumulation when actuator saturated (HEATER_PRESSURIZE clamp, slew limiter) or dead-time gap > 0.5 gpm. Transport delay ensures +/-1 gpm bias arrives 60s late, producing modest contribution rather than same-step amplification. Pressurization rate now thermal-expansion-dominated.
  - CS-0023: Resolved v0.2.0.0 â€” No code change to surge flow required (calculation was correct). SurgePressureConsistent diagnostic added: 100% consistency in both HEATER_PRESSURIZE (260/260 steps) and HOLD_SOLID (2712/2712 steps). Resolved naturally by CS-0021 transport delay fix.
  - CS-0033: Pass â€” Finding A resolved v0.3.1.1 (flow-coupled pump heat). Finding B deferred to CS-0034. Finding C deferred to CS-0035.
