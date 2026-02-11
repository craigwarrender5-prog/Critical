// CRITICAL: Master the Atom - Phase 2 Unity Test Runner
// Phase2UnityTestRunner.cs - Unity Editor Integration for Phase 2 Tests
//
// Provides editor menu and MonoBehaviour for running Phase 2 validation.
// Attach to any GameObject or use menu: Critical > Run Phase 2 Tests

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Critical.Tests
{
    /// <summary>
    /// Unity MonoBehaviour wrapper for Phase 2 test runner.
    /// </summary>
    public class Phase2UnityTestRunner : MonoBehaviour
    {
        [Header("Test Configuration")]
        [Tooltip("Run tests on Start")]
        public bool RunOnStart = false;
        
        [Tooltip("Run tests on key press")]
        public KeyCode TestKey = KeyCode.F5;
        
        private void Start()
        {
            if (RunOnStart)
            {
                RunAllTests();
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(TestKey))
            {
                RunAllTests();
            }
        }
        
        /// <summary>
        /// Run all Phase 2 tests.
        /// </summary>
        [ContextMenu("Run Phase 2 Tests")]
        public void RunAllTests()
        {
            Debug.Log("Starting Phase 2 Test Suite...\n");
            
            var runner = new Phase2TestRunner();
            runner.RunAllTests();
            
            Debug.Log("\nPhase 2 Tests Complete. Check console for results.");
        }
        
        #if UNITY_EDITOR
        
        /// <summary>
        /// Menu item to run Phase 2 tests from editor.
        /// </summary>
        [MenuItem("Critical/Run Phase 2 Tests %#t")]
        public static void RunTestsFromMenu()
        {
            Debug.Log("═══════════════════════════════════════════════════════════════");
            Debug.Log("  CRITICAL - Running Phase 2 Reactor Validation Tests");
            Debug.Log("═══════════════════════════════════════════════════════════════\n");
            
            var runner = new Phase2TestRunner();
            runner.RunAllTests();
        }
        
        /// <summary>
        /// Menu item to run quick smoke test.
        /// </summary>
        [MenuItem("Critical/Quick Smoke Test")]
        public static void RunSmokeTest()
        {
            Debug.Log("Running Quick Smoke Test...\n");
            
            int passed = 0;
            int failed = 0;
            
            // Test 1: FuelAssembly creates
            try
            {
                var fuel = Physics.FuelAssembly.CreateAverageAssembly(557f);
                fuel.Update(0.5f, 557f, 1.0f, 0.1f);
                if (fuel.CenterlineTemp_F > 557f)
                {
                    Debug.Log("✓ FuelAssembly: Creates and calculates temperature");
                    passed++;
                }
                else
                {
                    Debug.LogError("✗ FuelAssembly: Temperature not calculated");
                    failed++;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ FuelAssembly: Exception - {ex.Message}");
                failed++;
            }
            
            // Test 2: ControlRodBank creates
            try
            {
                var rods = new Physics.ControlRodBank();
                rods.SetAllBankPositions(Physics.ControlRodBank.STEPS_TOTAL);
                if (rods.TotalRodReactivity > 8000f)
                {
                    Debug.Log("✓ ControlRodBank: Creates with correct total worth");
                    passed++;
                }
                else
                {
                    Debug.LogError($"✗ ControlRodBank: Wrong total worth ({rods.TotalRodReactivity})");
                    failed++;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ ControlRodBank: Exception - {ex.Message}");
                failed++;
            }
            
            // Test 3: PowerCalculator creates
            try
            {
                var power = new Physics.PowerCalculator();
                power.SetPower(0.5f);
                if (System.Math.Abs(power.ThermalPower - 0.5f) < 0.01f)
                {
                    Debug.Log("✓ PowerCalculator: Creates and tracks power");
                    passed++;
                }
                else
                {
                    Debug.LogError($"✗ PowerCalculator: Wrong power ({power.ThermalPower})");
                    failed++;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ PowerCalculator: Exception - {ex.Message}");
                failed++;
            }
            
            // Test 4: FeedbackCalculator creates
            try
            {
                var feedback = new Physics.FeedbackCalculator();
                feedback.SetReferenceConditions(557f, 557f, 1500f);
                feedback.Update(600f, 570f, 1400f, -1000f, 500f);
                Debug.Log($"✓ FeedbackCalculator: Doppler={feedback.DopplerFeedback_pcm:F1}, MTC={feedback.MTCFeedback_pcm:F1}");
                passed++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ FeedbackCalculator: Exception - {ex.Message}");
                failed++;
            }
            
            // Test 5: ReactorCore creates
            try
            {
                var core = new Physics.ReactorCore();
                core.InitializeToHZP();
                if (System.Math.Abs(core.Tavg - 557f) < 5f)
                {
                    Debug.Log($"✓ ReactorCore: HZP initialized, Tavg={core.Tavg:F1}°F");
                    passed++;
                }
                else
                {
                    Debug.LogError($"✗ ReactorCore: Wrong HZP Tavg ({core.Tavg})");
                    failed++;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ ReactorCore: Exception - {ex.Message}");
                failed++;
            }
            
            // Test 6: ReactorCore at 100% power
            try
            {
                var core = new Physics.ReactorCore();
                core.InitializeToEquilibrium(1.0f);
                if (System.Math.Abs(core.ThermalPower - 1.0f) < 0.05f && 
                    System.Math.Abs(core.Tavg - 588f) < 5f)
                {
                    Debug.Log($"✓ ReactorCore: 100% power, P={core.ThermalPower*100:F1}%, Tavg={core.Tavg:F1}°F");
                    passed++;
                }
                else
                {
                    Debug.LogError($"✗ ReactorCore: Wrong 100% state (P={core.ThermalPower}, Tavg={core.Tavg})");
                    failed++;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ ReactorCore 100%: Exception - {ex.Message}");
                failed++;
            }
            
            Debug.Log($"\n═══ Smoke Test: {passed}/{passed + failed} passed ═══");
            
            if (failed == 0)
            {
                Debug.Log("✓ All smoke tests passed - modules functional");
            }
            else
            {
                Debug.LogError($"✗ {failed} smoke tests failed - check module compilation");
            }
        }
        
        /// <summary>
        /// Validate Westinghouse reference values.
        /// </summary>
        [MenuItem("Critical/Validate Reference Values")]
        public static void ValidateReferenceValues()
        {
            Debug.Log("═══════════════════════════════════════════════════════════════");
            Debug.Log("  Westinghouse 4-Loop PWR Reference Value Validation");
            Debug.Log("═══════════════════════════════════════════════════════════════\n");
            
            var pc = typeof(Physics.PlantConstants);
            
            Debug.Log("THERMAL-HYDRAULIC:");
            Debug.Log($"  Rated Power:     {Physics.PlantConstants.THERMAL_POWER_MWT:F0} MWt (expected: 3411)");
            Debug.Log($"  T-avg:           {Physics.PlantConstants.T_AVG:F1}°F (expected: 588.4)");
            Debug.Log($"  T-hot:           {Physics.PlantConstants.T_HOT:F1}°F (expected: 618.5)");
            Debug.Log($"  T-cold:          {Physics.PlantConstants.T_COLD:F1}°F (expected: 558.3)");
            Debug.Log($"  Core ΔT:         {Physics.PlantConstants.CORE_DELTA_T:F1}°F (expected: 60.2)");
            Debug.Log($"  RCS Pressure:    {Physics.PlantConstants.OPERATING_PRESSURE:F0} psia (expected: 2250)");
            
            Debug.Log("\nCORE GEOMETRY:");
            Debug.Log($"  Fuel Assemblies: {Physics.PlantConstants.FUEL_ASSEMBLIES} (expected: 193)");
            Debug.Log($"  Rods/Assembly:   {Physics.PlantConstants.RODS_PER_ASSEMBLY} (expected: 264)");
            Debug.Log($"  Total Rods:      {Physics.PlantConstants.TOTAL_RODS} (expected: 50,952)");
            Debug.Log($"  Active Height:   {Physics.FuelAssembly.ACTIVE_LENGTH_FT:F1} ft (expected: 12.0)");
            
            Debug.Log("\nREACTIVITY COEFFICIENTS:");
            Debug.Log($"  Doppler:         {Physics.PlantConstants.DOPPLER_COEFF:F1} pcm/√°R (expected: -2.5)");
            Debug.Log($"  MTC (BOL):       {Physics.PlantConstants.MTC_HIGH_BORON:F1} pcm/°F (expected: +5)");
            Debug.Log($"  MTC (EOL):       {Physics.PlantConstants.MTC_LOW_BORON:F1} pcm/°F (expected: -40)");
            Debug.Log($"  Boron Worth:     {Physics.PlantConstants.BORON_WORTH:F1} pcm/ppm (expected: -9)");
            
            Debug.Log("\nROD CONTROL:");
            Debug.Log($"  Total Steps:     {Physics.ControlRodBank.STEPS_TOTAL} (expected: 228)");
            Debug.Log($"  Rod Speed:       {Physics.ControlRodBank.STEPS_PER_MINUTE:F0} steps/min (expected: 72)");
            Debug.Log($"  Trip Time:       {Physics.ControlRodBank.ROD_DROP_TIME_SEC:F1} sec (expected: 2.0)");
            
            Debug.Log("\nXENON DYNAMICS:");
            Debug.Log($"  Xe Eq @ 100%:    {Physics.ReactorKinetics.XenonEquilibrium(1.0f):F0} pcm (expected: -2500 to -3000)");
            Debug.Log($"  Xe Eq @ 50%:     {Physics.ReactorKinetics.XenonEquilibrium(0.5f):F0} pcm");
            Debug.Log($"  Xe Eq @ 0%:      {Physics.ReactorKinetics.XenonEquilibrium(0f):F0} pcm (expected: 0)");
            
            Debug.Log("\n═══ Reference validation complete ═══");
        }
        
        #endif
    }
}
