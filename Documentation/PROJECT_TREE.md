# Critical Simulator Project Tree
## For External Architectural Review
### Generated: February 2026

---

## Legend

| Marker | Meaning |
|--------|---------|
| ★ [PZR DRAIN] | Implements PZR drain logic |
| ★ [BUBBLE/TWO-PHASE] | Implements bubble/two-phase transition logic |
| ★ [CANONICAL_SOLID] | Computes CANONICAL_SOLID mass |
| ★ [CANONICAL_TWO_PHASE] | Computes CANONICAL_TWO_PHASE mass |
| ★ [RVLIS] | Handles RVLIS calculation |
| [GOLD] | GOLD Standard module — do not modify without authorization |

---

```
Critical/
│
├── Assets/
│   ├── Animations/
│   ├── Documentation/
│   ├── InputActions/
│   │   └── InputSystem_Actions.inputactions
│   ├── Materials/
│   ├── Models/
│   ├── Prefabs/
│   ├── Resources/
│   │   ├── Fonts & Materials/
│   │   ├── Images/
│   │   ├── ReactorOperatorPanel/
│   │   └── Sprites/
│   ├── Scenes/
│   ├── Settings/
│   ├── TextMesh Pro/
│   ├── Textures/
│   ├── _Recovery/
│   │
│   └── Scripts/
│       │
│       ├── Blender/
│       │   ├── create_reactor_panel.py                     # Blender script for reactor panel model generation
│       │   └── render_panel_textures.py                    # Blender script for panel texture rendering
│       │
│       ├── Core/
│       │   ├── SceneBridge.cs                              # Scene persistence and DontDestroyOnLoad management
│       │   └── WindowFocusManager.cs                       # Application window focus handling
│       │
│       ├── Physics/                                        # ══════════════════════════════════════════════════
│       │   │                                               #   CORE PHYSICS MODULES
│       │   │                                               # ══════════════════════════════════════════════════
│       │   │
│       │   ├── AlarmManager.cs                             # Centralized annunciator setpoint checking
│       │   │
│       │   ├── BRSPhysics.cs                               # [GOLD] Boron Recycle System — holdup tank and evaporator model
│       │   │
│       │   ├── CoupledThermo.cs                            # P-T-V equilibrium solver for closed RCS
│       │   │
│       │   ├── CVCSController.cs                           # CVCS PI controller, letdown path selection, heater mode control
│       │   │
│       │   ├── FluidFlow.cs                                # Fluid flow calculations (friction, velocity, head loss)
│       │   │
│       │   ├── HeatTransfer.cs                             # Insulation losses, surge line natural convection
│       │   │
│       │   ├── HZPStabilizationController.cs               # HZP state machine for Hot Zero Power stabilization
│       │   │
│       │   ├── LoopThermodynamics.cs                       # T_hot/T_cold calculation for RCS loops
│       │   │
│       │   ├── PlantConstants.cs                           # Core Westinghouse 4-Loop reference values
│       │   ├── PlantConstants.BRS.cs                       # BRS-specific constants (holdup capacity, evaporator rates)
│       │   ├── PlantConstants.CVCS.cs                      # CVCS constants (flows, orifices, setpoints)
│       │   ├── PlantConstants.Heatup.cs                    # Heatup-specific constants and procedural limits
│       │   ├── PlantConstants.Nuclear.cs                   # Reactor physics constants (reactivity coefficients, kinetics)
│       │   ├── PlantConstants.Pressure.cs                  # Pressure control constants and interlocks
│       │   ├── PlantConstants.Pressurizer.cs               # PZR geometry, heater groups, spray constants
│       │   ├── PlantConstants.RHR.cs                       # RHR system constants (flow rates, HX UA, temperatures)
│       │   ├── PlantConstants.SG.cs                        # Steam generator thermal constants
│       │   ├── PlantConstants.SteamDump.cs                 # Steam dump controller constants
│       │   ├── PlantConstants.Validation.cs                # Validation test setpoints and tolerances
│       │   │
│       │   ├── PressurizerPhysics.cs                       # ★ [CANONICAL_TWO_PHASE] ★ [BUBBLE/TWO-PHASE]
│       │   │                                               #   Three-region PZR model (subcooled/saturated/steam)
│       │   │                                               #   Flash evaporation, spray condensation, heater dynamics
│       │   │                                               #   Wall condensation, rainout, thermal lag (tau=20s)
│       │   │                                               #   Computes two-phase PZR mass from water+steam volumes
│       │   │
│       │   ├── RCPSequencer.cs                             # RCP startup timing, NPSH requirements, permissive logic
│       │   │
│       │   ├── RCSHeatup.cs                                # Isolated and bulk RCS heatup step calculations
│       │   │
│       │   ├── ReactorKinetics.cs                          # Point kinetics model (delayed neutron precursors, reactivity)
│       │   │
│       │   ├── RHRSystem.cs                                # [GOLD] RHR thermal model — pump heat, HX bypass, isolation sequence
│       │   │
│       │   ├── RVLISPhysics.cs                             # ★ [RVLIS]
│       │   │                                               #   Reactor Vessel Level Indication System physics
│       │   │                                               #   Dynamic Range (RCPs on), Full Range (RCPs off), Upper Range
│       │   │                                               #   ΔP-based level calculation with flow compensation
│       │   │
│       │   ├── SGForensics.cs                              # SG diagnostic data collection and CSV logging
│       │   │
│       │   ├── SGMultiNodeThermal.cs                       # [GOLD] Multi-node SG secondary model — three-regime physics
│       │   │                                               #   Regime 1: Subcooled (closed system, thermocline)
│       │   │                                               #   Regime 2: Boiling (open system, steam exits MSIVs)
│       │   │                                               #   Regime 3: Steam dump controlled (HZP stabilization)
│       │   │
│       │   ├── SGSecondaryThermal.cs                       # SG secondary steaming detection logic
│       │   │
│       │   ├── SolidPlantPressure.cs                       # ★ [CANONICAL_SOLID] ★ [PZR DRAIN] ★ [BUBBLE/TWO-PHASE]
│       │   │                                               #   [GOLD] Solid PZR P-T-V coupling during cold shutdown
│       │   │                                               #   CVCS pressure control (PI controller for charging/letdown)
│       │   │                                               #   Thermal expansion → surge flow calculation
│       │   │                                               #   Bubble detection (T_pzr >= T_sat transition)
│       │   │                                               #   Mass conservation: PzrWaterMass updated only by surge transfer
│       │   │                                               #   v5.0.2: Canonical mass tracking, SurgeMassTransfer_lb field
│       │   │
│       │   ├── SteamDumpController.cs                      # Steam dump control for HZP heat removal to condenser
│       │   │
│       │   ├── SteamThermodynamics.cs                      # Steam-side thermodynamic calculations
│       │   │
│       │   ├── ThermalExpansion.cs                         # Volumetric expansion coefficients, isothermal compressibility
│       │   │                                               #   β(T,P) and κ(T,P) with saturation enhancement
│       │   │
│       │   ├── ThermalMass.cs                              # Heat capacity of RCS metal structures + fluid inventory
│       │   │
│       │   ├── TimeAcceleration.cs                         # Dual-clock time warp (wall time vs simulation time)
│       │   │
│       │   ├── VCTPhysics.cs                               # [GOLD] Volume Control Tank inventory and boron tracking
│       │   │                                               #   Level control, auto-makeup, divert logic
│       │   │                                               #   Mass balance verification
│       │   │
│       │   ├── WaterProperties.cs                          # [GOLD] NIST-validated steam tables
│       │   │                                               #   Saturation temperature/pressure
│       │   │                                               #   Density (liquid/steam), specific heat
│       │   │                                               #   Enthalpy (liquid/fg/steam)
│       │   │                                               #   Valid range: 1-3000 psia, 100-700°F
│       │   │
│       │   └── Critical.Physics.asmdef                     # Assembly definition for Critical.Physics namespace
│       │
│       ├── Reactor/                                        # ══════════════════════════════════════════════════
│       │   │                                               #   REACTOR CORE MODELS
│       │   │                                               # ══════════════════════════════════════════════════
│       │   │
│       │   ├── ControlRodBank.cs                           # Control rod bank position tracking and movement
│       │   │
│       │   ├── FeedbackCalculator.cs                       # Moderator temperature coefficient, Doppler feedback
│       │   │
│       │   ├── FuelAssembly.cs                             # Individual fuel assembly thermal-hydraulic model
│       │   │
│       │   ├── PowerCalculator.cs                          # Reactor power calculation from neutron flux
│       │   │
│       │   ├── ReactorController.cs                        # High-level reactor control logic (rod control, power)
│       │   │
│       │   ├── ReactorCore.cs                              # Core geometry, fuel loading pattern, power distribution
│       │   │
│       │   └── ReactorSimEngine.cs                         # Power operations simulation engine (at-power scenarios)
│       │
│       ├── Tests/                                          # ══════════════════════════════════════════════════
│       │   │                                               #   VALIDATION & TEST SUITES
│       │   │                                               # ══════════════════════════════════════════════════
│       │   │
│       │   ├── AcceptanceTests_v5_4_0.cs                   # v5.4.0 acceptance criteria validation suite
│       │   │
│       │   ├── HeatupIntegrationTests.cs                   # End-to-end heatup simulation integration tests
│       │   │
│       │   ├── IntegrationTests.cs                         # Cross-system integration test suite
│       │   │
│       │   ├── Phase1TestRunner.cs                         # Phase 1 physics module unit test runner
│       │   │
│       │   ├── Phase2TestRunner.cs                         # Phase 2 reactor core test runner
│       │   │
│       │   ├── Phase2UnityTestRunner.cs                    # Unity Test Framework integration for Phase 2
│       │   │
│       │   └── UnityTestRunner.cs                          # Generic Unity test execution wrapper
│       │
│       ├── UI/                                             # ══════════════════════════════════════════════════
│       │   │                                               #   USER INTERFACE COMPONENTS
│       │   │                                               # ══════════════════════════════════════════════════
│       │   │
│       │   ├── Editor/
│       │   │   ├── InstrumentMaterialSetup.cs              # Editor tool for instrument material configuration
│       │   │   ├── InstrumentSpriteGenerator.cs            # Editor tool for procedural sprite generation
│       │   │   └── InstrumentSpriteImporter.cs             # Editor tool for sprite import processing
│       │   │
│       │   ├── AssemblyDetailPanel.cs                      # Fuel assembly detail popup panel
│       │   ├── AuxiliarySystemsScreen.cs                   # Auxiliary systems overview screen
│       │   ├── CoreMapData.cs                              # Core map data structures and lookups
│       │   ├── CoreMosaicMap.cs                            # Core map mosaic display component
│       │   ├── CVCSScreen.cs                               # CVCS system control and monitoring screen
│       │   ├── MosaicAlarmPanel.cs                         # Annunciator alarm panel widget
│       │   ├── MosaicBoard.cs                              # Main mosaic board controller
│       │   ├── MosaicBoardBuilder.cs                       # Mosaic board procedural UI construction
│       │   ├── MosaicBoardSetup.cs                         # Mosaic board initialization and configuration
│       │   ├── MosaicControlPanel.cs                       # Mosaic control panel widget container
│       │   ├── MosaicGauge.cs                              # Generic gauge widget for mosaic displays
│       │   ├── MosaicIndicator.cs                          # Status indicator lamp widget
│       │   ├── MosaicRodDisplay.cs                         # Control rod position indicator display
│       │   ├── MosaicTypes.cs                              # Type definitions for mosaic UI system
│       │   ├── MultiScreenBuilder.cs                       # Multi-screen layout construction system
│       │   ├── OperatorScreen.cs                           # Base operator screen class
│       │   ├── OperatorScreenBuilder.cs                    # Operator screen construction helper
│       │   ├── PlantOverviewScreen.cs                      # Plant-wide status overview screen
│       │   ├── PressurizerScreen.cs                        # Pressurizer control and status screen
│       │   ├── RCPControlPanel.cs                          # RCP start/stop control panel
│       │   ├── RCSGaugeTypes.cs                            # RCS-specific gauge type definitions
│       │   ├── RCSPrimaryLoopScreen.cs                     # RCS primary loop mimic diagram screen
│       │   ├── RCSVisualizationController.cs               # RCS 3D visualization controller
│       │   ├── ReactorOperatorGUI_IntegrationTests.cs      # GUI integration test stubs
│       │   ├── ReactorOperatorScreen.cs                    # Main reactor operator workstation screen
│       │   ├── ReactorOperatorScreenSkin.cs                # Operator screen visual styling/theming
│       │   ├── ReactorScreenAdapter.cs                     # Screen data adapter for engine decoupling
│       │   ├── RodControlPanel.cs                          # Control rod manipulation panel
│       │   ├── ScreenDataBridge.cs                         # Data bridge between engine and UI screens
│       │   ├── ScreenManager.cs                            # Multi-screen navigation and lifecycle manager
│       │   ├── SecondarySystemsScreen.cs                   # Secondary systems overview screen
│       │   ├── SteamGeneratorScreen.cs                     # Steam generator monitoring screen
│       │   └── TurbineGeneratorScreen.cs                   # Turbine/generator status screen
│       │
│       └── Validation/                                     # ══════════════════════════════════════════════════
│           │                                               #   HEATUP SIMULATION ENGINE (Partial Classes)
│           │                                               # ══════════════════════════════════════════════════
│           │
│           ├── HeatupSimEngine.cs                          # ★ [CANONICAL_SOLID] ★ [CANONICAL_TWO_PHASE]
│           │                                               #   [GOLD] Core state container, lifecycle management
│           │                                               #   Physics dispatch coordinator (calls all modules)
│           │                                               #   CANONICAL mass variables: TotalPrimaryMass_lb
│           │                                               #   Derives LoopMass implicitly (total - PZR)
│           │                                               #   Time acceleration, frame-rate decoupling
│           │
│           ├── HeatupSimEngine.Init.cs                     #   Cold/warm start initialization
│           │                                               #   Sets initial conditions for all state variables
│           │
│           ├── HeatupSimEngine.BubbleFormation.cs          # ★ [BUBBLE/TWO-PHASE] ★ [PZR DRAIN]
│           │                                               #   [GOLD] 7-phase bubble formation state machine
│           │                                               #   Phases: NONE→DETECTION→VERIFICATION→DRAIN→
│           │                                               #           STABILIZE→PRESSURIZE→COMPLETE
│           │                                               #   Steam displacement calculation (v5.4.0)
│           │                                               #   Thermodynamic drain (not mechanical)
│           │                                               #   CCP start logic, aux spray test model
│           │
│           ├── HeatupSimEngine.CVCS.cs                     #   CVCS flow control dispatch
│           │                                               #   RCS inventory mass tracking (two-phase)
│           │                                               #   VCT physics update coordination
│           │                                               #   Letdown path state management
│           │
│           ├── HeatupSimEngine.HZP.cs                      #   HZP stabilization and RCP handoff logic
│           │                                               #   Steam dump activation sequencing
│           │
│           ├── HeatupSimEngine.Alarms.cs                   #   Annunciator evaluation and edge detection
│           │                                               #   RVLIS state update dispatch
│           │                                               #   Alarm history tracking
│           │
│           ├── HeatupSimEngine.Logging.cs                  #   Event log management
│           │                                               #   History buffer ring storage
│           │                                               #   CSV file output for forensics
│           │
│           ├── HeatupValidationVisual.cs                   #   Main validation dashboard GUI controller
│           ├── HeatupValidationVisual.Annunciators.cs      #   Annunciator panel rendering
│           ├── HeatupValidationVisual.Gauges.cs            #   Gauge widget rendering
│           ├── HeatupValidationVisual.Graphs.cs            #   Real-time trend graph plotting
│           ├── HeatupValidationVisual.Panels.cs            #   Dashboard panel layout management
│           ├── HeatupValidationVisual.Styles.cs            #   Visual style definitions (colors, fonts)
│           ├── HeatupValidationVisual.TabCritical.cs       #   Critical parameters monitoring tab
│           ├── HeatupValidationVisual.TabCVCS.cs           #   CVCS system monitoring tab
│           ├── HeatupValidationVisual.TabEventLog.cs       #   Event log display tab
│           ├── HeatupValidationVisual.TabOverview.cs       #   Overview summary tab
│           ├── HeatupValidationVisual.TabPressurizer.cs    #   Pressurizer monitoring tab
│           ├── HeatupValidationVisual.TabRCPElectrical.cs  #   RCP electrical monitoring tab
│           ├── HeatupValidationVisual.TabSGRHR.cs          #   SG and RHR monitoring tab
│           └── HeatupValidationVisual.TabValidation.cs     #   Validation criteria pass/fail tab
│
├── Documentation/                                          # ══════════════════════════════════════════════════
│   │                                                       #   DESIGN DOCUMENTATION
│   │                                                       # ══════════════════════════════════════════════════
│   │
│   ├── Implementation/
│   │   ├── Archive/
│   │   ├── Phase 1/
│   │   ├── Phase 2/
│   │   ├── Phase 3/
│   │   ├── Phase 4/
│   │   ├── Phase 5/
│   │   ├── Phase 6/
│   │   ├── Critical_Validation_Report.md
│   │   └── Phase_Mapping_Analysis.md
│   │
│   ├── Updates/
│   │   ├── DESIGN_BubbleFormation_HeaterControl_v1.4.0.0.md
│   │   └── HANDOVER_PZR_BubbleFormation_and_RCP_Bugs.md
│   │
│   ├── Critical_Phase1_Physics_Engine.docx                 # Phase 1 design specification
│   ├── Critical_Phase1_Addendum_CoupledThermo.docx         # CoupledThermo addendum
│   ├── Critical_Phase1_Gap_Analysis.docx                   # Gap analysis for Phase 1
│   ├── Critical_Phase2_Reactor_Core.docx                   # Phase 2 reactor core design
│   ├── Critical_Phase3_Pressurizer.docx                    # Phase 3 pressurizer design
│   ├── Critical_Phase4_CVCS_RCPs.docx                      # Phase 4 CVCS and RCP design
│   ├── Critical_Phase5_Steam_Generators.docx               # Phase 5 SG design
│   ├── Critical_Phase6_Turbine_Generator.docx              # Phase 6 turbine/generator design
│   ├── CVCS_System_Analysis_v1.docx                        # CVCS detailed analysis
│   ├── GOLD_STANDARD_TEMPLATE.md                           # Template for GOLD standard modules
│   ├── PROJECT_OVERVIEW.md                                 # Project overview and architecture
│   ├── Project_Summary_For_Continuation.md                 # Handoff summary document
│   ├── PWR_Heatup_Simulation_Analysis_Report.md            # Heatup simulation analysis
│   ├── PWR_Master_Development_Plan_v5.docx                 # Master development plan
│   ├── PWR_Physics_Review_Summary.md                       # Physics implementation review
│   ├── REFACTORING_PLAN.md                                 # Code refactoring plan
│   ├── SG_Model_Correct_Analysis.md                        # SG model correctness analysis
│   ├── SG_Secondary_Heating_Implementation.md              # SG secondary heating design
│   └── STRUCTURAL_MAP.md                                   # Codebase structural map
│
├── HeatupLogs/                                             # ══════════════════════════════════════════════════
│   │                                                       #   SIMULATION OUTPUT LOGS
│   │                                                       # ══════════════════════════════════════════════════
│   │
│   ├── Forensics/                                          # Detailed forensic data buffers
│   ├── Heatup_Interval_001_0.25hr.txt                      # Interval snapshot at 0.25 hours
│   ├── Heatup_Interval_003_0.50hr.txt                      # Interval snapshot at 0.50 hours
│   └── Heatup_Interval_005_0.75hr.txt                      # Interval snapshot at 0.75 hours
│
├── Manuals/                                                # ══════════════════════════════════════════════════
│   │                                                       #   OPERATOR INTERFACE MANUALS
│   │                                                       # ══════════════════════════════════════════════════
│   │
│   ├── CRITICAL_Complete_Manual_Collection.pdf             # Complete manual collection
│   ├── ReactorOperatorGUI_Design_v1_0_0_0.md               # GUI design specification
│   ├── Section_0_Title_and_Overview.md                     # Manual section 0
│   ├── Section_1_Vertical_Edgewise_Meter.md                # Edgewise meter specification
│   ├── Section_2_Vertical_Bar_Graph.md                     # Bar graph specification
│   ├── Section_3_Strip_Chart_Recorder.md                   # Strip chart specification
│   ├── Section_4_Annunciator_Window_Tile.md                # Annunciator specification
│   ├── Section_5_Indicator_Lamp.md                         # Indicator lamp specification
│   ├── Section_6_Digital_Numeric_Readout.md                # Digital readout specification
│   ├── Section_7_Guidelines_and_Quick_Reference.md         # Quick reference guide
│   ├── T_HOT_Gauge_Blender_Manual.md                       # T_hot gauge Blender workflow
│   └── Unity_Implementation_Manual_v1_0_0_0.md             # Unity implementation guide
│
├── Technical_Documentation/                                # ══════════════════════════════════════════════════
│   │                                                       #   EXTERNAL TECHNICAL REFERENCES
│   │                                                       # ══════════════════════════════════════════════════
│   │
│   ├── NRC_HRTD_Startup_Pressurization_Reference.md        # NRC HRTD pressurization reference
│   ├── NRC_REFERENCE_SOURCES.md                            # Index of NRC reference documents
│   ├── PZR_Level_Pressure_Deficit_Analysis_v4.4.0.md       # PZR level/pressure analysis
│   ├── RHR_SYSTEM_RESEARCH_v3.0.0.md                       # RHR system research notes
│   ├── SG_MODEL_RESEARCH_HANDOFF.md                        # SG model research handoff
│   ├── SG_Secondary_Pressurization_During_Heatup_Research.md # SG pressurization research
│   └── SG_THERMAL_MODEL_RESEARCH_v3.0.0.md                 # SG thermal model research
│
├── Updates/                                                # ══════════════════════════════════════════════════
│   │                                                       #   IMPLEMENTATION PLANS & CHANGELOGS
│   │                                                       # ══════════════════════════════════════════════════
│   │
│   ├── Archives/                                           # Historical changelogs and plans (v0.1.0 - v3.0.0)
│   │   ├── CHANGELOG v0.1.0.md
│   │   ├── CHANGELOG v0.2.0.md
│   │   ├── ... (extensive archive)
│   │   └── CHANGELOG_v3.0.0.md
│   │
│   ├── Changelogs/
│   │   ├── CHANGELOG_v4.0.0.md
│   │   ├── CHANGELOG_v4.1.0.md
│   │   ├── CHANGELOG_v4.2.2.md
│   │   ├── CHANGELOG_v4.4.0.md
│   │   ├── CHANGELOG_v5.0.0.md
│   │   ├── CHANGELOG_v5.0.1.md
│   │   ├── CHANGELOG_v5.0.2.md                             # Mass conservation fix (canonical mass tracking)
│   │   ├── CHANGELOG_v5.0.3.md
│   │   ├── CHANGELOG_v5.1.0.md
│   │   ├── CHANGELOG_v5.2.0.md
│   │   └── CHANGELOG_v5.3.0.md
│   │
│   ├── Future_Features/
│   │   └── FUTURE_ENHANCEMENTS_ROADMAP.md                  # Planned future enhancements
│   │
│   ├── IMPLEMENTATION_PLAN_v5.0.0.md                       # Three-regime SG model
│   ├── IMPLEMENTATION_PLAN_v5.0.1.md                       # Regime continuity blend
│   ├── IMPLEMENTATION_PLAN_v5.0.2.md                       # Mass conservation fix
│   ├── IMPLEMENTATION_PLAN_v5.0.3.md
│   ├── IMPLEMENTATION_PLAN_v5.1.0.md
│   ├── IMPLEMENTATION_PLAN_v5.2.0.md
│   ├── IMPLEMENTATION_PLAN_v5.3.0.md
│   ├── IMPLEMENTATION_PLAN_v5.3.1.md
│   ├── IMPLEMENTATION_PLAN_v5.3.2.md
│   ├── IMPLEMENTATION_PLAN_v5.4.0.md                       # Volume displacement correction
│   ├── Inventory_Audit_Stage0_v5.3.0.md
│   ├── Inventory_Audit_v1.0.0.md
│   └── SG_HEATUP_BREAKTHROUGH_HANDOFF.md                   # SG physics breakthrough documentation
│
├── Build/                                                  # Unity build output (excluded from tree)
├── ProjectSettings/                                        # Unity project settings
├── UserSettings/                                           # Unity user preferences
│
├── Critical.Physics.csproj                                 # Physics assembly project file
├── Critical.Reactor.csproj                                 # Reactor assembly project file
├── Critical.slnx                                           # Solution file
├── Assembly-CSharp.csproj                                  # Main Unity assembly
└── Assembly-CSharp-Editor.csproj                           # Editor assembly
```

---

## Key File Summary by Function

### Mass Conservation (Canonical Mass Tracking)

| File | Role |
|------|------|
| `HeatupSimEngine.cs` | Owns `TotalPrimaryMass_lb` — the single source of truth for total RCS mass |
| `SolidPlantPressure.cs` | Tracks `PzrWaterMass` during solid operations; computes `SurgeMassTransfer_lb` |
| `PressurizerPhysics.cs` | Computes two-phase PZR mass from `WaterVolume × ρ_water + SteamVolume × ρ_steam` |

### Pressurizer Drain Logic

| File | Role |
|------|------|
| `SolidPlantPressure.cs` | Thermal expansion → surge flow; mass transfer PZR→RCS during heatup |
| `HeatupSimEngine.BubbleFormation.cs` | DRAIN phase: thermodynamic steam displacement + CVCS trim |

### Bubble / Two-Phase Transition

| File | Role |
|------|------|
| `SolidPlantPressure.cs` | Detects `T_pzr >= T_sat`, sets `BubbleFormed = true` |
| `PressurizerPhysics.cs` | `FormBubble()` initializes two-phase state; `ThreeRegionUpdate()` for normal ops |
| `HeatupSimEngine.BubbleFormation.cs` | 7-phase state machine managing the entire bubble formation procedure |

### RVLIS Calculation

| File | Role |
|------|------|
| `RVLISPhysics.cs` | All RVLIS calculation: Dynamic/Full/Upper Range based on RCP state |
| `HeatupSimEngine.Alarms.cs` | Dispatches RVLIS update, checks level alarms |

---

## Excluded Directories

The following directories are excluded from this tree as they contain auto-generated or third-party content:

- `Library/` — Unity cache
- `Temp/` — Unity temporary files
- `obj/` — Build intermediates
- `Packages/` — Unity package manager
- `.git/` — Version control
- `.vs/` — Visual Studio cache
- `Logs/` — Unity editor logs (not simulation logs)

---

*Document generated for external architectural review.*
*Version: v5.4.0*
