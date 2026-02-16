// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard Tab Navigation
// TabNavigationController.cs - Tab Bar and Navigation Control
// ============================================================================
//
// PURPOSE:
//   Manages the tab navigation bar below the header, providing:
//   - Visual tab buttons with active/inactive states
//   - Mouse click handling for tab switching
//   - Visual feedback (underline indicator, color changes)
//   - Tab content panel coordination
//
// LAYOUT:
//   ┌────────────────────────────────────────────────────────────────────┐
//   │ [OVERVIEW] │ [PRIMARY] │ [PRESSURIZER] │ [CVCS] │ [SG/RHR] │ ... │
//   │ ═══════════                                                        │
//   └────────────────────────────────────────────────────────────────────┘
//
// VERSION: 1.0.0
// DATE: 2026-02-16
// IP: IP-0031 Stage 1
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Controls the tab navigation bar and coordinates tab switching.
    /// </summary>
    public class TabNavigationController : MonoBehaviour
    {
        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================

        [Header("References")]
        [Tooltip("Reference to the main dashboard controller")]
        [SerializeField] private ValidationDashboardController controller;

        [Tooltip("Container transform for tab buttons")]
        [SerializeField] private Transform tabButtonContainer;

        [Tooltip("The active tab underline indicator")]
        [SerializeField] private RectTransform activeIndicator;

        [Header("Animation")]
        [Tooltip("Duration of indicator slide animation")]
        [SerializeField] private float indicatorSlideDuration = 0.2f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = false;

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        private List<TabButton> _tabButtons = new List<TabButton>();
        private int _currentTabIndex = 0;
        private float _indicatorTargetX;
        private float _indicatorCurrentX;
        private float _indicatorTargetWidth;
        private float _indicatorCurrentWidth;
        private bool _isAnimating;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        void Awake()
        {
            // Auto-find controller if not assigned
            if (controller == null)
            {
                controller = GetComponentInParent<ValidationDashboardController>();
            }
        }

        void Start()
        {
            Initialize();
        }

        void Update()
        {
            if (_isAnimating)
            {
                UpdateIndicatorAnimation();
            }
        }

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        private void Initialize()
        {
            if (controller == null)
            {
                Debug.LogError("[TabNavigationController] No controller reference!");
                return;
            }

            // Subscribe to tab change events
            controller.OnTabChanged += OnTabChanged;

            // Find or create tab buttons
            if (_tabButtons.Count == 0)
            {
                FindExistingTabButtons();
            }

            // Set initial state
            UpdateTabButtonStates(0);
            UpdateIndicatorPosition(0, immediate: true);

            if (enableDebugLogging)
                Debug.Log($"[TabNavigationController] Initialized with {_tabButtons.Count} tabs");
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.OnTabChanged -= OnTabChanged;
            }
        }

        private void FindExistingTabButtons()
        {
            _tabButtons.Clear();

            if (tabButtonContainer == null) return;

            // Find all TabButton components in children
            var buttons = tabButtonContainer.GetComponentsInChildren<TabButton>(true);
            foreach (var btn in buttons)
            {
                _tabButtons.Add(btn);
                btn.OnTabClicked += HandleTabClicked;
            }

            // Sort by sibling index
            _tabButtons.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

            // Assign indices
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                _tabButtons[i].TabIndex = i;
            }
        }

        // ====================================================================
        // TAB SWITCHING
        // ====================================================================

        private void OnTabChanged(int newTabIndex)
        {
            if (newTabIndex == _currentTabIndex) return;

            _currentTabIndex = newTabIndex;
            UpdateTabButtonStates(newTabIndex);
            UpdateIndicatorPosition(newTabIndex, immediate: false);
        }

        private void HandleTabClicked(int tabIndex)
        {
            if (controller != null)
            {
                controller.SwitchToTab(tabIndex);
            }
        }

        private void UpdateTabButtonStates(int activeIndex)
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                _tabButtons[i].SetActive(i == activeIndex);
            }
        }

        // ====================================================================
        // INDICATOR ANIMATION
        // ====================================================================

        private void UpdateIndicatorPosition(int tabIndex, bool immediate)
        {
            if (activeIndicator == null || tabIndex >= _tabButtons.Count) return;

            RectTransform targetButton = _tabButtons[tabIndex].GetComponent<RectTransform>();
            if (targetButton == null) return;

            // Calculate target position and width
            _indicatorTargetX = targetButton.anchoredPosition.x;
            _indicatorTargetWidth = targetButton.rect.width;

            if (immediate)
            {
                // Instant update
                _indicatorCurrentX = _indicatorTargetX;
                _indicatorCurrentWidth = _indicatorTargetWidth;
                ApplyIndicatorTransform();
                _isAnimating = false;
            }
            else
            {
                // Start animation
                _isAnimating = true;
            }
        }

        private void UpdateIndicatorAnimation()
        {
            float step = Time.unscaledDeltaTime / indicatorSlideDuration;

            _indicatorCurrentX = Mathf.MoveTowards(_indicatorCurrentX, _indicatorTargetX, 
                Mathf.Abs(_indicatorTargetX - _indicatorCurrentX) * step * 5f + 50f * step);
            _indicatorCurrentWidth = Mathf.MoveTowards(_indicatorCurrentWidth, _indicatorTargetWidth,
                Mathf.Abs(_indicatorTargetWidth - _indicatorCurrentWidth) * step * 5f + 50f * step);

            ApplyIndicatorTransform();

            // Check if animation complete
            if (Mathf.Approximately(_indicatorCurrentX, _indicatorTargetX) &&
                Mathf.Approximately(_indicatorCurrentWidth, _indicatorTargetWidth))
            {
                _isAnimating = false;
            }
        }

        private void ApplyIndicatorTransform()
        {
            if (activeIndicator == null) return;

            activeIndicator.anchoredPosition = new Vector2(_indicatorCurrentX, activeIndicator.anchoredPosition.y);
            activeIndicator.sizeDelta = new Vector2(_indicatorCurrentWidth, activeIndicator.sizeDelta.y);
        }

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Register a tab button (called by TabButton on Awake if auto-registering).
        /// </summary>
        public void RegisterTabButton(TabButton button)
        {
            if (!_tabButtons.Contains(button))
            {
                _tabButtons.Add(button);
                button.OnTabClicked += HandleTabClicked;
            }
        }

        /// <summary>
        /// Get the current active tab index.
        /// </summary>
        public int CurrentTabIndex => _currentTabIndex;

        // ====================================================================
        // RUNTIME PREFAB BUILDER
        // ====================================================================

        /// <summary>
        /// Creates the tab navigation bar UI hierarchy programmatically.
        /// </summary>
        public static TabNavigationController CreateTabNavigation(Transform parent, ValidationDashboardController controller)
        {
            // Create container GameObject
            GameObject navGO = new GameObject("TabNavigation");
            navGO.transform.SetParent(parent, false);

            // Position below header
            RectTransform rt = navGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -ValidationDashboardTheme.HeaderBarHeight);
            rt.sizeDelta = new Vector2(0, ValidationDashboardTheme.TabBarHeight);

            // Background
            Image bg = navGO.AddComponent<Image>();
            bg.color = ValidationDashboardTheme.BackgroundPanel;

            // Horizontal layout for tabs
            HorizontalLayoutGroup layout = navGO.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.spacing = 0;
            layout.padding = new RectOffset(
                (int)ValidationDashboardTheme.PaddingStandard, 
                (int)ValidationDashboardTheme.PaddingStandard, 
                0, 0);

            // Add controller component
            TabNavigationController navController = navGO.AddComponent<TabNavigationController>();
            navController.controller = controller;
            navController.tabButtonContainer = navGO.transform;

            // Create tab buttons
            string[] tabNames = new string[] { "OVERVIEW", "PRIMARY", "PRESSURIZER", "CVCS", "SG / RHR", "ALARMS", "VALIDATION" };
            float[] tabWidths = new float[] { 100f, 90f, 110f, 70f, 90f, 80f, 110f };

            for (int i = 0; i < tabNames.Length; i++)
            {
                TabButton btn = TabButton.CreateTabButton(navGO.transform, tabNames[i], i, tabWidths[i]);
                navController._tabButtons.Add(btn);
                btn.OnTabClicked += navController.HandleTabClicked;
            }

            // Create active indicator (underline)
            GameObject indicatorGO = new GameObject("ActiveIndicator");
            indicatorGO.transform.SetParent(navGO.transform, false);

            RectTransform indicatorRT = indicatorGO.AddComponent<RectTransform>();
            indicatorRT.anchorMin = new Vector2(0, 0);
            indicatorRT.anchorMax = new Vector2(0, 0);
            indicatorRT.pivot = new Vector2(0.5f, 0);
            indicatorRT.sizeDelta = new Vector2(100f, 3f);
            indicatorRT.anchoredPosition = new Vector2(50f + ValidationDashboardTheme.PaddingStandard, 0);

            Image indicatorImg = indicatorGO.AddComponent<Image>();
            indicatorImg.color = ValidationDashboardTheme.AccentBlue;

            navController.activeIndicator = indicatorRT;

            // Set initial state
            if (navController._tabButtons.Count > 0)
            {
                navController._tabButtons[0].SetActive(true);
                navController.UpdateIndicatorPosition(0, immediate: true);
            }

            return navController;
        }
    }

    // ========================================================================
    // TAB BUTTON COMPONENT
    // ========================================================================

    /// <summary>
    /// Individual tab button with hover/active states.
    /// </summary>
    public class TabButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        // ====================================================================
        // PUBLIC STATE
        // ====================================================================

        /// <summary>Index of this tab.</summary>
        public int TabIndex { get; set; }

        /// <summary>Is this tab currently active?</summary>
        public bool IsActive { get; private set; }

        /// <summary>Fired when this tab is clicked.</summary>
        public event Action<int> OnTabClicked;

        // ====================================================================
        // REFERENCES
        // ====================================================================

        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Image backgroundImage;

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        private bool _isHovered;
        private Color _normalColor = Color.clear;
        private Color _hoverColor;
        private Color _activeColor;

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        void Awake()
        {
            _hoverColor = new Color(1f, 1f, 1f, 0.05f);
            _activeColor = new Color(1f, 1f, 1f, 0.1f);
        }

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Set this tab as active or inactive.
        /// </summary>
        public void SetActive(bool active)
        {
            IsActive = active;
            UpdateVisuals();
        }

        // ====================================================================
        // POINTER EVENTS
        // ====================================================================

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            UpdateVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            UpdateVisuals();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnTabClicked?.Invoke(TabIndex);
        }

        // ====================================================================
        // VISUAL UPDATE
        // ====================================================================

        private void UpdateVisuals()
        {
            // Background color
            Color bgColor;
            if (IsActive)
                bgColor = _activeColor;
            else if (_isHovered)
                bgColor = _hoverColor;
            else
                bgColor = _normalColor;

            if (backgroundImage != null)
                backgroundImage.color = bgColor;

            // Text color
            Color textColor;
            if (IsActive)
                textColor = ValidationDashboardTheme.TextPrimary;
            else if (_isHovered)
                textColor = ValidationDashboardTheme.TextPrimary;
            else
                textColor = ValidationDashboardTheme.TextSecondary;

            if (labelText != null)
                labelText.color = textColor;
        }

        // ====================================================================
        // FACTORY METHOD
        // ====================================================================

        /// <summary>
        /// Create a tab button programmatically.
        /// </summary>
        public static TabButton CreateTabButton(Transform parent, string label, int index, float width)
        {
            GameObject btnGO = new GameObject($"Tab_{label}");
            btnGO.transform.SetParent(parent, false);

            // Layout sizing
            LayoutElement le = btnGO.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;

            // Background (for hover/active states)
            Image bg = btnGO.AddComponent<Image>();
            bg.color = Color.clear;

            // Text child
            GameObject textGO = new GameObject("Label");
            textGO.transform.SetParent(btnGO.transform, false);

            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 12;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Midline;
            text.color = ValidationDashboardTheme.TextSecondary;

            // Add TabButton component
            TabButton btn = btnGO.AddComponent<TabButton>();
            btn.TabIndex = index;
            btn.labelText = text;
            btn.backgroundImage = bg;

            return btn;
        }
    }
}
