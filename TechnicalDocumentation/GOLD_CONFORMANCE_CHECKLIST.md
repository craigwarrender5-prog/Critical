# GOLD Conformance Checklist

## Purpose
Provide deterministic file-level conformance criteria with objective evidence requirements.

## Usage Rule
- Every item below is required unless explicitly marked `N/A` by scope.
- `N/A` requires one-line rationale in audit output.

## Checklist Items

| ID | Requirement | PASS Test | FAIL Trigger | Evidence Required |
|---|---|---|---|---|
| C01 | Header present and complete | File starts with contiguous header containing banner, `PURPOSE`, `SOURCES`, `UNITS`, architecture field, `GOLD STANDARD` | Missing or split header fields | Header line references |
| C02 | Units clearly defined | Physics quantities declare units in identifier or XML docs | Any physics variable/constant with implicit units | Example lines for representative fields |
| C03 | No undocumented magic numbers | Numeric literals are sourced, derived, or unit-conversion constants | Unsourced physics literal in logic or constants | Literal line references and missing source note |
| C04 | Lifecycle separation respected | Behavior maps cleanly to `INIT`, `START`, `UPDATE`, `VALIDATION` with no phase leakage | Any mixed-phase method or cross-phase leakage | Method line references |
| C05 | No UPDATE-time initialization | `UPDATE` path has no one-time setup, except documented justified exception | Lazy init or scenario setup in update path | Update path line references |
| C06 | No hidden subsystem activation | Activation/deactivation conditions are explicit and documented | Implicit enable/disable side effects | Activation callsite lines |
| C07 | Partial classes used appropriately | Large or multi-concern modules are partitioned with clean ownership | Large mixed-responsibility file without partitioning | File size and responsibility evidence |
| C08 | No mixed responsibility | File contains one primary concern | Physics/UI/controller/constants logic mixed in one file | Mixed-concern line references |
| C09 | Structured state snapshot exposed | Engine-facing module exposes typed state and snapshot access | Ad hoc mutable public state without typed boundary | State type and accessor lines |
| C10 | Logging semantics unambiguous | Regime changes, active subsystems, and gross/net terms are clearly logged | Silent transitions or ambiguous thermal/flow terms | Logging callsite lines |

## Mandatory Fail Gates
Any single gate below forces overall `FAIL`:
- missing mandatory header semantics (`C01`)
- undocumented physics magic number (`C03`)
- lifecycle leakage into wrong phase (`C04` or `C05`)
- hidden regime/coupling activation without logging (`C06` or `C10`)
- constants file with runtime logic or mutable static state
- engine-facing module lacking typed state boundary (`C09`)

## Decision Algorithm
1. Evaluate `C01-C10`.
2. Mark each item `PASS`, `FAIL`, or `N/A`.
3. Apply mandatory fail gates.
4. Compute overall result:
- `PASS`: all applicable items PASS and no mandatory fail gate triggered.
- `FAIL`: any applicable item FAIL or any mandatory fail gate triggered.

## Required Audit Output (Per File)
1. `File: <path>`
2. `Overall: PASS/FAIL`
3. `Item Results: C01=..., C02=..., ...`
4. `Failed Items: <comma-separated IDs or NONE>`
5. `Evidence: <path:line references>`
6. `Notes: <concise rationale>`

## Evidence Policy
- Every `FAIL` MUST include at least one exact line reference.
- Every `PASS` SHOULD include at least one representative line reference.
- Evidence without line references is non-conformant audit output.

This checklist is standards-only and authorizes no remediation.
