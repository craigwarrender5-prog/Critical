---
Identifier: DP-0008
Domain (Canonical): Operator Interface & Scenarios
Status: Open
Linked Issues: CS-0102, CS-0103, CS-0108
Last Reviewed: 2026-02-17
Authorization Status: PENDING (IP-0050 awaiting approval)
Mode: ACTIVE
---

# DP-0008 - Operator Interface & Scenarios

## A) Domain Summary
- Canonical Domain: Operator Interface & Scenarios
- DP Status: Open
- Total CS Count in Domain: 3
- Active Implementation Plan: IP-0050

## B) Severity Distribution
| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 1 |
| Medium | 2 |
| Low | 0 |

## C) Ordered Issue Backlog
| CS ID | Title | Severity | Status | Blocking Dependency | Validation Outcome |
|---|---|---|---|---|---|
| CS-0102 | Establish scenario system framework with registry and scenario abstraction | High | READY | - | Pending |
| CS-0103 | Add in-simulator scenario selection overlay with keybind trigger | Medium | READY | CS-0102 | Pending |
| CS-0108 | PZR temperature monitoring missing for bubble formation readiness during cold startup | Medium | READY | - | Pending |

## D) Execution Readiness Indicator
**AUTHORIZED FOR EXECUTION (IP-0050)**

IP-0050 addresses CS-0108 (PZR temperature monitoring).
CS-0102 and CS-0103 (scenario system) are pending separate IP creation.

## E) Notes / Investigation Links
- Registry consistency synchronized on 2026-02-17.
- CS-0037 (Surge line flow direction) was closed as INVALID on 2026-02-15.
- CS-0077 (HeatupValidationVisual redesign) was closed as FAILED on 2026-02-15 (replaced by IP-0043 ValidationDashboard rebuild).
- CS-0042 was withdrawn on 2026-02-15 (resolution: INVALID).
- IP-0050 created 2026-02-17 for CS-0108 (PZR temperature monitoring).
- Active execution plan: `Governance/ImplementationPlans/IP-0050/IP-0050.md`
