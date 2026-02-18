// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// ControlShowcaseController.cs — Demonstrates all control types
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Critical.UI.POC
{
    [RequireComponent(typeof(UIDocument))]
    public class ControlShowcaseController : MonoBehaviour
    {
        // ====================================================================
        // INSPECTOR
        // ====================================================================
        
        [Header("Animation")]
        [SerializeField] private bool animate = true;
        [SerializeField] private float updateRate = 10f;
        
        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================
        
        private UIDocument _uiDocument;
        private float _time;
        private float _nextUpdate;
        
        // Controls
        private List<ArcGaugePOC> _arcGauges = new List<ArcGaugePOC>();
        private List<LinearGaugePOC> _linearGauges = new List<LinearGaugePOC>();
        private List<TankLevelPOC> _tankLevels = new List<TankLevelPOC>();
        private List<BidirectionalGaugePOC> _biGauges = new List<BidirectionalGaugePOC>();
        private List<StatusLEDPOC> _leds = new List<StatusLEDPOC>();
        private List<AnnunciatorTilePOC> _annunciators = new List<AnnunciatorTilePOC>();
        private StripChartPOC _chart;
        private PressurizerVesselPOC _pressurizer;
        
        // Labels
        private Label _perfLabel;
        
        // ====================================================================
        // LIFECYCLE
        // ====================================================================
        
        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            BuildUI();
        }
        
        private void Update()
        {
            if (!animate) return;
            
            if (Time.time < _nextUpdate) return;
            _nextUpdate = Time.time + 1f / updateRate;
            
            _time += 1f / updateRate;
            
            UpdateControls();
        }
        
        // ====================================================================
        // ANIMATION
        // ====================================================================
        
        private void UpdateControls()
        {
            float sin1 = Mathf.Sin(_time * 0.5f);
            float sin2 = Mathf.Sin(_time * 0.7f);
            float sin3 = Mathf.Sin(_time * 0.3f);
            
            // Arc gauges - simulate heatup
            float temp = 200f + sin1 * 100f + _time * 5f;
            temp = Mathf.Clamp(temp, 70f, 547f);
            
            if (_arcGauges.Count > 0) _arcGauges[0].value = temp;
            if (_arcGauges.Count > 1) _arcGauges[1].value = 400f + sin2 * 300f;
            
            // Linear gauges
            if (_linearGauges.Count > 0) _linearGauges[0].value = 50f + sin1 * 40f;
            if (_linearGauges.Count > 1) _linearGauges[1].value = 30f + sin2 * 60f;
            if (_linearGauges.Count > 2) _linearGauges[2].value = 60f + sin3 * 35f;
            
            // Tank levels
            if (_tankLevels.Count > 0) _tankLevels[0].value = 50f + sin1 * 30f;
            if (_tankLevels.Count > 1) _tankLevels[1].value = 65f + sin2 * 25f;
            
            // Bidirectional gauges
            if (_biGauges.Count > 0) _biGauges[0].value = sin1 * 80f;
            if (_biGauges.Count > 1) _biGauges[1].value = sin2 * 50f;
            
            // LEDs - cycle through states
            int ledState = ((int)(_time * 2f)) % 4;
            for (int i = 0; i < _leds.Count; i++)
            {
                _leds[i].state = (LEDState)(((int)(_time * (1f + i * 0.3f))) % 4);
            }
            
            // Annunciators - cycle through states
            for (int i = 0; i < _annunciators.Count; i++)
            {
                int stateIndex = ((int)(_time * 0.5f) + i) % 4;
                _annunciators[i].state = (AnnunciatorState)stateIndex;
            }
            
            // Chart
            if (_chart != null)
            {
                _chart.AddValue(0, 50f + sin1 * 30f);
                _chart.AddValue(1, 50f + sin2 * 25f);
                _chart.AddValue(2, 50f + sin3 * 20f);
            }
            
            // Pressurizer
            if (_pressurizer != null)
            {
                _pressurizer.level = 50f + sin1 * 25f;
                _pressurizer.heaterPower = 50f + sin2 * 50f;
                _pressurizer.sprayActive = sin3 > 0.5f;
                _pressurizer.showBubbleZone = _time % 10f < 5f;  // Toggle bubble zone
                
                // Charging/Letdown flows - simulate level control
                // When level low, charging > letdown; when level high, letdown > charging
                float levelError = _pressurizer.levelSetpoint - _pressurizer.level;
                _pressurizer.chargingFlow = levelError > 0 ? Mathf.Abs(levelError) * 2f : 5f;
                _pressurizer.letdownFlow = levelError < 0 ? Mathf.Abs(levelError) * 2f : 5f;
                
                // Update flow animation
                _pressurizer.UpdateFlowAnimation(1f / updateRate);
            }
            
            // Perf label
            if (_perfLabel != null)
            {
                _perfLabel.text = $"Time: {_time:F1}s | Update Rate: {updateRate}Hz";
            }
        }
        
        // ====================================================================
        // UI CONSTRUCTION
        // ====================================================================
        
        private void BuildUI()
        {
            var root = _uiDocument.rootVisualElement;
            root.Clear();
            
            _arcGauges.Clear();
            _linearGauges.Clear();
            _tankLevels.Clear();
            _biGauges.Clear();
            _leds.Clear();
            _annunciators.Clear();
            
            // Root styling
            root.style.backgroundColor = new Color(0.04f, 0.04f, 0.06f, 1f);
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft = 15;
            root.style.paddingRight = 15;
            
            // Header
            var header = CreateHeader();
            root.Add(header);
            
            // Main content - scroll view
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            root.Add(scrollView);
            
            // Section 1: Pressurizer Vessel (NEW)
            scrollView.Add(CreateSection("PRESSURIZER VESSEL (Two-Zone)", CreatePressurizerContent()));
            
            // Section 2: Arc Gauges
            scrollView.Add(CreateSection("ARC GAUGES", CreateArcGaugesContent()));
            
            // Section 3: Linear Gauges
            scrollView.Add(CreateSection("LINEAR GAUGES (Horizontal & Vertical)", CreateLinearGaugesContent()));
            
            // Section 4: Tank Levels
            scrollView.Add(CreateSection("TANK LEVEL INDICATORS", CreateTankLevelsContent()));
            
            // Section 5: Bidirectional Gauges
            scrollView.Add(CreateSection("BIDIRECTIONAL GAUGES (Center-Zero)", CreateBidirectionalContent()));
            
            // Section 6: Status LEDs
            scrollView.Add(CreateSection("STATUS LEDs", CreateLEDsContent()));
            
            // Section 7: Annunciator Tiles
            scrollView.Add(CreateSection("ANNUNCIATOR TILES (ISA-18.1 Style)", CreateAnnunciatorsContent()));
            
            // Section 8: Strip Chart
            scrollView.Add(CreateSection("STRIP CHART (Multi-Trace)", CreateChartContent()));
            
            // Footer
            var footer = CreateFooter();
            root.Add(footer);
            
            Debug.Log("[ControlShowcase] UI built with all control types");
        }
        
        private VisualElement CreateHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.height = 40;
            header.style.backgroundColor = new Color(0.06f, 0.06f, 0.09f, 1f);
            header.style.marginBottom = 15;
            header.style.paddingLeft = 15;
            header.style.paddingRight = 15;
            header.style.borderBottomWidth = 2;
            header.style.borderBottomColor = new Color(0f, 1f, 0.533f, 1f);
            
            var title = new Label("UI TOOLKIT CONTROL SHOWCASE");
            title.style.color = new Color(0f, 1f, 0.533f, 1f);
            title.style.fontSize = 16;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);
            
            var subtitle = new Label("All Control Types for Validation Dashboard");
            subtitle.style.color = new Color(0.5f, 0.5f, 0.6f, 1f);
            subtitle.style.fontSize = 12;
            header.Add(subtitle);
            
            return header;
        }
        
        private VisualElement CreateSection(string title, VisualElement content)
        {
            var section = new VisualElement();
            section.style.marginBottom = 20;
            
            var titleLabel = new Label(title);
            titleLabel.style.color = new Color(0f, 1f, 0.533f, 1f);
            titleLabel.style.fontSize = 12;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 8;
            section.Add(titleLabel);
            
            var contentBox = new VisualElement();
            contentBox.style.backgroundColor = new Color(0.07f, 0.07f, 0.1f, 1f);
            contentBox.style.borderTopLeftRadius = 6;
            contentBox.style.borderTopRightRadius = 6;
            contentBox.style.borderBottomLeftRadius = 6;
            contentBox.style.borderBottomRightRadius = 6;
            contentBox.style.paddingTop = 15;
            contentBox.style.paddingBottom = 15;
            contentBox.style.paddingLeft = 15;
            contentBox.style.paddingRight = 15;
            contentBox.Add(content);
            section.Add(contentBox);
            
            return section;
        }
        
        private VisualElement CreatePressurizerContent()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            
            // Pressurizer vessel
            var vesselContainer = new VisualElement();
            vesselContainer.style.width = 140;
            vesselContainer.style.height = 220;
            vesselContainer.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            vesselContainer.style.borderTopLeftRadius = 6;
            vesselContainer.style.borderTopRightRadius = 6;
            vesselContainer.style.borderBottomLeftRadius = 6;
            vesselContainer.style.borderBottomRightRadius = 6;
            vesselContainer.style.marginRight = 20;
            
            _pressurizer = new PressurizerVesselPOC();
            _pressurizer.style.flexGrow = 1;
            _pressurizer.level = 60f;
            _pressurizer.levelSetpoint = 65f;
            _pressurizer.heaterPower = 30f;
            vesselContainer.Add(_pressurizer);
            container.Add(vesselContainer);
            
            // Info panel
            var infoPanel = new VisualElement();
            infoPanel.style.flexGrow = 1;
            infoPanel.style.justifyContent = Justify.Center;
            
            var title = new Label("PRESSURIZER");
            title.style.fontSize = 14;
            title.style.color = new Color(0f, 1f, 0.533f, 1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 10;
            infoPanel.Add(title);
            
            var features = new string[]
            {
                "• Water region (blue) with level",
                "• Steam region (gray) above water",
                "• Bubble zone during formation",
                "• Heater elements with glow effect",
                "• Spray droplets when active",
                "• Charging flow (cyan arrows IN)",
                "• Letdown flow (orange arrows OUT)",
                "• Level setpoint marker (green)",
                "• Surge line connection"
            };
            
            foreach (var feature in features)
            {
                var label = new Label(feature);
                label.style.fontSize = 11;
                label.style.color = new Color(0.6f, 0.6f, 0.7f, 1f);
                label.style.marginBottom = 3;
                infoPanel.Add(label);
            }
            
            container.Add(infoPanel);
            
            return container;
        }
        
        private VisualElement CreateArcGaugesContent()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceAround;
            
            // Two arc gauges
            row.Add(CreateLabeledArcGauge("T_AVG", 70f, 620f, "°F", out var gauge1));
            row.Add(CreateLabeledArcGauge("PRESSURE", 0f, 2500f, "psia", out var gauge2));
            
            _arcGauges.Add(gauge1);
            _arcGauges.Add(gauge2);
            
            return row;
        }
        
        private VisualElement CreateLabeledArcGauge(string label, float min, float max, string unit, out ArcGaugePOC gauge)
        {
            var container = new VisualElement();
            container.style.alignItems = Align.Center;
            container.style.width = 150;
            
            var holder = new VisualElement();
            holder.style.width = 120;
            holder.style.height = 100;
            holder.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            holder.style.borderTopLeftRadius = 6;
            holder.style.borderTopRightRadius = 6;
            holder.style.borderBottomLeftRadius = 6;
            holder.style.borderBottomRightRadius = 6;
            
            gauge = new ArcGaugePOC();
            gauge.style.flexGrow = 1;
            gauge.minValue = min;
            gauge.maxValue = max;
            gauge.value = min;
            holder.Add(gauge);
            container.Add(holder);
            
            var labelEl = new Label(label);
            labelEl.style.fontSize = 10;
            labelEl.style.color = new Color(0.5f, 0.5f, 0.6f, 1f);
            labelEl.style.marginTop = 5;
            container.Add(labelEl);
            
            return container;
        }
        
        private VisualElement CreateLinearGaugesContent()
        {
            var container = new VisualElement();
            
            // Horizontal gauges
            var hRow = new VisualElement();
            hRow.style.flexDirection = FlexDirection.Row;
            hRow.style.marginBottom = 15;
            
            for (int i = 0; i < 2; i++)
            {
                var gaugeContainer = new VisualElement();
                gaugeContainer.style.flexGrow = 1;
                gaugeContainer.style.marginRight = i < 1 ? 15 : 0;
                
                var label = new Label(i == 0 ? "HEATER POWER" : "RHR FLOW");
                label.style.fontSize = 10;
                label.style.color = new Color(0.5f, 0.5f, 0.6f, 1f);
                label.style.marginBottom = 4;
                gaugeContainer.Add(label);
                
                var gauge = new LinearGaugePOC();
                gauge.style.height = 24;
                gauge.orientation = LinearGaugeOrientation.Horizontal;
                gauge.minValue = 0;
                gauge.maxValue = 100;
                gauge.warningThreshold = 70;
                gauge.alarmThreshold = 90;
                gaugeContainer.Add(gauge);
                
                _linearGauges.Add(gauge);
                hRow.Add(gaugeContainer);
            }
            container.Add(hRow);
            
            // Vertical gauge
            var vRow = new VisualElement();
            vRow.style.flexDirection = FlexDirection.Row;
            vRow.style.alignItems = Align.FlexEnd;
            
            var vLabel = new Label("VERTICAL:");
            vLabel.style.fontSize = 10;
            vLabel.style.color = new Color(0.5f, 0.5f, 0.6f, 1f);
            vLabel.style.marginRight = 10;
            vRow.Add(vLabel);
            
            var vGauge = new LinearGaugePOC();
            vGauge.style.width = 30;
            vGauge.style.height = 80;
            vGauge.orientation = LinearGaugeOrientation.Vertical;
            vGauge.minValue = 0;
            vGauge.maxValue = 100;
            _linearGauges.Add(vGauge);
            vRow.Add(vGauge);
            
            container.Add(vRow);
            
            return container;
        }
        
        private VisualElement CreateTankLevelsContent()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceAround;
            
            string[] labels = { "PZR LEVEL", "VCT LEVEL" };
            
            for (int i = 0; i < 2; i++)
            {
                var container = new VisualElement();
                container.style.alignItems = Align.Center;
                
                var tank = new TankLevelPOC();
                tank.style.width = 50;
                tank.style.height = 100;
                tank.minValue = 0;
                tank.maxValue = 100;
                tank.lowAlarm = 25;
                tank.highAlarm = 75;
                _tankLevels.Add(tank);
                container.Add(tank);
                
                var label = new Label(labels[i]);
                label.style.fontSize = 10;
                label.style.color = new Color(0.5f, 0.5f, 0.6f, 1f);
                label.style.marginTop = 5;
                container.Add(label);
                
                row.Add(container);
            }
            
            return row;
        }
        
        private VisualElement CreateBidirectionalContent()
        {
            var container = new VisualElement();
            
            string[] labels = { "HEAT BALANCE (MW)", "FLOW DIFFERENTIAL (gpm)" };
            
            for (int i = 0; i < 2; i++)
            {
                var row = new VisualElement();
                row.style.marginBottom = i < 1 ? 10 : 0;
                
                var label = new Label(labels[i]);
                label.style.fontSize = 10;
                label.style.color = new Color(0.5f, 0.5f, 0.6f, 1f);
                label.style.marginBottom = 4;
                row.Add(label);
                
                var gauge = new BidirectionalGaugePOC();
                gauge.style.height = 28;
                gauge.minValue = -100;
                gauge.maxValue = 100;
                _biGauges.Add(gauge);
                row.Add(gauge);
                
                container.Add(row);
            }
            
            return container;
        }
        
        private VisualElement CreateLEDsContent()
        {
            var container = new VisualElement();
            
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceAround;
            
            string[] labels = { "RCP 1", "RCP 2", "RCP 3", "RCP 4", "RHR A", "RHR B" };
            
            for (int i = 0; i < labels.Length; i++)
            {
                var item = new VisualElement();
                item.style.alignItems = Align.Center;
                
                var led = new StatusLEDPOC();
                led.style.width = 20;
                led.style.height = 20;
                led.pulsing = (i >= 4);  // RHR pumps pulse
                _leds.Add(led);
                item.Add(led);
                
                var label = new Label(labels[i]);
                label.style.fontSize = 9;
                label.style.color = new Color(0.5f, 0.5f, 0.6f, 1f);
                label.style.marginTop = 4;
                item.Add(label);
                
                row.Add(item);
            }
            
            container.Add(row);
            
            // Legend
            var legend = new VisualElement();
            legend.style.flexDirection = FlexDirection.Row;
            legend.style.justifyContent = Justify.Center;
            legend.style.marginTop = 10;
            
            string[] legendItems = { "Off", "Normal", "Warning", "Alarm" };
            Color[] legendColors = { 
                new Color(0.15f, 0.15f, 0.18f, 1f),
                new Color(0f, 1f, 0.533f, 1f),
                new Color(1f, 0.667f, 0f, 1f),
                new Color(1f, 0.267f, 0.267f, 1f)
            };
            
            for (int i = 0; i < legendItems.Length; i++)
            {
                var item = new VisualElement();
                item.style.flexDirection = FlexDirection.Row;
                item.style.alignItems = Align.Center;
                item.style.marginRight = 15;
                
                var dot = new VisualElement();
                dot.style.width = 10;
                dot.style.height = 10;
                dot.style.borderTopLeftRadius = 5;
                dot.style.borderTopRightRadius = 5;
                dot.style.borderBottomLeftRadius = 5;
                dot.style.borderBottomRightRadius = 5;
                dot.style.backgroundColor = legendColors[i];
                dot.style.marginRight = 4;
                item.Add(dot);
                
                var label = new Label(legendItems[i]);
                label.style.fontSize = 9;
                label.style.color = new Color(0.5f, 0.5f, 0.6f, 1f);
                item.Add(label);
                
                legend.Add(item);
            }
            
            container.Add(legend);
            
            return container;
        }
        
        private VisualElement CreateAnnunciatorsContent()
        {
            var container = new VisualElement();
            
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceAround;
            
            string[] titles = { "HIGH PRESS", "LOW LEVEL", "PUMP TRIP", "TEMP HIGH" };
            string[] descs = { "PZR > 2235", "VCT < 20%", "RCP 1A", "T_AVG > 550" };
            bool[] isWarning = { false, true, false, true };
            
            for (int i = 0; i < titles.Length; i++)
            {
                var tileContainer = new VisualElement();
                tileContainer.style.alignItems = Align.Center;
                
                var tile = new AnnunciatorTilePOC();
                tile.style.width = 100;
                tile.style.height = 60;
                tile.title = titles[i];
                tile.description = descs[i];
                tile.isWarning = isWarning[i];
                _annunciators.Add(tile);
                tileContainer.Add(tile);
                
                // Add text labels (since Painter2D can't draw text)
                var titleLabel = new Label(titles[i]);
                titleLabel.style.fontSize = 10;
                titleLabel.style.color = Color.white;
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLabel.style.position = Position.Absolute;
                titleLabel.style.top = 8;
                titleLabel.style.left = 0;
                titleLabel.style.right = 0;
                titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                tile.Add(titleLabel);
                
                var descLabel = new Label(descs[i]);
                descLabel.style.fontSize = 9;
                descLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                descLabel.style.position = Position.Absolute;
                descLabel.style.bottom = 8;
                descLabel.style.left = 0;
                descLabel.style.right = 0;
                descLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                tile.Add(descLabel);
                
                row.Add(tileContainer);
            }
            
            container.Add(row);
            
            // State legend
            var legend = new Label("States cycle: Normal → Alerting (fast flash) → Acknowledged (steady) → Clearing (slow flash)");
            legend.style.fontSize = 9;
            legend.style.color = new Color(0.4f, 0.4f, 0.5f, 1f);
            legend.style.marginTop = 10;
            legend.style.unityTextAlign = TextAnchor.MiddleCenter;
            container.Add(legend);
            
            return container;
        }
        
        private VisualElement CreateChartContent()
        {
            var container = new VisualElement();
            
            _chart = new StripChartPOC();
            _chart.style.height = 150;
            _chart.title = "MULTI-TRACE TREND";
            _chart.AddTrace("Trace 1", new Color(0f, 1f, 0.533f, 1f), 0f, 100f);
            _chart.AddTrace("Trace 2", new Color(0.4f, 0.8f, 1f, 1f), 0f, 100f);
            _chart.AddTrace("Trace 3", new Color(1f, 0.667f, 0f, 1f), 0f, 100f);
            container.Add(_chart);
            
            // Legend
            var legend = new VisualElement();
            legend.style.flexDirection = FlexDirection.Row;
            legend.style.justifyContent = Justify.Center;
            legend.style.marginTop = 8;
            
            Color[] colors = { new Color(0f, 1f, 0.533f, 1f), new Color(0.4f, 0.8f, 1f, 1f), new Color(1f, 0.667f, 0f, 1f) };
            string[] names = { "Trace 1", "Trace 2", "Trace 3" };
            
            for (int i = 0; i < 3; i++)
            {
                var item = new VisualElement();
                item.style.flexDirection = FlexDirection.Row;
                item.style.alignItems = Align.Center;
                item.style.marginRight = 20;
                
                var line = new VisualElement();
                line.style.width = 20;
                line.style.height = 3;
                line.style.backgroundColor = colors[i];
                line.style.marginRight = 5;
                item.Add(line);
                
                var label = new Label(names[i]);
                label.style.fontSize = 10;
                label.style.color = new Color(0.6f, 0.6f, 0.7f, 1f);
                item.Add(label);
                
                legend.Add(item);
            }
            
            container.Add(legend);
            
            return container;
        }
        
        private VisualElement CreateFooter()
        {
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.SpaceBetween;
            footer.style.alignItems = Align.Center;
            footer.style.height = 30;
            footer.style.backgroundColor = new Color(0.06f, 0.06f, 0.09f, 1f);
            footer.style.marginTop = 10;
            footer.style.paddingLeft = 15;
            footer.style.paddingRight = 15;
            footer.style.borderTopLeftRadius = 4;
            footer.style.borderTopRightRadius = 4;
            footer.style.borderBottomLeftRadius = 4;
            footer.style.borderBottomRightRadius = 4;
            
            var title = new Label("POC STATUS");
            title.style.color = new Color(0f, 1f, 0.533f, 1f);
            title.style.fontSize = 11;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            footer.Add(title);
            
            _perfLabel = new Label("Initializing...");
            _perfLabel.style.color = new Color(0.5f, 0.5f, 0.6f, 1f);
            _perfLabel.style.fontSize = 10;
            footer.Add(_perfLabel);
            
            return footer;
        }
    }
}
