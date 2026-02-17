// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// ValidationDashboard.Styles.cs - Colors, Fonts, GUIStyles
// ============================================================================
//
// PURPOSE:
//   All visual styling for the new Validation Dashboard. Centralizes colors,
//   GUIStyle factories, and texture management. No other partial should
//   create GUIStyles or define colors.
//
// PERFORMANCE CRITICAL:
//   - All GUIStyles created ONCE in InitializeStyles(), never in OnGUI
//   - All textures created ONCE and cached
//   - GetColorTex() returns cached textures, never creates new ones in OnGUI
//
// REFERENCE:
//   Westinghouse 4-Loop PWR control room color conventions:
//     - Dark background (near-black with slight blue tint)
//     - Green for normal/safe parameters
//     - Amber/yellow for warnings
//     - Red for alarms
//     - Cyan for informational states
//
// GOLD STANDARD: Yes
// VERSION: 1.0.0.0
// ============================================================================

using UnityEngine;

namespace Critical.Validation
{
    public partial class ValidationDashboard
    {
        // ====================================================================
        // INITIALIZATION FLAG
        // ====================================================================

        private bool _stylesInitialized = false;

        // ====================================================================
        // COLOR PALETTE
        // ====================================================================

        #region Colors

        // Background tones
        internal static readonly Color _cBgDark = new Color(0.04f, 0.05f, 0.07f, 1f);
        internal static readonly Color _cBgPanel = new Color(0.08f, 0.09f, 0.12f, 0.95f);
        internal static readonly Color _cBgHeader = new Color(0.05f, 0.06f, 0.08f, 1f);
        internal static readonly Color _cBgGauge = new Color(0.06f, 0.07f, 0.10f, 1f);

        // Functional status colors
        internal static readonly Color _cNormalGreen = new Color(0.00f, 1.00f, 0.53f, 1f);   // #00FF88
        internal static readonly Color _cWarningAmber = new Color(1.00f, 0.67f, 0.00f, 1f);  // #FFAA00
        internal static readonly Color _cAlarmRed = new Color(1.00f, 0.27f, 0.27f, 1f);      // #FF4444

        // Informational colors
        internal static readonly Color _cCyanInfo = new Color(0.40f, 0.80f, 1.00f, 1f);      // #66CCFF
        internal static readonly Color _cBlueAccent = new Color(0.20f, 0.50f, 1.00f, 1f);

        // Text colors
        internal static readonly Color _cTextPrimary = new Color(0.90f, 0.90f, 0.90f, 1f);   // #E6E6E6 - brighter for readability
        internal static readonly Color _cTextSecondary = new Color(0.65f, 0.65f, 0.65f, 1f); // #A6A6A6 - brighter labels
        internal static readonly Color _cTextBright = new Color(1.00f, 1.00f, 1.00f, 1f);

        // Gauge colors
        internal static readonly Color _cGaugeArcBg = new Color(0.15f, 0.16f, 0.19f, 1f);   // Slightly brighter background
        internal static readonly Color _cGaugeNeedle = new Color(1.00f, 1.00f, 1.00f, 0.95f);
        internal static readonly Color _cGaugeTick = new Color(0.50f, 0.52f, 0.56f, 1f);   // Brighter borders/dividers

        // LED colors
        internal static readonly Color _cLedOff = new Color(0.15f, 0.16f, 0.18f, 1f);
        internal static readonly Color _cLedOn = new Color(0.00f, 0.90f, 0.45f, 1f);
        internal static readonly Color _cLedWarning = new Color(1.00f, 0.70f, 0.00f, 1f);
        internal static readonly Color _cLedAlarm = new Color(1.00f, 0.20f, 0.20f, 1f);

        // Annunciator colors - BRIGHT for visibility
        internal static readonly Color _cAnnOff = new Color(0.15f, 0.16f, 0.18f, 1f);       // Dark gray (inactive)
        internal static readonly Color _cAnnNormal = new Color(0.10f, 0.50f, 0.15f, 1f);    // GREEN - normal/safe
        internal static readonly Color _cAnnAlerting = new Color(0.90f, 0.70f, 0.00f, 1f);  // YELLOW/AMBER - alerting
        internal static readonly Color _cAnnAlarm = new Color(0.90f, 0.15f, 0.15f, 1f);     // RED - alarm
        internal static readonly Color _cAnnAcked = new Color(0.60f, 0.50f, 0.00f, 1f);     // DARK YELLOW - acknowledged

        #endregion

        // ====================================================================
        // TEXTURES
        // ====================================================================

        #region Textures

        private Texture2D _bgTex;
        private Texture2D _panelTex;
        private Texture2D _headerTex;
        private Texture2D _gaugeBgTex;
        private Texture2D _whiteTex;
        private Texture2D _greenTex;
        private Texture2D _amberTex;
        private Texture2D _redTex;
        private Texture2D _cyanTex;
        private Texture2D _gaugeArcBgTex;
        private Texture2D _needleTex;
        private Texture2D _ledOffTex;
        private Texture2D _ledOnTex;
        private Texture2D _ledWarningTex;
        private Texture2D _ledAlarmTex;
        
        // Annunciator textures
        private Texture2D _annOffTex;
        private Texture2D _annNormalTex;
        private Texture2D _annAlertingTex;
        private Texture2D _annAlarmTex;
        private Texture2D _annAckedTex;

        #endregion

        // ====================================================================
        // GUI STYLES
        // ====================================================================

        #region Styles

        // Background styles (internal for tab access)
        internal GUIStyle _headerBgStyle;
        internal GUIStyle _panelBgStyle;
        internal GUIStyle _gaugeBgStyle;

        // Text styles (internal for tab access)
        internal GUIStyle _headerLabelStyle;
        internal GUIStyle _sectionHeaderStyle;
        internal GUIStyle _gaugeLabelStyle;
        internal GUIStyle _gaugeValueStyle;
        internal GUIStyle _gaugeUnitStyle;
        internal GUIStyle _statusLabelStyle;
        internal GUIStyle _statusValueStyle;
        internal GUIStyle _digitalReadoutStyle;

        // Interactive styles
        internal GUIStyle _tabStyle;
        internal GUIStyle _buttonStyle;

        // LED/Indicator styles
        internal GUIStyle _ledLabelStyle;

        #endregion

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        /// <summary>
        /// Initialize all styles and textures. Called once, idempotent.
        /// </summary>
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            // Create textures
            _bgTex = MakeTex(_cBgDark);
            _panelTex = MakeTex(_cBgPanel);
            _headerTex = MakeTex(_cBgHeader);
            _gaugeBgTex = MakeTex(_cBgGauge);
            _whiteTex = MakeTex(Color.white);
            _greenTex = MakeTex(_cNormalGreen);
            _amberTex = MakeTex(_cWarningAmber);
            _redTex = MakeTex(_cAlarmRed);
            _cyanTex = MakeTex(_cCyanInfo);
            _gaugeArcBgTex = MakeTex(_cGaugeArcBg);
            _needleTex = MakeTex(_cGaugeNeedle);
            _ledOffTex = MakeTex(_cLedOff);
            _ledOnTex = MakeTex(_cLedOn);
            _ledWarningTex = MakeTex(_cLedWarning);
            _ledAlarmTex = MakeTex(_cLedAlarm);
            
            // Annunciator textures
            _annOffTex = MakeTex(_cAnnOff);
            _annNormalTex = MakeTex(_cAnnNormal);
            _annAlertingTex = MakeTex(_cAnnAlerting);
            _annAlarmTex = MakeTex(_cAnnAlarm);
            _annAckedTex = MakeTex(_cAnnAcked);

            // Background styles
            _headerBgStyle = MakeBoxStyle(_headerTex);
            _panelBgStyle = MakeBoxStyle(_panelTex);
            _gaugeBgStyle = MakeBoxStyle(_gaugeBgTex);

            // Header label
            _headerLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            _headerLabelStyle.normal.textColor = _cTextPrimary;
            _headerLabelStyle.padding = new RectOffset(2, 2, 0, 0);

            // Section header
            _sectionHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _sectionHeaderStyle.normal.textColor = _cTextSecondary;

            // Gauge label (small, above gauge)
            _gaugeLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperCenter,
                wordWrap = false
            };
            _gaugeLabelStyle.normal.textColor = _cTextSecondary;

            // Gauge value (large digital)
            _gaugeValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _gaugeValueStyle.normal.textColor = _cTextPrimary;

            // Gauge unit
            _gaugeUnitStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 8,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperCenter
            };
            _gaugeUnitStyle.normal.textColor = _cTextSecondary;

            // Status label (left side of row)
            _statusLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft
            };
            _statusLabelStyle.normal.textColor = _cTextSecondary;

            // Status value (right side of row)
            _statusValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            _statusValueStyle.normal.textColor = _cTextPrimary;

            // Digital readout
            _digitalReadoutStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            _digitalReadoutStyle.normal.textColor = _cTextPrimary;

            // Tab toolbar
            _tabStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _tabStyle.normal.textColor = _cTextSecondary;
            _tabStyle.active.textColor = _cCyanInfo;
            _tabStyle.onNormal.textColor = _cCyanInfo;
            _tabStyle.padding = new RectOffset(4, 4, 2, 2);

            // Button
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _buttonStyle.normal.textColor = _cTextPrimary;

            // LED label
            _ledLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft
            };
            _ledLabelStyle.normal.textColor = _cTextSecondary;

            _stylesInitialized = true;
            Debug.Log("[ValidationDashboard] Styles initialized");
        }

        // ====================================================================
        // TEXTURE FACTORY
        // ====================================================================

        /// <summary>
        /// Create a 1×1 solid-color texture.
        /// </summary>
        private static Texture2D MakeTex(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }

        /// <summary>
        /// Create a GUIStyle for box backgrounds.
        /// </summary>
        private static GUIStyle MakeBoxStyle(Texture2D tex)
        {
            var style = new GUIStyle(GUI.skin.box);
            style.normal.background = tex;
            style.border = new RectOffset(0, 0, 0, 0);
            style.margin = new RectOffset(0, 0, 0, 0);
            style.padding = new RectOffset(0, 0, 0, 0);
            return style;
        }

        // ====================================================================
        // COLOR UTILITIES
        // ====================================================================

        /// <summary>
        /// Get threshold-based color for two-sided thresholds.
        /// </summary>
        internal static Color GetThresholdColor(float value, float warnLow, float warnHigh,
            float alarmLow, float alarmHigh)
        {
            if (value < alarmLow || value > alarmHigh) return _cAlarmRed;
            if (value < warnLow || value > warnHigh) return _cWarningAmber;
            return _cNormalGreen;
        }

        /// <summary>
        /// Get threshold-based color for low-is-bad parameters.
        /// </summary>
        internal static Color GetLowThresholdColor(float value, float warn, float alarm)
        {
            if (value < alarm) return _cAlarmRed;
            if (value < warn) return _cWarningAmber;
            return _cNormalGreen;
        }

        /// <summary>
        /// Get threshold-based color for high-is-bad parameters.
        /// </summary>
        internal static Color GetHighThresholdColor(float value, float warn, float alarm)
        {
            if (value > alarm) return _cAlarmRed;
            if (value > warn) return _cWarningAmber;
            return _cNormalGreen;
        }

        /// <summary>
        /// Get cached texture for a color. Returns closest match.
        /// PERFORMANCE CRITICAL: Never creates textures in OnGUI.
        /// </summary>
        internal Texture2D GetColorTex(Color color)
        {
            // Match against cached colors
            if (ColorsEqual(color, _cNormalGreen)) return _greenTex;
            if (ColorsEqual(color, _cWarningAmber)) return _amberTex;
            if (ColorsEqual(color, _cAlarmRed)) return _redTex;
            if (ColorsEqual(color, _cCyanInfo)) return _cyanTex;
            if (ColorsEqual(color, Color.white)) return _whiteTex;
            if (ColorsEqual(color, _cTextPrimary)) return _whiteTex;
            if (ColorsEqual(color, _cGaugeArcBg)) return _gaugeArcBgTex;
            if (ColorsEqual(color, _cGaugeNeedle)) return _needleTex;
            if (ColorsEqual(color, _cLedOff)) return _ledOffTex;
            if (ColorsEqual(color, _cLedOn)) return _ledOnTex;
            if (ColorsEqual(color, _cLedWarning)) return _ledWarningTex;
            if (ColorsEqual(color, _cLedAlarm)) return _ledAlarmTex;
            if (ColorsEqual(color, _cBgPanel)) return _panelTex;
            if (ColorsEqual(color, _cBgHeader)) return _headerTex;
            if (ColorsEqual(color, _cBgGauge)) return _gaugeBgTex;
            
            // Annunciator colors
            if (ColorsEqual(color, _cAnnOff)) return _annOffTex;
            if (ColorsEqual(color, _cAnnNormal)) return _annNormalTex;
            if (ColorsEqual(color, _cAnnAlerting)) return _annAlertingTex;
            if (ColorsEqual(color, _cAnnAlarm)) return _annAlarmTex;
            if (ColorsEqual(color, _cAnnAcked)) return _annAckedTex;

            // Fallback to white
            return _whiteTex;
        }

        /// <summary>
        /// Compare colors with epsilon tolerance.
        /// </summary>
        private static bool ColorsEqual(Color a, Color b)
        {
            const float eps = 0.02f;
            return Mathf.Abs(a.r - b.r) < eps &&
                   Mathf.Abs(a.g - b.g) < eps &&
                   Mathf.Abs(a.b - b.b) < eps &&
                   Mathf.Abs(a.a - b.a) < eps;
        }

        // ====================================================================
        // CLEANUP
        // ====================================================================

        void OnDestroy()
        {
            // Dispose sparkline textures
            _sparklineManager?.Dispose();

            // Destroy cached textures
            if (_bgTex != null) DestroyImmediate(_bgTex);
            if (_panelTex != null) DestroyImmediate(_panelTex);
            if (_headerTex != null) DestroyImmediate(_headerTex);
            if (_gaugeBgTex != null) DestroyImmediate(_gaugeBgTex);
            if (_whiteTex != null) DestroyImmediate(_whiteTex);
            if (_greenTex != null) DestroyImmediate(_greenTex);
            if (_amberTex != null) DestroyImmediate(_amberTex);
            if (_redTex != null) DestroyImmediate(_redTex);
            if (_cyanTex != null) DestroyImmediate(_cyanTex);
            if (_gaugeArcBgTex != null) DestroyImmediate(_gaugeArcBgTex);
            if (_needleTex != null) DestroyImmediate(_needleTex);
            if (_ledOffTex != null) DestroyImmediate(_ledOffTex);
            if (_ledOnTex != null) DestroyImmediate(_ledOnTex);
            if (_ledWarningTex != null) DestroyImmediate(_ledWarningTex);
            if (_ledAlarmTex != null) DestroyImmediate(_ledAlarmTex);
            if (_annOffTex != null) DestroyImmediate(_annOffTex);
            if (_annNormalTex != null) DestroyImmediate(_annNormalTex);
            if (_annAlertingTex != null) DestroyImmediate(_annAlertingTex);
            if (_annAlarmTex != null) DestroyImmediate(_annAlarmTex);
            if (_annAckedTex != null) DestroyImmediate(_annAckedTex);

            _stylesInitialized = false;
        }
    }
}
