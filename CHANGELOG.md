# Changelog

All notable project-level governance and implementation changes are recorded here.

## [2026-02-17] - IP-0047 Gate B-E Execution Wave

### Added
- Structural authority targets for scenario and modular RCS paths in `Documentation/PROJECT_TREE.md`.
- GOLD metadata normalization across audited governance-critical modules:
  - `Assets/Scripts/Validation/HeatupSimEngine.cs`
  - `Assets/Scripts/Physics/VCTPhysics.cs`
  - `Assets/Scripts/Physics/PlantConstants.Pressure.cs`
  - `Assets/Scripts/Physics/CVCSController.cs`
  - `Assets/Scripts/Physics/SGMultiNodeThermal.cs`
  - `Assets/Scripts/UI/MultiScreenBuilder.cs`
  - `Assets/Scripts/Validation/HeatupValidationVisual.cs`
  - `Assets/Scripts/Validation/HeatupValidationVisual.TabCritical.cs`

### Changed
- `HeatupSimEngine` header now includes explicit `SOURCES` and `UNITS` sections required by GOLD C01.

### Governance
- Introduced repository-level changelog authority required by GOLD versioning/changelog process.
