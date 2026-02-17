# IP-0032 Stage B Design Freeze (2026-02-16_205500)

- IP: `IP-0032`
- DP: `DP-0010`
- Stage: `B`
- Input baseline: `Governance/ImplementationPlans/IP-0032/Reports/IP-0032_StageA_RootCause_2026-02-16_193103.md`

## 1) Scope
- `CS-0097`
- `CS-0083`
- `CS-0060`
- `CS-0058`

## 2) CS-0097 Governance Contract Freeze
1. `issue_index.json` remains the full-lifecycle authority (`OPEN + CLOSED`).
2. `issue_register.json` is enforced as active-only working set (non-`CLOSED` only).
3. `issue_archive.json` is enforced as the closed projection of the index.
4. Stage D parity gate is strict:
`index(CLOSED) == archive` and `index(non-CLOSED) == register`.

## 3) CS-0083 Numeric Authority Freeze
1. Published authority decision:
`Technical_Documentation/RCP_Heat_Authority_Decision_2026-02-16.md`.
2. Precedence decision:
- Normative cold-water startup RCP heat authority = `~6 MW per RCP` (`~24 MW total`).
- `RHR_SYSTEM_RESEARCH_v3.0.0.md` values are historical assumptions, not baseline authority.
3. Runtime constant alignment remains explicitly tracked under `CS-0080` / `IP-0036`.

## 4) CS-0060 Constants Responsibility Freeze
1. `PlantConstants.cs` and `PlantConstants.CVCS.cs` become data-only constant files.
2. Runtime calculation extraction targets:
- Unit/thermal helper math -> `PlantMath.cs`
- CVCS/orifice/letdown flow math -> `CVCSFlowMath.cs`
3. All runtime call sites must reference utility classes, not constants partials.

## 5) CS-0058 Lifecycle Ownership Freeze
1. `UpdateHZPSystems()` is update-only processing; no first-time initialization allowed.
2. HZP initialization authority is moved to explicit lifecycle transition handling (`UpdateHZPLifecycle()`).
3. Session reset path must clear HZP lifecycle state via `ResetHZPSystemsLifecycle()` during initialization.

## 6) Stage B Exit
1. Governance, authority, extraction, and lifecycle designs are explicit: `PASS`.
2. Stage C implementation authorized.
