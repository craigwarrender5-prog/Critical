// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// ValidationDashboard.Layout.cs - Screen Layout Calculations
// ============================================================================
//
// PURPOSE:
//   Screen layout calculations for the Overview tab 5-column layout.
//   Precomputes all Rects once per resolution change to avoid per-frame
//   allocation. Provides column widths and heights for consistent layout.
//
// ARCHITECTURE:
//   Layout is calculated once when screen dimensions change, then cached.
//   Tab rendering methods use these cached Rects directly.
//
// LAYOUT DESIGN (1920×1080 reference):
//   ┌────────┬────────┬────────┬────────┬──────────────┐
//   │  RCS   │  PZR   │  CVCS  │ SG/RHR │   TRENDS     │
//   │  18%   │  16%   │  16%   │  16%   │    24%       │
//   │        │        │        │        │              │
//   │ 18     │  16    │  14    │  12    │  8 sparklines│
//   │ params │ params │ params │ params │              │
//   ├────────┴────────┴────────┴────────┴──────────────┤
//   │                    FOOTER                         │
//   │  27 annunciator tiles (3×9) + 8 event log entries │
//   └───────────────────────────────────────────────────┘
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
        // LAYOUT CONSTANTS — Overview Tab Column Fractions
        // ====================================================================

        /// <summary>RCS column width fraction (18%)</summary>
        private const float COL_RCS_FRAC = 0.18f;

        /// <summary>Pressurizer column width fraction (16%)</summary>
        private const float COL_PZR_FRAC = 0.16f;

        /// <summary>CVCS column width fraction (16%)</summary>
        private const float COL_CVCS_FRAC = 0.16f;

        /// <summary>SG/RHR column width fraction (16%)</summary>
        private const float COL_SGRHR_FRAC = 0.16f;

        /// <summary>Trends column width fraction (24%) - remainder after other columns + gaps</summary>
        private const float COL_TRENDS_FRAC = 0.24f;

        /// <summary>Footer height fraction (18% of content area)</summary>
        private const float FOOTER_FRAC = 0.18f;

        /// <summary>Minimum footer height in pixels</summary>
        private const float FOOTER_MIN_HEIGHT = 100f;

        /// <summary>Column gap in pixels</summary>
        private const float COL_GAP = 4f;

        /// <summary>Section padding in pixels</summary>
        private const float SECTION_PAD = 6f;

        // ====================================================================
        // LAYOUT CONSTANTS — Gauge Sizing
        // ====================================================================

        /// <summary>Arc gauge diameter in pixels</summary>
        private const float ARC_GAUGE_SIZE = 70f;

        /// <summary>Arc gauge vertical spacing</summary>
        private const float ARC_GAUGE_SPACING = 85f;

        /// <summary>Bar gauge height</summary>
        private const float BAR_HEIGHT = 18f;

        /// <summary>Bar gauge vertical spacing</summary>
        private const float BAR_SPACING = 22f;

        /// <summary>LED row height</summary>
        private const float LED_HEIGHT = 18f;

        /// <summary>LED vertical spacing</summary>
        private const float LED_SPACING = 20f;

        /// <summary>Digital readout height</summary>
        private const float READOUT_HEIGHT = 16f;

        /// <summary>Digital readout spacing</summary>
        private const float READOUT_SPACING = 18f;

        /// <summary>Section header height</summary>
        private const float SECTION_HEADER_HEIGHT = 18f;

        // ====================================================================
        // LAYOUT CONSTANTS — Annunciator Tiles
        // ====================================================================

        /// <summary>Annunciator tile width</summary>
        private const float ANN_TILE_WIDTH = 100f;

        /// <summary>Annunciator tile height</summary>
        private const float ANN_TILE_HEIGHT = 32f;

        /// <summary>Annunciator tile gap</summary>
        private const float ANN_TILE_GAP = 3f;

        /// <summary>Number of annunciator columns</summary>
        private const int ANN_COLS = 9;

        /// <summary>Number of annunciator rows</summary>
        private const int ANN_ROWS = 3;

        // ====================================================================
        // LAYOUT CONSTANTS — Sparklines
        // ====================================================================

        /// <summary>Sparkline height</summary>
        private const float SPARKLINE_HEIGHT = 40f;

        /// <summary>Sparkline label width</summary>
        private const float SPARKLINE_LABEL_WIDTH = 60f;

        /// <summary>Number of sparkline traces</summary>
        private const int SPARKLINE_COUNT = 8;

        // ====================================================================
        // CACHED LAYOUT RECTS
        // ====================================================================

        private float _cachedScreenW;
        private float _cachedScreenH;

        // Column rects (main content area)
        private Rect _colRcsRect;
        private Rect _colPzrRect;
        private Rect _colCvcsRect;
        private Rect _colSgRhrRect;
        private Rect _colTrendsRect;

        // Footer rect
        private Rect _footerRect;

        // Annunciator grid rect (within footer)
        private Rect _annGridRect;

        // Event log rect (within footer)
        private Rect _eventLogRect;

        // ====================================================================
        // LAYOUT CALCULATION
        // ====================================================================

        /// <summary>
        /// Calculate all layout rects for the given content area.
        /// Called when screen dimensions change.
        /// </summary>
        private void CalculateLayout(Rect contentArea)
        {
            // Check if recalculation needed
            if (Mathf.Approximately(_cachedScreenW, _screenWidth) &&
                Mathf.Approximately(_cachedScreenH, _screenHeight))
            {
                return;
            }

            _cachedScreenW = _screenWidth;
            _cachedScreenH = _screenHeight;

            // Calculate footer height
            float footerH = Mathf.Max(contentArea.height * FOOTER_FRAC, FOOTER_MIN_HEIGHT);
            float mainH = contentArea.height - footerH;

            // Calculate column widths (account for gaps)
            float totalGaps = COL_GAP * 4; // 4 gaps between 5 columns
            float availableW = contentArea.width - totalGaps - SECTION_PAD * 2;

            float colRcsW = availableW * COL_RCS_FRAC;
            float colPzrW = availableW * COL_PZR_FRAC;
            float colCvcsW = availableW * COL_CVCS_FRAC;
            float colSgRhrW = availableW * COL_SGRHR_FRAC;
            float colTrendsW = availableW - colRcsW - colPzrW - colCvcsW - colSgRhrW;

            // Build column rects
            float x = contentArea.x + SECTION_PAD;
            float y = contentArea.y + SECTION_PAD;
            float colH = mainH - SECTION_PAD * 2;

            _colRcsRect = new Rect(x, y, colRcsW, colH);
            x += colRcsW + COL_GAP;

            _colPzrRect = new Rect(x, y, colPzrW, colH);
            x += colPzrW + COL_GAP;

            _colCvcsRect = new Rect(x, y, colCvcsW, colH);
            x += colCvcsW + COL_GAP;

            _colSgRhrRect = new Rect(x, y, colSgRhrW, colH);
            x += colSgRhrW + COL_GAP;

            _colTrendsRect = new Rect(x, y, colTrendsW, colH);

            // Footer rect
            _footerRect = new Rect(
                contentArea.x + SECTION_PAD,
                contentArea.y + mainH,
                contentArea.width - SECTION_PAD * 2,
                footerH - SECTION_PAD);

            // Annunciator grid (left 60% of footer)
            float annGridW = _footerRect.width * 0.60f;
            _annGridRect = new Rect(_footerRect.x, _footerRect.y, annGridW, _footerRect.height);

            // Event log (right 40% of footer)
            _eventLogRect = new Rect(
                _footerRect.x + annGridW + COL_GAP,
                _footerRect.y,
                _footerRect.width - annGridW - COL_GAP,
                _footerRect.height);
        }

        // ====================================================================
        // HELPER METHODS — Row Positioning
        // ====================================================================

        /// <summary>
        /// Get Y position for a gauge row within a column.
        /// </summary>
        private float GetGaugeRowY(Rect column, int rowIndex)
        {
            return column.y + SECTION_HEADER_HEIGHT + SECTION_PAD + (rowIndex * ARC_GAUGE_SPACING);
        }

        /// <summary>
        /// Get Y position for a bar row within a column.
        /// </summary>
        private float GetBarRowY(Rect column, int rowIndex, int gaugeRowCount)
        {
            float afterGauges = column.y + SECTION_HEADER_HEIGHT + SECTION_PAD + 
                               (gaugeRowCount * ARC_GAUGE_SPACING);
            return afterGauges + (rowIndex * BAR_SPACING);
        }

        /// <summary>
        /// Get Y position for an LED row within a column.
        /// </summary>
        private float GetLedRowY(Rect column, int rowIndex, int gaugeRowCount, int barRowCount)
        {
            float afterBars = column.y + SECTION_HEADER_HEIGHT + SECTION_PAD +
                             (gaugeRowCount * ARC_GAUGE_SPACING) +
                             (barRowCount * BAR_SPACING);
            return afterBars + (rowIndex * LED_SPACING);
        }

        /// <summary>
        /// Get center point for an arc gauge in a column.
        /// </summary>
        private Vector2 GetArcGaugeCenter(Rect column, int rowIndex)
        {
            float y = GetGaugeRowY(column, rowIndex);
            return new Vector2(
                column.x + column.width / 2f,
                y + ARC_GAUGE_SIZE / 2f);
        }

        // ====================================================================
        // HELPER METHODS — Annunciator Grid
        // ====================================================================

        /// <summary>
        /// Get rect for an annunciator tile at grid position.
        /// </summary>
        private Rect GetAnnunciatorTileRect(int row, int col)
        {
            float x = _annGridRect.x + col * (ANN_TILE_WIDTH + ANN_TILE_GAP);
            float y = _annGridRect.y + row * (ANN_TILE_HEIGHT + ANN_TILE_GAP);
            return new Rect(x, y, ANN_TILE_WIDTH, ANN_TILE_HEIGHT);
        }

        // ====================================================================
        // HELPER METHODS — Sparklines
        // ====================================================================

        /// <summary>
        /// Get rect for a sparkline at index.
        /// </summary>
        private Rect GetSparklineRect(int index)
        {
            float spacing = (_colTrendsRect.height - SECTION_HEADER_HEIGHT) / SPARKLINE_COUNT;
            float y = _colTrendsRect.y + SECTION_HEADER_HEIGHT + index * spacing;
            return new Rect(_colTrendsRect.x, y, _colTrendsRect.width, spacing - 2f);
        }
    }
}
