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
        private static readonly List<Func<ISimulationScenario>> ScenarioFactories =
            new List<Func<ISimulationScenario>>();
        private static bool _defaultFactoriesInitialized;

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

        /// <summary>
        /// Register a scenario factory for future bootstrap passes.
        /// </summary>
        public static bool RegisterFactory(Func<ISimulationScenario> scenarioFactory)
        {
            if (scenarioFactory == null)
            {
                return false;
            }

            EnsureDefaultFactories();
            ScenarioFactories.Add(scenarioFactory);
            return true;
        }

        /// <summary>
        /// Bootstrap all configured scenario factories into the active registry.
        /// Returns number of new registrations applied.
        /// </summary>
        public static int BootstrapFromFactories(bool overwrite = false)
        {
            EnsureDefaultFactories();

            int registered = 0;
            foreach (Func<ISimulationScenario> factory in ScenarioFactories)
            {
                if (factory == null)
                {
                    continue;
                }

                ISimulationScenario scenario = factory();
                if (scenario == null)
                {
                    continue;
                }

                if (Register(scenario, overwrite))
                {
                    registered++;
                }
            }

            return registered;
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

        /// <summary>
        /// Seed built-in scenario factories once. Other domains can append factories
        /// through RegisterFactory() without modifying this registry core.
        /// </summary>
        private static void EnsureDefaultFactories()
        {
            if (_defaultFactoriesInitialized)
            {
                return;
            }

            _defaultFactoriesInitialized = true;
            ScenarioFactories.Add(() => new ValidationHeatupScenario());
        }
    }
}
