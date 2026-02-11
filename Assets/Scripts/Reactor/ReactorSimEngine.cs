// CRITICAL: Master the Atom - Phase 2 Simulation Engine
// ReactorSimEngine.cs - Scenario Management and Simulation Coordination
//
// Manages simulation scenarios and coordinates with ReactorController:
//   - Predefined startup sequences (Cold → HZP → Power)
//   - Scenario loading and saving
//   - Automatic milestone tracking
//   - Training scenarios with targets
//
// Reference: Westinghouse 4-Loop PWR (3411 MWt)
//
// Gold Standard Architecture:
//   - Pure coordination role
//   - No physics calculations
//   - ReactorController owns reactor behavior

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Critical.Controllers
{
    using Physics;
    
    /// <summary>
    /// Simulation scenario type enumeration.
    /// </summary>
    public enum ScenarioType
    {
        /// <summary>Free-form operation, no objectives</summary>
        FreePlay,
        
        /// <summary>Startup from Hot Zero Power to specified power</summary>
        Startup,
        
        /// <summary>Power maneuvering between power levels</summary>
        LoadFollow,
        
        /// <summary>Reactor trip and recovery</summary>
        Trip,
        
        /// <summary>Xenon oscillation management</summary>
        XenonTransient,
        
        /// <summary>Custom scenario with defined objectives</summary>
        Custom
    }
    
    /// <summary>
    /// Milestone types for scenario tracking.
    /// </summary>
    public enum MilestoneType
    {
        CriticalityAchieved,
        PowerLevelReached,
        SteadyStateAchieved,
        TavgOnProgram,
        XenonEquilibrium,
        TargetBoronReached,
        TripSuccessful,
        TripRecoveryComplete,
        Custom
    }
    
    /// <summary>
    /// Represents a milestone in a scenario.
    /// </summary>
    [Serializable]
    public class ScenarioMilestone
    {
        public MilestoneType Type;
        public string Description;
        public float TargetValue;
        public float Tolerance;
        public bool IsCompleted;
        public float CompletedAtTime;
        
        public ScenarioMilestone(MilestoneType type, string description, float target = 0f, float tolerance = 0f)
        {
            Type = type;
            Description = description;
            TargetValue = target;
            Tolerance = tolerance;
            IsCompleted = false;
            CompletedAtTime = -1f;
        }
    }
    
    /// <summary>
    /// Scenario definition with objectives and parameters.
    /// </summary>
    [Serializable]
    public class SimulationScenario
    {
        public string Name;
        public string Description;
        public ScenarioType Type;
        
        // Initial conditions
        public float InitialPower;
        public float InitialBoron_ppm;
        public float InitialXenon_pcm;
        public float InletTemp_F;
        
        // Targets
        public float TargetPower;
        public float TargetBoron_ppm;
        public float TimeLimit_min;
        
        // Milestones
        public List<ScenarioMilestone> Milestones = new List<ScenarioMilestone>();
        
        // Completion
        public bool IsCompleted;
        public float CompletionTime_min;
        public int Score;
    }
    
    /// <summary>
    /// Simulation engine managing scenarios and coordination.
    /// </summary>
    public class ReactorSimEngine : MonoBehaviour
    {
        #region Unity Inspector Fields
        
        [Header("Reactor Reference")]
        [Tooltip("Reference to ReactorController")]
        public ReactorController Reactor;
        
        [Header("Scenario")]
        [Tooltip("Current scenario type")]
        public ScenarioType CurrentScenarioType = ScenarioType.FreePlay;
        
        [Header("Startup Parameters")]
        [Tooltip("Target power for startup scenarios")]
        [Range(0f, 1f)]
        public float StartupTargetPower = 1.0f;
        
        [Tooltip("Power ramp rate (%/min)")]
        [Range(1f, 5f)]
        public float PowerRampRate = 3f;
        
        [Header("Steady State Criteria")]
        [Tooltip("Power stability tolerance (%)")]
        public float PowerStabilityTolerance = 0.5f;
        
        [Tooltip("Tavg stability tolerance (°F)")]
        public float TavgStabilityTolerance = 2f;
        
        [Tooltip("Time required at stable conditions (seconds)")]
        public float StabilityDuration = 30f;
        
        [Header("Debug")]
        public bool DebugLogging = false;
        
        #endregion
        
        #region Events
        
        /// <summary>Fired when scenario starts</summary>
        public event Action<SimulationScenario> OnScenarioStart;
        
        /// <summary>Fired when milestone completed</summary>
        public event Action<ScenarioMilestone> OnMilestoneComplete;
        
        /// <summary>Fired when scenario completed</summary>
        public event Action<SimulationScenario> OnScenarioComplete;
        
        /// <summary>Fired when scenario failed</summary>
        public event Action<SimulationScenario, string> OnScenarioFailed;
        
        #endregion
        
        #region Private Fields
        
        private SimulationScenario _currentScenario;
        private bool _scenarioActive = false;
        private float _scenarioStartTime;
        
        // Stability tracking
        private float _stableStartTime = -1f;
        private float _lastPower;
        private float _lastTavg;
        
        // Predefined scenarios
        private Dictionary<string, SimulationScenario> _scenarioLibrary;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>Current active scenario</summary>
        public SimulationScenario CurrentScenario => _currentScenario;
        
        /// <summary>Is a scenario currently active?</summary>
        public bool IsScenarioActive => _scenarioActive;
        
        /// <summary>Scenario elapsed time in minutes</summary>
        public float ScenarioElapsedTime_min => _scenarioActive 
            ? (Reactor?.SimulationTime ?? 0f - _scenarioStartTime) / 60f 
            : 0f;
        
        /// <summary>Number of milestones completed</summary>
        public int MilestonesCompleted => _currentScenario?.Milestones?.FindAll(m => m.IsCompleted).Count ?? 0;
        
        /// <summary>Total milestones in scenario</summary>
        public int TotalMilestones => _currentScenario?.Milestones?.Count ?? 0;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeScenarioLibrary();
        }
        
        private void Start()
        {
            // Auto-find reactor if not assigned
            if (Reactor == null)
            {
                Reactor = FindObjectOfType<ReactorController>();
            }
            
            if (Reactor != null)
            {
                // Subscribe to reactor events
                Reactor.OnCriticality += HandleCriticality;
                Reactor.OnReactorTrip += HandleTrip;
                Reactor.OnPowerReached += HandlePowerReached;
            }
        }
        
        private void Update()
        {
            if (!_scenarioActive || Reactor == null) return;
            
            // Check milestone progress
            CheckMilestones();
            
            // Check for steady state
            CheckSteadyState();
            
            // Check for time limit
            CheckTimeLimit();
        }
        
        private void OnDestroy()
        {
            if (Reactor != null)
            {
                Reactor.OnCriticality -= HandleCriticality;
                Reactor.OnReactorTrip -= HandleTrip;
                Reactor.OnPowerReached -= HandlePowerReached;
            }
        }
        
        #endregion
        
        #region Scenario Library
        
        /// <summary>
        /// Initialize the predefined scenario library.
        /// </summary>
        private void InitializeScenarioLibrary()
        {
            _scenarioLibrary = new Dictionary<string, SimulationScenario>();
            
            // Startup from HZP to 100%
            _scenarioLibrary["startup_100"] = new SimulationScenario
            {
                Name = "Reactor Startup to 100%",
                Description = "Start from Hot Zero Power and bring the reactor to 100% power",
                Type = ScenarioType.Startup,
                InitialPower = 0f,
                InitialBoron_ppm = 1500f,
                InitialXenon_pcm = 0f,
                InletTemp_F = 557f,
                TargetPower = 1.0f,
                TargetBoron_ppm = 800f,
                TimeLimit_min = 480f, // 8 hours
                Milestones = new List<ScenarioMilestone>
                {
                    new ScenarioMilestone(MilestoneType.CriticalityAchieved, "Achieve criticality"),
                    new ScenarioMilestone(MilestoneType.PowerLevelReached, "Reach 10% power", 0.10f, 0.01f),
                    new ScenarioMilestone(MilestoneType.PowerLevelReached, "Reach 50% power", 0.50f, 0.02f),
                    new ScenarioMilestone(MilestoneType.PowerLevelReached, "Reach 100% power", 1.00f, 0.005f),
                    new ScenarioMilestone(MilestoneType.TavgOnProgram, "Tavg on program at 100%", 588f, 2f),
                    new ScenarioMilestone(MilestoneType.SteadyStateAchieved, "Achieve steady state at 100%")
                }
            };
            
            // Startup from HZP to 50%
            _scenarioLibrary["startup_50"] = new SimulationScenario
            {
                Name = "Reactor Startup to 50%",
                Description = "Start from Hot Zero Power and bring the reactor to 50% power",
                Type = ScenarioType.Startup,
                InitialPower = 0f,
                InitialBoron_ppm = 1500f,
                InitialXenon_pcm = 0f,
                InletTemp_F = 557f,
                TargetPower = 0.5f,
                TargetBoron_ppm = 1100f,
                TimeLimit_min = 240f, // 4 hours
                Milestones = new List<ScenarioMilestone>
                {
                    new ScenarioMilestone(MilestoneType.CriticalityAchieved, "Achieve criticality"),
                    new ScenarioMilestone(MilestoneType.PowerLevelReached, "Reach 25% power", 0.25f, 0.02f),
                    new ScenarioMilestone(MilestoneType.PowerLevelReached, "Reach 50% power", 0.50f, 0.01f),
                    new ScenarioMilestone(MilestoneType.SteadyStateAchieved, "Achieve steady state at 50%")
                }
            };
            
            // Load follow 100% to 50% to 100%
            _scenarioLibrary["loadfollow"] = new SimulationScenario
            {
                Name = "Load Follow Maneuver",
                Description = "Reduce power from 100% to 50%, then return to 100%",
                Type = ScenarioType.LoadFollow,
                InitialPower = 1.0f,
                InitialBoron_ppm = 800f,
                InitialXenon_pcm = -2800f,
                InletTemp_F = 558f,
                TargetPower = 1.0f,
                TimeLimit_min = 720f, // 12 hours (xenon transient)
                Milestones = new List<ScenarioMilestone>
                {
                    new ScenarioMilestone(MilestoneType.PowerLevelReached, "Reduce to 50% power", 0.50f, 0.02f),
                    new ScenarioMilestone(MilestoneType.SteadyStateAchieved, "Hold at 50%"),
                    new ScenarioMilestone(MilestoneType.PowerLevelReached, "Return to 100% power", 1.00f, 0.02f),
                    new ScenarioMilestone(MilestoneType.XenonEquilibrium, "Xenon returned to equilibrium", -2800f, 200f)
                }
            };
            
            // Trip and recovery
            _scenarioLibrary["trip_recovery"] = new SimulationScenario
            {
                Name = "Trip Recovery",
                Description = "Recover from a reactor trip within the xenon window",
                Type = ScenarioType.Trip,
                InitialPower = 1.0f,
                InitialBoron_ppm = 800f,
                InitialXenon_pcm = -2800f,
                InletTemp_F = 558f,
                TargetPower = 0.75f,
                TimeLimit_min = 60f, // 1 hour xenon window
                Milestones = new List<ScenarioMilestone>
                {
                    new ScenarioMilestone(MilestoneType.TripSuccessful, "Trip occurs"),
                    new ScenarioMilestone(MilestoneType.CriticalityAchieved, "Regain criticality"),
                    new ScenarioMilestone(MilestoneType.PowerLevelReached, "Reach 75% power", 0.75f, 0.05f),
                    new ScenarioMilestone(MilestoneType.TripRecoveryComplete, "Stable operation achieved")
                }
            };
            
            // Free play
            _scenarioLibrary["freeplay"] = new SimulationScenario
            {
                Name = "Free Play",
                Description = "Operate the reactor freely with no objectives",
                Type = ScenarioType.FreePlay,
                InitialPower = 0f,
                InitialBoron_ppm = 1500f,
                InitialXenon_pcm = 0f,
                InletTemp_F = 557f,
                TargetPower = 0f,
                TimeLimit_min = -1f // No limit
            };
            
            if (DebugLogging)
            {
                Debug.Log($"[SimEngine] Loaded {_scenarioLibrary.Count} predefined scenarios");
            }
        }
        
        /// <summary>
        /// Get list of available scenario names.
        /// </summary>
        public string[] GetAvailableScenarios()
        {
            var names = new string[_scenarioLibrary.Count];
            _scenarioLibrary.Keys.CopyTo(names, 0);
            return names;
        }
        
        #endregion
        
        #region Scenario Control
        
        /// <summary>
        /// Load and start a predefined scenario.
        /// </summary>
        /// <param name="scenarioKey">Scenario key from library</param>
        public void LoadScenario(string scenarioKey)
        {
            if (!_scenarioLibrary.TryGetValue(scenarioKey, out var scenario))
            {
                Debug.LogError($"[SimEngine] Unknown scenario: {scenarioKey}");
                return;
            }
            
            StartScenario(scenario);
        }
        
        /// <summary>
        /// Start a scenario.
        /// </summary>
        /// <param name="scenario">Scenario to start</param>
        public void StartScenario(SimulationScenario scenario)
        {
            if (Reactor == null)
            {
                Debug.LogError("[SimEngine] No ReactorController assigned");
                return;
            }
            
            _currentScenario = scenario;
            _currentScenario.IsCompleted = false;
            _currentScenario.Score = 0;
            
            // Reset milestones
            foreach (var milestone in _currentScenario.Milestones)
            {
                milestone.IsCompleted = false;
                milestone.CompletedAtTime = -1f;
            }
            
            // Initialize reactor to scenario conditions
            if (scenario.InitialPower > 0.01f)
            {
                Reactor.InitializeToPower(scenario.InitialPower);
            }
            else
            {
                Reactor.InitializeToHZP();
            }
            
            Reactor.SetBoron(scenario.InitialBoron_ppm);
            Reactor.CoolantInletTemp_F = scenario.InletTemp_F;
            
            _scenarioStartTime = Reactor.SimulationTime;
            _scenarioActive = true;
            _stableStartTime = -1f;
            
            CurrentScenarioType = scenario.Type;
            
            OnScenarioStart?.Invoke(_currentScenario);
            
            if (DebugLogging)
            {
                Debug.Log($"[SimEngine] Started scenario: {scenario.Name}");
            }
        }
        
        /// <summary>
        /// Start quick startup scenario with current settings.
        /// </summary>
        public void StartQuickStartup()
        {
            var scenario = new SimulationScenario
            {
                Name = "Quick Startup",
                Description = $"Startup to {StartupTargetPower * 100f:F0}% power",
                Type = ScenarioType.Startup,
                InitialPower = 0f,
                InitialBoron_ppm = Reactor?.Boron_ppm ?? 1500f,
                InletTemp_F = Reactor?.CoolantInletTemp_F ?? 557f,
                TargetPower = StartupTargetPower,
                TimeLimit_min = 480f,
                Milestones = new List<ScenarioMilestone>
                {
                    new ScenarioMilestone(MilestoneType.CriticalityAchieved, "Achieve criticality"),
                    new ScenarioMilestone(MilestoneType.PowerLevelReached, 
                        $"Reach {StartupTargetPower * 100f:F0}% power", 
                        StartupTargetPower, 0.01f),
                    new ScenarioMilestone(MilestoneType.SteadyStateAchieved, "Achieve steady state")
                }
            };
            
            StartScenario(scenario);
        }
        
        /// <summary>
        /// Stop current scenario without completion.
        /// </summary>
        public void StopScenario()
        {
            if (!_scenarioActive) return;
            
            _scenarioActive = false;
            
            if (DebugLogging)
            {
                Debug.Log("[SimEngine] Scenario stopped");
            }
        }
        
        /// <summary>
        /// Restart current scenario from beginning.
        /// </summary>
        public void RestartScenario()
        {
            if (_currentScenario != null)
            {
                StartScenario(_currentScenario);
            }
        }
        
        #endregion
        
        #region Milestone Checking
        
        /// <summary>
        /// Check progress on all milestones.
        /// </summary>
        private void CheckMilestones()
        {
            if (_currentScenario == null) return;
            
            float currentTime = Reactor.SimulationTime;
            
            foreach (var milestone in _currentScenario.Milestones)
            {
                if (milestone.IsCompleted) continue;
                
                bool completed = CheckMilestone(milestone);
                
                if (completed)
                {
                    milestone.IsCompleted = true;
                    milestone.CompletedAtTime = currentTime;
                    
                    OnMilestoneComplete?.Invoke(milestone);
                    
                    if (DebugLogging)
                    {
                        Debug.Log($"[SimEngine] Milestone completed: {milestone.Description}");
                    }
                }
            }
            
            // Check if all milestones completed
            if (_currentScenario.Milestones.TrueForAll(m => m.IsCompleted))
            {
                CompleteScenario();
            }
        }
        
        /// <summary>
        /// Check a single milestone for completion.
        /// </summary>
        private bool CheckMilestone(ScenarioMilestone milestone)
        {
            switch (milestone.Type)
            {
                case MilestoneType.CriticalityAchieved:
                    return Reactor.IsCritical;
                    
                case MilestoneType.PowerLevelReached:
                    return Mathf.Abs(Reactor.ThermalPower - milestone.TargetValue) <= milestone.Tolerance;
                    
                case MilestoneType.TavgOnProgram:
                    float programTavg = Reactor.CalculateTavgProgram(Reactor.ThermalPower);
                    return Mathf.Abs(Reactor.Tavg - programTavg) <= milestone.Tolerance;
                    
                case MilestoneType.XenonEquilibrium:
                    return Mathf.Abs(Reactor.Xenon_pcm - milestone.TargetValue) <= milestone.Tolerance;
                    
                case MilestoneType.TargetBoronReached:
                    return Mathf.Abs(Reactor.Boron_ppm - milestone.TargetValue) <= milestone.Tolerance;
                    
                case MilestoneType.TripSuccessful:
                    return Reactor.IsTripped;
                    
                case MilestoneType.SteadyStateAchieved:
                    return IsSteadyState();
                    
                case MilestoneType.TripRecoveryComplete:
                    return !Reactor.IsTripped && Reactor.ThermalPower >= _currentScenario.TargetPower * 0.9f && IsSteadyState();
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Check for steady state conditions.
        /// </summary>
        private void CheckSteadyState()
        {
            float currentPower = Reactor.ThermalPower;
            float currentTavg = Reactor.Tavg;
            
            bool powerStable = Mathf.Abs(currentPower - _lastPower) < PowerStabilityTolerance / 100f;
            bool tavgStable = Mathf.Abs(currentTavg - _lastTavg) < TavgStabilityTolerance;
            
            if (powerStable && tavgStable)
            {
                if (_stableStartTime < 0f)
                {
                    _stableStartTime = Reactor.SimulationTime;
                }
            }
            else
            {
                _stableStartTime = -1f;
            }
            
            _lastPower = currentPower;
            _lastTavg = currentTavg;
        }
        
        /// <summary>
        /// Is reactor in steady state?
        /// </summary>
        public bool IsSteadyState()
        {
            if (_stableStartTime < 0f) return false;
            return (Reactor.SimulationTime - _stableStartTime) >= StabilityDuration;
        }
        
        #endregion
        
        #region Scenario Completion
        
        /// <summary>
        /// Check for time limit exceeded.
        /// </summary>
        private void CheckTimeLimit()
        {
            if (_currentScenario.TimeLimit_min <= 0f) return;
            
            if (ScenarioElapsedTime_min > _currentScenario.TimeLimit_min)
            {
                FailScenario("Time limit exceeded");
            }
        }
        
        /// <summary>
        /// Complete scenario successfully.
        /// </summary>
        private void CompleteScenario()
        {
            _scenarioActive = false;
            _currentScenario.IsCompleted = true;
            _currentScenario.CompletionTime_min = ScenarioElapsedTime_min;
            
            // Calculate score based on time and efficiency
            _currentScenario.Score = CalculateScore();
            
            OnScenarioComplete?.Invoke(_currentScenario);
            
            if (DebugLogging)
            {
                Debug.Log($"[SimEngine] Scenario completed! Score: {_currentScenario.Score}");
            }
        }
        
        /// <summary>
        /// Fail scenario.
        /// </summary>
        private void FailScenario(string reason)
        {
            _scenarioActive = false;
            _currentScenario.IsCompleted = false;
            _currentScenario.Score = 0;
            
            OnScenarioFailed?.Invoke(_currentScenario, reason);
            
            if (DebugLogging)
            {
                Debug.Log($"[SimEngine] Scenario failed: {reason}");
            }
        }
        
        /// <summary>
        /// Calculate scenario score.
        /// </summary>
        private int CalculateScore()
        {
            int score = 1000; // Base score
            
            // Time bonus (faster = better, up to 500 points)
            if (_currentScenario.TimeLimit_min > 0)
            {
                float timeRatio = ScenarioElapsedTime_min / _currentScenario.TimeLimit_min;
                int timeBonus = Mathf.RoundToInt((1f - timeRatio) * 500f);
                score += Mathf.Max(0, timeBonus);
            }
            
            // Milestone bonuses
            score += MilestonesCompleted * 100;
            
            // Penalty for trips (unless trip scenario)
            if (_currentScenario.Type != ScenarioType.Trip && Reactor.IsTripped)
            {
                score -= 500;
            }
            
            return Mathf.Max(0, score);
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleCriticality()
        {
            // Mark criticality milestone if present
            var milestone = _currentScenario?.Milestones?.Find(m => 
                m.Type == MilestoneType.CriticalityAchieved && !m.IsCompleted);
            
            if (milestone != null)
            {
                milestone.IsCompleted = true;
                milestone.CompletedAtTime = Reactor.SimulationTime;
                OnMilestoneComplete?.Invoke(milestone);
            }
        }
        
        private void HandleTrip()
        {
            if (_currentScenario?.Type == ScenarioType.Trip)
            {
                // Expected trip in trip scenario
                var milestone = _currentScenario.Milestones.Find(m => 
                    m.Type == MilestoneType.TripSuccessful && !m.IsCompleted);
                
                if (milestone != null)
                {
                    milestone.IsCompleted = true;
                    milestone.CompletedAtTime = Reactor.SimulationTime;
                    OnMilestoneComplete?.Invoke(milestone);
                }
            }
            else if (_scenarioActive)
            {
                // Unexpected trip - don't fail immediately, allow recovery in some scenarios
                if (DebugLogging)
                {
                    Debug.Log("[SimEngine] Unexpected reactor trip during scenario");
                }
            }
        }
        
        private void HandlePowerReached(float power)
        {
            // Check if this matches a power milestone
            var milestone = _currentScenario?.Milestones?.Find(m => 
                m.Type == MilestoneType.PowerLevelReached && 
                !m.IsCompleted &&
                Mathf.Abs(power - m.TargetValue) <= m.Tolerance);
            
            if (milestone != null)
            {
                milestone.IsCompleted = true;
                milestone.CompletedAtTime = Reactor.SimulationTime;
                OnMilestoneComplete?.Invoke(milestone);
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Get progress percentage toward completion.
        /// </summary>
        public float GetProgressPercent()
        {
            if (_currentScenario == null || TotalMilestones == 0)
                return 0f;
            
            return (float)MilestonesCompleted / TotalMilestones * 100f;
        }
        
        /// <summary>
        /// Get current milestone description.
        /// </summary>
        public string GetCurrentObjective()
        {
            if (_currentScenario == null) return "No active scenario";
            
            var nextMilestone = _currentScenario.Milestones.Find(m => !m.IsCompleted);
            
            return nextMilestone?.Description ?? "All objectives complete!";
        }
        
        /// <summary>
        /// Get formatted scenario status.
        /// </summary>
        public string GetStatusText()
        {
            if (!_scenarioActive || _currentScenario == null)
            {
                return "No active scenario";
            }
            
            return $"{_currentScenario.Name}\n" +
                   $"Objective: {GetCurrentObjective()}\n" +
                   $"Progress: {MilestonesCompleted}/{TotalMilestones} ({GetProgressPercent():F0}%)\n" +
                   $"Time: {ScenarioElapsedTime_min:F1} min";
        }
        
        #endregion
        
        #region Validation
        
        #if UNITY_EDITOR
        
        [ContextMenu("Run Validation Tests")]
        public void RunValidationTests()
        {
            Debug.Log("=== ReactorSimEngine Validation ===");
            int passed = 0;
            int failed = 0;
            
            // Test 1: Scenario library initialized
            if (_scenarioLibrary != null && _scenarioLibrary.Count > 0)
            {
                Debug.Log($"✓ Test 1: Scenario library has {_scenarioLibrary.Count} scenarios");
                passed++;
            }
            else
            {
                Debug.LogError("✗ Test 1: Scenario library not initialized");
                failed++;
            }
            
            // Test 2: Startup scenario has correct milestones
            if (_scenarioLibrary.TryGetValue("startup_100", out var startup))
            {
                if (startup.Milestones.Count >= 5)
                {
                    Debug.Log($"✓ Test 2: Startup scenario has {startup.Milestones.Count} milestones");
                    passed++;
                }
                else
                {
                    Debug.LogError($"✗ Test 2: Expected 5+ milestones, got {startup.Milestones.Count}");
                    failed++;
                }
            }
            else
            {
                Debug.LogError("✗ Test 2: Startup scenario not found");
                failed++;
            }
            
            // Test 3: Progress calculation
            var testScenario = new SimulationScenario
            {
                Milestones = new List<ScenarioMilestone>
                {
                    new ScenarioMilestone(MilestoneType.CriticalityAchieved, "Test 1") { IsCompleted = true },
                    new ScenarioMilestone(MilestoneType.PowerLevelReached, "Test 2") { IsCompleted = false },
                    new ScenarioMilestone(MilestoneType.SteadyStateAchieved, "Test 3") { IsCompleted = false }
                }
            };
            _currentScenario = testScenario;
            
            float progress = GetProgressPercent();
            if (Mathf.Abs(progress - 33.33f) < 1f)
            {
                Debug.Log($"✓ Test 3: Progress calculation correct ({progress:F1}%)");
                passed++;
            }
            else
            {
                Debug.LogError($"✗ Test 3: Expected ~33%, got {progress:F1}%");
                failed++;
            }
            
            _currentScenario = null;
            
            Debug.Log($"=== Validation Complete: {passed} passed, {failed} failed ===");
        }
        
        #endif
        
        #endregion
    }
}
