// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// DashboardTab.cs - Abstract Base Class for Dashboard Tabs
// ============================================================================
//
// PURPOSE:
//   Abstract base class for dashboard tabs. Each tab implements its own
//   rendering logic while sharing common infrastructure from the dashboard.
//
// ARCHITECTURE:
//   - Tabs receive reference to dashboard and snapshot
//   - Tabs implement Draw() method for their content
//   - Tabs can access shared styles and helpers via dashboard reference
//
// GOLD STANDARD: Yes
// VERSION: 1.0.0.0
// ============================================================================

using UnityEngine;

namespace Critical.Validation
{
    /// <summary>
    /// Abstract base class for dashboard tabs.
    /// </summary>
    public abstract class DashboardTab
    {
        // ====================================================================
        // FIELDS
        // ====================================================================

        /// <summary>Reference to the parent dashboard.</summary>
        protected ValidationDashboard Dashboard { get; private set; }

        /// <summary>Tab display name.</summary>
        public string Name { get; protected set; }

        /// <summary>Tab index in toolbar.</summary>
        public int Index { get; protected set; }

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        /// <summary>
        /// Create a new tab.
        /// </summary>
        protected DashboardTab(ValidationDashboard dashboard, string name, int index)
        {
            Dashboard = dashboard;
            Name = name;
            Index = index;
        }

        // ====================================================================
        // ABSTRACT METHODS
        // ====================================================================

        /// <summary>
        /// Draw the tab content.
        /// </summary>
        /// <param name="area">Content area rect.</param>
        public abstract void Draw(Rect area);

        // ====================================================================
        // HELPER ACCESSORS
        // ====================================================================

        /// <summary>Get the current snapshot.</summary>
        protected DashboardSnapshot Snapshot => Dashboard?.Snapshot;

        /// <summary>Get screen width.</summary>
        protected float ScreenWidth => Dashboard?.ScreenWidth ?? Screen.width;

        /// <summary>Get screen height.</summary>
        protected float ScreenHeight => Dashboard?.ScreenHeight ?? Screen.height;
    }
}
