// ============================================================================
// CRITICAL: Master the Atom - Validation Heatup Scenario Wrapper
// ValidationHeatupScenario.cs
// ============================================================================

using Critical.Validation;

namespace Critical.ScenarioSystem
{
    /// <summary>
    /// Exposes the existing validation runner through the scenario registry.
    /// </summary>
    public sealed class ValidationHeatupScenario : ISimulationScenario
    {
        public const string ScenarioId = "validation.heatup.baseline";

        public string Id => ScenarioId;
        public string DisplayName => "Validation Heatup Baseline";
        // Descriptor ownership follows the DP-0008 scenario-system surface.
        public string DomainOwner => "DP-0008";

        public bool TryStart(in ScenarioExecutionContext context, out string reason)
        {
            HeatupSimEngine engine = context.Engine;
            if (engine == null)
            {
                reason = "HeatupSimEngine context is null.";
                return false;
            }

            if (engine.isRunning)
            {
                reason = "Simulation already running.";
                return false;
            }

            // Preserve legacy semantics by using the canonical start path.
            engine.StartSimulation();
            reason = string.Empty;
            return true;
        }
    }
}
