// ============================================================================
// CRITICAL: Master the Atom - Plant Overview Screen
// PlantOverviewScreen.cs - Screen Tab: Plant-Wide Overview & Mimic Diagram
// ============================================================================
//
// PURPOSE:
//   Provides a high-level overview of the entire plant with a simplified
//   mimic diagram showing reactor, RCS loops, pressurizer, steam generators,
//   turbine-generator, and condenser. Displays key parameters from all
//   major systems in a single view.
//
// LAYOUT (1920x1080):
//   - Left Panel (0-15%): 8 nuclear/primary gauges
//   - Center Panel (15-65%): Simplified plant mimic diagram (procedural 2D)
//   - Right Panel (65-100%): 8 secondary/output gauges
//   - Bottom Panel (0-26%): Plant status, RCP indicators, alarms, controls
//
// KEYBOARD:
//   - Tab: Toggle screen visibility
//
// DATA SOURCES (via ScreenDataBridge):
//   - ReactorController: Power, Tavg, boron, xenon, rod position, flow
//   - HeatupSimEngine: Temperatures, pressures, levels, RCP count
//   - ScreenDataBridge: Unified getters with NaN placeholder convention
//
// MIMIC DIAGRAM COMPONENTS:
//   - Reactor vessel (center) with power overlay
//   - 4 RCS hot/cold leg lines with temperature-based coloring
//   - Pressurizer with pressure/level overlay
//   - 4 Steam Generators with level bars
//   - Turbine-Generator with MWe overlay
//   - Condenser
//   - Feedwater return line
//   - Color-coded based on temperature where data available
//   - Static schematic with dynamic parameter text overlays
//
// SOURCES:
//   - Operator_Screen_Layout_Plan_v1_0_0.md
//   - IMPLEMENTATION_PLAN_v2.0.0_MultiScreenGUI.md (Stage 3)
//   - Westinghouse 4-Loop PWR General Arrangement
//
// VERSION: 4.3.0
// DATE: 2026-02-11
// CLASSIFICATION: UI — Operator Interface
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI
{
    /// <summary>
    /// Plant Overview Screen (Tab key).
    /// Displays a high-level plant mimic diagram with key parameters
    /// from all major systems.
    /// </summary>
    public class PlantOverviewScreen : OperatorScreen
    {
        // ====================================================================
        // ABSTRACT PROPERTY IMPLEMENTATIONS
        // ====================================================================

        #region OperatorScreen Implementation

        public override KeyCode ToggleKey => KeyCode.Tab;
        public override string ScreenName => "PLANT OVERVIEW";
        public override int ScreenIndex => ScreenManager.OVERVIEW_INDEX; // 100

        #endregion

        // ====================================================================
        // CONSTANTS
        // ====================================================================

        #region Constants

        // Westinghouse 4-Loop PWR rated parameters
        private const float RATED_THERMAL_POWER_MWT = 3411f;
        private const float RATED_ELECTRICAL_MWE = 1150f;
        private const float RATED_RCS_FLOW_GPM = 390400f;
        private const float NOMINAL_TAVG_F = 588.4f;
        private const float NOMINAL_PZR_PRESSURE_PSIA = 2235f;
        private const float NOMINAL_PZR_LEVEL_PCT = 60f;
        private const float NOMINAL_SG_PRESSURE_PSIG = 1000f;

        // Temperature color mapping
        private const float COLD_TEMP_F = 100f;   // Deep blue
        private const float WARM_TEMP_F = 400f;   // Yellow
        private const float HOT_TEMP_F = 650f;    // Red

        // Update throttling
        private const float GAUGE_UPDATE_INTERVAL = 0.1f;   // 10 Hz
        private const float MIMIC_UPDATE_INTERVAL = 0.5f;   // 2 Hz

        #endregion

        // ====================================================================
        // SERIALIZED FIELDS — Left Panel (Nuclear/Primary)
        // ====================================================================

        #region Inspector Fields - Left Panel

        [Header("=== LEFT PANEL — NUCLEAR/PRIMARY ===")]
        [SerializeField] private TextMeshProUGUI text_ReactorPower;
        [SerializeField] private TextMeshProUGUI text_Tavg;
        [SerializeField] private TextMeshProUGUI text_RCSPressure;
        [SerializeField] private TextMeshProUGUI text_PZRLevel;
        [SerializeField] private TextMeshProUGUI text_TotalRCSFlow;
        [SerializeField] private TextMeshProUGUI text_RodPosition;
        [SerializeField] private TextMeshProUGUI text_BoronConc;
        [SerializeField] private TextMeshProUGUI text_XenonWorth;

        #endregion

        // ====================================================================
        // SERIALIZED FIELDS — Right Panel (Secondary/Output)
        // ====================================================================

        #region Inspector Fields - Right Panel

        [Header("=== RIGHT PANEL — SECONDARY/OUTPUT ===")]
        [SerializeField] private TextMeshProUGUI text_SGLevelAvg;
        [SerializeField] private TextMeshProUGUI text_SteamPressure;
        [SerializeField] private TextMeshProUGUI text_FeedwaterFlow;
        [SerializeField] private TextMeshProUGUI text_TurbinePower;
        [SerializeField] private TextMeshProUGUI text_GeneratorOutput;
        [SerializeField] private TextMeshProUGUI text_CondenserVacuum;
        [SerializeField] private TextMeshProUGUI text_FeedwaterTemp;
        [SerializeField] private TextMeshProUGUI text_MainSteamFlow;

        #endregion

        // ====================================================================
        // SERIALIZED FIELDS — Center Panel (Mimic Diagram)
        // ====================================================================

        #region Inspector Fields - Mimic Diagram

        [Header("=== MIMIC DIAGRAM ===")]
        [SerializeField] private Image mimic_ReactorVessel;
        [SerializeField] private TextMeshProUGUI mimic_ReactorPowerText;

        [SerializeField] private Image mimic_Pressurizer;
        [SerializeField] private Image mimic_PZRLevelFill;
        [SerializeField] private TextMeshProUGUI mimic_PZRText;

        [SerializeField] private Image[] mimic_HotLegs = new Image[4];
        [SerializeField] private Image[] mimic_ColdLegs = new Image[4];

        [SerializeField] private Image[] mimic_SteamGenerators = new Image[4];
        [SerializeField] private Image[] mimic_SGLevelFills = new Image[4];
        [SerializeField] private TextMeshProUGUI[] mimic_SGLabels = new TextMeshProUGUI[4];

        [SerializeField] private Image mimic_Turbine;
        [SerializeField] private TextMeshProUGUI mimic_TurbineText;

        [SerializeField] private Image mimic_Generator;
        [SerializeField] private TextMeshProUGUI mimic_GeneratorText;

        [SerializeField] private Image mimic_Condenser;

        [SerializeField] private Image mimic_FeedwaterLine;
        [SerializeField] private Image mimic_MainSteamLine;

        #endregion

        // ====================================================================
        // SERIALIZED FIELDS — Bottom Panel (Status)
        // ====================================================================

        #region Inspector Fields - Bottom Panel

        [Header("=== BOTTOM PANEL — STATUS ===")]
        [SerializeField] private TextMeshProUGUI text_ReactorMode;
        [SerializeField] private TextMeshProUGUI text_SimTime;
        [SerializeField] private TextMeshProUGUI text_TimeCompression;

        [SerializeField] private Image[] indicators_RCP = new Image[4];
        [SerializeField] private TextMeshProUGUI[] labels_RCP = new TextMeshProUGUI[4];

        [SerializeField] private Image indicator_TurbineStatus;
        [SerializeField] private TextMeshProUGUI text_TurbineStatus;

        [SerializeField] private Image indicator_GeneratorBreaker;
        [SerializeField] private TextMeshProUGUI text_GeneratorBreaker;

        [SerializeField] private Transform alarmSummaryContainer;

        [SerializeField] private Button button_ReactorTrip;
        [SerializeField] private Button button_TurbineTrip;

        #endregion

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        #region Private Fields

        private ScreenDataBridge _data;
        private float _lastGaugeUpdate;
        private float _lastMimicUpdate;

        // Status colors
        private static readonly Color COLOR_RUNNING = new Color(0.2f, 0.9f, 0.2f);
        private static readonly Color COLOR_STOPPED = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color COLOR_WARNING = new Color(1f, 0.7f, 0.2f);
        private static readonly Color COLOR_ALARM = new Color(0.9f, 0.2f, 0.2f);
        private static readonly Color COLOR_NORMAL_TEXT = new Color(0f, 1f, 0.53f);
        private static readonly Color COLOR_PLACEHOLDER = new Color(0.4f, 0.4f, 0.5f);

        #endregion

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();

            _data = ScreenDataBridge.Instance;

            if (_data == null)
            {
                Debug.LogWarning("[PlantOverviewScreen] ScreenDataBridge not found. Data will be unavailable.");
            }

            Debug.Log("[PlantOverviewScreen] Initialized. Toggle: Tab");
        }

        protected override void Update()
        {
            base.Update();

            if (!IsVisible || _data == null) return;

            float time = Time.time;

            // Update gauges at 10 Hz
            if (time - _lastGaugeUpdate >= GAUGE_UPDATE_INTERVAL)
            {
                _lastGaugeUpdate = time;
                UpdateLeftPanelGauges();
                UpdateRightPanelGauges();
                UpdateBottomPanelStatus();
            }

            // Update mimic diagram at 2 Hz
            if (time - _lastMimicUpdate >= MIMIC_UPDATE_INTERVAL)
            {
                _lastMimicUpdate = time;
                UpdateMimicDiagram();
            }
        }

        #endregion

        // ====================================================================
        // GAUGE UPDATES — Left Panel (Nuclear/Primary)
        // ====================================================================

        #region Left Panel Updates

        private void UpdateLeftPanelGauges()
        {
            // Reactor Power (%)
            if (text_ReactorPower != null)
            {
                float power = _data.HasReactorController ? _data.ReactorCtrl.ThermalPower * 100f : float.NaN;
                SetGaugeText(text_ReactorPower, power, "F1", "%");
            }

            // T-avg
            if (text_Tavg != null)
            {
                SetGaugeText(text_Tavg, _data.GetTavg(), "F1", "°F");
            }

            // RCS Pressure
            if (text_RCSPressure != null)
            {
                SetGaugeText(text_RCSPressure, _data.GetPZRPressure(), "F0", " psia");
            }

            // PZR Level
            if (text_PZRLevel != null)
            {
                SetGaugeText(text_PZRLevel, _data.GetPZRLevel(), "F1", "%");
            }

            // Total RCS Flow
            if (text_TotalRCSFlow != null)
            {
                float flow = _data.GetFlowFraction();
                if (!float.IsNaN(flow))
                {
                    float gpm = flow * RATED_RCS_FLOW_GPM;
                    text_TotalRCSFlow.text = $"{gpm / 1000f:F0}K gpm";
                    text_TotalRCSFlow.color = COLOR_NORMAL_TEXT;
                }
                else
                {
                    text_TotalRCSFlow.text = "---";
                    text_TotalRCSFlow.color = COLOR_PLACEHOLDER;
                }
            }

            // Control Rod Position (Bank D)
            if (text_RodPosition != null)
            {
                if (_data.HasReactorController)
                {
                    float pos = _data.ReactorCtrl.BankDPosition;
                    text_RodPosition.text = $"{pos:F0} steps";
                    text_RodPosition.color = COLOR_NORMAL_TEXT;
                }
                else
                {
                    text_RodPosition.text = "---";
                    text_RodPosition.color = COLOR_PLACEHOLDER;
                }
            }

            // Boron Concentration
            if (text_BoronConc != null)
            {
                SetGaugeText(text_BoronConc, _data.GetBoronConcentration(), "F0", " ppm");
            }

            // Xenon Worth
            if (text_XenonWorth != null)
            {
                if (_data.HasReactorController)
                {
                    float xenon = _data.ReactorCtrl.Xenon_pcm;
                    text_XenonWorth.text = $"{xenon:F0} pcm";
                    text_XenonWorth.color = COLOR_NORMAL_TEXT;
                }
                else
                {
                    text_XenonWorth.text = "---";
                    text_XenonWorth.color = COLOR_PLACEHOLDER;
                }
            }
        }

        #endregion

        // ====================================================================
        // GAUGE UPDATES — Right Panel (Secondary/Output)
        // ====================================================================

        #region Right Panel Updates

        private void UpdateRightPanelGauges()
        {
            // SG Level Average
            if (text_SGLevelAvg != null)
            {
                float sgLevel = _data.GetSGLevel(0);
                SetGaugeText(text_SGLevelAvg, sgLevel, "F1", "%");
            }

            // Steam Pressure — v4.3.0: Use tracked SG secondary pressure
            if (text_SteamPressure != null)
            {
                SetGaugeText(text_SteamPressure, _data.GetSGSecondaryPressure_psig(), "F0", " psig");
            }

            // PLACEHOLDER — Feedwater Flow
            if (text_FeedwaterFlow != null)
            {
                text_FeedwaterFlow.text = "---";
                text_FeedwaterFlow.color = COLOR_PLACEHOLDER;
            }

            // PLACEHOLDER — Turbine Power
            if (text_TurbinePower != null)
            {
                text_TurbinePower.text = "---";
                text_TurbinePower.color = COLOR_PLACEHOLDER;
            }

            // PLACEHOLDER — Generator Output
            if (text_GeneratorOutput != null)
            {
                text_GeneratorOutput.text = "---";
                text_GeneratorOutput.color = COLOR_PLACEHOLDER;
            }

            // PLACEHOLDER — Condenser Vacuum
            if (text_CondenserVacuum != null)
            {
                text_CondenserVacuum.text = "---";
                text_CondenserVacuum.color = COLOR_PLACEHOLDER;
            }

            // PLACEHOLDER — Feedwater Temperature
            if (text_FeedwaterTemp != null)
            {
                text_FeedwaterTemp.text = "---";
                text_FeedwaterTemp.color = COLOR_PLACEHOLDER;
            }

            // PLACEHOLDER — Main Steam Flow
            if (text_MainSteamFlow != null)
            {
                text_MainSteamFlow.text = "---";
                text_MainSteamFlow.color = COLOR_PLACEHOLDER;
            }
        }

        #endregion

        // ====================================================================
        // MIMIC DIAGRAM UPDATE
        // ====================================================================

        #region Mimic Diagram Update

        private void UpdateMimicDiagram()
        {
            UpdateReactorVessel();
            UpdatePressurizer();
            UpdateRCSLoops();
            UpdateSteamGenerators();
            UpdateTurbineGenerator();
        }

        private void UpdateReactorVessel()
        {
            // Reactor power overlay
            if (mimic_ReactorPowerText != null)
            {
                float power = _data.HasReactorController ? _data.ReactorCtrl.ThermalPower * 100f : 0f;
                mimic_ReactorPowerText.text = $"{power:F1}%";
            }

            // Reactor vessel color based on temperature
            if (mimic_ReactorVessel != null)
            {
                float tavg = _data.GetTavg();
                mimic_ReactorVessel.color = float.IsNaN(tavg) ?
                    COLOR_STOPPED : GetTemperatureColor(tavg);
            }
        }

        private void UpdatePressurizer()
        {
            // PZR level fill
            if (mimic_PZRLevelFill != null)
            {
                float level = _data.GetPZRLevel();
                mimic_PZRLevelFill.fillAmount = float.IsNaN(level) ? 0f : level / 100f;
            }

            // PZR text overlay
            if (mimic_PZRText != null)
            {
                float pressure = _data.GetPZRPressure();
                float level = _data.GetPZRLevel();
                string pStr = float.IsNaN(pressure) ? "---" : $"{pressure:F0}";
                string lStr = float.IsNaN(level) ? "---" : $"{level:F0}%";
                mimic_PZRText.text = $"{pStr}\n{lStr}";
            }
        }

        private void UpdateRCSLoops()
        {
            float tHot = _data.GetThot();
            float tCold = _data.GetTcold();

            Color hotColor = float.IsNaN(tHot) ? COLOR_STOPPED : GetTemperatureColor(tHot);
            Color coldColor = float.IsNaN(tCold) ? COLOR_STOPPED : GetTemperatureColor(tCold);

            for (int i = 0; i < 4; i++)
            {
                if (i < mimic_HotLegs.Length && mimic_HotLegs[i] != null)
                {
                    mimic_HotLegs[i].color = hotColor;
                }
                if (i < mimic_ColdLegs.Length && mimic_ColdLegs[i] != null)
                {
                    mimic_ColdLegs[i].color = coldColor;
                }
            }
        }

        private void UpdateSteamGenerators()
        {
            float sgTemp = _data.GetSGSecondaryTemp();
            Color sgColor = float.IsNaN(sgTemp) ? COLOR_STOPPED : GetTemperatureColor(sgTemp);

            for (int i = 0; i < 4; i++)
            {
                // SG body color
                if (i < mimic_SteamGenerators.Length && mimic_SteamGenerators[i] != null)
                {
                    mimic_SteamGenerators[i].color = sgColor;
                }

                // SG level fill (lumped model — all SGs same)
                if (i < mimic_SGLevelFills.Length && mimic_SGLevelFills[i] != null)
                {
                    float level = _data.GetSGLevel(i);
                    mimic_SGLevelFills[i].fillAmount = float.IsNaN(level) ? 0.5f : level / 100f;
                }

                // SG label — v4.3.0: Use tracked SG secondary pressure
                if (i < mimic_SGLabels.Length && mimic_SGLabels[i] != null)
                {
                    string letter = ((char)('A' + i)).ToString();
                    float sgPress = _data.GetSGSecondaryPressure_psig();
                    string pStr = float.IsNaN(sgPress) ? "---" : $"{sgPress:F0}";
                    mimic_SGLabels[i].text = $"SG-{letter}\n{pStr} psig";
                }
            }
        }

        private void UpdateTurbineGenerator()
        {
            // PLACEHOLDER — No turbine model. Show static text.
            if (mimic_TurbineText != null)
            {
                mimic_TurbineText.text = "---\nMWt";
            }

            if (mimic_GeneratorText != null)
            {
                mimic_GeneratorText.text = "---\nMWe";
            }
        }

        #endregion

        // ====================================================================
        // BOTTOM PANEL STATUS UPDATE
        // ====================================================================

        #region Bottom Panel Updates

        private void UpdateBottomPanelStatus()
        {
            // Reactor Mode
            if (text_ReactorMode != null)
            {
                text_ReactorMode.text = _data.GetPlantModeString();
                int mode = _data.GetPlantMode();
                text_ReactorMode.color = mode <= 2 ? COLOR_RUNNING :
                                         mode <= 4 ? COLOR_WARNING : COLOR_STOPPED;
            }

            // Simulation Time
            if (text_SimTime != null)
            {
                float simTime = _data.GetSimulationTime();
                int hours = Mathf.FloorToInt(simTime / 3600f);
                int minutes = Mathf.FloorToInt((simTime % 3600f) / 60f);
                int seconds = Mathf.FloorToInt(simTime % 60f);
                text_SimTime.text = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            }

            // Time Compression
            if (text_TimeCompression != null)
            {
                float ts = Time.timeScale;
                text_TimeCompression.text = ts <= 0f ? "PAUSED" :
                                           ts >= 1000f ? $"{ts / 1000f:F1}kx" :
                                           $"{ts:F0}x";
            }

            // RCP Indicators
            int rcpCount = _data.GetRCPCount();
            for (int i = 0; i < 4; i++)
            {
                bool running = i < rcpCount;

                if (i < indicators_RCP.Length && indicators_RCP[i] != null)
                {
                    indicators_RCP[i].color = running ? COLOR_RUNNING : COLOR_STOPPED;
                }

                if (i < labels_RCP.Length && labels_RCP[i] != null)
                {
                    labels_RCP[i].text = $"RCP-{i + 1}";
                    labels_RCP[i].color = running ? COLOR_RUNNING : COLOR_STOPPED;
                }
            }

            // PLACEHOLDER — Turbine status
            if (indicator_TurbineStatus != null)
            {
                indicator_TurbineStatus.color = COLOR_STOPPED;
            }
            if (text_TurbineStatus != null)
            {
                text_TurbineStatus.text = "TURBINE: OFF";
                text_TurbineStatus.color = COLOR_STOPPED;
            }

            // PLACEHOLDER — Generator breaker
            if (indicator_GeneratorBreaker != null)
            {
                indicator_GeneratorBreaker.color = COLOR_STOPPED;
            }
            if (text_GeneratorBreaker != null)
            {
                text_GeneratorBreaker.text = "GEN BKR: OPEN";
                text_GeneratorBreaker.color = COLOR_STOPPED;
            }
        }

        #endregion

        // ====================================================================
        // SCREEN LIFECYCLE OVERRIDES
        // ====================================================================

        #region Screen Lifecycle

        protected override void OnScreenShownInternal()
        {
            base.OnScreenShownInternal();

            // Force immediate update when shown
            _lastGaugeUpdate = 0f;
            _lastMimicUpdate = 0f;
        }

        #endregion

        // ====================================================================
        // UTILITY
        // ====================================================================

        #region Utility

        /// <summary>
        /// Set a gauge text field, handling NaN as placeholder.
        /// </summary>
        private void SetGaugeText(TextMeshProUGUI textField, float value, string format, string suffix)
        {
            if (textField == null) return;

            if (float.IsNaN(value))
            {
                textField.text = "---";
                textField.color = COLOR_PLACEHOLDER;
            }
            else
            {
                textField.text = value.ToString(format) + suffix;
                textField.color = COLOR_NORMAL_TEXT;
            }
        }

        /// <summary>
        /// Map a temperature to a color for the mimic diagram.
        /// Cold (100°F) → blue, Warm (400°F) → yellow, Hot (650°F) → red.
        /// </summary>
        private Color GetTemperatureColor(float tempF)
        {
            if (tempF <= COLD_TEMP_F) return new Color(0.2f, 0.3f, 0.8f);   // Cool blue
            if (tempF >= HOT_TEMP_F) return new Color(0.9f, 0.2f, 0.1f);    // Hot red

            // Interpolate through blue → cyan → yellow → orange → red
            float t = Mathf.InverseLerp(COLD_TEMP_F, HOT_TEMP_F, tempF);

            if (t < 0.33f)
            {
                // Blue to cyan
                float lt = t / 0.33f;
                return Color.Lerp(new Color(0.2f, 0.3f, 0.8f), new Color(0f, 0.8f, 0.9f), lt);
            }
            else if (t < 0.66f)
            {
                // Cyan to yellow-orange
                float lt = (t - 0.33f) / 0.33f;
                return Color.Lerp(new Color(0f, 0.8f, 0.9f), new Color(1f, 0.7f, 0.2f), lt);
            }
            else
            {
                // Yellow-orange to red
                float lt = (t - 0.66f) / 0.34f;
                return Color.Lerp(new Color(1f, 0.7f, 0.2f), new Color(0.9f, 0.2f, 0.1f), lt);
            }
        }

        #endregion
    }
}
