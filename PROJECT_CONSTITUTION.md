# CRITICAL SIMULATOR CONSTITUTION

## Version 1.8.0.0

### Binding Governance Framework

**Effective Date: 2026-02-18**

---

## Article I - Authority and Purpose

This Constitution is the governing authority for project execution, documentation, architecture, and release control.

All project work SHALL prioritize:

* Physics integrity
* Conservation-law compliance
* Architectural boundaries
* Traceability
* Deterministic governance
* Validation before release

This document is a full replacement constitution. Prior constitutions are superseded in full.

---

## Article II - Normative Language

The following terms are normative:

* **MUST / SHALL**: mandatory requirement
* **SHOULD**: recommended requirement unless justified otherwise
* **MAY**: optional behavior

If any conflict exists between convenience and governance, governance prevails.
No implicit exceptions are permitted unless explicitly written in this constitution.

---

## Article III - Canonical Artifacts and Locations

The following artifacts are mandatory and authoritative at these repository-relative paths:

1. **Issue Governance Register (single source of truth for issues)**

   * Active issues location: `Governance/IssueRegister/issue_register.json`
   * Closed issues location: `Governance/IssueRegister/issue_archive.json`
   * Search index location: `Governance/IssueRegister/issue_index.json`
   * Schemas: `Governance/IssueRegister/issue_register.schema.json`, `Governance/IssueRegister/issue_archive.schema.json`

2. **Domain Plans (DP)**

   * Active and persistent location: `Governance/DomainPlans/`
   * Archival location for formally deprecated or merged domains only: `Governance/DomainPlans/Closed/`
   * Naming pattern: `DP-XXXX - <Canonical Domain>.md`

3. **Implementation Plans (IP)**

   * Active bundle location: `Governance/ImplementationPlans/IP-XXXX <Domain Name>/`
   * Closed bundle location: `Governance/ImplementationPlans/Closed/IP-XXXX <Domain Name>/`
   * Required controlling plan file: `IP-XXXX.md`

4. **Governance and roadmap indexes**

   * Roadmap location: `Governance/DP_EXECUTION_RECOMMENDATION.md`
   * DP registry index location: `Governance/DP_REGISTRY_CONSISTENCY_REPORT.md`

5. **Investigation artifacts**

   * Pre-IP investigation location: `Governance/Issues/CS-XXXX/`
   * Investigation report naming inside CS folder: `Investigation_Report_YYYY-MM-DD_HHMMSS.md`
   * When a CS is assigned to an active IP: CS folder location is `Governance/ImplementationPlans/IP-XXXX <Domain Name>/CS-XXXX/`
   * When an IP closes: CS folders move with the IP bundle to `Governance/ImplementationPlans/Closed/IP-XXXX <Domain Name>/CS-XXXX/`
   * Legacy flat investigation files under `Governance/Issues/` are grandfathered and not back-migrated by default (forward-only migration policy).

No authoritative process record SHALL be maintained outside these locations.

---

## Article IV - Absolute CS Gate (No Change Without CS)

1. No modification SHALL be made to:

   * Code
   * Assets
   * Documentation
   * Configuration
   * Architecture
   * Tuning/parameters

   without a corresponding CS item in the issue register.

2. If a change is detected without a CS:

   * Execution MUST halt immediately.
   * A corrective CS MUST be created before proceeding.

3. No implicit exceptions are permitted.

---

## Article V - Mandatory CS Lifecycle

### Section 1 - Lifecycle States

The canonical CS lifecycle states are:

* `OPEN`
* `INVESTIGATING`
* `READY`
* `BLOCKED`
* `DEFERRED`
* `CLOSED`

### Section 2 - Required Lifecycle Flow

1. Upon CS creation, status MUST be `INVESTIGATING`.
2. A CS MUST NOT transition to `READY` until investigation is complete.
3. A CS in `INVESTIGATING` MUST NOT be referenced by an Implementation Plan.
4. `CLOSED` status MUST only be applied through formal closure workflow and archive migration.

### Section 3 - Transition Constraints

Allowed transition families:

* `INVESTIGATING -> READY`
* `READY -> BLOCKED`
* `BLOCKED -> READY`
* `READY -> DEFERRED`
* `DEFERRED -> READY`
* `READY -> CLOSED`

Direct transition to `CLOSED` from `INVESTIGATING` is prohibited.

---

## Article VI - Mandatory CS Investigation Phase

Investigation is mandatory for every CS and SHALL be completed before execution planning.

Every investigation record MUST document:

* Observed symptoms
* Reproduction steps (if applicable)
* Root cause analysis (confirmed or hypothesis)
* Proposed fix options
* Recommended fix
* Risk assessment (affected systems/domains)
* Validation method (how success will be proven)

Domain Plan assignment may occur only after investigation is complete.
Only after investigation completion may the CS transition to `READY`.

---

## Article VII - Domain Plan Ownership and Persistent Domain Model

### Section 1 - CS Ownership

1. Every CS MUST belong to exactly ONE Domain Plan.
2. A CS MAY be unassigned only while status is `INVESTIGATING`.
3. Domain assignment MUST be justified by investigation findings.

### Section 2 - Domain Plan Permanence

1. Domain Plans are permanent architectural governance units.
2. Domain Plans MUST NOT be archived solely because all contained CS items are closed.
3. Domain Plans MUST NOT be archived solely because an associated IP has completed.
4. Closing an IP does NOT close or archive a Domain Plan.

### Section 3 - Allowed Domain Plan Archival

A Domain Plan may be archived only if:

* The architectural domain is formally deprecated, merged, or removed, and
* A governance decision record documents the reason and effective date.

---

## Article VIII - Implementation Plan Requirements and Revision Control

### Section 1 - Mandatory IP Scope and Dependency Hierarchy

Every IP MUST include:

1. A complete list of included CS items.
2. A dependency hierarchy analysis that identifies:
   * prerequisite CS items,
   * blocking relationships,
   * shared files/systems,
   * interference risk between fixes.
3. A clearly defined execution order.
4. Explicit identification of critical-path CS items.

No IP may begin execution without documented dependency hierarchy.

### Section 2 - Mandatory Revision History

Every IP MUST contain a `Revision History` section.

Each revision entry MUST include:

* Revision number (for example: `v0.1`, `v1.0`, `v1.1`)
* Date
* Author (`Codex`, `Manual`, or `Governance`)
* Summary of changes
* Reason for amendment

Revision numbering rules:

* `v0.x` = Draft phase
* `v1.0` = Approved for execution
* `v1.x` = Amendments during execution
* `v2.0` = Major structural revision

Any amendment made after initial review, approval, or during execution MUST increment revision number.

Substantive amendments MUST update impacted sections:

* Dependency hierarchy
* Risk assessment
* Cross-domain documentation

IP execution MUST reference the latest revision number at start.
IP revision history governs planning evolution and is distinct from Git tagging.

---

## Article IX - Cross-Domain Inclusion Protocol

If a CS requires work that belongs to a different Domain Plan, a formal Cross-Domain Inclusion Request is mandatory.

The request MUST include:

* CS ID(s)
* Owning Domain Plan
* Target IP
* Justification for inclusion
* Minimal required scope
* Validation implications

Execution MUST NOT begin until explicit governance approval is recorded.

After approval:

* The CS may execute within the current IP.
* The CS retains original Domain ownership.
* Closeout documentation MUST reference the approval artifact.

Implicit cross-domain work is prohibited.

---

## Article X - CS Persistence and Reassignment Rule

1. If a CS is not implemented during an IP:

   * It MUST remain open (not closed).
   * It MUST remain assigned to its current Domain Plan.

2. A CS may be reassigned to another Domain Plan only if:

   * Investigation proves the original assignment was incorrect, or
   * Architectural restructuring formally justifies relocation.

3. A CS MUST NOT be:

   * Closed administratively without closure evidence,
   * Removed from its Domain Plan without justification,
   * Silently abandoned.

4. If an IP completes without implementing an included CS, the CS MUST explicitly be set to one of:

   * `DEFERRED`, or
   * `BLOCKED`, or
   * `READY`

   with a documented reason.

5. Closing an IP does NOT alter the lifecycle state of unimplemented CS items.

---

## Article XI - Issue Register Determinism

### Section 1 - Active Register Status Contract

`Governance/IssueRegister/issue_register.json` is authoritative for active CS records.

Allowed active statuses are strictly:

* `OPEN`
* `INVESTIGATING`
* `READY`
* `BLOCKED`
* `DEFERRED`

`READY_FOR_FIX` is retired and SHALL NOT be used in new records.

### Section 2 - Required Active Fields

Each active CS entry MUST include:

* `id`
* `title`
* `domain`
* `severity`
* `status`
* `created_at`
* `updated_at`
* `stage_detected`
* `assigned_dp`
* `assigned_ip`

### Section 3 - Cross-Artifact Consistency

Every register mutation MUST update:

* `issue_register.json`
* `issue_index.json`
* Counts and timestamps relevant to those files

Schema validation MUST pass for active/archive JSON artifacts.

---

## Article XII - Validation, Closure, and Release

### Section 1 - Validation and Closure Preconditions

1. Validation evidence is mandatory before CS closure.
2. CS closure requires migration from active register to archive snapshot.
3. IP closure does not change Domain Plan persistence requirements in Article VII.

### Section 2 - Mandatory Changelog Initiation at IP Closure

1. Every IP closure SHALL initiate a changelog entry immediately.
2. The changelog entry MUST capture the sum total of implemented change in that IP closure.
3. The changelog entry MUST include:
   * IP ID,
   * included CS IDs and final dispositions,
   * scope summary,
   * impact classification rationale (`MAJOR`, `MINOR`, `PATCH`, or `REVISION`).
4. Release/versioning occurs only after validated closure and changelog update.

### Section 3 - Version Increment Classification Rules

The project version SHALL use a four-part format:

`MAJOR.MINOR.PATCH.REVISION`

Classification SHALL be based on total impact of the closed IP change set:

1. `MAJOR`:
   * Breaking architecture or governance framework reset,
   * backward-incompatible behavior contracts,
   * structural platform-level replacement.
   * Increment rule: `MAJOR +1`, set `MINOR=0`, `PATCH=0`, `REVISION=0`.
2. `MINOR`:
   * Material new capability or cross-system feature expansion without platform break,
   * substantial behavior extension across one or more domains.
   * Increment rule: `MINOR +1`, set `PATCH=0`, `REVISION=0`.
3. `PATCH`:
   * Defect remediation, tuning correction, bounded behavioral fix, or non-breaking implementation hardening.
   * Increment rule: `PATCH +1`, set `REVISION=0`.
4. `REVISION`:
   * Governance/reporting/documentation-only updates with no intended runtime behavior change.
   * Increment rule: `REVISION +1`.

Classification precedence MUST be deterministic: choose the highest-impact class present in the total closed-IP change set.

---

## Article XIII - Amendment Rule

This Constitution may be changed only by full-document replacement with a new constitution version.
Partial amendments are not permitted.

---

## Migration Requirement (v1.8.0.0)

Upon constitution upgrade:

1. Active lifecycle status terminology MUST use `READY` instead of `READY_FOR_FIX`.
2. All new CS creation flows MUST begin in `INVESTIGATING`.
3. Domain Plans MUST be treated as persistent architecture units, not auto-archived execution containers.
4. Every active and future IP MUST include dependency hierarchy and revision-history sections.
5. Cross-domain execution MUST use explicit inclusion request and approval artifacts.
6. IP closure workflow MUST initiate a changelog entry in the same closure transaction.
7. Version increment selection MUST follow Article XII Section 3 with explicit rationale.
8. Investigation artifact governance SHALL use CS-folder hierarchy for all newly created or newly assigned CS items.
9. Migration policy is forward-only by default: existing legacy flat investigation files are preserved unless a specific migration CS/IP authorizes back-migration.

---

## Deterministic Execution Directive

Codex SHALL execute governance in the following mandatory order:

1. Ensure CS exists for intended change.
2. Create/update CS in `INVESTIGATING` state.
3. Complete investigation record.
4. Assign Domain Plan based on investigation.
5. Transition CS to `READY`.
6. Build or amend IP with dependency hierarchy and revision entry.
7. Execute only latest approved IP revision.
8. Validate outcomes and update lifecycle state deterministically.

No step may be bypassed.

---

## Final Directive

Observation precedes assumption.
Investigation precedes readiness.
Governance precedes implementation.
Validation precedes closure.
Changelog precedes version assignment.

All project work SHALL proceed under this Constitution.
