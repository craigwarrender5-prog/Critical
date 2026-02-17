// ============================================================================
// CRITICAL: Master the Atom - Primary Loop Detail Panel
// PrimaryLoopPanel.cs - Detailed RCS and Loop Temperature Display
// ============================================================================
//
// TAB: 1 (PRIMARY)
// VERSION: 1.0.0
// DATE: 2026-02-17
// IP: IP-0031 Stage 4
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Critical.Physics;

namespace Critical.UI.ValidationDashboard
{
    public class PrimaryLoopPanel : ValidationPanelBase
    {
        public override string PanelName => "PrimaryLoopPanel";
        public override int TabIndex => 1;

        // Temperature section
        private ArcGauge _tAvgGauge;
        private ArcGauge _tHotGauge;
        private ArcGauge _tColdGauge;
        private DigitalReadout _tSatReadout;
        private DigitalReadout _subcoolReadout;

        // Pressure section
        private ArcGauge _pressureGauge;
        private DigitalReadout _pressureRateReadout;

        // RCP section
        private StatusIndicator[] _rcpIndicators = new StatusIndicator[4];
        private DigitalReadout _rcpHeatReadout;
        private DigitalReadout _surgeFlowReadout;

        protected override void OnInitialize()
        {
            BuildLayout();
        }

        private void BuildLayout()
        {
            GameObject columnsGO = new GameObject("Columns");
            columnsGO.transform.SetParent(transform, false);

            RectTransform columnsRT = columnsGO.AddComponent<RectTransform>();
            columnsRT.anchorMin = Vector2.zero;
            columnsRT.anchorMax = Vector2.one;
            columnsRT.offsetMin = Vector2.zero;
            columnsRT.offsetMax = Vector2.zero;

            HorizontalLayoutGroup columnsLayout = columnsGO.AddComponent<HorizontalLayoutGroup>();
            columnsLayout.childAlignment = TextAnchor.UpperLeft;
            columnsLayout.childControlWidth = true;
            columnsLayout.childControlHeight = true;
            columnsLayout.childForceExpandWidth = true;
            columnsLayout.childForceExpandHeight = true;
            columnsLayout.spacing = 12;
            columnsLayout.padding = new RectOffset(8, 8, 8, 8);

            Transform tempCol = CreateColumn(columnsGO.transform, "TemperatureColumn", 1.2f);
            BuildTemperatureSection(tempCol);

            Transform pressCol = CreateColumn(columnsGO.transform, "PressureColumn", 1f);
            BuildPressureSection(pressCol);

            Transform rcpCol = CreateColumn(columnsGO.transform, "RCPColumn", 1f);
            BuildRCPSection(rcpCol);
        }

        private Transform CreateColumn(Transform parent, string name, float flex)
        {
            GameObject colGO = new GameObject(name);
            colGO.transform.SetParent(parent, false);

            LayoutElement le = colGO.AddComponent<LayoutElement>();
            le.flexibleWidth = flex;

            Image bg = colGO.AddComponent<Image>();
            bg.color = ValidationDashboardTheme.BackgroundSection;

            VerticalLayoutGroup layout = colGO.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 8;
            layout.padding = new RectOffset(8, 8, 8, 8);

            return colGO.transform;
        }

        private void BuildTemperatureSection(Transform parent)
        {
            CreateSectionHeader(parent, "LOOP TEMPERATURES");
            _tAvgGauge = ArcGauge.Create(parent, "T-AVG", 50f, 600f, 100f, PlantConstants.MODE_3_TEMP_F, 80f, PlantConstants.MODE_3_TEMP_F + 20f, "°F");

            GameObject tempRow = new GameObject("TempRow");
            tempRow.transform.SetParent(parent, false);
            LayoutElement rowLE = tempRow.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 100;

            HorizontalLayoutGroup rowLayout = tempRow.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;
            rowLayout.spacing = 8;

            _tHotGauge = ArcGauge.Create(tempRow.transform, "T-HOT", 50f, 650f, 100f, 620f, 80f, 640f, "°F");
            _tColdGauge = ArcGauge.Create(tempRow.transform, "T-COLD", 50f, 600f, 100f, 560f, 80f, 580f, "°F");

            _tSatReadout = DigitalReadout.Create(parent, "T-SAT", "°F", "F1", 20f);
            _subcoolReadout = DigitalReadout.Create(parent, "SUBCOOLING", "°F", "F1", 28f);
        }

        private void BuildPressureSection(Transform parent)
        {
            CreateSectionHeader(parent, "RCS PRESSURE");
            _pressureGauge = ArcGauge.Create(parent, "PRESSURE", 0f, 2500f, 400f, PlantConstants.PZR_OPERATING_PRESSURE_PSIA + 50f, 350f, PlantConstants.PZR_OPERATING_PRESSURE_PSIA + 100f, " psia");
            _pressureRateReadout = DigitalReadout.Create(parent, "PRESSURE RATE", " psi/hr", "F1", 18f);
        }

        private void BuildRCPSection(Transform parent)
        {
            CreateSectionHeader(parent, "REACTOR COOLANT PUMPS");

            GameObject rcpRow = new GameObject("RCPRow");
            rcpRow.transform.SetParent(parent, false);
            LayoutElement rcpRowLE = rcpRow.AddComponent<LayoutElement>();
            rcpRowLE.preferredHeight = 32;

            HorizontalLayoutGroup rcpLayout = rcpRow.AddComponent<HorizontalLayoutGroup>();
            rcpLayout.childAlignment = TextAnchor.MiddleCenter;
            rcpLayout.childControlWidth = false;
            rcpLayout.childControlHeight = true;
            rcpLayout.childForceExpandWidth = false;
            rcpLayout.childForceExpandHeight = false;
            rcpLayout.spacing = 8;

            for (int i = 0; i < 4; i++)
                _rcpIndicators[i] = StatusIndicator.Create(rcpRow.transform, $"RCP-{i + 1}", StatusIndicator.IndicatorShape.Pill, 55f, 26f);

            _rcpHeatReadout = DigitalReadout.Create(parent, "RCP HEAT", " MW", "F2", 20f);

            CreateSectionHeader(parent, "SURGE LINE");
            _surgeFlowReadout = DigitalReadout.Create(parent, "SURGE FLOW", " gpm", "F1", 18f);
        }

        private void CreateSectionHeader(Transform parent, string title)
        {
            GameObject headerGO = new GameObject("Header_" + title);
            headerGO.transform.SetParent(parent, false);

            LayoutElement le = headerGO.AddComponent<LayoutElement>();
            le.preferredHeight = 24;
            le.minHeight = 24;

            TextMeshProUGUI text = headerGO.AddComponent<TextMeshProUGUI>();
            text.text = title;
            text.fontSize = 11;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = ValidationDashboardTheme.TextSecondary;
        }

        protected override void OnUpdateData()
        {
            if (Engine == null) return;

            _tAvgGauge?.SetValue(Engine.T_avg);
            _tHotGauge?.SetValue(Engine.T_hot);
            _tColdGauge?.SetValue(Engine.T_cold);
            _tSatReadout?.SetValue(Engine.T_sat);
            _subcoolReadout?.SetValue(Engine.subcooling);

            if (_subcoolReadout != null)
            {
                if (Engine.subcoolingLow) _subcoolReadout.SetColor(ValidationDashboardTheme.AlarmRed);
                else if (Engine.subcooling < 30f) _subcoolReadout.SetColor(ValidationDashboardTheme.WarningAmber);
                else _subcoolReadout.SetColor(ValidationDashboardTheme.NormalGreen);
            }

            _pressureGauge?.SetValue(Engine.pressure);
            _pressureRateReadout?.SetValue(Engine.pressureRate);

            if (_pressureRateReadout != null)
            {
                if (Mathf.Abs(Engine.pressureRate) > 100f) _pressureRateReadout.SetColor(ValidationDashboardTheme.AlarmRed);
                else if (Mathf.Abs(Engine.pressureRate) > 50f) _pressureRateReadout.SetColor(ValidationDashboardTheme.WarningAmber);
                else _pressureRateReadout.SetColor(ValidationDashboardTheme.TextPrimary);
            }

            for (int i = 0; i < 4; i++)
                if (_rcpIndicators[i] != null) _rcpIndicators[i].SetOn(Engine.rcpRunning[i]);

            _rcpHeatReadout?.SetValue(Engine.rcpHeat);
            _surgeFlowReadout?.SetValue(Engine.surgeFlow);

            if (_surgeFlowReadout != null)
            {
                if (Engine.surgeFlow > 10f) _surgeFlowReadout.SetColor(ValidationDashboardTheme.AccentBlue);
                else if (Engine.surgeFlow < -10f) _surgeFlowReadout.SetColor(ValidationDashboardTheme.AccentOrange);
                else _surgeFlowReadout.SetColor(ValidationDashboardTheme.TextPrimary);
            }
        }
    }
}
