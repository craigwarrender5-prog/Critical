using System;
using System.Collections.Generic;
using Critical.Physics;

namespace Critical.Systems.RCS
{
    /// <summary>
    /// Manager/aggregator for N-loop RCS execution with N=1 compatibility support.
    /// </summary>
    public sealed class RCSLoopManager
    {
        readonly List<RCSLoop> loops = new List<RCSLoop>(4);
        RCSAggregateState aggregateState = RCSAggregateState.Empty;

        /// <summary>Current loop count owned by the manager.</summary>
        public int LoopCount => loops.Count;

        /// <summary>Current aggregate state across all managed loops.</summary>
        public RCSAggregateState AggregateState => aggregateState;

        /// <summary>
        /// Creates an RCS loop manager with the requested loop count.
        /// </summary>
        public RCSLoopManager(int loopCount = 1)
        {
            ConfigureLoopCount(loopCount);
        }

        /// <summary>
        /// Reconfigures loop ownership for a target loop count.
        /// </summary>
        public void ConfigureLoopCount(int loopCount)
        {
            if (loopCount < 1)
                throw new ArgumentOutOfRangeException(nameof(loopCount), "Loop count must be >= 1.");

            loops.Clear();
            for (int i = 0; i < loopCount; i++)
                loops.Add(new RCSLoop(i));

            aggregateState = RCSAggregateState.Empty;
        }

        /// <summary>
        /// Gets the current loop state by index.
        /// </summary>
        public RCSLoopState GetLoopState(int loopIndex)
        {
            if (loopIndex < 0 || loopIndex >= loops.Count)
                throw new ArgumentOutOfRangeException(nameof(loopIndex));

            return loops[loopIndex].CurrentState;
        }

        /// <summary>
        /// Tries to get the current loop state by index.
        /// </summary>
        public bool TryGetLoopState(int loopIndex, out RCSLoopState state)
        {
            if (loopIndex < 0 || loopIndex >= loops.Count)
            {
                state = default;
                return false;
            }

            state = loops[loopIndex].CurrentState;
            return true;
        }

        /// <summary>
        /// Updates a single loop and recomputes manager aggregates.
        /// </summary>
        public RCSLoopState UpdateLoop(int loopIndex, in RCSLoopInput input)
        {
            if (loopIndex < 0 || loopIndex >= loops.Count)
                throw new ArgumentOutOfRangeException(nameof(loopIndex));

            RCSLoopState loopState = loops[loopIndex].Evaluate(input);
            RecomputeAggregate();
            return loopState;
        }

        /// <summary>
        /// Updates all loops from an indexed input set and recomputes aggregate outputs.
        /// </summary>
        public RCSAggregateState UpdateAll(IReadOnlyList<RCSLoopInput> inputs)
        {
            if (inputs == null)
                throw new ArgumentNullException(nameof(inputs));
            if (inputs.Count != loops.Count)
                throw new ArgumentException("Input count must match loop count.", nameof(inputs));

            for (int i = 0; i < loops.Count; i++)
                loops[i].Evaluate(inputs[i]);

            RecomputeAggregate();
            return aggregateState;
        }

        /// <summary>
        /// N=1 compatibility update that maps directly to legacy single-loop inputs.
        /// </summary>
        public RCSAggregateState UpdateSingleLoopCompatibility(
            float bulkTemperature_F,
            float pressure_psia,
            int runningRcpCount,
            float rcpHeat_MW,
            float pressurizerTemperature_F)
        {
            if (loops.Count != 1)
                throw new InvalidOperationException("N=1 compatibility path requires loop count == 1.");

            RCSLoopInput input = new RCSLoopInput(
                bulkTemperature_F,
                pressure_psia,
                runningRcpCount,
                rcpHeat_MW,
                pressurizerTemperature_F);

            UpdateLoop(0, input);
            return aggregateState;
        }

        /// <summary>
        /// Executes a deterministic N=1 compatibility check against legacy loop thermodynamics.
        /// </summary>
        public static bool ValidateN1Compatibility(out string details)
        {
            const float epsilon = 1e-4f;

            RCSLoopManager manager = new RCSLoopManager(1);
            RCSLoopInput input = new RCSLoopInput(
                bulkTemperature_F: 557f,
                pressure_psia: 2235f + 14.7f,
                runningRcpCount: 4,
                rcpHeat_MW: PlantConstants.RCP_HEAT_MW,
                pressurizerTemperature_F: 560f);

            RCSAggregateState aggregate = manager.UpdateSingleLoopCompatibility(
                input.BulkTemperature_F,
                input.Pressure_psia,
                input.RunningRcpCount,
                input.RcpHeat_MW,
                input.PressurizerTemperature_F);

            LoopTemperatureResult legacy = LoopThermodynamics.CalculateLoopTemperatures(
                input.BulkTemperature_F,
                input.Pressure_psia,
                input.RunningRcpCount,
                input.RcpHeat_MW,
                input.PressurizerTemperature_F);

            RCSLoopState managed = manager.GetLoopState(0);
            bool pass =
                Math.Abs(managed.T_Hot_F - legacy.T_hot) <= epsilon &&
                Math.Abs(managed.T_Cold_F - legacy.T_cold) <= epsilon &&
                Math.Abs(managed.T_Avg_F - legacy.T_avg) <= epsilon &&
                Math.Abs(managed.DeltaT_F - legacy.DeltaT) <= epsilon &&
                aggregate.LoopCount == 1 &&
                Math.Abs(aggregate.AverageT_Hot_F - legacy.T_hot) <= epsilon &&
                Math.Abs(aggregate.AverageT_Cold_F - legacy.T_cold) <= epsilon &&
                Math.Abs(aggregate.AverageDeltaT_F - legacy.DeltaT) <= epsilon;

            details = pass
                ? "N=1 compatibility PASS: manager outputs match LoopThermodynamics authority."
                : "N=1 compatibility FAIL: manager outputs diverge from LoopThermodynamics.";
            return pass;
        }

        void RecomputeAggregate()
        {
            if (loops.Count == 0)
            {
                aggregateState = RCSAggregateState.Empty;
                return;
            }

            float flow = 0f;
            float hot = 0f;
            float cold = 0f;
            float avg = 0f;
            float delta = 0f;
            float minDelta = float.MaxValue;
            float maxDelta = float.MinValue;
            bool anyForced = false;

            for (int i = 0; i < loops.Count; i++)
            {
                RCSLoopState s = loops[i].CurrentState;
                flow += s.Flow_gpm;
                hot += s.T_Hot_F;
                cold += s.T_Cold_F;
                avg += s.T_Avg_F;
                delta += s.DeltaT_F;
                minDelta = Math.Min(minDelta, s.DeltaT_F);
                maxDelta = Math.Max(maxDelta, s.DeltaT_F);
                anyForced |= s.IsForcedFlow;
            }

            float invCount = 1f / loops.Count;
            aggregateState = new RCSAggregateState(
                loops.Count,
                flow,
                hot * invCount,
                cold * invCount,
                avg * invCount,
                delta * invCount,
                minDelta,
                maxDelta,
                anyForced);
        }
    }
}
