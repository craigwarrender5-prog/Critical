# IP-0045 Stage B Design Freeze (2026-02-17_184300)

- IP: `IP-0045`
- DP: `DP-0001`
- Stage: `B`

## 1) Frozen Scope Contract
1. `CS-0080` remains frozen to prior authority alignment closure evidence; no thermal retuning is in scope for this IP.
2. `CS-0105` introduces reusable loop-local RCS module contracts and boundary implementation.
3. `CS-0106` introduces manager-owned N-loop contract and aggregate semantics with deterministic N=1 compatibility.

## 2) Frozen Technical Decisions
1. Authoritative RCP heat constants remain:
- `RCP_HEAT_MW = 24f`
- `RCP_HEAT_MW_EACH = 6f`
2. Modular RCS boundary is introduced under:
- `Assets/Scripts/Systems/RCS/`
- `Assets/Prefabs/Systems/RCS/`
3. Manager contract must include:
- Explicit loop ownership/count
- Indexed loop-state retrieval
- Aggregate flow/temperature contract
- N=1 compatibility path against `LoopThermodynamics`
4. UI integration remains additive and compatibility-safe via `ScreenDataBridge` manager-backed accessors.

## 3) Validation Contract Freeze
1. Stage D acceptance requires objective evidence for:
- `CS-0080` authority baseline non-regression
- `CS-0105` reusable loop boundary contracts
- `CS-0106` manager/aggregate API plus N=1 parity contract
2. Stage D compile gate requires `dotnet build Critical.slnx` with `0` errors.
3. Scope excludes unrelated parallel workstream files present in the working tree.

## 4) Stage B Exit
Stage B design freeze is complete for `IP-0045`. Stage C controlled remediation authorized.
