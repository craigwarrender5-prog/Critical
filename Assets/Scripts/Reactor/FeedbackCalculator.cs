// CRITICAL: Master the Atom - Phase 2 Reactor Core
// FeedbackCalculator.cs - Combined Reactivity Feedback
//
// Combines all reactivity feedback mechanisms into total feedback:
//   - Doppler (fuel temperature) - fast, always negative
//   - MTC (moderator temperature) - can be positive or negative
//   - Boron (soluble poison) - negative
//   - Xenon (fission product poison) - negative, slow dynamics
//   - Control rods - positive when withdrawn
//
// Reference: Westinghouse 4-Loop PWR
// Sources: NRC HRTD Chapter 4, FSAR Chapter 4
//
// Gold Standard Architecture:
//   - Wraps ReactorKinetics functions with state tracking
//   - Engine provides temperatures and boron, reads total feedback
//   - Historical tracking for feedback contribution analysis

using System;

namespace Critical.Physics
{
    /// <summary>
    /// Feedback calculator combining all reactivity mechanisms.
    /// Tracks individual contributions and calculates total feedback.
    /// </summary>
    public class FeedbackCalculator
    {
        #region Reference Conditions
        
        /// <summary>Reference fuel temperature in °F (HZP conditions)</summary>
        public const float REF_FUEL_TEMP_F = 557f;
        
        /// <summary>Reference moderator temperature in °F (HZP Tavg)</summary>
        public const float REF_MOD_TEMP_F = 557f;
        
        /// <summary>Reference boron concentration in ppm (HZP critical)</summary>
        public const float REF_BORON_PPM = 1500f;
        
        /// <summary>Reference xenon reactivity in pcm (HZP equilibrium)</summary>
        public const float REF_XENON_PCM = 0f;  // No xenon at HZP startup
        
        #endregion
        
        #region Feedback Coefficients
        
        /// <summary>Doppler coefficient in pcm/√°R (always negative)</summary>
        public const float ALPHA_DOPPLER = -100f;
        
        /// <summary>Boron worth in pcm/ppm (negative)</summary>
        public const float ALPHA_BORON = -9f;
        
        #endregion
        
        #region Instance State
        
        // Current conditions
        private float _fuelTemp_F;
        private float _moderatorTemp_F;
        private float _boron_ppm;
        private float _xenonReactivity_pcm;
        private float _rodReactivity_pcm;
        
        // Reference conditions (may differ from constants for specific scenario)
        private float _refFuelTemp_F;
        private float _refModTemp_F;
        private float _refBoron_ppm;
        
        // Individual feedback contributions (pcm)
        private float _dopplerFeedback_pcm;
        private float _mtcFeedback_pcm;
        private float _boronFeedback_pcm;
        private float _xenonFeedback_pcm;
        private float _totalFeedback_pcm;
        
        // MTC (varies with boron)
        private float _currentMTC;
        
        // Power defect tracking
        private float _powerDefect_pcm;  // Total feedback from 0% to current power
        
        #endregion
        
        #region Public Properties
        
        /// <summary>Current fuel temperature in °F</summary>
        public float FuelTemp_F => _fuelTemp_F;
        
        /// <summary>Current moderator temperature in °F</summary>
        public float ModeratorTemp_F => _moderatorTemp_F;
        
        /// <summary>Current boron concentration in ppm</summary>
        public float Boron_ppm => _boron_ppm;
        
        /// <summary>Current xenon reactivity in pcm (negative)</summary>
        public float XenonReactivity_pcm => _xenonReactivity_pcm;
        
        /// <summary>Current control rod reactivity in pcm</summary>
        public float RodReactivity_pcm => _rodReactivity_pcm;
        
        /// <summary>Doppler feedback in pcm (negative for temperature increase)</summary>
        public float DopplerFeedback_pcm => _dopplerFeedback_pcm;
        
        /// <summary>Moderator temperature feedback in pcm</summary>
        public float MTCFeedback_pcm => _mtcFeedback_pcm;
        
        /// <summary>Boron feedback in pcm (negative for boron increase)</summary>
        public float BoronFeedback_pcm => _boronFeedback_pcm;
        
        /// <summary>Xenon feedback in pcm (always negative at power)</summary>
        public float XenonFeedback_pcm => _xenonFeedback_pcm;
        
        /// <summary>
        /// Total feedback reactivity in pcm (sum of all mechanisms).
        /// Does NOT include control rod reactivity - that's added separately.
        /// </summary>
        public float TotalFeedback_pcm => _totalFeedback_pcm;
        
        /// <summary>
        /// Total reactivity including rods in pcm.
        /// This is what goes to point kinetics.
        /// </summary>
        public float TotalReactivity_pcm => _totalFeedback_pcm + _rodReactivity_pcm;
        
        /// <summary>Current MTC in pcm/°F (varies with boron)</summary>
        public float CurrentMTC => _currentMTC;
        
        /// <summary>
        /// Power defect in pcm.
        /// Total negative feedback from 0% to current power level.
        /// </summary>
        public float PowerDefect_pcm => _powerDefect_pcm;
        
        /// <summary>
        /// True if total feedback is net stabilizing (negative for power increases).
        /// </summary>
        public bool IsStabilizing => (_dopplerFeedback_pcm + _mtcFeedback_pcm) < 0f;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Create feedback calculator with initial conditions.
        /// </summary>
        /// <param name="initialFuelTemp_F">Initial fuel temperature</param>
        /// <param name="initialModTemp_F">Initial moderator temperature</param>
        /// <param name="initialBoron_ppm">Initial boron concentration</param>
        public FeedbackCalculator(float initialFuelTemp_F = REF_FUEL_TEMP_F,
                                  float initialModTemp_F = REF_MOD_TEMP_F,
                                  float initialBoron_ppm = REF_BORON_PPM)
        {
            // Set reference conditions
            _refFuelTemp_F = REF_FUEL_TEMP_F;
            _refModTemp_F = REF_MOD_TEMP_F;
            _refBoron_ppm = REF_BORON_PPM;
            
            // Set initial conditions
            _fuelTemp_F = initialFuelTemp_F;
            _moderatorTemp_F = initialModTemp_F;
            _boron_ppm = initialBoron_ppm;
            _xenonReactivity_pcm = 0f;
            _rodReactivity_pcm = 0f;
            
            // Calculate initial feedback
            Calculate();
        }
        
        #endregion
        
        #region Update Methods
        
        /// <summary>
        /// Update feedback calculation with new conditions.
        /// </summary>
        /// <param name="fuelTemp_F">Current fuel temperature in °F</param>
        /// <param name="moderatorTemp_F">Current moderator temperature in °F</param>
        /// <param name="boron_ppm">Current boron concentration in ppm</param>
        /// <param name="xenonReactivity_pcm">Current xenon reactivity in pcm</param>
        /// <param name="rodReactivity_pcm">Current control rod reactivity in pcm</param>
        public void Update(float fuelTemp_F, float moderatorTemp_F, float boron_ppm,
                          float xenonReactivity_pcm, float rodReactivity_pcm)
        {
            _fuelTemp_F = fuelTemp_F;
            _moderatorTemp_F = moderatorTemp_F;
            _boron_ppm = Math.Max(0f, boron_ppm);
            _xenonReactivity_pcm = xenonReactivity_pcm;
            _rodReactivity_pcm = rodReactivity_pcm;
            
            Calculate();
        }
        
        /// <summary>
        /// Update only temperatures (for transient analysis).
        /// </summary>
        public void UpdateTemperatures(float fuelTemp_F, float moderatorTemp_F)
        {
            _fuelTemp_F = fuelTemp_F;
            _moderatorTemp_F = moderatorTemp_F;
            Calculate();
        }
        
        /// <summary>
        /// Update only boron concentration.
        /// </summary>
        public void UpdateBoron(float boron_ppm)
        {
            _boron_ppm = Math.Max(0f, boron_ppm);
            Calculate();
        }
        
        /// <summary>
        /// Update xenon reactivity (from ReactorKinetics.XenonTransient or XenonRate).
        /// </summary>
        public void UpdateXenon(float xenonReactivity_pcm)
        {
            _xenonReactivity_pcm = xenonReactivity_pcm;
            Calculate();
        }
        
        /// <summary>
        /// Update rod reactivity (from ControlRodBank).
        /// </summary>
        public void UpdateRods(float rodReactivity_pcm)
        {
            _rodReactivity_pcm = rodReactivity_pcm;
            // Note: Rod reactivity doesn't affect feedback calculation,
            // it's just tracked here for total reactivity
        }
        
        /// <summary>
        /// Core calculation method - evaluates all feedback mechanisms.
        /// </summary>
        private void Calculate()
        {
            // 1. Doppler feedback (uses ReactorKinetics)
            float fuelTempChange = _fuelTemp_F - _refFuelTemp_F;
            _dopplerFeedback_pcm = ReactorKinetics.DopplerReactivity(fuelTempChange, _refFuelTemp_F);
            
            // 2. MTC feedback
            // First get current MTC (depends on boron)
            _currentMTC = ReactorKinetics.ModeratorTempCoefficient(_boron_ppm);
            
            // Then calculate moderator feedback
            float modTempChange = _moderatorTemp_F - _refModTemp_F;
            _mtcFeedback_pcm = ReactorKinetics.ModeratorReactivity(modTempChange, _boron_ppm);
            
            // 3. Boron feedback
            float boronChange = _boron_ppm - _refBoron_ppm;
            _boronFeedback_pcm = ReactorKinetics.BoronReactivity(boronChange);
            
            // 4. Xenon feedback (already calculated externally, just store)
            _xenonFeedback_pcm = _xenonReactivity_pcm;
            
            // 5. Total feedback (excluding rods)
            _totalFeedback_pcm = _dopplerFeedback_pcm + _mtcFeedback_pcm + 
                                 _boronFeedback_pcm + _xenonFeedback_pcm;
            
            // 6. Power defect (Doppler + MTC contribution)
            // This represents the reactivity loss from HZP to current power
            _powerDefect_pcm = _dopplerFeedback_pcm + _mtcFeedback_pcm;
        }
        
        #endregion
        
        #region Reference Condition Methods
        
        /// <summary>
        /// Set reference conditions for feedback calculations.
        /// Feedback is calculated as deviation from these conditions.
        /// </summary>
        /// <param name="refFuelTemp_F">Reference fuel temperature</param>
        /// <param name="refModTemp_F">Reference moderator temperature</param>
        /// <param name="refBoron_ppm">Reference boron concentration</param>
        public void SetReferenceConditions(float refFuelTemp_F, float refModTemp_F, float refBoron_ppm)
        {
            _refFuelTemp_F = refFuelTemp_F;
            _refModTemp_F = refModTemp_F;
            _refBoron_ppm = refBoron_ppm;
            Calculate();
        }
        
        /// <summary>
        /// Set current conditions as the reference (zero feedback point).
        /// </summary>
        public void ZeroFeedbackAtCurrentConditions()
        {
            _refFuelTemp_F = _fuelTemp_F;
            _refModTemp_F = _moderatorTemp_F;
            _refBoron_ppm = _boron_ppm;
            Calculate();
        }
        
        #endregion
        
        #region Analysis Methods
        
        /// <summary>
        /// Calculate required boron change to compensate given reactivity.
        /// </summary>
        /// <param name="reactivityToCompensate_pcm">Reactivity to be compensated</param>
        /// <returns>Required boron change in ppm (positive = add boron)</returns>
        public static float BoronChangeForReactivity(float reactivityToCompensate_pcm)
        {
            // ρ = α_B × Δppm
            // Δppm = ρ / α_B
            // Note: α_B is negative, so positive reactivity requires negative boron change
            return reactivityToCompensate_pcm / ALPHA_BORON;
        }
        
        /// <summary>
        /// Calculate expected power defect for given power change.
        /// </summary>
        /// <param name="initialPower_frac">Initial power fraction</param>
        /// <param name="finalPower_frac">Final power fraction</param>
        /// <param name="boron_ppm">Boron concentration (affects MTC)</param>
        /// <returns>Expected power defect in pcm</returns>
        public static float EstimatePowerDefect(float initialPower_frac, float finalPower_frac, float boron_ppm)
        {
            // Simplified estimate based on typical coefficients
            // At 100% power: Tavg = 588°F, Tfuel = ~1800°F (average)
            // At 0% power: Tavg = 557°F, Tfuel = 557°F
            
            float powerChange = finalPower_frac - initialPower_frac;
            
            // Temperature changes
            float deltaTavg = powerChange * (PlantConstants.T_AVG - PlantConstants.T_AVG_NO_LOAD);
            float deltaTfuel = powerChange * (1800f - 557f);  // Rough fuel temp change
            
            // Doppler (uses effective fuel temp)
            float dopplerDefect = ReactorKinetics.DopplerReactivity(deltaTfuel * 0.4f, 557f);
            
            // MTC
            float mtc = ReactorKinetics.ModeratorTempCoefficient(boron_ppm);
            float mtcDefect = mtc * deltaTavg;
            
            return dopplerDefect + mtcDefect;
        }
        
        /// <summary>
        /// Calculate effective multiplication factor keff from reactivity.
        /// </summary>
        /// <param name="reactivity_pcm">Reactivity in pcm</param>
        /// <returns>keff value</returns>
        public static float ReactivityToKeff(float reactivity_pcm)
        {
            // ρ = (k-1)/k, so k = 1/(1-ρ)
            float rho = reactivity_pcm * 1e-5f;
            if (rho >= 1f) return float.MaxValue;
            return 1f / (1f - rho);
        }
        
        /// <summary>
        /// Calculate reactivity from keff.
        /// </summary>
        /// <param name="keff">Effective multiplication factor</param>
        /// <returns>Reactivity in pcm</returns>
        public static float KeffToReactivity(float keff)
        {
            if (keff <= 0f) return float.NegativeInfinity;
            float rho = (keff - 1f) / keff;
            return rho * 1e5f;  // Convert to pcm
        }
        
        /// <summary>
        /// Get feedback breakdown as formatted string.
        /// </summary>
        public string GetFeedbackBreakdown()
        {
            return $"Doppler: {_dopplerFeedback_pcm:+0;-0;0} pcm\n" +
                   $"MTC: {_mtcFeedback_pcm:+0;-0;0} pcm (α={_currentMTC:F1} pcm/°F)\n" +
                   $"Boron: {_boronFeedback_pcm:+0;-0;0} pcm\n" +
                   $"Xenon: {_xenonFeedback_pcm:+0;-0;0} pcm\n" +
                   $"Total Feedback: {_totalFeedback_pcm:+0;-0;0} pcm\n" +
                   $"Rods: {_rodReactivity_pcm:+0;-0;0} pcm\n" +
                   $"Net: {TotalReactivity_pcm:+0;-0;0} pcm";
        }
        
        #endregion
        
        #region Criticality Estimation
        
        /// <summary>
        /// Estimate critical boron concentration at given conditions.
        /// </summary>
        /// <param name="fuelTemp_F">Fuel temperature</param>
        /// <param name="modTemp_F">Moderator temperature</param>
        /// <param name="xenon_pcm">Xenon reactivity</param>
        /// <param name="rodWorth_pcm">Withdrawn rod worth</param>
        /// <returns>Estimated critical boron in ppm</returns>
        public float EstimateCriticalBoron(float fuelTemp_F, float modTemp_F, 
                                           float xenon_pcm, float rodWorth_pcm)
        {
            // At criticality: ρ_total = 0
            // ρ_Doppler + ρ_MTC + ρ_Boron + ρ_Xenon + ρ_Rods = 0
            // ρ_Boron = -(ρ_Doppler + ρ_MTC + ρ_Xenon + ρ_Rods)
            // α_B × (B - B_ref) = -other
            // B = B_ref - other / α_B
            
            float dopplerChange = fuelTemp_F - _refFuelTemp_F;
            float doppler = ReactorKinetics.DopplerReactivity(dopplerChange, _refFuelTemp_F);
            
            // MTC is boron-dependent, so iterate
            float criticalBoron = _refBoron_ppm;
            
            for (int iter = 0; iter < 5; iter++)
            {
                float mtc = ReactorKinetics.ModeratorTempCoefficient(criticalBoron);
                float modTempChange = modTemp_F - _refModTemp_F;
                float mtcEffect = mtc * modTempChange;
                
                float otherEffects = doppler + mtcEffect + xenon_pcm + rodWorth_pcm;
                float requiredBoronEffect = -otherEffects;
                
                criticalBoron = _refBoron_ppm + requiredBoronEffect / ALPHA_BORON;
                criticalBoron = Math.Max(0f, criticalBoron);
            }
            
            return criticalBoron;
        }
        
        /// <summary>
        /// Check if current conditions are near criticality.
        /// </summary>
        /// <param name="tolerance_pcm">Tolerance in pcm for "near critical"</param>
        /// <returns>True if within tolerance of critical</returns>
        public bool IsNearCritical(float tolerance_pcm = 100f)
        {
            return Math.Abs(TotalReactivity_pcm) <= tolerance_pcm;
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate feedback calculator.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: Zero conditions should give zero feedback
            var calc = new FeedbackCalculator(REF_FUEL_TEMP_F, REF_MOD_TEMP_F, REF_BORON_PPM);
            if (Math.Abs(calc.TotalFeedback_pcm) > 1f) valid = false;
            
            // Test 2: Fuel temperature increase should give negative Doppler
            calc.Update(REF_FUEL_TEMP_F + 100f, REF_MOD_TEMP_F, REF_BORON_PPM, 0f, 0f);
            if (calc.DopplerFeedback_pcm >= 0f) valid = false;
            
            // Test 3: At high boron, MTC should be positive (temp increase = positive reactivity)
            calc = new FeedbackCalculator(557f, 557f, 1500f);
            calc.Update(557f, 600f, 1500f, 0f, 0f);  // Increase mod temp
            // At 1500 ppm, MTC ≈ +5 pcm/°F, so 43°F increase ≈ +215 pcm
            if (calc.MTCFeedback_pcm <= 0f) valid = false;
            if (calc.CurrentMTC <= 0f) valid = false;
            
            // Test 4: At low boron, MTC should be negative
            calc = new FeedbackCalculator(557f, 557f, 100f);
            calc.Update(557f, 600f, 100f, 0f, 0f);
            if (calc.MTCFeedback_pcm >= 0f) valid = false;
            if (calc.CurrentMTC >= 0f) valid = false;
            
            // Test 5: Boron increase should give negative reactivity
            calc = new FeedbackCalculator(557f, 557f, 1000f);
            calc.Update(557f, 557f, 1100f, 0f, 0f);  // Add 100 ppm
            // -9 pcm/ppm × 100 ppm = -900 pcm
            if (calc.BoronFeedback_pcm >= 0f) valid = false;
            if (Math.Abs(calc.BoronFeedback_pcm + 900f) > 100f) valid = false;
            
            // Test 6: Total reactivity should include rods
            calc = new FeedbackCalculator();
            calc.Update(557f, 557f, 1500f, 0f, 1000f);  // 1000 pcm from rods
            if (Math.Abs(calc.TotalReactivity_pcm - 1000f) > 10f) valid = false;
            
            // Test 7: BoronChangeForReactivity
            float boronChange = BoronChangeForReactivity(-900f);  // Need -900 pcm
            // Should require +100 ppm (boron worth is -9 pcm/ppm)
            if (Math.Abs(boronChange - 100f) > 5f) valid = false;
            
            // Test 8: Keff conversion
            float keff = ReactivityToKeff(0f);
            if (Math.Abs(keff - 1f) > 0.001f) valid = false;
            
            keff = ReactivityToKeff(650f);  // 650 pcm = β
            // k = 1/(1-0.0065) ≈ 1.00654
            if (Math.Abs(keff - 1.00654f) > 0.001f) valid = false;
            
            return valid;
        }
        
        #endregion
    }
}
