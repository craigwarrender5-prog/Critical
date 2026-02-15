using System;
using System.Collections.Generic;
using Critical.Simulation.Modular.Modules;
using Critical.Simulation.Modular.Transfer;
using Critical.Simulation.Modular.Validation;

namespace Critical.Simulation.Modular
{
    /// <summary>
    /// Coordinator-only orchestration shell. Contains no physics logic.
    /// </summary>
    public sealed class PlantSimulationCoordinator
    {
        private readonly List<IPlantModule> _allModules = new List<IPlantModule>();
        private readonly HeatupSimEngine _engine;
        private readonly PlantBus _plantBus;
        private readonly LegacySimulatorModule _legacyModule;
        private readonly ReactorModule _reactorModule;
        private readonly RCPModule _rcpModule;
        private readonly RCSModule _rcsModule;
        private readonly PressurizerModule _pressurizerModule;
        private readonly CVCSModule _cvcsModule;
        private readonly RHRModule _rhrModule;
        private readonly Dictionary<string, ModuleComparatorResult> _lastComparatorResults =
            new Dictionary<string, ModuleComparatorResult>();

        private TransferLedger _latestTransferLedger = TransferLedger.Empty;
        private int _stepIndex;
        private bool _initialized;

        private const float ComparatorPressureTol = 1e-4f;
        private const float ComparatorLevelTol = 1e-4f;
        private const float ComparatorMassTol = 1e-3f;

        public PlantSimulationCoordinator(HeatupSimEngine engine)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));

            _engine = engine;
            _plantBus = new PlantBus();
            _legacyModule = new LegacySimulatorModule(engine, _plantBus);
            _reactorModule = new ReactorModule(engine);
            _rcpModule = new RCPModule(engine);
            _rcsModule = new RCSModule(engine);
            _pressurizerModule = new PressurizerModule(engine);
            _cvcsModule = new CVCSModule(engine);
            _rhrModule = new RHRModule(engine);

            _allModules.Add(_legacyModule);
            _allModules.Add(_reactorModule);
            _allModules.Add(_rcpModule);
            _allModules.Add(_rcsModule);
            _allModules.Add(_pressurizerModule);
            _allModules.Add(_cvcsModule);
            _allModules.Add(_rhrModule);
        }

        public TransferLedger LatestTransferLedger => _latestTransferLedger;
        public IReadOnlyDictionary<string, ModuleComparatorResult> LastComparatorResults => _lastComparatorResults;

        public void Initialize()
        {
            if (_initialized)
                return;

            foreach (IPlantModule module in _allModules)
                module.Initialize();

            _initialized = true;
        }

        public void Step(float dt)
        {
            Initialize();
            _stepIndex++;
            _plantBus.ClearStep();
            _lastComparatorResults.Clear();

            // Authoritative mutable-state path (Stage A-D): legacy adapter only.
            _legacyModule.Step(dt);

            // Stage D scaffolding: deterministic module slot order, no moved physics.
            if (ModularFeatureFlags.AnyModularExtractionEnabled())
            {
                RunDeterministicStubOrder(dt);
            }

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

            foreach (IPlantModule module in _allModules)
                module.Shutdown();

            _initialized = false;
        }

        private void RunDeterministicStubOrder(float dt)
        {
            // Provisional parity order for extraction scaffolding:
            // Reactor -> RCP -> RCS -> PZR -> CVCS -> RHR
            if (ModularFeatureFlags.UseModularReactor)
            {
                _reactorModule.Step(dt);
                RunComparatorIfEnabled("REACTOR", ModularFeatureFlags.EnableComparatorReactor, _reactorModule.CaptureShadowState);
            }

            if (ModularFeatureFlags.UseModularRCP)
            {
                _rcpModule.Step(dt);
                RunComparatorIfEnabled("RCP", ModularFeatureFlags.EnableComparatorRCP, _rcpModule.CaptureShadowState);
            }

            if (ModularFeatureFlags.UseModularRCS)
            {
                _rcsModule.Step(dt);
                RunComparatorIfEnabled("RCS", ModularFeatureFlags.EnableComparatorRCS, _rcsModule.CaptureShadowState);
            }

            if (ModularFeatureFlags.UseModularPZR)
            {
                _pressurizerModule.Step(dt);
                RunComparatorIfEnabled("PZR", ModularFeatureFlags.EnableComparatorPZR, _pressurizerModule.CaptureShadowState);
            }

            if (ModularFeatureFlags.UseModularCVCS)
            {
                _cvcsModule.Step(dt);
                RunComparatorIfEnabled("CVCS", ModularFeatureFlags.EnableComparatorCVCS, _cvcsModule.CaptureShadowState);
            }

            if (ModularFeatureFlags.UseModularRHR)
            {
                _rhrModule.Step(dt);
                RunComparatorIfEnabled("RHR", ModularFeatureFlags.EnableComparatorRHR, _rhrModule.CaptureShadowState);
            }
        }

        private void RunComparatorIfEnabled(string moduleId, bool enabled, Func<ModuleShadowState> modularShadowCapture)
        {
            if (!enabled)
                return;

            // Comparator contract: both captures are side-effect free temporary value objects.
            ModuleShadowState legacyShadow = ModuleComparator.CaptureShadow(CaptureLegacyShadow);
            ModuleShadowState modularShadow = ModuleComparator.CaptureShadow(modularShadowCapture);
            ModuleComparatorResult result = ModuleComparator.Compare(
                moduleId,
                legacyShadow,
                modularShadow,
                ComparatorPressureTol,
                ComparatorLevelTol,
                ComparatorMassTol);
            _lastComparatorResults[moduleId] = result;
        }

        private ModuleShadowState CaptureLegacyShadow()
        {
            return new ModuleShadowState(_engine.pressure, _engine.pzrLevel, _engine.primaryMassLedger_lb);
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
