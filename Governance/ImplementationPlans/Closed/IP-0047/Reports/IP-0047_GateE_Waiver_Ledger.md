# IP-0047 Gate E Waiver Ledger

- IP: `IP-0047`
- Date (UTC): `2026-02-17T16:47:47Z`
- Author: `Codex`
- Policy Basis: `Technical_Documentation/Archive/GOLD_FILE_HYGIENE_LIMITS.md`

## Waiver Scope
The following simulation-facing files remain above the GOLD hard threshold (`>1400` lines) and are approved for continued operation under documented containment controls.

| File | Lines | Waiver Basis | Containment Controls |
|---|---:|---|---|
| `Assets/Scripts/UI/MultiScreenBuilder.cs` | 4343 | Editor-only builder with no runtime simulation loop ownership; decomposition would be high-risk for existing screen construction automation without dedicated UI refactor wave. | Restrict changes to isolated feature branches; no physics/control logic additions permitted; require targeted regression checks for generated screens. |
| `Assets/Scripts/Validation/HeatupSimEngine.cs` | 3421 | Coordinator already partially decomposed; remaining size due broad orchestration/state surface and legacy compatibility constraints. | Continue strict partial extraction strategy; prohibit new subsystem internals in this file; route new logic to partial/module files only. |
| `Assets/Scripts/Physics/SGMultiNodeThermal.cs` | 2694 | High-density thermodynamics model with regime continuity, instrumentation, and inventory logic tightly coupled to validated behavior envelope. | No mixed UI concerns allowed; require evidence-driven edits and regime transition regression logs for each change. |
| `Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs` | 2081 | Multi-phase startup authority logic retained in one partial for deterministic sequencing and audit readability. | Freeze phase-order contract; require explicit phase-transition logging for any amendment. |
| `Assets/Scripts/Physics/CVCSController.cs` | 1554 | Control-law and mode handling breadth expanded by startup determinism controls; near-threshold but still single-controller ownership. | No non-CVCS responsibilities allowed; require controller unit evidence before merge. |
| `Assets/Scripts/Validation/HeatupSimEngine.Logging.cs` | 1510 | Consolidated event/logging and interval reporting responsibilities; size growth tied to observability commitments. | Keep logging-only responsibility; disallow physics state mutation additions beyond event capture. |

## Approval Statement
- Waivers are accepted for Gate E closure of `IP-0047` subject to the containment controls above.
- This waiver set does not authorize uncontrolled growth; each waived file remains under heightened review standards.

## Decision
- Waiver ledger approved for Gate E acceptance workflow.
