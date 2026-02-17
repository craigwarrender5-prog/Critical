// ============================================================================
// CRITICAL: Master the Atom - Overview Section: Alarm Annunciator
// OverviewSection_Alarms.cs - ISA-18.1 Annunciator Tile Grid
// ============================================================================
//
// PURPOSE:
//   MosaicAlarmPanel-style annunciator grid with ISA-18.1 state machine
//   for alarm/warning tiles. 6Ã—2 grid of 12 tiles covering key heatup
//   alarm conditions and system status.
//
// VISUAL STANDARD:
//   Adopted from MosaicAlarmPanel.cs and NRC HRTD Section 4:
//   - 4-edge borders, instrument font, dim/lit/flash states
//   - INACTIVE â†’ ALERTING (3 Hz) â†’ ACKNOWLEDGED â†’ CLEARING (0.7 Hz)
//   - Status tiles (green) are simple on/off, no state machine
//
// VERSION: 2.0.0
// DATE: 2026-02-17
// IP: IP-0040 Stage 5
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Critical.Physics;

using Critical.Validation;
namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Annunciator grid section with ISA-18.1 compliant alarm tiles.
    /// </summary>
    public class OverviewSection_Alarms : OverviewSectionBase
    {
        // 12 annunciator tiles
        private DashboardAnnunciatorTile[] _tiles;
        private const int TILE_COUNT = 12;

        // Tile index constants for readability
        private const int PRESS_HIGH = 0;
        private const int PRESS_LOW = 1;
        private const int LVL_HIGH = 2;
        private const int LVL_LOW = 3;
        private const int SUBCOOL_LOW = 4;
        private const int VCT_LEVEL = 5;
        private const int MASS_CONS = 6;
        private const int FLOW_LOW = 7;
        private const int SG_PRESS_HIGH = 8;
        private const int PZR_HTRS_ON = 9;
        private const int SPRAY_ACTIVE = 10;
        private const int BUBBLE_FORMED = 11;

        // ACK button reference
        private GameObject _ackButton;

        protected override void BuildContent()
        {
            // --- Annunciator grid: 6Ã—2 ---
            GameObject gridGO = new GameObject("AnnunciatorGrid");
            gridGO.transform.SetParent(ContentRoot, false);

            RectTransform gridRT = gridGO.AddComponent<RectTransform>();
            LayoutElement gridLE = gridGO.AddComponent<LayoutElement>();
            gridLE.flexibleHeight = 1;
            gridLE.flexibleWidth = 1;

            GridLayoutGroup grid = gridGO.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            grid.cellSize = new Vector2(72, 36);
            grid.spacing = new Vector2(3, 3);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.padding = new RectOffset(2, 2, 2, 2);

            // Define tile descriptors
            var descriptors = new AnnunciatorTileDescriptor[TILE_COUNT];
            descriptors[PRESS_HIGH]     = new AnnunciatorTileDescriptor("PRESS\nHIGH", true);
            descriptors[PRESS_LOW]      = new AnnunciatorTileDescriptor("PRESS\nLOW", true);
            descriptors[LVL_HIGH]       = new AnnunciatorTileDescriptor("LVL\nHIGH", false, true);  // warning
            descriptors[LVL_LOW]        = new AnnunciatorTileDescriptor("LVL\nLOW", true);
            descriptors[SUBCOOL_LOW]    = new AnnunciatorTileDescriptor("SUBCOOL\nLOW", true);
            descriptors[VCT_LEVEL]      = new AnnunciatorTileDescriptor("VCT\nLEVEL", false, true);  // warning
            descriptors[MASS_CONS]      = new AnnunciatorTileDescriptor("MASS\nCONS", true);
            descriptors[FLOW_LOW]       = new AnnunciatorTileDescriptor("FLOW\nLOW", true);
            descriptors[SG_PRESS_HIGH]  = new AnnunciatorTileDescriptor("SG PRESS\nHIGH", false, true);  // warning
            descriptors[PZR_HTRS_ON]    = new AnnunciatorTileDescriptor("PZR HTRS\nON", false);       // status (green)
            descriptors[SPRAY_ACTIVE]   = new AnnunciatorTileDescriptor("SPRAY\nACTIVE", false);      // status (green)
            descriptors[BUBBLE_FORMED]  = new AnnunciatorTileDescriptor("BUBBLE\nFORMED", false);     // status (green)

            // Create tiles
            _tiles = new DashboardAnnunciatorTile[TILE_COUNT];
            for (int i = 0; i < TILE_COUNT; i++)
            {
                _tiles[i] = DashboardAnnunciatorTile.Create(gridGO.transform, descriptors[i]);
            }

            // --- ACK button ---
            GameObject ackRow = new GameObject("AckRow");
            ackRow.transform.SetParent(ContentRoot, false);

            LayoutElement ackLE = ackRow.AddComponent<LayoutElement>();
            ackLE.preferredHeight = 22;
            ackLE.minHeight = 22;

            HorizontalLayoutGroup ackLayout = ackRow.AddComponent<HorizontalLayoutGroup>();
            ackLayout.childAlignment = TextAnchor.MiddleCenter;
            ackLayout.childControlWidth = false;
            ackLayout.childControlHeight = false;

            _ackButton = new GameObject("AckButton");
            _ackButton.transform.SetParent(ackRow.transform, false);

            RectTransform ackRT = _ackButton.AddComponent<RectTransform>();
            ackRT.sizeDelta = new Vector2(80, 20);

            Image ackBg = _ackButton.AddComponent<Image>();
            ackBg.color = new Color(0.25f, 0.25f, 0.30f, 1f);

            Button ackBtn = _ackButton.AddComponent<Button>();
            ackBtn.targetGraphic = ackBg;
            ackBtn.onClick.AddListener(AcknowledgeAll);

            // ACK label
            GameObject ackLabelGO = new GameObject("Label");
            ackLabelGO.transform.SetParent(_ackButton.transform, false);

            RectTransform ackLabelRT = ackLabelGO.AddComponent<RectTransform>();
            ackLabelRT.anchorMin = Vector2.zero;
            ackLabelRT.anchorMax = Vector2.one;
            ackLabelRT.offsetMin = Vector2.zero;
            ackLabelRT.offsetMax = Vector2.zero;

            TextMeshProUGUI ackText = ackLabelGO.AddComponent<TextMeshProUGUI>();
            ackText.text = "ACK";
            ackText.fontSize = 10;
            ackText.fontStyle = FontStyles.Bold;
            ackText.alignment = TextAlignmentOptions.Center;
            ackText.color = ValidationDashboardTheme.TextSecondary;
        }

        public override void UpdateData(HeatupSimEngine engine)
        {
            if (engine == null || _tiles == null) return;

            // Alarm tiles (red) â€” use ISA-18.1 state machine
            _tiles[PRESS_HIGH]?.UpdateCondition(engine.pressureHigh);
            _tiles[PRESS_LOW]?.UpdateCondition(engine.pressureLow);
            _tiles[LVL_LOW]?.UpdateCondition(engine.pzrLevelLow);
            _tiles[SUBCOOL_LOW]?.UpdateCondition(engine.subcoolingLow);
            _tiles[MASS_CONS]?.UpdateCondition(engine.primaryMassAlarm);
            _tiles[FLOW_LOW]?.UpdateCondition(engine.rcsFlowLow);

            // Warning tiles (amber) â€” use ISA-18.1 state machine
            _tiles[LVL_HIGH]?.UpdateCondition(engine.pzrLevelHigh);
            _tiles[VCT_LEVEL]?.UpdateCondition(engine.vctLevelHigh || engine.vctLevelLow);
            _tiles[SG_PRESS_HIGH]?.UpdateCondition(engine.sgSecondaryPressureHigh);

            // Status tiles (green) â€” simple on/off, no state machine
            _tiles[PZR_HTRS_ON]?.UpdateCondition(engine.pzrHeatersOn);
            _tiles[SPRAY_ACTIVE]?.UpdateCondition(engine.sprayActive);
            _tiles[BUBBLE_FORMED]?.UpdateCondition(engine.bubbleFormed);
        }

        public override void UpdateVisuals()
        {
            // Tiles handle their own flash animation via Update()
        }

        // ====================================================================
        // ACK / RESET
        // ====================================================================

        /// <summary>
        /// Acknowledge all alerting tiles (ALERTING â†’ ACKNOWLEDGED).
        /// </summary>
        private void AcknowledgeAll()
        {
            if (_tiles == null) return;
            for (int i = 0; i < _tiles.Length; i++)
            {
                _tiles[i]?.Acknowledge();
            }
        }

        /// <summary>
        /// Reset all clearing tiles (CLEARING â†’ INACTIVE).
        /// Called automatically by auto-reset timer in DashboardAnnunciatorTile.
        /// </summary>
        public void ResetAll()
        {
            if (_tiles == null) return;
            for (int i = 0; i < _tiles.Length; i++)
            {
                _tiles[i]?.Reset();
            }
        }
    }
}

