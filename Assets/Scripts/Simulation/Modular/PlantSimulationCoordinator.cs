using System;
using System.Collections.Generic;
using Critical.Simulation.Modular.Modules;

namespace Critical.Simulation.Modular
{
    /// <summary>
    /// Coordinator-only orchestration shell. Contains no physics logic.
    /// </summary>
    public sealed class PlantSimulationCoordinator
    {
        private readonly List<IPlantModule> _activeModules = new List<IPlantModule>();
        private readonly LegacySimulatorModule _legacyModule;
        private bool _initialized;

        public PlantSimulationCoordinator(HeatupSimEngine engine)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));

            _legacyModule = new LegacySimulatorModule(engine);

            // Stage A: coordinator runs only legacy adapter module.
            _activeModules.Add(_legacyModule);
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            foreach (IPlantModule module in _activeModules)
                module.Initialize();

            _initialized = true;
        }

        public void Step(float dt)
        {
            Initialize();

            foreach (IPlantModule module in _activeModules)
                module.Step(dt);
        }

        public void Shutdown()
        {
            if (!_initialized)
                return;

            foreach (IPlantModule module in _activeModules)
                module.Shutdown();

            _initialized = false;
        }
    }
}
