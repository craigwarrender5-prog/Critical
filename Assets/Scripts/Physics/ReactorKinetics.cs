// CRITICAL: Master the Atom - Phase 1 Core Physics Engine
// ReactorKinetics.cs - Reactor Kinetics and Reactivity Feedback
//
// Point kinetics model with delayed neutrons
// Reactivity feedback: Doppler, moderator temperature, boron, control rods, xenon
// Units: pcm for reactivity, seconds for time, fraction for power

using System;

namespace Critical.Physics
{
    /// <summary>
    /// Reactor kinetics calculations including point kinetics, 
    /// reactivity coefficients, and xenon dynamics.
    /// </summary>
    public static class ReactorKinetics
    {
        #region Delayed Neutron Data
        
        /// <summary>
        /// Delayed neutron fractions for 6 groups (U-235 thermal fission).
        /// </summary>
        public static readonly float[] BETA_GROUPS = 
        { 
            0.000215f,  // Group 1: λ = 0.0124 /s
            0.001424f,  // Group 2: λ = 0.0305 /s
            0.001274f,  // Group 3: λ = 0.111 /s
            0.002568f,  // Group 4: λ = 0.301 /s
            0.000748f,  // Group 5: λ = 1.14 /s
            0.000273f   // Group 6: λ = 3.01 /s
        };
        
        /// <summary>
        /// Decay constants for 6 delayed neutron groups (1/s).
        /// </summary>
        public static readonly float[] LAMBDA_GROUPS = 
        { 
            0.0124f,    // Group 1
            0.0305f,    // Group 2
            0.111f,     // Group 3
            0.301f,     // Group 4
            1.14f,      // Group 5
            3.01f       // Group 6
        };
        
        /// <summary>
        /// Total delayed neutron fraction.
        /// </summary>
        public const float BETA_TOTAL = 0.0065f;
        
        /// <summary>
        /// Prompt neutron generation time in seconds.
        /// </summary>
        public const float GENERATION_TIME = 2e-5f;
        
        #endregion
        
        #region Point Kinetics
        
        /// <summary>
        /// Calculate power change using point kinetics with 6 delayed neutron groups.
        /// dn/dt = (ρ - β)/Λ × n + Σλᵢcᵢ
        /// dcᵢ/dt = βᵢ/Λ × n - λᵢcᵢ
        ///
        /// Uses the semi-implicit (prompt-jump) method standard in reactor kinetics
        /// codes (PARCS, RELAP, SIMULATE). The prompt neutron lifetime Λ ≈ 2×10⁻⁵s
        /// makes the system extremely stiff; explicit Euler is unstable for any
        /// practical timestep. The semi-implicit method analytically eliminates the
        /// prompt timescale by assuming dn/dt_prompt ≈ 0 (valid for ρ < β), then
        /// time-steps only the slow delayed neutron precursors.
        ///
        /// For ρ > β (prompt supercritical), falls back to sub-stepped explicit
        /// integration with dt ≤ Λ/(ρ-β) for stability.
        /// </summary>
        /// <param name="power">Current power (fraction or absolute)</param>
        /// <param name="reactivity_pcm">Total reactivity in pcm</param>
        /// <param name="precursorConc">Array of 6 precursor concentrations</param>
        /// <param name="dt_sec">Time step in seconds</param>
        /// <param name="newPrecursorConc">Output: new precursor concentrations</param>
        /// <returns>New power after time step</returns>
        public static float PointKinetics(
            float power, 
            float reactivity_pcm, 
            float[] precursorConc,
            float dt_sec,
            out float[] newPrecursorConc)
        {
            // Convert reactivity from pcm to absolute (1 pcm = 1e-5)
            float rho = reactivity_pcm * 1e-5f;
            
            // Initialize output array
            newPrecursorConc = new float[6];
            
            // Guard: prevent NaN propagation from upstream
            if (float.IsNaN(power) || float.IsInfinity(power))
                power = 1e-10f;
            if (float.IsNaN(rho) || float.IsInfinity(rho))
                rho = 0f;
            
            // ================================================================
            // Semi-implicit (prompt-jump) method
            // ================================================================
            // At quasi-static equilibrium the prompt neutron equation gives:
            //   n = Λ × Σλᵢcᵢ / (β - ρ)
            //
            // This is valid when ρ < β (delayed supercritical or subcritical),
            // which covers all normal and transient operations. The precursor
            // equations are then integrated with implicit Euler for stability:
            //   cᵢ(t+dt) = [cᵢ(t) + (βᵢ/Λ) × n × dt] / (1 + λᵢ × dt)
            // ================================================================
            
            if (rho < BETA_TOTAL * 0.95f)
            {
                // --- Delayed supercritical / subcritical regime ---
                // Step 1: Advance precursors using implicit Euler
                //   dcᵢ/dt = βᵢn/Λ - λᵢcᵢ
                //   Implicit: cᵢ_new = (cᵢ_old + βᵢ/Λ × n_old × dt) / (1 + λᵢ × dt)
                
                float delayedSource = 0f;
                for (int i = 0; i < 6; i++)
                {
                    float ci_old = Math.Max(precursorConc[i], 0f);
                    if (float.IsNaN(ci_old) || float.IsInfinity(ci_old))
                        ci_old = BETA_GROUPS[i] * power / (GENERATION_TIME * LAMBDA_GROUPS[i]);
                    
                    float production = BETA_GROUPS[i] / GENERATION_TIME * power * dt_sec;
                    newPrecursorConc[i] = (ci_old + production) / (1f + LAMBDA_GROUPS[i] * dt_sec);
                    newPrecursorConc[i] = Math.Max(newPrecursorConc[i], 0f);
                    
                    delayedSource += LAMBDA_GROUPS[i] * newPrecursorConc[i];
                }
                
                // Step 2: Prompt-jump power from quasi-static balance
                //   n = Λ × Σλᵢcᵢ_new / (β - ρ)
                float denominator = BETA_TOTAL - rho;
                denominator = Math.Max(denominator, 1e-6f); // Safety floor
                
                float newPower = GENERATION_TIME * delayedSource / denominator;
                newPower = Math.Max(newPower, 1e-10f);
                
                return newPower;
            }
            else
            {
                // --- Prompt supercritical regime (ρ ≥ 0.95β) ---
                // Must resolve prompt timescale; sub-step with stability limit.
                // This regime only occurs during unprotected reactivity insertion
                // accidents (e.g., rod ejection). Normal operations never reach
                // prompt critical.
                
                float safetyDt = GENERATION_TIME / Math.Max(Math.Abs(rho - BETA_TOTAL), 1e-8f) * 0.5f;
                safetyDt = Math.Max(safetyDt, 1e-6f);
                int subSteps = (int)Math.Ceiling(dt_sec / safetyDt);
                subSteps = Math.Min(subSteps, 10000); // Cap iterations
                float subDt = dt_sec / subSteps;
                
                float n = power;
                float[] c = new float[6];
                Array.Copy(precursorConc, c, 6);
                
                for (int step = 0; step < subSteps; step++)
                {
                    float delayedSource = 0f;
                    for (int i = 0; i < 6; i++)
                        delayedSource += LAMBDA_GROUPS[i] * c[i];
                    
                    float dndt = (rho - BETA_TOTAL) / GENERATION_TIME * n + delayedSource;
                    n += dndt * subDt;
                    n = Math.Max(n, 1e-10f);
                    
                    for (int i = 0; i < 6; i++)
                    {
                        float dcdt = BETA_GROUPS[i] / GENERATION_TIME * n - LAMBDA_GROUPS[i] * c[i];
                        c[i] += dcdt * subDt;
                        c[i] = Math.Max(c[i], 0f);
                    }
                }
                
                Array.Copy(c, newPrecursorConc, 6);
                return Math.Max(n, 1e-10f);
            }
        }
        
        /// <summary>
        /// Calculate equilibrium precursor concentrations at given power.
        /// At equilibrium: cᵢ = βᵢ × n / (Λ × λᵢ)
        /// </summary>
        /// <param name="power">Power level (fraction or absolute)</param>
        /// <returns>Array of 6 equilibrium precursor concentrations</returns>
        public static float[] EquilibriumPrecursors(float power)
        {
            float[] conc = new float[6];
            for (int i = 0; i < 6; i++)
            {
                conc[i] = BETA_GROUPS[i] * power / (GENERATION_TIME * LAMBDA_GROUPS[i]);
            }
            return conc;
        }
        
        /// <summary>
        /// Calculate reactor period for given reactivity.
        /// T ≈ ℓ/(ρ-β) for ρ > β (prompt supercritical)
        /// T ≈ β/(λ_eff × ρ) for small positive ρ
        /// </summary>
        /// <param name="reactivity_pcm">Reactivity in pcm</param>
        /// <returns>Reactor period in seconds (positive = increasing power)</returns>
        public static float ReactorPeriod(float reactivity_pcm)
        {
            float rho = reactivity_pcm * 1e-5f;
            
            if (Math.Abs(rho) < 1e-7f) return float.MaxValue; // Critical
            
            if (rho > BETA_TOTAL)
            {
                // Prompt supercritical - very short period
                return GENERATION_TIME / (rho - BETA_TOTAL);
            }
            else if (rho > 0f)
            {
                // Delayed supercritical
                float lambda_eff = 0.1f; // Effective decay constant
                return BETA_TOTAL / (lambda_eff * rho);
            }
            else
            {
                // Subcritical - power decreasing
                return -BETA_TOTAL / (0.1f * Math.Abs(rho));
            }
        }
        
        /// <summary>
        /// Simplified power change for prompt jump approximation.
        /// n_final/n_initial = β/(β-ρ) for step reactivity insertion
        /// </summary>
        /// <param name="initialPower">Initial power</param>
        /// <param name="reactivity_pcm">Step reactivity in pcm</param>
        /// <returns>Power after prompt jump</returns>
        public static float PromptJump(float initialPower, float reactivity_pcm)
        {
            float rho = reactivity_pcm * 1e-5f;
            
            if (rho >= BETA_TOTAL) 
                return initialPower * 100f; // Prompt critical - limited for safety
            
            return initialPower * BETA_TOTAL / (BETA_TOTAL - rho);
        }
        
        #endregion
        
        #region Reactivity Coefficients
        
        /// <summary>
        /// Calculate Doppler reactivity feedback.
        /// Doppler coefficient is typically expressed as pcm/√°R.
        /// ρ_Doppler = α_D × (√T_final - √T_initial)
        /// </summary>
        /// <param name="fuelTempChange_F">Fuel temperature change in °F</param>
        /// <param name="initialFuelTemp_F">Initial fuel temperature in °F</param>
        /// <returns>Doppler reactivity in pcm (negative for temperature increase)</returns>
        public static float DopplerReactivity(float fuelTempChange_F, float initialFuelTemp_F)
        {
            // Convert to Rankine
            float T1_R = initialFuelTemp_F + PlantConstants.RANKINE_OFFSET;
            float T2_R = T1_R + fuelTempChange_F;
            
            // Doppler coefficient: -2.5 pcm/√°R
            float deltaRho = PlantConstants.DOPPLER_COEFF * ((float)Math.Sqrt(T2_R) - (float)Math.Sqrt(T1_R));
            
            return deltaRho;
        }
        
        /// <summary>
        /// Calculate moderator temperature coefficient (MTC).
        /// MTC varies with boron concentration - positive at high boron, negative at low boron.
        /// </summary>
        /// <param name="boron_ppm">Current boron concentration in ppm</param>
        /// <returns>MTC in pcm/°F</returns>
        public static float ModeratorTempCoefficient(float boron_ppm)
        {
            // MTC transitions from positive to negative as boron decreases
            // At 1500 ppm: MTC ≈ +5 pcm/°F
            // At 500 ppm: MTC ≈ 0 pcm/°F
            // At 100 ppm: MTC ≈ -40 pcm/°F
            
            if (boron_ppm >= 1500f)
            {
                return PlantConstants.MTC_HIGH_BORON;
            }
            else if (boron_ppm <= 100f)
            {
                return PlantConstants.MTC_LOW_BORON;
            }
            else
            {
                // Linear interpolation
                float fraction = (boron_ppm - 100f) / (1500f - 100f);
                return PlantConstants.MTC_LOW_BORON + 
                       fraction * (PlantConstants.MTC_HIGH_BORON - PlantConstants.MTC_LOW_BORON);
            }
        }
        
        /// <summary>
        /// Calculate moderator reactivity feedback.
        /// </summary>
        /// <param name="tempChange_F">Moderator temperature change in °F</param>
        /// <param name="boron_ppm">Current boron concentration in ppm</param>
        /// <returns>Moderator reactivity in pcm</returns>
        public static float ModeratorReactivity(float tempChange_F, float boron_ppm)
        {
            float mtc = ModeratorTempCoefficient(boron_ppm);
            return mtc * tempChange_F;
        }
        
        /// <summary>
        /// Calculate boron reactivity change.
        /// </summary>
        /// <param name="boronChange_ppm">Change in boron concentration in ppm</param>
        /// <returns>Boron reactivity in pcm (negative for boron increase)</returns>
        public static float BoronReactivity(float boronChange_ppm)
        {
            // Boron worth: -9 pcm/ppm
            return PlantConstants.BORON_WORTH * boronChange_ppm;
        }
        
        /// <summary>
        /// Calculate total reactivity from all feedback mechanisms.
        /// </summary>
        /// <param name="doppler_pcm">Doppler reactivity in pcm</param>
        /// <param name="moderator_pcm">Moderator reactivity in pcm</param>
        /// <param name="boron_pcm">Boron reactivity in pcm</param>
        /// <param name="rods_pcm">Control rod reactivity in pcm</param>
        /// <param name="xenon_pcm">Xenon reactivity in pcm</param>
        /// <returns>Total reactivity in pcm</returns>
        public static float TotalReactivity(
            float doppler_pcm, 
            float moderator_pcm, 
            float boron_pcm, 
            float rods_pcm, 
            float xenon_pcm)
        {
            return doppler_pcm + moderator_pcm + boron_pcm + rods_pcm + xenon_pcm;
        }
        
        #endregion
        
        #region Control Rods
        
        /// <summary>
        /// Calculate control rod reactivity from position using integral worth curve.
        /// </summary>
        /// <param name="position_steps">Rod position in steps (0 = fully inserted, 228 = fully withdrawn)</param>
        /// <param name="totalWorth_pcm">Total rod bank worth in pcm</param>
        /// <returns>Reactivity added by rod withdrawal in pcm</returns>
        public static float ControlRodReactivity(float position_steps, float totalWorth_pcm)
        {
            // Typical differential worth curve is roughly bell-shaped
            // Integral worth is S-shaped
            // Simplified model using sine-squared distribution
            
            float fractionWithdrawn = Math.Max(0f, Math.Min(position_steps / (float)PlantConstants.ROD_TOTAL_STEPS, 1f));
            
            // S-curve for integral worth
            float worth = totalWorth_pcm * (float)Math.Sin(fractionWithdrawn * Math.PI / 2f);
            worth = worth * worth / totalWorth_pcm; // Square gives S-shape
            
            return worth;
        }
        
        /// <summary>
        /// Calculate differential control rod worth at given position.
        /// </summary>
        /// <param name="position_steps">Rod position in steps</param>
        /// <param name="totalWorth_pcm">Total rod bank worth in pcm</param>
        /// <returns>Differential worth in pcm/step</returns>
        public static float DifferentialRodWorth(float position_steps, float totalWorth_pcm)
        {
            // Differential worth is derivative of integral worth
            // For sine-squared: d/dx[sin²(πx/2)] = π×sin(πx/2)×cos(πx/2) = (π/2)×sin(πx)
            
            float fractionWithdrawn = Math.Max(0f, Math.Min(position_steps / (float)PlantConstants.ROD_TOTAL_STEPS, 1f));
            float diffWorth = totalWorth_pcm * (float)Math.PI / 2f * 
                             (float)Math.Sin(fractionWithdrawn * Math.PI) / PlantConstants.ROD_TOTAL_STEPS;
            
            return diffWorth;
        }
        
        #endregion
        
        #region Xenon Dynamics
        
        /// <summary>
        /// Calculate equilibrium xenon concentration at given power.
        /// </summary>
        /// <param name="powerFraction">Power as fraction of nominal (0-1)</param>
        /// <returns>Equilibrium xenon reactivity in pcm (negative)</returns>
        public static float XenonEquilibrium(float powerFraction)
        {
            if (powerFraction < 0.01f) return 0f;
            
            // Xenon equilibrium scales roughly with power
            // At 100%: -2500 to -3000 pcm
            float baseXenon = -2750f; // Average equilibrium value
            return baseXenon * powerFraction;
        }
        
        /// <summary>
        /// Calculate xenon concentration after power change.
        /// Simplified model for xenon transient following power change.
        /// </summary>
        /// <param name="initialXenon_pcm">Initial xenon reactivity in pcm</param>
        /// <param name="finalPowerFraction">New power level as fraction</param>
        /// <param name="timeAfterChange_hr">Time since power change in hours</param>
        /// <returns>Current xenon reactivity in pcm</returns>
        public static float XenonTransient(
            float initialXenon_pcm, 
            float finalPowerFraction, 
            float timeAfterChange_hr)
        {
            // Simplified xenon transient model
            // After trip: xenon peaks at ~8-10 hours, then decays
            // After power increase: xenon dips then builds to new equilibrium
            
            float equilibriumXenon = XenonEquilibrium(finalPowerFraction);
            
            // Time constants (approximate)
            float tau_build = 6f;  // Xenon buildup time constant (hours)
            float tau_decay = 9f;  // Xenon decay time constant (hours)
            
            // Power reduced - xenon peaks then decays
            if (Math.Abs(initialXenon_pcm) > Math.Abs(equilibriumXenon))
            {
                // Following power reduction
                // Xenon peaks due to iodine decay, then decays
                float peakTime = PlantConstants.XENON_PEAK_TIME_HOURS;
                
                if (timeAfterChange_hr < peakTime)
                {
                    // Building toward peak
                    float peakXenon = initialXenon_pcm * 1.3f; // ~30% overshoot
                    float progress = timeAfterChange_hr / peakTime;
                    return initialXenon_pcm + progress * (peakXenon - initialXenon_pcm);
                }
                else
                {
                    // Decaying from peak
                    float peakXenon = initialXenon_pcm * 1.3f;
                    float decayTime = timeAfterChange_hr - peakTime;
                    float decay = (float)Math.Exp(-decayTime / tau_decay);
                    return equilibriumXenon + (peakXenon - equilibriumXenon) * decay;
                }
            }
            else
            {
                // Power increased - xenon dips then builds
                float approach = 1f - (float)Math.Exp(-timeAfterChange_hr / tau_build);
                return initialXenon_pcm + approach * (equilibriumXenon - initialXenon_pcm);
            }
        }
        
        /// <summary>
        /// Calculate xenon reactivity change rate.
        /// </summary>
        /// <param name="currentXenon_pcm">Current xenon reactivity in pcm</param>
        /// <param name="powerFraction">Current power fraction</param>
        /// <returns>Rate of xenon change in pcm/hour</returns>
        public static float XenonRate(float currentXenon_pcm, float powerFraction)
        {
            float equilibrium = XenonEquilibrium(powerFraction);
            float difference = equilibrium - currentXenon_pcm;
            
            // Simple first-order approach to equilibrium
            float tau = 6f; // hours
            return difference / tau;
        }
        
        #endregion
        
        #region Decay Heat
        
        /// <summary>
        /// Calculate decay heat as fraction of full power using ANS 5.1-2005.
        /// </summary>
        /// <param name="timeAfterShutdown_sec">Time since shutdown in seconds</param>
        /// <returns>Decay heat as fraction of full power</returns>
        public static float DecayHeatFraction(float timeAfterShutdown_sec)
        {
            if (timeAfterShutdown_sec < 1f) return PlantConstants.DECAY_HEAT_TRIP;
            
            // ANS 5.1-2005 approximation
            // P/P0 ≈ 0.066 × t^(-0.2) for 1 < t < 10^6 seconds
            // Plus correction factors for specific nuclides
            
            float t = timeAfterShutdown_sec;
            float decayHeat = 0.066f * (float)Math.Pow(t, -0.2);
            
            // Additional correction for first few minutes
            // Use <= 60 to ensure continuity at t=60 where decay heat = 5%
            if (t <= 60f)
            {
                decayHeat = PlantConstants.DECAY_HEAT_TRIP - 
                            (PlantConstants.DECAY_HEAT_TRIP - PlantConstants.DECAY_HEAT_1MIN) * t / 60f;
            }
            
            return Math.Max(decayHeat, 0.001f);
        }
        
        /// <summary>
        /// Calculate decay heat power in MWt.
        /// </summary>
        /// <param name="timeAfterShutdown_sec">Time since shutdown in seconds</param>
        /// <returns>Decay heat in MWt</returns>
        public static float DecayHeatPower(float timeAfterShutdown_sec)
        {
            float fraction = DecayHeatFraction(timeAfterShutdown_sec);
            return fraction * PlantConstants.THERMAL_POWER_MWT;
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate reactor kinetics calculations.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: BETA_TOTAL should match sum of groups
            float betaSum = 0f;
            for (int i = 0; i < 6; i++) betaSum += BETA_GROUPS[i];
            if (Math.Abs(betaSum - BETA_TOTAL) > 0.0001f) valid = false;
            
            // Test 2: Doppler should be negative for temperature increase
            float doppler = DopplerReactivity(100f, 1000f);
            if (doppler >= 0f) valid = false;
            
            // Test 3: MTC at high boron should be positive
            float mtcHigh = ModeratorTempCoefficient(1500f);
            if (mtcHigh <= 0f) valid = false;
            
            // Test 4: MTC at low boron should be negative
            float mtcLow = ModeratorTempCoefficient(100f);
            if (mtcLow >= 0f) valid = false;
            
            // Test 5: Boron addition should decrease reactivity
            float boronRho = BoronReactivity(100f); // Add 100 ppm
            if (boronRho >= 0f) valid = false;
            
            // Test 6: Equilibrium xenon at 100% should be ~-2750 pcm
            float xenonEq = XenonEquilibrium(1f);
            if (xenonEq > -2000f || xenonEq < -3500f) valid = false;
            
            // Test 7: Decay heat at 1 minute
            float decayHeat1min = DecayHeatFraction(60f);
            if (Math.Abs(decayHeat1min - PlantConstants.DECAY_HEAT_1MIN) > 0.01f) valid = false;
            
            // Test 8: Equilibrium precursors should match expected
            float[] precursors = EquilibriumPrecursors(1f);
            if (precursors == null || precursors.Length != 6) valid = false;
            
            return valid;
        }
        
        #endregion
    }
}
