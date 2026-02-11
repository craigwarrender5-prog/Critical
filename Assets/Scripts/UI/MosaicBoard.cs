// CRITICAL: Master the Atom - Phase 2 Mosaic Board
// MosaicBoard.cs - Main Mosaic Board Controller
//
// Central controller for the reactor control room visual display:
//   - Manages all gauge, indicator, and display components
//   - Provides data binding to ReactorController
//   - Handles alarm acknowledgment and display
//   - Supports customizable layout
//
// Reference: Westinghouse 4-Loop PWR Control Room Layout
//
// Usage: Attach to main Mosaic Board GameObject in scene.
// Child objects with MosaicGauge, MosaicIndicator, etc. auto-register.
//
// CHANGE: v4.0.0 — Fixed Period gauge displaying float.MaxValue (3.4e+38) as
//         raw text when subcritical. Now catches MaxValue, NaN, and any period
//         >1e10 seconds and displays "∞" instead.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Critical.UI
{
    using Controllers;
    using Physics;
    
    /// <summary>
    /// Mosaic Board section identifiers.
    /// </summary>
    public enum MosaicSection
    {
        Power,
        Temperature,
        Reactivity,
        RodControl,
        Chemistry,
        Alarms,
        Status
    }
    
    /// <summary>
    /// Main Mosaic Board controller for reactor visualization.
    /// </summary>
    public class MosaicBoard : MonoBehaviour
    {
        #region Singleton
        
        private static MosaicBoard _instance;
        public static MosaicBoard Instance => _instance;
        
        #endregion
        
        #region Unity Inspector Fields
        
        [Header("Controller References")]
        [Tooltip("Reactor controller reference")]
        public ReactorController Reactor;
        
        [Tooltip("Simulation engine reference (optional)")]
        public ReactorSimEngine SimEngine;
        
        [Header("Section Containers")]
        [Tooltip("Power section container")]
        public RectTransform PowerSection;
        
        [Tooltip("Temperature section container")]
        public RectTransform TemperatureSection;
        
        [Tooltip("Reactivity section container")]
        public RectTransform ReactivitySection;
        
        [Tooltip("Rod control section container")]
        public RectTransform RodControlSection;
        
        [Tooltip("Chemistry section container")]
        public RectTransform ChemistrySection;
        
        [Tooltip("Alarm section container")]
        public RectTransform AlarmSection;
        
        [Header("Display Settings")]
        [Tooltip("Update rate in Hz")]
        [Range(1f, 60f)]
        public float UpdateRate = 10f;
        
        [Tooltip("Enable smooth value transitions")]
        public bool SmoothTransitions = true;
        
        [Tooltip("Transition smoothing factor")]
        [Range(0.01f, 1f)]
        public float SmoothFactor = 0.1f;
        
        [Header("Alarm Settings")]
        [Tooltip("Alarm flash rate in Hz")]
        public float AlarmFlashRate = 2f;
        
        [Tooltip("Alarm horn enabled")]
        public bool AlarmHornEnabled = true;
        
        [Tooltip("Alarm horn audio source")]
        public AudioSource AlarmHorn;
        
        [Header("Theme Colors")]
        public Color NormalColor = new Color(0.2f, 0.8f, 0.2f);
        public Color WarningColor = new Color(1f, 0.8f, 0f);
        public Color AlarmColor = new Color(1f, 0.2f, 0.2f);
        public Color TripColor = new Color(1f, 0f, 1f);
        public Color OffColor = new Color(0.3f, 0.3f, 0.3f);
        public Color BackgroundColor = new Color(0.1f, 0.1f, 0.12f);
        public Color TextColor = new Color(0.9f, 0.9f, 0.9f);
        public Color AccentColor = new Color(0f, 0.6f, 1f);
        
        #endregion
        
        #region Events
        
        /// <summary>Fired when data is updated</summary>
        public event Action OnDataUpdate;
        
        /// <summary>Fired when alarm state changes</summary>
        public event Action<bool> OnAlarmStateChanged;
        
        #endregion
        
        #region Private Fields
        
        private float _lastUpdateTime;
        private float _updateInterval;
        private bool _alarmFlashState;
        private float _lastFlashTime;
        
        private List<IMosaicComponent> _components = new List<IMosaicComponent>();
        private List<ActiveAlarm> _alarms = new List<ActiveAlarm>();
        private bool _hasUnacknowledgedAlarms;
        
        // Cached values for smooth transitions
        private Dictionary<string, float> _smoothedValues = new Dictionary<string, float>();
        
        #endregion
        
        #region Public Properties
        
        /// <summary>Is alarm active (unacknowledged)?</summary>
        public bool IsAlarmActive => _hasUnacknowledgedAlarms;
        
        /// <summary>Current alarm flash state</summary>
        public bool AlarmFlashState => _alarmFlashState;
        
        /// <summary>All active alarms</summary>
        public IReadOnlyList<ActiveAlarm> ActiveAlarms => _alarms.AsReadOnly();
        
        /// <summary>Number of unacknowledged alarms</summary>
        public int UnacknowledgedAlarmCount => _alarms.FindAll(a => !a.Acknowledged).Count;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _instance = this;
            _updateInterval = 1f / UpdateRate;
        }
        
        private void Start()
        {
            // Auto-find references
            if (Reactor == null)
            {
                Reactor = FindObjectOfType<ReactorController>();
            }
            
            if (SimEngine == null)
            {
                SimEngine = FindObjectOfType<ReactorSimEngine>();
            }
            
            // Register all child components
            RegisterComponents();
            
            // Subscribe to reactor events
            if (Reactor != null)
            {
                Reactor.OnAlarm += HandleAlarm;
                Reactor.OnReactorTrip += HandleTrip;
            }
            
            // Initial update
            UpdateAllComponents();
        }
        
        private void Update()
        {
            // Rate-limited data updates
            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdateAllComponents();
                _lastUpdateTime = Time.time;
            }
            
            // Alarm flash update
            if (_hasUnacknowledgedAlarms)
            {
                if (Time.time - _lastFlashTime >= 0.5f / AlarmFlashRate)
                {
                    _alarmFlashState = !_alarmFlashState;
                    _lastFlashTime = Time.time;
                    UpdateAlarmFlash();
                }
            }
        }
        
        private void OnDestroy()
        {
            if (Reactor != null)
            {
                Reactor.OnAlarm -= HandleAlarm;
                Reactor.OnReactorTrip -= HandleTrip;
            }
            
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion
        
        #region Component Registration
        
        /// <summary>
        /// Register all child mosaic components.
        /// </summary>
        private void RegisterComponents()
        {
            _components.Clear();
            
            // Find all components implementing IMosaicComponent
            var allComponents = GetComponentsInChildren<IMosaicComponent>(true);
            
            foreach (var component in allComponents)
            {
                _components.Add(component);
                component.Initialize(this);
            }
            
            Debug.Log($"[MosaicBoard] Registered {_components.Count} components");
        }
        
        /// <summary>
        /// Manually register a component.
        /// </summary>
        public void RegisterComponent(IMosaicComponent component)
        {
            if (!_components.Contains(component))
            {
                _components.Add(component);
                component.Initialize(this);
            }
        }
        
        /// <summary>
        /// Unregister a component.
        /// </summary>
        public void UnregisterComponent(IMosaicComponent component)
        {
            _components.Remove(component);
        }
        
        #endregion
        
        #region Data Updates
        
        /// <summary>
        /// Update all registered components with current data.
        /// </summary>
        private void UpdateAllComponents()
        {
            if (Reactor == null) return;
            
            foreach (var component in _components)
            {
                try
                {
                    component.UpdateData();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MosaicBoard] Component update error: {ex.Message}");
                }
            }
            
            OnDataUpdate?.Invoke();
        }
        
        /// <summary>
        /// Get a value with optional smoothing.
        /// </summary>
        public float GetSmoothedValue(string key, float currentValue)
        {
            if (!SmoothTransitions)
            {
                return currentValue;
            }
            
            if (_smoothedValues.TryGetValue(key, out float smoothed))
            {
                float newValue = Mathf.Lerp(smoothed, currentValue, SmoothFactor);
                _smoothedValues[key] = newValue;
                return newValue;
            }
            else
            {
                _smoothedValues[key] = currentValue;
                return currentValue;
            }
        }
        
        #endregion
        
        #region Data Access
        
        /// <summary>
        /// Get current value for a gauge type.
        /// </summary>
        public float GetValue(GaugeType type)
        {
            if (Reactor == null) return 0f;
            
            return type switch
            {
                GaugeType.NeutronPower => Reactor.NeutronPower * 100f,
                GaugeType.ThermalPower => Reactor.ThermalPower_MWt,
                GaugeType.Tavg => Reactor.Tavg,
                GaugeType.Thot => Reactor.Thot,
                GaugeType.Tcold => Reactor.Tcold,
                GaugeType.DeltaT => Reactor.DeltaT,
                GaugeType.FuelCenterline => Reactor.FuelCenterline,
                GaugeType.TotalReactivity => Reactor.TotalReactivity,
                GaugeType.StartupRate => Reactor.StartupRate_DPM,
                GaugeType.ReactorPeriod => Reactor.ReactorPeriod,
                GaugeType.Boron => Reactor.Boron_ppm,
                GaugeType.Xenon => Reactor.Xenon_pcm,
                GaugeType.BankDPosition => Reactor.BankDPosition,
                GaugeType.FlowFraction => Reactor.FlowFraction * 100f,
                _ => 0f
            };
        }
        
        /// <summary>
        /// Get color for a value based on gauge thresholds.
        /// </summary>
        public Color GetValueColor(GaugeType type, float value)
        {
            var alarmState = GetAlarmState(type, value);
            
            return alarmState switch
            {
                AlarmState.Trip => TripColor,
                AlarmState.Alarm => AlarmColor,
                AlarmState.Warning => WarningColor,
                _ => NormalColor
            };
        }
        
        /// <summary>
        /// Get alarm state for a gauge value.
        /// </summary>
        public AlarmState GetAlarmState(GaugeType type, float value)
        {
            // Trip conditions
            if (Reactor != null && Reactor.IsTripped)
            {
                return AlarmState.Trip;
            }
            
            // Type-specific thresholds
            return type switch
            {
                GaugeType.NeutronPower when value > 109f => AlarmState.Alarm,
                GaugeType.NeutronPower when value > 105f => AlarmState.Warning,
                
                GaugeType.Tavg when value > 605f || value < 540f => AlarmState.Alarm,
                GaugeType.Tavg when value > 595f || value < 550f => AlarmState.Warning,
                
                GaugeType.Thot when value > 650f => AlarmState.Alarm,
                GaugeType.Thot when value > 630f => AlarmState.Warning,
                
                GaugeType.FuelCenterline when value > 4000f => AlarmState.Alarm,
                GaugeType.FuelCenterline when value > 3500f => AlarmState.Warning,
                
                GaugeType.TotalReactivity when value > 500f => AlarmState.Alarm,
                GaugeType.TotalReactivity when value > 100f => AlarmState.Warning,
                
                GaugeType.StartupRate when value > 2.0f => AlarmState.Alarm,
                GaugeType.StartupRate when value > 1.0f => AlarmState.Warning,
                
                GaugeType.ReactorPeriod when value > 0f && value < 10f => AlarmState.Alarm,
                GaugeType.ReactorPeriod when value > 0f && value < 30f => AlarmState.Warning,
                
                GaugeType.BankDPosition when value < 10f => AlarmState.Alarm,
                GaugeType.BankDPosition when value < 30f => AlarmState.Warning,
                
                GaugeType.FlowFraction when value < 87f => AlarmState.Alarm,
                GaugeType.FlowFraction when value < 90f => AlarmState.Warning,
                
                _ => AlarmState.Normal
            };
        }
        
        /// <summary>
        /// Get formatted string for a gauge value.
        /// </summary>
        public string GetFormattedValue(GaugeType type, float value)
        {
            return type switch
            {
                GaugeType.NeutronPower => $"{value:F2}%",
                GaugeType.ThermalPower => $"{value:F0} MWt",
                GaugeType.Tavg => $"{value:F1}°F",
                GaugeType.Thot => $"{value:F1}°F",
                GaugeType.Tcold => $"{value:F1}°F",
                GaugeType.DeltaT => $"{value:F1}°F",
                GaugeType.FuelCenterline => $"{value:F0}°F",
                GaugeType.TotalReactivity => $"{value:F1} pcm",
                GaugeType.StartupRate => $"{value:F2} DPM",
                // FIX v4.0.0: ReactorController returns float.MaxValue (not Infinity) when
                // subcritical. Catch MaxValue, NaN, and any absurdly large period (>10 billion sec).
                GaugeType.ReactorPeriod when float.IsInfinity(value) || float.IsNaN(value) || Mathf.Abs(value) > 1e10f => "∞",
                GaugeType.ReactorPeriod => $"{value:F1} s",
                GaugeType.Boron => $"{value:F0} ppm",
                GaugeType.Xenon => $"{value:F0} pcm",
                GaugeType.BankDPosition => $"{value:F0} steps",
                GaugeType.FlowFraction => $"{value:F1}%",
                _ => value.ToString("F2")
            };
        }
        
        /// <summary>
        /// Get gauge range for normalization.
        /// </summary>
        public (float min, float max) GetGaugeRange(GaugeType type)
        {
            return type switch
            {
                GaugeType.NeutronPower => (0f, 120f),
                GaugeType.ThermalPower => (0f, 4000f),
                GaugeType.Tavg => (500f, 650f),
                GaugeType.Thot => (500f, 700f),
                GaugeType.Tcold => (500f, 600f),
                GaugeType.DeltaT => (0f, 100f),
                GaugeType.FuelCenterline => (500f, 5000f),
                GaugeType.TotalReactivity => (-5000f, 1000f),
                GaugeType.StartupRate => (-5f, 5f),
                GaugeType.ReactorPeriod => (-1000f, 1000f),
                GaugeType.Boron => (0f, 2500f),
                GaugeType.Xenon => (-5000f, 0f),
                GaugeType.BankDPosition => (0f, 228f),
                GaugeType.FlowFraction => (0f, 100f),
                _ => (0f, 100f)
            };
        }
        
        /// <summary>
        /// Get normalized value (0-1) for a gauge.
        /// </summary>
        public float GetNormalizedValue(GaugeType type, float value)
        {
            var (min, max) = GetGaugeRange(type);
            return Mathf.InverseLerp(min, max, value);
        }
        
        #endregion
        
        #region Alarm Handling
        
        private void HandleAlarm(string message)
        {
            var alarm = new ActiveAlarm
            {
                Message = message,
                Severity = message.Contains("TRIP") ? AlarmState.Trip : AlarmState.Alarm,
                TimeActivated = Time.time,
                Acknowledged = false
            };
            
            _alarms.Add(alarm);
            _hasUnacknowledgedAlarms = true;
            
            // Limit alarm history
            while (_alarms.Count > 20)
            {
                _alarms.RemoveAt(0);
            }
            
            // Sound horn
            if (AlarmHornEnabled && AlarmHorn != null && !AlarmHorn.isPlaying)
            {
                AlarmHorn.Play();
            }
            
            OnAlarmStateChanged?.Invoke(true);
        }
        
        private void HandleTrip()
        {
            HandleAlarm("REACTOR TRIP");
        }
        
        private void UpdateAlarmFlash()
        {
            // Notify components of flash state change
            foreach (var component in _components)
            {
                if (component is IAlarmFlashReceiver flashReceiver)
                {
                    flashReceiver.OnAlarmFlash(_alarmFlashState);
                }
            }
        }
        
        /// <summary>
        /// Acknowledge all active alarms.
        /// </summary>
        public void AcknowledgeAlarms()
        {
            foreach (var alarm in _alarms)
            {
                alarm.Acknowledged = true;
            }
            
            _hasUnacknowledgedAlarms = false;
            _alarmFlashState = false;
            
            // Stop horn
            if (AlarmHorn != null && AlarmHorn.isPlaying)
            {
                AlarmHorn.Stop();
            }
            
            OnAlarmStateChanged?.Invoke(false);
        }
        
        /// <summary>
        /// Clear alarm history.
        /// </summary>
        public void ClearAlarms()
        {
            _alarms.Clear();
            _hasUnacknowledgedAlarms = false;
        }
        
        /// <summary>
        /// Get most recent alarm message.
        /// </summary>
        public string GetLatestAlarmMessage()
        {
            if (_alarms.Count == 0) return "NO ALARMS";
            return _alarms[_alarms.Count - 1].Message;
        }
        
        #endregion
        
        #region Control Passthrough
        
        /// <summary>
        /// Withdraw rods via board control.
        /// </summary>
        public void WithdrawRods()
        {
            Reactor?.WithdrawRods();
        }
        
        /// <summary>
        /// Insert rods via board control.
        /// </summary>
        public void InsertRods()
        {
            Reactor?.InsertRods();
        }
        
        /// <summary>
        /// Stop rod motion via board control.
        /// </summary>
        public void StopRods()
        {
            Reactor?.StopRods();
        }
        
        /// <summary>
        /// Trip reactor via board control.
        /// </summary>
        public void Trip()
        {
            Reactor?.Trip();
        }
        
        /// <summary>
        /// Reset trip via board control.
        /// </summary>
        public void ResetTrip()
        {
            Reactor?.ResetTrip();
        }
        
        /// <summary>
        /// Set time compression.
        /// </summary>
        public void SetTimeCompression(float factor)
        {
            if (Reactor != null)
            {
                Reactor.TimeCompression = factor;
            }
        }
        
        #endregion
    }
    
    #region Interfaces
    
    /// <summary>
    /// Interface for Mosaic Board components.
    /// </summary>
    public interface IMosaicComponent
    {
        /// <summary>Initialize with board reference</summary>
        void Initialize(MosaicBoard board);
        
        /// <summary>Update component data</summary>
        void UpdateData();
    }
    
    /// <summary>
    /// Interface for components that respond to alarm flash.
    /// </summary>
    public interface IAlarmFlashReceiver
    {
        /// <summary>Called when alarm flash state changes</summary>
        void OnAlarmFlash(bool flashOn);
    }
    
    /// <summary>
    /// Active alarm data.
    /// </summary>
    public class ActiveAlarm
    {
        public string Message;
        public AlarmState Severity;
        public float TimeActivated;
        public bool Acknowledged;
    }
    
    #endregion
}
