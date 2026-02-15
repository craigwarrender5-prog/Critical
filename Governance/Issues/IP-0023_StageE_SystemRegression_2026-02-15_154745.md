# IP-0023 Stage E - System Regression Validation

- Timestamp: 2026-02-15 15:47:48
- Scope: runtime/performance convergence for DP-0009

## Referenced Stage D Artifacts
- Group 1 checkpoint: `Governance/Issues/IP-0023_Group1_Checkpoint_2026-02-15_154745.md`
- Group 2 checkpoint: `Governance/Issues/IP-0023_Group2_Checkpoint_2026-02-15_154745.md`
- Group 3 checkpoint: `Governance/Issues/IP-0023_Group3_Checkpoint_2026-02-15_154745.md`
- Group 4 checkpoint: `Governance/Issues/IP-0023_Group4_Checkpoint_2026-02-15_154745.md`

## Legacy-Approx vs Remediated Metrics
| Metric | Legacy Approx | Remediated |
|---|---:|---:|
| Step P95 (ms) | 0.043 | 0.052 |
| Step P99 (ms) | 0.425 | 0.158 |
| GC Gen0 count delta | 0 | 0 |
| Simulation log rate (/s) | 0.001 | 0.000 |
| Async dispatch max (ms) | 1.906 | 2.047 |
| Deterministic replay gate | N/A | PASS |

## Stage E Outcome
- Result: PASS
- Basis: legacyP95=0.043 ms legacyP99=0.425 ms; remediatedP95=0.052 ms remediatedP99=0.158 ms; gc0 legacy/rem=0/0; remLogRate=0.000/s; determinismGate=True
