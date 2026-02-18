# CS-0124 Investigation Report

**Issue ID:** CS-0124  
**Title:** Large file refactoring analysis - multiple files exceed 50KB threshold  
**Domain:** Project Governance  
**Severity:** MEDIUM  
**Status:** READY (investigation complete)  
**Created:** 2026-02-18T12:00:00Z  
**Assigned DP:** DP-0010 (Project Governance)  
**Assigned IP:** IP-0055

---

## A) Observed Symptoms

Multiple source files have grown beyond 50KB since the last refactoring effort (IP-0047, closed 2026-02-17). This indicates substantial codebase growth requiring assessment for GOLD standard compliance and separation of concerns.

### Files Over 50KB (Current State)

| File | Size | Location | Current Partials | Assessment |
|------|------|----------|------------------|------------|
| MultiScreenBuilder.cs | 215.3 KB | UI | None | **REFACTOR CANDIDATE** |
| HeatupSimEngine.cs | 173.0 KB | Validation | 8 partials exist | **REVIEW NEEDED** |
| SGMultiNodeThermal.cs | 136.3 KB | Physics | None | **REFACTOR CANDIDATE** |
| HeatupSimEngine.BubbleFormation.cs | 97.6 KB | Validation | Is a partial | **REFACTOR CANDIDATE** |
| HeatupSimEngine.Logging.cs | 89.8 KB | Validation | Is a partial | **REFACTOR CANDIDATE** |
| CVCSController.cs | 73.7 KB | Physics | None | **REFACTOR CANDIDATE** |
| SolidPlantPressure.cs | 55.9 KB | Physics | None | **REFACTOR CANDIDATE** |
| OperatorScreenBuilder.cs | 51.2 KB | UI | None | **MONITOR** |

### Total Impact
- **8 files** exceed 50KB threshold
- **3 files** exceed 100KB (critical)
- Combined size of oversized files: ~853 KB

---

## B) Reproduction Steps

1. Run file size analysis on `Assets/Scripts/` directory
2. Sort by size descending
3. Identify all files > 50KB
4. Cross-reference with existing partial class structures

---

## C) Root Cause Analysis

### Hypothesis
Post-IP-0047 development has added substantial functionality without corresponding refactoring:
- v5.0.0 three-regime SG thermal model added significant code to SGMultiNodeThermal.cs
- Bubble formation logic expanded during pressurizer remodel
- UI builders accumulated screen-specific code without extraction
- CVCS controller grew with multiple operational modes

### Confirmed Factors
1. **SGMultiNodeThermal.cs (136.3 KB)**: Three-regime model implementation added ~80KB of physics code including subcooled, boiling, and steam dump regimes
2. **MultiScreenBuilder.cs (215.3 KB)**: Editor tool with 9 screen builders - each screen adds ~20KB of layout code
3. **CVCSController.cs (73.7 KB)**: Multiple heater modes, seal flow, letdown path logic accumulated
4. **Partial files growing**: Even existing partials (BubbleFormation, Logging) have grown beyond recommended limits

---

## D) Proposed Fix Options

### Option A: Partial Class Extraction (Recommended)
Extract logical subsystems into partial classes following established patterns:

**SGMultiNodeThermal.cs** → Split into:
- `SGMultiNodeThermal.cs` - Core state, regime enum, public API
- `SGMultiNodeThermal.Subcooled.cs` - Regime 1 thermocline model
- `SGMultiNodeThermal.Boiling.cs` - Regime 2 open-system model
- `SGMultiNodeThermal.SteamDump.cs` - Regime 3 pressure control
- `SGMultiNodeThermal.Diagnostics.cs` - Forensics and validation

**MultiScreenBuilder.cs** → Split into:
- `MultiScreenBuilder.cs` - Core canvas, shared utilities
- `MultiScreenBuilder.ReactorScreen.cs` - Screen 1
- `MultiScreenBuilder.RCSScreen.cs` - Screen 2
- `MultiScreenBuilder.PressurizerScreen.cs` - Screen 3
- (etc. for each screen)

**CVCSController.cs** → Split into:
- `CVCSController.cs` - Core state, enums, public API
- `CVCSController.Heaters.cs` - Heater mode logic
- `CVCSController.Letdown.cs` - Letdown path selection
- `CVCSController.SealFlow.cs` - Seal injection accounting

### Option B: Module Extraction
Extract entirely separate classes for distinct responsibilities. Higher risk of breaking changes.

### Option C: Defer with Documented Waiver
Document current state as acceptable with waiver. Not recommended given magnitude.

---

## E) Recommended Fix

**Option A: Partial Class Extraction**

Rationale:
- Maintains GOLD standard compliance
- Follows established project patterns (HeatupSimEngine partials)
- Preserves API surface - no breaking changes
- Improves maintainability and code navigation
- Reduces cognitive load when editing specific subsystems

---

## F) Risk Assessment

### Affected Systems
- Physics/SGMultiNodeThermal.cs - GOLD module, requires careful extraction
- Physics/CVCSController.cs - GOLD module, controller logic
- Physics/SolidPlantPressure.cs - GOLD module, solid-plant physics
- UI/MultiScreenBuilder.cs - Editor tool, lower risk
- Validation/* - Partial classes, moderate risk

### Risks
1. **Merge conflicts**: Active development may conflict with refactoring
2. **Partial class ordering**: Unity/C# partial class compilation order
3. **Field visibility**: Private fields may need internal visibility for partials
4. **Test coverage**: Existing tests must pass after refactoring

### Mitigations
- Execute during stable development window
- Validate compilation after each file extraction
- Run full acceptance test suite post-refactoring
- Maintain git commits per-file for easy rollback

---

## G) Validation Method

1. **Pre-refactoring**: Document file sizes, run acceptance tests
2. **Post-refactoring**: 
   - All files < 50KB (or documented waiver)
   - Zero compilation errors
   - Acceptance tests pass
   - No behavioral changes (physics outputs identical)
3. **GOLD compliance check**: Header metadata present in all new partials

---

## H) Dependencies

- None blocking - this is a code hygiene improvement
- Should be scheduled during stable development window
- Consider executing after current active development completes

---

## I) Investigation Conclusion

**Status:** READY (investigation complete)  
**Proposed Domain:** DP-0010 (Project Governance)  
**Assigned IP:** IP-0055
**Estimated Effort:** Medium (2-3 implementation stages)

---

## J) Notes

- Prior CS-0063 addressed similar concerns but was closed 2026-02-17
- Codebase has grown substantially since that closure
- This CS addresses the new growth requiring fresh assessment
- Assigned permanent ID CS-0124 after issue-register integrity audit
- Investigation artifact migrated to IP-0055 folder per CS-0126 governance restructure
