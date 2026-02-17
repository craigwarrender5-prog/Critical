# IP-0036 Stage B Design Freeze (2026-02-17_070900)

- IP: `IP-0036`
- DP: `DP-0001`
- Stage: `B`

## 1) Frozen Scope Contract
1. `CS-0080` remediation aligns all in-scope runtime and acceptance contract references to the frozen authority basis from `IP-0032`.

## 2) Frozen Technical Decisions
1. Authoritative RCP startup heat constants are:
- `RCP_HEAT_MW = 24f` (4 pumps total)
- `RCP_HEAT_MW_EACH = 6f` (per pump)
2. Active runtime paths must reference constants rather than hard-coded `21 MW` / `5.25 MW`.
3. Validation/test references in in-repo test modules touched by this scope are updated to constant-driven expectations.
4. Scope excludes unrelated dashboard/UI workstream files currently present in the working tree.

## 3) Validation Contract Freeze
1. Stage D acceptance requires explicit constant authority alignment in:
- Constants definition
- Heat contribution path
- Heatup phase descriptors
- Unit/integration validation checks in scope
2. Stage D compile validation is best-effort from terminal workspace; known pre-existing compile defects outside scoped deltas are documented, not masked.

## 4) Stage B Exit
Stage B design freeze is complete for `CS-0080`. Stage C controlled remediation authorized.
