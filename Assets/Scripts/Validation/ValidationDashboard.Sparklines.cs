// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// ValidationDashboard.Sparklines.cs - High-Performance Sparkline Renderer
// ============================================================================
//
// PURPOSE:
//   Renders mini trend graphs (sparklines) for the Overview tab's Trends column.
//   Each sparkline shows ~1 hour of historical data in a compact form.
//
// PERFORMANCE CRITICAL:
//   - Fixed-size circular buffer (no allocations during runtime)
//   - Texture2D created once, updated in place via SetPixels32
//   - Preallocated Color32 pixel buffer
//   - Apply(false) to skip mipmap regeneration
//   - Target: < 0.3ms for all 8 sparklines
//
// ARCHITECTURE:
//   - SparklineRenderer: Individual sparkline with buffer and texture
//   - SparklineManager: Coordinates all 8 sparklines, handles updates
//   - Integration: Called from OverviewTab.DrawTrendsColumn()
//
// REFERENCE:
//   Nuclear control room strip chart conventions
//   Time base: ~1 hour visible (at 10 Hz = 36000 samples, downsampled to 256)
//
// GOLD STANDARD: Yes
// VERSION: 1.0.0.0
// ============================================================================

using UnityEngine;
using System;

namespace Critical.Validation
{
    /// <summary>
    /// Individual sparkline renderer with circular buffer and cached texture.
    /// </summary>
    public class SparklineRenderer
    {
        // ====================================================================
        // CONFIGURATION
        // ====================================================================

        /// <summary>Buffer size (number of data points stored)</summary>
        public const int BUFFER_SIZE = 256;

        /// <summary>Default sparkline width in pixels</summary>
        public const int DEFAULT_WIDTH = 200;

        /// <summary>Default sparkline height in pixels</summary>
        public const int DEFAULT_HEIGHT = 32;

        // ====================================================================
        // STATE
        // ====================================================================

        private readonly float[] _buffer;
        private int _head;
        private int _count;

        private readonly Texture2D _texture;
        private readonly Color32[] _pixels;
        private readonly int _width;
        private readonly int _height;

        // Display range
        private float _minValue;
        private float _maxValue;
        private bool _autoRange;

        // Colors
        private Color32 _bgColor;
        private Color32 _lineColor;
        private Color32 _gridColor;

        // Cached values
        private float _lastValue;
        private float _minSeen;
        private float _maxSeen;

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        /// <summary>
        /// Create a new sparkline renderer.
        /// </summary>
        /// <param name="width">Texture width in pixels</param>
        /// <param name="height">Texture height in pixels</param>
        /// <param name="minValue">Minimum display value (or NaN for auto-range)</param>
        /// <param name="maxValue">Maximum display value (or NaN for auto-range)</param>
        public SparklineRenderer(int width = DEFAULT_WIDTH, int height = DEFAULT_HEIGHT,
            float minValue = float.NaN, float maxValue = float.NaN)
        {
            _width = width;
            _height = height;
            _buffer = new float[BUFFER_SIZE];
            _head = 0;
            _count = 0;

            // Create texture (once, never reallocated)
            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _texture.filterMode = FilterMode.Point;
            _texture.wrapMode = TextureWrapMode.Clamp;
            _texture.hideFlags = HideFlags.DontSave;

            // Preallocate pixel buffer
            _pixels = new Color32[width * height];

            // Range settings
            _autoRange = float.IsNaN(minValue) || float.IsNaN(maxValue);
            _minValue = _autoRange ? 0f : minValue;
            _maxValue = _autoRange ? 100f : maxValue;
            _minSeen = float.MaxValue;
            _maxSeen = float.MinValue;

            // Default colors (can be customized)
            _bgColor = new Color32(15, 17, 22, 255);      // Dark background
            _lineColor = new Color32(0, 255, 136, 255);   // Green line
            _gridColor = new Color32(40, 45, 55, 255);    // Subtle grid

            // Initialize texture with background
            ClearTexture();
        }

        // ====================================================================
        // DATA INPUT
        // ====================================================================

        /// <summary>
        /// Push a new value into the circular buffer.
        /// Call this at snapshot rate (10 Hz).
        /// </summary>
        public void Push(float value)
        {
            _buffer[_head] = value;
            _head = (_head + 1) % BUFFER_SIZE;
            if (_count < BUFFER_SIZE) _count++;

            _lastValue = value;

            // Track range for auto-scaling
            if (value < _minSeen) _minSeen = value;
            if (value > _maxSeen) _maxSeen = value;
        }

        /// <summary>
        /// Get the most recent value.
        /// </summary>
        public float LastValue => _lastValue;

        /// <summary>
        /// Get the number of valid samples in buffer.
        /// </summary>
        public int Count => _count;

        // ====================================================================
        // RENDERING
        // ====================================================================

        /// <summary>
        /// Update the texture with current buffer data.
        /// Call this before drawing, typically at 10 Hz.
        /// </summary>
        public void UpdateTexture()
        {
            // Clear to background
            ClearPixels(_bgColor);

            if (_count < 2) 
            {
                ApplyTexture();
                return;
            }

            // Calculate effective range
            float rangeMin, rangeMax;
            if (_autoRange && _count > 10)
            {
                // Auto-range with 10% padding
                float range = _maxSeen - _minSeen;
                float padding = range * 0.1f;
                if (padding < 1f) padding = 1f;
                rangeMin = _minSeen - padding;
                rangeMax = _maxSeen + padding;
            }
            else
            {
                rangeMin = _minValue;
                rangeMax = _maxValue;
            }

            float rangeSpan = rangeMax - rangeMin;
            if (rangeSpan < 0.001f) rangeSpan = 1f;

            // Draw horizontal grid lines (25%, 50%, 75%)
            DrawHorizontalLine(_height / 4, _gridColor);
            DrawHorizontalLine(_height / 2, _gridColor);
            DrawHorizontalLine(_height * 3 / 4, _gridColor);

            // Draw sparkline
            int startIdx = (_head - _count + BUFFER_SIZE) % BUFFER_SIZE;
            int prevY = -1;

            for (int i = 0; i < _count; i++)
            {
                int bufIdx = (startIdx + i) % BUFFER_SIZE;
                float value = _buffer[bufIdx];

                // Map buffer index to x pixel
                int x = (i * (_width - 1)) / (BUFFER_SIZE - 1);

                // Map value to y pixel (inverted: 0 at bottom)
                float normalized = (value - rangeMin) / rangeSpan;
                normalized = Mathf.Clamp01(normalized);
                int y = (int)(normalized * (_height - 1));

                // Draw vertical line from prevY to y for continuity
                if (prevY >= 0 && x > 0)
                {
                    DrawVerticalSegment(x, prevY, y, _lineColor);
                }

                // Draw point
                SetPixel(x, y, _lineColor);
                prevY = y;
            }

            ApplyTexture();
        }

        /// <summary>
        /// Draw the sparkline texture at the specified rect.
        /// Call this from OnGUI.
        /// </summary>
        public void Draw(Rect rect)
        {
            GUI.DrawTexture(rect, _texture);
        }

        /// <summary>
        /// Draw the sparkline with label and current value.
        /// </summary>
        public void Draw(Rect rect, string label, string valueFormat, string unit, 
            GUIStyle labelStyle, GUIStyle valueStyle, Color valueColor)
        {
            // Draw texture
            GUI.DrawTexture(rect, _texture);

            // Label (top-left)
            Rect labelRect = new Rect(rect.x + 2f, rect.y + 1f, rect.width * 0.4f, 12f);
            GUI.Label(labelRect, label, labelStyle);

            // Value (top-right)
            string valueStr = _lastValue.ToString(valueFormat) + " " + unit;
            Rect valueRect = new Rect(rect.x + rect.width * 0.5f, rect.y + 1f, 
                rect.width * 0.48f, 12f);
            GUI.contentColor = valueColor;
            GUI.Label(valueRect, valueStr, valueStyle);
            GUI.contentColor = Color.white;
        }

        // ====================================================================
        // CONFIGURATION
        // ====================================================================

        /// <summary>
        /// Set the display range.
        /// </summary>
        public void SetRange(float min, float max)
        {
            _minValue = min;
            _maxValue = max;
            _autoRange = false;
        }

        /// <summary>
        /// Enable auto-ranging based on observed data.
        /// </summary>
        public void EnableAutoRange()
        {
            _autoRange = true;
        }

        /// <summary>
        /// Set the line color.
        /// </summary>
        public void SetLineColor(Color color)
        {
            _lineColor = new Color32(
                (byte)(color.r * 255),
                (byte)(color.g * 255),
                (byte)(color.b * 255),
                255);
        }

        /// <summary>
        /// Reset the buffer and range tracking.
        /// </summary>
        public void Reset()
        {
            _head = 0;
            _count = 0;
            _minSeen = float.MaxValue;
            _maxSeen = float.MinValue;
            _lastValue = 0f;
            ClearTexture();
        }

        // ====================================================================
        // CLEANUP
        // ====================================================================

        /// <summary>
        /// Destroy the texture when done.
        /// </summary>
        public void Dispose()
        {
            if (_texture != null)
            {
                UnityEngine.Object.DestroyImmediate(_texture);
            }
        }

        // ====================================================================
        // PRIVATE HELPERS
        // ====================================================================

        private void ClearPixels(Color32 color)
        {
            for (int i = 0; i < _pixels.Length; i++)
            {
                _pixels[i] = color;
            }
        }

        private void ClearTexture()
        {
            ClearPixels(_bgColor);
            ApplyTexture();
        }

        private void ApplyTexture()
        {
            _texture.SetPixels32(_pixels);
            _texture.Apply(false); // false = don't rebuild mipmaps
        }

        private void SetPixel(int x, int y, Color32 color)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;
            _pixels[y * _width + x] = color;
        }

        private void DrawHorizontalLine(int y, Color32 color)
        {
            if (y < 0 || y >= _height) return;
            int offset = y * _width;
            for (int x = 0; x < _width; x++)
            {
                _pixels[offset + x] = color;
            }
        }

        private void DrawVerticalSegment(int x, int y1, int y2, Color32 color)
        {
            if (x < 0 || x >= _width) return;
            int minY = Mathf.Min(y1, y2);
            int maxY = Mathf.Max(y1, y2);
            minY = Mathf.Max(0, minY);
            maxY = Mathf.Min(_height - 1, maxY);

            for (int y = minY; y <= maxY; y++)
            {
                _pixels[y * _width + x] = color;
            }
        }
    }

    /// <summary>
    /// Manages all 8 sparklines for the Overview tab.
    /// </summary>
    public class SparklineManager
    {
        // ====================================================================
        // SPARKLINE INDICES
        // ====================================================================

        public const int IDX_RCS_PRESSURE = 0;
        public const int IDX_PZR_LEVEL = 1;
        public const int IDX_T_AVG = 2;
        public const int IDX_HEATUP_RATE = 3;
        public const int IDX_SUBCOOLING = 4;
        public const int IDX_NET_CVCS = 5;
        public const int IDX_SG_PRESSURE = 6;
        public const int IDX_NET_HEAT = 7;

        public const int SPARKLINE_COUNT = 8;

        // ====================================================================
        // STATE
        // ====================================================================

        private readonly SparklineRenderer[] _sparklines;
        private readonly string[] _labels;
        private readonly string[] _formats;
        private readonly string[] _units;
        private readonly Color[] _colors;

        private bool _initialized = false;

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public SparklineManager()
        {
            _sparklines = new SparklineRenderer[SPARKLINE_COUNT];
            _labels = new string[SPARKLINE_COUNT];
            _formats = new string[SPARKLINE_COUNT];
            _units = new string[SPARKLINE_COUNT];
            _colors = new Color[SPARKLINE_COUNT];
        }

        /// <summary>
        /// Initialize all sparklines with appropriate ranges and colors.
        /// </summary>
        public void Initialize(int width, int height)
        {
            if (_initialized) return;

            // RCS Pressure: 0-2500 psia
            _sparklines[IDX_RCS_PRESSURE] = new SparklineRenderer(width, height, 0f, 2500f);
            _sparklines[IDX_RCS_PRESSURE].SetLineColor(ValidationDashboard._cCyanInfo);
            _labels[IDX_RCS_PRESSURE] = "RCS PRESS";
            _formats[IDX_RCS_PRESSURE] = "F0";
            _units[IDX_RCS_PRESSURE] = "psia";
            _colors[IDX_RCS_PRESSURE] = ValidationDashboard._cCyanInfo;

            // PZR Level: 0-100%
            _sparklines[IDX_PZR_LEVEL] = new SparklineRenderer(width, height, 0f, 100f);
            _sparklines[IDX_PZR_LEVEL].SetLineColor(ValidationDashboard._cNormalGreen);
            _labels[IDX_PZR_LEVEL] = "PZR LEVEL";
            _formats[IDX_PZR_LEVEL] = "F1";
            _units[IDX_PZR_LEVEL] = "%";
            _colors[IDX_PZR_LEVEL] = ValidationDashboard._cNormalGreen;

            // T_avg: 50-600°F
            _sparklines[IDX_T_AVG] = new SparklineRenderer(width, height, 50f, 600f);
            _sparklines[IDX_T_AVG].SetLineColor(ValidationDashboard._cWarningAmber);
            _labels[IDX_T_AVG] = "T_AVG";
            _formats[IDX_T_AVG] = "F1";
            _units[IDX_T_AVG] = "°F";
            _colors[IDX_T_AVG] = ValidationDashboard._cWarningAmber;

            // Heatup Rate: 0-60 °F/hr
            _sparklines[IDX_HEATUP_RATE] = new SparklineRenderer(width, height, 0f, 60f);
            _sparklines[IDX_HEATUP_RATE].SetLineColor(ValidationDashboard._cWarningAmber);
            _labels[IDX_HEATUP_RATE] = "HEATUP";
            _formats[IDX_HEATUP_RATE] = "F1";
            _units[IDX_HEATUP_RATE] = "°F/hr";
            _colors[IDX_HEATUP_RATE] = ValidationDashboard._cWarningAmber;

            // Subcooling: 0-100°F
            _sparklines[IDX_SUBCOOLING] = new SparklineRenderer(width, height, 0f, 100f);
            _sparklines[IDX_SUBCOOLING].SetLineColor(ValidationDashboard._cNormalGreen);
            _labels[IDX_SUBCOOLING] = "SUBCOOL";
            _formats[IDX_SUBCOOLING] = "F1";
            _units[IDX_SUBCOOLING] = "°F";
            _colors[IDX_SUBCOOLING] = ValidationDashboard._cNormalGreen;

            // Net CVCS: -50 to +50 gpm
            _sparklines[IDX_NET_CVCS] = new SparklineRenderer(width, height, -50f, 50f);
            _sparklines[IDX_NET_CVCS].SetLineColor(ValidationDashboard._cCyanInfo);
            _labels[IDX_NET_CVCS] = "NET CVCS";
            _formats[IDX_NET_CVCS] = "+0.0;-0.0;0.0";
            _units[IDX_NET_CVCS] = "gpm";
            _colors[IDX_NET_CVCS] = ValidationDashboard._cCyanInfo;

            // SG Pressure: 0-1200 psia
            _sparklines[IDX_SG_PRESSURE] = new SparklineRenderer(width, height, 0f, 1200f);
            _sparklines[IDX_SG_PRESSURE].SetLineColor(ValidationDashboard._cCyanInfo);
            _labels[IDX_SG_PRESSURE] = "SG PRESS";
            _formats[IDX_SG_PRESSURE] = "F0";
            _units[IDX_SG_PRESSURE] = "psia";
            _colors[IDX_SG_PRESSURE] = ValidationDashboard._cCyanInfo;

            // Net Heat: auto-range
            _sparklines[IDX_NET_HEAT] = new SparklineRenderer(width, height);
            _sparklines[IDX_NET_HEAT].EnableAutoRange();
            _sparklines[IDX_NET_HEAT].SetLineColor(ValidationDashboard._cWarningAmber);
            _labels[IDX_NET_HEAT] = "NET HEAT";
            _formats[IDX_NET_HEAT] = "F2";
            _units[IDX_NET_HEAT] = "MW";
            _colors[IDX_NET_HEAT] = ValidationDashboard._cWarningAmber;

            _initialized = true;
        }

        /// <summary>
        /// Push new values from snapshot into all sparklines.
        /// Call this at snapshot rate (10 Hz).
        /// </summary>
        public void PushValues(DashboardSnapshot snapshot)
        {
            if (!_initialized || snapshot == null) return;

            _sparklines[IDX_RCS_PRESSURE].Push(snapshot.Pressure);
            _sparklines[IDX_PZR_LEVEL].Push(snapshot.PzrLevel);
            _sparklines[IDX_T_AVG].Push(snapshot.T_avg);
            _sparklines[IDX_HEATUP_RATE].Push(snapshot.HeatupRate);
            _sparklines[IDX_SUBCOOLING].Push(snapshot.Subcooling);

            float netCvcs = snapshot.ChargingFlow - snapshot.LetdownFlow;
            _sparklines[IDX_NET_CVCS].Push(netCvcs);

            _sparklines[IDX_SG_PRESSURE].Push(snapshot.SgSecondaryPressure);

            // Net heat = RCP heat + PZR heaters - SG transfer - RHR removal
            float netHeat = snapshot.RcpHeat + snapshot.PzrHeaterPower 
                          - snapshot.SgHeatTransfer - snapshot.RhrNetHeat;
            _sparklines[IDX_NET_HEAT].Push(netHeat);
        }

        /// <summary>
        /// Update all sparkline textures.
        /// Call this at display refresh rate.
        /// </summary>
        public void UpdateTextures()
        {
            if (!_initialized) return;

            for (int i = 0; i < SPARKLINE_COUNT; i++)
            {
                _sparklines[i].UpdateTexture();
            }
        }

        /// <summary>
        /// Draw a sparkline at the specified rect.
        /// </summary>
        public void Draw(int index, Rect rect, GUIStyle labelStyle, GUIStyle valueStyle)
        {
            if (!_initialized || index < 0 || index >= SPARKLINE_COUNT) return;

            _sparklines[index].Draw(rect, _labels[index], _formats[index], _units[index],
                labelStyle, valueStyle, _colors[index]);
        }

        /// <summary>
        /// Get a sparkline by index.
        /// </summary>
        public SparklineRenderer GetSparkline(int index)
        {
            if (!_initialized || index < 0 || index >= SPARKLINE_COUNT) return null;
            return _sparklines[index];
        }

        /// <summary>
        /// Dispose all sparklines.
        /// </summary>
        public void Dispose()
        {
            if (_sparklines != null)
            {
                for (int i = 0; i < SPARKLINE_COUNT; i++)
                {
                    _sparklines[i]?.Dispose();
                }
            }
            _initialized = false;
        }

        /// <summary>
        /// Is the manager initialized?
        /// </summary>
        public bool IsInitialized => _initialized;
    }
}
