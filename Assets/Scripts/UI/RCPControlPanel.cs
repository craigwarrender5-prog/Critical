// ============================================================================
// CRITICAL: Master the Atom - RCP Control Panel Component
// RCPControlPanel.cs - Individual RCP Control Panel UI Component
// ============================================================================
//
// PURPOSE:
//   Provides the UI control panel for a single Reactor Coolant Pump (RCP).
//   Each panel displays pump status and provides START/STOP controls.
//
// FEATURES:
//   - Pump number label
//   - Status indicator (Running/Stopped/Ramping/Tripped)
//   - Speed display (RPM)
//   - Flow percentage display
//   - START button with interlock indication
//   - STOP/TRIP button
//   - Amps display (motor current)
//
// LAYOUT:
//   ┌─────────────────────────────────┐
//   │         RCP-1                   │
//   │  ┌─────┐  Speed: 1189 RPM      │
//   │  │ ● ● │  Flow:  100.0%        │
//   │  │STAT │  Amps:  245 A         │
//   │  └─────┘                        │
//   │  [START]         [STOP]        │
//   └─────────────────────────────────┘
//
// USAGE:
//   1. Create UI panel with required child elements
//   2. Attach this component to the panel
//   3. Assign UI references in Inspector
//   4. Call Initialize() with callbacks
//   5. Call UpdateDisplay() each frame with current state
//
// VERSION: 1.0.0
// DATE: 2026-02-09
// CLASSIFICATION: UI — Control Component
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI
{
    /// <summary>
    /// UI control panel for a single Reactor Coolant Pump.
    /// </summary>
    public class RCPControlPanel : MonoBehaviour
    {
        // ====================================================================
        // CONSTANTS
        // ====================================================================

        #region Constants

        // Westinghouse RCP specifications
        private const float RCP_RATED_SPEED_RPM = 1189f;
        private const float RCP_RATED_AMPS = 245f;
        private const float RCP_IDLE_AMPS = 50f;

        #endregion

        // ====================================================================
        // SERIALIZED FIELDS
        // ====================================================================

        #region Inspector Fields - Labels

        [Header("=== LABELS ===")]
        [Tooltip("Pump number label (e.g., 'RCP-1')")]
        [SerializeField] private TextMeshProUGUI text_PumpLabel;

        [Tooltip("Status text display")]
        [SerializeField] private TextMeshProUGUI text_Status;

        [Tooltip("Speed display (RPM)")]
        [SerializeField] private TextMeshProUGUI text_Speed;

        [Tooltip("Flow percentage display")]
        [SerializeField] private TextMeshProUGUI text_Flow;

        [Tooltip("Motor amps display")]
        [SerializeField] private TextMeshProUGUI text_Amps;

        #endregion

        #region Inspector Fields - Indicators

        [Header("=== INDICATORS ===")]
        [Tooltip("Main status indicator image")]
        [SerializeField] private Image indicator_Status;

        [Tooltip("Running indicator light")]
        [SerializeField] private Image indicator_Running;

        [Tooltip("Stopped indicator light")]
        [SerializeField] private Image indicator_Stopped;

        [Tooltip("Optional speed bar fill")]
        [SerializeField] private Image bar_SpeedFill;

        #endregion

        #region Inspector Fields - Buttons

        [Header("=== BUTTONS ===")]
        [Tooltip("START button")]
        [SerializeField] private Button button_Start;

        [Tooltip("STOP/TRIP button")]
        [SerializeField] private Button button_Stop;

        [Tooltip("Optional interlock indicator on START button")]
        [SerializeField] private Image indicator_Interlock;

        #endregion

        #region Inspector Fields - Colors

        [Header("=== COLORS ===")]
        [SerializeField] private Color color_Running = new Color(0.2f, 0.9f, 0.2f);
        [SerializeField] private Color color_Stopped = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color color_Ramping = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private Color color_Tripped = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color color_Interlock = new Color(1f, 0.5f, 0f);
        [SerializeField] private Color color_ButtonEnabled = new Color(0.3f, 0.3f, 0.35f);
        [SerializeField] private Color color_ButtonDisabled = new Color(0.2f, 0.2f, 0.22f);

        #endregion

        #region Inspector Fields - Animation

        [Header("=== ANIMATION ===")]
        [Tooltip("Enable indicator flash for ramping state")]
        [SerializeField] private bool enableFlash = true;

        [Tooltip("Flash frequency (Hz)")]
        [SerializeField] private float flashFrequency = 2f;

        #endregion

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        #region Private Fields

        private int _pumpNumber;
        private Action _onStartRequested;
        private Action _onStopRequested;
        private RCPState _currentState = RCPState.Stopped;
        private bool _isInitialized = false;
        private float _flashTimer = 0f;
        private bool _flashState = false;

        #endregion

        // ====================================================================
        // PUBLIC PROPERTIES
        // ====================================================================

        #region Public Properties

        /// <summary>
        /// Pump number (1-4).
        /// </summary>
        public int PumpNumber => _pumpNumber;

        /// <summary>
        /// Current pump state.
        /// </summary>
        public RCPState CurrentState => _currentState;

        /// <summary>
        /// Is the panel initialized?
        /// </summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        #region Unity Lifecycle

        private void Awake()
        {
            // Wire up button events if buttons exist
            if (button_Start != null)
            {
                button_Start.onClick.AddListener(OnStartButtonClicked);
            }

            if (button_Stop != null)
            {
                button_Stop.onClick.AddListener(OnStopButtonClicked);
            }
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // Handle flash animation for ramping state
            if (enableFlash && _currentState == RCPState.Ramping)
            {
                _flashTimer += Time.deltaTime;
                if (_flashTimer >= 1f / flashFrequency)
                {
                    _flashTimer = 0f;
                    _flashState = !_flashState;
                    UpdateFlashState();
                }
            }
        }

        private void OnDestroy()
        {
            // Clean up button listeners
            if (button_Start != null)
            {
                button_Start.onClick.RemoveListener(OnStartButtonClicked);
            }

            if (button_Stop != null)
            {
                button_Stop.onClick.RemoveListener(OnStopButtonClicked);
            }
        }

        #endregion

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        #region Initialization

        /// <summary>
        /// Initialize the control panel with pump number and callbacks.
        /// </summary>
        /// <param name="pumpNumber">Pump number (1-4)</param>
        /// <param name="onStartRequested">Callback when START is pressed</param>
        /// <param name="onStopRequested">Callback when STOP is pressed</param>
        public void Initialize(int pumpNumber, Action onStartRequested, Action onStopRequested)
        {
            _pumpNumber = pumpNumber;
            _onStartRequested = onStartRequested;
            _onStopRequested = onStopRequested;

            // Set pump label
            if (text_PumpLabel != null)
            {
                text_PumpLabel.text = $"RCP-{pumpNumber}";
            }

            // Initial display update
            UpdateDisplay(RCPState.Stopped, 0f, 0f, true, false);

            _isInitialized = true;

            Debug.Log($"[RCPControlPanel] RCP-{pumpNumber} initialized");
        }

        #endregion

        // ====================================================================
        // DISPLAY UPDATE
        // ====================================================================

        #region Display Update

        /// <summary>
        /// Update the panel display with current pump state.
        /// </summary>
        /// <param name="state">Current pump state</param>
        /// <param name="speed">Current speed in RPM</param>
        /// <param name="flowFraction">Flow fraction (0-1)</param>
        /// <param name="canStart">Is START button enabled</param>
        /// <param name="canStop">Is STOP button enabled</param>
        public void UpdateDisplay(RCPState state, float speed, float flowFraction, bool canStart, bool canStop)
        {
            _currentState = state;

            // Update status text and color
            UpdateStatusDisplay(state);

            // Update speed display
            UpdateSpeedDisplay(speed);

            // Update flow display
            UpdateFlowDisplay(flowFraction);

            // Update amps display (estimated from speed)
            UpdateAmpsDisplay(speed);

            // Update button states
            UpdateButtonStates(canStart, canStop, state);

            // Update indicators
            UpdateIndicators(state);

            // Update speed bar
            UpdateSpeedBar(speed);
        }

        /// <summary>
        /// Update status text and color based on state.
        /// </summary>
        private void UpdateStatusDisplay(RCPState state)
        {
            if (text_Status == null) return;

            switch (state)
            {
                case RCPState.Running:
                    text_Status.text = "RUNNING";
                    text_Status.color = color_Running;
                    break;

                case RCPState.Stopped:
                    text_Status.text = "STOPPED";
                    text_Status.color = color_Stopped;
                    break;

                case RCPState.Ramping:
                    text_Status.text = "RAMPING";
                    text_Status.color = color_Ramping;
                    break;

                case RCPState.Tripped:
                    text_Status.text = "TRIPPED";
                    text_Status.color = color_Tripped;
                    break;
            }
        }

        /// <summary>
        /// Update speed display.
        /// </summary>
        private void UpdateSpeedDisplay(float speed)
        {
            if (text_Speed == null) return;

            if (speed < 1f)
            {
                text_Speed.text = "Speed: --- RPM";
            }
            else
            {
                text_Speed.text = $"Speed: {speed:F0} RPM";
            }
        }

        /// <summary>
        /// Update flow percentage display.
        /// </summary>
        private void UpdateFlowDisplay(float flowFraction)
        {
            if (text_Flow == null) return;

            float percent = flowFraction * 100f;
            text_Flow.text = $"Flow: {percent:F1}%";
        }

        /// <summary>
        /// Update motor amps display (estimated from speed).
        /// </summary>
        private void UpdateAmpsDisplay(float speed)
        {
            if (text_Amps == null) return;

            // Estimate amps based on speed
            // At zero speed: idle amps
            // At rated speed: rated amps
            float speedFraction = Mathf.Clamp01(speed / RCP_RATED_SPEED_RPM);
            float amps = Mathf.Lerp(RCP_IDLE_AMPS, RCP_RATED_AMPS, speedFraction * speedFraction);

            if (speed < 1f)
            {
                text_Amps.text = "Amps: --- A";
            }
            else
            {
                text_Amps.text = $"Amps: {amps:F0} A";
            }
        }

        /// <summary>
        /// Update button interactivity and colors.
        /// </summary>
        private void UpdateButtonStates(bool canStart, bool canStop, RCPState state)
        {
            // START button
            if (button_Start != null)
            {
                button_Start.interactable = canStart;

                var startColors = button_Start.colors;
                startColors.normalColor = canStart ? color_ButtonEnabled : color_ButtonDisabled;
                button_Start.colors = startColors;
            }

            // Interlock indicator
            if (indicator_Interlock != null)
            {
                bool showInterlock = !canStart && state == RCPState.Stopped;
                indicator_Interlock.gameObject.SetActive(showInterlock);
                if (showInterlock)
                {
                    indicator_Interlock.color = color_Interlock;
                }
            }

            // STOP button
            if (button_Stop != null)
            {
                button_Stop.interactable = canStop;

                var stopColors = button_Stop.colors;
                stopColors.normalColor = canStop ? color_Tripped : color_ButtonDisabled;
                button_Stop.colors = stopColors;
            }
        }

        /// <summary>
        /// Update status indicators.
        /// </summary>
        private void UpdateIndicators(RCPState state)
        {
            // Main status indicator
            if (indicator_Status != null)
            {
                indicator_Status.color = GetStateColor(state);
            }

            // Running/Stopped indicators
            if (indicator_Running != null)
            {
                bool isOn = (state == RCPState.Running || state == RCPState.Ramping);
                indicator_Running.color = isOn ? color_Running : color_Stopped;
            }

            if (indicator_Stopped != null)
            {
                bool isOff = (state == RCPState.Stopped || state == RCPState.Tripped);
                indicator_Stopped.color = isOff ? color_Tripped : color_Stopped;
            }
        }

        /// <summary>
        /// Update speed bar fill.
        /// </summary>
        private void UpdateSpeedBar(float speed)
        {
            if (bar_SpeedFill == null) return;

            float fill = Mathf.Clamp01(speed / RCP_RATED_SPEED_RPM);
            bar_SpeedFill.fillAmount = fill;

            // Color based on speed
            if (fill > 0.95f)
            {
                bar_SpeedFill.color = color_Running;
            }
            else if (fill > 0.1f)
            {
                bar_SpeedFill.color = color_Ramping;
            }
            else
            {
                bar_SpeedFill.color = color_Stopped;
            }
        }

        /// <summary>
        /// Update flash state for ramping indicators.
        /// </summary>
        private void UpdateFlashState()
        {
            if (_currentState != RCPState.Ramping) return;

            Color flashColor = _flashState ? color_Ramping : color_Stopped;

            if (indicator_Status != null)
            {
                indicator_Status.color = flashColor;
            }

            if (indicator_Running != null)
            {
                indicator_Running.color = flashColor;
            }
        }

        /// <summary>
        /// Get color for a given state.
        /// </summary>
        private Color GetStateColor(RCPState state)
        {
            return state switch
            {
                RCPState.Running => color_Running,
                RCPState.Ramping => color_Ramping,
                RCPState.Tripped => color_Tripped,
                _ => color_Stopped
            };
        }

        #endregion

        // ====================================================================
        // BUTTON HANDLERS
        // ====================================================================

        #region Button Handlers

        /// <summary>
        /// Handle START button click.
        /// </summary>
        private void OnStartButtonClicked()
        {
            if (!_isInitialized) return;

            Debug.Log($"[RCPControlPanel] RCP-{_pumpNumber} START requested");
            _onStartRequested?.Invoke();
        }

        /// <summary>
        /// Handle STOP button click.
        /// </summary>
        private void OnStopButtonClicked()
        {
            if (!_isInitialized) return;

            Debug.Log($"[RCPControlPanel] RCP-{_pumpNumber} STOP requested");
            _onStopRequested?.Invoke();
        }

        #endregion

        // ====================================================================
        // PUBLIC METHODS
        // ====================================================================

        #region Public Methods

        /// <summary>
        /// Set the panel to show tripped state with alarm.
        /// </summary>
        public void SetTripped()
        {
            _currentState = RCPState.Tripped;
            UpdateStatusDisplay(RCPState.Tripped);
            UpdateIndicators(RCPState.Tripped);
            UpdateButtonStates(false, false, RCPState.Tripped);

            // Flash effect
            if (indicator_Status != null)
            {
                StartCoroutine(TripFlashCoroutine());
            }
        }

        /// <summary>
        /// Reset tripped state to stopped.
        /// </summary>
        public void ResetTrip()
        {
            if (_currentState == RCPState.Tripped)
            {
                _currentState = RCPState.Stopped;
                UpdateDisplay(RCPState.Stopped, 0f, 0f, true, false);
            }
        }

        /// <summary>
        /// Enable or disable the entire panel.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            if (button_Start != null) button_Start.interactable = enabled;
            if (button_Stop != null) button_Stop.interactable = enabled;

            // Dim the panel if disabled
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = enabled ? 1f : 0.5f;
            }
        }

        #endregion

        // ====================================================================
        // COROUTINES
        // ====================================================================

        #region Coroutines

        /// <summary>
        /// Flash the trip indicator several times.
        /// </summary>
        private System.Collections.IEnumerator TripFlashCoroutine()
        {
            for (int i = 0; i < 6; i++)
            {
                if (indicator_Status != null)
                {
                    indicator_Status.color = (i % 2 == 0) ? color_Tripped : color_Stopped;
                }
                yield return new WaitForSeconds(0.15f);
            }

            // End on tripped color
            if (indicator_Status != null)
            {
                indicator_Status.color = color_Tripped;
            }
        }

        #endregion

        // ====================================================================
        // EDITOR HELPERS
        // ====================================================================

        #region Editor Helpers

#if UNITY_EDITOR
        /// <summary>
        /// Auto-find UI references by name.
        /// </summary>
        [ContextMenu("Auto-Find References")]
        private void AutoFindReferences()
        {
            // Find text elements
            text_PumpLabel = FindChildComponent<TextMeshProUGUI>("Label", "PumpLabel", "Title");
            text_Status = FindChildComponent<TextMeshProUGUI>("Status", "StatusText");
            text_Speed = FindChildComponent<TextMeshProUGUI>("Speed", "SpeedText", "RPM");
            text_Flow = FindChildComponent<TextMeshProUGUI>("Flow", "FlowText");
            text_Amps = FindChildComponent<TextMeshProUGUI>("Amps", "AmpsText", "Current");

            // Find indicators
            indicator_Status = FindChildComponent<Image>("StatusIndicator", "Indicator", "Light");
            indicator_Running = FindChildComponent<Image>("RunningLight", "Running");
            indicator_Stopped = FindChildComponent<Image>("StoppedLight", "Stopped");
            bar_SpeedFill = FindChildComponent<Image>("SpeedBar", "SpeedFill", "BarFill");

            // Find buttons
            button_Start = FindChildComponent<Button>("Start", "StartButton", "BTN_Start");
            button_Stop = FindChildComponent<Button>("Stop", "StopButton", "Trip", "BTN_Stop");
            indicator_Interlock = FindChildComponent<Image>("Interlock", "InterlockIndicator");

            Debug.Log("[RCPControlPanel] Auto-find complete. Check Inspector for results.");
        }

        /// <summary>
        /// Find a child component by possible names.
        /// </summary>
        private T FindChildComponent<T>(params string[] possibleNames) where T : Component
        {
            foreach (string name in possibleNames)
            {
                T found = GetComponentInChildren<T>(true);
                if (found != null)
                {
                    Transform t = transform.Find(name);
                    if (t != null)
                    {
                        T component = t.GetComponent<T>();
                        if (component != null) return component;
                    }

                    // Also search recursively
                    foreach (Transform child in GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name.ToLower().Contains(name.ToLower()))
                        {
                            T comp = child.GetComponent<T>();
                            if (comp != null) return comp;
                        }
                    }
                }
            }
            return null;
        }
#endif

        #endregion
    }
}
