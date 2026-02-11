// ============================================================================
// CRITICAL: Master the Atom - CVCS Operator Screen
// CVCSScreen.cs - Screen 4: Chemical and Volume Control System
// ============================================================================
//
// PURPOSE:
//   Implements the CVCS operator screen (Key 4) displaying:
//   - Charging and letdown flows
//   - VCT level, temperature, and boron concentration
//   - RCS boron concentration and boration/dilution status
//   - Seal injection flow to RCPs
//   - 2D CVCS flow diagram with VCT, CCPs, letdown/charging lines,
//     seal injection branches, boration/dilution paths
//
// LAYOUT (1920x1080):
//   - Left Panel (0-15%): 8 flow/control gauges
//   - Center Panel (15-65%): 2D CVCS flow diagram
//   - Right Panel (65-100%): 8 chemistry/boron gauges
//   - Bottom Panel (0-26%): CCP controls, letdown controls,
//                            boration/dilution controls, alarm panel
//
// KEYBOARD:
//   - Key 4: Toggle screen visibility
//
// DATA SOURCES (via ScreenDataBridge):
//   - HeatupSimEngine: Charging flow, letdown flow, boron concentration,
//     VCT level, VCT boron, net inventory change
//   - Others: PLACEHOLDER (seal injection, VCT temp/pressure, boration/
//     dilution flows, boron worth, letdown/charging temp, purification)
//
// WESTINGHOUSE 4-LOOP PWR CVCS SPECIFICATIONS:
//   - Charging Pumps: 3 centrifugal charging pumps (CCP-A, B, C)
//     - Normal: 1 running, 2 standby
//     - Capacity: ~98 gpm each at 2500 psig
//     - CCP discharge pressure: ~2350-2500 psig
//   - Normal charging flow: ~46 gpm (balanced with letdown)
//   - Normal letdown flow: ~46 gpm (through letdown orifice)
//   - Letdown orifice sizes: 45, 60, 75 gpm (selectable)
//   - Seal injection: ~8 gpm per RCP (32 gpm total for 4 RCPs)
//   - VCT: ~4500 gallons capacity, normal level 20-80%
//   - VCT nominal pressure: ~15-20 psig (hydrogen overpressure)
//   - Boric acid concentration: 4 wt% (21,000 ppm) in BAST
//   - Normal RCS boron: varies by cycle burnup
//   - Mixed bed demineralizer downstream of letdown heat exchanger
//   - Letdown temperature: ~120 F after letdown heat exchanger
//   - Charging temperature: ~130 F (from VCT through regenerative HX)
//
// SOURCES:
//   - Operator_Screen_Layout_Plan_v1_0_0.md (Screen 4 specification)
//   - NRC HRTD Rev 0606 - CVCS Chapter
//   - Westinghouse 4-Loop PWR Technical Specifications
//   - IMPLEMENTATION_PLAN_v2.0.0_MultiScreenGUI.md (Stage 5)
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
    public class CVCSScreen : OperatorScreen
    {
        #region OperatorScreen Implementation
        public override KeyCode ToggleKey => KeyCode.Alpha4;
        public override string ScreenName => "CVCS";
        public override int ScreenIndex => 4;
        #endregion

        #region Constants
        // CVCS Design Parameters
        private const float NORMAL_CHARGING_FLOW_GPM = 46f;
        private const float NORMAL_LETDOWN_FLOW_GPM = 46f;
        private const float CCP_CAPACITY_GPM = 98f;
        private const float CCP_DISCHARGE_PRESSURE_PSIG = 2450f;
        private const float SEAL_INJECTION_PER_RCP_GPM = 8f;
        private const float SEAL_INJECTION_TOTAL_GPM = 32f;
        private const float VCT_CAPACITY_GAL = 4500f;
        private const float VCT_NORMAL_LEVEL_LOW_PCT = 20f;
        private const float VCT_NORMAL_LEVEL_HIGH_PCT = 80f;
        private const float VCT_ALARM_LOW_PCT = 15f;
        private const float VCT_ALARM_HIGH_PCT = 85f;
        private const float VCT_NOMINAL_PRESSURE_PSIG = 18f;
        private const float BAST_BORON_CONC_PPM = 21000f;
        private const float LETDOWN_ORIFICE_45_GPM = 45f;
        private const float LETDOWN_ORIFICE_60_GPM = 60f;
        private const float LETDOWN_ORIFICE_75_GPM = 75f;
        private const float LETDOWN_TEMP_AFTER_HX_F = 120f;
        private const float CHARGING_TEMP_F = 130f;

        // Alarm thresholds
        private const float BORON_HIGH_ALARM_PPM = 2000f;
        private const float BORON_LOW_ALARM_PPM = 10f;
        private const float NET_INVENTORY_WARNING_GPM = 10f;

        // Update throttling
        private const float GAUGE_UPDATE_INTERVAL = 0.1f;     // 10 Hz
        private const float DIAGRAM_UPDATE_INTERVAL = 0.5f;   // 2 Hz
        #endregion

        #region Inspector Fields - Left Panel
        [Header("=== LEFT PANEL - FLOW & CONTROL ===")]
        [SerializeField] private TextMeshProUGUI text_ChargingFlow;
        [SerializeField] private TextMeshProUGUI text_LetdownFlow;
        [SerializeField] private TextMeshProUGUI text_SealInjectionFlow;
        [SerializeField] private TextMeshProUGUI text_NetInventoryChange;
        [SerializeField] private TextMeshProUGUI text_VCTLevel;
        [SerializeField] private TextMeshProUGUI text_VCTTemperature;
        [SerializeField] private TextMeshProUGUI text_VCTPressure;
        [SerializeField] private TextMeshProUGUI text_CCPDischargePressure;
        #endregion

        #region Inspector Fields - Right Panel
        [Header("=== RIGHT PANEL - CHEMISTRY & BORON ===")]
        [SerializeField] private TextMeshProUGUI text_RCSBoronConc;
        [SerializeField] private TextMeshProUGUI text_VCTBoronConc;
        [SerializeField] private TextMeshProUGUI text_BorationFlow;
        [SerializeField] private TextMeshProUGUI text_DilutionFlow;
        [SerializeField] private TextMeshProUGUI text_BoronWorth;
        [SerializeField] private TextMeshProUGUI text_LetdownTemp;
        [SerializeField] private TextMeshProUGUI text_ChargingTemp;
        [SerializeField] private TextMeshProUGUI text_PurificationFlow;
        #endregion

        #region Inspector Fields - Flow Diagram
        [Header("=== FLOW DIAGRAM ===")]
        [SerializeField] private Image diagram_VCTTank;
        [SerializeField] private Image diagram_VCTLevelFill;
        [SerializeField] private Image diagram_CCP_A;
        [SerializeField] private Image diagram_CCP_B;
        [SerializeField] private Image diagram_CCP_C;
        [SerializeField] private Image diagram_LetdownLine;
        [SerializeField] private Image diagram_ChargingLine;
        [SerializeField] private Image[] diagram_SealInjectionLines = new Image[4];
        [SerializeField] private Image diagram_BorationPath;
        [SerializeField] private Image diagram_DilutionPath;
        [SerializeField] private Image diagram_Demineralizer;
        [SerializeField] private Image diagram_LetdownHX;
        [SerializeField] private Image diagram_RCSConnection;
        [SerializeField] private TextMeshProUGUI diagram_VCTLevelText;
        [SerializeField] private TextMeshProUGUI diagram_VCTBoronText;
        [SerializeField] private TextMeshProUGUI diagram_ChargingFlowText;
        [SerializeField] private TextMeshProUGUI diagram_LetdownFlowText;
        [SerializeField] private TextMeshProUGUI diagram_RCSBoronText;
        #endregion

        #region Inspector Fields - Bottom Panel
        [Header("=== BOTTOM PANEL - CONTROLS & STATUS ===")]
        [SerializeField] private TextMeshProUGUI text_ReactorMode;
        [SerializeField] private TextMeshProUGUI text_SimTime;
        [SerializeField] private TextMeshProUGUI text_TimeCompression;
        [Header("CCP Controls (Visual Only)")]
        [SerializeField] private Button button_CCP_A_Start;
        [SerializeField] private Button button_CCP_A_Stop;
        [SerializeField] private Image indicator_CCP_A;
        [SerializeField] private TextMeshProUGUI text_CCP_A_Status;
        [SerializeField] private Button button_CCP_B_Start;
        [SerializeField] private Button button_CCP_B_Stop;
        [SerializeField] private Image indicator_CCP_B;
        [SerializeField] private TextMeshProUGUI text_CCP_B_Status;
        [SerializeField] private Button button_CCP_C_Start;
        [SerializeField] private Button button_CCP_C_Stop;
        [SerializeField] private Image indicator_CCP_C;
        [SerializeField] private TextMeshProUGUI text_CCP_C_Status;
        [Header("Boration / Dilution (Visual Only)")]
        [SerializeField] private Button button_Borate;
        [SerializeField] private Button button_Dilute;
        [SerializeField] private TextMeshProUGUI text_BoronMode;
        [Header("Alarm Panel")]
        [SerializeField] private Transform alarmContainer;
        #endregion

        #region Private Fields
        private ScreenDataBridge _data;
        private float _lastGaugeUpdate;
        private float _lastDiagramUpdate;

        private static readonly Color COLOR_RUNNING = new Color(0.2f, 0.9f, 0.2f);
        private static readonly Color COLOR_STOPPED = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color COLOR_WARNING = new Color(1f, 0.7f, 0.2f);
        private static readonly Color COLOR_ALARM = new Color(0.9f, 0.2f, 0.2f);
        private static readonly Color COLOR_NORMAL_TEXT = new Color(0f, 1f, 0.53f);
        private static readonly Color COLOR_PLACEHOLDER = new Color(0.4f, 0.4f, 0.5f);

        // Flow diagram colors
        private static readonly Color COLOR_FLOW_ACTIVE = new Color(0.2f, 0.6f, 1f, 0.8f);
        private static readonly Color COLOR_FLOW_INACTIVE = new Color(0.25f, 0.25f, 0.30f, 0.4f);
        private static readonly Color COLOR_PUMP_RUNNING = new Color(0.2f, 0.8f, 0.3f);
        private static readonly Color COLOR_PUMP_STOPPED = new Color(0.35f, 0.35f, 0.35f);
        private static readonly Color COLOR_VCT_WATER = new Color(0.15f, 0.4f, 0.7f, 0.7f);
        private static readonly Color COLOR_BORATION = new Color(0.8f, 0.4f, 0.1f, 0.7f);
        private static readonly Color COLOR_DILUTION = new Color(0.3f, 0.7f, 1f, 0.7f);
        private static readonly Color COLOR_EQUIPMENT = new Color(0.2f, 0.2f, 0.28f);
        #endregion

        #region Unity Lifecycle
        protected override void Awake() { base.Awake(); }

        protected override void Start()
        {
            base.Start();
            _data = ScreenDataBridge.Instance;
            if (_data == null)
                Debug.LogWarning("[CVCSScreen] ScreenDataBridge not found.");
            Debug.Log("[CVCSScreen] Initialized. Toggle: Key 4");
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
            if (time - _lastDiagramUpdate >= DIAGRAM_UPDATE_INTERVAL)
            {
                _lastDiagramUpdate = time;
                UpdateFlowDiagram();
            }
        }
        #endregion

        #region Left Panel Updates
        private void UpdateLeftPanelGauges()
        {
            // Charging Flow
            if (text_ChargingFlow != null)
            {
                float cf = _data.GetChargingFlow();
                SetGaugeText(text_ChargingFlow, cf, "F1", " gpm");
            }
            // Letdown Flow
            if (text_LetdownFlow != null)
            {
                float lf = _data.GetLetdownFlow();
                SetGaugeText(text_LetdownFlow, lf, "F1", " gpm");
            }
            // Seal Injection Flow — PLACEHOLDER
            if (text_SealInjectionFlow != null)
            {
                text_SealInjectionFlow.text = "---";
                text_SealInjectionFlow.color = COLOR_PLACEHOLDER;
            }
            // Net Inventory Change
            if (text_NetInventoryChange != null)
            {
                float net = _data.GetNetInventoryChange();
                if (!float.IsNaN(net))
                {
                    string sign = net >= 0 ? "+" : "";
                    text_NetInventoryChange.text = $"{sign}{net:F1} gpm";
                    text_NetInventoryChange.color = Mathf.Abs(net) > NET_INVENTORY_WARNING_GPM
                        ? COLOR_WARNING : COLOR_NORMAL_TEXT;
                }
                else { text_NetInventoryChange.text = "---"; text_NetInventoryChange.color = COLOR_PLACEHOLDER; }
            }
            // VCT Level
            if (text_VCTLevel != null)
            {
                float vl = _data.GetVCTLevel();
                SetGaugeText(text_VCTLevel, vl, "F1", "%");
                if (!float.IsNaN(vl))
                {
                    if (vl <= VCT_ALARM_LOW_PCT || vl >= VCT_ALARM_HIGH_PCT) text_VCTLevel.color = COLOR_ALARM;
                    else if (vl <= VCT_NORMAL_LEVEL_LOW_PCT || vl >= VCT_NORMAL_LEVEL_HIGH_PCT) text_VCTLevel.color = COLOR_WARNING;
                }
            }
            // VCT Temperature — PLACEHOLDER
            if (text_VCTTemperature != null)
            {
                text_VCTTemperature.text = "---";
                text_VCTTemperature.color = COLOR_PLACEHOLDER;
            }
            // VCT Pressure — PLACEHOLDER
            if (text_VCTPressure != null)
            {
                text_VCTPressure.text = "---";
                text_VCTPressure.color = COLOR_PLACEHOLDER;
            }
            // CCP Discharge Pressure — PLACEHOLDER
            if (text_CCPDischargePressure != null)
            {
                text_CCPDischargePressure.text = "---";
                text_CCPDischargePressure.color = COLOR_PLACEHOLDER;
            }
        }
        #endregion

        #region Right Panel Updates
        private void UpdateRightPanelGauges()
        {
            // RCS Boron Concentration
            if (text_RCSBoronConc != null)
            {
                float b = _data.GetBoronConcentration();
                SetGaugeText(text_RCSBoronConc, b, "F0", " ppm");
                if (!float.IsNaN(b) && (b > BORON_HIGH_ALARM_PPM || b < BORON_LOW_ALARM_PPM))
                    text_RCSBoronConc.color = COLOR_WARNING;
            }
            // VCT Boron Concentration
            if (text_VCTBoronConc != null)
            {
                float vb = _data.GetVCTBoronConcentration();
                SetGaugeText(text_VCTBoronConc, vb, "F0", " ppm");
            }
            // Boration Flow — PLACEHOLDER
            if (text_BorationFlow != null) { text_BorationFlow.text = "---"; text_BorationFlow.color = COLOR_PLACEHOLDER; }
            // Dilution Flow — PLACEHOLDER
            if (text_DilutionFlow != null) { text_DilutionFlow.text = "---"; text_DilutionFlow.color = COLOR_PLACEHOLDER; }
            // Boron Worth — PLACEHOLDER
            if (text_BoronWorth != null) { text_BoronWorth.text = "---"; text_BoronWorth.color = COLOR_PLACEHOLDER; }
            // Letdown Temperature — PLACEHOLDER
            if (text_LetdownTemp != null) { text_LetdownTemp.text = "---"; text_LetdownTemp.color = COLOR_PLACEHOLDER; }
            // Charging Temperature — PLACEHOLDER
            if (text_ChargingTemp != null) { text_ChargingTemp.text = "---"; text_ChargingTemp.color = COLOR_PLACEHOLDER; }
            // Purification Flow — PLACEHOLDER
            if (text_PurificationFlow != null) { text_PurificationFlow.text = "---"; text_PurificationFlow.color = COLOR_PLACEHOLDER; }
        }
        #endregion

        #region Flow Diagram Updates
        private void UpdateFlowDiagram()
        {
            UpdateVCTVisualization();
            UpdateFlowLines();
            UpdateCCPIndicators();
            UpdateDiagramOverlayText();
        }

        private void UpdateVCTVisualization()
        {
            if (diagram_VCTLevelFill == null) return;
            float vl = _data.GetVCTLevel();
            if (!float.IsNaN(vl))
            {
                diagram_VCTLevelFill.fillAmount = Mathf.Clamp01(vl / 100f);
                diagram_VCTLevelFill.color = COLOR_VCT_WATER;
            }
            else diagram_VCTLevelFill.fillAmount = 0f;
        }

        private void UpdateFlowLines()
        {
            float cf = _data.GetChargingFlow();
            float lf = _data.GetLetdownFlow();
            bool chargingActive = !float.IsNaN(cf) && cf > 1f;
            bool letdownActive = !float.IsNaN(lf) && lf > 1f;

            if (diagram_ChargingLine != null)
                diagram_ChargingLine.color = chargingActive ? COLOR_FLOW_ACTIVE : COLOR_FLOW_INACTIVE;
            if (diagram_LetdownLine != null)
                diagram_LetdownLine.color = letdownActive ? COLOR_FLOW_ACTIVE : COLOR_FLOW_INACTIVE;

            // Seal injection lines — active when charging is active (fed from CCP discharge)
            for (int i = 0; i < diagram_SealInjectionLines.Length; i++)
                if (diagram_SealInjectionLines[i] != null)
                    diagram_SealInjectionLines[i].color = chargingActive ? new Color(0.3f, 0.8f, 0.6f, 0.6f) : COLOR_FLOW_INACTIVE;

            // Boration/dilution paths — PLACEHOLDER (no boration model, show inactive)
            if (diagram_BorationPath != null) diagram_BorationPath.color = COLOR_FLOW_INACTIVE;
            if (diagram_DilutionPath != null) diagram_DilutionPath.color = COLOR_FLOW_INACTIVE;

            // Demineralizer and HX — active with letdown
            if (diagram_Demineralizer != null)
                diagram_Demineralizer.color = letdownActive ? COLOR_EQUIPMENT : new Color(0.15f, 0.15f, 0.18f);
            if (diagram_LetdownHX != null)
                diagram_LetdownHX.color = letdownActive ? COLOR_EQUIPMENT : new Color(0.15f, 0.15f, 0.18f);

            // RCS connection indicator — active when either flow path is running
            if (diagram_RCSConnection != null)
                diagram_RCSConnection.color = (chargingActive || letdownActive)
                    ? new Color(0.6f, 0.2f, 0.2f, 0.8f) : COLOR_FLOW_INACTIVE;
        }

        private void UpdateCCPIndicators()
        {
            // CCP status — infer from charging flow: if charging > 0, at least one CCP running
            float cf = _data.GetChargingFlow();
            bool anyRunning = !float.IsNaN(cf) && cf > 1f;

            // CCP-A assumed running if any charging flow present; B, C standby
            UpdateSingleCCPIndicator(diagram_CCP_A, indicator_CCP_A, text_CCP_A_Status, "CCP-A", anyRunning);
            UpdateSingleCCPIndicator(diagram_CCP_B, indicator_CCP_B, text_CCP_B_Status, "CCP-B", false);
            UpdateSingleCCPIndicator(diagram_CCP_C, indicator_CCP_C, text_CCP_C_Status, "CCP-C", false);
        }

        private void UpdateSingleCCPIndicator(Image diagramIcon, Image indicator, TextMeshProUGUI statusText, string name, bool running)
        {
            if (diagramIcon != null) diagramIcon.color = running ? COLOR_PUMP_RUNNING : COLOR_PUMP_STOPPED;
            if (indicator != null) indicator.color = running ? COLOR_RUNNING : COLOR_STOPPED;
            if (statusText != null)
            {
                statusText.text = $"{name}: {(running ? "RUN" : "STBY")}";
                statusText.color = running ? COLOR_RUNNING : COLOR_STOPPED;
            }
        }

        private void UpdateDiagramOverlayText()
        {
            if (diagram_VCTLevelText != null)
            {
                float vl = _data.GetVCTLevel();
                diagram_VCTLevelText.text = float.IsNaN(vl) ? "--- %" : $"{vl:F1}%";
            }
            if (diagram_VCTBoronText != null)
            {
                float vb = _data.GetVCTBoronConcentration();
                diagram_VCTBoronText.text = float.IsNaN(vb) ? "--- ppm" : $"{vb:F0} ppm";
            }
            if (diagram_ChargingFlowText != null)
            {
                float cf = _data.GetChargingFlow();
                diagram_ChargingFlowText.text = float.IsNaN(cf) ? "--- gpm" : $"{cf:F1} gpm";
            }
            if (diagram_LetdownFlowText != null)
            {
                float lf = _data.GetLetdownFlow();
                diagram_LetdownFlowText.text = float.IsNaN(lf) ? "--- gpm" : $"{lf:F1} gpm";
            }
            if (diagram_RCSBoronText != null)
            {
                float b = _data.GetBoronConcentration();
                diagram_RCSBoronText.text = float.IsNaN(b) ? "--- ppm" : $"{b:F0} ppm";
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
            // Boron mode — PLACEHOLDER (no boration/dilution controller yet)
            if (text_BoronMode != null)
            {
                text_BoronMode.text = "MANUAL";
                text_BoronMode.color = COLOR_STOPPED;
            }
        }
        #endregion

        #region Screen Lifecycle
        protected override void OnScreenShownInternal()
        {
            base.OnScreenShownInternal();
            _lastGaugeUpdate = 0f;
            _lastDiagramUpdate = 0f;
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
