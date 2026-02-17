# IP Execution Sequence (2026-02-17)

## Purpose
Priority order for prepared implementation plans across all active Domain Plans with assigned CS, excluding `DP-0008`.

## Candidate IP Set
- `IP-0044` (`DP-0006`)
- `IP-0045` (`DP-0001`)
- `IP-0046` (`DP-0011`)
- `IP-0047` (`DP-0010`)
- `IP-0048` (`DP-0012`)
- `IP-0049` (`DP-0013`)

## Priority Order (Severity + Impact + Blocking)
1. `IP-0047` (`DP-0010`)  
Reason: Contains `CS-0099` (`CRITICAL`) plus cross-cutting governance and structure controls that reduce execution risk in all other IPs.
2. `IP-0048` (`DP-0012`)  
Reason: Establishes deterministic startup baseline (`CS-0101`) and heater-release behavior (`CS-0098`), a prerequisite validation anchor for startup-sensitive domains.
3. `IP-0044` (`DP-0006`)  
Reason: Safety/plant-protection startup permissive and SG alarm controls (`CS-0079`, `CS-0010`) are high operational impact and depend on stable startup baseline.
4. `IP-0045` (`DP-0001`)  
Reason: Primary authority and modularization (`CS-0080`, `CS-0105`, `CS-0106`) are high-impact architecture work that should follow governance/startup baseline stabilization.
5. `IP-0046` (`DP-0011`)  
Reason: SG startup boundary/pressure work (`CS-0082`, `CS-0078`, `CS-0057`) depends on stable startup and primary behavior baselines for clean validation evidence.
6. `IP-0049` (`DP-0013`)  
Reason: Single high-priority item (`CS-0104`) but hard-blocked by `DP-0008` scenario interfaces and required cross-domain inclusion approval.

## Blocking Rules
- `IP-0047` should start first and establish governance baseline before broad implementation execution.
- `IP-0048` should complete baseline freeze before Stage D/E validation in `IP-0044` and `IP-0046`.
- `IP-0045` should freeze primary behavior baseline before final SG startup validation in `IP-0046`.
- `IP-0049` cannot execute integration implementation until DP-0008 interfaces are ready and explicit cross-domain approval is recorded.

## Parallelization Guidance
- `IP-0044` and `IP-0045` may run in parallel after `IP-0047` and `IP-0048` Stage B freezes.
- `IP-0046` should start after baseline freeze points in `IP-0045` and `IP-0048`.
- `IP-0049` remains blocked until DP-0008 prerequisites and approval gates are met.

