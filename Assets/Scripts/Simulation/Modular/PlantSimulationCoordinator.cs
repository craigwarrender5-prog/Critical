using System;
using System.Collections.Generic;
using Critical.Simulation.Modular.Modules;
using Critical.Simulation.Modular.Modules.PZR;
using Critical.Simulation.Modular.State;
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

            bool modularPzrEnabled = ModularFeatureFlags.UseModularPZR;
            bool bypassLegacyPzr = modularPzrEnabled && ModularFeatureFlags.BypassLegacyPZR;
            if (modularPzrEnabled && !bypassLegacyPzr)
            {
                throw new InvalidOperationException(
                    "Single-writer rule violation: UseModularPZR requires BypassLegacyPZR=true.");
            }

            if (modularPzrEnabled)
            {
                PressurizerSnapshot inputSnapshot = GetPressurizerInputSnapshot(dt);
                PressurizerOutputs pzrOutputs = _pressurizerModule.StepAuthoritative(
                    dt,
                    inputSnapshot,
                    _plantBus,
                    _stepIndex);
                _engine.ApplyModularPressurizerOutputs(pzrOutputs);
            }

            // Authoritative mutable-state path: legacy solver with optional PZR control bypass.
            _legacyModule.Step(dt, bypassLegacyPzr);

            if (modularPzrEnabled)
            {
                PressurizerSnapshot postSnapshot = LegacyStateBridge.ExportPressurizerSnapshot(_engine, dt);
                PlantState postPlantState = LegacyStateBridge.Export(_engine, dt);
                _pressurizerModule.CapturePostStepSnapshot(postSnapshot, postPlantState, _plantBus, _stepIndex);
            }

            // Stage D scaffolding: deterministic module slot order, no moved physics.
            if (ModularFeatureFlags.AnyModularExtractionEnabled())
            {
                RunDeterministicStubOrder(dt, skipPressurizerStep: modularPzrEnabled);
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

        private void RunDeterministicStubOrder(float dt, bool skipPressurizerStep)
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

            if (ModularFeatureFlags.UseModularPZR && !skipPressurizerStep)
            {
                _pressurizerModule.Step(dt);
                RunComparatorIfEnabled("PZR", ModularFeatureFlags.EnableComparatorPZR, _pressurizerModule.CaptureShadowState);
            }
            else if (ModularFeatureFlags.UseModularPZR && ModularFeatureFlags.EnableComparatorPZR)
            {
                RunComparatorIfEnabled("PZR", true, _pressurizerModule.CaptureShadowState);
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

        private PressurizerSnapshot GetPressurizerInputSnapshot(float dt)
        {
            StepSnapshot snapshot = _engine.GetStepSnapshot();
            if (snapshot?.PressurizerSnapshot != null && snapshot.PressurizerSnapshot != PressurizerSnapshot.Empty)
                return snapshot.PressurizerSnapshot;

            return LegacyStateBridge.ExportPressurizerSnapshot(_engine, dt);
        }

        private (bool, string) DetectUnledgeredMutation()
        {
            if (Math.Abs(_engine.surgeFlow) > 1e-6f &&
                !_plantBus.HasSignal(TransferIntentKinds.SignalSurgeFlowGpm, TransferQuantityType.FlowGpm))
            {
                return (true, $"{TransferIntentKinds.SignalSurgeFlowGpm} mutation observed without ledger event.");
            }

            if (Math.Abs(_engine.sprayFlow_GPM) > 1e-6f &&
                !_plantBus.HasSignal(TransferIntentKinds.SignalSprayFlowGpm, TransferQuantityType.FlowGpm))
            {
                return (true, $"{TransferIntentKinds.SignalSprayFlowGpm} mutation observed without ledger event.");
            }

            if (Math.Abs(_engine.pzrHeaterPower) > 1e-6f &&
                !_plantBus.HasSignal(TransferIntentKinds.SignalPzrHeaterPowerMw, TransferQuantityType.EnergyMw))
            {
                return (true, $"{TransferIntentKinds.SignalPzrHeaterPowerMw} mutation observed without ledger event.");
            }

            return (false, string.Empty);
        }
    }
}
