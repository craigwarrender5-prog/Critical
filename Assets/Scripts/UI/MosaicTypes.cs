// CRITICAL: Master the Atom - Phase 2 Mosaic Types
// MosaicTypes.cs - Shared Type Definitions for Mosaic Board UI
//
// Contains enums and common types used across all Mosaic components.

namespace Critical.UI
{
    /// <summary>
    /// Types of gauges available on the Mosaic Board.
    /// </summary>
    public enum GaugeType
    {
        NeutronPower,
        ThermalPower,
        Tavg,
        Thot,
        Tcold,
        DeltaT,
        FuelCenterline,
        TotalReactivity,
        StartupRate,
        ReactorPeriod,
        Boron,
        Xenon,
        BankDPosition,
        FlowFraction
    }
    
    /// <summary>
    /// Alarm severity states.
    /// </summary>
    public enum AlarmState
    {
        Normal = 0,
        Warning = 1,
        Alarm = 2,
        Trip = 3
    }
}
