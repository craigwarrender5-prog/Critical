# CRITICAL SIMULATOR CONSTITUTION

## Version 0.1.0.0

### Binding Governance Framework

**Effective Immediately**

---

## ARTICLE I — PURPOSE

The Critical Simulator shall be developed as a controlled engineering system governed by:

- Physics integrity
- Conservation law compliance
- Architectural clarity
- Explicit system boundaries
- Validation discipline
- Structured, traceable evolution

All development shall proceed under this Constitution. No undocumented modification is permitted.

---

## ARTICLE II — VERSIONING PROTOCOL

### Section 1 — Version Format

The simulator shall use:

```
vMAJOR.MINOR.PATCH.REVISION
```

Example: `v0.2.5.1`

### Section 2 — Field Definitions

**MAJOR**
Architectural epoch.
Incremented only for fundamental generation changes.
When incremented:

```
MAJOR += 1
MINOR = 0
PATCH = 0
REVISION = 0
```

**MINOR**
Capability milestone.
Incremented when a defined simulator milestone is achieved (e.g., stable Mode 3 startup, validated full energy accounting, major subsystem completion).
When incremented:

```
MINOR += 1
PATCH = 0
REVISION = 0
```

**PATCH**
Structured release increment.
Incremented when an Implementation Plan completes:

- Exit criteria satisfied
- Validation passed
- Issues resolved or formally deferred
- Changelog written

When incremented:

```
PATCH += 1
REVISION = 0
```

**REVISION**
Hotfix release.
Incremented only when:

- A post-release corrective fix is required
- Scope is minimal and surgical
- No architectural expansion occurs
- Validation performed
- Changelog written

When incremented:

```
REVISION += 1
```

Severity does not determine version digit. Scope and structural impact determine version digit.

Failed validation prohibits version increment.

---

## ARTICLE III — ISSUE REGISTRY AUTHORITY

The file:

```
Critical/Updates/ISSUE_REGISTRY.md
```

is the single source of truth.

No issue may exist outside this registry.

### Section 1 — Mandatory Issue Fields

Each issue must include:

- Issue ID (CS-XXXX)
- Title
- Severity (Critical / High / Medium / Low)
- Status
- Detected In Version
- System Area
- Discipline
- Operational Impact
- Physics Integrity Impact
- Root Cause Status
- Assigned Implementation Plan
- Validation Outcome
- Related Issues

### Section 2 — Valid Status States

Only the following are permitted:

- Open
- Assigned
- In Progress
- Validating
- Closed
- Reopened
- Deferred
- Superseded

No state skipping allowed.

---

## ARTICLE IV — ISSUE LIFECYCLE

### Section 1 — Valid Transitions

- Open → Assigned
- Assigned → In Progress
- In Progress → Validating
- Validating → Closed (Pass)
- Validating → Reopened (Fail — Same Issue)
- Closed → Reopened (with evidence)
- Any → Deferred (with justification)
- Any → Superseded (must reference replacement issue)

### Section 2 — Validation Outcomes

Each validation must record:

- Not Tested
- Pass
- Fail — Same Issue
- Fail — New Issue Identified
- Fail — Regression Introduced

### Section 3 — Validation Failure Handling

**Same Issue Persists**
Status → Reopened
No version increment

**New Issue Identified**
New Issue ID required
Cross-reference issues
No version increment until exit criteria met

**Regression Introduced**
New Issue ID required
Must reference originating Plan
No version increment until resolved

---

## ARTICLE V — IMPLEMENTATION PLANS

Implementation Plans are structured work containers.

Stored in:

```
Critical/Updates/Implementation_Plans/
```

### Section 1 — Domain-Centric Model

At any given time, only one active Implementation Plan may exist per architectural domain.

A domain is defined by:

- System Area
- Discipline
- Architectural Boundary

Parallel plans for the same domain are prohibited.

### Section 2 — Plan Requirements

Each plan must include:

- Purpose
- Architectural Domain Definition
- Issues Assigned
- Plan Severity
- Scope
- Dependencies
- Exit Criteria
- Known Limitations

### Section 3 — Plan Severity

Each Plan must declare:

```
Plan Severity: Critical / High / Medium / Low
```

Default rule:

Plan Severity = Highest Severity of any assigned issue.

Plan Severity must be recalculated whenever:

- An issue is added
- An issue is deferred
- An issue is closed

### Section 4 — Plan Sequencing

Plan implementation order is determined by:

1. Plan Severity
2. Architectural dependency
3. System stability risk
4. Logical progression

Creation order does not determine execution order.

### Section 5 — Deferral Rule

If during execution:

- An issue blocks another
- Scope becomes excessive
- Architectural conflict emerges

Then:

- The issue may be Deferred
- A new Implementation Plan must be created
- Assigned Implementation Plan field updated
- Deferral reason documented
- Severity reassessed

The receiving Plan inherits the issue's severity.

Plans may close with deferred issues only if:

- Exit criteria met
- Deferred issues reassigned
- Registry updated

### Section 6 — Plan Closure Requirements

A Plan may close only when:

- All issues are Closed or Deferred
- Exit criteria validated
- Changelog written
- Version increment performed

---

## ARTICLE VI — CHANGELOG GOVERNANCE

Changelogs stored in:

```
Critical/Updates/Changelogs/
```

Each release must include:

- Version number
- Implementation Plan ID
- Issues Closed
- Validation summary

No release exists without a changelog.

---

## ARTICLE VII — ARCHITECTURAL DISCIPLINE

The simulator must maintain:

- Explicit mass boundaries
- Explicit energy boundaries
- No hidden state transitions
- No undocumented physics shortcuts
- No threshold-only physical behavior without documentation

All fidelity-impacting approximations must be logged as issues.

---

## ARTICLE VIII — CLAUDE OPERATING CONSTRAINTS

Claude shall:

- Not implement outside an assigned Issue and Plan
- Not close issues without validation evidence
- Not merge unrelated issues
- Not introduce architectural changes without logging an Issue
- Not increment versions outside Article II rules
- Implement one stage at a time
- Confirm before proceeding to next stage

Claude is an implementation agent. The Constitution is governing authority.

---

## ARTICLE IX — NON-SILENT FAILURE PRINCIPLE

Any anomaly must result in:

- Issue entry
- Root Cause Status assignment
- Severity assignment

No silent fixes permitted.

---

## ARTICLE X — AMENDMENT AUTHORITY

This Constitution may be modified only by full replacement with a new version number.

Partial edits are prohibited.

---

## FINAL DIRECTIVE

Governance prevails over urgency.
Traceability prevails over speed.
Validation prevails over assumption.

All development shall proceed under this Constitution.
