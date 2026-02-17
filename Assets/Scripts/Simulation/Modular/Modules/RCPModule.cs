using Critical.Simulation.Modular.Validation;

using Critical.Validation;
namespace Critical.Simulation.Modular.Modules
{
    public sealed class RCPModule : IPlantModule
    {
        private readonly HeatupSimEngine _engine;

        public RCPModule(HeatupSimEngine engine)
        {
            _engine = engine;
        }

        public string ModuleId => "RCP";

        public void Initialize()
        {
        }

        public void Step(float dt)
        {
            // Stage D stub only: no physics moved.
        }

        public void Shutdown()
        {
        }

        public ModuleShadowState CaptureShadowState()
        {
            return new ModuleShadowState(_engine.pressure, _engine.pzrLevel, _engine.primaryMassLedger_lb);
        }
    }
}

