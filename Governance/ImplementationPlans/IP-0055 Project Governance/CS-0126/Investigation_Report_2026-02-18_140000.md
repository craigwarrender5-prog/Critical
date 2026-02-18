# CS-0126 Investigation Report

**Title:** Restructure governance documentation trail with hierarchical CS-folder organization under Implementation Plans

**Date:** 2026-02-18T14:00:00Z  
**Status:** READY (investigation complete)  
**Domain:** Project Governance  
**Severity:** MEDIUM  
**Investigator:** Codex  

---

## 1. Observed Symptoms / Request

User requested a change to centralize the documentation trail with a more logical folder hierarchy:

1. Current state: Investigation reports are flat files in `Governance/Issues/` directory
2. Proposed state: Each CS gets its own subfolder, which travels with its IP when assigned

---

## 2. Current State Analysis

### Current Structure
```
Governance/
├── Issues/
│   ├── CS-0098_Investigation_Report_2026-02-16_214500.md
│   ├── CS-0099_Investigation_Report_2026-02-17_124700.md
│   ├── ... (flat file structure, ~70+ files)
│   └── various IP checkpoint files mixed in
│
├── ImplementationPlans/
│   ├── Closed/
│   │   └── IP-XXXX/
│   │       └── IP-XXXX.md
│   ├── IP-0053/
│   │   └── (current structure)
│   └── *.md files (loose IP documents)
```

### Problems with Current Structure
1. **No physical co-location**: Investigation artifacts are separate from their IP execution context
2. **Mixed content**: `Governance/Issues/` contains both CS investigations AND IP checkpoint files
3. **Traceability gap**: When an IP closes, the related investigation artifacts don't travel with it
4. **Audit difficulty**: Finding all artifacts for a specific IP requires searching multiple locations

---

## 3. Proposed New Structure

### Phase 1: CS Creation & Investigation
```
Governance/Issues/CS-XXXX/
├── Investigation_Report_YYYY-MM-DD_HHMMSS.md
├── (supporting evidence: logs, screenshots, analysis)
└── (root cause analysis artifacts)
```

### Phase 2: Implementation Planning
When a Domain's IP is created, CS folders migrate into the IP structure:
```
Governance/ImplementationPlans/IP-XXXX Domain Name/
├── IP-XXXX.md                          (controlling plan)
├── CS-0001/
│   └── Investigation_Report_...md
├── CS-0002/
│   └── Investigation_Report_...md
└── CS-0003/
    └── Investigation_Report_...md
```

### Phase 3: IP Closure
When IP moves to Closed, all CS subfolders move with it:
```
Governance/ImplementationPlans/Closed/IP-XXXX Domain Name/
├── IP-XXXX.md
├── CS-0001/
│   └── Investigation_Report_...md
└── (complete archive bundle)
```

---

## 4. Lifecycle Rules

### CS Folder Lifecycle

| CS Status | Folder Location |
|-----------|-----------------|
| INVESTIGATING | `Governance/Issues/CS-XXXX/` |
| READY (unassigned to IP) | `Governance/Issues/CS-XXXX/` |
| READY (assigned to IP) | `Governance/ImplementationPlans/IP-XXXX Domain/CS-XXXX/` |
| BLOCKED | Remains in current location |
| DEFERRED | Remains in `Governance/Issues/CS-XXXX/` until future IP |
| CLOSED | `Governance/ImplementationPlans/Closed/IP-XXXX Domain/CS-XXXX/` |

### Key Rules

1. **CS folders for DEFERRED/BLOCKED items NOT in current IP**: Remain in `Governance/Issues/` until a future IP picks them up

2. **IP closure bundles everything**: When IP moves to Closed, all CS subfolders move with it as a complete archive

3. **Constitution update required**: Article III (Canonical Artifacts and Locations) must be updated to reflect this new structure

---

## 5. Migration Strategy

### Option A: Forward-Only Migration (RECOMMENDED)
- Apply new structure to all NEW CS items (CS-0126 onward)
- Existing flat files remain in place (historical)
- When creating new IPs, migrate only newly-created CS folders

### Option B: Full Migration
- Restructure all existing investigation files into CS folders
- Higher effort, higher risk
- Not recommended due to volume (~70+ files)

### Option C: Hybrid Migration
- Restructure active CS items only (CS-0102, 0103, 0113, 0114, 0118, 0120, 0121, 0124, 0125)
- Leave closed/archived items in place

**Recommendation:** Option A (Forward-Only) for immediate implementation, with Option C as follow-up if desired.

---

## 6. Constitution Amendment Required

Article III Section 5 currently states:
```
5. **Investigation artifacts**
   * Location: `Governance/Issues/`
   * Naming: `CS-XXXX_Investigation_Report_YYYY-MM-DD_HHMMSS.md`
```

Must be updated to:
```
5. **Investigation artifacts**
   * Active investigation location: `Governance/Issues/CS-XXXX/`
   * Naming within folder: `Investigation_Report_YYYY-MM-DD_HHMMSS.md`
   * When CS assigned to IP: Folder moves to `Governance/ImplementationPlans/IP-XXXX Domain/CS-XXXX/`
   * IP closure moves CS folders with IP bundle to `Governance/ImplementationPlans/Closed/`
```

---

## 7. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Path references in existing artifacts break | LOW | LOW | Forward-only migration avoids this |
| Confusion during transition period | MEDIUM | LOW | Clear documentation of new vs. old structure |
| Constitution version increment required | CERTAIN | LOW | Standard governance process |

---

## 8. Affected Systems/Domains

- **Primary:** DP-0010 (Project Governance)
- **Cross-Domain Impact:** All future Domain Plans and Implementation Plans will follow new structure

---

## 9. Validation Method

1. Create CS-0126 using new folder structure (this CS)
2. Verify folder hierarchy is correct
3. Update Constitution Article III
4. Document transition in Constitution migration requirements
5. Future IPs demonstrate CS folder migration on IP creation

---

## 10. Proposed Fix / Resolution Candidate

1. Update Constitution v1.7.0.0 → v1.8.0.0 with new Article III structure
2. Add migration requirements section for folder structure
3. Apply new structure starting with CS-0126
4. Document that existing flat files are grandfathered

---

## 11. Recommendation

**Proceed with implementation** using Forward-Only Migration (Option A).

This CS (CS-0126) serves as the first example of the new structure - its folder already exists at `Governance/Issues/CS-0126/`.
