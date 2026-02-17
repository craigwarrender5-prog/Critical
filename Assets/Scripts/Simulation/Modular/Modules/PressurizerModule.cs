using System;
using Critical.Physics;
using Critical.Simulation.Modular.Modules.PZR;
using Critical.Simulation.Modular.State;
using Critical.Simulation.Modular.Transfer;
using Critical.Simulation.Modular.Validation;

using Critical.Validation;
namespace Critical.Simulation.Modular.Modules
{
    public sealed class PressurizerModule : IPlantModule
    {
        private readonly PressurizerModuleState _state = new PressurizerModuleState();
        private PressurizerOutputs _lastOutputs = PressurizerOutputs.Empty;
        private float _lastInputSimTimeHr = float.MinValue;

        public PressurizerModule(HeatupSimEngine engine)
        {
            // Stage E: constructor signature retained for coordinator compatibility.
        }

        public string ModuleId => "PZR";
        public PressurizerOutputs LastOutputs => _lastOutputs;

        public void Initialize()
        {
            _state.Reset();
            _lastOutputs = PressurizerOutputs.Empty;
            _lastInputSimTimeHr = float.MinValue;
        }

        public void Step(float dt)
        {
            // Stage E authority path uses StepAuthoritative.
        }

        public void Shutdown()
        {
            _state.Reset();
            _lastOutputs = PressurizerOutputs.Empty;
            _lastInputSimTimeHr = float.MinValue;
        }

        public PressurizerOutputs StepAuthoritative(
            float dt,
            PressurizerSnapshot snapshot,
            PlantBus bus,
            int stepIndex)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));

            EnsureStateInitialized(snapshot);
            UpdateStartupHoldState(snapshot.SimTimeHr);

            float pzrLevelSetpointForHeater = (snapshot.SolidPressurizer || snapshot.BubblePreDrainPhase)
                ? 100f
                : PlantConstants.GetPZRLevelSetpointUnified(snapshot.TavgF);
            bool letdownIsolatedForHeater = snapshot.PzrLevelPct < PlantConstants.PZR_LOW_LEVEL_ISOLATION;

            if (_state.StartupHoldActive)
            {
                _state.HeaterPidOutput = 0f;
                _state.HeaterPidActive = false;
            }
            else if (_state.CurrentHeaterMode == HeaterMode.PRESSURIZE_AUTO &&
                     snapshot.PressurePsia >= PlantConstants.HEATER_MODE_TRANSITION_PRESSURE_PSIA)
            {
                _state.CurrentHeaterMode = HeaterMode.AUTOMATIC_PID;
                float currentPressurePsig = snapshot.PressurePsia - PlantConstants.PSIG_TO_PSIA;
                _state.HeaterPidState = CVCSController.InitializeHeaterPID(currentPressurePsig);
                _state.HeaterPidActive = true;
            }

            float heaterPowerMw;
            bool heatersOn;

            if (_state.StartupHoldActive)
            {
                heaterPowerMw = 0f;
                heatersOn = false;
                _state.HeaterPidOutput = 0f;
                _state.HeaterPidActive = false;
            }
            else if (_state.CurrentHeaterMode == HeaterMode.AUTOMATIC_PID)
            {
                float currentPsig = snapshot.PressurePsia - PlantConstants.PSIG_TO_PSIA;
                heaterPowerMw = CVCSController.UpdateHeaterPID(
                    ref _state.HeaterPidState,
                    currentPsig,
                    snapshot.PzrLevelPct,
                    dt);
                heatersOn = heaterPowerMw > 0.001f;
                _state.HeaterPidOutput = _state.HeaterPidState.SmoothedOutput;
                _state.HeaterPidActive = true;
            }
            else
            {
                HeaterControlState heaterState = CVCSController.CalculateHeaterState(
                    snapshot.PzrLevelPct,
                    pzrLevelSetpointForHeater,
                    letdownIsolatedForHeater,
                    snapshot.SolidPressurizer || snapshot.BubblePreDrainPhase,
                    PlantConstants.HEATER_POWER_TOTAL / 1000f,
                    _state.CurrentHeaterMode,
                    snapshot.PressureRatePsiPerHr,
                    dt,
                    _state.BubbleHeaterSmoothedOutput);

                heaterPowerMw = heaterState.HeaterPower_MW;
                heatersOn = heaterState.HeatersEnabled;

                if (_state.CurrentHeaterMode == HeaterMode.BUBBLE_FORMATION_AUTO ||
                    _state.CurrentHeaterMode == HeaterMode.PRESSURIZE_AUTO)
                {
                    _state.BubbleHeaterSmoothedOutput = heaterState.SmoothedOutput;
                }
            }

            CVCSController.UpdateSpray(
                ref _state.SprayState,
                snapshot.PressurePsia - PlantConstants.PSIG_TO_PSIA,
                snapshot.TpzrF,
                snapshot.TcoldF,
                snapshot.PzrSteamVolumeFt3,
                snapshot.PressurePsia,
                snapshot.RcpCount,
                dt);
            _state.SprayActive = _state.SprayState.IsActive;

            _lastOutputs = new PressurizerOutputs(
                heaterPowerMw,
                heatersOn,
                _state.HeaterPidOutput,
                _state.HeaterPidActive,
                _state.HeaterPidState,
                _state.SprayState,
                _state.SprayState.SprayFlow_GPM,
                _state.SprayState.ValvePosition,
                _state.SprayState.IsActive,
                _state.SprayState.SteamCondensed_lbm,
                _state.CurrentHeaterMode,
                _state.BubbleHeaterSmoothedOutput,
                _state.StartupHoldActive,
                _state.StartupHoldReleaseLogged,
                _state.StartupHoldActivationLogged,
                snapshot.PressurePsia,
                snapshot.PzrLevelPct,
                snapshot.PzrWaterVolumeFt3,
                snapshot.PzrSteamVolumeFt3,
                0f,
                0f);

            EmitPreStepIntents(bus, stepIndex, _lastOutputs);
            return _lastOutputs;
        }

        public void CapturePostStepSnapshot(
            PressurizerSnapshot snapshot,
            PlantState plantState,
            PlantBus bus,
            int stepIndex)
        {
            if (snapshot == null || plantState == null || bus == null)
                return;

            _lastOutputs = _lastOutputs.WithObservedStep(
                snapshot.PressurePsia,
                snapshot.PzrLevelPct,
                snapshot.PzrWaterVolumeFt3,
                snapshot.PzrSteamVolumeFt3,
                plantState.SurgeFlowGpm,
                plantState.PrimaryMassLedgerLb);

            if (Math.Abs(plantState.SurgeFlowGpm) > 1e-6f)
            {
                bus.EmitFlowTransfer(
                    stepIndex,
                    TransferIntentKinds.SignalSurgeFlowGpm,
                    "RCS",
                    "PZR",
                    plantState.SurgeFlowGpm,
                    isBoundary: false,
                    authorityPath: TransferIntentKinds.AuthorityModularPzr);
            }
        }

        public ModuleShadowState CaptureShadowState()
        {
            return new ModuleShadowState(
                _lastOutputs.ObservedPressurePsia,
                _lastOutputs.ObservedPzrLevelPct,
                _lastOutputs.ObservedPrimaryMassLedgerLb);
        }

        private void EnsureStateInitialized(PressurizerSnapshot snapshot)
        {
            bool rewound = snapshot.SimTimeHr + 1e-9f < _lastInputSimTimeHr;
            if (!_state.IsInitialized || rewound)
                _state.SeedFromSnapshot(snapshot);

            _lastInputSimTimeHr = snapshot.SimTimeHr;
        }

        private void UpdateStartupHoldState(float simTimeHr)
        {
            if (!_state.StartupHoldActive)
                return;

            if (!_state.StartupHoldActivationLogged)
                _state.StartupHoldActivationLogged = true;

            if (!_state.StartupHoldReleaseLogged && simTimeHr >= _state.StartupHoldReleaseTimeHr)
            {
                _state.StartupHoldActive = false;
                _state.StartupHoldReleaseLogged = true;

                // CS-0098 parity: modular authority path has no manual-disable
                // input yet, so OFF is always re-armed on hold release.
                if (_state.CurrentHeaterMode == HeaterMode.OFF)
                    _state.CurrentHeaterMode = HeaterMode.PRESSURIZE_AUTO;
            }
        }

        private static void EmitPreStepIntents(PlantBus bus, int stepIndex, PressurizerOutputs outputs)
        {
            if (Math.Abs(outputs.PzrHeaterPowerMw) > 1e-6f)
            {
                bus.EmitEnergyTransfer(
                    stepIndex,
                    TransferIntentKinds.SignalPzrHeaterPowerMw,
                    "GRID",
                    "PZR",
                    outputs.PzrHeaterPowerMw,
                    isBoundary: true,
                    authorityPath: TransferIntentKinds.AuthorityModularPzr);
            }

            if (Math.Abs(outputs.SprayFlowGpm) > 1e-6f)
            {
                bus.EmitFlowTransfer(
                    stepIndex,
                    TransferIntentKinds.SignalSprayFlowGpm,
                    "RCS",
                    "PZR",
                    outputs.SprayFlowGpm,
                    isBoundary: false,
                    authorityPath: TransferIntentKinds.AuthorityModularPzr);
            }

            if (Math.Abs(outputs.SpraySteamCondensedLbm) > 1e-6f)
            {
                bus.EmitMassTransfer(
                    stepIndex,
                    TransferIntentKinds.SignalSprayCondensedMassLb,
                    "PZR_STEAM",
                    "PZR_WATER",
                    outputs.SpraySteamCondensedLbm,
                    isBoundary: false,
                    authorityPath: TransferIntentKinds.AuthorityModularPzr);
            }
        }
    }
}

