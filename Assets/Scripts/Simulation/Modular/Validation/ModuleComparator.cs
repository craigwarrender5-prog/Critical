using System;

namespace Critical.Simulation.Modular.Validation
{
    /// <summary>
    /// Temporary shadow payload used for side-effect-free comparator checks.
    /// </summary>
    public readonly struct ModuleShadowState
    {
        public readonly float PressurePsia;
        public readonly float PzrLevelPct;
        public readonly float PrimaryMassLb;

        public ModuleShadowState(float pressurePsia, float pzrLevelPct, float primaryMassLb)
        {
            PressurePsia = pressurePsia;
            PzrLevelPct = pzrLevelPct;
            PrimaryMassLb = primaryMassLb;
        }
    }

    public readonly struct ModuleComparatorResult
    {
        public readonly string ModuleId;
        public readonly bool Pass;
        public readonly float PressureError;
        public readonly float PzrLevelError;
        public readonly float PrimaryMassError;

        public ModuleComparatorResult(
            string moduleId,
            bool pass,
            float pressureError,
            float pzrLevelError,
            float primaryMassError)
        {
            ModuleId = moduleId ?? string.Empty;
            Pass = pass;
            PressureError = pressureError;
            PzrLevelError = pzrLevelError;
            PrimaryMassError = primaryMassError;
        }
    }

    /// <summary>
    /// Stage D comparator harness. Shadow captures are read-only and side-effect free.
    /// </summary>
    public static class ModuleComparator
    {
        public static ModuleShadowState CaptureShadow(Func<ModuleShadowState> shadowCapture)
        {
            if (shadowCapture == null)
                return default;

            // Shadow capture contract: read-only projection into temporary value object.
            return shadowCapture();
        }

        public static ModuleComparatorResult Compare(
            string moduleId,
            ModuleShadowState legacyShadow,
            ModuleShadowState modularShadow,
            float pressureTolerance,
            float pzrLevelTolerance,
            float primaryMassTolerance)
        {
            float pressureError = Math.Abs(modularShadow.PressurePsia - legacyShadow.PressurePsia);
            float levelError = Math.Abs(modularShadow.PzrLevelPct - legacyShadow.PzrLevelPct);
            float massError = Math.Abs(modularShadow.PrimaryMassLb - legacyShadow.PrimaryMassLb);

            bool pass = pressureError <= pressureTolerance &&
                        levelError <= pzrLevelTolerance &&
                        massError <= primaryMassTolerance;

            return new ModuleComparatorResult(moduleId, pass, pressureError, levelError, massError);
        }
    }
}
