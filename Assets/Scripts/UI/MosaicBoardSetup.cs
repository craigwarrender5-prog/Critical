// CRITICAL: Master the Atom - Phase 2 Mosaic Board Setup
// MosaicBoardSetup.cs - Runtime Initialization and Wiring
//
// Handles runtime setup of Mosaic Board:
//   - Creates ReactorController if missing
//   - Wires up all component references
//   - Initializes to specified scenario
//
// Usage: Attach to MosaicBoard GameObject for automatic setup.

using UnityEngine;

namespace Critical.UI
{
    using Controllers;
    using Physics;
    
    /// <summary>
    /// Runtime setup for Mosaic Board.
    /// </summary>
    [RequireComponent(typeof(MosaicBoard))]
    public class MosaicBoardSetup : MonoBehaviour
    {
        #region Unity Inspector Fields
        
        [Header("Initial State")]
        [Tooltip("Initial reactor state")]
        public InitialState StartState = InitialState.HotZeroPower;
        
        [Tooltip("Initial power level (for PowerOperation)")]
        [Range(0f, 1f)]
        public float InitialPower = 1.0f;
        
        [Tooltip("Initial boron concentration")]
        public float InitialBoron_ppm = 1500f;
        
        [Header("Simulation Settings")]
        [Tooltip("Initial time compression")]
        public float InitialTimeCompression = 1f;
        
        [Tooltip("Auto-start simulation")]
        public bool AutoStart = true;
        
        [Header("Debug")]
        [Tooltip("Show debug info")]
        public bool DebugMode = false;
        
        #endregion
        
        #region Enums
        
        public enum InitialState
        {
            ColdShutdown,
            HotZeroPower,
            PowerOperation
        }
        
        #endregion
        
        #region Private Fields
        
        private MosaicBoard _board;
        private ReactorController _reactor;
        private ReactorSimEngine _simEngine;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _board = GetComponent<MosaicBoard>();
            
            // Find or create ReactorController
            SetupReactorController();
            
            // Find or create SimEngine
            SetupSimEngine();
            
            // Wire up references
            WireReferences();
        }
        
        private void Start()
        {
            // Initialize to starting state
            InitializeReactor();
            
            if (AutoStart)
            {
                _reactor.TimeCompression = InitialTimeCompression;
            }
            else
            {
                _reactor.TimeCompression = 0f;
            }
            
            if (DebugMode)
            {
                LogState();
            }
        }
        
        #endregion
        
        #region Setup Methods
        
        private void SetupReactorController()
        {
            _reactor = FindObjectOfType<ReactorController>();
            
            if (_reactor == null)
            {
                var reactorGO = new GameObject("ReactorController");
                _reactor = reactorGO.AddComponent<ReactorController>();
                
                if (DebugMode)
                {
                    Debug.Log("[MosaicBoardSetup] Created ReactorController");
                }
            }
            
            _board.Reactor = _reactor;
        }
        
        private void SetupSimEngine()
        {
            _simEngine = FindObjectOfType<ReactorSimEngine>();
            
            if (_simEngine == null)
            {
                var simGO = new GameObject("ReactorSimEngine");
                _simEngine = simGO.AddComponent<ReactorSimEngine>();
                
                if (DebugMode)
                {
                    Debug.Log("[MosaicBoardSetup] Created ReactorSimEngine");
                }
            }
            
            _board.SimEngine = _simEngine;
            _simEngine.Reactor = _reactor;
        }
        
        private void WireReferences()
        {
            // Wire alarm panel
            var alarmPanel = GetComponentInChildren<MosaicAlarmPanel>();
            if (alarmPanel != null)
            {
                // Panel will self-wire via MosaicBoard.Instance
            }
            
            // Wire control panel
            var controlPanel = GetComponentInChildren<MosaicControlPanel>();
            if (controlPanel != null)
            {
                // Panel will self-wire via MosaicBoard.Instance
            }
            
            // Wire rod display
            var rodDisplay = GetComponentInChildren<MosaicRodDisplay>();
            if (rodDisplay != null)
            {
                // Display will self-wire via MosaicBoard.Instance
            }
            
            if (DebugMode)
            {
                Debug.Log("[MosaicBoardSetup] References wired");
            }
        }
        
        private void InitializeReactor()
        {
            switch (StartState)
            {
                case InitialState.ColdShutdown:
                    _reactor.Reset();
                    break;
                    
                case InitialState.HotZeroPower:
                    _reactor.InitializeToHZP();
                    break;
                    
                case InitialState.PowerOperation:
                    _reactor.InitializeToPower(InitialPower);
                    break;
            }
            
            if (DebugMode)
            {
                Debug.Log($"[MosaicBoardSetup] Initialized to {StartState}");
            }
        }
        
        #endregion
        
        #region Debug
        
        private void LogState()
        {
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("  MOSAIC BOARD INITIALIZED");
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log($"  State:    {StartState}");
            Debug.Log($"  Power:    {_reactor.ThermalPower * 100f:F1}%");
            Debug.Log($"  Tavg:     {_reactor.Tavg:F1}°F");
            Debug.Log($"  Boron:    {_reactor.Boron_ppm:F0} ppm");
            Debug.Log($"  Bank D:   {_reactor.BankDPosition:F0} steps");
            Debug.Log($"  Tripped:  {_reactor.IsTripped}");
            Debug.Log("═══════════════════════════════════════════════════════════");
        }
        
        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            LogState();
        }
        
        [ContextMenu("Initialize to HZP")]
        public void MenuInitHZP()
        {
            _reactor.InitializeToHZP();
            LogState();
        }
        
        [ContextMenu("Initialize to 100%")]
        public void MenuInit100()
        {
            _reactor.InitializeToPower(1.0f);
            LogState();
        }
        
        #endregion
    }
}
