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

1. **Master Issue Register (single source of truth for issues)**

   * Location: `Updates/ISSUE_REGISTRY.md`

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
3. Register issue in `Updates/ISSUE_REGISTRY.md`
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

## Article VI - Master Issue Register Governance

### Section 1 - Authority

`Updates/ISSUE_REGISTRY.md` is the single authoritative issue list.
No issue may exist outside this register.

### Section 2 - Issue Identifier and Severity

* Issue ID format: `CS-XXXX`
* Severity values are restricted to:

  * `Critical`
  * `High`
  * `Medium`
  * `Low`

### Section 3 - Required Issue Fields

Each issue record SHALL include:

* Issue ID
* Title
* Severity
* Status
* Investigation Evidence Location (standalone report path OR DP entry reference)
* **Domain (Canonical)**
* **Subdomain (Freeform detail tag)**
* Assigned DP ID
* Assigned IP ID (blank until IP is created)
* Detected in Version (if known)
* Validation Outcome
* Deferral/Supersession Reason (if applicable)
* Blocking Dependency (if applicable)

### Section 4 - Canonical Domain Types (CONTROLLED VOCABULARY)

The ONLY permitted values for `ISSUE_REGISTRY.Domain` and DP/IP domain titles are:

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

**Agent restriction (non-negotiable):**

* Agents MUST NOT invent, rename, split, merge, infer, or extend Domain Types.
* Any domain not in the list above is invalid.

### Section 5 - Subdomain Rule

* `ISSUE_REGISTRY.Subdomain` MAY be any descriptive text and SHOULD preserve prior detailed classification.
* Recommended format: `<Canonical Domain> — <Area>`.

### Section 6 - Issue-to-Domain Assignment Rule (NO GUESSING)

* Every CS issue MUST be assigned to exactly one canonical Domain and therefore exactly one DP.
* Agents MUST choose from the fixed Domain list only.
* If an issue cannot be mapped with high confidence using existing issue text/evidence, the agent MUST:

  1. Leave Domain blank,
  2. Leave Assigned DP blank,
  3. Flag the issue in a report as requiring user assignment.
     Agents MUST NOT guess.

### Section 7 - Issue Status Model

Allowed Issue statuses:

* `Registered`
* `Assigned`
* `In Progress`
* `Closed`
* `Deferred`
* `Superseded`

Allowed transitions:

* `Registered -> Assigned`
* `Assigned -> In Progress`
* `In Progress -> Closed`
* `Registered -> Deferred` (exception, Article IX)
* `Assigned -> Deferred` (exception, Article IX)
* `In Progress -> Deferred` (exception, Article IX)
* `Registered -> Superseded` (exception, Article IX)
* `Assigned -> Superseded` (exception, Article IX)
* `In Progress -> Superseded` (exception, Article IX)

No other transitions are allowed.

---

## Article VII - Domain Plans (DP)

### Section 1 - Role

A Domain Plan (`DP-XXXX`) is a domain-level container that groups related issues under ONE canonical Domain Type.
DPs organize scope and execution readiness; they are not release artifacts.

### Section 2 - DP Construction Rule (HARD)

* A DP MUST correspond to exactly one Canonical Domain Type (Article VI Section 4).
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

Failure outcomes SHALL be registered in the Master Issue Register before closure.

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
2. Severity and ordering MUST be sourced from ISSUE_REGISTRY.md.
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
