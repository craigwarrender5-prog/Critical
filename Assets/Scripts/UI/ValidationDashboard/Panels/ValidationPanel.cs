// ============================================================================
// CRITICAL: Master the Atom - Validation Panel
// ValidationPanel.cs - Mass/Energy Conservation and Simulation Metrics
// ============================================================================
// TAB: 6 (VALIDATION)
// VERSION: 1.0.1
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
        private StatusIndicator _massStatusIndicator;

        // Energy Balance
        private DigitalReadout _netHeatReadout;
        private DigitalReadout _heatInputReadout;
        private DigitalReadout _heatRemovalReadout;

        // Thermal Sources
        private DigitalReadout _rcpHeatReadout;
        private DigitalReadout _pzrHeatReadout;
        private DigitalReadout _sgTransferReadout;
        private DigitalReadout _rhrRemovalReadout;

        // Stability
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

            Transform statusCol = CreateColumn(columnsGO.transform, "StatusColumn", 1f);
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
            _massErrorReadout = DigitalReadout.Create(parent, "MASS ERROR", " lbm", "F2", 24f);
            _massStatusIndicator = StatusIndicator.Create(parent, "STATUS", StatusIndicator.IndicatorShape.Pill, 80f, 28f);
        }

        private void BuildEnergySection(Transform parent)
        {
            CreateSectionHeader(parent, "ENERGY BALANCE");
            _netHeatReadout = DigitalReadout.Create(parent, "NET HEAT", " MW", "F2", 24f);
            _heatInputReadout = DigitalReadout.Create(parent, "HEAT INPUT", " MW", "F2", 18f);
            _heatRemovalReadout = DigitalReadout.Create(parent, "HEAT REMOVAL", " MW", "F2", 18f);
        }

        private void BuildThermalSection(Transform parent)
        {
            CreateSectionHeader(parent, "THERMAL SOURCES");
            _rcpHeatReadout = DigitalReadout.Create(parent, "RCP HEAT", " MW", "F2", 18f);
            _pzrHeatReadout = DigitalReadout.Create(parent, "PZR HEATERS", " MW", "F3", 18f);
            _sgTransferReadout = DigitalReadout.Create(parent, "SG TRANSFER", " MW", "F2", 18f);
            _rhrRemovalReadout = DigitalReadout.Create(parent, "RHR REMOVAL", " MW", "F2", 18f);
        }

        private void BuildStatusSection(Transform parent)
        {
            CreateSectionHeader(parent, "STABILITY STATUS");
            _thermalStabilityIndicator = StatusIndicator.Create(parent, "THERMAL", StatusIndicator.IndicatorShape.Pill, 80f, 26f);
            _pressureStabilityIndicator = StatusIndicator.Create(parent, "PRESSURE", StatusIndicator.IndicatorShape.Pill, 80f, 26f);
            _levelStabilityIndicator = StatusIndicator.Create(parent, "LEVEL", StatusIndicator.IndicatorShape.Pill, 80f, 26f);
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

            // Mass conservation - use massError_lbm field
            float massErr = Engine.massError_lbm;
            _massErrorReadout?.SetValue(massErr);
            
            if (_massStatusIndicator != null)
            {
                bool massOK = Mathf.Abs(massErr) < 10f;
                _massStatusIndicator.SetOn(massOK);
                _massStatusIndicator.SetColor(massOK ? ValidationDashboardTheme.NormalGreen : 
                    Mathf.Abs(massErr) < 50f ? ValidationDashboardTheme.WarningAmber : ValidationDashboardTheme.AlarmRed);
            }

            // Energy balance - use netPlantHeat_MW
            _netHeatReadout?.SetValue(Engine.netPlantHeat_MW);
            
            // Heat input = RCP + PZR heaters + RHR pump heat
            float heatInput = Engine.rcpHeat + Engine.pzrHeaterPower + Mathf.Max(0f, Engine.rhrPumpHeat_MW);
            _heatInputReadout?.SetValue(heatInput);
            
            // Heat removal = SG + RHR HX + insulation losses
            float heatRemoval = Engine.sgHeatTransfer_MW + Engine.rhrHXRemoval_MW;
            _heatRemovalReadout?.SetValue(heatRemoval);

            // Thermal sources
            _rcpHeatReadout?.SetValue(Engine.rcpHeat);
            _pzrHeatReadout?.SetValue(Engine.pzrHeaterPower);
            _sgTransferReadout?.SetValue(Engine.sgHeatTransfer_MW);
            _rhrRemovalReadout?.SetValue(Engine.rhrHXRemoval_MW);

            // Stability indicators - use heatupRate and pressureRate
            bool thermalStable = Mathf.Abs(Engine.heatupRate) < 5f;
            _thermalStabilityIndicator?.SetOn(thermalStable);
            _thermalStabilityIndicator?.SetColor(thermalStable ? ValidationDashboardTheme.NormalGreen : ValidationDashboardTheme.WarningAmber);

            bool pressureStable = Mathf.Abs(Engine.pressureRate) < 20f;
            _pressureStabilityIndicator?.SetOn(pressureStable);
            _pressureStabilityIndicator?.SetColor(pressureStable ? ValidationDashboardTheme.NormalGreen : ValidationDashboardTheme.WarningAmber);

            // Level stability - approximate from surge flow
            bool levelStable = Mathf.Abs(Engine.surgeFlow) < 50f;
            _levelStabilityIndicator?.SetOn(levelStable);
            _levelStabilityIndicator?.SetColor(levelStable ? ValidationDashboardTheme.NormalGreen : ValidationDashboardTheme.WarningAmber);
        }
    }
}
