using System;
using Critical.Simulation.Modular.Transfer;

namespace Critical.Simulation.Modular.Modules
{
    /// <summary>
    /// Stage A legacy adapter that delegates physics stepping to HeatupSimEngine.
    /// </summary>
    public sealed class LegacySimulatorModule : IPlantModule
    {
        private readonly HeatupSimEngine _engine;
        private readonly PlantBus _plantBus;
        private int _lastPbocTickRecorded = -1;

        public LegacySimulatorModule(HeatupSimEngine engine, PlantBus plantBus)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _plantBus = plantBus ?? throw new ArgumentNullException(nameof(plantBus));
        }

        public string ModuleId => "LEGACY_SIMULATOR";

        public void Initialize()
        {
            // Stage A: no extra initialization.
        }

        public void Step(float dt)
        {
            Step(dt, bypassLegacyPressurizerControl: false);
        }

        public void Step(float dt, bool bypassLegacyPressurizerControl)
        {
            _engine.RunLegacySimulationStepForCoordinator(dt, bypassLegacyPressurizerControl);
            EmitTransferIntents(dt);
        }

        public void Shutdown()
        {
            // Stage A: no shutdown hooks.
        }

        private void EmitTransferIntents(float dt)
        {
            int stepIndex = (int)Math.Round(_engine.simTime / dt);
            bool modularPzrAuthoritative = ModularFeatureFlags.UseModularPZR && ModularFeatureFlags.BypassLegacyPZR;

            if (!modularPzrAuthoritative && Math.Abs(_engine.surgeFlow) > 1e-6f)
            {
                _plantBus.EmitFlowTransfer(
                    stepIndex,
                    TransferIntentKinds.SignalSurgeFlowGpm,
                    "RCS",
                    "PZR",
                    _engine.surgeFlow,
                    isBoundary: false,
                    authorityPath: TransferIntentKinds.AuthorityLegacyStep);
            }

            if (!modularPzrAuthoritative && Math.Abs(_engine.sprayFlow_GPM) > 1e-6f)
            {
                _plantBus.EmitFlowTransfer(
                    stepIndex,
                    TransferIntentKinds.SignalSprayFlowGpm,
                    "RCS",
                    "PZR",
                    _engine.sprayFlow_GPM,
                    isBoundary: false,
                    authorityPath: TransferIntentKinds.AuthorityLegacyStep);
            }

            if (!modularPzrAuthoritative && Math.Abs(_engine.pzrHeaterPower) > 1e-6f)
            {
                _plantBus.EmitEnergyTransfer(
                    stepIndex,
                    TransferIntentKinds.SignalPzrHeaterPowerMw,
                    "GRID",
                    "PZR",
                    _engine.pzrHeaterPower,
                    isBoundary: true,
                    authorityPath: TransferIntentKinds.AuthorityLegacyStep);
            }

            HeatupSimEngine.PrimaryBoundaryFlowEvent pboc = _engine.pbocLastEvent;
            if (pboc.TickIndex > 0 && pboc.TickIndex != _lastPbocTickRecorded)
            {
                _lastPbocTickRecorded = pboc.TickIndex;

                if (Math.Abs(pboc.MassIn_lbm) > 1e-6f)
                {
                    _plantBus.EmitMassTransfer(
                        stepIndex,
                        TransferIntentKinds.SignalPrimaryBoundaryInLb,
                        "EXTERNAL",
                        "PRIMARY",
                        pboc.MassIn_lbm,
                        isBoundary: true,
                        authorityPath: TransferIntentKinds.AuthorityPbocEvent);
                }

                if (Math.Abs(pboc.MassOut_lbm) > 1e-6f)
                {
                    _plantBus.EmitMassTransfer(
                        stepIndex,
                        TransferIntentKinds.SignalPrimaryBoundaryOutLb,
                        "PRIMARY",
                        "EXTERNAL",
                        pboc.MassOut_lbm,
                        isBoundary: true,
                        authorityPath: TransferIntentKinds.AuthorityPbocEvent);
                }
            }
        }
    }
}
