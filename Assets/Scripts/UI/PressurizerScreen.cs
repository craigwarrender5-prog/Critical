// ============================================================================
// CRITICAL: Master the Atom - Pressurizer Operator Screen
// PressurizerScreen.cs - Screen 3: Pressurizer Pressure, Level, Heaters, Spray
// ============================================================================
//
// PURPOSE:
//   Implements the Pressurizer operator screen (Key 3) displaying:
//   - Pressurizer pressure, level, and temperature
//   - Heater power and control status
//   - Spray flow and valve status
//   - PORV and safety valve status
//   - Surge line flow and temperature
//   - 2D pressurizer vessel cutaway with animated water level, heater glow,
//     spray indicator, and steam dome
//
// LAYOUT (1920x1080):
//   - Left Panel (0-15%): 8 pressure-related gauges
//   - Center Panel (15-65%): 2D pressurizer vessel cutaway visualization
//   - Right Panel (65-100%): 8 level/volume-related gauges
//   - Bottom Panel (0-26%): Heater/spray controls (visual only), PORV/SV
//                            indicators, alarm panel
//
// KEYBOARD:
//   - Key 3: Toggle screen visibility
//
// DATA SOURCES (via ScreenDataBridge):
//   - HeatupSimEngine: PZR pressure, level, heater power, water/steam volume,
//     Tsat, subcooling, pressure rate, surge flow, water temperature
//   - ScreenDataBridge: Unified getters with NaN placeholder convention
//
// WESTINGHOUSE 4-LOOP PWR PRESSURIZER SPECIFICATIONS:
//   - Total volume: 1800 ft3 (51.0 m3)
//   - Design pressure: 2500 psia
//   - Operating pressure: 2235 psia
//   - Operating temperature: 653 F (Tsat at 2235 psia)
//   - Normal level: 60% (nominal hot full power)
//   - Proportional heaters: 660 kW (4 groups x 165 kW)
//   - Backup heaters: 1020 kW (2 groups x 510 kW)
//   - Total heater capacity: 1680 kW
//   - Spray design flow: 800 gpm (from cold legs)
//   - Spray nozzle flow: ~1-5 gpm (continuous bypass)
//   - 2 PORVs: set pressure 2335 psia, each 210,000 lb/hr
//   - 3 Safety Valves: set pressure 2485 psia, each 420,000 lb/hr
//   - Surge line: 14-inch (connects to Loop 4 hot leg)
//
// SOURCES:
//   - Operator_Screen_Layout_Plan_v1_0_0.md (Screen 3 specification)
//   - NRC HRTD Rev 0606 - Pressurizer Chapter
//   - Westinghouse 4-Loop PWR Technical Specifications
//   - IMPLEMENTATION_PLAN_v2.0.0_MultiScreenGUI.md (Stage 4)
//
// VERSION: 2.0.0
// DATE: 2026-02-10
// CLASSIFICATION: UI - Operator Interface
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI
{
    public class PressurizerScreen : OperatorScreen
    {
        #region OperatorScreen Implementation
        public override KeyCode ToggleKey => KeyCode.Alpha3;
        public override string ScreenName => "PRESSURIZER";
        public override int ScreenIndex => 3;
        #endregion

        #region Constants
        private const float PZR_TOTAL_VOLUME_FT3 = 1800f;
        private const float PZR_DESIGN_PRESSURE_PSIA = 2500f;
        private const float PZR_OPERATING_PRESSURE_PSIA = 2235f;
        private const float PZR_NOMINAL_LEVEL_PCT = 60f;
        private const float PZR_TSAT_AT_2235_F = 653f;
        private const float PROPORTIONAL_HEATER_KW = 660f;
        private const float BACKUP_HEATER_KW = 1020f;
        private const float TOTAL_HEATER_KW = 1680f;
        private const float SPRAY_DESIGN_FLOW_GPM = 800f;
        private const float SPRAY_BYPASS_FLOW_GPM = 3f;
        private const float PORV_SETPOINT_PSIA = 2335f;
        private const float SAFETY_VALVE_SETPOINT_PSIA = 2485f;
        private const float PRESSURE_SETPOINT_PSIA = 2235f;
        private const float PRESSURE_HIGH_ALARM_PSIA = 2300f;
        private const float PRESSURE_LOW_ALARM_PSIA = 2185f;
        private const float LEVEL_HIGH_ALARM_PCT = 70f;
        private const float LEVEL_LOW_ALARM_PCT = 17f;
        private const float GAUGE_UPDATE_INTERVAL = 0.1f;
        private const float VESSEL_UPDATE_INTERVAL = 0.5f;
        #endregion

        #region Inspector Fields - Left Panel
        [Header("=== LEFT PANEL - PRESSURE ===")]
        [SerializeField] private TextMeshProUGUI text_PZRPressure;
        [SerializeField] private TextMeshProUGUI text_PressureSetpoint;
        [SerializeField] private TextMeshProUGUI text_PressureError;
        [SerializeField] private TextMeshProUGUI text_PressureRate;
        [SerializeField] private TextMeshProUGUI text_HeaterPower;
        [SerializeField] private TextMeshProUGUI text_SprayFlow;
        [SerializeField] private TextMeshProUGUI text_BackupHeaterStatus;
        [SerializeField] private TextMeshProUGUI text_PORVStatus;
        #endregion

        #region Inspector Fields - Right Panel
        [Header("=== RIGHT PANEL - LEVEL / VOLUME ===")]
        [SerializeField] private TextMeshProUGUI text_PZRLevel;
        [SerializeField] private TextMeshProUGUI text_LevelSetpoint;
        [SerializeField] private TextMeshProUGUI text_LevelError;
        [SerializeField] private TextMeshProUGUI text_SurgeFlow;
        [SerializeField] private TextMeshProUGUI text_SteamVolume;
        [SerializeField] private TextMeshProUGUI text_WaterVolume;
        [SerializeField] private TextMeshProUGUI text_PZRWaterTemp;
        [SerializeField] private TextMeshProUGUI text_Subcooling;
        #endregion

        #region Inspector Fields - Vessel Cutaway
        [Header("=== VESSEL CUTAWAY ===")]
        [SerializeField] private Image vessel_Shell;
        [SerializeField] private Image vessel_WaterFill;
        [SerializeField] private Image vessel_SteamDome;
        [SerializeField] private Image[] vessel_HeaterBars = new Image[4];
        [SerializeField] private Image vessel_SprayIndicator;
        [SerializeField] private Image vessel_SurgeLine;
        [SerializeField] private Image[] vessel_PORVIndicators = new Image[2];
        [SerializeField] private Image[] vessel_SafetyValveIndicators = new Image[3];
        [SerializeField] private TextMeshProUGUI vessel_PressureText;
        [SerializeField] private TextMeshProUGUI vessel_LevelText;
        [SerializeField] private TextMeshProUGUI vessel_TempText;
        [SerializeField] private TextMeshProUGUI vessel_HeaterText;
        #endregion

        #region Inspector Fields - Bottom Panel
        [Header("=== BOTTOM PANEL - CONTROLS & STATUS ===")]
        [SerializeField] private TextMeshProUGUI text_ReactorMode;
        [SerializeField] private TextMeshProUGUI text_SimTime;
        [SerializeField] private TextMeshProUGUI text_TimeCompression;
        [SerializeField] private TextMeshProUGUI text_HeaterPIDStatus;
        [SerializeField] private TextMeshProUGUI text_HZPStatus;
        [Header("Heater Controls (Visual Only)")]
        [SerializeField] private Button button_ProportionalHeaters;
        [SerializeField] private Button button_BackupHeaters;
        [Header("Spray Controls (Visual Only)")]
        [SerializeField] private Button button_SprayOpen;
        [SerializeField] private Button button_SprayClose;
        [Header("PORV / Safety Valve Indicators")]
        [SerializeField] private Image indicator_PORV_A;
        [SerializeField] private TextMeshProUGUI text_PORV_A;
        [SerializeField] private Image indicator_PORV_B;
        [SerializeField] private TextMeshProUGUI text_PORV_B;
        [SerializeField] private Image indicator_SV_1;
        [SerializeField] private TextMeshProUGUI text_SV_1;
        [SerializeField] private Image indicator_SV_2;
        [SerializeField] private TextMeshProUGUI text_SV_2;
        [SerializeField] private Image indicator_SV_3;
        [SerializeField] private TextMeshProUGUI text_SV_3;
        [Header("Alarm Panel")]
        [SerializeField] private Transform alarmContainer;
        #endregion

        #region Private Fields
        private ScreenDataBridge _data;
        private float _lastGaugeUpdate;
        private float _lastVesselUpdate;
        private static readonly Color COLOR_RUNNING = new Color(0.2f, 0.9f, 0.2f);
        private static readonly Color COLOR_STOPPED = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color COLOR_WARNING = new Color(1f, 0.7f, 0.2f);
        private static readonly Color COLOR_ALARM = new Color(0.9f, 0.2f, 0.2f);
        private static readonly Color COLOR_NORMAL_TEXT = new Color(0f, 1f, 0.53f);
        private static readonly Color COLOR_PLACEHOLDER = new Color(0.4f, 0.4f, 0.5f);
        private static readonly Color COLOR_WATER = new Color(0.1f, 0.3f, 0.8f, 0.7f);
        private static readonly Color COLOR_WATER_HOT = new Color(0.4f, 0.2f, 0.7f, 0.7f);
        private static readonly Color COLOR_STEAM = new Color(0.6f, 0.6f, 0.7f, 0.3f);
        private static readonly Color COLOR_HEATER_OFF = new Color(0.2f, 0.15f, 0.1f);
        private static readonly Color COLOR_HEATER_ON = new Color(1f, 0.4f, 0.1f);
        private static readonly Color COLOR_SPRAY_ACTIVE = new Color(0.3f, 0.7f, 1f, 0.8f);
        private static readonly Color COLOR_SPRAY_INACTIVE = new Color(0.2f, 0.2f, 0.3f, 0.3f);
        private static readonly Color COLOR_VALVE_CLOSED = new Color(0.3f, 0.3f, 0.3f);
        private static readonly Color COLOR_VALVE_OPEN = new Color(0.9f, 0.2f, 0.2f);
        #endregion

        #region Unity Lifecycle
        protected override void Awake() { base.Awake(); }

        protected override void Start()
        {
            base.Start();
            _data = ScreenDataBridge.Instance;
            if (_data == null)
                Debug.LogWarning("[PressurizerScreen] ScreenDataBridge not found.");
            Debug.Log("[PressurizerScreen] Initialized. Toggle: Key 3");
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
            if (time - _lastVesselUpdate >= VESSEL_UPDATE_INTERVAL)
            {
                _lastVesselUpdate = time;
                UpdateVesselVisualization();
            }
        }
        #endregion

        #region Left Panel Updates
        private void UpdateLeftPanelGauges()
        {
            if (text_PZRPressure != null)
            {
                float pressure = _data.GetPZRPressure();
                SetGaugeText(text_PZRPressure, pressure, "F0", " psia");
                if (!float.IsNaN(pressure))
                {
                    if (pressure >= PORV_SETPOINT_PSIA) text_PZRPressure.color = COLOR_ALARM;
                    else if (pressure >= PRESSURE_HIGH_ALARM_PSIA || pressure <= PRESSURE_LOW_ALARM_PSIA) text_PZRPressure.color = COLOR_WARNING;
                }
            }
            if (text_PressureSetpoint != null)
            {
                text_PressureSetpoint.text = $"{PRESSURE_SETPOINT_PSIA:F0} psia";
                text_PressureSetpoint.color = COLOR_NORMAL_TEXT;
            }
            if (text_PressureError != null)
            {
                float pressure = _data.GetPZRPressure();
                if (!float.IsNaN(pressure))
                {
                    float error = pressure - PRESSURE_SETPOINT_PSIA;
                    string sign = error >= 0 ? "+" : "";
                    text_PressureError.text = $"{sign}{error:F0} psi";
                    text_PressureError.color = Mathf.Abs(error) > 50f ? COLOR_WARNING : COLOR_NORMAL_TEXT;
                }
                else { text_PressureError.text = "---"; text_PressureError.color = COLOR_PLACEHOLDER; }
            }
            if (text_PressureRate != null)
            {
                float rate = _data.GetPressureRate();
                if (!float.IsNaN(rate))
                {
                    string sign = rate >= 0 ? "+" : "";
                    text_PressureRate.text = $"{sign}{rate:F1} psi/hr";
                    text_PressureRate.color = Mathf.Abs(rate) > 100f ? COLOR_WARNING : COLOR_NORMAL_TEXT;
                }
                else { text_PressureRate.text = "---"; text_PressureRate.color = COLOR_PLACEHOLDER; }
            }
            if (text_HeaterPower != null)
            {
                float hp = _data.GetHeaterPower();
                if (!float.IsNaN(hp)) { text_HeaterPower.text = $"{hp:F0} kW"; text_HeaterPower.color = hp > 0f ? COLOR_NORMAL_TEXT : COLOR_STOPPED; }
                else { text_HeaterPower.text = "---"; text_HeaterPower.color = COLOR_PLACEHOLDER; }
            }
            // PLACEHOLDER: Spray flow not modeled
            if (text_SprayFlow != null) { text_SprayFlow.text = "---"; text_SprayFlow.color = COLOR_PLACEHOLDER; }
            if (text_BackupHeaterStatus != null)
            {
                float hp = _data.GetHeaterPower();
                if (!float.IsNaN(hp))
                {
                    if (hp > PROPORTIONAL_HEATER_KW) { text_BackupHeaterStatus.text = "ENERGIZED"; text_BackupHeaterStatus.color = COLOR_WARNING; }
                    else if (hp > 0f) { text_BackupHeaterStatus.text = "STANDBY"; text_BackupHeaterStatus.color = COLOR_NORMAL_TEXT; }
                    else { text_BackupHeaterStatus.text = "OFF"; text_BackupHeaterStatus.color = COLOR_STOPPED; }
                }
                else { text_BackupHeaterStatus.text = "---"; text_BackupHeaterStatus.color = COLOR_PLACEHOLDER; }
            }
            if (text_PORVStatus != null)
            {
                float p = _data.GetPZRPressure();
                if (!float.IsNaN(p) && p >= PORV_SETPOINT_PSIA) { text_PORVStatus.text = "OPEN"; text_PORVStatus.color = COLOR_ALARM; }
                else { text_PORVStatus.text = "CLOSED"; text_PORVStatus.color = COLOR_NORMAL_TEXT; }
            }
        }
        #endregion

        #region Right Panel Updates
        private void UpdateRightPanelGauges()
        {
            if (text_PZRLevel != null)
            {
                float level = _data.GetPZRLevel();
                SetGaugeText(text_PZRLevel, level, "F1", "%");
                if (!float.IsNaN(level) && (level >= LEVEL_HIGH_ALARM_PCT || level <= LEVEL_LOW_ALARM_PCT))
                    text_PZRLevel.color = COLOR_WARNING;
            }
            if (text_LevelSetpoint != null) { text_LevelSetpoint.text = $"{PZR_NOMINAL_LEVEL_PCT:F0}%"; text_LevelSetpoint.color = COLOR_NORMAL_TEXT; }
            if (text_LevelError != null)
            {
                float level = _data.GetPZRLevel();
                if (!float.IsNaN(level))
                {
                    float error = level - PZR_NOMINAL_LEVEL_PCT;
                    string sign = error >= 0 ? "+" : "";
                    text_LevelError.text = $"{sign}{error:F1}%";
                    text_LevelError.color = Mathf.Abs(error) > 10f ? COLOR_WARNING : COLOR_NORMAL_TEXT;
                }
                else { text_LevelError.text = "---"; text_LevelError.color = COLOR_PLACEHOLDER; }
            }
            if (text_SurgeFlow != null)
            {
                float sf = _data.GetSurgeFlow();
                if (!float.IsNaN(sf))
                {
                    string sign = sf >= 0 ? "+" : "";
                    text_SurgeFlow.text = $"{sign}{sf:F1} gpm";
                    text_SurgeFlow.color = Mathf.Abs(sf) > 50f ? COLOR_WARNING : COLOR_NORMAL_TEXT;
                }
                else { text_SurgeFlow.text = "---"; text_SurgeFlow.color = COLOR_PLACEHOLDER; }
            }
            if (text_SteamVolume != null) SetGaugeText(text_SteamVolume, _data.GetPZRSteamVolume(), "F0", " ft3");
            if (text_WaterVolume != null) SetGaugeText(text_WaterVolume, _data.GetPZRWaterVolume(), "F0", " ft3");
            if (text_PZRWaterTemp != null) SetGaugeText(text_PZRWaterTemp, _data.GetPZRWaterTemp(), "F1", " F");
            if (text_Subcooling != null)
            {
                float sc = _data.GetSubcooling();
                if (!float.IsNaN(sc))
                {
                    text_Subcooling.text = $"{sc:F1} F";
                    text_Subcooling.color = sc > 20f ? COLOR_NORMAL_TEXT : sc > 0f ? COLOR_WARNING : COLOR_ALARM;
                }
                else { text_Subcooling.text = "---"; text_Subcooling.color = COLOR_PLACEHOLDER; }
            }
        }
        #endregion

        #region Vessel Visualization
        private void UpdateVesselVisualization()
        {
            UpdateWaterLevel();
            UpdateSteamDome();
            UpdateHeaterBars();
            UpdateSprayIndicator();
            UpdateSurgeLineIndicator();
            UpdateReliefValveIndicators();
            UpdateVesselOverlayText();
        }

        private void UpdateWaterLevel()
        {
            if (vessel_WaterFill == null) return;
            float level = _data.GetPZRLevel();
            if (!float.IsNaN(level))
            {
                vessel_WaterFill.fillAmount = Mathf.Clamp01(level / 100f);
                float waterTemp = _data.GetPZRWaterTemp();
                if (!float.IsNaN(waterTemp))
                {
                    float t = Mathf.InverseLerp(100f, PZR_TSAT_AT_2235_F, waterTemp);
                    vessel_WaterFill.color = Color.Lerp(COLOR_WATER, COLOR_WATER_HOT, t);
                }
                else vessel_WaterFill.color = COLOR_WATER;
            }
            else vessel_WaterFill.fillAmount = 0f;
        }

        private void UpdateSteamDome()
        {
            if (vessel_SteamDome == null) return;
            float steamVol = _data.GetPZRSteamVolume();
            vessel_SteamDome.color = (!float.IsNaN(steamVol) && steamVol > 0f) ? COLOR_STEAM : new Color(0.15f, 0.15f, 0.2f, 0.15f);
        }

        private void UpdateHeaterBars()
        {
            float hp = _data.GetHeaterPower();
            if (float.IsNaN(hp)) { foreach (var bar in vessel_HeaterBars) if (bar != null) bar.color = COLOR_HEATER_OFF; return; }
            float fraction = Mathf.Clamp01(hp / TOTAL_HEATER_KW);
            for (int i = 0; i < vessel_HeaterBars.Length; i++)
            {
                if (vessel_HeaterBars[i] == null) continue;
                float barIntensity = Mathf.Clamp01((fraction - (i / (float)vessel_HeaterBars.Length)) * vessel_HeaterBars.Length);
                vessel_HeaterBars[i].color = Color.Lerp(COLOR_HEATER_OFF, COLOR_HEATER_ON, barIntensity);
            }
        }

        private void UpdateSprayIndicator()
        {
            if (vessel_SprayIndicator == null) return;
            float p = _data.GetPZRPressure();
            bool sprayActive = !float.IsNaN(p) && p > (PRESSURE_SETPOINT_PSIA + 25f);
            vessel_SprayIndicator.color = sprayActive ? COLOR_SPRAY_ACTIVE : COLOR_SPRAY_INACTIVE;
        }

        private void UpdateSurgeLineIndicator()
        {
            if (vessel_SurgeLine == null) return;
            float sf = _data.GetSurgeFlow();
            if (!float.IsNaN(sf))
            {
                if (sf > 5f) vessel_SurgeLine.color = new Color(0.8f, 0.4f, 0.1f, 0.8f);
                else if (sf < -5f) vessel_SurgeLine.color = new Color(0.2f, 0.5f, 0.9f, 0.8f);
                else vessel_SurgeLine.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            }
            else vessel_SurgeLine.color = COLOR_STOPPED;
        }

        private void UpdateReliefValveIndicators()
        {
            float p = _data.GetPZRPressure();
            bool porvOpen = !float.IsNaN(p) && p >= PORV_SETPOINT_PSIA;
            bool svOpen = !float.IsNaN(p) && p >= SAFETY_VALVE_SETPOINT_PSIA;
            for (int i = 0; i < vessel_PORVIndicators.Length; i++)
                if (vessel_PORVIndicators[i] != null) vessel_PORVIndicators[i].color = porvOpen ? COLOR_VALVE_OPEN : COLOR_VALVE_CLOSED;
            for (int i = 0; i < vessel_SafetyValveIndicators.Length; i++)
                if (vessel_SafetyValveIndicators[i] != null) vessel_SafetyValveIndicators[i].color = svOpen ? COLOR_VALVE_OPEN : COLOR_VALVE_CLOSED;
            UpdateValveTextIndicator(indicator_PORV_A, text_PORV_A, "PORV-A", porvOpen);
            UpdateValveTextIndicator(indicator_PORV_B, text_PORV_B, "PORV-B", porvOpen);
            UpdateValveTextIndicator(indicator_SV_1, text_SV_1, "SV-1", svOpen);
            UpdateValveTextIndicator(indicator_SV_2, text_SV_2, "SV-2", svOpen);
            UpdateValveTextIndicator(indicator_SV_3, text_SV_3, "SV-3", svOpen);
        }

        private void UpdateValveTextIndicator(Image indicator, TextMeshProUGUI label, string valveName, bool isOpen)
        {
            if (indicator != null) indicator.color = isOpen ? COLOR_VALVE_OPEN : COLOR_VALVE_CLOSED;
            if (label != null) { label.text = $"{valveName}: {(isOpen ? "OPEN" : "SHUT")}"; label.color = isOpen ? COLOR_ALARM : COLOR_NORMAL_TEXT; }
        }

        private void UpdateVesselOverlayText()
        {
            if (vessel_PressureText != null) { float v = _data.GetPZRPressure(); vessel_PressureText.text = float.IsNaN(v) ? "--- psia" : $"{v:F0} psia"; }
            if (vessel_LevelText != null) { float v = _data.GetPZRLevel(); vessel_LevelText.text = float.IsNaN(v) ? "--- %" : $"{v:F1}%"; }
            if (vessel_TempText != null) { float v = _data.GetPZRWaterTemp(); vessel_TempText.text = float.IsNaN(v) ? "--- F" : $"{v:F0} F"; }
            if (vessel_HeaterText != null) { float v = _data.GetHeaterPower(); vessel_HeaterText.text = float.IsNaN(v) ? "--- kW" : $"{v:F0} kW"; }
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
            if (text_HeaterPIDStatus != null)
            {
                bool pidActive = _data.GetHeaterPIDActive();
                text_HeaterPIDStatus.text = pidActive ? "PID: ACTIVE" : "PID: MANUAL";
                text_HeaterPIDStatus.color = pidActive ? COLOR_RUNNING : COLOR_STOPPED;
            }
            if (text_HZPStatus != null)
            {
                bool hzpStable = _data.GetHZPStable();
                float hzpProgress = _data.GetHZPProgress();
                if (hzpStable) { text_HZPStatus.text = "HZP: STABLE"; text_HZPStatus.color = COLOR_RUNNING; }
                else if (!float.IsNaN(hzpProgress) && hzpProgress > 0f) { text_HZPStatus.text = $"HZP: {hzpProgress:F0}%"; text_HZPStatus.color = COLOR_WARNING; }
                else { text_HZPStatus.text = "HZP: ---"; text_HZPStatus.color = COLOR_STOPPED; }
            }
        }
        #endregion

        #region Screen Lifecycle
        protected override void OnScreenShownInternal()
        {
            base.OnScreenShownInternal();
            _lastGaugeUpdate = 0f;
            _lastVesselUpdate = 0f;
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
