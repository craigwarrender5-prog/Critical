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
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = false;
        
        // ====================================================================
        // SINGLETON
        // ====================================================================
        
        public static UITKDashboardController Instance { get; private set; }
        
        // ====================================================================
        // PUBLIC STATE
        // ====================================================================
        
        /// <summary>Is the dashboard visible?</summary>
        public bool IsVisible => uiDocument != null && uiDocument.rootVisualElement.style.display == DisplayStyle.Flex;
        
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
        
        private List<Button> _tabButtons = new List<Button>();
        private List<VisualElement> _tabContents = new List<VisualElement>();
        
        // Gauge elements for animation updates
        private List<ArcGaugeElement> _arcGauges = new List<ArcGaugeElement>();
        private List<LEDIndicatorElement> _ledIndicators = new List<LEDIndicatorElement>();
        
        private float _refreshTimer;
        private float _refreshInterval;
        private bool _initialized = false;
        
        // Critical tab gauges (quick reference for data binding)
        private ArcGaugeElement _gaugeTAvg;
        private ArcGaugeElement _gaugePressure;
        private ArcGaugeElement _gaugePzrLevel;
        private ArcGaugeElement _gaugeSubcooling;
        private ArcGaugeElement _gaugeHeatupRate;
        private ArcGaugeElement _gaugeSgPressure;
        
        // Critical tab LEDs
        private LEDIndicatorElement _ledRcpA;
        private LEDIndicatorElement _ledRcpB;
        private LEDIndicatorElement _ledRcpC;
        private LEDIndicatorElement _ledRcpD;
        
        // Critical tab readouts
        private DigitalReadoutElement _readoutBubble;
        private DigitalReadoutElement _readoutHzpProgress;
        
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
            
            _refreshInterval = 1f / dataRefreshRate;
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
            Debug.Log("[UITKDashboard] Initialize() called");
            
            // Find engine - don't block on this, we'll keep trying in Update
            if (engine == null)
            {
                engine = FindObjectOfType<HeatupSimEngine>();
                if (engine == null)
                {
                    Debug.LogWarning("[UITKDashboard] No HeatupSimEngine found yet - will retry");
                }
                else
                {
                    Debug.Log("[UITKDashboard] Found HeatupSimEngine");
                }
            }
            
            // Disable the old OnGUI dashboard if present
            var oldDashboard = FindObjectOfType<HeatupValidationVisual>();
            if (oldDashboard != null)
            {
                oldDashboard.dashboardVisible = false;
                Debug.Log("[UITKDashboard] Disabled legacy OnGUI dashboard");
            }
            
            // Find UIDocument
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
                if (uiDocument == null)
                {
                    Debug.LogError("[UITKDashboard] No UIDocument component on this GameObject!");
                    return;
                }
            }
            Debug.Log($"[UITKDashboard] UIDocument found. PanelSettings: {(uiDocument.panelSettings != null ? uiDocument.panelSettings.name : "NULL")}, SourceAsset: {(uiDocument.visualTreeAsset != null ? uiDocument.visualTreeAsset.name : "NULL")}");
            
            _root = uiDocument.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("[UITKDashboard] UIDocument.rootVisualElement is NULL! Check PanelSettings and SourceAsset assignments.");
                return;
            }
            Debug.Log($"[UITKDashboard] Root element found. Child count: {_root.childCount}");
            
            // Get key elements from UXML
            _tabBar = _root.Q<VisualElement>("tab-bar");
            _content = _root.Q<VisualElement>("content");
            _simTimeLabel = _root.Q<Label>("sim-time");
            _plantModeLabel = _root.Q<Label>("plant-mode");
            
            Debug.Log($"[UITKDashboard] UXML elements - tabBar: {(_tabBar != null)}, content: {(_content != null)}, simTime: {(_simTimeLabel != null)}, plantMode: {(_plantModeLabel != null)}");
            
            // If UXML didn't load properly, create elements programmatically
            if (_tabBar == null || _content == null)
            {
                Debug.LogWarning("[UITKDashboard] UXML elements not found - building programmatically");
                BuildFullDashboardProgrammatically();
            }
            else
            {
                // Build UI into existing UXML structure
                BuildTabBar();
                BuildCriticalTab();
            }
            
            // Show first tab
            SwitchToTab(0);
            
            _initialized = true;
            
            Debug.Log("[UITKDashboard] Initialized successfully");
        }
        
        /// <summary>
        /// Build the entire dashboard programmatically when UXML isn't loaded.
        /// </summary>
        private void BuildFullDashboardProgrammatically()
        {
            Debug.Log("[UITKDashboard] Building full dashboard programmatically");
            
            _root.Clear();
            _root.style.flexGrow = 1;
            _root.style.backgroundColor = new Color(0.059f, 0.067f, 0.094f, 1f);
            
            // Header
            var header = new VisualElement();
            header.name = "header";
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.height = 48;
            header.style.paddingLeft = 16;
            header.style.paddingRight = 16;
            header.style.backgroundColor = new Color(0.047f, 0.055f, 0.078f, 1f);
            
            var headerTitle = new Label("VALIDATION DASHBOARD");
            headerTitle.style.fontSize = 18;
            headerTitle.style.color = new Color(0.92f, 0.93f, 0.95f, 1f);
            headerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(headerTitle);
            
            var headerRight = new VisualElement();
            headerRight.style.flexDirection = FlexDirection.Row;
            headerRight.style.alignItems = Align.Center;
            
            _simTimeLabel = new Label("00:00:00");
            _simTimeLabel.name = "sim-time";
            _simTimeLabel.style.fontSize = 14;
            _simTimeLabel.style.color = new Color(0.0f, 0.85f, 0.95f, 1f);
            _simTimeLabel.style.marginRight = 16;
            headerRight.Add(_simTimeLabel);
            
            _plantModeLabel = new Label("COLD SHUTDOWN");
            _plantModeLabel.name = "plant-mode";
            _plantModeLabel.style.fontSize = 14;
            _plantModeLabel.style.color = new Color(0.18f, 0.85f, 0.25f, 1f);
            headerRight.Add(_plantModeLabel);
            
            header.Add(headerRight);
            _root.Add(header);
            
            // Tab bar
            _tabBar = new VisualElement();
            _tabBar.name = "tab-bar";
            _tabBar.style.flexDirection = FlexDirection.Row;
            _tabBar.style.height = 36;
            _tabBar.style.backgroundColor = new Color(0.07f, 0.08f, 0.11f, 1f);
            _root.Add(_tabBar);
            
            // Content area
            _content = new VisualElement();
            _content.name = "content";
            _content.style.flexGrow = 1;
            _content.style.paddingTop = 8;
            _content.style.paddingBottom = 8;
            _content.style.paddingLeft = 8;
            _content.style.paddingRight = 8;
            _root.Add(_content);
            
            // Now build the tabs and content
            BuildTabBar();
            BuildCriticalTab();
        }
        
        private void BuildTabBar()
        {
            if (_tabBar == null) return;
            
            _tabBar.Clear();
            _tabButtons.Clear();
            
            for (int i = 0; i < TAB_NAMES.Length; i++)
            {
                int tabIndex = i; // Capture for closure
                
                var tabButton = new Button(() => SwitchToTab(tabIndex));
                tabButton.AddToClassList("validation-dashboard__tab");
                
                var tabLabel = new Label(TAB_NAMES[i]);
                tabLabel.AddToClassList("validation-dashboard__tab-label");
                tabButton.Add(tabLabel);
                
                _tabBar.Add(tabButton);
                _tabButtons.Add(tabButton);
            }
        }
        
        private void BuildCriticalTab()
        {
            if (_content == null) return;
            
            // Create CRITICAL tab content
            var criticalTab = new VisualElement();
            criticalTab.name = "tab-critical";
            criticalTab.style.flexGrow = 1;
            criticalTab.style.flexDirection = FlexDirection.Column;
            
            // === TOP ROW: 6 Arc Gauges ===
            var gaugeRow = new VisualElement();
            gaugeRow.AddToClassList("gauge-row");
            gaugeRow.style.flexDirection = FlexDirection.Row;
            gaugeRow.style.justifyContent = Justify.SpaceAround;
            gaugeRow.style.marginBottom = 16;
            
            // T_avg gauge
            _gaugeTAvg = CreateArcGauge("T_AVG", "°F", 70f, 557f);
            _gaugeTAvg.SetThresholds(100f, 547f, 80f, 557f); // Warn at edges
            gaugeRow.Add(_gaugeTAvg);
            
            // Pressure gauge
            _gaugePressure = CreateArcGauge("PRESSURE", " psig", 0f, 2485f);
            _gaugePressure.SetHighThresholds(2335f, 2385f); // High is bad
            gaugeRow.Add(_gaugePressure);
            
            // PZR Level gauge
            _gaugePzrLevel = CreateArcGauge("PZR LEVEL", "%", 0f, 100f);
            _gaugePzrLevel.SetThresholds(17f, 92f, 12f, 97f); // Both extremes bad
            gaugeRow.Add(_gaugePzrLevel);
            
            // Subcooling gauge
            _gaugeSubcooling = CreateArcGauge("SUBCOOLING", "°F", 0f, 100f);
            _gaugeSubcooling.SetLowThresholds(25f, 10f); // Low is bad
            gaugeRow.Add(_gaugeSubcooling);
            
            // Heatup Rate gauge
            _gaugeHeatupRate = CreateArcGauge("HEATUP", "°F/hr", 0f, 100f);
            _gaugeHeatupRate.SetHighThresholds(60f, 80f); // High is bad (TS limit)
            gaugeRow.Add(_gaugeHeatupRate);
            
            // SG Pressure gauge
            _gaugeSgPressure = CreateArcGauge("SG PRESS", " psig", 0f, 100f);
            gaugeRow.Add(_gaugeSgPressure);
            
            criticalTab.Add(gaugeRow);
            
            // === MIDDLE ROW: RCP Status + Plant Status ===
            var middleRow = new VisualElement();
            middleRow.style.flexDirection = FlexDirection.Row;
            middleRow.style.justifyContent = Justify.SpaceBetween;
            middleRow.style.marginBottom = 16;
            
            // RCP Status Panel
            var rcpPanel = CreatePanel("REACTOR COOLANT PUMPS");
            var rcpRow = new VisualElement();
            rcpRow.style.flexDirection = FlexDirection.Row;
            rcpRow.style.justifyContent = Justify.SpaceAround;
            rcpRow.style.paddingTop = 8;
            rcpRow.style.paddingBottom = 8;
            
            _ledRcpA = CreateLED("RCP-A");
            _ledRcpB = CreateLED("RCP-B");
            _ledRcpC = CreateLED("RCP-C");
            _ledRcpD = CreateLED("RCP-D");
            
            rcpRow.Add(_ledRcpA);
            rcpRow.Add(_ledRcpB);
            rcpRow.Add(_ledRcpC);
            rcpRow.Add(_ledRcpD);
            
            rcpPanel.Add(rcpRow);
            rcpPanel.style.flexGrow = 1;
            rcpPanel.style.marginRight = 8;
            middleRow.Add(rcpPanel);
            
            // Plant Status Panel
            var statusPanel = CreatePanel("PLANT STATUS");
            var statusContent = new VisualElement();
            statusContent.style.flexDirection = FlexDirection.Row;
            statusContent.style.justifyContent = Justify.SpaceAround;
            statusContent.style.paddingTop = 8;
            statusContent.style.paddingBottom = 8;
            
            _readoutBubble = CreateDigitalReadout("BUBBLE", "");
            _readoutBubble.valueFormat = "F0";
            _readoutBubble.autoTrend = false;
            
            _readoutHzpProgress = CreateDigitalReadout("HZP PROGRESS", "%");
            _readoutHzpProgress.valueFormat = "F1";
            
            statusContent.Add(_readoutBubble);
            statusContent.Add(_readoutHzpProgress);
            
            statusPanel.Add(statusContent);
            statusPanel.style.flexGrow = 1;
            statusPanel.style.marginLeft = 8;
            middleRow.Add(statusPanel);
            
            criticalTab.Add(middleRow);
            
            // === BOTTOM: Placeholder for alarms ===
            var alarmsPanel = CreatePanel("ACTIVE ALARMS");
            alarmsPanel.style.flexGrow = 1;
            alarmsPanel.style.minHeight = 80;
            
            var noAlarmsLabel = new Label("No active alarms");
            noAlarmsLabel.style.color = new Color(0.55f, 0.58f, 0.65f, 1f);
            noAlarmsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            noAlarmsLabel.style.flexGrow = 1;
            alarmsPanel.Add(noAlarmsLabel);
            
            criticalTab.Add(alarmsPanel);
            
            // Add to content and store reference
            _content.Add(criticalTab);
            _tabContents.Add(criticalTab);
            
            // Create placeholder tabs for others
            for (int i = 1; i < TAB_NAMES.Length; i++)
            {
                var placeholder = new VisualElement();
                placeholder.name = $"tab-{TAB_NAMES[i].ToLower().Replace(" / ", "-")}";
                placeholder.style.flexGrow = 1;
                placeholder.style.display = DisplayStyle.None;
                
                var placeholderLabel = new Label($"{TAB_NAMES[i]} Tab - Coming Soon");
                placeholderLabel.style.color = new Color(0.55f, 0.58f, 0.65f, 1f);
                placeholderLabel.style.fontSize = 18;
                placeholderLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                placeholderLabel.style.flexGrow = 1;
                placeholder.Add(placeholderLabel);
                
                _content.Add(placeholder);
                _tabContents.Add(placeholder);
            }
        }
        
        // ====================================================================
        // ELEMENT FACTORY METHODS
        // ====================================================================
        
        private ArcGaugeElement CreateArcGauge(string label, string unit, float min, float max)
        {
            var gauge = new ArcGaugeElement();
            gauge.label = label;
            gauge.unit = unit;
            gauge.minValue = min;
            gauge.maxValue = max;
            gauge.style.width = 120;
            gauge.style.height = 140;
            
            _arcGauges.Add(gauge);
            return gauge;
        }
        
        private LEDIndicatorElement CreateLED(string label)
        {
            var led = new LEDIndicatorElement();
            led.Configure(label, new Color(0.18f, 0.85f, 0.25f, 1f), false);
            
            _ledIndicators.Add(led);
            return led;
        }
        
        private DigitalReadoutElement CreateDigitalReadout(string label, string unit)
        {
            var readout = new DigitalReadoutElement();
            readout.label = label;
            readout.unit = unit;
            return readout;
        }
        
        private VisualElement CreatePanel(string title)
        {
            var panel = new VisualElement();
            panel.AddToClassList("dashboard-panel");
            
            var header = new VisualElement();
            header.AddToClassList("dashboard-panel__header");
            
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("dashboard-panel__title");
            header.Add(titleLabel);
            
            panel.Add(header);
            return panel;
        }
        
        // ====================================================================
        // DATA REFRESH
        // ====================================================================
        
        private void RefreshData()
        {
            if (engine == null) return;
            
            // Update header
            if (_simTimeLabel != null)
            {
                TimeSpan ts = TimeSpan.FromSeconds(engine.simTime);
                _simTimeLabel.text = $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
            }
            
            if (_plantModeLabel != null)
            {
                _plantModeLabel.text = GetPlantModeString(engine.plantMode);
            }
            
            // Update CRITICAL tab gauges
            if (_gaugeTAvg != null) _gaugeTAvg.value = engine.T_avg;
            if (_gaugePressure != null) _gaugePressure.value = engine.pressure;
            if (_gaugePzrLevel != null) _gaugePzrLevel.value = engine.pzrLevel;
            if (_gaugeSubcooling != null) _gaugeSubcooling.value = engine.subcooling;
            if (_gaugeHeatupRate != null) _gaugeHeatupRate.value = Mathf.Abs(engine.heatupRate);
            if (_gaugeSgPressure != null) _gaugeSgPressure.value = engine.sgSecondaryPressure_psia;
            
            // Update RCP LEDs
            if (_ledRcpA != null) _ledRcpA.isOn = engine.rcpCount >= 1;
            if (_ledRcpB != null) _ledRcpB.isOn = engine.rcpCount >= 2;
            if (_ledRcpC != null) _ledRcpC.isOn = engine.rcpCount >= 3;
            if (_ledRcpD != null) _ledRcpD.isOn = engine.rcpCount >= 4;
            
            // Update status readouts
            if (_readoutBubble != null)
            {
                bool hasBubble = !engine.solidPressurizer;
                _readoutBubble.SetValue(hasBubble ? 1f : 0f);
            }
            
            if (_readoutHzpProgress != null)
            {
                _readoutHzpProgress.value = engine.hzpProgress * 100f;
            }
            
            // Fire event
            OnDataRefresh?.Invoke();
        }
        
        private void UpdateAnimations()
        {
            // Update all arc gauge animations
            foreach (var gauge in _arcGauges)
            {
                gauge.UpdateAnimation();
            }
            
            // Update LED flash animations
            foreach (var led in _ledIndicators)
            {
                led.UpdateFlash();
            }
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
                if (i == index)
                    _tabButtons[i].AddToClassList("validation-dashboard__tab--active");
                else
                    _tabButtons[i].RemoveFromClassList("validation-dashboard__tab--active");
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
                int newIndex = (engine.currentSpeedIndex + 1) % 5;
                engine?.SetTimeAcceleration(newIndex);
            }
            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
            {
                int newIndex = (engine.currentSpeedIndex - 1 + 5) % 5;
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
