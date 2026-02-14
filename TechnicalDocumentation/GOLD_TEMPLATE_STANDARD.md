# GOLD Template Standard

## Purpose
Define mandatory file template and structure rules for simulation-facing code so audit results can be judged as PASS/FAIL with minimal interpretation.

## Scope
Applies to all simulation-facing source files, including physics modules, controllers, orchestrators, constants partials, and companion visual modules used in runtime simulation.

## Alignment Baseline
This standard is tightened against:
- `Documentation/GOLD_STANDARD_TEMPLATE.md` (`G1-G10`)
- existing GOLD headers and module patterns in `Assets/Scripts`

Conformance MUST satisfy both this document and `G1-G10` intent.

## A) Mandatory File Header Block
Every in-scope file MUST start with a header block.

Required semantic fields:
1. `Banner Title`
2. `PURPOSE`
3. `SOURCES` (physics-based modules: required; non-physics: explicit `N/A` allowed)
4. `UNITS` (physics-based modules: required; non-physics: explicit `N/A` allowed)
5. `ARCHITECTURE NOTES`
6. `GOLD STANDARD: Yes/No`

Accepted architecture labels (exact token match, one required):
- `ARCHITECTURE:`
- `ARCHITECTURE NOTES:`
- Constants-partial equivalent: `DOMAIN:` plus `NOTE:`

Required conformance:
- PASS: all required semantic fields present, non-empty, and header is contiguous at file top.
- FAIL: missing field, ambiguous field, or header split by code.

Recommended skeleton:

```text
// ============================================================================
// <Banner Title>
// PURPOSE:
// SOURCES:
// UNITS:
// ARCHITECTURE NOTES:
// GOLD STANDARD: Yes/No
// ============================================================================
```

## B) Region Structure Order
Canonical region order:
1. `#region Constants`
2. `#region Submodules`
3. `#region Instance State`
4. `#region Public Properties`
5. `#region Constructor / Init`
6. `#region Lifecycle (Start / Update)`
7. `#region Control Methods`
8. `#region Validation`
9. `#region State Access`

Applicability rules:
- Files MAY omit non-applicable regions.
- If regions are present, they MUST preserve canonical order.
- Region names may be more specific (`#region Main Update`, `#region Public API`) only if unambiguous mapping exists.

Conformance:
- PASS: present regions map cleanly to canonical order with no responsibility mixing.
- FAIL: reordered regions, ambiguous mapping, or mixed responsibilities inside a region.

## C) Naming and Units Rules
Rules:
- Physics quantities MUST declare units either:
  - in identifier (`_MW`, `_psia`, `_gpm`, `_F`, `_lbm`, `_hr`), or
  - in XML declaration docs.
- Magic literals are forbidden except:
  - unit conversion constants,
  - deterministic derived coefficients with derivation comment,
  - explicitly cited source constants.
- Physics constants MUST include provenance (`NRC`, `FSAR`, equation, or derivation note).
- Ambiguous identifiers (`value`, `factor`, `temp`, `rate`) are forbidden for module state/constants.
- Namespace MUST follow `G7` intent:
  - physics/constants: `Critical.Physics`
  - UI: `Critical.UI`
  - tests: `Critical.Tests`

Conformance:
- PASS: explicit units and provenance; no ambiguous physics identifiers.
- FAIL: undocumented physics literals, implicit units, or namespace misuse.

## D) Constants File Rules
For `PlantConstants`-style files:
- MUST be `static partial class`.
- MUST be partitioned by subsystem domain.
- MUST NOT contain runtime state.
- MUST NOT contain lifecycle/update logic.
- MUST NOT contain mutable static fields.
- Allowed calculations:
  - unit conversions,
  - deterministic derived coefficients with documented derivation.
- All other calculations are forbidden in constants partials.

Conformance:
- PASS: constants-only role with documented deterministic derivations.
- FAIL: runtime logic, mutable state, non-deterministic computation.

## E) State Snapshot Rule
Engine-facing runtime modules MUST expose:
- structured state (`struct`/`class`), and
- explicit snapshot access (`GetState()` or equivalent typed state return path).

Mutation rules:
- External callers MUST NOT directly mutate module-private fields.
- Snapshot consumers MUST use structured state surface.

Allowed exception:
- top-level orchestration engines may expose `[HideInInspector]` telemetry fields for Unity serialization, but MUST still have a typed internal state boundary for physics modules.

Conformance:
- PASS: structured state boundary exists and mutation ownership is explicit.
- FAIL: ad hoc mutable public state with no typed boundary.

## F) Engine Delegation Guard (G3)
Coordinator engines MUST delegate physics calculations to physics modules.

Conformance:
- PASS: no inline physics integration in engine orchestration.
- FAIL: duplicated physics equations or hidden physics calculators in engine files.
