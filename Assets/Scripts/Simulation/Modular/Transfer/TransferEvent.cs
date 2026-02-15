using System;

namespace Critical.Simulation.Modular.Transfer
{
    public enum TransferQuantityType
    {
        MassLb = 0,
        EnergyMw = 1,
        FlowGpm = 2
    }

    /// <summary>
    /// Immutable transfer intent/event captured during one simulation step.
    /// </summary>
    public sealed class TransferEvent
    {
        public int StepIndex { get; }
        public string Signal { get; }
        public string Source { get; }
        public string Destination { get; }
        public TransferQuantityType QuantityType { get; }
        public float Amount { get; }
        public bool IsBoundary { get; }
        public string AuthorityPath { get; }

        public TransferEvent(
            int stepIndex,
            string signal,
            string source,
            string destination,
            TransferQuantityType quantityType,
            float amount,
            bool isBoundary,
            string authorityPath)
        {
            StepIndex = stepIndex;
            Signal = signal ?? string.Empty;
            Source = source ?? string.Empty;
            Destination = destination ?? string.Empty;
            QuantityType = quantityType;
            Amount = amount;
            IsBoundary = isBoundary;
            AuthorityPath = authorityPath ?? string.Empty;
        }
    }
}
