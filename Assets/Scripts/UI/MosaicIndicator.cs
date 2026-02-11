// CRITICAL: Master the Atom - Phase 2 Mosaic Indicator
// MosaicIndicator.cs - Status Indicator Light Component
//
// Displays binary status with illuminated indicator:
//   - On/Off states with configurable colors
//   - Flashing mode for alarms
//   - Multiple condition types
//
// Reference: Westinghouse Control Room Annunciator Panel

using UnityEngine;
using UnityEngine.UI;

namespace Critical.UI
{
    using Controllers;
    
    /// <summary>
    /// Status indicator condition types.
    /// </summary>
    public enum IndicatorCondition
    {
        // Reactor Status
        ReactorTripped,
        ReactorCritical,
        ReactorSubcritical,
        ReactorSupercritical,
        
        // Power Status
        PowerAbove10Percent,
        PowerAbove50Percent,
        PowerAbove100Percent,
        Overpower,
        
        // Temperature Status
        TavgHigh,
        TavgLow,
        ThotHigh,
        FuelTempHigh,
        
        // Rod Status
        RodsWithdrawing,
        RodsInserting,
        AllRodsIn,
        AllRodsOut,
        RodBottomAlarm,
        
        // Flow Status
        LowFlow,
        
        // Alarms
        AnyAlarm,
        UnacknowledgedAlarm,
        
        // Custom (use CustomCondition delegate)
        Custom
    }
    
    /// <summary>
    /// Individual status indicator light for Mosaic Board.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class MosaicIndicator : MonoBehaviour, IMosaicComponent, IAlarmFlashReceiver
    {
        #region Unity Inspector Fields
        
        [Header("Indicator Configuration")]
        [Tooltip("Condition to monitor")]
        public IndicatorCondition Condition = IndicatorCondition.ReactorCritical;
        
        [Tooltip("Invert condition (true when condition is false)")]
        public bool InvertCondition = false;
        
        [Tooltip("Enable flashing when active")]
        public bool FlashWhenActive = false;
        
        [Header("Visual References")]
        [Tooltip("Main indicator image")]
        public Image IndicatorImage;
        
        [Tooltip("Label text")]
        public Text LabelText;
        
        [Tooltip("Custom label (overrides default)")]
        public string CustomLabel;
        
        [Header("Colors")]
        [Tooltip("Color when indicator is ON")]
        public Color OnColor = new Color(0f, 1f, 0f);
        
        [Tooltip("Color when indicator is OFF")]
        public Color OffColor = new Color(0.2f, 0.2f, 0.2f);
        
        [Tooltip("Use board theme colors")]
        public bool UseBoardColors = true;
        
        [Tooltip("Indicator type for board colors")]
        public AlarmState ColorType = AlarmState.Normal;
        
        #endregion
        
        #region Private Fields
        
        private MosaicBoard _board;
        private bool _isActive;
        private bool _flashState;
        
        /// <summary>Custom condition delegate for Custom type</summary>
        public System.Func<bool> CustomCondition;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>Is indicator currently active?</summary>
        public bool IsActive => _isActive;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (IndicatorImage == null)
            {
                IndicatorImage = GetComponent<Image>();
            }
            
            // Set label
            if (LabelText != null)
            {
                LabelText.text = !string.IsNullOrEmpty(CustomLabel) ? CustomLabel : GetDefaultLabel();
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
            
            // Update colors from board theme
            if (UseBoardColors)
            {
                UpdateColorsFromBoard();
            }
        }
        
        public void UpdateData()
        {
            // Evaluate condition
            _isActive = EvaluateCondition();
            
            if (InvertCondition)
            {
                _isActive = !_isActive;
            }
            
            // Update visual
            UpdateVisual();
        }
        
        #endregion
        
        #region IAlarmFlashReceiver Implementation
        
        public void OnAlarmFlash(bool flashOn)
        {
            _flashState = flashOn;
            
            if (FlashWhenActive && _isActive)
            {
                UpdateVisual();
            }
        }
        
        #endregion
        
        #region Condition Evaluation
        
        private bool EvaluateCondition()
        {
            if (_board?.Reactor == null) return false;
            
            var reactor = _board.Reactor;
            
            return Condition switch
            {
                // Reactor Status
                IndicatorCondition.ReactorTripped => reactor.IsTripped,
                IndicatorCondition.ReactorCritical => reactor.IsCritical,
                IndicatorCondition.ReactorSubcritical => reactor.IsSubcritical,
                IndicatorCondition.ReactorSupercritical => reactor.Keff > 1.001f,
                
                // Power Status
                IndicatorCondition.PowerAbove10Percent => reactor.ThermalPower > 0.10f,
                IndicatorCondition.PowerAbove50Percent => reactor.ThermalPower > 0.50f,
                IndicatorCondition.PowerAbove100Percent => reactor.ThermalPower > 1.00f,
                IndicatorCondition.Overpower => reactor.ThermalPower > 1.05f,
                
                // Temperature Status
                IndicatorCondition.TavgHigh => reactor.Tavg > 595f,
                IndicatorCondition.TavgLow => reactor.Tavg < 550f,
                IndicatorCondition.ThotHigh => reactor.Thot > 630f,
                IndicatorCondition.FuelTempHigh => reactor.FuelCenterline > 3500f,
                
                // Rod Status
                IndicatorCondition.RodsWithdrawing => IsRodsWithdrawing(),
                IndicatorCondition.RodsInserting => IsRodsInserting(),
                IndicatorCondition.AllRodsIn => reactor.BankDPosition < 5f,
                IndicatorCondition.AllRodsOut => reactor.BankDPosition > 220f,
                IndicatorCondition.RodBottomAlarm => reactor.BankDPosition < 10f && reactor.ThermalPower > 0.25f,
                
                // Flow Status
                IndicatorCondition.LowFlow => reactor.FlowFraction < 0.90f,
                
                // Alarms
                IndicatorCondition.AnyAlarm => _board.ActiveAlarms.Count > 0,
                IndicatorCondition.UnacknowledgedAlarm => _board.UnacknowledgedAlarmCount > 0,
                
                // Custom
                IndicatorCondition.Custom => CustomCondition?.Invoke() ?? false,
                
                _ => false
            };
        }
        
        private bool IsRodsWithdrawing()
        {
            // Check if rod position is increasing
            // This would need to be tracked in the controller
            return false; // Placeholder - needs rod motion state
        }
        
        private bool IsRodsInserting()
        {
            // Check if rod position is decreasing
            return false; // Placeholder - needs rod motion state
        }
        
        #endregion
        
        #region Visual Updates
        
        private void UpdateVisual()
        {
            if (IndicatorImage == null) return;
            
            Color targetColor;
            
            if (_isActive)
            {
                if (FlashWhenActive && !_flashState)
                {
                    targetColor = OffColor;
                }
                else
                {
                    targetColor = OnColor;
                }
            }
            else
            {
                targetColor = OffColor;
            }
            
            IndicatorImage.color = targetColor;
        }
        
        private void UpdateColorsFromBoard()
        {
            if (_board == null) return;
            
            OnColor = ColorType switch
            {
                AlarmState.Trip => _board.TripColor,
                AlarmState.Alarm => _board.AlarmColor,
                AlarmState.Warning => _board.WarningColor,
                _ => _board.NormalColor
            };
            
            OffColor = _board.OffColor;
        }
        
        #endregion
        
        #region Helpers
        
        private string GetDefaultLabel()
        {
            return Condition switch
            {
                IndicatorCondition.ReactorTripped => "TRIPPED",
                IndicatorCondition.ReactorCritical => "CRITICAL",
                IndicatorCondition.ReactorSubcritical => "SUBCRIT",
                IndicatorCondition.ReactorSupercritical => "SUPERCRIT",
                IndicatorCondition.PowerAbove10Percent => "P > 10%",
                IndicatorCondition.PowerAbove50Percent => "P > 50%",
                IndicatorCondition.PowerAbove100Percent => "P > 100%",
                IndicatorCondition.Overpower => "OVERPOWER",
                IndicatorCondition.TavgHigh => "TAVG HI",
                IndicatorCondition.TavgLow => "TAVG LO",
                IndicatorCondition.ThotHigh => "THOT HI",
                IndicatorCondition.FuelTempHigh => "FUEL HI",
                IndicatorCondition.RodsWithdrawing => "RODS OUT",
                IndicatorCondition.RodsInserting => "RODS IN",
                IndicatorCondition.AllRodsIn => "ALL IN",
                IndicatorCondition.AllRodsOut => "ALL OUT",
                IndicatorCondition.RodBottomAlarm => "ROD BOT",
                IndicatorCondition.LowFlow => "LO FLOW",
                IndicatorCondition.AnyAlarm => "ALARM",
                IndicatorCondition.UnacknowledgedAlarm => "UNACK",
                _ => Condition.ToString()
            };
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Force indicator on regardless of condition.
        /// </summary>
        public void ForceOn()
        {
            _isActive = true;
            UpdateVisual();
        }
        
        /// <summary>
        /// Force indicator off regardless of condition.
        /// </summary>
        public void ForceOff()
        {
            _isActive = false;
            UpdateVisual();
        }
        
        /// <summary>
        /// Clear forced state and return to condition evaluation.
        /// </summary>
        public void ClearForce()
        {
            UpdateData();
        }
        
        #endregion
    }
}
