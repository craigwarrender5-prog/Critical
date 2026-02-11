// ============================================================================
// CRITICAL: Master the Atom - Auxiliary Systems Operator Screen
// AuxiliarySystemsScreen.cs - Screen 8: Auxiliary Systems (Combined)
// ============================================================================
//
// PURPOSE:
//   Implements the Auxiliary Systems operator screen (Key 8) displaying:
//   - RHR system gauges (Trains A & B)
//   - CCW/Service Water gauges
//   - Auxiliary systems overview diagram
//   - RHR/CCW/SW pump controls (visual only)
//
// LAYOUT (1920x1080):
//   - Left Panel (0-15%): 8 RHR system gauges
//   - Center Panel (15-65%): Auxiliary systems overview diagram
//   - Right Panel (65-100%): 8 cooling water gauges
//   - Bottom Panel (0-26%): Pump controls, status, alarms
//
// KEYBOARD:
//   - Key 8: Toggle screen visibility
//
// DATA SOURCES (via ScreenDataBridge):
//   - ALL PLACEHOLDER — RHR/CCW/SW systems not modeled
//
// WESTINGHOUSE 4-LOOP PWR AUXILIARY SYSTEM SPECIFICATIONS:
//   - RHR: 2 trains (A & B), each with pump and HX
//   - RHR pump capacity: ~3000 gpm per pump
//   - RHR HX capacity: ~40 MBtu/hr per HX
//   - RHR entry conditions: RCS <350°F, <425 psig
//   - CCW: 3 pumps (2 running + 1 standby), 2 HXs
//   - CCW design flow: ~20,000 gpm total
//   - CCW surge tank: ~2000 gal
//   - SW: 3 pumps (2 running + 1 standby)
//   - SW design flow: ~30,000 gpm total
//   - RCP thermal barrier cooling: via CCW, ~50 gpm per RCP
//
// NOTE: RHR system is now LIVE (v4.3.0) via RHRSystem.cs physics.
//       CCW and SW remain PLACEHOLDER (no physics model).
//
// VERSION: 4.3.0
// DATE: 2026-02-11
// CLASSIFICATION: UI - Operator Interface
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI
{
    public class AuxiliarySystemsScreen : OperatorScreen
    {
        #region OperatorScreen Implementation
        public override KeyCode ToggleKey => KeyCode.Alpha8;
        public override string ScreenName => "AUXILIARY SYSTEMS";
        public override int ScreenIndex => 8;
        #endregion

        #region Constants
        private const float GAUGE_UPDATE_INTERVAL = 0.1f;
        private const float VISUAL_UPDATE_INTERVAL = 0.5f;
        #endregion

        #region Inspector Fields - Left Panel
        [Header("=== LEFT PANEL - RHR SYSTEM ===")]
        [SerializeField] private TextMeshProUGUI text_RHR_A_Flow;
        [SerializeField] private TextMeshProUGUI text_RHR_B_Flow;
        [SerializeField] private TextMeshProUGUI text_RHR_HXA_InletTemp;
        [SerializeField] private TextMeshProUGUI text_RHR_HXA_OutletTemp;
        [SerializeField] private TextMeshProUGUI text_RHR_HXB_InletTemp;
        [SerializeField] private TextMeshProUGUI text_RHR_HXB_OutletTemp;
        [SerializeField] private TextMeshProUGUI text_RHR_SuctionPressure;
        [SerializeField] private TextMeshProUGUI text_RHR_PumpStatus;
        #endregion

        #region Inspector Fields - Right Panel
        [Header("=== RIGHT PANEL - COOLING WATER ===")]
        [SerializeField] private TextMeshProUGUI text_CCW_SupplyP;
        [SerializeField] private TextMeshProUGUI text_CCW_ReturnP;
        [SerializeField] private TextMeshProUGUI text_CCW_SurgeTankLevel;
        [SerializeField] private TextMeshProUGUI text_CCW_Temperature;
        [SerializeField] private TextMeshProUGUI text_SW_Flow;
        [SerializeField] private TextMeshProUGUI text_SW_Temperature;
        [SerializeField] private TextMeshProUGUI text_RCP_ThermalBarrierFlow;
        [SerializeField] private TextMeshProUGUI text_CCW_HeatLoad;
        #endregion

        #region Inspector Fields - Diagram
        [Header("=== AUXILIARY SYSTEMS DIAGRAM ===")]
        [SerializeField] private Image diagram_RHR_HX_A;
        [SerializeField] private Image diagram_RHR_HX_B;
        [SerializeField] private Image diagram_RHR_Pump_A;
        [SerializeField] private Image diagram_RHR_Pump_B;
        [SerializeField] private Image diagram_CCW_HX_A;
        [SerializeField] private Image diagram_CCW_HX_B;
        [SerializeField] private Image diagram_CCW_Pumps;
        [SerializeField] private Image diagram_SW_Pumps;
        [SerializeField] private Image diagram_RCS_Connection;
        [SerializeField] private Image diagram_CCW_Header;
        [SerializeField] private Image diagram_SW_Header;
        [SerializeField] private TextMeshProUGUI diagram_RHR_StatusText;
        [SerializeField] private TextMeshProUGUI diagram_CCW_StatusText;
        [SerializeField] private TextMeshProUGUI diagram_SW_StatusText;
        #endregion

        #region Inspector Fields - Bottom Panel
        [Header("=== BOTTOM PANEL ===")]
        [SerializeField] private TextMeshProUGUI text_ReactorMode;
        [SerializeField] private TextMeshProUGUI text_SimTime;
        [SerializeField] private TextMeshProUGUI text_TimeCompression;
        [Header("RHR Controls (Visual Only)")]
        [SerializeField] private Button button_RHR_A_Start;
        [SerializeField] private Button button_RHR_A_Stop;
        [SerializeField] private Button button_RHR_B_Start;
        [SerializeField] private Button button_RHR_B_Stop;
        [SerializeField] private Image indicator_RHR_A;
        [SerializeField] private Image indicator_RHR_B;
        [SerializeField] private TextMeshProUGUI text_RHR_A_Status;
        [SerializeField] private TextMeshProUGUI text_RHR_B_Status;
        [Header("CCW/SW Controls (Visual Only)")]
        [SerializeField] private Button button_CCW_Start;
        [SerializeField] private Button button_SW_Start;
        [SerializeField] private Image indicator_CCW;
        [SerializeField] private Image indicator_SW;
        [SerializeField] private TextMeshProUGUI text_CCW_Status;
        [SerializeField] private TextMeshProUGUI text_SW_Status;
        [Header("Alarms")]
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
        private static readonly Color COLOR_EQUIPMENT_OFF = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color COLOR_INACTIVE = new Color(0.25f, 0.25f, 0.30f, 0.4f);
        #endregion

        #region Unity Lifecycle
        protected override void Awake() { base.Awake(); }

        protected override void Start()
        {
            base.Start();
            _data = ScreenDataBridge.Instance;
            if (_data == null)
                Debug.LogWarning("[AuxiliarySystemsScreen] ScreenDataBridge not found.");
            Debug.Log("[AuxiliarySystemsScreen] Initialized. Toggle: Key 8 (RHR LIVE, CCW/SW placeholder)");
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
                UpdateDiagram();
            }
        }
        #endregion

        #region Left Panel Updates (RHR)
        private void UpdateLeftPanelGauges()
        {
            // v4.3.0: RHR gauges now LIVE from RHRSystem.cs physics
            bool rhrActive = _data.GetRHRActive();

            // Flow — lumped model, both trains show same value
            float rhrFlow = _data.GetRHRFlow();
            SetLiveGauge(text_RHR_A_Flow, rhrFlow, "F0", " gpm");
            SetLiveGauge(text_RHR_B_Flow, rhrFlow, "F0", " gpm");

            // HX temperatures
            float hxInlet = _data.GetRHRHXInletTemp();
            float hxOutlet = _data.GetRHRHXOutletTemp();
            SetLiveGauge(text_RHR_HXA_InletTemp, hxInlet, "F1", " F");
            SetLiveGauge(text_RHR_HXA_OutletTemp, hxOutlet, "F1", " F");
            SetLiveGauge(text_RHR_HXB_InletTemp, hxInlet, "F1", " F");
            SetLiveGauge(text_RHR_HXB_OutletTemp, hxOutlet, "F1", " F");

            // Suction pressure
            SetLiveGauge(text_RHR_SuctionPressure, _data.GetRHRSuctionPressure(), "F0", " psig");

            // Pump status
            if (text_RHR_PumpStatus != null)
            {
                string mode = _data.GetRHRMode();
                text_RHR_PumpStatus.text = mode;
                text_RHR_PumpStatus.color = rhrActive ? COLOR_RUNNING : COLOR_STOPPED;
            }
        }
        #endregion

        #region Right Panel Updates (CCW/SW)
        private void UpdateRightPanelGauges()
        {
            // All CCW/SW gauges — PLACEHOLDER
            SetPlaceholder(text_CCW_SupplyP);
            SetPlaceholder(text_CCW_ReturnP);
            SetPlaceholder(text_CCW_SurgeTankLevel);
            SetPlaceholder(text_CCW_Temperature);
            SetPlaceholder(text_SW_Flow);
            SetPlaceholder(text_SW_Temperature);
            SetPlaceholder(text_RCP_ThermalBarrierFlow);
            SetPlaceholder(text_CCW_HeatLoad);
        }
        #endregion

        #region Diagram Updates
        private void UpdateDiagram()
        {
            // v4.3.0: RHR equipment is now LIVE
            bool rhrActive = _data.GetRHRActive();
            Color rhrColor = rhrActive ? COLOR_RUNNING : COLOR_EQUIPMENT_OFF;

            if (diagram_RHR_HX_A != null) diagram_RHR_HX_A.color = rhrColor;
            if (diagram_RHR_HX_B != null) diagram_RHR_HX_B.color = rhrColor;
            if (diagram_RHR_Pump_A != null) diagram_RHR_Pump_A.color = rhrColor;
            if (diagram_RHR_Pump_B != null) diagram_RHR_Pump_B.color = rhrColor;
            if (diagram_RCS_Connection != null)
                diagram_RCS_Connection.color = rhrActive ? COLOR_RUNNING : COLOR_INACTIVE;

            // CCW/SW remain placeholder
            if (diagram_CCW_HX_A != null) diagram_CCW_HX_A.color = COLOR_EQUIPMENT_OFF;
            if (diagram_CCW_HX_B != null) diagram_CCW_HX_B.color = COLOR_EQUIPMENT_OFF;
            if (diagram_CCW_Pumps != null) diagram_CCW_Pumps.color = COLOR_EQUIPMENT_OFF;
            if (diagram_SW_Pumps != null) diagram_SW_Pumps.color = COLOR_EQUIPMENT_OFF;
            if (diagram_CCW_Header != null) diagram_CCW_Header.color = COLOR_INACTIVE;
            if (diagram_SW_Header != null) diagram_SW_Header.color = COLOR_INACTIVE;

            // RHR status text — LIVE
            if (diagram_RHR_StatusText != null)
            {
                string mode = _data.GetRHRMode();
                diagram_RHR_StatusText.text = mode;
                diagram_RHR_StatusText.color = rhrActive ? COLOR_RUNNING : COLOR_STOPPED;
            }
            // CCW/SW still placeholder
            if (diagram_CCW_StatusText != null) { diagram_CCW_StatusText.text = "NOT MODELED"; diagram_CCW_StatusText.color = COLOR_PLACEHOLDER; }
            if (diagram_SW_StatusText != null) { diagram_SW_StatusText.text = "NOT MODELED"; diagram_SW_StatusText.color = COLOR_PLACEHOLDER; }
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
            // v4.3.0: RHR pump indicators are now LIVE
            bool rhrActive = _data.GetRHRActive();
            string rhrMode = _data.GetRHRMode();
            Color rhrIndicatorColor = rhrActive ? COLOR_RUNNING : COLOR_STOPPED;
            if (indicator_RHR_A != null) indicator_RHR_A.color = rhrIndicatorColor;
            if (indicator_RHR_B != null) indicator_RHR_B.color = rhrIndicatorColor;
            if (text_RHR_A_Status != null) { text_RHR_A_Status.text = rhrMode; text_RHR_A_Status.color = rhrIndicatorColor; }
            if (text_RHR_B_Status != null) { text_RHR_B_Status.text = rhrMode; text_RHR_B_Status.color = rhrIndicatorColor; }
            // CCW/SW remain placeholder
            if (indicator_CCW != null) indicator_CCW.color = COLOR_STOPPED;
            if (indicator_SW != null) indicator_SW.color = COLOR_STOPPED;
            if (text_CCW_Status != null) { text_CCW_Status.text = "STOPPED"; text_CCW_Status.color = COLOR_STOPPED; }
            if (text_SW_Status != null) { text_SW_Status.text = "STOPPED"; text_SW_Status.color = COLOR_STOPPED; }
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
        private void SetPlaceholder(TextMeshProUGUI textField)
        {
            if (textField != null) { textField.text = "---"; textField.color = COLOR_PLACEHOLDER; }
        }

        /// <summary>
        /// Set a gauge text field from a live value. Shows "---" if NaN.
        /// v4.3.0: Shared utility for live RHR data display.
        /// </summary>
        private void SetLiveGauge(TextMeshProUGUI textField, float value, string format, string suffix)
        {
            if (textField == null) return;
            if (float.IsNaN(value)) { textField.text = "---"; textField.color = COLOR_PLACEHOLDER; }
            else { textField.text = value.ToString(format) + suffix; textField.color = COLOR_NORMAL; }
        }
        #endregion
    }
}
