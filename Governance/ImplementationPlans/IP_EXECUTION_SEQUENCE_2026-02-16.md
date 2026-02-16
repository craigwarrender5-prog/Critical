# IP Execution Sequence (2026-02-16)

## Purpose
Execution ordering for newly created READY_FOR_FIX implementation plans.

## Ordered IP Queue
1. `IP-0032` (`DP-0010`) - Wave 0 governance/authority preconditions. `IN_PROGRESS` (Stage A complete; Stage B next)
2. `IP-0033` (`DP-0007`) - Wave 1 diagnostics/evidence foundation.
3. `IP-0034` (`DP-0009`) - Wave 1 performance logging cleanup (parallel-capable with `IP-0033`).
4. `IP-0035` (`DP-0006`) - Wave 2 plant protection startup permissive/alarm alignment.
5. `IP-0036` (`DP-0001`) - Wave 2 RCP heat authority alignment.
6. `IP-0037` (`DP-0011`) - Wave 3 SG startup boundary and pressure response remediation.
7. `IP-0038` (`DP-0010`) - Wave 4 governance hardening closeout.

## Blocking Rules
- `IP-0032` must complete precondition gates before Wave 2 starts.
- `IP-0035` and `IP-0036` should complete (or freeze validation baselines) before `IP-0037`.
- `IP-0038` executes last to avoid governance churn during active behavior remediation.
