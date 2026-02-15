using System;
using System.Collections.Generic;

namespace Critical.Simulation.Modular.Transfer
{
    /// <summary>
    /// Immutable finalized per-step transfer ledger.
    /// </summary>
    public sealed class TransferLedger
    {
        public static readonly TransferLedger Empty = new TransferLedger(
            0,
            Array.Empty<TransferEvent>(),
            false,
            string.Empty);

        public int StepIndex { get; }
        public IReadOnlyList<TransferEvent> Events { get; }
        public float TotalMassTransferLb { get; }
        public float TotalEnergyTransferMw { get; }
        public float TotalFlowTransferGpm { get; }
        public bool UnledgeredMutationDetected { get; }
        public string UnledgeredMutationReason { get; }

        public TransferLedger(
            int stepIndex,
            IReadOnlyList<TransferEvent> events,
            bool unledgeredMutationDetected,
            string unledgeredMutationReason)
        {
            StepIndex = stepIndex;

            int count = events?.Count ?? 0;
            var copy = new TransferEvent[count];
            float totalMass = 0f;
            float totalEnergy = 0f;
            float totalFlow = 0f;
            for (int i = 0; i < count; i++)
            {
                TransferEvent evt = events[i];
                copy[i] = evt;
                switch (evt.QuantityType)
                {
                    case TransferQuantityType.MassLb:
                        totalMass += evt.Amount;
                        break;
                    case TransferQuantityType.EnergyMw:
                        totalEnergy += evt.Amount;
                        break;
                    case TransferQuantityType.FlowGpm:
                        totalFlow += evt.Amount;
                        break;
                }
            }

            Events = Array.AsReadOnly(copy);
            TotalMassTransferLb = totalMass;
            TotalEnergyTransferMw = totalEnergy;
            TotalFlowTransferGpm = totalFlow;
            UnledgeredMutationDetected = unledgeredMutationDetected;
            UnledgeredMutationReason = unledgeredMutationReason ?? string.Empty;
        }

        public float SumBySignal(string signal, TransferQuantityType quantityType)
        {
            if (string.IsNullOrEmpty(signal))
                return 0f;

            float sum = 0f;
            for (int i = 0; i < Events.Count; i++)
            {
                TransferEvent evt = Events[i];
                if (evt.QuantityType == quantityType && evt.Signal == signal)
                    sum += evt.Amount;
            }

            return sum;
        }
    }
}
