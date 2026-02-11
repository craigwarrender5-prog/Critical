// CRITICAL: Master the Atom - Phase 2 Mosaic Gauge
// MosaicGauge.cs - Individual Gauge Display Component
//
// Displays a single reactor parameter with:
//   - Analog dial display (optional)
//   - Digital readout with TMP instrument font and glow effects
//   - Horizontal fill bar indicator
//   - Color-coded alarm states with material swapping
//   - Configurable ranges and thresholds
//
// Reference: Westinghouse Nuclear Instrumentation Displays
//
// Usage: Attach to gauge UI element, configure type and visuals.
//
// GOLD STANDARD: Yes
// CHANGE: v4.1.0 — Upgraded from legacy Text to TextMeshProUGUI for
//         ValueText, LabelText, UnitsText. Added FillBarIndicator and
//         GlowImage references. Added TMP material swapping for alarm
//         state visual feedback (green/amber/red glow).

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI
{
    using Controllers;
    
    /// <summary>
    /// Individual gauge display for Mosaic Board.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MosaicGauge : MonoBehaviour, IMosaicComponent, IAlarmFlashReceiver
    {
        #region Unity Inspector Fields
        
        [Header("Gauge Configuration")]
        [Tooltip("Type of value to display")]
        public GaugeType Type = GaugeType.NeutronPower;
        
        [Tooltip("Custom label (overrides default)")]
        public string CustomLabel;
        
        [Header("Display Mode")]
        [Tooltip("Show analog dial")]
        public bool ShowAnalog = true;
        
        [Tooltip("Show digital readout")]
        public bool ShowDigital = true;
        
        [Tooltip("Show label")]
        public bool ShowLabel = true;
        
        [Tooltip("Show units")]
        public bool ShowUnits = true;
        
        [Header("Visual References")]
        [Tooltip("Needle/pointer transform for analog display")]
        public RectTransform Needle;
        
        [Tooltip("Fill image for bar-style gauge")]
        public Image FillBar;
        
        [Tooltip("Digital value text (TMP)")]
        public TextMeshProUGUI ValueText;
        
        [Tooltip("Label text (TMP)")]
        public TextMeshProUGUI LabelText;
        
        [Tooltip("Units text (TMP)")]
        public TextMeshProUGUI UnitsText;
        
        [Tooltip("Background image")]
        public Image Background;
        
        [Tooltip("Border/frame image")]
        public Image Border;
        
        [Header("v4.1.0 Visual Enhancements")]
        [Tooltip("Horizontal fill bar showing normalized value position")]
        public Image FillBarIndicator;
        
        [Tooltip("Soft glow image behind value text, tinted to alarm color")]
        public Image GlowImage;
        
        [Header("Analog Settings")]
        [Tooltip("Minimum needle angle (degrees)")]
        public float MinAngle = 135f;
        
        [Tooltip("Maximum needle angle (degrees)")]
        public float MaxAngle = -135f;
        
        [Header("Thresholds Override")]
        [Tooltip("Use custom thresholds")]
        public bool UseCustomThresholds = false;
        
        public float CustomWarningLow = float.MinValue;
        public float CustomWarningHigh = float.MaxValue;
        public float CustomAlarmLow = float.MinValue;
        public float CustomAlarmHigh = float.MaxValue;
        
        #endregion
        
        #region Private Fields
        
        private MosaicBoard _board;
        private float _currentValue;
        private float _displayedValue;
        private AlarmState _currentAlarmState;
        private bool _alarmFlashing;
        
        // v4.1.0: Cached TMP materials for alarm-state color swapping
        private Material _matGreen;
        private Material _matAmber;
        private Material _matRed;
        private Material _matCyan;
        private AlarmState _lastMaterialState = AlarmState.Normal;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>Current raw value</summary>
        public float Value => _currentValue;
        
        /// <summary>Current alarm state</summary>
        public AlarmState CurrentAlarmState => _currentAlarmState;
        
        /// <summary>Gauge label</summary>
        public string Label => !string.IsNullOrEmpty(CustomLabel) ? CustomLabel : GetDefaultLabel();
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Set initial label
            if (LabelText != null && ShowLabel)
            {
                LabelText.text = Label;
            }
            
            // Set units
            if (UnitsText != null && ShowUnits)
            {
                UnitsText.text = GetUnits();
            }
            
            // v4.1.0: Load TMP materials for alarm state color swapping
            LoadInstrumentMaterials();
        }
        
        private void OnEnable()
        {
            // Register with board if it exists
            if (_board == null && MosaicBoard.Instance != null)
            {
                _board = MosaicBoard.Instance;
                _board.RegisterComponent(this);
            }
        }
        
        private void OnDisable()
        {
            _board?.UnregisterComponent(this);
        }
        
        #endregion
        
        #region IMosaicComponent Implementation
        
        public void Initialize(MosaicBoard board)
        {
            _board = board;
        }
        
        public void UpdateData()
        {
            if (_board == null) return;
            
            // Get current value
            _currentValue = _board.GetValue(Type);
            
            // Apply smoothing
            _displayedValue = _board.GetSmoothedValue($"gauge_{Type}_{GetInstanceID()}", _currentValue);
            
            // Get alarm state
            _currentAlarmState = UseCustomThresholds 
                ? GetCustomAlarmState(_currentValue)
                : _board.GetAlarmState(Type, _currentValue);
            
            // Update visuals
            UpdateNeedle();
            UpdateFillBar();
            UpdateDigitalDisplay();
            UpdateColors();
            
            // v4.1.0: Update enhanced visuals
            UpdateFillBarIndicator();
            UpdateGlowEffect();
            UpdateValueMaterial();
        }
        
        #endregion
        
        #region IAlarmFlashReceiver Implementation
        
        public void OnAlarmFlash(bool flashOn)
        {
            _alarmFlashing = flashOn;
            
            // Flash border on alarm
            if (_currentAlarmState >= AlarmState.Alarm && Border != null)
            {
                Border.color = flashOn ? _board.AlarmColor : _board.OffColor;
            }
            
            // v4.1.0: Pulse TMP glow power on alarm material
            if (_currentAlarmState >= AlarmState.Alarm && ValueText != null && _matRed != null)
            {
                // Animate glow intensity between dim and bright
                float glowPower = flashOn ? 0.6f : 0.15f;
                ValueText.fontMaterial.SetFloat("_GlowPower", glowPower);
            }
        }
        
        #endregion
        
        #region Visual Updates
        
        private void UpdateNeedle()
        {
            if (Needle == null || !ShowAnalog) return;
            
            // Normalize value to 0-1
            float normalized = _board.GetNormalizedValue(Type, _displayedValue);
            
            // Calculate angle
            float angle = Mathf.Lerp(MinAngle, MaxAngle, normalized);
            
            // Apply rotation
            Needle.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
        
        private void UpdateFillBar()
        {
            if (FillBar == null) return;
            
            // Normalize value to 0-1
            float normalized = _board.GetNormalizedValue(Type, _displayedValue);
            
            // Apply fill
            FillBar.fillAmount = Mathf.Clamp01(normalized);
            
            // Apply color
            FillBar.color = GetDisplayColor();
        }
        
        private void UpdateDigitalDisplay()
        {
            if (ValueText == null || !ShowDigital) return;
            
            // Format value
            ValueText.text = _board.GetFormattedValue(Type, _currentValue);
            
            // Apply color (fallback if no TMP materials loaded)
            if (_matGreen == null)
            {
                ValueText.color = GetDisplayColor();
            }
            // When TMP materials are loaded, color comes from material face color
            // via UpdateValueMaterial() — no need to set .color here
        }
        
        private void UpdateColors()
        {
            Color displayColor = GetDisplayColor();
            
            // Update needle color if applicable
            if (Needle != null)
            {
                var needleImage = Needle.GetComponent<Image>();
                if (needleImage != null)
                {
                    needleImage.color = displayColor;
                }
            }
            
            // Update border based on alarm state
            if (Border != null && _currentAlarmState < AlarmState.Alarm)
            {
                Border.color = _currentAlarmState == AlarmState.Warning 
                    ? _board.WarningColor 
                    : _board.OffColor;
            }
        }
        
        private Color GetDisplayColor()
        {
            if (_board == null) return Color.white;
            
            return _currentAlarmState switch
            {
                AlarmState.Trip => _alarmFlashing ? _board.TripColor : _board.OffColor,
                AlarmState.Alarm => _alarmFlashing ? _board.AlarmColor : _board.OffColor,
                AlarmState.Warning => _board.WarningColor,
                _ => _board.NormalColor
            };
        }
        
        #endregion
        
        #region v4.1.0 Enhanced Visuals
        
        /// <summary>
        /// Load TMP material presets from Resources for alarm-state color swapping.
        /// Falls back gracefully if materials not found (legacy color-based display).
        /// </summary>
        private void LoadInstrumentMaterials()
        {
            _matGreen = Resources.Load<Material>("Fonts & Materials/Instrument_Green_Glow");
            _matAmber = Resources.Load<Material>("Fonts & Materials/Instrument_Amber_Glow");
            _matRed = Resources.Load<Material>("Fonts & Materials/Instrument_Red_Glow");
            _matCyan = Resources.Load<Material>("Fonts & Materials/Instrument_Cyan");
            
            if (_matGreen != null && ValueText != null)
            {
                // Set initial material
                ValueText.fontSharedMaterial = _matGreen;
            }
        }
        
        /// <summary>
        /// Update the fill bar indicator width based on normalized value.
        /// </summary>
        private void UpdateFillBarIndicator()
        {
            if (FillBarIndicator == null || _board == null) return;
            
            float normalized = _board.GetNormalizedValue(Type, _displayedValue);
            normalized = Mathf.Clamp01(normalized);
            
            // Scale X to show fill amount (anchored left)
            RectTransform rt = FillBarIndicator.rectTransform;
            rt.anchorMax = new Vector2(normalized, rt.anchorMax.y);
            
            // Tint to alarm color
            FillBarIndicator.color = GetDisplayColor();
        }
        
        /// <summary>
        /// Update glow image behind value text — tinted to alarm state.
        /// </summary>
        private void UpdateGlowEffect()
        {
            if (GlowImage == null) return;
            
            Color glowColor = GetDisplayColor();
            
            // Glow intensity varies with alarm state
            float alpha = _currentAlarmState switch
            {
                AlarmState.Trip => _alarmFlashing ? 0.5f : 0.1f,
                AlarmState.Alarm => _alarmFlashing ? 0.4f : 0.1f,
                AlarmState.Warning => 0.2f,
                _ => 0.12f
            };
            
            glowColor.a = alpha;
            GlowImage.color = glowColor;
        }
        
        /// <summary>
        /// Swap TMP material preset based on current alarm state.
        /// Only swaps when state actually changes (avoids per-frame material assignment).
        /// </summary>
        private void UpdateValueMaterial()
        {
            if (ValueText == null || _matGreen == null) return;
            if (_currentAlarmState == _lastMaterialState) return;
            
            Material targetMat = _currentAlarmState switch
            {
                AlarmState.Trip => _matRed,
                AlarmState.Alarm => _matRed,
                AlarmState.Warning => _matAmber,
                _ => _matGreen
            };
            
            if (targetMat != null)
            {
                ValueText.fontSharedMaterial = targetMat;
            }
            
            _lastMaterialState = _currentAlarmState;
        }
        
        #endregion
        
        #region Helpers
        
        private string GetDefaultLabel()
        {
            return Type switch
            {
                GaugeType.NeutronPower => "NEUTRON POWER",
                GaugeType.ThermalPower => "THERMAL POWER",
                GaugeType.Tavg => "T-AVG",
                GaugeType.Thot => "T-HOT",
                GaugeType.Tcold => "T-COLD",
                GaugeType.DeltaT => "DELTA-T",
                GaugeType.FuelCenterline => "FUEL TEMP",
                GaugeType.TotalReactivity => "REACTIVITY",
                GaugeType.StartupRate => "STARTUP RATE",
                GaugeType.ReactorPeriod => "PERIOD",
                GaugeType.Boron => "BORON",
                GaugeType.Xenon => "XENON",
                GaugeType.BankDPosition => "BANK D",
                GaugeType.FlowFraction => "RCS FLOW",
                _ => Type.ToString()
            };
        }
        
        private string GetUnits()
        {
            return Type switch
            {
                GaugeType.NeutronPower => "%",
                GaugeType.ThermalPower => "MWt",
                GaugeType.Tavg => "°F",
                GaugeType.Thot => "°F",
                GaugeType.Tcold => "°F",
                GaugeType.DeltaT => "°F",
                GaugeType.FuelCenterline => "°F",
                GaugeType.TotalReactivity => "pcm",
                GaugeType.StartupRate => "DPM",
                GaugeType.ReactorPeriod => "sec",
                GaugeType.Boron => "ppm",
                GaugeType.Xenon => "pcm",
                GaugeType.BankDPosition => "steps",
                GaugeType.FlowFraction => "%",
                _ => ""
            };
        }
        
        private AlarmState GetCustomAlarmState(float value)
        {
            if (value < CustomAlarmLow || value > CustomAlarmHigh)
                return AlarmState.Alarm;
            
            if (value < CustomWarningLow || value > CustomWarningHigh)
                return AlarmState.Warning;
            
            return AlarmState.Normal;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Force refresh of display.
        /// </summary>
        public void Refresh()
        {
            if (_board != null)
            {
                UpdateData();
            }
        }
        
        /// <summary>
        /// Set gauge type at runtime.
        /// </summary>
        public void SetType(GaugeType newType)
        {
            Type = newType;
            
            if (LabelText != null && ShowLabel && string.IsNullOrEmpty(CustomLabel))
            {
                LabelText.text = GetDefaultLabel();
            }
            
            if (UnitsText != null && ShowUnits)
            {
                UnitsText.text = GetUnits();
            }
            
            Refresh();
        }
        
        #endregion
    }
}
