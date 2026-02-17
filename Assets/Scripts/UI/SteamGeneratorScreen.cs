// ============================================================================
// CRITICAL: Master the Atom - Steam Generator Operator Screen
// SteamGeneratorScreen.cs - Screen 5: Steam Generators (4x SG Monitor)
// ============================================================================
//
// PURPOSE:
//   Implements the Steam Generator operator screen (Key 5) displaying:
//   - Primary side inlet/outlet temperatures for all 4 SGs
//   - Secondary side levels, steam pressure, heat transfer
//   - Quad-SG 2x2 layout with U-tube schematics and level indicators
//   - Feedwater and steam flow indicators
//
// LAYOUT (1920x1080):
//   - Left Panel (0-15%): 8 primary side temperature gauges (4 inlet + 4 outlet)
//   - Center Panel (15-65%): Quad-SG 2x2 layout with U-tube schematics
//   - Right Panel (65-100%): 8 secondary side gauges (4 level + 4 pressure)
//   - Bottom Panel (0-26%): FW/steam flow, heat removal, pump status, alarms
//
// KEYBOARD:
//   - Key 5: Toggle screen visibility
//
// DATA SOURCES (via ScreenDataBridge):
//   - HeatupSimEngine: SG secondary temp (lumped), SG heat transfer,
//     SG top/bottom node temps, steam pressure, steaming status,
//     T_hot (SG inlet), T_cold (SG outlet), SG circulation fraction
//   - NOTE: Currently lumped model â€” all 4 SGs show identical data.
//     Per-SG instrumentation is PLACEHOLDER pending individual SG models.
//
// WESTINGHOUSE 4-LOOP PWR STEAM GENERATOR SPECIFICATIONS:
//   - Model F (or 51 series), 4 units
//   - Design pressure (primary): 2500 psia
//   - Design pressure (secondary): 1185 psia
//   - Normal operating steam pressure: ~1000-1100 psig
//   - Safety valve setpoint: ~1185 psig
//   - Normal narrow-range level: 50-65% (power dependent)
//   - Wide-range span: 0-100% (bottom of tube sheet to steam nozzle)
//   - Tube material: Inconel 600 (or 690 in replacement SGs)
//   - Number of tubes: ~5626 per SG
//   - Heat transfer area: ~55,000 ft2 per SG
//   - Normal feedwater flow: ~3.8 Mlbm/hr per SG at full power
//   - Steam flow: ~3.8 Mlbm/hr per SG at full power
//   - Feedwater temperature: ~440 F (after FW heaters)
//
// VERSION: 4.3.0
// DATE: 2026-02-11
// CLASSIFICATION: UI - Operator Interface
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Critical.Validation;
namespace Critical.UI
{
    public class SteamGeneratorScreen : OperatorScreen
    {
        #region OperatorScreen Implementation
        public override KeyCode ToggleKey => KeyCode.Alpha5;
        public override string ScreenName => "STEAM GENERATORS";
        public override int ScreenIndex => 5;
        #endregion

        #region Constants
        private const float SG_NORMAL_LEVEL_LOW_PCT = 50f;
        private const float SG_NORMAL_LEVEL_HIGH_PCT = 65f;
        private const float SG_LEVEL_ALARM_LOW_PCT = 25f;
        private const float SG_LEVEL_ALARM_HIGH_PCT = 80f;
        private const float SG_NORMAL_STEAM_PRESSURE_PSIG = 1050f;
        private const float SG_SAFETY_VALVE_PSIG = 1185f;
        private const float SG_LOW_PRESSURE_ALARM_PSIG = 900f;
        private const float GAUGE_UPDATE_INTERVAL = 0.1f;
        private const float VISUAL_UPDATE_INTERVAL = 0.5f;
        #endregion

        #region Inspector Fields - Left Panel (Primary Side)
        [Header("=== LEFT PANEL - PRIMARY SIDE TEMPS ===")]
        [SerializeField] private TextMeshProUGUI text_SGA_InletTemp;
        [SerializeField] private TextMeshProUGUI text_SGB_InletTemp;
        [SerializeField] private TextMeshProUGUI text_SGC_InletTemp;
        [SerializeField] private TextMeshProUGUI text_SGD_InletTemp;
        [SerializeField] private TextMeshProUGUI text_SGA_OutletTemp;
        [SerializeField] private TextMeshProUGUI text_SGB_OutletTemp;
        [SerializeField] private TextMeshProUGUI text_SGC_OutletTemp;
        [SerializeField] private TextMeshProUGUI text_SGD_OutletTemp;
        #endregion

        #region Inspector Fields - Right Panel (Secondary Side)
        [Header("=== RIGHT PANEL - SECONDARY SIDE ===")]
        [SerializeField] private TextMeshProUGUI text_SGA_Level;
        [SerializeField] private TextMeshProUGUI text_SGB_Level;
        [SerializeField] private TextMeshProUGUI text_SGC_Level;
        [SerializeField] private TextMeshProUGUI text_SGD_Level;
        [SerializeField] private TextMeshProUGUI text_SGA_SteamPressure;
        [SerializeField] private TextMeshProUGUI text_SGB_SteamPressure;
        [SerializeField] private TextMeshProUGUI text_SGC_SteamPressure;
        [SerializeField] private TextMeshProUGUI text_SGD_SteamPressure;
        #endregion

        #region Inspector Fields - Quad SG Visualization
        [Header("=== QUAD SG VISUALIZATION ===")]
        [SerializeField] private Image[] sg_Shells = new Image[4];
        [SerializeField] private Image[] sg_TubeBundles = new Image[4];
        [SerializeField] private Image[] sg_LevelFills = new Image[4];
        [SerializeField] private Image[] sg_SteamDomes = new Image[4];
        [SerializeField] private Image[] sg_HotLegInlets = new Image[4];
        [SerializeField] private Image[] sg_ColdLegOutlets = new Image[4];
        [SerializeField] private TextMeshProUGUI[] sg_LevelTexts = new TextMeshProUGUI[4];
        [SerializeField] private TextMeshProUGUI[] sg_PressureTexts = new TextMeshProUGUI[4];
        [SerializeField] private TextMeshProUGUI[] sg_TempTexts = new TextMeshProUGUI[4];
        #endregion

        #region Inspector Fields - Bottom Panel
        [Header("=== BOTTOM PANEL ===")]
        [SerializeField] private TextMeshProUGUI text_ReactorMode;
        [SerializeField] private TextMeshProUGUI text_SimTime;
        [SerializeField] private TextMeshProUGUI text_TimeCompression;
        [SerializeField] private TextMeshProUGUI text_TotalHeatRemoval;
        [SerializeField] private TextMeshProUGUI text_SteamingStatus;
        [SerializeField] private TextMeshProUGUI text_SGSecondaryTemp;
        [SerializeField] private TextMeshProUGUI text_CirculationFraction;
        [SerializeField] private TextMeshProUGUI text_FeedwaterFlow;
        [SerializeField] private TextMeshProUGUI text_SteamFlow;
        [SerializeField] private Transform alarmContainer;
        #endregion

        #region Private Fields
        private ScreenDataBridge _data;
        private float _lastGaugeUpdate;
        private float _lastVisualUpdate;

        private static readonly Color COLOR_RUNNING = new Color(0.2f, 0.9f, 0.2f);
        private static readonly Color COLOR_STOPPED = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color COLOR_WARNING = new Color(1f, 0.7f, 0.2f);
        private static readonly Color COLOR_ALARM = new Color(0.9f, 0.2f, 0.2f);
        private static readonly Color COLOR_NORMAL_TEXT = new Color(0f, 1f, 0.53f);
        private static readonly Color COLOR_PLACEHOLDER = new Color(0.4f, 0.4f, 0.5f);

        // SG visualization colors
        private static readonly Color COLOR_SG_SHELL = new Color(0.20f, 0.22f, 0.28f);
        private static readonly Color COLOR_TUBE_BUNDLE = new Color(0.30f, 0.25f, 0.20f, 0.6f);
        private static readonly Color COLOR_TUBE_HOT = new Color(0.7f, 0.3f, 0.15f, 0.7f);
        private static readonly Color COLOR_TUBE_COLD = new Color(0.2f, 0.3f, 0.6f, 0.5f);
        private static readonly Color COLOR_WATER_LEVEL = new Color(0.15f, 0.4f, 0.7f, 0.7f);
        private static readonly Color COLOR_STEAM = new Color(0.6f, 0.6f, 0.7f, 0.3f);
        private static readonly Color COLOR_STEAM_ACTIVE = new Color(0.7f, 0.7f, 0.8f, 0.5f);
        private static readonly Color COLOR_HOT_LEG = new Color(0.8f, 0.3f, 0.2f, 0.8f);
        private static readonly Color COLOR_COLD_LEG = new Color(0.2f, 0.4f, 0.8f, 0.8f);
        private static readonly Color COLOR_LEG_INACTIVE = new Color(0.25f, 0.25f, 0.30f, 0.4f);
        #endregion

        #region Unity Lifecycle
        protected override void Awake() { base.Awake(); }

        protected override void Start()
        {
            base.Start();
            _data = ScreenDataBridge.Instance;
            if (_data == null)
                Debug.LogWarning("[SteamGeneratorScreen] ScreenDataBridge not found.");
            Debug.Log("[SteamGeneratorScreen] Initialized. Toggle: Key 5");
        }

        protected override void Update()
        {
            base.Update();
            if (!IsVisible || _data == null) return;
            float time = Time.time;
            if (time - _lastGaugeUpdate >= GAUGE_UPDATE_INTERVAL)
            {
                _lastGaugeUpdate = time;
                UpdateLeftPanelGauges();
                UpdateRightPanelGauges();
                UpdateBottomPanelStatus();
            }
            if (time - _lastVisualUpdate >= VISUAL_UPDATE_INTERVAL)
            {
                _lastVisualUpdate = time;
                UpdateQuadSGVisualization();
            }
        }
        #endregion

        #region Left Panel Updates (Primary Side)
        private void UpdateLeftPanelGauges()
        {
            // Lumped model: all 4 SG inlets = T_hot, all 4 outlets = T_cold
            float tHot = _data.GetThot();
            float tCold = _data.GetTcold();

            SetGaugeText(text_SGA_InletTemp, tHot, "F1", " F");
            SetGaugeText(text_SGB_InletTemp, tHot, "F1", " F");
            SetGaugeText(text_SGC_InletTemp, tHot, "F1", " F");
            SetGaugeText(text_SGD_InletTemp, tHot, "F1", " F");
            SetGaugeText(text_SGA_OutletTemp, tCold, "F1", " F");
            SetGaugeText(text_SGB_OutletTemp, tCold, "F1", " F");
            SetGaugeText(text_SGC_OutletTemp, tCold, "F1", " F");
            SetGaugeText(text_SGD_OutletTemp, tCold, "F1", " F");
        }
        #endregion

        #region Right Panel Updates (Secondary Side)
        private void UpdateRightPanelGauges()
        {
            // Levels â€” per-SG getter (currently returns NaN for all)
            TextMeshProUGUI[] levelTexts = { text_SGA_Level, text_SGB_Level, text_SGC_Level, text_SGD_Level };
            for (int i = 0; i < 4; i++)
            {
                if (levelTexts[i] == null) continue;
                float level = _data.GetSGLevel(i);
                SetGaugeText(levelTexts[i], level, "F1", "%");
                if (!float.IsNaN(level))
                {
                    if (level <= SG_LEVEL_ALARM_LOW_PCT || level >= SG_LEVEL_ALARM_HIGH_PCT)
                        levelTexts[i].color = COLOR_ALARM;
                    else if (level <= SG_NORMAL_LEVEL_LOW_PCT || level >= SG_NORMAL_LEVEL_HIGH_PCT)
                        levelTexts[i].color = COLOR_WARNING;
                }
            }

            // Steam pressure â€” v4.3.0: Use tracked SG secondary pressure
            float steamP = _data.GetSGSecondaryPressure_psig();
            TextMeshProUGUI[] pressTexts = { text_SGA_SteamPressure, text_SGB_SteamPressure, text_SGC_SteamPressure, text_SGD_SteamPressure };
            for (int i = 0; i < 4; i++)
            {
                if (pressTexts[i] == null) continue;
                SetGaugeText(pressTexts[i], steamP, "F0", " psig");
                if (!float.IsNaN(steamP))
                {
                    if (steamP >= SG_SAFETY_VALVE_PSIG) pressTexts[i].color = COLOR_ALARM;
                    else if (steamP <= SG_LOW_PRESSURE_ALARM_PSIG) pressTexts[i].color = COLOR_WARNING;
                }
            }
        }
        #endregion

        #region Quad SG Visualization
        private void UpdateQuadSGVisualization()
        {
            float tHot = _data.GetThot();
            float tCold = _data.GetTcold();
            float sgSecTemp = _data.GetSGSecondaryTemp();
            float steamP = _data.GetSGSecondaryPressure_psig();  // v4.3.0
            bool steaming = _data.GetSGSteaming();
            float boilIntensity = _data.GetSGBoilingIntensity();  // v4.3.0
            bool boilingActive = _data.GetSGBoilingActive();      // v4.3.0
            int rcpCount = _data.GetRCPCount();
            bool flowActive = rcpCount > 0;  // v4.3.0: Use RCP count instead of deprecated circulation fraction

            for (int i = 0; i < 4; i++)
            {
                // Tube bundle color â€” interpolate based on primary temps
                if (sg_TubeBundles[i] != null)
                {
                    if (!float.IsNaN(tHot))
                    {
                        float t = Mathf.InverseLerp(100f, 620f, tHot);
                        sg_TubeBundles[i].color = Color.Lerp(COLOR_TUBE_COLD, COLOR_TUBE_HOT, t);
                    }
                    else sg_TubeBundles[i].color = COLOR_TUBE_BUNDLE;
                }

                // Level fill â€” uses per-SG getter (currently NaN â†’ empty)
                if (sg_LevelFills[i] != null)
                {
                    float level = _data.GetSGLevel(i);
                    if (!float.IsNaN(level))
                    {
                        sg_LevelFills[i].fillAmount = Mathf.Clamp01(level / 100f);
                        sg_LevelFills[i].color = COLOR_WATER_LEVEL;
                    }
                    else
                    {
                        sg_LevelFills[i].fillAmount = 0f;
                    }
                }

                // Steam dome â€” active if steaming
                if (sg_SteamDomes[i] != null)
                    sg_SteamDomes[i].color = steaming ? COLOR_STEAM_ACTIVE : COLOR_STEAM;

                // Hot leg inlet color
                if (sg_HotLegInlets[i] != null)
                    sg_HotLegInlets[i].color = flowActive ? COLOR_HOT_LEG : COLOR_LEG_INACTIVE;

                // Cold leg outlet color
                if (sg_ColdLegOutlets[i] != null)
                    sg_ColdLegOutlets[i].color = flowActive ? COLOR_COLD_LEG : COLOR_LEG_INACTIVE;

                // Overlay text on each SG
                if (sg_LevelTexts[i] != null)
                {
                    float level = _data.GetSGLevel(i);
                    sg_LevelTexts[i].text = float.IsNaN(level) ? "---%" : $"{level:F1}%";
                }
                if (sg_PressureTexts[i] != null)
                    sg_PressureTexts[i].text = float.IsNaN(steamP) ? "--- psig" : $"{steamP:F0} psig";
                if (sg_TempTexts[i] != null)
                    sg_TempTexts[i].text = float.IsNaN(sgSecTemp) ? "--- F" : $"{sgSecTemp:F0} F";
            }
        }
        #endregion

        #region Bottom Panel Updates
        private void UpdateBottomPanelStatus()
        {
            if (text_ReactorMode != null)
            {
                text_ReactorMode.text = _data.GetPlantModeString();
                int mode = _data.GetPlantMode();
                text_ReactorMode.color = mode <= 2 ? COLOR_RUNNING : mode <= 4 ? COLOR_WARNING : COLOR_STOPPED;
            }
            if (text_SimTime != null)
            {
                float st = _data.GetSimulationTime();
                text_SimTime.text = $"{Mathf.FloorToInt(st / 3600f):D2}:{Mathf.FloorToInt((st % 3600f) / 60f):D2}:{Mathf.FloorToInt(st % 60f):D2}";
            }
            if (text_TimeCompression != null)
            {
                float ts = Time.timeScale;
                text_TimeCompression.text = ts <= 0f ? "PAUSED" : ts >= 1000f ? $"{ts / 1000f:F1}kx" : $"{ts:F0}x";
            }
            if (text_TotalHeatRemoval != null)
            {
                float ht = _data.GetSGHeatTransfer();
                if (!float.IsNaN(ht)) { text_TotalHeatRemoval.text = $"{ht:F1} MW"; text_TotalHeatRemoval.color = ht > 0.1f ? COLOR_NORMAL_TEXT : COLOR_STOPPED; }
                else { text_TotalHeatRemoval.text = "---"; text_TotalHeatRemoval.color = COLOR_PLACEHOLDER; }
            }
            if (text_SteamingStatus != null)
            {
                // v4.3.0: Enhanced steaming status with boiling intensity
                bool boiling = _data.GetSGBoilingActive();
                float intensity = _data.GetSGBoilingIntensity();
                bool n2Isolated = _data.GetSGNitrogenIsolated();
                if (boiling && !float.IsNaN(intensity))
                {
                    text_SteamingStatus.text = $"BOILING ({intensity * 100f:F0}%)";
                    text_SteamingStatus.color = COLOR_RUNNING;
                }
                else if (n2Isolated)
                {
                    text_SteamingStatus.text = "N\u2082 ISOLATED";
                    text_SteamingStatus.color = COLOR_WARNING;
                }
                else
                {
                    text_SteamingStatus.text = "SUBCOOLED";
                    text_SteamingStatus.color = COLOR_STOPPED;
                }
            }
            if (text_SGSecondaryTemp != null)
            {
                // v4.3.0: Show secondary temp with T_sat reference
                float sgTemp = _data.GetSGSecondaryTemp();
                float tSat = _data.GetSGSaturationTemp();
                if (!float.IsNaN(sgTemp) && !float.IsNaN(tSat))
                {
                    text_SGSecondaryTemp.text = $"{sgTemp:F1} F (Tsat={tSat:F0})";
                    text_SGSecondaryTemp.color = COLOR_NORMAL_TEXT;
                }
                else
                    SetGaugeText(text_SGSecondaryTemp, sgTemp, "F1", " F");
            }
            if (text_CirculationFraction != null)
            {
                // v4.3.0: Repurposed from deprecated CirculationFraction to Boiling Intensity
                float bi = _data.GetSGBoilingIntensity();
                if (!float.IsNaN(bi))
                {
                    text_CirculationFraction.text = $"Boil: {bi * 100f:F0}%";
                    text_CirculationFraction.color = bi > 0.5f ? COLOR_RUNNING : bi > 0f ? COLOR_WARNING : COLOR_STOPPED;
                }
                else { text_CirculationFraction.text = "---"; text_CirculationFraction.color = COLOR_PLACEHOLDER; }
            }
            // Feedwater / Steam flow â€” PLACEHOLDER
            if (text_FeedwaterFlow != null) { text_FeedwaterFlow.text = "---"; text_FeedwaterFlow.color = COLOR_PLACEHOLDER; }
            if (text_SteamFlow != null) { text_SteamFlow.text = "---"; text_SteamFlow.color = COLOR_PLACEHOLDER; }
        }
        #endregion

        #region Screen Lifecycle
        protected override void OnScreenShownInternal()
        {
            base.OnScreenShownInternal();
            _lastGaugeUpdate = 0f;
            _lastVisualUpdate = 0f;
        }
        #endregion

        #region Utility
        private void SetGaugeText(TextMeshProUGUI textField, float value, string format, string suffix)
        {
            if (textField == null) return;
            if (float.IsNaN(value)) { textField.text = "---"; textField.color = COLOR_PLACEHOLDER; }
            else { textField.text = value.ToString(format) + suffix; textField.color = COLOR_NORMAL_TEXT; }
        }
        #endregion
    }
}

