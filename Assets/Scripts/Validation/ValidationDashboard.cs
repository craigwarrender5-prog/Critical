// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// ValidationDashboard.cs - Main MonoBehaviour and Core Coordinator
// ============================================================================
//
// PURPOSE:
//   New OnGUI-based validation dashboard with comprehensive parameter coverage.
//   Replaces the overcrowded HeatupValidationVisual with a cleaner 5-column
//   layout featuring all critical parameters visible on a single screen.
//
// ARCHITECTURE:
//   This is the CORE class. Companion partials (single responsibility):
//     - ValidationDashboard.Layout.cs      : Screen region calculations
//     - ValidationDashboard.Styles.cs      : Colors, fonts, GUIStyles
//     - ValidationDashboard.Gauges.cs      : Arc gauge, bar gauge, LED rendering
//     - ValidationDashboard.Panels.cs      : Panel/section rendering helpers
//     - ValidationDashboard.Snapshot.cs    : Data snapshot class
//     - ValidationDashboard.Strings.cs     : String preformatting
//   Tab classes in Tabs/ subdirectory:
//     - DashboardTab.cs                    : Abstract base class
//     - OverviewTab.cs                     : Primary operations surface
//     - (additional tabs in later stages)
//
// SCENE INTEGRATION:
//   - Lives in Validator.unity scene (loaded additively via V key)
//   - SceneBridge.cs handles scene loading/unloading
//   - Finds persistent HeatupSimEngine via FindObjectOfType
//   - Replaces legacy HeatupValidationVisual
//
// KEYBOARD:
//   Ctrl+1-8 : Switch tabs
//   +/-      : Increment/decrement time acceleration
//
// GOLD STANDARD: Yes (from day one per IP-0043)
// VERSION: 1.0.0.0
// DATE: 2026-02-17
// ============================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace Critical.Validation
{
    /// <summary>
    /// New Validation Dashboard — OnGUI coordinator with comprehensive parameter coverage.
    /// Reads HeatupSimEngine public state via snapshot pattern.
    /// All rendering delegated to partials and tab classes.
    /// </summary>
    public partial class ValidationDashboard : MonoBehaviour
    {
        // ====================================================================
        // INSPECTOR SETTINGS
        // ====================================================================

        [Header("Engine Reference")]
        [Tooltip("The heatup simulation engine to monitor")]
        public HeatupSimEngine engine;

        [Header("Dashboard Settings")]
        [Tooltip("Dashboard refresh rate in Hz (snapshot capture rate)")]
        [Range(2f, 30f)]
        public float refreshRate = 10f;

        [Tooltip("Show dashboard on start")]
        public bool showOnStart = true;

        [Header("Debug")]
        [Tooltip("Enable performance timing logs")]
        public bool enablePerfLogs = false;

        // ====================================================================
        // PUBLIC STATE
        // ====================================================================

        /// <summary>Is this dashboard currently visible?</summary>
        [HideInInspector] public bool dashboardVisible = true;

        /// <summary>Is this dashboard active (vs legacy HeatupValidationVisual)?</summary>
        [HideInInspector] public bool isActiveDashboard = true;

        // ====================================================================
        // LAYOUT CONSTANTS
        // ====================================================================

        /// <summary>Header height as fraction of screen height</summary>
        private const float HEADER_HEIGHT_FRAC = 0.055f;

        /// <summary>Minimum header height in pixels</summary>
        private const float HEADER_MIN_HEIGHT = 36f;

        /// <summary>Tab bar height in pixels</summary>
        private const float TAB_BAR_HEIGHT = 26f;

        // ====================================================================
        // INTERNAL STATE
        // ====================================================================

        // Tab management
        private int _currentTab = 0;
        private readonly string[] _tabLabels = new string[]
        {
            "OVERVIEW",     // 0: Primary operations surface
            "RCS",          // 1: RCS Primary details
            "PZR",          // 2: Pressurizer details
            "CVCS",         // 3: CVCS/VCT details
            "SG/RHR",       // 4: SG and RHR details
            "SYSTEMS",      // 5: BRS, Orifices, Mass Conservation
            "GRAPHS",       // 6: Strip chart trends
            "LOG"           // 7: Event log and annunciators
        };

        // Tab instances
        private OverviewTab _overviewTab;
        private RCSTab _rcsTab;
        private PressurizerTab _pzrTab;
        private CVCSTab _cvcsTab;
        private SGRHRTab _sgRhrTab;
        private SystemsTab _systemsTab;
        private GraphsTab _graphsTab;
        private LogTab _logTab;

        // Sparkline manager (shared across tabs)
        private SparklineManager _sparklineManager;

        // Annunciator manager
        private AnnunciatorManager _annunciatorManager;

        // Snapshot and timing
        private DashboardSnapshot _snapshot;
        private float _lastSnapshotTime;
        private float _lastRefreshTime;

        // Cached screen dimensions
        private float _screenWidth;
        private float _screenHeight;



        // Performance tracking
        private float _lastOnGuiTime;
        private float _maxOnGuiTime;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        void Awake()
        {
            _snapshot = new DashboardSnapshot();
            
            // Create sparkline manager
            _sparklineManager = new SparklineManager();
            
            // Create annunciator manager
            _annunciatorManager = new AnnunciatorManager();
            _annunciatorManager.Initialize();
            
            // Create tab instances
            _overviewTab = new OverviewTab(this);
            _rcsTab = new RCSTab(this);
            _pzrTab = new PressurizerTab(this);
            _cvcsTab = new CVCSTab(this);
            _sgRhrTab = new SGRHRTab(this);
            _systemsTab = new SystemsTab(this);
            _graphsTab = new GraphsTab(this);
            _logTab = new LogTab(this);
        }

        void Start()
        {
            dashboardVisible = showOnStart;

            // Auto-find engine if not assigned
            if (engine == null)
                engine = FindObjectOfType<HeatupSimEngine>();

            if (engine == null)
            {
                Debug.LogError("[ValidationDashboard] No HeatupSimEngine found in scene!");
            }
            else
            {
                // Initial snapshot capture
                _snapshot.CaptureFrom(engine);
            }

            Debug.Log("[ValidationDashboard] Initialized v1.0.0.0");
        }

        void Update()
        {
            // Capture snapshot at refresh rate
            if (engine != null && Time.time - _lastSnapshotTime >= 1f / refreshRate)
            {
                _lastSnapshotTime = Time.time;
                _snapshot.CaptureFrom(engine);
                PreformatStrings();
                
                // Update sparklines with new data
                if (_sparklineManager != null && _sparklineManager.IsInitialized)
                {
                    _sparklineManager.PushValues(_snapshot);
                    _sparklineManager.UpdateTextures();
                }
                
                // Update annunciators
                if (_annunciatorManager != null && _annunciatorManager.IsInitialized)
                {
                    _annunciatorManager.Update(_snapshot);
                }
            }

            // Process keyboard input
            ProcessInput();
        }

        void OnGUI()
        {
            // Skip if not visible or not active dashboard
            if (!dashboardVisible || !isActiveDashboard || engine == null)
                return;

            float startTime = Time.realtimeSinceStartup;

            // Throttle Layout events to refresh rate
            if (Event.current.type == EventType.Layout)
            {
                float interval = 1f / refreshRate;
                if (Time.unscaledTime - _lastRefreshTime < interval)
                    return;
                _lastRefreshTime = Time.unscaledTime;
            }

            // Cache screen dimensions
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;

            // Ensure styles are initialized
            if (!_stylesInitialized)
                InitializeStyles();

            // Draw full-screen background
            GUI.DrawTexture(new Rect(0, 0, _screenWidth, _screenHeight), _bgTex, ScaleMode.StretchToFill);

            // Draw header bar
            float headerH = Mathf.Max(_screenHeight * HEADER_HEIGHT_FRAC, HEADER_MIN_HEIGHT);
            Rect headerRect = new Rect(0, 0, _screenWidth, headerH);
            DrawHeader(headerRect);

            // Draw tab bar
            float tabBarY = headerH;
            Rect tabBarRect = new Rect(4f, tabBarY + 2f, _screenWidth - 8f, TAB_BAR_HEIGHT - 4f);
            _currentTab = GUI.Toolbar(tabBarRect, _currentTab, _tabLabels, _tabStyle);

            // Draw tab content
            float contentY = headerH + TAB_BAR_HEIGHT;
            float contentH = _screenHeight - contentY;
            Rect contentRect = new Rect(0, contentY, _screenWidth, contentH);
            DrawTabContent(contentRect);

            // Performance tracking
            float elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
            _lastOnGuiTime = elapsed;
            if (elapsed > _maxOnGuiTime)
                _maxOnGuiTime = elapsed;

            if (enablePerfLogs && Event.current.type == EventType.Repaint)
            {
                if (elapsed > 2f)
                {
                    Debug.LogWarning($"[ValidationDashboard] OnGUI exceeded 2ms budget: {elapsed:F2}ms");
                }
            }
        }

        // ====================================================================
        // INPUT HANDLING
        // ====================================================================

        private void ProcessInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // Tab switching (only when visible and active)
            if (dashboardVisible && isActiveDashboard)
            {
                bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
                if (ctrl)
                {
                    if (kb.digit1Key.wasPressedThisFrame) _currentTab = 0;
                    if (kb.digit2Key.wasPressedThisFrame) _currentTab = 1;
                    if (kb.digit3Key.wasPressedThisFrame) _currentTab = 2;
                    if (kb.digit4Key.wasPressedThisFrame) _currentTab = 3;
                    if (kb.digit5Key.wasPressedThisFrame) _currentTab = 4;
                    if (kb.digit6Key.wasPressedThisFrame) _currentTab = 5;
                    if (kb.digit7Key.wasPressedThisFrame) _currentTab = 6;
                    if (kb.digit8Key.wasPressedThisFrame) _currentTab = 7;
                }

                // Time acceleration (+/-)
                if (engine != null)
                {
                    if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame)
                    {
                        int newIndex = (engine.currentSpeedIndex + 1) % 5;
                        engine.SetTimeAcceleration(newIndex);
                    }
                    if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
                    {
                        int newIndex = (engine.currentSpeedIndex - 1 + 5) % 5;
                        engine.SetTimeAcceleration(newIndex);
                    }
                }
            }
        }



        // ====================================================================
        // HEADER RENDERING
        // ====================================================================

        private void DrawHeader(Rect area)
        {
            GUI.Box(area, GUIContent.none, _headerBgStyle);

            float pad = 4f;
            float x = pad;
            float y = area.y + pad;
            float h = area.height - pad * 2f;

            // MODE indicator
            DrawHeaderCell(ref x, y, 130f, h, _cachedModeStr, _cachedModeColor);

            // Phase description
            DrawHeaderCell(ref x, y, 240f, h, _cachedPhaseStr, _cNormalGreen);

            // Sim time
            DrawHeaderCell(ref x, y, 130f, h, _cachedSimTimeStr, _cTextPrimary);

            // Wall time
            DrawHeaderCell(ref x, y, 130f, h, _cachedWallTimeStr, _cTextPrimary);

            // Speed
            DrawHeaderCell(ref x, y, 90f, h, _cachedSpeedStr, _cachedSpeedColor);

            // Alarm count indicator
            int alarmCount = _annunciatorManager?.TotalActiveCount ?? 0;
            if (alarmCount > 0)
            {
                string alarmStr = $"⚠ {alarmCount} ALARM{(alarmCount > 1 ? "S" : "")}";
                Color alarmColor = (_annunciatorManager?.AlarmCount ?? 0) > 0 
                    ? _cAlarmRed 
                    : _cWarningAmber;
                DrawHeaderCell(ref x, y, 100f, h, alarmStr, alarmColor);
            }

            // Performance info (debug)
            if (enablePerfLogs)
            {
                string perfInfo = $"{_lastOnGuiTime:F2}ms";
                DrawHeaderCell(ref x, y, 80f, h, perfInfo, _cTextSecondary);
            }

            // Dashboard indicator (right-aligned)
            string dashLabel = "VALIDATION DASHBOARD v1.0";
            float labelW = 200f;
            Rect labelRect = new Rect(area.width - labelW - pad, area.y + pad, labelW, h);
            GUI.contentColor = _cCyanInfo;
            GUI.Label(labelRect, dashLabel, _headerLabelStyle);
            GUI.contentColor = Color.white;
        }

        private void DrawHeaderCell(ref float x, float y, float w, float h, string text, Color color)
        {
            GUI.contentColor = color;
            GUI.Label(new Rect(x, y, w, h), text ?? "---", _headerLabelStyle);
            GUI.contentColor = Color.white;
            x += w + 4f;
        }

        // ====================================================================
        // TAB CONTENT DISPATCH
        // ====================================================================

        private void DrawTabContent(Rect area)
        {
            switch (_currentTab)
            {
                case 0:
                    // Overview tab — full 5-column layout with 60+ parameters
                    _overviewTab.Draw(area);
                    break;
                case 1:
                    // RCS detail tab
                    _rcsTab.Draw(area);
                    break;
                case 2:
                    // Pressurizer detail tab
                    _pzrTab.Draw(area);
                    break;
                case 3:
                    // CVCS detail tab
                    _cvcsTab.Draw(area);
                    break;
                case 4:
                    // SG/RHR detail tab
                    _sgRhrTab.Draw(area);
                    break;
                case 5:
                    // Systems detail tab
                    _systemsTab.Draw(area);
                    break;
                case 6:
                    // Graphs tab
                    _graphsTab.Draw(area);
                    break;
                case 7:
                    // Event log tab
                    _logTab.Draw(area);
                    break;
            }
        }

        /// <summary>
        /// Draw a placeholder tab for stages not yet implemented.
        /// </summary>
        private void DrawPlaceholderTab(Rect area, string title, string message)
        {
            GUI.Box(area, GUIContent.none, _panelBgStyle);

            float centerX = area.x + area.width / 2f;
            float centerY = area.y + area.height / 2f;

            // Title
            Rect titleRect = new Rect(centerX - 200f, centerY - 40f, 400f, 30f);
            GUI.contentColor = _cTextPrimary;
            GUI.Label(titleRect, title, _sectionHeaderStyle);

            // Message
            Rect msgRect = new Rect(centerX - 200f, centerY, 400f, 30f);
            GUI.contentColor = _cTextSecondary;
            GUI.Label(msgRect, message, _statusLabelStyle);
            GUI.contentColor = Color.white;
        }

        // ====================================================================
        // PUBLIC ACCESSORS (for tab classes)
        // ====================================================================

        /// <summary>Current snapshot for tab classes to read.</summary>
        public DashboardSnapshot Snapshot => _snapshot;

        /// <summary>Current tab index.</summary>
        public int CurrentTab => _currentTab;

        /// <summary>Screen width cached from last OnGUI.</summary>
        public float ScreenWidth => _screenWidth;

        /// <summary>Screen height cached from last OnGUI.</summary>
        public float ScreenHeight => _screenHeight;

        /// <summary>Last OnGUI execution time in ms.</summary>
        public float LastOnGuiTime => _lastOnGuiTime;

        /// <summary>Maximum OnGUI execution time in ms.</summary>
        public float MaxOnGuiTime => _maxOnGuiTime;

        /// <summary>Reset maximum OnGUI time tracking.</summary>
        public void ResetMaxOnGuiTime() => _maxOnGuiTime = 0f;

        /// <summary>Check if performance is within 2ms budget.</summary>
        public bool IsPerformanceWithinBudget => _maxOnGuiTime < 2.0f;

        /// <summary>Sparkline manager for trend graphs.</summary>
        public SparklineManager SparklineManager => _sparklineManager;

        /// <summary>Annunciator manager for alarm tiles.</summary>
        public AnnunciatorManager AnnunciatorManager => _annunciatorManager;

    }
}
