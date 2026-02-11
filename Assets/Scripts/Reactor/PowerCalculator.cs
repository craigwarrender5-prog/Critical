// CRITICAL: Master the Atom - Phase 2 Reactor Core
// PowerCalculator.cs - Neutron Power to Thermal Power Conversion
//
// Models the thermal lag between neutron power and thermal power output.
// Neutron power changes instantaneously with reactivity, but thermal power
// lags due to fuel heat capacity (τ ~ 5-10 seconds).
//
// Key physics:
//   - Neutron power from point kinetics (instantaneous)
//   - Fuel temperature response (τ_fuel ~ 7 seconds)
//   - Thermal power transfer to coolant
//   - Power range instrumentation dynamics
//
// Reference: Westinghouse 4-Loop PWR (3411 MWt)
// Sources: NRC HRTD, FSAR Chapter 7
//
// Gold Standard Architecture:
//   - Module owns power conversion physics
//   - Engine provides neutron power, reads thermal power
//   - Instrumentation lag modeled separately from physical lag

using System;

namespace Critical.Physics
{
    /// <summary>
    /// Power calculator with neutron-to-thermal conversion and thermal lag.
    /// </summary>
    public class PowerCalculator
    {
        #region Constants
        
        /// <summary>Fuel thermal time constant in seconds</summary>
        public const float FUEL_THERMAL_TAU = 7.0f;
        
        /// <summary>Coolant transport time constant in seconds</summary>
        public const float COOLANT_TRANSPORT_TAU = 2.0f;
        
        /// <summary>Power range detector time constant in seconds</summary>
        public const float DETECTOR_TAU = 0.5f;
        
        /// <summary>Rate lag time constant for power rate (dP/dt) in seconds</summary>
        public const float RATE_LAG_TAU = 2.0f;
        
        /// <summary>Nominal full power in MWt — delegated to PlantConstants (Issue #18)</summary>
        public static float NOMINAL_POWER_MWT => PlantConstants.THERMAL_POWER_MWT;
        
        /// <summary>Maximum overpower limit (fraction of nominal)</summary>
        public const float OVERPOWER_LIMIT = 1.18f;
        
        /// <summary>Minimum detectable power (fraction of nominal)</summary>
        public const float MIN_POWER_FRACTION = 1e-9f;
        
        #endregion
        
        #region Power Range Constants
        
        /// <summary>Source range upper limit (counts per second, approximate)</summary>
        public const float SOURCE_RANGE_MAX_CPS = 1e6f;
        
        /// <summary>Intermediate range lower limit (fraction of nominal)</summary>
        public const float INTERMEDIATE_RANGE_MIN = 1e-8f;
        
        /// <summary>Intermediate range upper limit (fraction of nominal)</summary>
        public const float INTERMEDIATE_RANGE_MAX = 2e-1f;
        
        /// <summary>Power range lower limit (fraction of nominal)</summary>
        public const float POWER_RANGE_MIN = 1e-4f;
        
        /// <summary>Power range upper limit (fraction of nominal)</summary>
        public const float POWER_RANGE_MAX = 1.2f;
        
        #endregion
        
        #region Instance State
        
        // Core power values
        private float _neutronPower_frac;      // Instantaneous neutron power (fraction)
        private float _thermalPower_frac;      // Thermal power to coolant (fraction, lagged)
        private float _indicatedPower_frac;    // Indicated power from detectors (fraction)
        
        // Power derivatives
        private float _neutronPowerRate;       // dP/dt for neutron power (%/s)
        private float _thermalPowerRate;       // dP/dt for thermal power (%/s)
        private float _indicatedPowerRate;     // dP/dt for indicated power (%/s)
        
        // Historical values for rate calculation
        private float _prevNeutronPower;
        private float _prevThermalPower;
        private float _prevIndicatedPower;
        
        // Range indication
        private bool _sourceRangeValid;
        private bool _intermediateRangeValid;
        private bool _powerRangeValid;
        
        // Alarms
        private bool _overpowerAlarm;
        private bool _highPowerRate;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>Neutron power as fraction of nominal (0-1+)</summary>
        public float NeutronPower => _neutronPower_frac;
        
        /// <summary>Thermal power as fraction of nominal (0-1+), includes thermal lag</summary>
        public float ThermalPower => _thermalPower_frac;
        
        /// <summary>Indicated power from detectors, includes instrumentation lag</summary>
        public float IndicatedPower => _indicatedPower_frac;
        
        /// <summary>Neutron power in MWt</summary>
        public float NeutronPower_MWt => _neutronPower_frac * NOMINAL_POWER_MWT;
        
        /// <summary>Thermal power in MWt</summary>
        public float ThermalPower_MWt => _thermalPower_frac * NOMINAL_POWER_MWT;
        
        /// <summary>Indicated power in MWt</summary>
        public float IndicatedPower_MWt => _indicatedPower_frac * NOMINAL_POWER_MWT;
        
        /// <summary>Neutron power as percentage (0-100+)</summary>
        public float NeutronPower_Percent => _neutronPower_frac * 100f;
        
        /// <summary>Thermal power as percentage (0-100+)</summary>
        public float ThermalPower_Percent => _thermalPower_frac * 100f;
        
        /// <summary>Indicated power as percentage (0-100+)</summary>
        public float IndicatedPower_Percent => _indicatedPower_frac * 100f;
        
        /// <summary>Neutron power rate of change in %/second</summary>
        public float NeutronPowerRate => _neutronPowerRate;
        
        /// <summary>Thermal power rate of change in %/second</summary>
        public float ThermalPowerRate => _thermalPowerRate;
        
        /// <summary>Indicated power rate of change in %/second</summary>
        public float IndicatedPowerRate => _indicatedPowerRate;
        
        /// <summary>True if source range detectors are in range</summary>
        public bool SourceRangeValid => _sourceRangeValid;
        
        /// <summary>True if intermediate range detectors are in range</summary>
        public bool IntermediateRangeValid => _intermediateRangeValid;
        
        /// <summary>True if power range detectors are in range</summary>
        public bool PowerRangeValid => _powerRangeValid;
        
        /// <summary>True if overpower condition exists</summary>
        public bool OverpowerAlarm => _overpowerAlarm;
        
        /// <summary>True if high power rate condition exists</summary>
        public bool HighPowerRate => _highPowerRate;
        
        /// <summary>
        /// Startup rate in decades per minute (DPM).
        /// Positive = power increasing, negative = power decreasing.
        /// </summary>
        public float StartupRate_DPM
        {
            get
            {
                if (_neutronPower_frac < MIN_POWER_FRACTION) return 0f;
                // SUR = (dP/dt) / (P × ln(10)) in decades/second
                // Convert to DPM by × 60
                float sur_dps = _neutronPowerRate / (_neutronPower_frac * 100f * 2.303f);
                return sur_dps * 60f;
            }
        }
        
        /// <summary>
        /// Reactor period in seconds.
        /// Positive = power increasing, negative = power decreasing.
        /// </summary>
        public float ReactorPeriod_sec
        {
            get
            {
                if (Math.Abs(_neutronPowerRate) < 0.001f) return float.MaxValue;
                return _neutronPower_frac * 100f / _neutronPowerRate;
            }
        }
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Create power calculator with initial power level.
        /// </summary>
        /// <param name="initialPower_frac">Initial power as fraction of nominal</param>
        public PowerCalculator(float initialPower_frac = 0f)
        {
            _neutronPower_frac = Math.Max(MIN_POWER_FRACTION, initialPower_frac);
            _thermalPower_frac = _neutronPower_frac;
            _indicatedPower_frac = _neutronPower_frac;
            
            _prevNeutronPower = _neutronPower_frac;
            _prevThermalPower = _thermalPower_frac;
            _prevIndicatedPower = _indicatedPower_frac;
            
            _neutronPowerRate = 0f;
            _thermalPowerRate = 0f;
            _indicatedPowerRate = 0f;
            
            UpdateRangeIndication();
        }
        
        #endregion
        
        #region Update Methods
        
        /// <summary>
        /// Update power values with new neutron power.
        /// Applies thermal lag to calculate thermal and indicated power.
        /// </summary>
        /// <param name="neutronPower_frac">New neutron power as fraction of nominal</param>
        /// <param name="dt_sec">Time step in seconds</param>
        public void Update(float neutronPower_frac, float dt_sec)
        {
            // Store previous values for rate calculation
            _prevNeutronPower = _neutronPower_frac;
            _prevThermalPower = _thermalPower_frac;
            _prevIndicatedPower = _indicatedPower_frac;
            
            // Update neutron power (instantaneous)
            _neutronPower_frac = Math.Max(MIN_POWER_FRACTION, neutronPower_frac);
            
            // Apply fuel thermal lag to get thermal power
            // First-order lag: dT/dt = (T_target - T) / τ
            float thermalAlpha = Math.Min(dt_sec / FUEL_THERMAL_TAU, 1f);
            _thermalPower_frac += thermalAlpha * (_neutronPower_frac - _thermalPower_frac);
            
            // Apply detector lag to get indicated power
            float detectorAlpha = Math.Min(dt_sec / DETECTOR_TAU, 1f);
            _indicatedPower_frac += detectorAlpha * (_thermalPower_frac - _indicatedPower_frac);
            
            // Calculate power rates
            if (dt_sec > 0.001f)
            {
                float rawNeutronRate = (_neutronPower_frac - _prevNeutronPower) / dt_sec * 100f;
                float rawThermalRate = (_thermalPower_frac - _prevThermalPower) / dt_sec * 100f;
                float rawIndicatedRate = (_indicatedPower_frac - _prevIndicatedPower) / dt_sec * 100f;
                
                // Apply rate lag filter
                float rateAlpha = Math.Min(dt_sec / RATE_LAG_TAU, 1f);
                _neutronPowerRate += rateAlpha * (rawNeutronRate - _neutronPowerRate);
                _thermalPowerRate += rateAlpha * (rawThermalRate - _thermalPowerRate);
                _indicatedPowerRate += rateAlpha * (rawIndicatedRate - _indicatedPowerRate);
            }
            
            // Update range indication
            UpdateRangeIndication();
            
            // Check alarms
            CheckAlarms();
        }
        
        /// <summary>
        /// Update range indication flags based on current power.
        /// </summary>
        private void UpdateRangeIndication()
        {
            // Source range: very low power (startup)
            _sourceRangeValid = _neutronPower_frac < INTERMEDIATE_RANGE_MAX;
            
            // Intermediate range: 10^-8 to 0.2 (20%)
            _intermediateRangeValid = _neutronPower_frac >= INTERMEDIATE_RANGE_MIN 
                                   && _neutronPower_frac <= INTERMEDIATE_RANGE_MAX;
            
            // Power range: 10^-4 to 120%
            _powerRangeValid = _neutronPower_frac >= POWER_RANGE_MIN 
                            && _neutronPower_frac <= POWER_RANGE_MAX;
        }
        
        /// <summary>
        /// Check alarm conditions.
        /// </summary>
        private void CheckAlarms()
        {
            // Overpower alarm
            _overpowerAlarm = _indicatedPower_frac > OVERPOWER_LIMIT;
            
            // High power rate (> 5%/sec considered high)
            _highPowerRate = Math.Abs(_indicatedPowerRate) > 5f;
        }
        
        #endregion
        
        #region Auxiliary Methods
        
        /// <summary>
        /// Set power directly (for initialization or testing).
        /// Sets all power values equal (no lag).
        /// </summary>
        /// <param name="power_frac">Power as fraction of nominal</param>
        public void SetPower(float power_frac)
        {
            power_frac = Math.Max(MIN_POWER_FRACTION, power_frac);
            _neutronPower_frac = power_frac;
            _thermalPower_frac = power_frac;
            _indicatedPower_frac = power_frac;
            
            _prevNeutronPower = power_frac;
            _prevThermalPower = power_frac;
            _prevIndicatedPower = power_frac;
            
            _neutronPowerRate = 0f;
            _thermalPowerRate = 0f;
            _indicatedPowerRate = 0f;
            
            UpdateRangeIndication();
            CheckAlarms();
        }
        
        /// <summary>
        /// Calculate inverse count rate for 1/M plot during approach to criticality.
        /// </summary>
        /// <param name="countRate">Current count rate</param>
        /// <param name="initialCountRate">Initial count rate (baseline)</param>
        /// <returns>M value (multiplication factor)</returns>
        public static float CalculateMultiplication(float countRate, float initialCountRate)
        {
            if (initialCountRate <= 0) return 1f;
            return countRate / initialCountRate;
        }
        
        /// <summary>
        /// Estimate criticality from 1/M extrapolation.
        /// </summary>
        /// <param name="rodPositions">Array of rod positions at each measurement</param>
        /// <param name="invMValues">Array of 1/M values at each measurement</param>
        /// <returns>Estimated rod position at criticality (1/M = 0)</returns>
        public static float EstimateCriticalRodPosition(float[] rodPositions, float[] invMValues)
        {
            if (rodPositions == null || invMValues == null) return 0f;
            if (rodPositions.Length < 2 || rodPositions.Length != invMValues.Length) return 0f;
            
            // Linear extrapolation using last two points
            int n = rodPositions.Length;
            float x1 = rodPositions[n - 2];
            float x2 = rodPositions[n - 1];
            float y1 = invMValues[n - 2];
            float y2 = invMValues[n - 1];
            
            if (Math.Abs(y2 - y1) < 0.001f) return x2; // No change, can't extrapolate
            
            // Line: y = y1 + (y2-y1)/(x2-x1) × (x - x1)
            // At y = 0: x = x1 - y1 × (x2-x1)/(y2-y1)
            float slope = (y2 - y1) / (x2 - x1);
            float criticalPosition = x1 - y1 / slope;
            
            return criticalPosition;
        }
        
        /// <summary>
        /// Get appropriate nuclear instrument range string.
        /// </summary>
        public string GetActiveRange()
        {
            if (_neutronPower_frac < INTERMEDIATE_RANGE_MIN)
                return "SOURCE";
            else if (_neutronPower_frac < POWER_RANGE_MIN)
                return "INTERMEDIATE";
            else
                return "POWER";
        }
        
        /// <summary>
        /// Get power as exponential notation string (for source/intermediate range).
        /// </summary>
        public string GetPowerExponential()
        {
            if (_neutronPower_frac < 0.01f)
            {
                // Scientific notation for small powers
                int exp = (int)Math.Floor(Math.Log10(_neutronPower_frac));
                float mantissa = _neutronPower_frac / (float)Math.Pow(10, exp);
                return $"{mantissa:F2}E{exp}";
            }
            else
            {
                return $"{_neutronPower_frac * 100f:F2}%";
            }
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate power calculator calculations.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: Initial power should be equal across all values
            var calc = new PowerCalculator(0.5f);
            if (Math.Abs(calc.NeutronPower - 0.5f) > 0.001f) valid = false;
            if (Math.Abs(calc.ThermalPower - 0.5f) > 0.001f) valid = false;
            if (Math.Abs(calc.IndicatedPower - 0.5f) > 0.001f) valid = false;
            
            // Test 2: Thermal power should lag behind neutron power
            calc = new PowerCalculator(0.5f);
            calc.Update(1.0f, 1f);  // Step from 50% to 100%
            if (calc.ThermalPower >= calc.NeutronPower) valid = false;  // Should lag
            if (calc.ThermalPower <= 0.5f) valid = false;  // Should have moved
            
            // Test 3: After long time, thermal should equal neutron
            for (int i = 0; i < 100; i++)
            {
                calc.Update(1.0f, 1f);  // 100 seconds at 100%
            }
            if (Math.Abs(calc.ThermalPower - 1.0f) > 0.01f) valid = false;
            
            // Test 4: Power in MWt should match percentage
            calc.SetPower(0.75f);
            float expectedMWt = 0.75f * NOMINAL_POWER_MWT;
            if (Math.Abs(calc.ThermalPower_MWt - expectedMWt) > 1f) valid = false;
            
            // Test 5: Overpower alarm should trigger above limit
            calc.SetPower(OVERPOWER_LIMIT + 0.01f);
            calc.Update(OVERPOWER_LIMIT + 0.01f, 0.1f);
            if (!calc.OverpowerAlarm) valid = false;
            
            // Test 6: Range indication should be correct
            calc.SetPower(1e-9f);
            calc.Update(1e-9f, 0.1f);
            if (!calc.SourceRangeValid) valid = false;
            
            calc.SetPower(0.1f);
            calc.Update(0.1f, 0.1f);
            if (!calc.IntermediateRangeValid) valid = false;
            
            calc.SetPower(0.5f);
            calc.Update(0.5f, 0.1f);
            if (!calc.PowerRangeValid) valid = false;
            
            // Test 7: Reactor period calculation
            calc.SetPower(0.5f);
            calc.Update(0.51f, 1f);  // 1% increase in 1 second
            // Period should be roughly P/(dP/dt) = 50/1 = 50 seconds
            // Allow wide tolerance due to lag filters
            if (Math.Abs(calc.ReactorPeriod_sec) < 10f) valid = false;  // Shouldn't be too fast
            if (calc.ReactorPeriod_sec < 0) valid = false;  // Should be positive (increasing)
            
            return valid;
        }
        
        #endregion
    }
}
