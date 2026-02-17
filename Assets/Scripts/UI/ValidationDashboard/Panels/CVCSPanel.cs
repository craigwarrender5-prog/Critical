// ============================================================================
// CRITICAL: Master the Atom - CVCS Detail Panel
// CVCSPanel.cs - Charging, Letdown, VCT, Boric Acid Control
// ============================================================================
//
// TAB: 3 (CVCS)
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
    public class CVCSPanel : ValidationPanelBase
    {
        public override string PanelName => "CVCSPanel";
        public override int TabIndex => 3;

        // Charging section
        private StatusIndicator _chargingPumpIndicator;
        private ArcGauge _chargingFlowGauge;
        private DigitalReadout _chargingTempReadout;

        // Letdown section
        private StatusIndicator _letdownIndicator;
        private ArcGauge _letdownFlowGauge;
        private DigitalReadout _letdownTempReadout;
        private StatusIndicator _letdownIsolatedIndicator;

        // Net flow section
        private BidirectionalGauge _netFlowGauge;
        private DigitalReadout _netFlowReadout;
        private DigitalReadout _rcsInventoryReadout;

        // VCT section
        private ArcGauge _vctLevelGauge;
        private DigitalReadout _vctLevelRateReadout;
        private StatusIndicator _vctAutoMakeupIndicator;
        private StatusIndicator _vctDivertIndicator;

        // Boron section
        private DigitalReadout _boronConcReadout;
        private DigitalReadout _boronRateReadout;

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

            Transform chgCol = CreateColumn(columnsGO.transform, "ChargingColumn", 1f);
            BuildChargingSection(chgCol);

            Transform ltdCol = CreateColumn(columnsGO.transform, "LetdownColumn", 1f);
            BuildLetdownSection(ltdCol);

            Transform vctCol = CreateColumn(columnsGO.transform, "VCTColumn", 1f);
            BuildVCTSection(vctCol);

            Transform boronCol = CreateColumn(columnsGO.transform, "BoronColumn", 0.8f);
            BuildBoronSection(boronCol);
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
            CreateSectionHeader(parent, "CHARGING SYSTEM");
            _chargingPumpIndicator = StatusIndicator.Create(parent, "CHG PUMP", StatusIndicator.IndicatorShape.Pill, 100f, 28f);
            _chargingFlowGauge = ArcGauge.Create(parent, "CHARGING FLOW", 0f, 150f, 40f, 120f, 20f, 140f, " gpm");
            _chargingTempReadout = DigitalReadout.Create(parent, "CHG TEMP", "°F", "F1", 16f);

            CreateSectionHeader(parent, "NET FLOW");
            _netFlowGauge = BidirectionalGauge.Create(parent, "NET CVCS", 100f, " gpm");
            _netFlowReadout = DigitalReadout.Create(parent, "NET", " gpm", "F1", 18f);
        }

        private void BuildLetdownSection(Transform parent)
        {
            CreateSectionHeader(parent, "LETDOWN SYSTEM");
            _letdownIndicator = StatusIndicator.Create(parent, "LETDOWN", StatusIndicator.IndicatorShape.Pill, 100f, 28f);
            _letdownFlowGauge = ArcGauge.Create(parent, "LETDOWN FLOW", 0f, 150f, 40f, 120f, 20f, 140f, " gpm");
            _letdownTempReadout = DigitalReadout.Create(parent, "LTD TEMP", "°F", "F1", 16f);
            _letdownIsolatedIndicator = StatusIndicator.Create(parent, "ISOLATED", StatusIndicator.IndicatorShape.Pill, 100f, 28f);

            CreateSectionHeader(parent, "RCS INVENTORY");
            _rcsInventoryReadout = DigitalReadout.Create(parent, "RCS MASS", " lbm", "F0", 16f);
        }

        private void BuildVCTSection(Transform parent)
        {
            CreateSectionHeader(parent, "VOLUME CONTROL TANK");
            _vctLevelGauge = ArcGauge.Create(parent, "VCT LEVEL", 0f, 100f, 20f, 80f, 11f, 95f, "%");
            _vctLevelRateReadout = DigitalReadout.Create(parent, "VCT RATE", " %/hr", "F2", 16f);

            CreateSectionHeader(parent, "VCT CONTROLS");
            _vctAutoMakeupIndicator = StatusIndicator.Create(parent, "AUTO MAKEUP", StatusIndicator.IndicatorShape.Pill, 110f, 26f);
            _vctDivertIndicator = StatusIndicator.Create(parent, "DIVERT", StatusIndicator.IndicatorShape.Pill, 110f, 26f);
        }

        private void BuildBoronSection(Transform parent)
        {
            CreateSectionHeader(parent, "BORON CONTROL");
            _boronConcReadout = DigitalReadout.Create(parent, "BORON CONC", " ppm", "F0", 20f);
            _boronRateReadout = DigitalReadout.Create(parent, "BORON RATE", " ppm/hr", "F2", 16f);
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

            // Charging
            _chargingPumpIndicator?.SetOn(Engine.chargingActive);
            _chargingFlowGauge?.SetValue(Engine.chargingFlow);
            _chargingTempReadout?.SetValue(Engine.chargingTemp);

            // Letdown
            _letdownIndicator?.SetOn(Engine.letdownActive);
            _letdownFlowGauge?.SetValue(Engine.letdownFlow);
            _letdownTempReadout?.SetValue(Engine.letdownTemp);
            _letdownIsolatedIndicator?.SetState(Engine.letdownIsolatedFlag, Engine.letdownIsolatedFlag);

            // Net flow
            float netFlow = Engine.chargingFlow - Engine.letdownFlow;
            _netFlowGauge?.SetValue(netFlow);
            _netFlowReadout?.SetValue(netFlow);

            if (_netFlowReadout != null)
            {
                if (netFlow > 5f) _netFlowReadout.SetColor(ValidationDashboardTheme.AccentBlue);
                else if (netFlow < -5f) _netFlowReadout.SetColor(ValidationDashboardTheme.AccentOrange);
                else _netFlowReadout.SetColor(ValidationDashboardTheme.NormalGreen);
            }

            // RCS inventory
            _rcsInventoryReadout?.SetValue(Engine.rcsMass);

            // VCT
            float vctLevel = Engine.vctState.Level_percent;
            _vctLevelGauge?.SetValue(vctLevel);
            _vctLevelRateReadout?.SetValue(Engine.vctState.LevelRate_pctPerHour);
            _vctAutoMakeupIndicator?.SetOn(Engine.vctAutoMakeupActive);
            _vctDivertIndicator?.SetOn(Engine.vctDivertActive);

            // Boron
            _boronConcReadout?.SetValue(Engine.boronConcentration);
            _boronRateReadout?.SetValue(Engine.boronRate);
        }
    }
}
