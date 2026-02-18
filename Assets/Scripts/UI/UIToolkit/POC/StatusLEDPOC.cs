// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// StatusLEDPOC.cs — Simple status indicator LED
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    public enum LEDState
    {
        Off,
        Normal,
        Warning,
        Alarm
    }
    
    [UxmlElement]
    public partial class StatusLEDPOC : VisualElement
    {
        // ====================================================================
        // COLORS
        // ====================================================================
        
        private static readonly Color COLOR_OFF = new Color(0.15f, 0.15f, 0.18f, 1f);
        private static readonly Color COLOR_OFF_BORDER = new Color(0.25f, 0.25f, 0.3f, 1f);
        private static readonly Color COLOR_NORMAL = new Color(0f, 1f, 0.533f, 1f);
        private static readonly Color COLOR_WARNING = new Color(1f, 0.667f, 0f, 1f);
        private static readonly Color COLOR_ALARM = new Color(1f, 0.267f, 0.267f, 1f);
        
        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        private LEDState _state = LEDState.Off;
        private bool _pulsing = false;
        
        private IVisualElementScheduledItem _pulseSchedule;
        private bool _pulseOn = true;
        
        [UxmlAttribute]
        public LEDState state
        {
            get => _state;
            set { _state = value; MarkDirtyRepaint(); }
        }
        
        [UxmlAttribute]
        public bool pulsing
        {
            get => _pulsing;
            set
            {
                _pulsing = value;
                UpdatePulseSchedule();
                MarkDirtyRepaint();
            }
        }
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public StatusLEDPOC()
        {
            style.width = 16;
            style.height = 16;
            
            generateVisualContent += OnGenerateVisualContent;
            
            RegisterCallback<AttachToPanelEvent>(evt => UpdatePulseSchedule());
            RegisterCallback<DetachFromPanelEvent>(evt => { _pulseSchedule?.Pause(); _pulseSchedule = null; });
        }
        
        private void UpdatePulseSchedule()
        {
            _pulseSchedule?.Pause();
            _pulseSchedule = null;
            
            if (_pulsing && _state != LEDState.Off)
            {
                _pulseSchedule = schedule.Execute(() => { _pulseOn = !_pulseOn; MarkDirtyRepaint(); }).Every(250);
            }
            else
            {
                _pulseOn = true;
            }
        }
        
        // ====================================================================
        // RENDERING
        // ====================================================================
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            float size = Mathf.Min(contentRect.width, contentRect.height);
            if (size < 4f) return;
            
            var painter = mgc.painter2D;
            
            Vector2 center = new Vector2(contentRect.width / 2f, contentRect.height / 2f);
            float radius = size / 2f - 2f;
            
            // Get color
            Color ledColor = COLOR_OFF;
            Color borderColor = COLOR_OFF_BORDER;
            
            if (_pulseOn || !_pulsing)
            {
                switch (_state)
                {
                    case LEDState.Normal:
                        ledColor = COLOR_NORMAL;
                        borderColor = COLOR_NORMAL;
                        break;
                    case LEDState.Warning:
                        ledColor = COLOR_WARNING;
                        borderColor = COLOR_WARNING;
                        break;
                    case LEDState.Alarm:
                        ledColor = COLOR_ALARM;
                        borderColor = COLOR_ALARM;
                        break;
                }
            }
            
            // Draw LED fill
            painter.fillColor = ledColor;
            painter.BeginPath();
            painter.Arc(center, radius, 0f, 360f);
            painter.ClosePath();
            painter.Fill();
            
            // Draw border
            painter.strokeColor = borderColor;
            painter.lineWidth = 1.5f;
            painter.BeginPath();
            painter.Arc(center, radius, 0f, 360f);
            painter.Stroke();
            
            // Draw highlight (top-left)
            if (_state != LEDState.Off && (_pulseOn || !_pulsing))
            {
                painter.fillColor = new Color(1f, 1f, 1f, 0.3f);
                painter.BeginPath();
                painter.Arc(new Vector2(center.x - radius * 0.3f, center.y - radius * 0.3f), radius * 0.25f, 0f, 360f);
                painter.ClosePath();
                painter.Fill();
            }
        }
    }
}
