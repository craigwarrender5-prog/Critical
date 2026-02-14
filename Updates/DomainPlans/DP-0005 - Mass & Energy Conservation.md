---
Identifier: DP-0005
Domain (Canonical): Mass & Energy Conservation
Status: ACTIVE — Under IP-0016
Linked Issues: CS-0051, CS-0050, CS-0001, CS-0002, CS-0003, CS-0005, CS-0008, CS-0004, CS-0013
Last Reviewed: 2026-02-14
Authorization Status: NOT AUTHORIZED
Mode: SPEC/DRAFT
---

# DP-0005 - Mass & Energy Conservation

## A) Domain Summary
- Canonical Domain: Mass & Energy Conservation
- DP Status: Open
- Total CS Count in Domain: 9

## B) Severity Distribution
| Severity | Count |
|---|---:|
| Critical | 3 |
| High | 1 |
| Medium | 3 |
| Low | 2 |

## C) Ordered Issue Backlog
| CS ID | Title | Severity | Status | Blocking Dependency | Validation Outcome |
|---|---|---|---|---|---|
| CS-0051 | Stage E mass conservation discontinuity at 8.25 hr during solid->two-phase handoff | Critical | Open - Investigating | Triggered by regime handoff at bubble DETECTION; DP-0005 ownership because failure is canonical mass continuity/conservation authority | Stage E evidence: error jump 48.6 lbm -> 17,447.2 lbm at 8.25 hr; dominant bucket change PZR water mass drop ~17.5 klbm. |
| CS-0050 | Persistent plant-wide mass conservation imbalance (~10,000 gal class) across multiple intervals | Critical | Assigned | Validation trigger: IP-0015 Stage E rerun evidence | Stage E (18.00 hr) rerun: SG startup criteria PASS, conservation criterion FAIL (max mass error 40,407.19 lbm). |
| CS-0001 | CoupledThermo canonical mass mode never activated | Critical | Assigned | - | Resolved v0.1.0.0 â€” Both R2/R3 call sites now pass `physicsState.TotalPrimaryMass_lb` as 10th arg. Solver logs "CANONICAL mode active" on first coupled step. Default parameter removed in Phase D (compile-time enforcement). |
| CS-0002 | TotalPrimaryMass_lb freezes after first step in Regime 2/3 | High | Assigned | Blocked by: CS-0001. Related: CS-0006 (diagnostic reads stale ledger) | Resolved v0.1.0.0 â€” Auto-resolved by CS-0001. CVCS boundary flows now mutate `TotalPrimaryMass_lb` every timestep in R2/R3. Ledger tracks live state. |
| CS-0003 | Boundary flow accumulators never incremented | Medium | Assigned | Blocked by: CS-0001. Blocks: CS-0006. | Resolved v0.1.0.0 â€” Accumulators incremented at 4 sites: R1 solid ops, R2 pre-solver, R3 pre-solver, CVCS.cs post-physics. Double-count guarded by `regime3CVCSPreApplied`. Relief accumulator documented (no two-phase relief physics â†’ stays 0f by design). |
| CS-0005 | CVCS double-count guard works by accident | Medium | Assigned | Blocked by: CS-0001. Related: CS-0003 (accumulators track the redirected flow) | Resolved v0.1.0.0 â€” CVCS now targets `TotalPrimaryMass_lb` (ledger mutation) instead of `RCSWaterMass`. Guard still prevents double-counting in CVCS.cs. Ownership documented in Phase D comments. Mechanism is now architecturally intentional, not accidental. |
| CS-0008 | No runtime solver mass conservation check | Medium | Assigned | - | Resolved v0.1.0.0 â€” Post-solver guard rail added in `RCSHeatup.BulkHeatupStep()`: computes `M_out = RCS + PZR_water + PZR_steam`, compares to canonical ledger. WARNING at >10 lb delta, ERROR at >100 lb delta. Diagnostics only â€” does not modify state. |
| CS-0004 | Pre-solver V*rho PZR mass computation redundant with solver | Low | Assigned | depends on: CS-0001 | Resolved v0.1.0.0 â€” Authority ownership documented at R2/R3 headers and CVCS blocks. Default parameter removed from `BulkHeatupStep` (compile-time enforcement). LEGACY path deprecated with 24-line comment block. Pre-sync computation now meaningful under canonical mode. |
| CS-0013 | Session lifecycle resets for canonical baseline and solver log flag | Low | Assigned | - | Resolved â€” `firstStepLedgerBaselined = false` added at Init.cs:52; `CoupledThermo.ResetSessionFlags()` added at Init.cs:53 and CoupledThermo.cs:51-54. |

## D) Regime Transition Conservation Contract (RTCC)
Status: MANDATORY DESIGN SPECIFICATION (prerequisite for DP-0005 implementation work)

### D.1 IP-XXXX Mandate
- Every DP-0005 implementation plan (IP-XXXX) must include or reference this RTCC before code authorization.
- No physics modification may occur until RTCC is explicitly included in the governing implementation plan.

### D.2 Invariant (Mathematical)
At any regime transition boundary:
`TotalTrackedMass_before == TotalTrackedMass_after`
within defined floating tolerance `epsilon_mass`.

Operational assert form:
`abs(TotalTrackedMass_after - TotalTrackedMass_before) <= epsilon_mass`

### D.3 Authority Model
- Before transition: a single declared structure is canonical mass authority for the active regime.
- After transition: a single declared structure is canonical mass authority for the destination regime.
- Authority may change only through an explicit reconciliation step that closes mass delta at the handoff boundary.
- No implicit overwrite is allowed to become canonical without reconciliation.

### D.4 Handoff Procedure (Abstract, Required Sequence)
1. Pre-handoff snapshot of all conserved buckets.
2. Compute reconstructed mass terms required by destination regime state.
3. Compute handoff delta: reconstructed total minus canonical pre-handoff total.
4. Apply equal/opposite transfer or ledger reconciliation to close the delta.
5. Execute post-handoff conservation assertion against tolerance.
6. Log reconciliation delta and authority handoff metadata.

### D.5 Assertion Requirement
- Every regime transition must execute a conservation assert.
- Every transition must emit a logged reconciliation delta, including explicit zero when no correction is needed.
- Transition path must fail fast if `abs(delta) > epsilon_mass`.

### D.6 Reusability Requirement
RTCC applies to:
- Solid -> Two-phase.
- Two-phase -> Solid (if/when applicable).
- Any future regime swap that changes canonical authority.

### D.7 Validation Coupling
RTCC conformance is coupled to validation outcomes:
- Stage E conservation pass criteria must pass.
- Interval inventory audit must agree across regime boundaries (no discontinuity class jumps).
- Step-level `massError_lbm` must remain within approved tolerance band at and after transition.

## E) Execution Readiness Indicator
**READY FOR AUTHORIZATION**

No blocking Critical issues unresolved outside domain.

## F) DP-0005 Closure Criteria
- RTCC is included as a mandatory prerequisite in the active DP-0005 implementation plan (IP-XXXX).
- All DP-0005 regime-transition paths implement RTCC assertion and reconciliation logging.
- Stage E rerun passes conservation criteria with no transition discontinuity class failures.
- Interval inventory audit and step-level `massError_lbm` satisfy approved tolerances.

## G) Notes / Investigation Links
- Prior IP references:
  - IP-0001 â€” Primary Mass Conservation â€” Phase A
  - IP-0001 â€” Primary Mass Conservation â€” Phase A (auto-resolved when canonical mode activated)
  - IP-0001 â€” Primary Mass Conservation â€” Phase A (redirect CVCS to ledger)
  - IP-0001 â€” Primary Mass Conservation â€” Phase B
  - IP-0001 â€” Primary Mass Conservation â€” Phase D (document ownership, evaluate cleanup)
- Preliminary investigation references:
  - Updates/Issues/CS-0051_Investigation_Report.md
  - Updates/Issues/CS-0050_Investigation_Report.md
  - Updates/Issues/IP-0015_StageE_Rerun_2026-02-14_123200.md
- Validation evidence references:
  - CS-0001: Resolved v0.1.0.0 â€” Both R2/R3 call sites now pass `physicsState.TotalPrimaryMass_lb` as 10th arg. Solver logs "CANONICAL mode active" on first coupled step. Default parameter removed in Phase D (compile-time enforcement).
  - CS-0002: Resolved v0.1.0.0 â€” Auto-resolved by CS-0001. CVCS boundary flows now mutate `TotalPrimaryMass_lb` every timestep in R2/R3. Ledger tracks live state.
  - CS-0003: Resolved v0.1.0.0 â€” Accumulators incremented at 4 sites: R1 solid ops, R2 pre-solver, R3 pre-solver, CVCS.cs post-physics. Double-count guarded by `regime3CVCSPreApplied`. Relief accumulator documented (no two-phase relief physics â†’ stays 0f by design).
  - CS-0004: Resolved v0.1.0.0 â€” Authority ownership documented at R2/R3 headers and CVCS blocks. Default parameter removed from `BulkHeatupStep` (compile-time enforcement). LEGACY path deprecated with 24-line comment block. Pre-sync computation now meaningful under canonical mode.
  - CS-0005: Resolved v0.1.0.0 â€” CVCS now targets `TotalPrimaryMass_lb` (ledger mutation) instead of `RCSWaterMass`. Guard still prevents double-counting in CVCS.cs. Ownership documented in Phase D comments. Mechanism is now architecturally intentional, not accidental.
  - CS-0008: Resolved v0.1.0.0 â€” Post-solver guard rail added in `RCSHeatup.BulkHeatupStep()`: computes `M_out = RCS + PZR_water + PZR_steam`, compares to canonical ledger. WARNING at >10 lb delta, ERROR at >100 lb delta. Diagnostics only â€” does not modify state.
  - CS-0013: Resolved â€” `firstStepLedgerBaselined = false` added at Init.cs:52; `CoupledThermo.ResetSessionFlags()` added at Init.cs:53 and CoupledThermo.cs:51-54.

