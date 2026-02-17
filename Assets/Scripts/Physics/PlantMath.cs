// ============================================================================
// CRITICAL: Master the Atom - Plant Math Utilities
// PlantMath.cs - Runtime conversion and thermal helper math
// ============================================================================
//
// PURPOSE:
//   Runtime calculation helpers used by simulation/control paths.
//   This file intentionally keeps executable math out of PlantConstants.*
//
// GOLD STANDARD: Yes
// ============================================================================

namespace Critical.Physics
{
    public static class PlantMath
    {
        /// <summary>
        /// Convert psig to psia.
        /// </summary>
        public static float PsigToPsia(float psig)
        {
            return psig + PlantConstants.PSIG_TO_PSIA;
        }

        /// <summary>
        /// Convert psia to psig.
        /// </summary>
        public static float PsiaToPsig(float psia)
        {
            return psia - PlantConstants.PSIG_TO_PSIA;
        }

        /// <summary>
        /// Convert degF to degR (Rankine).
        /// </summary>
        public static float FahrenheitToRankine(float fahrenheit)
        {
            return fahrenheit + PlantConstants.RANKINE_OFFSET;
        }

        /// <summary>
        /// Calculate insulation heat loss in MW at a given system temperature.
        /// Uses linear scaling: Q = k * (T_system - T_ambient), bounded at >= 0.
        /// </summary>
        public static float CalculateHeatLoss_MW(float systemTemperature_F)
        {
            float deltaT = systemTemperature_F - PlantConstants.AMBIENT_TEMP_F;
            if (deltaT <= 0f)
            {
                return 0f;
            }

            return PlantConstants.HEAT_LOSS_COEFF_MW_PER_F * deltaT;
        }
    }
}
