// CRITICAL: Master the Atom - Phase 1 Core Physics Engine
// FluidFlow.cs - Fluid Flow and Pump Dynamics
//
// Implements: Gap #9 - Surge line hydraulics (Darcy-Weisbach)
// Key physics: Pump affinity laws, coastdown, natural circulation
// Units: gpm for flow, ft for head, psi for pressure

using System;

namespace Critical.Physics
{
    /// <summary>
    /// Fluid flow calculations for RCS pumps, natural circulation, and surge line.
    /// Critical for transient analysis and pressurizer surge dynamics.
    /// </summary>
    public static class FluidFlow
    {
        #region Pump Dynamics
        
        /// <summary>
        /// Calculate pump coastdown speed using exponential decay.
        /// N(t) = N_0 × exp(-t/τ)
        /// </summary>
        /// <param name="initialSpeed">Initial speed (fraction or rpm)</param>
        /// <param name="timeConstant_sec">Coastdown time constant in seconds</param>
        /// <param name="elapsedTime_sec">Time since trip in seconds</param>
        /// <returns>Current speed (same units as input)</returns>
        public static float PumpCoastdown(float initialSpeed, float timeConstant_sec, float elapsedTime_sec)
        {
            if (timeConstant_sec < 0.1f) return 0f;
            return initialSpeed * (float)Math.Exp(-elapsedTime_sec / timeConstant_sec);
        }
        
        /// <summary>
        /// Calculate pump speed after time step using differential form.
        /// dN/dt = -N/τ
        /// </summary>
        /// <param name="currentSpeed">Current speed</param>
        /// <param name="dt_sec">Time step in seconds</param>
        /// <param name="timeConstant_sec">Coastdown time constant</param>
        /// <returns>New speed after dt</returns>
        public static float PumpCoastdownStep(float currentSpeed, float dt_sec, float timeConstant_sec)
        {
            if (timeConstant_sec < 0.1f) return 0f;
            float decay = (float)Math.Exp(-dt_sec / timeConstant_sec);
            return currentSpeed * decay;
        }
        
        /// <summary>
        /// Calculate flow using pump affinity laws.
        /// Q ∝ N (flow proportional to speed)
        /// </summary>
        /// <param name="speed">Current speed</param>
        /// <param name="nominalSpeed">Nominal speed</param>
        /// <param name="nominalFlow">Nominal flow at nominal speed</param>
        /// <returns>Flow at current speed</returns>
        public static float AffinityLaws_Flow(float speed, float nominalSpeed, float nominalFlow)
        {
            if (nominalSpeed < 1f) return 0f;
            return nominalFlow * (speed / nominalSpeed);
        }
        
        /// <summary>
        /// Calculate pump head using affinity laws.
        /// H ∝ N² (head proportional to speed squared)
        /// </summary>
        /// <param name="speed">Current speed</param>
        /// <param name="nominalSpeed">Nominal speed</param>
        /// <param name="nominalHead">Nominal head at nominal speed</param>
        /// <returns>Head at current speed</returns>
        public static float AffinityLaws_Head(float speed, float nominalSpeed, float nominalHead)
        {
            if (nominalSpeed < 1f) return 0f;
            float ratio = speed / nominalSpeed;
            return nominalHead * ratio * ratio;
        }
        
        /// <summary>
        /// Calculate pump power using affinity laws.
        /// P ∝ N³ (power proportional to speed cubed)
        /// </summary>
        /// <param name="speed">Current speed</param>
        /// <param name="nominalSpeed">Nominal speed</param>
        /// <param name="nominalPower">Nominal power at nominal speed</param>
        /// <returns>Power at current speed</returns>
        public static float AffinityLaws_Power(float speed, float nominalSpeed, float nominalPower)
        {
            if (nominalSpeed < 1f) return 0f;
            float ratio = speed / nominalSpeed;
            return nominalPower * ratio * ratio * ratio;
        }
        
        /// <summary>
        /// Calculate total RCS flow from all RCPs.
        /// </summary>
        /// <param name="pumpSpeeds">Array of 4 pump speeds (fraction of nominal)</param>
        /// <returns>Total RCS flow in gpm</returns>
        public static float TotalRCSFlow(float[] pumpSpeeds)
        {
            if (pumpSpeeds == null || pumpSpeeds.Length != 4) return 0f;
            
            float totalFlow = 0f;
            for (int i = 0; i < 4; i++)
            {
                totalFlow += AffinityLaws_Flow(pumpSpeeds[i], 1f, PlantConstants.RCP_FLOW_EACH);
            }
            return totalFlow;
        }
        
        /// <summary>
        /// Calculate pump heat addition to RCS.
        /// </summary>
        /// <param name="pumpSpeed">Pump speed as fraction of nominal</param>
        /// <returns>Pump heat in MW</returns>
        public static float PumpHeat(float pumpSpeed)
        {
            // Pump heat is approximately proportional to speed cubed
            // At nominal: 21 MW per pump
            return AffinityLaws_Power(pumpSpeed, 1f, PlantConstants.RCP_HEAT_MW);
        }
        
        #endregion
        
        #region Natural Circulation
        
        /// <summary>
        /// Calculate natural circulation flow rate.
        /// Driven by density difference between hot and cold legs.
        /// </summary>
        /// <param name="deltaT_F">Temperature difference (hot - cold) in °F</param>
        /// <param name="elevation_ft">Driving head elevation in ft</param>
        /// <param name="resistance">Flow resistance coefficient</param>
        /// <returns>Natural circulation flow in gpm</returns>
        public static float NaturalCirculationFlow(float deltaT_F, float elevation_ft, float resistance)
        {
            if (deltaT_F <= 0f || elevation_ft <= 0f || resistance <= 0f) return 0f;
            
            // Natural circulation is driven by buoyancy
            // ΔP_driving = g × H × Δρ
            // Flow rate Q ∝ √(ΔP_driving / R)
            
            // Typical values for Westinghouse 4-loop:
            // At 61°F ΔT: natural circ = 3-6% of normal flow
            // Normal flow = 390,400 gpm, so nat circ = 12,000 - 23,000 gpm
            
            // Simplified model calibrated to PWR data
            // Q = K × √(ΔT × H) where K is calibration factor
            
            float K = 2500f; // Calibrated for typical PWR
            float flow = K * (float)Math.Sqrt(deltaT_F * elevation_ft / resistance);
            
            // Clamp to reasonable range
            return Math.Max(PlantConstants.NAT_CIRC_FLOW_MIN, 
                   Math.Min(flow, PlantConstants.NAT_CIRC_FLOW_MAX));
        }
        
        /// <summary>
        /// Calculate natural circulation driving head.
        /// </summary>
        /// <param name="T_hot">Hot leg temperature in °F</param>
        /// <param name="T_cold">Cold leg temperature in °F</param>
        /// <param name="pressure_psia">System pressure</param>
        /// <param name="elevation_ft">Elevation difference in ft</param>
        /// <returns>Driving head in ft of water</returns>
        public static float NaturalCirculationHead(float T_hot, float T_cold, float pressure_psia, float elevation_ft)
        {
            float rho_hot = WaterProperties.WaterDensity(T_hot, pressure_psia);
            float rho_cold = WaterProperties.WaterDensity(T_cold, pressure_psia);
            
            float deltaRho = rho_cold - rho_hot;
            float avgRho = (rho_hot + rho_cold) / 2f;
            
            // Driving head = H × Δρ / ρ_avg (in ft of average density water)
            return elevation_ft * deltaRho / avgRho;
        }
        
        /// <summary>
        /// Calculate natural circulation as fraction of normal flow.
        /// </summary>
        /// <param name="deltaT_F">Temperature difference in °F</param>
        /// <returns>Natural circulation fraction (typically 0.03-0.06)</returns>
        public static float NaturalCirculationFraction(float deltaT_F)
        {
            // Linear approximation based on ΔT
            // At 61°F ΔT: ~3-6% of normal
            float fraction = 0.001f * deltaT_F;
            return Math.Max(PlantConstants.NAT_CIRC_PERCENT_MIN,
                   Math.Min(fraction, PlantConstants.NAT_CIRC_PERCENT_MAX));
        }
        
        #endregion
        
        #region Surge Line Flow (Gap #9)
        
        /// <summary>
        /// Calculate surge line flow using Darcy-Weisbach equation.
        /// ΔP = f × (L/D) × (ρV²/2) converted for flow rate
        /// </summary>
        /// <param name="deltaP_psi">Pressure difference (RCS - PZR) in psi</param>
        /// <param name="diameter_in">Pipe diameter in inches</param>
        /// <param name="length_ft">Pipe length in ft</param>
        /// <param name="friction">Darcy friction factor</param>
        /// <param name="density_lb_ft3">Fluid density in lb/ft³</param>
        /// <returns>Flow rate in gpm (positive = into PZR)</returns>
        public static float SurgeLineFlow(
            float deltaP_psi, 
            float diameter_in, 
            float length_ft, 
            float friction,
            float density_lb_ft3)
        {
            if (Math.Abs(deltaP_psi) < 0.01f) return 0f;
            if (diameter_in < 1f || length_ft < 1f || friction < 0.001f) return 0f;
            if (density_lb_ft3 < 1f) return 0f;
            
            // Convert diameter to ft
            float D_ft = diameter_in / 12f;
            
            // Pipe cross-sectional area (ft²)
            float area = (float)Math.PI * D_ft * D_ft / 4f;
            
            // Convert pressure to lb/ft² (1 psi = 144 lb/ft²)
            float deltaP_lbft2 = Math.Abs(deltaP_psi) * 144f;
            
            // Darcy-Weisbach solved for velocity:
            // V = √(2 × ΔP × D / (f × L × ρ))
            float V_ft_sec = (float)Math.Sqrt(2f * deltaP_lbft2 * D_ft / (friction * length_ft * density_lb_ft3));
            
            // Volumetric flow rate (ft³/sec)
            float Q_ft3_sec = area * V_ft_sec;
            
            // Convert to gpm (1 ft³/sec = 448.831 gpm)
            float Q_gpm = Q_ft3_sec * 448.831f;
            
            // Apply sign based on pressure difference direction
            return deltaP_psi > 0f ? Q_gpm : -Q_gpm;
        }
        
        /// <summary>
        /// Calculate surge line flow using plant constants.
        /// </summary>
        /// <param name="P_RCS_psia">RCS pressure in psia</param>
        /// <param name="P_PZR_psia">Pressurizer pressure in psia</param>
        /// <param name="temp_F">Fluid temperature in °F</param>
        /// <returns>Flow rate in gpm (positive = insurge)</returns>
        public static float SurgeLineFlowSimple(float P_RCS_psia, float P_PZR_psia, float temp_F)
        {
            float deltaP = P_RCS_psia - P_PZR_psia;
            float rho = WaterProperties.WaterDensity(temp_F, (P_RCS_psia + P_PZR_psia) / 2f);
            
            return SurgeLineFlow(
                deltaP,
                PlantConstants.SURGE_LINE_DIAMETER,
                PlantConstants.SURGE_LINE_LENGTH,
                PlantConstants.SURGE_LINE_FRICTION,
                rho);
        }
        
        /// <summary>
        /// Calculate pressure drop in surge line for given flow.
        /// </summary>
        /// <param name="flow_gpm">Flow rate in gpm</param>
        /// <param name="diameter_in">Pipe diameter in inches</param>
        /// <param name="length_ft">Pipe length in ft</param>
        /// <param name="friction">Darcy friction factor</param>
        /// <param name="density_lb_ft3">Fluid density in lb/ft³</param>
        /// <returns>Pressure drop in psi</returns>
        public static float SurgeLinePressureDrop(
            float flow_gpm,
            float diameter_in,
            float length_ft,
            float friction,
            float density_lb_ft3)
        {
            if (Math.Abs(flow_gpm) < 0.1f) return 0f;
            
            // Convert diameter to ft
            float D_ft = diameter_in / 12f;
            
            // Pipe cross-sectional area (ft²)
            float area = (float)Math.PI * D_ft * D_ft / 4f;
            
            // Convert flow to ft³/sec
            float Q_ft3_sec = Math.Abs(flow_gpm) / 448.831f;
            
            // Velocity
            float V_ft_sec = Q_ft3_sec / area;
            
            // Darcy-Weisbach: ΔP = f × (L/D) × (ρV²/2)
            float deltaP_lbft2 = friction * (length_ft / D_ft) * (density_lb_ft3 * V_ft_sec * V_ft_sec / 2f);
            
            // Convert to psi
            float deltaP_psi = deltaP_lbft2 / 144f;
            
            // Apply sign
            return flow_gpm > 0f ? deltaP_psi : -deltaP_psi;
        }
        
        #endregion
        
        #region General Pipe Flow
        
        /// <summary>
        /// Calculate Reynolds number for pipe flow.
        /// </summary>
        /// <param name="velocity_ft_sec">Flow velocity in ft/sec</param>
        /// <param name="diameter_ft">Pipe diameter in ft</param>
        /// <param name="density_lb_ft3">Fluid density in lb/ft³</param>
        /// <param name="viscosity_lbm_ft_sec">Dynamic viscosity in lbm/(ft·sec)</param>
        /// <returns>Reynolds number (dimensionless)</returns>
        public static float ReynoldsNumber(float velocity_ft_sec, float diameter_ft, float density_lb_ft3, float viscosity_lbm_ft_sec)
        {
            if (viscosity_lbm_ft_sec < 1e-6f) return 0f;
            return density_lb_ft3 * velocity_ft_sec * diameter_ft / viscosity_lbm_ft_sec;
        }
        
        /// <summary>
        /// Estimate Darcy friction factor using Colebrook approximation.
        /// </summary>
        /// <param name="Re">Reynolds number</param>
        /// <param name="roughness">Relative roughness (ε/D)</param>
        /// <returns>Darcy friction factor</returns>
        public static float DarcyFrictionFactor(float Re, float roughness)
        {
            if (Re < 100f) return 0.1f; // Stokes flow approximation
            
            if (Re < 2300f)
            {
                // Laminar flow: f = 64/Re
                return 64f / Re;
            }
            else
            {
                // Turbulent flow: Haaland approximation of Colebrook
                // 1/√f = -1.8 × log10[(ε/D/3.7)^1.11 + 6.9/Re]
                float term1 = (float)Math.Pow(roughness / 3.7f, 1.11f);
                float term2 = 6.9f / Re;
                float invSqrtF = -1.8f * (float)Math.Log10(term1 + term2);
                if (invSqrtF < 0.1f) invSqrtF = 0.1f;
                return 1f / (invSqrtF * invSqrtF);
            }
        }
        
        /// <summary>
        /// Calculate pressure drop for general pipe flow.
        /// </summary>
        /// <param name="flow_gpm">Volumetric flow rate in gpm</param>
        /// <param name="diameter_in">Pipe inner diameter in inches</param>
        /// <param name="length_ft">Pipe length in ft</param>
        /// <param name="density_lb_ft3">Fluid density in lb/ft³</param>
        /// <param name="frictionFactor">Darcy friction factor</param>
        /// <returns>Pressure drop in psi</returns>
        public static float PressureDrop(
            float flow_gpm,
            float diameter_in,
            float length_ft,
            float density_lb_ft3,
            float frictionFactor)
        {
            return SurgeLinePressureDrop(flow_gpm, diameter_in, length_ft, frictionFactor, density_lb_ft3);
        }
        
        #endregion
        
        #region Flow Conversion Utilities
        
        /// <summary>
        /// Convert volumetric flow to mass flow.
        /// </summary>
        /// <param name="flow_gpm">Volumetric flow in gpm</param>
        /// <param name="density_lb_ft3">Fluid density in lb/ft³</param>
        /// <returns>Mass flow in lb/sec</returns>
        public static float VolumetricToMassFlow(float flow_gpm, float density_lb_ft3)
        {
            // gpm × ft³/gpm × lb/ft³ = lb/min → divide by 60 for lb/sec
            return flow_gpm * PlantConstants.GPM_TO_FT3_SEC * density_lb_ft3;
        }
        
        /// <summary>
        /// Convert mass flow to volumetric flow.
        /// </summary>
        /// <param name="massFlow_lb_sec">Mass flow in lb/sec</param>
        /// <param name="density_lb_ft3">Fluid density in lb/ft³</param>
        /// <returns>Volumetric flow in gpm</returns>
        public static float MassToVolumetricFlow(float massFlow_lb_sec, float density_lb_ft3)
        {
            if (density_lb_ft3 < 1f) return 0f;
            return massFlow_lb_sec / density_lb_ft3 / PlantConstants.GPM_TO_FT3_SEC;
        }
        
        /// <summary>
        /// Calculate flow velocity in pipe.
        /// </summary>
        /// <param name="flow_gpm">Volumetric flow in gpm</param>
        /// <param name="diameter_in">Pipe diameter in inches</param>
        /// <returns>Velocity in ft/sec</returns>
        public static float FlowVelocity(float flow_gpm, float diameter_in)
        {
            float D_ft = diameter_in / 12f;
            float area = (float)Math.PI * D_ft * D_ft / 4f;
            float Q_ft3_sec = flow_gpm / 448.831f;
            return Q_ft3_sec / area;
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate fluid flow calculations.
        /// </summary>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: Pump coastdown at t=τ should be 37% of initial
            float speed1 = PumpCoastdown(1f, 12f, 12f);
            float expected1 = (float)Math.Exp(-1f); // ≈ 0.368
            if (Math.Abs(speed1 - expected1) > 0.01f) valid = false;
            
            // Test 2: Affinity law - half speed should give half flow
            float flow2 = AffinityLaws_Flow(0.5f, 1f, 100f);
            if (Math.Abs(flow2 - 50f) > 0.1f) valid = false;
            
            // Test 3: Affinity law - half speed should give quarter head
            float head3 = AffinityLaws_Head(0.5f, 1f, 100f);
            if (Math.Abs(head3 - 25f) > 0.1f) valid = false;
            
            // Test 4: Natural circulation at nominal ΔT should be 3-6%
            float natCirc = NaturalCirculationFraction(PlantConstants.CORE_DELTA_T);
            if (natCirc < 0.03f || natCirc > 0.06f) valid = false;
            
            // Test 5: Surge line flow - positive ΔP should give positive flow (insurge)
            float surgeFlow = SurgeLineFlow(10f, 14f, 50f, 0.015f, 46f);
            if (surgeFlow <= 0f) valid = false;
            
            // Test 6: Surge line flow - negative ΔP should give negative flow (outsurge)
            float surgeFlow2 = SurgeLineFlow(-10f, 14f, 50f, 0.015f, 46f);
            if (surgeFlow2 >= 0f) valid = false;
            
            // Test 7: 4 pumps at nominal should give total flow
            float[] speeds = { 1f, 1f, 1f, 1f };
            float totalFlow = TotalRCSFlow(speeds);
            if (Math.Abs(totalFlow - PlantConstants.RCS_FLOW_TOTAL) > 100f) valid = false;
            
            return valid;
        }
        
        #endregion
    }
}
