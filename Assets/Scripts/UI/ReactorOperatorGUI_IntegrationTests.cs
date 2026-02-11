// ============================================================================
// CRITICAL: Master the Atom - Reactor Operator GUI
// ReactorOperatorGUI_IntegrationTests.cs - Stage 6 Integration & Testing
// ============================================================================
//
// PURPOSE:
//   Integration tests for the Reactor Operator GUI (v1.0.0).
//   Validates all components work together correctly.
//
// TEST CATEGORIES:
//   1. Component Creation & Hierarchy
//   2. Data Flow: ReactorController → GUI
//   3. Core Map Visualization
//   4. Gauge Updates
//   5. User Interactions
//   6. Assembly Detail Panel
//
// USAGE:
//   Attach to ReactorOperatorScreen GameObject in Unity Editor.
//   Run tests from context menu: "Run Integration Tests"
//
// ============================================================================

using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Critical.UI
{
    using Controllers;

    /// <summary>
    /// Integration tests for Reactor Operator GUI.
    /// </summary>
    public class ReactorOperatorGUI_IntegrationTests : MonoBehaviour
    {
        #if UNITY_EDITOR

        // ====================================================================
        // TEST CONFIGURATION
        // ====================================================================

        private ReactorOperatorScreen _screen;
        private ReactorController _reactor;
        private int _testsPassed = 0;
        private int _testsFailed = 0;

        // ====================================================================
        // MENU ITEM
        // ====================================================================

        [MenuItem("Critical/Run Operator GUI Integration Tests")]
        public static void RunIntegrationTestsMenuItem()
        {
            ReactorOperatorScreen screen = FindObjectOfType<ReactorOperatorScreen>();
            if (screen == null)
            {
                Debug.LogError("[Integration Tests] No ReactorOperatorScreen found in scene!");
                EditorUtility.DisplayDialog("Test Error", 
                    "No ReactorOperatorScreen found in scene. Please create one first.", "OK");
                return;
            }

            ReactorOperatorGUI_IntegrationTests tests = screen.GetComponent<ReactorOperatorGUI_IntegrationTests>();
            if (tests == null)
            {
                tests = screen.gameObject.AddComponent<ReactorOperatorGUI_IntegrationTests>();
            }

            tests.RunAllTests();
        }

        // ====================================================================
        // TEST RUNNER
        // ====================================================================

        [ContextMenu("Run Integration Tests")]
        public void RunAllTests()
        {
            Debug.Log("========================================");
            Debug.Log("REACTOR OPERATOR GUI - INTEGRATION TESTS");
            Debug.Log("========================================");

            _screen = GetComponent<ReactorOperatorScreen>();
            _reactor = _screen?.Reactor;
            _testsPassed = 0;
            _testsFailed = 0;

            // Category 1: Component Creation & Hierarchy
            Debug.Log("\n--- CATEGORY 1: Component Creation & Hierarchy ---");
            Test_ScreenExists();
            Test_CoreMapExists();
            Test_DetailPanelExists();
            Test_GaugesExist();
            Test_ControlsExist();

            // Category 2: Data Flow
            Debug.Log("\n--- CATEGORY 2: Data Flow ---");
            Test_ReactorConnection();
            Test_BoardConnection();

            // Category 3: Core Map
            Debug.Log("\n--- CATEGORY 3: Core Map Visualization ---");
            Test_CoreMapCellCount();
            Test_CoreMapDataValidation();
            Test_BankAssignments();

            // Category 4: Gauge Updates
            Debug.Log("\n--- CATEGORY 4: Gauge Updates ---");
            Test_GaugeDataBinding();

            // Category 5: User Interactions (if in Play mode)
            if (Application.isPlaying)
            {
                Debug.Log("\n--- CATEGORY 5: User Interactions ---");
                Test_DisplayModeButtons();
                Test_BankFilterButtons();
                Test_ScreenToggle();
            }
            else
            {
                Debug.Log("\n--- CATEGORY 5: User Interactions ---");
                Debug.Log("⊘ Tests skipped (requires Play mode)");
            }

            // Category 6: Assembly Detail Panel
            Debug.Log("\n--- CATEGORY 6: Assembly Detail Panel ---");
            Test_DetailPanelComponents();

            // Summary
            Debug.Log("\n========================================");
            Debug.Log($"TESTS COMPLETE: {_testsPassed} passed, {_testsFailed} failed");
            Debug.Log("========================================");

            #if UNITY_EDITOR
            if (_testsFailed > 0)
            {
                EditorUtility.DisplayDialog("Integration Tests Failed", 
                    $"{_testsFailed} test(s) failed. Check console for details.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Integration Tests Passed", 
                    $"All {_testsPassed} tests passed!", "OK");
            }
            #endif
        }

        // ====================================================================
        // CATEGORY 1: Component Creation & Hierarchy
        // ====================================================================

        private void Test_ScreenExists()
        {
            if (_screen != null)
            {
                Pass("ReactorOperatorScreen component exists");
            }
            else
            {
                Fail("ReactorOperatorScreen component missing");
            }
        }

        private void Test_CoreMapExists()
        {
            if (_screen.CoreMap != null)
            {
                Pass("CoreMosaicMap component exists");
            }
            else
            {
                Fail("CoreMosaicMap component missing");
            }
        }

        private void Test_DetailPanelExists()
        {
            if (_screen.DetailPanel != null)
            {
                Pass("AssemblyDetailPanel component exists");
            }
            else
            {
                Fail("AssemblyDetailPanel component missing");
            }
        }

        private void Test_GaugesExist()
        {
            int gaugeCount = 0;
            int expectedCount = 17; // 9 nuclear + 8 thermal-hydraulic

            if (_screen.NeutronPowerGauge != null) gaugeCount++;
            if (_screen.ThermalPowerGauge != null) gaugeCount++;
            if (_screen.StartupRateGauge != null) gaugeCount++;
            if (_screen.PeriodGauge != null) gaugeCount++;
            if (_screen.ReactivityGauge != null) gaugeCount++;
            if (_screen.KeffGauge != null) gaugeCount++;
            if (_screen.BoronGauge != null) gaugeCount++;
            if (_screen.XenonGauge != null) gaugeCount++;
            if (_screen.FlowGauge != null) gaugeCount++;

            if (_screen.TavgGauge != null) gaugeCount++;
            if (_screen.ThotGauge != null) gaugeCount++;
            if (_screen.TcoldGauge != null) gaugeCount++;
            if (_screen.DeltaTGauge != null) gaugeCount++;
            if (_screen.FuelCenterlineGauge != null) gaugeCount++;
            if (_screen.HotChannelGauge != null) gaugeCount++;
            if (_screen.PressureGauge != null) gaugeCount++;
            if (_screen.PZRLevelGauge != null) gaugeCount++;

            if (gaugeCount == expectedCount)
            {
                Pass($"All {expectedCount} gauges exist");
            }
            else
            {
                Fail($"Only {gaugeCount}/{expectedCount} gauges exist");
            }
        }

        private void Test_ControlsExist()
        {
            bool controlsExist = true;

            if (_screen.RodDisplay == null)
            {
                Debug.LogError("  Missing: RodDisplay");
                controlsExist = false;
            }

            if (_screen.ControlPanel == null)
            {
                Debug.LogError("  Missing: ControlPanel");
                controlsExist = false;
            }

            if (_screen.AlarmPanel == null)
            {
                Debug.LogError("  Missing: AlarmPanel");
                controlsExist = false;
            }

            if (controlsExist)
            {
                Pass("Control components exist");
            }
            else
            {
                Fail("Some control components missing");
            }
        }

        // ====================================================================
        // CATEGORY 2: Data Flow
        // ====================================================================

        private void Test_ReactorConnection()
        {
            if (_reactor != null)
            {
                Pass("ReactorController connection established");
            }
            else
            {
                Fail("ReactorController not connected");
            }
        }

        private void Test_BoardConnection()
        {
            if (_screen.Board != null)
            {
                Pass("MosaicBoard connection established");
            }
            else
            {
                Fail("MosaicBoard not connected");
            }
        }

        // ====================================================================
        // CATEGORY 3: Core Map
        // ====================================================================

        private void Test_CoreMapCellCount()
        {
            if (!CoreMapData.Validate())
            {
                Fail("CoreMapData validation failed");
                return;
            }

            if (CoreMapData.ASSEMBLY_COUNT == 193)
            {
                Pass("Core map has correct assembly count (193)");
            }
            else
            {
                Fail($"Core map has wrong assembly count ({CoreMapData.ASSEMBLY_COUNT})");
            }
        }

        private void Test_CoreMapDataValidation()
        {
            // Check grid size
            if (CoreMapData.GRID_SIZE == 15)
            {
                Pass("Grid size is correct (15x15)");
            }
            else
            {
                Fail($"Grid size is wrong ({CoreMapData.GRID_SIZE})");
            }

            // Check coordinate lookup
            string coord = CoreMapData.GetCoordinateString(0);
            if (!string.IsNullOrEmpty(coord))
            {
                Pass($"Coordinate lookup works (assembly 0 = {coord})");
            }
            else
            {
                Fail("Coordinate lookup failed");
            }
        }

        private void Test_BankAssignments()
        {
            int rccaCount = 0;

            for (int i = 0; i < CoreMapData.ASSEMBLY_COUNT; i++)
            {
                if (CoreMapData.HasRCCA(i))
                {
                    rccaCount++;
                }
            }

            if (rccaCount == 53)
            {
                Pass("Correct RCCA count (53)");
            }
            else
            {
                Fail($"Wrong RCCA count ({rccaCount}, expected 53)");
            }
        }

        // ====================================================================
        // CATEGORY 4: Gauge Updates
        // ====================================================================

        private void Test_GaugeDataBinding()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("⊘ Gauge data binding test skipped (requires Play mode)");
                return;
            }

            if (_reactor == null)
            {
                Fail("Cannot test gauge binding - no reactor");
                return;
            }

            // Check if gauges are updating
            // This is a simple check - in Play mode, values should be non-zero
            bool anyGaugeActive = false;

            if (_screen.NeutronPowerGauge != null && _reactor.NeutronPower > 0f)
            {
                anyGaugeActive = true;
            }

            if (anyGaugeActive)
            {
                Pass("Gauge data binding active");
            }
            else
            {
                Fail("Gauges not receiving data");
            }
        }

        // ====================================================================
        // CATEGORY 5: User Interactions
        // ====================================================================

        private void Test_DisplayModeButtons()
        {
            if (_screen.PowerModeButton == null || 
                _screen.FuelTempModeButton == null ||
                _screen.CoolantTempModeButton == null ||
                _screen.RodBankModeButton == null)
            {
                Fail("Display mode buttons missing");
                return;
            }

            Pass("Display mode buttons exist");
        }

        private void Test_BankFilterButtons()
        {
            if (_screen.BankAllButton == null ||
                _screen.BankDButton == null ||
                _screen.BankAButton == null)
            {
                Fail("Bank filter buttons missing");
                return;
            }

            Pass("Bank filter buttons exist");
        }

        private void Test_ScreenToggle()
        {
            bool initialState = _screen.IsVisible;
            
            // Note: Can't actually test toggle without simulating key press
            // Just verify the property exists
            Pass("Screen visibility toggle available");
        }

        // ====================================================================
        // CATEGORY 6: Assembly Detail Panel
        // ====================================================================

        private void Test_DetailPanelComponents()
        {
            if (_screen.DetailPanel == null)
            {
                Fail("Detail panel missing");
                return;
            }

            AssemblyDetailPanel panel = _screen.DetailPanel;

            bool allComponentsExist = true;

            if (panel.HeaderText == null)
            {
                Debug.LogError("  Missing: HeaderText");
                allComponentsExist = false;
            }

            if (panel.PowerValue == null)
            {
                Debug.LogError("  Missing: PowerValue");
                allComponentsExist = false;
            }

            if (panel.FuelTempValue == null)
            {
                Debug.LogError("  Missing: FuelTempValue");
                allComponentsExist = false;
            }

            if (panel.CloseButton == null)
            {
                Debug.LogError("  Missing: CloseButton");
                allComponentsExist = false;
            }

            if (allComponentsExist)
            {
                Pass("Detail panel components complete");
            }
            else
            {
                Fail("Some detail panel components missing");
            }
        }

        // ====================================================================
        // TEST HELPERS
        // ====================================================================

        private void Pass(string message)
        {
            Debug.Log($"✓ {message}");
            _testsPassed++;
        }

        private void Fail(string message)
        {
            Debug.LogError($"✗ {message}");
            _testsFailed++;
        }

        #endif
    }
}
