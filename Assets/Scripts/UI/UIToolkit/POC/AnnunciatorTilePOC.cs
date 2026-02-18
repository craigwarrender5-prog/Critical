// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// AnnunciatorTilePOC.cs — ISA-18.1 style alarm annunciator tile
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    public enum AnnunciatorState
    {
        Normal,      // Dark/off
        Alerting,    // Flashing (new alarm, unacknowledged)
        Acknowledged,// Steady on (alarm active, acknowledged)
        Clearing     // Slow flash (returning to normal)
    }
    
    [UxmlElement]
    public partial class AnnunciatorTilePOC : VisualElement
    {
        // ====================================================================
        // CONSTANTS
        // ====================================================================
        
        private const float ALERT_FLASH_RATE = 3f;    // Hz - fast flash for new alarms
        private const float CLEAR_FLASH_RATE = 0.7f;  // Hz - slow flash for clearing
        
        // ====================================================================
        // COLORS
        // ====================================================================
        
        private static readonly Color COLOR_NORMAL_BG = new Color(0.08f, 0.08f, 0.1f, 1f);
        private static readonly Color COLOR_NORMAL_BORDER = new Color(0.2f, 0.2f, 0.25f, 1f);
        private static readonly Color COLOR_NORMAL_TEXT = new Color(0.3f, 0.3f, 0.35f, 1f);
        
        private static readonly Color COLOR_WARNING_BG = new Color(0.4f, 0.27f, 0f, 1f);
        private static readonly Color COLOR_WARNING_BORDER = new Color(1f, 0.667f, 0f, 1f);
        private static readonly Color COLOR_WARNING_TEXT = new Color(1f, 0.667f, 0f, 1f);
        
        private static readonly Color COLOR_ALARM_BG = new Color(0.4f, 0.1f, 0.1f, 1f);
        private static readonly Color COLOR_ALARM_BORDER = new Color(1f, 0.267f, 0.267f, 1f);
        private static readonly Color COLOR_ALARM_TEXT = new Color(1f, 0.267f, 0.267f, 1f);
        
        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        private string _title = "ALARM";
        private string _description = "Description";
        private string _value = "";
        private AnnunciatorState _state = AnnunciatorState.Normal;
        private bool _isWarning = false;  // false = alarm (red), true = warning (amber)
        
        private IVisualElementScheduledItem _flashSchedule;
        private bool _flashOn = true;
        
        [UxmlAttribute]
        public string title { get => _title; set { _title = value; MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public string description { get => _description; set { _description = value; MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public string value { get => _value; set { _value = value; MarkDirtyRepaint(); } }
        
        [UxmlAttribute]
        public bool isWarning { get => _isWarning; set { _isWarning = value; MarkDirtyRepaint(); } }
        
        public AnnunciatorState state
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    UpdateFlashSchedule();
                    MarkDirtyRepaint();
                }
            }
        }
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public AnnunciatorTilePOC()
        {
            style.minWidth = 80;
            style.minHeight = 50;
            
            generateVisualContent += OnGenerateVisualContent;
            
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }
        
        private void OnAttach(AttachToPanelEvent evt)
        {
            UpdateFlashSchedule();
        }
        
        private void OnDetach(DetachFromPanelEvent evt)
        {
            _flashSchedule?.Pause();
            _flashSchedule = null;
        }
        
        private void UpdateFlashSchedule()
        {
            _flashSchedule?.Pause();
            _flashSchedule = null;
            
            if (_state == AnnunciatorState.Alerting)
            {
                long intervalMs = (long)(1000f / ALERT_FLASH_RATE / 2f);
                _flashSchedule = schedule.Execute(ToggleFlash).Every(intervalMs);
            }
            else if (_state == AnnunciatorState.Clearing)
            {
                long intervalMs = (long)(1000f / CLEAR_FLASH_RATE / 2f);
                _flashSchedule = schedule.Execute(ToggleFlash).Every(intervalMs);
            }
            else
            {
                _flashOn = true;
            }
        }
        
        private void ToggleFlash()
        {
            _flashOn = !_flashOn;
            MarkDirtyRepaint();
        }
        
        // ====================================================================
        // RENDERING
        // ====================================================================
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            float width = contentRect.width;
            float height = contentRect.height;
            
            if (width < 20f || height < 20f) return;
            
            var painter = mgc.painter2D;
            
            // Determine colors based on state
            Color bgColor, borderColor, textColor;
            GetStateColors(out bgColor, out borderColor, out textColor);
            
            // Apply flash state
            if (!_flashOn && (_state == AnnunciatorState.Alerting || _state == AnnunciatorState.Clearing))
            {
                bgColor = COLOR_NORMAL_BG;
                borderColor = COLOR_NORMAL_BORDER;
                textColor = COLOR_NORMAL_TEXT;
            }
            
            // Draw background
            painter.fillColor = bgColor;
            painter.BeginPath();
            DrawRoundedRect(painter, 1f, 1f, width - 2f, height - 2f, 4f);
            painter.Fill();
            
            // Draw border
            painter.strokeColor = borderColor;
            painter.lineWidth = 2f;
            painter.BeginPath();
            DrawRoundedRect(painter, 1f, 1f, width - 2f, height - 2f, 4f);
            painter.Stroke();
            
            // Draw alert indicator triangle (top-left corner when active)
            if (_state != AnnunciatorState.Normal && _flashOn)
            {
                painter.fillColor = borderColor;
                painter.BeginPath();
                painter.MoveTo(new Vector2(4f, 4f));
                painter.LineTo(new Vector2(16f, 4f));
                painter.LineTo(new Vector2(4f, 16f));
                painter.ClosePath();
                painter.Fill();
            }
        }
        
        private void GetStateColors(out Color bg, out Color border, out Color text)
        {
            if (_state == AnnunciatorState.Normal)
            {
                bg = COLOR_NORMAL_BG;
                border = COLOR_NORMAL_BORDER;
                text = COLOR_NORMAL_TEXT;
            }
            else if (_isWarning)
            {
                bg = COLOR_WARNING_BG;
                border = COLOR_WARNING_BORDER;
                text = COLOR_WARNING_TEXT;
            }
            else
            {
                bg = COLOR_ALARM_BG;
                border = COLOR_ALARM_BORDER;
                text = COLOR_ALARM_TEXT;
            }
        }
        
        private void DrawRoundedRect(Painter2D painter, float x, float y, float w, float h, float r)
        {
            painter.MoveTo(new Vector2(x + r, y));
            painter.LineTo(new Vector2(x + w - r, y));
            painter.Arc(new Vector2(x + w - r, y + r), r, 270f, 360f);
            painter.LineTo(new Vector2(x + w, y + h - r));
            painter.Arc(new Vector2(x + w - r, y + h - r), r, 0f, 90f);
            painter.LineTo(new Vector2(x + r, y + h));
            painter.Arc(new Vector2(x + r, y + h - r), r, 90f, 180f);
            painter.LineTo(new Vector2(x, y + r));
            painter.Arc(new Vector2(x + r, y + r), r, 180f, 270f);
            painter.ClosePath();
        }
    }
}
