// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// ExtendedPOCController.cs — Full dashboard-style test with gauges + charts
// ============================================================================
//
// PURPOSE:
//   Test multiple gauges and strip charts together to validate:
//   1. Multiple arc gauges render correctly
//   2. Strip chart line rendering works
//   3. Performance is acceptable with multiple elements updating
//
// SETUP:
//   Replace ArcGaugePOCController with this on your UIDocument GameObject
//
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Critical.UI.POC
{
    [RequireComponent(typeof(UIDocument))]
    public class ExtendedPOCController : MonoBehaviour
    {
        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================
        
        [Header("Update Settings")]
        [Tooltip("Update rate in Hz (updates per second)")]
        [SerializeField] private float updateRate = 5f;  // 5 Hz = 200ms
        
        [Header("Simulation")]
        [SerializeField] private bool simulateData = true;
        
        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================
        
        private UIDocument _uiDocument;
        
        // Gauges
        private ArcGaugePOC _gaugeTemp;
        private ArcGaugePOC _gaugePressure;
        private ArcGaugePOC _gaugeLevel;
        private ArcGaugePOC _gaugeSubcool;
        
        // Charts
        private StripChartPOC _chartTemps;
        private StripChartPOC _chartPressure;
        
        // Labels
        private Label _labelTemp;
        private Label _labelPressure;
        private Label _labelLevel;
        private Label _labelSubcool;
        private Label _perfLabel;
        
        // Timing
        private float _updateInterval;
        private float _nextUpdate;
        private float _lastUpdateTime;
        private float _updateDuration;
        
        // Simulated values
        private float _simTime;
        private float _simTemp = 70f;
        private float _simPressure = 15f;
        private float _simLevel = 50f;
        private float _simSubcool = 100f;
        
        // ====================================================================
        // LIFECYCLE
        // ====================================================================
        
        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            _updateInterval = 1f / updateRate;
            _nextUpdate = 0f;
            
            BuildUI();
        }
        
        private void Update()
        {
            if (Time.time < _nextUpdate)
                return;
            
            _nextUpdate = Time.time + _updateInterval;
            float startTime = Time.realtimeSinceStartup;
            
            if (simulateData)
            {
                UpdateSimulation();
            }
            
            UpdateUI();
            
            _updateDuration = (Time.realtimeSinceStartup - startTime) * 1000f;  // ms
            
            if (_perfLabel != null)
            {
                _perfLabel.text = $"Update: {_updateDuration:F2}ms | Rate: {updateRate}Hz | Gauges: 4 | Charts: 2";
            }
        }
        
        // ====================================================================
        // SIMULATION
        // ====================================================================
        
        private void UpdateSimulation()
        {
            _simTime += _updateInterval;
            
            // Simulate heatup profile
            // Temperature: 70°F → 547°F over ~8 hours (simulate faster)
            _simTemp = 70f + (477f * Mathf.Clamp01(_simTime / 60f));  // Full range in 60 seconds for testing
            _simTemp += Mathf.Sin(_simTime * 2f) * 5f;  // Add some noise
            
            // Pressure: 15 psia → 2235 psia
            _simPressure = 15f + (2220f * Mathf.Clamp01(_simTime / 60f));
            _simPressure += Mathf.Sin(_simTime * 1.5f) * 20f;
            
            // Level: varies with pressure/temp
            _simLevel = 50f + Mathf.Sin(_simTime * 0.5f) * 30f;
            
            // Subcooling: decreases as we approach saturation
            _simSubcool = Mathf.Max(0f, 100f - (_simTime * 2f) + Mathf.Sin(_simTime) * 10f);
            
            // Wrap simulation
            if (_simTime > 65f)
            {
                _simTime = 0f;
            }
        }
        
        private void UpdateUI()
        {
            // Update gauges
            if (_gaugeTemp != null)
            {
                _gaugeTemp.value = _simTemp;
                _labelTemp.text = $"{_simTemp:F1} °F";
            }
            
            if (_gaugePressure != null)
            {
                _gaugePressure.value = _simPressure;
                _labelPressure.text = $"{_simPressure:F0} psia";
            }
            
            if (_gaugeLevel != null)
            {
                _gaugeLevel.value = _simLevel;
                _labelLevel.text = $"{_simLevel:F1} %";
            }
            
            if (_gaugeSubcool != null)
            {
                _gaugeSubcool.value = _simSubcool;
                _labelSubcool.text = $"{_simSubcool:F1} °F";
            }
            
            // Update charts
            if (_chartTemps != null)
            {
                _chartTemps.AddValue(0, _simTemp);
                _chartTemps.AddValue(1, _simSubcool * 5f);  // Scale for visibility
            }
            
            if (_chartPressure != null)
            {
                _chartPressure.AddValue(0, _simPressure);
                _chartPressure.AddValue(1, _simLevel * 22f);  // Scale for visibility
            }
        }
        
        // ====================================================================
        // UI CONSTRUCTION
        // ====================================================================
        
        private void BuildUI()
        {
            var root = _uiDocument.rootVisualElement;
            root.Clear();
            
            // Root styling
            root.style.backgroundColor = new Color(0.04f, 0.04f, 0.06f, 1f);
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft = 15;
            root.style.paddingRight = 15;
            
            // ================================================================
            // Header
            // ================================================================
            var header = CreateHeader();
            root.Add(header);
            
            // ================================================================
            // Gauges Row
            // ================================================================
            var gaugesSection = CreateGaugesSection();
            root.Add(gaugesSection);
            
            // ================================================================
            // Charts Row
            // ================================================================
            var chartsSection = CreateChartsSection();
            root.Add(chartsSection);
            
            // ================================================================
            // Performance Footer
            // ================================================================
            var footer = CreateFooter();
            root.Add(footer);
            
            Debug.Log("[ExtendedPOC] UI built with 4 gauges and 2 strip charts");
        }
        
        private VisualElement CreateHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.height = 35;
            header.style.backgroundColor = new Color(0.06f, 0.06f, 0.09f, 1f);
            header.style.marginBottom = 10;
            header.style.paddingLeft = 15;
            header.style.paddingRight = 15;
            header.style.borderBottomWidth = 2;
            header.style.borderBottomColor = new Color(0f, 1f, 0.533f, 1f);
            
            var modeLabel = new Label("MODE 5 Cold Shutdown");
            modeLabel.style.color = new Color(0f, 1f, 0.533f, 1f);
            modeLabel.style.fontSize = 14;
            modeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(modeLabel);
            
            var phaseLabel = new Label("EXTENDED POC — Testing Multiple Gauges + Charts");
            phaseLabel.style.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            phaseLabel.style.fontSize = 12;
            header.Add(phaseLabel);
            
            var timeLabel = new Label("UI Toolkit Validation");
            timeLabel.style.color = new Color(0.4f, 0.8f, 1f, 1f);
            timeLabel.style.fontSize = 12;
            header.Add(timeLabel);
            
            return header;
        }
        
        private VisualElement CreateGaugesSection()
        {
            var section = new VisualElement();
            section.style.marginBottom = 15;
            
            // Section title
            var title = new Label("ARC GAUGES — Simulated Engine Data");
            title.style.color = new Color(0f, 1f, 0.533f, 1f);
            title.style.fontSize = 13;
            title.style.marginBottom = 10;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            section.Add(title);
            
            // Gauges row
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceAround;
            row.style.backgroundColor = new Color(0.07f, 0.07f, 0.1f, 1f);
            row.style.paddingTop = 15;
            row.style.paddingBottom = 15;
            row.style.borderTopLeftRadius = 8;
            row.style.borderTopRightRadius = 8;
            row.style.borderBottomLeftRadius = 8;
            row.style.borderBottomRightRadius = 8;
            section.Add(row);
            
            // Create 4 gauges
            (_gaugeTemp, _labelTemp) = CreateGaugeWithLabel("T_AVG", 70f, 620f, "°F");
            (_gaugePressure, _labelPressure) = CreateGaugeWithLabel("PRESSURE", 0f, 2500f, "psia");
            (_gaugeLevel, _labelLevel) = CreateGaugeWithLabel("PZR LEVEL", 0f, 100f, "%");
            (_gaugeSubcool, _labelSubcool) = CreateGaugeWithLabel("SUBCOOL", 0f, 150f, "°F");
            
            row.Add(CreateGaugeContainer(_gaugeTemp, _labelTemp, "T_AVG"));
            row.Add(CreateGaugeContainer(_gaugePressure, _labelPressure, "PRESSURE"));
            row.Add(CreateGaugeContainer(_gaugeLevel, _labelLevel, "PZR LEVEL"));
            row.Add(CreateGaugeContainer(_gaugeSubcool, _labelSubcool, "SUBCOOL"));
            
            return section;
        }
        
        private (ArcGaugePOC gauge, Label valueLabel) CreateGaugeWithLabel(string name, float min, float max, string unit)
        {
            var gauge = new ArcGaugePOC();
            gauge.minValue = min;
            gauge.maxValue = max;
            gauge.value = min;
            gauge.label = name;
            
            var label = new Label($"--- {unit}");
            label.style.fontSize = 18;
            label.style.color = new Color(0f, 1f, 0.533f, 1f);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            
            return (gauge, label);
        }
        
        private VisualElement CreateGaugeContainer(ArcGaugePOC gauge, Label valueLabel, string title)
        {
            var container = new VisualElement();
            container.style.alignItems = Align.Center;
            container.style.width = 150;
            
            // Gauge
            var gaugeHolder = new VisualElement();
            gaugeHolder.style.width = 130;
            gaugeHolder.style.height = 110;
            gaugeHolder.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            gaugeHolder.style.borderTopLeftRadius = 6;
            gaugeHolder.style.borderTopRightRadius = 6;
            gaugeHolder.style.borderBottomLeftRadius = 6;
            gaugeHolder.style.borderBottomRightRadius = 6;
            gauge.style.flexGrow = 1;
            gaugeHolder.Add(gauge);
            container.Add(gaugeHolder);
            
            // Title
            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 11;
            titleLabel.style.color = new Color(0.5f, 0.5f, 0.6f, 1f);
            titleLabel.style.marginTop = 6;
            container.Add(titleLabel);
            
            // Value
            valueLabel.style.marginTop = 2;
            container.Add(valueLabel);
            
            return container;
        }
        
        private VisualElement CreateChartsSection()
        {
            var section = new VisualElement();
            section.style.flexGrow = 1;
            section.style.marginBottom = 15;
            
            // Section title
            var title = new Label("STRIP CHARTS — Rolling History");
            title.style.color = new Color(0f, 1f, 0.533f, 1f);
            title.style.fontSize = 13;
            title.style.marginBottom = 10;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            section.Add(title);
            
            // Charts row
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexGrow = 1;
            section.Add(row);
            
            // Temperature chart
            var tempChartContainer = CreateChartContainer("TEMPERATURES", out _chartTemps);
            _chartTemps.AddTrace("T_avg", new Color(0f, 1f, 0.533f, 1f), 70f, 620f);
            _chartTemps.AddTrace("Subcool×5", new Color(0.4f, 0.8f, 1f, 1f), 0f, 750f);
            row.Add(tempChartContainer);
            
            // Spacer
            var spacer = new VisualElement();
            spacer.style.width = 15;
            row.Add(spacer);
            
            // Pressure/Level chart
            var pressChartContainer = CreateChartContainer("PRESSURE / LEVEL", out _chartPressure);
            _chartPressure.AddTrace("Pressure", new Color(1f, 0.667f, 0f, 1f), 0f, 2500f);
            _chartPressure.AddTrace("Level×22", new Color(1f, 0.4f, 0.4f, 1f), 0f, 2500f);
            row.Add(pressChartContainer);
            
            return section;
        }
        
        private VisualElement CreateChartContainer(string title, out StripChartPOC chart)
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.backgroundColor = new Color(0.07f, 0.07f, 0.1f, 1f);
            container.style.borderTopLeftRadius = 8;
            container.style.borderTopRightRadius = 8;
            container.style.borderBottomLeftRadius = 8;
            container.style.borderBottomRightRadius = 8;
            container.style.paddingTop = 8;
            container.style.paddingBottom = 8;
            container.style.paddingLeft = 10;
            container.style.paddingRight = 10;
            
            // Title
            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 11;
            titleLabel.style.color = new Color(0.5f, 0.5f, 0.6f, 1f);
            titleLabel.style.marginBottom = 5;
            container.Add(titleLabel);
            
            // Chart
            chart = new StripChartPOC();
            chart.style.flexGrow = 1;
            chart.style.minHeight = 120;
            container.Add(chart);
            
            return container;
        }
        
        private VisualElement CreateFooter()
        {
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.SpaceBetween;
            footer.style.backgroundColor = new Color(0.07f, 0.07f, 0.1f, 1f);
            footer.style.paddingTop = 8;
            footer.style.paddingBottom = 8;
            footer.style.paddingLeft = 15;
            footer.style.paddingRight = 15;
            footer.style.borderTopLeftRadius = 6;
            footer.style.borderTopRightRadius = 6;
            footer.style.borderBottomLeftRadius = 6;
            footer.style.borderBottomRightRadius = 6;
            
            var titleLabel = new Label("PERFORMANCE");
            titleLabel.style.color = new Color(0f, 1f, 0.533f, 1f);
            titleLabel.style.fontSize = 12;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            footer.Add(titleLabel);
            
            _perfLabel = new Label("Initializing...");
            _perfLabel.style.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            _perfLabel.style.fontSize = 11;
            footer.Add(_perfLabel);
            
            return footer;
        }
    }
}
