namespace Critical.Simulation.Modular
{
    /// <summary>
    /// Centralized modular migration feature flags.
    /// All flags default to false by design.
    /// </summary>
    public static class ModularFeatureFlags
    {
        // Stage A seam toggle: run coordinator path from HeatupSimEngine.
        public static bool EnableCoordinatorPath { get; set; } = false;

        // Per-system extraction flags.
        public static bool UseModularPZR { get; set; } = false;
        public static bool UseModularCVCS { get; set; } = false;
        public static bool UseModularRHR { get; set; } = false;
        public static bool UseModularRCP { get; set; } = false;
        public static bool UseModularReactor { get; set; } = false;
        public static bool UseModularRCS { get; set; } = false;

        // Optional comparator flags (Stage D harness).
        public static bool EnableComparatorPZR { get; set; } = false;
        public static bool EnableComparatorCVCS { get; set; } = false;
        public static bool EnableComparatorRHR { get; set; } = false;
        public static bool EnableComparatorRCP { get; set; } = false;
        public static bool EnableComparatorReactor { get; set; } = false;
        public static bool EnableComparatorRCS { get; set; } = false;

        public static void ResetAll()
        {
            EnableCoordinatorPath = false;
            UseModularPZR = false;
            UseModularCVCS = false;
            UseModularRHR = false;
            UseModularRCP = false;
            UseModularReactor = false;
            UseModularRCS = false;
            EnableComparatorPZR = false;
            EnableComparatorCVCS = false;
            EnableComparatorRHR = false;
            EnableComparatorRCP = false;
            EnableComparatorReactor = false;
            EnableComparatorRCS = false;
        }
    }
}
