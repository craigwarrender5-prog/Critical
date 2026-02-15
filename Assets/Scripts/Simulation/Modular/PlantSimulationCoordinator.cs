using System;
using System.Collections.Generic;
using Critical.Simulation.Modular.Modules;
using Critical.Simulation.Modular.Transfer;

namespace Critical.Simulation.Modular
{
    /// <summary>
    /// Coordinator-only orchestration shell. Contains no physics logic.
    /// </summary>
    public sealed class PlantSimulationCoordinator
    {
        private readonly List<IPlantModule> _activeModules = new List<IPlantModule>();
        private readonly HeatupSimEngine _engine;
        private readonly PlantBus _plantBus;
        private readonly LegacySimulatorModule _legacyModule;
        private TransferLedger _latestTransferLedger = TransferLedger.Empty;
        private int _stepIndex;
        private bool _initialized;

        public PlantSimulationCoordinator(HeatupSimEngine engine)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));

            _engine = engine;
            _plantBus = new PlantBus();
            _legacyModule = new LegacySimulatorModule(engine, _plantBus);

            // Stage A: coordinator runs only legacy adapter module.
            _activeModules.Add(_legacyModule);
        }

        public TransferLedger LatestTransferLedger => _latestTransferLedger;

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
            _stepIndex++;
            _plantBus.ClearStep();

            foreach (IPlantModule module in _activeModules)
                module.Step(dt);

            (bool unledgeredMutation, string reason) = DetectUnledgeredMutation();
            _latestTransferLedger = new TransferLedger(
                _stepIndex,
                _plantBus.SnapshotEvents(),
                unledgeredMutation,
                reason);
        }

        public void Shutdown()
        {
            if (!_initialized)
                return;

            foreach (IPlantModule module in _activeModules)
                module.Shutdown();

            _initialized = false;
        }

        private (bool, string) DetectUnledgeredMutation()
        {
            if (Math.Abs(_engine.surgeFlow) > 1e-6f &&
                !_plantBus.HasSignal("SURGE_FLOW_GPM", TransferQuantityType.FlowGpm))
            {
                return (true, "SURGE_FLOW_GPM mutation observed without ledger event.");
            }

            if (Math.Abs(_engine.sprayFlow_GPM) > 1e-6f &&
                !_plantBus.HasSignal("SPRAY_FLOW_GPM", TransferQuantityType.FlowGpm))
            {
                return (true, "SPRAY_FLOW_GPM mutation observed without ledger event.");
            }

            if (Math.Abs(_engine.pzrHeaterPower) > 1e-6f &&
                !_plantBus.HasSignal("PZR_HEATER_POWER_MW", TransferQuantityType.EnergyMw))
            {
                return (true, "PZR_HEATER_POWER_MW mutation observed without ledger event.");
            }

            return (false, string.Empty);
        }
    }
}
