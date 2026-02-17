using Critical.Physics;

namespace Critical.Systems.RCS
{
    /// <summary>
    /// Reusable loop-local boundary for RCS thermal/flow state.
    /// </summary>
    public sealed class RCSLoop
    {
        /// <summary>Loop index (0-based).</summary>
        public int LoopIndex { get; }

        /// <summary>Most recent loop-local output state.</summary>
        public RCSLoopState CurrentState { get; private set; }

        /// <summary>
        /// Creates a new loop-local boundary.
        /// </summary>
        public RCSLoop(int loopIndex)
        {
            LoopIndex = loopIndex;
            CurrentState = new RCSLoopState(loopIndex, 0f, 0f, 0f, 0f, 0f, 0f, false);
        }

        /// <summary>
        /// Evaluates the loop using existing loop-thermodynamics authority.
        /// </summary>
        public RCSLoopState Evaluate(in RCSLoopInput input)
        {
            LoopTemperatureResult legacyResult = LoopThermodynamics.CalculateLoopTemperatures(
                input.BulkTemperature_F,
                input.Pressure_psia,
                input.RunningRcpCount,
                input.RcpHeat_MW,
                input.PressurizerTemperature_F);

            float flow_gpm = input.RunningRcpCount > 0
                ? input.RunningRcpCount * PlantConstants.RCP_FLOW_EACH
                : LoopThermodynamics.NaturalCirculationFlowRate(
                    legacyResult.T_hot,
                    legacyResult.T_cold,
                    input.Pressure_psia);

            CurrentState = new RCSLoopState(
                LoopIndex,
                legacyResult.T_hot,
                legacyResult.T_cold,
                legacyResult.T_avg,
                legacyResult.DeltaT,
                legacyResult.MassFlow,
                flow_gpm,
                legacyResult.IsForcedFlow);

            return CurrentState;
        }
    }
}
