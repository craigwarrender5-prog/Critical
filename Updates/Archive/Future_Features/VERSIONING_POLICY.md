# Versioning Policy — Critical Simulator
## Critical: Master the Atom -- NSSS Simulator

**Effective:** 2026-02-13 (applies to all versions after v5.4.1)
**Governing Document:** `PROJECT_CONSTITUTION.md`

---

## Version Format

The Critical Simulator uses a four-level structured versioning system:

```
Major.Minor.Patch.Revision
```

Example: `5.4.2.1`

---

## Version Level Definitions

### X.0.0.0 — Major Release

Structural or architectural change that alters core modeling behavior or simulation paradigm.

**Examples:**
- Thermal engine rewrite
- Flow solver redesign
- Transition to multicore deterministic execution
- Breaking changes to physics boundary definitions

Major releases may alter baseline simulation behavior.

### 0.X.0.0 — Minor Release

Introduction of a new subsystem, feature, or significant modeling domain expansion without breaking existing architecture.

**Examples:**
- Steam Generator pressure boundary implementation
- Turbine/condenser system addition
- Secondary loop completion
- New plant system integration

Minor releases expand capabilities.

### 0.0.X.0 — Patch

Corrections, repairs, or fidelity improvements within existing scope.

**Examples:**
- Mass conservation tightening
- Transient correction
- Regime logic fix
- Constant recalibration

Patch releases may include small feature additions within an already-established domain, provided they do not introduce a new subsystem boundary. Patches must not introduce new feature scope beyond the domain established by the parent Minor release.

### 0.0.0.X — Revision

Adjustment to an existing patch or feature to correct behavior so that it aligns with intended design, without expanding scope.

**Examples:**
- Tolerance adjustment
- Edge case correction
- Logging addition
- Minor numerical stabilization

Revisions must not change architectural boundaries.

---

## Optional Status Tag (Non-Versioned)

To indicate maturity state, an optional tag may be appended:

- `Stable`
- `Validation`
- `Experimental`
- `Research`

Example: `5.4.2.1 (Validation)`

Status tags do not alter version hierarchy.

---

## Governance Rules

1. Version increments must match scope level.
2. Minor or Major versions may not be created without explicit roadmap approval.
3. Patch and Revision releases must not introduce new subsystems.
4. Version numbers must never be auto-incremented without defined scope.
5. Documentation must reflect current active version baseline.

---

## Adoption Notice

This policy is effective for all versions **after v5.4.1**. Historical versions (v5.4.1 and earlier) used three-level semantic versioning and are not retroactively amended. The first version issued under this policy will use the four-level format.

### Transition Example

| Historical (3-level) | New Policy (4-level) | Notes |
|----------------------|---------------------|-------|
| v5.4.1 | v5.4.1 | Last 3-level version (current baseline) |
| v5.4.2 (planned) | v5.4.2.0 | First version under 4-level policy |
| — | v5.4.2.1 | Hypothetical revision to v5.4.2.0 |

---

## Policy Ensures

- Clear scope tracking
- Prevention of version inflation
- Architectural governance discipline
- Long-term roadmap integrity

---

*Policy established 2026-02-13.*
