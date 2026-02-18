// ============================================================================
// CRITICAL: Master the Atom - Scenario System Contracts
// ISimulationScenario.cs
// ============================================================================

using System;
using Critical.Validation;

namespace Critical.ScenarioSystem
{
    /// <summary>
    /// Immutable descriptor used for scenario listing surfaces.
    /// </summary>
    public readonly struct ScenarioDescriptor
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly string DomainOwner;

        public ScenarioDescriptor(string id, string displayName, string domainOwner)
        {
            Id = id;
            DisplayName = displayName;
            DomainOwner = domainOwner;
        }
    }

    /// <summary>
    /// Start context passed into scenario wrappers.
    /// </summary>
    public readonly struct ScenarioExecutionContext
    {
        public readonly HeatupSimEngine Engine;

        public ScenarioExecutionContext(HeatupSimEngine engine)
        {
            Engine = engine;
        }
    }

    /// <summary>
    /// Minimal scenario wrapper contract for deterministic start handoff.
    /// </summary>
    public interface ISimulationScenario
    {
        string Id { get; }
        string DisplayName { get; }
        string DomainOwner { get; }

        bool TryStart(in ScenarioExecutionContext context, out string reason);
    }
}

