// ============================================================================
// CRITICAL: Master the Atom - Alarms Panel
// AlarmsPanel.cs - Annunciator Tile Display
// ============================================================================
// TAB: 5 (ALARMS)
// VERSION: 1.0.1
// IP: IP-0031 Stage 4
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Critical.Physics;

namespace Critical.UI.ValidationDashboard
{
    public class AlarmsPanel : ValidationPanelBase
    {
        public override string PanelName => "AlarmsPanel";
        public override int TabIndex => 5;

        // RCS Alarms
        private StatusIndicator _pressHighIndicator;
        private StatusIndicator _pressLowIndicator;
        private StatusIndicator _subcoolLowIndicator;
        private StatusIndicator _flowLowIndicator;

        // PZR Alarms
        private StatusIndicator _pzrLevelHighIndicator;
        private StatusIndicator _pzrLevelLowIndicator;
        private StatusIndicator _heaterFailIndicator;
        private StatusIndicator _sprayFailIndicator;

        // CVCS Alarms
        private StatusIndicator _vctLevelHighIndicator;
        private StatusIndicator _vctLevelLowIndicator;
        private StatusIndicator _makeupActiveIndicator;
        private StatusIndicator _divertActiveIndicator;

        // SG/RHR Alarms
        private StatusIndicator _sgPressHighIndicator;
        private StatusIndicator _rhrFlowLowIndicator;
        private StatusIndicator _boilingIndicator;

        // System Alarms
        private StatusIndicator _massErrorIndicator;

        protected override void OnInitialize()
        {
            BuildLayout();
        }

        private void BuildLayout()
        {
            GameObject mainGO = new GameObject("AlarmGrid");
            mainGO.transform.SetParent(transform, false);

            RectTransform mainRT = mainGO.AddComponent<RectTransform>();
            mainRT.anchorMin = Vector2.zero;
            mainRT.anchorMax = Vector2.one;
            mainRT.offsetMin = Vector2.zero;
            mainRT.offsetMax = Vector2.zero;

            VerticalLayoutGroup mainLayout = mainGO.AddComponent<VerticalLayoutGroup>();
            mainLayout.childAlignment = TextAnchor.UpperCenter;
            mainLayout.childControlWidth = true;
            mainLayout.childControlHeight = false;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = false;
            mainLayout.spacing = 12;
            mainLayout.padding = new RectOffset(8, 8, 8, 8);

            // RCS Row
            Transform rcsRow = CreateAlarmRow(mainGO.transform, "RCS ALARMS");
            _pressHighIndicator = StatusIndicator.Create(rcsRow, "PRESS HI", StatusIndicator.IndicatorShape.Pill, 75f, 28f);
            _pressLowIndicator = StatusIndicator.Create(rcsRow, "PRESS LO", StatusIndicator.IndicatorShape.Pill, 75f, 28f);
            _subcoolLowIndicator = StatusIndicator.Create(rcsRow, "SUBCOOL", StatusIndicator.IndicatorShape.Pill, 75f, 28f);
            _flowLowIndicator = StatusIndicator.Create(rcsRow, "FLOW LO", StatusIndicator.IndicatorShape.Pill, 70f, 28f);

            // PZR Row
            Transform pzrRow = CreateAlarmRow(mainGO.transform, "PRESSURIZER ALARMS");
            _pzrLevelHighIndicator = StatusIndicator.Create(pzrRow, "LVL HI", StatusIndicator.IndicatorShape.Pill, 65f, 28f);
            _pzrLevelLowIndicator = StatusIndicator.Create(pzrRow, "LVL LO", StatusIndicator.IndicatorShape.Pill, 65f, 28f);
            _heaterFailIndicator = StatusIndicator.Create(pzrRow, "HTR FAIL", StatusIndicator.IndicatorShape.Pill, 75f, 28f);
            _sprayFailIndicator = StatusIndicator.Create(pzrRow, "SPRAY", StatusIndicator.IndicatorShape.Pill, 60f, 28f);

            // CVCS Row
            Transform cvcsRow = CreateAlarmRow(mainGO.transform, "CVCS ALARMS");
            _vctLevelHighIndicator = StatusIndicator.Create(cvcsRow, "VCT HI", StatusIndicator.IndicatorShape.Pill, 65f, 28f);
            _vctLevelLowIndicator = StatusIndicator.Create(cvcsRow, "VCT LO", StatusIndicator.IndicatorShape.Pill, 65f, 28f);
            _makeupActiveIndicator = StatusIndicator.Create(cvcsRow, "MAKEUP", StatusIndicator.IndicatorShape.Pill, 70f, 28f);
            _divertActiveIndicator = StatusIndicator.Create(cvcsRow, "DIVERT", StatusIndicator.IndicatorShape.Pill, 65f, 28f);

            // SG/RHR Row
            Transform sgRow = CreateAlarmRow(mainGO.transform, "SG / RHR ALARMS");
            _sgPressHighIndicator = StatusIndicator.Create(sgRow, "SG PRESS", StatusIndicator.IndicatorShape.Pill, 75f, 28f);
            _rhrFlowLowIndicator = StatusIndicator.Create(sgRow, "RHR FLOW", StatusIndicator.IndicatorShape.Pill, 80f, 28f);
            _boilingIndicator = StatusIndicator.Create(sgRow, "BOILING", StatusIndicator.IndicatorShape.Pill, 70f, 28f);

            // System Row
            Transform sysRow = CreateAlarmRow(mainGO.transform, "SYSTEM ALARMS");
            _massErrorIndicator = StatusIndicator.Create(sysRow, "MASS ERR", StatusIndicator.IndicatorShape.Pill, 80f, 28f);
        }

        private Transform CreateAlarmRow(Transform parent, string title)
        {
            GameObject rowGO = new GameObject("Row_" + title);
            rowGO.transform.SetParent(parent, false);

            VerticalLayoutGroup vLayout = rowGO.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.spacing = 4;
            vLayout.padding = new RectOffset(4, 4, 4, 4);

            // Header
            GameObject headerGO = new GameObject("Header");
            headerGO.transform.SetParent(rowGO.transform, false);
            LayoutElement headerLE = headerGO.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 20;
            TextMeshProUGUI headerText = headerGO.AddComponent<TextMeshProUGUI>();
            headerText.text = title;
            headerText.fontSize = 10;
            headerText.fontStyle = FontStyles.Bold;
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.color = ValidationDashboardTheme.TextSecondary;

            // Indicator row
            GameObject indicatorRowGO = new GameObject("Indicators");
            indicatorRowGO.transform.SetParent(rowGO.transform, false);
            HorizontalLayoutGroup hLayout = indicatorRowGO.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;
            hLayout.spacing = 8;
            hLayout.padding = new RectOffset(4, 4, 4, 4);

            return indicatorRowGO.transform;
        }

        protected override void OnUpdateData()
        {
            if (Engine == null) return;

            // RCS Alarms
            UpdateAlarm(_pressHighIndicator, Engine.pressureHigh);
            UpdateAlarm(_pressLowIndicator, Engine.pressureLow);
            UpdateAlarm(_subcoolLowIndicator, Engine.subcoolingLow);
            UpdateAlarm(_flowLowIndicator, Engine.rcsFlowLow);

            // PZR Alarms
            UpdateAlarm(_pzrLevelHighIndicator, Engine.pzrLevelHigh);
            UpdateAlarm(_pzrLevelLowIndicator, Engine.pzrLevelLow);
            // Heater fail - no direct field, use heater power < expected when on
            bool heaterFail = Engine.pzrHeatersOn && Engine.pzrHeaterPower < 0.001f;
            UpdateAlarm(_heaterFailIndicator, heaterFail);
            UpdateAlarm(_sprayFailIndicator, Engine.sprayActive);

            // CVCS Alarms
            UpdateAlarm(_vctLevelHighIndicator, Engine.vctLevelHigh);
            UpdateAlarm(_vctLevelLowIndicator, Engine.vctLevelLow);
            UpdateAlarm(_makeupActiveIndicator, Engine.vctMakeupActive);
            UpdateAlarm(_divertActiveIndicator, Engine.vctDivertActive);

            // SG/RHR Alarms
            UpdateAlarm(_sgPressHighIndicator, Engine.sgSecondaryPressureHigh);
            // RHR flow low when active but flow is 0
            bool rhrFlowLow = Engine.rhrActive && Engine.rhrState.FlowRate_gpm < 100f;
            UpdateAlarm(_rhrFlowLowIndicator, rhrFlowLow);
            UpdateAlarm(_boilingIndicator, Engine.sgBoilingActive);

            // System Alarms
            bool massError = Mathf.Abs(Engine.massError_lbm) > 50f;
            UpdateAlarm(_massErrorIndicator, massError);
        }

        private void UpdateAlarm(StatusIndicator indicator, bool alarmed)
        {
            if (indicator == null) return;
            indicator.SetOn(alarmed);
            indicator.SetColor(alarmed ? ValidationDashboardTheme.AlarmRed : new Color(0.2f, 0.2f, 0.2f, 0.5f));
        }
    }
}
