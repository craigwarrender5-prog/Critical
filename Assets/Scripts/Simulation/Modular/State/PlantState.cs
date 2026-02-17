using System;

namespace Critical.Simulation.Modular.State
{
    /// <summary>
    /// Immutable projection of selected simulator state used by validation/UI.
    /// Stage B authority: projection-only surface emitted by LegacyStateBridge.
    /// </summary>
    public sealed class PlantState
    {
        public static readonly PlantState Empty = new PlantState(
            0f,
            0f,
            0,
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
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            false,
            0,
            false,
            0f,
            0f,
            0f,
            string.Empty,
            string.Empty);

        public float TimeHr { get; }
        public float DtHr { get; }
        public int PlantMode { get; }
        public float PressurePsia { get; }
        public float TavgF { get; }
        public float TrcsF { get; }
        public float PzrLevelPct { get; }
        public float PzrHeaterPowerMw { get; }
        public float SprayFlowGpm { get; }
        public float ChargingFlowGpm { get; }
        public float LetdownFlowGpm { get; }
        public float SurgeFlowGpm { get; }
        public float ReactorPowerMw { get; }
        public float RhrNetHeatMw { get; }
        public float PrimaryMassLedgerLb { get; }
        public float PrimaryMassComponentsLb { get; }
        public float PrimaryMassDriftLb { get; }
        public float PrimaryMassBoundaryErrorLb { get; }
        public float PrimaryMassExpectedLb { get; }
        public float TotalSystemMassLb { get; }
        public bool PrimaryMassConservationOk { get; }
        public int RcpCount { get; }
        public bool RhrActive { get; }
        public float RhrFlowGpm { get; }
        public float VctLevelPct { get; }
        public float RcsBoronPpm { get; }
        public string RhrMode { get; }
        public string HeatupPhaseDescription { get; }

        public PlantState(
            float timeHr,
            float dtHr,
            int plantMode,
            float pressurePsia,
            float tavgF,
            float trcsF,
            float pzrLevelPct,
            float pzrHeaterPowerMw,
            float sprayFlowGpm,
            float chargingFlowGpm,
            float letdownFlowGpm,
            float surgeFlowGpm,
            float reactorPowerMw,
            float rhrNetHeatMw,
            float primaryMassLedgerLb,
            float primaryMassComponentsLb,
            float primaryMassDriftLb,
            float primaryMassBoundaryErrorLb,
            float primaryMassExpectedLb,
            float totalSystemMassLb,
            bool primaryMassConservationOk,
            int rcpCount,
            bool rhrActive,
            float rhrFlowGpm,
            float vctLevelPct,
            float rcsBoronPpm,
            string rhrMode,
            string heatupPhaseDescription)
        {
            TimeHr = timeHr;
            DtHr = dtHr;
            PlantMode = plantMode;
            PressurePsia = pressurePsia;
            TavgF = tavgF;
            TrcsF = trcsF;
            PzrLevelPct = pzrLevelPct;
            PzrHeaterPowerMw = pzrHeaterPowerMw;
            SprayFlowGpm = sprayFlowGpm;
            ChargingFlowGpm = chargingFlowGpm;
            LetdownFlowGpm = letdownFlowGpm;
            SurgeFlowGpm = surgeFlowGpm;
            ReactorPowerMw = reactorPowerMw;
            RhrNetHeatMw = rhrNetHeatMw;
            PrimaryMassLedgerLb = primaryMassLedgerLb;
            PrimaryMassComponentsLb = primaryMassComponentsLb;
            PrimaryMassDriftLb = primaryMassDriftLb;
            PrimaryMassBoundaryErrorLb = primaryMassBoundaryErrorLb;
            PrimaryMassExpectedLb = primaryMassExpectedLb;
            TotalSystemMassLb = totalSystemMassLb;
            PrimaryMassConservationOk = primaryMassConservationOk;
            RcpCount = rcpCount;
            RhrActive = rhrActive;
            RhrFlowGpm = rhrFlowGpm;
            VctLevelPct = vctLevelPct;
            RcsBoronPpm = rcsBoronPpm;
            RhrMode = rhrMode ?? string.Empty;
            HeatupPhaseDescription = heatupPhaseDescription ?? string.Empty;
        }
    }
}
