// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// GraphsTab.cs - Full-Width Strip Chart Trends Tab
// ============================================================================
//
// PURPOSE:
//   Displays full-width strip chart trends organized by category. Provides
//   larger, more detailed trend views than the Overview tab sparklines.
//   Users can select between different parameter groups.
//
// LAYOUT:
//   ┌─────────────────────────────────────────────────────────────────────────┐
//   │  [TEMPS] [PRESSURE] [CVCS] [SG/RHR] [RATES] [HEAT] [ALL]               │
//   ├─────────────────────────────────────────────────────────────────────────┤
//   │                                                                         │
//   │  ┌─────────────────────────────────────────────────────────────────┐   │
//   │  │                      TREND 1                                     │   │
//   │  └─────────────────────────────────────────────────────────────────┘   │
//   │  ┌─────────────────────────────────────────────────────────────────┐   │
//   │  │                      TREND 2                                     │   │
//   │  └─────────────────────────────────────────────────────────────────┘   │
//   │  ┌─────────────────────────────────────────────────────────────────┐   │
//   │  │                      TREND 3                                     │   │
//   │  └─────────────────────────────────────────────────────────────────┘   │
//   │  ┌─────────────────────────────────────────────────────────────────┐   │
//   │  │                      TREND 4                                     │   │
//   │  └─────────────────────────────────────────────────────────────────┘   │
//   │                                                                         │
//   └─────────────────────────────────────────────────────────────────────────┘
//
// CATEGORIES:
//   - TEMPS: T_AVG, HEATUP RATE, SUBCOOLING
//   - PRESSURE: RCS PRESSURE, PZR LEVEL
//   - CVCS: NET CVCS, PZR LEVEL
//   - SG/RHR: SG PRESSURE, NET HEAT
//   - RATES: HEATUP RATE, SUBCOOLING
//   - HEAT: NET HEAT, SG PRESSURE
//   - ALL: All 8 sparklines in compact view
//
// REFERENCE:
//   NRC HRTD trend monitoring requirements
//   Control room strip chart recorder conventions
//
// GOLD STANDARD: Yes
// VERSION: 1.0.0.0
// ============================================================================

using UnityEngine;

namespace Critical.Validation
{
    /// <summary>
    /// Graphs tab with full-width strip chart trends organized by category.
    /// </summary>
    public class GraphsTab : DashboardTab
    {
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public GraphsTab(ValidationDashboard dashboard) 
            : base(dashboard, "GRAPHS", 6)
        {
        }

        // ====================================================================
        // CATEGORY DEFINITIONS
        // ====================================================================

        private int _selectedCategory = 0;
        private readonly string[] _categoryLabels = new string[]
        {
            "TEMPS",     // 0
            "PRESSURE",  // 1
            "CVCS",      // 2
            "SG/RHR",    // 3
            "RATES",     // 4
            "HEAT",      // 5
            "ALL"        // 6
        };

        // Sparkline indices for each category
        // Index mapping: 0=RCS PRESS, 1=PZR LEVEL, 2=T_AVG, 3=HEATUP, 4=SUBCOOL, 5=NET CVCS, 6=SG PRESS, 7=NET HEAT
        private readonly int[][] _categorySparklines = new int[][]
        {
            new[] { 2, 3, 4 },       // TEMPS: T_AVG, HEATUP, SUBCOOL
            new[] { 0, 1 },          // PRESSURE: RCS PRESS, PZR LEVEL
            new[] { 5, 1 },          // CVCS: NET CVCS, PZR LEVEL
            new[] { 6, 7 },          // SG/RHR: SG PRESS, NET HEAT
            new[] { 3, 4 },          // RATES: HEATUP, SUBCOOL
            new[] { 7, 6 },          // HEAT: NET HEAT, SG PRESS
            new[] { 0, 1, 2, 3, 4, 5, 6, 7 }  // ALL: all 8
        };

        // ====================================================================
        // LAYOUT
        // ====================================================================

        private const float TOOLBAR_HEIGHT = 28f;
        private const float PAD = 8f;
        private const float GRAPH_GAP = 4f;

        // ====================================================================
        // MAIN DRAW
        // ====================================================================

        public override void Draw(Rect area)
        {
            var d = Dashboard;

            // Draw background
            GUI.Box(area, GUIContent.none, d._panelBgStyle);

            // Category toolbar
            Rect toolbarRect = new Rect(area.x + PAD, area.y + PAD, 
                area.width - PAD * 2, TOOLBAR_HEIGHT);
            _selectedCategory = GUI.Toolbar(toolbarRect, _selectedCategory, 
                _categoryLabels, d._tabStyle);

            // Graph area
            float graphY = area.y + PAD + TOOLBAR_HEIGHT + PAD;
            float graphH = area.height - PAD * 2 - TOOLBAR_HEIGHT - PAD;
            Rect graphArea = new Rect(area.x + PAD, graphY, 
                area.width - PAD * 2, graphH);

            DrawCategoryGraphs(graphArea);
        }

        // ====================================================================
        // CATEGORY GRAPHS
        // ====================================================================

        private void DrawCategoryGraphs(Rect area)
        {
            var d = Dashboard;
            var sm = d.SparklineManager;

            if (sm == null || !sm.IsInitialized)
            {
                // Placeholder
                GUI.contentColor = ValidationDashboard._cTextSecondary;
                GUI.Label(new Rect(area.x, area.y + area.height / 2f - 10f, area.width, 20f),
                    "Sparklines initializing...", d._sectionHeaderStyle);
                GUI.contentColor = Color.white;
                return;
            }

            int[] indices = _categorySparklines[_selectedCategory];
            int graphCount = indices.Length;

            // Calculate graph heights
            float totalGaps = (graphCount - 1) * GRAPH_GAP;
            float graphH = (area.height - totalGaps) / graphCount;
            graphH = Mathf.Max(graphH, 40f); // Minimum height

            float y = area.y;

            for (int i = 0; i < graphCount; i++)
            {
                int sparkIndex = indices[i];
                Rect graphRect = new Rect(area.x, y, area.width, graphH);

                // Draw graph background
                GUI.Box(graphRect, GUIContent.none, d._gaugeBgStyle);

                // Draw sparkline
                sm.Draw(sparkIndex, graphRect, d._gaugeLabelStyle, d._statusValueStyle);

                y += graphH + GRAPH_GAP;
            }
        }
    }
}
