# Conformance Audit Report (2026-02-15)

## Scope

This audit is documentation and implementation conformance only.  
No code changes were made during audit execution.

Baselines used:

- **Baseline A:** Real-world operational reference in `Technical_Documentation/`
- **Baseline B:** `Technical_Documentation/GOLD_STANDARD_CSharp_Module_Template.md`

Audit focus:

- `Assets/Scripts/Physics/*`
- `Assets/Scripts/Validation/*`
- high-impact integration points used by heatup startup behavior

## Criteria

### Technical mismatch categories (C-xx)

- **C-01** Startup pressure permissive mismatch
- **C-02** Primary heat input mismatch
- **C-03** Solid-plant pressure band mismatch
- **C-04** SG startup boundary/steam-line behavior mismatch
- **C-05** Reference baseline conflict/unresolved authority

### GOLD mismatch categories (G-xx)

- **G-01** Header missing/noncompliant
- **G-02** No/poor change history
- **G-03** Missing XML docs on public API
- **G-04** File too large/separation of concerns broken
- **G-05** Performance anti-patterns (Unity hot paths)
- **G-06** Naming/structure inconsistent with template
- **G-07** Versioning/changelog process not followed

## Findings

### F-001

- **Type:** TECH
- **Category:** C-01
- **System / Module:** RCP startup permissives
- **Reference expectation (cite doc):**  
  `Technical_Documentation/NRC_HRTD_Section_3.2_Reactor_Coolant_System.md:86` and `Technical_Documentation/NRC_HRTD_Section_3.2_Reactor_Coolant_System.md:90` require minimum 400 psig and no RCP start below that threshold.
- **Current implementation (cite file + function):**  
  `Assets/Scripts/Physics/PlantConstants.Pressure.cs:216` (`MIN_RCP_PRESSURE_PSIG = 320f`) and `Assets/Scripts/Physics/PlantConstants.Pressure.cs:518` (`CanStartRCP`).
- **Why inconsistent:** Implementation allows RCP starts at 320 psig, 80 psig below cited operational minimum.
- **Severity:** High
- **Recommended action:** Fix
- **Suggested domain plan (DP):** DP-RCP-001 (align startup permissive, add telemetry around start lockout reasons)

### F-002

- **Type:** TECH
- **Category:** C-02
- **System / Module:** RCP heat contribution in heatup model
- **Reference expectation (cite doc):**  
  `Technical_Documentation/NRC_HRTD_Section_3.2_Reactor_Coolant_System.md:92` and `Technical_Documentation/NRC_REFERENCE_SOURCES.md:49` cite about 6 MW per RCP in cold water.
- **Current implementation (cite file + function):**  
  `Assets/Scripts/Physics/PlantConstants.Pressure.cs:199` (`RCP_HEAT_MW = 21f`) and `Assets/Scripts/Physics/PlantConstants.Pressure.cs:205` (`RCP_HEAT_MW_EACH = 5.25f`), consumed by heatup paths such as `Assets/Scripts/Physics/RCSHeatup.cs:250`.
- **Why inconsistent:** Primary heat input baseline is lower than the cited Section 3.2 value, shifting heatup trajectory and timing.
- **Severity:** High
- **Recommended action:** Investigate
- **Suggested domain plan (DP):** DP-RCP-002 (reconcile 21 MW vs 24 MW authority, then calibrate acceptance tests)

### F-003

- **Type:** TECH
- **Category:** C-03
- **System / Module:** Solid plant pressure control band
- **Reference expectation (cite doc):**  
  `Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md:41` states pressure maintained between 320 and 400 psig.
- **Current implementation (cite file + function):**  
  `Assets/Scripts/Physics/PlantConstants.Pressure.cs:96` sets `SOLID_PLANT_P_HIGH_PSIG = 450f`; this is used by control/visualization paths (`Assets/Scripts/Validation/HeatupSimEngine.Init.cs:246`, `Assets/Scripts/Physics/SolidPlantPressure.cs:803`).
- **Why inconsistent:** High-band definition exceeds the cited operating range and can normalize out-of-band behavior in validation/UI.
- **Severity:** Medium
- **Recommended action:** Investigate
- **Suggested domain plan (DP):** DP-PZR-001 (review whether 450 is relief/protection-only versus control band)

### F-004

- **Type:** BOTH
- **Category:** C-04
- **System / Module:** SG startup boundary (open vs isolated steam path)
- **Reference expectation (cite doc):**  
  `Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md:26`, `Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md:79`, and `Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md:193` describe MSIV opening and steam-line warming during heatup.
- **Current implementation (cite file + function):**  
  `Assets/Scripts/Validation/HeatupSimEngine.cs:2557` (`ShouldIsolateSGBoundary`) isolates boundary outside `OpenPreheat`/`SteamDump`; isolation is applied at `Assets/Scripts/Validation/HeatupSimEngine.cs:2554`.  
  Isolated mode sets steam outflow to zero in `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1470`.
- **Why inconsistent:** Modeled isolation suppresses steam-line discharge/warming path across startup states where reference describes an open warmup path.
- **Severity:** High
- **Recommended action:** Instrument
- **Suggested domain plan (DP):** DP-SG-001 (A/B run: isolated vs warmed-open boundary with pressure/temperature envelope checks)

### F-005

- **Type:** TECH
- **Category:** C-05
- **System / Module:** Baseline A authority consistency
- **Reference expectation (cite doc):**  
  Baseline A should provide one authoritative operational target set.
- **Current implementation (cite file + function):**  
  Baseline A contains unresolved internal conflict: `Technical_Documentation/NRC_HRTD_Section_3.2_Reactor_Coolant_System.md:92` (~6 MW/pump) vs `Technical_Documentation/RHR_SYSTEM_RESEARCH_v3.0.0.md:118` (~5.25 MW/pump, 21 MW total at `Technical_Documentation/RHR_SYSTEM_RESEARCH_v3.0.0.md:219`).
- **Why inconsistent:** Implementation choice cannot be audited cleanly when baseline documents disagree on core numeric inputs.
- **Severity:** Medium
- **Recommended action:** Document deviation
- **Suggested domain plan (DP):** DP-DOC-001 (declare primary-source precedence and mark derived model assumptions)

### F-006

- **Type:** GOLD
- **Category:** G-01
- **System / Module:** File header compliance across project-owned modules
- **Reference expectation (cite doc):**  
  `Technical_Documentation/GOLD_STANDARD_CSharp_Module_Template.md` requires `Version`, `Last Updated`, and `Changes` in header.
- **Current implementation (cite file + function):**  
  Example: `Assets/Scripts/Physics/VCTPhysics.cs:15` has a legacy version string but no `Last Updated` or `Changes`; similar pattern in `Assets/Scripts/Physics/PlantConstants.Pressure.cs:23` and `Assets/Scripts/Validation/HeatupSimEngine.cs:58`.
- **Why inconsistent:** Header metadata is not audit-grade and does not support reliable file-level traceability.
- **Severity:** High
- **Recommended action:** Fix
- **Suggested domain plan (DP):** DP-GOLD-001 (header retrofit pass + lint/pre-commit check)

### F-007

- **Type:** GOLD
- **Category:** G-02
- **System / Module:** File-level change history quality
- **Reference expectation (cite doc):**  
  `Technical_Documentation/GOLD_STANDARD_CSharp_Module_Template.md` requires latest 5-10 changes in each file header.
- **Current implementation (cite file + function):**  
  Most audited files contain no bounded change ledger in header (for example `Assets/Scripts/Physics/CVCSController.cs:1`, `Assets/Scripts/Physics/SGMultiNodeThermal.cs:1`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1`).
- **Why inconsistent:** No lightweight in-file audit trail for recent amendments.
- **Severity:** Medium
- **Recommended action:** Fix
- **Suggested domain plan (DP):** DP-GOLD-002 (populate current top modules first, then broad sweep)

### F-008

- **Type:** GOLD
- **Category:** G-03
- **System / Module:** Public API XML docs
- **Reference expectation (cite doc):**  
  `Technical_Documentation/GOLD_STANDARD_CSharp_Module_Template.md` requires XML docs on public APIs.
- **Current implementation (cite file + function):**  
  Missing XML on public APIs in audited modules, including `Assets/Scripts/Physics/VCTPhysics.cs:22`, `Assets/Scripts/Physics/VCTPhysics.cs:26`, `Assets/Scripts/Physics/VCTPhysics.cs:94`, and public methods in `Assets/Scripts/Validation/HeatupSimEngine.cs:837`.
- **Why inconsistent:** Public contracts are not uniformly self-documenting for maintenance/audit handoff.
- **Severity:** Medium
- **Recommended action:** Fix
- **Suggested domain plan (DP):** DP-GOLD-003 (API-doc pass on Physics and Validation namespaces)

### F-009

- **Type:** GOLD
- **Category:** G-04
- **System / Module:** File size and separation of concerns
- **Reference expectation (cite doc):**  
  `Technical_Documentation/GOLD_STANDARD_CSharp_Module_Template.md` requires responsible file size and encourages partial decomposition.
- **Current implementation (cite file + function):**  
  Large monolithic files include `Assets/Scripts/UI/MultiScreenBuilder.cs` (4340 lines, class at `Assets/Scripts/UI/MultiScreenBuilder.cs:73`), `Assets/Scripts/Physics/SGMultiNodeThermal.cs` (2674 lines, class at `Assets/Scripts/Physics/SGMultiNodeThermal.cs:572`), and `Assets/Scripts/Physics/CVCSController.cs` (1519 lines, class at `Assets/Scripts/Physics/CVCSController.cs:233`).
- **Why inconsistent:** High change-coupling and review complexity increase regression risk.
- **Severity:** Medium
- **Recommended action:** Investigate
- **Suggested domain plan (DP):** DP-GOLD-004 (extract strategy by subsystem, no behavioral change first)

### F-010

- **Type:** GOLD
- **Category:** G-05
- **System / Module:** Hot-path logging and allocation risk
- **Reference expectation (cite doc):**  
  `Technical_Documentation/GOLD_STANDARD_CSharp_Module_Template.md` forbids per-frame/per-tick log spam/string allocations in hot paths.
- **Current implementation (cite file + function):**  
  `Assets/Scripts/Validation/HeatupSimEngine.cs:936`, `Assets/Scripts/Validation/HeatupSimEngine.cs:975`, `Assets/Scripts/Validation/HeatupSimEngine.cs:1455` contain runtime log calls in simulation-step code paths.
- **Why inconsistent:** In accelerated simulation runs these logs can produce avoidable CPU/GC pressure and noisy diagnostics.
- **Severity:** Medium
- **Recommended action:** Fix
- **Suggested domain plan (DP):** DP-GOLD-005 (log gating + sampling policy + perf counters)

### F-011

- **Type:** GOLD
- **Category:** G-06
- **System / Module:** Namespace/folder structure consistency
- **Reference expectation (cite doc):**  
  `Technical_Documentation/GOLD_STANDARD_CSharp_Module_Template.md` requires namespace and folder conventions.
- **Current implementation (cite file + function):**  
  Validation core files are project code in global namespace (for example `Assets/Scripts/Validation/HeatupSimEngine.cs:73`, `Assets/Scripts/Validation/HeatupValidationVisual.cs:65`, `Assets/Scripts/Validation/HeatupValidationVisual.TabCritical.cs:55`).
- **Why inconsistent:** Namespace drift weakens modular boundaries and API discoverability.
- **Severity:** Medium
- **Recommended action:** Investigate
- **Suggested domain plan (DP):** DP-GOLD-006 (namespace migration plan with assembly-safe staged rollout)

### F-012

- **Type:** GOLD
- **Category:** G-07
- **System / Module:** Versioning/changelog governance
- **Reference expectation (cite doc):**  
  `Technical_Documentation/GOLD_STANDARD_CSharp_Module_Template.md` requires project-level `CHANGELOG.md` plus file/API-level change records.
- **Current implementation (cite file + function):**  
  Repository-level `CHANGELOG.md` is absent; file headers generally do not carry bounded `Changes` history.
- **Why inconsistent:** Intentional deviations and release-level behavior shifts are not traceable to one authoritative ledger.
- **Severity:** High
- **Recommended action:** Fix
- **Suggested domain plan (DP):** DP-GOLD-007 (introduce changelog + enforce conventional commit mapping)

## Governance Output (High Severity -> INVESTIGATE)

High findings requiring issue register entries:

- F-001
- F-002
- F-004
- F-006
- F-012

Issue Register has been updated with INVESTIGATE entries referencing these finding IDs as evidence.

