// ============================================================================
// CRITICAL: Master the Atom - Reactor Operator GUI
// ReactorOperatorScreen.cs - Master Screen Controller
// ============================================================================
//
// PURPOSE:
//   Master controller for the Reactor Operator GUI screen. Manages screen
//   visibility (key '1' toggle), layout zones, component orchestration,
//   gauge data binding, and the overall update loop.
//
// LAYOUT ZONES (1920x1080):
//   - Left Gauge Panel (0-15%): 9 nuclear instrumentation gauges
//   - Core Map Panel (15-65%): 193-assembly interactive core mosaic
//   - Right Gauge Panel (65-80%): 8 thermal-hydraulic gauges
//   - Detail Panel (80-100%): Assembly detail (shown on selection)
//   - Bottom Panel (0-26%): Controls, rod display, alarms
//
// FEATURES:
//   - Keyboard toggle: Press '1' to show/hide screen (New Input System)
//   - Automatic gauge registration and data binding
//   - Coordinated update loop (10 Hz gauges, 2 Hz core map)
//   - Integration with existing MosaicBoard infrastructure
//
// ARCHITECTURE:
//   - Sits atop MosaicBoard (data provider)
//   - Orchestrates CoreMosaicMap, AssemblyDetailPanel, gauges
//   - Does not modify GOLD STANDARD physics modules
//
// SOURCES:
//   - ReactorOperatorGUI_Design_v1_0_0_0.md
//   - Unity_Implementation_Manual_v1_0_0_0.md
//
// GOLD STANDARD: Yes
// CHANGE: v2.0.0 — Migrated from legacy Input.GetKeyDown() to New Input System
//         InputAction (activeInputHandler: 1 requires New Input System API)
// CHANGE: v2.0.2 — Toggle action now only shows (never hides) Screen 1.
//         Pressing key 1 while Screen 1 is already visible is a no-op.
//         ScreenManager coordinates mutual exclusion across all screens.
// CHANGE: v4.1.0 — Status display fields (SimTimeText, TimeCompressionText,
//         ReactorModeText, ScreenTitleText) changed from legacy Text to
//         TextMeshProUGUI for visual consistency with instrument font upgrade.
// ============================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI
{
    using Controllers;

    /// <summary>
    /// Master controller for the Reactor Operator GUI screen.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class ReactorOperatorScreen : MonoBehaviour
    {
        // ====================================================================
        // INSPECTOR FIELDS
        // ====================================================================

        #region Inspector Fields

        [Header("Screen Settings")]
        [Tooltip("Keyboard key to toggle screen visibility")]
        public KeyCode ToggleKey = KeyCode.Alpha1;

        [Tooltip("Start with screen visible")]
        public bool StartVisible = true;

        [Tooltip("Screen ID for multi-screen management")]
        public int ScreenID = 1;

        [Header("Color Theme")]
        public Color BackgroundColor = new Color(0.10f, 0.10f, 0.12f);      // #1A1A1F
        public Color PanelColor = new Color(0.12f, 0.12f, 0.16f);           // #1E1E28
        public Color BorderColor = new Color(0.16f, 0.16f, 0.21f);          // #2A2A35

        [Header("Layout Panels")]
        public RectTransform LeftGaugePanel;
        public RectTransform CoreMapPanel;
        public RectTransform RightGaugePanel;
        public RectTransform DetailPanelArea;
        public RectTransform BottomPanel;

        [Header("Core Components")]
        public MosaicBoard Board;
        public CoreMosaicMap CoreMap;
        public AssemblyDetailPanel DetailPanel;

        [Header("Control Components")]
        public MosaicRodDisplay RodDisplay;
        public MosaicControlPanel ControlPanel;
        public MosaicAlarmPanel AlarmPanel;

        [Header("Left Gauges (Nuclear)")]
        public MosaicGauge NeutronPowerGauge;
        public MosaicGauge ThermalPowerGauge;
        public MosaicGauge StartupRateGauge;
        public MosaicGauge PeriodGauge;
        public MosaicGauge ReactivityGauge;
        public MosaicGauge KeffGauge;
        public MosaicGauge BoronGauge;
        public MosaicGauge XenonGauge;
        public MosaicGauge FlowGauge;

        [Header("Right Gauges (Thermal-Hydraulic)")]
        public MosaicGauge TavgGauge;
        public MosaicGauge ThotGauge;
        public MosaicGauge TcoldGauge;
        public MosaicGauge DeltaTGauge;
        public MosaicGauge FuelCenterlineGauge;
        public MosaicGauge HotChannelGauge;
        public MosaicGauge PressureGauge;
        public MosaicGauge PZRLevelGauge;

        [Header("Display Mode Buttons")]
        public Button PowerModeButton;
        public Button FuelTempModeButton;
        public Button CoolantTempModeButton;
        public Button RodBankModeButton;

        [Header("Bank Filter Buttons")]
        public Button BankAllButton;
        public Button BankSAButton;
        public Button BankSBButton;
        public Button BankSCButton;
        public Button BankSDButton;
        public Button BankDButton;
        public Button BankCButton;
        public Button BankBButton;
        public Button BankAButton;

        [Header("Status Display")]
        public TextMeshProUGUI ScreenTitleText;
        public TextMeshProUGUI SimTimeText;
        public TextMeshProUGUI TimeCompressionText;
        public TextMeshProUGUI ReactorModeText;

        #endregion

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        #region Private Fields

        private bool _isVisible;
        private Image _backgroundImage;
        private CanvasGroup _canvasGroup;
        private ReactorController _reactor;
        private InputAction _toggleAction;

        #endregion

        // ====================================================================
        // PROPERTIES
        // ====================================================================

        #region Properties

        /// <summary>Is the screen currently visible?</summary>
        public bool IsVisible => _isVisible;

        /// <summary>Reference to the ReactorController.</summary>
        public ReactorController Reactor => _reactor;

        #endregion

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        #region Unity Lifecycle

        private void Awake()
        {
            // Get/add components
            _backgroundImage = GetComponent<Image>();
            if (_backgroundImage != null)
            {
                _backgroundImage.color = BackgroundColor;
            }

            // Add CanvasGroup for fade effects (optional)
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            // Find MosaicBoard
            if (Board == null)
            {
                Board = GetComponent<MosaicBoard>();
                if (Board == null)
                {
                    Board = GetComponentInChildren<MosaicBoard>();
                }
            }

            // Get reactor reference
            if (Board != null)
            {
                _reactor = Board.Reactor;
            }

            if (_reactor == null)
            {
                _reactor = FindObjectOfType<ReactorController>();
            }

            // Wire up buttons
            WireDisplayModeButtons();
            WireBankFilterButtons();

            // Set initial visibility
            SetVisible(StartVisible);

            // Update title
            if (ScreenTitleText != null)
            {
                ScreenTitleText.text = "REACTOR OPERATOR";
            }

            // Create New Input System action for toggle key
            // (Legacy Input.GetKeyDown is disabled under activeInputHandler: 1)
            _toggleAction = new InputAction(
                name: "ReactorScreenToggle",
                type: InputActionType.Button,
                binding: "<Keyboard>/1"
            );
            // Only show — never hide via own key press.
            // ScreenManager coordinates mutual exclusion across all screens.
            // If already visible, pressing '1' again is a no-op.
            _toggleAction.performed += _ =>
            {
                if (!_isVisible)
                    SetVisible(true);
            };
            _toggleAction.Enable();

            Debug.Log($"[ReactorOperatorScreen] Initialized. Toggle key: {ToggleKey}");
        }

        private void Update()
        {

            // Update status displays if visible
            if (_isVisible)
            {
                UpdateStatusDisplays();
            }
        }

        private void OnDestroy()
        {
            if (_toggleAction != null)
            {
                _toggleAction.Disable();
                _toggleAction.Dispose();
                _toggleAction = null;
            }
        }

        #endregion

        // ====================================================================
        // VISIBILITY CONTROL
        // ====================================================================

        #region Visibility Control

        /// <summary>
        /// Toggle screen visibility.
        /// </summary>
        public void ToggleVisibility()
        {
            SetVisible(!_isVisible);
        }

        /// <summary>
        /// Set screen visibility.
        /// </summary>
        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            gameObject.SetActive(visible);

            if (visible)
            {
                OnScreenShown();
            }
            else
            {
                OnScreenHidden();
            }

            Debug.Log($"[ReactorOperatorScreen] Visibility: {visible}");
        }

        /// <summary>
        /// Show the screen.
        /// </summary>
        public void Show()
        {
            SetVisible(true);
        }

        /// <summary>
        /// Hide the screen.
        /// </summary>
        public void Hide()
        {
            SetVisible(false);
        }

        private void OnScreenShown()
        {
            // Refresh components when shown
            if (CoreMap != null)
            {
                CoreMap.Refresh();
            }

            // Ensure detail panel is in correct state
            if (DetailPanel != null && CoreMap != null)
            {
                int selected = CoreMap.GetSelectedAssembly();
                if (selected < 0)
                {
                    DetailPanel.Hide();
                }
            }
        }

        private void OnScreenHidden()
        {
            // Optionally clear selection when hiding
            // if (CoreMap != null) CoreMap.ClearSelection();
        }

        #endregion

        // ====================================================================
        // STATUS UPDATES
        // ====================================================================

        #region Status Updates

        private void UpdateStatusDisplays()
        {
            // Update simulation time
            if (SimTimeText != null && _reactor != null)
            {
                float simTime = Time.time; // TODO: Get actual sim time from reactor
                int hours = Mathf.FloorToInt(simTime / 3600f);
                int minutes = Mathf.FloorToInt((simTime % 3600f) / 60f);
                int seconds = Mathf.FloorToInt(simTime % 60f);
                SimTimeText.text = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            }

            // Update time compression
            if (TimeCompressionText != null)
            {
                float timeScale = Time.timeScale;
                if (timeScale <= 0f)
                {
                    TimeCompressionText.text = "PAUSED";
                }
                else if (timeScale >= 1000f)
                {
                    TimeCompressionText.text = $"{timeScale / 1000f:F1}kx";
                }
                else
                {
                    TimeCompressionText.text = $"{timeScale:F0}x";
                }
            }

            // Update reactor mode
            if (ReactorModeText != null && _reactor != null)
            {
                string mode = GetReactorModeString();
                ReactorModeText.text = mode;
            }
        }

        private string GetReactorModeString()
        {
            if (_reactor == null) return "---";

            if (_reactor.IsTripped)
                return "TRIPPED";

            float power = _reactor.NeutronPower;

            if (power > 0.99f)
                return "MODE 1 - POWER OP";
            else if (power > 0.05f)
                return "MODE 1 - STARTUP";
            else if (power > 0.001f)
                return "MODE 2 - STARTUP";
            else
                return "MODE 3 - HOT STBY";
        }

        #endregion

        // ====================================================================
        // BUTTON WIRING
        // ====================================================================

        #region Button Wiring

        private void WireDisplayModeButtons()
        {
            if (CoreMap == null) return;

            if (PowerModeButton != null)
                PowerModeButton.onClick.AddListener(CoreMap.OnPowerModeClick);

            if (FuelTempModeButton != null)
                FuelTempModeButton.onClick.AddListener(CoreMap.OnFuelTempModeClick);

            if (CoolantTempModeButton != null)
                CoolantTempModeButton.onClick.AddListener(CoreMap.OnCoolantTempModeClick);

            if (RodBankModeButton != null)
                RodBankModeButton.onClick.AddListener(CoreMap.OnRodBankModeClick);
        }

        private void WireBankFilterButtons()
        {
            if (CoreMap == null) return;

            if (BankAllButton != null)
                BankAllButton.onClick.AddListener(CoreMap.OnBankFilterAll);

            if (BankSAButton != null)
                BankSAButton.onClick.AddListener(CoreMap.OnBankFilterSA);

            if (BankSBButton != null)
                BankSBButton.onClick.AddListener(CoreMap.OnBankFilterSB);

            if (BankSCButton != null)
                BankSCButton.onClick.AddListener(CoreMap.OnBankFilterSC);

            if (BankSDButton != null)
                BankSDButton.onClick.AddListener(CoreMap.OnBankFilterSD);

            if (BankDButton != null)
                BankDButton.onClick.AddListener(CoreMap.OnBankFilterD);

            if (BankCButton != null)
                BankCButton.onClick.AddListener(CoreMap.OnBankFilterC);

            if (BankBButton != null)
                BankBButton.onClick.AddListener(CoreMap.OnBankFilterB);

            if (BankAButton != null)
                BankAButton.onClick.AddListener(CoreMap.OnBankFilterA);
        }

        #endregion

        // ====================================================================
        // PUBLIC CONTROL METHODS
        // ====================================================================

        #region Public Control Methods

        /// <summary>
        /// Set core map display mode.
        /// </summary>
        public void SetDisplayMode(CoreMapDisplayMode mode)
        {
            if (CoreMap != null)
            {
                CoreMap.SetDisplayMode(mode);
            }
        }

        /// <summary>
        /// Set bank filter.
        /// </summary>
        public void SetBankFilter(int bankIndex)
        {
            if (CoreMap != null)
            {
                CoreMap.SetBankFilter(bankIndex);
            }
        }

        /// <summary>
        /// Clear assembly selection.
        /// </summary>
        public void ClearSelection()
        {
            if (CoreMap != null)
            {
                CoreMap.ClearSelection();
            }
        }

        /// <summary>
        /// Force refresh all components.
        /// </summary>
        public void Refresh()
        {
            if (CoreMap != null)
            {
                CoreMap.Refresh();
            }

            // Force gauge updates through MosaicBoard
            if (Board != null)
            {
                // MosaicBoard handles gauge updates automatically
            }
        }

        #endregion

        // ====================================================================
        // GAUGE CONFIGURATION
        // ====================================================================

        #region Gauge Configuration

        /// <summary>
        /// Configure all gauges with correct types.
        /// Called by OperatorScreenBuilder after creating gauges.
        /// </summary>
        public void ConfigureGauges()
        {
            // Left panel - Nuclear instrumentation
            if (NeutronPowerGauge != null) NeutronPowerGauge.Type = GaugeType.NeutronPower;
            if (ThermalPowerGauge != null) ThermalPowerGauge.Type = GaugeType.ThermalPower;
            if (StartupRateGauge != null) StartupRateGauge.Type = GaugeType.StartupRate;
            if (PeriodGauge != null) PeriodGauge.Type = GaugeType.ReactorPeriod;
            if (ReactivityGauge != null) ReactivityGauge.Type = GaugeType.TotalReactivity;
            // KeffGauge - needs new GaugeType or custom handling
            if (BoronGauge != null) BoronGauge.Type = GaugeType.Boron;
            if (XenonGauge != null) XenonGauge.Type = GaugeType.Xenon;
            if (FlowGauge != null) FlowGauge.Type = GaugeType.FlowFraction;

            // Right panel - Thermal-hydraulic
            if (TavgGauge != null) TavgGauge.Type = GaugeType.Tavg;
            if (ThotGauge != null) ThotGauge.Type = GaugeType.Thot;
            if (TcoldGauge != null) TcoldGauge.Type = GaugeType.Tcold;
            if (DeltaTGauge != null) DeltaTGauge.Type = GaugeType.DeltaT;
            if (FuelCenterlineGauge != null) FuelCenterlineGauge.Type = GaugeType.FuelCenterline;
            // HotChannelGauge - needs new GaugeType
            // PressureGauge - needs new GaugeType
            // PZRLevelGauge - needs new GaugeType

            Debug.Log("[ReactorOperatorScreen] Gauges configured");
        }

        #endregion

        // ====================================================================
        // LAYOUT HELPERS
        // ====================================================================

        #region Layout Helpers

        /// <summary>
        /// Get the layout anchors for a panel zone.
        /// </summary>
        public static (Vector2 min, Vector2 max) GetReactorScreenZoneAnchors(ScreenZone zone)
        {
            return zone switch
            {
                ScreenZone.LeftGauges => (new Vector2(0f, 0.26f), new Vector2(0.15f, 1f)),
                ScreenZone.CenterVisualization => (new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f)),
                ScreenZone.RightGauges => (new Vector2(0.65f, 0.26f), new Vector2(0.80f, 1f)),
                ScreenZone.BottomControls => (new Vector2(0f, 0f), new Vector2(1f, 0.26f)),
                _ => (Vector2.zero, Vector2.one)
            };
        }

        #endregion
    }

    // ========================================================================
    // SUPPORTING TYPES
    // ========================================================================
    // NOTE: ScreenZone enum is defined in OperatorScreen.cs (base class).
    // ReactorOperatorScreen uses the shared ScreenZone enum from there.
}
