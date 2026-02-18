# Critical Simulator Project Tree
## Canonical Repository Structure
### Generated: 2026-02-18

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
|   |   `-- Systems/
|   |       `-- RCS/
|   |-- Resources/
|   |-- Scenes/
|   |-- Settings/
|   |-- TextMesh Pro/
|   |-- Textures/
|   |-- _Recovery/
|   `-- Scripts/
|       |-- Blender/
|       |-- Core/
|       |   |-- SceneBridge.cs
|       |   `-- WindowFocusManager.cs
|       |-- Physics/
|       |   |-- AlarmManager.cs
|       |   |-- BRSPhysics.cs
|       |   |-- CondenserPhysics.cs
|       |   |-- CoupledThermo.cs
|       |   |-- CVCSController.cs
|       |   |-- CVCSFlowMath.cs
|       |   |-- FeedwaterSystem.cs
|       |   |-- FluidFlow.cs
|       |   |-- HeatTransfer.cs
|       |   |-- HZPStabilizationController.cs
|       |   |-- LoopThermodynamics.cs
|       |   |-- PlantConstants.*.cs (partials)
|       |   |-- PlantMath.cs
|       |   |-- PressurizerPhysics.cs
|       |   |-- RCPSequencer.cs
|       |   |-- RCSHeatup.cs
|       |   |-- ReactorKinetics.cs
|       |   |-- RHRSystem.cs
|       |   |-- RVLISPhysics.cs
|       |   |-- SGMultiNodeThermal.cs
|       |   |-- SGSecondaryThermal.cs
|       |   |-- SolidPlantPressure.cs
|       |   |-- StartupPermissives.cs
|       |   |-- SteamDumpController.cs
|       |   |-- SteamThermodynamics.cs
|       |   |-- ThermalExpansion.cs
|       |   |-- ThermalMass.cs
|       |   |-- TimeAcceleration.cs
|       |   |-- VCTPhysics.cs
|       |   `-- WaterProperties.cs
|       |-- Reactor/
|       |-- ScenarioSystem/
|       |   |-- ISimulationScenario.cs
|       |   |-- ScenarioRegistry.cs
|       |   `-- ValidationHeatupScenario.cs
|       |-- Simulation/
|       |   `-- Modular/
|       |       |-- IPlantModule.cs
|       |       |-- ModularFeatureFlags.cs
|       |       |-- PlantSimulationCoordinator.cs
|       |       |-- Modules/
|       |       |   |-- CVCSModule.cs
|       |       |   |-- LegacySimulatorModule.cs
|       |       |   |-- PressurizerModule.cs
|       |       |   |-- RCPModule.cs
|       |       |   |-- RCSModule.cs
|       |       |   |-- ReactorModule.cs
|       |       |   |-- RHRModule.cs
|       |       |   `-- PZR/
|       |       |       |-- PressurizerOutputs.cs
|       |       |       `-- PressurizerState.cs
|       |       |-- State/
|       |       |   |-- LegacyStateBridge.cs
|       |       |   |-- PlantState.cs
|       |       |   |-- PressurizerSnapshot.cs
|       |       |   `-- StepSnapshot.cs
|       |       |-- Transfer/
|       |       |   |-- PlantBus.cs
|       |       |   |-- TransferEvent.cs
|       |       |   |-- TransferIntentKinds.cs
|       |       |   `-- TransferLedger.cs
|       |       `-- Validation/
|       |           `-- ModuleComparator.cs
|       |-- Systems/
|       |   `-- RCS/
|       |       |-- RCSLoop.cs
|       |       |-- RCSLoopContracts.cs
|       |       `-- RCSLoopManager.cs
|       |-- Tests/
|       |   |-- AcceptanceSimulationEvidence.cs
|       |   |-- AcceptanceTests_v5_4_0.cs
|       |   |-- HeatupIntegrationTests.cs
|       |   |-- IntegrationTests.cs
|       |   |-- Phase1TestRunner.cs
|       |   |-- Phase2TestRunner.cs
|       |   |-- Phase2UnityTestRunner.cs
|       |   |-- RCSLoopManagerCompatibilityTests.cs
|       |   `-- UnityTestRunner.cs
|       |-- UI/
|       |   |-- Editor/
|       |   |-- UIToolkit/
|       |   |-- ValidationDashboard/
|       |   |   |-- Effects/
|       |   |   |   |-- AlarmFlashEffect.cs
|       |   |   |   `-- GlowEffect.cs
|       |   |   |-- Gauges/
|       |   |   |   |-- ArcGauge.cs
|       |   |   |   |-- BidirectionalGauge.cs
|       |   |   |   |-- DashboardAnnunciatorTile.cs
|       |   |   |   |-- DigitalReadout.cs
|       |   |   |   |-- GaugeAnimator.cs
|       |   |   |   |-- InstrumentFontHelper.cs
|       |   |   |   |-- LinearGauge.cs
|       |   |   |   `-- StatusIndicator.cs
|       |   |   |-- Panels/
|       |   |   |   |-- AlarmsPanel.cs
|       |   |   |   |-- CVCSPanel.cs
|       |   |   |   |-- EventLogPanel.cs
|       |   |   |   |-- HeaderPanel.cs
|       |   |   |   |-- MiniTrendsPanel.cs
|       |   |   |   |-- OverviewPanel.cs
|       |   |   |   |-- OverviewSection_*.cs (partials)
|       |   |   |   |-- PressurizerPanel.cs
|       |   |   |   |-- PrimaryLoopPanel.cs
|       |   |   |   |-- SGRHRPanel.cs
|       |   |   |   |-- ValidationPanel.cs
|       |   |   |   `-- ValidationPanelBase.cs
|       |   |   |-- Trends/
|       |   |   |   |-- MiniTrendStrip.cs
|       |   |   |   |-- StripChart.cs
|       |   |   |   `-- TrendBuffer.cs
|       |   |   |-- TabNavigationController.cs
|       |   |   |-- ValidationDashboardBuilder.cs
|       |   |   |-- ValidationDashboardController.cs
|       |   |   |-- ValidationDashboardLauncher.cs
|       |   |   |-- ValidationDashboardSceneSetup.cs
|       |   |   |-- ValidationDashboardTestSetup.cs
|       |   |   `-- ValidationDashboardTheme.cs
|       |   |-- _Archive/
|       |   |-- MultiScreenBuilder.cs
|       |   |-- OperatorScreen*.cs
|       |   |-- ScreenDataBridge.cs
|       |   |-- ScreenManager.cs
|       |   `-- [Other operator screens]
|       `-- Validation/
|           |-- HeatupSimEngine.cs
|           |-- HeatupSimEngine.*.cs (partials)
|           |-- HeatupValidationVisual.cs
|           |-- HeatupValidationVisual.*.cs (partials)
|           |-- ValidationDashboard.cs
|           |-- ValidationDashboard.*.cs (partials)
|           `-- Tabs/
|               |-- CVCSTab.cs
|               |-- DashboardTab.cs
|               |-- GraphsTab.cs
|               |-- LogTab.cs
|               |-- OverviewTab.cs
|               |-- PressurizerTab.cs
|               |-- RCSTab.cs
|               |-- SGRHRTab.cs
|               `-- SystemsTab.cs
|
|-- Governance/
|   |-- Archive_DEPRACATED/
|   |-- Changelogs/
|   |-- DomainPlans/
|   |   `-- Closed/
|   |-- DP_EXECUTION_RECOMMENDATION.md
|   |-- DP_REGISTRY_CONSISTENCY_REPORT.md
|   |-- Future_Features.md
|   |-- ImplementationPlans/
|   |   `-- Closed/
|   |-- ImplementationReports/
|   |-- Investigations/
|   |-- IssueRegister/
|   |   |-- issue_register.json (active working set)
|   |   |-- issue_archive.json (closed issues)
|   |   |-- issue_index.json (full history)
|   |   |-- issue_register.schema.json
|   |   `-- issue_archive.schema.json
|   |-- Issues/
|   `-- REGISTRY_AUDIT_REPORT.md
|
|-- Documentation/
|   |-- Implementation/
|   |-- Updates/
|   |-- GOLD_STANDARD_TEMPLATE.md
|   |-- PROJECT_OVERVIEW.md
|   |-- PROJECT_TREE.md
|   |-- Project_Summary_For_Continuation.md
|   |-- PWR_Heatup_Simulation_Analysis_Report.md
|   |-- REFACTORING_PLAN.md
|   |-- SG_Model_Correct_Analysis.md
|   |-- SG_Secondary_Heating_Implementation.md
|   `-- STRUCTURAL_MAP.md
|
|-- Technical_Documentation/
|   |-- Archive/
|   |-- Condenser_Feedwater_Architecture_Specification.md
|   |-- Conformance_Audit_Report_2026-02-15.md
|   |-- GOLD_STANDARD_CSharp_Module_Template.md
|   |-- NRC_HRTD_*.md (reference documents)
|   |-- NRC_REFERENCE_SOURCES.md
|   |-- Pressurizer_System_Audit_2026-02-17.md
|   |-- PWR_Startup_State_Sequence.md
|   |-- PZR_Baseline_Profile.md
|   |-- RCP_Heat_Authority_Decision_2026-02-16.md
|   |-- RCS_Pressure_Temperature_Limit_Curves_Implementation.md
|   |-- RHR_SYSTEM_RESEARCH_v3.0.0.md
|   |-- SG_*.md (steam generator research)
|   |-- Startup_Boundary_and_SteamDump_Authoritative_Spec.md
|   |-- Steam_Tables_PWR_Startup_Range.md
|   |-- Technical_Documentation_Index.md
|   `-- Westinghouse_4Loop_Pressurizer_Specifications_Summary.md
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
|-- Critical.UI.UIToolkit.csproj
|-- Critical.slnx
`-- PROJECT_CONSTITUTION.md
```

---

## Governance Notes

- Active governance artifacts are under `Governance/`.
- `Updates/Archive_DEPRECATED/` is retained for legacy traceability only.
- Issue state and routing authority are in `Governance/IssueRegister/`.

### Scenario System
- Scenario system execution surfaces are governed under `Assets/Scripts/ScenarioSystem/`.
- `ISimulationScenario.cs` defines the scenario contract.
- `ScenarioRegistry.cs` provides scenario discovery and instantiation.
- `ValidationHeatupScenario.cs` is the primary heatup validation scenario implementation.

### Modular Simulation Architecture
- Modular simulator architecture is governed under `Assets/Scripts/Simulation/Modular/`.
- `PlantSimulationCoordinator.cs` orchestrates module execution order.
- `IPlantModule.cs` defines the module contract.
- `ModularFeatureFlags.cs` controls feature toggles for legacy/modular switching.
- Individual plant modules: CVCSModule, PressurizerModule, RCPModule, RCSModule, ReactorModule, RHRModule.
- `LegacySimulatorModule.cs` provides strangler-fig migration bridge.
- State management: `State/PlantState.cs`, `State/StepSnapshot.cs`.
- Transfer bus: `Transfer/PlantBus.cs`, `Transfer/TransferLedger.cs`.
- Shadow-mode validation: `Validation/ModuleComparator.cs`.

### RCS Loop System
- Modular RCS prefab roots are governed under `Assets/Prefabs/Systems/RCS/`.
- Modular RCS runtime systems are governed under `Assets/Scripts/Systems/RCS/`.
- `RCSLoopContracts.cs` defines loop-level interface contracts.
- `RCSLoop.cs` provides single-loop encapsulation.
- `RCSLoopManager.cs` provides N-loop aggregation with N=1 compatibility.

### Validation Dashboard
- Professional instrumentation dashboard under `Assets/Scripts/UI/ValidationDashboard/`.
- `Effects/` - Visual effects (alarm flash, glow).
- `Gauges/` - Arc gauges, digital readouts, indicators.
- `Panels/` - System panels (Pressurizer, CVCS, SG/RHR, etc.).
- `Trends/` - Strip charts and trend buffers.
- Tab-based navigation via `TabNavigationController.cs`.

### Validation Engine
- Core validation engine under `Assets/Scripts/Validation/`.
- `HeatupSimEngine.cs` and partials (Alarms, BubbleFormation, CVCS, HZP, Init, Logging, RuntimePerf, Scenarios).
- `ValidationDashboard.cs` and partials for OnGUI-based dashboard.
- `HeatupValidationVisual.cs` and partials for legacy tabbed display.
- `Tabs/` contains modular tab implementations.

## Excluded from deep listing

- `Library/`, `Temp/`, `obj/`, and package cache content are intentionally not expanded.
- `.meta` files are not listed but are present for all Unity assets.
- Individual NRC_HRTD_* reference documents are grouped for brevity.

---

## Amendment History

| Date | CS Reference | Summary |
|------|--------------|---------|
| 2026-02-14 | Initial | Initial canonical tree |
| 2026-02-17 | CS-0100 | Added Scenario System and Modular RCS target structure (via IP-0047) |
| 2026-02-18 | CS-0123 | Full structural update reflecting IP-0025 through IP-0051 additions |

---

*Document purpose: architectural orientation and governance navigation.*
