# Registry Audit Report
Date: 2026-02-15
Scope: Governance artifacts only (`issue_index.json`, `issue_register.json`, `issue_archive.json`, and DP markdown files)

## Single Source of Truth Declaration
- `issue_index.json` is the authoritative full lifecycle registry (OPEN + CLOSED history).
- `issue_register.json` is the active-only working set (non-CLOSED issues only).
- `issue_archive.json` is the closed snapshot store and SHALL contain all CLOSED entries represented in `issue_index.json`.

## Totals
- Total issues (issue_index.json): 71
- Active issue count (status != CLOSED): 38
- Closed issue count (status == CLOSED): 33
- Active register issue count (issue_register.json): 38
- active_issue_count field value: 38
- archive_issue_count field value: 28

## Active Register Purity
- CLOSED issues present in active register: 0
- Purity status: PASS

## Orphan Detection
- Issues without assigned_dp: 0
- Issues assigned to non-existent DP: 0

## Register/Index/Archive Consistency
- Active set parity (`issue_register.json` vs `issue_index.json` ACTIVE entries): PASS
- Archive parity (`issue_archive.json` vs `issue_index.json` CLOSED entries): FAIL
- Missing archive snapshots for CLOSED index entries: 5
  - CS-0009
  - CS-0017
  - CS-0019
  - CS-0020
  - CS-0054

## Final Registry Integrity State
**REGISTRY_INCONSISTENT_ARCHIVE_GAP**

REGISTRY_INCONSISTENT_ARCHIVE_GAP
