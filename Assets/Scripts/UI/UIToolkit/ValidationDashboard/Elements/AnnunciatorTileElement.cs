// ============================================================================
// CRITICAL: Master the Atom — Annunciator Tile Element (UI Toolkit)
// AnnunciatorTileElement.cs
// ============================================================================
//
// PURPOSE:
//   Custom UI Toolkit VisualElement for a single annunciator window tile.
//   Implements ISA-18.1 alarm sequence with authentic industrial light box
//   appearance matching APEX-style annunciator panels.
//
// ISA-18.1 STATE MACHINE:
//   INACTIVE  → Condition not active, tile dark
//   ALERTING  → New alarm, unacknowledged, fast flash (3 Hz)
//   ACKNOWLEDGED → Alarm active, acknowledged, steady on
//   CLEARING  → Condition cleared but not reset, slow flash (0.7 Hz)
//
// FEATURES:
//   - Configurable color type (green/amber/red/white)
//   - ISA-18.1 compliant state machine
//   - Click-to-acknowledge support
//   - Smooth flash transitions
//   - Size variants (compact/standard/large)
//
// REFERENCE:
//   ANSI/ISA-18.1 — Annunciator Sequences and Specifications
//   APEX 60 Channel Light Box Indicator
//   NRC HRTD Section 4 — Annunciator Window conventions
//
// VERSION: 0.1.0-POC
// DATE: 2026-02-18
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace Critical.UI.Elements
{
    // ========================================================================
    // ENUMERATIONS
    // ========================================================================
    
    /// <summary>
    /// ISA-18.1 annunciator state sequence.
    /// </summary>
    public enum AnnunciatorTileState
    {
        /// <summary>Dark, condition not active.</summary>
        Inactive,
        
        /// <summary>Fast flash (3 Hz), new alarm not yet acknowledged.</summary>
        Alerting,
        
        /// <summary>Steady on, alarm acknowledged but condition still active.</summary>
        Acknowledged,
        
        /// <summary>Slow flash (0.7 Hz), condition cleared but not yet reset.</summary>
        Clearing
    }
    
    /// <summary>
    /// Tile color type determining the illumination color when active.
    /// </summary>
    public enum AnnunciatorTileColor
    {
        /// <summary>Green - normal status indicators (running, OK).</summary>
        Green,
        
        /// <summary>Amber - warning conditions (approaching limits).</summary>
        Amber,
        
        /// <summary>Red - alarm conditions (limits exceeded, trips).</summary>
        Red,
        
        /// <summary>White - neutral/informational indicators.</summary>
        White
    }
    
    // ========================================================================
    // ANNUNCIATOR TILE ELEMENT
    // ========================================================================
    
    /// <summary>
    /// Single annunciator window tile with ISA-18.1 state machine.
    /// </summary>
    public class AnnunciatorTileElement : VisualElement
    {
        // ====================================================================
        // UXML FACTORY
        // ====================================================================
        
        public new class UxmlFactory : UxmlFactory<AnnunciatorTileElement, UxmlTraits> { }
        
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private UxmlStringAttributeDescription m_Label =
                new UxmlStringAttributeDescription { name = "label", defaultValue = "ALARM" };
            
            private UxmlStringAttributeDescription m_Tag =
                new UxmlStringAttributeDescription { name = "tag", defaultValue = "" };
            
            private UxmlEnumAttributeDescription<AnnunciatorTileColor> m_Color =
                new UxmlEnumAttributeDescription<AnnunciatorTileColor> { name = "tile-color", defaultValue = AnnunciatorTileColor.Red };
            
            private UxmlBoolAttributeDescription m_IsStatus =
                new UxmlBoolAttributeDescription { name = "is-status", defaultValue = false };
            
            private UxmlBoolAttributeDescription m_Clickable =
                new UxmlBoolAttributeDescription { name = "clickable", defaultValue = true };
            
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var tile = ve as AnnunciatorTileElement;
                
                tile.Label = m_Label.GetValueFromBag(bag, cc);
                tile.Tag = m_Tag.GetValueFromBag(bag, cc);
                tile.TileColor = m_Color.GetValueFromBag(bag, cc);
                tile.IsStatus = m_IsStatus.GetValueFromBag(bag, cc);
                tile.Clickable = m_Clickable.GetValueFromBag(bag, cc);
            }
        }
        
        // ====================================================================
        // CONSTANTS
        // ====================================================================
        
        private const string USS_TILE = "annunciator-tile";
        private const string USS_LABEL = "annunciator-tile__label";
        private const string USS_CLICKABLE = "annunciator-tile--clickable";
        private const string USS_INACTIVE = "annunciator-tile--inactive";
        private const string USS_ACKNOWLEDGED = "annunciator-tile--acknowledged";
        private const string USS_FLASH_OFF = "annunciator-tile--flash-off";
        
        private const float ALERT_FLASH_HZ = 3.0f;    // ISA-18.1 fast flash
        private const float CLEAR_FLASH_HZ = 0.7f;    // ISA-18.1 slow flash
        private const float AUTO_RESET_DELAY = 5.0f;  // Seconds before auto-reset from Clearing
        
        // ====================================================================
        // BACKING FIELDS
        // ====================================================================
        
        private string m_Label = "ALARM";
        private string m_Tag = "";
        private AnnunciatorTileColor m_TileColor = AnnunciatorTileColor.Red;
        private AnnunciatorTileState m_State = AnnunciatorTileState.Inactive;
        private bool m_IsStatus;
        private bool m_Clickable = true;
        private bool m_ConditionActive;
        
        // Flash state
        private IVisualElementScheduledItem m_FlashSchedule;
        private bool m_FlashOn = true;
        private float m_ClearingEnteredTime;
        
        // Child elements
        private Label m_LabelElement;
        
        // Events
        public event Action<AnnunciatorTileElement> OnAcknowledged;
        public event Action<AnnunciatorTileElement> OnClicked;
        
        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        /// <summary>Text label displayed on the tile (supports \n for two lines).</summary>
        public string Label
        {
            get => m_Label;
            set
            {
                m_Label = value;
                if (m_LabelElement != null)
                    m_LabelElement.text = value;
            }
        }
        
        /// <summary>Optional tag/ID for identification (e.g., "XV-0551A").</summary>
        public string Tag
        {
            get => m_Tag;
            set => m_Tag = value;
        }
        
        /// <summary>Color type determining illumination color when active.</summary>
        public AnnunciatorTileColor TileColor
        {
            get => m_TileColor;
            set
            {
                m_TileColor = value;
                UpdateVisualState();
            }
        }
        
        /// <summary>Current ISA-18.1 state.</summary>
        public AnnunciatorTileState State => m_State;
        
        /// <summary>Whether the underlying condition is currently active.</summary>
        public bool ConditionActive => m_ConditionActive;
        
        /// <summary>
        /// If true, this is a status indicator (green when lit, no state machine).
        /// Status tiles don't flash and don't require acknowledgment.
        /// </summary>
        public bool IsStatus
        {
            get => m_IsStatus;
            set
            {
                m_IsStatus = value;
                if (m_IsStatus)
                    m_TileColor = AnnunciatorTileColor.Green;
            }
        }
        
        /// <summary>Whether the tile responds to clicks (for acknowledgment).</summary>
        public bool Clickable
        {
            get => m_Clickable;
            set
            {
                m_Clickable = value;
                EnableInClassList(USS_CLICKABLE, value);
            }
        }
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public AnnunciatorTileElement()
        {
            AddToClassList(USS_TILE);
            AddToClassList(USS_INACTIVE);
            
            // Create label
            m_LabelElement = new Label(m_Label);
            m_LabelElement.AddToClassList(USS_LABEL);
            Add(m_LabelElement);
            
            // Register click handler
            RegisterCallback<ClickEvent>(OnClick);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }
        
        // ====================================================================
        // LIFECYCLE
        // ====================================================================
        
        private void OnAttach(AttachToPanelEvent evt)
        {
            UpdateFlashSchedule();
        }
        
        private void OnDetach(DetachFromPanelEvent evt)
        {
            StopFlash();
        }
        
        // ====================================================================
        // ISA-18.1 STATE MACHINE
        // ====================================================================
        
        /// <summary>
        /// Update the underlying condition. Drives ISA-18.1 state transitions.
        /// Call this each data refresh cycle with the current condition value.
        /// </summary>
        public void UpdateCondition(bool active)
        {
            bool wasActive = m_ConditionActive;
            m_ConditionActive = active;
            
            // Status tiles: simple on/off, no state machine
            if (m_IsStatus)
            {
                m_State = active ? AnnunciatorTileState.Acknowledged : AnnunciatorTileState.Inactive;
                UpdateVisualState();
                return;
            }
            
            // ISA-18.1 state transitions
            if (active && !wasActive)
            {
                // Condition onset
                if (m_State == AnnunciatorTileState.Inactive || m_State == AnnunciatorTileState.Clearing)
                {
                    m_State = AnnunciatorTileState.Alerting;
                    UpdateFlashSchedule();
                }
            }
            else if (!active && wasActive)
            {
                // Condition cleared
                switch (m_State)
                {
                    case AnnunciatorTileState.Alerting:
                    case AnnunciatorTileState.Acknowledged:
                        // Move to clearing (slow flash)
                        m_State = AnnunciatorTileState.Clearing;
                        m_ClearingEnteredTime = Time.realtimeSinceStartup;
                        UpdateFlashSchedule();
                        break;
                }
            }
            
            UpdateVisualState();
        }
        
        /// <summary>
        /// Acknowledge this tile. ALERTING → ACKNOWLEDGED.
        /// </summary>
        public void Acknowledge()
        {
            if (m_State == AnnunciatorTileState.Alerting)
            {
                m_State = AnnunciatorTileState.Acknowledged;
                StopFlash();
                m_FlashOn = true;
                UpdateVisualState();
                OnAcknowledged?.Invoke(this);
            }
        }
        
        /// <summary>
        /// Reset this tile. CLEARING → INACTIVE.
        /// </summary>
        public void Reset()
        {
            if (m_State == AnnunciatorTileState.Clearing)
            {
                m_State = AnnunciatorTileState.Inactive;
                StopFlash();
                m_FlashOn = true;
                UpdateVisualState();
            }
        }
        
        /// <summary>
        /// Force the tile to a specific state (for testing/demo).
        /// </summary>
        public void SetState(AnnunciatorTileState state)
        {
            m_State = state;
            m_ConditionActive = (state != AnnunciatorTileState.Inactive && state != AnnunciatorTileState.Clearing);
            UpdateFlashSchedule();
            UpdateVisualState();
        }
        
        // ====================================================================
        // FLASH CONTROL
        // ====================================================================
        
        private void UpdateFlashSchedule()
        {
            StopFlash();
            
            float flashHz = 0f;
            
            switch (m_State)
            {
                case AnnunciatorTileState.Alerting:
                    flashHz = ALERT_FLASH_HZ;
                    break;
                case AnnunciatorTileState.Clearing:
                    flashHz = CLEAR_FLASH_HZ;
                    break;
                default:
                    m_FlashOn = true;
                    return;
            }
            
            if (flashHz > 0f)
            {
                long intervalMs = (long)(1000f / flashHz / 2f);
                m_FlashSchedule = schedule.Execute(ToggleFlash).Every(intervalMs);
            }
        }
        
        private void StopFlash()
        {
            m_FlashSchedule?.Pause();
            m_FlashSchedule = null;
        }
        
        private void ToggleFlash()
        {
            m_FlashOn = !m_FlashOn;
            UpdateVisualState();
            
            // Check for auto-reset from Clearing state
            if (m_State == AnnunciatorTileState.Clearing)
            {
                if (Time.realtimeSinceStartup - m_ClearingEnteredTime >= AUTO_RESET_DELAY)
                {
                    Reset();
                }
            }
        }
        
        // ====================================================================
        // VISUAL STATE
        // ====================================================================
        
        private void UpdateVisualState()
        {
            // Remove all state classes
            RemoveFromClassList(USS_INACTIVE);
            RemoveFromClassList(USS_ACKNOWLEDGED);
            RemoveFromClassList(USS_FLASH_OFF);
            RemoveFromClassList("annunciator-tile--green");
            RemoveFromClassList("annunciator-tile--green-lit");
            RemoveFromClassList("annunciator-tile--amber");
            RemoveFromClassList("annunciator-tile--amber-lit");
            RemoveFromClassList("annunciator-tile--red");
            RemoveFromClassList("annunciator-tile--red-lit");
            RemoveFromClassList("annunciator-tile--white");
            
            // Determine visual state
            if (m_State == AnnunciatorTileState.Inactive)
            {
                AddToClassList(USS_INACTIVE);
                return;
            }
            
            // Check flash state
            bool showLit = m_FlashOn || m_State == AnnunciatorTileState.Acknowledged;
            
            if (!showLit)
            {
                AddToClassList(USS_FLASH_OFF);
                return;
            }
            
            // Apply color class
            string colorClass = GetColorClass(m_State == AnnunciatorTileState.Alerting);
            AddToClassList(colorClass);
            
            // Acknowledged state gets dimmed
            if (m_State == AnnunciatorTileState.Acknowledged)
            {
                AddToClassList(USS_ACKNOWLEDGED);
            }
        }
        
        private string GetColorClass(bool lit)
        {
            string suffix = lit ? "-lit" : "";
            
            switch (m_TileColor)
            {
                case AnnunciatorTileColor.Green:
                    return $"annunciator-tile--green{suffix}";
                case AnnunciatorTileColor.Amber:
                    return $"annunciator-tile--amber{suffix}";
                case AnnunciatorTileColor.Red:
                    return $"annunciator-tile--red{suffix}";
                case AnnunciatorTileColor.White:
                    return "annunciator-tile--white";
                default:
                    return $"annunciator-tile--red{suffix}";
            }
        }
        
        // ====================================================================
        // EVENT HANDLERS
        // ====================================================================
        
        private void OnClick(ClickEvent evt)
        {
            if (!m_Clickable) return;
            
            OnClicked?.Invoke(this);
            
            // Click to acknowledge
            if (m_State == AnnunciatorTileState.Alerting)
            {
                Acknowledge();
            }
            // Click to reset from clearing
            else if (m_State == AnnunciatorTileState.Clearing)
            {
                Reset();
            }
        }
    }
}
