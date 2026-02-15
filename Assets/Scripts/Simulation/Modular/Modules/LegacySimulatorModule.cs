using System;

namespace Critical.Simulation.Modular.Modules
{
    /// <summary>
    /// Stage A legacy adapter that delegates physics stepping to HeatupSimEngine.
    /// </summary>
    public sealed class LegacySimulatorModule : IPlantModule
    {
        private readonly HeatupSimEngine _engine;

        public LegacySimulatorModule(HeatupSimEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public string ModuleId => "LEGACY_SIMULATOR";

        public void Initialize()
        {
            // Stage A: no extra initialization.
        }

        public void Step(float dt)
        {
            _engine.RunLegacySimulationStepForCoordinator(dt);
        }

        public void Shutdown()
        {
            // Stage A: no shutdown hooks.
        }
    }
}
