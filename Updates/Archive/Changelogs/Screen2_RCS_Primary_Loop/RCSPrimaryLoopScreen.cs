// ============================================================================
// CRITICAL: Master the Atom - RCS Primary Loop Operator Screen
// RCSPrimaryLoopScreen.cs - Screen 2: RCS Loops, RCPs, Flow, Temperatures
// ============================================================================
//
// PURPOSE:
//   Implements the RCS Primary Loop operator screen (Key 2) displaying:
//   - 4-loop RCS schematic with Blender-generated 3D visualization
//   - Loop temperatures (T-hot, T-cold per loop)
//   - RCS flow rates (total and per loop)
//   - RCP controls and status
//   - Flow direction animation
//
// LAYOUT:
//   - Center (15-65%): 3D RCS Loop visualization with animated flow
//   - Left (0-15%): Loop temperature gauges (8 gauges)
//   - Right (65-100%): Flow and power gauges (8 gauges)
//   - Bottom (74-100%): RCP controls, status, alarms
//
// KEYBOARD:
//   - Key 2: Toggle screen visibility
//
// SOURCES:
//   - Operator_Screen_Layout_Plan_v1_0_0.md (Screen 2 specification)
//   - NRC HRTD Section 3.2 - Reactor Coolant System
//   - RCS_Technical_Specifications.md
//
// VERSION: 1.0.0
// DATE: 2026-02-09
// CLASSIFICATION: UI — Operator Interface
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Critical.Physics;

namespace Critical.UI
{
    /// <summary>
    /// RCS Primary Loop Operator Screen (Key 2).
    /// Displays 4-loop RCS visualization with temperatures, flows, and RCP controls.
    /// </summary>
    public class RCSPrimaryLoopScreen : OperatorScreen
    {
        // ====================================================================
        // CONSTANTS
        // ====================================================================
        
        /// <summary>Screen toggle key</summary>
        public override KeyCode ToggleKey => KeyCode.Alpha2;
        
        /// <summary>Screen identifier</summary>
        public override string ScreenName => "RCS Primary Loop";
        
        /// <summary>Screen index for manager</summary>
        public override int ScreenIndex => 2;
        
        // Layout percentages (1920x1080 reference)
        private const float LEFT_PANEL_WIDTH_PCT = 0.15f;
        private const float CENTER_WIDTH_PCT = 0.50f;
        private const float RIGHT_PANEL_WIDTH_PCT = 0.35f;
        private const float BOTTOM_PANEL_HEIGHT_PCT = 0.26f;
        
        // ====================================================================
        // SERIALIZED FIELDS - Inspector Configuration
        // ====================================================================
        
        [Header("=== PANEL REFERENCES ===")]
        [SerializeField] private RectTransform leftPanel;
        [SerializeField] private RectTransform centerPanel;
        [SerializeField] private RectTransform rightPanel;
        [SerializeField] private RectTransform bottomPanel;
        
        [Header("=== 3D VISUALIZATION ===")]
        [SerializeField] private GameObject rcsVisualPrefab;
        [SerializeField] private Camera visualizationCamera;
        [SerializeField] private RenderTexture visualizationRT;
        [SerializeField] private RawImage visualizationDisplay;
        [SerializeField] private float cameraOrbitSpeed = 5f;
        [SerializeField] private bool autoRotateCamera = false;
        
        [Header("=== LEFT PANEL - TEMPERATURE GAUGES ===")]
        [SerializeField] private MosaicGauge loop1THotGauge;
        [SerializeField] private MosaicGauge loop2THotGauge;
        [SerializeField] private MosaicGauge loop3THotGauge;
        [SerializeField] private MosaicGauge loop4THotGauge;
        [SerializeField] private MosaicGauge loop1TColdGauge;
        [SerializeField] private MosaicGauge loop2TColdGauge;
        [SerializeField] private MosaicGauge loop3TColdGauge;
        [SerializeField] private MosaicGauge loop4TColdGauge;
        
        [Header("=== RIGHT PANEL - FLOW GAUGES ===")]
        [SerializeField] private MosaicGauge totalFlowGauge;
        [SerializeField] private MosaicGauge loop1FlowGauge;
        [SerializeField] private MosaicGauge loop2FlowGauge;
        [SerializeField] private MosaicGauge loop3FlowGauge;
        [SerializeField] private MosaicGauge loop4FlowGauge;
        [SerializeField] private MosaicGauge corePowerGauge;
        [SerializeField] private MosaicGauge coreDeltaTGauge;
        [SerializeField] private MosaicGauge avgTAvgGauge;
        
        [Header("=== BOTTOM PANEL - RCP CONTROLS ===")]
        [SerializeField] private RCPControlPanel rcp1Panel;
        [SerializeField] private RCPControlPanel rcp2Panel;
        [SerializeField] private RCPControlPanel rcp3Panel;
        [SerializeField] private RCPControlPanel rcp4Panel;
        
        [Header("=== STATUS DISPLAYS ===")]
        [SerializeField] private TextMeshProUGUI rcpCountDisplay;
        [SerializeField] private TextMeshProUGUI naturalCircStatusDisplay;
        [SerializeField] private Image naturalCircIndicator;
        [SerializeField] private TextMeshProUGUI modeDisplay;
        
        [Header("=== ALARM PANEL ===")]
        [SerializeField] private Transform alarmListContainer;
        [SerializeField] private GameObject alarmEntryPrefab;
        [SerializeField] private int maxVisibleAlarms = 5;
        
        [Header("=== COLORS ===")]
        [SerializeField] private Color runningColor = new Color(0f, 1f, 0f, 1f);
        [SerializeField] private Color stoppedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color trippedColor = new Color(1f, 0f, 0f, 1f);
        [SerializeField] private Color rampingColor = new Color(1f, 1f, 0f, 1f);
        [SerializeField] private Color hotColor = new Color(1f, 0.27f, 0f, 1f);
        [SerializeField] private Color coldColor = new Color(0.12f, 0.56f, 1f, 1f);
        
        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================
        
        // References
        private HeatupSimEngine simEngine;
        private MosaicBoard mosaicBoard;
        private GameObject rcsVisualInstance;
        private RCSVisualController visualController;
        
        // Gauge arrays for easier iteration
        private MosaicGauge[] tHotGauges;
        private MosaicGauge[] tColdGauges;
        private MosaicGauge[] flowGauges;
        private RCPControlPanel[] rcpPanels;
        
        // State tracking
        private bool[] rcpRunningStates = new bool[4];
        private float[] rcpFlowFractions = new float[4];
        private float cameraAngle = 0f;
        
        // Alarm management
        private List<GameObject> activeAlarmEntries = new List<GameObject>();
        private Queue<string> pendingAlarms = new Queue<string>();
        
        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================
        
        protected override void Awake()
        {
            base.Awake();
            
            // Build gauge arrays
            tHotGauges = new MosaicGauge[] { loop1THotGauge, loop2THotGauge, loop3THotGauge, loop4THotGauge };
            tColdGauges = new MosaicGauge[] { loop1TColdGauge, loop2TColdGauge, loop3TColdGauge, loop4TColdGauge };
            flowGauges = new MosaicGauge[] { loop1FlowGauge, loop2FlowGauge, loop3FlowGauge, loop4FlowGauge };
            rcpPanels = new RCPControlPanel[] { rcp1Panel, rcp2Panel, rcp3Panel, rcp4Panel };
            
            // Find simulation references
            simEngine = FindObjectOfType<HeatupSimEngine>();
            mosaicBoard = FindObjectOfType<MosaicBoard>();
        }
        
        protected override void Start()
        {
            base.Start();
            
            InitializeVisualization();
            InitializeGauges();
            InitializeRCPControls();
            
            // Register with screen manager
            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.RegisterScreen(this);
            }
        }
        
        protected override void Update()
        {
            base.Update();
            
            if (IsVisible)
            {
                UpdateGauges();
                UpdateRCPStatus();
                UpdateVisualization();
                UpdateStatusDisplays();
                ProcessPendingAlarms();
                
                if (autoRotateCamera)
                {
                    UpdateCameraOrbit();
                }
            }
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            // Cleanup
            if (rcsVisualInstance != null)
            {
                Destroy(rcsVisualInstance);
            }
            
            if (visualizationRT != null)
            {
                visualizationRT.Release();
            }
        }
        
        // ====================================================================
        // INITIALIZATION
        // ====================================================================
        
        /// <summary>
        /// Initialize the 3D RCS visualization system.
        /// </summary>
        private void InitializeVisualization()
        {
            // Create render texture if not assigned
            if (visualizationRT == null)
            {
                visualizationRT = new RenderTexture(960, 720, 24, RenderTextureFormat.ARGB32);
                visualizationRT.antiAliasing = 4;
                visualizationRT.Create();
            }
            
            // Setup visualization camera
            if (visualizationCamera == null)
            {
                // Create camera if not assigned
                GameObject camObj = new GameObject("RCS_Visualization_Camera");
                visualizationCamera = camObj.AddComponent<Camera>();
                visualizationCamera.clearFlags = CameraClearFlags.SolidColor;
                visualizationCamera.backgroundColor = new Color(0.1f, 0.1f, 0.12f, 1f);
                visualizationCamera.cullingMask = LayerMask.GetMask("RCSVisualization");
            }
            
            visualizationCamera.targetTexture = visualizationRT;
            
            // Instantiate the 3D model
            if (rcsVisualPrefab != null)
            {
                // Position far from main scene to avoid interference
                Vector3 visualPos = new Vector3(1000f, 0f, 0f);
                rcsVisualInstance = Instantiate(rcsVisualPrefab, visualPos, Quaternion.identity);
                rcsVisualInstance.name = "RCS_Visual_Instance";
                
                // Set layer for culling
                SetLayerRecursive(rcsVisualInstance, LayerMask.NameToLayer("RCSVisualization"));
                
                // Get or add controller
                visualController = rcsVisualInstance.GetComponent<RCSVisualController>();
                if (visualController == null)
                {
                    visualController = rcsVisualInstance.AddComponent<RCSVisualController>();
                }
                
                // Position camera to view the model
                visualizationCamera.transform.position = visualPos + new Vector3(0f, 50f, -100f);
                visualizationCamera.transform.LookAt(visualPos + new Vector3(0f, 20f, 0f));
            }
            
            // Assign render texture to display
            if (visualizationDisplay != null)
            {
                visualizationDisplay.texture = visualizationRT;
            }
        }
        
        /// <summary>
        /// Set layer recursively for all children.
        /// </summary>
        private void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
        
        /// <summary>
        /// Initialize all gauges with proper ranges and labels.
        /// </summary>
        private void InitializeGauges()
        {
            // Temperature gauges: 100-700°F for full heatup range
            string[] loopNames = { "Loop 1", "Loop 2", "Loop 3", "Loop 4" };
            
            for (int i = 0; i < 4; i++)
            {
                if (tHotGauges[i] != null)
                {
                    tHotGauges[i].Initialize(
                        label: $"{loopNames[i]} T-hot",
                        units: "°F",
                        minValue: 100f,
                        maxValue: 700f,
                        warningLow: 0f,
                        warningHigh: 620f,
                        dangerLow: 0f,
                        dangerHigh: 650f
                    );
                }
                
                if (tColdGauges[i] != null)
                {
                    tColdGauges[i].Initialize(
                        label: $"{loopNames[i]} T-cold",
                        units: "°F",
                        minValue: 100f,
                        maxValue: 700f,
                        warningLow: 0f,
                        warningHigh: 560f,
                        dangerLow: 0f,
                        dangerHigh: 580f
                    );
                }
                
                if (flowGauges[i] != null)
                {
                    flowGauges[i].Initialize(
                        label: $"{loopNames[i]} Flow",
                        units: "gpm",
                        minValue: 0f,
                        maxValue: 100000f,
                        warningLow: 70000f,
                        warningHigh: 0f,
                        dangerLow: 60000f,
                        dangerHigh: 0f
                    );
                }
            }
            
            // Total flow gauge
            if (totalFlowGauge != null)
            {
                totalFlowGauge.Initialize(
                    label: "Total RCS Flow",
                    units: "gpm",
                    minValue: 0f,
                    maxValue: 400000f,
                    warningLow: 280000f,
                    warningHigh: 0f,
                    dangerLow: 240000f,
                    dangerHigh: 0f
                );
            }
            
            // Core thermal power gauge
            if (corePowerGauge != null)
            {
                corePowerGauge.Initialize(
                    label: "Core Thermal Power",
                    units: "MWt",
                    minValue: 0f,
                    maxValue: 50f,  // During heatup, power is from RCPs (~21 MW)
                    warningLow: 0f,
                    warningHigh: 30f,
                    dangerLow: 0f,
                    dangerHigh: 40f
                );
            }
            
            // Core ΔT gauge
            if (coreDeltaTGauge != null)
            {
                coreDeltaTGauge.Initialize(
                    label: "Core ΔT",
                    units: "°F",
                    minValue: 0f,
                    maxValue: 80f,
                    warningLow: 0f,
                    warningHigh: 65f,
                    dangerLow: 0f,
                    dangerHigh: 70f
                );
            }
            
            // Average T-avg gauge
            if (avgTAvgGauge != null)
            {
                avgTAvgGauge.Initialize(
                    label: "Average T-avg",
                    units: "°F",
                    minValue: 100f,
                    maxValue: 620f,
                    warningLow: 0f,
                    warningHigh: 588f,
                    dangerLow: 0f,
                    dangerHigh: 600f
                );
            }
        }
        
        /// <summary>
        /// Initialize RCP control panels with callbacks.
        /// </summary>
        private void InitializeRCPControls()
        {
            for (int i = 0; i < 4; i++)
            {
                if (rcpPanels[i] != null)
                {
                    int pumpIndex = i; // Capture for closure
                    rcpPanels[i].Initialize(
                        pumpNumber: i + 1,
                        onStartClicked: () => OnRCPStartRequested(pumpIndex),
                        onStopClicked: () => OnRCPStopRequested(pumpIndex)
                    );
                }
            }
        }
        
        // ====================================================================
        // UPDATE METHODS
        // ====================================================================
        
        /// <summary>
        /// Update all gauge values from simulation.
        /// </summary>
        private void UpdateGauges()
        {
            if (simEngine == null) return;
            
            // Get current values
            float tHot = simEngine.T_hot;
            float tCold = simEngine.T_cold;
            float tAvg = simEngine.T_avg;
            float deltaT = tHot - tCold;
            int rcpCount = simEngine.rcpCount;
            float rcpHeat = simEngine.rcpHeat;
            
            // Per RCP rated flow
            const float FLOW_PER_RCP = 88500f; // gpm at rated conditions
            
            // Calculate total flow
            float totalFlow = 0f;
            
            for (int i = 0; i < 4; i++)
            {
                // In lumped model, all running loops have same temperature
                if (tHotGauges[i] != null)
                {
                    tHotGauges[i].SetValue(tHot);
                }
                
                if (tColdGauges[i] != null)
                {
                    tColdGauges[i].SetValue(tCold);
                }
                
                // Flow depends on RCP state
                float loopFlow = 0f;
                if (i < rcpCount && rcpFlowFractions[i] > 0.01f)
                {
                    loopFlow = FLOW_PER_RCP * rcpFlowFractions[i];
                }
                totalFlow += loopFlow;
                
                if (flowGauges[i] != null)
                {
                    flowGauges[i].SetValue(loopFlow);
                }
            }
            
            // Update totals
            if (totalFlowGauge != null)
            {
                totalFlowGauge.SetValue(totalFlow);
            }
            
            if (corePowerGauge != null)
            {
                corePowerGauge.SetValue(rcpHeat);
            }
            
            if (coreDeltaTGauge != null)
            {
                coreDeltaTGauge.SetValue(Mathf.Abs(deltaT));
            }
            
            if (avgTAvgGauge != null)
            {
                avgTAvgGauge.SetValue(tAvg);
            }
        }
        
        /// <summary>
        /// Update RCP panel status indicators.
        /// </summary>
        private void UpdateRCPStatus()
        {
            if (simEngine == null) return;
            
            int rcpCount = simEngine.rcpCount;
            float simTime = simEngine.simTime;
            
            for (int i = 0; i < 4; i++)
            {
                bool isRunning = (i < rcpCount);
                
                // Get detailed pump state
                RCPSequencer.PumpRampState pumpState = default;
                if (isRunning && simEngine.rcpStartTimes != null && i < simEngine.rcpStartTimes.Length)
                {
                    pumpState = RCPSequencer.UpdatePumpRampState(i, simEngine.rcpStartTimes[i], simTime);
                    rcpFlowFractions[i] = pumpState.FlowFraction;
                }
                else
                {
                    rcpFlowFractions[i] = 0f;
                }
                
                // Determine state
                RCPState state;
                float speed = 0f;
                
                if (!isRunning)
                {
                    state = RCPState.Stopped;
                }
                else if (rcpFlowFractions[i] < 0.99f)
                {
                    state = RCPState.Ramping;
                    speed = rcpFlowFractions[i] * 1200f;
                }
                else
                {
                    state = RCPState.Running;
                    speed = 1200f;
                }
                
                // Update panel
                if (rcpPanels[i] != null)
                {
                    rcpPanels[i].UpdateStatus(
                        state: state,
                        speed: speed,
                        flowFraction: rcpFlowFractions[i],
                        canStart: !isRunning && CanStartRCP(i),
                        canStop: isRunning
                    );
                }
                
                rcpRunningStates[i] = isRunning;
            }
        }
        
        /// <summary>
        /// Update 3D visualization based on current state.
        /// </summary>
        private void UpdateVisualization()
        {
            if (visualController == null) return;
            
            // Update temperature colors on piping
            float tHot = simEngine != null ? simEngine.T_hot : 100f;
            float tCold = simEngine != null ? simEngine.T_cold : 100f;
            
            visualController.UpdateTemperatures(tHot, tCold);
            
            // Update RCP indicators
            for (int i = 0; i < 4; i++)
            {
                visualController.SetRCPState(i, rcpRunningStates[i], rcpFlowFractions[i]);
            }
            
            // Update flow animation speed
            int rcpCount = simEngine != null ? simEngine.rcpCount : 0;
            float avgFlowFraction = 0f;
            for (int i = 0; i < rcpCount; i++)
            {
                avgFlowFraction += rcpFlowFractions[i];
            }
            if (rcpCount > 0) avgFlowFraction /= rcpCount;
            
            visualController.SetFlowAnimationSpeed(avgFlowFraction);
        }
        
        /// <summary>
        /// Update status display texts.
        /// </summary>
        private void UpdateStatusDisplays()
        {
            if (simEngine == null) return;
            
            int rcpCount = simEngine.rcpCount;
            float tAvg = simEngine.T_avg;
            
            // RCP count
            if (rcpCountDisplay != null)
            {
                rcpCountDisplay.text = $"RCPs Running: {rcpCount}/4";
            }
            
            // Natural circulation status
            bool isNatCirc = (rcpCount == 0 && tAvg > 350f);
            if (naturalCircStatusDisplay != null)
            {
                naturalCircStatusDisplay.text = isNatCirc ? "NATURAL CIRC ACTIVE" : "FORCED CIRCULATION";
            }
            if (naturalCircIndicator != null)
            {
                naturalCircIndicator.color = isNatCirc ? rampingColor : stoppedColor;
            }
            
            // Operating mode
            if (modeDisplay != null)
            {
                int mode = PlantConstants.GetPlantMode(tAvg);
                modeDisplay.text = $"MODE {mode}";
            }
        }
        
        /// <summary>
        /// Rotate camera around the model if auto-rotate is enabled.
        /// </summary>
        private void UpdateCameraOrbit()
        {
            if (visualizationCamera == null || rcsVisualInstance == null) return;
            
            cameraAngle += cameraOrbitSpeed * Time.deltaTime;
            if (cameraAngle >= 360f) cameraAngle -= 360f;
            
            Vector3 center = rcsVisualInstance.transform.position + new Vector3(0f, 20f, 0f);
            float radius = 120f;
            float height = 50f;
            
            float rad = cameraAngle * Mathf.Deg2Rad;
            Vector3 camPos = center + new Vector3(
                Mathf.Sin(rad) * radius,
                height,
                Mathf.Cos(rad) * radius
            );
            
            visualizationCamera.transform.position = camPos;
            visualizationCamera.transform.LookAt(center);
        }
        
        // ====================================================================
        // RCP CONTROL METHODS
        // ====================================================================
        
        /// <summary>
        /// Check if RCP can be started (interlocks check).
        /// </summary>
        private bool CanStartRCP(int pumpIndex)
        {
            if (simEngine == null) return false;
            
            // Minimum pressure interlock
            if (simEngine.pressure < PlantConstants.MIN_RCP_PRESSURE_PSIA)
            {
                return false;
            }
            
            // Minimum subcooling interlock
            if (simEngine.subcooling < 20f)
            {
                return false;
            }
            
            // Can only start next sequential pump
            if (pumpIndex > simEngine.rcpCount)
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Handle RCP start request.
        /// </summary>
        private void OnRCPStartRequested(int pumpIndex)
        {
            if (!CanStartRCP(pumpIndex))
            {
                string reason = GetStartBlockReason(pumpIndex);
                AddAlarm($"RCP-{pumpIndex + 1} START BLOCKED: {reason}");
                return;
            }
            
            // In current implementation, simulation handles RCP starts automatically
            // This would request a manual start in a more complete implementation
            Debug.Log($"[RCSScreen] RCP-{pumpIndex + 1} start requested");
            AddAlarm($"RCP-{pumpIndex + 1} START REQUESTED");
        }
        
        /// <summary>
        /// Handle RCP stop request.
        /// </summary>
        private void OnRCPStopRequested(int pumpIndex)
        {
            Debug.Log($"[RCSScreen] RCP-{pumpIndex + 1} stop requested");
            AddAlarm($"RCP-{pumpIndex + 1} STOP REQUESTED");
            
            // In a complete implementation, this would trigger RCP trip
        }
        
        /// <summary>
        /// Get reason why RCP cannot be started.
        /// </summary>
        private string GetStartBlockReason(int pumpIndex)
        {
            if (simEngine == null) return "SIM UNAVAILABLE";
            
            if (simEngine.pressure < PlantConstants.MIN_RCP_PRESSURE_PSIA)
                return $"LOW PRESSURE ({simEngine.pressure:F0} < {PlantConstants.MIN_RCP_PRESSURE_PSIA} PSIA)";
            
            if (simEngine.subcooling < 20f)
                return $"LOW SUBCOOLING ({simEngine.subcooling:F1} < 20°F)";
            
            if (pumpIndex > simEngine.rcpCount)
                return "OUT OF SEQUENCE";
            
            return "UNKNOWN";
        }
        
        // ====================================================================
        // ALARM MANAGEMENT
        // ====================================================================
        
        /// <summary>
        /// Add an alarm to the display queue.
        /// </summary>
        public void AddAlarm(string message)
        {
            pendingAlarms.Enqueue($"[{GetTimeString()}] {message}");
        }
        
        /// <summary>
        /// Process pending alarms and update display.
        /// </summary>
        private void ProcessPendingAlarms()
        {
            while (pendingAlarms.Count > 0)
            {
                string alarm = pendingAlarms.Dequeue();
                CreateAlarmEntry(alarm);
            }
            
            // Trim old alarms
            while (activeAlarmEntries.Count > maxVisibleAlarms)
            {
                GameObject oldest = activeAlarmEntries[0];
                activeAlarmEntries.RemoveAt(0);
                Destroy(oldest);
            }
        }
        
        /// <summary>
        /// Create a visual alarm entry.
        /// </summary>
        private void CreateAlarmEntry(string message)
        {
            if (alarmListContainer == null || alarmEntryPrefab == null) return;
            
            GameObject entry = Instantiate(alarmEntryPrefab, alarmListContainer);
            TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = message;
            }
            
            activeAlarmEntries.Add(entry);
        }
        
        /// <summary>
        /// Get formatted time string from simulation.
        /// </summary>
        private string GetTimeString()
        {
            if (simEngine == null) return "00:00:00";
            
            float simTime = simEngine.simTime;
            int hours = (int)simTime;
            int minutes = (int)((simTime - hours) * 60f);
            int seconds = (int)((simTime - hours - minutes / 60f) * 3600f) % 60;
            
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }
        
        // ====================================================================
        // PUBLIC API
        // ====================================================================
        
        /// <summary>
        /// Get the current RCP running states.
        /// </summary>
        public bool[] GetRCPStates()
        {
            return (bool[])rcpRunningStates.Clone();
        }
        
        /// <summary>
        /// Get the current RCP flow fractions.
        /// </summary>
        public float[] GetRCPFlowFractions()
        {
            return (float[])rcpFlowFractions.Clone();
        }
        
        /// <summary>
        /// Enable or disable camera auto-rotation.
        /// </summary>
        public void SetCameraAutoRotate(bool enabled)
        {
            autoRotateCamera = enabled;
        }
    }
    
    // ========================================================================
    // SUPPORTING TYPES
    // ========================================================================
    
    /// <summary>
    /// RCP operational states.
    /// </summary>
    public enum RCPState
    {
        Stopped,
        Ramping,
        Running,
        Tripped
    }
    
    /// <summary>
    /// Individual RCP control panel component.
    /// Attach to a UI panel with start/stop buttons and status indicators.
    /// </summary>
    [System.Serializable]
    public class RCPControlPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI pumpNameLabel;
        [SerializeField] private TextMeshProUGUI speedDisplay;
        [SerializeField] private TextMeshProUGUI statusDisplay;
        [SerializeField] private Image statusIndicator;
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Slider flowBar;
        
        [Header("Colors")]
        [SerializeField] private Color runningColor = Color.green;
        [SerializeField] private Color stoppedColor = Color.gray;
        [SerializeField] private Color rampingColor = Color.yellow;
        [SerializeField] private Color trippedColor = Color.red;
        
        private System.Action onStartCallback;
        private System.Action onStopCallback;
        
        /// <summary>
        /// Initialize the control panel.
        /// </summary>
        public void Initialize(int pumpNumber, System.Action onStartClicked, System.Action onStopClicked)
        {
            if (pumpNameLabel != null)
            {
                pumpNameLabel.text = $"RCP-{pumpNumber}";
            }
            
            onStartCallback = onStartClicked;
            onStopCallback = onStopClicked;
            
            if (startButton != null)
            {
                startButton.onClick.AddListener(() => onStartCallback?.Invoke());
            }
            
            if (stopButton != null)
            {
                stopButton.onClick.AddListener(() => onStopCallback?.Invoke());
            }
        }
        
        /// <summary>
        /// Update the panel display.
        /// </summary>
        public void UpdateStatus(RCPState state, float speed, float flowFraction, bool canStart, bool canStop)
        {
            // Speed display
            if (speedDisplay != null)
            {
                speedDisplay.text = $"{speed:F0} rpm";
            }
            
            // Status text and color
            Color indicatorColor;
            string statusText;
            
            switch (state)
            {
                case RCPState.Running:
                    indicatorColor = runningColor;
                    statusText = "RUNNING";
                    break;
                case RCPState.Ramping:
                    indicatorColor = rampingColor;
                    statusText = "RAMPING";
                    break;
                case RCPState.Tripped:
                    indicatorColor = trippedColor;
                    statusText = "TRIPPED";
                    break;
                default:
                    indicatorColor = stoppedColor;
                    statusText = "STOPPED";
                    break;
            }
            
            if (statusIndicator != null)
            {
                statusIndicator.color = indicatorColor;
            }
            
            if (statusDisplay != null)
            {
                statusDisplay.text = statusText;
            }
            
            // Flow bar
            if (flowBar != null)
            {
                flowBar.value = flowFraction;
            }
            
            // Button states
            if (startButton != null)
            {
                startButton.interactable = canStart;
            }
            
            if (stopButton != null)
            {
                stopButton.interactable = canStop;
            }
        }
    }
    
    /// <summary>
    /// Controller for the 3D RCS visualization.
    /// Attach to the root of the Blender-imported RCS model.
    /// </summary>
    public class RCSVisualController : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private Transform reactorVessel;
        [SerializeField] private Transform[] steamGenerators = new Transform[4];
        [SerializeField] private Transform[] rcps = new Transform[4];
        [SerializeField] private Transform pressurizer;
        
        [Header("Piping References")]
        [SerializeField] private Renderer[] hotLegRenderers = new Renderer[4];
        [SerializeField] private Renderer[] coldLegRenderers = new Renderer[4];
        [SerializeField] private Renderer[] crossoverRenderers = new Renderer[4];
        
        [Header("RCP Indicators")]
        [SerializeField] private Transform[] rcpRotors = new Transform[4];
        [SerializeField] private Renderer[] rcpIndicators = new Renderer[4];
        
        [Header("Flow Arrows")]
        [SerializeField] private Transform[] flowArrows;
        [SerializeField] private float flowArrowSpeed = 5f;
        
        [Header("Temperature Colors")]
        [SerializeField] private Gradient temperatureGradient;
        [SerializeField] private float minTemp = 100f;
        [SerializeField] private float maxTemp = 650f;
        
        // Materials
        private Material[] hotLegMaterials;
        private Material[] coldLegMaterials;
        private Material[] rcpIndicatorMaterials;
        
        // Animation state
        private float[] rcpRotorAngles = new float[4];
        private float flowAnimationPhase = 0f;
        private float currentFlowSpeed = 0f;
        
        private void Awake()
        {
            // Cache materials for runtime modification
            CacheMaterials();
            
            // Setup default temperature gradient
            if (temperatureGradient == null)
            {
                temperatureGradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[4];
                colorKeys[0] = new GradientColorKey(new Color(0.12f, 0.56f, 1f), 0f);   // Cold blue
                colorKeys[1] = new GradientColorKey(new Color(0f, 0.8f, 0.8f), 0.33f);  // Cyan
                colorKeys[2] = new GradientColorKey(new Color(1f, 0.8f, 0f), 0.66f);    // Yellow
                colorKeys[3] = new GradientColorKey(new Color(1f, 0.27f, 0f), 1f);      // Hot orange
                temperatureGradient.colorKeys = colorKeys;
            }
        }
        
        private void Update()
        {
            AnimateRCPRotors();
            AnimateFlowArrows();
        }
        
        private void CacheMaterials()
        {
            hotLegMaterials = new Material[4];
            coldLegMaterials = new Material[4];
            rcpIndicatorMaterials = new Material[4];
            
            for (int i = 0; i < 4; i++)
            {
                if (hotLegRenderers[i] != null)
                {
                    hotLegMaterials[i] = hotLegRenderers[i].material;
                }
                if (coldLegRenderers[i] != null)
                {
                    coldLegMaterials[i] = coldLegRenderers[i].material;
                }
                if (rcpIndicators[i] != null)
                {
                    rcpIndicatorMaterials[i] = rcpIndicators[i].material;
                }
            }
        }
        
        /// <summary>
        /// Update piping colors based on temperatures.
        /// </summary>
        public void UpdateTemperatures(float tHot, float tCold)
        {
            Color hotColor = temperatureGradient.Evaluate((tHot - minTemp) / (maxTemp - minTemp));
            Color coldColor = temperatureGradient.Evaluate((tCold - minTemp) / (maxTemp - minTemp));
            
            for (int i = 0; i < 4; i++)
            {
                if (hotLegMaterials[i] != null)
                {
                    hotLegMaterials[i].color = hotColor;
                    hotLegMaterials[i].SetColor("_EmissionColor", hotColor * 0.3f);
                }
                
                if (coldLegMaterials[i] != null)
                {
                    coldLegMaterials[i].color = coldColor;
                    coldLegMaterials[i].SetColor("_EmissionColor", coldColor * 0.2f);
                }
            }
        }
        
        /// <summary>
        /// Set RCP state for visual indication.
        /// </summary>
        public void SetRCPState(int index, bool running, float flowFraction)
        {
            if (index < 0 || index >= 4) return;
            
            // Update indicator color
            if (rcpIndicatorMaterials[index] != null)
            {
                Color color;
                if (!running)
                {
                    color = Color.gray;
                }
                else if (flowFraction < 0.99f)
                {
                    color = Color.yellow;
                }
                else
                {
                    color = Color.green;
                }
                
                rcpIndicatorMaterials[index].color = color;
                rcpIndicatorMaterials[index].SetColor("_EmissionColor", color * 2f);
            }
        }
        
        /// <summary>
        /// Set overall flow animation speed.
        /// </summary>
        public void SetFlowAnimationSpeed(float normalizedSpeed)
        {
            currentFlowSpeed = normalizedSpeed;
        }
        
        private void AnimateRCPRotors()
        {
            for (int i = 0; i < 4; i++)
            {
                if (rcpRotors[i] != null)
                {
                    // Rotate based on whether pump is running
                    // In a full implementation, track individual pump states
                    rcpRotorAngles[i] += 360f * Time.deltaTime * currentFlowSpeed;
                    rcpRotors[i].localRotation = Quaternion.Euler(0f, rcpRotorAngles[i], 0f);
                }
            }
        }
        
        private void AnimateFlowArrows()
        {
            if (flowArrows == null || flowArrows.Length == 0) return;
            
            flowAnimationPhase += flowArrowSpeed * currentFlowSpeed * Time.deltaTime;
            if (flowAnimationPhase > 1f) flowAnimationPhase -= 1f;
            
            // Pulse scale of arrows based on flow
            float scale = 1f + Mathf.Sin(flowAnimationPhase * Mathf.PI * 2f) * 0.2f * currentFlowSpeed;
            
            foreach (var arrow in flowArrows)
            {
                if (arrow != null)
                {
                    arrow.localScale = Vector3.one * scale;
                }
            }
        }
    }
}
