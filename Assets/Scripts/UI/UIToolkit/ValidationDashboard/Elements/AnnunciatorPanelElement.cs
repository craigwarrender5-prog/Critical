// ============================================================================
// CRITICAL: Master the Atom — Annunciator Panel Element (UI Toolkit)
// AnnunciatorPanelElement.cs
// ============================================================================
//
// PURPOSE:
//   Custom UI Toolkit VisualElement for a complete annunciator panel containing
//   multiple tiles in a grid layout. Provides panel-level operations like
//   Acknowledge All, Silence, and Reset.
//
// FEATURES:
//   - Grid layout with configurable columns
//   - Panel-level acknowledge/silence/reset
//   - Active alarm count tracking
//   - Optional audio feedback hooks
//   - Industrial frame styling
//
// REFERENCE:
//   APEX 60 Channel Light Box Indicator
//   ANSI/ISA-18.1 — Annunciator Sequences and Specifications
//
// VERSION: 0.1.0-POC
// DATE: 2026-02-18
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace Critical.UI.Elements
{
    /// <summary>
    /// Complete annunciator panel with grid of tiles and control buttons.
    /// </summary>
    public class AnnunciatorPanelElement : VisualElement
    {
        // ====================================================================
        // UXML FACTORY
        // ====================================================================
        
        public new class UxmlFactory : UxmlFactory<AnnunciatorPanelElement, UxmlTraits> { }
        
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private UxmlStringAttributeDescription m_Title =
                new UxmlStringAttributeDescription { name = "title", defaultValue = "ANNUNCIATOR PANEL" };
            
            private UxmlIntAttributeDescription m_Columns =
                new UxmlIntAttributeDescription { name = "columns", defaultValue = 10 };
            
            private UxmlBoolAttributeDescription m_ShowControls =
                new UxmlBoolAttributeDescription { name = "show-controls", defaultValue = true };
            
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var panel = ve as AnnunciatorPanelElement;
                
                panel.Title = m_Title.GetValueFromBag(bag, cc);
                panel.Columns = m_Columns.GetValueFromBag(bag, cc);
                panel.ShowControls = m_ShowControls.GetValueFromBag(bag, cc);
            }
        }
        
        // ====================================================================
        // CONSTANTS
        // ====================================================================
        
        private const string USS_PANEL = "annunciator-panel";
        private const string USS_INNER = "annunciator-panel__inner-frame";
        private const string USS_TITLE_BAR = "annunciator-panel__title-bar";
        private const string USS_TITLE = "annunciator-panel__title";
        private const string USS_STATUS = "annunciator-panel__status";
        private const string USS_GRID = "annunciator-panel__grid";
        private const string USS_CONTROLS = "annunciator-panel__controls";
        private const string USS_BUTTON = "annunciator-button";
        private const string USS_BUTTON_LABEL = "annunciator-button__label";
        private const string USS_BUTTON_ACK_ACTIVE = "annunciator-button--ack-active";
        
        // ====================================================================
        // BACKING FIELDS
        // ====================================================================
        
        private string m_Title = "ANNUNCIATOR PANEL";
        private int m_Columns = 10;
        private bool m_ShowControls = true;
        private bool m_Silenced;
        
        // Child elements
        private VisualElement m_InnerFrame;
        private Label m_TitleLabel;
        private Label m_StatusLabel;
        private VisualElement m_Grid;
        private VisualElement m_ControlsContainer;
        private Button m_AckButton;
        private Button m_SilenceButton;
        private Button m_ResetButton;
        
        // Tile tracking
        private List<AnnunciatorTileElement> m_Tiles = new List<AnnunciatorTileElement>();
        
        // Events
        public event Action OnAcknowledgeAll;
        public event Action OnSilence;
        public event Action OnResetAll;
        public event Action<AnnunciatorTileElement> OnTileAcknowledged;
        
        // ====================================================================
        // PROPERTIES
        // ====================================================================
        
        /// <summary>Panel title displayed in the title bar.</summary>
        public string Title
        {
            get => m_Title;
            set
            {
                m_Title = value;
                if (m_TitleLabel != null)
                    m_TitleLabel.text = value;
            }
        }
        
        /// <summary>Number of columns in the tile grid.</summary>
        public int Columns
        {
            get => m_Columns;
            set
            {
                m_Columns = Mathf.Max(1, value);
                UpdateGridLayout();
            }
        }
        
        /// <summary>Whether to show ACK/SILENCE/RESET buttons.</summary>
        public bool ShowControls
        {
            get => m_ShowControls;
            set
            {
                m_ShowControls = value;
                if (m_ControlsContainer != null)
                    m_ControlsContainer.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        /// <summary>Whether alarms are silenced.</summary>
        public bool Silenced => m_Silenced;
        
        /// <summary>All tiles in the panel.</summary>
        public IReadOnlyList<AnnunciatorTileElement> Tiles => m_Tiles;
        
        /// <summary>Count of tiles currently in ALERTING state (unacknowledged).</summary>
        public int AlertingCount
        {
            get
            {
                int count = 0;
                foreach (var tile in m_Tiles)
                {
                    if (tile.State == AnnunciatorTileState.Alerting)
                        count++;
                }
                return count;
            }
        }
        
        /// <summary>Count of tiles with active conditions.</summary>
        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (var tile in m_Tiles)
                {
                    if (tile.State != AnnunciatorTileState.Inactive)
                        count++;
                }
                return count;
            }
        }
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public AnnunciatorPanelElement()
        {
            AddToClassList(USS_PANEL);
            
            BuildVisualTree();
        }
        
        // ====================================================================
        // VISUAL TREE CONSTRUCTION
        // ====================================================================
        
        private void BuildVisualTree()
        {
            // Inner frame
            m_InnerFrame = new VisualElement();
            m_InnerFrame.AddToClassList(USS_INNER);
            Add(m_InnerFrame);
            
            // Title bar
            var titleBar = new VisualElement();
            titleBar.AddToClassList(USS_TITLE_BAR);
            m_InnerFrame.Add(titleBar);
            
            m_TitleLabel = new Label(m_Title);
            m_TitleLabel.AddToClassList(USS_TITLE);
            titleBar.Add(m_TitleLabel);
            
            m_StatusLabel = new Label("0 ACTIVE");
            m_StatusLabel.AddToClassList(USS_STATUS);
            titleBar.Add(m_StatusLabel);
            
            // Tile grid
            m_Grid = new VisualElement();
            m_Grid.AddToClassList(USS_GRID);
            m_InnerFrame.Add(m_Grid);
            
            // Control buttons
            m_ControlsContainer = new VisualElement();
            m_ControlsContainer.AddToClassList(USS_CONTROLS);
            m_InnerFrame.Add(m_ControlsContainer);
            
            m_AckButton = CreateButton("ACK", OnAckClick);
            m_SilenceButton = CreateButton("SILENCE", OnSilenceClick);
            m_ResetButton = CreateButton("RESET", OnResetClick);
            
            m_ControlsContainer.Add(m_AckButton);
            m_ControlsContainer.Add(m_SilenceButton);
            m_ControlsContainer.Add(m_ResetButton);
            
            // Schedule status updates
            schedule.Execute(UpdateStatus).Every(500);
        }
        
        private Button CreateButton(string text, Action clickAction)
        {
            var button = new Button(clickAction);
            button.AddToClassList(USS_BUTTON);
            
            var label = new Label(text);
            label.AddToClassList(USS_BUTTON_LABEL);
            button.Add(label);
            
            return button;
        }
        
        private void UpdateGridLayout()
        {
            if (m_Grid == null) return;
            
            m_Grid.style.flexWrap = Wrap.Wrap;
            m_Grid.style.flexDirection = FlexDirection.Row;
        }
        
        // ====================================================================
        // TILE MANAGEMENT
        // ====================================================================
        
        /// <summary>
        /// Add a tile to the panel.
        /// </summary>
        public AnnunciatorTileElement AddTile(string label, AnnunciatorTileColor color, bool isStatus = false)
        {
            var tile = new AnnunciatorTileElement
            {
                Label = label,
                TileColor = color,
                IsStatus = isStatus,
                Clickable = true
            };
            
            tile.OnAcknowledged += OnTileAck;
            
            m_Tiles.Add(tile);
            m_Grid.Add(tile);
            
            return tile;
        }
        
        /// <summary>
        /// Add a pre-configured tile to the panel.
        /// </summary>
        public void AddTile(AnnunciatorTileElement tile)
        {
            tile.OnAcknowledged += OnTileAck;
            m_Tiles.Add(tile);
            m_Grid.Add(tile);
        }
        
        /// <summary>
        /// Remove all tiles from the panel.
        /// </summary>
        public void ClearTiles()
        {
            foreach (var tile in m_Tiles)
            {
                tile.OnAcknowledged -= OnTileAck;
            }
            m_Tiles.Clear();
            m_Grid.Clear();
        }
        
        /// <summary>
        /// Get a tile by index.
        /// </summary>
        public AnnunciatorTileElement GetTile(int index)
        {
            if (index < 0 || index >= m_Tiles.Count) return null;
            return m_Tiles[index];
        }
        
        /// <summary>
        /// Get a tile by tag.
        /// </summary>
        public AnnunciatorTileElement GetTileByTag(string tag)
        {
            foreach (var tile in m_Tiles)
            {
                if (tile.Tag == tag) return tile;
            }
            return null;
        }
        
        // ====================================================================
        // PANEL-LEVEL OPERATIONS
        // ====================================================================
        
        /// <summary>
        /// Acknowledge all tiles in ALERTING state.
        /// </summary>
        public void AcknowledgeAll()
        {
            foreach (var tile in m_Tiles)
            {
                if (tile.State == AnnunciatorTileState.Alerting)
                {
                    tile.Acknowledge();
                }
            }
            OnAcknowledgeAll?.Invoke();
        }
        
        /// <summary>
        /// Silence alarm audio (visual flashing continues).
        /// </summary>
        public void Silence()
        {
            m_Silenced = true;
            OnSilence?.Invoke();
        }
        
        /// <summary>
        /// Reset all tiles in CLEARING state.
        /// </summary>
        public void ResetAll()
        {
            foreach (var tile in m_Tiles)
            {
                if (tile.State == AnnunciatorTileState.Clearing)
                {
                    tile.Reset();
                }
            }
            m_Silenced = false;
            OnResetAll?.Invoke();
        }
        
        // ====================================================================
        // STATUS UPDATES
        // ====================================================================
        
        private void UpdateStatus()
        {
            int alerting = AlertingCount;
            int active = ActiveCount;
            
            if (m_StatusLabel != null)
            {
                if (alerting > 0)
                    m_StatusLabel.text = $"{alerting} UNACK / {active} ACTIVE";
                else if (active > 0)
                    m_StatusLabel.text = $"{active} ACTIVE";
                else
                    m_StatusLabel.text = "NORMAL";
            }
            
            // Highlight ACK button when unacknowledged alarms exist
            if (m_AckButton != null)
            {
                m_AckButton.EnableInClassList(USS_BUTTON_ACK_ACTIVE, alerting > 0);
            }
        }
        
        // ====================================================================
        // EVENT HANDLERS
        // ====================================================================
        
        private void OnTileAck(AnnunciatorTileElement tile)
        {
            OnTileAcknowledged?.Invoke(tile);
        }
        
        private void OnAckClick()
        {
            AcknowledgeAll();
        }
        
        private void OnSilenceClick()
        {
            Silence();
        }
        
        private void OnResetClick()
        {
            ResetAll();
        }
    }
}
