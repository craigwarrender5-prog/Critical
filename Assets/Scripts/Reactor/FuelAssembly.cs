// CRITICAL: Master the Atom - Phase 2 Reactor Core
// FuelAssembly.cs - Fuel Temperature Model with Radial Profile
//
// Models the radial temperature distribution in PWR fuel:
//   Fuel Centerline → Pellet Surface → Gap → Clad Inner → Clad Outer → Coolant
//
// Key physics:
//   - Volumetric heat generation in UO2 fuel pellet
//   - Radial conduction through fuel (temperature-dependent conductivity)
//   - Gap conductance (burnup and temperature dependent)
//   - Clad conduction (Zircaloy-4)
//   - Convective heat transfer to coolant
//
// Reference: Westinghouse 4-Loop PWR (3411 MWt)
// Sources: NUREG-0800 SRP 4.2, Fink (2000) JNM 279:1-18, FRAPCON-4 (PNNL-19418)
//          Todreas & Kazimi "Nuclear Systems I" (integral conductivity method)
//
// Gold Standard Architecture:
//   - Module owns all fuel temperature physics
//   - Engine calls Update(), reads temperature results
//   - No inline physics in engine

using System;

namespace Critical.Physics
{
    /// <summary>
    /// Fuel assembly temperature model with radial profile calculation.
    /// Provides effective fuel temperature for Doppler feedback.
    /// </summary>
    public class FuelAssembly
    {
        #region Fuel Geometry Constants (Westinghouse 17x17)
        
        /// <summary>Fuel pellet radius in ft (8.19 mm / 2 = 4.095 mm = 0.01343 ft)</summary>
        public const float PELLET_RADIUS_FT = 0.01343f;
        
        /// <summary>Fuel pellet diameter in inches (0.3225")</summary>
        public const float PELLET_DIAMETER_IN = 0.3225f;
        
        /// <summary>Pellet-clad gap width in ft (0.17 mm = 0.000558 ft)</summary>
        public const float GAP_WIDTH_FT = 0.000558f;
        
        /// <summary>Cladding inner radius in ft (pellet radius + gap)</summary>
        public const float CLAD_INNER_RADIUS_FT = PELLET_RADIUS_FT + GAP_WIDTH_FT;
        
        /// <summary>Cladding thickness in ft (0.57 mm = 0.00187 ft)</summary>
        public const float CLAD_THICKNESS_FT = 0.00187f;
        
        /// <summary>Cladding outer radius in ft</summary>
        public const float CLAD_OUTER_RADIUS_FT = CLAD_INNER_RADIUS_FT + CLAD_THICKNESS_FT;
        
        /// <summary>Cladding outer diameter in inches (0.374")</summary>
        public const float CLAD_OD_IN = 0.374f;
        
        /// <summary>Active fuel length in ft</summary>
        public const float ACTIVE_LENGTH_FT = 12f;
        
        /// <summary>Fuel rod pitch in inches (0.496" for 17x17)</summary>
        public const float ROD_PITCH_IN = 0.496f;
        
        #endregion
        
        #region Material Properties
        
        // =====================================================================
        // UO2 Fuel Properties
        // Fink (2000), Journal of Nuclear Materials 279: 1-18
        // "Thermophysical properties of uranium dioxide"
        // Standard reference correlation used in FRAPCON-4, RELAP, etc.
        // =====================================================================
        
        /// <summary>
        /// Fink (2000) phonon scattering coefficient A in (m·K/W).
        /// k_phonon = 100 / (A + B*t + C*t²) where t = T(K)/1000
        /// </summary>
        public const float FINK_A = 7.5408f;
        
        /// <summary>Fink (2000) phonon scattering coefficient B in (m/W)</summary>
        public const float FINK_B = 17.692f;
        
        /// <summary>Fink (2000) phonon scattering coefficient C in (m/(W·K))</summary>
        public const float FINK_C = 3.6142f;
        
        /// <summary>Fink (2000) electronic/radiation coefficient D in (W·K/m)</summary>
        public const float FINK_D = 6400f;
        
        /// <summary>Fink (2000) electronic activation energy E (dimensionless)</summary>
        public const float FINK_E = 16.35f;
        
        /// <summary>
        /// Conversion factor: W/(m·K) to BTU/(hr·ft·°F).
        /// 1 W/(m·K) = 0.5782 BTU/(hr·ft·°F)
        /// </summary>
        public const float SI_TO_BTU_CONDUCTIVITY = 0.5782f;
        
        /// <summary>
        /// Maximum number of Newton-Raphson iterations for integral conductivity.
        /// Convergence is typically achieved in 5-8 iterations.
        /// </summary>
        private const int MAX_NR_ITERATIONS = 20;
        
        /// <summary>
        /// Convergence tolerance for integral conductivity Newton-Raphson in BTU/(hr·ft).
        /// At 0.5 BTU/(hr·ft), temperature error is < 0.5°F.
        /// </summary>
        private const float NR_CONVERGENCE_TOL = 0.5f;
        
        /// <summary>
        /// UO2 volumetric heat capacity in BTU/(ft³·°F).
        /// ρcp = 10970 kg/m³ × 300 J/(kg·K) = 3.29 MJ/(m³·K)
        /// = 48.8 BTU/(ft³·°F)
        /// </summary>
        public const float UO2_HEAT_CAPACITY = 48.8f;
        
        /// <summary>
        /// UO2 density at 95% theoretical density in lb/ft³.
        /// 10.97 g/cm³ × 95% = 10.42 g/cm³ = 650 lb/ft³
        /// </summary>
        public const float UO2_DENSITY = 650f;
        
        /// <summary>UO2 melting point in °F (2865°C = 5189°F)</summary>
        public const float UO2_MELTING_POINT_F = 5189f;
        
        // =====================================================================
        // Zircaloy-4 Cladding Properties
        // =====================================================================
        
        /// <summary>
        /// Zircaloy thermal conductivity in BTU/(hr·ft·°F).
        /// k = 15 W/m-K ≈ 8.67 BTU/(hr·ft·°F)
        /// </summary>
        public const float ZIRC_CONDUCTIVITY = 8.67f;
        
        /// <summary>
        /// Zircaloy volumetric heat capacity in BTU/(ft³·°F).
        /// ρcp = 6500 kg/m³ × 330 J/(kg·K) = 2.15 MJ/(m³·K)
        /// = 31.9 BTU/(ft³·°F)
        /// </summary>
        public const float ZIRC_HEAT_CAPACITY = 31.9f;
        
        /// <summary>Zircaloy density in lb/ft³ (6500 kg/m³ = 406 lb/ft³)</summary>
        public const float ZIRC_DENSITY = 406f;
        
        // =====================================================================
        // Gap Conductance (FRAPCON model simplified)
        // =====================================================================
        
        /// <summary>
        /// Beginning-of-life gap conductance in BTU/(hr·ft²·°F).
        /// Fresh fuel with He fill gas, standard diametral gap (0.17mm):
        ///   h_gas = k_He / (gap + roughness + jump) ≈ 2550 W/(m²·K)
        ///   h_rad ≈ 125 W/(m²·K) at operating temperatures
        ///   h_total ≈ 2675 W/(m²·K) ≈ 471 BTU/(hr·ft²·°F)
        /// Rounded to 500 for slight pellet relocation effect at first power ascension.
        /// Source: FRAPCON-4 (PNNL-19418), Westinghouse 17x17 fuel design
        /// </summary>
        public const float GAP_CONDUCTANCE_BOL = 500f;
        
        /// <summary>
        /// End-of-life gap conductance in BTU/(hr·ft²·°F).
        /// h_gap = 10000 W/m²-K ≈ 1760 BTU/(hr·ft²·°F) after pellet-clad contact
        /// </summary>
        public const float GAP_CONDUCTANCE_EOL = 1760f;
        
        /// <summary>
        /// Burnup at which gap closes (MWd/MTU).
        /// Gap typically closes at 20,000-30,000 MWd/MTU
        /// </summary>
        public const float GAP_CLOSURE_BURNUP = 25000f;
        
        // =====================================================================
        // Coolant Heat Transfer
        // =====================================================================
        
        /// <summary>
        /// Coolant heat transfer coefficient in BTU/(hr·ft²·°F).
        /// Dittus-Boelter: h ≈ 6000 BTU/(hr·ft²·°F) at full flow
        /// Scaled with flow: h ∝ (flow)^0.8
        /// </summary>
        public const float COOLANT_HTC_NOMINAL = 6000f;
        
        #endregion
        
        #region Thermal Time Constants
        
        /// <summary>
        /// Fuel thermal time constant in seconds.
        /// τ = ρcp × r² / (4k) ≈ 5-10 seconds for UO2
        /// </summary>
        public const float FUEL_THERMAL_TAU_SEC = 7f;
        
        /// <summary>
        /// Cladding thermal time constant in seconds.
        /// Much shorter than fuel due to thinner geometry and higher k
        /// </summary>
        public const float CLAD_THERMAL_TAU_SEC = 0.5f;
        
        #endregion
        
        #region Instance State
        
        // Temperature Profile (°F)
        private float _centerlineTemp_F;
        private float _pelletSurfaceTemp_F;
        private float _cladInnerTemp_F;
        private float _cladOuterTemp_F;
        private float _coolantTemp_F;
        
        // Power State
        private float _linearHeatRate_kWft;  // Current linear heat rate (kW/ft)
        private float _powerFraction;         // Power as fraction of nominal (0-1)
        private float _peakingFactor;         // Local peaking factor (Fq)
        
        // Assembly Properties
        private float _burnup_MWdMTU;         // Current burnup
        private int _assemblyIndex;           // Assembly number (0-192)
        private float _flowFraction;          // Coolant flow as fraction of nominal
        
        // Derived Values
        private float _effectiveFuelTemp_F;   // Doppler-weighted effective temperature
        private float _gapConductance;        // Current gap conductance
        
        #endregion
        
        #region Public Properties
        
        /// <summary>Fuel centerline temperature in °F</summary>
        public float CenterlineTemp_F => _centerlineTemp_F;
        
        /// <summary>Fuel pellet surface temperature in °F</summary>
        public float PelletSurfaceTemp_F => _pelletSurfaceTemp_F;
        
        /// <summary>Cladding inner surface temperature in °F</summary>
        public float CladInnerTemp_F => _cladInnerTemp_F;
        
        /// <summary>Cladding outer surface temperature in °F</summary>
        public float CladOuterTemp_F => _cladOuterTemp_F;
        
        /// <summary>Bulk coolant temperature in °F</summary>
        public float CoolantTemp_F => _coolantTemp_F;
        
        /// <summary>
        /// Effective fuel temperature for Doppler feedback in °F.
        /// Weighted average biased toward hotter regions where resonance
        /// absorption is most significant.
        /// </summary>
        public float EffectiveFuelTemp_F => _effectiveFuelTemp_F;
        
        /// <summary>Current linear heat rate in kW/ft</summary>
        public float LinearHeatRate_kWft => _linearHeatRate_kWft;
        
        /// <summary>Current power as fraction of nominal (0-1)</summary>
        public float PowerFraction => _powerFraction;
        
        /// <summary>Local peaking factor (Fq)</summary>
        public float PeakingFactor => _peakingFactor;
        
        /// <summary>Current burnup in MWd/MTU</summary>
        public float Burnup_MWdMTU => _burnup_MWdMTU;
        
        /// <summary>Assembly index (0-192)</summary>
        public int AssemblyIndex => _assemblyIndex;
        
        /// <summary>Current gap conductance in BTU/(hr·ft²·°F)</summary>
        public float GapConductance => _gapConductance;
        
        /// <summary>Temperature margin to fuel melting in °F</summary>
        public float MeltingMargin_F => UO2_MELTING_POINT_F - _centerlineTemp_F;
        
        /// <summary>True if fuel centerline exceeds melting point</summary>
        public bool IsMelted => _centerlineTemp_F >= UO2_MELTING_POINT_F;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Create a fuel assembly with initial conditions.
        /// </summary>
        /// <param name="assemblyIndex">Assembly number (0-192)</param>
        /// <param name="initialCoolantTemp_F">Initial coolant temperature</param>
        /// <param name="burnup_MWdMTU">Initial burnup</param>
        /// <param name="peakingFactor">Local peaking factor (default 1.0)</param>
        public FuelAssembly(int assemblyIndex, float initialCoolantTemp_F, 
                           float burnup_MWdMTU = 0f, float peakingFactor = 1.0f)
        {
            _assemblyIndex = Math.Max(0, Math.Min(assemblyIndex, PlantConstants.FUEL_ASSEMBLIES - 1));
            _coolantTemp_F = initialCoolantTemp_F;
            _burnup_MWdMTU = Math.Max(0f, burnup_MWdMTU);
            _peakingFactor = Math.Max(0.5f, Math.Min(peakingFactor, 2.5f));
            
            // Initialize at zero power - all temperatures equal coolant
            _centerlineTemp_F = initialCoolantTemp_F;
            _pelletSurfaceTemp_F = initialCoolantTemp_F;
            _cladInnerTemp_F = initialCoolantTemp_F;
            _cladOuterTemp_F = initialCoolantTemp_F;
            _effectiveFuelTemp_F = initialCoolantTemp_F;
            
            _linearHeatRate_kWft = 0f;
            _powerFraction = 0f;
            _flowFraction = 1.0f;
            
            // Calculate initial gap conductance
            _gapConductance = CalculateGapConductance(_burnup_MWdMTU);
        }
        
        #endregion
        
        #region Core Calculation Methods
        
        /// <summary>
        /// Update fuel temperatures based on current power and coolant conditions.
        /// This is the main entry point called by the simulation engine.
        /// </summary>
        /// <param name="powerFraction">Reactor power as fraction of nominal (0-1)</param>
        /// <param name="coolantTemp_F">Bulk coolant temperature in °F</param>
        /// <param name="flowFraction">Coolant flow as fraction of nominal (0-1)</param>
        /// <param name="dt_sec">Time step in seconds</param>
        public void Update(float powerFraction, float coolantTemp_F, float flowFraction, float dt_sec)
        {
            // Clamp inputs
            _powerFraction = Math.Max(0f, Math.Min(powerFraction, 1.5f));
            _coolantTemp_F = coolantTemp_F;
            _flowFraction = Math.Max(0.03f, Math.Min(flowFraction, 1.2f)); // Min is natural circ
            
            // Calculate linear heat rate
            _linearHeatRate_kWft = PlantConstants.AVG_LINEAR_HEAT * _powerFraction * _peakingFactor;
            
            // Calculate steady-state temperature profile
            var ssTemps = CalculateSteadyStateProfile(
                _linearHeatRate_kWft, 
                _coolantTemp_F, 
                _flowFraction);
            
            // Apply thermal lag (fuel has significant thermal inertia)
            float fuelAlpha = dt_sec / FUEL_THERMAL_TAU_SEC;
            float cladAlpha = dt_sec / CLAD_THERMAL_TAU_SEC;
            
            // First-order lag toward steady state
            fuelAlpha = Math.Min(fuelAlpha, 1f);
            cladAlpha = Math.Min(cladAlpha, 1f);
            
            _centerlineTemp_F += fuelAlpha * (ssTemps.centerline - _centerlineTemp_F);
            _pelletSurfaceTemp_F += fuelAlpha * (ssTemps.pelletSurface - _pelletSurfaceTemp_F);
            _cladInnerTemp_F += cladAlpha * (ssTemps.cladInner - _cladInnerTemp_F);
            _cladOuterTemp_F += cladAlpha * (ssTemps.cladOuter - _cladOuterTemp_F);
            
            // Calculate effective fuel temperature for Doppler
            _effectiveFuelTemp_F = CalculateEffectiveFuelTemp(_centerlineTemp_F, _pelletSurfaceTemp_F);
            
            // Update gap conductance (changes slowly with burnup)
            _gapConductance = CalculateGapConductance(_burnup_MWdMTU);
        }
        
        /// <summary>
        /// Calculate steady-state radial temperature profile.
        /// Uses resistance network: q' flows through fuel, gap, clad, coolant film.
        /// 
        /// Clad and gap use standard thermal resistance (constant-property).
        /// Fuel pellet uses the integral conductivity method:
        ///   ∫[T_surface to T_center] k(T)dT = q'/(4π)
        /// solved via Newton-Raphson iteration. This properly accounts for the
        /// strong temperature dependence of UO2 conductivity across the large
        /// radial temperature gradient (ΔT > 1000°F).
        /// 
        /// Reference: Todreas & Kazimi, "Nuclear Systems I", Section 8.3
        /// </summary>
        private (float centerline, float pelletSurface, float cladInner, float cladOuter) 
            CalculateSteadyStateProfile(float q_kWft, float T_coolant_F, float flowFraction)
        {
            // Convert linear heat rate to BTU/hr-ft
            // 1 kW/ft = 3412 BTU/hr-ft
            float q_BTUhrft = q_kWft * 3412f;
            
            if (q_BTUhrft < 0.1f)
            {
                // Zero power - isothermal at coolant temperature
                return (T_coolant_F, T_coolant_F, T_coolant_F, T_coolant_F);
            }
            
            // ============================================================
            // Outer boundary: coolant film → clad outer
            // q' = h × (π × D_o) × (T_co - T_cool)
            // ============================================================
            float htc = COOLANT_HTC_NOMINAL * (float)Math.Pow(flowFraction, 0.8);
            float cladArea = (float)Math.PI * 2f * CLAD_OUTER_RADIUS_FT;
            float T_cladOuter = T_coolant_F + q_BTUhrft / (htc * cladArea);
            
            // ============================================================
            // Clad wall conduction: clad outer → clad inner
            // q' = 2πk_clad (T_ci - T_co) / ln(r_o/r_i)
            // ============================================================
            float cladResistance = (float)Math.Log(CLAD_OUTER_RADIUS_FT / CLAD_INNER_RADIUS_FT) 
                                   / (2f * (float)Math.PI * ZIRC_CONDUCTIVITY);
            float T_cladInner = T_cladOuter + q_BTUhrft * cladResistance;
            
            // ============================================================
            // Gap: clad inner → pellet surface
            // q' = h_gap × (π × D_pellet) × (T_ps - T_ci)
            // ============================================================
            float gapArea = (float)Math.PI * 2f * PELLET_RADIUS_FT;
            float T_pelletSurface = T_cladInner + q_BTUhrft / (_gapConductance * gapArea);
            
            // ============================================================
            // Fuel pellet: integral conductivity method
            // ∫[T_surface to T_center] k(T)dT = q'/(4π)
            // 
            // Newton-Raphson: find T_cl such that F(T_cl) = 0
            //   F(T_cl) = ∫[T_ps to T_cl] k(T)dT - q'/(4π)
            //   F'(T_cl) = k(T_cl)
            //   T_cl_new = T_cl - F(T_cl)/k(T_cl)
            // ============================================================
            float q_target = q_BTUhrft / (4f * (float)Math.PI);
            
            // Initial guess using point-average conductivity
            float k_surface = CalculateUO2Conductivity(T_pelletSurface);
            float T_centerline = T_pelletSurface + q_target / k_surface;
            
            // Newton-Raphson iteration
            for (int iter = 0; iter < MAX_NR_ITERATIONS; iter++)
            {
                float integral = IntegrateK(T_pelletSurface, T_centerline);
                float residual = integral - q_target;
                float k_cl = CalculateUO2Conductivity(T_centerline);
                
                if (k_cl < 0.01f) break;  // Safety: avoid division by near-zero
                
                float correction = residual / k_cl;
                T_centerline -= correction;
                
                // Ensure centerline stays above surface
                T_centerline = Math.Max(T_centerline, T_pelletSurface + 1f);
                
                if (Math.Abs(residual) < NR_CONVERGENCE_TOL) break;
            }
            
            return (T_centerline, T_pelletSurface, T_cladInner, T_cladOuter);
        }
        
        /// <summary>
        /// Calculate UO2 thermal conductivity at given temperature.
        /// Uses Fink (2000) JNM 279:1-18 correlation at 95% theoretical density.
        /// k(T) = [100/(A + B*t + C*t²) + D/t^2.5 * exp(-E/t)] * porosity_factor
        /// where t = T(K)/1000
        /// 
        /// At 1832°F (1000°C): k ≈ 1.61 BTU/(hr·ft·°F) = 2.79 W/(m·K)
        /// Validated range: 300K to 3120K (80°F to 5156°F)
        /// </summary>
        /// <param name="temp_F">Fuel temperature in °F</param>
        /// <returns>Thermal conductivity in BTU/(hr·ft·°F)</returns>
        public static float CalculateUO2Conductivity(float temp_F)
        {
            // Convert °F to Kelvin
            float T_K = (temp_F - 32f) * 5f / 9f + 273.15f;
            T_K = Math.Max(T_K, 300f);  // Lower validity bound
            
            float t = T_K / 1000f;  // Reduced temperature
            
            // Phonon scattering term (dominates at T < 2000K)
            float k_phonon = 100f / (FINK_A + FINK_B * t + FINK_C * t * t);
            
            // Electronic/radiation term (significant only at T > 2000K)
            float k_electronic = FINK_D / (t * t * (float)Math.Sqrt(t)) 
                                 * (float)Math.Exp(-FINK_E / t);
            
            // Total conductivity in W/(m·K) at 95% theoretical density.
            // Fink (2000) coefficients are fitted to 95% TD experimental data;
            // no additional porosity correction is needed.
            float k_95TD = k_phonon + k_electronic;
            
            // Convert to BTU/(hr·ft·°F)
            float k_BTU = k_95TD * SI_TO_BTU_CONDUCTIVITY;
            
            // Clamp to physical range
            return Math.Max(0.3f, Math.Min(k_BTU, 5f));
        }
        
        /// <summary>
        /// Compute the integral of thermal conductivity from T1 to T2.
        /// ∫k(T)dT from T1 to T2, using Simpson's rule for accuracy.
        /// This is needed for the integral conductivity method:
        ///   ∫[T_surface to T_center] k(T)dT = q'/(4π)
        /// </summary>
        /// <param name="T1_F">Lower temperature bound in °F</param>
        /// <param name="T2_F">Upper temperature bound in °F</param>
        /// <returns>Integral of k(T)dT in BTU·°F/(hr·ft)</returns>
        private static float IntegrateK(float T1_F, float T2_F)
        {
            if (T2_F <= T1_F) return 0f;
            
            // Simpson's rule with 20 panels (41 points) - accurate to O(h^4)
            const int N = 20;
            float h = (T2_F - T1_F) / (2 * N);
            
            float sum = CalculateUO2Conductivity(T1_F) + CalculateUO2Conductivity(T2_F);
            
            for (int i = 1; i < 2 * N; i++)
            {
                float T = T1_F + i * h;
                float k = CalculateUO2Conductivity(T);
                sum += (i % 2 == 0) ? 2f * k : 4f * k;
            }
            
            return sum * h / 3f;
        }
        
        /// <summary>
        /// Calculate gap conductance based on burnup.
        /// Gap closes with burnup due to pellet swelling and clad creep-down.
        /// </summary>
        /// <param name="burnup_MWdMTU">Burnup in MWd/MTU</param>
        /// <returns>Gap conductance in BTU/(hr·ft²·°F)</returns>
        public static float CalculateGapConductance(float burnup_MWdMTU)
        {
            if (burnup_MWdMTU <= 0f)
                return GAP_CONDUCTANCE_BOL;
            
            if (burnup_MWdMTU >= GAP_CLOSURE_BURNUP)
                return GAP_CONDUCTANCE_EOL;
            
            // Linear interpolation (simplified from FRAPCON)
            float fraction = burnup_MWdMTU / GAP_CLOSURE_BURNUP;
            return GAP_CONDUCTANCE_BOL + fraction * (GAP_CONDUCTANCE_EOL - GAP_CONDUCTANCE_BOL);
        }
        
        /// <summary>
        /// Calculate effective fuel temperature for Doppler feedback.
        /// Uses empirical weighting that biases toward hotter regions
        /// where resonance absorption is most significant.
        /// </summary>
        /// <param name="centerline_F">Centerline temperature in °F</param>
        /// <param name="surface_F">Surface temperature in °F</param>
        /// <returns>Effective temperature for Doppler in °F</returns>
        public static float CalculateEffectiveFuelTemp(float centerline_F, float surface_F)
        {
            // Rowlands correlation (simplified):
            // T_eff ≈ T_surface + 0.4 × (T_centerline - T_surface)
            // This weights toward the hotter central region
            
            return surface_F + 0.4f * (centerline_F - surface_F);
        }
        
        #endregion
        
        #region Auxiliary Methods
        
        /// <summary>
        /// Set peaking factor for this assembly.
        /// </summary>
        /// <param name="peakingFactor">Local peaking factor (Fq), typically 0.5-2.5</param>
        public void SetPeakingFactor(float peakingFactor)
        {
            _peakingFactor = Math.Max(0.5f, Math.Min(peakingFactor, 2.5f));
        }
        
        /// <summary>
        /// Increment burnup based on power and time.
        /// </summary>
        /// <param name="powerFraction">Power as fraction of nominal</param>
        /// <param name="dt_hr">Time step in hours</param>
        public void IncrementBurnup(float powerFraction, float dt_hr)
        {
            // Burnup rate at 100% power is approximately:
            // 3411 MWt / 100 MTU heavy metal ≈ 34 MWd/MTU per day at 100%
            const float BURNUP_RATE_100_PERCENT = 34f / 24f; // MWd/MTU per hour at 100%
            
            _burnup_MWdMTU += powerFraction * BURNUP_RATE_100_PERCENT * dt_hr;
        }
        
        /// <summary>
        /// Reset to cold shutdown conditions.
        /// </summary>
        /// <param name="coldTemp_F">Cold temperature in °F</param>
        public void ResetToCold(float coldTemp_F)
        {
            _centerlineTemp_F = coldTemp_F;
            _pelletSurfaceTemp_F = coldTemp_F;
            _cladInnerTemp_F = coldTemp_F;
            _cladOuterTemp_F = coldTemp_F;
            _coolantTemp_F = coldTemp_F;
            _effectiveFuelTemp_F = coldTemp_F;
            _linearHeatRate_kWft = 0f;
            _powerFraction = 0f;
        }
        
        /// <summary>
        /// Get temperature at given radial position (fraction from center).
        /// </summary>
        /// <param name="radialFraction">0 = center, 1 = pellet surface</param>
        /// <returns>Temperature at radial position in °F</returns>
        public float GetRadialTemperature(float radialFraction)
        {
            radialFraction = Math.Max(0f, Math.Min(radialFraction, 1f));
            
            // Parabolic profile in fuel pellet: T(r) = Tc - (Tc - Ts) × (r/R)²
            float deltaT = _centerlineTemp_F - _pelletSurfaceTemp_F;
            return _centerlineTemp_F - deltaT * radialFraction * radialFraction;
        }
        
        /// <summary>
        /// Get complete temperature profile as an array.
        /// </summary>
        /// <returns>Array of [centerline, pelletSurface, cladInner, cladOuter, coolant]</returns>
        public float[] GetTemperatureProfile()
        {
            return new float[]
            {
                _centerlineTemp_F,
                _pelletSurfaceTemp_F,
                _cladInnerTemp_F,
                _cladOuterTemp_F,
                _coolantTemp_F
            };
        }
        
        /// <summary>
        /// Check if fuel temperatures are within safe limits.
        /// </summary>
        /// <returns>True if all temperatures within limits</returns>
        public bool IsWithinLimits()
        {
            // Centerline must be below melting (with margin)
            if (_centerlineTemp_F > UO2_MELTING_POINT_F - 500f) return false;
            
            // Clad temperature limit (oxidation accelerates above ~2200°F)
            if (_cladOuterTemp_F > 2200f) return false;
            
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create an average assembly representing the core average.
        /// </summary>
        /// <param name="initialCoolantTemp_F">Initial coolant temperature</param>
        /// <param name="burnup_MWdMTU">Core average burnup</param>
        /// <returns>Average fuel assembly</returns>
        public static FuelAssembly CreateAverageAssembly(float initialCoolantTemp_F, float burnup_MWdMTU = 0f)
        {
            return new FuelAssembly(0, initialCoolantTemp_F, burnup_MWdMTU, 1.0f);
        }
        
        /// <summary>
        /// Create a hot channel assembly representing the limiting location.
        /// </summary>
        /// <param name="initialCoolantTemp_F">Initial coolant temperature</param>
        /// <param name="burnup_MWdMTU">Local burnup</param>
        /// <param name="peakingFactor">Peak linear heat rate factor (Fq)</param>
        /// <returns>Hot channel fuel assembly</returns>
        public static FuelAssembly CreateHotChannelAssembly(float initialCoolantTemp_F, 
                                                            float burnup_MWdMTU = 0f,
                                                            float peakingFactor = 2.0f)
        {
            return new FuelAssembly(96, initialCoolantTemp_F, burnup_MWdMTU, peakingFactor);
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validate fuel assembly calculations against expected values.
        /// </summary>
        /// <returns>True if all validations pass</returns>
        public static bool ValidateCalculations()
        {
            bool valid = true;
            
            // Test 1: Zero power should be isothermal
            var assembly = new FuelAssembly(0, 557f, 0f, 1.0f);
            assembly.Update(0f, 557f, 1f, 10f);
            if (Math.Abs(assembly.CenterlineTemp_F - 557f) > 1f) valid = false;
            
            // Test 2: Full power should produce significant temperature rise
            assembly.Update(1f, 588f, 1f, 100f);
            if (assembly.CenterlineTemp_F < 1500f) valid = false;  // Should be hot
            if (assembly.CenterlineTemp_F > 3000f) valid = false;  // But not too hot
            
            // Test 3: Centerline should be hotter than surface
            if (assembly.CenterlineTemp_F <= assembly.PelletSurfaceTemp_F) valid = false;
            
            // Test 4: Surface should be hotter than clad
            if (assembly.PelletSurfaceTemp_F <= assembly.CladInnerTemp_F) valid = false;
            
            // Test 5: Clad inner should be hotter than clad outer
            if (assembly.CladInnerTemp_F <= assembly.CladOuterTemp_F) valid = false;
            
            // Test 6: Clad outer should be hotter than coolant
            if (assembly.CladOuterTemp_F <= assembly.CoolantTemp_F) valid = false;
            
            // Test 7: Gap conductance should increase with burnup
            float hGapFresh = CalculateGapConductance(0f);
            float hGapBurned = CalculateGapConductance(30000f);
            if (hGapBurned <= hGapFresh) valid = false;
            
            // Test 8: UO2 conductivity should decrease with temperature
            float kLow = CalculateUO2Conductivity(1000f);
            float kHigh = CalculateUO2Conductivity(3000f);
            if (kHigh >= kLow) valid = false;
            
            // Test 9: Effective fuel temp should be between surface and centerline
            float Teff = CalculateEffectiveFuelTemp(2000f, 1200f);
            if (Teff <= 1200f || Teff >= 2000f) valid = false;
            
            // Test 10: Peaking factor should affect linear heat rate
            var avgAssembly = new FuelAssembly(0, 557f, 0f, 1.0f);
            var hotAssembly = new FuelAssembly(0, 557f, 0f, 2.0f);
            avgAssembly.Update(1f, 588f, 1f, 100f);
            hotAssembly.Update(1f, 588f, 1f, 100f);
            if (hotAssembly.LinearHeatRate_kWft <= avgAssembly.LinearHeatRate_kWft) valid = false;
            
            return valid;
        }
        
        #endregion
    }
}
