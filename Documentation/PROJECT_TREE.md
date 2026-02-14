# Critical Simulator Project Tree
## Canonical Repository Structure
### Generated: 2026-02-14

---

## Scope

This tree documents the active project structure used for governance, implementation, and audit work.

## Canonical Tree

```text
Critical/
|-- Assets/
|   |-- Animations/
|   |-- Documentation/
|   |-- InputActions/
|   |-- Materials/
|   |-- Models/
|   |-- Prefabs/
|   |-- Resources/
|   |-- Scenes/
|   |-- Settings/
|   |-- TextMesh Pro/
|   |-- Textures/
|   |-- _Recovery/
|   `-- Scripts/
|       |-- Blender/
|       |-- Core/
|       |-- Physics/
|       |-- Reactor/
|       |-- Tests/
|       |-- UI/
|       `-- Validation/
|
|-- Governance/
|   |-- Changelogs/
|   |-- DomainPlans/
|   |   `-- Closed/
|   |-- ImplementationPlans/
|   |   `-- Closed/
|   |-- ImplementationReports/
|   |-- IssueRegister/
|   `-- Issues/
|
|-- Documentation/
|   |-- Implementation/
|   |-- Updates/
|   |-- GOLD_STANDARD_TEMPLATE.md
|   |-- PROJECT_OVERVIEW.md
|   |-- PROJECT_TREE.md
|   `-- STRUCTURAL_MAP.md
|
|-- TechnicalDocumentation/
|   |-- GOLD_CONFORMANCE_CHECKLIST.md
|   |-- GOLD_FILE_HYGIENE_LIMITS.md
|   |-- GOLD_LIFECYCLE_CONTRACT.md
|   `-- GOLD_TEMPLATE_STANDARD.md
|
|-- Technical_Documentation/
|   |-- Archive/
|   |-- NRC_* references
|   `-- Technical_Documentation_Index.md
|
|-- Manuals/
|-- HeatupLogs/
|-- Updates/
|   |-- Archive_DEPRECATED/
|   `-- Forensics/
|
|-- Build/
|-- Library/
|-- Logs/
|-- Packages/
|-- ProjectSettings/
|-- Temp/
|-- UserSettings/
|-- .claude/
|-- .gitattributes
|-- .gitignore
|-- .vsconfig
|-- Assembly-CSharp.csproj
|-- Assembly-CSharp-Editor.csproj
|-- Critical.Physics.csproj
|-- Critical.Reactor.csproj
|-- Critical.slnx
`-- PROJECT_CONSTITUTION.md
```

---

## Governance Notes

- Active governance artifacts are under `Governance/`.
- `Updates/Archive_DEPRECATED/` is retained for legacy traceability only.
- Issue state and routing authority are in `Governance/IssueRegister/`.

## Excluded from deep listing

- `Library/`, `Temp/`, `obj/`, and package cache content are intentionally not expanded.

---

*Document purpose: architectural orientation and governance navigation.*
