using Critical.Physics;
using Critical.Simulation.Modular.State;

namespace Critical.Simulation.Modular.Modules.PZR
{
    /// <summary>
    /// Module-local persistent PZR controller state for Stage E packaging.
    /// </summary>
    public sealed class PressurizerModuleState
    {
        public bool IsInitialized;
        public bool StartupHoldActive;
        public float StartupHoldReleaseTimeHr;
        public bool StartupHoldReleaseLogged;
        public bool StartupHoldActivationLogged;
        public HeaterMode CurrentHeaterMode;
        public float BubbleHeaterSmoothedOutput;
        public HeaterPIDState HeaterPidState;
        public bool HeaterPidActive;
        public float HeaterPidOutput;
        public SprayControlState SprayState;
        public bool SprayActive;

        public void SeedFromSnapshot(PressurizerSnapshot snapshot)
        {
            StartupHoldActive = snapshot.StartupHoldActive;
            StartupHoldReleaseTimeHr = snapshot.StartupHoldReleaseTimeHr;
            StartupHoldReleaseLogged = snapshot.StartupHoldReleaseLogged;
            StartupHoldActivationLogged = snapshot.StartupHoldActivationLogged;
            CurrentHeaterMode = snapshot.CurrentHeaterMode;
            BubbleHeaterSmoothedOutput = snapshot.BubbleHeaterSmoothedOutput;
            HeaterPidState = snapshot.HeaterPidState;
            HeaterPidActive = snapshot.HeaterPidActive;
            HeaterPidOutput = snapshot.HeaterPidOutput;
            SprayState = snapshot.SprayState;
            SprayActive = snapshot.SprayActive;
            IsInitialized = true;
        }

        public void Reset()
        {
            IsInitialized = false;
            StartupHoldActive = false;
            StartupHoldReleaseTimeHr = 0f;
            StartupHoldReleaseLogged = false;
            StartupHoldActivationLogged = false;
            CurrentHeaterMode = HeaterMode.STARTUP_FULL_POWER;
            BubbleHeaterSmoothedOutput = 1f;
            HeaterPidState = default;
            HeaterPidActive = false;
            HeaterPidOutput = 0f;
            SprayState = default;
            SprayActive = false;
        }
    }
}
