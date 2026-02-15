# GOLD Standard C# Module Template

Last Updated: 2026-02-15  
Owner: Engineering Standards

## Purpose & Scope

This document is the authoritative C# coding and module standard for this repository.

- Applies to all C# source files under `Assets/`.
- Use partial classes where practical to separate concerns and keep file size responsible.
- Treat this template as the baseline for implementation reviews, audits, and pull request checks.

## GOLD Checklist - New Files

1. Add a full file header (see standard below).
2. Use namespace and folder conventions:
   - Folder path should map to namespace (for example `Assets/Scripts/Physics/...` -> `Critical.Physics`).
   - Avoid global-namespace project files.
3. Design for SRP/SOLID:
   - One clear module responsibility per file.
   - Split large/coordinator files into partials or focused collaborators.
4. Add XML docs for public API surfaces:
   - Public classes/structs/enums/interfaces.
   - Public methods/properties/events.
5. Add or update unit/integration tests where behavior changed or risk is non-trivial.
6. Follow Unity performance guardrails for hot paths:
   - No avoidable GC allocations in per-frame/per-tick loops.
   - No LINQ in hot loops.
   - No per-frame log spam or string-building in simulation loops.

## GOLD Checklist - Amending Existing Files

1. Update file header `Last Updated`.
2. Add one new entry in file header `Changes` (newest first).
3. Keep only the latest 5-10 `Changes` entries in the file header; move older entries to `CHANGELOG.md`.
4. Preserve backward compatibility or explicitly document breakage in XML `<remarks>` and changelog.
5. Re-run impacted tests and record outcome in PR/commit notes.
6. If behavior intentionally deviates from reference documentation, add a brief in-code comment and an explicit note in project changelog/audit docs.
7. If no code change is needed after review, leave a comment in the review artifact explaining why.

## File Header Standard

Required header fields:

- `File`
- `Module`
- `Responsibility`
- `Standards`
- `Version` (MAJOR.MINOR only)
- `Last Updated` (must change on every amendment)
- `Changes` (last 5-10 entries only)

Example:

```csharp
// ============================================================================
// File: Assets/Scripts/Physics/ExampleModule.cs
// Module: Critical.Physics.ExampleModule
// Responsibility: One-line summary of behavior owned by this file.
// Standards: GOLD v1.0, SRP/SOLID, Unity Hot-Path Guardrails
// Version: 2.3
// Last Updated: 2026-02-15
// Changes:
//   - 2.3 (2026-02-15): Added pressure clamp telemetry and guard rails.
//   - 2.2 (2026-02-10): Split validation helpers into partial file.
//   - 2.1 (2026-02-07): Fixed null-safe startup path and unit tests.
// ============================================================================
```

## Semantic Versioning Rules (SemVer 2.0)

Use SemVer meaning, even if file header displays MAJOR.MINOR only.

| Change Type | Rule | Example |
|---|---|---|
| MAJOR | Breaking API/behavior change | `2.4.7 -> 3.0.0` |
| MINOR | Backward-compatible feature | `2.4.7 -> 2.5.0` |
| PATCH | Backward-compatible fix | `2.4.7 -> 2.4.8` |

Header `Version` stores `MAJOR.MINOR` only (for example `2.5`).  
Patch/build metadata should be tracked in project changelog/build output.

## Where Changes Are Recorded

1. File-level: header `Changes` section (latest 5-10 entries).
2. Project-level: `CHANGELOG.md` (authoritative long history).
3. API-level: XML `<remarks>` on public members when behavior/contract changes.
4. Commit-level: conventional commit style guidance:
   - `feat(scope): ...`
   - `fix(scope): ...`
   - `perf(scope): ...`
   - `refactor(scope): ...`
   - `docs(scope): ...`
   - `test(scope): ...`

## Branching / Workflow Guidance

- `main`: production-ready, protected branch.
- `develop`: integration branch for validated work.
- `feature/*`: feature or refactor work, merged into `develop`.
- `hotfix/*`: urgent production fixes, merged into `main` and back-merged to `develop`.
- `release/*`: stabilization branch for release readiness.

Minimum flow:

1. Branch from `develop` (`feature/*`) or `main` (`hotfix/*`).
2. Implement + test + update headers/changelog.
3. Open PR with audit/test evidence.
4. Merge via non-fast-forward to preserve history context.

## Automated Versioning

Use CI/build metadata for patch/build identity; keep human-managed header version at MAJOR.MINOR.

Example `.csproj` snippet:

```xml
<PropertyGroup>
  <VersionPrefix>5.4</VersionPrefix>
  <Version>$(VersionPrefix).$(BuildCounter)</Version>
  <InformationalVersion>$(Version)+sha.$(SourceRevisionId)</InformationalVersion>
  <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
</PropertyGroup>
```

Manual updates:

- File header `Version` (MAJOR.MINOR)
- File header `Last Updated`
- File header `Changes`
- `CHANGELOG.md`

Build-provided updates:

- Patch counter/build number
- Commit SHA metadata
- CI timestamps/artifact metadata

## Validation / Pre-commit Examples

Header `Last Updated` verification example (PowerShell):

```powershell
Get-ChildItem Assets -Recurse -Filter *.cs |
  Where-Object { -not (Select-String -Path $_.FullName -Pattern 'Last Updated:\s+\d{4}-\d{2}-\d{2}' -Quiet) } |
  Select-Object -ExpandProperty FullName
```

Optional API break check guidance:

- Generate a public API baseline from `main`.
- Compare PR branch public signatures to baseline.
- If breaks are intentional, require MAJOR bump note + changelog entry + migration note.

## Unity Performance Guardrails

- Avoid LINQ and heap allocations in `Update`, `FixedUpdate`, and tight simulation loops.
- Avoid per-frame string concatenation/interpolation and `Debug.Log` spam in hot paths.
- Cache component references (`GetComponent` outside hot loops).
- Prefer reusable buffers over repeated allocation.
- Use structs carefully; avoid hidden boxing/copying in high-frequency paths.
- Gate diagnostics behind flags and sampling intervals.

## Audit Alignment Clause

"If Technical_Documentation/ defines real-world PWR behaviour, implementation must align OR document intentional deviation."

