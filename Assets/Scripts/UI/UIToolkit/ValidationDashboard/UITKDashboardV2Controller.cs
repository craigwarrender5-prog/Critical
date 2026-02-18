// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard V2
// UITKDashboardV2Controller.cs — Main Controller (Foundation Shell)
// ============================================================================
//
// PURPOSE:
//   Top-level controller for the V2 UI Toolkit Validation Dashboard.
//   Manages lifecycle, UIDocument binding, full-screen layout shell,
//   header bar, tab navigation, keyboard input, and 5Hz data refresh.
//
// ARCHITECTURE:
//   - Partial class: each tab builder lives in its own file
//   - Engine is READ-ONLY — dashboard never modifies simulation state
//   - 5Hz data refresh with smooth gauge animation at Update() framerate
//   - All existing POC elements reused (ArcGauge, AnalogGauge, LED, etc.)
//
// KEYBOARD:
//   V key (via SceneBridge) toggles visibility
//   Ctrl+1-8 switches tabs
//   F5-F9 / +/- time acceleration
//
// IP: IP-0060 Stage 1
// VERSION: 6.0.0
// DATE: 2026-02-18
// ============================================================================

using System;
using System.Collections.Generic;
using Critical.Physics;
using Critical.UI.Elements;
using Critical.UI.POC;
using Critical.Validation;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

namespace Critical.UI.UIToolkit.ValidationDashboard
{
    /// <summary>
    /// V2 Validation Dashboard controller — full-screen, 8-tab, living control room.
    /// Partial class: tab builders are in separate files for GOLD compliance.
    /// </summary>
    public partial class UITKDashboardV2Controller : MonoBehaviour
    {
        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================

        [Header("References")]
        [Tooltip("Reference to the HeatupSimEngine. Auto-finds if not assigned.")]
        [SerializeField] private HeatupSimEngine engine;

        [Tooltip("UIDocument component. Auto-finds if not assigned.")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Refresh Settings")]
        [Tooltip("Data refresh rate in Hz")]
        [Range(2f, 10f)]
        [SerializeField] private float dataRefreshRate = 5f;

        [Header("Startup")]
        [SerializeField] private bool startVisible = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging;

        // ====================================================================
        // SINGLETON
        // ====================================================================

        public static UITKDashboardV2Controller Instance { get; private set; }

        // ====================================================================
        // PUBLIC STATE
        // ====================================================================

        /// <summary>Is the dashboard currently visible?</summary>
        public bool IsVisible => _root != null && _root.style.display != DisplayStyle.None;

        /// <summary>Currently active tab index (0-7).</summary>
        public int CurrentTabIndex { get; private set; }

        /// <summary>Direct read-only access to the simulation engine.</summary>
        public HeatupSimEngine Engine => engine;

        // ====================================================================
        // EVENTS
        // ====================================================================

        /// <summary>Fired at 5Hz when data is refreshed.</summary>
        public event Action OnDataRefresh;

        /// <summary>Fired when the active tab changes.</summary>
        public event Action<int> OnTabChanged;

        /// <summary>Fired when visibility changes.</summary>
        public event Action<bool> OnVisibilityChanged;

        // ====================================================================
        // TAB DEFINITIONS
        // ====================================================================

        private static readonly string[] TAB_NAMES =
        {
            "CRITICAL", "RCS", "PRESSURIZER", "CVCS",
            "SG / RHR", "CONDENSER", "TRENDS", "LOG"
        };

        /// <summary>Total number of tabs.</summary>
        public int TabCount => TAB_NAMES.Length;

        /// <summary>Get tab name by index.</summary>
        public string GetTabName(int index)
        {
            return index >= 0 && index < TAB_NAMES.Length ? TAB_NAMES[index] : "UNKNOWN";
        }

        // ====================================================================
        // LAYOUT REFERENCES
        // ====================================================================

        private VisualElement _root;
        private VisualElement _headerBar;
        private VisualElement _tabBar;
        private VisualElement _contentArea;

        // Header labels
        private Label _headerTitle;
        private Label _headerSimTime;
        private Label _headerPlantMode;
        private Label _headerSpeed;

        // Tab system
        private readonly List<Button> _tabButtons = new List<Button>(8);
        private readonly List<VisualElement> _tabContents = new List<VisualElement>(8);

        // ====================================================================
        // DATA BINDING REGISTRIES
        // ====================================================================

        // Shared dictionaries for metric labels, gauge references, etc.
        // Tab partial classes register their elements here for data refresh.
        private readonly Dictionary<string, Label> _metricLabels = new Dictionary<string, Label>(64);
        private readonly Dictionary<string, ArcGaugeElement> _arcGauges = new Dictionary<string, ArcGaugeElement>(32);
        private readonly Dictionary<string, AnalogGaugeElement> _analogGauges = new Dictionary<string, AnalogGaugeElement>(16);
        private readonly Dictionary<string, LEDIndicatorElement> _ledIndicators = new Dictionary<string, LEDIndicatorElement>(16);

        // Lists for animation updates (all gauges/LEDs regardless of tab)
        private readonly List<ArcGaugeElement> _allArcGauges = new List<ArcGaugeElement>(32);
        private readonly List<LEDIndicatorElement> _allLEDs = new List<LEDIndicatorElement>(16);

        // ====================================================================
        // TIMING STATE
        // ====================================================================

        private float _refreshTimer;
        private float _refreshInterval;
        private bool _initialized;

        // Legacy dashboard references (disabled on startup)
        private HeatupValidationVisual _legacyHeatupDashboard;
        private Critical.Validation.ValidationDashboard _legacyValidationDashboard;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[UITKDashboardV2] Duplicate controller — destroying");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _refreshInterval = 1f / Mathf.Max(2f, dataRefreshRate);
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            // Late-bind engine if not yet found
            if (engine == null)
            {
                engine = FindObjectOfType<HeatupSimEngine>();
                if (engine != null && enableDebugLogging)
                    Debug.Log("[UITKDashboardV2] Found HeatupSimEngine");
            }

            SuppressLegacyDashboards();

            if (!_initialized || engine == null) return;

            // Animations run every frame for smooth needle/LED movement
            TickAnimations();

            // Keyboard input
            HandleInput();

            // 5Hz data refresh
            _refreshTimer += Time.unscaledDeltaTime;
            if (_refreshTimer >= _refreshInterval)
            {
                _refreshTimer = 0f;
                RefreshAllData();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            OnDataRefresh = null;
            OnTabChanged = null;
            OnVisibilityChanged = null;
        }

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        private void Initialize()
        {
            if (engine == null)
                engine = FindObjectOfType<HeatupSimEngine>();

            // Locate and suppress legacy dashboards
            _legacyHeatupDashboard = FindObjectOfType<HeatupValidationVisual>();
            _legacyValidationDashboard = FindObjectOfType<Critical.Validation.ValidationDashboard>();
            SuppressLegacyDashboards();

            // Obtain UIDocument root
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("[UITKDashboardV2] No UIDocument found — aborting init");
                return;
            }

            _root = uiDocument.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("[UITKDashboardV2] UIDocument rootVisualElement is null");
                return;
            }

            // Build the entire UI hierarchy
            BuildShell();
            BuildTabBar();
            BuildTabContents();

            // Default to CRITICAL tab, apply visibility
            SwitchToTab(0);
            SetVisibility(startVisible);

            _initialized = true;

            // Initial data push
            RefreshAllData();

            if (enableDebugLogging)
                Debug.Log("[UITKDashboardV2] Initialization complete — 8 tabs ready");
        }

        // ====================================================================
        // SHELL CONSTRUCTION — Header + Tab Bar + Content Area
        // ====================================================================

        private void BuildShell()
        {
            _root.Clear();
            _root.style.flexDirection = FlexDirection.Column;
            _root.style.flexGrow = 1f;
            _root.style.paddingTop = 6f;
            _root.style.paddingBottom = 6f;
            _root.style.paddingLeft = 8f;
            _root.style.paddingRight = 8f;
            _root.style.backgroundColor = UITKDashboardTheme.BackgroundDark.ToStyleColor();

            // ── HEADER BAR ──────────────────────────────────────────────
            _headerBar = new VisualElement { name = "v2-header" };
            _headerBar.style.flexDirection = FlexDirection.Row;
            _headerBar.style.alignItems = Align.Center;
            _headerBar.style.justifyContent = Justify.SpaceBetween;
            _headerBar.style.height = UITKDashboardTheme.HeaderHeight;
            _headerBar.style.paddingLeft = 14f;
            _headerBar.style.paddingRight = 14f;
            _headerBar.style.marginBottom = 4f;
            _headerBar.style.backgroundColor = UITKDashboardTheme.BackgroundHeader.ToStyleColor();
            _headerBar.style.borderTopLeftRadius = 6f;
            _headerBar.style.borderTopRightRadius = 6f;
            _headerBar.style.borderBottomLeftRadius = 6f;
            _headerBar.style.borderBottomRightRadius = 6f;

            // Title (left side)
            _headerTitle = MakeLabel("CRITICAL: MASTER THE ATOM", 18f, FontStyle.Bold,
                UITKDashboardTheme.TextPrimary);
            _headerBar.Add(_headerTitle);

            // Right-side status cluster
            var headerRight = new VisualElement { name = "v2-header-right" };
            headerRight.style.flexDirection = FlexDirection.Row;
            headerRight.style.alignItems = Align.Center;

            _headerSimTime = MakeHeaderPill("00:00:00", UITKDashboardTheme.InfoCyan);
            _headerPlantMode = MakeHeaderPill("COLD SHUTDOWN", UITKDashboardTheme.InfoCyan);
            _headerSpeed = MakeHeaderPill("1x RT", UITKDashboardTheme.InfoCyan);

            headerRight.Add(_headerSimTime);
            headerRight.Add(_headerPlantMode);
            headerRight.Add(_headerSpeed);
            _headerBar.Add(headerRight);

            _root.Add(_headerBar);

            // ── TAB BAR ─────────────────────────────────────────────────
            _tabBar = new VisualElement { name = "v2-tab-bar" };
            _tabBar.style.flexDirection = FlexDirection.Row;
            _tabBar.style.flexWrap = Wrap.NoWrap;
            _tabBar.style.height = UITKDashboardTheme.TabBarHeight;
            _tabBar.style.marginBottom = 4f;
            _tabBar.style.alignItems = Align.Stretch;
            _root.Add(_tabBar);

            // ── CONTENT AREA ────────────────────────────────────────────
            _contentArea = new VisualElement { name = "v2-content" };
            _contentArea.style.flexGrow = 1f;
            _contentArea.style.overflow = Overflow.Hidden;
            _contentArea.style.paddingTop = 6f;
            _contentArea.style.paddingBottom = 6f;
            _contentArea.style.paddingLeft = 8f;
            _contentArea.style.paddingRight = 8f;
            _contentArea.style.backgroundColor = UITKDashboardTheme.BackgroundPanel.ToStyleColor();
            _contentArea.style.borderTopLeftRadius = 6f;
            _contentArea.style.borderTopRightRadius = 6f;
            _contentArea.style.borderBottomLeftRadius = 6f;
            _contentArea.style.borderBottomRightRadius = 6f;
            _root.Add(_contentArea);
        }

        // ====================================================================
        // TAB BAR CONSTRUCTION
        // ====================================================================

        private void BuildTabBar()
        {
            _tabBar.Clear();
            _tabButtons.Clear();

            for (int i = 0; i < TAB_NAMES.Length; i++)
            {
                int idx = i; // closure capture
                var btn = new Button(() => SwitchToTab(idx));
                btn.text = TAB_NAMES[i];
                btn.style.flexGrow = 1f;
                btn.style.flexShrink = 1f;
                btn.style.flexBasis = 0f;
                btn.style.minWidth = 90f;
                btn.style.height = UITKDashboardTheme.TabBarHeight;
                btn.style.unityTextAlign = TextAnchor.MiddleCenter;
                btn.style.unityFontStyleAndWeight = FontStyle.Bold;
                btn.style.fontSize = 11f;
                btn.style.paddingLeft = 6f;
                btn.style.paddingRight = 6f;
                btn.style.marginRight = i < TAB_NAMES.Length - 1 ? 3f : 0f;
                btn.style.borderTopWidth = 0f;
                btn.style.borderBottomWidth = 0f;
                btn.style.borderLeftWidth = 0f;
                btn.style.borderRightWidth = 0f;
                btn.style.borderTopLeftRadius = 4f;
                btn.style.borderTopRightRadius = 4f;
                btn.style.borderBottomLeftRadius = 0f;
                btn.style.borderBottomRightRadius = 0f;
                ApplyTabStyle(btn, false);

                _tabBar.Add(btn);
                _tabButtons.Add(btn);
            }
        }

        private void ApplyTabStyle(Button btn, bool active)
        {
            if (active)
            {
                btn.style.backgroundColor = UITKDashboardTheme.AccentBlue.ToStyleColor();
                btn.style.color = new Color(0.93f, 0.96f, 0.99f, 1f);
            }
            else
            {
                btn.style.backgroundColor = new Color(0.102f, 0.133f, 0.204f, 1f);
                btn.style.color = new Color(0.53f, 0.59f, 0.68f, 1f);
            }
        }

        // ====================================================================
        // TAB CONTENT CONSTRUCTION — Dispatches to partial class builders
        // ====================================================================

        private void BuildTabContents()
        {
            _tabContents.Clear();
            _metricLabels.Clear();
            _arcGauges.Clear();
            _analogGauges.Clear();
            _ledIndicators.Clear();
            _allArcGauges.Clear();
            _allLEDs.Clear();

            // Tab 0 (CRITICAL) is built by the CriticalTab partial class.
            // Remaining tabs use placeholders until their stages are implemented.
            for (int i = 0; i < TAB_NAMES.Length; i++)
            {
                VisualElement tabContent;
                switch (i)
                {
                    case 0: tabContent = BuildCriticalTab(); break;
                    case 1: tabContent = BuildRCSTab(); break;
                    case 2: tabContent = BuildPressurizerTab(); break;
                    case 3: tabContent = BuildCVCSTab(); break;
                    case 4: tabContent = BuildSGRHRTab(); break;
                    case 5: tabContent = BuildCondenserTab(); break;
                    case 6: tabContent = BuildTrendsTab(); break;
                    case 7: tabContent = BuildLogTab(); break;
                    default: tabContent = BuildTabPlaceholder(i); break;
                }
                tabContent.style.display = DisplayStyle.None;
                _contentArea.Add(tabContent);
                _tabContents.Add(tabContent);
            }
        }

        /// <summary>
        /// Placeholder tab content — replaced by partial class builders in later stages.
        /// </summary>
        private VisualElement BuildTabPlaceholder(int tabIndex)
        {
            var container = new VisualElement();
            container.name = $"v2-tab-{tabIndex}";
            container.style.flexGrow = 1f;
            container.style.flexDirection = FlexDirection.Column;
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.Center;

            var label = MakeLabel(TAB_NAMES[tabIndex], 24f, FontStyle.Bold,
                UITKDashboardTheme.TextSecondary);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            container.Add(label);

            var subtitle = MakeLabel("Content will be built in subsequent stages.", 12f,
                FontStyle.Normal, UITKDashboardTheme.TextDisabled);
            subtitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            subtitle.style.marginTop = 8f;
            container.Add(subtitle);

            return container;
        }

        // ====================================================================
        // TAB NAVIGATION
        // ====================================================================

        /// <summary>Switch to tab by index (0-7).</summary>
        public void SwitchToTab(int index)
        {
            if (index < 0 || index >= TAB_NAMES.Length) return;
            if (index == CurrentTabIndex && _initialized) return;

            CurrentTabIndex = index;

            // Update button visuals
            for (int i = 0; i < _tabButtons.Count; i++)
                ApplyTabStyle(_tabButtons[i], i == index);

            // Show/hide tab content panels
            for (int i = 0; i < _tabContents.Count; i++)
                _tabContents[i].style.display = i == index ? DisplayStyle.Flex : DisplayStyle.None;

            OnTabChanged?.Invoke(index);

            if (enableDebugLogging)
                Debug.Log($"[UITKDashboardV2] Tab → {TAB_NAMES[index]}");
        }

        // ====================================================================
        // VISIBILITY
        // ====================================================================

        /// <summary>Toggle dashboard visibility.</summary>
        public void ToggleVisibility() => SetVisibility(!IsVisible);

        /// <summary>Set dashboard visibility.</summary>
        public void SetVisibility(bool visible)
        {
            if (_root == null) return;
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            OnVisibilityChanged?.Invoke(visible);
        }

        // ====================================================================
        // DATA REFRESH (5Hz)
        // ====================================================================

        private void RefreshAllData()
        {
            if (engine == null) return;

            // ── Header ──
            RefreshHeader();

            // ── Active tab data (partial class methods, wired in later stages) ──
            RefreshActiveTabData();

            // ── Event dispatch ──
            OnDataRefresh?.Invoke();
        }

        private void RefreshHeader()
        {
            int speedIdx = Mathf.Clamp(engine.currentSpeedIndex, 0,
                TimeAcceleration.SpeedLabelsShort.Length - 1);

            if (_headerSimTime != null)
                _headerSimTime.text = TimeAcceleration.FormatTime(engine.simTime);

            if (_headerPlantMode != null)
            {
                string modeStr = GetPlantModeString(engine.plantMode);
                _headerPlantMode.text = modeStr;
                _headerPlantMode.style.color = GetPlantModeColor(engine.plantMode);
            }

            if (_headerSpeed != null)
                _headerSpeed.text = $"SPEED {TimeAcceleration.SpeedLabelsShort[speedIdx]}";
        }

        /// <summary>
        /// Dispatches data refresh to the active tab's partial class method.
        /// Each tab only refreshes when it is the currently displayed tab.
        /// </summary>
        private void RefreshActiveTabData()
        {
            switch (CurrentTabIndex)
            {
                case 0:
                    RefreshCriticalTabUpper();
                    RefreshCriticalTabLower();
                    break;
                case 1:
                    RefreshRCSTab();
                    break;
                case 2:
                    RefreshPressurizerTab();
                    break;
                case 3:
                    RefreshCVCSTab();
                    break;
                case 4:
                    RefreshSGRHRTab();
                    break;
                case 5:
                    RefreshCondenserTab();
                    break;
                case 6:
                    RefreshTrendsTab();
                    break;
                case 7:
                    RefreshLogTab();
                    break;
            }
        }

        // ====================================================================
        // ANIMATION TICK (every frame)
        // ====================================================================

        private void TickAnimations()
        {
            float dt = Time.unscaledDeltaTime;

            foreach (var gauge in _allArcGauges)
                gauge.UpdateAnimation();

            foreach (var led in _allLEDs)
                led.UpdateFlash();

            // CRITICAL tab annunciator flash (runs regardless of active tab
            // for consistent alarm state when switching back)
            TickAnnunciatorFlash(dt);

            // RCS loop diagram flow animation
            TickRCSLoopAnimation(dt);
        }

        // ====================================================================
        // INPUT HANDLING
        // ====================================================================

        private void HandleInput()
        {
            var kb = Keyboard.current;
            if (kb == null || !IsVisible) return;

            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;

            // Tab switching: Ctrl+1 through Ctrl+8
            if (ctrl)
            {
                if (kb.digit1Key.wasPressedThisFrame) SwitchToTab(0);
                if (kb.digit2Key.wasPressedThisFrame) SwitchToTab(1);
                if (kb.digit3Key.wasPressedThisFrame) SwitchToTab(2);
                if (kb.digit4Key.wasPressedThisFrame) SwitchToTab(3);
                if (kb.digit5Key.wasPressedThisFrame) SwitchToTab(4);
                if (kb.digit6Key.wasPressedThisFrame) SwitchToTab(5);
                if (kb.digit7Key.wasPressedThisFrame) SwitchToTab(6);
                if (kb.digit8Key.wasPressedThisFrame) SwitchToTab(7);
            }

            // Time acceleration: F5-F9
            if (kb.f5Key.wasPressedThisFrame) engine?.SetTimeAcceleration(0);
            if (kb.f6Key.wasPressedThisFrame) engine?.SetTimeAcceleration(1);
            if (kb.f7Key.wasPressedThisFrame) engine?.SetTimeAcceleration(2);
            if (kb.f8Key.wasPressedThisFrame) engine?.SetTimeAcceleration(3);
            if (kb.f9Key.wasPressedThisFrame) engine?.SetTimeAcceleration(4);

            // +/- speed adjustment
            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame)
                engine?.SetTimeAcceleration(Mathf.Min(engine.currentSpeedIndex + 1, 4));
            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
                engine?.SetTimeAcceleration(Mathf.Max(engine.currentSpeedIndex - 1, 0));
        }

        // ====================================================================
        // LEGACY DASHBOARD SUPPRESSION
        // ====================================================================

        private void SuppressLegacyDashboards()
        {
            if (_legacyHeatupDashboard != null)
            {
                _legacyHeatupDashboard.dashboardVisible = false;
                _legacyHeatupDashboard.enabled = false;
            }

            if (_legacyValidationDashboard != null)
            {
                _legacyValidationDashboard.dashboardVisible = false;
                _legacyValidationDashboard.isActiveDashboard = false;
                _legacyValidationDashboard.enabled = false;
            }
        }

        // ====================================================================
        // HELPER: Plant Mode Display
        // ====================================================================

        /// <summary>
        /// Plant mode index → display string.
        /// Engine uses NRC mode definitions: 5=Cold Shutdown, 4=Hot Shutdown, 3=Hot Standby.
        /// </summary>
        private static string GetPlantModeString(int mode)
        {
            return mode switch
            {
                5 => "MODE 5 — COLD SHUTDOWN",
                4 => "MODE 4 — HOT SHUTDOWN",
                3 => "MODE 3 — HOT STANDBY",
                _ => $"MODE {mode}"
            };
        }

        /// <summary>Plant mode index → header color.</summary>
        private static Color GetPlantModeColor(int mode)
        {
            return mode switch
            {
                5 => UITKDashboardTheme.InfoCyan,        // Cold Shutdown
                4 => UITKDashboardTheme.WarningAmber,    // Hot Shutdown
                3 => UITKDashboardTheme.NormalGreen,     // Hot Standby
                _ => UITKDashboardTheme.TextSecondary
            };
        }

        // ====================================================================
        // HELPER: UI Element Factories
        // ====================================================================

        /// <summary>Create a styled Label.</summary>
        private static Label MakeLabel(string text, float fontSize, FontStyle fontStyle, Color color)
        {
            var label = new Label(text);
            label.style.fontSize = fontSize;
            label.style.unityFontStyleAndWeight = fontStyle;
            label.style.color = color;
            return label;
        }

        /// <summary>Create a header status pill (sim time, mode, speed).</summary>
        private static Label MakeHeaderPill(string text, Color color)
        {
            var pill = new Label(text);
            pill.style.fontSize = 13f;
            pill.style.unityFontStyleAndWeight = FontStyle.Bold;
            pill.style.color = color;
            pill.style.marginLeft = 16f;
            pill.style.paddingLeft = 10f;
            pill.style.paddingRight = 10f;
            pill.style.paddingTop = 4f;
            pill.style.paddingBottom = 4f;
            pill.style.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 0.6f);
            pill.style.borderTopLeftRadius = 4f;
            pill.style.borderTopRightRadius = 4f;
            pill.style.borderBottomLeftRadius = 4f;
            pill.style.borderBottomRightRadius = 4f;
            pill.style.borderTopWidth = 1f;
            pill.style.borderBottomWidth = 1f;
            pill.style.borderLeftWidth = 1f;
            pill.style.borderRightWidth = 1f;
            pill.style.borderTopColor = new Color(0.15f, 0.18f, 0.25f, 1f);
            pill.style.borderBottomColor = new Color(0.15f, 0.18f, 0.25f, 1f);
            pill.style.borderLeftColor = new Color(0.15f, 0.18f, 0.25f, 1f);
            pill.style.borderRightColor = new Color(0.15f, 0.18f, 0.25f, 1f);
            return pill;
        }

        /// <summary>Create a section panel with title bar (reusable across tabs).</summary>
        protected VisualElement MakePanel(string title)
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Column;
            panel.style.overflow = Overflow.Hidden;
            panel.style.backgroundColor = new Color(0.078f, 0.102f, 0.157f, 1f);
            panel.style.borderTopLeftRadius = 6f;
            panel.style.borderTopRightRadius = 6f;
            panel.style.borderBottomLeftRadius = 6f;
            panel.style.borderBottomRightRadius = 6f;
            panel.style.paddingTop = 8f;
            panel.style.paddingBottom = 8f;
            panel.style.paddingLeft = 8f;
            panel.style.paddingRight = 8f;
            panel.style.marginRight = 6f;
            panel.style.marginBottom = 6f;
            panel.style.minWidth = 0f;

            var titleLabel = MakeLabel(title, 11f, FontStyle.Bold, UITKDashboardTheme.TextSecondary);
            titleLabel.style.marginBottom = 6f;
            panel.Add(titleLabel);
            return panel;
        }

        /// <summary>Create a horizontal row container.</summary>
        protected static VisualElement MakeRow(float grow = 0f)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Stretch;
            row.style.minHeight = 0f;
            if (grow > 0f) row.style.flexGrow = grow;
            return row;
        }

        /// <summary>Create a tab root container.</summary>
        protected static VisualElement MakeTabRoot(string name)
        {
            var tab = new VisualElement { name = name };
            tab.style.flexGrow = 1f;
            tab.style.flexDirection = FlexDirection.Column;
            tab.style.overflow = Overflow.Hidden;
            return tab;
        }

        /// <summary>Register a metric label for data binding by key.</summary>
        protected void RegisterMetric(string key, Label label)
        {
            _metricLabels[key] = label;
        }

        /// <summary>Set a metric label's text and optional color.</summary>
        protected void SetMetric(string key, string text, Color? color = null)
        {
            if (_metricLabels.TryGetValue(key, out var label))
            {
                label.text = text;
                if (color.HasValue)
                    label.style.color = color.Value;
            }
        }

        /// <summary>Register an ArcGauge for animation ticking.</summary>
        protected void RegisterArcGauge(string key, ArcGaugeElement gauge)
        {
            _arcGauges[key] = gauge;
            _allArcGauges.Add(gauge);
        }

        /// <summary>Register an LED for flash animation ticking.</summary>
        protected void RegisterLED(string key, LEDIndicatorElement led)
        {
            _ledIndicators[key] = led;
            _allLEDs.Add(led);
        }

        // ====================================================================
        // SCENARIO SELECTOR (routed from SceneBridge F2)
        // ====================================================================

        /// <summary>
        /// Toggle the scenario selector overlay.
        /// Called by SceneBridge when F2 is pressed.
        /// TODO: Implement UITK scenario selector panel.
        /// </summary>
        public void ToggleScenarioSelector()
        {
            if (enableDebugLogging)
                Debug.Log("[UITKDashboardV2] ToggleScenarioSelector called (not yet implemented)");
        }
    }
}
