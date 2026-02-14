# GOLD Lifecycle Contract

## Purpose
Define non-overlapping lifecycle ownership so initialization, scenario activation, and per-timestep physics evolution are auditable as PASS or FAIL.

## Scope
Applies to simulation-facing modules and coordinators, including Unity `MonoBehaviour` orchestration code.

## Normative Terms
- `MUST` and `MUST NOT` are mandatory.
- `SHOULD` is recommended but not mandatory.

## Lifecycle Map
- `INIT`: constructor/allocation/default state phase.
- `START`: scenario activation and initial condition phase.
- `UPDATE`: per-timestep evolution and integration phase.
- `VALIDATION`: optional invariant/diagnostic checks.

## INIT (Constructor / Allocation Phase)
Allowed:
- allocate arrays, buffers, and deterministic containers
- instantiate submodules
- assign deterministic defaults

Forbidden:
- timestep evolution or integration
- implicit subsystem activation
- reading runtime-only engine state
- scenario-specific branching

PASS criteria:
- no dependency on runtime `dt`, simulation time, or evolving plant state
- only deterministic allocation/default setup

FAIL criteria:
- any time advancement, physics integration, or hidden mode activation

## START (Scenario Activation Phase)
Allowed:
- apply scenario initial conditions
- explicitly enable or disable subsystems
- reset timers, latches, and trip states
- run one-time equilibrium pre-computation

Forbidden:
- ongoing integration loops
- hidden coupling activation
- undocumented regime switching
- allocations that belong to INIT

PASS criteria:
- one-time scenario setup only
- all enabled paths are explicit and traceable

FAIL criteria:
- repeated physics evolution in START
- implicit coupling or undocumented state transitions

## UPDATE (Per-Timestep Evolution)
Allowed:
- advance simulation time
- integrate physics with explicit `dt`
- perform deterministic sub-stepping for stability
- apply documented regime transitions

Forbidden:
- lazy initialization
- scenario reconfiguration
- silent regime switching
- silent coupling activation
- allocation in hot path unless justified and documented

PASS criteria:
- deterministic update path with explicit timestep handling
- every regime/coupling transition has log visibility

FAIL criteria:
- one-time setup behavior in UPDATE
- undocumented transition behavior
- hidden subsystem activation

## VALIDATION (Optional)
Allowed:
- invariant checks
- threshold diagnostics
- audit telemetry generation

Forbidden:
- global state mutation
- wall-clock dependent behavior
- hidden correction of runtime state

PASS criteria:
- side-effect-free and deterministic checks

FAIL criteria:
- validation mutates simulation state or depends on wall-clock timing

## Cross-Phase Ownership Rules
- Each behavior MUST belong to exactly one lifecycle phase.
- If a method performs actions from multiple phases, that method is FAIL.
- Non-obvious ownership MUST be documented inline at the call site.
- `G3` delegation intent applies: coordinator engines MUST NOT embed inline physics solvers.

## Transition Visibility Contract
- Regime switches MUST log prior state, new state, and trigger condition.
- Coupling activation/deactivation MUST log active subsystem and gating condition.
- Gross and net thermal/flow terms SHOULD be labeled distinctly when both are present.

## Audit Decision Procedure
1. Classify each method as `INIT`, `START`, `UPDATE`, or `VALIDATION`.
2. Mark every method that mixes phase responsibilities as FAIL.
3. Verify transition logging for regime or coupling changes.
4. Return overall lifecycle status:
- `PASS`: no phase leakage and no hidden transitions.
- `FAIL`: any leakage, hidden activation, or undocumented transition.
