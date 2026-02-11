// CRITICAL: Master the Atom - Phase 2 Reactor Core
// ReactorCore.cs - Integrated Reactor Core Model
//
// Master integration module that coordinates:
//   - Point kinetics (neutron population dynamics)
//   - Fuel assemblies (temperature response)
//   - Control rod banks (reactivity control)
//   - Feedback mechanisms (Doppler, MTC, Boron, Xenon)
//   - Power calculation (neutron to thermal)
//
// This module owns the complete reactor physics loop:
//   1. Calculate total reactivity (feedback + rods)
//   2. Solve point kinetics for new neutron power
//   3. Update fuel temperatures
//   4. Update feedback based on new temperatures
//   5. Repeat
//
// Reference: Westinghouse 4-Loop PWR (3411 MWt)
//
// Gold Standard Architecture:
//   - Integrates all Phase 2 physics modules
//   - Engine calls Update(), reads all state
//   - Complete reactor dynamics in one module

using System;

namespace Critical.Physics
{
    /// <summary>
    /// Reactor core state for external access.
    /// </summary>
    public struct ReactorCoreState
    {
        // Power
        public float NeutronPower_frac;
        public float ThermalPower_frac;
        public float ThermalPower_MWt;
        
        // Temperatures
        public float Tavg_F;
        public float Thot_F;
        public float Tcold_F;
        public float FuelCenterline_F;
        public float EffectiveFuelTemp_F;
        
        // Reactivity (pcm)
        public float TotalReactivity;
        public float DopplerFeedback;
        public float MTCFeedback;
        public float BoronFeedback;
        public float XenonFeedback;
        public float RodReactivity;
        
        // Control rods
        public float BankDPosition;
        public float BankAPosition;
        public bool RodsTripped;
        
        // Kinetics
        public float ReactorPeriod_sec;
        public float StartupRate_DPM;
        public float Keff;
        
        // Chemistry
        public float Boron_ppm;
        public float Xenon_pcm;
        
        // Status
        public bool IsCritical;
        public bool IsOverpower;
        public bool IsTripCondition;
    }
    
    /// <summary>
    /// Integrated reactor core model.
    /// </summary>
    public class ReactorCore
    {
        #region Constants
        
        /// <summary>Subcriticality threshold in pcm</summary>
        public const float SUBCRITICAL_THRESHOLD_PCM = -100f;
        
        /// <summary>Criticality threshold in pcm</summary>
        public const float CRITICAL_THRESHOLD_PCM = 50f;
        
        /// <summary>Overpower trip setpoint (fraction of nominal)</summary>
        public const float OVERPOWER_TRIP = 1.18f;
        
        /// <summary>High flux trip setpoint (fraction of nominal)</summary>
        public const float HIGH_FLUX_TRIP = 1.09f;
        
        /// <summary>Low-low flow trip setpoint (fraction of nominal)</summary>
        public const float LOW_FLOW_TRIP = 0.87f;
        
        /// <summary>Minimum timestep for kinetics stability (seconds)</summary>
        public const float MIN_KINETICS_DT = 0.001f;
        
        /// <summary>Maximum timestep for kinetics stability (seconds)</summary>
        public const float MAX_KINETICS_DT = 0.1f;
        
        #endregion
        
        #region Sub-modules
        
        private FuelAssembly _avgFuel;          // Average fuel assembly
        private FuelAssembly _hotChannel;        // Hot channel (limiting)
        private ControlRodBank _rods;            // Control rod banks
        private PowerCalculator _power;          // Power conversion
        private FeedbackCalculator _feedback;    // Reactivity feedback
        
        #endregion
        
        #region Instance State
        
        // Kinetics state
        private float[] _precursorConc;  // Delayed neutron precursors
        private float _neutronPower_frac;
        
        // Core conditions
        private float _boron_ppm;
        private float _xenon_pcm;
        private float _tavg_F;
        private float _thot_F;
        private float _tcold_F;
        private float _flowFraction;
        
        // Simulation time
        private float _simulationTime_sec;
        private float _timeAtPower_hr;
        
        // Trip state
        private bool _tripped;
        private float _tripTime_sec;
        private float _preTripPower;
        
        // Xenon tracking
        private float _xenonEquilibrium_pcm;
        private float _lastPowerChange_hr;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>Average fuel assembly</summary>
        public FuelAssembly AverageFuel => _avgFuel;
        
        /// <summary>Hot channel fuel assembly</summary>
        public FuelAssembly HotChannel => _hotChannel;
        
        /// <summary>Control rod banks</summary>
        public ControlRodBank Rods => _rods;
        
        /// <summary>Power calculator</summary>
        public PowerCalculator Power => _power;
        
        /// <summary>Feedback calculator</summary>
        public FeedbackCalculator Feedback => _feedback;
        
        /// <summary>Current neutron power as fraction of nominal</summary>
        public float NeutronPower => _neutronPower_frac;
        
        /// <summary>Current thermal power as fraction of nominal</summary>
        public float ThermalPower => _power.ThermalPower;
        
        /// <summary>Average coolant temperature in °F</summary>
        public float Tavg => _tavg_F;
        
        /// <summary>Hot leg temperature in °F</summary>
        public float Thot => _thot_F;
        
        /// <summary>Cold leg temperature in °F</summary>
        public float Tcold => _tcold_F;
        
        /// <summary>Current boron concentration in ppm</summary>
        public float Boron_ppm => _boron_ppm;
        
        /// <summary>Current xenon reactivity in pcm</summary>
        public float Xenon_pcm => _xenon_pcm;
        
        /// <summary>True if reactor is tripped</summary>
        public bool IsTripped => _tripped;
        
        /// <summary>True if reactor is critical (within threshold)</summary>
        public bool IsCritical => Math.Abs(_feedback.TotalReactivity_pcm) < CRITICAL_THRESHOLD_PCM 
                                  && _neutronPower_frac > 1e-6f;
        
        /// <summary>True if reactor is subcritical</summary>
        public bool IsSubcritical => _feedback.TotalReactivity_pcm < SUBCRITICAL_THRESHOLD_PCM;
        
        /// <summary>True if reactor is supercritical</summary>
        public bool IsSupercritical => _feedback.TotalReactivity_pcm > CRITICAL_THRESHOLD_PCM;
        
        /// <summary>Simulation time in seconds</summary>
        public float SimulationTime => _simulationTime_sec;
        
        /// <summary>Time at significant power in hours (for xenon tracking)</summary>
        public float TimeAtPower_hr => _timeAtPower_hr;
        
        /// <summary>Coolant flow as fraction of nominal</summary>
        public float FlowFraction => _flowFraction;
        
        /// <summary>Effective multiplication factor</summary>
        public float Keff => FeedbackCalculator.ReactivityToKeff(_feedback.TotalReactivity_pcm);
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Create reactor core at initial conditions.
        /// </summary>
        /// <param name="initialTavg_F">Initial average coolant temperature</param>
        /// <param name="initialBoron_ppm">Initial boron concentration</param>
        /// <param name="rodsWithdrawn">True to start with rods out</param>
        public ReactorCore(float initialTavg_F = PlantConstants.T_AVG_NO_LOAD,
                          float initialBoron_ppm = 1500f,
                          bool rodsWithdrawn = false)
        {
            // Initialize sub-modules
            _avgFuel = FuelAssembly.CreateAverageAssembly(initialTavg_F, 0f);
            _hotChannel = FuelAssembly.CreateHotChannelAssembly(initialTavg_F, 0f, 2.0f);
            _rods = new ControlRodBank(rodsWithdrawn);
            _power = new PowerCalculator(1e-9f);  // Start at source level
            _feedback = new FeedbackCalculator(initialTavg_F, initialTavg_F, initialBoron_ppm);
            
            // Initialize kinetics state
            _neutronPower_frac = 1e-9f;  // Source level
            _precursorConc = ReactorKinetics.EquilibriumPrecursors(_neutronPower_frac);
            
            // Initialize conditions
            _boron_ppm = initialBoron_ppm;
            _xenon_pcm = 0f;
            _xenonEquilibrium_pcm = 0f;
            _tavg_F = initialTavg_F;
            _thot_F = initialTavg_F;
            _tcold_F = initialTavg_F;
            _flowFraction = 1f;
            
            // Initialize timing
            _simulationTime_sec = 0f;
            _timeAtPower_hr = 0f;
            _lastPowerChange_hr = 0f;
            
            // Initialize trip state
            _tripped = false;
            _tripTime_sec = 0f;
            _preTripPower = 0f;
        }
        
        #endregion
        
        #region Main Update Loop
        
        /// <summary>
        /// Update reactor core for one timestep.
        /// This is the main entry point called by the simulation engine.
        /// </summary>
        /// <param name="coolantInletTemp_F">Cold leg (inlet) temperature</param>
        /// <param name="flowFraction">Coolant flow as fraction of nominal</param>
        /// <param name="dt_sec">Time step in seconds</param>
        public void Update(float coolantInletTemp_F, float flowFraction, float dt_sec)
        {
            _simulationTime_sec += dt_sec;
            _tcold_F = coolantInletTemp_F;
            _flowFraction = Math.Max(0.03f, flowFraction);  // Min is natural circ
            
            // Subdivide timestep for kinetics stability
            float kineticsSteps = Math.Max(1f, dt_sec / MAX_KINETICS_DT);
            float kineticsdt = dt_sec / kineticsSteps;
            
            for (int i = 0; i < (int)kineticsSteps; i++)
            {
                UpdateKinetics(kineticsdt);
            }
            
            // Update thermal calculations (can use larger timestep)
            UpdateThermal(dt_sec);
            
            // Update rods
            _rods.Update(dt_sec);
            
            // Update xenon (slow process, full timestep OK)
            UpdateXenon(dt_sec);
            
            // Update power tracking
            if (_power.ThermalPower > 0.01f)  // Above 1%
            {
                _timeAtPower_hr += dt_sec / 3600f;
            }
            
            // Check for trips
            CheckTripConditions();
        }
        
        /// <summary>
        /// Update kinetics for sub-timestep.
        /// </summary>
        private void UpdateKinetics(float dt_sec)
        {
            // Net rod reactivity: deviation from all-rods-out reference
            // All rods in:  0 - 8600 = -8600 pcm (deeply subcritical)
            // All rods out: 8600 - 8600 = 0 pcm (at reference keff)
            // This correctly models rod insertion worth as negative reactivity
            float netRodReactivity = _rods.TotalRodReactivity - ControlRodBank.TOTAL_WORTH_PCM;
            
            // Update feedback with current conditions
            _feedback.Update(
                _avgFuel.EffectiveFuelTemp_F,
                _tavg_F,
                _boron_ppm,
                _xenon_pcm,
                netRodReactivity
            );
            
            float totalReactivity = _feedback.TotalReactivity_pcm;
            
            // Solve point kinetics
            float[] newPrecursors;
            float newPower = ReactorKinetics.PointKinetics(
                _neutronPower_frac,
                totalReactivity,
                _precursorConc,
                dt_sec,
                out newPrecursors
            );
            
            _neutronPower_frac = newPower;
            _precursorConc = newPrecursors;
            
            // Update power calculator
            _power.Update(_neutronPower_frac, dt_sec);
        }
        
        /// <summary>
        /// Update thermal calculations.
        /// </summary>
        private void UpdateThermal(float dt_sec)
        {
            // Calculate hot leg temperature from power and flow
            // ΔT = Q / (ṁ × cp)
            // At 100% power, 100% flow: ΔT = 61°F
            float thermalPower = _power.ThermalPower;
            float deltaT = PlantConstants.CORE_DELTA_T * thermalPower / _flowFraction;
            
            _thot_F = _tcold_F + deltaT;
            _tavg_F = (_thot_F + _tcold_F) / 2f;
            
            // Update fuel assemblies
            _avgFuel.Update(thermalPower, _tavg_F, _flowFraction, dt_sec);
            _hotChannel.Update(thermalPower, _tavg_F, _flowFraction, dt_sec);
        }
        
        /// <summary>
        /// Update xenon concentration.
        /// </summary>
        private void UpdateXenon(float dt_sec)
        {
            // Calculate equilibrium xenon at current power
            _xenonEquilibrium_pcm = ReactorKinetics.XenonEquilibrium(_power.ThermalPower);
            
            // Update xenon using rate equation
            float xenonRate = ReactorKinetics.XenonRate(_xenon_pcm, _power.ThermalPower);
            _xenon_pcm += xenonRate * (dt_sec / 3600f);  // Convert to hours
            
            // Clamp to reasonable range
            _xenon_pcm = Math.Max(-5000f, Math.Min(_xenon_pcm, 0f));
        }
        
        #endregion
        
        #region Trip Logic
        
        /// <summary>
        /// Check for automatic trip conditions.
        /// </summary>
        private void CheckTripConditions()
        {
            if (_tripped) return;  // Already tripped
            
            bool shouldTrip = false;
            
            // High flux trip
            if (_power.IndicatedPower > HIGH_FLUX_TRIP)
            {
                shouldTrip = true;
            }
            
            // Overpower ΔT trip (simplified)
            float actualDeltaT = _thot_F - _tcold_F;
            float nominalDeltaT = PlantConstants.CORE_DELTA_T * _power.ThermalPower;
            if (actualDeltaT > nominalDeltaT * 1.2f && _power.ThermalPower > 0.25f)
            {
                shouldTrip = true;
            }
            
            // Low flow trip (only at power)
            if (_flowFraction < LOW_FLOW_TRIP && _power.ThermalPower > 0.1f)
            {
                shouldTrip = true;
            }
            
            if (shouldTrip)
            {
                Trip();
            }
        }
        
        /// <summary>
        /// Initiate reactor trip.
        /// </summary>
        public void Trip()
        {
            if (_tripped) return;
            
            _tripped = true;
            _tripTime_sec = _simulationTime_sec;
            _preTripPower = _power.ThermalPower;
            
            // Trip the rods
            _rods.Trip();
        }
        
        /// <summary>
        /// Reset trip condition (requires all rods in and deliberate action).
        /// </summary>
        public bool ResetTrip()
        {
            if (!_rods.AllRodsIn) return false;
            if (_neutronPower_frac > 0.01f) return false;  // Must be below 1%
            
            _tripped = false;
            _rods.ResetTrip();
            return true;
        }
        
        #endregion
        
        #region Control Methods
        
        /// <summary>
        /// Set boron concentration.
        /// </summary>
        public void SetBoron(float boron_ppm)
        {
            _boron_ppm = Math.Max(0f, boron_ppm);
        }
        
        /// <summary>
        /// Change boron concentration (for dilution/boration).
        /// </summary>
        public void ChangeBoron(float delta_ppm)
        {
            _boron_ppm = Math.Max(0f, _boron_ppm + delta_ppm);
        }
        
        /// <summary>
        /// Set coolant flow fraction.
        /// </summary>
        public void SetFlow(float flowFraction)
        {
            _flowFraction = Math.Max(0.03f, Math.Min(flowFraction, 1.2f));
        }
        
        /// <summary>
        /// Withdraw control rods in sequence.
        /// </summary>
        public void WithdrawRods()
        {
            if (_tripped) return;
            _rods.WithdrawInSequence();
        }
        
        /// <summary>
        /// Insert control rods in sequence.
        /// </summary>
        public void InsertRods()
        {
            if (_tripped) return;
            _rods.InsertInSequence();
        }
        
        /// <summary>
        /// Stop all rod motion.
        /// </summary>
        public void StopRods()
        {
            _rods.StopAllBanks();
        }
        
        #endregion
        
        #region State Access
        
        /// <summary>
        /// Get complete reactor core state.
        /// </summary>
        public ReactorCoreState GetState()
        {
            return new ReactorCoreState
            {
                NeutronPower_frac = _neutronPower_frac,
                ThermalPower_frac = _power.ThermalPower,
                ThermalPower_MWt = _power.ThermalPower_MWt,
                
                Tavg_F = _tavg_F,
                Thot_F = _thot_F,
                Tcold_F = _tcold_F,
                FuelCenterline_F = _avgFuel.CenterlineTemp_F,
                EffectiveFuelTemp_F = _avgFuel.EffectiveFuelTemp_F,
                
                TotalReactivity = _feedback.TotalReactivity_pcm,
                DopplerFeedback = _feedback.DopplerFeedback_pcm,
                MTCFeedback = _feedback.MTCFeedback_pcm,
                BoronFeedback = _feedback.BoronFeedback_pcm,
                XenonFeedback = _feedback.XenonFeedback_pcm,
                RodReactivity = _rods.TotalRodReactivity,
                
                BankDPosition = _rods.BankDPosition,
                BankAPosition = _rods.BankAPosition,
                RodsTripped = _tripped,
                
                ReactorPeriod_sec = _power.ReactorPeriod_sec,
                StartupRate_DPM = _power.StartupRate_DPM,
                Keff = FeedbackCalculator.ReactivityToKeff(_feedback.TotalReactivity_pcm),
                
                Boron_ppm = _boron_ppm,
                Xenon_pcm = _xenon_pcm,
                
                IsCritical = IsCritical,
                IsOverpower = _power.OverpowerAlarm,
                IsTripCondition = _tripped
            };
        }
        
        /// <summary>
        /// Get decay heat power if reactor were tripped now.
        /// </summary>
        public float GetDecayHeatPower_MWt(float timeAfterTrip_sec)
        {
            if (!_tripped) return 0f;
            return ReactorKinetics.DecayHeatPower(timeAfterTrip_sec);
        }
        
        #endregion
        
        #region Initialization Methods
        
        /// <summary>
        /// Initialize to HZP (Hot Zero Power) conditions.
        /// </summary>
        public void InitializeToHZP()
        {
            _tavg_F = PlantConstants.T_AVG_NO_LOAD;
            _thot_F = PlantConstants.T_AVG_NO_LOAD;
            _tcold_F = PlantConstants.T_AVG_NO_LOAD;
            _neutronPower_frac = 1e-9f;
            _power.SetPower(1e-9f);
            _precursorConc = ReactorKinetics.EquilibriumPrecursors(_neutronPower_frac);
            _xenon_pcm = 0f;
            _tripped = false;
            _avgFuel.ResetToCold(_tavg_F);
            _hotChannel.ResetToCold(_tavg_F);
            
            // Rods stay at current position (default = all in from constructor)
            // Update feedback so subcriticality is correctly reflected
            float netRodReactivity = _rods.TotalRodReactivity - ControlRodBank.TOTAL_WORTH_PCM;
            _feedback.Update(_tavg_F, _tavg_F, _boron_ppm, _xenon_pcm, netRodReactivity);
        }
        
        /// <summary>
        /// Initialize to specified power level with equilibrium conditions.
        /// </summary>
        public void InitializeToEquilibrium(float powerFraction)
        {
            powerFraction = Math.Max(0f, Math.Min(powerFraction, 1f));
            
            // 1. Set temperatures at power
            float deltaT = PlantConstants.CORE_DELTA_T * powerFraction;
            _tcold_F = PlantConstants.T_COLD;
            _thot_F = _tcold_F + deltaT;
            _tavg_F = (_thot_F + _tcold_F) / 2f;
            
            // 2. Set power and kinetics state
            _neutronPower_frac = powerFraction;
            _power.SetPower(powerFraction);
            _precursorConc = ReactorKinetics.EquilibriumPrecursors(_neutronPower_frac);
            
            // 3. Set equilibrium xenon
            _xenon_pcm = ReactorKinetics.XenonEquilibrium(powerFraction);
            
            // 4. Withdraw all rods for equilibrium power operation
            _rods.SetAllBankPositions(ControlRodBank.STEPS_TOTAL);
            _tripped = false;
            
            // 5. Stabilize fuel temperatures (100 iterations × 1s >> 7s time constant)
            for (int i = 0; i < 100; i++)
            {
                _avgFuel.Update(powerFraction, _tavg_F, 1f, 1f);
                _hotChannel.Update(powerFraction, _tavg_F, 1f, 1f);
            }
            
            // 6. Net rod reactivity at equilibrium (all rods out = 0)
            float netRodReactivity = _rods.TotalRodReactivity - ControlRodBank.TOTAL_WORTH_PCM;
            
            // 7. Find critical boron via iteration
            //    Uses FeedbackCalculator.EstimateCriticalBoron which iterates
            //    to account for boron-dependent MTC
            _boron_ppm = _feedback.EstimateCriticalBoron(
                _avgFuel.EffectiveFuelTemp_F, _tavg_F, _xenon_pcm, netRodReactivity);
            
            // 8. Refine: iterate feedback↔boron a few more times for convergence
            for (int i = 0; i < 5; i++)
            {
                _feedback.Update(
                    _avgFuel.EffectiveFuelTemp_F, _tavg_F, _boron_ppm,
                    _xenon_pcm, netRodReactivity);
                
                float residual = _feedback.TotalReactivity_pcm;
                if (Math.Abs(residual) < 1f) break;
                
                // Adjust boron to cancel residual reactivity
                _boron_ppm -= residual / PlantConstants.BORON_WORTH;
                _boron_ppm = Math.Max(0f, _boron_ppm);
            }
            
            // 9. Final feedback update at converged conditions
            _feedback.Update(
                _avgFuel.EffectiveFuelTemp_F, _tavg_F, _boron_ppm,
                _xenon_pcm, netRodReactivity);
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate reactor core model.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: Initial HZP conditions
            var core = new ReactorCore(PlantConstants.T_AVG_NO_LOAD, 1500f, false);
            if (!core.IsSubcritical) valid = false;  // Should be subcritical with rods in
            
            // Test 2: Withdrawal should add reactivity vs all-rods-in
            float rho_allIn = core._feedback.TotalReactivity_pcm;
            core._rods.SetAllBankPositions(228);  // Full out
            core.Update(557f, 1f, 0.1f);
            if (core._feedback.TotalReactivity_pcm <= rho_allIn) valid = false;  // Should have gained reactivity
            
            // Test 3: Trip should insert rods
            core.Trip();
            for (int i = 0; i < 30; i++) core.Update(557f, 1f, 0.1f);  // 3 seconds
            if (!core._rods.AllRodsIn) valid = false;
            
            // Test 4: Initialize to power should give correct temperatures
            core = new ReactorCore();
            core.InitializeToEquilibrium(1f);
            if (Math.Abs(core._tavg_F - PlantConstants.T_AVG) > 5f) valid = false;
            
            // Test 5: Xenon should be present at power
            if (core._xenon_pcm >= -1000f) valid = false;  // Should be ~ -2750 pcm
            
            // Test 6: Doppler feedback should be present at power
            if (core._feedback.DopplerFeedback_pcm >= 0f) valid = false;  // Should be negative
            
            return valid;
        }
        
        #endregion
    }
}
