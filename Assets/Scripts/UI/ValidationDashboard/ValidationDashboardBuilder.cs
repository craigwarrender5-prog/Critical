// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard Builder
// ValidationDashboardBuilder.cs - Runtime UI Hierarchy Constructor
// ============================================================================
//
// PURPOSE:
//   Constructs the complete Validation Dashboard UI hierarchy at runtime.
//   This allows the dashboard to be created without prefab dependencies,
//   ensuring all styling and layout is code-driven and version-controlled.
//
// ARCHITECTURE:
//   Creates the following hierarchy:
//
//   ValidationDashboardCanvas (Screen Space Overlay, Sort Order 100)
//   ├── Background (full-screen dark panel)
//   ├── HeaderPanel (top bar: mode, phase, time, speed)
//   ├── TabNavigation (tab buttons with indicator)
//   ├── MainContent (tab content area)
//   │   ├── OverviewPanel (tab 0)
//   │   ├── PrimaryPanel (tab 1)
//   │   ├── PressurizerPanel (tab 2)
//   │   ├── CVCSPanel (tab 3)
//   │   ├── SGRHRPanel (tab 4)
//   │   ├── AlarmsPanel (tab 5)
//   │   └── ValidationPanel (tab 6)
//   └── MiniTrendsPanel (right edge, always visible)
//
// USAGE:
//   Call ValidationDashboardBuilder.Build() from a MonoBehaviour to create
//   the entire dashboard. The method returns the controller reference.
//
// VERSION: 1.0.0
// DATE: 2026-02-16
// IP: IP-0031 Stage 1
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Static builder class that constructs the dashboard UI hierarchy.
    /// </summary>
    public static class ValidationDashboardBuilder
    {
        // ====================================================================
        // PUBLIC BUILD METHOD
        // ====================================================================

        /// <summary>
        /// Build the complete validation dashboard UI hierarchy.
        /// </summary>
        /// <param name="engine">Reference to the HeatupSimEngine (optional, auto-finds if null)</param>
        /// <returns>The ValidationDashboardController component</returns>
        public static ValidationDashboardController Build(HeatupSimEngine engine = null)
        {
            // Find engine if not provided
            if (engine == null)
            {
                engine = Object.FindObjectOfType<HeatupSimEngine>();
                if (engine == null)
                {
                    Debug.LogError("[ValidationDashboardBuilder] No HeatupSimEngine found!");
                    return null;
                }
            }

            Debug.Log("[ValidationDashboardBuilder] Building dashboard hierarchy...");

            // Create root canvas
            GameObject canvasGO = CreateCanvas();
            ValidationDashboardController controller = canvasGO.AddComponent<ValidationDashboardController>();

            // Create background
            CreateBackground(canvasGO.transform);

            // Create header panel
            HeaderPanel header = HeaderPanel.CreateHeaderPanel(canvasGO.transform, Screen.width);

            // Create tab navigation
            TabNavigationController tabNav = TabNavigationController.CreateTabNavigation(canvasGO.transform, controller);

            // Create main content area
            GameObject mainContent = CreateMainContentArea(canvasGO.transform);

            // Create all detail panels (Stages 3-4)
            CreatePanel<OverviewPanel>(mainContent.transform, "OverviewPanel", 0);
            CreatePanel<PrimaryLoopPanel>(mainContent.transform, "PrimaryLoopPanel", 1);
            CreatePanel<PressurizerPanel>(mainContent.transform, "PressurizerPanel", 2);
            CreatePanel<CVCSPanel>(mainContent.transform, "CVCSPanel", 3);
            CreatePanel<SGRHRPanel>(mainContent.transform, "SGRHRPanel", 4);
            CreatePanel<AlarmsPanel>(mainContent.transform, "AlarmsPanel", 5);
            CreatePanel<ValidationPanel>(mainContent.transform, "ValidationPanel", 6);

            // Create mini-trends panel (right edge) - Stage 5
            CreateMiniTrendsPanel(canvasGO.transform);

            Debug.Log("[ValidationDashboardBuilder] Dashboard hierarchy complete");

            return controller;
        }

        // ====================================================================
        // CANVAS CREATION
        // ====================================================================

        private static GameObject CreateCanvas()
        {
            GameObject canvasGO = new GameObject("ValidationDashboardCanvas");

            // Canvas component
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;  // Above other UI

            // Canvas scaler for resolution independence
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Graphic raycaster for UI interaction
            canvasGO.AddComponent<GraphicRaycaster>();

            // CanvasGroup for visibility control
            CanvasGroup cg = canvasGO.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            return canvasGO;
        }

        // ====================================================================
        // BACKGROUND
        // ====================================================================

        private static void CreateBackground(Transform parent)
        {
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(parent, false);

            RectTransform rt = bgGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = bgGO.AddComponent<Image>();
            img.color = ValidationDashboardTheme.BackgroundDark;
            img.raycastTarget = true;  // Block clicks to underlying UI
        }

        // ====================================================================
        // MAIN CONTENT AREA
        // ====================================================================

        private static GameObject CreateMainContentArea(Transform parent)
        {
            GameObject contentGO = new GameObject("MainContent");
            contentGO.transform.SetParent(parent, false);

            // Position below header and tab bar, leave space for mini-trends on right
            float topOffset = ValidationDashboardTheme.HeaderBarHeight + ValidationDashboardTheme.TabBarHeight;
            float rightMargin = 220f;  // Mini-trends panel width + padding

            RectTransform rt = contentGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(ValidationDashboardTheme.PaddingStandard, ValidationDashboardTheme.PaddingStandard);
            rt.offsetMax = new Vector2(-rightMargin, -topOffset);

            // Background (slightly lighter than main background)
            Image bg = contentGO.AddComponent<Image>();
            bg.color = ValidationDashboardTheme.BackgroundPanel;

            return contentGO;
        }

        // ====================================================================
        // GENERIC PANEL CREATION (Stages 3-4)
        // ====================================================================

        private static T CreatePanel<T>(Transform parent, string name, int tabIndex) where T : ValidationPanelBase
        {
            GameObject panelGO = new GameObject(name);
            panelGO.transform.SetParent(parent, false);

            // Fill parent
            RectTransform rt = panelGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(ValidationDashboardTheme.PaddingStandard, ValidationDashboardTheme.PaddingStandard);
            rt.offsetMax = new Vector2(-ValidationDashboardTheme.PaddingStandard, -ValidationDashboardTheme.PaddingStandard);

            // CanvasGroup for visibility
            CanvasGroup cg = panelGO.AddComponent<CanvasGroup>();

            // Add the panel component
            T panel = panelGO.AddComponent<T>();

            // Hide all except Overview (tab 0)
            if (tabIndex != 0)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
                panelGO.SetActive(false);
            }

            return panel;
        }

        // ====================================================================
        // PLACEHOLDER PANEL (kept for backward compatibility)
        // ====================================================================

        private static void CreatePlaceholderPanel(Transform parent, string name, int tabIndex, string message)
        {
            GameObject panelGO = new GameObject(name);
            panelGO.transform.SetParent(parent, false);

            // Fill parent
            RectTransform rt = panelGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(ValidationDashboardTheme.PaddingStandard, ValidationDashboardTheme.PaddingStandard);
            rt.offsetMax = new Vector2(-ValidationDashboardTheme.PaddingStandard, -ValidationDashboardTheme.PaddingStandard);

            // CanvasGroup for visibility
            CanvasGroup cg = panelGO.AddComponent<CanvasGroup>();

            // Add placeholder panel component
            PlaceholderPanel panel = panelGO.AddComponent<PlaceholderPanel>();
            panel.SetTabIndex(tabIndex);

            // Create centered message
            GameObject textGO = new GameObject("Message");
            textGO.transform.SetParent(panelGO.transform, false);

            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0.5f, 0.5f);
            textRT.anchorMax = new Vector2(0.5f, 0.5f);
            textRT.sizeDelta = new Vector2(400, 100);

            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = message;
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.color = ValidationDashboardTheme.TextSecondary;

            // Hide all except first (Overview)
            if (tabIndex != 0)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
                panelGO.SetActive(false);
            }
        }

        // ====================================================================
        // MINI-TRENDS PANEL (Stage 5)
        // ====================================================================

        private static void CreateMiniTrendsPanel(Transform parent)
        {
            GameObject trendsGO = new GameObject("MiniTrendsPanel");
            trendsGO.transform.SetParent(parent, false);

            // Position on right edge
            float topOffset = ValidationDashboardTheme.HeaderBarHeight + ValidationDashboardTheme.TabBarHeight;
            float width = 200f;

            RectTransform rt = trendsGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 0.5f);
            rt.offsetMin = new Vector2(-width - ValidationDashboardTheme.PaddingStandard, ValidationDashboardTheme.PaddingStandard);
            rt.offsetMax = new Vector2(-ValidationDashboardTheme.PaddingStandard, -topOffset);

            // Background
            Image bg = trendsGO.AddComponent<Image>();
            bg.color = ValidationDashboardTheme.BackgroundPanel;

            // CanvasGroup
            trendsGO.AddComponent<CanvasGroup>();

            // Add the MiniTrendsPanel component
            trendsGO.AddComponent<MiniTrendsPanel>();
        }
    }

    // ========================================================================
    // PLACEHOLDER PANEL (temporary - will be replaced with real panels)
    // ========================================================================

    /// <summary>
    /// Temporary placeholder panel used during Stage 1.
    /// Will be replaced with real panel implementations in later stages.
    /// </summary>
    public class PlaceholderPanel : ValidationPanelBase
    {
        private int _tabIndex = 0;
        private string _panelName = "Placeholder";

        public override string PanelName => _panelName;
        public override int TabIndex => _tabIndex;

        public void SetTabIndex(int index)
        {
            _tabIndex = index;
            _panelName = $"PlaceholderPanel_Tab{index}";
        }

        protected override void OnUpdateData()
        {
            // Placeholder - no data to update
        }

        protected override void OnUpdateVisuals()
        {
            // Placeholder - no visuals to update
        }
    }
}
