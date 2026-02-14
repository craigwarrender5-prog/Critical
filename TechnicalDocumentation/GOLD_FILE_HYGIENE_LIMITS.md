# GOLD File Hygiene Limits

## Purpose
Define objective structural limits for maintainability, auditability, and strict separation of concerns.

## Scope
Applies to simulation-facing files, including physics modules, engines/coordinators, constants partials, controllers, and companion diagnostics.

## A) File Size Limits
- Target range per partial: `<= 1000` lines.
- Warning range: `1001-1400` lines.
- Hard fail threshold: `> 1400` lines unless a documented audit waiver exists.
- Size does not override responsibility: a small file can still FAIL for mixed concerns.

PASS criteria:
- file length is within target, or warning range with single clear responsibility

FAIL criteria:
- file exceeds hard threshold without waiver
- file is in warning range and contains mixed responsibilities

## B) Partial Class Partitioning Rules
Large or multi-concern modules MUST use structured partial partitioning.

Recommended pattern:
- `ReactorCore.cs` (orchestration only)
- `ReactorCore.Constants.cs`
- `ReactorCore.State.cs`
- `ReactorCore.Kinetics.cs`
- `ReactorCore.Thermal.cs`
- `ReactorCore.TripLogic.cs`
- `ReactorCore.Initialization.cs`
- `ReactorCore.Validation.cs`

Mandatory rules:
- each partial MUST have one primary responsibility
- no duplicate state field declarations across partials
- main partial MUST contain orchestration and lifecycle routing only
- physics equations MUST NOT be mixed with UI rendering logic
- validation helpers MUST NOT be embedded inside hot-path update files unless tightly scoped and documented

PASS criteria:
- clean partition boundaries and no cross-cutting ownership bleed

FAIL criteria:
- duplicated state fields
- mixed physics, UI, logging, and validation concerns in one partial
- orchestration partial containing embedded subsystem internals

## C) Separation of Concerns Contract
Physics modules MUST NOT:
- directly manipulate UI
- own scenario configuration policy
- own validation polling loops

Controllers MUST NOT:
- perform low-level physics integration
- own canonical constants definitions

Constants files MUST NOT:
- execute runtime logic
- contain mutable static fields
- reference mutable runtime state

PASS criteria:
- each file role is singular and architecture-consistent

FAIL criteria:
- any role violation between physics, controller, constants, UI, and validation

## D) Logging and Visibility Standard
Required visibility:
- regime switching events are logged
- active subsystems are logged
- gross and net terms are explicitly distinguished when both exist
- hidden heat or flow paths are prohibited

Minimum log content for coupled thermal-hydraulic steps:
- active/inactive state by subsystem
- heat or flow source terms
- heat or flow sink terms
- applied net effect
- gating condition or interlock condition

PASS criteria:
- an auditor can identify active pathways and net effects from logs without source inspection

FAIL criteria:
- silent transitions
- unlabeled gross versus net terms
- active couplings not visible in logs

## E) Conformance Decision Rule
- `PASS`: all sections `A-D` satisfy PASS criteria.
- `FAIL`: any FAIL trigger in sections `A-D`.

## Enforcement Note
This is standards-only baseline documentation. It authorizes no remediation and no implementation actions.
