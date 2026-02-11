// ============================================================================
// CRITICAL: Master the Atom - Reactor Operator GUI
// RodControlPanel.cs - Rod Control Interface Component
// ============================================================================
//
// PURPOSE:
//   Provides the operator rod control interface for the Reactor Operator
//   Screen (Screen 1). Manages bank selection, withdraw/insert/stop
//   commands, step position display, and rod motion status indication.
//
// FEATURES:
//   - Bank selector: 8 toggle buttons (SA, SB, SC, SD, D, C, B, A)
//   - WITHDRAW / INSERT / STOP command buttons
//   - Step position readout for selected bank (0-228 steps)
//   - Rod motion status indicator (WITHDRAWING / INSERTING / STOPPED / TRIPPED)
//   - Controls disabled when reactor is tripped
//   - Defaults to Control Bank D selected on startup
//
// ARCHITECTURE:
//   - Reads from ReactorController via MosaicBoard reference
//   - Sends commands to ReactorController.WithdrawBank/InsertBank/StopBank
//   - Implements IMosaicComponent for automatic registration and update
//   - Does not modify any physics modules
//
// ROD BANK INDICES (from ControlRodBank.cs / RodBank enum):
//   0=SA, 1=SB, 2=SC, 3=SD, 4=D, 5=C, 6=B, 7=A
//
// SOURCES:
//   - IMPLEMENTATION_PLAN_v4.0.0 Stage 2
//   - Westinghouse 4-Loop PWR Control Room Rod Control Panel
//
// CREATED: v4.0.0
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI
{
    using Controllers;
    using Physics;

    /// <summary>
    /// Rod control panel component for Reactor Operator Screen.
    /// Provides bank selection, withdraw/insert/stop, and position display.
    /// </summary>
    public class RodControlPanel : MonoBehaviour, IMosaicComponent
    {
        // ====================================================================
        // INSPECTOR FIELDS
        // ====================================================================

        #region Inspector Fields

        [Header("Bank Selector Buttons")]
        [Tooltip("8 bank selector toggle buttons in order: SA, SB, SC, SD, D, C, B, A")]
        public Button[] BankSelectorButtons = new Button[8];

        [Header("Command Buttons")]
        [Tooltip("Withdraw rods button")]
        public Button WithdrawButton;

        [Tooltip("Insert rods button")]
        public Button InsertButton;

        [Tooltip("Stop rod motion button")]
        public Button StopButton;

        [Header("Displays")]
        [Tooltip("Selected bank name text")]
        public TextMeshProUGUI SelectedBankText;

        [Tooltip("Step position readout text")]
        public TextMeshProUGUI StepPositionText;

        [Tooltip("Rod motion status text")]
        public TextMeshProUGUI MotionStatusText;

        [Header("Visual Feedback")]
        [Tooltip("Highlight image on selected bank button")]
        public Color SelectedBankColor = new Color(0f, 0.8f, 1f, 1f);       // Cyan

        [Tooltip("Default bank button color")]
        public Color DefaultBankColor = new Color(0.2f, 0.2f, 0.25f, 1f);   // Dark grey

        [Tooltip("Withdraw button active color")]
        public Color WithdrawActiveColor = new Color(0.1f, 0.5f, 0.1f);     // Dark green

        [Tooltip("Insert button active color")]
        public Color InsertActiveColor = new Color(0.6f, 0.5f, 0.1f);       // Dark amber

        [Tooltip("Stop button color")]
        public Color StopButtonColor = new Color(0.5f, 0.1f, 0.1f);         // Dark red

        [Tooltip("Disabled button color")]
        public Color DisabledColor = new Color(0.15f, 0.15f, 0.15f);

        [Tooltip("Motion status - withdrawing color")]
        public Color WithdrawingStatusColor = new Color(0.2f, 0.8f, 0.2f);  // Green

        [Tooltip("Motion status - inserting color")]
        public Color InsertingStatusColor = new Color(1f, 0.72f, 0.19f);    // Amber

        [Tooltip("Motion status - stopped color")]
        public Color StoppedStatusColor = new Color(0.5f, 0.57f, 0.63f);    // Grey

        [Tooltip("Motion status - tripped color")]
        public Color TrippedStatusColor = new Color(1f, 0.2f, 0.27f);       // Red

        #endregion

        // ====================================================================
        // CONSTANTS
        // ====================================================================

        private const int BANK_COUNT = 8;
        private const int DEFAULT_BANK = 4; // Control Bank D

        private static readonly string[] BANK_NAMES = { "SA", "SB", "SC", "SD", "D", "C", "B", "A" };

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        #region Private Fields

        private MosaicBoard _board;
        private ReactorController _reactor;
        private int _selectedBankIndex = DEFAULT_BANK;
        private Image[] _bankButtonImages;

        #endregion

        // ====================================================================
        // PUBLIC PROPERTIES
        // ====================================================================

        #region Properties

        /// <summary>Currently selected bank index (0-7).</summary>
        public int SelectedBankIndex => _selectedBankIndex;

        /// <summary>Currently selected bank name.</summary>
        public string SelectedBankName => BANK_NAMES[_selectedBankIndex];

        #endregion

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        #region Unity Lifecycle

        private void Awake()
        {
            CacheBankButtonImages();
            WireButtons();
        }

        private void OnEnable()
        {
            if (_board == null && MosaicBoard.Instance != null)
            {
                _board = MosaicBoard.Instance;
                _board.RegisterComponent(this);
                _reactor = _board.Reactor;
            }
        }

        private void OnDisable()
        {
            _board?.UnregisterComponent(this);
        }

        #endregion

        // ====================================================================
        // IMosaicComponent IMPLEMENTATION
        // ====================================================================

        #region IMosaicComponent

        public void Initialize(MosaicBoard board)
        {
            _board = board;
            _reactor = board?.Reactor;

            // Apply initial bank selection visual
            UpdateBankSelectionVisual();
        }

        public void UpdateData()
        {
            if (_reactor == null) return;

            UpdateStepPositionDisplay();
            UpdateMotionStatusDisplay();
            UpdateButtonStates();
        }

        #endregion

        // ====================================================================
        // SETUP
        // ====================================================================

        #region Setup

        private void CacheBankButtonImages()
        {
            _bankButtonImages = new Image[BANK_COUNT];
            for (int i = 0; i < BANK_COUNT; i++)
            {
                if (BankSelectorButtons != null && i < BankSelectorButtons.Length && BankSelectorButtons[i] != null)
                {
                    _bankButtonImages[i] = BankSelectorButtons[i].GetComponent<Image>();
                }
            }
        }

        private void WireButtons()
        {
            // Bank selector buttons
            if (BankSelectorButtons != null)
            {
                for (int i = 0; i < BankSelectorButtons.Length && i < BANK_COUNT; i++)
                {
                    if (BankSelectorButtons[i] != null)
                    {
                        int bankIdx = i; // Capture for lambda
                        BankSelectorButtons[i].onClick.AddListener(() => SelectBank(bankIdx));
                    }
                }
            }

            // Command buttons
            if (WithdrawButton != null)
                WithdrawButton.onClick.AddListener(OnWithdrawClick);

            if (InsertButton != null)
                InsertButton.onClick.AddListener(OnInsertClick);

            if (StopButton != null)
                StopButton.onClick.AddListener(OnStopClick);
        }

        #endregion

        // ====================================================================
        // DISPLAY UPDATES (called at MosaicBoard update rate)
        // ====================================================================

        #region Display Updates

        private void UpdateStepPositionDisplay()
        {
            if (StepPositionText == null || _reactor == null) return;

            float position = _reactor.GetBankPosition(_selectedBankIndex);
            StepPositionText.text = $"{position:F0}";
        }

        private void UpdateMotionStatusDisplay()
        {
            if (MotionStatusText == null || _reactor == null) return;

            if (_reactor.IsTripped)
            {
                MotionStatusText.text = "TRIPPED";
                MotionStatusText.color = TrippedStatusColor;
                return;
            }

            RodDirection direction = _reactor.GetBankDirection(_selectedBankIndex);

            switch (direction)
            {
                case RodDirection.Withdrawing:
                    MotionStatusText.text = "WITHDRAWING";
                    MotionStatusText.color = WithdrawingStatusColor;
                    break;

                case RodDirection.Inserting:
                    MotionStatusText.text = "INSERTING";
                    MotionStatusText.color = InsertingStatusColor;
                    break;

                default:
                    MotionStatusText.text = "STOPPED";
                    MotionStatusText.color = StoppedStatusColor;
                    break;
            }
        }

        private void UpdateButtonStates()
        {
            bool isTripped = _reactor?.IsTripped ?? true;

            if (WithdrawButton != null) WithdrawButton.interactable = !isTripped;
            if (InsertButton != null) InsertButton.interactable = !isTripped;
            // Stop is always available (even during trip, for completeness)
            if (StopButton != null) StopButton.interactable = true;
        }

        private void UpdateBankSelectionVisual()
        {
            for (int i = 0; i < BANK_COUNT; i++)
            {
                if (_bankButtonImages != null && i < _bankButtonImages.Length && _bankButtonImages[i] != null)
                {
                    _bankButtonImages[i].color = (i == _selectedBankIndex)
                        ? SelectedBankColor
                        : DefaultBankColor;
                }
            }

            // Update bank name display
            if (SelectedBankText != null)
            {
                SelectedBankText.text = BANK_NAMES[_selectedBankIndex];
            }
        }

        #endregion

        // ====================================================================
        // BUTTON HANDLERS
        // ====================================================================

        #region Button Handlers

        /// <summary>
        /// Select a bank for rod control commands.
        /// </summary>
        public void SelectBank(int bankIndex)
        {
            if (bankIndex < 0 || bankIndex >= BANK_COUNT) return;

            _selectedBankIndex = bankIndex;
            UpdateBankSelectionVisual();

            // Immediately update position display for new bank
            UpdateStepPositionDisplay();
            UpdateMotionStatusDisplay();

            Debug.Log($"[RodControlPanel] Selected bank: {BANK_NAMES[bankIndex]} (index {bankIndex})");
        }

        private void OnWithdrawClick()
        {
            if (_reactor == null || _reactor.IsTripped) return;

            _reactor.WithdrawBank(_selectedBankIndex);
            Debug.Log($"[RodControlPanel] Withdraw {BANK_NAMES[_selectedBankIndex]}");
        }

        private void OnInsertClick()
        {
            if (_reactor == null || _reactor.IsTripped) return;

            _reactor.InsertBank(_selectedBankIndex);
            Debug.Log($"[RodControlPanel] Insert {BANK_NAMES[_selectedBankIndex]}");
        }

        private void OnStopClick()
        {
            if (_reactor == null) return;

            _reactor.StopBank(_selectedBankIndex);
            Debug.Log($"[RodControlPanel] Stop {BANK_NAMES[_selectedBankIndex]}");
        }

        #endregion

        // ====================================================================
        // PUBLIC METHODS
        // ====================================================================

        #region Public Methods

        /// <summary>
        /// Force refresh all displays.
        /// </summary>
        public void Refresh()
        {
            UpdateBankSelectionVisual();
            UpdateStepPositionDisplay();
            UpdateMotionStatusDisplay();
            UpdateButtonStates();
        }

        #endregion
    }
}
