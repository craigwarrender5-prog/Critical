using System;
using System.Collections.Generic;

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
            Array.Empty<string>());

        public float TimeHr { get; }
        public float DtHr { get; }
        public PlantState PlantState { get; }
        public HeatupSimEngine.RuntimeTelemetrySnapshot Telemetry { get; }
        public IReadOnlyList<string> TransferLedgerPlaceholders { get; }

        public StepSnapshot(
            float timeHr,
            float dtHr,
            PlantState plantState,
            HeatupSimEngine.RuntimeTelemetrySnapshot telemetry,
            IReadOnlyList<string> transferLedgerPlaceholders)
        {
            TimeHr = timeHr;
            DtHr = dtHr;
            PlantState = plantState ?? PlantState.Empty;
            Telemetry = telemetry;
            int count = transferLedgerPlaceholders?.Count ?? 0;
            if (count == 0)
            {
                TransferLedgerPlaceholders = Array.AsReadOnly(Array.Empty<string>());
            }
            else
            {
                var copy = new string[count];
                for (int i = 0; i < count; i++)
                    copy[i] = transferLedgerPlaceholders[i] ?? string.Empty;

                TransferLedgerPlaceholders = Array.AsReadOnly(copy);
            }
        }
    }
}
