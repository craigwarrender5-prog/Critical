# IP-0025 Stage D - Legacy Order Parity

- Timestamp: 2026-02-15 21:33:15
- Run stamp: `2026-02-15_213315`
- Scope: legacy `StepSimulation(dt)` causal sequence mapping to coordinator module slots.

## Legacy Causal Sequence (from `HeatupSimEngine.StepSimulation(dt)` callsite comments)
1. RCP startup sequencing and contribution update.
2. Heater and spray control updates before thermal solve paths.
3. Regime-based thermal/pressure update paths and CVCS/transport coupling.
4. RVLIS + annunciator updates.
5. HZP system updates.
6. Inventory audit and primary mass ledger diagnostics.

## Proposed Coordinator Slot Order (Stage D provisional)
1. `Reactor`
2. `RCP`
3. `RCS`
4. `PZR`
5. `CVCS`
6. `RHR`
7. transfer finalize
8. snapshot publish
9. validation hook

## Parity Matrix
| Legacy Phase | Proposed Slot(s) | Parity Status | Decision |
|---|---|---|---|
| RCP sequencing | `RCP` | MATCH | Keep slot order. |
| Heater + spray control | `PZR` | PARTIAL (legacy pre-solve control timing) | Preserve legacy authority path in Stage D; defer slot-level behavioral activation to Stage E packaging. |
| Regime thermal + coupling | `RCS`, `CVCS`, `RHR`, `Reactor` | PARTIAL (legacy monolithic solve) | Keep stubs no-op in Stage D; no physics moved. |
| RVLIS + annunciators | post-module internal legacy update | MATCH (via legacy authority path) | Keep under legacy step until extraction stage. |
| Inventory + primary ledger diagnostics | transfer finalize + snapshot publish | MATCH | Keep finalize/publish order after legacy step. |

## Mismatch Flags
- No blocking mismatch for Stage D scaffolding because authoritative mutable updates remain legacy-only.
- Legacy bypass flags remain scaffold-only; subsystem bypass activation is deferred with Stage E extraction authorization.

## Decision Record
- Stage D coordinator order is accepted as provisional parity scaffold.
- Comparator remains shadow-only and side-effect free.
- Any causal-order change that affects mutable-state authority requires explicit Stage E authorization.
