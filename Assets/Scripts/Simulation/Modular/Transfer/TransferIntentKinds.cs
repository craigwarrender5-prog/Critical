namespace Critical.Simulation.Modular.Transfer
{
    /// <summary>
    /// Canonical transfer-intent signal IDs used by modular modules.
    /// </summary>
    public static class TransferIntentKinds
    {
        public const string AuthorityLegacyStep = "LEGACY_STEP";
        public const string AuthorityModularPzr = "MODULAR_PZR";
        public const string AuthorityPbocEvent = "PBOC_EVENT";

        public const string SignalSurgeFlowGpm = "SURGE_FLOW_GPM";
        public const string SignalSprayFlowGpm = "SPRAY_FLOW_GPM";
        public const string SignalPzrHeaterPowerMw = "PZR_HEATER_POWER_MW";
        public const string SignalSprayCondensedMassLb = "PZR_SPRAY_CONDENSED_LB";
        public const string SignalPrimaryBoundaryInLb = "PRIMARY_BOUNDARY_IN_LB";
        public const string SignalPrimaryBoundaryOutLb = "PRIMARY_BOUNDARY_OUT_LB";
    }
}
