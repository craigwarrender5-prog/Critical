using Critical.Physics;

namespace Critical.Simulation.Modular.Modules.PZR
{
    /// <summary>
    /// Immutable module output payload for Stage E PZR packaging.
    /// </summary>
    public sealed class PressurizerOutputs
    {
        public static readonly PressurizerOutputs Empty = new PressurizerOutputs(
            0f,
            false,
            0f,
            false,
            default,
            default,
            0f,
            0f,
            false,
            0f,
            HeaterMode.STARTUP_FULL_POWER,
            0f,
            false,
            false,
            false,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f);

        public float PzrHeaterPowerMw { get; }
        public bool PzrHeatersOn { get; }
        public float HeaterPidOutput { get; }
        public bool HeaterPidActive { get; }
        public HeaterPIDState HeaterPidState { get; }
        public SprayControlState SprayState { get; }
        public float SprayFlowGpm { get; }
        public float SprayValvePosition { get; }
        public bool SprayActive { get; }
        public float SpraySteamCondensedLbm { get; }
        public HeaterMode CurrentHeaterMode { get; }
        public float BubbleHeaterSmoothedOutput { get; }
        public bool StartupHoldActive { get; }
        public bool StartupHoldReleaseLogged { get; }
        public bool StartupHoldActivationLogged { get; }
        public float ObservedPressurePsia { get; }
        public float ObservedPzrLevelPct { get; }
        public float ObservedPzrWaterVolumeFt3 { get; }
        public float ObservedPzrSteamVolumeFt3 { get; }
        public float ObservedSurgeFlowGpm { get; }
        public float ObservedPrimaryMassLedgerLb { get; }

        public PressurizerOutputs(
            float pzrHeaterPowerMw,
            bool pzrHeatersOn,
            float heaterPidOutput,
            bool heaterPidActive,
            HeaterPIDState heaterPidState,
            SprayControlState sprayState,
            float sprayFlowGpm,
            float sprayValvePosition,
            bool sprayActive,
            float spraySteamCondensedLbm,
            HeaterMode currentHeaterMode,
            float bubbleHeaterSmoothedOutput,
            bool startupHoldActive,
            bool startupHoldReleaseLogged,
            bool startupHoldActivationLogged,
            float observedPressurePsia,
            float observedPzrLevelPct,
            float observedPzrWaterVolumeFt3,
            float observedPzrSteamVolumeFt3,
            float observedSurgeFlowGpm,
            float observedPrimaryMassLedgerLb)
        {
            PzrHeaterPowerMw = pzrHeaterPowerMw;
            PzrHeatersOn = pzrHeatersOn;
            HeaterPidOutput = heaterPidOutput;
            HeaterPidActive = heaterPidActive;
            HeaterPidState = heaterPidState;
            SprayState = sprayState;
            SprayFlowGpm = sprayFlowGpm;
            SprayValvePosition = sprayValvePosition;
            SprayActive = sprayActive;
            SpraySteamCondensedLbm = spraySteamCondensedLbm;
            CurrentHeaterMode = currentHeaterMode;
            BubbleHeaterSmoothedOutput = bubbleHeaterSmoothedOutput;
            StartupHoldActive = startupHoldActive;
            StartupHoldReleaseLogged = startupHoldReleaseLogged;
            StartupHoldActivationLogged = startupHoldActivationLogged;
            ObservedPressurePsia = observedPressurePsia;
            ObservedPzrLevelPct = observedPzrLevelPct;
            ObservedPzrWaterVolumeFt3 = observedPzrWaterVolumeFt3;
            ObservedPzrSteamVolumeFt3 = observedPzrSteamVolumeFt3;
            ObservedSurgeFlowGpm = observedSurgeFlowGpm;
            ObservedPrimaryMassLedgerLb = observedPrimaryMassLedgerLb;
        }

        public PressurizerOutputs WithObservedStep(
            float pressurePsia,
            float pzrLevelPct,
            float pzrWaterVolumeFt3,
            float pzrSteamVolumeFt3,
            float surgeFlowGpm,
            float primaryMassLedgerLb)
        {
            return new PressurizerOutputs(
                PzrHeaterPowerMw,
                PzrHeatersOn,
                HeaterPidOutput,
                HeaterPidActive,
                HeaterPidState,
                SprayState,
                SprayFlowGpm,
                SprayValvePosition,
                SprayActive,
                SpraySteamCondensedLbm,
                CurrentHeaterMode,
                BubbleHeaterSmoothedOutput,
                StartupHoldActive,
                StartupHoldReleaseLogged,
                StartupHoldActivationLogged,
                pressurePsia,
                pzrLevelPct,
                pzrWaterVolumeFt3,
                pzrSteamVolumeFt3,
                surgeFlowGpm,
                primaryMassLedgerLb);
        }
    }
}
