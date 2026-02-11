// ============================================================================
// CRITICAL: Master the Atom - Secondary Systems Operator Screen
// SecondarySystemsScreen.cs - Screen 7: Secondary Systems (Combined)
// ============================================================================
//
// PURPOSE:
//   Implements the Secondary Systems operator screen (Key 7) displaying:
//   - Feedwater train gauges (condensate, deaerator, FW pumps)
//   - Steam system gauges (main steam, steam dump, MSIVs)
//   - Secondary cycle flow diagram
//   - Steam dump controls and MSIV controls (visual only)
//
// LAYOUT (1920x1080):
//   - Left Panel (0-15%): 8 feedwater train gauges
//   - Center Panel (15-65%): Secondary cycle flow diagram
//   - Right Panel (65-100%): 8 steam system gauges
//   - Bottom Panel (0-26%): Steam dump controls, MSIV controls, alarms
//
// KEYBOARD:
//   - Key 7: Toggle screen visibility
//
// DATA SOURCES (via ScreenDataBridge):
//   - Steam dump active/heat/demand from SteamDumpController
//   - Steam pressure (psig) from SG model
//   - All feedwater parameters: PLACEHOLDER
//   - All MSIV parameters: PLACEHOLDER
//
// WESTINGHOUSE 4-LOOP PWR SECONDARY SYSTEM SPECIFICATIONS:
//   - Main steam pressure: ~1000-1100 psig at full power
//   - Steam dump to condenser: 40% capacity (~1366 MWt equivalent)
//   - Steam dump setpoint: 1092 psig (steam pressure mode)
//   - 4 MSIVs (one per SG loop)
//   - Condensate pumps: 3 x 50% (2 running, 1 standby)
//   - Main feedwater pumps: 3 x 50% (turbine-driven)
//   - Deaerator: ~100 psig, ~50 ft storage tank
//   - 6 stages feedwater heating (3 LP, 3 HP)
//   - Final feedwater temperature: ~440 F
//   - Condensate polishing demineralizers
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
    public class SecondarySystemsScreen : OperatorScreen
    {
        #region OperatorScreen Implementation
        public override KeyCode ToggleKey => KeyCode.Alpha7;
        public override string ScreenName => "SECONDARY SYSTEMS";
        public override int ScreenIndex => 7;
        #endregion

        #region Constants
        private const float STEAM_DUMP_SETPOINT_PSIG = 1092f;
        private const float GAUGE_UPDATE_INTERVAL = 0.1f;
        private const float VISUAL_UPDATE_INTERVAL = 0.5f;
        #endregion

        #region Inspector Fields - Left Panel
        [Header("=== LEFT PANEL - FEEDWATER TRAIN ===")]
        [SerializeField] private TextMeshProUGUI text_HotwellLevel;
        [SerializeField] private TextMeshProUGUI text_CondPumpDischP;
        [SerializeField] private TextMeshProUGUI text_DeaeratorPressure;
        [SerializeField] private TextMeshProUGUI text_DeaeratorLevel;
        [SerializeField] private TextMeshProUGUI text_FWPumpSuctionP;
        [SerializeField] private TextMeshProUGUI text_FWPumpDischP;
        [SerializeField] private TextMeshProUGUI text_FinalFWTemp;
        [SerializeField] private TextMeshProUGUI text_FWFlowTotal;
        #endregion

        #region Inspector Fields - Right Panel
        [Header("=== RIGHT PANEL - STEAM SYSTEM ===")]
        [SerializeField] private TextMeshProUGUI text_MainSteamPressure;
        [SerializeField] private TextMeshProUGUI text_SteamFlowToTurbine;
        [SerializeField] private TextMeshProUGUI text_SteamDumpFlow;
        [SerializeField] private TextMeshProUGUI text_MSIV_A;
        [SerializeField] private TextMeshProUGUI text_MSIV_B;
        [SerializeField] private TextMeshProUGUI text_MSIV_C;
        [SerializeField] private TextMeshProUGUI text_MSIV_D;
        [SerializeField] private TextMeshProUGUI text_TurbineBypass;
        #endregion

        #region Inspector Fields - Flow Diagram
        [Header("=== FLOW DIAGRAM ===")]
        [SerializeField] private Image diagram_Condenser;
        [SerializeField] private Image diagram_CondPumps;
        [SerializeField] private Image diagram_LPHeaters;
        [SerializeField] private Image diagram_Deaerator;
        [SerializeField] private Image diagram_FWPumps;
        [SerializeField] private Image diagram_HPHeaters;
        [SerializeField] private Image diagram_MainSteamHeader;
        [SerializeField] private Image diagram_SteamDumpValves;
        [SerializeField] private Image diagram_MSIVBlock;
        [SerializeField] private Image diagram_FWToSGLines;
        [SerializeField] private Image diagram_SteamFromSGLines;
        [SerializeField] private TextMeshProUGUI diagram_SteamPressureText;
        [SerializeField] private TextMeshProUGUI diagram_SteamDumpText;
        [SerializeField] private TextMeshProUGUI diagram_FWFlowText;
        #endregion

        #region Inspector Fields - Bottom Panel
        [Header("=== BOTTOM PANEL ===")]
        [SerializeField] private TextMeshProUGUI text_ReactorMode;
        [SerializeField] private TextMeshProUGUI text_SimTime;
        [SerializeField] private TextMeshProUGUI text_TimeCompression;
        [Header("Steam Dump Controls (Visual Only)")]
        [SerializeField] private Button button_SteamDumpAuto;
        [SerializeField] private Button button_SteamDumpManual;
        [SerializeField] private TextMeshProUGUI text_SteamDumpMode;
        [SerializeField] private TextMeshProUGUI text_SteamDumpDemand;
        [SerializeField] private TextMeshProUGUI text_SteamDumpHeat;
        [Header("MSIV Controls (Visual Only)")]
        [SerializeField] private Button button_MSIV_Open;
        [SerializeField] private Button button_MSIV_Close;
        [SerializeField] private TextMeshProUGUI text_MSIVStatus;
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
        private static readonly Color COLOR_ACTIVE_FLOW = new Color(0.2f, 0.6f, 1f, 0.8f);
        private static readonly Color COLOR_INACTIVE_FLOW = new Color(0.25f, 0.25f, 0.30f, 0.4f);
        private static readonly Color COLOR_EQUIPMENT = new Color(0.2f, 0.2f, 0.28f);
        private static readonly Color COLOR_EQUIPMENT_OFF = new Color(0.15f, 0.15f, 0.18f);
        private static readonly Color COLOR_STEAM_DUMP_ACTIVE = new Color(0.9f, 0.5f, 0.1f, 0.8f);
        #endregion

        #region Unity Lifecycle
        protected override void Awake() { base.Awake(); }

        protected override void Start()
        {
            base.Start();
            _data = ScreenDataBridge.Instance;
            if (_data == null)
                Debug.LogWarning("[SecondarySystemsScreen] ScreenDataBridge not found.");
            Debug.Log("[SecondarySystemsScreen] Initialized. Toggle: Key 7");
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
                UpdateFlowDiagram();
            }
        }
        #endregion

        #region Left Panel Updates (Feedwater Train)
        private void UpdateLeftPanelGauges()
        {
            // All feedwater train gauges — PLACEHOLDER
            SetPlaceholder(text_HotwellLevel);
            SetPlaceholder(text_CondPumpDischP);
            SetPlaceholder(text_DeaeratorPressure);
            SetPlaceholder(text_DeaeratorLevel);
            SetPlaceholder(text_FWPumpSuctionP);
            SetPlaceholder(text_FWPumpDischP);
            SetPlaceholder(text_FinalFWTemp);
            SetPlaceholder(text_FWFlowTotal);
        }
        #endregion

        #region Right Panel Updates (Steam System)
        private void UpdateRightPanelGauges()
        {
            // Main steam pressure — live
            if (text_MainSteamPressure != null)
            {
                float sp = _data.GetSteamPressure();
                SetGaugeText(text_MainSteamPressure, sp, "F0", " psig");
            }
            // Steam flow to turbine — PLACEHOLDER
            SetPlaceholder(text_SteamFlowToTurbine);
            // Steam dump flow — derived from demand and heat
            if (text_SteamDumpFlow != null)
            {
                float sdHeat = _data.GetSteamDumpHeat();
                bool sdActive = _data.GetSteamDumpActive();
                if (sdActive && !float.IsNaN(sdHeat))
                {
                    text_SteamDumpFlow.text = $"{sdHeat:F1} MW";
                    text_SteamDumpFlow.color = COLOR_NORMAL;
                }
                else { text_SteamDumpFlow.text = "0.0 MW"; text_SteamDumpFlow.color = COLOR_STOPPED; }
            }
            // MSIVs — PLACEHOLDER (assume all open)
            SetMSIVText(text_MSIV_A, "MSIV-A", true);
            SetMSIVText(text_MSIV_B, "MSIV-B", true);
            SetMSIVText(text_MSIV_C, "MSIV-C", true);
            SetMSIVText(text_MSIV_D, "MSIV-D", true);
            // Turbine bypass — PLACEHOLDER
            SetPlaceholder(text_TurbineBypass);
        }

        private void SetMSIVText(TextMeshProUGUI field, string label, bool open)
        {
            if (field == null) return;
            field.text = open ? "OPEN" : "CLOSED";
            field.color = open ? COLOR_RUNNING : COLOR_ALARM;
        }
        #endregion

        #region Flow Diagram Updates
        private void UpdateFlowDiagram()
        {
            bool sdActive = _data.GetSteamDumpActive();

            // Steam dump valves — active color when dumping
            if (diagram_SteamDumpValves != null)
                diagram_SteamDumpValves.color = sdActive ? COLOR_STEAM_DUMP_ACTIVE : COLOR_EQUIPMENT_OFF;

            // Main steam header — active based on steam pressure
            float sp = _data.GetSteamPressure();
            bool steamPresent = !float.IsNaN(sp) && sp > 100f;
            if (diagram_MainSteamHeader != null)
                diagram_MainSteamHeader.color = steamPresent ? COLOR_ACTIVE_FLOW : COLOR_INACTIVE_FLOW;
            if (diagram_SteamFromSGLines != null)
                diagram_SteamFromSGLines.color = steamPresent ? COLOR_ACTIVE_FLOW : COLOR_INACTIVE_FLOW;

            // MSIV block — assume open
            if (diagram_MSIVBlock != null)
                diagram_MSIVBlock.color = COLOR_RUNNING;

            // FW side — all inactive (no FW model)
            if (diagram_Condenser != null) diagram_Condenser.color = COLOR_EQUIPMENT_OFF;
            if (diagram_CondPumps != null) diagram_CondPumps.color = COLOR_EQUIPMENT_OFF;
            if (diagram_LPHeaters != null) diagram_LPHeaters.color = COLOR_EQUIPMENT_OFF;
            if (diagram_Deaerator != null) diagram_Deaerator.color = COLOR_EQUIPMENT_OFF;
            if (diagram_FWPumps != null) diagram_FWPumps.color = COLOR_EQUIPMENT_OFF;
            if (diagram_HPHeaters != null) diagram_HPHeaters.color = COLOR_EQUIPMENT_OFF;
            if (diagram_FWToSGLines != null) diagram_FWToSGLines.color = COLOR_INACTIVE_FLOW;

            // Overlay text
            if (diagram_SteamPressureText != null)
                diagram_SteamPressureText.text = float.IsNaN(sp) ? "--- psig" : $"{sp:F0} psig";
            if (diagram_SteamDumpText != null)
            {
                float sdDemand = _data.GetSteamDumpDemand();
                if (sdActive && !float.IsNaN(sdDemand))
                    diagram_SteamDumpText.text = $"DUMP {sdDemand * 100f:F0}%";
                else
                    diagram_SteamDumpText.text = "DUMP OFF";
            }
            if (diagram_FWFlowText != null)
            {
                diagram_FWFlowText.text = "FW: ---";
                diagram_FWFlowText.color = COLOR_PLACEHOLDER;
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
            // Steam dump status
            if (text_SteamDumpMode != null)
            {
                bool sdActive = _data.GetSteamDumpActive();
                text_SteamDumpMode.text = sdActive ? "STM P MODE" : "OFF";
                text_SteamDumpMode.color = sdActive ? COLOR_RUNNING : COLOR_STOPPED;
            }
            if (text_SteamDumpDemand != null)
            {
                float demand = _data.GetSteamDumpDemand();
                if (!float.IsNaN(demand))
                {
                    text_SteamDumpDemand.text = $"{demand * 100f:F0}%";
                    text_SteamDumpDemand.color = demand > 0.01f ? COLOR_NORMAL : COLOR_STOPPED;
                }
                else { text_SteamDumpDemand.text = "---"; text_SteamDumpDemand.color = COLOR_PLACEHOLDER; }
            }
            if (text_SteamDumpHeat != null)
            {
                float sdHeat = _data.GetSteamDumpHeat();
                if (!float.IsNaN(sdHeat))
                {
                    text_SteamDumpHeat.text = $"{sdHeat:F1} MW";
                    text_SteamDumpHeat.color = sdHeat > 0.1f ? COLOR_NORMAL : COLOR_STOPPED;
                }
                else { text_SteamDumpHeat.text = "---"; text_SteamDumpHeat.color = COLOR_PLACEHOLDER; }
            }
            // MSIV status — PLACEHOLDER (all open)
            if (text_MSIVStatus != null)
            {
                text_MSIVStatus.text = "ALL OPEN";
                text_MSIVStatus.color = COLOR_RUNNING;
            }
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
