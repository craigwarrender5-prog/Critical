// ============================================================================
// CRITICAL: Master the Atom â€” UI Toolkit Test Controller
// GaugeTestController.cs â€” Stage 0 Proof of Concept Test Harness
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using System.Diagnostics;
using System.Collections.Generic;
using Critical.UI.UIToolkit.Elements;

using Critical.Validation;
namespace Critical.UI.UIToolkit
{
    [RequireComponent(typeof(UIDocument))]
    public class GaugeTestController : MonoBehaviour
    {
        [Header("References")]
        public HeatupSimEngine engine;
        
        [Header("Settings")]
        [Range(5f, 60f)]
        public float updateRate = 10f;
        public bool logPerformance = true;
        public float perfLogInterval = 5f;
        
        // UI Elements
        private VisualElement _root;
        private ArcGaugeElement _gaugeTavg;
        private ArcGaugeElement _gaugePressure;
        private ArcGaugeElement _gaugePzrLevel;
        private ArcGaugeElement _gaugeSubcool;
        
        private Label _labelTavg;
        private Label _labelPressure;
        private Label _labelPzrLevel;
        private Label _labelSubcool;
        private Label _modeLabel;
        private Label _phaseLabel;
        private Label _simTimeLabel;
        private Label _wallTimeLabel;
        
        private StripChartElement _chartTemps;
        private StripChartElement _chartPressure;
        
        private Label _perfFrameTime;
        private Label _perfUpdateTime;
        private Label _perfTraceCount;
        
        // Performance
        private Stopwatch _updateStopwatch = new Stopwatch();
        private float _lastUpdateTime;
        private float _lastPerfLogTime;
        private float _accumulatedUpdateTime;
        private int _updateCount;
        private float _maxUpdateTime;
        
        void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null)
            {
                UnityEngine.Debug.LogError("[GaugeTestController] No UIDocument!");
                enabled = false;
                return;
            }
            
            _root = doc.rootVisualElement;
            if (_root == null)
            {
                UnityEngine.Debug.LogError("[GaugeTestController] Root is null!");
                enabled = false;
                return;
            }
            
            if (engine == null)
                engine = FindObjectOfType<HeatupSimEngine>();
            
            if (engine == null)
                UnityEngine.Debug.LogWarning("[GaugeTestController] No engine found - using test data");
            
            // Query existing labels from UXML
            QueryLabels();
            
            // Create and add gauges
            CreateGauges();
            
            // Create and add charts
            CreateCharts();
            
            UnityEngine.Debug.Log("[GaugeTestController] Initialized successfully");
        }
        
        void Update()
        {
            float interval = 1f / updateRate;
            if (Time.time - _lastUpdateTime < interval)
                return;
            
            _lastUpdateTime = Time.time;
            _updateStopwatch.Restart();
            
            UpdateUI();
            
            _updateStopwatch.Stop();
            float updateMs = (float)_updateStopwatch.Elapsed.TotalMilliseconds;
            
            _accumulatedUpdateTime += updateMs;
            _updateCount++;
            if (updateMs > _maxUpdateTime) _maxUpdateTime = updateMs;
            
            UpdatePerformanceDisplay(updateMs);
            
            if (logPerformance && Time.time - _lastPerfLogTime >= perfLogInterval)
            {
                LogPerformance();
                _lastPerfLogTime = Time.time;
            }
        }
        
        private void QueryLabels()
        {
            _labelTavg = _root.Q<Label>("label-tavg");
            _labelPressure = _root.Q<Label>("label-pressure");
            _labelPzrLevel = _root.Q<Label>("label-pzrlevel");
            _labelSubcool = _root.Q<Label>("label-subcool");
            _modeLabel = _root.Q<Label>("mode-label");
            _phaseLabel = _root.Q<Label>("phase-label");
            _simTimeLabel = _root.Q<Label>("sim-time");
            _wallTimeLabel = _root.Q<Label>("wall-time");
            _perfFrameTime = _root.Q<Label>("perf-frametime");
            _perfUpdateTime = _root.Q<Label>("perf-updatetime");
            _perfTraceCount = _root.Q<Label>("perf-tracecount");
        }
        
        private void CreateGauges()
        {
            // Find placeholder containers and replace with actual gauges
            var containers = new (string name, string label, float min, float max, 
                                  float warnLo, float warnHi, float alarmLo, float alarmHi)[]
            {
                ("gauge-tavg", "T_AVG", 50f, 650f, 100f, 545f, 80f, 570f),
                ("gauge-pressure", "PRESSURE", 0f, 2500f, 1800f, 2300f, 1600f, 2400f),
                ("gauge-pzrlevel", "PZR LEVEL", 0f, 100f, 20f, 70f, 15f, 80f),
                ("gauge-subcool", "SUBCOOL", 0f, 200f, 50f, 9999f, 20f, 9999f),
            };
            
            foreach (var c in containers)
            {
                var placeholder = _root.Q<VisualElement>(c.name);
                if (placeholder == null)
                {
                    UnityEngine.Debug.LogWarning($"[GaugeTestController] Placeholder '{c.name}' not found");
                    continue;
                }
                
                var gauge = new ArcGaugeElement();
                gauge.Label = c.label;
                gauge.MinValue = c.min;
                gauge.MaxValue = c.max;
                gauge.SetThresholds(c.warnLo, c.warnHi, c.alarmLo, c.alarmHi);
                gauge.style.width = 150;
                gauge.style.height = 120;
                
                var parent = placeholder.parent;
                int index = parent.IndexOf(placeholder);
                parent.Remove(placeholder);
                parent.Insert(index, gauge);
                
                // Store reference
                switch (c.name)
                {
                    case "gauge-tavg": _gaugeTavg = gauge; break;
                    case "gauge-pressure": _gaugePressure = gauge; break;
                    case "gauge-pzrlevel": _gaugePzrLevel = gauge; break;
                    case "gauge-subcool": _gaugeSubcool = gauge; break;
                }
                
                UnityEngine.Debug.Log($"[GaugeTestController] Created gauge: {c.label}");
            }
        }
        
        private void CreateCharts()
        {
            var containerTemps = _root.Q<VisualElement>("chart-temps");
            var containerPressure = _root.Q<VisualElement>("chart-pressure");
            
            if (containerTemps != null)
            {
                _chartTemps = new StripChartElement();
                _chartTemps.Title = "TEMPERATURES";
                _chartTemps.TimeWindowHours = 4f;
                _chartTemps.style.flexGrow = 1;
                _chartTemps.style.minHeight = 150;
                
                var parent = containerTemps.parent;
                int index = parent.IndexOf(containerTemps);
                parent.Remove(containerTemps);
                parent.Insert(index, _chartTemps);
                
                if (engine != null)
                {
                    _chartTemps.AddTrace("T_avg", new Color(0f, 1f, 0.533f), engine.tempHistory, engine.timeHistory);
                    _chartTemps.AddTrace("T_hot", new Color(1f, 0.4f, 0.4f), engine.tHotHistory, engine.timeHistory);
                    _chartTemps.AddTrace("T_cold", new Color(0.4f, 0.7f, 1f), engine.tColdHistory, engine.timeHistory);
                }
                
                UnityEngine.Debug.Log("[GaugeTestController] Created temperature chart");
            }
            
            if (containerPressure != null)
            {
                _chartPressure = new StripChartElement();
                _chartPressure.Title = "PRESSURE";
                _chartPressure.TimeWindowHours = 4f;
                _chartPressure.style.flexGrow = 1;
                _chartPressure.style.minHeight = 150;
                
                var parent = containerPressure.parent;
                int index = parent.IndexOf(containerPressure);
                parent.Remove(containerPressure);
                parent.Insert(index, _chartPressure);
                
                if (engine != null)
                {
                    _chartPressure.AddTrace("Pressure", new Color(0f, 1f, 0.533f), engine.pressHistory, engine.timeHistory);
                    _chartPressure.AddTrace("PZR Level", new Color(1f, 0.667f, 0f), engine.pzrLevelHistory, engine.timeHistory);
                }
                
                UnityEngine.Debug.Log("[GaugeTestController] Created pressure chart");
            }
            
            int traceCount = 0;
            if (_chartTemps != null) traceCount += _chartTemps.Traces.Count;
            if (_chartPressure != null) traceCount += _chartPressure.Traces.Count;
            if (_perfTraceCount != null) _perfTraceCount.text = $"Chart Traces: {traceCount}";
        }
        
        private void UpdateUI()
        {
            if (engine == null)
            {
                UpdateWithTestData();
                return;
            }
            
            UpdateWithEngineData();
        }
        
        private void UpdateWithEngineData()
        {
            if (_gaugeTavg != null) _gaugeTavg.Value = engine.T_avg;
            if (_gaugePressure != null) _gaugePressure.Value = engine.pressure;
            if (_gaugePzrLevel != null) _gaugePzrLevel.Value = engine.pzrLevel;
            if (_gaugeSubcool != null) _gaugeSubcool.Value = engine.subcooling;
            
            if (_labelTavg != null) _labelTavg.text = $"{engine.T_avg:F1} Â°F";
            if (_labelPressure != null) _labelPressure.text = $"{engine.pressure:F0} psia";
            if (_labelPzrLevel != null) _labelPzrLevel.text = $"{engine.pzrLevel:F1} %";
            if (_labelSubcool != null) _labelSubcool.text = $"{engine.subcooling:F1} Â°F";
            
            if (_modeLabel != null) _modeLabel.text = engine.GetModeString().Replace("\n", " ");
            if (_phaseLabel != null) _phaseLabel.text = engine.heatupPhaseDesc ?? "INITIALIZING";
            if (_simTimeLabel != null) _simTimeLabel.text = $"SIM: {FormatTime(engine.simTime)}";
            if (_wallTimeLabel != null) _wallTimeLabel.text = $"WALL: {FormatTime(engine.wallClockTime)}";
            
            if (_chartTemps != null)
            {
                _chartTemps.CurrentTime = engine.simTime;
                _chartTemps.MarkDirtyRepaint();
            }
            if (_chartPressure != null)
            {
                _chartPressure.CurrentTime = engine.simTime;
                _chartPressure.MarkDirtyRepaint();
            }
        }
        
        private void UpdateWithTestData()
        {
            float t = Time.time;
            
            float testTavg = 300f + Mathf.Sin(t * 0.5f) * 100f;
            float testPressure = 1500f + Mathf.Sin(t * 0.3f) * 500f;
            float testLevel = 50f + Mathf.Sin(t * 0.7f) * 30f;
            float testSubcool = 80f + Mathf.Cos(t * 0.4f) * 40f;
            
            if (_gaugeTavg != null) _gaugeTavg.Value = testTavg;
            if (_gaugePressure != null) _gaugePressure.Value = testPressure;
            if (_gaugePzrLevel != null) _gaugePzrLevel.Value = testLevel;
            if (_gaugeSubcool != null) _gaugeSubcool.Value = testSubcool;
            
            if (_labelTavg != null) _labelTavg.text = $"{testTavg:F1} Â°F";
            if (_labelPressure != null) _labelPressure.text = $"{testPressure:F0} psia";
            if (_labelPzrLevel != null) _labelPzrLevel.text = $"{testLevel:F1} %";
            if (_labelSubcool != null) _labelSubcool.text = $"{testSubcool:F1} Â°F";
            
            if (_modeLabel != null) _modeLabel.text = "MODE 5 Cold Shutdown (TEST)";
            if (_phaseLabel != null) _phaseLabel.text = "UI TOOLKIT TEST MODE";
            if (_simTimeLabel != null) _simTimeLabel.text = $"SIM: {FormatTime(t / 3600f)}";
            if (_wallTimeLabel != null) _wallTimeLabel.text = $"WALL: {FormatTime(t / 3600f)}";
        }
        
        private void UpdatePerformanceDisplay(float updateMs)
        {
            if (_perfFrameTime != null) _perfFrameTime.text = $"Frame Time: {Time.deltaTime * 1000f:F1} ms";
            if (_perfUpdateTime != null) _perfUpdateTime.text = $"UI Update: {updateMs:F2} ms";
        }
        
        private void LogPerformance()
        {
            if (_updateCount == 0) return;
            float avgMs = _accumulatedUpdateTime / _updateCount;
            UnityEngine.Debug.Log($"[GaugeTest PERF] Avg: {avgMs:F3}ms, Max: {_maxUpdateTime:F3}ms â€” {(avgMs < 1.0f ? "PASS âœ“" : "FAIL âœ—")}");
            _accumulatedUpdateTime = 0f;
            _updateCount = 0;
            _maxUpdateTime = 0f;
        }
        
        private string FormatTime(float hours)
        {
            int totalSeconds = Mathf.FloorToInt(hours * 3600f);
            int h = totalSeconds / 3600;
            int m = (totalSeconds % 3600) / 60;
            int s = totalSeconds % 60;
            return $"{h}:{m:D2}:{s:D2}";
        }
    }
}

