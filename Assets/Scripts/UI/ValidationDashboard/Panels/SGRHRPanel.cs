// ============================================================================
// CRITICAL: Master the Atom - SG/RHR Detail Panel
// SGRHRPanel.cs - Steam Generator and RHR System Display
// ============================================================================
// TAB: 4 (SG / RHR)
// VERSION: 1.0.1
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

        // Hero gauges
        private ArcGauge _sgPressHeroGauge;
        private ArcGauge _heatTransferHeroGauge;

        // SG Primary
        private DigitalReadout _sgPrimaryTempReadout;
        private DigitalReadout _sgHeatTransferReadout;
        private DigitalReadout _sgDeltaTReadout;

        // SG Secondary
        private DigitalReadout _sgSecPressureReadout;
        private DigitalReadout _sgSecTempReadout;
        private DigitalReadout _sgSatTempReadout;
        private StatusIndicator _sgBoilingIndicator;

        // RHR
        private StatusIndicator _rhrActiveIndicator;
        private DigitalReadout _rhrModeReadout;
        private DigitalReadout _rhrHeatReadout;

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

            // Hero gauge row
            BuildHeroRow(columnsGO.transform);

            Transform sgPriCol = CreateColumn(columnsGO.transform, "SGPrimaryColumn", 1f);
            BuildSGPrimarySection(sgPriCol);

            Transform sgSecCol = CreateColumn(columnsGO.transform, "SGSecondaryColumn", 1f);
            BuildSGSecondarySection(sgSecCol);

            Transform rhrCol = CreateColumn(columnsGO.transform, "RHRColumn", 1f);
            BuildRHRSection(rhrCol);
        }

        private void BuildHeroRow(Transform parent)
        {
            GameObject heroGO = new GameObject("HeroGauges");
            heroGO.transform.SetParent(parent.parent, false);
            heroGO.transform.SetAsFirstSibling();

            LayoutElement heroLE = heroGO.AddComponent<LayoutElement>();
            heroLE.preferredHeight = 150;
            heroLE.flexibleWidth = 1;

            HorizontalLayoutGroup heroLayout = heroGO.AddComponent<HorizontalLayoutGroup>();
            heroLayout.childAlignment = TextAnchor.MiddleCenter;
            heroLayout.childControlWidth = false;
            heroLayout.childControlHeight = false;
            heroLayout.spacing = 20;

            _sgPressHeroGauge = ArcGauge.Create(heroGO.transform,
                "SG PRESS", 0f, 1200f, 0f, 1000f, 0f, 1100f, " psia");
            _heatTransferHeroGauge = ArcGauge.Create(heroGO.transform,
                "SG HEAT", -5f, 20f, -2f, 15f, -3f, 18f, " MW");
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
            _sgPrimaryTempReadout = DigitalReadout.Create(parent, "PRIMARY TEMP", "°F", "F1", 22f);
            _sgHeatTransferReadout = DigitalReadout.Create(parent, "HEAT TRANSFER", " MW", "F2", 24f);
            _sgDeltaTReadout = DigitalReadout.Create(parent, "ΔT (Pri-Sec)", "°F", "F1", 20f);
        }

        private void BuildSGSecondarySection(Transform parent)
        {
            CreateSectionHeader(parent, "SG SECONDARY SIDE");
            _sgSecPressureReadout = DigitalReadout.Create(parent, "SEC PRESSURE", " psia", "F0", 22f);
            _sgSecTempReadout = DigitalReadout.Create(parent, "SEC TEMP", "°F", "F1", 20f);
            _sgSatTempReadout = DigitalReadout.Create(parent, "T-SAT @ P", "°F", "F1", 18f);
            _sgBoilingIndicator = StatusIndicator.Create(parent, "BOILING", StatusIndicator.IndicatorShape.Pill, 70f, 26f);
        }

        private void BuildRHRSection(Transform parent)
        {
            CreateSectionHeader(parent, "RHR SYSTEM");
            _rhrActiveIndicator = StatusIndicator.Create(parent, "RHR ACTIVE", StatusIndicator.IndicatorShape.Pill, 80f, 28f);
            _rhrModeReadout = DigitalReadout.Create(parent, "MODE", "", "F0", 18f);
            _rhrHeatReadout = DigitalReadout.Create(parent, "NET HEAT", " MW", "F2", 22f);
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

            // Hero gauges
            _sgPressHeroGauge?.SetValue(Engine.sgSecondaryPressure_psia);
            _heatTransferHeroGauge?.SetValue(Engine.sgHeatTransfer_MW);

            // SG Primary - use T_rcs as primary temp
            _sgPrimaryTempReadout?.SetValue(Engine.T_rcs);
            _sgHeatTransferReadout?.SetValue(Engine.sgHeatTransfer_MW);
            
            // Delta T between primary and secondary
            float deltaT = Engine.T_rcs - Engine.T_sg_secondary;
            _sgDeltaTReadout?.SetValue(deltaT);

            // SG Secondary - use actual field names
            _sgSecPressureReadout?.SetValue(Engine.sgSecondaryPressure_psia);
            _sgSecTempReadout?.SetValue(Engine.T_sg_secondary);
            _sgSatTempReadout?.SetValue(Engine.sgSaturationTemp_F);
            
            _sgBoilingIndicator?.SetOn(Engine.sgBoilingActive);
            if (_sgBoilingIndicator != null)
            {
                _sgBoilingIndicator.SetColor(Engine.sgBoilingActive ? 
                    ValidationDashboardTheme.WarningAmber : ValidationDashboardTheme.TextSecondary);
            }

            // RHR - use actual field names
            _rhrActiveIndicator?.SetOn(Engine.rhrActive);
            _rhrActiveIndicator?.SetColor(Engine.rhrActive ? 
                ValidationDashboardTheme.NormalGreen : ValidationDashboardTheme.TextSecondary);

            // Mode string
            if (_rhrModeReadout != null)
            {
                _rhrModeReadout.SetText(Engine.rhrModeString);
            }

            // Net RHR heat effect
            _rhrHeatReadout?.SetValue(Engine.rhrNetHeat_MW);
            if (_rhrHeatReadout != null)
            {
                if (Engine.rhrNetHeat_MW > 0.5f) _rhrHeatReadout.SetColor(ValidationDashboardTheme.AccentOrange);
                else if (Engine.rhrNetHeat_MW < -0.5f) _rhrHeatReadout.SetColor(ValidationDashboardTheme.AccentBlue);
                else _rhrHeatReadout.SetColor(ValidationDashboardTheme.TextPrimary);
            }
        }
    }
}
