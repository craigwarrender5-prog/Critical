// ============================================================================
// CRITICAL: Master the Atom - Mini Trends Panel
// MiniTrendsPanel.cs - Always-Visible Strip Charts
// ============================================================================
//
// PURPOSE:
//   Displays 8 mini strip charts on the right edge of the dashboard that
//   are always visible regardless of which tab is selected. These provide
//   continuous visual feedback of the most critical trending parameters.
//
// DISPLAYED TRENDS (per IP-0030):
//   1. RCS Pressure (pressHistory)
//   2. PZR Level (pzrLevelHistory)
//   3. Tavg (tempHistory)
//   4. Heater Demand (heaterPIDOutputHistory)
//   5. Spray Flow (sprayFlowHistory)
//   6. Charging Flow (chargingHistory)
//   7. Letdown Flow (letdownHistory)
//   8. Mass Error (new buffer or computed)
//
// LAYOUT:
//   ┌─────────────────┐
//   │ RCS PRESS  ▲▼   │
//   │ ═══════════════ │
//   ├─────────────────┤
//   │ PZR LEVEL  ▲▼   │
//   │ ═══════════════ │
//   ├─────────────────┤
//   │     ...etc      │
//   └─────────────────┘
//
// VERSION: 1.0.0
// DATE: 2026-02-16
// IP: IP-0031 Stage 1
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Panel containing 8 always-visible mini trend strip charts.
    /// </summary>
    public class MiniTrendsPanel : ValidationPanelBase
    {
        // ====================================================================
        // PANEL IDENTITY
        // ====================================================================

        public override string PanelName => "MiniTrendsPanel";
        public override int TabIndex => -1;  // Not associated with any tab
        public override bool AlwaysVisible => true;

        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================

        [Header("Layout")]
        [Tooltip("Width of the mini trends panel")]
        [SerializeField] private float panelWidth = 200f;

        [Tooltip("Height of each mini trend strip")]
        [SerializeField] private float stripHeight = 50f;

        [Tooltip("Spacing between strips")]
        [SerializeField] private float stripSpacing = 4f;

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        private List<MiniTrendStrip> _strips = new List<MiniTrendStrip>();

        // Trend definitions
        private static readonly TrendDefinition[] TREND_DEFINITIONS = new TrendDefinition[]
        {
            new TrendDefinition
            {
                Label = "RCS PRESS",
                Unit = "psia",
                MinValue = 0f,
                MaxValue = 2500f,
                HistoryField = "pressHistory",
                TraceColor = ValidationDashboardTheme.Trace1,
                WarnLow = 300f,
                WarnHigh = 2300f,
                AlarmLow = 200f,
                AlarmHigh = 2400f
            },
            new TrendDefinition
            {
                Label = "PZR LEVEL",
                Unit = "%",
                MinValue = 0f,
                MaxValue = 100f,
                HistoryField = "pzrLevelHistory",
                TraceColor = ValidationDashboardTheme.Trace4,
                WarnLow = 15f,
                WarnHigh = 85f,
                AlarmLow = 10f,
                AlarmHigh = 92f
            },
            new TrendDefinition
            {
                Label = "T-AVG",
                Unit = "°F",
                MinValue = 50f,
                MaxValue = 600f,
                HistoryField = "tempHistory",
                TraceColor = ValidationDashboardTheme.Trace1,
                WarnLow = 0f,
                WarnHigh = 560f,
                AlarmLow = 0f,
                AlarmHigh = 575f
            },
            new TrendDefinition
            {
                Label = "HTR DEMAND",
                Unit = "%",
                MinValue = 0f,
                MaxValue = 100f,
                HistoryField = "heaterPIDOutputHistory",
                TraceColor = ValidationDashboardTheme.Trace2,
                WarnLow = -1f,
                WarnHigh = 101f,
                AlarmLow = -1f,
                AlarmHigh = 101f
            },
            new TrendDefinition
            {
                Label = "SPRAY FLOW",
                Unit = "GPM",
                MinValue = 0f,
                MaxValue = 500f,
                HistoryField = "sprayFlowHistory",
                TraceColor = ValidationDashboardTheme.Trace3,
                WarnLow = -1f,
                WarnHigh = 450f,
                AlarmLow = -1f,
                AlarmHigh = 480f
            },
            new TrendDefinition
            {
                Label = "CHARGING",
                Unit = "GPM",
                MinValue = 0f,
                MaxValue = 150f,
                HistoryField = "chargingHistory",
                TraceColor = ValidationDashboardTheme.Trace6,
                WarnLow = -1f,
                WarnHigh = 130f,
                AlarmLow = -1f,
                AlarmHigh = 140f
            },
            new TrendDefinition
            {
                Label = "LETDOWN",
                Unit = "GPM",
                MinValue = 0f,
                MaxValue = 150f,
                HistoryField = "letdownHistory",
                TraceColor = ValidationDashboardTheme.AccentOrange,
                WarnLow = -1f,
                WarnHigh = 130f,
                AlarmLow = -1f,
                AlarmHigh = 140f
            },
            new TrendDefinition
            {
                Label = "MASS ERR",
                Unit = "lbm",
                MinValue = -100f,
                MaxValue = 100f,
                HistoryField = "massError",  // Direct value, not history
                TraceColor = ValidationDashboardTheme.TripMagenta,
                WarnLow = -50f,
                WarnHigh = 50f,
                AlarmLow = -80f,
                AlarmHigh = 80f
            }
        };

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        protected override void OnInitialize()
        {
            BuildStrips();
        }

        private void BuildStrips()
        {
            _strips.Clear();

            // Get or create vertical layout container
            VerticalLayoutGroup layout = GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                layout.spacing = stripSpacing;
                layout.padding = new RectOffset(4, 4, 4, 4);
            }

            // Create strip for each trend
            foreach (var def in TREND_DEFINITIONS)
            {
                MiniTrendStrip strip = CreateStrip(def);
                _strips.Add(strip);
            }
        }

        private MiniTrendStrip CreateStrip(TrendDefinition def)
        {
            // Container
            GameObject stripGO = new GameObject($"Strip_{def.Label.Replace(" ", "")}");
            stripGO.transform.SetParent(transform, false);

            RectTransform rt = stripGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(panelWidth - 8, stripHeight);

            LayoutElement le = stripGO.AddComponent<LayoutElement>();
            le.preferredHeight = stripHeight;
            le.flexibleWidth = 1f;

            // Background
            Image bg = stripGO.AddComponent<Image>();
            bg.color = ValidationDashboardTheme.BackgroundGraph;

            // Label (top-left)
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(stripGO.transform, false);

            RectTransform labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0, 1);
            labelRT.anchorMax = new Vector2(0.6f, 1);
            labelRT.pivot = new Vector2(0, 1);
            labelRT.anchoredPosition = new Vector2(4, -2);
            labelRT.sizeDelta = new Vector2(0, 12);

            TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = def.Label;
            labelText.fontSize = 9;
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.TopLeft;
            labelText.color = ValidationDashboardTheme.TextSecondary;

            // Value (top-right)
            GameObject valueGO = new GameObject("Value");
            valueGO.transform.SetParent(stripGO.transform, false);

            RectTransform valueRT = valueGO.AddComponent<RectTransform>();
            valueRT.anchorMin = new Vector2(0.6f, 1);
            valueRT.anchorMax = new Vector2(1, 1);
            valueRT.pivot = new Vector2(1, 1);
            valueRT.anchoredPosition = new Vector2(-4, -2);
            valueRT.sizeDelta = new Vector2(0, 12);

            TextMeshProUGUI valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.text = "---";
            valueText.fontSize = 10;
            valueText.fontStyle = FontStyles.Bold;
            valueText.alignment = TextAlignmentOptions.TopRight;
            valueText.color = ValidationDashboardTheme.TextPrimary;

            // Chart area (using RawImage for texture-based rendering)
            GameObject chartGO = new GameObject("Chart");
            chartGO.transform.SetParent(stripGO.transform, false);

            RectTransform chartRT = chartGO.AddComponent<RectTransform>();
            chartRT.anchorMin = new Vector2(0, 0);
            chartRT.anchorMax = new Vector2(1, 1);
            chartRT.offsetMin = new Vector2(2, 2);
            chartRT.offsetMax = new Vector2(-2, -14);

            RawImage chartImage = chartGO.AddComponent<RawImage>();
            chartImage.color = Color.white;

            // Create the strip data holder
            MiniTrendStrip strip = stripGO.AddComponent<MiniTrendStrip>();
            strip.Initialize(def, labelText, valueText, chartImage);

            return strip;
        }

        // ====================================================================
        // DATA UPDATE
        // ====================================================================

        protected override void OnUpdateData()
        {
            if (Engine == null) return;

            // Update each strip with current engine data
            foreach (var strip in _strips)
            {
                strip.UpdateFromEngine(Engine);
            }
        }

        // ====================================================================
        // VISUAL UPDATE
        // ====================================================================

        protected override void OnUpdateVisuals()
        {
            // Each strip handles its own visual updates
            foreach (var strip in _strips)
            {
                strip.RefreshVisuals();
            }
        }

        // ====================================================================
        // STATIC BUILDER
        // ====================================================================

        /// <summary>
        /// Creates the mini trends panel programmatically.
        /// </summary>
        public static MiniTrendsPanel CreateMiniTrendsPanel(Transform parent)
        {
            GameObject panelGO = new GameObject("MiniTrendsPanel");
            panelGO.transform.SetParent(parent, false);

            RectTransform rt = panelGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(200f, 0);
            rt.offsetMin = new Vector2(-200f, ValidationDashboardTheme.HeaderBarHeight + ValidationDashboardTheme.TabBarHeight + 8);
            rt.offsetMax = new Vector2(0, -8);

            // Background
            Image bg = panelGO.AddComponent<Image>();
            bg.color = ValidationDashboardTheme.BackgroundPanel;

            // Add panel component
            MiniTrendsPanel panel = panelGO.AddComponent<MiniTrendsPanel>();

            return panel;
        }

        // ====================================================================
        // NESTED TYPES
        // ====================================================================

        /// <summary>
        /// Definition for a mini trend strip chart.
        /// </summary>
        [System.Serializable]
        public struct TrendDefinition
        {
            public string Label;
            public string Unit;
            public float MinValue;
            public float MaxValue;
            public string HistoryField;
            public Color TraceColor;
            public float WarnLow;
            public float WarnHigh;
            public float AlarmLow;
            public float AlarmHigh;
        }
    }

    /// <summary>
    /// Individual mini trend strip chart component.
    /// </summary>
    public class MiniTrendStrip : MonoBehaviour
    {
        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        private MiniTrendsPanel.TrendDefinition _definition;
        private TextMeshProUGUI _labelText;
        private TextMeshProUGUI _valueText;
        private RawImage _chartImage;
        private Texture2D _chartTexture;

        private float _currentValue;
        private float[] _historyBuffer;
        private int _historyLength = 120;  // ~5 minutes at 2-second intervals
        private int _historyIndex;
        private bool _historyFull;

        private Color32[] _pixelBuffer;
        private int _textureWidth = 180;
        private int _textureHeight = 30;

        private bool _isInitialized;
        private string _cachedValueString;
        private Color _cachedValueColor;

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        public void Initialize(MiniTrendsPanel.TrendDefinition definition,
            TextMeshProUGUI labelText, TextMeshProUGUI valueText, RawImage chartImage)
        {
            _definition = definition;
            _labelText = labelText;
            _valueText = valueText;
            _chartImage = chartImage;

            // Create history buffer
            _historyBuffer = new float[_historyLength];
            _historyIndex = 0;
            _historyFull = false;

            // Create texture for chart
            _chartTexture = new Texture2D(_textureWidth, _textureHeight, TextureFormat.RGBA32, false);
            _chartTexture.filterMode = FilterMode.Point;
            _chartTexture.wrapMode = TextureWrapMode.Clamp;

            // Initialize pixel buffer
            _pixelBuffer = new Color32[_textureWidth * _textureHeight];
            ClearTexture();

            if (_chartImage != null)
            {
                _chartImage.texture = _chartTexture;
            }

            _isInitialized = true;
        }

        // ====================================================================
        // DATA UPDATE
        // ====================================================================

        public void UpdateFromEngine(HeatupSimEngine engine)
        {
            if (!_isInitialized || engine == null) return;

            // Get current value based on field name
            float value = GetValueFromEngine(engine, _definition.HistoryField);
            _currentValue = value;

            // Add to history buffer
            AddToHistory(value);

            // Update value string
            _cachedValueString = $"{value:F1} {_definition.Unit}";

            // Determine color based on thresholds
            _cachedValueColor = GetThresholdColor(value);
        }

        private float GetValueFromEngine(HeatupSimEngine engine, string fieldName)
        {
            // Map field names to engine values
            switch (fieldName)
            {
                case "pressHistory":
                    return engine.pressure;
                case "pzrLevelHistory":
                    return engine.pzrLevel;
                case "tempHistory":
                    return engine.T_avg;
                case "heaterPIDOutputHistory":
                    return engine.heaterPIDOutput * 100f;  // Convert to %
                case "sprayFlowHistory":
                    return engine.sprayFlow_GPM;
                case "chargingHistory":
                    return engine.chargingFlow;
                case "letdownHistory":
                    return engine.letdownFlow;
                case "massError":
                    return engine.massError_lbm;
                default:
                    return 0f;
            }
        }

        private void AddToHistory(float value)
        {
            _historyBuffer[_historyIndex] = value;
            _historyIndex = (_historyIndex + 1) % _historyLength;
            if (_historyIndex == 0) _historyFull = true;
        }

        private Color GetThresholdColor(float value)
        {
            if (value < _definition.AlarmLow || value > _definition.AlarmHigh)
                return ValidationDashboardTheme.AlarmRed;
            if (value < _definition.WarnLow || value > _definition.WarnHigh)
                return ValidationDashboardTheme.WarningAmber;
            return ValidationDashboardTheme.NormalGreen;
        }

        // ====================================================================
        // VISUAL UPDATE
        // ====================================================================

        public void RefreshVisuals()
        {
            if (!_isInitialized) return;

            // Update value text
            if (_valueText != null)
            {
                _valueText.text = _cachedValueString;
                _valueText.color = _cachedValueColor;
            }

            // Redraw chart
            RedrawChart();
        }

        private void RedrawChart()
        {
            // Clear to background color
            Color32 bgColor = new Color32(10, 12, 18, 255);
            for (int i = 0; i < _pixelBuffer.Length; i++)
            {
                _pixelBuffer[i] = bgColor;
            }

            // Draw grid lines (subtle)
            Color32 gridColor = new Color32(30, 35, 45, 255);
            int midY = _textureHeight / 2;
            for (int x = 0; x < _textureWidth; x++)
            {
                _pixelBuffer[midY * _textureWidth + x] = gridColor;
            }

            // Draw trace
            Color32 traceColor = new Color32(
                (byte)(_definition.TraceColor.r * 255),
                (byte)(_definition.TraceColor.g * 255),
                (byte)(_definition.TraceColor.b * 255),
                255);

            int sampleCount = _historyFull ? _historyLength : _historyIndex;
            if (sampleCount < 2) return;

            float range = _definition.MaxValue - _definition.MinValue;
            if (range <= 0) range = 1f;

            int prevY = -1;
            for (int i = 0; i < sampleCount && i < _textureWidth; i++)
            {
                // Calculate buffer index (oldest to newest)
                int bufferIdx;
                if (_historyFull)
                {
                    bufferIdx = (_historyIndex + i) % _historyLength;
                }
                else
                {
                    bufferIdx = i;
                }

                float value = _historyBuffer[bufferIdx];
                float normalized = (value - _definition.MinValue) / range;
                normalized = Mathf.Clamp01(normalized);

                int x = (_textureWidth - sampleCount) + i;
                if (x < 0) continue;

                int y = Mathf.RoundToInt(normalized * (_textureHeight - 1));
                y = Mathf.Clamp(y, 0, _textureHeight - 1);

                // Draw vertical line from prev to current for continuous trace
                if (prevY >= 0 && prevY != y)
                {
                    int minY = Mathf.Min(prevY, y);
                    int maxY = Mathf.Max(prevY, y);
                    for (int lineY = minY; lineY <= maxY; lineY++)
                    {
                        _pixelBuffer[lineY * _textureWidth + x] = traceColor;
                    }
                }
                else
                {
                    _pixelBuffer[y * _textureWidth + x] = traceColor;
                }

                prevY = y;
            }

            // Apply pixels to texture
            _chartTexture.SetPixels32(_pixelBuffer);
            _chartTexture.Apply();
        }

        private void ClearTexture()
        {
            Color32 bgColor = new Color32(10, 12, 18, 255);
            for (int i = 0; i < _pixelBuffer.Length; i++)
            {
                _pixelBuffer[i] = bgColor;
            }
            _chartTexture.SetPixels32(_pixelBuffer);
            _chartTexture.Apply();
        }

        private void OnDestroy()
        {
            if (_chartTexture != null)
            {
                Destroy(_chartTexture);
            }
        }
    }
}
