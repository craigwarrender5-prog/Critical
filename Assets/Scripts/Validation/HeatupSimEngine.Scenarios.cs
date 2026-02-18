// ============================================================================
// CRITICAL: Master the Atom - Simulation Engine (Scenario Bridge Partial)
// HeatupSimEngine.Scenarios.cs
// ============================================================================
//
// PURPOSE:
//   Scenario-system bridge for validation execution.
//   Exposes selectable scenario IDs and deterministic start handoff while
//   preserving legacy StartSimulation behavior.
//
// SCOPE:
//   - Registry bootstrap for built-in validation scenario wrapper
//   - Scenario listing for future selector UI surfaces
//   - Scenario-start entrypoint by scenario ID
//
// GOLD STANDARD: Yes
// ============================================================================

using Critical.ScenarioSystem;
using UnityEngine;

namespace Critical.Validation
{
    public partial class HeatupSimEngine
    {
        [Header("Scenario Bridge (IP-0049)")]
        [Tooltip("When true, runOnStart uses StartScenarioById(startupScenarioId) instead of StartSimulation().")]
        public bool useScenarioStartPath = false;

        [Tooltip("Scenario ID used for scenario-path startup when useScenarioStartPath is enabled.")]
        public string startupScenarioId = ValidationHeatupScenario.ScenarioId;

        [HideInInspector] public string activeScenarioId = string.Empty;

        private static bool _scenarioRegistryBootstrapped;

        public string[] GetAvailableScenarioIds()
        {
            EnsureScenarioRegistryBootstrapped();
            return ScenarioRegistry.GetIds();
        }

        public ScenarioDescriptor[] GetAvailableScenarioDescriptors()
        {
            EnsureScenarioRegistryBootstrapped();
            return ScenarioRegistry.GetDescriptors();
        }

        public bool StartScenarioById(string scenarioId)
        {
            EnsureScenarioRegistryBootstrapped();

            if (!ScenarioRegistry.TryGet(scenarioId, out ISimulationScenario scenario))
            {
                LogEvent(EventSeverity.ALARM, $"SCENARIO START FAILED - UNKNOWN ID: {scenarioId}");
                return false;
            }

            bool started = scenario.TryStart(new ScenarioExecutionContext(this), out string reason);
            if (!started)
            {
                string failureReason = string.IsNullOrWhiteSpace(reason) ? "UNSPECIFIED" : reason;
                LogEvent(
                    EventSeverity.ALARM,
                    $"SCENARIO START FAILED - ID={scenario.Id}, NAME={scenario.DisplayName}, REASON={failureReason}");
                return false;
            }

            activeScenarioId = scenario.Id;
            LogEvent(EventSeverity.INFO, $"SCENARIO STARTED - ID={scenario.Id}, NAME={scenario.DisplayName}");
            return true;
        }

        private static void EnsureScenarioRegistryBootstrapped()
        {
            if (_scenarioRegistryBootstrapped)
            {
                return;
            }

            ScenarioRegistry.BootstrapFromFactories();

            _scenarioRegistryBootstrapped = true;
        }
    }
}
