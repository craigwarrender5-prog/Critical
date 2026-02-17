using System;

using Critical.Validation;
namespace Critical.Physics.Tests
{
    /// <summary>
    /// Runtime acceptance evidence captured from a live HeatupSimEngine execution.
    /// </summary>
    public sealed class AcceptanceSimulationEvidence
    {
        public static readonly AcceptanceSimulationEvidence Empty = new AcceptanceSimulationEvidence();

        public string EvidenceId { get; set; } = string.Empty;
        public DateTime CapturedAtUtc { get; set; } = DateTime.MinValue;
        public string Source { get; set; } = string.Empty;
        public At02Evidence AT02 { get; set; } = new At02Evidence();
        public At03Evidence AT03 { get; set; } = new At03Evidence();
        public At08Evidence AT08 { get; set; } = new At08Evidence();

        public bool HasRuntimeEvidence =>
            AT02.Observed || AT03.Observed || AT08.Observed;
    }

    public sealed class At02Evidence
    {
        public bool Observed { get; set; }
        public float WindowHours { get; set; }
        public float StartMassLb { get; set; }
        public float EndMassLb { get; set; }
        public float AbsoluteDriftLb { get; set; }
        public float AbsoluteDriftPercent { get; set; }
        public bool Passed { get; set; }
    }

    public sealed class At03Evidence
    {
        public bool Observed { get; set; }
        public float TransitionDiscontinuityLb { get; set; }
        public bool Passed { get; set; }
    }

    public sealed class At08Evidence
    {
        public bool Observed { get; set; }
        public int WindowStepsEvaluated { get; set; }
        public float MaxPzrLevelStepDeltaPercent { get; set; }
        public bool Passed { get; set; }
    }

    public static class AcceptanceSimulationEvidenceStore
    {
        private static AcceptanceSimulationEvidence _latest = AcceptanceSimulationEvidence.Empty;

        public static AcceptanceSimulationEvidence Latest => _latest ?? AcceptanceSimulationEvidence.Empty;

        public static void Set(AcceptanceSimulationEvidence evidence)
        {
            _latest = evidence ?? AcceptanceSimulationEvidence.Empty;
        }

        public static void Clear()
        {
            _latest = AcceptanceSimulationEvidence.Empty;
        }
    }
}

