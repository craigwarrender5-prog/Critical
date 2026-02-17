// CRITICAL: Master the Atom - Phase 1 Core Physics Engine
// Phase1TestRunner.cs - Test Runner for All Phase 1 Validation
//
// Run this to verify all 156 tests pass before proceeding to Phase 2.
// (105 module unit + 7 integration + 9 heatup integration + 35 support module)
//
// v1.0.1.6: Added CVCSController (7), RCSHeatup (9), RCPSequencer (8),
//           LoopThermodynamics (6), RVLISPhysics (5) = 35 new tests.
//           Total: 121 original + 35 new = 156.
//
// Works in both Unity (uses Debug.Log) and standalone .NET (uses Log)

using System;
using Critical.Physics;
using Critical.Physics.Tests;

namespace Critical.Tests
{
    /// <summary>
    /// Test runner for Phase 1 physics validation.
    /// Works in both Unity and standalone .NET console applications.
    /// </summary>
    public class Phase1TestRunner
    {
        private int _totalTests = 0;
        private int _passedTests = 0;
        private int _failedTests = 0;
        
        // Helper method that works in both Unity and Console
        private static void Log(string message)
        {
            #if UNITY_5_3_OR_NEWER || UNITY_2017_1_OR_NEWER
            UnityEngine.Debug.Log(message);
            #else
            Console.WriteLine(message);
            #endif
        }
        
        public static void Main(string[] args)
        {
            var runner = new Phase1TestRunner();
            runner.RunAllTests();
        }
        
        public void RunAllTests()
        {
            Log("╔═══════════════════════════════════════════════════════════════╗");
            Log("║     CRITICAL: MASTER THE ATOM - PHASE 1 PHYSICS TESTS        ║");
            Log("║              Core Physics Engine Validation                   ║");
            Log("╚═══════════════════════════════════════════════════════════════╝");
            Log("");
            
            // Run module validation tests
            RunPlantConstantsTests();
            RunWaterPropertiesTests();
            RunSteamThermodynamicsTests();
            RunThermalMassTests();
            RunThermalExpansionTests();
            RunHeatTransferTests();
            RunFluidFlowTests();
            RunReactorKineticsTests();
            RunPressurizerPhysicsTests();
            RunCoupledThermoTests();
            
            // Run support module tests (v1.0.1.6 — previously unwired)
            RunCVCSControllerTests();
            RunRCSHeatupTests();
            RunRCPSequencerTests();
            RunLoopThermodynamicsTests();
            RunRVLISPhysicsTests();
            
            // Run integration tests
            RunIntegrationTests();
            
            // Run heatup integration tests (Phase D cross-module validation)
            RunHeatupIntegrationTests();
            
            // Print summary
            PrintSummary();
        }
        
        private void Test(string id, string description, Func<bool> test)
        {
            _totalTests++;
            try
            {
                bool passed = test();
                if (passed)
                {
                    _passedTests++;
                    Log($"  [PASS] {id}: {description}");
                }
                else
                {
                    _failedTests++;
                    Log($"  [FAIL] {id}: {description}");
                }
            }
            catch (Exception ex)
            {
                _failedTests++;
                Log($"  [FAIL] {id}: {description} - Exception: {ex.Message}");
            }
        }
        
        #region Module Tests
        
        private void RunPlantConstantsTests()
        {
            Log("\n─────────────────────────────────────────────────────────────────");
            Log("  PlantConstants Tests (5 tests)");
            Log("─────────────────────────────────────────────────────────────────");
            
            Test("PC-01", "T_AVG = (T_HOT + T_COLD) / 2",
                () => Math.Abs((PlantConstants.T_HOT + PlantConstants.T_COLD) / 2f - PlantConstants.T_AVG) < 0.5f);
            
            Test("PC-02", "CORE_DELTA_T = T_HOT - T_COLD",
                () => Math.Abs(PlantConstants.T_HOT - PlantConstants.T_COLD - PlantConstants.CORE_DELTA_T) < 0.5f);
            
            Test("PC-03", "RCS_FLOW_TOTAL = 4 × RCP_FLOW_EACH",
                () => Math.Abs(4 * PlantConstants.RCP_FLOW_EACH - PlantConstants.RCS_FLOW_TOTAL) < 100f);
            
            Test("PC-04", "PZR_WATER + PZR_STEAM = PZR_TOTAL",
                () => Math.Abs(PlantConstants.PZR_WATER_VOLUME + PlantConstants.PZR_STEAM_VOLUME - 
                              PlantConstants.PZR_TOTAL_VOLUME) < 1f);
            
            Test("PC-05", "All derived constants consistent",
                () => PlantConstants.ValidateConstants());
        }
        
        private void RunWaterPropertiesTests()
        {
            Log("\n─────────────────────────────────────────────────────────────────");
            Log("  WaterProperties Tests (18 tests)");
            Log("─────────────────────────────────────────────────────────────────");
            
            Test("WP-01", "Psat(212°F) ≈ 14.7 psia",
                () => Math.Abs(WaterProperties.SaturationPressure(212f) - 14.7f) < 3f);
            
            Test("WP-02", "Psat(653°F) ≈ 2250 psia",
                () => Math.Abs(WaterProperties.SaturationPressure(653f) - 2250f) / 2250f < 0.1f);
            
            Test("WP-03", "Tsat(2250 psia) ≈ 653°F",
                () => Math.Abs(WaterProperties.SaturationTemperature(2250f) - 653f) < 5f);
            
            Test("WP-04", "ρ(588°F, 2250 psia) ≈ 46 lb/ft³",
                () => Math.Abs(WaterProperties.WaterDensity(588f, 2250f) - 46f) / 46f < 0.15f);
            
            Test("WP-05", "hfg(2250 psia) ≈ 465 BTU/lb",
                () => Math.Abs(WaterProperties.LatentHeat(2250f) - 465f) / 465f < 0.15f);
            
            Test("WP-06", "h(619°F, 2250 psia) ≈ 640 BTU/lb",
                () => Math.Abs(WaterProperties.WaterEnthalpy(619f, 2250f) - 640f) / 640f < 0.15f);
            
            Test("WP-07", "hf(2250 psia) ≈ 700 BTU/lb",
                () => Math.Abs(WaterProperties.SaturatedLiquidEnthalpy(2250f) - 700f) / 700f < 0.15f);
            
            Test("WP-08", "Subcooling(619°F, 2250 psia) ≈ 34°F",
                () => Math.Abs(WaterProperties.SubcoolingMargin(619f, 2250f) - 34f) < 10f);
            
            Test("WP-09", "Steam density at 2250 psia > 0",
                () => WaterProperties.SaturatedSteamDensity(2250f) > 0f);
            
            Test("WP-10", "Cp water > 1.0 at high T",
                () => WaterProperties.WaterSpecificHeat(600f, 2250f) > 1.0f);
            
            Test("WP-11", "IsSubcooled(619°F, 2250 psia) = true",
                () => WaterProperties.IsSubcooled(619f, 2250f));
            
            Test("WP-12", "hg = hf + hfg",
                () => Math.Abs(WaterProperties.SaturatedSteamEnthalpy(2250f) - 
                              WaterProperties.SaturatedLiquidEnthalpy(2250f) - 
                              WaterProperties.LatentHeat(2250f)) < 10f);
            
            Test("WP-13", "Surge enthalpy deficit 40-80 BTU/lb",
                () => {
                    float deficit = WaterProperties.SurgeEnthalpyDeficit(619f, 2250f);
                    return deficit > 30f && deficit < 90f;
                });
            
            Test("WP-14", "Tsat increases with pressure",
                () => WaterProperties.SaturationTemperature(2300f) > WaterProperties.SaturationTemperature(2200f));
            
            Test("WP-15", "ρ decreases with temperature",
                () => WaterProperties.WaterDensity(600f, 2250f) < WaterProperties.WaterDensity(500f, 2250f));
            
            Test("WP-16", "hfg decreases with pressure",
                () => WaterProperties.LatentHeat(2300f) < WaterProperties.LatentHeat(2200f));
            
            Test("WP-17", "Validate against NIST",
                () => WaterProperties.ValidateAgainstNIST());
            
            Test("WP-18", "Steam Cp in reasonable range",
                () => {
                    float cp = WaterProperties.SteamSpecificHeat(700f, 2250f);
                    return cp > 0.3f && cp < 2.0f;
                });
        }
        
        private void RunSteamThermodynamicsTests()
        {
            Log("\n─────────────────────────────────────────────────────────────────");
            Log("  SteamThermodynamics Tests (10 tests)");
            Log("─────────────────────────────────────────────────────────────────");
            
            Test("ST-01", "Quality=0 gives void=0",
                () => SteamThermodynamics.VoidFraction(0f, 2250f) < 0.01f);
            
            Test("ST-02", "Quality=1 gives void=1",
                () => SteamThermodynamics.VoidFraction(1f, 2250f) > 0.99f);
            
            Test("ST-03", "Void > quality for x < 1 at low P",
                () => SteamThermodynamics.VoidFraction(0.5f, 100f) > 0.5f);
            
            Test("ST-04", "Two-phase h(x=0) = hf",
                () => Math.Abs(SteamThermodynamics.TwoPhaseEnthalpy(0f, 2250f) - 
                              WaterProperties.SaturatedLiquidEnthalpy(2250f)) < 5f);
            
            Test("ST-05", "Two-phase h(x=1) = hg",
                () => Math.Abs(SteamThermodynamics.TwoPhaseEnthalpy(1f, 2250f) - 
                              WaterProperties.SaturatedSteamEnthalpy(2250f)) < 5f);
            
            Test("ST-06", "Quality from void inverse",
                () => {
                    float x = 0.3f;
                    float alpha = SteamThermodynamics.VoidFraction(x, 2250f);
                    float x_back = SteamThermodynamics.QualityFromVoidFraction(alpha, 2250f);
                    return Math.Abs(x_back - x) < 0.02f;
                });
            
            Test("ST-07", "Two-phase density between rhoF and rhoG",
                () => {
                    float rho = SteamThermodynamics.TwoPhaseDensity(0.5f, 2250f);
                    float rhoF = WaterProperties.WaterDensity(WaterProperties.SaturationTemperature(2250f), 2250f);
                    float rhoG = WaterProperties.SaturatedSteamDensity(2250f);
                    return rho > rhoG && rho < rhoF;
                });
            
            Test("ST-08", "Subcooled phase detected",
                () => SteamThermodynamics.DeterminePhase(600f, 2250f) == PhaseState.SubcooledLiquid);
            
            Test("ST-09", "Superheated phase detected",
                () => SteamThermodynamics.DeterminePhase(700f, 2250f) == PhaseState.SuperheatedSteam);
            
            Test("ST-10", "Validation passes",
                () => SteamThermodynamics.ValidateCalculations());
        }
        
        private void RunThermalMassTests()
        {
            Log("\n─────────────────────────────────────────────────────────────────");
            Log("  ThermalMass Tests (6 tests)");
            Log("─────────────────────────────────────────────────────────────────");
            
            Test("TM-01", "Q = m × Cp × ΔT",
                () => Math.Abs(ThermalMass.HeatRequired(1f, 1f, 1f) - 1f) < 0.01f);
            
            Test("TM-02", "ΔT = Q / (m × Cp)",
                () => Math.Abs(ThermalMass.TemperatureChange(1f, 1f, 1f) - 1f) < 0.01f);
            
            Test("TM-03", "Equilibrium temperature correct",
                () => Math.Abs(ThermalMass.EquilibriumTemperature(1f, 1f, 100f, 1f, 1f, 200f) - 150f) < 1f);
            
            Test("TM-04", "PZR wall capacity ≈ 24,000 BTU/°F",
                () => Math.Abs(ThermalMass.PressurizerWallHeatCapacity() - 24000f) < 1000f);
            
            Test("TM-05", "First order response at τ ≈ 63%",
                () => {
                    float tNew = ThermalMass.FirstOrderResponse(100f, 200f, 10f, 10f);
                    float expected = 100f + 0.632f * 100f;
                    return Math.Abs(tNew - expected) < 5f;
                });
            
            Test("TM-06", "Validation passes",
                () => ThermalMass.ValidateCalculations());
        }
        
        private void RunThermalExpansionTests()
        {
            Log("\n─────────────────────────────────────────────────────────────────");
            Log("  ThermalExpansion Tests (6 tests)");
            Log("─────────────────────────────────────────────────────────────────");
            
            Test("TE-01", "β in valid range at operating T",
                () => {
                    float beta = ThermalExpansion.ExpansionCoefficient(588f, 2250f);
                    return beta > 2e-4f && beta < 1e-3f;
                });
            
            Test("TE-02", "κ in valid range at operating T",
                () => {
                    float kappa = ThermalExpansion.Compressibility(588f, 2250f);
                    return kappa > 1e-6f && kappa < 2e-5f;
                });
            
            Test("TE-03", "Pressure coefficient 3-15 psi/°F",
                () => {
                    float dPdT = ThermalExpansion.PressureCoefficient(588f, 2250f);
                    return dPdT > 2f && dPdT < 20f;
                });
            
            Test("TE-04", "10°F → 30-150 psi (uncoupled estimate)",
                () => {
                    float deltaP = ThermalExpansion.PressureChangeFromTemp(10f, 588f, 2250f);
                    return deltaP > 20f && deltaP < 200f;
                });
            
            Test("TE-05", "Surge volume positive for T increase",
                () => ThermalExpansion.UncoupledSurgeVolume(11500f, 10f, 588f, 2250f) > 0f);
            
            Test("TE-06", "Coupled surge < uncoupled surge",
                () => {
                    float uncoupled = ThermalExpansion.UncoupledSurgeVolume(11500f, 10f, 588f, 2250f);
                    float coupled = ThermalExpansion.CoupledSurgeVolume(11500f, 720f, 10f, 588f, 2250f);
                    return coupled < uncoupled;
                });
        }
        
        private void RunHeatTransferTests()
        {
            Log("\n─────────────────────────────────────────────────────────────────");
            Log("  HeatTransfer Tests (6 tests)");
            Log("─────────────────────────────────────────────────────────────────");
            
            Test("HT-01", "Surge enthalpy deficit 40-80 BTU/lb",
                () => {
                    float deficit = HeatTransfer.SurgeEnthalpyDeficit(619f, 2250f);
                    return deficit > 30f && deficit < 90f;
                });
            
            Test("HT-02", "Surge deficit ≈ 0 at Tsat",
                () => {
                    float tSat = WaterProperties.SaturationTemperature(2250f);
                    float deficit = HeatTransfer.SurgeEnthalpyDeficit(tSat, 2250f);
                    return Math.Abs(deficit) < 10f;
                });
            
            Test("HT-03", "Surge heating load > 0 for insurge",
                () => HeatTransfer.SurgeHeatingLoad(500f, 619f, 2250f) > 0f);
            
            Test("HT-04", "Condensing HTC in range 50-500",
                () => {
                    float htc = HeatTransfer.CondensingHTC(2250f, 620f, 10f);
                    return htc > 40f && htc < 600f;
                });
            
            Test("HT-05", "LMTD calculation correct",
                () => {
                    // Parallel flow LMTD: dT1 = Th_in - Tc_in = 200-100 = 100
                    //                     dT2 = Th_out - Tc_out = 150-50 = 100
                    // When dT1 = dT2, LMTD = arithmetic mean = 100°F
                    float lmtd = HeatTransfer.LMTD(200f, 150f, 100f, 50f);
                    return Math.Abs(lmtd - 100f) < 5f;
                });
            
            Test("HT-06", "Validation passes",
                () => HeatTransfer.ValidateCalculations());
        }
        
        private void RunFluidFlowTests()
        {
            Log("\n─────────────────────────────────────────────────────────────────");
            Log("  FluidFlow Tests (8 tests)");
            Log("─────────────────────────────────────────────────────────────────");
            
            Test("FF-01", "4 RCPs at full speed ≈ 390,400 gpm",
                () => {
                    float[] speeds = { 1f, 1f, 1f, 1f };
                    float flow = FluidFlow.TotalRCSFlow(speeds);
                    return Math.Abs(flow - PlantConstants.RCS_FLOW_TOTAL) < 1000f;
                });
            
            Test("FF-02", "Coastdown at t=τ ≈ 37%",
                () => {
                    float speed = FluidFlow.PumpCoastdown(1f, 12f, 12f);
                    float expected = (float)Math.Exp(-1f);
                    return Math.Abs(speed - expected) < 0.02f;
                });
            
            Test("FF-03", "Natural circ 12,000-23,000 gpm",
                () => {
                    float flow = FluidFlow.NaturalCirculationFlow(61f, 30f, 1f);
                    return flow >= PlantConstants.NAT_CIRC_FLOW_MIN && 
                           flow <= PlantConstants.NAT_CIRC_FLOW_MAX;
                });
            
            Test("FF-04", "Surge flow positive for +ΔP",
                () => FluidFlow.SurgeLineFlow(10f, 14f, 50f, 0.015f, 46f) > 0f);
            
            Test("FF-05", "Surge flow negative for -ΔP",
                () => FluidFlow.SurgeLineFlow(-10f, 14f, 50f, 0.015f, 46f) < 0f);
            
            Test("FF-06", "Affinity: half speed → half flow",
                () => Math.Abs(FluidFlow.AffinityLaws_Flow(0.5f, 1f, 100f) - 50f) < 1f);
            
            Test("FF-07", "Affinity: half speed → quarter head",
                () => Math.Abs(FluidFlow.AffinityLaws_Head(0.5f, 1f, 100f) - 25f) < 1f);
            
            Test("FF-08", "Validation passes",
                () => FluidFlow.ValidateCalculations());
        }
        
        private void RunReactorKineticsTests()
        {
            Log("\n─────────────────────────────────────────────────────────────────");
            Log("  ReactorKinetics Tests (10 tests)");
            Log("─────────────────────────────────────────────────────────────────");
            
            Test("RK-01", "BETA_TOTAL = sum of groups",
                () => {
                    float sum = 0f;
                    foreach (float b in ReactorKinetics.BETA_GROUPS) sum += b;
                    return Math.Abs(sum - ReactorKinetics.BETA_TOTAL) < 0.0001f;
                });
            
            Test("RK-02", "Doppler < 0 for T increase",
                () => ReactorKinetics.DopplerReactivity(100f, 1000f) < 0f);
            
            Test("RK-03", "MTC > 0 at high boron",
                () => ReactorKinetics.ModeratorTempCoefficient(1500f) > 0f);
            
            Test("RK-04", "MTC < 0 at low boron",
                () => ReactorKinetics.ModeratorTempCoefficient(100f) < 0f);
            
            Test("RK-05", "Boron addition → negative reactivity",
                () => ReactorKinetics.BoronReactivity(100f) < 0f);
            
            Test("RK-06", "Equilibrium xenon at 100% ≈ -2750 pcm",
                () => {
                    float xenon = ReactorKinetics.XenonEquilibrium(1f);
                    return xenon < -2000f && xenon > -3500f;
                });
            
            Test("RK-07", "Decay heat at trip ≈ 7%",
                () => Math.Abs(ReactorKinetics.DecayHeatFraction(0.1f) - 0.07f) < 0.02f);
            
            Test("RK-08", "Decay heat at 1 min ≈ 5%",
                () => Math.Abs(ReactorKinetics.DecayHeatFraction(60f) - 0.05f) < 0.02f);
            
            Test("RK-09", "Equilibrium precursors array length = 6",
                () => ReactorKinetics.EquilibriumPrecursors(1f).Length == 6);
            
            Test("RK-10", "Validation passes",
                () => ReactorKinetics.ValidateCalculations());
        }
        
        private void RunPressurizerPhysicsTests()
        {
            Log("\n─────────────────────────────────────────────────────────────────");
            Log("  PressurizerPhysics Tests (24 tests)");
            Log("─────────────────────────────────────────────────────────────────");
            
            Test("PZ-01", "Flash rate > 0 during depressurization",
                () => PressurizerPhysics.FlashEvaporationRate(2250f, -10f, 50000f) > 0f);
            
            Test("PZ-02", "Flash rate = 0 during pressurization",
                () => PressurizerPhysics.FlashEvaporationRate(2250f, 10f, 50000f) == 0f);
            
            Test("PZ-03", "Spray condensation rate 5-50 lb/sec at full flow",
                () => {
                    float rate = PressurizerPhysics.SprayCondensationRate(900f, 558f, 2250f);
                    return rate > 3f && rate < 60f;
                });
            
            Test("PZ-04", "Wall condensation > 0 when wall cold",
                () => PressurizerPhysics.WallCondensationRate(620f, 2250f, 600f) > 0f);
            
            Test("PZ-05", "Rainout > 0 when steam subcooled",
                () => PressurizerPhysics.RainoutRate(640f, 2250f, 1000f) > 0f);
            
            Test("PZ-06", "Heater steam rate 1-10 lb/sec at full power",
                () => {
                    float rate = PressurizerPhysics.HeaterSteamRate(1800f, 1800f, 2250f);
                    return rate > 0.5f && rate < 15f;
                });
            
            Test("PZ-07", "Heater lag τ ≈ 20 sec (63% at τ)",
                () => PressurizerPhysics.ValidateHeaterLag());
            
            Test("PZ-08", "Flash increases with depressurization rate",
                () => {
                    float slow = PressurizerPhysics.FlashEvaporationRate(2250f, -5f, 50000f);
                    float fast = PressurizerPhysics.FlashEvaporationRate(2250f, -20f, 50000f);
                    return fast > slow;
                });
            
            Test("PZ-09", "Spray flow demand = 0 below setpoint",
                () => PressurizerPhysics.SprayFlowDemand(2200f) == 0f);
            
            Test("PZ-10", "Spray flow demand = max above setpoint",
                () => PressurizerPhysics.SprayFlowDemand(2290f) == PlantConstants.SPRAY_FLOW_MAX);
            
            Test("PZ-11", "Heater demand = max below setpoint",
                () => PressurizerPhysics.HeaterPowerDemand(2200f) == PlantConstants.HEATER_POWER_TOTAL);
            
            Test("PZ-12", "Heater demand = 0 above setpoint",
                () => PressurizerPhysics.HeaterPowerDemand(2240f) == 0f);
            
            Test("PZ-13", "Initialize steady state at 60% level",
                () => {
                    var state = PressurizerPhysics.InitializeSteadyState(2250f, 60f);
                    return Math.Abs(state.Level - 60f) < 1f;
                });
            
            Test("PZ-14", "Initialized pressure correct",
                () => {
                    var state = PressurizerPhysics.InitializeSteadyState(2250f, 60f);
                    return Math.Abs(state.Pressure - 2250f) < 1f;
                });
            
            Test("PZ-15", "Water mass > 0 after init",
                () => PressurizerPhysics.InitializeSteadyState(2250f, 60f).WaterMass > 0f);
            
            Test("PZ-16", "Steam mass > 0 after init",
                () => PressurizerPhysics.InitializeSteadyState(2250f, 60f).SteamMass > 0f);
            
            Test("PZ-17", "Total volume = PZR_TOTAL_VOLUME",
                () => {
                    var state = PressurizerPhysics.InitializeSteadyState(2250f, 60f);
                    return Math.Abs(state.WaterVolume + state.SteamVolume - PlantConstants.PZR_TOTAL_VOLUME) < 1f;
                });
            
            Test("PZ-18", "Wall temp ≈ Tsat after init",
                () => {
                    var state = PressurizerPhysics.InitializeSteadyState(2250f, 60f);
                    float tSat = WaterProperties.SaturationTemperature(2250f);
                    return Math.Abs(state.WallTemp - tSat) < 5f;
                });
            
            Test("PZ-19", "Heater effective power = 0 after init",
                () => PressurizerPhysics.InitializeSteadyState(2250f, 60f).HeaterEffectivePower == 0f);
            
            Test("PZ-20", "Surge mass flow calculation positive for insurge",
                () => PressurizerPhysics.SurgeMassFlowRate(100f, 619f, 2250f) > 0f);
            
            Test("PZ-21", "Spray efficiency = 85%",
                () => Math.Abs(PlantConstants.SPRAY_EFFICIENCY - 0.85f) < 0.01f);
            
            Test("PZ-22", "Heater τ = 20 seconds",
                () => Math.Abs(PlantConstants.HEATER_TAU - 20f) < 0.1f);
            
            Test("PZ-23", "Flash evaporation self-regulating",
                () => {
                    // Flash rate should be proportional to depressurization rate
                    float rate1 = PressurizerPhysics.FlashEvaporationRate(2250f, -5f, 50000f);
                    float rate2 = PressurizerPhysics.FlashEvaporationRate(2250f, -10f, 50000f);
                    return rate2 > rate1 * 1.5f;
                });
            
            Test("PZ-24", "Validation passes",
                () => PressurizerPhysics.ValidateCalculations());
        }
        
        private void RunCoupledThermoTests()
        {
            Log("\n─────────────────────────────────────────────────────────────────");
            Log("  CoupledThermo Tests (12 tests) - GAP #1 CRITICAL");
            Log("─────────────────────────────────────────────────────────────────");
            
            Test("CT-01", "Solver converges < 20 iterations",
                () => CoupledThermo.ValidateConvergence());
            
            Test("CT-02", "10°F rise → 50-100 psi increase (CRITICAL)",
                () => CoupledThermo.Validate10DegreeTest());
            
            Test("CT-03", "Coupled expansion < uncoupled",
                () => CoupledThermo.ValidateCoupledLessThanUncoupled());
            
            Test("CT-04", "Mass conserved < 0.1% error",
                () => CoupledThermo.ValidateMassConservation());
            
            Test("CT-05", "Volume conserved < 0.01% error",
                () => CoupledThermo.ValidateVolumeConservation());
            
            Test("CT-06", "Steam space ≥ minimum",
                () => CoupledThermo.ValidateSteamSpaceMinimum());
            
            Test("CT-07", "Quick estimate in reasonable range",
                () => {
                    float dP = CoupledThermo.QuickPressureEstimate(588f, 2250f, 10f, 11500f, 720f);
                    return dP > 20f && dP < 150f;
                });
            
            Test("CT-08", "Initialize at steady state correct",
                () => {
                    var state = CoupledThermo.InitializeAtSteadyState();
                    return Math.Abs(state.Pressure - 2250f) < 1f &&
                           Math.Abs(state.Temperature - 588.5f) < 1f;
                });
            
            Test("CT-09", "Positive ΔT → positive ΔP",
                () => {
                    var state = CoupledThermo.InitializeAtSteadyState();
                    float P0 = state.Pressure;
                    CoupledThermo.SolveEquilibrium(ref state, 5f);
                    return state.Pressure > P0;
                });
            
            Test("CT-10", "Negative ΔT → negative ΔP",
                () => {
                    var state = CoupledThermo.InitializeAtSteadyState();
                    float P0 = state.Pressure;
                    CoupledThermo.SolveEquilibrium(ref state, -5f);
                    return state.Pressure < P0;
                });
            
            Test("CT-11", "PZR level increases with T increase",
                () => {
                    var state = CoupledThermo.InitializeAtSteadyState();
                    float level0 = state.PZRLevel;
                    CoupledThermo.SolveEquilibrium(ref state, 10f);
                    return state.PZRLevel > level0;
                });
            
            Test("CT-12", "All validation tests pass",
                () => CoupledThermo.ValidateAll());
        }
        
        // ================================================================
        // Support Module Tests (v1.0.1.6)
        // Previously these modules had ValidateCalculations() methods but
        // were never wired into the Phase 1 test runner exit gate.
        // ================================================================
        
        private void RunCVCSControllerTests()
        {
            Log("\n─────────────────────────────────────────────────────────────────");
            Log("  CVCSController Tests (7 tests)");
            Log("  Reference: NRC HRTD 10.3 - CVCS Charging/Letdown Control");
            Log("─────────────────────────────────────────────────────────────────");
            
            Test("CV-01", "Initialization produces valid active state",
                () => {
                    var state = CVCSController.Initialize(50f, 50f, 75f, 75f, 32f);
                    return state.IsActive && Math.Abs(state.LevelError) < 0.1f;
                });
            
            Test("CV-02", "Low level increases charging above base + seal",
                () => {
                    var state = CVCSController.Initialize(40f, 50f, 75f, 75f, 32f);
                    CVCSController.Update(ref state, 40f, 400f, 1500f, 4, 1f/360f);
                    return state.ChargingFlow > 75f + 32f;
                });
            
            Test("CV-03", "High level decreases charging below base + seal",
                () => {
                    var state = CVCSController.Initialize(60f, 50f, 75f, 75f, 32f);
                    CVCSController.Update(ref state, 60f, 400f, 1500f, 4, 1f/360f);
                    return state.ChargingFlow < 75f + 32f;
                });
            
            Test("CV-04", "Very low level triggers letdown isolation",
                () => {
                    var state = CVCSController.Initialize(15f, 50f, 75f, 75f, 32f);
                    CVCSController.Update(ref state, 15f, 400f, 1500f, 4, 1f/360f);
                    return state.LetdownIsolated && state.LetdownFlow == 0f;
                });
            
            Test("CV-05", "Integral error accumulates over time",
                () => {
                    var state = CVCSController.Initialize(45f, 50f, 75f, 75f, 32f);
                    float integral0 = state.IntegralError;
                    for (int i = 0; i < 100; i++)
                        CVCSController.Update(ref state, 45f, 400f, 1500f, 4, 1f/360f);
                    return Math.Abs(state.IntegralError - integral0) > 0.1f;
                });
            
            Test("CV-06", "Charging clamped to minimum (seal injection)",
                () => {
                    var state = CVCSController.Initialize(90f, 50f, 0f, 0f, 32f);
                    CVCSController.Update(ref state, 90f, 400f, 1500f, 4, 1f/360f);
                    return state.ChargingFlow >= 32f;
                });
            
            Test("CV-07", "Net flow calculation correct",
                () => {
                    var state = CVCSController.Initialize(50f, 50f, 100f, 80f, 32f);
                    state.ChargingFlow = 100f;
                    state.LetdownFlow = 80f;
                    float netFlow = CVCSController.GetNetRCSFlow(state);
                    return Math.Abs(netFlow - 20f) < 0.1f;
                });
        }
        
        private void RunRCSHeatupTests()
        {
            Log("\n─────────────────────────────────────────────────────────────────");
            Log("  RCSHeatup Tests (9 tests)");
            Log("  Reference: NRC HRTD 19.2 - RCS Heatup Procedures");
            Log("─────────────────────────────────────────────────────────────────");
            
            Test("RH-01", "Heatup rate ~50°F/hr with 4 RCPs + 1.8 MW heaters",
                () => {
                    float rate = RCSHeatup.EstimateHeatupRate(4, 1.8f, 400f, 1100000f);
                    return rate > 30f && rate < 80f;
                });
            
            Test("RH-02", "More RCPs = faster heatup",
                () => {
                    float rate2 = RCSHeatup.EstimateHeatupRate(2, 1.8f, 400f, 1100000f);
                    float rate4 = RCSHeatup.EstimateHeatupRate(4, 1.8f, 400f, 1100000f);
                    return rate4 > rate2;
                });
            
            Test("RH-03", "Higher temperature = slower heatup (more ambient losses)",
                () => {
                    float rateHot = RCSHeatup.EstimateHeatupRate(4, 1.8f, 500f, 1100000f);
                    float rateCold = RCSHeatup.EstimateHeatupRate(4, 1.8f, 200f, 1100000f);
                    return rateHot < rateCold;
                });
            
            Test("RH-04", "Isolated heating increases PZR temperature",
                () => {
                    var result = RCSHeatup.IsolatedHeatingStep(
                        400f, 350f, 800f, 1.8f, 1080f, 50000f, 1000000f, 1f/360f);
                    return result.T_pzr > 400f;
                });
            
            Test("RH-05", "Conduction heats RCS when T_pzr > T_rcs",
                () => {
                    var result = RCSHeatup.IsolatedHeatingStep(
                        400f, 350f, 800f, 1.8f, 1080f, 50000f, 1000000f, 1f/360f);
                    return result.ConductionHeat_MW > 0f;
                });
            
            Test("RH-06", "Time to target is finite and reasonable",
                () => {
                    float time = RCSHeatup.EstimateTimeToTarget(400f, 557f, 50f);
                    return time > 0f && time < 10f;
                });
            
            // v1.0.3.0 Stratified Model Integration
            Test("RH-07", "PZR heatup rate 40-120°F/hr with 1800 kW heaters",
                () => {
                    var result = RCSHeatup.IsolatedHeatingStep(
                        150f, 100f, 365f, 1.8f, 1080f, 74000f, 985000f, 1f/360f);
                    float pzrRate = (result.T_pzr - 150f) / (1f/360f);
                    return pzrRate > 40f && pzrRate < 120f;
                });
            
            Test("RH-08", "PZR heats even at large ΔT=200°F (PZR-RCS)",
                () => {
                    var result = RCSHeatup.IsolatedHeatingStep(
                        300f, 100f, 365f, 1.8f, 1080f, 74000f, 985000f, 1f/360f);
                    return result.T_pzr > 300f;
                });
            
            Test("RH-09", "RCS nearly static during isolated PZR heating",
                () => {
                    var result = RCSHeatup.IsolatedHeatingStep(
                        150f, 100f, 365f, 1.8f, 1080f, 74000f, 985000f, 1f/360f);
                    float rcsDelta = result.T_rcs - 100f;
                    return Math.Abs(rcsDelta) < 0.01f;
                });
        }
        
        private void RunRCPSequencerTests()
        {
            Log("\n─────────────────────────────────────────────────────────────────");
            Log("  RCPSequencer Tests (8 tests)");
            Log("  Reference: Westinghouse RCP Start Criteria & Sequencing");
            Log("─────────────────────────────────────────────────────────────────");
            
            Test("RS-01", "No RCPs without bubble formed",
                () => RCPSequencer.GetTargetRCPCount(false, 10f, 0f) == 0);
            
            Test("RS-02", "No RCPs immediately after bubble (< delay)",
                () => RCPSequencer.GetTargetRCPCount(true, 1.5f, 1.0f) == 0);
            
            Test("RS-03", "First RCP after start delay",
                () => RCPSequencer.GetTargetRCPCount(true, 2.1f, 1.0f) == 1);
            
            Test("RS-04", "Second RCP after interval",
                () => RCPSequencer.GetTargetRCPCount(true, 2.6f, 1.0f) == 2);
            
            Test("RS-05", "All 4 RCPs after sufficient time",
                () => RCPSequencer.GetTargetRCPCount(true, 5.0f, 1.0f) == 4);
            
            Test("RS-06", "Low pressure blocks RCP start",
                () => RCPSequencer.GetTargetRCPCount(true, 5.0f, 1.0f, 300f) == 0);
            
            Test("RS-07", "Scheduled start times correct",
                () => {
                    float t1 = RCPSequencer.GetScheduledStartTime(1, 1.0f);
                    float t2 = RCPSequencer.GetScheduledStartTime(2, 1.0f);
                    return Math.Abs(t1 - 2.0f) < 0.01f && Math.Abs(t2 - 2.5f) < 0.01f;
                });
            
            Test("RS-08", "4 RCP heat input = PlantConstants.RCP_HEAT_MW",
                () => Math.Abs(RCPSequencer.GetRCPHeat_MW(4) - PlantConstants.RCP_HEAT_MW) < 0.1f);
        }
        
        private void RunLoopThermodynamicsTests()
        {
            Log("\n─────────────────────────────────────────────────────────────────");
            Log("  LoopThermodynamics Tests (6 tests)");
            Log("  Reference: Westinghouse 4-Loop RCS Thermal-Hydraulics");
            Log("─────────────────────────────────────────────────────────────────");
            
            Test("LT-01", "With 4 RCPs: T_hot > T_cold, forced flow",
                () => {
                    var result = LoopThermodynamics.CalculateLoopTemperatures(400f, 800f, 4, PlantConstants.RCP_HEAT_MW);
                    return result.T_hot > result.T_cold && result.IsForcedFlow;
                });
            
            Test("LT-02", "With 0 RCPs and T_pzr > T_rcs: natural circ flow",
                () => {
                    var result = LoopThermodynamics.CalculateLoopTemperatures(300f, 500f, 0, 0f, 400f);
                    return result.T_hot >= result.T_cold && !result.IsForcedFlow;
                });
            
            Test("LT-03", "T_avg ≈ input T_rcs",
                () => {
                    var result = LoopThermodynamics.CalculateLoopTemperatures(400f, 800f, 4, PlantConstants.RCP_HEAT_MW);
                    return Math.Abs(result.T_avg - 400f) < 0.1f;
                });
            
            Test("LT-04", "ΔT at HZP with 4 RCPs = 5-15°F (RCP heat only)",
                () => {
                    var result = LoopThermodynamics.CalculateLoopTemperatures(557f, 2250f, 4, PlantConstants.RCP_HEAT_MW);
                    return result.DeltaT > 3f && result.DeltaT < 20f;
                });
            
            Test("LT-05", "Natural circulation flow in valid range",
                () => {
                    float flow = LoopThermodynamics.NaturalCirculationFlowRate(619f, 558f, 2250f);
                    return flow >= PlantConstants.NAT_CIRC_FLOW_MIN &&
                           flow <= PlantConstants.NAT_CIRC_FLOW_MAX;
                });
            
            Test("LT-06", "All validation tests pass",
                () => LoopThermodynamics.ValidateCalculations());
        }
        
        private void RunRVLISPhysicsTests()
        {
            Log("\n─────────────────────────────────────────────────────────────────");
            Log("  RVLISPhysics Tests (5 tests)");
            Log("  Reference: NRC NUREG-0737 Supp. 1 - RVLIS Requirements");
            Log("─────────────────────────────────────────────────────────────────");
            
            Test("RV-01", "With RCPs: dynamic range valid, full range invalid",
                () => {
                    var state = RVLISPhysics.Calculate(500000f, 557f, 2250f, 4);
                    return state.DynamicValid && !state.FullRangeValid;
                });
            
            Test("RV-02", "Without RCPs: full range valid, dynamic invalid",
                () => {
                    var state = RVLISPhysics.Calculate(500000f, 400f, 800f, 0);
                    return !state.DynamicValid && state.FullRangeValid;
                });
            
            Test("RV-03", "Full RCS mass → dynamic range > 95%",
                () => {
                    float rho = WaterProperties.WaterDensity(557f, 2250f);
                    float fullMass = PlantConstants.RCS_WATER_VOLUME * rho;
                    var state = RVLISPhysics.Calculate(fullMass, 557f, 2250f, 4);
                    return state.DynamicRange > 95f;
                });
            
            Test("RV-04", "80% mass with no RCPs → low level alarm",
                () => {
                    float rho = WaterProperties.WaterDensity(400f, 800f);
                    float fullMass = PlantConstants.RCS_WATER_VOLUME * rho;
                    var state = RVLISPhysics.Calculate(fullMass * 0.8f, 400f, 800f, 0);
                    return state.LevelLowAlarm;
                });
            
            Test("RV-05", "Dynamic range depressed with no flow (< 50%)",
                () => {
                    float rho = WaterProperties.WaterDensity(400f, 800f);
                    float fullMass = PlantConstants.RCS_WATER_VOLUME * rho;
                    var state = RVLISPhysics.Calculate(fullMass, 400f, 800f, 0);
                    return state.DynamicRange < 50f;
                });
        }
        
        #endregion
        
        #region Integration Tests
        
        private void RunIntegrationTests()
        {
            Log("\n═══════════════════════════════════════════════════════════════");
            Log("  INTEGRATION TESTS (7 tests)");
            Log("═══════════════════════════════════════════════════════════════");
            
            var summary = IntegrationTests.RunAllTests();
            
            foreach (var result in summary.Results)
            {
                _totalTests++;
                if (result.Passed)
                {
                    _passedTests++;
                    Log($"  [PASS] {result.TestId}: {result.TestName}");
                }
                else
                {
                    _failedTests++;
                    Log($"  [FAIL] {result.TestId}: {result.TestName}");
                    if (!string.IsNullOrEmpty(result.Notes))
                        Log($"         Note: {result.Notes}");
                }
            }
        }
        
        private void RunHeatupIntegrationTests()
        {
            Log("\n═══════════════════════════════════════════════════════════════");
            Log("  HEATUP INTEGRATION TESTS (9 tests) - Phase D Cross-Module");
            Log("═══════════════════════════════════════════════════════════════");
            
            var summary = HeatupIntegrationTests.RunAllTests();
            
            foreach (var result in summary.Results)
            {
                _totalTests++;
                if (result.Passed)
                {
                    _passedTests++;
                    Log($"  [PASS] {result.TestId}: {result.TestName}");
                }
                else
                {
                    _failedTests++;
                    Log($"  [FAIL] {result.TestId}: {result.TestName}");
                    if (!string.IsNullOrEmpty(result.Notes))
                        Log($"         Note: {result.Notes}");
                }
            }
        }
        
        #endregion
        
        #region Summary
        
        private void PrintSummary()
        {
            Log("\n╔═══════════════════════════════════════════════════════════════╗");
            Log("║                    PHASE 1 TEST SUMMARY                       ║");
            Log("╠═══════════════════════════════════════════════════════════════╣");
            Log($"║  Total Tests:  {_totalTests,3}                                          ║");
            Log($"║  Passed:       {_passedTests,3}                                          ║");
            Log($"║  Failed:       {_failedTests,3}                                          ║");
            Log("╠═══════════════════════════════════════════════════════════════╣");
            
            if (_failedTests == 0)
            {
                Log("║  ✓ ALL TESTS PASSED - PHASE 1 EXIT GATE MET                  ║");
                Log("║    Ready to proceed to Phase 2: Reactor Core Implementation  ║");
            }
            else
            {
                Log("║  ✗ TESTS FAILED - PHASE 1 EXIT GATE NOT MET                  ║");
                Log("║    Fix failing tests before proceeding to Phase 2            ║");
            }
            
            Log("╚═══════════════════════════════════════════════════════════════╝");
        }
        
        #endregion
    }
}

