// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// PressurizerScreenPOC.cs — Full Pressurizer Operator Screen
// ============================================================================
//
// PURPOSE:
//   Complete pressurizer display panel that reads from HeatupSimEngine.
//   Demonstrates UI Toolkit integration with live simulation data.
//
// DISPLAYS:
//   - Pressurizer vessel visualization (water/steam zones)
//   - Level indication with setpoint
//   - Pressure with rate of change
//   - Heater status and power
//   - Spray system status
//   - CVCS flows (charging/letdown)
//   - Bubble formation state
//   - Key alarms and limits
//
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using Critical.Validation;

namespace Critical.UI.POC
{
    [RequireComponent(typeof(UIDocument))]
    public class PressurizerScreenPOC : MonoBehaviour
    {
        // ====================================================================
        // INSPECTOR
        // ====================================================================
        
        [Header("Data Source")]
        [SerializeField] private bool useSimulatedData = true;
        
        [Header("Update Settings")]
        [SerializeField] private float updateRate = 5f;
        
        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================
        
        private UIDocument _uiDocument;
        private HeatupSimEngine _engine;
        private float _nextUpdate;
        private float _simTime;
        
        // Vessel
        private PressurizerVesselPOC _vessel;
        
        // Gauges
        private ArcGaugePOC _pressureGauge;
        private ArcGaugePOC _levelGauge;
        private ArcGaugePOC _heaterGauge;
        private ArcGaugePOC _subcoolGauge;
        
        // Linear gauges
        private LinearGaugePOC _chargingBar;
        private LinearGaugePOC _letdownBar;
        
        // Strip chart
        private StripChartPOC _trendChart;
        
        // Labels
        private Label _pressureValue;
        private Label _pressureRate;
        private Label _levelValue;
        private Label _levelSetpoint;
        private Label _heaterValue;
        private Label _heaterMode;
        private Label _subcoolValue;
        private Label _chargingValue;
        private Label _letdownValue;
        private Label _bubblePhaseLabel;
        private Label _statusLabel;
        private Label _simTimeLabel;
        private Label _modeLabel;
        
        // Status LEDs
        private StatusLEDPOC _ledHeaters;
        private StatusLEDPOC _ledSpray;
        private StatusLEDPOC _ledCharging;
        private StatusLEDPOC _ledLetdown;
        private StatusLEDPOC _ledHiPress;
        private StatusLEDPOC _ledLoLevel;
        
        // ====================================================================
        // COLORS
        // ====================================================================
        
        private static readonly Color COLOR_ACCENT = new Color(0f, 1f, 0.533f, 1f);
        private static readonly Color COLOR_TEXT = new Color(0.8f, 0.8f, 0.85f, 1f);
        private static readonly Color COLOR_DIM = new Color(0.5f, 0.5f, 0.6f, 1f);
        private static readonly Color COLOR_BG = new Color(0.04f, 0.04f, 0.06f, 1f);
        private static readonly Color COLOR_PANEL = new Color(0.07f, 0.07f, 0.1f, 1f);
        
        // ====================================================================
        // LIFECYCLE
        // ====================================================================
        
        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            
            // Try to find HeatupSimEngine
            if (!useSimulatedData)
            {
                _engine = FindObjectOfType<HeatupSimEngine>();
                if (_engine == null)
                {
                    Debug.LogWarning("[PZR Screen] HeatupSimEngine not found, using simulated data");
                    useSimulatedData = true;
                }
            }
            
            BuildUI();
        }
        
        private void Update()
        {
            if (Time.time < _nextUpdate) return;
            _nextUpdate = Time.time + 1f / updateRate;
            
            if (useSimulatedData)
            {
                UpdateSimulatedData();
            }
            else
            {
                UpdateFromEngine();
            }
            
            // Update flow animation
            _vessel?.UpdateFlowAnimation(1f / updateRate);
        }
        
        // ====================================================================
        // DATA UPDATE
        // ====================================================================
        
        private void UpdateSimulatedData()
        {
            _simTime += 1f / updateRate;
            
            float sin1 = Mathf.Sin(_simTime * 0.3f);
            float sin2 = Mathf.Sin(_simTime * 0.5f);
            float sin3 = Mathf.Sin(_simTime * 0.7f);
            
            // Simulate heatup progression
            float progress = Mathf.Clamp01(_simTime / 120f);  // 2 minute cycle
            
            // Pressure: 400 -> 2235 psia
            float pressure = 400f + progress * 1835f + sin1 * 20f;
            float pressureRate = 50f + sin2 * 30f;
            
            // Level: 100% -> 60% during drain, then stabilize
            float level = progress < 0.3f ? 100f - progress * 133f : 60f + sin1 * 10f;
            float levelSetpoint = progress < 0.3f ? 100f : 60f;
            
            // Heater power
            float heaterPower = 50f + sin2 * 40f;
            bool heatersOn = heaterPower > 10f;
            
            // Subcooling
            float subcool = Mathf.Max(0f, 50f - progress * 45f + sin3 * 5f);
            
            // Flows
            float chargingFlow = 44f + sin1 * 20f;
            float letdownFlow = 75f + sin2 * 15f;
            
            // Spray
            bool sprayActive = pressure > 2200f && sin3 > 0.7f;
            
            // Bubble phase
            string bubblePhase = progress < 0.1f ? "DETECTION" :
                                 progress < 0.2f ? "VERIFICATION" :
                                 progress < 0.4f ? "DRAIN" :
                                 progress < 0.5f ? "STABILIZE" :
                                 progress < 0.7f ? "PRESSURIZE" : "COMPLETE";
            
            // Update displays
            UpdateDisplays(pressure, pressureRate, level, levelSetpoint, 
                          heaterPower, heatersOn, subcool, 
                          chargingFlow, letdownFlow, sprayActive,
                          bubblePhase, "SIMULATED DATA", _simTime);
            
            // Wrap simulation
            if (_simTime > 120f) _simTime = 0f;
        }
        
        private void UpdateFromEngine()
        {
            if (_engine == null) return;
            
            // Read from engine
            float pressure = _engine.pressure;
            float pressureRate = _engine.pressureRate;
            float level = _engine.pzrLevel;
            float levelSetpoint = Critical.Physics.PlantConstants.GetPZRLevelSetpointUnified(_engine.T_avg);
            float heaterPower = _engine.pzrHeaterPower;
            bool heatersOn = heaterPower > 0.1f;
            float subcool = _engine.subcooling;
            float chargingFlow = _engine.chargingFlow;
            float letdownFlow = _engine.letdownFlow;
            bool sprayActive = false;  // TODO: Add spray field to engine
            string bubblePhase = _engine.bubblePhase.ToString();
            string status = _engine.statusMessage;
            float simTime = _engine.simTime;
            
            UpdateDisplays(pressure, pressureRate, level, levelSetpoint,
                          heaterPower, heatersOn, subcool,
                          chargingFlow, letdownFlow, sprayActive,
                          bubblePhase, status, simTime);
        }
        
        private void UpdateDisplays(float pressure, float pressureRate, float level, float levelSetpoint,
                                   float heaterPower, bool heatersOn, float subcool,
                                   float chargingFlow, float letdownFlow, bool sprayActive,
                                   string bubblePhase, string status, float simTime)
        {
            // Vessel
            if (_vessel != null)
            {
                _vessel.level = level;
                _vessel.levelSetpoint = levelSetpoint;
                _vessel.pressure = pressure;
                _vessel.heaterPower = heaterPower;
                _vessel.sprayActive = sprayActive;
                _vessel.chargingFlow = chargingFlow;
                _vessel.letdownFlow = letdownFlow;
                _vessel.showBubbleZone = bubblePhase == "DRAIN" || bubblePhase == "DETECTION";
            }
            
            // Gauges
            if (_pressureGauge != null) _pressureGauge.value = pressure;
            if (_levelGauge != null) _levelGauge.value = level;
            if (_heaterGauge != null) _heaterGauge.value = heaterPower;
            if (_subcoolGauge != null) _subcoolGauge.value = subcool;
            
            // Linear gauges
            if (_chargingBar != null) _chargingBar.value = chargingFlow;
            if (_letdownBar != null) _letdownBar.value = letdownFlow;
            
            // Labels
            if (_pressureValue != null) _pressureValue.text = $"{pressure:F0} psia";
            if (_pressureRate != null) 
            {
                string sign = pressureRate >= 0 ? "+" : "";
                _pressureRate.text = $"{sign}{pressureRate:F1} psi/hr";
                _pressureRate.style.color = pressureRate > 100f ? new Color(1f, 0.667f, 0f) : COLOR_DIM;
            }
            if (_levelValue != null) _levelValue.text = $"{level:F1} %";
            if (_levelSetpoint != null) _levelSetpoint.text = $"SP: {levelSetpoint:F0}%";
            if (_heaterValue != null) _heaterValue.text = $"{heaterPower:F0} %";
            if (_heaterMode != null) _heaterMode.text = heatersOn ? "AUTO" : "OFF";
            if (_subcoolValue != null) _subcoolValue.text = $"{subcool:F1} °F";
            if (_chargingValue != null) _chargingValue.text = $"{chargingFlow:F0} gpm";
            if (_letdownValue != null) _letdownValue.text = $"{letdownFlow:F0} gpm";
            if (_bubblePhaseLabel != null) _bubblePhaseLabel.text = bubblePhase;
            if (_statusLabel != null) _statusLabel.text = status;
            if (_simTimeLabel != null)
            {
                int hours = (int)simTime;
                int minutes = (int)((simTime - hours) * 60f);
                _simTimeLabel.text = $"T+ {hours:D2}:{minutes:D2}";
            }
            
            // LEDs
            if (_ledHeaters != null) _ledHeaters.state = heatersOn ? LEDState.Normal : LEDState.Off;
            if (_ledSpray != null) _ledSpray.state = sprayActive ? LEDState.Normal : LEDState.Off;
            if (_ledCharging != null) _ledCharging.state = chargingFlow > 5f ? LEDState.Normal : LEDState.Off;
            if (_ledLetdown != null) _ledLetdown.state = letdownFlow > 5f ? LEDState.Normal : LEDState.Off;
            if (_ledHiPress != null) _ledHiPress.state = pressure > 2235f ? LEDState.Alarm : LEDState.Off;
            if (_ledLoLevel != null) _ledLoLevel.state = level < 17f ? LEDState.Alarm : LEDState.Off;
            
            // Trend chart
            if (_trendChart != null)
            {
                _trendChart.AddValue(0, pressure / 25f);  // Scale to 0-100 range
                _trendChart.AddValue(1, level);
                _trendChart.AddValue(2, heaterPower);
            }
        }
        
        // ====================================================================
        // UI CONSTRUCTION
        // ====================================================================
        
        private void BuildUI()
        {
            var root = _uiDocument.rootVisualElement;
            root.Clear();
            
            root.style.backgroundColor = COLOR_BG;
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingTop = 5;
            root.style.paddingBottom = 5;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            
            // Header
            root.Add(CreateHeader());
            
            // Main content row
            var mainRow = new VisualElement();
            mainRow.style.flexDirection = FlexDirection.Row;
            mainRow.style.flexGrow = 1;
            mainRow.style.marginTop = 10;
            root.Add(mainRow);
            
            // Left column: Vessel
            mainRow.Add(CreateVesselColumn());
            
            // Center column: Gauges
            mainRow.Add(CreateGaugesColumn());
            
            // Right column: CVCS & Status
            mainRow.Add(CreateStatusColumn());
            
            // Bottom: Trend chart
            root.Add(CreateTrendSection());
        }
        
        private VisualElement CreateHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.height = 35;
            header.style.backgroundColor = new Color(0.06f, 0.06f, 0.09f, 1f);
            header.style.paddingLeft = 15;
            header.style.paddingRight = 15;
            header.style.borderBottomWidth = 2;
            header.style.borderBottomColor = COLOR_ACCENT;
            
            var title = new Label("PRESSURIZER");
            title.style.color = COLOR_ACCENT;
            title.style.fontSize = 18;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);
            
            _modeLabel = new Label("MODE 5 - COLD SHUTDOWN");
            _modeLabel.style.color = COLOR_DIM;
            _modeLabel.style.fontSize = 12;
            header.Add(_modeLabel);
            
            _simTimeLabel = new Label("T+ 00:00");
            _simTimeLabel.style.color = new Color(0.4f, 0.8f, 1f);
            _simTimeLabel.style.fontSize = 14;
            header.Add(_simTimeLabel);
            
            return header;
        }
        
        private VisualElement CreateVesselColumn()
        {
            var column = new VisualElement();
            column.style.width = 200;
            column.style.marginRight = 15;
            
            // Vessel section
            var vesselBox = CreatePanel("VESSEL");
            vesselBox.style.flexGrow = 1;
            
            var vesselHolder = new VisualElement();
            vesselHolder.style.flexGrow = 1;
            vesselHolder.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            vesselHolder.style.borderTopLeftRadius = 4;
            vesselHolder.style.borderTopRightRadius = 4;
            vesselHolder.style.borderBottomLeftRadius = 4;
            vesselHolder.style.borderBottomRightRadius = 4;
            vesselHolder.style.marginTop = 5;
            
            _vessel = new PressurizerVesselPOC();
            _vessel.style.flexGrow = 1;
            vesselHolder.Add(_vessel);
            vesselBox.Add(vesselHolder);
            
            column.Add(vesselBox);
            
            // Bubble phase indicator
            var phaseBox = CreatePanel("BUBBLE FORMATION");
            phaseBox.style.height = 50;
            phaseBox.style.marginTop = 10;
            
            _bubblePhaseLabel = new Label("NONE");
            _bubblePhaseLabel.style.fontSize = 16;
            _bubblePhaseLabel.style.color = COLOR_ACCENT;
            _bubblePhaseLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _bubblePhaseLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _bubblePhaseLabel.style.flexGrow = 1;
            phaseBox.Add(_bubblePhaseLabel);
            
            column.Add(phaseBox);
            
            return column;
        }
        
        private VisualElement CreateGaugesColumn()
        {
            var column = new VisualElement();
            column.style.flexGrow = 1;
            column.style.marginRight = 15;
            
            // Top row: Pressure and Level
            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.height = 160;
            column.Add(topRow);
            
            topRow.Add(CreateGaugePanel("PRESSURE", 0f, 2500f, "psia", 
                out _pressureGauge, out _pressureValue, out _pressureRate));
            
            topRow.Add(CreateGaugePanel("LEVEL", 0f, 100f, "%",
                out _levelGauge, out _levelValue, out _levelSetpoint));
            
            // Bottom row: Heater and Subcool
            var bottomRow = new VisualElement();
            bottomRow.style.flexDirection = FlexDirection.Row;
            bottomRow.style.height = 160;
            bottomRow.style.marginTop = 10;
            column.Add(bottomRow);
            
            bottomRow.Add(CreateGaugePanel("HEATER POWER", 0f, 100f, "%",
                out _heaterGauge, out _heaterValue, out _heaterMode));
            
            Label unusedLabel;
            bottomRow.Add(CreateGaugePanel("SUBCOOLING", 0f, 100f, "°F",
                out _subcoolGauge, out _subcoolValue, out unusedLabel));
            
            return column;
        }
        
        private VisualElement CreateGaugePanel(string title, float min, float max, string unit,
            out ArcGaugePOC gauge, out Label valueLabel, out Label subLabel)
        {
            var panel = CreatePanel(title);
            panel.style.flexGrow = 1;
            panel.style.marginRight = 10;
            
            var gaugeHolder = new VisualElement();
            gaugeHolder.style.height = 80;
            gaugeHolder.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            gaugeHolder.style.borderTopLeftRadius = 4;
            gaugeHolder.style.borderTopRightRadius = 4;
            gaugeHolder.style.borderBottomLeftRadius = 4;
            gaugeHolder.style.borderBottomRightRadius = 4;
            gaugeHolder.style.marginTop = 5;
            
            gauge = new ArcGaugePOC();
            gauge.style.flexGrow = 1;
            gauge.minValue = min;
            gauge.maxValue = max;
            gaugeHolder.Add(gauge);
            panel.Add(gaugeHolder);
            
            valueLabel = new Label($"--- {unit}");
            valueLabel.style.fontSize = 18;
            valueLabel.style.color = COLOR_ACCENT;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            valueLabel.style.marginTop = 5;
            panel.Add(valueLabel);
            
            subLabel = new Label("");
            subLabel.style.fontSize = 11;
            subLabel.style.color = COLOR_DIM;
            subLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(subLabel);
            
            return panel;
        }
        
        private VisualElement CreateStatusColumn()
        {
            var column = new VisualElement();
            column.style.width = 180;
            
            // CVCS Flows
            var cvcsBox = CreatePanel("CVCS FLOWS");
            cvcsBox.style.height = 140;
            
            // Charging
            var chargingRow = CreateFlowRow("CHARGING", out _chargingBar, out _chargingValue, out _ledCharging);
            _chargingBar.minValue = 0;
            _chargingBar.maxValue = 100;
            _chargingBar.warningThreshold = 80;
            _chargingBar.alarmThreshold = 95;
            cvcsBox.Add(chargingRow);
            
            // Letdown
            var letdownRow = CreateFlowRow("LETDOWN", out _letdownBar, out _letdownValue, out _ledLetdown);
            _letdownBar.minValue = 0;
            _letdownBar.maxValue = 120;
            _letdownBar.warningThreshold = 90;
            _letdownBar.alarmThreshold = 110;
            cvcsBox.Add(letdownRow);
            
            column.Add(cvcsBox);
            
            // Equipment Status
            var equipBox = CreatePanel("EQUIPMENT");
            equipBox.style.height = 100;
            equipBox.style.marginTop = 10;
            
            equipBox.Add(CreateStatusRow("HEATERS", out _ledHeaters));
            equipBox.Add(CreateStatusRow("SPRAY", out _ledSpray));
            
            column.Add(equipBox);
            
            // Alarms
            var alarmBox = CreatePanel("ALARMS");
            alarmBox.style.height = 80;
            alarmBox.style.marginTop = 10;
            
            alarmBox.Add(CreateStatusRow("HI PRESSURE", out _ledHiPress));
            alarmBox.Add(CreateStatusRow("LO LEVEL", out _ledLoLevel));
            
            column.Add(alarmBox);
            
            // Status
            var statusBox = CreatePanel("STATUS");
            statusBox.style.flexGrow = 1;
            statusBox.style.marginTop = 10;
            
            _statusLabel = new Label("READY");
            _statusLabel.style.fontSize = 11;
            _statusLabel.style.color = COLOR_TEXT;
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            _statusLabel.style.marginTop = 5;
            statusBox.Add(_statusLabel);
            
            column.Add(statusBox);
            
            return column;
        }
        
        private VisualElement CreateFlowRow(string label, out LinearGaugePOC bar, out Label value, out StatusLEDPOC led)
        {
            var row = new VisualElement();
            row.style.marginTop = 8;
            
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.alignItems = Align.Center;
            
            var labelEl = new Label(label);
            labelEl.style.fontSize = 10;
            labelEl.style.color = COLOR_DIM;
            headerRow.Add(labelEl);
            
            var rightSide = new VisualElement();
            rightSide.style.flexDirection = FlexDirection.Row;
            rightSide.style.alignItems = Align.Center;
            
            value = new Label("--- gpm");
            value.style.fontSize = 12;
            value.style.color = COLOR_TEXT;
            value.style.marginRight = 5;
            rightSide.Add(value);
            
            led = new StatusLEDPOC();
            led.style.width = 12;
            led.style.height = 12;
            rightSide.Add(led);
            
            headerRow.Add(rightSide);
            row.Add(headerRow);
            
            bar = new LinearGaugePOC();
            bar.style.height = 16;
            bar.style.marginTop = 3;
            bar.orientation = LinearGaugeOrientation.Horizontal;
            row.Add(bar);
            
            return row;
        }
        
        private VisualElement CreateStatusRow(string label, out StatusLEDPOC led)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 8;
            
            var labelEl = new Label(label);
            labelEl.style.fontSize = 11;
            labelEl.style.color = COLOR_DIM;
            row.Add(labelEl);
            
            led = new StatusLEDPOC();
            led.style.width = 16;
            led.style.height = 16;
            row.Add(led);
            
            return row;
        }
        
        private VisualElement CreateTrendSection()
        {
            var section = CreatePanel("TRENDS — 4 Hour Rolling");
            section.style.height = 120;
            section.style.marginTop = 10;
            
            var chartHolder = new VisualElement();
            chartHolder.style.flexGrow = 1;
            chartHolder.style.flexDirection = FlexDirection.Row;
            chartHolder.style.marginTop = 5;
            
            _trendChart = new StripChartPOC();
            _trendChart.style.flexGrow = 1;
            _trendChart.AddTrace("Pressure", new Color(1f, 0.667f, 0f, 1f), 0f, 100f);
            _trendChart.AddTrace("Level", new Color(0f, 1f, 0.533f, 1f), 0f, 100f);
            _trendChart.AddTrace("Heater", new Color(0.4f, 0.8f, 1f, 1f), 0f, 100f);
            chartHolder.Add(_trendChart);
            
            // Legend
            var legend = new VisualElement();
            legend.style.width = 80;
            legend.style.marginLeft = 10;
            legend.style.justifyContent = Justify.Center;
            
            legend.Add(CreateLegendItem("Pressure", new Color(1f, 0.667f, 0f, 1f)));
            legend.Add(CreateLegendItem("Level", new Color(0f, 1f, 0.533f, 1f)));
            legend.Add(CreateLegendItem("Heater", new Color(0.4f, 0.8f, 1f, 1f)));
            
            chartHolder.Add(legend);
            section.Add(chartHolder);
            
            return section;
        }
        
        private VisualElement CreateLegendItem(string label, Color color)
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;
            item.style.marginBottom = 5;
            
            var line = new VisualElement();
            line.style.width = 15;
            line.style.height = 3;
            line.style.backgroundColor = color;
            line.style.marginRight = 5;
            item.Add(line);
            
            var labelEl = new Label(label);
            labelEl.style.fontSize = 10;
            labelEl.style.color = COLOR_DIM;
            item.Add(labelEl);
            
            return item;
        }
        
        private VisualElement CreatePanel(string title)
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = COLOR_PANEL;
            panel.style.borderTopLeftRadius = 6;
            panel.style.borderTopRightRadius = 6;
            panel.style.borderBottomLeftRadius = 6;
            panel.style.borderBottomRightRadius = 6;
            panel.style.paddingTop = 8;
            panel.style.paddingBottom = 8;
            panel.style.paddingLeft = 10;
            panel.style.paddingRight = 10;
            
            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 11;
            titleLabel.style.color = COLOR_ACCENT;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(titleLabel);
            
            return panel;
        }
    }
}
