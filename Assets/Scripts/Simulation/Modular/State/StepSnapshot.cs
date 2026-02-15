using System;
using Critical.Simulation.Modular.Transfer;

namespace Critical.Simulation.Modular.State
{
    /// <summary>
    /// Immutable per-step boundary object consumed by validation/UI.
    /// Stage B uses a placeholder transfer ledger payload; Stage C upgrades it.
    /// </summary>
    public sealed class StepSnapshot
    {
        public static readonly StepSnapshot Empty = new StepSnapshot(
            0f,
            0f,
            PlantState.Empty,
            HeatupSimEngine.RuntimeTelemetrySnapshot.Empty,
            TransferLedger.Empty);

        public float TimeHr { get; }
        public float DtHr { get; }
        public PlantState PlantState { get; }
        public HeatupSimEngine.RuntimeTelemetrySnapshot Telemetry { get; }
        public TransferLedger TransferLedger { get; }

        public StepSnapshot(
            float timeHr,
            float dtHr,
            PlantState plantState,
            HeatupSimEngine.RuntimeTelemetrySnapshot telemetry,
            TransferLedger transferLedger)
        {
            TimeHr = timeHr;
            DtHr = dtHr;
            PlantState = plantState ?? PlantState.Empty;
            Telemetry = telemetry;
            TransferLedger = transferLedger ?? TransferLedger.Empty;
        }
    }
}
