// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard
// UITKDashboardController.cs — Main Controller for UI Toolkit Dashboard
// ============================================================================
//
// PURPOSE:
//   Top-level controller for the UI Toolkit-based Validation Dashboard.
//   Manages UIDocument lifecycle, engine binding, 5Hz data refresh,
//   and coordinates all gauge elements and tabs.
//
// ARCHITECTURE:
//   - Uses Unity UI Toolkit (UIDocument + VisualElement tree)
//   - Replaces both HeatupValidationVisual.cs and ValidationDashboard.cs
//   - 5Hz data refresh rate with smooth gauge animations
//   - Tab-based navigation with CRITICAL tab as primary view
//
// KEYBOARD:
//   V key (via SceneBridge) toggles visibility
//   Ctrl+1-7 switches tabs
//   F5-F9 time acceleration
//
// VERSION: 1.0.0
// DATE: 2026-02-18
// CS: CS-0127 Stage 1
// ============================================================================

using System;
using System.Collections.Generic;
using Critical.Physics;
using Critical.UI.Elements;
using Critical.UI.POC;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using Critical.Validation;

namespace Critical.UI.UIToolkit.ValidationDashboard
{
    /// <summary>
    /// Main controller for the UI Toolkit Validation Dashboard.
    /// </summary>
    public class UITKDashboardController : MonoBehaviour
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
        
        public static UITKDashboardController Instance { get; private set; }
        
        // ====================================================================
        // PUBLIC STATE
        // ====================================================================
        
        /// <summary>Is the dashboard visible?</summary>
        public bool IsVisible => _root != null && _root.style.display != DisplayStyle.None;
        
        /// <summary>Currently active tab index.</summary>
        public int CurrentTabIndex { get; private set; } = 0;
        
        /// <summary>Direct access to engine data.</summary>
        public HeatupSimEngine Engine => engine;
        
        // ====================================================================
        // EVENTS
        // ====================================================================
        
        /// <summary>Fired when data is refreshed (5Hz).</summary>
        public event Action OnDataRefresh;
        
        /// <summary>Fired when tab changes.</summary>
        public event Action<int> OnTabChanged;
        
        /// <summary>Fired when visibility changes.</summary>
        public event Action<bool> OnVisibilityChanged;
        
        // ====================================================================
        // TAB DEFINITIONS
        // ====================================================================
        
        private static readonly string[] TAB_NAMES = new string[]
        {
            "CRITICAL",
            "RCS",
            "PRESSURIZER",
            "CVCS",
            "SG / RHR",
            "CONDENSER",
            "TRENDS",
            "LOG"
        };
        
        // ====================================================================
        // PRIVATE STATE
        // ====================================================================
        
        private VisualElement _root;
        private VisualElement _tabBar;
        private VisualElement _content;
        private Label _simTimeLabel;
        private Label _plantModeLabel;
        private Label _speedLabel;
        private Label _alarmSummaryLabel;
        private ScrollView _eventLogScroll;
        
        private List<Button> _tabButtons = new List<Button>();
        private List<VisualElement> _tabContents = new List<VisualElement>();
        
        // Gauge elements for animation updates
        private List<ArcGaugeElement> _arcGauges = new List<ArcGaugeElement>();
        private List<LEDIndicatorElement> _ledIndicators = new List<LEDIndicatorElement>();
        private readonly Dictionary<string, ArcGaugeElement> _gauges = new Dictionary<string, ArcGaugeElement>();
        private readonly Dictionary<string, LEDIndicatorElement> _leds = new Dictionary<string, LEDIndicatorElement>();
        private readonly Dictionary<string, Label> _metrics = new Dictionary<string, Label>();
        private readonly Dictionary<string, Label> _statusValues = new Dictionary<string, Label>();
        private readonly List<Label> _alarmRows = new List<Label>(8);
        private readonly List<string> _alarmMessages = new List<string>(16);
        
        private float _refreshTimer;
        private float _refreshInterval;
        private bool _initialized = false;
        private int _lastEventLogCount = -1;
        private HeatupValidationVisual _legacyHeatupDashboard;
        private Critical.Validation.ValidationDashboard _legacyValidationDashboard;
        
        // Critical tab gauges (quick reference for data binding)
        private ArcGaugeElement _gaugeSgHeat;
        private ArcGaugeElement _gaugeRhrNet;
        private ArcGaugeElement _gaugeCondenserVac;
        private ArcGaugeElement _gaugeHotwell;
        private ArcGaugeElement _gaugeCharging;
        private ArcGaugeElement _gaugeLetdown;
        private ArcGaugeElement _gaugePzrTemp;
        private ArcGaugeElement _gaugePzrSat;
        private ArcGaugeElement _gaugePzrLevelDetail;
        private ArcGaugeElement _gaugePzrHeater;
        private ArcGaugeElement _gaugeSprayFlow;
        private ArcGaugeElement _gaugeCoreDeltaT;
        private ArcGaugeElement _gaugeRcsHot;
        private ArcGaugeElement _gaugeRcsCold;
        private ArcGaugeElement _gaugeRcsAvg;
        private ArcGaugeElement _gaugeRcsPressure;
        private ArcGaugeElement _gaugeSgPressureDetail;

        private AnalogGaugeElement _criticalAnalogTAvg;
        private AnalogGaugeElement _criticalAnalogPressure;
        private AnalogGaugeElement _criticalAnalogPzrLevel;
        private AnalogGaugeElement _criticalAnalogPzrTemp;
        private AnalogGaugeElement _criticalAnalogSubcool;
        private AnalogGaugeElement _criticalAnalogHeatup;
        private AnalogGaugeElement _criticalAnalogPressRate;
        private AnalogGaugeElement _criticalAnalogCondenserVac;
        private AnalogGaugeElement _criticalAnalogSgPressure;
        private readonly Dictionary<string, Label> _criticalGaugeReadouts = new Dictionary<string, Label>(16);
        private readonly Dictionary<string, VisualElement> _criticalBarFills = new Dictionary<string, VisualElement>(8);
        private readonly Dictionary<string, Label> _criticalBarValues = new Dictionary<string, Label>(8);
        private readonly Dictionary<string, Label> _criticalProcessValues = new Dictionary<string, Label>(16);
        private ScrollView _criticalLogScroll;
        private int _lastCriticalLogCount = -1;
        private TankLevelPOC _criticalVctTank;
        private TankLevelPOC _criticalHotwellTank;
        private BidirectionalGaugePOC _criticalCvcsNetGauge;
        private LinearGaugePOC _criticalLinearSgHeat;
        private LinearGaugePOC _criticalLinearCharging;
        private LinearGaugePOC _criticalLinearLetdown;
        private LinearGaugePOC _criticalLinearHzp;
        private LinearGaugePOC _criticalLinearMass;
        private EdgewiseMeterElement _criticalEdgePressRate;
        private EdgewiseMeterElement _criticalEdgeHeatup;
        private EdgewiseMeterElement _criticalEdgeSubcool;

        private enum CriticalAnnunciatorTone
        {
            Info,
            Warning,
            Alarm
        }

        private sealed class CriticalAnnunciatorBinding
        {
            public VisualElement Root;
            public Label Text;
            public CriticalAnnunciatorTone Tone;
            public Func<HeatupSimEngine, bool> Condition;
            public bool IsActive;
        }

        private readonly List<CriticalAnnunciatorBinding> _criticalAnnunciators = new List<CriticalAnnunciatorBinding>(30);
        private Label _criticalAnnSummary;
        private float _criticalAnnFlashTimer;
        private bool _criticalAnnFlashVisible = true;
        
        // Critical tab LEDs
        private LEDIndicatorElement _ledRcpA;
        private LEDIndicatorElement _ledRcpB;
        private LEDIndicatorElement _ledRcpC;
        private LEDIndicatorElement _ledRcpD;

        private PressurizerVesselPOC _criticalPzrGraphic;
        private PressurizerVesselPOC _pressurizerGraphic;
        private RCSLoopDiagramPOC _rcsGraphic;
        private BidirectionalGaugePOC _cvcsNetGauge;
        private BidirectionalGaugePOC _pzrSurgeGauge;
        private TankLevelPOC _vctTank;
        private TankLevelPOC _hotwellTank;
        private StripChartPOC _criticalChart;
        private StripChartPOC _trendChart;

        private int _criticalTraceTemp = -1;
        private int _criticalTracePressure = -1;
        private int _criticalTraceLevel = -1;
        private int _trendTraceTemp = -1;
        private int _trendTracePressure = -1;
        private int _trendTraceLevel = -1;
        private int _trendTraceSubcool = -1;
        private int _trendTraceHzp = -1;
        
        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[UITKDashboard] Duplicate controller - destroying");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            _refreshInterval = 1f / Mathf.Max(2f, dataRefreshRate);
        }
        
        void Start()
        {
            Initialize();
        }
        
        void Update()
        {
            // Keep trying to find engine if not found yet
            if (engine == null)
            {
                engine = FindObjectOfType<HeatupSimEngine>();
                if (engine != null && enableDebugLogging)
                    Debug.Log("[UITKDashboard] Found HeatupSimEngine");
            }
            
            DisableLegacyDashboards();

            if (!_initialized || engine == null) return;
            
            // Always update animations (even when not refreshing data)
            UpdateAnimations();
            
            // Handle input
            HandleInput();
            
            // Data refresh at specified rate
            _refreshTimer += Time.unscaledDeltaTime;
            if (_refreshTimer >= _refreshInterval)
            {
                RefreshData();
                _refreshTimer = 0f;
            }
        }
        
        void OnDestroy()
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
            {
                engine = FindObjectOfType<HeatupSimEngine>();
            }
            
            _legacyHeatupDashboard = FindObjectOfType<HeatupValidationVisual>();
            _legacyValidationDashboard = FindObjectOfType<Critical.Validation.ValidationDashboard>();
            DisableLegacyDashboards();
            
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
                if (uiDocument == null)
                {
                    return;
                }
            }
            
            _root = uiDocument.rootVisualElement;
            if (_root == null)
            {
                return;
            }

            _root.AddToClassList("vd-root");
            _tabBar = _root.Q<VisualElement>("tab-bar");
            _content = _root.Q<VisualElement>("content");
            _simTimeLabel = _root.Q<Label>("sim-time");
            _plantModeLabel = _root.Q<Label>("plant-mode");
            
            if (_tabBar == null || _content == null)
            {
                BuildDashboardShell();
            }
            else
            {
                EnsureHeaderVisuals();
                _tabBar.Clear();
                _content.Clear();
            }

            BuildTabBar();
            BuildAllTabs();

            SwitchToTab(0);
            SetVisibility(startVisible);
            
            _initialized = true;
            RefreshData();
        }
        
        private void BuildDashboardShell()
        {
            _root.Clear();
            _root.AddToClassList("vd-root");
            
            var header = new VisualElement();
            header.name = "header";
            header.AddToClassList("vd-header");
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.height = 54f;
            header.style.paddingLeft = 14f;
            header.style.paddingRight = 14f;
            header.style.marginBottom = 8f;
            header.style.backgroundColor = new Color(0.063f, 0.082f, 0.125f, 1f);
            header.style.borderTopLeftRadius = 6f;
            header.style.borderTopRightRadius = 6f;
            header.style.borderBottomLeftRadius = 6f;
            header.style.borderBottomRightRadius = 6f;
            
            var headerTitle = new Label("VALIDATION DASHBOARD");
            headerTitle.AddToClassList("vd-title");
            headerTitle.style.fontSize = 20f;
            headerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerTitle.style.color = UITKDashboardTheme.TextPrimary;
            header.Add(headerTitle);
            
            var headerRight = new VisualElement();
            headerRight.name = "header-right";
            headerRight.AddToClassList("vd-header-right");
            headerRight.style.flexDirection = FlexDirection.Row;
            headerRight.style.alignItems = Align.Center;
            
            _simTimeLabel = new Label("00:00:00");
            _simTimeLabel.name = "sim-time";
            _simTimeLabel.AddToClassList("vd-header-status");
            _simTimeLabel.style.fontSize = 12f;
            _simTimeLabel.style.color = UITKDashboardTheme.InfoCyan;
            _simTimeLabel.style.marginLeft = 12f;
            headerRight.Add(_simTimeLabel);
            
            _plantModeLabel = new Label("COLD SHUTDOWN");
            _plantModeLabel.name = "plant-mode";
            _plantModeLabel.AddToClassList("vd-header-status");
            _plantModeLabel.style.fontSize = 12f;
            _plantModeLabel.style.color = UITKDashboardTheme.InfoCyan;
            _plantModeLabel.style.marginLeft = 12f;
            headerRight.Add(_plantModeLabel);

            _speedLabel = new Label("SPEED 1x");
            _speedLabel.name = "speed";
            _speedLabel.AddToClassList("vd-header-status");
            _speedLabel.style.fontSize = 12f;
            _speedLabel.style.color = UITKDashboardTheme.InfoCyan;
            _speedLabel.style.marginLeft = 12f;
            headerRight.Add(_speedLabel);
            
            header.Add(headerRight);
            _root.Add(header);
            
            _tabBar = new VisualElement();
            _tabBar.name = "tab-bar";
            _tabBar.AddToClassList("vd-tab-bar");
            _tabBar.style.flexDirection = FlexDirection.Row;
            _tabBar.style.flexWrap = Wrap.NoWrap;
            _tabBar.style.height = 38f;
            _tabBar.style.marginBottom = 8f;
            _tabBar.style.alignItems = Align.Stretch;
            _root.Add(_tabBar);
            
            _content = new VisualElement();
            _content.name = "content";
            _content.AddToClassList("vd-content");
            _content.style.flexGrow = 1f;
            _content.style.overflow = Overflow.Hidden;
            _content.style.paddingTop = 8f;
            _content.style.paddingBottom = 8f;
            _content.style.paddingLeft = 8f;
            _content.style.paddingRight = 8f;
            _content.style.backgroundColor = new Color(0.055f, 0.071f, 0.106f, 1f);
            _content.style.borderTopLeftRadius = 6f;
            _content.style.borderTopRightRadius = 6f;
            _content.style.borderBottomLeftRadius = 6f;
            _content.style.borderBottomRightRadius = 6f;
            _root.Add(_content);

            ApplyShellInlineStyles();
        }

        private void EnsureHeaderVisuals()
        {
            var header = _root.Q<VisualElement>("header");
            if (header != null)
            {
                header.AddToClassList("vd-header");
            }

            if (_simTimeLabel != null)
            {
                _simTimeLabel.AddToClassList("vd-header-status");
            }

            if (_plantModeLabel != null)
            {
                _plantModeLabel.AddToClassList("vd-header-status");
            }

            if (_speedLabel == null)
            {
                var headerRight = _root.Q<VisualElement>("header-right");
                if (headerRight != null)
                {
                    headerRight.AddToClassList("vd-header-right");
                    _speedLabel = new Label("SPEED 1x");
                    _speedLabel.name = "speed";
                    _speedLabel.AddToClassList("vd-header-status");
                    headerRight.Add(_speedLabel);
                }
            }

            ApplyShellInlineStyles();
        }

        private void ApplyShellInlineStyles()
        {
            if (_root != null)
            {
                _root.style.flexDirection = FlexDirection.Column;
                _root.style.flexGrow = 1f;
                _root.style.paddingTop = 10f;
                _root.style.paddingBottom = 10f;
                _root.style.paddingLeft = 10f;
                _root.style.paddingRight = 10f;
                _root.style.backgroundColor = new Color(0.039f, 0.055f, 0.086f, 1f);
            }

            var header = _root?.Q<VisualElement>("header");
            if (header != null)
            {
                header.style.flexDirection = FlexDirection.Row;
                header.style.alignItems = Align.Center;
                header.style.justifyContent = Justify.SpaceBetween;
                header.style.height = 54f;
                header.style.paddingLeft = 14f;
                header.style.paddingRight = 14f;
                header.style.marginBottom = 8f;
                header.style.backgroundColor = new Color(0.063f, 0.082f, 0.125f, 1f);
            }

            var headerTitle = _root?.Q<Label>("header-title");
            if (headerTitle != null)
            {
                headerTitle.style.fontSize = 20f;
                headerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                headerTitle.style.color = UITKDashboardTheme.TextPrimary;
            }

            var headerRight = _root?.Q<VisualElement>("header-right");
            if (headerRight != null)
            {
                headerRight.style.flexDirection = FlexDirection.Row;
                headerRight.style.alignItems = Align.Center;
            }

            if (_simTimeLabel != null)
            {
                _simTimeLabel.style.fontSize = 12f;
                _simTimeLabel.style.color = UITKDashboardTheme.InfoCyan;
                _simTimeLabel.style.marginLeft = 12f;
            }

            if (_plantModeLabel != null)
            {
                _plantModeLabel.style.fontSize = 12f;
                _plantModeLabel.style.color = UITKDashboardTheme.InfoCyan;
                _plantModeLabel.style.marginLeft = 12f;
            }

            if (_speedLabel != null)
            {
                _speedLabel.style.fontSize = 12f;
                _speedLabel.style.color = UITKDashboardTheme.InfoCyan;
                _speedLabel.style.marginLeft = 12f;
            }

            if (_tabBar != null)
            {
                _tabBar.AddToClassList("vd-tab-bar");
                _tabBar.style.flexDirection = FlexDirection.Row;
                _tabBar.style.flexWrap = Wrap.NoWrap;
                _tabBar.style.height = 38f;
                _tabBar.style.marginBottom = 8f;
                _tabBar.style.alignItems = Align.Stretch;
            }

            if (_content != null)
            {
                _content.AddToClassList("vd-content");
                _content.style.flexGrow = 1f;
                _content.style.overflow = Overflow.Hidden;
                _content.style.paddingTop = 8f;
                _content.style.paddingBottom = 8f;
                _content.style.paddingLeft = 8f;
                _content.style.paddingRight = 8f;
                _content.style.backgroundColor = new Color(0.055f, 0.071f, 0.106f, 1f);
                _content.style.borderTopLeftRadius = 6f;
                _content.style.borderTopRightRadius = 6f;
                _content.style.borderBottomLeftRadius = 6f;
                _content.style.borderBottomRightRadius = 6f;
            }
        }
        
        private void BuildTabBar()
        {
            if (_tabBar == null) return;
            
            _tabBar.Clear();
            _tabButtons.Clear();
            _tabBar.style.flexDirection = FlexDirection.Row;
            _tabBar.style.flexWrap = Wrap.NoWrap;
            _tabBar.style.height = 38f;
            _tabBar.style.alignItems = Align.Stretch;
            
            for (int i = 0; i < TAB_NAMES.Length; i++)
            {
                int tabIndex = i; // Capture for closure
                
                var tabButton = new Button(() => SwitchToTab(tabIndex));
                tabButton.text = TAB_NAMES[i];
                tabButton.AddToClassList("vd-tab-button");
                tabButton.style.flexGrow = 1f;
                tabButton.style.flexShrink = 1f;
                tabButton.style.flexBasis = 0f;
                tabButton.style.minWidth = 90f;
                tabButton.style.height = 36f;
                tabButton.style.unityTextAlign = TextAnchor.MiddleCenter;
                tabButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                tabButton.style.fontSize = 11f;
                tabButton.style.paddingLeft = 8f;
                tabButton.style.paddingRight = 8f;
                tabButton.style.marginRight = i < TAB_NAMES.Length - 1 ? 4f : 0f;
                tabButton.style.borderBottomWidth = 0f;
                tabButton.style.borderLeftWidth = 0f;
                tabButton.style.borderRightWidth = 0f;
                tabButton.style.borderTopWidth = 0f;
                tabButton.style.borderTopLeftRadius = 4f;
                tabButton.style.borderTopRightRadius = 4f;
                ApplyTabButtonState(tabButton, false);
                
                _tabBar.Add(tabButton);
                _tabButtons.Add(tabButton);
            }
        }

        private void ApplyTabButtonState(Button tabButton, bool active)
        {
            if (active)
            {
                tabButton.AddToClassList("vd-tab-button--active");
                tabButton.style.backgroundColor = new Color(0.145f, 0.416f, 0.839f, 1f);
                tabButton.style.color = new Color(0.933f, 0.957f, 0.988f, 1f);
            }
            else
            {
                tabButton.RemoveFromClassList("vd-tab-button--active");
                tabButton.style.backgroundColor = new Color(0.102f, 0.133f, 0.204f, 1f);
                tabButton.style.color = new Color(0.529f, 0.588f, 0.675f, 1f);
            }
        }

        private void BuildAllTabs()
        {
            _tabContents.Clear();
            _arcGauges.Clear();
            _ledIndicators.Clear();
            _gauges.Clear();
            _leds.Clear();
            _metrics.Clear();
            _statusValues.Clear();
            _alarmRows.Clear();
            _alarmMessages.Clear();
            _criticalGaugeReadouts.Clear();
            _criticalBarFills.Clear();
            _criticalBarValues.Clear();
            _criticalProcessValues.Clear();
            _criticalAnnunciators.Clear();
            _criticalAnnSummary = null;
            _criticalAnnFlashTimer = 0f;
            _criticalAnnFlashVisible = true;
            _criticalPzrGraphic = null;
            _pressurizerGraphic = null;
            _rcsGraphic = null;
            _criticalAnalogTAvg = null;
            _criticalAnalogPressure = null;
            _criticalAnalogPzrLevel = null;
            _criticalAnalogPzrTemp = null;
            _criticalAnalogSubcool = null;
            _criticalAnalogHeatup = null;
            _criticalAnalogPressRate = null;
            _criticalAnalogCondenserVac = null;
            _criticalAnalogSgPressure = null;
            _criticalVctTank = null;
            _criticalHotwellTank = null;
            _criticalCvcsNetGauge = null;
            _criticalLinearSgHeat = null;
            _criticalLinearCharging = null;
            _criticalLinearLetdown = null;
            _criticalLinearHzp = null;
            _criticalLinearMass = null;
            _criticalEdgePressRate = null;
            _criticalEdgeHeatup = null;
            _criticalEdgeSubcool = null;
            _cvcsNetGauge = null;
            _pzrSurgeGauge = null;
            _vctTank = null;
            _hotwellTank = null;
            _criticalChart = null;
            _trendChart = null;
            _criticalLogScroll = null;
            _eventLogScroll = null;
            _lastCriticalLogCount = -1;
            _lastEventLogCount = -1;

            AddTab(BuildCriticalTab());
            AddTab(BuildRcsTab());
            AddTab(BuildPressurizerTab());
            AddTab(BuildCvcsTab());
            AddTab(BuildSgRhrTab());
            AddTab(BuildCondenserTab());
            AddTab(BuildTrendsTab());
            AddTab(BuildLogTab());
        }

        private void AddTab(VisualElement tab)
        {
            _content.Add(tab);
            _tabContents.Add(tab);
            tab.style.display = DisplayStyle.None;
        }
        
        private VisualElement BuildCriticalTab()
        {
            var tab = NewTab("tab-critical");

            var topRow = NewRow(2.1f);
            topRow.style.minHeight = 0f;

            var gaugePanel = CreatePanel("CORE FLIGHT DECK");
            gaugePanel.style.flexGrow = 1.55f;
            gaugePanel.style.minWidth = 0f;

            var gaugeGrid = new VisualElement();
            gaugeGrid.style.flexDirection = FlexDirection.Row;
            gaugeGrid.style.flexWrap = Wrap.Wrap;
            gaugeGrid.style.alignContent = Align.FlexStart;
            gaugeGrid.style.marginBottom = 6f;

            gaugeGrid.Add(CreateCriticalGaugeCard("cg_tavg", "RCS AVG TEMP", "F", 70f, 600f, 100f, 4, out _criticalAnalogTAvg));
            gaugeGrid.Add(CreateCriticalGaugeCard("cg_pressure", "RCS PRESSURE", "PSIA", 0f, 2600f, 500f, 4, out _criticalAnalogPressure));
            gaugeGrid.Add(CreateCriticalGaugeCard("cg_pzr_level", "PZR LEVEL", "%", 0f, 100f, 20f, 4, out _criticalAnalogPzrLevel));
            gaugeGrid.Add(CreateCriticalGaugeCard("cg_pzr_temp", "PZR TEMP", "F", 50f, 700f, 100f, 4, out _criticalAnalogPzrTemp));
            gaugeGrid.Add(CreateCriticalGaugeCard("cg_subcool", "SUBCOOLING", "F", 0f, 120f, 20f, 4, out _criticalAnalogSubcool));
            gaugeGrid.Add(CreateCriticalGaugeCard("cg_sg_pressure", "SG PRESSURE", "PSIA", 0f, 1400f, 200f, 4, out _criticalAnalogSgPressure));
            gaugeGrid.Add(CreateCriticalGaugeCard("cg_heatup", "HEATUP RATE", "F/HR", 0f, 120f, 20f, 4, out _criticalAnalogHeatup));
            gaugeGrid.Add(CreateCriticalGaugeCard("cg_press_rate", "PRESS RATE", "PSI/HR", 0f, 300f, 50f, 5, out _criticalAnalogPressRate));
            gaugeGrid.Add(CreateCriticalGaugeCard("cg_vac", "COND VAC", "inHg", 0f, 30f, 5f, 5, out _criticalAnalogCondenserVac));
            gaugePanel.Add(gaugeGrid);

            var ledRow = NewRow();
            ledRow.AddToClassList("vd-led-row");
            _ledRcpA = CreateLED("RCP-A");
            _ledRcpB = CreateLED("RCP-B");
            _ledRcpC = CreateLED("RCP-C");
            _ledRcpD = CreateLED("RCP-D");
            ledRow.Add(_ledRcpA);
            ledRow.Add(_ledRcpB);
            ledRow.Add(_ledRcpC);
            ledRow.Add(_ledRcpD);
            gaugePanel.Add(ledRow);

            var statusBanner = NewMetricGrid();
            statusBanner.style.marginTop = 4f;
            statusBanner.style.marginBottom = 2f;
            AddMetric(statusBanner, "critical_mode", "Plant Mode", true);
            AddMetric(statusBanner, "critical_phase", "Heatup Phase", true);
            AddMetric(statusBanner, "critical_bubble", "PZR State", true);
            AddMetric(statusBanner, "critical_dump", "Steam Dump", true);
            gaugePanel.Add(statusBanner);
            topRow.Add(gaugePanel);

            var annPanel = CreatePanel("ANNUNCIATOR WALL");
            annPanel.style.flexGrow = 1.15f;
            annPanel.style.minWidth = 0f;
            annPanel.Add(CreateCriticalAnnunciatorWall());

            var annStatusRow = new VisualElement();
            annStatusRow.style.flexDirection = FlexDirection.Row;
            annStatusRow.style.justifyContent = Justify.SpaceBetween;
            annStatusRow.style.alignItems = Align.Center;
            annStatusRow.style.marginTop = 2f;
            var annTitle = new Label("3x10 MATRIX");
            annTitle.style.fontSize = 10f;
            annTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            annTitle.style.color = new Color(0.486f, 0.573f, 0.714f, 1f);
            annStatusRow.Add(annTitle);
            _criticalAnnSummary = new Label("0 active / 0 alarm");
            _criticalAnnSummary.style.fontSize = 11f;
            _criticalAnnSummary.style.unityFontStyleAndWeight = FontStyle.Bold;
            _criticalAnnSummary.style.color = UITKDashboardTheme.TextSecondary;
            annStatusRow.Add(_criticalAnnSummary);
            annPanel.Add(annStatusRow);
            topRow.Add(annPanel);
            tab.Add(topRow);

            var bottomRow = NewRow(1.55f);
            bottomRow.style.minHeight = 0f;

            var trendPanel = CreatePanel("STRIP TRENDS");
            trendPanel.style.flexGrow = 1.2f;
            trendPanel.style.minWidth = 0f;
            _criticalChart = new StripChartPOC();
            _criticalChart.AddToClassList("vd-chart");
            _criticalChart.style.flexGrow = 1f;
            _criticalChart.style.minHeight = 260f;
            _criticalTraceTemp = _criticalChart.AddTrace("T AVG", new Color(0f, 0.9f, 0.45f, 1f), 70f, 600f);
            _criticalTracePressure = _criticalChart.AddTrace("PRESS", new Color(1f, 0.75f, 0f, 1f), 0f, 2600f);
            _criticalTraceLevel = _criticalChart.AddTrace("PZR %", new Color(0.3f, 0.8f, 1f, 1f), 0f, 100f);
            trendPanel.Add(_criticalChart);
            bottomRow.Add(trendPanel);

            var processPanel = CreatePanel("PROCESS METER DECK");
            processPanel.style.flexGrow = 1.05f;
            processPanel.style.minWidth = 0f;

            var edgeRow = new VisualElement();
            edgeRow.style.flexDirection = FlexDirection.Row;
            edgeRow.style.justifyContent = Justify.SpaceBetween;
            edgeRow.style.marginBottom = 8f;
            edgeRow.Add(CreateEdgeMeterCard("edge_press_rate", "PRESS RATE", "psi/hr", -300f, 300f, out _criticalEdgePressRate));
            edgeRow.Add(CreateEdgeMeterCard("edge_heatup", "HEATUP", "F/hr", -120f, 120f, out _criticalEdgeHeatup));
            edgeRow.Add(CreateEdgeMeterCard("edge_subcool", "SUBCOOL", "F", 0f, 120f, out _criticalEdgeSubcool));
            processPanel.Add(edgeRow);

            var tanksRow = new VisualElement();
            tanksRow.style.flexDirection = FlexDirection.Row;
            tanksRow.style.justifyContent = Justify.SpaceBetween;
            tanksRow.style.marginBottom = 8f;
            tanksRow.Add(CreateTankCard("tank_vct", "VCT", 0f, 100f, 20f, 80f, out _criticalVctTank));
            tanksRow.Add(CreateTankCard("tank_hotwell", "HOTWELL", 0f, 100f, 20f, 80f, out _criticalHotwellTank));
            processPanel.Add(tanksRow);

            var netLabel = new Label("NET CVCS (+ charging / - letdown)");
            netLabel.style.fontSize = 9f;
            netLabel.style.color = new Color(0.63f, 0.72f, 0.84f, 1f);
            netLabel.style.marginBottom = 2f;
            processPanel.Add(netLabel);
            _criticalCvcsNetGauge = new BidirectionalGaugePOC { minValue = -140f, maxValue = 140f };
            _criticalCvcsNetGauge.style.height = 18f;
            _criticalCvcsNetGauge.style.marginBottom = 8f;
            processPanel.Add(_criticalCvcsNetGauge);

            processPanel.Add(CreateLinearGaugeMetric("lin_sg_heat", "SG HEAT", 0f, 120f, 80f, 100f, out _criticalLinearSgHeat));
            processPanel.Add(CreateLinearGaugeMetric("lin_charging", "CHARGING", 0f, 220f, 150f, 190f, out _criticalLinearCharging));
            processPanel.Add(CreateLinearGaugeMetric("lin_letdown", "LETDOWN", 0f, 220f, 150f, 190f, out _criticalLinearLetdown));
            processPanel.Add(CreateLinearGaugeMetric("lin_hzp", "HZP PROGRESS", 0f, 100f, 80f, 95f, out _criticalLinearHzp));
            processPanel.Add(CreateLinearGaugeMetric("lin_mass", "MASS ERROR", 0f, 1000f, 300f, 600f, out _criticalLinearMass));
            bottomRow.Add(processPanel);

            var logPanel = CreatePanel("OPS LOG");
            logPanel.style.flexGrow = 0.95f;
            logPanel.style.minWidth = 0f;
            _alarmSummaryLabel = new Label("No active alarms");
            _alarmSummaryLabel.AddToClassList("vd-alarm-summary");
            logPanel.Add(_alarmSummaryLabel);
            for (int i = 0; i < 4; i++)
            {
                var row = new Label("--");
                row.AddToClassList("vd-alarm-row");
                _alarmRows.Add(row);
                logPanel.Add(row);
            }
            _criticalLogScroll = new ScrollView(ScrollViewMode.Vertical);
            _criticalLogScroll.style.flexGrow = 1f;
            _criticalLogScroll.style.marginTop = 6f;
            _criticalLogScroll.style.backgroundColor = new Color(0.032f, 0.043f, 0.07f, 1f);
            _criticalLogScroll.style.borderTopLeftRadius = 4f;
            _criticalLogScroll.style.borderTopRightRadius = 4f;
            _criticalLogScroll.style.borderBottomLeftRadius = 4f;
            _criticalLogScroll.style.borderBottomRightRadius = 4f;
            _criticalLogScroll.style.paddingLeft = 4f;
            _criticalLogScroll.style.paddingRight = 4f;
            logPanel.Add(_criticalLogScroll);
            bottomRow.Add(logPanel);

            _gaugeCoreDeltaT = CreateArcGauge("DELTA-T", "F", 0f, 90f);
            _gaugeCoreDeltaT.style.display = DisplayStyle.None;

            tab.Add(bottomRow);

            return tab;
        }

        private VisualElement BuildRcsTab()
        {
            var tab = NewTab("tab-rcs");
            var row = NewRow(1f);

            var leftPanel = CreatePanel("RCS LOOP OVERVIEW");
            leftPanel.style.flexGrow = 1.8f;
            _rcsGraphic = new RCSLoopDiagramPOC();
            _rcsGraphic.AddToClassList("vd-graphic");
            _rcsGraphic.style.flexGrow = 1f;
            _rcsGraphic.style.minHeight = 460f;
            leftPanel.Add(_rcsGraphic);
            row.Add(leftPanel);

            var rightPanel = CreatePanel("RCS DRILL DOWN");
            rightPanel.style.flexGrow = 1f;
            var grid = NewGaugeGrid();
            _gaugeRcsHot = CreateArcGauge("HOT LEG", "F", 50f, 650f);
            _gaugeRcsCold = CreateArcGauge("COLD LEG", "F", 50f, 650f);
            _gaugeRcsAvg = CreateArcGauge("AVERAGE", "F", 50f, 650f);
            _gaugeRcsPressure = CreateArcGauge("PRESSURE", "psia", 0f, 2600f);
            grid.Add(_gaugeRcsHot);
            grid.Add(_gaugeRcsCold);
            grid.Add(_gaugeRcsAvg);
            grid.Add(_gaugeRcsPressure);
            _gaugeCoreDeltaT.style.display = DisplayStyle.Flex;
            grid.Add(_gaugeCoreDeltaT);
            rightPanel.Add(grid);

            var metrics = NewMetricGrid();
            AddMetric(metrics, "rcs_mode", "Plant Mode");
            AddMetric(metrics, "rcs_subcool", "Subcooling");
            AddMetric(metrics, "rcs_rate", "Heatup Rate");
            AddMetric(metrics, "rcs_rhr", "RHR Net Heat");
            rightPanel.Add(metrics);
            row.Add(rightPanel);

            tab.Add(row);
            return tab;
        }

        private VisualElement BuildPressurizerTab()
        {
            var tab = NewTab("tab-pressurizer");
            var row = NewRow(1f);

            var leftPanel = CreatePanel("PRESSURIZER VESSEL");
            leftPanel.style.flexGrow = 1.4f;
            leftPanel.style.minWidth = 0f;
            _pressurizerGraphic = new PressurizerVesselPOC();
            _pressurizerGraphic.AddToClassList("vd-graphic");
            _pressurizerGraphic.style.width = 500f;
            _pressurizerGraphic.style.height = 560f;
            _pressurizerGraphic.style.flexGrow = 0f;
            _pressurizerGraphic.style.flexShrink = 0f;
            var pzrWrap = new VisualElement();
            pzrWrap.style.flexGrow = 1f;
            pzrWrap.style.justifyContent = Justify.Center;
            pzrWrap.style.alignItems = Align.Center;
            pzrWrap.style.overflow = Overflow.Hidden;
            pzrWrap.style.minHeight = 540f;
            pzrWrap.Add(_pressurizerGraphic);
            leftPanel.Add(pzrWrap);
            row.Add(leftPanel);

            var rightPanel = CreatePanel("PRESSURIZER DRILL DOWN");
            rightPanel.style.flexGrow = 1f;
            rightPanel.style.minWidth = 0f;
            var grid = NewGaugeGrid();
            _gaugePzrTemp = CreateArcGauge("PZR TEMP", "F", 50f, 700f);
            _gaugePzrSat = CreateArcGauge("TSAT", "F", 50f, 700f);
            _gaugePzrLevelDetail = CreateArcGauge("LEVEL", "%", 0f, 100f);
            _gaugeSprayFlow = CreateArcGauge("SPRAY", "gpm", 0f, 250f);
            _gaugePzrHeater = CreateArcGauge("HEATER", "%", 0f, 100f);
            grid.Add(_gaugePzrLevelDetail);
            grid.Add(_gaugePzrTemp);
            grid.Add(_gaugePzrSat);
            grid.Add(_gaugeSprayFlow);
            grid.Add(_gaugePzrHeater);
            rightPanel.Add(grid);

            var surgeTitle = new Label("SURGE FLOW (IN / OUT)");
            surgeTitle.AddToClassList("vd-subtitle");
            rightPanel.Add(surgeTitle);
            _pzrSurgeGauge = new BidirectionalGaugePOC { minValue = -180f, maxValue = 180f };
            _pzrSurgeGauge.AddToClassList("vd-bidir");
            rightPanel.Add(_pzrSurgeGauge);

            var metrics = NewMetricGrid();
            AddMetric(metrics, "pzr_phase", "Bubble Phase");
            AddMetric(metrics, "pzr_state", "State");
            AddMetric(metrics, "pzr_steam", "Steam Volume");
            AddMetric(metrics, "pzr_surge", "Surge");
            rightPanel.Add(metrics);
            row.Add(rightPanel);

            tab.Add(row);
            return tab;
        }

        private VisualElement BuildCvcsTab()
        {
            var tab = NewTab("tab-cvcs");
            var row = NewRow(1f);

            var leftPanel = CreatePanel("CVCS FLOW BALANCE");
            leftPanel.style.flexGrow = 1.2f;
            var grid = NewGaugeGrid();
            _gaugeCharging = CreateArcGauge("CHARGING", "gpm", 0f, 220f);
            _gaugeLetdown = CreateArcGauge("LETDOWN", "gpm", 0f, 220f);
            grid.Add(_gaugeCharging);
            grid.Add(_gaugeLetdown);
            leftPanel.Add(grid);

            var netLabel = new Label("NET FLOW (CHARGING - LETDOWN)");
            netLabel.AddToClassList("vd-subtitle");
            leftPanel.Add(netLabel);
            _cvcsNetGauge = new BidirectionalGaugePOC { minValue = -140f, maxValue = 140f };
            _cvcsNetGauge.AddToClassList("vd-bidir");
            leftPanel.Add(_cvcsNetGauge);

            var metrics = NewMetricGrid();
            AddMetric(metrics, "cvcs_net", "Net Flow");
            AddMetric(metrics, "cvcs_vct", "VCT Level");
            AddMetric(metrics, "cvcs_brs", "BRS");
            AddMetric(metrics, "cvcs_seal", "Seal Injection");
            leftPanel.Add(metrics);
            row.Add(leftPanel);

            var rightPanel = CreatePanel("TANKS AND INVENTORY");
            rightPanel.style.flexGrow = 1f;
            _vctTank = new TankLevelPOC { minValue = 0f, maxValue = 100f, lowAlarm = 20f, highAlarm = 80f };
            _vctTank.style.height = 260f;
            _vctTank.style.width = 90f;
            rightPanel.Add(_vctTank);
            row.Add(rightPanel);

            tab.Add(row);
            return tab;
        }

        private VisualElement BuildSgRhrTab()
        {
            var tab = NewTab("tab-sg-rhr");
            var row = NewRow(1f);

            var leftPanel = CreatePanel("STEAM GENERATOR PERFORMANCE");
            leftPanel.style.flexGrow = 1.1f;
            var leftGrid = NewGaugeGrid();
            _gaugeSgHeat = CreateArcGauge("SG HEAT", "MW", 0f, 120f);
            _gaugeSgPressureDetail = CreateArcGauge("SG PRESS", "psia", 0f, 1400f);
            leftGrid.Add(_gaugeSgPressureDetail);
            leftGrid.Add(_gaugeSgHeat);
            leftPanel.Add(leftGrid);
            row.Add(leftPanel);

            var rightPanel = CreatePanel("RHR AND SECONDARY HEAT SINK");
            rightPanel.style.flexGrow = 1f;
            var rightGrid = NewGaugeGrid();
            _gaugeRhrNet = CreateArcGauge("RHR NET", "MW", -20f, 20f);
            rightGrid.Add(_gaugeRhrNet);
            rightPanel.Add(rightGrid);
            row.Add(rightPanel);

            tab.Add(row);
            return tab;
        }

        private VisualElement BuildCondenserTab()
        {
            var tab = NewTab("tab-condenser");
            var row = NewRow(1f);

            var leftPanel = CreatePanel("CONDENSER READINESS");
            leftPanel.style.flexGrow = 1.1f;
            var grid = NewGaugeGrid();
            _gaugeCondenserVac = CreateArcGauge("VACUUM", "inHg", 0f, 30f);
            _gaugeHotwell = CreateArcGauge("HOTWELL", "%", 0f, 100f);
            grid.Add(_gaugeCondenserVac);
            grid.Add(_gaugeHotwell);
            leftPanel.Add(grid);
            row.Add(leftPanel);

            var rightPanel = CreatePanel("HOTWELL INVENTORY");
            rightPanel.style.flexGrow = 1f;
            _hotwellTank = new TankLevelPOC { minValue = 0f, maxValue = 100f, lowAlarm = 25f, highAlarm = 85f };
            _hotwellTank.style.height = 300f;
            _hotwellTank.style.width = 140f;
            rightPanel.Add(_hotwellTank);
            row.Add(rightPanel);

            tab.Add(row);
            return tab;
        }

        private VisualElement BuildTrendsTab()
        {
            var tab = NewTab("tab-trends");
            var row = NewRow(1f);
            var leftPanel = CreatePanel("MULTI-PARAMETER TRENDS");
            leftPanel.style.flexGrow = 2f;
            _trendChart = new StripChartPOC();
            _trendChart.AddToClassList("vd-chart");
            _trendChart.style.flexGrow = 1f;
            _trendChart.style.minHeight = 520f;
            _trendTraceTemp = _trendChart.AddTrace("T AVG", new Color(0f, 0.9f, 0.45f, 1f), 70f, 600f);
            _trendTracePressure = _trendChart.AddTrace("PRESS", new Color(1f, 0.75f, 0f, 1f), 0f, 2600f);
            _trendTraceLevel = _trendChart.AddTrace("PZR", new Color(0.3f, 0.8f, 1f, 1f), 0f, 100f);
            _trendTraceSubcool = _trendChart.AddTrace("SUBCOOL", new Color(1f, 0.2f, 0.2f, 1f), 0f, 120f);
            _trendTraceHzp = _trendChart.AddTrace("HZP", new Color(0.8f, 0.5f, 1f, 1f), 0f, 100f);
            leftPanel.Add(_trendChart);
            row.Add(leftPanel);

            var rightPanel = CreatePanel("LIVE VALUES");
            rightPanel.style.flexGrow = 1f;
            var metrics = NewMetricGrid();
            AddMetric(metrics, "trend_tavg", "T Avg");
            AddMetric(metrics, "trend_pressure", "Pressure");
            AddMetric(metrics, "trend_level", "PZR Level");
            AddMetric(metrics, "trend_subcool", "Subcooling");
            AddMetric(metrics, "trend_hzp", "HZP");
            rightPanel.Add(metrics);
            row.Add(rightPanel);

            tab.Add(row);
            return tab;
        }

        private VisualElement BuildLogTab()
        {
            var tab = NewTab("tab-log");
            var row = NewRow(1f);

            var leftPanel = CreatePanel("OPERATIONS EVENT LOG");
            leftPanel.style.flexGrow = 2f;
            _eventLogScroll = new ScrollView(ScrollViewMode.Vertical);
            _eventLogScroll.AddToClassList("vd-log-scroll");
            _eventLogScroll.style.flexGrow = 1f;
            leftPanel.Add(_eventLogScroll);
            row.Add(leftPanel);

            var rightPanel = CreatePanel("SESSION SNAPSHOT");
            rightPanel.style.flexGrow = 1f;
            var metrics = NewMetricGrid();
            AddMetric(metrics, "log_entries", "Entries");
            AddMetric(metrics, "log_sim_time", "Sim Time");
            AddMetric(metrics, "log_mode", "Plant Mode");
            AddMetric(metrics, "log_speed", "Speed");
            AddMetric(metrics, "log_phase", "Phase");
            rightPanel.Add(metrics);
            row.Add(rightPanel);

            tab.Add(row);
            return tab;
        }
        
        // ====================================================================
        // ELEMENT FACTORY METHODS
        // ====================================================================
        
        private VisualElement NewTab(string name)
        {
            var tab = new VisualElement();
            tab.name = name;
            tab.AddToClassList("vd-tab");
            tab.style.flexGrow = 1f;
            tab.style.flexDirection = FlexDirection.Column;
            tab.style.overflow = Overflow.Hidden;
            return tab;
        }

        private VisualElement NewRow(float grow = 0f)
        {
            var row = new VisualElement();
            row.AddToClassList("vd-row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Stretch;
            row.style.minHeight = 0f;
            if (grow > 0f) row.style.flexGrow = grow;
            return row;
        }

        private VisualElement NewGaugeGrid()
        {
            var grid = new VisualElement();
            grid.AddToClassList("vd-gauge-grid");
            return grid;
        }

        private VisualElement NewMetricGrid()
        {
            var grid = new VisualElement();
            grid.AddToClassList("vd-metric-grid");
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.justifyContent = Justify.SpaceBetween;
            grid.style.alignItems = Align.FlexStart;
            grid.style.marginTop = 6f;
            return grid;
        }

        private VisualElement CreateStatusRow(string key, string name)
        {
            var row = new VisualElement();
            row.AddToClassList("vd-status-row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;

            var label = new Label(name);
            label.AddToClassList("vd-status-title");
            label.style.flexGrow = 1f;
            label.style.fontSize = 11f;
            label.style.color = UITKDashboardTheme.TextPrimary;
            row.Add(label);

            var value = new Label("--");
            value.AddToClassList("vd-status-value");
            value.style.fontSize = 10f;
            value.style.color = UITKDashboardTheme.TextSecondary;
            value.style.unityFontStyleAndWeight = FontStyle.Bold;
            value.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(value);

            _statusValues[key] = value;
            return row;
        }

        private ArcGaugeElement CreateArcGauge(string label, string unit, float min, float max)
        {
            var gauge = new ArcGaugeElement();
            gauge.label = label;
            gauge.unit = unit;
            gauge.minValue = min;
            gauge.maxValue = max;
            gauge.style.width = 120;
            gauge.style.height = 140;
            gauge.AddToClassList("vd-gauge");
            
            _arcGauges.Add(gauge);
            return gauge;
        }

        private AnalogGaugeElement CreateAnalogGauge(
            string title,
            string unit,
            float min,
            float max,
            float majorTick,
            int minorTicks)
        {
            var gauge = new AnalogGaugeElement();
            gauge.Title = title;
            gauge.Unit = unit;
            gauge.MinValue = min;
            gauge.MaxValue = max;
            gauge.MajorTickInterval = majorTick;
            gauge.MinorTicksPerMajor = minorTicks;
            gauge.Value = min;
            gauge.AddToClassList("vd-analog");
            gauge.style.width = 102f;
            gauge.style.height = 102f;
            gauge.style.minWidth = 102f;
            gauge.style.minHeight = 102f;
            gauge.style.maxWidth = 102f;
            gauge.style.maxHeight = 102f;
            gauge.style.flexGrow = 0f;
            gauge.style.flexShrink = 0f;
            return gauge;
        }

        private VisualElement CreateAnalogGaugeTile(
            string caption,
            string unit,
            float min,
            float max,
            float majorTick,
            int minorTicks,
            out AnalogGaugeElement gauge)
        {
            var tile = new VisualElement();
            tile.AddToClassList("vd-analog-tile");
            tile.style.flexDirection = FlexDirection.Column;
            tile.style.alignItems = Align.Center;
            tile.style.justifyContent = Justify.FlexStart;
            tile.style.flexGrow = 1f;
            tile.style.flexBasis = 0f;
            tile.style.minWidth = 0f;
            tile.style.marginBottom = 2f;

            gauge = CreateAnalogGauge(string.Empty, unit, min, max, majorTick, minorTicks);
            tile.Add(gauge);

            var captionLabel = new Label(caption);
            captionLabel.AddToClassList("vd-analog-caption");
            captionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            captionLabel.style.fontSize = 11f;
            captionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            captionLabel.style.color = new Color(0.9f, 0.93f, 0.98f, 1f);
            captionLabel.style.marginTop = 3f;
            tile.Add(captionLabel);
            return tile;
        }

        private VisualElement CreateAnalogGaugeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("vd-analog-row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.FlexStart;
            return row;
        }

        private VisualElement CreateCriticalGaugeCard(
            string key,
            string title,
            string unit,
            float min,
            float max,
            float majorTick,
            int minorTicks,
            out AnalogGaugeElement gauge)
        {
            var card = new VisualElement();
            card.style.width = new Length(32.2f, LengthUnit.Percent);
            card.style.marginRight = 4f;
            card.style.marginBottom = 6f;
            card.style.paddingTop = 4f;
            card.style.paddingBottom = 4f;
            card.style.paddingLeft = 3f;
            card.style.paddingRight = 3f;
            card.style.alignItems = Align.Center;
            card.style.backgroundColor = new Color(0.045f, 0.059f, 0.094f, 1f);
            card.style.borderTopLeftRadius = 4f;
            card.style.borderTopRightRadius = 4f;
            card.style.borderBottomLeftRadius = 4f;
            card.style.borderBottomRightRadius = 4f;
            card.style.borderTopWidth = 1f;
            card.style.borderBottomWidth = 1f;
            card.style.borderLeftWidth = 1f;
            card.style.borderRightWidth = 1f;
            card.style.borderTopColor = new Color(0.118f, 0.173f, 0.271f, 1f);
            card.style.borderBottomColor = new Color(0.118f, 0.173f, 0.271f, 1f);
            card.style.borderLeftColor = new Color(0.118f, 0.173f, 0.271f, 1f);
            card.style.borderRightColor = new Color(0.118f, 0.173f, 0.271f, 1f);

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 9f;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = new Color(0.73f, 0.8f, 0.9f, 1f);
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            card.Add(titleLabel);

            gauge = CreateAnalogGauge(string.Empty, unit, min, max, majorTick, minorTicks);
            gauge.style.width = 102f;
            gauge.style.height = 102f;
            card.Add(gauge);

            var valueLabel = new Label("--");
            valueLabel.style.fontSize = 12f;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            valueLabel.style.color = UITKDashboardTheme.InfoCyan;
            valueLabel.style.marginTop = 2f;
            card.Add(valueLabel);
            _criticalGaugeReadouts[key] = valueLabel;

            return card;
        }

        private void SetCriticalGauge(AnalogGaugeElement gauge, string key, float value, string text, Color color)
        {
            if (gauge != null)
            {
                gauge.Value = value;
            }

            if (_criticalGaugeReadouts.TryGetValue(key, out var label))
            {
                label.text = text;
                label.style.color = color;
            }
        }

        private VisualElement CreateBarMetric(string key, string title, Color fillColor)
        {
            var row = new VisualElement();
            row.style.marginBottom = 8f;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 9f;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = new Color(0.63f, 0.72f, 0.84f, 1f);
            header.Add(titleLabel);

            var valueLabel = new Label("--");
            valueLabel.style.fontSize = 10f;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.color = UITKDashboardTheme.TextPrimary;
            header.Add(valueLabel);
            _criticalBarValues[key] = valueLabel;

            row.Add(header);

            var track = new VisualElement();
            track.style.height = 12f;
            track.style.marginTop = 2f;
            track.style.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 1f);
            track.style.borderTopLeftRadius = 3f;
            track.style.borderTopRightRadius = 3f;
            track.style.borderBottomLeftRadius = 3f;
            track.style.borderBottomRightRadius = 3f;
            track.style.overflow = Overflow.Hidden;

            var fill = new VisualElement();
            fill.style.width = new Length(0f, LengthUnit.Percent);
            fill.style.height = new Length(100f, LengthUnit.Percent);
            fill.style.backgroundColor = fillColor;
            track.Add(fill);
            _criticalBarFills[key] = fill;

            row.Add(track);
            return row;
        }

        private void SetBarMetric(string key, float value, float min, float max, string text, Color valueColor)
        {
            if (_criticalBarFills.TryGetValue(key, out var fill))
            {
                float pct = Mathf.InverseLerp(min, max, value) * 100f;
                fill.style.width = new Length(Mathf.Clamp(pct, 0f, 100f), LengthUnit.Percent);
            }

            if (_criticalBarValues.TryGetValue(key, out var label))
            {
                label.text = text;
                label.style.color = valueColor;
            }
        }

        private VisualElement CreateLinearGaugeMetric(
            string key,
            string title,
            float min,
            float max,
            float warning,
            float alarm,
            out LinearGaugePOC gauge)
        {
            var row = new VisualElement();
            row.style.marginBottom = 7f;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 9f;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = new Color(0.63f, 0.72f, 0.84f, 1f);
            header.Add(titleLabel);

            var valueLabel = new Label("--");
            valueLabel.style.fontSize = 10f;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.color = UITKDashboardTheme.TextPrimary;
            header.Add(valueLabel);
            _criticalProcessValues[key] = valueLabel;

            row.Add(header);

            gauge = new LinearGaugePOC
            {
                orientation = LinearGaugeOrientation.Horizontal,
                minValue = min,
                maxValue = max,
                warningThreshold = warning,
                alarmThreshold = alarm
            };
            gauge.style.height = 14f;
            gauge.style.marginTop = 2f;
            row.Add(gauge);

            return row;
        }

        private VisualElement CreateTankCard(
            string key,
            string title,
            float min,
            float max,
            float lowAlarm,
            float highAlarm,
            out TankLevelPOC tank)
        {
            var card = new VisualElement();
            card.style.width = new Length(49f, LengthUnit.Percent);
            card.style.alignItems = Align.Center;
            card.style.paddingTop = 4f;
            card.style.paddingBottom = 4f;
            card.style.backgroundColor = new Color(0.045f, 0.059f, 0.094f, 1f);
            card.style.borderTopLeftRadius = 4f;
            card.style.borderTopRightRadius = 4f;
            card.style.borderBottomLeftRadius = 4f;
            card.style.borderBottomRightRadius = 4f;

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 9f;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = new Color(0.63f, 0.72f, 0.84f, 1f);
            card.Add(titleLabel);

            tank = new TankLevelPOC
            {
                minValue = min,
                maxValue = max,
                lowAlarm = lowAlarm,
                highAlarm = highAlarm
            };
            tank.style.width = 62f;
            tank.style.height = 86f;
            tank.style.marginTop = 4f;
            card.Add(tank);

            var valueLabel = new Label("--");
            valueLabel.style.fontSize = 10f;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.color = UITKDashboardTheme.InfoCyan;
            valueLabel.style.marginTop = 3f;
            card.Add(valueLabel);
            _criticalProcessValues[key] = valueLabel;

            return card;
        }

        private VisualElement CreateEdgeMeterCard(
            string key,
            string title,
            string unit,
            float min,
            float max,
            out EdgewiseMeterElement meter)
        {
            var card = new VisualElement();
            card.style.width = new Length(32.2f, LengthUnit.Percent);
            card.style.backgroundColor = new Color(0.045f, 0.059f, 0.094f, 1f);
            card.style.borderTopLeftRadius = 4f;
            card.style.borderTopRightRadius = 4f;
            card.style.borderBottomLeftRadius = 4f;
            card.style.borderBottomRightRadius = 4f;
            card.style.paddingTop = 4f;
            card.style.paddingBottom = 4f;
            card.style.paddingLeft = 4f;
            card.style.paddingRight = 4f;

            meter = new EdgewiseMeterElement
            {
                Orientation = MeterOrientation.Horizontal,
                MinValue = min,
                MaxValue = max,
                CenterZero = min < 0f && max > 0f,
                Unit = unit,
                Title = title,
                MajorTickInterval = Mathf.Max(5f, (max - min) / 4f),
                MinorTicksPerMajor = 1
            };
            meter.style.width = new Length(100f, LengthUnit.Percent);
            meter.style.height = 56f;
            card.Add(meter);

            var valueLabel = new Label("--");
            valueLabel.style.fontSize = 10f;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.color = UITKDashboardTheme.InfoCyan;
            valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            valueLabel.style.marginTop = 2f;
            card.Add(valueLabel);
            _criticalProcessValues[key] = valueLabel;

            return card;
        }

        private void SetCriticalProcessValue(string key, string text, Color color)
        {
            if (_criticalProcessValues.TryGetValue(key, out var label))
            {
                label.text = text;
                label.style.color = color;
            }
        }

        private VisualElement CreateCriticalAnnunciatorWall()
        {
            _criticalAnnunciators.Clear();
            _criticalAnnFlashTimer = 0f;
            _criticalAnnFlashVisible = true;

            var wall = new VisualElement();
            wall.AddToClassList("vd-ann-wall");
            wall.style.flexGrow = 1f;
            wall.style.minHeight = 292f;
            wall.style.marginBottom = 8f;
            wall.style.paddingTop = 6f;
            wall.style.paddingBottom = 6f;
            wall.style.paddingLeft = 6f;
            wall.style.paddingRight = 6f;
            wall.style.backgroundColor = new Color(0.035f, 0.051f, 0.086f, 1f);
            wall.style.borderTopWidth = 1f;
            wall.style.borderBottomWidth = 1f;
            wall.style.borderLeftWidth = 1f;
            wall.style.borderRightWidth = 1f;
            wall.style.borderTopColor = new Color(0.118f, 0.173f, 0.271f, 1f);
            wall.style.borderBottomColor = new Color(0.118f, 0.173f, 0.271f, 1f);
            wall.style.borderLeftColor = new Color(0.118f, 0.173f, 0.271f, 1f);
            wall.style.borderRightColor = new Color(0.118f, 0.173f, 0.271f, 1f);

            var legend = new VisualElement();
            legend.AddToClassList("vd-ann-legend");
            legend.style.flexDirection = FlexDirection.Row;
            legend.style.justifyContent = Justify.FlexEnd;
            legend.style.marginBottom = 6f;
            legend.Add(CreateAnnunciatorLegendPill("INFO", "vd-ann-legend-pill--info"));
            legend.Add(CreateAnnunciatorLegendPill("WARNING", "vd-ann-legend-pill--warning"));
            legend.Add(CreateAnnunciatorLegendPill("ALARM", "vd-ann-legend-pill--alarm"));
            wall.Add(legend);

            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Column;
            grid.style.flexGrow = 1f;
            wall.Add(grid);

            var rows = new VisualElement[3];
            for (int row = 0; row < rows.Length; row++)
            {
                rows[row] = new VisualElement();
                rows[row].style.flexDirection = FlexDirection.Row;
                rows[row].style.justifyContent = Justify.SpaceBetween;
                rows[row].style.marginBottom = row < rows.Length - 1 ? 2f : 0f;
                grid.Add(rows[row]);
            }

            int index = 0;
            void AddTile(string label, CriticalAnnunciatorTone tone, Func<HeatupSimEngine, bool> condition)
            {
                int rowIndex = Mathf.Clamp(index / 10, 0, rows.Length - 1);
                AddCriticalAnnunciatorTile(rows[rowIndex], label, tone, condition);
                index++;
            }

            AddTile("PZR HTRS\nON", CriticalAnnunciatorTone.Info, e => e.pzrHeatersOn);
            AddTile("HEATUP\nIN PROG", CriticalAnnunciatorTone.Info, e => e.heatupInProgress);
            AddTile("STEAM\nBUBBLE OK", CriticalAnnunciatorTone.Info, e => e.steamBubbleOK);
            AddTile("MODE\nPERMISSIVE", CriticalAnnunciatorTone.Info, e => e.modePermissive);
            AddTile("PRESS\nLOW", CriticalAnnunciatorTone.Alarm, e => e.pressureLow);
            AddTile("PRESS\nHIGH", CriticalAnnunciatorTone.Alarm, e => e.pressureHigh);
            AddTile("SUBCOOL\nLOW", CriticalAnnunciatorTone.Alarm, e => e.subcoolingLow);
            AddTile("RCS FLOW\nLOW", CriticalAnnunciatorTone.Alarm, e => e.rcsFlowLow);
            AddTile("PZR LVL\nLOW", CriticalAnnunciatorTone.Alarm, e => e.pzrLevelLow);
            AddTile("PZR LVL\nHIGH", CriticalAnnunciatorTone.Warning, e => e.pzrLevelHigh);
            AddTile("RVLIS\nLOW", CriticalAnnunciatorTone.Alarm, e => e.rvlisLevelLow);
            AddTile("CCW\nRUNNING", CriticalAnnunciatorTone.Info, e => e.ccwRunning);
            AddTile("CHARGING\nACTIVE", CriticalAnnunciatorTone.Info, e => e.chargingActive);
            AddTile("LETDOWN\nACTIVE", CriticalAnnunciatorTone.Info, e => e.letdownActive);
            AddTile("SEAL INJ\nOK", CriticalAnnunciatorTone.Info, e => e.sealInjectionOK);
            AddTile("VCT LVL\nLOW", CriticalAnnunciatorTone.Alarm, e => e.vctLevelLow);
            AddTile("VCT LVL\nHIGH", CriticalAnnunciatorTone.Warning, e => e.vctLevelHigh);
            AddTile("VCT\nDIVERT", CriticalAnnunciatorTone.Info, e => e.vctDivertActive);
            AddTile("VCT\nMAKEUP", CriticalAnnunciatorTone.Info, e => e.vctMakeupActive);
            AddTile("RWST\nSUCTION", CriticalAnnunciatorTone.Alarm, e => e.vctRWSTSuction);
            AddTile("RCP #1\nRUN", CriticalAnnunciatorTone.Info, e => IsRcpRunning(e, 0));
            AddTile("RCP #2\nRUN", CriticalAnnunciatorTone.Info, e => IsRcpRunning(e, 1));
            AddTile("RCP #3\nRUN", CriticalAnnunciatorTone.Info, e => IsRcpRunning(e, 2));
            AddTile("RCP #4\nRUN", CriticalAnnunciatorTone.Info, e => IsRcpRunning(e, 3));
            AddTile("SMM LOW\nMARGIN", CriticalAnnunciatorTone.Warning, e => e.smmLowMargin);
            AddTile("SMM NO\nMARGIN", CriticalAnnunciatorTone.Alarm, e => e.smmNoMargin);
            AddTile("SG PRESS\nHIGH", CriticalAnnunciatorTone.Alarm, e => e.sgSecondaryPressureHigh);

            while (index < 30)
            {
                AddTile("SPARE", CriticalAnnunciatorTone.Info, _ => false);
            }

            return wall;
        }

        private VisualElement CreateAnnunciatorLegendPill(string text, string colorClass)
        {
            var pill = new Label(text);
            pill.AddToClassList("vd-ann-legend-pill");
            pill.AddToClassList(colorClass);
            pill.style.fontSize = 9f;
            pill.style.unityFontStyleAndWeight = FontStyle.Bold;
            pill.style.unityTextAlign = TextAnchor.MiddleCenter;
            pill.style.marginLeft = 4f;
            pill.style.paddingLeft = 6f;
            pill.style.paddingRight = 6f;
            pill.style.paddingTop = 2f;
            pill.style.paddingBottom = 2f;
            pill.style.borderTopWidth = 1f;
            pill.style.borderBottomWidth = 1f;
            pill.style.borderLeftWidth = 1f;
            pill.style.borderRightWidth = 1f;
            if (colorClass == "vd-ann-legend-pill--info")
            {
                pill.style.color = new Color(0.569f, 1f, 0.682f, 1f);
                pill.style.backgroundColor = new Color(0.051f, 0.18f, 0.09f, 1f);
                pill.style.borderTopColor = new Color(0.259f, 0.604f, 0.345f, 1f);
                pill.style.borderBottomColor = new Color(0.259f, 0.604f, 0.345f, 1f);
                pill.style.borderLeftColor = new Color(0.259f, 0.604f, 0.345f, 1f);
                pill.style.borderRightColor = new Color(0.259f, 0.604f, 0.345f, 1f);
            }
            else if (colorClass == "vd-ann-legend-pill--warning")
            {
                pill.style.color = new Color(1f, 0.835f, 0.439f, 1f);
                pill.style.backgroundColor = new Color(0.22f, 0.133f, 0.016f, 1f);
                pill.style.borderTopColor = new Color(0.769f, 0.529f, 0.075f, 1f);
                pill.style.borderBottomColor = new Color(0.769f, 0.529f, 0.075f, 1f);
                pill.style.borderLeftColor = new Color(0.769f, 0.529f, 0.075f, 1f);
                pill.style.borderRightColor = new Color(0.769f, 0.529f, 0.075f, 1f);
            }
            else
            {
                pill.style.color = new Color(1f, 0.604f, 0.604f, 1f);
                pill.style.backgroundColor = new Color(0.231f, 0.047f, 0.047f, 1f);
                pill.style.borderTopColor = new Color(0.745f, 0.212f, 0.212f, 1f);
                pill.style.borderBottomColor = new Color(0.745f, 0.212f, 0.212f, 1f);
                pill.style.borderLeftColor = new Color(0.745f, 0.212f, 0.212f, 1f);
                pill.style.borderRightColor = new Color(0.745f, 0.212f, 0.212f, 1f);
            }
            return pill;
        }

        private void AddCriticalAnnunciatorTile(
            VisualElement parent,
            string label,
            CriticalAnnunciatorTone tone,
            Func<HeatupSimEngine, bool> condition)
        {
            var tile = new VisualElement();
            tile.AddToClassList("vd-ann-tile");
            tile.style.width = new Length(9.6f, LengthUnit.Percent);
            tile.style.height = 38f;
            tile.style.minHeight = 38f;
            tile.style.marginRight = 0f;
            tile.style.alignItems = Align.Center;
            tile.style.justifyContent = Justify.Center;
            tile.style.backgroundColor = new Color(0.055f, 0.063f, 0.086f, 1f);
            tile.style.borderTopWidth = 1f;
            tile.style.borderBottomWidth = 1f;
            tile.style.borderLeftWidth = 1f;
            tile.style.borderRightWidth = 1f;
            tile.style.borderTopColor = new Color(0.2f, 0.224f, 0.263f, 1f);
            tile.style.borderBottomColor = new Color(0.2f, 0.224f, 0.263f, 1f);
            tile.style.borderLeftColor = new Color(0.2f, 0.224f, 0.263f, 1f);
            tile.style.borderRightColor = new Color(0.2f, 0.224f, 0.263f, 1f);

            var text = new Label(label);
            text.AddToClassList("vd-ann-tile-label");
            text.style.fontSize = 9f;
            text.style.unityFontStyleAndWeight = FontStyle.Bold;
            text.style.unityTextAlign = TextAnchor.MiddleCenter;
            text.style.whiteSpace = WhiteSpace.Normal;
            text.style.color = new Color(0.58f, 0.62f, 0.69f, 1f);
            tile.Add(text);
            parent.Add(tile);

            _criticalAnnunciators.Add(new CriticalAnnunciatorBinding
            {
                Root = tile,
                Text = text,
                Tone = tone,
                Condition = condition,
                IsActive = false
            });
        }

        private static void SetTileBorder(VisualElement root, Color color)
        {
            root.style.borderTopColor = color;
            root.style.borderBottomColor = color;
            root.style.borderLeftColor = color;
            root.style.borderRightColor = color;
        }

        private static void ApplyAnnunciatorVisual(CriticalAnnunciatorBinding tile, bool flashOff)
        {
            if (!tile.IsActive || flashOff)
            {
                tile.Root.style.backgroundColor = new Color(0.055f, 0.063f, 0.086f, 1f);
                SetTileBorder(tile.Root, new Color(0.2f, 0.224f, 0.263f, 1f));
                tile.Text.style.color = new Color(0.58f, 0.62f, 0.69f, 1f);
                return;
            }

            if (tile.Tone == CriticalAnnunciatorTone.Info)
            {
                tile.Root.style.backgroundColor = new Color(0.039f, 0.149f, 0.086f, 1f);
                SetTileBorder(tile.Root, new Color(0.169f, 0.663f, 0.349f, 1f));
                tile.Text.style.color = new Color(0.376f, 0.922f, 0.533f, 1f);
                return;
            }

            if (tile.Tone == CriticalAnnunciatorTone.Warning)
            {
                tile.Root.style.backgroundColor = new Color(0.22f, 0.145f, 0.016f, 1f);
                SetTileBorder(tile.Root, new Color(0.769f, 0.529f, 0.075f, 1f));
                tile.Text.style.color = new Color(1f, 0.765f, 0.329f, 1f);
                return;
            }

            tile.Root.style.backgroundColor = new Color(0.251f, 0.055f, 0.063f, 1f);
            SetTileBorder(tile.Root, new Color(0.82f, 0.29f, 0.29f, 1f));
            tile.Text.style.color = new Color(1f, 0.49f, 0.49f, 1f);
        }

        private static bool IsRcpRunning(HeatupSimEngine source, int index)
        {
            return source != null &&
                   source.rcpRunning != null &&
                   source.rcpRunning.Length > index &&
                   source.rcpRunning[index];
        }

        private void UpdateCriticalAnnunciators()
        {
            if (engine == null || _criticalAnnunciators.Count == 0)
            {
                return;
            }

            int activeCount = 0;
            int alarmCount = 0;

            foreach (var tile in _criticalAnnunciators)
            {
                bool active = tile.Condition != null && tile.Condition(engine);
                tile.IsActive = active;

                bool flashOff = active &&
                                tile.Tone == CriticalAnnunciatorTone.Alarm &&
                                !_criticalAnnFlashVisible;
                ApplyAnnunciatorVisual(tile, flashOff);

                if (!active)
                {
                    continue;
                }

                activeCount++;
                if (tile.Tone == CriticalAnnunciatorTone.Alarm)
                {
                    alarmCount++;
                }
            }

            if (_criticalAnnSummary != null)
            {
                _criticalAnnSummary.text = $"{activeCount} active / {alarmCount} alarm";
                _criticalAnnSummary.style.color = alarmCount > 0
                    ? UITKDashboardTheme.AlarmRed
                    : activeCount > 0
                        ? UITKDashboardTheme.NormalGreen
                        : UITKDashboardTheme.TextSecondary;
            }
        }

        private void UpdateCriticalAnnunciatorFlash(float deltaTime)
        {
            if (_criticalAnnunciators.Count == 0)
            {
                return;
            }

            _criticalAnnFlashTimer += deltaTime;
            if (_criticalAnnFlashTimer < 0.18f)
            {
                return;
            }

            _criticalAnnFlashTimer = 0f;
            _criticalAnnFlashVisible = !_criticalAnnFlashVisible;

            foreach (var tile in _criticalAnnunciators)
            {
                ApplyAnnunciatorVisual(
                    tile,
                    tile.Tone == CriticalAnnunciatorTone.Alarm &&
                    tile.IsActive &&
                    !_criticalAnnFlashVisible);
            }
        }
        
        private LEDIndicatorElement CreateLED(string label)
        {
            var led = new LEDIndicatorElement();
            led.Configure(label, new Color(0.18f, 0.85f, 0.25f, 1f), false);
            led.AddToClassList("vd-led");
            _leds[label] = led;
            
            _ledIndicators.Add(led);
            return led;
        }

        private void AddMetric(VisualElement parent, string key, string title, bool compact = false)
        {
            var metric = new VisualElement();
            metric.AddToClassList("vd-metric");
            metric.style.width = new Length(49f, LengthUnit.Percent);
            metric.style.minHeight = 40f;
            metric.style.backgroundColor = new Color(0.055f, 0.071f, 0.106f, 1f);
            metric.style.borderTopLeftRadius = 4f;
            metric.style.borderTopRightRadius = 4f;
            metric.style.borderBottomLeftRadius = 4f;
            metric.style.borderBottomRightRadius = 4f;
            metric.style.marginBottom = 4f;
            metric.style.paddingTop = 4f;
            metric.style.paddingBottom = 4f;
            metric.style.paddingLeft = 6f;
            metric.style.paddingRight = 6f;
            if (compact)
            {
                metric.style.width = new Length(48f, LengthUnit.Percent);
                metric.style.minHeight = 36f;
                metric.style.paddingTop = 4f;
                metric.style.paddingBottom = 4f;
                metric.style.paddingLeft = 5f;
                metric.style.paddingRight = 5f;
            }

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("vd-metric-title");
            titleLabel.style.fontSize = compact ? 9f : 10f;
            titleLabel.style.color = UITKDashboardTheme.TextSecondary;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            metric.Add(titleLabel);

            var valueLabel = new Label("--");
            valueLabel.AddToClassList("vd-metric-value");
            valueLabel.style.fontSize = compact ? 12f : 13f;
            valueLabel.style.color = UITKDashboardTheme.TextPrimary;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            metric.Add(valueLabel);

            _metrics[key] = valueLabel;
            parent.Add(metric);
        }

        private VisualElement CreatePanel(string title)
        {
            var panel = new VisualElement();
            panel.AddToClassList("vd-panel");
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
            panel.style.marginRight = 8f;
            panel.style.marginBottom = 8f;
            panel.style.minWidth = 0f;
            
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("vd-panel-title");
            titleLabel.style.fontSize = 11f;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = UITKDashboardTheme.TextSecondary;
            titleLabel.style.marginBottom = 6f;
            panel.Add(titleLabel);
            return panel;
        }

        private void DisableLegacyDashboards()
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

        private void SetMetric(string key, string value, Color? color = null)
        {
            if (!_metrics.TryGetValue(key, out var label))
            {
                return;
            }

            label.text = value;
            label.style.color = color ?? UITKDashboardTheme.TextPrimary;
        }

        private void SetStatus(string key, string value, Color? color = null)
        {
            if (!_statusValues.TryGetValue(key, out var label))
            {
                return;
            }

            label.text = value;
            label.style.color = color ?? UITKDashboardTheme.TextPrimary;
        }
        
        // ====================================================================
        // DATA REFRESH
        // ====================================================================
        
        private void RefreshData()
        {
            if (engine == null) return;

            int speedIndex = Mathf.Clamp(engine.currentSpeedIndex, 0, TimeAcceleration.SpeedLabelsShort.Length - 1);
            float heaterPct = Mathf.Clamp01(engine.pzrHeaterPower / 1.8f) * 100f;
            float netCvcs = engine.chargingFlow - engine.letdownFlow;
            float hzpProgress = Mathf.Clamp(engine.hzpProgress, 0f, 100f);
            float deltaT = Mathf.Max(0f, engine.T_hot - engine.T_cold);
            float absPressureRate = Mathf.Abs(engine.pressureRate);
            float vctLevel = engine.vctState.Level;
            float massErr = engine.massError_lbm;

            if (_simTimeLabel != null) _simTimeLabel.text = TimeAcceleration.FormatTime(engine.simTime);
            if (_plantModeLabel != null) _plantModeLabel.text = GetPlantModeString(engine.plantMode);
            if (_speedLabel != null) _speedLabel.text = $"SPEED {TimeAcceleration.SpeedLabelsShort[speedIndex]}";

            SetGauge(_gaugeSgHeat, engine.sgHeatTransfer_MW);
            SetGauge(_gaugeRhrNet, engine.rhrNetHeat_MW);
            SetGauge(_gaugeCondenserVac, engine.condenserVacuum_inHg);
            SetGauge(_gaugeHotwell, engine.hotwellLevel_pct);
            SetGauge(_gaugeCharging, engine.chargingFlow);
            SetGauge(_gaugeLetdown, engine.letdownFlow);
            SetGauge(_gaugePzrTemp, engine.T_pzr);
            SetGauge(_gaugePzrSat, engine.T_sat);
            SetGauge(_gaugePzrLevelDetail, engine.pzrLevel);
            SetGauge(_gaugePzrHeater, heaterPct);
            SetGauge(_gaugeSprayFlow, engine.sprayFlow_GPM);
            SetGauge(_gaugeCoreDeltaT, deltaT);
            SetGauge(_gaugeRcsHot, engine.T_hot);
            SetGauge(_gaugeRcsCold, engine.T_cold);
            SetGauge(_gaugeRcsAvg, engine.T_avg);
            SetGauge(_gaugeRcsPressure, engine.pressure);
            SetGauge(_gaugeSgPressureDetail, engine.sgSecondaryPressure_psia);

            bool rcp1 = engine.rcpRunning != null && engine.rcpRunning.Length > 0 && engine.rcpRunning[0];
            bool rcp2 = engine.rcpRunning != null && engine.rcpRunning.Length > 1 && engine.rcpRunning[1];
            bool rcp3 = engine.rcpRunning != null && engine.rcpRunning.Length > 2 && engine.rcpRunning[2];
            bool rcp4 = engine.rcpRunning != null && engine.rcpRunning.Length > 3 && engine.rcpRunning[3];
            if (_ledRcpA != null) _ledRcpA.isOn = rcp1;
            if (_ledRcpB != null) _ledRcpB.isOn = rcp2;
            if (_ledRcpC != null) _ledRcpC.isOn = rcp3;
            if (_ledRcpD != null) _ledRcpD.isOn = rcp4;

            Color pressRateColor = absPressureRate < 100f ? UITKDashboardTheme.NormalGreen :
                absPressureRate < 200f ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.AlarmRed;
            Color vctColor = (vctLevel < 20f || vctLevel > 80f) ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.NormalGreen;
            Color massColor = Mathf.Abs(massErr) < 100f ? UITKDashboardTheme.NormalGreen :
                Mathf.Abs(massErr) < 500f ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.AlarmRed;
            Color netCvcsColor = Mathf.Abs(netCvcs) < 10f ? UITKDashboardTheme.NormalGreen :
                Mathf.Abs(netCvcs) < 20f ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.AlarmRed;

            SetCriticalGauge(_criticalAnalogTAvg, "cg_tavg", engine.T_avg, $"{engine.T_avg:F1} F", UITKDashboardTheme.InfoCyan);
            SetCriticalGauge(_criticalAnalogPressure, "cg_pressure", engine.pressure, $"{engine.pressure:F0} psia",
                engine.pressureHigh || engine.pressureLow ? UITKDashboardTheme.AlarmRed : UITKDashboardTheme.InfoCyan);
            SetCriticalGauge(_criticalAnalogPzrLevel, "cg_pzr_level", engine.pzrLevel, $"{engine.pzrLevel:F1}%", vctColor);
            SetCriticalGauge(_criticalAnalogPzrTemp, "cg_pzr_temp", engine.T_pzr, $"{engine.T_pzr:F1} F", UITKDashboardTheme.InfoCyan);
            SetCriticalGauge(_criticalAnalogSubcool, "cg_subcool", engine.subcooling, $"{engine.subcooling:F1} F",
                engine.subcoolingLow ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.NormalGreen);
            SetCriticalGauge(_criticalAnalogSgPressure, "cg_sg_pressure", engine.sgSecondaryPressure_psia,
                $"{engine.sgSecondaryPressure_psia:F0} psia",
                engine.sgSecondaryPressureHigh ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.InfoCyan);
            SetCriticalGauge(_criticalAnalogHeatup, "cg_heatup", Mathf.Abs(engine.heatupRate),
                $"{engine.heatupRate:F1} F/hr", UITKDashboardTheme.InfoCyan);
            SetCriticalGauge(_criticalAnalogPressRate, "cg_press_rate", absPressureRate,
                $"{engine.pressureRate:F1} psi/hr", pressRateColor);
            SetCriticalGauge(_criticalAnalogCondenserVac, "cg_vac", engine.condenserVacuum_inHg,
                $"{engine.condenserVacuum_inHg:F1} inHg", UITKDashboardTheme.InfoCyan);
            if (_criticalEdgePressRate != null) _criticalEdgePressRate.Value = engine.pressureRate;
            if (_criticalEdgeHeatup != null) _criticalEdgeHeatup.Value = engine.heatupRate;
            if (_criticalEdgeSubcool != null) _criticalEdgeSubcool.Value = engine.subcooling;

            if (_criticalVctTank != null) _criticalVctTank.value = engine.vctState.Level;
            if (_criticalHotwellTank != null) _criticalHotwellTank.value = engine.hotwellLevel_pct;
            if (_criticalCvcsNetGauge != null) _criticalCvcsNetGauge.value = netCvcs;

            if (_criticalLinearSgHeat != null) _criticalLinearSgHeat.value = engine.sgHeatTransfer_MW;
            if (_criticalLinearCharging != null) _criticalLinearCharging.value = engine.chargingFlow;
            if (_criticalLinearLetdown != null) _criticalLinearLetdown.value = engine.letdownFlow;
            if (_criticalLinearHzp != null) _criticalLinearHzp.value = hzpProgress;
            if (_criticalLinearMass != null) _criticalLinearMass.value = Mathf.Abs(massErr);

            SetCriticalProcessValue("edge_press_rate", $"{engine.pressureRate:F1}", pressRateColor);
            SetCriticalProcessValue("edge_heatup", $"{engine.heatupRate:F1}", UITKDashboardTheme.InfoCyan);
            SetCriticalProcessValue("edge_subcool", $"{engine.subcooling:F1}", engine.subcoolingLow ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.NormalGreen);
            SetCriticalProcessValue("tank_vct", $"{engine.vctState.Level:F1}%", vctColor);
            SetCriticalProcessValue("tank_hotwell", $"{engine.hotwellLevel_pct:F1}%", UITKDashboardTheme.InfoCyan);
            SetCriticalProcessValue("lin_sg_heat", $"{engine.sgHeatTransfer_MW:F2} MW",
                engine.sgHeatTransfer_MW > 0.1f ? UITKDashboardTheme.NormalGreen : UITKDashboardTheme.TextSecondary);
            SetCriticalProcessValue("lin_charging", $"{engine.chargingFlow:F1} gpm", UITKDashboardTheme.NormalGreen);
            SetCriticalProcessValue("lin_letdown", $"{engine.letdownFlow:F1} gpm", UITKDashboardTheme.WarningAmber);
            SetCriticalProcessValue("lin_hzp", $"{hzpProgress:F1}%", UITKDashboardTheme.InfoCyan);
            SetCriticalProcessValue("lin_mass", $"{massErr:F1} lbm", massColor);

            SetMetric("critical_mode", GetPlantModeString(engine.plantMode), UITKDashboardTheme.InfoCyan);
            SetMetric("critical_phase", engine.heatupPhaseDesc, UITKDashboardTheme.InfoCyan);
            SetMetric("critical_tavg", $"{engine.T_avg:F1} F");
            SetMetric("critical_pressure", $"{engine.pressure:F0} psia");
            SetMetric("critical_pzr_temp", $"{engine.T_pzr:F1} F");
            SetMetric("critical_subcool", $"{engine.subcooling:F1} F");
            SetMetric("critical_press_rate", $"{engine.pressureRate:F1} psi/hr", pressRateColor);
            SetMetric("critical_vct", $"{vctLevel:F1}%", vctColor);
            SetMetric("critical_netcvcs", $"{netCvcs:+0.0;-0.0;0.0} gpm", netCvcsColor);
            SetMetric("critical_sgheat", $"{engine.sgHeatTransfer_MW:F2} MW");
            SetMetric("critical_rhrheat", $"{engine.rhrNetHeat_MW:F2} MW");
            SetMetric("critical_bubble", engine.solidPressurizer ? "SOLID" : "BUBBLE",
                engine.solidPressurizer ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.NormalGreen);
            SetMetric("critical_mass", $"{massErr:F1} lbm", massColor);
            SetMetric("critical_dump", engine.steamDumpPermitted ? "PERMITTED" : "BLOCKED",
                engine.steamDumpPermitted ? UITKDashboardTheme.NormalGreen : UITKDashboardTheme.AlarmRed);
            SetMetric("lim_press_rate", $"{engine.pressureRate:F1} psi/hr", pressRateColor);
            SetMetric("lim_delta_t", $"{deltaT:F1} F", UITKDashboardTheme.InfoCyan);
            SetMetric("lim_sg_heat", $"{engine.sgHeatTransfer_MW:F2} MW",
                engine.sgHeatTransfer_MW > 0.1f ? UITKDashboardTheme.NormalGreen : UITKDashboardTheme.TextSecondary);
            SetMetric("lim_rhr_net", $"{engine.rhrNetHeat_MW:F2} MW",
                Mathf.Abs(engine.rhrNetHeat_MW) > 0.05f ? UITKDashboardTheme.InfoCyan : UITKDashboardTheme.TextSecondary);
            SetMetric("lim_vac", $"{engine.condenserVacuum_inHg:F1} inHg");
            SetMetric("lim_hotwell", $"{engine.hotwellLevel_pct:F1}%");

            SetMetric("rcs_mode", GetPlantModeString(engine.plantMode));
            SetMetric("rcs_subcool", $"{engine.subcooling:F1} F");
            SetMetric("rcs_rate", $"{engine.heatupRate:F1} F/hr");
            SetMetric("rcs_rhr", $"{engine.rhrNetHeat_MW:F2} MW");

            SetMetric("pzr_phase", engine.bubblePhase.ToString());
            SetMetric("pzr_state", engine.solidPressurizer ? "SOLID" : "TWO-PHASE");
            SetMetric("pzr_steam", $"{engine.pzrSteamVolume:F1} ft3");
            SetMetric("pzr_surge", $"{engine.surgeFlow:F1} gpm");

            SetMetric("cvcs_net", $"{netCvcs:F1} gpm");
            SetMetric("cvcs_vct", $"{engine.vctState.Level:F1}%");
            SetMetric("cvcs_brs", engine.brsState.ProcessingActive ? "PROCESSING" : "IDLE");
            SetMetric("cvcs_seal", engine.sealInjectionOK ? "OK" : "LOW");

            SetMetric("trend_tavg", $"{engine.T_avg:F1} F");
            SetMetric("trend_pressure", $"{engine.pressure:F1} psia");
            SetMetric("trend_level", $"{engine.pzrLevel:F1}%");
            SetMetric("trend_subcool", $"{engine.subcooling:F1} F");
            SetMetric("trend_hzp", $"{hzpProgress:F1}%");

            SetMetric("log_entries", $"{engine.eventLog.Count}");
            SetMetric("log_sim_time", TimeAcceleration.FormatTime(engine.simTime));
            SetMetric("log_mode", GetPlantModeString(engine.plantMode));
            SetMetric("log_speed", TimeAcceleration.SpeedLabelsShort[speedIndex]);
            SetMetric("log_phase", engine.heatupPhaseDesc);

            SetStatus("perm_pzr_heaters", engine.pzrHeatersOn ? "ON" : "OFF",
                engine.pzrHeatersOn ? UITKDashboardTheme.NormalGreen : UITKDashboardTheme.TextSecondary);
            SetStatus("perm_spray", engine.sprayActive ? "ACTIVE" : "OFF",
                engine.sprayActive ? UITKDashboardTheme.InfoCyan : UITKDashboardTheme.TextSecondary);
            SetStatus("perm_steam_dump", engine.steamDumpPermitted ? "PERMITTED" : "BLOCKED",
                engine.steamDumpPermitted ? UITKDashboardTheme.NormalGreen : UITKDashboardTheme.AlarmRed);
            SetStatus("perm_mode", engine.modePermissive ? "READY" : "BLOCKED",
                engine.modePermissive ? UITKDashboardTheme.NormalGreen : UITKDashboardTheme.AlarmRed);
            SetStatus("perm_rhr", engine.rhrActive ? "IN SERVICE" : "STANDBY",
                engine.rhrActive ? UITKDashboardTheme.InfoCyan : UITKDashboardTheme.TextSecondary);
            SetStatus("perm_hzp", engine.hzpStable ? "STABLE" : "NOT STABLE",
                engine.hzpStable ? UITKDashboardTheme.NormalGreen : UITKDashboardTheme.WarningAmber);
            SetStatus("perm_makeup", engine.vctMakeupActive ? "ACTIVE" : "IDLE",
                engine.vctMakeupActive ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.TextSecondary);
            SetStatus("perm_divert", engine.vctDivertActive ? "ACTIVE" : "IDLE",
                engine.vctDivertActive ? UITKDashboardTheme.WarningAmber : UITKDashboardTheme.TextSecondary);
            UpdateCriticalAnnunciators();

            if (_criticalPzrGraphic != null) UpdatePressurizerGraphic(_criticalPzrGraphic, heaterPct);
            if (_pressurizerGraphic != null) UpdatePressurizerGraphic(_pressurizerGraphic, heaterPct);

            if (_rcsGraphic != null)
            {
                _rcsGraphic.T_hot = engine.T_hot;
                _rcsGraphic.T_cold = engine.T_cold;
                _rcsGraphic.T_avg = engine.T_avg;
                _rcsGraphic.pressure = engine.pressure;
                _rcsGraphic.rcpCount = engine.rcpCount;
                _rcsGraphic.rhrActive = engine.rhrActive;
                _rcsGraphic.SetRCPRunning(0, rcp1);
                _rcsGraphic.SetRCPRunning(1, rcp2);
                _rcsGraphic.SetRCPRunning(2, rcp3);
                _rcsGraphic.SetRCPRunning(3, rcp4);
            }

            if (_cvcsNetGauge != null) _cvcsNetGauge.value = netCvcs;
            if (_pzrSurgeGauge != null) _pzrSurgeGauge.value = engine.surgeFlow;
            if (_vctTank != null) _vctTank.value = engine.vctState.Level;
            if (_hotwellTank != null) _hotwellTank.value = engine.hotwellLevel_pct;

            if (_criticalChart != null)
            {
                _criticalChart.AddValue(_criticalTraceTemp, engine.T_avg);
                _criticalChart.AddValue(_criticalTracePressure, engine.pressure);
                _criticalChart.AddValue(_criticalTraceLevel, engine.pzrLevel);
            }

            if (_trendChart != null)
            {
                _trendChart.AddValue(_trendTraceTemp, engine.T_avg);
                _trendChart.AddValue(_trendTracePressure, engine.pressure);
                _trendChart.AddValue(_trendTraceLevel, engine.pzrLevel);
                _trendChart.AddValue(_trendTraceSubcool, engine.subcooling);
                _trendChart.AddValue(_trendTraceHzp, hzpProgress);
            }

            RefreshAlarms();
            RefreshEventLog();
            OnDataRefresh?.Invoke();
        }

        private void SetGauge(ArcGaugeElement gauge, float value)
        {
            if (gauge != null) gauge.value = value;
        }

        private void UpdatePressurizerGraphic(PressurizerVesselPOC graphic, float heaterPct)
        {
            graphic.level = engine.pzrLevel;
            graphic.levelSetpoint = engine.pzrLevelSetpointDisplay;
            graphic.pressure = engine.pressure;
            graphic.liquidTemperature = engine.T_pzr;
            graphic.heaterPower = heaterPct;
            graphic.sprayActive = engine.sprayActive;
            graphic.showBubbleZone = !engine.solidPressurizer;
            graphic.chargingFlow = engine.chargingFlow;
            graphic.letdownFlow = engine.letdownFlow;
        }

        private void RefreshAlarms()
        {
            _alarmMessages.Clear();
            if (engine.pressureLow) _alarmMessages.Add("RCS PRESSURE LOW");
            if (engine.pressureHigh) _alarmMessages.Add("RCS PRESSURE HIGH");
            if (engine.pzrLevelLow) _alarmMessages.Add("PZR LEVEL LOW");
            if (engine.pzrLevelHigh) _alarmMessages.Add("PZR LEVEL HIGH");
            if (engine.subcoolingLow) _alarmMessages.Add("SUBCOOLING LOW");
            if (engine.vctLevelLow) _alarmMessages.Add("VCT LEVEL LOW");
            if (engine.vctLevelHigh) _alarmMessages.Add("VCT LEVEL HIGH");
            if (engine.rcsFlowLow) _alarmMessages.Add("RCS FLOW LOW");
            if (Mathf.Abs(engine.massConservationError) > 500f) _alarmMessages.Add("MASS CONSERVATION ALERT");
            if (!engine.modePermissive) _alarmMessages.Add("MODE PERMISSIVE BLOCKED");

            if (_alarmSummaryLabel != null)
            {
                _alarmSummaryLabel.text = _alarmMessages.Count == 0 ? "No active alarms" : $"{_alarmMessages.Count} active alarm(s)";
                _alarmSummaryLabel.style.color = _alarmMessages.Count == 0 ? UITKDashboardTheme.NormalGreen : UITKDashboardTheme.AlarmRed;
            }

            for (int i = 0; i < _alarmRows.Count; i++)
            {
                if (i < _alarmMessages.Count)
                {
                    _alarmRows[i].text = _alarmMessages[i];
                    _alarmRows[i].style.color = UITKDashboardTheme.AlarmRed;
                }
                else
                {
                    _alarmRows[i].text = "--";
                    _alarmRows[i].style.color = UITKDashboardTheme.TextSecondary;
                }
            }
        }

        private void RefreshEventLog()
        {
            RefreshEventLogView(_eventLogScroll, 140, ref _lastEventLogCount);
            RefreshEventLogView(_criticalLogScroll, 28, ref _lastCriticalLogCount);
        }

        private void RefreshEventLogView(ScrollView target, int maxEntries, ref int cacheCount)
        {
            if (target == null || engine == null)
            {
                return;
            }

            if (cacheCount == engine.eventLog.Count)
            {
                return;
            }

            cacheCount = engine.eventLog.Count;
            target.Clear();
            int start = Mathf.Max(0, engine.eventLog.Count - maxEntries);
            for (int i = start; i < engine.eventLog.Count; i++)
            {
                var line = new Label(engine.eventLog[i].FormattedLine);
                line.AddToClassList("vd-log-line");
                target.Add(line);
            }

            target.schedule.Execute(() =>
            {
                if (target.verticalScroller != null)
                    target.verticalScroller.value = target.verticalScroller.highValue;
            }).StartingIn(5);
        }
        
        private void UpdateAnimations()
        {
            foreach (var gauge in _arcGauges)
            {
                gauge.UpdateAnimation();
            }
            
            foreach (var led in _ledIndicators)
            {
                led.UpdateFlash();
            }

            _criticalPzrGraphic?.UpdateFlowAnimation(Time.unscaledDeltaTime);
            _pressurizerGraphic?.UpdateFlowAnimation(Time.unscaledDeltaTime);
            _rcsGraphic?.UpdateAnimation(Time.unscaledDeltaTime);
            UpdateCriticalAnnunciatorFlash(Time.unscaledDeltaTime);
        }
        
        // ====================================================================
        // TAB NAVIGATION
        // ====================================================================
        
        /// <summary>
        /// Switch to a specific tab.
        /// </summary>
        public void SwitchToTab(int index)
        {
            if (index < 0 || index >= TAB_NAMES.Length) return;
            if (index == CurrentTabIndex && _initialized) return;
            
            CurrentTabIndex = index;
            
            // Update tab button states
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                ApplyTabButtonState(_tabButtons[i], i == index);
            }
            
            // Show/hide tab content
            for (int i = 0; i < _tabContents.Count; i++)
            {
                _tabContents[i].style.display = (i == index) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            OnTabChanged?.Invoke(index);
            
            if (enableDebugLogging)
                Debug.Log($"[UITKDashboard] Switched to tab: {TAB_NAMES[index]}");
        }
        
        // ====================================================================
        // VISIBILITY
        // ====================================================================
        
        /// <summary>
        /// Toggle dashboard visibility.
        /// </summary>
        public void ToggleVisibility()
        {
            SetVisibility(!IsVisible);
        }
        
        /// <summary>
        /// Set dashboard visibility.
        /// </summary>
        public void SetVisibility(bool visible)
        {
            if (_root == null) return;
            
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            OnVisibilityChanged?.Invoke(visible);
            
            if (enableDebugLogging)
                Debug.Log($"[UITKDashboard] Visibility: {visible}");
        }
        
        // ====================================================================
        // INPUT HANDLING
        // ====================================================================
        
        private void HandleInput()
        {
            var kb = Keyboard.current;
            if (kb == null || !IsVisible) return;
            
            // Ctrl+1-8 for tab switching
            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
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
            
            // Time acceleration (F5-F9)
            if (kb.f5Key.wasPressedThisFrame) engine?.SetTimeAcceleration(0);
            if (kb.f6Key.wasPressedThisFrame) engine?.SetTimeAcceleration(1);
            if (kb.f7Key.wasPressedThisFrame) engine?.SetTimeAcceleration(2);
            if (kb.f8Key.wasPressedThisFrame) engine?.SetTimeAcceleration(3);
            if (kb.f9Key.wasPressedThisFrame) engine?.SetTimeAcceleration(4);
            
            // +/- for time acceleration
            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame)
            {
                int newIndex = Mathf.Min(engine.currentSpeedIndex + 1, 4);
                engine?.SetTimeAcceleration(newIndex);
            }
            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
            {
                int newIndex = Mathf.Max(engine.currentSpeedIndex - 1, 0);
                engine?.SetTimeAcceleration(newIndex);
            }
        }
        
        // ====================================================================
        // PUBLIC API
        // ====================================================================
        
        /// <summary>
        /// Get tab name by index.
        /// </summary>
        public string GetTabName(int index)
        {
            if (index >= 0 && index < TAB_NAMES.Length)
                return TAB_NAMES[index];
            return "UNKNOWN";
        }
        
        /// <summary>
        /// Total number of tabs.
        /// </summary>
        public int TabCount => TAB_NAMES.Length;
        
        /// <summary>
        /// Get plant mode display string from mode index.
        /// </summary>
        private string GetPlantModeString(int modeIndex)
        {
            return modeIndex switch
            {
                0 => "COLD SHUTDOWN",
                1 => "SOLID HEATUP",
                2 => "BUBBLE FORMATION",
                3 => "PRESSURIZATION",
                4 => "RCP STARTUP",
                5 => "BULK HEATUP",
                6 => "APPROACH HZP",
                7 => "HZP STABLE",
                _ => "UNKNOWN"
            };
        }
    }
}
