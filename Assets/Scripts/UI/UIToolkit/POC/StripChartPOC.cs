// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// StripChartPOC.cs — Strip Chart for trend visualization
// ============================================================================
//
// PURPOSE:
//   Test Painter2D line rendering for strip charts / trend graphs.
//   Uses a ring buffer to store historical values and renders as a line graph.
//
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Critical.UI.POC
{
    /// <summary>
    /// A strip chart (trend line) element for displaying historical data.
    /// Supports multiple traces with different colors.
    /// </summary>
    [UxmlElement]
    public partial class StripChartPOC : VisualElement
    {
        // ====================================================================
        // CONSTANTS
        // ====================================================================
        
        private const int DEFAULT_BUFFER_SIZE = 200;  // Number of data points
        private const float GRID_LINE_ALPHA = 0.15f;
        
        // ====================================================================
        // COLORS
        // ====================================================================
        
        private static readonly Color COLOR_BACKGROUND = new Color(0.05f, 0.05f, 0.08f, 1f);
        private static readonly Color COLOR_GRID = new Color(1f, 1f, 1f, GRID_LINE_ALPHA);
        private static readonly Color COLOR_BORDER = new Color(0.2f, 0.2f, 0.25f, 1f);
        
        // Default trace colors
        private static readonly Color[] DEFAULT_TRACE_COLORS = new Color[]
        {
            new Color(0f, 1f, 0.533f, 1f),      // Green
            new Color(0.4f, 0.8f, 1f, 1f),      // Cyan
            new Color(1f, 0.667f, 0f, 1f),      // Amber
            new Color(1f, 0.4f, 0.4f, 1f),      // Red
        };
        
        // ====================================================================
        // TRACE DATA
        // ====================================================================
        
        private class Trace
        {
            public string Name;
            public Color Color;
            public float[] Buffer;
            public int WriteIndex;
            public int Count;
            public float MinValue;
            public float MaxValue;
            public float CurrentValue;
            
            public Trace(string name, Color color, int bufferSize, float minValue, float maxValue)
            {
                Name = name;
                Color = color;
                Buffer = new float[bufferSize];
                WriteIndex = 0;
                Count = 0;
                MinValue = minValue;
                MaxValue = maxValue;
                CurrentValue = minValue;
            }
            
            public void AddValue(float value)
            {
                CurrentValue = value;
                Buffer[WriteIndex] = Mathf.Clamp(value, MinValue, MaxValue);
                WriteIndex = (WriteIndex + 1) % Buffer.Length;
                if (Count < Buffer.Length) Count++;
            }
            
            public float GetValue(int index)
            {
                if (Count == 0) return MinValue;
                int actualIndex = (WriteIndex - Count + index + Buffer.Length) % Buffer.Length;
                return Buffer[actualIndex];
            }
        }
        
        private List<Trace> _traces = new List<Trace>();
        
        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        private string _title = "CHART";
        private int _bufferSize = DEFAULT_BUFFER_SIZE;
        private bool _showGrid = true;
        private int _gridLinesH = 4;
        private int _gridLinesV = 6;
        
        [UxmlAttribute]
        public string title
        {
            get => _title;
            set { _title = value; MarkDirtyRepaint(); }
        }
        
        [UxmlAttribute]
        public bool showGrid
        {
            get => _showGrid;
            set { _showGrid = value; MarkDirtyRepaint(); }
        }
        
        public int TraceCount => _traces.Count;
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public StripChartPOC()
        {
            style.minWidth = 200;
            style.minHeight = 100;
            style.backgroundColor = COLOR_BACKGROUND;
            
            generateVisualContent += OnGenerateVisualContent;
        }
        
        // ====================================================================
        // PUBLIC METHODS
        // ====================================================================
        
        /// <summary>
        /// Add a new trace to the chart.
        /// </summary>
        public int AddTrace(string name, Color color, float minValue, float maxValue)
        {
            var trace = new Trace(name, color, _bufferSize, minValue, maxValue);
            _traces.Add(trace);
            return _traces.Count - 1;
        }
        
        /// <summary>
        /// Add a trace with default color.
        /// </summary>
        public int AddTrace(string name, float minValue, float maxValue)
        {
            int colorIndex = _traces.Count % DEFAULT_TRACE_COLORS.Length;
            return AddTrace(name, DEFAULT_TRACE_COLORS[colorIndex], minValue, maxValue);
        }
        
        /// <summary>
        /// Add a value to a specific trace.
        /// </summary>
        public void AddValue(int traceIndex, float value)
        {
            if (traceIndex >= 0 && traceIndex < _traces.Count)
            {
                _traces[traceIndex].AddValue(value);
                MarkDirtyRepaint();
            }
        }
        
        /// <summary>
        /// Clear all data from all traces.
        /// </summary>
        public void ClearAll()
        {
            foreach (var trace in _traces)
            {
                trace.WriteIndex = 0;
                trace.Count = 0;
            }
            MarkDirtyRepaint();
        }
        
        // ====================================================================
        // RENDERING
        // ====================================================================
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            float width = contentRect.width;
            float height = contentRect.height;
            
            if (width < 20f || height < 20f)
                return;
            
            var painter = mgc.painter2D;
            if (painter == null)
                return;
            
            // Chart area (with margins for labels)
            float marginLeft = 5f;
            float marginRight = 5f;
            float marginTop = 5f;
            float marginBottom = 5f;
            
            float chartX = marginLeft;
            float chartY = marginTop;
            float chartWidth = width - marginLeft - marginRight;
            float chartHeight = height - marginTop - marginBottom;
            
            // ================================================================
            // Draw border
            // ================================================================
            painter.strokeColor = COLOR_BORDER;
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(chartX, chartY));
            painter.LineTo(new Vector2(chartX + chartWidth, chartY));
            painter.LineTo(new Vector2(chartX + chartWidth, chartY + chartHeight));
            painter.LineTo(new Vector2(chartX, chartY + chartHeight));
            painter.ClosePath();
            painter.Stroke();
            
            // ================================================================
            // Draw grid
            // ================================================================
            if (_showGrid)
            {
                painter.strokeColor = COLOR_GRID;
                painter.lineWidth = 1f;
                
                // Horizontal grid lines
                for (int i = 1; i < _gridLinesH; i++)
                {
                    float y = chartY + (chartHeight * i / _gridLinesH);
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(chartX, y));
                    painter.LineTo(new Vector2(chartX + chartWidth, y));
                    painter.Stroke();
                }
                
                // Vertical grid lines
                for (int i = 1; i < _gridLinesV; i++)
                {
                    float x = chartX + (chartWidth * i / _gridLinesV);
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(x, chartY));
                    painter.LineTo(new Vector2(x, chartY + chartHeight));
                    painter.Stroke();
                }
            }
            
            // ================================================================
            // Draw traces
            // ================================================================
            foreach (var trace in _traces)
            {
                if (trace.Count < 2)
                    continue;
                
                painter.strokeColor = trace.Color;
                painter.lineWidth = 1.5f;
                painter.lineCap = LineCap.Round;
                painter.lineJoin = LineJoin.Round;
                
                painter.BeginPath();
                
                float xStep = chartWidth / (trace.Buffer.Length - 1);
                
                for (int i = 0; i < trace.Count; i++)
                {
                    float value = trace.GetValue(i);
                    float normalizedValue = (value - trace.MinValue) / (trace.MaxValue - trace.MinValue);
                    normalizedValue = Mathf.Clamp01(normalizedValue);
                    
                    float x = chartX + (i * xStep);
                    float y = chartY + chartHeight - (normalizedValue * chartHeight);
                    
                    if (i == 0)
                        painter.MoveTo(new Vector2(x, y));
                    else
                        painter.LineTo(new Vector2(x, y));
                }
                
                painter.Stroke();
            }
        }
    }
}
