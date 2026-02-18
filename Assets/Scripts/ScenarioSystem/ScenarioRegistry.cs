// ============================================================================
// CRITICAL: Master the Atom - Scenario Registry
// ScenarioRegistry.cs
// ============================================================================

using System;
using System.Collections.Generic;

namespace Critical.ScenarioSystem
{
    /// <summary>
    /// Minimal in-memory scenario registry used by runtime selection bridges.
    /// </summary>
    public static class ScenarioRegistry
    {
        private static readonly Dictionary<string, ISimulationScenario> Scenarios =
            new Dictionary<string, ISimulationScenario>(StringComparer.OrdinalIgnoreCase);

        public static bool Contains(string scenarioId)
        {
            if (string.IsNullOrWhiteSpace(scenarioId))
            {
                return false;
            }

            return Scenarios.ContainsKey(scenarioId);
        }

        public static bool Register(ISimulationScenario scenario, bool overwrite = false)
        {
            if (scenario == null || string.IsNullOrWhiteSpace(scenario.Id))
            {
                return false;
            }

            if (Scenarios.ContainsKey(scenario.Id) && !overwrite)
            {
                return false;
            }

            Scenarios[scenario.Id] = scenario;
            return true;
        }

        public static bool TryGet(string scenarioId, out ISimulationScenario scenario)
        {
            if (string.IsNullOrWhiteSpace(scenarioId))
            {
                scenario = null;
                return false;
            }

            return Scenarios.TryGetValue(scenarioId, out scenario);
        }

        public static string[] GetIds()
        {
            var ids = new string[Scenarios.Count];
            Scenarios.Keys.CopyTo(ids, 0);
            Array.Sort(ids, StringComparer.OrdinalIgnoreCase);
            return ids;
        }

        public static ScenarioDescriptor[] GetDescriptors()
        {
            var values = new List<ScenarioDescriptor>(Scenarios.Count);
            foreach (var scenario in Scenarios.Values)
            {
                values.Add(new ScenarioDescriptor(scenario.Id, scenario.DisplayName, scenario.DomainOwner));
            }

            values.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase));
            return values.ToArray();
        }
    }
}

