// ============================================================================
// CRITICAL: Master the Atom - CVCS Flow Math Utilities
// CVCSFlowMath.cs - Runtime CVCS/orifice calculation helpers
// ============================================================================
//
// PURPOSE:
//   Runtime CVCS flow calculations used by engine and controller code.
//   Keeps executable logic out of PlantConstants.* constant-definition files.
//
// GOLD STANDARD: Yes
// ============================================================================

using System;

namespace Critical.Physics
{
    public static class CVCSFlowMath
    {
        /// <summary>
        /// Convert VCT level (%) to volume (gallons).
        /// </summary>
        public static float VCTLevelToVolume(float level_percent)
        {
            return PlantConstants.VCT_CAPACITY_GAL * level_percent / 100f;
        }

        /// <summary>
        /// Convert VCT volume (gallons) to level (%).
        /// </summary>
        public static float VCTVolumeToLevel(float volume_gal)
        {
            return 100f * volume_gal / PlantConstants.VCT_CAPACITY_GAL;
        }

        /// <summary>
        /// Calculate letdown flow through a single 75-gpm orifice based on RCS pressure.
        /// </summary>
        public static float CalculateOrificeLetdownFlow(float rcs_pressure_psig)
        {
            float deltaP = rcs_pressure_psig - PlantConstants.LETDOWN_BACKPRESSURE_PSIG;
            if (deltaP <= 0f)
            {
                return 0f;
            }

            return PlantConstants.ORIFICE_FLOW_COEFF_75 * (float)Math.Sqrt(deltaP);
        }

        /// <summary>
        /// Calculate letdown flow through a single 45-gpm orifice based on RCS pressure.
        /// </summary>
        public static float CalculateOrifice45LetdownFlow(float rcs_pressure_psig)
        {
            float deltaP = rcs_pressure_psig - PlantConstants.LETDOWN_BACKPRESSURE_PSIG;
            if (deltaP <= 0f)
            {
                return 0f;
            }

            return PlantConstants.ORIFICE_FLOW_COEFF_45 * (float)Math.Sqrt(deltaP);
        }

        /// <summary>
        /// Calculate total orifice letdown flow for a given lineup.
        /// </summary>
        public static float CalculateOrificeLineupFlow(float rcs_pressure_psig, int num75Open, bool open45)
        {
            float deltaP = rcs_pressure_psig - PlantConstants.LETDOWN_BACKPRESSURE_PSIG;
            if (deltaP <= 0f)
            {
                return 0f;
            }

            float sqrtDP = (float)Math.Sqrt(deltaP);
            float flow75 = num75Open * PlantConstants.ORIFICE_FLOW_COEFF_75 * sqrtDP;
            float flow45 = open45 ? PlantConstants.ORIFICE_FLOW_COEFF_45 * sqrtDP : 0f;
            float total = flow75 + flow45;

            return Math.Min(total, PlantConstants.LETDOWN_ION_EXCHANGER_MAX_GPM);
        }

        /// <summary>
        /// Determine total letdown flow based on plant conditions and orifice lineup.
        /// </summary>
        public static float CalculateTotalLetdownFlow(
            float T_rcs_F, float rcs_pressure_psia,
            int numOrificesOpen = 1,
            int num75Open = -1, bool open45 = false)
        {
            float rcs_pressure_psig = rcs_pressure_psia - PlantConstants.PSIG_TO_PSIA;
            bool useNewModel = num75Open >= 0;

            if (T_rcs_F < PlantConstants.RHR_LETDOWN_ISOLATION_TEMP_F)
            {
                float rhrFlow = PlantConstants.RHR_CROSSCONNECT_FLOW_GPM;
                float orificeFlow = useNewModel
                    ? CalculateOrificeLineupFlow(rcs_pressure_psig, num75Open, open45)
                    : CalculateOrificeLetdownFlow(rcs_pressure_psig) * numOrificesOpen;
                return Math.Max(rhrFlow, orificeFlow);
            }

            {
                float orificeFlow = useNewModel
                    ? CalculateOrificeLineupFlow(rcs_pressure_psig, num75Open, open45)
                    : CalculateOrificeLetdownFlow(rcs_pressure_psig) * numOrificesOpen;
                return useNewModel
                    ? orificeFlow
                    : Math.Min(orificeFlow, PlantConstants.LETDOWN_MAX_GPM);
            }
        }
    }
}
