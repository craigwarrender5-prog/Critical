// ============================================================================
// CRITICAL: Master the Atom - Validation Metrics Panel
// ValidationPanel.cs - Physics Validation and Conservation Metrics
// ============================================================================
//
// TAB: 6 (VALIDATION)
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
    public class ValidationPanel : ValidationPanelBase
    {
        public override string PanelName => "ValidationPanel";
        public override int TabIndex => 6;

        // Mass Conservation
        private DigitalReadout _massErrorReadout;
        private DigitalReadout _massErrorRateReadout;
        private LinearGauge _massErrorGauge;
        private StatusIndicator _massStatusIndicator;

        // Energy Conservation
        private DigitalReadout _netHeatReadout;
        private DigitalReadout _heatInputReadout;
        private DigitalReadout _heatRemovalReadout;
        private LinearGauge _energyBalanceGauge;

        // Thermal Balance
        private DigitalReadout _rcpHeatReadout;
        private DigitalReadout _pzrHeaterReadout;
        private DigitalReadout _sgHeatReadout;
        private DigitalReadout _rhrHeatReadout;

        // Timestep Metrics
        private DigitalReadout _dtReadout;
        private DigitalReadout _simSpeedReadout;
        private DigitalReadout _iterationsReadout;

        // Validation Status
        private StatusIndicator _thermalStabilityIndicator;
        private StatusIndicator _pressureStabilityIndicator;
        private StatusIndicator _levelStabilityIndicator;

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

            Transform massCol = CreateColumn(columnsGO.transform, "MassColumn", 1f);
            BuildMassSection(massCol);

            Transform energyCol = CreateColumn(columnsGO.transform, "EnergyColumn", 1f);
            BuildEnergySection(energyCol);

            Transform thermalCol = CreateColumn(columnsGO.transform, "ThermalColumn", 1f);
            BuildThermalSection(thermalCol);

            Transform statusCol = CreateColumn(columnsGO.transform, "StatusColumn", 0.8f);
            BuildStatusSection(statusCol);
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

        private void BuildMassSection(Transform parent)
        {
            CreateSectionHeader(parent, "MASS CONSERVATION");
            _massErrorReadout = DigitalReadout.Create(parent, "MASS ERROR", " lbm", "F3", 24f);
            _massErrorRateReadout = DigitalReadout.Create(parent, "ERROR RATE", " lbm/hr", "F3", 16f);
            _massErrorGauge = LinearGauge.Create(parent, "ERROR BAND", -100f, 100f, false, -10f, 10f, -50f, 50f);
            _massStatusIndicator = StatusIndicator.Create(parent, "MASS OK", StatusIndicator.IndicatorShape.Pill, 100f, 32f);
        }

        private void BuildEnergySection(Transform parent)
        {
            CreateSectionHeader(parent, "ENERGY BALANCE");
            _netHeatReadout = DigitalReadout.Create(parent, "NET HEAT", " MW", "F3", 24f);
            _heatInputReadout = DigitalReadout.Create(parent, "HEAT INPUT", " MW", "F3", 16f);
            _heatRemovalReadout = DigitalReadout.Create(parent, "HEAT REMOVAL", " MW", "F3", 16f);
            _energyBalanceGauge = LinearGauge.Create(parent, "BALANCE", -10f, 10f, false, -2f, 2f, -5f, 5f);
        }

        private void BuildThermalSection(Transform parent)
        {
            CreateSectionHeader(parent, "HEAT SOURCES / SINKS");
            _rcpHeatReadout = DigitalReadout.Create(parent, "RCP HEAT", " MW", "F3", 18f);
            _pzrHeaterReadout = DigitalReadout.Create(parent, "PZR HEATERS", " MW", "F3", 18f);
            _sgHeatReadout = DigitalReadout.Create(parent, "SG TRANSFER", " MW", "F3", 18f);
            _rhrHeatReadout = DigitalReadout.Create(parent, "RHR REMOVAL", " MW", "F3", 18f);

            CreateSectionHeader(parent, "TIMESTEP");
            _dtReadout = DigitalReadout.Create(parent, "DT", " sec", "F4", 16f);
            _simSpeedReadout = DigitalReadout.Create(parent, "SIM SPEED", "x", "F1", 16f);
            _iterationsReadout = DigitalReadout.Create(parent, "ITERATIONS", "", "F0", 16f);
        }

        private void BuildStatusSection(Transform parent)
        {
            CreateSectionHeader(parent, "STABILITY STATUS");
            _thermalStabilityIndicator = StatusIndicator.Create(parent, "THERMAL", StatusIndicator.IndicatorShape.Pill, 100f, 28f);
            _pressureStabilityIndicator = StatusIndicator.Create(parent, "PRESSURE", StatusIndicator.IndicatorShape.Pill, 100f, 28f);
            _levelStabilityIndicator = StatusIndicator.Create(parent, "LEVEL", StatusIndicator.IndicatorShape.Pill, 100f, 28f);
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

            // Mass Conservation
            _massErrorReadout?.SetValue(Engine.massError_lbm);
            _massErrorRateReadout?.SetValue(Engine.massErrorRate_lbmPerHour);
            _massErrorGauge?.SetValue(Engine.massError_lbm);
            _massStatusIndicator?.SetState(Engine.primaryMassConservationOK && !Engine.primaryMassAlarm, Engine.primaryMassAlarm);

            if (_massErrorReadout != null)
            {
                float absError = Mathf.Abs(Engine.massError_lbm);
                if (absError > 50f) _massErrorReadout.SetColor(ValidationDashboardTheme.AlarmRed);
                else if (absError > 10f) _massErrorReadout.SetColor(ValidationDashboardTheme.WarningAmber);
                else _massErrorReadout.SetColor(ValidationDashboardTheme.NormalGreen);
            }

            // Energy Balance
            _netHeatReadout?.SetValue(Engine.netPlantHeat_MW);
            _heatInputReadout?.SetValue(Engine.totalHeatInput_MW);
            _heatRemovalReadout?.SetValue(Engine.totalHeatRemoval_MW);
            _energyBalanceGauge?.SetValue(Engine.netPlantHeat_MW);

            if (_netHeatReadout != null)
            {
                float absNet = Mathf.Abs(Engine.netPlantHeat_MW);
                if (absNet > 5f) _netHeatReadout.SetColor(ValidationDashboardTheme.AlarmRed);
                else if (absNet > 2f) _netHeatReadout.SetColor(ValidationDashboardTheme.WarningAmber);
                else _netHeatReadout.SetColor(ValidationDashboardTheme.NormalGreen);
            }

            // Thermal Sources
            _rcpHeatReadout?.SetValue(Engine.rcpHeat);
            _pzrHeaterReadout?.SetValue(Engine.pzrHeaterPower);
            _sgHeatReadout?.SetValue(Engine.sgHeatTransfer_MW);
            _rhrHeatReadout?.SetValue(Engine.rhrHeatRemoval_MW);

            // Timestep
            _dtReadout?.SetValue(Engine.dt);
            _simSpeedReadout?.SetValue(Engine.effectiveTimeScale);
            _iterationsReadout?.SetValue(Engine.iterationCount);

            // Stability indicators
            bool thermalStable = Mathf.Abs(Engine.temperatureRate) < 5f; // <5°F/hr
            bool pressureStable = Mathf.Abs(Engine.pressureRate) < 20f;   // <20 psi/hr
            bool levelStable = Mathf.Abs(Engine.pzrLevelRate) < 1f;       // <1%/hr

            _thermalStabilityIndicator?.SetOn(thermalStable);
            _pressureStabilityIndicator?.SetOn(pressureStable);
            _levelStabilityIndicator?.SetOn(levelStable);
        }
    }
}
