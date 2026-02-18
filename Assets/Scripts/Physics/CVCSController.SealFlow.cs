// CRITICAL: Master the Atom - Phase 1 Core Physics Engine
// CVCSController.SealFlow.cs - Seal flow computations
//
// File: Assets/Scripts/Physics/CVCSController.SealFlow.cs
// Module: Critical.Physics.CVCSController
// Responsibility: RCP seal injection/leakoff/return flow accounting.
// Standards: GOLD v1.0, SRP/SOLID
// Version: 1.0
// Last Updated: 2026-02-18
using System;
namespace Critical.Physics
{
    public static partial class CVCSController
    {
        #region Seal Flow Calculations
        
        /// <summary>
        /// Calculate all RCP seal system flows.
        /// 
        /// Per NRC IN 93-84 and HRTD 3.2:
        ///   - Seal injection: 8 gpm per RCP (from charging)
        ///   - Seal leakoff to VCT: 3 gpm per RCP (#1 seal leakoff)
        ///   - Seal return to RCS: 5 gpm per RCP (past #1 seal to RCS)
        ///   - CBO loss: 1 gpm total when RCPs running
        /// 
        /// The seal injection is supplied by the charging pumps, so it
        /// represents a demand on the CVCS that does not reach the RCS.
        /// </summary>
        /// <param name="rcpCount">Number of RCPs currently running (0-4)</param>
        /// <returns>SealFlowState with all seal system flows</returns>
        public static SealFlowState CalculateSealFlows(int rcpCount)
        {
            var state = new SealFlowState();
            state.RCPCount = rcpCount;
            
            // Per-pump flows from PlantConstants
            state.SealInjection = rcpCount * PlantConstants.SEAL_INJECTION_PER_PUMP_GPM;
            state.SealReturnToVCT = rcpCount * PlantConstants.SEAL_LEAKOFF_PER_PUMP_GPM;
            state.SealReturnToRCS = rcpCount * PlantConstants.SEAL_FLOW_TO_RCS_PER_PUMP_GPM;
            
            // CBO is a small constant loss when any RCPs are running
            state.CBOLoss = rcpCount > 0 ? PlantConstants.CBO_LOSS_GPM : 0f;
            
            // Net seal demand = injection - returns
            // This is the net flow "lost" from charging that doesn't reach RCS
            state.NetSealDemand = state.SealInjection - state.SealReturnToRCS;
            
            return state;
        }
        
        #endregion
    }
}
