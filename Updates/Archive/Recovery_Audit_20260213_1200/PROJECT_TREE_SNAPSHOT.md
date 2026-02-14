# Project Tree Snapshot
**Generated**: 2026-02-13 12:00
**Version**: v5.4.1 (stable baseline)
**Purpose**: Recovery audit reference snapshot

---

## 1. Physics Modules
Assets/Scripts/Physics/AlarmManager.cs
Assets/Scripts/Physics/BRSPhysics.cs
Assets/Scripts/Physics/CoupledThermo.cs
Assets/Scripts/Physics/CVCSController.cs
Assets/Scripts/Physics/FluidFlow.cs
Assets/Scripts/Physics/HeatTransfer.cs
Assets/Scripts/Physics/HZPStabilizationController.cs
Assets/Scripts/Physics/LoopThermodynamics.cs
Assets/Scripts/Physics/PressurizerPhysics.cs
Assets/Scripts/Physics/RCPSequencer.cs
Assets/Scripts/Physics/RCSHeatup.cs
Assets/Scripts/Physics/ReactorKinetics.cs
Assets/Scripts/Physics/RHRSystem.cs
Assets/Scripts/Physics/RVLISPhysics.cs
Assets/Scripts/Physics/SGForensics.cs
Assets/Scripts/Physics/SGMultiNodeThermal.cs
Assets/Scripts/Physics/SGSecondaryThermal.cs
Assets/Scripts/Physics/SolidPlantPressure.cs
Assets/Scripts/Physics/SteamDumpController.cs
Assets/Scripts/Physics/SteamThermodynamics.cs
Assets/Scripts/Physics/ThermalExpansion.cs
Assets/Scripts/Physics/ThermalMass.cs
Assets/Scripts/Physics/TimeAcceleration.cs
Assets/Scripts/Physics/VCTPhysics.cs
Assets/Scripts/Physics/WaterProperties.cs

---

## 2. Simulation Engine
Assets/Scripts/Validation/HeatupSimEngine.cs
Assets/Scripts/Validation/HeatupSimEngine.Alarms.cs
Assets/Scripts/Validation/HeatupSimEngine.BubbleFormation.cs
Assets/Scripts/Validation/HeatupSimEngine.CVCS.cs
Assets/Scripts/Validation/HeatupSimEngine.HZP.cs
Assets/Scripts/Validation/HeatupSimEngine.Init.cs
Assets/Scripts/Validation/HeatupSimEngine.Logging.cs

---

## 3. UI / Visual
Assets/Scripts/Validation/HeatupValidationVisual.cs
Assets/Scripts/Validation/HeatupValidationVisual.Annunciators.cs
Assets/Scripts/Validation/HeatupValidationVisual.Gauges.cs
Assets/Scripts/Validation/HeatupValidationVisual.Graphs.cs
Assets/Scripts/Validation/HeatupValidationVisual.Panels.cs
Assets/Scripts/Validation/HeatupValidationVisual.Styles.cs
Assets/Scripts/Validation/HeatupValidationVisual.TabCritical.cs
Assets/Scripts/Validation/HeatupValidationVisual.TabCVCS.cs
Assets/Scripts/Validation/HeatupValidationVisual.TabEventLog.cs
Assets/Scripts/Validation/HeatupValidationVisual.TabOverview.cs
Assets/Scripts/Validation/HeatupValidationVisual.TabPressurizer.cs
Assets/Scripts/Validation/HeatupValidationVisual.TabRCPElectrical.cs
Assets/Scripts/Validation/HeatupValidationVisual.TabSGRHR.cs
Assets/Scripts/Validation/HeatupValidationVisual.TabValidation.cs
Assets/Scripts/UI/AssemblyDetailPanel.cs
Assets/Scripts/UI/AuxiliarySystemsScreen.cs
Assets/Scripts/UI/CoreMapData.cs
Assets/Scripts/UI/CoreMosaicMap.cs
Assets/Scripts/UI/CVCSScreen.cs
Assets/Scripts/UI/MosaicAlarmPanel.cs
Assets/Scripts/UI/MosaicBoard.cs
Assets/Scripts/UI/MosaicBoardBuilder.cs
Assets/Scripts/UI/MosaicBoardSetup.cs
Assets/Scripts/UI/MosaicControlPanel.cs
Assets/Scripts/UI/MosaicGauge.cs
Assets/Scripts/UI/MosaicIndicator.cs
Assets/Scripts/UI/MosaicRodDisplay.cs
Assets/Scripts/UI/MosaicTypes.cs
Assets/Scripts/UI/MultiScreenBuilder.cs
Assets/Scripts/UI/OperatorScreen.cs
Assets/Scripts/UI/OperatorScreenBuilder.cs
Assets/Scripts/UI/PlantOverviewScreen.cs
Assets/Scripts/UI/PressurizerScreen.cs
Assets/Scripts/UI/RCPControlPanel.cs
Assets/Scripts/UI/RCSGaugeTypes.cs
Assets/Scripts/UI/RCSPrimaryLoopScreen.cs
Assets/Scripts/UI/RCSVisualizationController.cs
Assets/Scripts/UI/ReactorOperatorScreen.cs
Assets/Scripts/UI/ReactorOperatorScreenSkin.cs
Assets/Scripts/UI/ReactorScreenAdapter.cs
Assets/Scripts/UI/RodControlPanel.cs
Assets/Scripts/UI/ScreenDataBridge.cs
Assets/Scripts/UI/ScreenManager.cs
Assets/Scripts/UI/SecondarySystemsScreen.cs
Assets/Scripts/UI/SteamGeneratorScreen.cs
Assets/Scripts/UI/TurbineGeneratorScreen.cs
Assets/Scripts/UI/Editor/InstrumentMaterialSetup.cs
Assets/Scripts/UI/Editor/InstrumentSpriteGenerator.cs
Assets/Scripts/UI/Editor/InstrumentSpriteImporter.cs

---

## 4. Tests
Assets/Scripts/Tests/AcceptanceTests_v5_4_0.cs
Assets/Scripts/Tests/HeatupIntegrationTests.cs
Assets/Scripts/Tests/IntegrationTests.cs
Assets/Scripts/Tests/Phase1TestRunner.cs
Assets/Scripts/Tests/Phase2TestRunner.cs
Assets/Scripts/Tests/Phase2UnityTestRunner.cs
Assets/Scripts/Tests/UnityTestRunner.cs
Assets/Scripts/UI/ReactorOperatorGUI_IntegrationTests.cs

---

## 5. Core Infrastructure
Assets/Scripts/Core/SceneBridge.cs
Assets/Scripts/Core/WindowFocusManager.cs

---

## 6. Reactor Subsystem
Assets/Scripts/Reactor/ControlRodBank.cs
Assets/Scripts/Reactor/FeedbackCalculator.cs
Assets/Scripts/Reactor/FuelAssembly.cs
Assets/Scripts/Reactor/PowerCalculator.cs
Assets/Scripts/Reactor/ReactorController.cs
Assets/Scripts/Reactor/ReactorCore.cs
Assets/Scripts/Reactor/ReactorSimEngine.cs

---

## 7. Scenes
Assets/Scenes/MainScene.unity
Assets/Scenes/PrimaryRCS.unity
Assets/Scenes/Reactor.unity
Assets/Scenes/SampleScene.unity
Assets/Scenes/Validator.unity
Assets/_Recovery/0.unity

---

## 8. Constants
Assets/Scripts/Physics/PlantConstants.cs
Assets/Scripts/Physics/PlantConstants.BRS.cs
Assets/Scripts/Physics/PlantConstants.CVCS.cs
Assets/Scripts/Physics/PlantConstants.Heatup.cs
Assets/Scripts/Physics/PlantConstants.Nuclear.cs
Assets/Scripts/Physics/PlantConstants.Pressure.cs
Assets/Scripts/Physics/PlantConstants.Pressurizer.cs
Assets/Scripts/Physics/PlantConstants.RHR.cs
Assets/Scripts/Physics/PlantConstants.SG.cs
Assets/Scripts/Physics/PlantConstants.SteamDump.cs
Assets/Scripts/Physics/PlantConstants.Validation.cs

---

## 9. Documentation / Updates

### Documentation/
Documentation/GOLD_STANDARD_TEMPLATE.md
Documentation/PROJECT_OVERVIEW.md
Documentation/PROJECT_TREE.md
Documentation/Project_Summary_For_Continuation.md
Documentation/PWR_Heatup_Simulation_Analysis_Report.md
Documentation/PWR_Physics_Review_Summary.md
Documentation/REFACTORING_PLAN.md
Documentation/SG_Model_Correct_Analysis.md
Documentation/SG_Secondary_Heating_Implementation.md
Documentation/STRUCTURAL_MAP.md

### Documentation/Implementation/
Documentation/Implementation/Critical_Validation_Report.md
Documentation/Implementation/Phase_Mapping_Analysis.md
Documentation/Implementation/Phase 1/CHANGELOG_physics_fixes.md
Documentation/Implementation/Phase 1/Phase1_Development_Handoff.md
Documentation/Implementation/Phase 1/Phase1_Implementation_Manual.md
Documentation/Implementation/Phase 1/Phase1_Implementation_Summary.md
Documentation/Implementation/Phase 1/VCT_Specification_Document.md

### Documentation/Updates/
Documentation/Updates/AUDIT_PLAN_Stage1_SubStages.md
Documentation/Updates/AUDIT_Stage1A_Constants_Properties.md
Documentation/Updates/AUDIT_Stage1B_Heat_Flow.md
Documentation/Updates/AUDIT_Stage1C_Pressurizer_Kinetics.md
Documentation/Updates/AUDIT_Stage1D_Support_Systems.md
Documentation/Updates/AUDIT_Stage1E_Reactor_Core.md
Documentation/Updates/AUDIT_Stage1F_Validation_Engine.md
Documentation/Updates/AUDIT_Stage1G_Tests_UI.md
Documentation/Updates/AUDIT_Stage2_ParameterAudit_Part1.md
Documentation/Updates/AUDIT_Stage2_ParameterAudit_Part2.md
Documentation/Updates/DESIGN_BubbleFormation_HeaterControl_v1.4.0.0.md
Documentation/Updates/HANDOVER_PZR_BubbleFormation_and_RCP_Bugs.md
Documentation/Updates/UPDATE-v1.0.1.0.md
Documentation/Updates/UPDATE-v1.0.1.1.md
Documentation/Updates/UPDATE-v1.0.1.2.md
Documentation/Updates/UPDATE-v1.0.1.3.md
Documentation/Updates/UPDATE-v1.0.1.4.md
Documentation/Updates/UPDATE-v1.0.1.5.md
Documentation/Updates/UPDATE-v1.0.1.6.md
Documentation/Updates/UPDATE-v1.0.2.0.md
Documentation/Updates/UPDATE-v1.0.3.0.md
Documentation/Updates/UPDATE-v1.0.4.0.md
Documentation/Updates/UPDATE-v1.0.5.0.md
Documentation/Updates/UPDATE-v1.0.5.1.md
Documentation/Updates/UPDATE-v1.1.0.0.md
Documentation/Updates/UPDATE-v1.2.0.0.md
Documentation/Updates/UPDATE-v1.3.0.0.md
Documentation/Updates/UPDATE-v1.3.0.1.md

### Technical_Documentation/
Technical_Documentation/NRC_HRTD_Startup_Pressurization_Reference.md
Technical_Documentation/NRC_REFERENCE_SOURCES.md
Technical_Documentation/PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md
Technical_Documentation/RHR_SYSTEM_RESEARCH_v3.0.0.md
Technical_Documentation/SG_MODEL_RESEARCH_HANDOFF.md
Technical_Documentation/SG_Secondary_Pressurization_During_Heatup_Research.md
Technical_Documentation/SG_THERMAL_MODEL_RESEARCH_v3.0.0.md

### Updates/Changelogs/
Updates/Changelogs/CHANGELOG_v4.0.0.md
Updates/Changelogs/CHANGELOG_v4.1.0.md
Updates/Changelogs/CHANGELOG_v4.2.2.md
Updates/Changelogs/CHANGELOG_v4.4.0.md
Updates/Changelogs/CHANGELOG_v5.0.0.md
Updates/Changelogs/CHANGELOG_v5.0.1.md
Updates/Changelogs/CHANGELOG_v5.0.2.md
Updates/Changelogs/CHANGELOG_v5.0.3.md
Updates/Changelogs/CHANGELOG_v5.1.0.md
Updates/Changelogs/CHANGELOG_v5.2.0.md
Updates/Changelogs/CHANGELOG_v5.3.0.md
Updates/Changelogs/CHANGELOG_v5.4.1.md
Updates/Changelogs/CHANGELOG_v5.4.1_AUDIT_FIX.md

### Updates/Future_Features/
Updates/Future_Features/FUTURE_ARCHITECTURE_ITEMS.md
Updates/Future_Features/FUTURE_ENHANCEMENTS_ROADMAP.md
Updates/Future_Features/FUTURE_FEATURE_TEMPLATE.md
Updates/Future_Features/TRIAGE_v5.4.1_PostStabilization.md
Updates/Future_Features/VERSIONING_POLICY.md

### Updates/Implementation_Plans/
Updates/IMPLEMENTATION_PLAN_v5.0.0.md
Updates/IMPLEMENTATION_PLAN_v5.0.1.md
Updates/IMPLEMENTATION_PLAN_v5.0.2.md
Updates/IMPLEMENTATION_PLAN_v5.0.3.md
Updates/IMPLEMENTATION_PLAN_v5.1.0.md
Updates/IMPLEMENTATION_PLAN_v5.2.0.md
Updates/IMPLEMENTATION_PLAN_v5.3.0.md
Updates/IMPLEMENTATION_PLAN_v5.3.1.md
Updates/IMPLEMENTATION_PLAN_v5.3.2.md
Updates/IMPLEMENTATION_PLAN_v5.4.0.md
Updates/IMPLEMENTATION_PLAN_v5.4.1.md
Updates/IMPLEMENTATION_PLAN_v5.4.1_AUDIT_FIX.md
Updates/IMPLEMENTATION_PLAN_v5.4.2.0.md

### Updates/Forensics/
Updates/Forensics/FORENSIC_SecondaryMass_Reporting_Audit.md
Updates/Forensics/INVENTORY_AUDIT_DISCREPANCY.md
Updates/Forensics/TELEMETRY_FLOW_AUDIT.md

### Updates/Other/
Updates/Inventory_Audit_Stage0_v5.3.0.md
Updates/Inventory_Audit_v1.0.0.md
Updates/SG_HEATUP_BREAKTHROUGH_HANDOFF.md

### Updates/Archive/
Updates/Archive/Changelogs/CHANGELOG v0.1.0.md
Updates/Archive/Changelogs/CHANGELOG v0.2.0.md
Updates/Archive/Changelogs/CHANGELOG v0.3.0.md
Updates/Archive/Changelogs/CHANGELOG v0.4.0.md
Updates/Archive/Changelogs/CHANGELOG v0.4.1.md
Updates/Archive/Changelogs/CHANGELOG v0.5.0.md
Updates/Archive/Changelogs/CHANGELOG v0.6.0.md
Updates/Archive/Changelogs/CHANGELOG v0.7.0.md
Updates/Archive/Changelogs/CHANGELOG v0.7.1.md
Updates/Archive/Changelogs/CHANGELOG v0.7.1_STAGE1.md
Updates/Archive/Changelogs/CHANGELOG_v0.8.0.md
Updates/Archive/Changelogs/CHANGELOG_v0.8.1.md
Updates/Archive/Changelogs/CHANGELOG_v0.8.2.md
Updates/Archive/Changelogs/CHANGELOG_v0.9.0.md
Updates/Archive/Changelogs/CHANGELOG_v0.9.1.md
Updates/Archive/Changelogs/CHANGELOG_v0.9.2.md
Updates/Archive/Changelogs/CHANGELOG_v0.9.3.md
Updates/Archive/Changelogs/CHANGELOG_v0.9.4.md
Updates/Archive/Changelogs/CHANGELOG_v0.9.5.md
Updates/Archive/Changelogs/CHANGELOG_v0.9.6.md
Updates/Archive/Changelogs/CHANGELOG_v0.9.6_Stage1.md
Updates/Archive/Changelogs/CHANGELOG_v0.9.6_Stage2.md
Updates/Archive/Changelogs/CHANGELOG_v1.0.0.md
Updates/Archive/Changelogs/CHANGELOG_v1.1.0.md
Updates/Archive/Changelogs/CHANGELOG_v1.1.1.md
Updates/Archive/Changelogs/CHANGELOG_v1.1.2.md
Updates/Archive/Changelogs/CHANGELOG_v1.2.0.md
Updates/Archive/Changelogs/CHANGELOG_v1.3.0.md
Updates/Archive/Changelogs/CHANGELOG_v2.0.1_InputSystemFix.md
Updates/Archive/Changelogs/CHANGELOG_v2.0.10.md
Updates/Archive/Changelogs/CHANGELOG_v2.0.11.md
Updates/Archive/Changelogs/CHANGELOG_v2.0.2_ScreenManagerWiring.md
Updates/Archive/Changelogs/CHANGELOG_v2.0.3_InactiveScreenRegistration.md
Updates/Archive/Changelogs/CHANGELOG_v2.0.4_MultiScreenGUI.md
Updates/Archive/Changelogs/CHANGELOG_v2.0.5.md
Updates/Archive/Changelogs/CHANGELOG_v2.0.9_MultiScreenGUI.md
Updates/Archive/Changelogs/CHANGELOG_v3.0.0.md
Updates/Archive/Changelogs/DESIGN_BubbleFormation_HeaterControl.md
Updates/Archive/Changelogs/DIAGNOSIS_REPORT.md
Updates/Archive/Changelogs/GUI_Screen_Layout_Mapping_v2.0.9.md
Updates/Archive/Changelogs/HANDOFF_v2.0.3_MultiScreenGUI.md
Updates/Archive/Changelogs/IMPL_PLAN_v0.4.0.md
Updates/Archive/Changelogs/IMPL_PLAN_v0.6.0.md
Updates/Archive/Changelogs/IMPL_PLAN_v0.7.1.md
Updates/Archive/Changelogs/IMPL_PLAN_v0.8.0.md
Updates/Archive/Changelogs/IMPL_PLAN_v0.8.1.md
Updates/Archive/Changelogs/IMPL_PLAN_v0.8.2.md
Updates/Archive/Changelogs/IMPL_PLAN_v0.9.0.md
Updates/Archive/Changelogs/IMPL_PLAN_v0.9.1.md
Updates/Archive/Changelogs/IMPL_PLAN_v0.9.3.md
Updates/Archive/Changelogs/IMPLEMENTATION_GUIDE.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v0.9.5.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v0.9.5_REVISED.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v0.9.6.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v1.0.0_ReactorOperatorGUI.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v1.1.0.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v1.1.1.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v1.1.2.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v1.1.3.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v1.2.0_RCS_Screen_and_Gauges.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v2.0.0_MultiScreenGUI.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v2.0.1_InputSystemFix.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v2.0.10.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v2.0.11.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v2.0.2_ScreenManagerWiring.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v2.0.3_InactiveScreenRegistration.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v2.0.5.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v2.1.0_ValidationMode.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v3.0.0.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v4.0.0_ReactorOperatorScreen_Overhaul.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v4.1.0_MosaicBoard_Visual_Upgrade.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v4.2.0.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v4.2.2.md
Updates/Archive/Changelogs/IMPLEMENTATION_PLAN_v4.3.0.md
Updates/Archive/Changelogs/Implementation_Plan_v4.4.0_PZR_Level_Pressure_Control_Fix.md
Updates/Archive/Changelogs/Manual_Heatup_Controls_Plan_v1_0_0.md
Updates/Archive/Changelogs/Operator_Screen_Layout_Plan_v1_0_0.md
Updates/Archive/Changelogs/REFACTORING_PLAN_v2.md
Updates/Archive/Changelogs/SG_Heat_Transfer_Investigation_Summary _ CRITICAL FOR v1.1.2 RESOLUTION.md
Updates/Archive/Changelogs/STAGE1_HANDOFF_SUMMARY.md
Updates/Archive/Changelogs/STAGE1_SUMMARY.md
Updates/Archive/Changelogs/v0.7.1_ALL_STAGES_COMPLETE.md
Updates/Archive/Changelogs/v0.7.1_IMPLEMENTATION_COMPLETE.md
Updates/Archive/Changelogs/Screen2_RCS_Primary_Loop/Blender_Unity_Export_Manual.md
Updates/Archive/Changelogs/Screen2_RCS_Primary_Loop/Blender5_Unity6_Export_Manual.md
Updates/Archive/Changelogs/Screen2_RCS_Primary_Loop/RCS_Screen_Assembly_Guide.md
Updates/Archive/Changelogs/Screen2_RCS_Primary_Loop/RCS_Technical_Specifications.md
Updates/Archive/Changelogs/Screen2_RCS_Primary_Loop/RCSPrimaryLoopScreen.cs
Updates/Archive/Changelogs/Screen2_RCS_Primary_Loop/README.md
Updates/Archive/Changelogs/Screen2_RCS_Primary_Loop/Screen2_Assembly_Instructions.md

### Manuals/
Manuals/ReactorOperatorGUI_Design_v1_0_0_0.md
Manuals/Section_0_Title_and_Overview.md
Manuals/Section_1_Vertical_Edgewise_Meter.md
Manuals/Section_2_Vertical_Bar_Graph.md
Manuals/Section_3_Strip_Chart_Recorder.md
Manuals/Section_4_Annunciator_Window_Tile.md
Manuals/Section_5_Indicator_Lamp.md
Manuals/Section_6_Digital_Numeric_Readout.md
Manuals/Section_7_Guidelines_and_Quick_Reference.md
Manuals/T_HOT_Gauge_Blender_Manual.md
Manuals/Unity_Implementation_Manual_v1_0_0_0.md

---

## 10. Configuration

### ProjectSettings/
ProjectSettings/AudioManager.asset
ProjectSettings/ClusterInputManager.asset
ProjectSettings/DynamicsManager.asset
ProjectSettings/EditorBuildSettings.asset
ProjectSettings/EditorSettings.asset
ProjectSettings/GraphicsSettings.asset
ProjectSettings/InputManager.asset
ProjectSettings/MemorySettings.asset
ProjectSettings/MultiplayerManager.asset
ProjectSettings/NavMeshAreas.asset
ProjectSettings/NetworkManager.asset
ProjectSettings/PackageManagerSettings.asset
ProjectSettings/Physics2DSettings.asset
ProjectSettings/PresetManager.asset
ProjectSettings/ProjectSettings.asset
ProjectSettings/ProjectVersion.txt
ProjectSettings/QualitySettings.asset
ProjectSettings/ShaderGraphSettings.asset
ProjectSettings/TagManager.asset
ProjectSettings/TimeManager.asset
ProjectSettings/UnityConnectSettings.asset
ProjectSettings/URPProjectSettings.asset
ProjectSettings/VersionControlSettings.asset
ProjectSettings/VFXManager.asset
ProjectSettings/XRSettings.asset
ProjectSettings/Packages/com.unity.dedicated-server/MultiplayerRolesSettings.asset

### Assets/Settings/
Assets/Settings/Renderer2D.asset
Assets/Settings/UniversalRP.asset
Assets/Settings/Scenes/URP2DSceneTemplate.unity

---

## 11. Other

### Root Configuration
PROJECT_CONSTITUTION.md
Assets/DefaultVolumeProfile.asset
Assets/UniversalRenderPipelineGlobalSettings.asset

### Prefabs
Assets/Prefabs/Screens/README_Prefabs.md

### Build Artifacts
Build/Critical_BurstDebugInformation_DoNotShip/Data/Plugins/x86_64/lib_burst_generated.txt

### HeatupLogs
HeatupLogs/Heatup_Interval_001_0.25hr.txt
HeatupLogs/Heatup_Interval_003_0.50hr.txt
HeatupLogs/Heatup_Interval_005_0.75hr.txt

### Build HeatupLogs
Build/HeatupLogs/Heatup_Interval_001_0.00hr.txt
Build/HeatupLogs/Heatup_Interval_002_0.25hr.txt
Build/HeatupLogs/Heatup_Interval_004_0.50hr.txt
Build/HeatupLogs/Heatup_Interval_006_0.75hr.txt
Build/HeatupLogs/Heatup_Interval_008_1.00hr.txt
Build/HeatupLogs/Heatup_Interval_010_1.25hr.txt
Build/HeatupLogs/Heatup_Interval_012_1.50hr.txt
Build/HeatupLogs/Heatup_Interval_014_1.75hr.txt
Build/HeatupLogs/Heatup_Interval_016_2.00hr.txt
Build/HeatupLogs/Heatup_Interval_018_2.25hr.txt
Build/HeatupLogs/Heatup_Interval_020_2.50hr.txt
Build/HeatupLogs/Heatup_Interval_022_2.75hr.txt
Build/HeatupLogs/Heatup_Interval_024_3.00hr.txt
Build/HeatupLogs/Heatup_Interval_026_3.25hr.txt
Build/HeatupLogs/Heatup_Interval_028_3.50hr.txt
Build/HeatupLogs/Heatup_Interval_030_3.75hr.txt
Build/HeatupLogs/Heatup_Interval_032_4.00hr.txt
Build/HeatupLogs/Heatup_Interval_034_4.25hr.txt
Build/HeatupLogs/Heatup_Interval_036_4.50hr.txt
Build/HeatupLogs/Heatup_Interval_038_4.75hr.txt
Build/HeatupLogs/Heatup_Interval_040_5.00hr.txt
Build/HeatupLogs/Heatup_Interval_042_5.25hr.txt
Build/HeatupLogs/Heatup_Interval_044_5.50hr.txt
Build/HeatupLogs/Heatup_Interval_046_5.75hr.txt
Build/HeatupLogs/Heatup_Interval_048_6.00hr.txt
Build/HeatupLogs/Heatup_Interval_050_6.25hr.txt
Build/HeatupLogs/Heatup_Interval_052_6.50hr.txt
Build/HeatupLogs/Heatup_Interval_054_6.75hr.txt
Build/HeatupLogs/Heatup_Interval_056_7.00hr.txt
Build/HeatupLogs/Heatup_Interval_058_7.25hr.txt
Build/HeatupLogs/Heatup_Interval_060_7.50hr.txt
Build/HeatupLogs/Heatup_Interval_062_7.75hr.txt
Build/HeatupLogs/Heatup_Interval_064_8.00hr.txt
Build/HeatupLogs/Heatup_Interval_066_8.25hr.txt
Build/HeatupLogs/Heatup_Interval_068_8.50hr.txt
Build/HeatupLogs/Heatup_Interval_070_8.75hr.txt
Build/HeatupLogs/Heatup_Interval_072_9.00hr.txt
Build/HeatupLogs/Heatup_Interval_074_9.25hr.txt

### TextMesh Pro (License Files)
Assets/TextMesh Pro/Examples & Extras/Fonts/Anton OFL.txt
Assets/TextMesh Pro/Examples & Extras/Fonts/Bangers - OFL.txt
Assets/TextMesh Pro/Examples & Extras/Fonts/Oswald-Bold - OFL.txt
Assets/TextMesh Pro/Examples & Extras/Fonts/Roboto-Bold - AFL.txt
Assets/TextMesh Pro/Examples & Extras/Fonts/Roboto-Bold - License.txt
Assets/TextMesh Pro/Examples & Extras/Fonts/Unity - OFL.txt
Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt
Assets/TextMesh Pro/Resources/LineBreaking Following Characters.txt
Assets/TextMesh Pro/Resources/LineBreaking Leading Characters.txt
Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt

### TextMesh Pro (Example Scenes)
Assets/TextMesh Pro/Examples & Extras/Scenes/01-  Single Line TextMesh Pro.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/02 - Multi-line TextMesh Pro.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/03 - Line Justification.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/04 - Word Wrapping.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/05 - Style Tags.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/06 - Extra Rich Text Examples.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/07 - Superscript & Subscript Example.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/08 - Improved Text Alignment.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/09 - Margin Tag Example.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/10 - Bullets & Numbered List Example.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/11 - The Style Tag.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/12 - Link Example.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/12a - Text Interactions.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/13 - Soft Hyphenation.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/14 - Multi Font & Sprites.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/15 - Inline Graphics & Sprites.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/16 - Linked text overflow mode example.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/17 - Old Computer Terminal.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/18 - ScrollRect & Masking & Layout.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/19 - Masking Texture & Soft Mask.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/20 - Input Field with Scrollbar.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/21 - Script Example.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/22 - Basic Scripting Example.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/23 - Animating Vertex Attributes.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/24 - Surface Shader Example URP.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/24 - Surface Shader Example.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/25 - Sunny Days Example.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/26 - Dropdown Placeholder Example.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/27 - Double Pass Shader Example.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/28 - HDRP Shader Example.unity
Assets/TextMesh Pro/Examples & Extras/Scenes/Benchmark (Floating Text).unity

### TextMesh Pro (Example Scripts)
Assets/TextMesh Pro/Examples & Extras/Scripts/Benchmark01.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/Benchmark01_UGUI.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/Benchmark02.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/Benchmark03.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/Benchmark04.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/CameraController.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/ChatController.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/DropdownSample.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/EnvMapAnimator.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/ObjectSpin.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/ShaderPropAnimator.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/SimpleScript.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/SkewTextExample.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/TeleType.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/TextConsoleSimulator.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/TextMeshProFloatingText.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/TextMeshSpawner.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_DigitValidator.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_ExampleScript_01.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_FrameRateCounter.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_PhoneNumberValidator.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextEventCheck.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextEventHandler.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextInfoDebugTool.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextSelector_A.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextSelector_B.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_UiFrameRateCounter.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/TMPro_InstructionOverlay.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/VertexColorCycler.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/VertexJitter.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/VertexShakeA.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/VertexShakeB.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/VertexZoom.cs
Assets/TextMesh Pro/Examples & Extras/Scripts/WarpTextExample.cs

### TextMesh Pro (Assets)
Assets/TextMesh Pro/Examples & Extras/Resources/Color Gradient Presets/Blue to Purple - Vertical.asset
Assets/TextMesh Pro/Examples & Extras/Resources/Color Gradient Presets/Dark to Light Green - Vertical.asset
Assets/TextMesh Pro/Examples & Extras/Resources/Color Gradient Presets/Light to Dark Green - Vertical.asset
Assets/TextMesh Pro/Examples & Extras/Resources/Color Gradient Presets/Yellow to Orange - Vertical.asset
Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Anton SDF.asset
Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Bangers SDF.asset
Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset
Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Oswald Bold SDF.asset
Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset
Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Unity SDF.asset
Assets/TextMesh Pro/Examples & Extras/Resources/Sprite Assets/Default Sprite Asset.asset
Assets/TextMesh Pro/Examples & Extras/Resources/Sprite Assets/DropCap Numbers.asset
Assets/TextMesh Pro/Examples & Extras/Scenes/28 - HDRP Shader Example/Sky and Fog Volume Profile.asset
Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset
Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset
Assets/TextMesh Pro/Resources/Sprite Assets/EmojiOne.asset
Assets/TextMesh Pro/Resources/Style Sheets/Default Style Sheet.asset
Assets/TextMesh Pro/Resources/TMP Settings.asset

### UI Marker
Assets/Scripts/UI/MultiScreenBuilder_TEMP_MARKER.txt

---

## Exclusions Applied
- .claude/worktrees/ (Claude Code worktrees)
- .meta files (Unity metadata)
- .plastic/ (Plastic SCM)
- Library/, Temp/, Logs/, obj/ (Unity build artifacts)
- UserSettings/ (Unity user-specific settings)

---

**Note**: This snapshot captures the project structure at v5.4.1 baseline for recovery audit purposes. All paths shown are relative to the project root at `C:\Users\craig\Projects\Critical\`.
