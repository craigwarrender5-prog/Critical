// ============================================================================
// CRITICAL: Master the Atom - Pressurizer Detail Panel
// PressurizerPanel.cs - Detailed Pressurizer System Display
// ============================================================================
// TAB: 2 (PRESSURIZER)
// VERSION: 1.0.1
// IP: IP-0031 Stage 4
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Critical.Physics;

namespace Critical.UI.ValidationDashboard
{
    public class PressurizerPanel : ValidationPanelBase
    {
        public override string PanelName => "PressurizerPanel";
        public override int TabIndex => 2;

        // Hero gauges
        private ArcGauge _levelHeroGauge;
        private ArcGauge _pressureHeroGauge;

        // Level
        private DigitalReadout _levelReadout;
        private DigitalReadout _waterVolumeReadout;
        private DigitalReadout _steamVolumeReadout;

        // Pressure
        private DigitalReadout _pressureReadout;
        private DigitalReadout _tSatReadout;
        private DigitalReadout _tPzrReadout;

        // Heater Control
        private StatusIndicator _heaterOnIndicator;
        private DigitalReadout _heaterPowerReadout;
        private DigitalReadout _heaterModeReadout;

        // Spray/Relief
        private StatusIndicator _sprayActiveIndicator;
        private DigitalReadout _sprayFlowReadout;

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

            // Hero gauge row at top
            Transform heroRow = CreateHeroRow(columnsGO.transform);

            Transform levelCol = CreateColumn(columnsGO.transform, "LevelColumn", 1f);
            BuildLevelSection(levelCol);

            Transform pressCol = CreateColumn(columnsGO.transform, "PressureColumn", 1f);
            BuildPressureSection(pressCol);

            Transform controlCol = CreateColumn(columnsGO.transform, "ControlColumn", 1f);
            BuildControlSection(controlCol);
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

        private Transform CreateHeroRow(Transform parent)
        {
            // Swap the parent layout to vertical to stack hero row above columns
            // This inserts a row of arc gauges above the existing 3-column layout
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

            _levelHeroGauge = ArcGauge.Create(heroGO.transform,
                "PZR LEVEL", 0f, 100f, 17f, 80f, 12f, 92f, " %");
            _pressureHeroGauge = ArcGauge.Create(heroGO.transform,
                "PZR PRESS", 0f, 2500f, 350f, 2300f, 300f, 2385f, " psia");

            return heroGO.transform;
        }

        private void BuildLevelSection(Transform parent)
        {
            CreateSectionHeader(parent, "PZR LEVEL");
            _levelReadout = DigitalReadout.Create(parent, "LEVEL", "%", "F1", 28f);
            _waterVolumeReadout = DigitalReadout.Create(parent, "WATER SPACE", " ft³", "F1", 18f);
            _steamVolumeReadout = DigitalReadout.Create(parent, "STEAM SPACE", " ft³", "F1", 18f);
        }

        private void BuildPressureSection(Transform parent)
        {
            CreateSectionHeader(parent, "PZR PRESSURE");
            _pressureReadout = DigitalReadout.Create(parent, "PRESSURE", " psia", "F0", 28f);
            _tSatReadout = DigitalReadout.Create(parent, "T-SAT @ P", "°F", "F1", 20f);
            _tPzrReadout = DigitalReadout.Create(parent, "T-PZR", "°F", "F1", 20f);
        }

        private void BuildControlSection(Transform parent)
        {
            CreateSectionHeader(parent, "HEATER CONTROL");
            _heaterOnIndicator = StatusIndicator.Create(parent, "HEATERS", StatusIndicator.IndicatorShape.Pill, 70f, 28f);
            _heaterPowerReadout = DigitalReadout.Create(parent, "POWER", " MW", "F3", 22f);
            _heaterModeReadout = DigitalReadout.Create(parent, "MODE", "", "F0", 16f);

            CreateSectionHeader(parent, "SPRAY CONTROL");
            _sprayActiveIndicator = StatusIndicator.Create(parent, "SPRAY", StatusIndicator.IndicatorShape.Pill, 60f, 26f);
            _sprayFlowReadout = DigitalReadout.Create(parent, "FLOW", " gpm", "F1", 18f);
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
            _levelHeroGauge?.SetValue(Engine.pzrLevel);
            _pressureHeroGauge?.SetValue(Engine.pressure);

            // Level
            _levelReadout?.SetValue(Engine.pzrLevel);
            _waterVolumeReadout?.SetValue(Engine.pzrWaterVolume);
            _steamVolumeReadout?.SetValue(Engine.pzrSteamVolume);

            // Colorize level
            if (_levelReadout != null)
            {
                if (Engine.pzrLevelLow || Engine.pzrLevelHigh)
                    _levelReadout.SetColor(ValidationDashboardTheme.AlarmRed);
                else if (Engine.pzrLevel < 20f || Engine.pzrLevel > 85f)
                    _levelReadout.SetColor(ValidationDashboardTheme.WarningAmber);
                else
                    _levelReadout.SetColor(ValidationDashboardTheme.NormalGreen);
            }

            // Pressure
            _pressureReadout?.SetValue(Engine.pressure);
            _tSatReadout?.SetValue(Engine.T_sat);
            _tPzrReadout?.SetValue(Engine.T_pzr);

            // Heater control
            _heaterOnIndicator?.SetOn(Engine.pzrHeatersOn);
            _heaterOnIndicator?.SetColor(Engine.pzrHeatersOn ? 
                ValidationDashboardTheme.AccentOrange : ValidationDashboardTheme.TextSecondary);
            
            _heaterPowerReadout?.SetValue(Engine.pzrHeaterPower);
            
            // Heater mode - use heaterAuthorityState string
            if (_heaterModeReadout != null)
            {
                _heaterModeReadout.SetText(Engine.heaterAuthorityState);
            }

            // Spray control
            _sprayActiveIndicator?.SetOn(Engine.sprayActive);
            _sprayActiveIndicator?.SetColor(Engine.sprayActive ? 
                ValidationDashboardTheme.AccentBlue : ValidationDashboardTheme.TextSecondary);
            
            _sprayFlowReadout?.SetValue(Engine.sprayFlow_GPM);
        }
    }
}
