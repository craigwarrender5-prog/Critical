// ============================================================================
// CRITICAL: Master the Atom - Mini Trends Panel
// MiniTrendsPanel.cs - Always-Visible Trend Strip Panel
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Critical.Physics;

namespace Critical.UI.ValidationDashboard
{
    public class MiniTrendsPanel : ValidationPanelBase
    {
        public override string PanelName => "MiniTrendsPanel";
        public override int TabIndex => -1;
        public override bool AlwaysVisible => true;

        private MiniTrendStrip _rcsPressureTrend;
        private MiniTrendStrip _pzrLevelTrend;
        private MiniTrendStrip _tAvgTrend;
        private MiniTrendStrip _subcoolTrend;
        private MiniTrendStrip _chargingTrend;
        private MiniTrendStrip _letdownTrend;
        private MiniTrendStrip _sgPressureTrend;
        private MiniTrendStrip _netHeatTrend;

        private float _lastSampleTime;
        private const float SAMPLE_INTERVAL = 1f; // Sample every 1 second sim time

        protected override void OnInitialize()
        {
            BuildLayout();
        }

        private void BuildLayout()
        {
            VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 2;
            layout.padding = new RectOffset(4, 4, 4, 4);

            // Header
            GameObject headerGO = new GameObject("Header");
            headerGO.transform.SetParent(transform, false);
            LayoutElement headerLE = headerGO.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 18;

            TextMeshProUGUI headerText = headerGO.AddComponent<TextMeshProUGUI>();
            headerText.text = "TRENDS";
            headerText.fontSize = 11;
            headerText.fontStyle = FontStyles.Bold;
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.color = ValidationDashboardTheme.TextSecondary;

            // Create trend strips
            _rcsPressureTrend = MiniTrendStrip.Create(transform, "RCS Press", 0, 2500, ValidationDashboardTheme.Trace1, " psia", "F0");
            _pzrLevelTrend = MiniTrendStrip.Create(transform, "PZR Level", 0, 100, ValidationDashboardTheme.Trace4, "%", "F1");
            _tAvgTrend = MiniTrendStrip.Create(transform, "T-avg", 50, 600, ValidationDashboardTheme.Trace2, "°F", "F1");
            _subcoolTrend = MiniTrendStrip.Create(transform, "Subcool", 0, 200, ValidationDashboardTheme.Trace3, "°F", "F1");
            _chargingTrend = MiniTrendStrip.Create(transform, "Charging", 0, 150, ValidationDashboardTheme.AccentBlue, " gpm", "F1");
            _letdownTrend = MiniTrendStrip.Create(transform, "Letdown", 0, 150, ValidationDashboardTheme.AccentOrange, " gpm", "F1");
            _sgPressureTrend = MiniTrendStrip.Create(transform, "SG Press", 0, 1200, ValidationDashboardTheme.Trace6, " psia", "F0");
            _netHeatTrend = MiniTrendStrip.Create(transform, "Net Heat", -10, 10, ValidationDashboardTheme.Trace5, " MW", "F2");
        }

        protected override void OnUpdateData()
        {
            if (Engine == null) return;

            float simTime = Engine.simTime * 3600f; // Convert to seconds
            if (simTime - _lastSampleTime >= SAMPLE_INTERVAL)
            {
                _lastSampleTime = simTime;
                SampleData(simTime);
            }
        }

        private void SampleData(float time)
        {
            _rcsPressureTrend?.AddDataPoint(time, Engine.pressure);
            _pzrLevelTrend?.AddDataPoint(time, Engine.pzrLevel);
            _tAvgTrend?.AddDataPoint(time, Engine.T_avg);
            _subcoolTrend?.AddDataPoint(time, Engine.subcooling);
            _chargingTrend?.AddDataPoint(time, Engine.chargingFlow);
            _letdownTrend?.AddDataPoint(time, Engine.letdownFlow);
            _sgPressureTrend?.AddDataPoint(time, Engine.sgSecondaryPressure_psia);
            _netHeatTrend?.AddDataPoint(time, Engine.netPlantHeat_MW);
        }
    }
}
