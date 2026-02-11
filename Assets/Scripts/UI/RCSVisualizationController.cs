// ============================================================================
// CRITICAL: Master the Atom - RCS Visualization Controller
// RCSVisualizationController.cs - 3D Model Animation and Color Control
// ============================================================================
//
// PURPOSE:
//   Controls the visual appearance and animation of the imported Blender
//   RCS 3D model. Handles:
//   - Temperature-based color coding of piping
//   - RCP rotor animation
//   - Flow arrow animation
//   - Status indicator lights
//
// USAGE:
//   1. Import Blender RCS model into Unity
//   2. Add this component to the root of the model
//   3. Assign material and transform references in Inspector
//   4. RCSPrimaryLoopScreen will control this component at runtime
//
// MATERIAL NAMING CONVENTION:
//   The script looks for materials with these names (from Blender export):
//   - MAT_HotLeg      (or variations: MAT_HotLeg_1, HotLeg, etc.)
//   - MAT_ColdLeg
//   - MAT_CrossoverLeg
//   - MAT_RCP
//   - MAT_ReactorVessel
//   - MAT_SteamGenerator
//   - MAT_Pressurizer
//   - MAT_FlowArrow
//
// HIERARCHY EXPECTATIONS:
//   The model should have named children for animation targets:
//   - RCP_1_Rotor, RCP_2_Rotor, etc. (for rotor animation)
//   - FlowArrow_* (for flow animation)
//   - StatusLight_RCP_1, etc. (for indicator lights)
//
// VERSION: 1.0.0
// DATE: 2026-02-09
// CLASSIFICATION: UI — 3D Visualization
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace Critical.UI
{
    /// <summary>
    /// Controls the visual appearance and animation of the RCS 3D model.
    /// Attach to the root of the imported Blender model.
    /// </summary>
    public class RCSVisualizationController : MonoBehaviour
    {
        // ====================================================================
        // SERIALIZED FIELDS
        // ====================================================================

        #region Inspector Fields - Temperature Colors

        [Header("=== TEMPERATURE COLOR GRADIENT ===")]
        [Tooltip("Color gradient for temperature visualization")]
        [SerializeField] private Gradient temperatureGradient;

        [Tooltip("Minimum temperature for gradient (°F)")]
        [SerializeField] private float minTemperature = 100f;

        [Tooltip("Maximum temperature for gradient (°F)")]
        [SerializeField] private float maxTemperature = 650f;

        [Tooltip("Enable emission on hot piping")]
        [SerializeField] private bool enableEmission = true;

        [Tooltip("Emission intensity multiplier")]
        [SerializeField] private float emissionIntensity = 0.3f;

        #endregion

        #region Inspector Fields - RCP Animation

        [Header("=== RCP ANIMATION ===")]
        [Tooltip("RCP rotor transforms (assign in Inspector)")]
        [SerializeField] private Transform[] rcpRotors = new Transform[4];

        [Tooltip("Maximum RCP rotation speed (degrees/second)")]
        [SerializeField] private float maxRotorSpeed = 720f;

        [Tooltip("RCP status indicator renderers")]
        [SerializeField] private Renderer[] rcpIndicators = new Renderer[4];

        #endregion

        #region Inspector Fields - Flow Animation

        [Header("=== FLOW ANIMATION ===")]
        [Tooltip("Flow arrow transforms")]
        [SerializeField] private Transform[] flowArrows;

        [Tooltip("Flow arrow animation speed")]
        [SerializeField] private float flowArrowSpeed = 2f;

        [Tooltip("Flow arrow pulse amplitude")]
        [SerializeField] private float flowArrowPulseAmplitude = 0.2f;

        [Tooltip("Flow arrow movement distance")]
        [SerializeField] private float flowArrowMovement = 0.5f;

        #endregion

        #region Inspector Fields - Materials

        [Header("=== MATERIAL REFERENCES ===")]
        [Tooltip("Hot leg material(s) - will search by name if not assigned")]
        [SerializeField] private Material[] hotLegMaterials;

        [Tooltip("Cold leg material(s)")]
        [SerializeField] private Material[] coldLegMaterials;

        [Tooltip("Crossover leg material(s)")]
        [SerializeField] private Material[] crossoverMaterials;

        #endregion

        #region Inspector Fields - Status Colors

        [Header("=== STATUS COLORS ===")]
        [SerializeField] private Color color_Running = new Color(0.2f, 1f, 0.2f);
        [SerializeField] private Color color_Stopped = new Color(0.4f, 0.4f, 0.4f);
        [SerializeField] private Color color_Ramping = new Color(1f, 1f, 0.2f);
        [SerializeField] private Color color_Tripped = new Color(1f, 0.2f, 0.2f);

        #endregion

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        #region Private Fields

        // Material instances for runtime modification
        private List<Material> _hotLegMaterialInstances = new List<Material>();
        private List<Material> _coldLegMaterialInstances = new List<Material>();
        private List<Material> _crossoverMaterialInstances = new List<Material>();
        private Material[] _rcpIndicatorMaterials = new Material[4];

        // Animation state
        private float[] _rcpRotorAngles = new float[4];
        private float[] _rcpTargetSpeeds = new float[4];
        private float[] _rcpCurrentSpeeds = new float[4];
        private float _flowAnimationPhase = 0f;
        private float _currentFlowSpeed = 0f;
        private Vector3[] _flowArrowBasePositions;
        private Vector3[] _flowArrowBaseScales;

        // Current state
        private float _currentTHot = 400f;
        private float _currentTCold = 380f;
        private bool _isInitialized = false;

        #endregion

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        #region Unity Lifecycle

        private void Awake()
        {
            // Setup default temperature gradient if not configured
            if (temperatureGradient == null || temperatureGradient.colorKeys.Length == 0)
            {
                CreateDefaultTemperatureGradient();
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (!_isInitialized) return;

            AnimateRCPRotors();
            AnimateFlowArrows();
        }

        #endregion

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        #region Initialization

        /// <summary>
        /// Initialize the controller - find materials and setup animation.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Find and cache materials
            FindAndCacheMaterials();

            // Cache flow arrow positions
            CacheFlowArrowPositions();

            // Initialize RCP states
            for (int i = 0; i < 4; i++)
            {
                _rcpRotorAngles[i] = 0f;
                _rcpTargetSpeeds[i] = 0f;
                _rcpCurrentSpeeds[i] = 0f;
            }

            _isInitialized = true;
            Debug.Log("[RCSVisualizationController] Initialized.");
        }

        /// <summary>
        /// Create default temperature gradient.
        /// </summary>
        private void CreateDefaultTemperatureGradient()
        {
            temperatureGradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0] = new GradientColorKey(new Color(0.12f, 0.47f, 0.80f), 0.0f);   // Cold blue
            colorKeys[1] = new GradientColorKey(new Color(0.20f, 0.70f, 0.70f), 0.25f);  // Cyan
            colorKeys[2] = new GradientColorKey(new Color(0.40f, 0.80f, 0.40f), 0.50f);  // Green
            colorKeys[3] = new GradientColorKey(new Color(0.90f, 0.70f, 0.20f), 0.75f);  // Yellow/Orange
            colorKeys[4] = new GradientColorKey(new Color(0.90f, 0.30f, 0.10f), 1.0f);   // Hot red/orange

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            temperatureGradient.SetKeys(colorKeys, alphaKeys);
        }

        /// <summary>
        /// Find and cache material instances for runtime modification.
        /// </summary>
        private void FindAndCacheMaterials()
        {
            // Get all renderers in hierarchy
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in allRenderers)
            {
                foreach (var mat in renderer.materials)
                {
                    string matName = mat.name.ToLower();

                    // Categorize materials by name
                    if (matName.Contains("hotleg") || matName.Contains("hot_leg"))
                    {
                        // Create instance for hot leg
                        Material instance = new Material(mat);
                        _hotLegMaterialInstances.Add(instance);

                        // Replace on renderer
                        ReplaceMaterialOnRenderer(renderer, mat, instance);
                    }
                    else if (matName.Contains("coldleg") || matName.Contains("cold_leg"))
                    {
                        Material instance = new Material(mat);
                        _coldLegMaterialInstances.Add(instance);
                        ReplaceMaterialOnRenderer(renderer, mat, instance);
                    }
                    else if (matName.Contains("crossover") || matName.Contains("cross_over"))
                    {
                        Material instance = new Material(mat);
                        _crossoverMaterialInstances.Add(instance);
                        ReplaceMaterialOnRenderer(renderer, mat, instance);
                    }
                }
            }

            // Also add any materials assigned in Inspector
            if (hotLegMaterials != null)
            {
                foreach (var mat in hotLegMaterials)
                {
                    if (mat != null && !_hotLegMaterialInstances.Contains(mat))
                    {
                        _hotLegMaterialInstances.Add(mat);
                    }
                }
            }

            if (coldLegMaterials != null)
            {
                foreach (var mat in coldLegMaterials)
                {
                    if (mat != null && !_coldLegMaterialInstances.Contains(mat))
                    {
                        _coldLegMaterialInstances.Add(mat);
                    }
                }
            }

            // Cache RCP indicator materials
            for (int i = 0; i < 4; i++)
            {
                if (rcpIndicators[i] != null)
                {
                    _rcpIndicatorMaterials[i] = rcpIndicators[i].material;
                }
            }

            // Try to find RCP rotors if not assigned
            if (rcpRotors[0] == null)
            {
                FindRCPRotors();
            }

            // Try to find flow arrows if not assigned
            if (flowArrows == null || flowArrows.Length == 0)
            {
                FindFlowArrows();
            }

            Debug.Log($"[RCSVisualizationController] Found materials: " +
                     $"HotLeg={_hotLegMaterialInstances.Count}, " +
                     $"ColdLeg={_coldLegMaterialInstances.Count}, " +
                     $"Crossover={_crossoverMaterialInstances.Count}");
        }

        /// <summary>
        /// Replace a specific material on a renderer.
        /// </summary>
        private void ReplaceMaterialOnRenderer(Renderer renderer, Material original, Material replacement)
        {
            var mats = renderer.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i].name == original.name || mats[i] == original)
                {
                    mats[i] = replacement;
                }
            }
            renderer.materials = mats;
        }

        /// <summary>
        /// Find RCP rotor transforms by name.
        /// </summary>
        private void FindRCPRotors()
        {
            string[] rotorNames = { "RCP_1_Rotor", "RCP_2_Rotor", "RCP_3_Rotor", "RCP_4_Rotor" };
            string[] altNames = { "Rotor_1", "Rotor_2", "Rotor_3", "Rotor_4" };

            for (int i = 0; i < 4; i++)
            {
                if (rcpRotors[i] == null)
                {
                    // Try primary name
                    Transform found = FindChildRecursive(transform, rotorNames[i]);
                    if (found == null)
                    {
                        // Try alternate name
                        found = FindChildRecursive(transform, altNames[i]);
                    }
                    rcpRotors[i] = found;
                }
            }
        }

        /// <summary>
        /// Find flow arrow transforms by name.
        /// </summary>
        private void FindFlowArrows()
        {
            List<Transform> found = new List<Transform>();

            // Search for objects containing "arrow" or "flow" in name
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                string name = child.name.ToLower();
                if (name.Contains("arrow") || name.Contains("flow"))
                {
                    found.Add(child);
                }
            }

            flowArrows = found.ToArray();
        }

        /// <summary>
        /// Cache flow arrow base positions for animation.
        /// </summary>
        private void CacheFlowArrowPositions()
        {
            if (flowArrows == null) return;

            _flowArrowBasePositions = new Vector3[flowArrows.Length];
            _flowArrowBaseScales = new Vector3[flowArrows.Length];

            for (int i = 0; i < flowArrows.Length; i++)
            {
                if (flowArrows[i] != null)
                {
                    _flowArrowBasePositions[i] = flowArrows[i].localPosition;
                    _flowArrowBaseScales[i] = flowArrows[i].localScale;
                }
            }
        }

        /// <summary>
        /// Recursively find a child by name.
        /// </summary>
        private Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }

                Transform found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        #endregion

        // ====================================================================
        // PUBLIC CONTROL METHODS
        // ====================================================================

        #region Public Control Methods

        /// <summary>
        /// Update piping colors based on temperatures.
        /// </summary>
        /// <param name="tHot">Hot leg temperature (°F)</param>
        /// <param name="tCold">Cold leg temperature (°F)</param>
        public void UpdateTemperatures(float tHot, float tCold)
        {
            _currentTHot = tHot;
            _currentTCold = tCold;

            // Calculate colors from gradient
            float hotNormalized = Mathf.InverseLerp(minTemperature, maxTemperature, tHot);
            float coldNormalized = Mathf.InverseLerp(minTemperature, maxTemperature, tCold);
            float avgNormalized = (hotNormalized + coldNormalized) / 2f;

            Color hotColor = temperatureGradient.Evaluate(hotNormalized);
            Color coldColor = temperatureGradient.Evaluate(coldNormalized);
            Color avgColor = temperatureGradient.Evaluate(avgNormalized);

            // Apply to hot leg materials
            foreach (var mat in _hotLegMaterialInstances)
            {
                if (mat != null)
                {
                    SetMaterialColor(mat, hotColor, hotNormalized);
                }
            }

            // Apply to cold leg materials
            foreach (var mat in _coldLegMaterialInstances)
            {
                if (mat != null)
                {
                    SetMaterialColor(mat, coldColor, coldNormalized);
                }
            }

            // Apply to crossover materials (use average)
            foreach (var mat in _crossoverMaterialInstances)
            {
                if (mat != null)
                {
                    SetMaterialColor(mat, avgColor, avgNormalized);
                }
            }
        }

        /// <summary>
        /// Set RCP state for visual indication.
        /// </summary>
        /// <param name="index">RCP index (0-3)</param>
        /// <param name="running">Is the pump running</param>
        /// <param name="flowFraction">Flow fraction (0-1, for ramp-up)</param>
        public void SetRCPState(int index, bool running, float flowFraction)
        {
            if (index < 0 || index >= 4) return;

            // Set target rotation speed
            if (running)
            {
                _rcpTargetSpeeds[index] = maxRotorSpeed * flowFraction;
            }
            else
            {
                _rcpTargetSpeeds[index] = 0f;
            }

            // Update indicator color
            if (_rcpIndicatorMaterials[index] != null)
            {
                Color color;
                if (!running)
                {
                    color = color_Stopped;
                }
                else if (flowFraction < 0.99f)
                {
                    color = color_Ramping;
                }
                else
                {
                    color = color_Running;
                }

                _rcpIndicatorMaterials[index].color = color;

                // Set emission
                if (_rcpIndicatorMaterials[index].HasProperty("_EmissionColor"))
                {
                    _rcpIndicatorMaterials[index].SetColor("_EmissionColor", color * 2f);
                    _rcpIndicatorMaterials[index].EnableKeyword("_EMISSION");
                }
            }
        }

        /// <summary>
        /// Set overall flow animation speed.
        /// </summary>
        /// <param name="normalizedSpeed">Speed from 0 (stopped) to 1 (full flow)</param>
        public void SetFlowAnimationSpeed(float normalizedSpeed)
        {
            _currentFlowSpeed = Mathf.Clamp01(normalizedSpeed);
        }

        #endregion

        // ====================================================================
        // ANIMATION
        // ====================================================================

        #region Animation

        /// <summary>
        /// Animate RCP rotors based on their target speeds.
        /// </summary>
        private void AnimateRCPRotors()
        {
            for (int i = 0; i < 4; i++)
            {
                // Smooth speed changes
                _rcpCurrentSpeeds[i] = Mathf.Lerp(
                    _rcpCurrentSpeeds[i],
                    _rcpTargetSpeeds[i],
                    Time.deltaTime * 2f
                );

                // Apply rotation
                if (rcpRotors[i] != null && _rcpCurrentSpeeds[i] > 0.1f)
                {
                    _rcpRotorAngles[i] += _rcpCurrentSpeeds[i] * Time.deltaTime;
                    if (_rcpRotorAngles[i] >= 360f) _rcpRotorAngles[i] -= 360f;

                    rcpRotors[i].localRotation = Quaternion.Euler(0f, _rcpRotorAngles[i], 0f);
                }
            }
        }

        /// <summary>
        /// Animate flow arrows based on current flow speed.
        /// </summary>
        private void AnimateFlowArrows()
        {
            if (flowArrows == null || flowArrows.Length == 0) return;
            if (_currentFlowSpeed < 0.01f)
            {
                // Reset arrows to base state when no flow
                for (int i = 0; i < flowArrows.Length; i++)
                {
                    if (flowArrows[i] != null && _flowArrowBaseScales != null && i < _flowArrowBaseScales.Length)
                    {
                        flowArrows[i].localScale = _flowArrowBaseScales[i];
                        flowArrows[i].localPosition = _flowArrowBasePositions[i];
                    }
                }
                return;
            }

            // Update animation phase
            _flowAnimationPhase += flowArrowSpeed * _currentFlowSpeed * Time.deltaTime;
            if (_flowAnimationPhase > 1f) _flowAnimationPhase -= 1f;

            // Animate each arrow
            for (int i = 0; i < flowArrows.Length; i++)
            {
                if (flowArrows[i] == null) continue;
                if (_flowArrowBaseScales == null || i >= _flowArrowBaseScales.Length) continue;

                // Phase offset for each arrow
                float phase = (_flowAnimationPhase + i * 0.25f) % 1f;

                // Pulse scale
                float scaleMultiplier = 1f + Mathf.Sin(phase * Mathf.PI * 2f) * flowArrowPulseAmplitude * _currentFlowSpeed;
                flowArrows[i].localScale = _flowArrowBaseScales[i] * scaleMultiplier;

                // Movement along flow direction (assuming Z is forward)
                float moveOffset = Mathf.Sin(phase * Mathf.PI * 2f) * flowArrowMovement * _currentFlowSpeed;
                Vector3 pos = _flowArrowBasePositions[i];
                pos += flowArrows[i].forward * moveOffset;
                flowArrows[i].localPosition = pos;
            }
        }

        #endregion

        // ====================================================================
        // MATERIAL HELPERS
        // ====================================================================

        #region Material Helpers

        /// <summary>
        /// Set material color with optional emission.
        /// </summary>
        private void SetMaterialColor(Material mat, Color color, float intensity)
        {
            // Set base color
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            else if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }

            // Set emission if enabled
            if (enableEmission && intensity > 0.3f)
            {
                Color emissionColor = color * emissionIntensity * (intensity - 0.3f) / 0.7f;

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", emissionColor);
                    mat.EnableKeyword("_EMISSION");
                }
            }
            else
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        #endregion

        // ====================================================================
        // DEBUG
        // ====================================================================

        #region Debug

        /// <summary>
        /// Log current state for debugging.
        /// </summary>
        [ContextMenu("Log State")]
        public void LogState()
        {
            Debug.Log($"=== RCSVisualizationController State ===");
            Debug.Log($"T-Hot: {_currentTHot:F1}°F, T-Cold: {_currentTCold:F1}°F");
            Debug.Log($"Flow Speed: {_currentFlowSpeed:F2}");
            Debug.Log($"Hot Leg Materials: {_hotLegMaterialInstances.Count}");
            Debug.Log($"Cold Leg Materials: {_coldLegMaterialInstances.Count}");
            Debug.Log($"Flow Arrows: {flowArrows?.Length ?? 0}");

            for (int i = 0; i < 4; i++)
            {
                Debug.Log($"RCP-{i + 1}: Speed={_rcpCurrentSpeeds[i]:F0}°/s, Rotor={rcpRotors[i] != null}");
            }
        }

        #endregion
    }
}
