# Issue Register Migration Report - 2026-02-14

## Summary
- Source markdown register: `Updates/IssueRegister_DEPRECATED.md`
- Total issues migrated: `52`
- Active issues migrated to `Governance/IssueRegister/issue_register.json`: `43`
- Closed issues migrated to `Governance/IssueRegister/issue_archive.json`: `9`
- Index entries written to `Governance/IssueRegister/issue_index.json`: `52`

## Field Completeness
- Issues missing domain: `0`
- Issues missing severity: `0`
- Issues missing status: `0`

## Normalization Applied
- Severity normalization to constitution enum:
  - `Critical / BLOCKER` -> `CRITICAL`
  - `Critical` -> `CRITICAL`
  - `High` and `Medium-High` -> `HIGH`
  - `Medium` -> `MEDIUM`
  - `Low` -> `LOW`
- Active status normalization to constitution enum:
  - `Assigned` -> `READY_FOR_FIX`
  - `Implemented - Pending Validation` -> `READY_FOR_FIX`
- Closed status handling:
  - `CLOSED` records moved to archive snapshots with `resolution_type` and closure references.

## Date Handling
- `created_at` sourced from `Date Discovered` when available.
- Where missing, `created_at` defaulted to `2026-02-13T00:00:00Z` (registry creation baseline).
- `updated_at` set to migration timestamp for active issues.
- `closed_at` sourced from `Closed Date` when available, else migration timestamp.

## Anomalies
- Duplicate issue IDs: `none`
- Missing titles: `none`
- Invalid domains requiring `UNASSIGNED`: `none`

## Governance and Deprecation
- New authoritative JSON governance artifacts created under `Governance/IssueRegister/`.
- Legacy markdown register preserved at `Updates/IssueRegister_DEPRECATED.md`.
- `Updates/ISSUE_REGISTRY.md` replaced with a deprecation pointer to JSON sources.
