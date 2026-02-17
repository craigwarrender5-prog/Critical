// ============================================================================
// CRITICAL: Master the Atom - CVCS Detail Panel
// CVCSPanel.cs - Chemical and Volume Control System Display
// ============================================================================
// TAB: 3 (CVCS)
// VERSION: 1.0.1
// IP: IP-0031 Stage 4
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Critical.Physics;

namespace Critical.UI.ValidationDashboard
{
    public class CVCSPanel : ValidationPanelBase
    {
        public override string PanelName => "CVCSPanel";
        public override int TabIndex => 3;

        // Hero gauges
        private ArcGauge _chargingHeroGauge;
        private ArcGauge _letdownHeroGauge;
        private BidirectionalGauge _netFlowHeroGauge;

        // Charging
        private StatusIndicator _chargingActiveIndicator;
        private DigitalReadout _chargingFlowReadout;
        private DigitalReadout _netFlowReadout;

        // Letdown
        private StatusIndicator _letdownActiveIndicator;
        private DigitalReadout _letdownFlowReadout;
        private StatusIndicator _letdownIsolatedIndicator;
        private DigitalReadout _rcsMassReadout;

        // VCT
        private DigitalReadout _vctLevelReadout;
        private StatusIndicator _makeupActiveIndicator;
        private StatusIndicator _divertActiveIndicator;

        // Boron
        private DigitalReadout _boronReadout;

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

            Transform chargingCol = CreateColumn(columnsGO.transform, "ChargingColumn", 1f);
            BuildChargingSection(chargingCol);

            Transform letdownCol = CreateColumn(columnsGO.transform, "LetdownColumn", 1f);
            BuildLetdownSection(letdownCol);

            Transform vctCol = CreateColumn(columnsGO.transform, "VCTColumn", 1f);
            BuildVCTSection(vctCol);

            Transform boronCol = CreateColumn(columnsGO.transform, "BoronColumn", 1f);
            BuildBoronSection(boronCol);
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
            heroLayout.spacing = 16;

            _chargingHeroGauge = ArcGauge.Create(heroGO.transform,
                "CHARGING", 0f, 150f, 0f, 120f, 0f, 140f, " gpm");
            _netFlowHeroGauge = BidirectionalGauge.Create(heroGO.transform,
                "NET FLOW", 75f, " gpm");
            _letdownHeroGauge = ArcGauge.Create(heroGO.transform,
                "LETDOWN", 0f, 150f, 0f, 120f, 0f, 140f, " gpm");
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

        private void BuildChargingSection(Transform parent)
        {
            CreateSectionHeader(parent, "CHARGING");
            _chargingActiveIndicator = StatusIndicator.Create(parent, "ACTIVE", StatusIndicator.IndicatorShape.Pill, 60f, 26f);
            _chargingFlowReadout = DigitalReadout.Create(parent, "FLOW", " gpm", "F1", 24f);
            _netFlowReadout = DigitalReadout.Create(parent, "NET FLOW", " gpm", "F1", 20f);
        }

        private void BuildLetdownSection(Transform parent)
        {
            CreateSectionHeader(parent, "LETDOWN");
            _letdownActiveIndicator = StatusIndicator.Create(parent, "ACTIVE", StatusIndicator.IndicatorShape.Pill, 60f, 26f);
            _letdownFlowReadout = DigitalReadout.Create(parent, "FLOW", " gpm", "F1", 24f);
            _letdownIsolatedIndicator = StatusIndicator.Create(parent, "ISOLATED", StatusIndicator.IndicatorShape.Pill, 70f, 26f);
            _rcsMassReadout = DigitalReadout.Create(parent, "RCS MASS", " lbm", "F0", 18f);
        }

        private void BuildVCTSection(Transform parent)
        {
            CreateSectionHeader(parent, "VCT");
            _vctLevelReadout = DigitalReadout.Create(parent, "LEVEL", "%", "F1", 24f);
            _makeupActiveIndicator = StatusIndicator.Create(parent, "MAKEUP", StatusIndicator.IndicatorShape.Pill, 70f, 26f);
            _divertActiveIndicator = StatusIndicator.Create(parent, "DIVERT", StatusIndicator.IndicatorShape.Pill, 65f, 26f);
        }

        private void BuildBoronSection(Transform parent)
        {
            CreateSectionHeader(parent, "BORON");
            _boronReadout = DigitalReadout.Create(parent, "CONC", " ppm", "F0", 24f);
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
            _chargingHeroGauge?.SetValue(Engine.chargingFlow);
            _letdownHeroGauge?.SetValue(Engine.letdownFlow);
            float heroNet = Engine.chargingFlow - Engine.letdownFlow;
            _netFlowHeroGauge?.SetValue(heroNet);

            // Charging
            _chargingActiveIndicator?.SetOn(Engine.chargingActive);
            _chargingFlowReadout?.SetValue(Engine.chargingFlow);
            
            // Net flow = charging - letdown
            float netFlow = Engine.chargingFlow - Engine.letdownFlow;
            _netFlowReadout?.SetValue(netFlow);
            if (_netFlowReadout != null)
            {
                if (netFlow > 10f) _netFlowReadout.SetColor(ValidationDashboardTheme.AccentBlue);
                else if (netFlow < -10f) _netFlowReadout.SetColor(ValidationDashboardTheme.AccentOrange);
                else _netFlowReadout.SetColor(ValidationDashboardTheme.TextPrimary);
            }

            // Letdown
            _letdownActiveIndicator?.SetOn(Engine.letdownActive);
            _letdownFlowReadout?.SetValue(Engine.letdownFlow);
            _letdownIsolatedIndicator?.SetOn(Engine.letdownIsolatedFlag);
            _letdownIsolatedIndicator?.SetColor(Engine.letdownIsolatedFlag ? 
                ValidationDashboardTheme.WarningAmber : ValidationDashboardTheme.TextSecondary);
            
            // RCS mass
            _rcsMassReadout?.SetValue(Engine.rcsWaterMass);

            // VCT
            _vctLevelReadout?.SetValue(Engine.vctState.Level);
            if (_vctLevelReadout != null)
            {
                if (Engine.vctLevelLow || Engine.vctLevelHigh)
                    _vctLevelReadout.SetColor(ValidationDashboardTheme.AlarmRed);
                else if (Engine.vctState.Level < 25f || Engine.vctState.Level > 80f)
                    _vctLevelReadout.SetColor(ValidationDashboardTheme.WarningAmber);
                else
                    _vctLevelReadout.SetColor(ValidationDashboardTheme.NormalGreen);
            }

            _makeupActiveIndicator?.SetOn(Engine.vctMakeupActive);
            _divertActiveIndicator?.SetOn(Engine.vctDivertActive);

            // Boron - use rcsBoronConcentration
            _boronReadout?.SetValue(Engine.rcsBoronConcentration);
        }
    }
}
