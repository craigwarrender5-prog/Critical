// CRITICAL: Master the Atom - Phase 1 Core Physics Engine
// CVCSController.Letdown.cs - Letdown path selection
//
// File: Assets/Scripts/Physics/CVCSController.Letdown.cs
// Module: Critical.Physics.CVCSController
// Responsibility: Letdown path selection and rationale state.
// Standards: GOLD v1.0, SRP/SOLID
// Version: 1.0
// Last Updated: 2026-02-18
using System;
namespace Critical.Physics
{
    public static partial class CVCSController
    {
        #region Letdown Path Selection
        
        /// <summary>
        /// Determine the active letdown flow path based on plant conditions.
        /// 
        /// Per NRC HRTD 19.0 and Section 4.1:
        ///   - At low RCS temperature (< 350Â°F): Letdown via RHR-CVCS crossconnect (HCV-128)
        ///     because the normal orifice path produces negligible flow at low Î”P
        ///   - At high RCS temperature (â‰¥ 350Â°F): Letdown via normal orifice path
        ///   - If low-level interlock active: Letdown is isolated regardless of temp
        ///   - During solid PZR ops: RHR path is used (temp will be < 350Â°F)
        /// 
        /// The 350Â°F threshold corresponds to RHR letdown isolation temperature
        /// per NRC HRTD Section 19.2.2.
        /// </summary>
        /// <param name="T_rcs">RCS temperature (Â°F)</param>
        /// <param name="pressure">RCS pressure (psia) - reserved for future use</param>
        /// <param name="solidPressurizer">True if in solid pressurizer operations</param>
        /// <param name="letdownIsolated">True if low-level interlock has isolated letdown</param>
        /// <returns>LetdownPathState with path selection and reasoning</returns>
        public static LetdownPathState GetLetdownPath(
            float T_rcs, 
            float pressure, 
            bool solidPressurizer, 
            bool letdownIsolated)
        {
            var state = new LetdownPathState();
            
            // Priority 1: Low-level interlock isolates letdown
            if (letdownIsolated)
            {
                state.Path = LetdownPath.ISOLATED;
                state.ViaRHR = false;
                state.ViaOrifice = false;
                state.IsIsolated = true;
                state.Reason = "Low PZR level interlock";
                return state;
            }
            
            // Priority 2: Temperature-based path selection
            // Per NRC HRTD 19.0: RHR crossconnect used below 350Â°F
            bool useRHR = (T_rcs < PlantConstants.RHR_LETDOWN_ISOLATION_TEMP_F);
            
            if (useRHR)
            {
                state.Path = LetdownPath.RHR_CROSSCONNECT;
                state.ViaRHR = true;
                state.ViaOrifice = false;
                state.IsIsolated = false;
                state.Reason = solidPressurizer 
                    ? "Solid PZR ops - RHR crossconnect" 
                    : $"T_rcs < {PlantConstants.RHR_LETDOWN_ISOLATION_TEMP_F}Â°F";
            }
            else
            {
                state.Path = LetdownPath.ORIFICE;
                state.ViaRHR = false;
                state.ViaOrifice = true;
                state.IsIsolated = false;
                state.Reason = "Normal orifice path";
            }
            
            return state;
        }
        
        #endregion
    }
}
