# CRITICAL SIMULATOR CONSTITUTION

## Version 1.3.1.0

### Binding Governance Framework

**Effective Date: 2026-02-14**

---

## Article I - Authority and Purpose

This Constitution is the governing authority for project execution, documentation, and release control.

All project work SHALL prioritize:

* Physics integrity
* Conservation-law compliance
* Architectural boundaries
* Traceability
* Verification before release

This document is a full replacement constitution. Prior constitutions are superseded in full.

---

## Article II - Normative Language

The following terms are normative:

* **MUST / SHALL**: mandatory requirement
* **SHOULD**: recommended requirement unless justified otherwise
* **MAY**: optional behavior

If any conflict exists between convenience and governance, governance prevails.

---

## Article III - Canonical Artifacts and Locations

The following artifacts are mandatory and authoritative at these repository-relative paths:

1. **Issue Governance Register (single source of truth for issues)**

   * Active issues location: `Governance/IssueRegister/issue_register.json`
   * Closed issues location: `Governance/IssueRegister/issue_archive.json`
   * Search index location: `Governance/IssueRegister/issue_index.json`
   * Schemas: `Governance/IssueRegister/issue_register.schema.json`, `Governance/IssueRegister/issue_archive.schema.json`
   * Legacy markdown issue lists are non-authoritative and MUST be marked deprecated.

2. **Domain Plans (DP)**

   * Location: `Updates/Implementation_Plans/`
   * Naming: `DP-XXXX - <Canonical Domain>.md`

3. **Implementation Plans (IP)**

   * Location: `Updates/Implementation_Plans/`
   * Naming: `IP-XXXX - <Canonical Domain>.md`

4. **Changelogs**

   * Location: `Updates/Changelogs/`
   * Naming: `CHANGELOG_vMAJOR.MINOR.PATCH.REVISION.md`

5. **Roadmap**

   * Location: `Updates/Future_Features/FUTURE_ENHANCEMENTS_ROADMAP.md`

Optional standalone investigation records MAY be maintained at:

* Location: `Updates/Issues/`
* Naming: `IR-XXXX - <Topic>.md` or `CS-XXXX_Investigation_Report.md`

Closed DP/IP records MAY be moved to `Updates/Implementation_Plans/Closed/` after closure.

No authoritative process record SHALL be maintained outside these locations.

---

## Article IV - End-to-End Lifecycle

The required lifecycle SHALL be:

1. Observation
2. Investigation and root-cause analysis (standalone report OR embedded DP issue entry)
3. Register issue in `Governance/IssueRegister/issue_register.json` and update `Governance/IssueRegister/issue_index.json`
4. Assign issue to a Domain Plan (`DP-XXXX`)
5. Authorize DP for execution
6. Create and execute an Implementation Plan (`IP-XXXX`) for that DP
7. Validate implementation outcomes
8. Record deferred work in a **new DP** when required
9. Write changelog
10. Determine release version at changelog time

No step may be skipped except as explicitly allowed by deferral/supersession rules in Article IX.

---

## Article V - Investigation Records (Pre-Issue / At Registration)

### Section 1 - Role

Investigation is mandatory, but a standalone Investigation Report per CS item is NOT required.

Investigation content MAY be documented in either:

1. A standalone investigation file in `Updates/Issues/`, OR
2. The corresponding Domain Plan issue entry (backlog row/notes).

Investigation content MUST exist before DP execution authorization for that issue.

### Section 2 - Required Contents

Each investigation record (standalone or embedded) SHALL include:

* Observation statement and reproducibility conditions
* Affected system boundary/domain
* Exact code path or runtime path traced
* Input -> transformation -> output trace
* Root-cause classification
* Evidence references (logs, telemetry, tests, screenshots as applicable)

### Section 3 - Investigation State Model

Allowed investigation states:

* `Preliminary`
* `Complete`
* `Archived`

Allowed transitions:

* `Preliminary -> Complete`
* `Complete -> Archived`
* `Complete -> Preliminary` (rework required)

State skipping is not allowed.

---

## Article VI - Issue Governance (JSON Register System)

### Section 1 - Authority and Single Source of Truth

The issue system is split by lifecycle state:

* Active (non-closed) issues: `Governance/IssueRegister/issue_register.json`
* Closed issue snapshots: `Governance/IssueRegister/issue_archive.json`
* Cross-set search index: `Governance/IssueRegister/issue_index.json`

Markdown issue lists are non-authoritative reference artifacts only and MUST NOT be used as the master record.

### Section 2 - Deterministic Identifiers

* Issue IDs MUST follow `CS-####`.
* Domain Plan IDs MUST follow `DP-###` (existing legacy `DP-####` IDs are accepted for compatibility).
* Implementation Plan IDs MUST follow `IP-####`.
* IDs are immutable and MUST NOT be reused.

### Section 3 - Hardcoded Domain Constraint (No Inference)

The only valid domain values are:

1. Primary Thermodynamics
2. Pressurizer & Two-Phase Physics
3. Steam Generator Secondary Physics
4. CVCS / Inventory Control
5. Mass & Energy Conservation
6. Plant Protection & Limits
7. Validation & Diagnostics
8. Operator Interface & Scenarios
9. Performance & Runtime
10. Project Governance
11. UNASSIGNED

Domain assignment MUST use this exact controlled list. Agents MUST NOT infer or invent new domains.
`UNASSIGNED` MAY be used only when assignment is genuinely unresolved and MUST be resolved during triage.

### Section 4 - Required Active Issue Fields

Each active issue entry in `issue_register.json` MUST include:

* `id`
* `title`
* `domain`
* `severity`
* `status` (must not be `CLOSED`)
* `created_at`
* `updated_at`
* `stage_detected` (null allowed when unknown)
* `assigned_dp` (string or null)
* `assigned_ip` (string or null)

Allowed active `severity` values:

* `CRITICAL`
* `HIGH`
* `MEDIUM`
* `LOW`

Allowed active `status` values:

* `OPEN`
* `INVESTIGATING`
* `BLOCKED`
* `DEFERRED`
* `READY_FOR_FIX`

Optional active fields are permitted, including:

* `observations`
* `evidence`
* `hypotheses`
* `root_cause`
* `resolution_candidate`
* `links`
* `tags`

### Section 5 - Required Closure Snapshot Fields

Each closed issue snapshot in `issue_archive.json` MUST include:

* `id`
* `title`
* `domain`
* `severity`
* `status` = `CLOSED`
* `created_at`
* `closed_at`
* `resolution_type`
* `fix_refs`
* `validation_refs`

Allowed `resolution_type` values:

* `FIXED`
* `WONT_FIX`
* `DUPLICATE`
* `INVALID`
* `CANT_REPRO`
* `DEFERRED_TO_NEW_ID`

If `resolution_type = FIXED`, `one_line_rca` is mandatory.

### Section 6 - Closure Minimization Requirement

When an issue transitions to `CLOSED`:

1. Remove the full issue object from `issue_register.json`.
2. Add a minimized closure snapshot to `issue_archive.json`.
3. Update `issue_index.json` to reflect closed state, `closed_at`, and `resolution_type`.

`issue_register.json` MUST NOT contain `CLOSED` issues.

### Section 7 - Operational Workflow Rules

Create:

* Add a full issue object to `issue_register.json`.
* Update `issue_index.json`.
* Set `status = OPEN` unless explicitly justified otherwise.

Update:

* Modify only `issue_register.json` for active issues.
* `updated_at` MUST change on each update.
* Evidence and observations SHOULD be append-only unless correcting factual errors.

Close:

* Follow Section 6 move/minimize procedure.
* Include closure references in archive (`fix_refs`, `validation_refs`).

Deferral to new issue:

* Close original with `resolution_type = DEFERRED_TO_NEW_ID` and `superseded_by`.
* New issue MUST include `supersedes` referencing the original.

### Section 8 - Schema Validation Requirement

Every change to issue JSON artifacts MUST validate against Draft 2020-12 schemas:

* `Governance/IssueRegister/issue_register.schema.json`
* `Governance/IssueRegister/issue_archive.schema.json`

Invalid JSON or schema violations are constitutional non-compliance.
---

## Article VII - Domain Plans (DP)

### Section 1 - Role

A Domain Plan (`DP-XXXX`) is a domain-level container that groups related issues under ONE canonical Domain Type.
DPs organize scope and execution readiness; they are not release artifacts.

### Section 2 - DP Construction Rule (HARD)

* A DP MUST correspond to exactly one Canonical Domain Type (Article VI Section 3).
* For each Canonical Domain Type, there SHOULD be exactly one Open DP at a time.
* Creating additional DPs within the same Domain is allowed ONLY for deferral/blocking reasons and MUST be justified.

### Section 3 - DP Status Model

Allowed DP statuses:

* `Open`
* `Executing`
* `Complete`

Allowed transitions:

* `Open -> Executing`
* `Executing -> Complete`

### Section 4 - DP Execution Rule

When a DP enters `Executing`, the agent MUST create an IP for that DP.
That IP MUST include all non-closed issues currently assigned to the DP at authorization time.

## Section 5 - REQUIRED DOMAIN PLAN CONTENTS (MANDATORY)

Every Domain Plan (DP-XXXX) MUST include the following sections:

### A) Domain Summary

* Canonical Domain name
* DP Status
* Total CS count in domain

### B) Severity Distribution

A table showing:

* Critical count
* High count
* Medium count
* Low count

This enables cross-domain execution prioritization without reading the full Issue Register.

### C) Ordered Issue Backlog

A table containing, for EVERY CS in the Domain:

* CS ID
* Title
* Severity
* Status
* Blocking Dependency (if any)
* Validation Outcome (if any)
* Investigation Evidence (one-line summary or standalone reference)

The table MUST be sorted by:

1. Blocking Critical
2. Non-blocking Critical
3. High
4. Medium
5. Low

Agents MUST NOT change this ordering rule.

### D) Execution Readiness Indicator

DP MUST state one of:

* **READY FOR AUTHORIZATION**
  (no blocking Critical issues unresolved outside domain)

* **BLOCKED**
  (list blocking CS IDs and external dependencies)

### E) Notes / Investigation Links

Optional references to:

* Standalone investigation reports (if used)
* Prior IPs
* Validation evidence

Failure to include ALL required sections renders a DP **non-compliant** and it MUST NOT be authorized for execution.

---

## Article VIII - Implementation Plans (IP)

### Section 1 - Role

An Implementation Plan (`IP-XXXX`) is the execution artifact for one executing DP.
An IP MUST reference exactly one DP.

### Section 2 - IP Status Model

Allowed IP statuses:

* `Draft`
* `Authorized`
* `Implemented`
* `Validated`
* `Closed`

Allowed transitions:

* `Draft -> Authorized`
* `Authorized -> Implemented`
* `Implemented -> Validated`
* `Validated -> Closed`
* `Validated -> Authorized` (rework cycle if validation fails)

### Section 3 - Closure Rule

IP closure is independent of release versioning.

---

## Article IX - Deferral and Supersession Rules

### Section 1 - Deferral During Execution

If an issue cannot be completed within an executing DP/IP, it MUST be deferred and moved to a **new DP ID**.
The register and plans SHALL record:

* Deferral reason
* Blocking dependency
* Originating DP/IP
* Receiving DP ID

### Section 2 - Supersession

An issue MAY be superseded only when replaced by one or more new issue IDs with clearer scope.
Supersession MUST include explicit replacement references.

### Section 3 - Roadmap Synchronization

Every deferred issue and new receiving DP MUST be reflected in:

* `Updates/Future_Features/FUTURE_ENHANCEMENTS_ROADMAP.md`

---

## Article X - Validation and Evidence

Before an IP may close, validation SHALL be recorded for each included issue.

Validation outcomes are:

* `Pass`
* `Fail - Same Issue`
* `Fail - New Issue`
* `Fail - Regression`

Failure outcomes SHALL be registered in the JSON issue governance system (`issue_register.json` / `issue_archive.json` / `issue_index.json`) before closure.

---

## Article XI - Release and Versioning

Release versioning occurs when a changelog is created after implementation and validation work is complete.

Version format:
`vMAJOR.MINOR.PATCH.REVISION`

---

## Article XII - Amendment Rule

This Constitution may be changed only by full-document replacement with a new constitution version.
Partial amendments are not permitted.

---

## Migration Requirement (v1.3.1.0)

Upon constitution upgrade:

1. All existing DP files MUST be regenerated to include the required sections above.
2. Severity and ordering MUST be sourced from `Governance/IssueRegister/issue_index.json` (authoritative), not markdown lists.
3. No Implementation Plans may be created until ALL DPs are compliant.
4. Standalone per-CS investigation files are optional; DP issue entries MAY carry investigation records.

---

## Final Directive

Observation precedes assumption.
Investigation precedes issue registration.
Issue governance precedes implementation.
Validation precedes release.
Changelog precedes version assignment.

All project work SHALL proceed under this Constitution.

