// ============================================================================
// CRITICAL: Master the Atom - Screen Data Bridge
// ScreenDataBridge.cs - Centralized Data Access for All Operator Screens
// ============================================================================
//
// PURPOSE:
//   Provides a single access point for all operator screens to read simulator
//   data. Screens should never directly search for physics modules â€” they
//   call ScreenDataBridge getters instead. This decouples UI from physics
//   module discovery and handles null/missing sources gracefully.
//
// ARCHITECTURE:
//   - Singleton MonoBehaviour (found or created at runtime)
//   - On Start(), searches scene for all known data sources
//   - Exposes typed getter methods for each parameter
//   - Returns safe default values when a source is null or missing
//   - Does NOT modify any physics state (read-only bridge)
//
// DATA SOURCES:
//   - HeatupSimEngine      : Primary source during heatup phase
//   - ReactorController    : Primary source during reactor operations
//   - MosaicBoard          : Gauge data provider for Screen 1
//   - PressurizerState     : Via HeatupSimEngine (PZR pressure, level, heaters)
//   - CVCSController       : Charging/letdown flows, boron
//   - SGMultiNodeThermal   : SG temperatures, levels, pressure (via HeatupSimEngine)
//   - SteamDumpController  : Steam dump flow/state (via HeatupSimEngine state)
//   - RCPSequencer         : Pump states (via HeatupSimEngine)
//   - RHRSystem            : v3.0.0 physics, v4.3.0 getters (via HeatupSimEngine)
//
// PLACEHOLDER CONVENTION:
//   Methods returning float use NaN for "no data available".
//   Screens should check float.IsNaN() and display "---" for placeholders.
//   Methods returning bool default to false.
//   Methods returning string default to "---".
//
// VERSION: 4.3.0
// DATE: 2026-02-11
// CLASSIFICATION: UI â€” Data Infrastructure
// ============================================================================

using UnityEngine;
using Critical.Controllers;
using Critical.Physics;
using Critical.Simulation.Modular.State;

using Critical.Validation;
namespace Critical.UI
{
    /// <summary>
    /// Singleton data bridge providing unified read-only access to all
    /// simulator data sources for operator screens.
    /// </summary>
    public class ScreenDataBridge : MonoBehaviour
    {
        // ====================================================================
        // SINGLETON
        // ====================================================================

        #region Singleton

        private static ScreenDataBridge _instance;
        private static bool _applicationQuitting = false;

        /// <summary>
        /// Global singleton instance. Creates one if not found.
        /// Returns null during application shutdown to prevent spawning
        /// new GameObjects from OnDestroy() callbacks.
        /// </summary>
        public static ScreenDataBridge Instance
        {
            get
            {
                if (_applicationQuitting)
                    return null;

                if (_instance == null)
                {
                    _instance = FindObjectOfType<ScreenDataBridge>();

                    if (_instance == null)
                    {
                        Debug.LogWarning("[ScreenDataBridge] No instance found in scene. Creating one.");
                        GameObject go = new GameObject("ScreenDataBridge");
                        _instance = go.AddComponent<ScreenDataBridge>();
                    }
                }
                return _instance;
            }
        }

        #endregion

        // ====================================================================
        // SERIALIZED FIELDS
        // ====================================================================

        #region Inspector Fields

        [Header("Data Source References (Auto-discovered if null)")]
        [Tooltip("HeatupSimEngine instance â€” primary data source during heatup")]
        [SerializeField] private HeatupSimEngine heatupEngine;

        [Tooltip("ReactorController instance â€” primary data source during operations")]
        [SerializeField] private ReactorController reactorController;

        [Tooltip("MosaicBoard instance â€” gauge data provider")]
        [SerializeField] private MosaicBoard mosaicBoard;

        [Header("Debug")]
        [SerializeField] private bool debugLogging = false;

        #endregion

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        #region Private Fields

        private bool _sourcesResolved = false;

        private bool TryGetPlantStateSnapshot(out PlantState plantState)
        {
            plantState = null;
            if (heatupEngine == null)
                return false;

            StepSnapshot snapshot = heatupEngine.GetStepSnapshot();
            if (snapshot == null || snapshot.PlantState == null || snapshot.PlantState == PlantState.Empty)
                return false;

            plantState = snapshot.PlantState;
            return true;
        }

        #endregion

        // ====================================================================
        // PUBLIC PROPERTIES â€” Source References
        // ====================================================================

        #region Source Properties

        /// <summary>True if a HeatupSimEngine is available and running.</summary>
        public bool HasHeatupEngine => heatupEngine != null;

        /// <summary>True if a ReactorController is available.</summary>
        public bool HasReactorController => reactorController != null;

        /// <summary>True if MosaicBoard is available.</summary>
        public bool HasMosaicBoard => mosaicBoard != null;

        /// <summary>Direct access to HeatupSimEngine (for advanced use). May be null.</summary>
        public HeatupSimEngine HeatupEngine => heatupEngine;

        /// <summary>Direct access to ReactorController (for advanced use). May be null.</summary>
        public ReactorController ReactorCtrl => reactorController;

        /// <summary>Direct access to MosaicBoard (for advanced use). May be null.</summary>
        public MosaicBoard Board => mosaicBoard;

        #endregion

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[ScreenDataBridge] Duplicate instance found. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            ResolveSources();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            _applicationQuitting = true;
        }

        #endregion

        // ====================================================================
        // SOURCE RESOLUTION
        // ====================================================================

        #region Source Resolution

        /// <summary>
        /// Discover all data sources in the scene.
        /// Called automatically on Start(), can be called again if sources change.
        /// </summary>
        public void ResolveSources()
        {
            if (heatupEngine == null)
                heatupEngine = FindObjectOfType<HeatupSimEngine>();

            if (reactorController == null)
                reactorController = FindObjectOfType<ReactorController>();

            if (mosaicBoard == null)
                mosaicBoard = FindObjectOfType<MosaicBoard>();

            _sourcesResolved = true;

            if (debugLogging)
            {
                Debug.Log($"[ScreenDataBridge] Sources resolved â€” " +
                          $"HeatupEngine: {(heatupEngine != null ? "Found" : "NULL")}, " +
                          $"ReactorController: {(reactorController != null ? "Found" : "NULL")}, " +
                          $"MosaicBoard: {(mosaicBoard != null ? "Found" : "NULL")}");
            }
        }

        #endregion

        // ====================================================================
        // CORE REACTOR PARAMETERS
        // ====================================================================

        #region Core Reactor Parameters

        /// <summary>Average coolant temperature (Â°F).</summary>
        public float GetTavg()
        {
            if (heatupEngine != null) return heatupEngine.T_avg;
            if (reactorController != null) return reactorController.CoolantInletTemp_F; // Approximate
            return float.NaN;
        }

        /// <summary>Hot leg temperature (Â°F).</summary>
        public float GetThot()
        {
            if (heatupEngine != null) return heatupEngine.T_hot;
            return float.NaN;
        }

        /// <summary>Cold leg temperature (Â°F).</summary>
        public float GetTcold()
        {
            if (heatupEngine != null) return heatupEngine.T_cold;
            return float.NaN;
        }

        /// <summary>Core delta-T (Â°F). T_hot - T_cold.</summary>
        public float GetDeltaT()
        {
            float thot = GetThot();
            float tcold = GetTcold();
            if (float.IsNaN(thot) || float.IsNaN(tcold)) return float.NaN;
            return thot - tcold;
        }

        /// <summary>RCS flow fraction (0.0â€“1.0).</summary>
        public float GetFlowFraction()
        {
            if (reactorController != null) return reactorController.FlowFraction;
            if (heatupEngine != null) return heatupEngine.rcpCount / 4f;
            return float.NaN;
        }

        /// <summary>Number of running RCPs (0â€“4).</summary>
        public int GetRCPCount()
        {
            if (heatupEngine != null) return heatupEngine.rcpCount;
            if (reactorController != null) return Mathf.RoundToInt(reactorController.FlowFraction * 4f);
            return 0;
        }

        /// <summary>Simulation elapsed time (seconds).</summary>
        public float GetSimulationTime()
        {
            if (heatupEngine != null) return heatupEngine.simTime;
            return Time.time;
        }

        /// <summary>Reactor heatup rate (Â°F/hr).</summary>
        public float GetHeatupRate()
        {
            if (heatupEngine != null) return heatupEngine.heatupRate;
            return float.NaN;
        }

        /// <summary>Plant operating mode (0=Cold Shutdown, etc.).</summary>
        public int GetPlantMode()
        {
            if (heatupEngine != null) return heatupEngine.plantMode;
            return 0;
        }

        /// <summary>Plant mode as descriptive string.</summary>
        public string GetPlantModeString()
        {
            int mode = GetPlantMode();
            return mode switch
            {
                1 => "MODE 1 - POWER OPERATION",
                2 => "MODE 2 - STARTUP",
                3 => "MODE 3 - HOT STANDBY",
                4 => "MODE 4 - HOT SHUTDOWN",
                5 => "MODE 5 - COLD SHUTDOWN",
                6 => "MODE 6 - REFUELING",
                _ => $"MODE {mode}"
            };
        }

        /// <summary>Is the simulator running?</summary>
        public bool IsSimulationRunning()
        {
            if (heatupEngine != null) return heatupEngine.isRunning;
            return true;
        }

        #endregion

        // ====================================================================
        // PRESSURIZER PARAMETERS
        // ====================================================================

        #region Pressurizer Parameters

        /// <summary>Pressurizer pressure (psia).</summary>
        public float GetPZRPressure()
        {
            if (heatupEngine != null) return heatupEngine.pressure;
            return float.NaN;
        }

        /// <summary>Pressurizer level (%).</summary>
        public float GetPZRLevel()
        {
            if (heatupEngine != null) return heatupEngine.pzrLevel;
            return float.NaN;
        }

        /// <summary>Pressurizer water temperature (Â°F).</summary>
        public float GetPZRWaterTemp()
        {
            if (heatupEngine != null) return heatupEngine.T_pzr;
            return float.NaN;
        }

        /// <summary>Pressurizer heater effective power (kW).</summary>
        public float GetHeaterPower()
        {
            if (heatupEngine != null) return heatupEngine.pzrHeaterPower;
            return float.NaN;
        }

        /// <summary>Pressurizer water volume (ftÂ³).</summary>
        public float GetPZRWaterVolume()
        {
            if (heatupEngine != null) return heatupEngine.pzrWaterVolume;
            return float.NaN;
        }

        /// <summary>Pressurizer steam volume (ftÂ³).</summary>
        public float GetPZRSteamVolume()
        {
            if (heatupEngine != null) return heatupEngine.pzrSteamVolume;
            return float.NaN;
        }

        /// <summary>Saturation temperature at current pressure (Â°F).</summary>
        public float GetTsat()
        {
            if (heatupEngine != null) return heatupEngine.T_sat;
            return float.NaN;
        }

        /// <summary>Subcooling margin (Â°F). Tsat - Tavg.</summary>
        public float GetSubcooling()
        {
            if (heatupEngine != null) return heatupEngine.subcooling;
            return float.NaN;
        }

        /// <summary>Pressure rate of change (psi/hr).</summary>
        public float GetPressureRate()
        {
            if (heatupEngine != null) return heatupEngine.pressureRate;
            return float.NaN;
        }

        /// <summary>Surge line flow (gpm).</summary>
        public float GetSurgeFlow()
        {
            if (heatupEngine != null) return heatupEngine.surgeFlow;
            return float.NaN;
        }

        #endregion

        // ====================================================================
        // CVCS PARAMETERS
        // ====================================================================

        #region CVCS Parameters

        /// <summary>CVCS charging flow (gpm).</summary>
        public float GetChargingFlow()
        {
            if (heatupEngine != null) return heatupEngine.chargingFlow;
            return float.NaN;
        }

        /// <summary>CVCS letdown flow (gpm).</summary>
        public float GetLetdownFlow()
        {
            if (heatupEngine != null) return heatupEngine.letdownFlow;
            return float.NaN;
        }

        /// <summary>RCS boron concentration (ppm).</summary>
        public float GetBoronConcentration()
        {
            if (heatupEngine != null) return heatupEngine.rcsBoronConcentration;
            if (reactorController != null) return reactorController.InitialBoron_ppm;
            return float.NaN;
        }

        /// <summary>VCT level (%).</summary>
        public float GetVCTLevel()
        {
            if (heatupEngine != null && heatupEngine.vctState.Level_percent >= 0f)
                return heatupEngine.vctState.Level_percent;
            return float.NaN;
        }

        /// <summary>VCT boron concentration (ppm).</summary>
        public float GetVCTBoronConcentration()
        {
            if (heatupEngine != null) return heatupEngine.vctState.BoronConcentration_ppm;
            return float.NaN;
        }

        /// <summary>Net inventory change rate (gpm). Positive = adding to RCS.</summary>
        public float GetNetInventoryChange()
        {
            float charging = GetChargingFlow();
            float letdown = GetLetdownFlow();
            if (float.IsNaN(charging) || float.IsNaN(letdown)) return float.NaN;
            return charging - letdown;
        }

        #endregion

        // ====================================================================
        // STEAM GENERATOR PARAMETERS
        // ====================================================================

        #region Steam Generator Parameters

        /// <summary>SG secondary average temperature (Â°F).</summary>
        public float GetSGSecondaryTemp()
        {
            if (heatupEngine != null) return heatupEngine.T_sg_secondary;
            return float.NaN;
        }

        /// <summary>SG heat transfer rate (MW).</summary>
        public float GetSGHeatTransfer()
        {
            if (heatupEngine != null) return heatupEngine.sgHeatTransfer_MW;
            return float.NaN;
        }

        /// <summary>SG top node temperature (Â°F) â€” multi-node model.</summary>
        public float GetSGTopNodeTemp()
        {
            if (heatupEngine != null) return heatupEngine.sgTopNodeTemp;
            return float.NaN;
        }

        /// <summary>SG bottom node temperature (Â°F) â€” multi-node model.</summary>
        public float GetSGBottomNodeTemp()
        {
            if (heatupEngine != null) return heatupEngine.sgBottomNodeTemp;
            return float.NaN;
        }

        /// <summary>SG stratification delta-T (Â°F).</summary>
        public float GetSGStratificationDeltaT()
        {
            if (heatupEngine != null) return heatupEngine.sgStratificationDeltaT;
            return float.NaN;
        }

        /// <summary>SG natural circulation fraction (0â€“1).</summary>
        public float GetSGCirculationFraction()
        {
            if (heatupEngine != null) return heatupEngine.sgCirculationFraction;
            return float.NaN;
        }

        /// <summary>Is SG secondary steaming?</summary>
        public bool GetSGSteaming()
        {
            if (heatupEngine != null) return heatupEngine.sgSteaming;
            return false;
        }

        /// <summary>SG secondary steam pressure (psig).</summary>
        public float GetSteamPressure()
        {
            if (heatupEngine != null) return heatupEngine.sgSecondaryPressure_psig;
            return float.NaN;
        }

        /// <summary>
        /// SG level for a specific SG (0-based index).
        /// Currently all 4 SGs use the same lumped model, so index is ignored.
        /// Returns percentage.
        /// </summary>
        public float GetSGLevel(int sgIndex)
        {
            // PLACEHOLDER: Lumped SG model â€” no per-SG level instrumentation yet.
            // All SGs report the same value.
            // Future: Replace with individual SG level tracking.
            return float.NaN;
        }

        // ----------------------------------------------------------------
        // v4.3.0: SG Secondary Pressure Model
        // ----------------------------------------------------------------

        /// <summary>SG secondary pressure (psia).</summary>
        public float GetSGSecondaryPressure_psia()
        {
            if (heatupEngine != null) return heatupEngine.sgSecondaryPressure_psia;
            return float.NaN;
        }

        /// <summary>SG secondary pressure (psig).</summary>
        public float GetSGSecondaryPressure_psig()
        {
            if (heatupEngine != null) return heatupEngine.sgSecondaryPressure_psia - 14.7f;
            return float.NaN;
        }

        /// <summary>Saturation temperature at current SG secondary pressure (Â°F).</summary>
        public float GetSGSaturationTemp()
        {
            if (heatupEngine != null) return heatupEngine.sgSaturationTemp_F;
            return float.NaN;
        }

        /// <summary>Max node superheat above T_sat (Â°F).</summary>
        public float GetSGMaxSuperheat()
        {
            if (heatupEngine != null) return heatupEngine.sgMaxSuperheat_F;
            return float.NaN;
        }

        /// <summary>Peak boiling intensity fraction (0.0 = subcooled, 1.0 = full boiling).</summary>
        public float GetSGBoilingIntensity()
        {
            if (heatupEngine != null) return heatupEngine.sgBoilingIntensity;
            return float.NaN;
        }

        /// <summary>Nitrogen blanket isolation status.</summary>
        public bool GetSGNitrogenIsolated()
        {
            if (heatupEngine != null) return heatupEngine.sgNitrogenIsolated;
            return false;
        }

        /// <summary>SG thermocline height from tubesheet (ft).</summary>
        public float GetSGThermoclineHeight()
        {
            if (heatupEngine != null) return heatupEngine.sgThermoclineHeight;
            return float.NaN;
        }

        /// <summary>SG active tube area fraction (above thermocline).</summary>
        public float GetSGActiveAreaFraction()
        {
            if (heatupEngine != null) return heatupEngine.sgActiveAreaFraction;
            return float.NaN;
        }

        /// <summary>Any SG node boiling (bool).</summary>
        public bool GetSGBoilingActive()
        {
            if (heatupEngine != null) return heatupEngine.sgBoilingActive;
            return false;
        }

        #endregion

        // ====================================================================
        // RHR SYSTEM PARAMETERS (v4.3.0 â€” getters for v3.0.0 physics)
        // ====================================================================

        #region RHR Parameters

        /// <summary>RHR operating mode as display string.</summary>
        public string GetRHRMode()
        {
            if (TryGetPlantStateSnapshot(out PlantState plantState))
                return string.IsNullOrWhiteSpace(plantState.RhrMode) ? "---" : plantState.RhrMode;
            return "---";
        }

        /// <summary>
        /// RHR mode as integer for color coding.
        /// 0 = Standby, 1 = Cooling, 2 = Heatup, 3 = Isolating, 4 = Secured.
        /// </summary>
        public int GetRHRModeEnum()
        {
            if (heatupEngine != null) return (int)heatupEngine.rhrState.Mode;
            return 0;
        }

        /// <summary>Net RHR thermal effect (MW). Positive = heating RCS.</summary>
        public float GetRHRNetHeat_MW()
        {
            if (TryGetPlantStateSnapshot(out PlantState plantState))
                return plantState.RhrNetHeatMw;
            return float.NaN;
        }

        /// <summary>RHR HX heat removal (MW).</summary>
        public float GetRHRHXRemoval_MW()
        {
            if (heatupEngine != null) return heatupEngine.rhrHXRemoval_MW;
            return float.NaN;
        }

        /// <summary>RHR pump heat input (MW).</summary>
        public float GetRHRPumpHeat_MW()
        {
            if (heatupEngine != null) return heatupEngine.rhrPumpHeat_MW;
            return float.NaN;
        }

        /// <summary>RHR isolation ramp progress (0.0â€“1.0). 1.0 = fully isolated.</summary>
        public float GetRHRIsolationProgress()
        {
            if (heatupEngine != null)
            {
                if (heatupEngine.rhrState.Mode == RHRMode.Standby) return 1f;
                if (heatupEngine.rhrState.Mode == RHRMode.Isolating)
                {
                    // Progress = 1 - (current flow / nominal flow)
                    float nominalFlow = PlantConstants.RHR_PUMP_FLOW_GPM_TOTAL;
                    return nominalFlow > 0 ? 1f - (heatupEngine.rhrState.FlowRate_gpm / nominalFlow) : 1f;
                }
                return 0f;  // Active modes: not isolating
            }
            return float.NaN;
        }

        /// <summary>Is RHR actively connected to RCS?</summary>
        public bool GetRHRActive()
        {
            if (heatupEngine != null) return heatupEngine.rhrActive;
            return false;
        }

        /// <summary>RHR suction pressure (psig). Approximated from RCS pressure.</summary>
        public float GetRHRSuctionPressure()
        {
            if (heatupEngine != null && heatupEngine.rhrActive)
                return heatupEngine.pressure - 14.7f;  // RCS pressure in psig
            return float.NaN;
        }

        /// <summary>RHR flow rate (gpm). Per HRTD: ~3000 gpm per train when active.</summary>
        public float GetRHRFlow()
        {
            if (heatupEngine != null && heatupEngine.rhrActive)
                return 3000f;  // Fixed: 2 trains at ~3000 gpm each, lumped model
            return 0f;
        }

        /// <summary>RHR HX inlet temperature (Â°F). Same as RCS temperature.</summary>
        public float GetRHRHXInletTemp()
        {
            if (heatupEngine != null && heatupEngine.rhrActive)
                return heatupEngine.T_rcs;
            return float.NaN;
        }

        /// <summary>RHR HX outlet temperature (Â°F). Estimated from removal and flow.</summary>
        public float GetRHRHXOutletTemp()
        {
            if (heatupEngine != null && heatupEngine.rhrActive)
            {
                // Q = m_dot Ã— cp Ã— Î”T  â†’  Î”T = Q / (m_dot Ã— cp)
                // At ~3000 gpm â‰ˆ 6.68 ftÂ³/s, Ï â‰ˆ 62 lb/ftÂ³ â†’ ~25,000 lb/min
                // Q_removal in BTU/hr, cp â‰ˆ 1.0
                float Q_BTUhr = heatupEngine.rhrHXRemoval_MW * 3.412e6f;
                float massFlowPerHr = 3000f * 60f * 8.33f;  // gpm â†’ lb/hr (water ~8.33 lb/gal)
                float deltaT = (massFlowPerHr > 0) ? Q_BTUhr / massFlowPerHr : 0f;
                return heatupEngine.T_rcs - deltaT;
            }
            return float.NaN;
        }

        #endregion

        // ====================================================================
        // STEAM DUMP PARAMETERS (v1.1.0)
        // ====================================================================

        #region Steam Dump Parameters

        /// <summary>Is steam dump system active?</summary>
        public bool GetSteamDumpActive()
        {
            if (heatupEngine != null) return heatupEngine.steamDumpActive;
            return false;
        }

        /// <summary>Steam dump heat removal (MW).</summary>
        public float GetSteamDumpHeat()
        {
            if (heatupEngine != null) return heatupEngine.steamDumpHeat_MW;
            return float.NaN;
        }

        /// <summary>Steam dump valve demand (0â€“1).</summary>
        public float GetSteamDumpDemand()
        {
            if (heatupEngine != null && heatupEngine.steamDumpState.IsActive)
                return heatupEngine.steamDumpState.DumpDemand;
            return float.NaN;
        }

        #endregion

        // ====================================================================
        // HZP STABILIZATION PARAMETERS (v1.1.0)
        // ====================================================================

        #region HZP Parameters

        /// <summary>Is HZP stable?</summary>
        public bool GetHZPStable()
        {
            if (heatupEngine != null) return heatupEngine.hzpStable;
            return false;
        }

        /// <summary>HZP stabilization progress (0â€“100%).</summary>
        public float GetHZPProgress()
        {
            if (heatupEngine != null) return heatupEngine.hzpProgress;
            return float.NaN;
        }

        /// <summary>Is heater PID controller active?</summary>
        public bool GetHeaterPIDActive()
        {
            if (heatupEngine != null) return heatupEngine.heaterPIDActive;
            return false;
        }

        #endregion

        // ====================================================================
        // RVLIS PARAMETERS
        // ====================================================================

        #region RVLIS Parameters

        /// <summary>RVLIS dynamic range reading (%).</summary>
        public float GetRVLISDynamic()
        {
            if (heatupEngine != null) return heatupEngine.rvlisDynamic;
            return float.NaN;
        }

        /// <summary>RVLIS full range reading (%).</summary>
        public float GetRVLISFull()
        {
            if (heatupEngine != null) return heatupEngine.rvlisFull;
            return float.NaN;
        }

        #endregion

        // ====================================================================
        // UTILITY â€” Placeholder Value Handling
        // ====================================================================

        #region Utility

        /// <summary>
        /// Format a value for display, showing "---" for NaN (no data).
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="format">C# numeric format string (e.g., "F1", "F0").</param>
        /// <param name="suffix">Unit suffix (e.g., "Â°F", "psia").</param>
        /// <returns>Formatted string or "---" if no data.</returns>
        public static string FormatOrPlaceholder(float value, string format = "F1", string suffix = "")
        {
            if (float.IsNaN(value)) return "---";
            return value.ToString(format) + suffix;
        }

        /// <summary>
        /// Check if a value represents valid data (not NaN).
        /// </summary>
        public static bool HasData(float value)
        {
            return !float.IsNaN(value);
        }

        #endregion
    }
}

