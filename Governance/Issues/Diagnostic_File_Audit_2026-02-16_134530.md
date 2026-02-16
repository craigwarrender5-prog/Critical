# Diagnostic-Only File Audit (2026-02-16_134530)

Scope:
- Repository-wide diagnostic-only inventory.
- Excluded: Build/HeatupLogs as requested.
- Full per-file table: Governance/Issues/Diagnostic_File_Audit_2026-02-16_134530.csv

Method:
- Classified diagnostic-only files by path and filename patterns (logs, investigation/evidence artifacts, editor runners).
- Marked files as `referenced` when their paths are cited in governance/documentation text.
- Recommended `KEEP` for referenced evidence, `ARCHIVE_CANDIDATE` for unreferenced governance artifacts, and `DELETE_CANDIDATE` for unreferenced runtime logs/deprecated archives.

## Summary
- Total diagnostic-only candidates: **3045**
- KEEP: **960**
- ARCHIVE_CANDIDATE: **69**
- DELETE_CANDIDATE: **2016**

### By Category
| Category | Count |
|---|---:|
| Runtime log artifact | 2784 |
| Legacy deprecated archive | 142 |
| Governance investigation/evidence | 75 |
| Untracked runtime log artifact | 13 |
| Implementation closeout report | 11 |
| Editor diagnostic harness | 10 |
| Editor diagnostic harness metadata | 10 |

## High-Confidence Deletion Buckets
| Bucket | File Count | Recommendation |
|---|---:|---|
| IP-0019_Extended_20260215_072522 | 209 | Delete after spot-check |
| IP-0019_Extended_20260215_065115 | 152 | Delete after spot-check |
| IP-0019_Extended_20260215_075811 | 149 | Delete after spot-check |
| IP-0019_Extended_20260215_075606 | 149 | Delete after spot-check |
| IP-0019_Extended_20260215_082537 | 149 | Delete after spot-check |
| IP-0019_Extended_20260215_084446 | 149 | Delete after spot-check |
| IP-0019_Extended_20260215_083746 | 149 | Delete after spot-check |
| IP-0019_Extended_20260215_073711 | 149 | Delete after spot-check |
| IP-0019_Extended_20260215_073559 | 149 | Delete after spot-check |
| IP-0019_Extended_20260215_075019 | 149 | Delete after spot-check |
| IP-0019_Extended_20260215_075440 | 149 | Delete after spot-check |
| IP-0019_Extended_20260215_081706 | 145 | Delete after spot-check |
| Updates/Archive_DEPRECATED | 142 | Delete after spot-check |
| IP-0019_Extended_20260215_073439 | 14 | Delete after spot-check |

## Key Findings
1. HeatupLogs is the dominant diagnostic footprint. Large portions are unreferenced by governance/docs and appear to be superseded reruns.
2. Many Governance/Issues artifacts are unreferenced by active plans/changelogs; these are safer to archive than hard-delete.
3. Updates/Archive_DEPRECATED has minimal active linkage and is a strong deletion candidate if external backups exist.
4. Legacy IP-specific editor runners are diagnostic-only; keep only currently useful runners (notably PzrBubbleInvestigationRunner.cs).

## Recommended Deletion Order
1. Delete untracked HeatupLogs generated in current workspace.
2. Delete tracked HeatupLogs rows marked DELETE_CANDIDATE in the CSV.
3. Delete Updates/Archive_DEPRECATED if historical backup exists externally.
4. Move ARCHIVE_CANDIDATE items to a dedicated archive branch/folder before considering deletion.
