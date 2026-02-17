// ============================================================================
// CRITICAL: Master the Atom - Pressurizer Detail Panel
// PressurizerPanel.cs - PZR Level, Pressure, Heaters, Spray, Relief
// ============================================================================
//
// TAB: 2 (PRESSURIZER)
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
    public class PressurizerPanel : ValidationPanelBase
    {
        public override string PanelName => "PressurizerPanel";
        public override int TabIndex => 2;

        // Level section
        private ArcGauge _levelGauge;
        private DigitalReadout _levelRateReadout;
        private LinearGauge _steamSpaceGauge;
        private LinearGauge _waterSpaceGauge;

        // Pressure section
        private ArcGauge _pressureGauge;
        private DigitalReadout _tSatReadout;
        private DigitalReadout _tPzrReadout;

        // Heater section
        private StatusIndicator _heaterMasterIndicator;
        private LinearGauge _heaterPowerGauge;
        private DigitalReadout _heaterGroupsReadout;
        private DigitalReadout _heaterOutputReadout;

        // Spray section
        private StatusIndicator _sprayIndicator;
        private DigitalReadout _sprayFlowReadout;
        private DigitalReadout _sprayDeltaTReadout;

        // Relief section
        private StatusIndicator _porvIndicator;
        private StatusIndicator _safetyIndicator;

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

            Transform levelCol = CreateColumn(columnsGO.transform, "LevelColumn", 1f);
            BuildLevelSection(levelCol);

            Transform pressCol = CreateColumn(columnsGO.transform, "PressureColumn", 1f);
            BuildPressureSection(pressCol);

            Transform controlCol = CreateColumn(columnsGO.transform, "ControlColumn", 1.2f);
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

        private void BuildLevelSection(Transform parent)
        {
            CreateSectionHeader(parent, "PRESSURIZER LEVEL");
            _levelGauge = ArcGauge.Create(parent, "LEVEL", 0f, 100f, 17f, 80f, 12f, 92f, "%");
            _levelRateReadout = DigitalReadout.Create(parent, "LEVEL RATE", " %/hr", "F2", 16f);

            CreateSectionHeader(parent, "VOLUME DISTRIBUTION");
            _steamSpaceGauge = LinearGauge.Create(parent, "STEAM SPACE", 0f, 100f, false, 20f, 80f, 10f, 90f);
            _waterSpaceGauge = LinearGauge.Create(parent, "WATER SPACE", 0f, 100f, false, 20f, 80f, 10f, 90f);
        }

        private void BuildPressureSection(Transform parent)
        {
            CreateSectionHeader(parent, "PZR PRESSURE");
            _pressureGauge = ArcGauge.Create(parent, "PRESSURE", 0f, 2500f, 400f, 2285f, 350f, 2385f, " psia");
            _tSatReadout = DigitalReadout.Create(parent, "T-SAT @ P", "°F", "F1", 18f);
            _tPzrReadout = DigitalReadout.Create(parent, "T-PZR", "°F", "F1", 18f);
        }

        private void BuildControlSection(Transform parent)
        {
            CreateSectionHeader(parent, "HEATER CONTROL");
            _heaterMasterIndicator = StatusIndicator.Create(parent, "HEATERS", StatusIndicator.IndicatorShape.Pill, 100f, 28f);
            _heaterPowerGauge = LinearGauge.Create(parent, "HEATER POWER", 0f, 100f, false);
            _heaterGroupsReadout = DigitalReadout.Create(parent, "GROUPS ON", "", "F0", 16f);
            _heaterOutputReadout = DigitalReadout.Create(parent, "OUTPUT", " kW", "F0", 16f);

            CreateSectionHeader(parent, "SPRAY CONTROL");
            _sprayIndicator = StatusIndicator.Create(parent, "SPRAY", StatusIndicator.IndicatorShape.Pill, 100f, 28f);
            _sprayFlowReadout = DigitalReadout.Create(parent, "SPRAY FLOW", " gpm", "F1", 16f);
            _sprayDeltaTReadout = DigitalReadout.Create(parent, "SPRAY ΔT", "°F", "F1", 16f);

            CreateSectionHeader(parent, "RELIEF VALVES");
            GameObject reliefRow = new GameObject("ReliefRow");
            reliefRow.transform.SetParent(parent, false);
            LayoutElement reliefLE = reliefRow.AddComponent<LayoutElement>();
            reliefLE.preferredHeight = 32;

            HorizontalLayoutGroup reliefLayout = reliefRow.AddComponent<HorizontalLayoutGroup>();
            reliefLayout.childAlignment = TextAnchor.MiddleCenter;
            reliefLayout.childControlWidth = false;
            reliefLayout.childControlHeight = true;
            reliefLayout.spacing = 16;

            _porvIndicator = StatusIndicator.Create(reliefRow.transform, "PORV", StatusIndicator.IndicatorShape.Pill, 70f, 26f);
            _safetyIndicator = StatusIndicator.Create(reliefRow.transform, "SAFETY", StatusIndicator.IndicatorShape.Pill, 70f, 26f);
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

            // Level
            _levelGauge?.SetValue(Engine.pzrLevel);
            _levelRateReadout?.SetValue(Engine.pzrLevelRate * 60f); // Convert to %/hr

            float steamPct = 100f - Engine.pzrLevel;
            _steamSpaceGauge?.SetValue(steamPct);
            _waterSpaceGauge?.SetValue(Engine.pzrLevel);

            // Pressure
            _pressureGauge?.SetValue(Engine.pressure);
            _tSatReadout?.SetValue(Engine.T_sat);
            _tPzrReadout?.SetValue(Engine.T_pzr);

            // Heaters
            bool heatersOn = Engine.pzrHeatersOn;
            _heaterMasterIndicator?.SetOn(heatersOn);
            
            float heaterPct = (Engine.pzrHeaterPower / PlantConstants.HEATER_POWER_TOTAL) * 100f * 1000f;
            _heaterPowerGauge?.SetValue(heaterPct);
            _heaterGroupsReadout?.SetValue(Engine.pzrHeaterGroupsOn);
            _heaterOutputReadout?.SetValue(Engine.pzrHeaterPower * 1000f);

            // Spray
            bool sprayActive = Engine.sprayActive;
            _sprayIndicator?.SetOn(sprayActive);
            _sprayFlowReadout?.SetValue(Engine.sprayFlow_GPM);
            
            float sprayDeltaT = Engine.T_pzr - Engine.T_cold;
            _sprayDeltaTReadout?.SetValue(sprayDeltaT);

            if (_sprayFlowReadout != null && sprayActive)
                _sprayFlowReadout.SetColor(ValidationDashboardTheme.InfoCyan);

            // Relief valves
            _porvIndicator?.SetState(Engine.porvOpen, Engine.porvOpen);
            _safetyIndicator?.SetState(Engine.safetyOpen, Engine.safetyOpen);
        }
    }
}
