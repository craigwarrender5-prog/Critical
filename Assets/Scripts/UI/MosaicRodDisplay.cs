// CRITICAL: Master the Atom - Phase 2 Mosaic Rod Display
// MosaicRodDisplay.cs - Control Rod Position Visualization
//
// Displays control rod bank positions with:
//   - Visual rod position bars
//   - Digital position readouts
//   - Bank labels and grouping
//   - Withdrawal/insertion limits
//
// Reference: Westinghouse Rod Position Indication System (RPIS)

using UnityEngine;
using UnityEngine.UI;

namespace Critical.UI
{
    using Controllers;
    using Physics;
    
    /// <summary>
    /// Control rod position display for Mosaic Board.
    /// </summary>
    public class MosaicRodDisplay : MonoBehaviour, IMosaicComponent, IAlarmFlashReceiver
    {
        #region Unity Inspector Fields
        
        [Header("Display Configuration")]
        [Tooltip("Show all 8 banks")]
        public bool ShowAllBanks = true;
        
        [Tooltip("Bank index to display (if not showing all)")]
        [Range(0, 7)]
        public int SingleBankIndex = 4; // Bank D by default
        
        [Tooltip("Show digital position values")]
        public bool ShowDigitalReadout = true;
        
        [Tooltip("Show insertion limit line")]
        public bool ShowInsertionLimit = true;
        
        [Header("Visual References - Single Bank Mode")]
        [Tooltip("Fill bar for rod position")]
        public Image FillBar;
        
        [Tooltip("Position text")]
        public Text PositionText;
        
        [Tooltip("Bank label text")]
        public Text BankLabel;
        
        [Tooltip("Insertion limit marker")]
        public RectTransform InsertionLimitMarker;
        
        [Header("Visual References - All Banks Mode")]
        [Tooltip("Container for bank bar prefabs")]
        public RectTransform BankContainer;
        
        [Tooltip("Bank bar prefab (optional)")]
        public GameObject BankBarPrefab;
        
        [Tooltip("Pre-assigned bank fill bars (SA through A)")]
        public Image[] BankFillBars = new Image[8];
        
        [Tooltip("Pre-assigned bank position texts")]
        public Text[] BankPositionTexts = new Text[8];
        
        [Tooltip("Pre-assigned bank labels")]
        public Text[] BankLabels = new Text[8];
        
        [Header("Trip Indicator")]
        [Tooltip("Trip indicator image")]
        public Image TripIndicator;
        
        [Tooltip("Trip indicator text")]
        public Text TripText;
        
        [Header("Colors")]
        public Color NormalColor = new Color(0.2f, 0.8f, 0.2f);
        public Color WarningColor = new Color(1f, 0.8f, 0f);
        public Color AlarmColor = new Color(1f, 0.2f, 0.2f);
        public Color TripColor = new Color(1f, 0f, 1f);
        public Color ShutdownBankColor = new Color(0.5f, 0.5f, 1f);
        public Color ControlBankColor = new Color(0.2f, 0.8f, 0.2f);
        
        #endregion
        
        #region Constants
        
        private static readonly string[] BANK_NAMES = { "SA", "SB", "SC", "SD", "D", "C", "B", "A" };
        private const float TOTAL_STEPS = 228f;
        private const float INSERTION_LIMIT = 30f; // Bank D insertion limit
        
        #endregion
        
        #region Private Fields
        
        private MosaicBoard _board;
        private float[] _bankPositions = new float[8];
        private bool _isTripped;
        private bool _alarmFlashing;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Initialize bank labels
            if (ShowAllBanks)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (BankLabels[i] != null)
                    {
                        BankLabels[i].text = BANK_NAMES[i];
                    }
                }
            }
            else if (BankLabel != null)
            {
                BankLabel.text = BANK_NAMES[SingleBankIndex];
            }
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
        }
        
        public void UpdateData()
        {
            if (_board?.Reactor == null) return;
            
            var reactor = _board.Reactor;
            
            // Get trip state
            _isTripped = reactor.IsTripped;
            
            // Get all bank positions
            for (int i = 0; i < 8; i++)
            {
                _bankPositions[i] = reactor.GetBankPosition(i);
            }
            
            // Update visuals
            if (ShowAllBanks)
            {
                UpdateAllBanks();
            }
            else
            {
                UpdateSingleBank();
            }
            
            UpdateTripIndicator();
        }
        
        #endregion
        
        #region IAlarmFlashReceiver Implementation
        
        public void OnAlarmFlash(bool flashOn)
        {
            _alarmFlashing = flashOn;
            
            // Flash trip indicator
            if (_isTripped && TripIndicator != null)
            {
                TripIndicator.color = flashOn ? TripColor : Color.black;
            }
        }
        
        #endregion
        
        #region Visual Updates
        
        private void UpdateAllBanks()
        {
            for (int i = 0; i < 8; i++)
            {
                UpdateBankVisual(i, _bankPositions[i]);
            }
        }
        
        private void UpdateSingleBank()
        {
            float position = _bankPositions[SingleBankIndex];
            
            // Update fill bar
            if (FillBar != null)
            {
                FillBar.fillAmount = position / TOTAL_STEPS;
                FillBar.color = GetBankColor(SingleBankIndex, position);
            }
            
            // Update position text
            if (PositionText != null && ShowDigitalReadout)
            {
                PositionText.text = $"{position:F0}";
            }
            
            // Update insertion limit marker
            if (InsertionLimitMarker != null && ShowInsertionLimit && SingleBankIndex == 4)
            {
                // Position marker at insertion limit
                float normalizedLimit = INSERTION_LIMIT / TOTAL_STEPS;
                var anchoredPos = InsertionLimitMarker.anchoredPosition;
                anchoredPos.y = normalizedLimit * InsertionLimitMarker.parent.GetComponent<RectTransform>().rect.height;
                InsertionLimitMarker.anchoredPosition = anchoredPos;
                
                // Color based on position
                var markerImage = InsertionLimitMarker.GetComponent<Image>();
                if (markerImage != null)
                {
                    markerImage.color = position < INSERTION_LIMIT ? AlarmColor : WarningColor;
                }
            }
        }
        
        private void UpdateBankVisual(int bankIndex, float position)
        {
            // Update fill bar
            if (bankIndex < BankFillBars.Length && BankFillBars[bankIndex] != null)
            {
                BankFillBars[bankIndex].fillAmount = position / TOTAL_STEPS;
                BankFillBars[bankIndex].color = GetBankColor(bankIndex, position);
            }
            
            // Update position text
            if (ShowDigitalReadout && bankIndex < BankPositionTexts.Length && BankPositionTexts[bankIndex] != null)
            {
                BankPositionTexts[bankIndex].text = $"{position:F0}";
            }
        }
        
        private void UpdateTripIndicator()
        {
            if (TripIndicator != null)
            {
                TripIndicator.gameObject.SetActive(_isTripped);
                
                if (_isTripped && !_alarmFlashing)
                {
                    TripIndicator.color = TripColor;
                }
            }
            
            if (TripText != null)
            {
                TripText.gameObject.SetActive(_isTripped);
                TripText.text = "TRIPPED";
            }
        }
        
        private Color GetBankColor(int bankIndex, float position)
        {
            // Trip color
            if (_isTripped)
            {
                return _alarmFlashing ? TripColor : Color.black;
            }
            
            // Alarm: Bank D below insertion limit while at power
            if (bankIndex == 4 && position < INSERTION_LIMIT && 
                _board?.Reactor?.ThermalPower > 0.25f)
            {
                return AlarmColor;
            }
            
            // Warning: Bank D approaching insertion limit
            if (bankIndex == 4 && position < INSERTION_LIMIT + 20f)
            {
                return WarningColor;
            }
            
            // Normal: Shutdown banks (SA-SD) vs Control banks (D-A)
            if (bankIndex < 4)
            {
                return ShutdownBankColor;
            }
            else
            {
                return ControlBankColor;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Get bank name by index.
        /// </summary>
        public string GetBankName(int index)
        {
            if (index >= 0 && index < BANK_NAMES.Length)
            {
                return BANK_NAMES[index];
            }
            return "??";
        }
        
        /// <summary>
        /// Get position of specific bank.
        /// </summary>
        public float GetBankPosition(int index)
        {
            if (index >= 0 && index < _bankPositions.Length)
            {
                return _bankPositions[index];
            }
            return 0f;
        }
        
        /// <summary>
        /// Get formatted rod status string.
        /// </summary>
        public string GetStatusText()
        {
            if (_isTripped)
            {
                return "RODS TRIPPED";
            }
            
            // Find which control bank is active
            for (int i = 7; i >= 4; i--) // A, B, C, D order
            {
                if (_bankPositions[i] > 0f && _bankPositions[i] < TOTAL_STEPS)
                {
                    return $"Bank {BANK_NAMES[i]}: {_bankPositions[i]:F0} steps";
                }
            }
            
            // All in or all out
            if (_bankPositions[4] < 5f) // Bank D nearly in
            {
                return "ALL RODS IN";
            }
            else if (_bankPositions[7] > TOTAL_STEPS - 5f) // Bank A nearly out
            {
                return "ALL RODS OUT";
            }
            
            return $"Bank D: {_bankPositions[4]:F0} steps";
        }
        
        #endregion
    }
}
