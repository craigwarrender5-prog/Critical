// ============================================================================
// CRITICAL: Master the Atom — Annunciator Panel POC Controller
// AnnunciatorPanelPOCController.cs
// ============================================================================
//
// PURPOSE:
//   MonoBehaviour controller for the Annunciator Panel POC scene.
//   Wires up the UI Toolkit demo with interactive controls and populates
//   the demo panel with realistic PWR alarm tiles.
//
// USAGE:
//   Attach to a GameObject with a UIDocument component.
//   Set the UIDocument's source asset to AnnunciatorPanelDemo.uxml.
//
// VERSION: 0.1.0-POC
// DATE: 2026-02-18
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using Critical.UI.Elements;
using System.Collections.Generic;

namespace Critical.UI.POC
{
    /// <summary>
    /// Controller for the Annunciator Panel POC demonstration.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class AnnunciatorPanelPOCController : MonoBehaviour
    {
        // ====================================================================
        // TILE DEFINITIONS
        // ====================================================================
        
        /// <summary>
        /// Tile descriptor for demo panel population.
        /// </summary>
        private struct TileDef
        {
            public string Label;
            public AnnunciatorTileColor Color;
            public bool IsStatus;
            
            public TileDef(string label, AnnunciatorTileColor color, bool isStatus = false)
            {
                Label = label;
                Color = color;
                IsStatus = isStatus;
            }
        }
        
        /// <summary>
        /// Realistic PWR annunciator tile definitions.
        /// Arranged to match typical control room panel layout.
        /// </summary>
        private static readonly TileDef[] DEMO_TILES = new TileDef[]
        {
            // Row 1: Critical status and alarms
            new TileDef("V-06A\nADSORPTION", AnnunciatorTileColor.Amber),
            new TileDef("SEQUENCE\nON", AnnunciatorTileColor.Green, true),
            new TileDef("CUMULATIVE\nALARM", AnnunciatorTileColor.Red),
            new TileDef("PHASE\nFAILURE", AnnunciatorTileColor.Red),
            new TileDef("VALVE\nFAILURE", AnnunciatorTileColor.Red),
            new TileDef("ALARM\nTAHH-0554", AnnunciatorTileColor.Red),
            new TileDef("ALARM\nTAL-0554", AnnunciatorTileColor.Red),
            new TileDef("ALARM\nFAL-0601", AnnunciatorTileColor.Red),
            new TileDef("LOW PANEL\nPRESSURE", AnnunciatorTileColor.Amber),
            new TileDef("V-06B\nADSORPTION", AnnunciatorTileColor.Amber),
            
            // Row 2: Valve positions A side
            new TileDef("V-06A\nDEPRESSURE", AnnunciatorTileColor.Amber),
            new TileDef("XV-0551A\nOPEN", AnnunciatorTileColor.Green, true),
            new TileDef("XV-0551A\nCLOSE", AnnunciatorTileColor.Red),
            new TileDef("XV-0555A\nOPEN", AnnunciatorTileColor.Green, true),
            new TileDef("XV-0555A\nCLOSE", AnnunciatorTileColor.Red),
            new TileDef("XV-0551B\nOPEN", AnnunciatorTileColor.Green, true),
            new TileDef("XV-0551B\nCLOSE", AnnunciatorTileColor.Red),
            new TileDef("XV-0555B\nOPEN", AnnunciatorTileColor.Green, true),
            new TileDef("XV-0555B\nCLOSE", AnnunciatorTileColor.Red),
            new TileDef("V-06B\nDEPRESSURE", AnnunciatorTileColor.Amber),
            
            // Row 3: Heating system
            new TileDef("V-06A\nHEATING", AnnunciatorTileColor.Amber),
            new TileDef("XV-0552A\nOPEN", AnnunciatorTileColor.Green, true),
            new TileDef("XV-0552A\nCLOSE", AnnunciatorTileColor.Red),
            new TileDef("XV-0556A\nOPEN", AnnunciatorTileColor.Green, true),
            new TileDef("XV-0556A\nCLOSE", AnnunciatorTileColor.Red),
            new TileDef("XV-0552B\nOPEN", AnnunciatorTileColor.Green, true),
            new TileDef("XV-0552B\nCLOSE", AnnunciatorTileColor.Red),
            new TileDef("XV-0556B\nOPEN", AnnunciatorTileColor.Green, true),
            new TileDef("XV-0556B\nCLOSE", AnnunciatorTileColor.Red),
            new TileDef("V-06B\nHEATING", AnnunciatorTileColor.Amber),
            
            // Row 4: Cooling system
            new TileDef("V-06A\nCOOLING", AnnunciatorTileColor.Amber),
            new TileDef("XV-0553A\nOPEN", AnnunciatorTileColor.Green, true),
            new TileDef("XV-0553A\nCLOSE", AnnunciatorTileColor.Red),
            new TileDef("XV-0557A\nOPEN", AnnunciatorTileColor.Green, true),
            new TileDef("XV-0557A\nCLOSE", AnnunciatorTileColor.Red),
            new TileDef("XV-0553B\nOPEN", AnnunciatorTileColor.Green, true),
            new TileDef("XV-0553B\nCLOSE", AnnunciatorTileColor.Red),
            new TileDef("XV-0601\nOPEN", AnnunciatorTileColor.Green, true),
            new TileDef("XV-0601\nCLOSE", AnnunciatorTileColor.Red),
            new TileDef("V-06B\nCOOLING", AnnunciatorTileColor.Amber),
            
            // Row 5: Pressurization
            new TileDef("V-06A\nPRESSURIS", AnnunciatorTileColor.Amber),
            new TileDef("XV-0554A\nOPEN", AnnunciatorTileColor.Green, true),
            new TileDef("XV-0554A\nCLOSE", AnnunciatorTileColor.Red),
            new TileDef("SPARE", AnnunciatorTileColor.White),
            new TileDef("SPARE", AnnunciatorTileColor.White),
            new TileDef("XV-0554B\nOPEN", AnnunciatorTileColor.Green, true),
            new TileDef("XV-0554B\nCLOSE", AnnunciatorTileColor.Red),
            new TileDef("XV-0602\nOPEN", AnnunciatorTileColor.Green, true),
            new TileDef("XV-0602\nCLOSE", AnnunciatorTileColor.Red),
            new TileDef("V-06B\nPRESSURIS", AnnunciatorTileColor.Amber),
            
            // Row 6: Mode and controls
            new TileDef("V-06A\nSTANDBY", AnnunciatorTileColor.Green, true),
            new TileDef("LOCAL", AnnunciatorTileColor.White),
            new TileDef("REMOTE", AnnunciatorTileColor.Green, true),
            new TileDef("MANUAL", AnnunciatorTileColor.Green, true),
            new TileDef("SPARE", AnnunciatorTileColor.White),
            new TileDef("COMPRESS\nON", AnnunciatorTileColor.Green, true),
            new TileDef("HEATER\nON", AnnunciatorTileColor.Green, true),
            new TileDef("FIXED\nTIME", AnnunciatorTileColor.White),
            new TileDef("VARIABLE\nTIME", AnnunciatorTileColor.Green, true),
            new TileDef("V-06B\nSTANDBY", AnnunciatorTileColor.Green, true),
        };
        
        // ====================================================================
        // REFERENCES
        // ====================================================================
        
        private UIDocument m_Document;
        private VisualElement m_Root;
        
        // Demo tiles (individual state demos)
        private List<AnnunciatorTileElement> m_DemoTiles = new List<AnnunciatorTileElement>();
        
        // Full panel
        private AnnunciatorPanelElement m_DemoPanel;
        
        // Test buttons
        private Button m_BtnTriggerRandom;
        private Button m_BtnClearRandom;
        private Button m_BtnTriggerAll;
        private Button m_BtnClearAll;
        
        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================
        
        private void OnEnable()
        {
            m_Document = GetComponent<UIDocument>();
            if (m_Document == null || m_Document.rootVisualElement == null)
            {
                Debug.LogError("[AnnunciatorPanelPOC] UIDocument or root is null");
                return;
            }
            
            m_Root = m_Document.rootVisualElement;
            
            // Initialize individual demo tiles
            InitializeDemoTiles();
            
            // Populate the demo panel
            PopulateDemoPanel();
            
            // Wire up test buttons
            WireTestButtons();
            
            Debug.Log("[AnnunciatorPanelPOC] Initialized");
        }
        
        // ====================================================================
        // INITIALIZATION
        // ====================================================================
        
        private void InitializeDemoTiles()
        {
            // Red tiles - set different states
            var redInactive = m_Root.Q<AnnunciatorTileElement>("demo-red-inactive");
            var redAlerting = m_Root.Q<AnnunciatorTileElement>("demo-red-alerting");
            var redAck = m_Root.Q<AnnunciatorTileElement>("demo-red-ack");
            var redClearing = m_Root.Q<AnnunciatorTileElement>("demo-red-clearing");
            
            if (redAlerting != null) redAlerting.SetState(AnnunciatorTileState.Alerting);
            if (redAck != null) redAck.SetState(AnnunciatorTileState.Acknowledged);
            if (redClearing != null) redClearing.SetState(AnnunciatorTileState.Clearing);
            
            // Amber tiles
            var amberAlerting = m_Root.Q<AnnunciatorTileElement>("demo-amber-alerting");
            var amberAck = m_Root.Q<AnnunciatorTileElement>("demo-amber-ack");
            var amberClearing = m_Root.Q<AnnunciatorTileElement>("demo-amber-clearing");
            
            if (amberAlerting != null) amberAlerting.SetState(AnnunciatorTileState.Alerting);
            if (amberAck != null) amberAck.SetState(AnnunciatorTileState.Acknowledged);
            if (amberClearing != null) amberClearing.SetState(AnnunciatorTileState.Clearing);
            
            // Green tiles (status) - turn some on
            var greenOn = m_Root.Q<AnnunciatorTileElement>("demo-green-on");
            var greenOn2 = m_Root.Q<AnnunciatorTileElement>("demo-green-on2");
            var greenOn3 = m_Root.Q<AnnunciatorTileElement>("demo-green-on3");
            
            if (greenOn != null) greenOn.UpdateCondition(true);
            if (greenOn2 != null) greenOn2.UpdateCondition(true);
            if (greenOn3 != null) greenOn3.UpdateCondition(true);
            
            // White tiles - turn some on
            var whiteOn = m_Root.Q<AnnunciatorTileElement>("demo-white-on");
            var whiteOn2 = m_Root.Q<AnnunciatorTileElement>("demo-white-on2");
            var whiteOn3 = m_Root.Q<AnnunciatorTileElement>("demo-white-on3");
            
            if (whiteOn != null) whiteOn.SetState(AnnunciatorTileState.Acknowledged);
            if (whiteOn2 != null) whiteOn2.SetState(AnnunciatorTileState.Acknowledged);
            if (whiteOn3 != null) whiteOn3.SetState(AnnunciatorTileState.Acknowledged);
            
            // Add click handlers to cycle states
            m_Root.Query<AnnunciatorTileElement>().ForEach(tile =>
            {
                tile.OnClicked += OnDemoTileClicked;
                m_DemoTiles.Add(tile);
            });
        }
        
        private void PopulateDemoPanel()
        {
            m_DemoPanel = m_Root.Q<AnnunciatorPanelElement>("demo-panel");
            if (m_DemoPanel == null)
            {
                Debug.LogWarning("[AnnunciatorPanelPOC] Demo panel not found");
                return;
            }
            
            // Add tiles from definitions
            foreach (var def in DEMO_TILES)
            {
                m_DemoPanel.AddTile(def.Label, def.Color, def.IsStatus);
            }
            
            // Set some initial conditions to show variety
            // Turn on some status indicators
            for (int i = 0; i < m_DemoPanel.Tiles.Count; i++)
            {
                var tile = m_DemoPanel.Tiles[i];
                if (tile.IsStatus && i % 3 == 1)
                {
                    tile.UpdateCondition(true);
                }
            }
            
            Debug.Log($"[AnnunciatorPanelPOC] Populated panel with {m_DemoPanel.Tiles.Count} tiles");
        }
        
        private void WireTestButtons()
        {
            m_BtnTriggerRandom = m_Root.Q<Button>("btn-trigger-random");
            m_BtnClearRandom = m_Root.Q<Button>("btn-clear-random");
            m_BtnTriggerAll = m_Root.Q<Button>("btn-trigger-all");
            m_BtnClearAll = m_Root.Q<Button>("btn-clear-all");
            
            if (m_BtnTriggerRandom != null) m_BtnTriggerRandom.clicked += OnTriggerRandom;
            if (m_BtnClearRandom != null) m_BtnClearRandom.clicked += OnClearRandom;
            if (m_BtnTriggerAll != null) m_BtnTriggerAll.clicked += OnTriggerAll;
            if (m_BtnClearAll != null) m_BtnClearAll.clicked += OnClearAll;
        }
        
        // ====================================================================
        // EVENT HANDLERS
        // ====================================================================
        
        private void OnDemoTileClicked(AnnunciatorTileElement tile)
        {
            // Individual demo tiles cycle through states on click
            // (handled automatically by the element for alerting/clearing)
            Debug.Log($"[AnnunciatorPanelPOC] Tile clicked: {tile.Label} - State: {tile.State}");
        }
        
        private void OnTriggerRandom()
        {
            if (m_DemoPanel == null || m_DemoPanel.Tiles.Count == 0) return;
            
            // Find an inactive non-status alarm tile and trigger it
            var candidates = new List<AnnunciatorTileElement>();
            foreach (var tile in m_DemoPanel.Tiles)
            {
                if (!tile.IsStatus && tile.State == AnnunciatorTileState.Inactive &&
                    tile.TileColor != AnnunciatorTileColor.White)
                {
                    candidates.Add(tile);
                }
            }
            
            if (candidates.Count > 0)
            {
                var tile = candidates[Random.Range(0, candidates.Count)];
                tile.UpdateCondition(true);
                Debug.Log($"[AnnunciatorPanelPOC] Triggered: {tile.Label}");
            }
        }
        
        private void OnClearRandom()
        {
            if (m_DemoPanel == null) return;
            
            // Find an active alarm tile and clear it
            var candidates = new List<AnnunciatorTileElement>();
            foreach (var tile in m_DemoPanel.Tiles)
            {
                if (!tile.IsStatus && tile.ConditionActive)
                {
                    candidates.Add(tile);
                }
            }
            
            if (candidates.Count > 0)
            {
                var tile = candidates[Random.Range(0, candidates.Count)];
                tile.UpdateCondition(false);
                Debug.Log($"[AnnunciatorPanelPOC] Cleared: {tile.Label}");
            }
        }
        
        private void OnTriggerAll()
        {
            if (m_DemoPanel == null) return;
            
            foreach (var tile in m_DemoPanel.Tiles)
            {
                if (!tile.IsStatus && tile.TileColor != AnnunciatorTileColor.White)
                {
                    tile.UpdateCondition(true);
                }
            }
        }
        
        private void OnClearAll()
        {
            if (m_DemoPanel == null) return;
            
            foreach (var tile in m_DemoPanel.Tiles)
            {
                if (!tile.IsStatus)
                {
                    tile.UpdateCondition(false);
                }
            }
        }
        
        // ====================================================================
        // CONTEXT MENU TESTS
        // ====================================================================
        
        [ContextMenu("Simulate Alarm Sequence")]
        public void SimulateAlarmSequence()
        {
            StartCoroutine(AlarmSequenceCoroutine());
        }
        
        private System.Collections.IEnumerator AlarmSequenceCoroutine()
        {
            if (m_DemoPanel == null || m_DemoPanel.Tiles.Count < 5) yield break;
            
            // Pick a tile
            var tile = m_DemoPanel.Tiles[2];
            
            Debug.Log("[AnnunciatorPanelPOC] Starting alarm sequence demo...");
            
            // 1. Trigger alarm (ALERTING - fast flash)
            Debug.Log("  → Alarm triggered (ALERTING)");
            tile.UpdateCondition(true);
            yield return new WaitForSeconds(3f);
            
            // 2. Acknowledge (ACKNOWLEDGED - steady)
            Debug.Log("  → Acknowledged (steady on)");
            tile.Acknowledge();
            yield return new WaitForSeconds(3f);
            
            // 3. Clear condition (CLEARING - slow flash)
            Debug.Log("  → Condition cleared (CLEARING)");
            tile.UpdateCondition(false);
            yield return new WaitForSeconds(4f);
            
            // 4. Reset (INACTIVE)
            Debug.Log("  → Reset (INACTIVE)");
            tile.Reset();
            
            Debug.Log("[AnnunciatorPanelPOC] Alarm sequence complete.");
        }
    }
}
