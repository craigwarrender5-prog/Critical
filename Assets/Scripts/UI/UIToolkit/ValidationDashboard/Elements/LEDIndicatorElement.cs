// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard
// LEDIndicatorElement.cs — Status LED with Glow Effect
// ============================================================================
//
// PURPOSE:
//   A simple status LED indicator with:
//   - On/Off state with color
//   - Optional pulsing/flashing for alarm states
//   - Glow effect using radial gradient
//   - Optional label
//
// VISUAL:
//   [●] RCP-A     (LED on, green)
//   [○] RCP-B     (LED off, dim)
//   [◉] ALARM     (LED flashing, red with glow)
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
    /// Status LED indicator with optional glow and flash effects.
    /// </summary>
    [UxmlElement]
    public partial class LEDIndicatorElement : VisualElement
    {
        // ====================================================================
        // USS CLASS NAMES
        // ====================================================================
        
        public new static readonly string ussClassName = "led-indicator";
        public static readonly string ussLabelClassName = "led-indicator__label";
        
        // ====================================================================
        // USS CUSTOM STYLE PROPERTIES
        // ====================================================================
        
        private static readonly CustomStyleProperty<Color> s_OnColor = 
            new CustomStyleProperty<Color>("--on-color");
        private static readonly CustomStyleProperty<Color> s_OffColor = 
            new CustomStyleProperty<Color>("--off-color");
        private static readonly CustomStyleProperty<float> s_Size = 
            new CustomStyleProperty<float>("--led-size");
        
        // ====================================================================
        // STATE
        // ====================================================================
        
        private bool _isOn = false;
        private bool _isFlashing = false;
        private float _flashRate = 2f; // Hz
        private bool _flashState = true;
        private long _lastFlashTicks;
        
        private Color _onColor = new Color(0.18f, 0.85f, 0.25f, 1f);  // Green
        private Color _offColor = new Color(0.25f, 0.26f, 0.30f, 1f); // Dark gray
        private float _ledSize = 12f;
        private bool _showGlow = true;
        
        private string _label = "";
        private Label _labelElement;
        
        // ====================================================================
        // UXML ATTRIBUTES
        // ====================================================================
        
        [UxmlAttribute]
        public bool isOn
        {
            get => _isOn;
            set
            {
                _isOn = value;
                MarkDirtyRepaint();
            }
        }
        
        [UxmlAttribute]
        public bool isFlashing
        {
            get => _isFlashing;
            set
            {
                _isFlashing = value;
                if (value)
                    _lastFlashTicks = DateTime.Now.Ticks;
                MarkDirtyRepaint();
            }
        }
        
        [UxmlAttribute]
        public float flashRate
        {
            get => _flashRate;
            set => _flashRate = Mathf.Max(0.1f, value);
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
        public bool showGlow
        {
            get => _showGlow;
            set
            {
                _showGlow = value;
                MarkDirtyRepaint();
            }
        }
        
        // ====================================================================
        // PUBLIC PROPERTIES
        // ====================================================================
        
        /// <summary>Current display color (accounts for on/off and flash state).</summary>
        public Color CurrentColor => GetCurrentColor();
        
        /// <summary>Set the on-state color.</summary>
        public Color OnColor
        {
            get => _onColor;
            set
            {
                _onColor = value;
                MarkDirtyRepaint();
            }
        }
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public LEDIndicatorElement()
        {
            AddToClassList(ussClassName);
            
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.minWidth = _ledSize + 4;
            style.minHeight = _ledSize + 4;
            
            // Register callbacks
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStylesResolved);
            generateVisualContent += OnGenerateVisualContent;
            
            _lastFlashTicks = DateTime.Now.Ticks;
        }
        
        // ====================================================================
        // PUBLIC API
        // ====================================================================
        
        /// <summary>
        /// Set the LED state with optional color override.
        /// </summary>
        public void SetState(bool on, Color? color = null)
        {
            _isOn = on;
            if (color.HasValue)
                _onColor = color.Value;
            MarkDirtyRepaint();
        }
        
        /// <summary>
        /// Configure as a status LED with specific color states.
        /// </summary>
        public void Configure(string labelText, Color onColor, bool startOn = false)
        {
            _label = labelText;
            _onColor = onColor;
            _isOn = startOn;
            
            // Add label element if text provided
            if (!string.IsNullOrEmpty(labelText) && _labelElement == null)
            {
                _labelElement = new Label(labelText);
                _labelElement.AddToClassList(ussLabelClassName);
                _labelElement.style.fontSize = 11;
                _labelElement.style.color = new Color(0.75f, 0.78f, 0.82f, 1f);
                _labelElement.style.marginLeft = 6;
                Add(_labelElement);
            }
            else if (_labelElement != null)
            {
                _labelElement.text = labelText;
            }
            
            MarkDirtyRepaint();
        }
        
        /// <summary>
        /// Update flash animation. Call from controller's update loop.
        /// </summary>
        public void UpdateFlash()
        {
            if (!_isFlashing) return;
            
            long nowTicks = DateTime.Now.Ticks;
            float elapsed = (nowTicks - _lastFlashTicks) / 10000000f;
            float period = 1f / _flashRate;
            
            if (elapsed >= period * 0.5f)
            {
                _flashState = !_flashState;
                _lastFlashTicks = nowTicks;
                MarkDirtyRepaint();
            }
        }
        
        // ====================================================================
        // RENDERING
        // ====================================================================
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            // Calculate drawable area for LED (leave space for label)
            float ledDrawSize = _ledSize + 8; // Extra for glow
            float width = Mathf.Min(contentRect.width, ledDrawSize);
            float height = Mathf.Min(contentRect.height, ledDrawSize);
            
            if (width < 4f || height < 4f) return;
            
            var painter = mgc.painter2D;
            if (painter == null) return;
            
            Vector2 center = new Vector2(ledDrawSize * 0.5f, contentRect.height * 0.5f);
            float radius = _ledSize * 0.5f;
            
            Color currentColor = GetCurrentColor();
            
            // 1. Draw glow (if on and enabled)
            if (_showGlow && (_isOn || _isFlashing) && (!_isFlashing || _flashState))
            {
                Color glowColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0.3f);
                
                // Outer glow
                painter.fillColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.15f);
                painter.BeginPath();
                painter.Arc(center, radius * 2f, 0f, 360f);
                painter.ClosePath();
                painter.Fill();
                
                // Inner glow
                painter.fillColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.25f);
                painter.BeginPath();
                painter.Arc(center, radius * 1.4f, 0f, 360f);
                painter.ClosePath();
                painter.Fill();
            }
            
            // 2. Draw LED body (dark ring)
            painter.fillColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            painter.BeginPath();
            painter.Arc(center, radius + 1f, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
            
            // 3. Draw LED face
            painter.fillColor = currentColor;
            painter.BeginPath();
            painter.Arc(center, radius, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
            
            // 4. Draw highlight (small white arc at top for 3D effect)
            if (_isOn || (_isFlashing && _flashState))
            {
                Color highlightColor = new Color(1f, 1f, 1f, 0.4f);
                painter.fillColor = highlightColor;
                painter.BeginPath();
                painter.Arc(center + new Vector2(-radius * 0.25f, -radius * 0.25f), 
                    radius * 0.35f, 0f, 360f);
                painter.ClosePath();
                painter.Fill();
            }
        }
        
        // ====================================================================
        // HELPERS
        // ====================================================================
        
        private Color GetCurrentColor()
        {
            if (_isFlashing)
            {
                return _flashState ? _onColor : _offColor;
            }
            return _isOn ? _onColor : _offColor;
        }
        
        private void OnCustomStylesResolved(CustomStyleResolvedEvent evt)
        {
            bool repaint = false;
            
            if (customStyle.TryGetValue(s_OnColor, out var onColor))
            {
                _onColor = onColor;
                repaint = true;
            }
            if (customStyle.TryGetValue(s_OffColor, out var offColor))
            {
                _offColor = offColor;
                repaint = true;
            }
            if (customStyle.TryGetValue(s_Size, out var size))
            {
                _ledSize = size;
                repaint = true;
            }
            
            if (repaint)
                MarkDirtyRepaint();
        }
    }
}
