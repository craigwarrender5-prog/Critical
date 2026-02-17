// ============================================================================
// CRITICAL: Master the Atom - Strip Chart Component
// StripChart.cs - Full-Size Multi-Trace Trend Chart
// ============================================================================
//
// PURPOSE:
//   Renders a full-size strip chart (trend graph) with multiple traces,
//   auto-scaling Y-axis, time axis labels, grid lines, and a compact legend.
//   Reads from engine history buffers (List<float>) for data.
//
// VISUAL DESIGN:
//   ┌──────────────────────────────────────────────────────┐
//   │  RCS TEMPERATURES                          LEGEND    │
//   │ 600├─────────────────────────────── T_avg ■ 557.2°F │
//   │    │        ╱───────────────────── T_hot  ■ 619.0°F │
//   │ 400├───╱───╱────────────────────── T_cold ■ 558.0°F │
//   │    │  ╱  ╱                                           │
//   │ 200├─╱──╱───────────────────────────────────────────│
//   │    │╱                                                │
//   │ 100├─────────────────────────────────────────────────│
//   │    0:00    0:30    1:00    1:30    2:00    2:30       │
//   └──────────────────────────────────────────────────────┘
//
// ARCHITECTURE:
//   Uses MaskableGraphic + OnPopulateMesh for GPU-efficient line rendering.
//   No per-frame allocations. Ring buffer reads via TrendBuffer or
//   direct List<float> history buffer access.
//
// VERSION: 1.0.0
// DATE: 2026-02-17
// IP: IP-0041 Stage 1
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Full-size multi-trace strip chart with auto-scaling, grid, and legend.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class StripChart : MaskableGraphic
    {
        // ====================================================================
        // TRACE DEFINITION
        // ====================================================================

        /// <summary>
        /// Defines a single data trace on the chart.
        /// </summary>
        public class TraceDefinition
        {
            public string Label;
            public Color TraceColor;
            public List<float> DataBuffer;         // Engine history buffer reference
            public List<float> TimeBuffer;          // Shared time axis buffer
            public float CurrentValue;              // Live value for legend display
            public string Format = "F1";
            public string Unit = "";
            public bool Visible = true;
        }

        // ====================================================================
        // REFERENCE LINE DEFINITION
        // ====================================================================

        public class ReferenceLine
        {
            public float Value;
            public Color LineColor;
            public string Label;
        }

        // ====================================================================
        // CONFIGURATION
        // ====================================================================

        private string _title = "";
        private float _lineWidth = 2f;
        private float _gridLineWidth = 1f;
        private int _horizontalGridLines = 4;
        private float _timeWindowHours = 4f;       // Rolling window width
        private float _yMin = float.MaxValue;
        private float _yMax = float.MinValue;
        private bool _autoScale = true;
        private float _autoScaleMarginPct = 0.1f;  // 10% headroom above/below data

        private readonly List<TraceDefinition> _traces = new List<TraceDefinition>();
        private readonly List<ReferenceLine> _referenceLines = new List<ReferenceLine>();

        // UI references (created by factory)
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI[] _yAxisLabels;
        private TextMeshProUGUI[] _xAxisLabels;
        private TextMeshProUGUI[] _legendLabels;
        private Image[] _legendSwatches;

        // Plot area (relative to this RectTransform)
        private readonly float _plotMarginLeft = 52f;
        private readonly float _plotMarginRight = 8f;
        private readonly float _plotMarginTop = 24f;
        private readonly float _plotMarginBottom = 20f;

        // Cached
        private Rect _plotRect;
        private float _cachedYMin;
        private float _cachedYMax;
        private bool _dirty = true;

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        public void SetTitle(string title)
        {
            _title = title;
            if (_titleText != null) _titleText.text = title;
        }

        public void SetTimeWindow(float hours) { _timeWindowHours = hours; }
        public void SetYRange(float min, float max) { _yMin = min; _yMax = max; _autoScale = false; }
        public void SetAutoScale(bool auto) { _autoScale = auto; }
        public void SetLineWidth(float w) { _lineWidth = w; }

        public int AddTrace(string label, Color color, List<float> dataBuffer,
            List<float> timeBuffer, string format = "F1", string unit = "")
        {
            _traces.Add(new TraceDefinition
            {
                Label = label,
                TraceColor = color,
                DataBuffer = dataBuffer,
                TimeBuffer = timeBuffer,
                Format = format,
                Unit = unit
            });
            return _traces.Count - 1;
        }

        public void AddReferenceLine(float value, Color color, string label = "")
        {
            _referenceLines.Add(new ReferenceLine
            {
                Value = value, LineColor = color, Label = label
            });
        }

        public void UpdateTraceValue(int index, float currentValue)
        {
            if (index >= 0 && index < _traces.Count)
                _traces[index].CurrentValue = currentValue;
        }

        public void MarkDirty() { _dirty = true; SetVerticesDirty(); }

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            _dirty = true;
        }

        void Update()
        {
            // Refresh mesh every frame for live data
            SetVerticesDirty();
            UpdateLegendValues();
        }

        // ====================================================================
        // MESH GENERATION
        // ====================================================================

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_traces.Count == 0) return;

            Rect rect = GetPixelAdjustedRect();
            _plotRect = new Rect(
                rect.x + _plotMarginLeft,
                rect.y + _plotMarginBottom,
                rect.width - _plotMarginLeft - _plotMarginRight,
                rect.height - _plotMarginTop - _plotMarginBottom);

            if (_plotRect.width < 10f || _plotRect.height < 10f) return;

            ComputeYRange();
            DrawGridLines(vh);
            DrawReferenceLines(vh);
            DrawTraces(vh);
            UpdateAxisLabels();
        }

        // ====================================================================
        // Y-AXIS AUTO-SCALING
        // ====================================================================

        void ComputeYRange()
        {
            if (!_autoScale)
            {
                _cachedYMin = _yMin;
                _cachedYMax = _yMax;
                return;
            }

            float dataMin = float.MaxValue;
            float dataMax = float.MinValue;

            // Determine current time window
            float currentTime = 0f;
            for (int t = 0; t < _traces.Count; t++)
            {
                var trace = _traces[t];
                if (!trace.Visible || trace.TimeBuffer == null || trace.TimeBuffer.Count == 0)
                    continue;
                currentTime = Mathf.Max(currentTime, trace.TimeBuffer[trace.TimeBuffer.Count - 1]);
            }
            float windowStart = Mathf.Max(0f, currentTime - _timeWindowHours);

            for (int t = 0; t < _traces.Count; t++)
            {
                var trace = _traces[t];
                if (!trace.Visible || trace.DataBuffer == null || trace.TimeBuffer == null)
                    continue;

                int count = Mathf.Min(trace.DataBuffer.Count, trace.TimeBuffer.Count);
                for (int i = 0; i < count; i++)
                {
                    if (trace.TimeBuffer[i] < windowStart) continue;
                    float val = trace.DataBuffer[i];
                    if (float.IsNaN(val) || float.IsInfinity(val)) continue;
                    if (val < dataMin) dataMin = val;
                    if (val > dataMax) dataMax = val;
                }
            }

            // Include reference lines in range
            for (int r = 0; r < _referenceLines.Count; r++)
            {
                float val = _referenceLines[r].Value;
                if (val < dataMin) dataMin = val;
                if (val > dataMax) dataMax = val;
            }

            if (dataMin == float.MaxValue || dataMax == float.MinValue)
            {
                _cachedYMin = 0f;
                _cachedYMax = 100f;
                return;
            }

            float range = dataMax - dataMin;
            if (range < 1f) range = 1f;
            float margin = range * _autoScaleMarginPct;
            _cachedYMin = dataMin - margin;
            _cachedYMax = dataMax + margin;
        }

        // ====================================================================
        // DRAWING HELPERS
        // ====================================================================

        void DrawGridLines(VertexHelper vh)
        {
            Color gridColor = ValidationDashboardTheme.TraceGrid;
            gridColor.a = 0.4f;

            // Horizontal grid lines
            for (int i = 0; i <= _horizontalGridLines; i++)
            {
                float t = (float)i / _horizontalGridLines;
                float y = _plotRect.yMin + t * _plotRect.height;
                AddLine(vh, new Vector2(_plotRect.xMin, y),
                    new Vector2(_plotRect.xMax, y), gridColor, _gridLineWidth);
            }

            // Vertical grid lines (every 30 min)
            float currentTime = GetCurrentTime();
            float windowStart = Mathf.Max(0f, currentTime - _timeWindowHours);
            float firstGridTime = Mathf.Ceil(windowStart * 2f) / 2f; // Round up to nearest 0.5 hr
            for (float gridTime = firstGridTime; gridTime <= currentTime; gridTime += 0.5f)
            {
                float xNorm = (gridTime - windowStart) / _timeWindowHours;
                float x = _plotRect.xMin + xNorm * _plotRect.width;
                AddLine(vh, new Vector2(x, _plotRect.yMin),
                    new Vector2(x, _plotRect.yMax), gridColor, _gridLineWidth);
            }
        }

        void DrawReferenceLines(VertexHelper vh)
        {
            for (int r = 0; r < _referenceLines.Count; r++)
            {
                var refLine = _referenceLines[r];
                float yNorm = (_cachedYMax - _cachedYMin) > 0.001f
                    ? (refLine.Value - _cachedYMin) / (_cachedYMax - _cachedYMin)
                    : 0.5f;
                if (yNorm < 0f || yNorm > 1f) continue;

                float y = _plotRect.yMin + yNorm * _plotRect.height;
                Color c = refLine.LineColor;
                c.a = 0.6f;
                AddDashedLine(vh, new Vector2(_plotRect.xMin, y),
                    new Vector2(_plotRect.xMax, y), c, _gridLineWidth);
            }
        }

        void DrawTraces(VertexHelper vh)
        {
            float currentTime = GetCurrentTime();
            float windowStart = Mathf.Max(0f, currentTime - _timeWindowHours);
            float yRange = _cachedYMax - _cachedYMin;
            if (yRange < 0.001f) yRange = 1f;

            for (int t = 0; t < _traces.Count; t++)
            {
                var trace = _traces[t];
                if (!trace.Visible || trace.DataBuffer == null || trace.TimeBuffer == null)
                    continue;

                int count = Mathf.Min(trace.DataBuffer.Count, trace.TimeBuffer.Count);
                if (count < 2) continue;

                // Find first visible sample
                int startIdx = 0;
                for (int i = 0; i < count; i++)
                {
                    if (trace.TimeBuffer[i] >= windowStart)
                    {
                        startIdx = Mathf.Max(0, i - 1);
                        break;
                    }
                }

                // Draw line segments
                Vector2 prevPoint = Vector2.zero;
                bool hasPrev = false;

                for (int i = startIdx; i < count; i++)
                {
                    float time = trace.TimeBuffer[i];
                    if (time < windowStart) continue;
                    if (time > currentTime) break;

                    float val = trace.DataBuffer[i];
                    if (float.IsNaN(val) || float.IsInfinity(val)) { hasPrev = false; continue; }

                    float xNorm = (time - windowStart) / _timeWindowHours;
                    float yNorm = (val - _cachedYMin) / yRange;
                    yNorm = Mathf.Clamp01(yNorm);

                    Vector2 point = new Vector2(
                        _plotRect.xMin + xNorm * _plotRect.width,
                        _plotRect.yMin + yNorm * _plotRect.height);

                    if (hasPrev)
                    {
                        AddLine(vh, prevPoint, point, trace.TraceColor, _lineWidth);
                    }

                    prevPoint = point;
                    hasPrev = true;
                }
            }
        }

        float GetCurrentTime()
        {
            float max = 0f;
            for (int t = 0; t < _traces.Count; t++)
            {
                var trace = _traces[t];
                if (trace.TimeBuffer != null && trace.TimeBuffer.Count > 0)
                    max = Mathf.Max(max, trace.TimeBuffer[trace.TimeBuffer.Count - 1]);
            }
            return max;
        }

        // ====================================================================
        // LINE RENDERING PRIMITIVES
        // ====================================================================

        void AddLine(VertexHelper vh, Vector2 a, Vector2 b, Color color, float width)
        {
            float halfWidth = width * 0.5f;
            Vector2 dir = (b - a).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x) * halfWidth;

            int idx = vh.currentVertCount;
            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;

            vert.position = a - perp; vh.AddVert(vert);
            vert.position = a + perp; vh.AddVert(vert);
            vert.position = b + perp; vh.AddVert(vert);
            vert.position = b - perp; vh.AddVert(vert);

            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 2, idx + 3);
        }

        void AddDashedLine(VertexHelper vh, Vector2 a, Vector2 b, Color color, float width)
        {
            float totalLen = Vector2.Distance(a, b);
            float dashLen = 6f;
            float gapLen = 4f;
            Vector2 dir = (b - a).normalized;

            float pos = 0f;
            while (pos < totalLen)
            {
                float end = Mathf.Min(pos + dashLen, totalLen);
                AddLine(vh, a + dir * pos, a + dir * end, color, width);
                pos = end + gapLen;
            }
        }

        // ====================================================================
        // AXIS LABELS (updated via TMP text objects, not mesh)
        // ====================================================================

        void UpdateAxisLabels()
        {
            // Y-axis labels
            if (_yAxisLabels != null)
            {
                float range = _cachedYMax - _cachedYMin;
                for (int i = 0; i < _yAxisLabels.Length && i <= _horizontalGridLines; i++)
                {
                    float t = (float)i / _horizontalGridLines;
                    float val = _cachedYMin + t * range;
                    _yAxisLabels[i].text = val.ToString(range > 100 ? "F0" : "F1");
                }
            }

            // X-axis labels
            if (_xAxisLabels != null)
            {
                float currentTime = GetCurrentTime();
                float windowStart = Mathf.Max(0f, currentTime - _timeWindowHours);
                for (int i = 0; i < _xAxisLabels.Length; i++)
                {
                    float t = (float)i / Mathf.Max(1, _xAxisLabels.Length - 1);
                    float timeVal = windowStart + t * _timeWindowHours;
                    int hours = (int)timeVal;
                    int minutes = (int)((timeVal - hours) * 60f);
                    _xAxisLabels[i].text = $"{hours}:{minutes:D2}";
                }
            }
        }

        void UpdateLegendValues()
        {
            if (_legendLabels == null) return;
            for (int i = 0; i < _legendLabels.Length && i < _traces.Count; i++)
            {
                var trace = _traces[i];
                _legendLabels[i].text = $"{trace.Label}: {trace.CurrentValue.ToString(trace.Format)}{trace.Unit}";
                _legendLabels[i].color = trace.TraceColor;
            }
        }

        // ====================================================================
        // INTERNAL LABEL SETUP (called by factory)
        // ====================================================================

        internal void SetupLabels(TextMeshProUGUI titleText,
            TextMeshProUGUI[] yAxisLabels, TextMeshProUGUI[] xAxisLabels,
            TextMeshProUGUI[] legendLabels, Image[] legendSwatches)
        {
            _titleText = titleText;
            _yAxisLabels = yAxisLabels;
            _xAxisLabels = xAxisLabels;
            _legendLabels = legendLabels;
            _legendSwatches = legendSwatches;

            if (_titleText != null) _titleText.text = _title;
        }

        // ====================================================================
        // FACTORY METHOD
        // ====================================================================

        /// <summary>
        /// Create a StripChart with title, Y-axis labels, X-axis labels, and legend area.
        /// </summary>
        public static StripChart Create(Transform parent, string title,
            float width = 400f, float height = 200f, int maxTraces = 6)
        {
            // Container
            GameObject container = new GameObject($"StripChart_{title}");
            container.transform.SetParent(parent, false);
            RectTransform containerRT = container.AddComponent<RectTransform>();
            containerRT.sizeDelta = new Vector2(width, height);

            // Background
            Image bg = container.AddComponent<Image>();
            bg.color = ValidationDashboardTheme.BackgroundGraph;
            bg.raycastTarget = false;

            // Chart graphic (the actual mesh renderer)
            GameObject chartGO = new GameObject("ChartMesh");
            chartGO.transform.SetParent(container.transform, false);
            chartGO.AddComponent<CanvasRenderer>();
            RectTransform chartRT = chartGO.AddComponent<RectTransform>();
            chartRT.anchorMin = Vector2.zero;
            chartRT.anchorMax = Vector2.one;
            chartRT.offsetMin = Vector2.zero;
            chartRT.offsetMax = Vector2.zero;

            StripChart chart = chartGO.AddComponent<StripChart>();
            chart.color = Color.clear; // Base color unused — traces use their own colors
            chart._title = title;

            // Title text
            TextMeshProUGUI titleTMP = CreateLabel(container.transform, title, 12f,
                TextAlignmentOptions.TopLeft, ValidationDashboardTheme.TextPrimary);
            RectTransform titleRT = titleTMP.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.offsetMin = new Vector2(4f, -20f);
            titleRT.offsetMax = new Vector2(-4f, -2f);

            // Y-axis labels (bottom to top)
            int yLabelCount = 5;
            TextMeshProUGUI[] yLabels = new TextMeshProUGUI[yLabelCount];
            for (int i = 0; i < yLabelCount; i++)
            {
                float t = (float)i / (yLabelCount - 1);
                yLabels[i] = CreateLabel(container.transform, "0", 9f,
                    TextAlignmentOptions.MidlineRight, ValidationDashboardTheme.TextSecondary);
                RectTransform yRT = yLabels[i].GetComponent<RectTransform>();
                float yPos = 20f + t * (height - 44f);
                yRT.anchorMin = new Vector2(0f, 0f);
                yRT.anchorMax = new Vector2(0f, 0f);
                yRT.anchoredPosition = new Vector2(24f, yPos);
                yRT.sizeDelta = new Vector2(44f, 14f);
            }

            // X-axis labels
            int xLabelCount = 5;
            TextMeshProUGUI[] xLabels = new TextMeshProUGUI[xLabelCount];
            for (int i = 0; i < xLabelCount; i++)
            {
                float t = (float)i / (xLabelCount - 1);
                xLabels[i] = CreateLabel(container.transform, "0:00", 9f,
                    TextAlignmentOptions.Top, ValidationDashboardTheme.TextSecondary);
                RectTransform xRT = xLabels[i].GetComponent<RectTransform>();
                float xPos = 52f + t * (width - 60f);
                xRT.anchorMin = new Vector2(0f, 0f);
                xRT.anchorMax = new Vector2(0f, 0f);
                xRT.anchoredPosition = new Vector2(xPos, 6f);
                xRT.sizeDelta = new Vector2(40f, 14f);
            }

            // Legend labels (top-right area)
            TextMeshProUGUI[] legendLabels = new TextMeshProUGUI[maxTraces];
            Image[] legendSwatches = new Image[maxTraces];
            for (int i = 0; i < maxTraces; i++)
            {
                legendLabels[i] = CreateLabel(container.transform, "", 9f,
                    TextAlignmentOptions.MidlineRight, ValidationDashboardTheme.TextSecondary);
                RectTransform legRT = legendLabels[i].GetComponent<RectTransform>();
                legRT.anchorMin = new Vector2(1f, 1f);
                legRT.anchorMax = new Vector2(1f, 1f);
                legRT.anchoredPosition = new Vector2(-8f, -6f - i * 13f);
                legRT.sizeDelta = new Vector2(180f, 12f);
            }

            chart.SetupLabels(titleTMP, yLabels, xLabels, legendLabels, legendSwatches);
            return chart;
        }

        static TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize,
            TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
