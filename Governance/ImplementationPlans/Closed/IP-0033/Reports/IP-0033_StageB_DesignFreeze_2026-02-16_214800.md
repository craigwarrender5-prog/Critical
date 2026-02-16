# IP-0033 Stage B Design Freeze (2026-02-16_214800)

- IP: `IP-0033`
- DP: `DP-0007`
- Stage: `B`
- Input baseline: `Governance/ImplementationPlans/IP-0033/Reports/IP-0033_StageA_RootCause_2026-02-16_214500.md`

## 1) Scope
- `CS-0006`
- `CS-0007`
- `CS-0011`
- `CS-0012`
- `CS-0041`
- `CS-0062`
- `CS-0064`

## 2) CS-0062 Telemetry Semantics Freeze
1. `stageE_PrimaryHeatInput_MW` must be sourced from independent primary-side heat additions only.
2. `sgHeatTransfer_MW` remains SG removal telemetry and must never be re-used as primary input.
3. Over-primary checks retain existing thresholds (`>5%` violation gate) against non-aliased denominator.

## 3) CS-0012 Regime Logging Freeze
1. Regime transitions are logged only on regime-ID edge changes (`1/2/3`), not each step.
2. Transition record includes previous label, next label, `alpha`, `rcpCount`, and reason token.
3. Initialization resets previous-regime sentinel state each run.

## 4) CS-0064 Snapshot Boundary Freeze
1. `ScreenDataBridge` RHR read paths must consume `StepSnapshot -> PlantState` first.
2. Direct mutable-field reads are removed from scoped RHR bridge getters.
3. `PlantState` contract extension approved: add immutable `RhrMode` payload emitted by `LegacyStateBridge`.

## 5) CS-0011 Runtime Acceptance Freeze
1. AT-02, AT-03, and AT-08 become runtime-evidence-gated.
2. `AcceptanceTests_v5_4_0.RunAllTests()` must fail these tests when runtime evidence is absent.
3. Add deterministic editor runner to capture machine-readable evidence and publish artifact.

## 6) Carry-Forward Closure Freeze
1. `CS-0006`, `CS-0007`, and `CS-0041` are closed in this IP with carry-forward code evidence.
2. Stage D verifies no regression in those remediated paths.

## 7) Stage B Exit
Design, authority, and evidence contracts frozen. Stage C remediation authorized.
