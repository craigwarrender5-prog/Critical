// ============================================================================
// CRITICAL: Master the Atom - RCS Primary Loop Operator Screen
// RCSPrimaryLoopScreen.cs - Screen 2: RCS Loops, RCPs, Flow, Temperatures
// ============================================================================
//
// PURPOSE:
//   Implements the RCS Primary Loop operator screen (Key 2) displaying:
//   - 2D schematic of 4-loop RCS (industry-standard mimic diagram)
//   - Loop temperatures (T-hot, T-cold per loop)
//   - RCS flow rates (total and per loop)
//   - RCP controls and status
//   - Color-coded piping based on temperature
//   - Flow direction indicators
//
// LAYOUT (1920x1080):
//   - Left Panel (0-15%): 8 temperature gauges (T-hot/T-cold per loop)
//   - Center Panel (15-65%): 2D RCS mimic schematic
//   - Right Panel (65-100%): 8 flow/power gauges
//   - Bottom Panel (0-26%): 4 RCP control panels, status, alarms
//
// VISUALIZATION:
//   Uses 2D UI mimic diagram (Image components) following the same pattern
//   as Pressurizer, CVCS, SG, and Plant Overview screens. No 3D model,
//   no RenderTexture, no separate camera. This matches the original design
//   specification in Operator_Screen_Layout_Plan_v1_0_0.md Section 3.2.2.
//
// KEYBOARD:
//   - Key 2: Toggle screen visibility
//
// INTEGRATION:
//   - Reads data from ReactorController or HeatupSimEngine
//   - Uses MosaicGauge components for gauge display
//   - Integrates with RCPSequencer for pump state
//
// SOURCES:
//   - Operator_Screen_Layout_Plan_v1_0_0.md (Screen 2 specification)
//   - NRC HRTD Section 3.2 - Reactor Coolant System
//   - RCS_Technical_Specifications.md
//
// VERSION: 2.0.0
// DATE: 2026-02-10
// CLASSIFICATION: UI — Operator Interface
// CHANGE: v2.0.0 — Replaced 3D model + RenderTexture with 2D mimic diagram
//         to match design spec and all other operator screens.
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI
{
    using Controllers;
    using Physics;

    /// <summary>
    /// RCS Primary Loop Operator Screen (Key 2).
    /// Displays 4-loop RCS 2D schematic with temperatures, flows, and RCP controls.
    /// </summary>
    public class RCSPrimaryLoopScreen : OperatorScreen
    {
        // ====================================================================
        // ABSTRACT PROPERTY IMPLEMENTATIONS
        // ====================================================================

        #region OperatorScreen Implementation

        public override KeyCode ToggleKey => KeyCode.Alpha2;
        public override string ScreenName => "RCS PRIMARY LOOP";
        public override int ScreenIndex => 2;

        #endregion

        // ====================================================================
        // CONSTANTS
        // ====================================================================

        #region Constants

        // Westinghouse 4-Loop PWR RCS Specifications
        private const float FLOW_PER_RCP_GPM = 97600f;      // Total flow / 4 loops
        private const float RATED_FLOW_PER_RCP_GPM = 88500f; // Rated design flow
        private const float RCP_RATED_SPEED_RPM = 1189f;     // Nominal RCP speed
        private const float TOTAL_RCS_FLOW_GPM = 390400f;    // Total RCS flow at 100%

        // Gauge ranges based on NRC HRTD specifications
        private const float TEMP_GAUGE_MIN = 100f;
        private const float TEMP_GAUGE_MAX = 700f;
        private const float FLOW_GAUGE_MAX = 120000f;
        private const float TOTAL_FLOW_GAUGE_MAX = 450000f;

        // Temperature color thresholds (°F)
        private const float TEMP_COLD = 200f;
        private const float TEMP_WARM = 400f;
        private const float TEMP_HOT = 550f;
        private const float TEMP_VERY_HOT = 620f;

        #endregion

        // ====================================================================
        // SERIALIZED FIELDS - Inspector Configuration
        // ====================================================================

        #region Inspector Fields - 2D Mimic Diagram

        [Header("=== 2D MIMIC DIAGRAM ===")]
        [Tooltip("Reactor vessel (center of schematic)")]
        [SerializeField] private Image mimic_ReactorVessel;

        [Tooltip("Reactor vessel power overlay text")]
        [SerializeField] private TextMeshProUGUI mimic_ReactorPowerText;

        [Tooltip("Reactor vessel T-avg overlay text")]
        [SerializeField] private TextMeshProUGUI mimic_ReactorTavgText;

        [Header("--- Hot Legs (4 loops) ---")]
        [SerializeField] private Image[] mimic_HotLegs = new Image[4];

        [Header("--- Cold Legs (4 loops) ---")]
        [SerializeField] private Image[] mimic_ColdLegs = new Image[4];

        [Header("--- Steam Generators (4 loops) ---")]
        [SerializeField] private Image[] mimic_SteamGenerators = new Image[4];
        [SerializeField] private TextMeshProUGUI[] mimic_SGLabels = new TextMeshProUGUI[4];

        [Header("--- RCP Icons (4 loops) ---")]
        [SerializeField] private Image[] mimic_RCPIcons = new Image[4];
        [SerializeField] private TextMeshProUGUI[] mimic_RCPStatusTexts = new TextMeshProUGUI[4];

        [Header("--- Crossover Legs (4 loops) ---")]
        [SerializeField] private Image[] mimic_CrossoverLegs = new Image[4];

        [Header("--- Pressurizer (connected to Loop 2 hot leg) ---")]
        [SerializeField] private Image mimic_Pressurizer;
        [SerializeField] private TextMeshProUGUI mimic_PZRText;

        [Header("--- Loop Temperature Overlays ---")]
        [SerializeField] private TextMeshProUGUI[] mimic_THotTexts = new TextMeshProUGUI[4];
        [SerializeField] private TextMeshProUGUI[] mimic_TColdTexts = new TextMeshProUGUI[4];

        #endregion

        #region Inspector Fields - Left Panel (Temperature Gauges)

        [Header("=== LEFT PANEL - TEMPERATURE GAUGES ===")]
        [SerializeField] private MosaicGauge gauge_Loop1_THot;
        [SerializeField] private MosaicGauge gauge_Loop2_THot;
        [SerializeField] private MosaicGauge gauge_Loop3_THot;
        [SerializeField] private MosaicGauge gauge_Loop4_THot;
        [SerializeField] private MosaicGauge gauge_Loop1_TCold;
        [SerializeField] private MosaicGauge gauge_Loop2_TCold;
        [SerializeField] private MosaicGauge gauge_Loop3_TCold;
        [SerializeField] private MosaicGauge gauge_Loop4_TCold;

        #endregion

        #region Inspector Fields - Right Panel (Flow Gauges)

        [Header("=== RIGHT PANEL - FLOW GAUGES ===")]
        [SerializeField] private MosaicGauge gauge_TotalFlow;
        [SerializeField] private MosaicGauge gauge_Loop1_Flow;
        [SerializeField] private MosaicGauge gauge_Loop2_Flow;
        [SerializeField] private MosaicGauge gauge_Loop3_Flow;
        [SerializeField] private MosaicGauge gauge_Loop4_Flow;
        [SerializeField] private MosaicGauge gauge_CorePower;
        [SerializeField] private MosaicGauge gauge_CoreDeltaT;
        [SerializeField] private MosaicGauge gauge_AverageTavg;

        #endregion

        #region Inspector Fields - Bottom Panel (RCP Controls)

        [Header("=== BOTTOM PANEL - RCP CONTROLS ===")]
        [SerializeField] private RCPControlPanel rcpPanel_1;
        [SerializeField] private RCPControlPanel rcpPanel_2;
        [SerializeField] private RCPControlPanel rcpPanel_3;
        [SerializeField] private RCPControlPanel rcpPanel_4;

        #endregion

        #region Inspector Fields - Status Displays

        [Header("=== STATUS DISPLAYS ===")]
        [SerializeField] private TextMeshProUGUI text_RCPCount;
        [SerializeField] private TextMeshProUGUI text_CirculationMode;
        [SerializeField] private TextMeshProUGUI text_PlantMode;
        [SerializeField] private Image indicator_NaturalCirc;

        [Header("=== ALARM PANEL ===")]
        [SerializeField] private Transform alarmListContainer;
        [SerializeField] private GameObject alarmEntryPrefab;
        [SerializeField] private int maxAlarmEntries = 8;

        #endregion

        #region Inspector Fields - Colors

        [Header("=== COLOR CONFIGURATION ===")]
        [SerializeField] private Color color_Running = new Color(0.2f, 0.9f, 0.2f);
        [SerializeField] private Color color_Stopped = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color color_Ramping = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private Color color_Tripped = new Color(0.9f, 0.2f, 0.2f);

        // Temperature-based piping colors (matching design spec Appendix B)
        [Header("=== PIPING TEMPERATURE COLORS ===")]
        [SerializeField] private Color color_PipeCold = new Color(0.2f, 0.4f, 0.8f);      // Blue
        [SerializeField] private Color color_PipeWarm = new Color(0.2f, 0.8f, 0.8f);      // Cyan
        [SerializeField] private Color color_PipeHot = new Color(0.8f, 0.5f, 0.2f);       // Orange
        [SerializeField] private Color color_PipeVeryHot = new Color(0.9f, 0.2f, 0.2f);   // Red

        #endregion

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        #region Private Fields

        // Data sources
        private ReactorController _reactorController;
        private MosaicBoard _mosaicBoard;

        // Gauge arrays for iteration
        private MosaicGauge[] _tHotGauges;
        private MosaicGauge[] _tColdGauges;
        private MosaicGauge[] _flowGauges;
        private RCPControlPanel[] _rcpPanels;

        // RCP state tracking
        private bool[] _rcpRunning = new bool[4];
        private float[] _rcpFlowFractions = new float[4];
        private float[] _rcpStartTimes = new float[4];

        // Alarm management
        private List<GameObject> _alarmEntries = new List<GameObject>();
        private Queue<string> _pendingAlarms = new Queue<string>();

        #endregion

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // Build gauge arrays for easier iteration
            _tHotGauges = new MosaicGauge[] {
                gauge_Loop1_THot, gauge_Loop2_THot, gauge_Loop3_THot, gauge_Loop4_THot
            };

            _tColdGauges = new MosaicGauge[] {
                gauge_Loop1_TCold, gauge_Loop2_TCold, gauge_Loop3_TCold, gauge_Loop4_TCold
            };

            _flowGauges = new MosaicGauge[] {
                gauge_Loop1_Flow, gauge_Loop2_Flow, gauge_Loop3_Flow, gauge_Loop4_Flow
            };

            _rcpPanels = new RCPControlPanel[] {
                rcpPanel_1, rcpPanel_2, rcpPanel_3, rcpPanel_4
            };

            // Initialize RCP start times to "not started"
            for (int i = 0; i < 4; i++)
            {
                _rcpStartTimes[i] = float.MaxValue;
            }
        }

        protected override void Start()
        {
            base.Start();

            // Find data sources
            FindDataSources();

            // Initialize gauges with proper ranges
            InitializeGauges();

            // Initialize RCP control panels
            InitializeRCPPanels();

            Debug.Log($"[RCSPrimaryLoopScreen] Initialized (2D mimic). Data source: " +
                      $"{(_reactorController != null ? "ReactorController" : "None")}");
        }

        protected override void Update()
        {
            base.Update();

            if (IsVisible)
            {
                // Update all displays
                UpdateGaugeValues();
                UpdateRCPStatus();
                UpdateMimicDiagram();
                UpdateStatusDisplays();
                ProcessPendingAlarms();
            }
        }

        #endregion

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        #region Initialization

        /// <summary>
        /// Find available data sources in the scene.
        /// </summary>
        private void FindDataSources()
        {
            _reactorController = FindObjectOfType<ReactorController>();
            _mosaicBoard = FindObjectOfType<MosaicBoard>();

            if (_reactorController == null)
            {
                Debug.LogWarning("[RCSPrimaryLoopScreen] ReactorController not found. Screen will show default values.");
            }
        }

        /// <summary>
        /// Initialize all gauges with proper labels, ranges, and thresholds.
        /// </summary>
        private void InitializeGauges()
        {
            string[] loopNames = { "LOOP 1", "LOOP 2", "LOOP 3", "LOOP 4" };

            for (int i = 0; i < 4; i++)
            {
                if (_tHotGauges[i] != null)
                {
                    _tHotGauges[i].CustomLabel = $"{loopNames[i]} T-HOT";
                    _tHotGauges[i].UseCustomThresholds = true;
                    _tHotGauges[i].CustomWarningHigh = 620f;
                    _tHotGauges[i].CustomAlarmHigh = 650f;
                }

                if (_tColdGauges[i] != null)
                {
                    _tColdGauges[i].CustomLabel = $"{loopNames[i]} T-COLD";
                    _tColdGauges[i].UseCustomThresholds = true;
                    _tColdGauges[i].CustomWarningHigh = 560f;
                    _tColdGauges[i].CustomAlarmHigh = 580f;
                }

                if (_flowGauges[i] != null)
                {
                    _flowGauges[i].CustomLabel = $"{loopNames[i]} FLOW";
                    _flowGauges[i].UseCustomThresholds = true;
                    _flowGauges[i].CustomWarningLow = 70000f;
                    _flowGauges[i].CustomAlarmLow = 60000f;
                }
            }

            if (gauge_TotalFlow != null)
            {
                gauge_TotalFlow.CustomLabel = "TOTAL RCS FLOW";
                gauge_TotalFlow.UseCustomThresholds = true;
                gauge_TotalFlow.CustomWarningLow = 280000f;
                gauge_TotalFlow.CustomAlarmLow = 240000f;
            }

            if (gauge_CorePower != null)
            {
                gauge_CorePower.CustomLabel = "CORE POWER";
                gauge_CorePower.UseCustomThresholds = true;
                gauge_CorePower.CustomWarningHigh = 30f;
                gauge_CorePower.CustomAlarmHigh = 40f;
            }

            if (gauge_CoreDeltaT != null)
            {
                gauge_CoreDeltaT.CustomLabel = "CORE ΔT";
                gauge_CoreDeltaT.UseCustomThresholds = true;
                gauge_CoreDeltaT.CustomWarningHigh = 65f;
                gauge_CoreDeltaT.CustomAlarmHigh = 70f;
            }

            if (gauge_AverageTavg != null)
            {
                gauge_AverageTavg.CustomLabel = "AVG T-AVG";
                gauge_AverageTavg.UseCustomThresholds = true;
                gauge_AverageTavg.CustomWarningHigh = 595f;
                gauge_AverageTavg.CustomAlarmHigh = 610f;
            }
        }

        /// <summary>
        /// Initialize RCP control panels with callbacks.
        /// </summary>
        private void InitializeRCPPanels()
        {
            for (int i = 0; i < 4; i++)
            {
                if (_rcpPanels[i] != null)
                {
                    int pumpIndex = i; // Capture for closure
                    _rcpPanels[i].Initialize(
                        pumpNumber: i + 1,
                        onStartRequested: () => OnRCPStartRequested(pumpIndex),
                        onStopRequested: () => OnRCPStopRequested(pumpIndex)
                    );
                }
            }
        }

        #endregion

        // ====================================================================
        // UPDATE METHODS
        // ====================================================================

        #region Update Methods

        /// <summary>
        /// Update all gauge values from data source.
        /// </summary>
        private void UpdateGaugeValues()
        {
            float tHot = _reactorController?.Thot ?? 557f;
            float tCold = _reactorController?.Tcold ?? 554f;
            float tAvg = _reactorController?.Tavg ?? 555.5f;
            float deltaT = tHot - tCold;
            float rcpHeat = GetCurrentRCPHeat();

            // In lumped model, all loops have same temperature
            // Gauges pull from MosaicBoard automatically if configured
            // with appropriate GaugeTypes.

            // Calculate totals
            float totalFlow = 0f;
            for (int i = 0; i < 4; i++)
            {
                if (_rcpRunning[i])
                {
                    totalFlow += RATED_FLOW_PER_RCP_GPM * _rcpFlowFractions[i];
                }
            }
        }

        /// <summary>
        /// Update RCP status displays.
        /// </summary>
        private void UpdateRCPStatus()
        {
            int rcpCount = GetRunningRCPCount();
            float simTime = _reactorController?.SimulationTime ?? Time.time;

            for (int i = 0; i < 4; i++)
            {
                bool shouldRun = (i < rcpCount);

                if (shouldRun && _rcpStartTimes[i] < float.MaxValue)
                {
                    var rampState = RCPSequencer.UpdatePumpRampState(i, _rcpStartTimes[i], simTime / 3600f);
                    _rcpFlowFractions[i] = rampState.FlowFraction;
                    _rcpRunning[i] = true;
                }
                else if (shouldRun && _rcpStartTimes[i] >= float.MaxValue)
                {
                    _rcpStartTimes[i] = simTime / 3600f;
                    _rcpFlowFractions[i] = 0f;
                    _rcpRunning[i] = true;
                }
                else
                {
                    _rcpFlowFractions[i] = 0f;
                    _rcpRunning[i] = false;
                }

                RCPState state;
                float speed = 0f;

                if (!_rcpRunning[i])
                {
                    state = RCPState.Stopped;
                }
                else if (_rcpFlowFractions[i] < 0.99f)
                {
                    state = RCPState.Ramping;
                    speed = _rcpFlowFractions[i] * RCP_RATED_SPEED_RPM;
                }
                else
                {
                    state = RCPState.Running;
                    speed = RCP_RATED_SPEED_RPM;
                }

                if (_rcpPanels[i] != null)
                {
                    _rcpPanels[i].UpdateDisplay(
                        state: state,
                        speed: speed,
                        flowFraction: _rcpFlowFractions[i],
                        canStart: !_rcpRunning[i] && CanStartRCP(i),
                        canStop: _rcpRunning[i]
                    );
                }
            }
        }

        /// <summary>
        /// Update the 2D mimic diagram based on current state.
        /// Colors piping by temperature, updates RCP indicators,
        /// and refreshes overlay texts.
        /// </summary>
        private void UpdateMimicDiagram()
        {
            float tHot = _reactorController?.Thot ?? 400f;
            float tCold = _reactorController?.Tcold ?? 380f;
            float tAvg = _reactorController?.Tavg ?? 390f;
            float power = _reactorController?.ThermalPower ?? 0f;

            // --- Color piping by temperature ---
            Color hotLegColor = TemperatureToColor(tHot);
            Color coldLegColor = TemperatureToColor(tCold);
            Color crossoverColor = TemperatureToColor((tHot + tCold) * 0.5f);

            for (int i = 0; i < 4; i++)
            {
                if (mimic_HotLegs != null && i < mimic_HotLegs.Length && mimic_HotLegs[i] != null)
                    mimic_HotLegs[i].color = hotLegColor;

                if (mimic_ColdLegs != null && i < mimic_ColdLegs.Length && mimic_ColdLegs[i] != null)
                    mimic_ColdLegs[i].color = coldLegColor;

                if (mimic_CrossoverLegs != null && i < mimic_CrossoverLegs.Length && mimic_CrossoverLegs[i] != null)
                    mimic_CrossoverLegs[i].color = crossoverColor;
            }

            // --- Update RCP icon colors based on state ---
            for (int i = 0; i < 4; i++)
            {
                if (mimic_RCPIcons != null && i < mimic_RCPIcons.Length && mimic_RCPIcons[i] != null)
                {
                    if (!_rcpRunning[i])
                        mimic_RCPIcons[i].color = color_Stopped;
                    else if (_rcpFlowFractions[i] < 0.99f)
                        mimic_RCPIcons[i].color = color_Ramping;
                    else
                        mimic_RCPIcons[i].color = color_Running;
                }

                // RCP status text on mimic
                if (mimic_RCPStatusTexts != null && i < mimic_RCPStatusTexts.Length && mimic_RCPStatusTexts[i] != null)
                {
                    if (!_rcpRunning[i])
                        mimic_RCPStatusTexts[i].text = "OFF";
                    else if (_rcpFlowFractions[i] < 0.99f)
                        mimic_RCPStatusTexts[i].text = $"{_rcpFlowFractions[i] * 100f:F0}%";
                    else
                        mimic_RCPStatusTexts[i].text = "RUN";
                }
            }

            // --- Update SG labels ---
            string[] sgNames = { "SG-A", "SG-B", "SG-C", "SG-D" };
            for (int i = 0; i < 4; i++)
            {
                if (mimic_SGLabels != null && i < mimic_SGLabels.Length && mimic_SGLabels[i] != null)
                {
                    mimic_SGLabels[i].text = sgNames[i];
                }
            }

            // --- Update temperature overlay texts on mimic ---
            for (int i = 0; i < 4; i++)
            {
                if (mimic_THotTexts != null && i < mimic_THotTexts.Length && mimic_THotTexts[i] != null)
                {
                    mimic_THotTexts[i].text = $"{tHot:F0}°F";
                    mimic_THotTexts[i].color = hotLegColor;
                }

                if (mimic_TColdTexts != null && i < mimic_TColdTexts.Length && mimic_TColdTexts[i] != null)
                {
                    mimic_TColdTexts[i].text = $"{tCold:F0}°F";
                    mimic_TColdTexts[i].color = coldLegColor;
                }
            }

            // --- Reactor vessel overlay ---
            if (mimic_ReactorPowerText != null)
            {
                mimic_ReactorPowerText.text = $"{power:F1}%";
            }

            if (mimic_ReactorTavgText != null)
            {
                mimic_ReactorTavgText.text = $"{tAvg:F0}°F";
            }

            // --- Reactor vessel color by T-avg ---
            if (mimic_ReactorVessel != null)
            {
                mimic_ReactorVessel.color = TemperatureToColor(tAvg) * 0.6f;
            }

            // --- Pressurizer ---
            if (mimic_PZRText != null)
            {
                float pzrPressure = 2235f; // Nominal — will be from physics when available
                float pzrLevel = 60f;
                mimic_PZRText.text = $"{pzrPressure:F0}\npsia";
            }
        }

        /// <summary>
        /// Update status display texts.
        /// </summary>
        private void UpdateStatusDisplays()
        {
            int rcpCount = GetRunningRCPCount();
            float tAvg = _reactorController?.Tavg ?? 400f;

            if (text_RCPCount != null)
            {
                text_RCPCount.text = $"RCPs: {rcpCount}/4";
            }

            bool isNaturalCirc = (rcpCount == 0 && tAvg > 350f);
            if (text_CirculationMode != null)
            {
                text_CirculationMode.text = isNaturalCirc ? "NATURAL CIRCULATION" : "FORCED CIRCULATION";
            }

            if (indicator_NaturalCirc != null)
            {
                indicator_NaturalCirc.color = isNaturalCirc ? color_Ramping : color_Stopped;
            }

            if (text_PlantMode != null)
            {
                int mode = GetPlantMode(tAvg);
                text_PlantMode.text = $"MODE {mode}";
            }
        }

        #endregion

        // ====================================================================
        // TEMPERATURE COLOR MAPPING
        // ====================================================================

        #region Temperature Colors

        /// <summary>
        /// Map a temperature (°F) to a piping color for the mimic diagram.
        /// Matches the design spec color gradient:
        ///   Cold (&lt;200°F) = Blue
        ///   Warm (200-400°F) = Cyan
        ///   Hot (400-550°F) = Orange
        ///   Very Hot (&gt;550°F) = Red
        /// </summary>
        private Color TemperatureToColor(float tempF)
        {
            if (tempF <= TEMP_COLD)
                return color_PipeCold;
            else if (tempF <= TEMP_WARM)
                return Color.Lerp(color_PipeCold, color_PipeWarm, (tempF - TEMP_COLD) / (TEMP_WARM - TEMP_COLD));
            else if (tempF <= TEMP_HOT)
                return Color.Lerp(color_PipeWarm, color_PipeHot, (tempF - TEMP_WARM) / (TEMP_HOT - TEMP_WARM));
            else if (tempF <= TEMP_VERY_HOT)
                return Color.Lerp(color_PipeHot, color_PipeVeryHot, (tempF - TEMP_HOT) / (TEMP_VERY_HOT - TEMP_HOT));
            else
                return color_PipeVeryHot;
        }

        #endregion

        // ====================================================================
        // RCP CONTROL
        // ====================================================================

        #region RCP Control

        private int GetRunningRCPCount()
        {
            if (_reactorController != null)
            {
                float flowFraction = _reactorController.FlowFraction;
                return Mathf.RoundToInt(flowFraction * 4f);
            }

            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                if (_rcpRunning[i]) count++;
            }
            return count;
        }

        private float GetCurrentRCPHeat()
        {
            float totalHeat = 0f;
            for (int i = 0; i < 4; i++)
            {
                if (_rcpRunning[i])
                {
                    totalHeat += _rcpFlowFractions[i] * PlantConstants.RCP_HEAT_MW_EACH;
                }
            }
            return totalHeat;
        }

        private bool CanStartRCP(int pumpIndex)
        {
            return true; // Full interlock check in future
        }

        private void OnRCPStartRequested(int pumpIndex)
        {
            if (!CanStartRCP(pumpIndex))
            {
                AddAlarm($"RCP-{pumpIndex + 1} START BLOCKED - INTERLOCKS");
                return;
            }

            _rcpStartTimes[pumpIndex] = (_reactorController?.SimulationTime ?? Time.time) / 3600f;
            _rcpRunning[pumpIndex] = true;

            AddAlarm($"RCP-{pumpIndex + 1} START INITIATED");
            Debug.Log($"[RCSScreen] RCP-{pumpIndex + 1} start requested");
        }

        private void OnRCPStopRequested(int pumpIndex)
        {
            _rcpRunning[pumpIndex] = false;
            _rcpFlowFractions[pumpIndex] = 0f;
            _rcpStartTimes[pumpIndex] = float.MaxValue;

            AddAlarm($"RCP-{pumpIndex + 1} TRIPPED");
            Debug.Log($"[RCSScreen] RCP-{pumpIndex + 1} stop requested");
        }

        #endregion

        // ====================================================================
        // ALARM MANAGEMENT
        // ====================================================================

        #region Alarm Management

        public void AddAlarm(string message)
        {
            string timestamp = GetTimestamp();
            _pendingAlarms.Enqueue($"[{timestamp}] {message}");
        }

        private void ProcessPendingAlarms()
        {
            while (_pendingAlarms.Count > 0)
            {
                string alarm = _pendingAlarms.Dequeue();
                CreateAlarmEntry(alarm);
            }

            while (_alarmEntries.Count > maxAlarmEntries)
            {
                GameObject oldest = _alarmEntries[0];
                _alarmEntries.RemoveAt(0);
                Destroy(oldest);
            }
        }

        private void CreateAlarmEntry(string message)
        {
            if (alarmListContainer == null) return;

            GameObject entry;
            if (alarmEntryPrefab != null)
            {
                entry = Instantiate(alarmEntryPrefab, alarmListContainer);
            }
            else
            {
                entry = new GameObject("AlarmEntry");
                entry.transform.SetParent(alarmListContainer, false);
                var text = entry.AddComponent<TextMeshProUGUI>();
                text.text = message;
                text.fontSize = 12;
                text.color = color_Ramping;
            }

            var tmpText = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = message;
            }

            _alarmEntries.Add(entry);
        }

        private string GetTimestamp()
        {
            float simTime = _reactorController?.SimulationTime ?? Time.time;
            int hours = Mathf.FloorToInt(simTime / 3600f);
            int minutes = Mathf.FloorToInt((simTime % 3600f) / 60f);
            int seconds = Mathf.FloorToInt(simTime % 60f);
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        #endregion

        // ====================================================================
        // UTILITY METHODS
        // ====================================================================

        #region Utility Methods

        private int GetPlantMode(float tAvg)
        {
            float power = _reactorController?.ThermalPower ?? 0f;
            bool isCritical = (_reactorController?.Keff ?? 0.95f) >= 0.99f;

            if (isCritical && power > 0.05f) return 1;
            if (isCritical) return 2;
            if (tAvg >= 350f) return 3;
            if (tAvg > 200f) return 4;
            return 5;
        }

        #endregion

        // ====================================================================
        // SCREEN LIFECYCLE OVERRIDES
        // ====================================================================

        #region Screen Lifecycle

        protected override void OnScreenShownInternal()
        {
            base.OnScreenShownInternal();

            UpdateGaugeValues();
            UpdateRCPStatus();
            UpdateMimicDiagram();
            UpdateStatusDisplays();
        }

        #endregion

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        #region Public API

        public bool[] GetRCPStates()
        {
            return (bool[])_rcpRunning.Clone();
        }

        public float[] GetRCPFlowFractions()
        {
            return (float[])_rcpFlowFractions.Clone();
        }

        #endregion
    }

    // ========================================================================
    // SUPPORTING TYPES
    // ========================================================================

    /// <summary>
    /// RCP operational states for display.
    /// </summary>
    public enum RCPState
    {
        Stopped,
        Ramping,
        Running,
        Tripped
    }
}
