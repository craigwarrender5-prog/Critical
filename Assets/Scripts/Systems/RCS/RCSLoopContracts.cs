using Critical.Physics;

namespace Critical.Systems.RCS
{
    /// <summary>
    /// Immutable loop input contract used by modular RCS loop components.
    /// </summary>
    public readonly struct RCSLoopInput
    {
        /// <summary>Loop-local bulk temperature in deg F.</summary>
        public readonly float BulkTemperature_F;

        /// <summary>Loop pressure in psia.</summary>
        public readonly float Pressure_psia;

        /// <summary>Running RCP count mapped to this loop input.</summary>
        public readonly int RunningRcpCount;

        /// <summary>Total RCP heat applied to this loop input in MW.</summary>
        public readonly float RcpHeat_MW;

        /// <summary>Pressurizer temperature in deg F used by natural-circulation path.</summary>
        public readonly float PressurizerTemperature_F;

        /// <summary>
        /// Initializes a new loop input.
        /// </summary>
        public RCSLoopInput(
            float bulkTemperature_F,
            float pressure_psia,
            int runningRcpCount,
            float rcpHeat_MW,
            float pressurizerTemperature_F)
        {
            BulkTemperature_F = bulkTemperature_F;
            Pressure_psia = pressure_psia;
            RunningRcpCount = runningRcpCount < 0 ? 0 : runningRcpCount;
            RcpHeat_MW = rcpHeat_MW;
            PressurizerTemperature_F = pressurizerTemperature_F;
        }
    }

    /// <summary>
    /// Loop-local output snapshot produced by <see cref="RCSLoop"/>.
    /// </summary>
    public readonly struct RCSLoopState
    {
        /// <summary>Loop index (0-based).</summary>
        public readonly int LoopIndex;

        /// <summary>Hot leg temperature in deg F.</summary>
        public readonly float T_Hot_F;

        /// <summary>Cold leg temperature in deg F.</summary>
        public readonly float T_Cold_F;

        /// <summary>Loop average bulk temperature in deg F.</summary>
        public readonly float T_Avg_F;

        /// <summary>Core delta-T in deg F.</summary>
        public readonly float DeltaT_F;

        /// <summary>Loop mass flow in lb/sec.</summary>
        public readonly float MassFlow_lb_per_sec;

        /// <summary>Loop volumetric flow in gpm.</summary>
        public readonly float Flow_gpm;

        /// <summary>True if forced circulation is active for this loop snapshot.</summary>
        public readonly bool IsForcedFlow;

        /// <summary>
        /// Initializes a loop output state.
        /// </summary>
        public RCSLoopState(
            int loopIndex,
            float tHot_F,
            float tCold_F,
            float tAvg_F,
            float deltaT_F,
            float massFlow_lb_per_sec,
            float flow_gpm,
            bool isForcedFlow)
        {
            LoopIndex = loopIndex;
            T_Hot_F = tHot_F;
            T_Cold_F = tCold_F;
            T_Avg_F = tAvg_F;
            DeltaT_F = deltaT_F;
            MassFlow_lb_per_sec = massFlow_lb_per_sec;
            Flow_gpm = flow_gpm;
            IsForcedFlow = isForcedFlow;
        }
    }

    /// <summary>
    /// Aggregate N-loop contract for downstream consumers.
    /// </summary>
    public readonly struct RCSAggregateState
    {
        /// <summary>Number of loops represented by this aggregate snapshot.</summary>
        public readonly int LoopCount;

        /// <summary>Total aggregate flow across all loops in gpm.</summary>
        public readonly float TotalFlow_gpm;

        /// <summary>Average hot leg temperature across loops in deg F.</summary>
        public readonly float AverageT_Hot_F;

        /// <summary>Average cold leg temperature across loops in deg F.</summary>
        public readonly float AverageT_Cold_F;

        /// <summary>Average bulk temperature across loops in deg F.</summary>
        public readonly float AverageT_Avg_F;

        /// <summary>Average delta-T across loops in deg F.</summary>
        public readonly float AverageDeltaT_F;

        /// <summary>Minimum loop delta-T in deg F.</summary>
        public readonly float MinDeltaT_F;

        /// <summary>Maximum loop delta-T in deg F.</summary>
        public readonly float MaxDeltaT_F;

        /// <summary>True if any loop reports forced circulation.</summary>
        public readonly bool AnyForcedFlow;

        /// <summary>
        /// Initializes an aggregate snapshot.
        /// </summary>
        public RCSAggregateState(
            int loopCount,
            float totalFlow_gpm,
            float averageT_Hot_F,
            float averageT_Cold_F,
            float averageT_Avg_F,
            float averageDeltaT_F,
            float minDeltaT_F,
            float maxDeltaT_F,
            bool anyForcedFlow)
        {
            LoopCount = loopCount;
            TotalFlow_gpm = totalFlow_gpm;
            AverageT_Hot_F = averageT_Hot_F;
            AverageT_Cold_F = averageT_Cold_F;
            AverageT_Avg_F = averageT_Avg_F;
            AverageDeltaT_F = averageDeltaT_F;
            MinDeltaT_F = minDeltaT_F;
            MaxDeltaT_F = maxDeltaT_F;
            AnyForcedFlow = anyForcedFlow;
        }

        /// <summary>
        /// Returns an empty aggregate state.
        /// </summary>
        public static RCSAggregateState Empty => new RCSAggregateState(
            0,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            0f,
            false);
    }
}
