using System;

namespace Critical.Simulation.Modular.State
{
    /// <summary>
    /// Single Stage B projection writer from legacy engine runtime to PlantState.
    /// </summary>
    public static class LegacyStateBridge
    {
        public static PlantState Export(HeatupSimEngine engine, float dtHr)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));

            return new PlantState(
                engine.simTime,
                dtHr,
                engine.plantMode,
                engine.pressure,
                engine.T_avg,
                engine.T_rcs,
                engine.pzrLevel,
                engine.pzrHeaterPower,
                engine.sprayFlow_GPM,
                engine.chargingFlow,
                engine.letdownFlow,
                engine.surgeFlow,
                engine.stageE_PrimaryHeatInput_MW,
                engine.rhrNetHeat_MW,
                engine.primaryMassLedger_lb,
                engine.primaryMassComponents_lb,
                engine.primaryMassDrift_lb,
                engine.primaryMassBoundaryError_lb,
                engine.primaryMassExpected_lb,
                engine.totalSystemMass_lbm,
                engine.primaryMassConservationOK,
                engine.rcpCount,
                engine.rhrActive,
                engine.rhrState.FlowRate_gpm,
                engine.vctState.Level_percent,
                engine.rcsBoronConcentration,
                engine.heatupPhaseDesc);
        }
    }
}
