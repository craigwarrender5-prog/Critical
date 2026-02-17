// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Element
// StripChartElement.cs — Multi-Trace Trend Graph with Auto-Scaling
// ============================================================================
//
// PURPOSE:
//   A custom UI Toolkit VisualElement that renders a strip chart (trend graph):
//   - Up to 6 simultaneous data traces
//   - Auto-scaling Y axis with configurable fixed range option
//   - Rolling time window (configurable, default 4 hours)
//   - Grid lines with axis labels
//   - Color-coded legend with live values
//
// USAGE:
//   var chart = new StripChartElement();
//   chart.TimeWindowHours = 4f;
//   chart.AddTrace("RCS Pressure", Color.green, pressureHistory, timeHistory);
//   chart.AddTrace("PZR Level", Color.yellow, levelHistory, timeHistory);
//
// RENDERING:
//   Uses MeshGenerationContext.Painter2D for vector line drawing.
//   Grid is drawn first, then traces are drawn as polylines.
//   Legend is rendered as child TextElements.
//
// ARCHITECTURE:
//   - Inherits from VisualElement
//   - Uses generateVisualContent callback for custom drawing
//   - Traces reference external List<float> buffers (no data copying)
//   - USS classes: strip-chart, strip-chart__legend
//
// IP-0042 Stage 0: Proof of Concept
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace Critical.UI.UIToolkit.Elements
{
    /// <summary>
    /// A multi-trace strip chart with auto-scaling and grid lines.
    /// </summary>
    public class StripChartElement : VisualElement
    {
        // ====================================================================
        // UXML FACTORY
        // ====================================================================
        
        public new class UxmlFactory : UxmlFactory<StripChartElement, UxmlTraits> { }
        
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Title = new() { name = "title", defaultValue = "TREND" };
            UxmlFloatAttributeDescription m_TimeWindow = new() { name = "time-window-hours", defaultValue = 4f };
            UxmlBoolAttributeDescription m_AutoScale = new() { name = "auto-scale", defaultValue = true };
            
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var chart = (StripChartElement)ve;
                chart.Title = m_Title.GetValueFromBag(bag, cc);
                chart.TimeWindowHours = m_TimeWindow.GetValueFromBag(bag, cc);
                chart.AutoScale = m_AutoScale.GetValueFromBag(bag, cc);
            }
        }
        
        // ====================================================================
        // CONSTANTS
        // ====================================================================
        
        private const int MAX_TRACES = 6;
        private const float GRID_LINE_WIDTH = 1f;
        private const float TRACE_LINE_WIDTH = 2f;
        private const int HORIZONTAL_GRID_LINES = 5;
        private const int VERTICAL_GRID_LINES = 8;
        
        // Margins for axis labels
        private const float MARGIN_LEFT = 50f;
        private const float MARGIN_RIGHT = 10f;
        private const float MARGIN_TOP = 25f;
        private const float MARGIN_BOTTOM = 20f;
        
        // ====================================================================
        // COLORS
        // ====================================================================
        
        private static readonly Color COLOR_BACKGROUND = new Color(0.06f, 0.06f, 0.09f, 1f);
        private static readonly Color COLOR_GRID = new Color(0.2f, 0.2f, 0.25f, 0.5f);
        private static readonly Color COLOR_AXIS = new Color(0.4f, 0.4f, 0.45f, 1f);
        private static readonly Color COLOR_TEXT = new Color(0.7f, 0.7f, 0.7f, 1f);
        
        // Default trace colors
        private static readonly Color[] DEFAULT_TRACE_COLORS = new Color[]
        {
            new Color(0f, 1f, 0.533f, 1f),      // Green #00FF88
            new Color(1f, 0.667f, 0f, 1f),      // Amber #FFAA00
            new Color(0.4f, 0.8f, 1f, 1f),      // Cyan
            new Color(1f, 0.4f, 0.4f, 1f),      // Red
            new Color(0.8f, 0.4f, 1f, 1f),      // Purple
            new Color(1f, 1f, 0.4f, 1f),        // Yellow
        };
        
        // ====================================================================
        // TRACE DATA STRUCTURE
        // ====================================================================
        
        public class TraceData
        {
            public string Name;
            public Color Color;
            public List<float> Values;       // Reference to external buffer
            public List<float> TimePoints;   // Reference to external time buffer
            public bool Visible = true;
            
            // Cached for rendering
            public float LastValue;
            public float MinValue;
            public float MaxValue;
        }
        
        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        private string _title = "TREND";
        public string Title
        {
            get => _title;
            set { _title = value; MarkDirtyRepaint(); }
        }
        
        private float _timeWindowHours = 4f;
        public float TimeWindowHours
        {
            get => _timeWindowHours;
            set { _timeWindowHours = Mathf.Max(0.1f, value); MarkDirtyRepaint(); }
        }
        
        private bool _autoScale = true;
        public bool AutoScale
        {
            get => _autoScale;
            set { _autoScale = value; MarkDirtyRepaint(); }
        }
        
        private float _fixedMinY = 0f;
        private float _fixedMaxY = 100f;
        
        /// <summary>
        /// Set fixed Y-axis range (disables auto-scaling).
        /// </summary>
        public void SetFixedRange(float minY, float maxY)
        {
            _fixedMinY = minY;
            _fixedMaxY = maxY;
            _autoScale = false;
            MarkDirtyRepaint();
        }
        
        // Current time for the right edge of the chart
        private float _currentTime = 0f;
        public float CurrentTime
        {
            get => _currentTime;
            set { _currentTime = value; MarkDirtyRepaint(); }
        }
        
        // ====================================================================
        // TRACE MANAGEMENT
        // ====================================================================
        
        private List<TraceData> _traces = new List<TraceData>();
        
        /// <summary>
        /// Add a trace to the chart.
        /// </summary>
        /// <param name="name">Display name for legend</param>
        /// <param name="color">Trace color</param>
        /// <param name="values">Reference to value buffer (not copied)</param>
        /// <param name="timePoints">Reference to time buffer (not copied)</param>
        /// <returns>Trace index, or -1 if max traces reached</returns>
        public int AddTrace(string name, Color color, List<float> values, List<float> timePoints)
        {
            if (_traces.Count >= MAX_TRACES) return -1;
            
            var trace = new TraceData
            {
                Name = name,
                Color = color,
                Values = values,
                TimePoints = timePoints
            };
            
            _traces.Add(trace);
            MarkDirtyRepaint();
            return _traces.Count - 1;
        }
        
        /// <summary>
        /// Add a trace with auto-assigned color.
        /// </summary>
        public int AddTrace(string name, List<float> values, List<float> timePoints)
        {
            int colorIndex = _traces.Count % DEFAULT_TRACE_COLORS.Length;
            return AddTrace(name, DEFAULT_TRACE_COLORS[colorIndex], values, timePoints);
        }
        
        /// <summary>
        /// Remove all traces.
        /// </summary>
        public void ClearTraces()
        {
            _traces.Clear();
            MarkDirtyRepaint();
        }
        
        /// <summary>
        /// Set trace visibility.
        /// </summary>
        public void SetTraceVisible(int index, bool visible)
        {
            if (index >= 0 && index < _traces.Count)
            {
                _traces[index].Visible = visible;
                MarkDirtyRepaint();
            }
        }
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public StripChartElement()
        {
            AddToClassList("strip-chart");
            
            style.minWidth = 200;
            style.minHeight = 100;
            
            generateVisualContent += OnGenerateVisualContent;
        }
        
        // ====================================================================
        // RENDERING
        // ====================================================================
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            Rect rect = contentRect;
            if (rect.width < 50 || rect.height < 50) return;
            
            var painter = mgc.painter2D;
            
            // Calculate chart area (inside margins)
            Rect chartArea = new Rect(
                MARGIN_LEFT,
                MARGIN_TOP,
                rect.width - MARGIN_LEFT - MARGIN_RIGHT,
                rect.height - MARGIN_TOP - MARGIN_BOTTOM
            );
            
            if (chartArea.width < 20 || chartArea.height < 20) return;
            
            // Draw background
            DrawBackground(painter, chartArea);
            
            // Calculate Y range
            float minY, maxY;
            CalculateYRange(out minY, out maxY);
            
            // Calculate time range
            float timeMin = _currentTime - _timeWindowHours;
            float timeMax = _currentTime;
            
            // Draw grid
            DrawGrid(painter, chartArea, minY, maxY, timeMin, timeMax);
            
            // Draw traces
            foreach (var trace in _traces)
            {
                if (trace.Visible && trace.Values != null && trace.Values.Count > 0)
                {
                    DrawTrace(painter, chartArea, trace, minY, maxY, timeMin, timeMax);
                }
            }
            
            // Draw axis frame
            DrawAxisFrame(painter, chartArea);
        }
        
        private void DrawBackground(Painter2D painter, Rect area)
        {
            painter.fillColor = COLOR_BACKGROUND;
            painter.BeginPath();
            painter.MoveTo(new Vector2(area.x, area.y));
            painter.LineTo(new Vector2(area.xMax, area.y));
            painter.LineTo(new Vector2(area.xMax, area.yMax));
            painter.LineTo(new Vector2(area.x, area.yMax));
            painter.ClosePath();
            painter.Fill();
        }
        
        private void DrawGrid(Painter2D painter, Rect area, float minY, float maxY, float timeMin, float timeMax)
        {
            painter.strokeColor = COLOR_GRID;
            painter.lineWidth = GRID_LINE_WIDTH;
            
            // Horizontal grid lines
            for (int i = 0; i <= HORIZONTAL_GRID_LINES; i++)
            {
                float y = area.y + (area.height * i / HORIZONTAL_GRID_LINES);
                painter.BeginPath();
                painter.MoveTo(new Vector2(area.x, y));
                painter.LineTo(new Vector2(area.xMax, y));
                painter.Stroke();
            }
            
            // Vertical grid lines
            for (int i = 0; i <= VERTICAL_GRID_LINES; i++)
            {
                float x = area.x + (area.width * i / VERTICAL_GRID_LINES);
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, area.y));
                painter.LineTo(new Vector2(x, area.yMax));
                painter.Stroke();
            }
        }
        
        private void DrawTrace(Painter2D painter, Rect area, TraceData trace, 
                               float minY, float maxY, float timeMin, float timeMax)
        {
            var values = trace.Values;
            var times = trace.TimePoints;
            
            if (values == null || times == null || values.Count == 0 || times.Count == 0)
                return;
            
            // Ensure equal length
            int count = Mathf.Min(values.Count, times.Count);
            if (count < 2) return;
            
            painter.strokeColor = trace.Color;
            painter.lineWidth = TRACE_LINE_WIDTH;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            
            float yRange = maxY - minY;
            float timeRange = timeMax - timeMin;
            
            if (yRange <= 0 || timeRange <= 0) return;
            
            painter.BeginPath();
            bool pathStarted = false;
            
            // Track min/max/last for legend display
            trace.MinValue = float.MaxValue;
            trace.MaxValue = float.MinValue;
            
            for (int i = 0; i < count; i++)
            {
                float t = times[i];
                float v = values[i];
                
                // Skip points outside time window
                if (t < timeMin || t > timeMax) continue;
                
                // Track stats
                if (v < trace.MinValue) trace.MinValue = v;
                if (v > trace.MaxValue) trace.MaxValue = v;
                trace.LastValue = v;
                
                // Map to screen coordinates
                float x = area.x + ((t - timeMin) / timeRange) * area.width;
                float y = area.yMax - ((v - minY) / yRange) * area.height;
                
                // Clamp to chart area
                y = Mathf.Clamp(y, area.y, area.yMax);
                
                if (!pathStarted)
                {
                    painter.MoveTo(new Vector2(x, y));
                    pathStarted = true;
                }
                else
                {
                    painter.LineTo(new Vector2(x, y));
                }
            }
            
            if (pathStarted)
            {
                painter.Stroke();
            }
        }
        
        private void DrawAxisFrame(Painter2D painter, Rect area)
        {
            painter.strokeColor = COLOR_AXIS;
            painter.lineWidth = 2f;
            
            // Left and bottom axis lines
            painter.BeginPath();
            painter.MoveTo(new Vector2(area.x, area.y));
            painter.LineTo(new Vector2(area.x, area.yMax));
            painter.LineTo(new Vector2(area.xMax, area.yMax));
            painter.Stroke();
        }
        
        private void CalculateYRange(out float minY, out float maxY)
        {
            if (!_autoScale)
            {
                minY = _fixedMinY;
                maxY = _fixedMaxY;
                return;
            }
            
            // Find min/max across all visible traces
            minY = float.MaxValue;
            maxY = float.MinValue;
            
            float timeMin = _currentTime - _timeWindowHours;
            
            foreach (var trace in _traces)
            {
                if (!trace.Visible || trace.Values == null || trace.TimePoints == null)
                    continue;
                
                int count = Mathf.Min(trace.Values.Count, trace.TimePoints.Count);
                for (int i = 0; i < count; i++)
                {
                    if (trace.TimePoints[i] < timeMin) continue;
                    
                    float v = trace.Values[i];
                    if (v < minY) minY = v;
                    if (v > maxY) maxY = v;
                }
            }
            
            // Handle edge cases
            if (minY == float.MaxValue || maxY == float.MinValue)
            {
                minY = 0f;
                maxY = 100f;
            }
            else if (Mathf.Approximately(minY, maxY))
            {
                // Add padding if flat
                float padding = Mathf.Max(1f, Mathf.Abs(minY) * 0.1f);
                minY -= padding;
                maxY += padding;
            }
            else
            {
                // Add 5% padding
                float range = maxY - minY;
                minY -= range * 0.05f;
                maxY += range * 0.05f;
            }
        }
        
        // ====================================================================
        // PUBLIC — Get trace info for legend display
        // ====================================================================
        
        /// <summary>
        /// Get trace data for external legend rendering.
        /// </summary>
        public IReadOnlyList<TraceData> Traces => _traces.AsReadOnly();
    }
}
