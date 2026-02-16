// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard Header Panel
// HeaderPanel.cs - Top Header Bar with Plant Status
// ============================================================================
//
// PURPOSE:
//   Displays the fixed header bar at the top of the dashboard showing:
//   - Plant Mode (Mode 5/4/3 with color coding)
//   - Current Phase description
//   - Simulation time (HH:MM:SS)
//   - Wall clock time
//   - Time acceleration controls
//   - Current tab indicator
//
// LAYOUT:
//   ┌────────────────────────────────────────────────────────────────────┐
//   │ [MODE 5]  │  COLD SHUTDOWN - HEATUP  │  SIM: 0:45:32  │  SPEED: 4x │
//   └────────────────────────────────────────────────────────────────────┘
//
// VERSION: 1.0.0
// DATE: 2026-02-16
// IP: IP-0031 Stage 1
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Critical.Physics;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Header panel displaying plant mode, phase, and time information.
    /// Always visible at top of dashboard.
    /// </summary>
    public class HeaderPanel : ValidationPanelBase
    {
        // ====================================================================
        // PANEL IDENTITY
        // ====================================================================

        public override string PanelName => "HeaderPanel";
        public override int TabIndex => -1;  // Not associated with any tab
        public override bool AlwaysVisible => true;  // Always shown

        // ====================================================================
        // UI REFERENCES
        // ====================================================================

        [Header("Mode Display")]
        [SerializeField] private TextMeshProUGUI modeText;
        [SerializeField] private Image modeBackground;

        [Header("Phase Display")]
        [SerializeField] private TextMeshProUGUI phaseText;

        [Header("Time Display")]
        [SerializeField] private TextMeshProUGUI simTimeText;
        [SerializeField] private TextMeshProUGUI wallTimeText;

        [Header("Speed Display")]
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private Image speedBackground;

        [Header("Tab Indicator")]
        [SerializeField] private TextMeshProUGUI tabIndicatorText;

        // ====================================================================
        // CACHED VALUES
        // ====================================================================

        private int _cachedPlantMode = -1;
        private string _cachedPhaseDesc;
        private float _cachedSimTime = -1f;
        private float _cachedWallTime = -1f;
        private int _cachedSpeedIndex = -1;
        private int _cachedTabIndex = -1;

        // Cached strings to avoid allocations
        private string _modeString;
        private Color _modeColor;
        private string _phaseString;
        private string _simTimeString;
        private string _wallTimeString;
        private string _speedString;
        private Color _speedColor;
        private string _tabString;

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        protected override void OnInitialize()
        {
            // Subscribe to tab change events
            if (Controller != null)
            {
                Controller.OnTabChanged += OnTabChanged;
            }
        }

        private void OnDestroy()
        {
            if (Controller != null)
            {
                Controller.OnTabChanged -= OnTabChanged;
            }
        }

        // ====================================================================
        // DATA UPDATE
        // ====================================================================

        protected override void OnUpdateData()
        {
            if (Engine == null) return;

            // Update mode (only when changed)
            int currentMode = Engine.plantMode;
            if (currentMode != _cachedPlantMode)
            {
                _cachedPlantMode = currentMode;
                UpdateModeDisplay(currentMode);
            }

            // Update phase (only when changed)
            string currentPhase = Engine.heatupPhaseDesc;
            if (currentPhase != _cachedPhaseDesc)
            {
                _cachedPhaseDesc = currentPhase;
                UpdatePhaseDisplay(currentPhase);
            }

            // Update sim time (truncate to 1-second resolution for change detection)
            float simTimeTrunc = Mathf.Floor(Engine.simTime * 3600f);
            if (simTimeTrunc != _cachedSimTime)
            {
                _cachedSimTime = simTimeTrunc;
                _simTimeString = $"SIM: {FormatTime(Engine.simTime)}";
            }

            // Update wall time (truncate to 1-second resolution)
            float wallTimeTrunc = Mathf.Floor(Engine.wallClockTime * 3600f);
            if (wallTimeTrunc != _cachedWallTime)
            {
                _cachedWallTime = wallTimeTrunc;
                _wallTimeString = $"WALL: {FormatTime(Engine.wallClockTime)}";
            }

            // Update speed (only when changed)
            int currentSpeed = Engine.currentSpeedIndex;
            if (currentSpeed != _cachedSpeedIndex)
            {
                _cachedSpeedIndex = currentSpeed;
                UpdateSpeedDisplay(currentSpeed, Engine.isAccelerated);
            }
        }

        // ====================================================================
        // VISUAL UPDATE
        // ====================================================================

        protected override void OnUpdateVisuals()
        {
            // Apply cached values to UI elements
            SetText(modeText, _modeString);
            SetImageColor(modeBackground, _modeColor);

            SetText(phaseText, _phaseString);

            SetText(simTimeText, _simTimeString);
            SetText(wallTimeText, _wallTimeString);

            SetText(speedText, _speedString);
            SetImageColor(speedBackground, _speedColor);

            SetText(tabIndicatorText, _tabString);
        }

        // ====================================================================
        // DISPLAY UPDATE HELPERS
        // ====================================================================

        private void UpdateModeDisplay(int mode)
        {
            switch (mode)
            {
                case 5:
                    _modeString = "MODE 5";
                    _modeColor = ValidationDashboardTheme.InfoCyan;
                    break;
                case 4:
                    _modeString = "MODE 4";
                    _modeColor = ValidationDashboardTheme.WarningAmber;
                    break;
                case 3:
                    _modeString = "MODE 3";
                    _modeColor = ValidationDashboardTheme.NormalGreen;
                    break;
                default:
                    _modeString = "MODE ?";
                    _modeColor = ValidationDashboardTheme.Neutral;
                    break;
            }
        }

        private void UpdatePhaseDisplay(string phase)
        {
            _phaseString = string.IsNullOrEmpty(phase) ? "INITIALIZING..." : phase;
        }

        private void UpdateSpeedDisplay(int speedIndex, bool isAccelerated)
        {
            // Get speed label from TimeAcceleration
            string speedLabel = "1x";
            if (speedIndex >= 0 && speedIndex < TimeAcceleration.SpeedLabelsShort.Length)
            {
                speedLabel = TimeAcceleration.SpeedLabelsShort[speedIndex];
            }

            _speedString = $"SPEED: {speedLabel}";
            _speedColor = isAccelerated 
                ? ValidationDashboardTheme.WarningAmber 
                : ValidationDashboardTheme.BackgroundSection;
        }

        private void OnTabChanged(int tabIndex)
        {
            if (tabIndex != _cachedTabIndex)
            {
                _cachedTabIndex = tabIndex;
                string tabName = Controller?.GetTabName(tabIndex) ?? "UNKNOWN";
                _tabString = $"[Ctrl+1-7] {tabName}";
            }
        }

        // ====================================================================
        // RUNTIME PREFAB BUILDER (for programmatic UI creation)
        // ====================================================================

        /// <summary>
        /// Creates the header panel UI hierarchy programmatically.
        /// Call this when building the dashboard without prefabs.
        /// </summary>
        public static HeaderPanel CreateHeaderPanel(Transform parent, float width)
        {
            // Create panel GameObject
            GameObject panelGO = new GameObject("HeaderPanel");
            panelGO.transform.SetParent(parent, false);

            // Add RectTransform
            RectTransform rt = panelGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, ValidationDashboardTheme.HeaderBarHeight);

            // Add background image
            Image bgImage = panelGO.AddComponent<Image>();
            bgImage.color = ValidationDashboardTheme.BackgroundHeader;

            // Add CanvasGroup for visibility control
            panelGO.AddComponent<CanvasGroup>();

            // Add horizontal layout
            HorizontalLayoutGroup layout = panelGO.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.spacing = ValidationDashboardTheme.PaddingStandard;
            layout.padding = new RectOffset(
                (int)ValidationDashboardTheme.PaddingStandard,
                (int)ValidationDashboardTheme.PaddingStandard,
                (int)ValidationDashboardTheme.PaddingSmall,
                (int)ValidationDashboardTheme.PaddingSmall);

            // Add HeaderPanel component
            HeaderPanel panel = panelGO.AddComponent<HeaderPanel>();

            // Create child elements
            panel.modeBackground = CreateHeaderCell(panelGO.transform, "ModeCell", 100f, out panel.modeText);
            panel.phaseText = CreateHeaderLabel(panelGO.transform, "PhaseText", 280f);
            panel.simTimeText = CreateHeaderLabel(panelGO.transform, "SimTimeText", 140f);
            panel.wallTimeText = CreateHeaderLabel(panelGO.transform, "WallTimeText", 140f);
            panel.speedBackground = CreateHeaderCell(panelGO.transform, "SpeedCell", 100f, out panel.speedText);

            // Flexible spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(panelGO.transform, false);
            LayoutElement spacerLE = spacer.AddComponent<LayoutElement>();
            spacerLE.flexibleWidth = 1f;

            // Tab indicator (right-aligned)
            panel.tabIndicatorText = CreateHeaderLabel(panelGO.transform, "TabIndicator", 180f);
            if (panel.tabIndicatorText != null)
            {
                panel.tabIndicatorText.alignment = TextAlignmentOptions.MidlineRight;
                panel.tabIndicatorText.color = ValidationDashboardTheme.TextSecondary;
            }

            return panel;
        }

        private static Image CreateHeaderCell(Transform parent, string name, float width, out TextMeshProUGUI textComponent)
        {
            GameObject cellGO = new GameObject(name);
            cellGO.transform.SetParent(parent, false);

            // Cell sizing
            LayoutElement le = cellGO.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;

            // Background
            Image bg = cellGO.AddComponent<Image>();
            bg.color = ValidationDashboardTheme.BackgroundSection;

            // Text child
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(cellGO.transform, false);

            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(8, 0);
            textRT.offsetMax = new Vector2(-8, 0);

            textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 13;
            textComponent.fontStyle = FontStyles.Bold;
            textComponent.alignment = TextAlignmentOptions.Midline;
            textComponent.color = ValidationDashboardTheme.TextPrimary;
            textComponent.text = "---";

            return bg;
        }

        private static TextMeshProUGUI CreateHeaderLabel(Transform parent, string name, float width)
        {
            GameObject labelGO = new GameObject(name);
            labelGO.transform.SetParent(parent, false);

            LayoutElement le = labelGO.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;

            TextMeshProUGUI text = labelGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 13;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = ValidationDashboardTheme.TextPrimary;
            text.text = "---";

            return text;
        }
    }
}
