using System.Collections.Generic;

namespace Critical.Simulation.Modular.Transfer
{
    /// <summary>
    /// Per-step transfer intent bus. Mutable only within coordinator-owned step scope.
    /// </summary>
    public sealed class PlantBus
    {
        private readonly List<TransferEvent> _events = new List<TransferEvent>();

        public void ClearStep()
        {
            _events.Clear();
        }

        public void EmitMassTransfer(
            int stepIndex,
            string signal,
            string source,
            string destination,
            float amountLb,
            bool isBoundary,
            string authorityPath)
        {
            _events.Add(new TransferEvent(
                stepIndex,
                signal,
                source,
                destination,
                TransferQuantityType.MassLb,
                amountLb,
                isBoundary,
                authorityPath));
        }

        public void EmitEnergyTransfer(
            int stepIndex,
            string signal,
            string source,
            string destination,
            float amountMw,
            bool isBoundary,
            string authorityPath)
        {
            _events.Add(new TransferEvent(
                stepIndex,
                signal,
                source,
                destination,
                TransferQuantityType.EnergyMw,
                amountMw,
                isBoundary,
                authorityPath));
        }

        public void EmitFlowTransfer(
            int stepIndex,
            string signal,
            string source,
            string destination,
            float amountGpm,
            bool isBoundary,
            string authorityPath)
        {
            _events.Add(new TransferEvent(
                stepIndex,
                signal,
                source,
                destination,
                TransferQuantityType.FlowGpm,
                amountGpm,
                isBoundary,
                authorityPath));
        }

        public bool HasSignal(string signal, TransferQuantityType quantityType)
        {
            for (int i = 0; i < _events.Count; i++)
            {
                TransferEvent evt = _events[i];
                if (evt.QuantityType == quantityType && evt.Signal == signal)
                    return true;
            }

            return false;
        }

        public IReadOnlyList<TransferEvent> SnapshotEvents()
        {
            return _events.ToArray();
        }
    }
}
