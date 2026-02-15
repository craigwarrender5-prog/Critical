namespace Critical.Simulation.Modular
{
    /// <summary>
    /// Minimal module contract for coordinator-owned step orchestration.
    /// </summary>
    public interface IPlantModule
    {
        string ModuleId { get; }

        void Initialize();

        void Step(float dt);

        void Shutdown();
    }
}
