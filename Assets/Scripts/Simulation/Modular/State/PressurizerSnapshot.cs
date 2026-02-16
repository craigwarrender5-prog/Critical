using Critical.Physics;

namespace Critical.Simulation.Modular.State
{
    /// <summary>
    /// Immutable pressurizer-specific snapshot payload consumed by the modular PZR package.
    /// </summary>
    public sealed class PressurizerSnapshot
    {
        public static readonly PressurizerSnapshot Empty = new PressurizerSnapshot(
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0,
            false,
            false,
            false,
            0f,
            false,
            false,
            HeaterMode.STARTUP_FULL_POWER,
            0f,
            default,
            false,
            0f,
            default,
            false,
            0f,
            0f,
            0f);

        public float SimTimeHr { get; }
        public float DtHr { get; }
        public float PressurePsia { get; }
        public float PressureRatePsiPerHr { get; }
        public float PzrLevelPct { get; }
        public float TavgF { get; }
        public float TpzrF { get; }
        public float TcoldF { get; }
        public float PzrWaterVolumeFt3 { get; }
        public float PzrSteamVolumeFt3 { get; }
        public int RcpCount { get; }
        public bool SolidPressurizer { get; }
        public bool BubblePreDrainPhase { get; }
        public bool StartupHoldActive { get; }
        public float StartupHoldReleaseTimeHr { get; }
        public bool StartupHoldReleaseLogged { get; }
        public bool StartupHoldActivationLogged { get; }
        public HeaterMode CurrentHeaterMode { get; }
        public float BubbleHeaterSmoothedOutput { get; }
        public HeaterPIDState HeaterPidState { get; }
        public bool HeaterPidActive { get; }
        public float HeaterPidOutput { get; }
        public SprayControlState SprayState { get; }
        public bool SprayActive { get; }
        public float SprayFlowGpm { get; }
        public float SprayValvePosition { get; }
        public float SpraySteamCondensedLbm { get; }

        public PressurizerSnapshot(
            float simTimeHr,
            float dtHr,
            float pressurePsia,
            float pressureRatePsiPerHr,
            float pzrLevelPct,
            float tavgF,
            float tpzrF,
            float tcoldF,
            float pzrWaterVolumeFt3,
            float pzrSteamVolumeFt3,
            int rcpCount,
            bool solidPressurizer,
            bool bubblePreDrainPhase,
            bool startupHoldActive,
            float startupHoldReleaseTimeHr,
            bool startupHoldReleaseLogged,
            bool startupHoldActivationLogged,
            HeaterMode currentHeaterMode,
            float bubbleHeaterSmoothedOutput,
            HeaterPIDState heaterPidState,
            bool heaterPidActive,
            float heaterPidOutput,
            SprayControlState sprayState,
            bool sprayActive,
            float sprayFlowGpm,
            float sprayValvePosition,
            float spraySteamCondensedLbm)
        {
            SimTimeHr = simTimeHr;
            DtHr = dtHr;
            PressurePsia = pressurePsia;
            PressureRatePsiPerHr = pressureRatePsiPerHr;
            PzrLevelPct = pzrLevelPct;
            TavgF = tavgF;
            TpzrF = tpzrF;
            TcoldF = tcoldF;
            PzrWaterVolumeFt3 = pzrWaterVolumeFt3;
            PzrSteamVolumeFt3 = pzrSteamVolumeFt3;
            RcpCount = rcpCount;
            SolidPressurizer = solidPressurizer;
            BubblePreDrainPhase = bubblePreDrainPhase;
            StartupHoldActive = startupHoldActive;
            StartupHoldReleaseTimeHr = startupHoldReleaseTimeHr;
            StartupHoldReleaseLogged = startupHoldReleaseLogged;
            StartupHoldActivationLogged = startupHoldActivationLogged;
            CurrentHeaterMode = currentHeaterMode;
            BubbleHeaterSmoothedOutput = bubbleHeaterSmoothedOutput;
            HeaterPidState = heaterPidState;
            HeaterPidActive = heaterPidActive;
            HeaterPidOutput = heaterPidOutput;
            SprayState = sprayState;
            SprayActive = sprayActive;
            SprayFlowGpm = sprayFlowGpm;
            SprayValvePosition = sprayValvePosition;
            SpraySteamCondensedLbm = spraySteamCondensedLbm;
        }
    }
}
