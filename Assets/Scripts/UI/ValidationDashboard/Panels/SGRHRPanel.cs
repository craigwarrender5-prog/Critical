// ============================================================================
// CRITICAL: Master the Atom - SG/RHR Detail Panel
// SGRHRPanel.cs - Steam Generators and Residual Heat Removal
// ============================================================================
//
// TAB: 4 (SG / RHR)
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
    public class SGRHRPanel : ValidationPanelBase
    {
        public override string PanelName => "SGRHRPanel";
        public override int TabIndex => 4;

        // SG Primary section
        private DigitalReadout _sgPrimaryTempReadout;
        private DigitalReadout _sgTubeTempReadout;

        // SG Secondary section
        private ArcGauge _sgPressureGauge;
        private DigitalReadout _sgSecondaryTempReadout;
        private DigitalReadout _sgSatTempReadout;
        private StatusIndicator _boilingIndicator;

        // SG Heat Transfer section
        private ArcGauge _sgHeatGauge;
        private DigitalReadout _sgDeltaTReadout;
        private DigitalReadout _sgUAReadout;

        // RHR section
        private StatusIndicator _rhrActiveIndicator;
        private DigitalReadout _rhrModeReadout;
        private ArcGauge _rhrFlowGauge;
        private DigitalReadout _rhrHeatRemovalReadout;
        private DigitalReadout _rhrInletTempReadout;
        private DigitalReadout _rhrOutletTempReadout;

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

            Transform sgPriCol = CreateColumn(columnsGO.transform, "SGPrimaryColumn", 1f);
            BuildSGPrimarySection(sgPriCol);

            Transform sgSecCol = CreateColumn(columnsGO.transform, "SGSecondaryColumn", 1f);
            BuildSGSecondarySection(sgSecCol);

            Transform rhrCol = CreateColumn(columnsGO.transform, "RHRColumn", 1.2f);
            BuildRHRSection(rhrCol);
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

        private void BuildSGPrimarySection(Transform parent)
        {
            CreateSectionHeader(parent, "SG PRIMARY SIDE");
            _sgPrimaryTempReadout = DigitalReadout.Create(parent, "PRIMARY TEMP", "°F", "F1", 24f);
            _sgTubeTempReadout = DigitalReadout.Create(parent, "TUBE WALL", "°F", "F1", 18f);

            CreateSectionHeader(parent, "HEAT TRANSFER");
            _sgHeatGauge = ArcGauge.Create(parent, "SG HEAT", -50f, 50f, -30f, 30f, -45f, 45f, " MW");
            _sgDeltaTReadout = DigitalReadout.Create(parent, "ΔT (Pri-Sec)", "°F", "F1", 18f);
            _sgUAReadout = DigitalReadout.Create(parent, "UA COEFFICIENT", " BTU/hr-°F", "F0", 14f);
        }

        private void BuildSGSecondarySection(Transform parent)
        {
            CreateSectionHeader(parent, "SG SECONDARY SIDE");
            _sgPressureGauge = ArcGauge.Create(parent, "SG PRESSURE", 0f, 1200f, 100f, 1000f, 50f, 1100f, " psia");
            _sgSecondaryTempReadout = DigitalReadout.Create(parent, "SECONDARY TEMP", "°F", "F1", 20f);
            _sgSatTempReadout = DigitalReadout.Create(parent, "T-SAT @ P", "°F", "F1", 16f);

            CreateSectionHeader(parent, "BOILING STATUS");
            _boilingIndicator = StatusIndicator.Create(parent, "BOILING", StatusIndicator.IndicatorShape.Pill, 100f, 32f);
        }

        private void BuildRHRSection(Transform parent)
        {
            CreateSectionHeader(parent, "RESIDUAL HEAT REMOVAL");
            _rhrActiveIndicator = StatusIndicator.Create(parent, "RHR SYSTEM", StatusIndicator.IndicatorShape.Pill, 120f, 32f);
            _rhrModeReadout = DigitalReadout.Create(parent, "MODE", "", "F0", 16f);
            _rhrFlowGauge = ArcGauge.Create(parent, "RHR FLOW", 0f, 5000f, 1000f, 4500f, 500f, 4800f, " gpm");

            CreateSectionHeader(parent, "RHR TEMPERATURES");
            _rhrInletTempReadout = DigitalReadout.Create(parent, "INLET TEMP", "°F", "F1", 18f);
            _rhrOutletTempReadout = DigitalReadout.Create(parent, "OUTLET TEMP", "°F", "F1", 18f);
            _rhrHeatRemovalReadout = DigitalReadout.Create(parent, "HEAT REMOVAL", " MW", "F2", 20f);
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

            // SG Primary
            _sgPrimaryTempReadout?.SetValue(Engine.T_avg);
            _sgTubeTempReadout?.SetValue(Engine.sgTubeWallTemp);

            // SG Secondary
            _sgPressureGauge?.SetValue(Engine.sgSecondaryPressure_psia);
            _sgSecondaryTempReadout?.SetValue(Engine.sgSecondaryTemp);
            _sgSatTempReadout?.SetValue(Engine.sgSatTemp);

            // Boiling status
            bool boiling = Engine.sgBoilingActive;
            _boilingIndicator?.SetState(boiling, false);
            if (_boilingIndicator != null)
            {
                if (boiling)
                    _boilingIndicator.SetColors(ValidationDashboardTheme.Neutral, ValidationDashboardTheme.WarningAmber, 
                        ValidationDashboardTheme.WarningAmber, ValidationDashboardTheme.AlarmRed);
            }

            // Heat transfer
            _sgHeatGauge?.SetValue(Engine.sgHeatTransfer_MW);
            float deltaT = Engine.T_avg - Engine.sgSecondaryTemp;
            _sgDeltaTReadout?.SetValue(deltaT);
            _sgUAReadout?.SetValue(Engine.sgUA);

            if (_sgHeatGauge != null)
            {
                if (Engine.sgHeatTransfer_MW > 0.5f)
                    _sgDeltaTReadout?.SetColor(ValidationDashboardTheme.InfoCyan);
                else if (Engine.sgHeatTransfer_MW < -0.5f)
                    _sgDeltaTReadout?.SetColor(ValidationDashboardTheme.AccentOrange);
                else
                    _sgDeltaTReadout?.SetColor(ValidationDashboardTheme.TextPrimary);
            }

            // RHR
            _rhrActiveIndicator?.SetOn(Engine.rhrActive);
            _rhrModeReadout?.SetValue(0); // Mode as number
            if (_rhrModeReadout != null)
            {
                // Override with text
                var textComp = _rhrModeReadout.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (textComp != null) textComp.text = Engine.rhrModeString;
            }

            _rhrFlowGauge?.SetValue(Engine.rhrFlow);
            _rhrInletTempReadout?.SetValue(Engine.rhrInletTemp);
            _rhrOutletTempReadout?.SetValue(Engine.rhrOutletTemp);
            _rhrHeatRemovalReadout?.SetValue(Engine.rhrHeatRemoval_MW);

            if (_rhrHeatRemovalReadout != null && Engine.rhrActive)
                _rhrHeatRemovalReadout.SetColor(ValidationDashboardTheme.InfoCyan);
        }
    }
}
