// CRITICAL: Master the Atom - Phase 2 Mosaic Control Panel
// MosaicControlPanel.cs - Operator Control Interface
//
// Provides operator controls for:
//   - Rod control (withdraw/insert/stop)
//   - Reactor trip
//   - Time compression
//   - Boron control
//   - Scenario control
//
// Reference: Westinghouse Control Room Operator Interface

using UnityEngine;
using UnityEngine.UI;

namespace Critical.UI
{
    using Controllers;
    
    /// <summary>
    /// Operator control panel for Mosaic Board.
    /// </summary>
    public class MosaicControlPanel : MonoBehaviour, IMosaicComponent
    {
        #region Unity Inspector Fields
        
        [Header("Rod Control")]
        [Tooltip("Rod withdraw button")]
        public Button WithdrawButton;
        
        [Tooltip("Rod insert button")]
        public Button InsertButton;
        
        [Tooltip("Rod stop button")]
        public Button StopButton;
        
        [Tooltip("Rod status text")]
        public Text RodStatusText;
        
        [Header("Trip Control")]
        [Tooltip("Reactor trip button")]
        public Button TripButton;
        
        [Tooltip("Trip reset button")]
        public Button ResetTripButton;
        
        [Tooltip("Trip button cover (optional toggle)")]
        public Toggle TripCoverToggle;
        
        [Tooltip("Trip indicator")]
        public Image TripIndicator;
        
        [Header("Time Control")]
        [Tooltip("Time compression slider")]
        public Slider TimeCompressionSlider;
        
        [Tooltip("Time compression text")]
        public Text TimeCompressionText;
        
        [Tooltip("Simulation time display")]
        public Text SimTimeText;
        
        [Tooltip("Pause button")]
        public Button PauseButton;
        
        [Tooltip("Pause indicator")]
        public Image PauseIndicator;
        
        [Header("Boron Control")]
        [Tooltip("Borate button (add boron)")]
        public Button BorateButton;
        
        [Tooltip("Dilute button (remove boron)")]
        public Button DiluteButton;
        
        [Tooltip("Boron change rate (ppm/click)")]
        public float BoronChangeRate = 10f;
        
        [Tooltip("Boron concentration text")]
        public Text BoronText;
        
        [Header("Scenario Control")]
        [Tooltip("Start scenario button")]
        public Button StartScenarioButton;
        
        [Tooltip("Stop scenario button")]
        public Button StopScenarioButton;
        
        [Tooltip("Scenario dropdown")]
        public Dropdown ScenarioDropdown;
        
        [Tooltip("Scenario status text")]
        public Text ScenarioStatusText;
        
        [Header("Quick Actions")]
        [Tooltip("Initialize to HZP button")]
        public Button InitHZPButton;
        
        [Tooltip("Initialize to 100% button")]
        public Button Init100Button;
        
        [Tooltip("Reset button")]
        public Button ResetButton;
        
        [Header("Colors")]
        public Color ActiveColor = new Color(0.2f, 0.8f, 0.2f);
        public Color InactiveColor = new Color(0.3f, 0.3f, 0.3f);
        public Color TripColor = new Color(1f, 0f, 0f);
        public Color WarningColor = new Color(1f, 0.8f, 0f);
        
        #endregion
        
        #region Private Fields
        
        private MosaicBoard _board;
        private bool _isPaused;
        private float _savedTimeCompression;
        private bool _tripCoverOpen;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            SetupButtons();
            SetupSliders();
            SetupDropdowns();
        }
        
        private void OnEnable()
        {
            if (_board == null && MosaicBoard.Instance != null)
            {
                _board = MosaicBoard.Instance;
                _board.RegisterComponent(this);
            }
        }
        
        private void OnDisable()
        {
            _board?.UnregisterComponent(this);
        }
        
        #endregion
        
        #region IMosaicComponent Implementation
        
        public void Initialize(MosaicBoard board)
        {
            _board = board;
            
            // Populate scenario dropdown
            if (ScenarioDropdown != null && _board.SimEngine != null)
            {
                var scenarios = _board.SimEngine.GetAvailableScenarios();
                ScenarioDropdown.ClearOptions();
                ScenarioDropdown.AddOptions(new System.Collections.Generic.List<string>(scenarios));
            }
        }
        
        public void UpdateData()
        {
            UpdateRodStatus();
            UpdateTripStatus();
            UpdateTimeDisplay();
            UpdateBoronDisplay();
            UpdateScenarioStatus();
            UpdateButtonStates();
        }
        
        #endregion
        
        #region Setup
        
        private void SetupButtons()
        {
            // Rod control
            if (WithdrawButton != null)
                WithdrawButton.onClick.AddListener(OnWithdrawClick);
            
            if (InsertButton != null)
                InsertButton.onClick.AddListener(OnInsertClick);
            
            if (StopButton != null)
                StopButton.onClick.AddListener(OnStopClick);
            
            // Trip control
            if (TripButton != null)
                TripButton.onClick.AddListener(OnTripClick);
            
            if (ResetTripButton != null)
                ResetTripButton.onClick.AddListener(OnResetTripClick);
            
            if (TripCoverToggle != null)
                TripCoverToggle.onValueChanged.AddListener(OnTripCoverToggle);
            
            // Time control
            if (PauseButton != null)
                PauseButton.onClick.AddListener(OnPauseClick);
            
            // Boron control
            if (BorateButton != null)
                BorateButton.onClick.AddListener(OnBorateClick);
            
            if (DiluteButton != null)
                DiluteButton.onClick.AddListener(OnDiluteClick);
            
            // Scenario control
            if (StartScenarioButton != null)
                StartScenarioButton.onClick.AddListener(OnStartScenarioClick);
            
            if (StopScenarioButton != null)
                StopScenarioButton.onClick.AddListener(OnStopScenarioClick);
            
            // Quick actions
            if (InitHZPButton != null)
                InitHZPButton.onClick.AddListener(OnInitHZPClick);
            
            if (Init100Button != null)
                Init100Button.onClick.AddListener(OnInit100Click);
            
            if (ResetButton != null)
                ResetButton.onClick.AddListener(OnResetClick);
        }
        
        private void SetupSliders()
        {
            if (TimeCompressionSlider != null)
            {
                TimeCompressionSlider.minValue = 0f;
                TimeCompressionSlider.maxValue = 1f;
                TimeCompressionSlider.value = 0f; // 1x
                TimeCompressionSlider.onValueChanged.AddListener(OnTimeCompressionChanged);
            }
        }
        
        private void SetupDropdowns()
        {
            if (ScenarioDropdown != null)
            {
                ScenarioDropdown.onValueChanged.AddListener(OnScenarioSelected);
            }
        }
        
        #endregion
        
        #region Display Updates
        
        private void UpdateRodStatus()
        {
            if (RodStatusText == null || _board?.Reactor == null) return;
            
            var reactor = _board.Reactor;
            float bankD = reactor.BankDPosition;
            
            RodStatusText.text = $"Bank D: {bankD:F0} steps";
            
            if (reactor.IsTripped)
            {
                RodStatusText.color = TripColor;
            }
            else if (bankD < 30f && reactor.ThermalPower > 0.25f)
            {
                RodStatusText.color = WarningColor;
            }
            else
            {
                RodStatusText.color = ActiveColor;
            }
        }
        
        private void UpdateTripStatus()
        {
            if (_board?.Reactor == null) return;
            
            bool isTripped = _board.Reactor.IsTripped;
            
            // Update trip button
            if (TripButton != null)
            {
                TripButton.interactable = !isTripped && (_tripCoverOpen || TripCoverToggle == null);
            }
            
            // Update reset button
            if (ResetTripButton != null)
            {
                ResetTripButton.interactable = isTripped;
            }
            
            // Update trip indicator
            if (TripIndicator != null)
            {
                TripIndicator.color = isTripped ? TripColor : InactiveColor;
            }
        }
        
        private void UpdateTimeDisplay()
        {
            if (_board?.Reactor == null) return;
            
            // Time compression
            if (TimeCompressionText != null)
            {
                float tc = _board.Reactor.TimeCompression;
                TimeCompressionText.text = $"{tc:F0}x";
            }
            
            // Simulation time
            if (SimTimeText != null)
            {
                float totalSeconds = _board.Reactor.SimulationTime;
                int hours = Mathf.FloorToInt(totalSeconds / 3600f);
                int minutes = Mathf.FloorToInt((totalSeconds % 3600f) / 60f);
                int seconds = Mathf.FloorToInt(totalSeconds % 60f);
                SimTimeText.text = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            }
            
            // Pause indicator
            if (PauseIndicator != null)
            {
                PauseIndicator.color = _isPaused ? WarningColor : InactiveColor;
            }
        }
        
        private void UpdateBoronDisplay()
        {
            if (BoronText == null || _board?.Reactor == null) return;
            
            BoronText.text = $"{_board.Reactor.Boron_ppm:F0} ppm";
        }
        
        private void UpdateScenarioStatus()
        {
            if (ScenarioStatusText == null || _board?.SimEngine == null) return;
            
            ScenarioStatusText.text = _board.SimEngine.GetStatusText();
        }
        
        private void UpdateButtonStates()
        {
            if (_board?.Reactor == null) return;
            
            bool isTripped = _board.Reactor.IsTripped;
            
            // Rod control buttons
            if (WithdrawButton != null) WithdrawButton.interactable = !isTripped;
            if (InsertButton != null) InsertButton.interactable = !isTripped;
            if (StopButton != null) StopButton.interactable = !isTripped;
            
            // Boron control
            if (BorateButton != null) BorateButton.interactable = !isTripped;
            if (DiluteButton != null) DiluteButton.interactable = !isTripped;
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnWithdrawClick()
        {
            _board?.WithdrawRods();
        }
        
        private void OnInsertClick()
        {
            _board?.InsertRods();
        }
        
        private void OnStopClick()
        {
            _board?.StopRods();
        }
        
        private void OnTripClick()
        {
            if (_tripCoverOpen || TripCoverToggle == null)
            {
                _board?.Trip();
            }
        }
        
        private void OnResetTripClick()
        {
            _board?.ResetTrip();
        }
        
        private void OnTripCoverToggle(bool isOpen)
        {
            _tripCoverOpen = isOpen;
            UpdateTripStatus();
        }
        
        private void OnPauseClick()
        {
            if (_board?.Reactor == null) return;
            
            if (_isPaused)
            {
                // Resume
                _board.Reactor.TimeCompression = _savedTimeCompression;
                _isPaused = false;
            }
            else
            {
                // Pause
                _savedTimeCompression = _board.Reactor.TimeCompression;
                _board.Reactor.TimeCompression = 0f;
                _isPaused = true;
            }
        }
        
        private void OnTimeCompressionChanged(float value)
        {
            if (_board?.Reactor == null) return;
            
            // Map 0-1 to 1-10000 (logarithmic)
            float compression = Mathf.Pow(10f, value * 4f);
            _board.SetTimeCompression(compression);
            
            // Sync slider display
            if (TimeCompressionSlider != null && !_isPaused)
            {
                _savedTimeCompression = compression;
            }
        }
        
        private void OnBorateClick()
        {
            _board?.Reactor?.ChangeBoron(BoronChangeRate);
        }
        
        private void OnDiluteClick()
        {
            _board?.Reactor?.ChangeBoron(-BoronChangeRate);
        }
        
        private void OnStartScenarioClick()
        {
            if (_board?.SimEngine == null || ScenarioDropdown == null) return;
            
            var scenarios = _board.SimEngine.GetAvailableScenarios();
            if (ScenarioDropdown.value < scenarios.Length)
            {
                _board.SimEngine.LoadScenario(scenarios[ScenarioDropdown.value]);
            }
        }
        
        private void OnStopScenarioClick()
        {
            _board?.SimEngine?.StopScenario();
        }
        
        private void OnScenarioSelected(int index)
        {
            // Preview selected scenario
        }
        
        private void OnInitHZPClick()
        {
            _board?.Reactor?.InitializeToHZP();
        }
        
        private void OnInit100Click()
        {
            _board?.Reactor?.InitializeToPower(1.0f);
        }
        
        private void OnResetClick()
        {
            _board?.Reactor?.Reset();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Set time compression directly.
        /// </summary>
        public void SetTimeCompression(float factor)
        {
            _board?.SetTimeCompression(factor);
            
            // Update slider
            if (TimeCompressionSlider != null)
            {
                float normalized = Mathf.Log10(factor) / 4f;
                TimeCompressionSlider.value = Mathf.Clamp01(normalized);
            }
        }
        
        /// <summary>
        /// Toggle pause state.
        /// </summary>
        public void TogglePause()
        {
            OnPauseClick();
        }
        
        #endregion
    }
}
