// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// RCSLoopScreenPOC.cs — RCS Primary Loop Operator Screen
// ============================================================================
//
// PURPOSE:
//   Complete RCS Primary Loop display with 4-loop schematic showing:
//   - Reactor vessel (center)
//   - 4 hot legs with T_hot
//   - 4 Steam Generators
//   - 4 RCPs with status
//   - 4 cold legs with T_cold
//   - Pressurizer on Loop 2 hot leg
//   - RHR connections
//   - Flow animations
//
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using Critical.Validation;

namespace Critical.UI.POC
{
    [RequireComponent(typeof(UIDocument))]
    public class RCSLoopScreenPOC : MonoBehaviour
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
        
        // Loop diagram element
        private RCSLoopDiagramPOC _loopDiagram;
        
        // Gauges
        private ArcGaugePOC _tAvgGauge;
        private ArcGaugePOC _pressureGauge;
        private ArcGaugePOC _flowGauge;
        private ArcGaugePOC _subcoolGauge;
        
        // Strip charts
        private StripChartPOC _tempChart;
        private StripChartPOC _pressureChart;
        
        // Labels
        private Label _tAvgValue;
        private Label _tHotValue;
        private Label _tColdValue;
        private Label _pressureValue;
        private Label _subcoolValue;
        private Label _rcpStatus;
        private Label _rhrStatus;
        private Label _simTimeLabel;
        private Label _modeLabel;
        
        // LEDs
        private StatusLEDPOC[] _rcpLEDs = new StatusLEDPOC[4];
        private StatusLEDPOC _rhrLED;
        
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
            
            if (!useSimulatedData)
            {
                _engine = FindObjectOfType<HeatupSimEngine>();
                if (_engine == null)
                {
                    Debug.LogWarning("[RCS Loop Screen] HeatupSimEngine not found, using simulated data");
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
        }
        
        // ====================================================================
        // DATA UPDATE
        // ====================================================================
        
        private void UpdateSimulatedData()
        {
            _simTime += 1f / updateRate;
            
            float sin1 = Mathf.Sin(_simTime * 0.3f);
            float sin2 = Mathf.Sin(_simTime * 0.5f);
            
            // Simulate heatup
            float progress = Mathf.Clamp01(_simTime / 120f);
            
            float tAvg = 100f + progress * 450f + sin1 * 5f;
            float tHot = tAvg + 15f + sin2 * 3f;
            float tCold = tAvg - 15f + sin2 * 3f;
            float pressure = 400f + progress * 1835f + sin1 * 20f;
            float subcool = Mathf.Max(0f, 80f - progress * 75f + sin2 * 5f);
            
            // RCP status - start sequentially
            int rcpCount = progress < 0.2f ? 0 :
                          progress < 0.4f ? 1 :
                          progress < 0.6f ? 2 :
                          progress < 0.8f ? 3 : 4;
            bool[] rcpRunning = new bool[4];
            for (int i = 0; i < 4; i++) rcpRunning[i] = i < rcpCount;
            
            // RHR - active until pressure high enough
            bool rhrActive = pressure < 600f;
            float rhrFlow = rhrActive ? 3000f : 0f;
            
            // SG temps
            float[] sgTemps = new float[4];
            for (int i = 0; i < 4; i++)
            {
                sgTemps[i] = tCold - 50f + Mathf.Sin(_simTime * 0.2f + i) * 10f;
            }
            
            UpdateDisplays(tAvg, tHot, tCold, pressure, subcool, 
                          rcpCount, rcpRunning, rhrActive, rhrFlow, sgTemps, _simTime);
            
            if (_simTime > 120f) _simTime = 0f;
        }
        
        private void UpdateFromEngine()
        {
            if (_engine == null) return;
            
            float tAvg = _engine.T_avg;
            float tHot = _engine.T_hot;
            float tCold = _engine.T_cold;
            float pressure = _engine.pressure;
            float subcool = _engine.subcooling;
            int rcpCount = _engine.rcpCount;
            bool[] rcpRunning = _engine.rcpRunning;
            bool rhrActive = _engine.rhrActive;
            float rhrFlow = rhrActive ? 3000f : 0f; // Simplified
            
            // SG temps - use secondary temp as proxy
            float[] sgTemps = new float[4];
            for (int i = 0; i < 4; i++)
            {
                sgTemps[i] = _engine.T_sg_secondary;
            }
            
            UpdateDisplays(tAvg, tHot, tCold, pressure, subcool,
                          rcpCount, rcpRunning, rhrActive, rhrFlow, sgTemps, _engine.simTime);
        }
        
        private void UpdateDisplays(float tAvg, float tHot, float tCold, float pressure, float subcool,
                                   int rcpCount, bool[] rcpRunning, bool rhrActive, float rhrFlow,
                                   float[] sgTemps, float simTime)
        {
            // Loop diagram
            if (_loopDiagram != null)
            {
                _loopDiagram.T_hot = tHot;
                _loopDiagram.T_cold = tCold;
                _loopDiagram.T_avg = tAvg;
                _loopDiagram.pressure = pressure;
                _loopDiagram.rcpCount = rcpCount;
                _loopDiagram.rhrActive = rhrActive;
                for (int i = 0; i < 4; i++)
                {
                    _loopDiagram.SetRCPRunning(i, rcpRunning[i]);
                    _loopDiagram.SetSGTemp(i, sgTemps[i]);
                }
                _loopDiagram.UpdateAnimation(1f / updateRate);
            }
            
            // Gauges
            if (_tAvgGauge != null) _tAvgGauge.value = tAvg;
            if (_pressureGauge != null) _pressureGauge.value = pressure;
            if (_flowGauge != null) _flowGauge.value = rcpCount * 25f; // % of full flow
            if (_subcoolGauge != null) _subcoolGauge.value = subcool;
            
            // Labels
            if (_tAvgValue != null) _tAvgValue.text = $"{tAvg:F1} °F";
            if (_tHotValue != null) _tHotValue.text = $"T_hot: {tHot:F1} °F";
            if (_tColdValue != null) _tColdValue.text = $"T_cold: {tCold:F1} °F";
            if (_pressureValue != null) _pressureValue.text = $"{pressure:F0} psia";
            if (_subcoolValue != null) _subcoolValue.text = $"{subcool:F1} °F";
            if (_rcpStatus != null) _rcpStatus.text = $"{rcpCount}/4 RCPs Running";
            if (_rhrStatus != null) _rhrStatus.text = rhrActive ? $"RHR Active ({rhrFlow:F0} gpm)" : "RHR Isolated";
            
            if (_simTimeLabel != null)
            {
                int hours = (int)simTime;
                int minutes = (int)((simTime - hours) * 60f);
                _simTimeLabel.text = $"T+ {hours:D2}:{minutes:D2}";
            }
            
            // RCP LEDs
            for (int i = 0; i < 4; i++)
            {
                if (_rcpLEDs[i] != null)
                {
                    _rcpLEDs[i].state = rcpRunning[i] ? LEDState.Normal : LEDState.Off;
                }
            }
            
            // RHR LED
            if (_rhrLED != null)
            {
                _rhrLED.state = rhrActive ? LEDState.Normal : LEDState.Off;
            }
            
            // Charts
            if (_tempChart != null)
            {
                _tempChart.AddValue(0, tHot);
                _tempChart.AddValue(1, tAvg);
                _tempChart.AddValue(2, tCold);
            }
            
            if (_pressureChart != null)
            {
                _pressureChart.AddValue(0, pressure / 25f); // Scale to 0-100
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
            
            // Main content
            var mainRow = new VisualElement();
            mainRow.style.flexDirection = FlexDirection.Row;
            mainRow.style.flexGrow = 1;
            mainRow.style.marginTop = 10;
            root.Add(mainRow);
            
            // Left: Loop diagram
            mainRow.Add(CreateLoopDiagramSection());
            
            // Right: Gauges and status
            mainRow.Add(CreateStatusSection());
            
            // Bottom: Trends
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
            
            var title = new Label("RCS PRIMARY LOOP");
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
        
        private VisualElement CreateLoopDiagramSection()
        {
            var section = new VisualElement();
            section.style.flexGrow = 1;
            section.style.marginRight = 15;
            
            var panel = CreatePanel("4-LOOP SCHEMATIC");
            panel.style.flexGrow = 1;
            
            var diagramHolder = new VisualElement();
            diagramHolder.style.flexGrow = 1;
            diagramHolder.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            diagramHolder.style.borderTopLeftRadius = 4;
            diagramHolder.style.borderTopRightRadius = 4;
            diagramHolder.style.borderBottomLeftRadius = 4;
            diagramHolder.style.borderBottomRightRadius = 4;
            diagramHolder.style.marginTop = 5;
            
            _loopDiagram = new RCSLoopDiagramPOC();
            _loopDiagram.style.flexGrow = 1;
            diagramHolder.Add(_loopDiagram);
            panel.Add(diagramHolder);
            
            section.Add(panel);
            return section;
        }
        
        private VisualElement CreateStatusSection()
        {
            var section = new VisualElement();
            section.style.width = 220;
            
            // Temperature panel
            var tempPanel = CreatePanel("TEMPERATURES");
            tempPanel.style.height = 180;
            
            var tempGaugeRow = new VisualElement();
            tempGaugeRow.style.flexDirection = FlexDirection.Row;
            tempGaugeRow.style.justifyContent = Justify.Center;
            tempGaugeRow.style.marginTop = 5;
            
            var tAvgHolder = new VisualElement();
            tAvgHolder.style.width = 100;
            tAvgHolder.style.height = 80;
            tAvgHolder.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            tAvgHolder.style.borderTopLeftRadius = 4;
            tAvgHolder.style.borderTopRightRadius = 4;
            tAvgHolder.style.borderBottomLeftRadius = 4;
            tAvgHolder.style.borderBottomRightRadius = 4;
            
            _tAvgGauge = new ArcGaugePOC();
            _tAvgGauge.style.flexGrow = 1;
            _tAvgGauge.minValue = 50;
            _tAvgGauge.maxValue = 620;
            tAvgHolder.Add(_tAvgGauge);
            tempGaugeRow.Add(tAvgHolder);
            tempPanel.Add(tempGaugeRow);
            
            _tAvgValue = new Label("--- °F");
            _tAvgValue.style.fontSize = 18;
            _tAvgValue.style.color = COLOR_ACCENT;
            _tAvgValue.style.unityFontStyleAndWeight = FontStyle.Bold;
            _tAvgValue.style.unityTextAlign = TextAnchor.MiddleCenter;
            _tAvgValue.style.marginTop = 5;
            tempPanel.Add(_tAvgValue);
            
            var tempLabel = new Label("T_AVG");
            tempLabel.style.fontSize = 10;
            tempLabel.style.color = COLOR_DIM;
            tempLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            tempPanel.Add(tempLabel);
            
            _tHotValue = new Label("T_hot: --- °F");
            _tHotValue.style.fontSize = 12;
            _tHotValue.style.color = new Color(1f, 0.5f, 0.3f);
            _tHotValue.style.unityTextAlign = TextAnchor.MiddleCenter;
            _tHotValue.style.marginTop = 5;
            tempPanel.Add(_tHotValue);
            
            _tColdValue = new Label("T_cold: --- °F");
            _tColdValue.style.fontSize = 12;
            _tColdValue.style.color = new Color(0.3f, 0.7f, 1f);
            _tColdValue.style.unityTextAlign = TextAnchor.MiddleCenter;
            tempPanel.Add(_tColdValue);
            
            section.Add(tempPanel);
            
            // Pressure panel
            var pressPanel = CreatePanel("PRESSURE");
            pressPanel.style.height = 130;
            pressPanel.style.marginTop = 10;
            
            var pressGaugeRow = new VisualElement();
            pressGaugeRow.style.flexDirection = FlexDirection.Row;
            pressGaugeRow.style.justifyContent = Justify.SpaceAround;
            pressGaugeRow.style.marginTop = 5;
            
            var pressHolder = new VisualElement();
            pressHolder.style.width = 80;
            pressHolder.style.height = 65;
            pressHolder.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            pressHolder.style.borderTopLeftRadius = 4;
            pressHolder.style.borderTopRightRadius = 4;
            pressHolder.style.borderBottomLeftRadius = 4;
            pressHolder.style.borderBottomRightRadius = 4;
            
            _pressureGauge = new ArcGaugePOC();
            _pressureGauge.style.flexGrow = 1;
            _pressureGauge.minValue = 0;
            _pressureGauge.maxValue = 2500;
            pressHolder.Add(_pressureGauge);
            pressGaugeRow.Add(pressHolder);
            
            var subcoolHolder = new VisualElement();
            subcoolHolder.style.width = 80;
            subcoolHolder.style.height = 65;
            subcoolHolder.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            subcoolHolder.style.borderTopLeftRadius = 4;
            subcoolHolder.style.borderTopRightRadius = 4;
            subcoolHolder.style.borderBottomLeftRadius = 4;
            subcoolHolder.style.borderBottomRightRadius = 4;
            
            _subcoolGauge = new ArcGaugePOC();
            _subcoolGauge.style.flexGrow = 1;
            _subcoolGauge.minValue = 0;
            _subcoolGauge.maxValue = 100;
            subcoolHolder.Add(_subcoolGauge);
            pressGaugeRow.Add(subcoolHolder);
            
            pressPanel.Add(pressGaugeRow);
            
            var pressLabels = new VisualElement();
            pressLabels.style.flexDirection = FlexDirection.Row;
            pressLabels.style.justifyContent = Justify.SpaceAround;
            pressLabels.style.marginTop = 3;
            
            var pressCol = new VisualElement();
            pressCol.style.alignItems = Align.Center;
            _pressureValue = new Label("--- psia");
            _pressureValue.style.fontSize = 12;
            _pressureValue.style.color = COLOR_ACCENT;
            _pressureValue.style.unityFontStyleAndWeight = FontStyle.Bold;
            pressCol.Add(_pressureValue);
            var pressLbl = new Label("PRESS");
            pressLbl.style.fontSize = 9;
            pressLbl.style.color = COLOR_DIM;
            pressCol.Add(pressLbl);
            pressLabels.Add(pressCol);
            
            var subcoolCol = new VisualElement();
            subcoolCol.style.alignItems = Align.Center;
            _subcoolValue = new Label("--- °F");
            _subcoolValue.style.fontSize = 12;
            _subcoolValue.style.color = COLOR_ACCENT;
            _subcoolValue.style.unityFontStyleAndWeight = FontStyle.Bold;
            subcoolCol.Add(_subcoolValue);
            var subcoolLbl = new Label("SUBCOOL");
            subcoolLbl.style.fontSize = 9;
            subcoolLbl.style.color = COLOR_DIM;
            subcoolCol.Add(subcoolLbl);
            pressLabels.Add(subcoolCol);
            
            pressPanel.Add(pressLabels);
            section.Add(pressPanel);
            
            // RCP Status panel
            var rcpPanel = CreatePanel("RCP STATUS");
            rcpPanel.style.height = 80;
            rcpPanel.style.marginTop = 10;
            
            var rcpRow = new VisualElement();
            rcpRow.style.flexDirection = FlexDirection.Row;
            rcpRow.style.justifyContent = Justify.SpaceAround;
            rcpRow.style.marginTop = 8;
            
            for (int i = 0; i < 4; i++)
            {
                var rcpItem = new VisualElement();
                rcpItem.style.alignItems = Align.Center;
                
                _rcpLEDs[i] = new StatusLEDPOC();
                _rcpLEDs[i].style.width = 20;
                _rcpLEDs[i].style.height = 20;
                rcpItem.Add(_rcpLEDs[i]);
                
                var rcpLabel = new Label($"RCP{i + 1}");
                rcpLabel.style.fontSize = 10;
                rcpLabel.style.color = COLOR_DIM;
                rcpLabel.style.marginTop = 3;
                rcpItem.Add(rcpLabel);
                
                rcpRow.Add(rcpItem);
            }
            rcpPanel.Add(rcpRow);
            
            _rcpStatus = new Label("0/4 RCPs Running");
            _rcpStatus.style.fontSize = 11;
            _rcpStatus.style.color = COLOR_TEXT;
            _rcpStatus.style.unityTextAlign = TextAnchor.MiddleCenter;
            _rcpStatus.style.marginTop = 5;
            rcpPanel.Add(_rcpStatus);
            
            section.Add(rcpPanel);
            
            // RHR Status panel
            var rhrPanel = CreatePanel("RHR SYSTEM");
            rhrPanel.style.flexGrow = 1;
            rhrPanel.style.marginTop = 10;
            
            var rhrRow = new VisualElement();
            rhrRow.style.flexDirection = FlexDirection.Row;
            rhrRow.style.alignItems = Align.Center;
            rhrRow.style.marginTop = 8;
            
            _rhrLED = new StatusLEDPOC();
            _rhrLED.style.width = 20;
            _rhrLED.style.height = 20;
            _rhrLED.style.marginRight = 10;
            rhrRow.Add(_rhrLED);
            
            _rhrStatus = new Label("RHR Isolated");
            _rhrStatus.style.fontSize = 12;
            _rhrStatus.style.color = COLOR_TEXT;
            rhrRow.Add(_rhrStatus);
            
            rhrPanel.Add(rhrRow);
            section.Add(rhrPanel);
            
            return section;
        }
        
        private VisualElement CreateTrendSection()
        {
            var section = new VisualElement();
            section.style.height = 100;
            section.style.marginTop = 10;
            section.style.flexDirection = FlexDirection.Row;
            
            // Temperature trend
            var tempTrendPanel = CreatePanel("TEMPERATURE TREND");
            tempTrendPanel.style.flexGrow = 1;
            tempTrendPanel.style.marginRight = 10;
            
            _tempChart = new StripChartPOC();
            _tempChart.style.flexGrow = 1;
            _tempChart.style.marginTop = 5;
            _tempChart.AddTrace("T_hot", new Color(1f, 0.5f, 0.3f, 1f), 50f, 650f);
            _tempChart.AddTrace("T_avg", COLOR_ACCENT, 50f, 650f);
            _tempChart.AddTrace("T_cold", new Color(0.3f, 0.7f, 1f, 1f), 50f, 650f);
            tempTrendPanel.Add(_tempChart);
            section.Add(tempTrendPanel);
            
            // Pressure trend
            var pressTrendPanel = CreatePanel("PRESSURE TREND");
            pressTrendPanel.style.flexGrow = 1;
            
            _pressureChart = new StripChartPOC();
            _pressureChart.style.flexGrow = 1;
            _pressureChart.style.marginTop = 5;
            _pressureChart.AddTrace("Pressure", new Color(1f, 0.667f, 0f, 1f), 0f, 100f);
            pressTrendPanel.Add(_pressureChart);
            section.Add(pressTrendPanel);
            
            return section;
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
