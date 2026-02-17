using System;
using Critical.Physics;
using Critical.Systems.RCS;

namespace Critical.Physics.Tests
{
    /// <summary>
    /// Compatibility checks for modular RCS loop manager scaffolding.
    /// </summary>
    public static class RCSLoopManagerCompatibilityTests
    {
        /// <summary>
        /// Validates N=1 aggregate outputs against legacy loop thermodynamics.
        /// </summary>
        public static bool ValidateN1AggregateParity(out string details)
        {
            const float epsilon = 1e-4f;

            RCSLoopManager manager = new RCSLoopManager(1);
            float tAvg = 540f;
            float pressure = 2150f;
            int rcpCount = 4;
            float rcpHeat = PlantConstants.RCP_HEAT_MW;
            float tPzr = 545f;

            RCSAggregateState aggregate = manager.UpdateSingleLoopCompatibility(
                tAvg,
                pressure,
                rcpCount,
                rcpHeat,
                tPzr);

            LoopTemperatureResult legacy = LoopThermodynamics.CalculateLoopTemperatures(
                tAvg,
                pressure,
                rcpCount,
                rcpHeat,
                tPzr);

            bool pass =
                aggregate.LoopCount == 1 &&
                Math.Abs(aggregate.AverageT_Hot_F - legacy.T_hot) <= epsilon &&
                Math.Abs(aggregate.AverageT_Cold_F - legacy.T_cold) <= epsilon &&
                Math.Abs(aggregate.AverageT_Avg_F - legacy.T_avg) <= epsilon &&
                Math.Abs(aggregate.AverageDeltaT_F - legacy.DeltaT) <= epsilon;

            details = pass
                ? "RCSLoopManager N=1 aggregate parity PASS."
                : "RCSLoopManager N=1 aggregate parity FAIL.";
            return pass;
        }

        /// <summary>
        /// Validates manager loop indexing and aggregate flow semantics.
        /// </summary>
        public static bool ValidateLoopIndexAndFlowAggregation(out string details)
        {
            const float epsilon = 1e-4f;

            RCSLoopManager manager = new RCSLoopManager(2);
            RCSLoopInput[] inputs =
            {
                new RCSLoopInput(500f, 2000f, 2, 12f, 510f),
                new RCSLoopInput(500f, 2000f, 2, 12f, 510f)
            };

            RCSAggregateState aggregate = manager.UpdateAll(inputs);
            bool indexPass =
                manager.TryGetLoopState(0, out RCSLoopState loop0) &&
                manager.TryGetLoopState(1, out RCSLoopState loop1) &&
                loop0.LoopIndex == 0 &&
                loop1.LoopIndex == 1;

            float expectedFlow = (inputs[0].RunningRcpCount + inputs[1].RunningRcpCount) * PlantConstants.RCP_FLOW_EACH;
            bool flowPass = Math.Abs(aggregate.TotalFlow_gpm - expectedFlow) <= epsilon;

            bool pass = indexPass && flowPass;
            details = pass
                ? "Loop indexing and aggregate flow contract PASS."
                : "Loop indexing and aggregate flow contract FAIL.";
            return pass;
        }
    }
}
