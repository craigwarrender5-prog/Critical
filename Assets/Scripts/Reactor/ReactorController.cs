// CRITICAL: Master the Atom - Phase 2 Reactor Controller
// ReactorController.cs - Unity MonoBehaviour Interface
//
// Bridges Unity game engine to reactor physics:
//   - Manages ReactorCore physics module
//   - Handles time compression (1x to 10,000x)
//   - Provides operator interface for rod control
//   - Manages startup/shutdown sequencing
//   - Exposes state for Mosaic Board visualization
//
// Reference: Westinghouse 4-Loop PWR (3411 MWt)
//
// Gold Standard Architecture:
//   - Physics module (ReactorCore) owns all behavior
//   - This MonoBehaviour is pure coordinator
//   - No physics calculations in this file
//
// CHANGE: v4.0.0 — Added GetBankDirection() and StopBank() passthroughs
//         for rod control panel UI. Pure delegation, no physics changes.

using System;
using UnityEngine;

namespace Critical.Controllers
{
    using Physics;
    
    /// <summary>
    /// Reactor operating mode enumeration.
    /// </summary>
    public enum ReactorMode
    {
        /// <summary>Cold shutdown, all rods in, no heat</summary>
        ColdShutdown,
        
        /// <summary>Hot Zero Power, critical at 557°F</summary>
        HotZeroPower,
        
        /// <summary>Power ascension phase</summary>
        PowerAscension,
        
        /// <summary>Steady state power operation</summary>
        PowerOperation,
        
        /// <summary>Tripped - automatic shutdown</summary>
        Tripped,
        
        /// <summary>Manual shutdown in progress</summary>
        Shutdown
    }
    
    /// <summary>
    /// Rod control mode enumeration.
    /// </summary>
    public enum RodControlMode
    {
        /// <summary>Manual rod control</summary>
        Manual,
        
        /// <summary>Automatic rod control maintaining Tavg</summary>
        Automatic
    }
    
    /// <summary>
    /// Unity MonoBehaviour controller for reactor operations.
    /// Coordinates physics with game engine timing and UI.
    /// </summary>
    public class ReactorController : MonoBehaviour
    {
        #region Unity Inspector Fields
        
        [Header("Time Control")]
        [Tooltip("Time compression factor (1 = real-time, 10000 = max)")]
        [Range(1f, 10000f)]
        public float TimeCompression = 1f;
        
        [Tooltip("Maximum physics timestep to maintain stability")]
        public float MaxPhysicsTimestep = 0.1f;
        
        [Header("Startup Configuration")]
        [Tooltip("Initial boron concentration (ppm)")]
        public float InitialBoron_ppm = 1500f;
        
        [Tooltip("Coolant inlet temperature (°F)")]
        public float CoolantInletTemp_F = 557f;
        
        [Tooltip("RCP flow fraction (0-1)")]
        [Range(0f, 1f)]
        public float FlowFraction = 1f;
        
        [Header("Automatic Control Settings")]
        [Tooltip("Rod control mode")]
        public RodControlMode ControlMode = RodControlMode.Manual;
        
        [Tooltip("Tavg setpoint for automatic control (°F)")]
        public float TavgSetpoint_F = 588f;
        
        [Tooltip("Tavg deadband for automatic control (°F)")]
        public float TavgDeadband_F = 1.5f;
        
        [Header("Debug")]
        [Tooltip("Log physics updates to console")]
        public bool DebugLogging = false;
        
        #endregion
        
        #region Events
        
        /// <summary>Fired when reactor trips</summary>
        public event Action OnReactorTrip;
        
        /// <summary>Fired when reactor achieves criticality</summary>
        public event Action OnCriticality;
        
        /// <summary>Fired when reactor reaches target power</summary>
        public event Action<float> OnPowerReached;
        
        /// <summary>Fired when alarm condition occurs</summary>
        public event Action<string> OnAlarm;
        
        /// <summary>Fired each physics update with new state</summary>
        public event Action<ReactorCoreState> OnStateUpdate;
        
        #endregion
        
        #region Private Fields
        
        private ReactorCore _core;
        private ReactorMode _mode = ReactorMode.ColdShutdown;
        private bool _wasCritical = false;
        private float _targetPower = 0f;
        private bool _powerAscensionActive = false;
        private float _powerRampRate = 0.03f; // 3%/min default
        
        // Alarm state tracking
        private bool _highPowerAlarmActive = false;
        private bool _highTempAlarmActive = false;
        private bool _rodBottomAlarmActive = false;
        
        // Statistics
        private float _simulationTime = 0f;
        private int _physicsIterations = 0;
        
        #endregion
        
        #region Public Properties - State
        
        /// <summary>Current reactor operating mode</summary>
        public ReactorMode Mode => _mode;
        
        /// <summary>Current reactor state snapshot</summary>
        public ReactorCoreState State => _core?.GetState() ?? default;
        
        /// <summary>Neutron power fraction (0-1+)</summary>
        public float NeutronPower => _core?.NeutronPower ?? 0f;
        
        /// <summary>Thermal power fraction (0-1+)</summary>
        public float ThermalPower => _core?.ThermalPower ?? 0f;
        
        /// <summary>Thermal power in MWt</summary>
        public float ThermalPower_MWt => _core?.Power?.ThermalPower_MWt ?? 0f;
        
        /// <summary>Average coolant temperature (°F)</summary>
        public float Tavg => _core?.Tavg ?? CoolantInletTemp_F;
        
        /// <summary>Hot leg temperature (°F)</summary>
        public float Thot => _core?.Thot ?? CoolantInletTemp_F;
        
        /// <summary>Cold leg temperature (°F)</summary>
        public float Tcold => _core?.Tcold ?? CoolantInletTemp_F;
        
        /// <summary>Core delta-T (°F)</summary>
        public float DeltaT => Thot - Tcold;
        
        /// <summary>Fuel centerline temperature (°F)</summary>
        public float FuelCenterline => _core?.AverageFuel?.CenterlineTemp_F ?? CoolantInletTemp_F;
        
        /// <summary>Hot channel fuel centerline (°F)</summary>
        public float HotChannelCenterline => _core?.HotChannel?.CenterlineTemp_F ?? CoolantInletTemp_F;
        
        /// <summary>Boron concentration (ppm)</summary>
        public float Boron_ppm => _core?.Boron_ppm ?? InitialBoron_ppm;
        
        /// <summary>Xenon reactivity (pcm)</summary>
        public float Xenon_pcm => _core?.Xenon_pcm ?? 0f;
        
        /// <summary>Total reactivity (pcm)</summary>
        public float TotalReactivity => _core?.Feedback?.TotalReactivity_pcm ?? 0f;
        
        /// <summary>Control bank D position (steps, 0-228)</summary>
        public float BankDPosition => _core?.Rods?.BankDPosition ?? 0f;
        
        /// <summary>Control bank A position (steps, 0-228)</summary>
        public float BankAPosition => _core?.Rods?.BankAPosition ?? 0f;
        
        /// <summary>Reactor period (seconds, negative = decreasing power)</summary>
        public float ReactorPeriod => _core?.Power?.ReactorPeriod_sec ?? float.PositiveInfinity;
        
        /// <summary>Startup rate (decades per minute)</summary>
        public float StartupRate_DPM => _core?.Power?.StartupRate_DPM ?? 0f;
        
        /// <summary>Effective multiplication factor</summary>
        public float Keff => _core?.Keff ?? 1f;
        
        /// <summary>Is reactor tripped?</summary>
        public bool IsTripped => _core?.IsTripped ?? false;
        
        /// <summary>Is reactor critical?</summary>
        public bool IsCritical => _core?.IsCritical ?? false;
        
        /// <summary>Is reactor subcritical?</summary>
        public bool IsSubcritical => _core?.IsSubcritical ?? true;
        
        /// <summary>Total simulation time (seconds)</summary>
        public float SimulationTime => _simulationTime;
        
        /// <summary>Target power for ascension (fraction)</summary>
        public float TargetPower => _targetPower;
        
        /// <summary>Is power ascension active?</summary>
        public bool IsPowerAscensionActive => _powerAscensionActive;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeCore();
        }
        
        private void Update()
        {
            if (_core == null) return;
            
            // Calculate physics timestep with time compression
            float realDt = Time.deltaTime;
            float simDt = realDt * TimeCompression;
            
            // Subdivide for stability if needed
            int iterations = Mathf.CeilToInt(simDt / MaxPhysicsTimestep);
            float subDt = simDt / iterations;
            
            for (int i = 0; i < iterations; i++)
            {
                UpdatePhysics(subDt);
                _physicsIterations++;
            }
            
            _simulationTime += simDt;
            
            // Update mode and check alarms
            UpdateMode();
            CheckAlarms();
            
            // Handle automatic rod control
            if (ControlMode == RodControlMode.Automatic && !IsTripped)
            {
                UpdateAutomaticRodControl();
            }
            
            // Handle power ascension
            if (_powerAscensionActive)
            {
                UpdatePowerAscension(simDt);
            }
            
            // Fire state update event
            OnStateUpdate?.Invoke(State);
            
            if (DebugLogging && _physicsIterations % 60 == 0)
            {
                LogState();
            }
        }
        
        private void OnDestroy()
        {
            _core = null;
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the reactor core physics.
        /// </summary>
        private void InitializeCore()
        {
            _core = new ReactorCore();
            _core.SetBoron(InitialBoron_ppm);
            _mode = ReactorMode.ColdShutdown;
            
            if (DebugLogging)
            {
                Debug.Log("[ReactorController] Core initialized - Cold Shutdown");
            }
        }
        
        /// <summary>
        /// Initialize reactor to Hot Zero Power conditions.
        /// </summary>
        public void InitializeToHZP()
        {
            if (_core == null) InitializeCore();
            
            _core.InitializeToHZP();
            CoolantInletTemp_F = 557f;
            _mode = ReactorMode.HotZeroPower;
            _wasCritical = false;
            
            if (DebugLogging)
            {
                Debug.Log("[ReactorController] Initialized to HZP");
            }
        }
        
        /// <summary>
        /// Initialize reactor to specified power with equilibrium xenon.
        /// </summary>
        /// <param name="powerFraction">Target power fraction (0-1)</param>
        public void InitializeToPower(float powerFraction)
        {
            if (_core == null) InitializeCore();
            
            _core.InitializeToEquilibrium(powerFraction);
            _mode = ReactorMode.PowerOperation;
            _wasCritical = true;
            _targetPower = powerFraction;
            
            // Set Tavg setpoint based on power (program follows power)
            TavgSetpoint_F = CalculateTavgProgram(powerFraction);
            
            if (DebugLogging)
            {
                Debug.Log($"[ReactorController] Initialized to {powerFraction * 100f:F1}% power");
            }
        }
        
        /// <summary>
        /// Reset the reactor to cold shutdown.
        /// </summary>
        public void Reset()
        {
            InitializeCore();
            _simulationTime = 0f;
            _physicsIterations = 0;
            _powerAscensionActive = false;
            _targetPower = 0f;
            _wasCritical = false;
            _highPowerAlarmActive = false;
            _highTempAlarmActive = false;
            _rodBottomAlarmActive = false;
        }
        
        #endregion
        
        #region Rod Control
        
        /// <summary>
        /// Withdraw rods in sequence (automatic bank selection).
        /// </summary>
        public void WithdrawRods()
        {
            if (_core == null || IsTripped) return;
            _core.WithdrawRods();
            
            if (DebugLogging)
            {
                Debug.Log("[ReactorController] Rod withdrawal initiated");
            }
        }
        
        /// <summary>
        /// Insert rods in sequence (automatic bank selection).
        /// </summary>
        public void InsertRods()
        {
            if (_core == null || IsTripped) return;
            _core.InsertRods();
            
            if (DebugLogging)
            {
                Debug.Log("[ReactorController] Rod insertion initiated");
            }
        }
        
        /// <summary>
        /// Stop all rod motion.
        /// </summary>
        public void StopRods()
        {
            if (_core == null) return;
            _core.StopRods();
            
            if (DebugLogging)
            {
                Debug.Log("[ReactorController] Rod motion stopped");
            }
        }
        
        /// <summary>
        /// Withdraw a specific bank.
        /// </summary>
        /// <param name="bankIndex">Bank index (0-7: SA, SB, SC, SD, D, C, B, A)</param>
        public void WithdrawBank(int bankIndex)
        {
            if (_core == null || IsTripped) return;
            _core.Rods?.WithdrawBank((RodBank)bankIndex);
        }
        
        /// <summary>
        /// Insert a specific bank.
        /// </summary>
        /// <param name="bankIndex">Bank index (0-7: SA, SB, SC, SD, D, C, B, A)</param>
        public void InsertBank(int bankIndex)
        {
            if (_core == null || IsTripped) return;
            _core.Rods?.InsertBank((RodBank)bankIndex);
        }
        
        /// <summary>
        /// Get position of a specific bank.
        /// </summary>
        /// <param name="bankIndex">Bank index (0-7)</param>
        /// <returns>Bank position in steps (0-228)</returns>
        public float GetBankPosition(int bankIndex)
        {
            return _core?.Rods?.GetBankPosition(bankIndex) ?? 0f;
        }
        
        /// <summary>
        /// Get name of a specific bank.
        /// </summary>
        /// <param name="bankIndex">Bank index (0-7)</param>
        /// <returns>Bank name (SA, SB, SC, SD, D, C, B, A)</returns>
        public string GetBankName(int bankIndex)
        {
            return ControlRodBank.GetBankName((RodBank)bankIndex);
        }
        
        /// <summary>
        /// Get motion direction of a specific bank.
        /// </summary>
        /// <param name="bankIndex">Bank index (0-7)</param>
        /// <returns>RodDirection (Stationary, Withdrawing, Inserting)</returns>
        /// ADDED: v4.0.0 — Passthrough for rod control panel UI
        public RodDirection GetBankDirection(int bankIndex)
        {
            return _core?.Rods?.GetBankDirection((RodBank)bankIndex) ?? RodDirection.Stationary;
        }
        
        /// <summary>
        /// Stop motion of a specific bank.
        /// </summary>
        /// <param name="bankIndex">Bank index (0-7)</param>
        /// ADDED: v4.0.0 — Passthrough for rod control panel UI
        public void StopBank(int bankIndex)
        {
            if (_core == null) return;
            _core.Rods?.StopBank((RodBank)bankIndex);
        }
        
        #endregion
        
        #region Trip Control
        
        /// <summary>
        /// Manually trip the reactor.
        /// </summary>
        public void Trip()
        {
            if (_core == null || IsTripped) return;
            
            _core.Trip();
            _mode = ReactorMode.Tripped;
            _powerAscensionActive = false;
            
            OnReactorTrip?.Invoke();
            OnAlarm?.Invoke("REACTOR TRIP");
            
            if (DebugLogging)
            {
                Debug.Log("[ReactorController] REACTOR TRIP");
            }
        }
        
        /// <summary>
        /// Reset trip after conditions allow.
        /// Requires rods fully inserted and power below 1%.
        /// </summary>
        /// <returns>True if trip reset successful</returns>
        public bool ResetTrip()
        {
            if (_core == null) return false;
            
            bool success = _core.ResetTrip();
            
            if (success)
            {
                _mode = ReactorMode.HotZeroPower;
                
                if (DebugLogging)
                {
                    Debug.Log("[ReactorController] Trip reset successful");
                }
            }
            
            return success;
        }
        
        #endregion
        
        #region Boron Control
        
        /// <summary>
        /// Set boron concentration directly.
        /// </summary>
        /// <param name="ppm">Boron concentration in ppm</param>
        public void SetBoron(float ppm)
        {
            if (_core == null) return;
            _core.SetBoron(ppm);
            
            if (DebugLogging)
            {
                Debug.Log($"[ReactorController] Boron set to {ppm:F0} ppm");
            }
        }
        
        /// <summary>
        /// Change boron concentration by specified amount.
        /// </summary>
        /// <param name="deltaPpm">Change in ppm (negative = dilution)</param>
        public void ChangeBoron(float deltaPpm)
        {
            if (_core == null) return;
            _core.ChangeBoron(deltaPpm);
            
            if (DebugLogging)
            {
                Debug.Log($"[ReactorController] Boron changed by {deltaPpm:F0} ppm, now {Boron_ppm:F0} ppm");
            }
        }
        
        /// <summary>
        /// Calculate boron change needed for target reactivity.
        /// </summary>
        /// <param name="targetReactivity_pcm">Desired reactivity change in pcm</param>
        /// <returns>Boron change needed in ppm</returns>
        public float CalculateBoronChange(float targetReactivity_pcm)
        {
            return FeedbackCalculator.BoronChangeForReactivity(targetReactivity_pcm);
        }
        
        #endregion
        
        #region Power Ascension
        
        /// <summary>
        /// Begin power ascension to target power.
        /// </summary>
        /// <param name="targetPowerFraction">Target power (0-1)</param>
        /// <param name="rampRate">Ramp rate in fraction per minute (default 0.03 = 3%/min)</param>
        public void BeginPowerAscension(float targetPowerFraction, float rampRate = 0.03f)
        {
            if (_core == null || IsTripped) return;
            
            _targetPower = Mathf.Clamp01(targetPowerFraction);
            _powerRampRate = Mathf.Clamp(rampRate, 0.01f, 0.05f);
            _powerAscensionActive = true;
            _mode = ReactorMode.PowerAscension;
            
            if (DebugLogging)
            {
                Debug.Log($"[ReactorController] Power ascension to {_targetPower * 100f:F0}% at {_powerRampRate * 100f:F1}%/min");
            }
        }
        
        /// <summary>
        /// Stop power ascension at current power.
        /// </summary>
        public void StopPowerAscension()
        {
            _powerAscensionActive = false;
            _targetPower = ThermalPower;
            
            if (ThermalPower > 0.05f)
            {
                _mode = ReactorMode.PowerOperation;
            }
            
            if (DebugLogging)
            {
                Debug.Log($"[ReactorController] Power ascension stopped at {ThermalPower * 100f:F1}%");
            }
        }
        
        /// <summary>
        /// Update power ascension with dilution.
        /// </summary>
        private void UpdatePowerAscension(float dt_sec)
        {
            if (!_powerAscensionActive) return;
            
            float currentPower = ThermalPower;
            float powerError = _targetPower - currentPower;
            
            // Target reached (within 0.5%)
            if (Mathf.Abs(powerError) < 0.005f)
            {
                _powerAscensionActive = false;
                _mode = ReactorMode.PowerOperation;
                OnPowerReached?.Invoke(currentPower);
                
                if (DebugLogging)
                {
                    Debug.Log($"[ReactorController] Target power {_targetPower * 100f:F0}% reached");
                }
                return;
            }
            
            // Calculate power rate needed (fraction/sec)
            float dt_min = dt_sec / 60f;
            float desiredPowerChange = _powerRampRate * dt_min;
            
            // If increasing power, dilute boron to add reactivity
            if (powerError > 0 && currentPower < _targetPower)
            {
                // Use FeedbackCalculator.EstimatePowerDefect for accurate power defect
                // estimation including Doppler and MTC components (Issue #20 fix)
                float reactivityNeeded = -FeedbackCalculator.EstimatePowerDefect(
                    currentPower, currentPower + desiredPowerChange, Boron_ppm);
                // EstimatePowerDefect returns negative (feedback opposes power increase),
                // so negate to get the positive reactivity we need to add
                float boronChange = CalculateBoronChange(-reactivityNeeded); // Negative for dilution
                
                // Limit dilution rate to realistic CVCS capability (~5 ppm/min)
                float maxDilutionRate = 5f; // ppm/min
                float maxDilution = maxDilutionRate * dt_min;
                boronChange = Mathf.Max(boronChange, -maxDilution);
                
                ChangeBoron(boronChange);
            }
        }
        
        #endregion
        
        #region Automatic Rod Control
        
        /// <summary>
        /// Update automatic rod control to maintain Tavg.
        /// </summary>
        private void UpdateAutomaticRodControl()
        {
            if (_mode != ReactorMode.PowerOperation) return;
            
            float tavgError = Tavg - TavgSetpoint_F;
            
            // Outside deadband, adjust rods
            if (tavgError > TavgDeadband_F)
            {
                // Tavg too high - insert rods
                InsertRods();
            }
            else if (tavgError < -TavgDeadband_F)
            {
                // Tavg too low - withdraw rods
                WithdrawRods();
            }
            else
            {
                // Within deadband - stop rods
                StopRods();
            }
        }
        
        /// <summary>
        /// Calculate Tavg program setpoint based on power.
        /// </summary>
        /// <param name="powerFraction">Current power fraction</param>
        /// <returns>Target Tavg in °F</returns>
        public float CalculateTavgProgram(float powerFraction)
        {
            // Westinghouse 4-loop Tavg program:
            // At 0% power: 557°F
            // At 100% power: 588°F
            // Linear between
            const float T_ZERO_POWER = 557f;
            const float T_FULL_POWER = 588f;
            
            return T_ZERO_POWER + (T_FULL_POWER - T_ZERO_POWER) * Mathf.Clamp01(powerFraction);
        }
        
        #endregion
        
        #region Physics Update
        
        /// <summary>
        /// Update reactor physics for one timestep.
        /// </summary>
        private void UpdatePhysics(float dt_sec)
        {
            _core?.Update(CoolantInletTemp_F, FlowFraction, dt_sec);
            
            // Check for criticality transition
            bool isCriticalNow = IsCritical;
            if (isCriticalNow && !_wasCritical)
            {
                OnCriticality?.Invoke();
                
                if (DebugLogging)
                {
                    Debug.Log("[ReactorController] CRITICALITY ACHIEVED");
                }
            }
            _wasCritical = isCriticalNow;
        }
        
        /// <summary>
        /// Update operating mode based on state.
        /// </summary>
        private void UpdateMode()
        {
            if (IsTripped)
            {
                _mode = ReactorMode.Tripped;
                return;
            }
            
            // Mode transitions based on power
            if (_mode == ReactorMode.ColdShutdown && Tavg > 350f)
            {
                _mode = ReactorMode.HotZeroPower;
            }
            else if (_mode == ReactorMode.HotZeroPower && ThermalPower > 0.02f)
            {
                _mode = _powerAscensionActive ? ReactorMode.PowerAscension : ReactorMode.PowerOperation;
            }
            else if (_mode == ReactorMode.PowerAscension && !_powerAscensionActive)
            {
                _mode = ReactorMode.PowerOperation;
            }
            else if (_mode == ReactorMode.PowerOperation && ThermalPower < 0.02f)
            {
                _mode = ReactorMode.HotZeroPower;
            }
        }
        
        #endregion
        
        #region Alarms
        
        /// <summary>
        /// Check for alarm conditions.
        /// </summary>
        private void CheckAlarms()
        {
            // High power alarm (105%)
            bool highPowerNow = ThermalPower > 1.05f;
            if (highPowerNow && !_highPowerAlarmActive)
            {
                _highPowerAlarmActive = true;
                OnAlarm?.Invoke("HIGH REACTOR POWER");
            }
            else if (!highPowerNow && _highPowerAlarmActive)
            {
                _highPowerAlarmActive = false;
            }
            
            // High fuel temperature alarm (>3000°F)
            bool highTempNow = HotChannelCenterline > 3000f;
            if (highTempNow && !_highTempAlarmActive)
            {
                _highTempAlarmActive = true;
                OnAlarm?.Invoke("HIGH FUEL TEMPERATURE");
            }
            else if (!highTempNow && _highTempAlarmActive)
            {
                _highTempAlarmActive = false;
            }
            
            // Rod bottom alarm
            bool rodBottomNow = _core?.Rods?.RodBottomAlarm ?? false;
            if (rodBottomNow && !_rodBottomAlarmActive)
            {
                _rodBottomAlarmActive = true;
                OnAlarm?.Invoke("ROD BOTTOM");
            }
            else if (!rodBottomNow && _rodBottomAlarmActive)
            {
                _rodBottomAlarmActive = false;
            }
        }
        
        #endregion
        
        #region Diagnostics
        
        /// <summary>
        /// Log current state to console.
        /// </summary>
        private void LogState()
        {
            Debug.Log($"[Reactor] Mode={_mode} | P_n={NeutronPower * 100f:F2}% P_th={ThermalPower * 100f:F2}% | " +
                      $"Tavg={Tavg:F1}°F Thot={Thot:F1}°F | " +
                      $"ρ={TotalReactivity:F1}pcm | B={Boron_ppm:F0}ppm | " +
                      $"D={BankDPosition:F0} steps | T={_simulationTime:F0}s");
        }
        
        /// <summary>
        /// Get formatted status string for UI display.
        /// </summary>
        public string GetStatusText()
        {
            string modeText = _mode switch
            {
                ReactorMode.ColdShutdown => "COLD SHUTDOWN",
                ReactorMode.HotZeroPower => "HOT ZERO POWER",
                ReactorMode.PowerAscension => $"POWER ASCENSION → {_targetPower * 100f:F0}%",
                ReactorMode.PowerOperation => "POWER OPERATION",
                ReactorMode.Tripped => "REACTOR TRIPPED",
                ReactorMode.Shutdown => "SHUTDOWN",
                _ => "UNKNOWN"
            };
            
            return $"{modeText}\n" +
                   $"Neutron Power: {NeutronPower * 100f:F2}%\n" +
                   $"Thermal Power: {ThermalPower_MWt:F0} MWt ({ThermalPower * 100f:F1}%)\n" +
                   $"Tavg: {Tavg:F1}°F | ΔT: {DeltaT:F1}°F\n" +
                   $"Reactivity: {TotalReactivity:F1} pcm\n" +
                   $"Boron: {Boron_ppm:F0} ppm | Xenon: {Xenon_pcm:F0} pcm\n" +
                   $"Bank D: {BankDPosition:F0} steps";
        }
        
        #endregion
        
        #region Validation (Editor Only)
        
        #if UNITY_EDITOR
        
        /// <summary>
        /// Run validation tests. Called from editor.
        /// </summary>
        [ContextMenu("Run Validation Tests")]
        public void RunValidationTests()
        {
            Debug.Log("=== ReactorController Validation ===");
            int passed = 0;
            int failed = 0;
            
            // Test 1: Initial state is cold shutdown
            Reset();
            if (_mode == ReactorMode.ColdShutdown)
            {
                Debug.Log("✓ Test 1: Initial mode is Cold Shutdown");
                passed++;
            }
            else
            {
                Debug.LogError($"✗ Test 1: Expected ColdShutdown, got {_mode}");
                failed++;
            }
            
            // Test 2: Initialize to HZP
            InitializeToHZP();
            if (_mode == ReactorMode.HotZeroPower && Mathf.Abs(Tavg - 557f) < 5f)
            {
                Debug.Log("✓ Test 2: HZP initialization correct");
                passed++;
            }
            else
            {
                Debug.LogError($"✗ Test 2: HZP failed - Mode={_mode}, Tavg={Tavg}");
                failed++;
            }
            
            // Test 3: Trip inserts rods
            Trip();
            if (IsTripped && _mode == ReactorMode.Tripped)
            {
                Debug.Log("✓ Test 3: Trip activates correctly");
                passed++;
            }
            else
            {
                Debug.LogError($"✗ Test 3: Trip failed - IsTripped={IsTripped}, Mode={_mode}");
                failed++;
            }
            
            // Test 4: Initialize to power
            InitializeToPower(1.0f);
            if (Mathf.Abs(ThermalPower - 1.0f) < 0.01f && Mathf.Abs(Tavg - 588f) < 5f)
            {
                Debug.Log($"✓ Test 4: 100% power initialization - Tavg={Tavg:F1}°F");
                passed++;
            }
            else
            {
                Debug.LogError($"✗ Test 4: Power init failed - P={ThermalPower}, Tavg={Tavg}");
                failed++;
            }
            
            // Test 5: Tavg program calculation
            float tavg50 = CalculateTavgProgram(0.5f);
            if (Mathf.Abs(tavg50 - 572.5f) < 1f)
            {
                Debug.Log($"✓ Test 5: Tavg program at 50% = {tavg50:F1}°F");
                passed++;
            }
            else
            {
                Debug.LogError($"✗ Test 5: Tavg program at 50% = {tavg50:F1}°F (expected 572.5°F)");
                failed++;
            }
            
            // Cleanup
            Reset();
            
            Debug.Log($"=== Validation Complete: {passed} passed, {failed} failed ===");
        }
        
        #endif
        
        #endregion
    }
}
