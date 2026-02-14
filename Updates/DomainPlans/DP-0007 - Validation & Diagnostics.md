---
Identifier: DP-0007
Domain (Canonical): Validation & Diagnostics
Status: Open
Linked Issues: CS-0006, CS-0011, CS-0040, CS-0007, CS-0041, CS-0012
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# DP-0007 - Validation & Diagnostics

## A) Domain Summary
- Canonical Domain: Validation & Diagnostics
- DP Status: Open
- Total CS Count in Domain: 6

## B) Severity Distribution
| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 3 |
| Medium | 2 |
| Low | 1 |

## C) Ordered Issue Backlog
| CS ID | Title | Severity | Status | Blocking Dependency | Validation Outcome |
|---|---|---|---|---|---|
| CS-0006 | UpdatePrimaryMassLedgerDiagnostics() never called (dead code) | High | Assigned | Blocked by: CS-0001, CS-0003. Blocks: CS-0007. | Resolved v0.1.0.0 â€” Call site added at end of `StepSimulation()` after `UpdateInventoryAudit(dt)`. Default status changed from `"OK"` to `"NOT_CHECKED"`. Init reset added for all diagnostic state fields. Diagnostic executes every physics timestep. |
| CS-0011 | Acceptance tests are formula-only, not simulation-validated | High | Assigned | - | Not Tested |
| CS-0040 | RVLIS indicator stale during PZR drain | High | Assigned | - | Not Tested |
| CS-0007 | No UI display for primary ledger drift | Medium | Assigned | Blocked by: CS-0006 | Resolved v0.1.0.0 â€” "Primary Ledger Drift" row added to TabValidation using `DrawCheckRowThreeState` (100 lb warn / 1000 lb error thresholds). Shows "Not checked yet" until first coupled step, then displays drift percentage. |
| CS-0041 | Inventory audit baseline type mismatch (geometric vs mass-derived gallons) | Medium | Assigned | - | Not Tested |
| CS-0012 | No regime transition logging | Low | Assigned | - | Not Tested |

## D) Execution Readiness Indicator
**READY FOR AUTHORIZATION**

No blocking Critical issues unresolved outside domain.

## E) Notes / Investigation Links
- Prior IP references:
  - IP-0001 â€” Primary Mass Conservation â€” Phase C
  - IP-0003 â€” Bubble Formation and Two-Phase
  - IP-0010 â€” Test Infrastructure
  - IP-0011 â€” Validation and Diagnostic Display
  - IP-0012 â€” Observability
- Validation evidence references:
  - CS-0006: Resolved v0.1.0.0 â€” Call site added at end of `StepSimulation()` after `UpdateInventoryAudit(dt)`. Default status changed from `"OK"` to `"NOT_CHECKED"`. Init reset added for all diagnostic state fields. Diagnostic executes every physics timestep.
  - CS-0007: Resolved v0.1.0.0 â€” "Primary Ledger Drift" row added to TabValidation using `DrawCheckRowThreeState` (100 lb warn / 1000 lb error thresholds). Shows "Not checked yet" until first coupled step, then displays drift percentage.
