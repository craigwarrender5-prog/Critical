// ============================================================================
// CRITICAL: Master the Atom - Turbine-Generator Operator Screen
// TurbineGeneratorScreen.cs - Screen 6: Turbine-Generator
// ============================================================================
//
// PURPOSE:
//   Implements the Turbine-Generator operator screen (Key 6) displaying:
//   - HP/LP turbine performance gauges
//   - Generator electrical output gauges
//   - Shaft train diagram (HP turbine, MSR, LP turbines, generator, condenser)
//   - Turbine/generator controls (visual only)
//
// LAYOUT (1920x1080):
//   - Left Panel (0-15%): 8 turbine performance gauges
//   - Center Panel (15-65%): Turbine-generator shaft train diagram
//   - Right Panel (65-100%): 8 generator/output gauges
//   - Bottom Panel (0-26%): Controls, breaker status, alarms
//
// KEYBOARD:
//   - Key 6: Toggle screen visibility
//
// DATA SOURCES (via ScreenDataBridge):
//   - SG heat transfer (MW) — used to estimate thermal input
//   - Steam pressure (psig) — HP turbine inlet reference
//   - All other parameters: PLACEHOLDER — turbine model not implemented
//
// WESTINGHOUSE 4-LOOP PWR TURBINE-GENERATOR SPECIFICATIONS:
//   - Turbine type: Tandem-compound, double-flow LP
//   - HP turbine: Single-flow, 1000-1100 psig inlet
//   - LP turbines: 2 double-flow units
//   - Generator: ~1300 MVA, 22 kV, 3600 RPM, 60 Hz
//   - Rated electrical output: ~1150 MWe gross
//   - Rated thermal output: ~3411 MWt (from NSSS)
//   - Turbine efficiency: ~33-34% overall
//   - Condenser vacuum: ~1.5-2.0 in Hg abs
//   - Moisture separator/reheater (MSR) between HP and LP
//   - Extraction steam for 6-7 stages of feedwater heating
//
// NOTE: This screen is almost entirely PLACEHOLDER. The turbine-generator
//       model does not exist in the current physics engine. All gauges
//       except steam pressure display "---" or estimated values.
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
    public class TurbineGeneratorScreen : OperatorScreen
    {
        #region OperatorScreen Implementation
        public override KeyCode ToggleKey => KeyCode.Alpha6;
        public override string ScreenName => "TURBINE-GENERATOR";
        public override int ScreenIndex => 6;
        #endregion

        #region Constants
        private const float RATED_THERMAL_MWT = 3411f;
        private const float RATED_ELECTRICAL_MWE = 1150f;
        private const float TURBINE_EFFICIENCY = 0.337f;
        private const float NOMINAL_SHAFT_RPM = 3600f;
        private const float NOMINAL_GRID_FREQ_HZ = 60f;
        private const float NOMINAL_GENERATOR_KV = 22f;
        private const float CONDENSER_VACUUM_INHG = 1.7f;
        private const float HP_INLET_PRESSURE_PSIA = 1000f;
        private const float GAUGE_UPDATE_INTERVAL = 0.1f;
        private const float VISUAL_UPDATE_INTERVAL = 0.5f;
        #endregion

        #region Inspector Fields - Left Panel
        [Header("=== LEFT PANEL - TURBINE PERFORMANCE ===")]
        [SerializeField] private TextMeshProUGUI text_HPInletPressure;
        [SerializeField] private TextMeshProUGUI text_HPInletTemp;
        [SerializeField] private TextMeshProUGUI text_HPExhaustPressure;
        [SerializeField] private TextMeshProUGUI text_LPExhaustPressure;
        [SerializeField] private TextMeshProUGUI text_ThrottleSteamFlow;
        [SerializeField] private TextMeshProUGUI text_FirstStagePressure;
        [SerializeField] private TextMeshProUGUI text_MSRPressure;
        [SerializeField] private TextMeshProUGUI text_ReheatSteamTemp;
        #endregion

        #region Inspector Fields - Right Panel
        [Header("=== RIGHT PANEL - GENERATOR OUTPUT ===")]
        [SerializeField] private TextMeshProUGUI text_GeneratorOutput;
        [SerializeField] private TextMeshProUGUI text_GrossOutput;
        [SerializeField] private TextMeshProUGUI text_AuxLoad;
        [SerializeField] private TextMeshProUGUI text_NetOutput;
        [SerializeField] private TextMeshProUGUI text_GeneratorVoltage;
        [SerializeField] private TextMeshProUGUI text_GeneratorCurrent;
        [SerializeField] private TextMeshProUGUI text_PowerFactor;
        [SerializeField] private TextMeshProUGUI text_GridFrequency;
        #endregion

        #region Inspector Fields - Shaft Train Diagram
        [Header("=== SHAFT TRAIN DIAGRAM ===")]
        [SerializeField] private Image diagram_HPTurbine;
        [SerializeField] private Image diagram_MSR;
        [SerializeField] private Image diagram_LPTurbineA;
        [SerializeField] private Image diagram_LPTurbineB;
        [SerializeField] private Image diagram_Generator;
        [SerializeField] private Image diagram_Condenser;
        [SerializeField] private Image diagram_SteamAdmission;
        [SerializeField] private Image diagram_ShaftLine;
        [SerializeField] private TextMeshProUGUI diagram_RPMText;
        [SerializeField] private TextMeshProUGUI diagram_PowerText;
        [SerializeField] private TextMeshProUGUI diagram_SteamPressureText;
        [SerializeField] private TextMeshProUGUI diagram_CondenserText;
        #endregion

        #region Inspector Fields - Bottom Panel
        [Header("=== BOTTOM PANEL ===")]
        [SerializeField] private TextMeshProUGUI text_ReactorMode;
        [SerializeField] private TextMeshProUGUI text_SimTime;
        [SerializeField] private TextMeshProUGUI text_TimeCompression;
        [SerializeField] private Button button_TurbineTrip;
        [SerializeField] private Button button_GenBreakerClose;
        [SerializeField] private Button button_GenBreakerOpen;
        [SerializeField] private Image indicator_TurbineTrip;
        [SerializeField] private TextMeshProUGUI text_TurbineTripStatus;
        [SerializeField] private Image indicator_GenBreaker;
        [SerializeField] private TextMeshProUGUI text_GenBreakerStatus;
        [SerializeField] private Transform alarmContainer;
        #endregion

        #region Private Fields
        private ScreenDataBridge _data;
        private float _lastGaugeUpdate;
        private float _lastVisualUpdate;

        private static readonly Color COLOR_NORMAL = new Color(0f, 1f, 0.53f);
        private static readonly Color COLOR_PLACEHOLDER = new Color(0.4f, 0.4f, 0.5f);
        private static readonly Color COLOR_WARNING = new Color(1f, 0.7f, 0.2f);
        private static readonly Color COLOR_ALARM = new Color(0.9f, 0.2f, 0.2f);
        private static readonly Color COLOR_RUNNING = new Color(0.2f, 0.9f, 0.2f);
        private static readonly Color COLOR_STOPPED = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color COLOR_EQUIPMENT_ON = new Color(0.2f, 0.3f, 0.4f);
        private static readonly Color COLOR_EQUIPMENT_OFF = new Color(0.15f, 0.15f, 0.18f);
        #endregion

        #region Unity Lifecycle
        protected override void Awake() { base.Awake(); }

        protected override void Start()
        {
            base.Start();
            _data = ScreenDataBridge.Instance;
            if (_data == null)
                Debug.LogWarning("[TurbineGeneratorScreen] ScreenDataBridge not found.");
            Debug.Log("[TurbineGeneratorScreen] Initialized. Toggle: Key 6 (MOSTLY PLACEHOLDER)");
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
                UpdateShaftTrainDiagram();
            }
        }
        #endregion

        #region Left Panel Updates
        private void UpdateLeftPanelGauges()
        {
            // HP Inlet Pressure — derived from steam pressure
            if (text_HPInletPressure != null)
            {
                float sp = _data.GetSteamPressure();
                SetGaugeText(text_HPInletPressure, sp, "F0", " psig");
            }
            // All others — PLACEHOLDER
            SetPlaceholder(text_HPInletTemp);
            SetPlaceholder(text_HPExhaustPressure);
            SetPlaceholder(text_LPExhaustPressure);
            SetPlaceholder(text_ThrottleSteamFlow);
            SetPlaceholder(text_FirstStagePressure);
            SetPlaceholder(text_MSRPressure);
            SetPlaceholder(text_ReheatSteamTemp);
        }
        #endregion

        #region Right Panel Updates
        private void UpdateRightPanelGauges()
        {
            // All PLACEHOLDER — no turbine-generator model
            SetPlaceholder(text_GeneratorOutput);
            SetPlaceholder(text_GrossOutput);
            SetPlaceholder(text_AuxLoad);
            SetPlaceholder(text_NetOutput);
            SetPlaceholder(text_GeneratorVoltage);
            SetPlaceholder(text_GeneratorCurrent);
            SetPlaceholder(text_PowerFactor);
            SetPlaceholder(text_GridFrequency);
        }
        #endregion

        #region Shaft Train Diagram
        private void UpdateShaftTrainDiagram()
        {
            // All equipment shown as inactive/standby — no turbine model
            Color eqColor = COLOR_EQUIPMENT_OFF;
            if (diagram_HPTurbine != null) diagram_HPTurbine.color = eqColor;
            if (diagram_MSR != null) diagram_MSR.color = eqColor;
            if (diagram_LPTurbineA != null) diagram_LPTurbineA.color = eqColor;
            if (diagram_LPTurbineB != null) diagram_LPTurbineB.color = eqColor;
            if (diagram_Generator != null) diagram_Generator.color = eqColor;
            if (diagram_Condenser != null) diagram_Condenser.color = eqColor;
            if (diagram_SteamAdmission != null) diagram_SteamAdmission.color = COLOR_EQUIPMENT_OFF;
            if (diagram_ShaftLine != null) diagram_ShaftLine.color = COLOR_STOPPED;

            // RPM — PLACEHOLDER
            if (diagram_RPMText != null) { diagram_RPMText.text = "--- RPM"; diagram_RPMText.color = COLOR_PLACEHOLDER; }

            // Power — PLACEHOLDER
            if (diagram_PowerText != null) { diagram_PowerText.text = "--- MWe"; diagram_PowerText.color = COLOR_PLACEHOLDER; }

            // Steam pressure — live
            if (diagram_SteamPressureText != null)
            {
                float sp = _data.GetSteamPressure();
                diagram_SteamPressureText.text = float.IsNaN(sp) ? "--- psig" : $"{sp:F0} psig";
                diagram_SteamPressureText.color = float.IsNaN(sp) ? COLOR_PLACEHOLDER : COLOR_NORMAL;
            }

            // Condenser — PLACEHOLDER
            if (diagram_CondenserText != null) { diagram_CondenserText.text = "--- in Hg"; diagram_CondenserText.color = COLOR_PLACEHOLDER; }
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
            // Turbine trip — PLACEHOLDER (always tripped/stopped)
            if (indicator_TurbineTrip != null) indicator_TurbineTrip.color = COLOR_ALARM;
            if (text_TurbineTripStatus != null) { text_TurbineTripStatus.text = "TRIPPED"; text_TurbineTripStatus.color = COLOR_ALARM; }
            // Generator breaker — PLACEHOLDER (always open)
            if (indicator_GenBreaker != null) indicator_GenBreaker.color = COLOR_STOPPED;
            if (text_GenBreakerStatus != null) { text_GenBreakerStatus.text = "OPEN"; text_GenBreakerStatus.color = COLOR_STOPPED; }
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
            else { textField.text = value.ToString(format) + suffix; textField.color = COLOR_NORMAL; }
        }

        private void SetPlaceholder(TextMeshProUGUI textField)
        {
            if (textField != null) { textField.text = "---"; textField.color = COLOR_PLACEHOLDER; }
        }
        #endregion
    }
}
