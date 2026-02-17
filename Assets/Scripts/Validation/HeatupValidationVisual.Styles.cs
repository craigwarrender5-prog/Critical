// ============================================================================
// CRITICAL: Master the Atom - UI Component (Styles Partial)
// HeatupValidationVisual.Styles.cs - Colors, Fonts, GUIStyles, Gauge Renderer
// ============================================================================
//
// PURPOSE:
//   All visual styling for the Heatup Validation Dashboard. Centralises
//   colors, GUIStyle factories, layout constants, and the gauge arc
//   rendering utility. No other partial should create GUIStyles or define
//   colors â€” they reference fields declared here.
//
// REFERENCE:
//   Westinghouse 4-Loop PWR control room color conventions:
//     - Dark background (near-black with slight blue tint)
//     - Green for normal/safe parameters
//     - Amber/yellow for warnings and approach to limit
//     - Red for alarms and limit violations
//     - Cyan for informational / in-progress states
//     - White for text labels and digital readouts
//
//   Standard nuclear I&C gauge conventions:
//     - Arc gauges with colored bands (green normal, amber warn, red alarm)
//     - Digital readout below arc
//     - Units and label text
//
// ARCHITECTURE:
//   Partial class of HeatupValidationVisual. This file owns:
//     - All Color fields (_c* prefix)
//     - All GUIStyle fields (_*Style suffix)
//     - All Texture2D fields (_*Tex suffix)
//     - InitializeStyles() â€” called once from Start(), creates all styles
//     - DrawGaugeArc() â€” reusable arc gauge renderer
//     - DrawMiniBar() â€” horizontal bar gauge
//     - MakeTex() â€” 1Ã—1 solid color texture factory
//
// GOLD STANDARD: Yes
// ============================================================================

using UnityEngine;


namespace Critical.Validation
{

public partial class HeatupValidationVisual
{
    // ========================================================================
    // STYLE INITIALIZATION FLAG
    // ========================================================================

    private bool _stylesInitialized;

    // ========================================================================
    // COLOR PALETTE â€” Westinghouse Control Room Conventions
    // ========================================================================

    #region Colors

    // Background tones
    internal Color _cBgDark       = new Color(0.06f, 0.07f, 0.10f, 1f);    // Main background
    internal Color _cBgPanel      = new Color(0.09f, 0.10f, 0.14f, 0.95f); // Panel backgrounds
    internal Color _cBgHeader     = new Color(0.05f, 0.06f, 0.09f, 1f);    // Header strip
    internal Color _cBgGauge      = new Color(0.08f, 0.09f, 0.12f, 1f);    // Gauge background
    internal Color _cBgGraph      = new Color(0.04f, 0.05f, 0.07f, 1f);    // Graph plot area

    // Functional status colors
    internal Color _cNormalGreen   = new Color(0.18f, 0.82f, 0.25f, 1f);   // Normal / safe
    internal Color _cWarningAmber  = new Color(1.00f, 0.78f, 0.00f, 1f);   // Warning / approach
    internal Color _cAlarmRed      = new Color(1.00f, 0.18f, 0.18f, 1f);   // Alarm / violation
    internal Color _cTripMagenta   = new Color(1.00f, 0.00f, 0.80f, 1f);   // Trip / critical

    // Informational colors
    internal Color _cCyanInfo      = new Color(0.00f, 0.85f, 0.95f, 1f);   // Info / in-progress
    internal Color _cBlueAccent    = new Color(0.20f, 0.50f, 1.00f, 1f);   // Accent / active
    internal Color _cOrangeAccent  = new Color(1.00f, 0.55f, 0.10f, 1f);   // BRS / special

    // Text colors
    internal Color _cTextPrimary   = new Color(0.92f, 0.93f, 0.95f, 1f);   // Primary text (white)
    internal Color _cTextSecondary = new Color(0.55f, 0.58f, 0.65f, 1f);   // Dim labels
    internal Color _cTextDark      = new Color(0.15f, 0.16f, 0.20f, 1f);   // Dark text on bright bg

    // Gauge-specific colors
    internal Color _cGaugeArcBg    = new Color(0.15f, 0.16f, 0.20f, 1f);   // Unlit arc background
    internal Color _cGaugeNeedle   = new Color(1.00f, 1.00f, 1.00f, 0.95f);// Needle / pointer
    internal Color _cGaugeTick     = new Color(0.40f, 0.42f, 0.48f, 1f);   // Tick marks
    internal Color _cSectionHeader = new Color(0.12f, 0.14f, 0.20f, 1f);  // v0.9.4: Section header bg

    // Annunciator tile states
    internal Color _cAnnOff        = new Color(0.12f, 0.13f, 0.16f, 1f);   // Inactive tile
    internal Color _cAnnNormal     = new Color(0.10f, 0.35f, 0.12f, 1f);   // Normal (dim green)
    internal Color _cAnnWarning    = new Color(0.45f, 0.35f, 0.00f, 1f);   // Warning (dim amber)
    internal Color _cAnnAlarm      = new Color(0.50f, 0.08f, 0.08f, 1f);   // Alarm (dim red)

    // Graph trace colors (distinct for multi-trace plots)
    internal Color _cTrace1  = new Color(0.18f, 0.82f, 0.25f, 1f);  // Green (T_rcs / primary)
    internal Color _cTrace2  = new Color(1.00f, 0.40f, 0.20f, 1f);  // Orange-red (T_hot)
    internal Color _cTrace3  = new Color(0.30f, 0.60f, 1.00f, 1f);  // Blue (T_cold)
    internal Color _cTrace4  = new Color(1.00f, 0.78f, 0.00f, 1f);  // Amber (T_pzr)
    internal Color _cTrace5  = new Color(0.80f, 0.20f, 0.90f, 1f);  // Purple (T_sat)
    internal Color _cTrace6  = new Color(0.00f, 0.85f, 0.95f, 1f);  // Cyan (secondary)
    internal Color _cTraceGrid = new Color(0.20f, 0.22f, 0.28f, 1f);// Grid lines

    #endregion

    // ========================================================================
    // TEXTURES â€” Cached 1Ã—1 solid color textures for GUI.DrawTexture
    // ========================================================================

    #region Textures

    internal Texture2D _bgTex;
    internal Texture2D _panelTex;
    internal Texture2D _headerTex;
    internal Texture2D _gaugeBgTex;
    internal Texture2D _graphBgTex;
    internal Texture2D _whiteTex;
    internal Texture2D _greenTex;
    internal Texture2D _amberTex;
    internal Texture2D _redTex;
    internal Texture2D _cyanTex;
    internal Texture2D _sectionHeaderTex;  // v0.9.4: Cached section header texture
    internal Texture2D _blueAccentTex;     // v0.9.4: Cached blue accent texture
    internal Texture2D _orangeAccentTex;   // v0.9.4: Cached orange accent texture
    internal Texture2D _textSecondaryTex;  // v0.9.4: Cached secondary text texture
    // v0.9.4: Cached trace colors for graph legends
    internal Texture2D _trace1Tex;
    internal Texture2D _trace2Tex;
    internal Texture2D _trace3Tex;
    internal Texture2D _trace4Tex;
    internal Texture2D _trace5Tex;
    internal Texture2D _trace6Tex;
    internal Texture2D _traceGridTex;
    internal Texture2D _tileBorderInactiveTex;  // v0.9.4: For annunciator tile borders
    // v0.9.4: Annunciator tile backgrounds
    internal Texture2D _annOffTex;
    internal Texture2D _annNormalTex;
    internal Texture2D _annWarningTex;
    internal Texture2D _annAlarmTex;
    internal Texture2D _gaugeNeedleTex;  // v2.0.5: Cache gauge needle color (0.95 alpha white)

    #endregion

    // ========================================================================
    // GUI STYLES
    // ========================================================================

    #region Styles

    // Background box styles
    internal GUIStyle _headerBgStyle;
    internal GUIStyle _panelBgStyle;
    internal GUIStyle _gaugeBgStyle;
    internal GUIStyle _graphBgStyle;

    // Text styles
    internal GUIStyle _headerLabelStyle;
    internal GUIStyle _sectionHeaderStyle;
    internal GUIStyle _gaugeLabelStyle;
    internal GUIStyle _gaugeValueStyle;
    internal GUIStyle _gaugeUnitStyle;
    internal GUIStyle _statusLabelStyle;
    internal GUIStyle _statusValueStyle;
    internal GUIStyle _eventLogStyle;
    internal GUIStyle _tabStyle;

    // Button styles
    internal GUIStyle _timeAccelBtnStyle;
    internal GUIStyle _timeAccelActiveStyle;

    // Annunciator tile style
    internal GUIStyle _annTileStyle;

    // Graph label style
    internal GUIStyle _graphLabelStyle;
    internal GUIStyle _graphAxisStyle;
    // v0.9.4: Cached alternate alignment styles to prevent per-frame allocation
    internal GUIStyle _graphAxisStyleRight;   // MiddleLeft alignment for right Y-axis
    internal GUIStyle _graphAxisStyleCenter;  // UpperCenter for X-axis labels
    internal GUIStyle _graphLabelStyleCenter; // UpperCenter for X-axis title

    #endregion

    // ========================================================================
    // LAYOUT SIZING CONSTANTS
    // ========================================================================

    #region Layout Constants

    // Gauge group dimensions (inside scroll view)
    internal const float GAUGE_GROUP_HEADER_H = 20f;
    internal const float GAUGE_ROW_H = 68f;         // Height per gauge row
    internal const float GAUGE_PADDING = 4f;
    internal const float GAUGE_ARC_SIZE = 56f;       // Diameter of arc gauge
    internal const float GAUGE_GROUP_GAP = 8f;       // Gap between groups

    // Mini bar gauge
    internal const float MINI_BAR_H = 10f;
    internal const float MINI_BAR_LABEL_W = 90f;
    internal const float MINI_BAR_VALUE_W = 70f;

    // Annunciator tiles
    internal const float ANN_TILE_W = 110f;
    internal const float ANN_TILE_H = 38f;
    internal const float ANN_TILE_GAP = 3f;

    // Status panel row height
    internal const float STATUS_ROW_H = 18f;
    internal const float STATUS_SECTION_GAP = 12f;

    #endregion

    // ========================================================================
    // INITIALIZATION
    // ========================================================================

    /// <summary>
    /// Create all GUIStyles and textures. Called once from Start().
    /// Safe to call multiple times (idempotent via _stylesInitialized flag).
    /// </summary>
    void InitializeStyles()
    {
        if (_stylesInitialized) return;

        // Textures
        _bgTex       = MakeTex(_cBgDark);
        _panelTex    = MakeTex(_cBgPanel);
        _headerTex   = MakeTex(_cBgHeader);
        _gaugeBgTex  = MakeTex(_cBgGauge);
        _graphBgTex  = MakeTex(_cBgGraph);
        _whiteTex    = MakeTex(Color.white);
        _greenTex    = MakeTex(_cNormalGreen);
        _amberTex    = MakeTex(_cWarningAmber);
        _redTex      = MakeTex(_cAlarmRed);
        _cyanTex     = MakeTex(_cCyanInfo);
        _sectionHeaderTex = MakeTex(_cSectionHeader);  // v0.9.4: Fix memory leak
        _blueAccentTex = MakeTex(_cBlueAccent);        // v0.9.4: Fix memory leak
        _orangeAccentTex = MakeTex(_cOrangeAccent);    // v0.9.4: Fix memory leak
        _textSecondaryTex = MakeTex(_cTextSecondary);  // v0.9.4: Fix memory leak
        // v0.9.4: Cache trace colors for graph legends
        _trace1Tex = MakeTex(_cTrace1);
        _trace2Tex = MakeTex(_cTrace2);
        _trace3Tex = MakeTex(_cTrace3);
        _trace4Tex = MakeTex(_cTrace4);
        _trace5Tex = MakeTex(_cTrace5);
        _trace6Tex = MakeTex(_cTrace6);
        _traceGridTex = MakeTex(_cTraceGrid);
        _tileBorderInactiveTex = MakeTex(new Color(0.2f, 0.22f, 0.26f, 1f));  // v0.9.4: Tile border
        // v0.9.4: Annunciator tile backgrounds
        _annOffTex = MakeTex(_cAnnOff);
        _annNormalTex = MakeTex(_cAnnNormal);
        _annWarningTex = MakeTex(_cAnnWarning);
        _annAlarmTex = MakeTex(_cAnnAlarm);
        _gaugeNeedleTex = MakeTex(_cGaugeNeedle);  // v2.0.5: Fix infinite logging loop

        // Background box styles
        _headerBgStyle = MakeBoxStyle(_headerTex);
        _panelBgStyle  = MakeBoxStyle(_panelTex);
        _gaugeBgStyle  = MakeBoxStyle(_gaugeBgTex);
        _graphBgStyle  = MakeBoxStyle(_graphBgTex);

        // Header label (bold, medium)
        _headerLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false,
            clipping = TextClipping.Clip
        };
        _headerLabelStyle.normal.textColor = _cTextPrimary;
        _headerLabelStyle.padding = new RectOffset(4, 4, 0, 0);

        // Section header (uppercase label above columns)
        _sectionHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _sectionHeaderStyle.normal.textColor = _cTextSecondary;

        // Gauge label (small, centered below arc)
        _gaugeLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.UpperCenter,
            wordWrap = false
        };
        _gaugeLabelStyle.normal.textColor = _cTextSecondary;

        // Gauge value (large digital readout)
        _gaugeValueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _gaugeValueStyle.normal.textColor = _cTextPrimary;

        // Gauge unit (small, under value)
        _gaugeUnitStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 8,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.UpperCenter
        };
        _gaugeUnitStyle.normal.textColor = _cTextSecondary;

        // Status panel label (left-aligned, small)
        _statusLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleLeft
        };
        _statusLabelStyle.normal.textColor = _cTextSecondary;

        // Status panel value (right-aligned, small bold)
        _statusValueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        _statusValueStyle.normal.textColor = _cTextPrimary;

        // Event log (monospace, small)
        _eventLogStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.UpperLeft,
            richText = true,
            wordWrap = true
        };
        _eventLogStyle.normal.textColor = _cTextPrimary;
        _eventLogStyle.padding = new RectOffset(4, 4, 1, 1);

        // Tab toolbar
        _tabStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _tabStyle.normal.textColor = _cTextSecondary;
        _tabStyle.active.textColor = _cCyanInfo;
        _tabStyle.onNormal.textColor = _cCyanInfo;
        _tabStyle.padding = new RectOffset(6, 6, 2, 2);

        // Time acceleration buttons
        _timeAccelBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _timeAccelBtnStyle.normal.textColor = _cTextSecondary;
        _timeAccelBtnStyle.padding = new RectOffset(2, 2, 2, 2);

        _timeAccelActiveStyle = new GUIStyle(_timeAccelBtnStyle);
        _timeAccelActiveStyle.normal.textColor = _cCyanInfo;
        _timeAccelActiveStyle.normal.background = MakeTex(new Color(0f, 0.25f, 0.40f, 0.9f));

        // Annunciator tile
        _annTileStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 9,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        _annTileStyle.normal.textColor = _cTextPrimary;
        _annTileStyle.padding = new RectOffset(2, 2, 2, 2);

        // Graph labels (small)
        _graphLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleLeft,
            richText = true
        };
        _graphLabelStyle.normal.textColor = _cTextSecondary;

        // Graph axis labels
        _graphAxisStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 8,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleRight
        };
        _graphAxisStyle.normal.textColor = _cTextSecondary;

        // v0.9.4: Cached alternate alignment styles to prevent per-frame allocation
        _graphAxisStyleRight = new GUIStyle(_graphAxisStyle)
        {
            alignment = TextAnchor.MiddleLeft
        };
        
        _graphAxisStyleCenter = new GUIStyle(_graphAxisStyle)
        {
            alignment = TextAnchor.UpperCenter
        };
        
        _graphLabelStyleCenter = new GUIStyle(_graphLabelStyle)
        {
            alignment = TextAnchor.UpperCenter
        };

        _stylesInitialized = true;
    }

    // ========================================================================
    // TEXTURE FACTORY
    // ========================================================================

    /// <summary>
    /// Create a 1Ã—1 solid-color Texture2D for use with GUI.DrawTexture.
    /// </summary>
    static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        tex.hideFlags = HideFlags.DontSave;
        return tex;
    }

    /// <summary>
    /// v0.9.5: Clean up all native resources (textures, materials) to prevent
    /// shutdown hangs and memory leaks. Call from OnDestroy and ForceQuit.
    /// </summary>
    internal void CleanupNativeResources()
    {
        Debug.Log("[HeatupValidationVisual] CleanupNativeResources");
        
        // Destroy all cached textures
        DestroyTex(ref _bgTex);
        DestroyTex(ref _panelTex);
        DestroyTex(ref _headerTex);
        DestroyTex(ref _gaugeBgTex);
        DestroyTex(ref _graphBgTex);
        DestroyTex(ref _whiteTex);
        DestroyTex(ref _greenTex);
        DestroyTex(ref _amberTex);
        DestroyTex(ref _redTex);
        DestroyTex(ref _cyanTex);
        DestroyTex(ref _sectionHeaderTex);
        DestroyTex(ref _blueAccentTex);
        DestroyTex(ref _orangeAccentTex);
        DestroyTex(ref _textSecondaryTex);
        DestroyTex(ref _trace1Tex);
        DestroyTex(ref _trace2Tex);
        DestroyTex(ref _trace3Tex);
        DestroyTex(ref _trace4Tex);
        DestroyTex(ref _trace5Tex);
        DestroyTex(ref _trace6Tex);
        DestroyTex(ref _traceGridTex);
        DestroyTex(ref _tileBorderInactiveTex);
        DestroyTex(ref _annOffTex);
        DestroyTex(ref _annNormalTex);
        DestroyTex(ref _annWarningTex);
        DestroyTex(ref _annAlarmTex);
        DestroyTex(ref _gaugeNeedleTex);  // v2.0.5
        
        // Destroy GL material
        if (_glMat != null)
        {
            DestroyImmediate(_glMat);
            _glMat = null;
        }
        
        // Reset initialization flag so resources aren't used after cleanup
        _stylesInitialized = false;
    }

    /// <summary>
    /// v0.9.5: Helper to safely destroy a texture and null the reference.
    /// </summary>
    void DestroyTex(ref Texture2D tex)
    {
        if (tex != null)
        {
            DestroyImmediate(tex);
            tex = null;
        }
    }

    /// <summary>
    /// Create a GUIStyle for box backgrounds using a solid texture.
    /// </summary>
    static GUIStyle MakeBoxStyle(Texture2D tex)
    {
        var style = new GUIStyle(GUI.skin.box);
        style.normal.background = tex;
        style.border = new RectOffset(0, 0, 0, 0);
        style.margin = new RectOffset(0, 0, 0, 0);
        style.padding = new RectOffset(0, 0, 0, 0);
        return style;
    }

    // ========================================================================
    // GAUGE ARC RENDERER â€” Reusable arc gauge with colored bands
    //
    // Draws a half-circle arc gauge using GL lines, with:
    //   - Background arc (dim)
    //   - Colored fill arc (green/amber/red based on value)
    //   - Needle line at current value
    //   - Digital readout centered below arc
    //
    // Arc spans 180Â° from left (min) to right (max), bottom-centered.
    // ========================================================================

    /// <summary>
    /// Draw an arc gauge with colored bands and needle.
    /// </summary>
    /// <param name="center">Center point of the arc (bottom of semicircle)</param>
    /// <param name="radius">Arc radius in pixels</param>
    /// <param name="value">Current value</param>
    /// <param name="min">Gauge minimum</param>
    /// <param name="max">Gauge maximum</param>
    /// <param name="valueColor">Color for the current value indication</param>
    /// <param name="label">Gauge label text</param>
    /// <param name="valueText">Formatted value text</param>
    /// <param name="unitText">Unit text</param>
    internal void DrawGaugeArc(Vector2 center, float radius, float value,
        float min, float max, Color valueColor, string label, string valueText, string unitText)
    {
        if (Event.current.type != EventType.Repaint) return;

        float normalised = Mathf.Clamp01((value - min) / (max - min));

        // Draw arc background (full sweep)
        DrawArcSegment(center, radius, 0f, 1f, _cGaugeArcBg, 3f);

        // Draw filled arc up to value
        if (normalised > 0.001f)
            DrawArcSegment(center, radius, 0f, normalised, valueColor, 3f);

        // Draw needle
        float needleAngle = Mathf.Lerp(Mathf.PI, 0f, normalised);
        Vector2 needleTip = center + new Vector2(
            Mathf.Cos(needleAngle) * radius,
            -Mathf.Sin(needleAngle) * radius);
        DrawLine(center, needleTip, _cGaugeNeedle, 2f);

        // Center dot
        DrawFilledRect(new Rect(center.x - 2f, center.y - 2f, 4f, 4f), _cGaugeNeedle);

        // Label above arc
        Rect labelRect = new Rect(center.x - radius, center.y - radius - 14f,
            radius * 2f, 14f);
        GUI.Label(labelRect, label, _gaugeLabelStyle);

        // Value readout below arc
        Rect valRect = new Rect(center.x - radius, center.y + 2f,
            radius * 2f, 16f);
        var prev = GUI.contentColor;
        GUI.contentColor = valueColor;
        GUI.Label(valRect, valueText, _gaugeValueStyle);
        GUI.contentColor = prev;

        // Units
        Rect unitRect = new Rect(center.x - radius, center.y + 16f,
            radius * 2f, 12f);
        GUI.Label(unitRect, unitText, _gaugeUnitStyle);
    }

    // ========================================================================
    // MINI BAR GAUGE â€” Horizontal fill bar with label and value
    // Used for secondary parameters within gauge groups.
    // ========================================================================

    /// <summary>
    /// Draw a compact horizontal bar gauge with label, bar, and value.
    /// </summary>
    internal void DrawMiniBar(Rect area, string label, float value, float min, float max,
        Color barColor, string format = "F1", string unit = "")
    {
        float labelW = MINI_BAR_LABEL_W;
        float valueW = MINI_BAR_VALUE_W;
        float barW = area.width - labelW - valueW - 8f;
        float y = area.y;
        float h = area.height;

        // Label
        GUI.Label(new Rect(area.x, y, labelW, h), label, _statusLabelStyle);

        // Bar background
        Rect barRect = new Rect(area.x + labelW + 2f, y + 2f, barW, h - 4f);
        DrawFilledRect(barRect, _cGaugeArcBg);

        // Bar fill
        float frac = Mathf.Clamp01((value - min) / (max - min));
        Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * frac, barRect.height);
        DrawFilledRect(fillRect, barColor);

        // Value text
        string valStr = value.ToString(format) + (string.IsNullOrEmpty(unit) ? "" : " " + unit);
        var prev = GUI.contentColor;
        GUI.contentColor = barColor;
        GUI.Label(new Rect(area.x + labelW + barW + 6f, y, valueW, h), valStr, _statusValueStyle);
        GUI.contentColor = prev;
    }

    // ========================================================================
    // GL DRAWING PRIMITIVES â€” Used by gauges and graphs
    // ========================================================================

    #region GL Primitives

    private static Material _glMat;

    /// <summary>
    /// Get or create the GL line material.
    /// </summary>
    static Material GetGLMaterial()
    {
        if (_glMat == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            _glMat = new Material(shader);
            _glMat.hideFlags = HideFlags.DontSave;
            _glMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _glMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _glMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _glMat.SetInt("_ZWrite", 0);
        }
        return _glMat;
    }

    /// <summary>
    /// Draw an arc segment from fraction startFrac to endFrac (0=left, 1=right).
    /// Arc is a top-half semicircle centered at 'center'.
    /// </summary>
    internal static void DrawArcSegment(Vector2 center, float radius,
        float startFrac, float endFrac, Color color, float thickness)
    {
        GetGLMaterial().SetPass(0);
        GL.PushMatrix();
        GL.LoadPixelMatrix();

        int segments = 24;
        float startAngle = Mathf.Lerp(Mathf.PI, 0f, startFrac);
        float endAngle = Mathf.Lerp(Mathf.PI, 0f, endFrac);

        // Draw thick arc as multiple offset lines
        for (float offset = -thickness / 2f; offset <= thickness / 2f; offset += 1f)
        {
            GL.Begin(GL.LINE_STRIP);
            GL.Color(color);
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = Mathf.Lerp(startAngle, endAngle, t);
                float r = radius + offset;
                float px = center.x + Mathf.Cos(angle) * r;
                float py = center.y - Mathf.Sin(angle) * r;
                GL.Vertex3(px, py, 0);
            }
            GL.End();
        }

        GL.PopMatrix();
    }

    /// <summary>
    /// Draw a line between two screen points.
    /// </summary>
    internal static void DrawLine(Vector2 a, Vector2 b, Color color, float width)
    {
        GetGLMaterial().SetPass(0);
        GL.PushMatrix();
        GL.LoadPixelMatrix();

        // Approximate thickness with offset lines
        Vector2 dir = (b - a).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x);

        for (float off = -width / 2f; off <= width / 2f; off += 1f)
        {
            Vector2 offset = perp * off;
            GL.Begin(GL.LINES);
            GL.Color(color);
            GL.Vertex3(a.x + offset.x, a.y + offset.y, 0);
            GL.Vertex3(b.x + offset.x, b.y + offset.y, 0);
            GL.End();
        }

        GL.PopMatrix();
    }

    /// <summary>
    /// v0.9.4: Draw a line with modified alpha (avoids creating new Color object).
    /// </summary>
    internal static void DrawLineWithAlpha(Vector2 a, Vector2 b, Color baseColor, float alpha, float width)
    {
        GetGLMaterial().SetPass(0);
        GL.PushMatrix();
        GL.LoadPixelMatrix();

        // Approximate thickness with offset lines
        Vector2 dir = (b - a).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x);

        for (float off = -width / 2f; off <= width / 2f; off += 1f)
        {
            Vector2 offset = perp * off;
            GL.Begin(GL.LINES);
            // Apply alpha directly to GL.Color without creating new Color object
            GL.Color(new Color(baseColor.r, baseColor.g, baseColor.b, alpha));
            GL.Vertex3(a.x + offset.x, a.y + offset.y, 0);
            GL.Vertex3(b.x + offset.x, b.y + offset.y, 0);
            GL.End();
        }

        GL.PopMatrix();
    }

    /// <summary>
    /// Draw a filled rectangle.
    /// </summary>
    internal void DrawFilledRect(Rect rect, Color color)
    {
        var tex = GetColorTex(color);
        GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill);
    }

    // v2.0.5: One-shot warning tracker â€” only fires once per unique uncached color
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static readonly System.Collections.Generic.HashSet<string> _warnedColors
        = new System.Collections.Generic.HashSet<string>();
    #endif

    /// <summary>
    /// Get a cached 1Ã—1 texture for the given color.
    /// v0.9.4: CRITICAL - All colors MUST be cached to prevent memory leak.
    /// If a color is not cached, returns white texture and logs warning.
    /// </summary>
    Texture2D GetColorTex(Color color)
    {
        // Use cached for common colors - compare with small epsilon for float imprecision
        if (ColorsEqual(color, _cNormalGreen)) return _greenTex;
        if (ColorsEqual(color, _cWarningAmber)) return _amberTex;
        if (ColorsEqual(color, _cAlarmRed)) return _redTex;
        if (ColorsEqual(color, _cCyanInfo)) return _cyanTex;
        if (ColorsEqual(color, Color.white)) return _whiteTex;
        if (ColorsEqual(color, _cGaugeArcBg)) return _gaugeBgTex;
        if (ColorsEqual(color, _cSectionHeader)) return _sectionHeaderTex;
        if (ColorsEqual(color, _cBlueAccent)) return _blueAccentTex;
        if (ColorsEqual(color, _cOrangeAccent)) return _orangeAccentTex;
        if (ColorsEqual(color, _cTextSecondary)) return _textSecondaryTex;
        if (ColorsEqual(color, _cTextPrimary)) return _whiteTex;  // Close enough
        if (ColorsEqual(color, _cBgPanel)) return _panelTex;
        if (ColorsEqual(color, _cBgHeader)) return _headerTex;
        if (ColorsEqual(color, _cBgGraph)) return _graphBgTex;
        // v0.9.4: Trace colors for graph legends
        if (ColorsEqual(color, _cTrace1)) return _trace1Tex;
        if (ColorsEqual(color, _cTrace2)) return _trace2Tex;
        if (ColorsEqual(color, _cTrace3)) return _trace3Tex;
        if (ColorsEqual(color, _cTrace4)) return _trace4Tex;
        if (ColorsEqual(color, _cTrace5)) return _trace5Tex;
        if (ColorsEqual(color, _cTrace6)) return _trace6Tex;
        if (ColorsEqual(color, _cTraceGrid)) return _traceGridTex;
        // v0.9.4: Annunciator tile border
        if (ColorsEqual(color, new Color(0.2f, 0.22f, 0.26f, 1f))) return _tileBorderInactiveTex;
        // v0.9.4: Annunciator tile backgrounds
        if (ColorsEqual(color, _cAnnOff)) return _annOffTex;
        if (ColorsEqual(color, _cAnnNormal)) return _annNormalTex;
        if (ColorsEqual(color, _cAnnWarning)) return _annWarningTex;
        if (ColorsEqual(color, _cAnnAlarm)) return _annAlarmTex;
        // v2.0.5: Gauge needle (0.95 alpha white) â€” was causing infinite logging loop
        if (ColorsEqual(color, _cGaugeNeedle)) return _gaugeNeedleTex;
        
        // v0.9.4: CRITICAL - DO NOT create new textures! This causes massive memory leak.
        // v2.0.5: One-shot warning per unique color (was per-frame spam causing app freeze)
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        string colorKey = $"{color.r:F3},{color.g:F3},{color.b:F3},{color.a:F3}";
        if (_warnedColors.Add(colorKey))
        {
            Debug.LogWarning($"[HeatupValidationVisual] Uncached color used in GetColorTex: {color} - using white fallback. Add this color to the cache!");
        }
        #endif
        return _whiteTex;
    }
    
    /// <summary>
    /// Compare two colors with small epsilon for float imprecision.
    /// </summary>
    static bool ColorsEqual(Color a, Color b)
    {
        const float eps = 0.01f;
        return Mathf.Abs(a.r - b.r) < eps &&
               Mathf.Abs(a.g - b.g) < eps &&
               Mathf.Abs(a.b - b.b) < eps &&
               Mathf.Abs(a.a - b.a) < eps;
    }

    #endregion

    // ========================================================================
    // PANEL / SECTION DRAWING HELPERS
    // ========================================================================

    /// <summary>
    /// Draw a section header bar within a panel.
    /// v0.9.4: Use cached color to prevent memory leak.
    /// </summary>
    internal void DrawSectionHeader(Rect area, string title)
    {
        // v0.9.4: Use cached _cSectionHeader instead of new Color() every frame
        DrawFilledRect(new Rect(area.x, area.y, area.width, GAUGE_GROUP_HEADER_H),
            _cSectionHeader);
        GUI.Label(new Rect(area.x + 4f, area.y, area.width - 8f, GAUGE_GROUP_HEADER_H),
            title, _sectionHeaderStyle);
    }

    /// <summary>
    /// Draw a labeled value row in a status panel.
    /// </summary>
    internal void DrawStatusRow(ref float y, float x, float w, string label, string value, Color valueColor)
    {
        float halfW = w * 0.55f;
        GUI.Label(new Rect(x, y, halfW, STATUS_ROW_H), label, _statusLabelStyle);
        var prev = GUI.contentColor;
        GUI.contentColor = valueColor;
        GUI.Label(new Rect(x + halfW, y, w - halfW, STATUS_ROW_H), value, _statusValueStyle);
        GUI.contentColor = prev;
        y += STATUS_ROW_H;
    }

    /// <summary>
    /// Draw a labeled value row with default (white) value color.
    /// </summary>
    internal void DrawStatusRow(ref float y, float x, float w, string label, string value)
    {
        DrawStatusRow(ref y, x, w, label, value, _cTextPrimary);
    }
}

}

