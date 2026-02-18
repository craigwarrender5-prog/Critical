// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard
// DigitalReadoutElement.cs — Numeric Display with Trend Arrow
// ============================================================================
//
// PURPOSE:
//   A digital numeric readout with:
//   - Configurable format and precision
//   - Unit suffix display
//   - Optional trend arrow (↑ ↓ →)
//   - Threshold-based coloring
//   - Label display
//
// VISUAL:
//   ┌─────────────────┐
//   │  2235.4 psig ↑  │
//   │  RCS PRESSURE   │
//   └─────────────────┘
//
// VERSION: 1.0.0
// DATE: 2026-02-18
// CS: CS-0127 Stage 1
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.UIToolkit.ValidationDashboard
{
    /// <summary>
    /// Digital numeric readout with optional trend indicator and threshold coloring.
    /// </summary>
    [UxmlElement]
    public partial class DigitalReadoutElement : VisualElement
    {
        // ====================================================================
        // USS CLASS NAMES
        // ====================================================================
        
        public new static readonly string ussClassName = "digital-readout";
        public static readonly string ussValueClassName = "digital-readout__value";
        public static readonly string ussLabelClassName = "digital-readout__label";
        public static readonly string ussTrendClassName = "digital-readout__trend";
        
        // ====================================================================
        // TREND ENUM
        // ====================================================================
        
        public enum Trend
        {
            None,
            Rising,
            Falling,
            Stable
        }
        
        // ====================================================================
        // USS CUSTOM STYLE PROPERTIES
        // ====================================================================
        
        private static readonly CustomStyleProperty<Color> s_NormalColor = 
            new CustomStyleProperty<Color>("--normal-color");
        private static readonly CustomStyleProperty<Color> s_WarningColor = 
            new CustomStyleProperty<Color>("--warning-color");
        private static readonly CustomStyleProperty<Color> s_AlarmColor = 
            new CustomStyleProperty<Color>("--alarm-color");
        
        // ====================================================================
        // STATE
        // ====================================================================
        
        private float _value = 0f;
        private float _previousValue = 0f;
        private string _label = "PARAMETER";
        private string _unit = "";
        private string _valueFormat = "F1";
        private Trend _trend = Trend.None;
        private bool _autoTrend = true;
        private float _trendThreshold = 0.1f;
        
        // Thresholds
        private float _warningLow = float.MinValue;
        private float _warningHigh = float.MaxValue;
        private float _alarmLow = float.MinValue;
        private float _alarmHigh = float.MaxValue;
        
        // Colors
        private Color _normalColor = new Color(0.18f, 0.85f, 0.25f, 1f);
        private Color _warningColor = new Color(1f, 0.78f, 0f, 1f);
        private Color _alarmColor = new Color(1f, 0.18f, 0.18f, 1f);
        private Color _labelColor = new Color(0.55f, 0.58f, 0.65f, 1f);
        
        // Child elements
        private VisualElement _valueRow;
        private Label _valueElement;
        private Label _trendElement;
        private Label _labelElement;
        
        // ====================================================================
        // UXML ATTRIBUTES
        // ====================================================================
        
        [UxmlAttribute]
        public float value
        {
            get => _value;
            set => SetValue(value);
        }
        
        [UxmlAttribute]
        public string label
        {
            get => _label;
            set
            {
                _label = value;
                if (_labelElement != null)
                    _labelElement.text = value;
            }
        }
        
        [UxmlAttribute]
        public string unit
        {
            get => _unit;
            set
            {
                _unit = value;
                UpdateDisplay();
            }
        }
        
        [UxmlAttribute]
        public string valueFormat
        {
            get => _valueFormat;
            set
            {
                _valueFormat = value;
                UpdateDisplay();
            }
        }
        
        [UxmlAttribute]
        public bool autoTrend
        {
            get => _autoTrend;
            set => _autoTrend = value;
        }
        
        [UxmlAttribute]
        public float trendThreshold
        {
            get => _trendThreshold;
            set => _trendThreshold = Mathf.Max(0.001f, value);
        }
        
        // ====================================================================
        // PUBLIC PROPERTIES
        // ====================================================================
        
        /// <summary>Current display color based on thresholds.</summary>
        public Color CurrentColor => GetValueColor(_value);
        
        /// <summary>Current trend direction.</summary>
        public Trend CurrentTrend => _trend;
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public DigitalReadoutElement()
        {
            AddToClassList(ussClassName);
            
            style.flexDirection = FlexDirection.Column;
            style.alignItems = Align.Center;
            style.minWidth = 80;
            style.paddingLeft = 8;
            style.paddingRight = 8;
            style.paddingTop = 4;
            style.paddingBottom = 4;
            
            // Value row (value + trend)
            _valueRow = new VisualElement();
            _valueRow.style.flexDirection = FlexDirection.Row;
            _valueRow.style.alignItems = Align.Center;
            _valueRow.style.justifyContent = Justify.Center;
            Add(_valueRow);
            
            // Value label
            _valueElement = new Label("0.0");
            _valueElement.AddToClassList(ussValueClassName);
            _valueElement.style.fontSize = 16;
            _valueElement.style.color = _normalColor;
            _valueElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            _valueRow.Add(_valueElement);
            
            // Trend indicator
            _trendElement = new Label("");
            _trendElement.AddToClassList(ussTrendClassName);
            _trendElement.style.fontSize = 14;
            _trendElement.style.color = _normalColor;
            _trendElement.style.marginLeft = 4;
            _trendElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            _valueRow.Add(_trendElement);
            
            // Parameter label
            _labelElement = new Label(_label);
            _labelElement.AddToClassList(ussLabelClassName);
            _labelElement.style.fontSize = 10;
            _labelElement.style.color = _labelColor;
            _labelElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            _labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            _labelElement.style.marginTop = 2;
            Add(_labelElement);
            
            // Register for style changes
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStylesResolved);
        }
        
        // ====================================================================
        // PUBLIC API
        // ====================================================================
        
        /// <summary>
        /// Set the display value.
        /// </summary>
        public void SetValue(float newValue)
        {
            _previousValue = _value;
            _value = newValue;
            
            if (_autoTrend)
                UpdateTrend();
            
            UpdateDisplay();
        }
        
        /// <summary>
        /// Set all thresholds at once.
        /// </summary>
        public void SetThresholds(float warningLow, float warningHigh, float alarmLow, float alarmHigh)
        {
            _warningLow = warningLow;
            _warningHigh = warningHigh;
            _alarmLow = alarmLow;
            _alarmHigh = alarmHigh;
            UpdateDisplay();
        }
        
        /// <summary>
        /// Set only high thresholds.
        /// </summary>
        public void SetHighThresholds(float warning, float alarm)
        {
            _warningLow = float.MinValue;
            _alarmLow = float.MinValue;
            _warningHigh = warning;
            _alarmHigh = alarm;
            UpdateDisplay();
        }
        
        /// <summary>
        /// Set only low thresholds.
        /// </summary>
        public void SetLowThresholds(float warning, float alarm)
        {
            _warningLow = warning;
            _alarmLow = alarm;
            _warningHigh = float.MaxValue;
            _alarmHigh = float.MaxValue;
            UpdateDisplay();
        }
        
        /// <summary>
        /// Manually set the trend indicator.
        /// </summary>
        public void SetTrend(Trend trend)
        {
            _trend = trend;
            UpdateTrendDisplay();
        }
        
        // ====================================================================
        // HELPERS
        // ====================================================================
        
        private void UpdateTrend()
        {
            float delta = _value - _previousValue;
            
            if (delta > _trendThreshold)
                _trend = Trend.Rising;
            else if (delta < -_trendThreshold)
                _trend = Trend.Falling;
            else
                _trend = Trend.Stable;
        }
        
        private void UpdateDisplay()
        {
            if (_valueElement == null) return;
            
            // Format value
            string text = _value.ToString(_valueFormat);
            if (!string.IsNullOrEmpty(_unit))
                text += " " + _unit;
            
            _valueElement.text = text;
            
            // Update color
            Color color = GetValueColor(_value);
            _valueElement.style.color = color;
            _trendElement.style.color = color;
            
            UpdateTrendDisplay();
        }
        
        private void UpdateTrendDisplay()
        {
            if (_trendElement == null) return;
            
            switch (_trend)
            {
                case Trend.Rising:
                    _trendElement.text = "▲";
                    break;
                case Trend.Falling:
                    _trendElement.text = "▼";
                    break;
                case Trend.Stable:
                    _trendElement.text = "►";
                    break;
                default:
                    _trendElement.text = "";
                    break;
            }
        }
        
        private Color GetValueColor(float val)
        {
            if (val < _alarmLow || val > _alarmHigh)
                return _alarmColor;
            if (val < _warningLow || val > _warningHigh)
                return _warningColor;
            return _normalColor;
        }
        
        private void OnCustomStylesResolved(CustomStyleResolvedEvent evt)
        {
            bool update = false;
            
            if (customStyle.TryGetValue(s_NormalColor, out var normalColor))
            {
                _normalColor = normalColor;
                update = true;
            }
            if (customStyle.TryGetValue(s_WarningColor, out var warningColor))
            {
                _warningColor = warningColor;
                update = true;
            }
            if (customStyle.TryGetValue(s_AlarmColor, out var alarmColor))
            {
                _alarmColor = alarmColor;
                update = true;
            }
            
            if (update)
                UpdateDisplay();
        }
    }
}
