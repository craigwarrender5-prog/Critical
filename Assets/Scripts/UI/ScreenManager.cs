// ============================================================================
// CRITICAL: Master the Atom - Screen Manager
// ScreenManager.cs - Singleton Manager for Multi-Screen Navigation
// ============================================================================
//
// PURPOSE:
//   Manages all operator screens in the simulator, providing:
//   - Centralized input handling for screen toggles (New Input System)
//   - Single-screen-visible enforcement (only one screen shown at a time)
//   - Screen registration and lookup
//   - Screen transition coordination
//
// FEATURES:
//   - Singleton pattern for global access
//   - Automatic screen registration
//   - New Input System via ScreenInputActions.inputactions
//   - Keyboard routing (Keys 1-8, Tab) via action callbacks
//   - Screen state tracking
//   - Mutual exclusion (hide current before showing new)
//
// KEYBOARD MAPPING (via OperatorScreens action map):
//   Key 1: Reactor Core (GOLD STANDARD, via ReactorScreenAdapter)
//   Key 2: RCS Primary Loop
//   Key 3: Pressurizer
//   Key 4: CVCS
//   Key 5: Steam Generators
//   Key 6: Turbine-Generator
//   Key 7: Secondary Systems
//   Key 8: Auxiliary Systems
//   Tab:   Plant Overview
//
// INPUT SYSTEM NOTES:
//   - The project uses Unity New Input System ONLY (activeInputHandler: 1)
//   - Legacy UnityEngine.Input API is disabled project-wide
//   - All screen switching uses ScreenInputActions.inputactions
//   - The existing Player action map has Previous (key 1) and Next (key 2)
//     bindings that may conflict — the OperatorScreens map takes priority
//     when enabled. Users should rebind or disable Player map keys 1/2.
//
// USAGE:
//   1. Create an empty GameObject named "ScreenManager"
//   2. Attach this script
//   3. Assign the ScreenInputActions asset in the Inspector (or it auto-loads)
//   4. Screens will auto-register on Start()
//   5. Access via ScreenManager.Instance
//
// SOURCES:
//   - Operator_Screen_Layout_Plan_v1_0_0.md
//   - IMPLEMENTATION_PLAN_v2.0.0_MultiScreenGUI.md
//
// VERSION: 2.0.3
// DATE: 2026-02-10
// CLASSIFICATION: UI - Base Infrastructure
// CHANGE: v2.0.0 — Migrated from legacy Input.GetKeyDown() to New Input System actions
// CHANGE: v2.0.3 — Added RegisterInactiveScreens() to discover screens on
//         inactive GameObjects that never ran Start() (fixes keys 2/Tab)
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Critical.UI
{
    /// <summary>
    /// Singleton manager for all operator screens.
    /// Handles New Input System actions, screen registration, and mutual exclusion.
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        // ====================================================================
        // SINGLETON
        // ====================================================================

        #region Singleton

        private static ScreenManager _instance;
        private static bool _applicationQuitting = false;

        /// <summary>
        /// Global singleton instance. Returns null during application shutdown
        /// to prevent spawning new GameObjects from OnDestroy() callbacks.
        /// </summary>
        public static ScreenManager Instance
        {
            get
            {
                if (_applicationQuitting)
                    return null;

                if (_instance == null)
                {
                    _instance = FindObjectOfType<ScreenManager>();

                    if (_instance == null)
                    {
                        Debug.LogWarning("[ScreenManager] No ScreenManager found in scene. Creating one.");
                        GameObject go = new GameObject("ScreenManager");
                        _instance = go.AddComponent<ScreenManager>();
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

        [Header("Input System")]
        [Tooltip("InputActionAsset containing the OperatorScreens action map. " +
                 "If null, attempts to load 'ScreenInputActions' from Resources or InputActions folder.")]
        [SerializeField] private InputActionAsset screenInputActions;

        [Header("Behavior Settings")]
        [Tooltip("Only allow one screen visible at a time")]
        [SerializeField] private bool mutualExclusion = true;

        [Tooltip("Allow hiding all screens (no screen visible)")]
        [SerializeField] private bool allowNoScreen = true;

        [Tooltip("Default screen to show on startup (by index, -1 for none)")]
        [SerializeField] private int defaultScreenIndex = 1;

        [Header("Debug")]
        [Tooltip("Log screen transitions")]
        [SerializeField] private bool debugLogging = true;

        #endregion

        // ====================================================================
        // EVENTS
        // ====================================================================

        #region Events

        /// <summary>
        /// Fired when active screen changes. Parameters: (old index, new index).
        /// Index of -1 means no screen is active.
        /// </summary>
        public event Action<int, int> OnActiveScreenChanged;

        /// <summary>
        /// Fired when a screen is registered.
        /// </summary>
        public event Action<OperatorScreen> OnScreenRegistered;

        /// <summary>
        /// Fired when a screen is unregistered.
        /// </summary>
        public event Action<OperatorScreen> OnScreenUnregistered;

        #endregion

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        #region Private Fields

        /// <summary>
        /// Dictionary of registered screens by index.
        /// </summary>
        private Dictionary<int, OperatorScreen> _screens = new Dictionary<int, OperatorScreen>();

        /// <summary>
        /// Currently active screen index (-1 if none).
        /// </summary>
        private int _activeScreenIndex = -1;

        /// <summary>
        /// The OperatorScreens action map from the InputActionAsset.
        /// </summary>
        private InputActionMap _screenActionMap;

        /// <summary>
        /// Mapping of action name to screen index for callback routing.
        /// </summary>
        private Dictionary<string, int> _actionNameToScreenIndex = new Dictionary<string, int>()
        {
            { "Screen1", 1 },
            { "Screen2", 2 },
            { "Screen3", 3 },
            { "Screen4", 4 },
            { "Screen5", 5 },
            { "Screen6", 6 },
            { "Screen7", 7 },
            { "Screen8", 8 },
            { "Overview", OVERVIEW_INDEX }
        };

        /// <summary>
        /// Index used for overview screen (Tab key).
        /// </summary>
        public const int OVERVIEW_INDEX = 100;

        #endregion

        // ====================================================================
        // PUBLIC PROPERTIES
        // ====================================================================

        #region Public Properties

        /// <summary>
        /// Index of the currently active screen (-1 if none).
        /// </summary>
        public int ActiveScreenIndex => _activeScreenIndex;

        /// <summary>
        /// The currently active screen (null if none).
        /// </summary>
        public OperatorScreen ActiveScreen
        {
            get
            {
                if (_activeScreenIndex < 0) return null;
                _screens.TryGetValue(_activeScreenIndex, out OperatorScreen screen);
                return screen;
            }
        }

        /// <summary>
        /// Number of registered screens.
        /// </summary>
        public int ScreenCount => _screens.Count;

        /// <summary>
        /// Is any screen currently visible?
        /// </summary>
        public bool IsAnyScreenVisible => _activeScreenIndex >= 0;

        /// <summary>
        /// Is mutual exclusion enabled?
        /// </summary>
        public bool MutualExclusion
        {
            get => mutualExclusion;
            set => mutualExclusion = value;
        }

        #endregion

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        #region Unity Lifecycle

        private void Awake()
        {
            // Enforce singleton
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[ScreenManager] Duplicate ScreenManager found. Destroying this instance.");
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (debugLogging)
            {
                Debug.Log("[ScreenManager] Initialized (New Input System)");
            }
        }

        private void Start()
        {
            // Initialize Input System
            InitializeInputActions();

            // Discover and register any inactive OperatorScreen components.
            // Screens built by MultiScreenBuilder start with SetActive(false),
            // which prevents their Start() from running, so they never
            // self-register. We find them here using GetComponentsInChildren
            // with includeInactive=true.
            RegisterInactiveScreens();

            // Show default screen if configured
            if (defaultScreenIndex >= 0 && _screens.ContainsKey(defaultScreenIndex))
            {
                ShowScreen(defaultScreenIndex);
            }
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void OnDestroy()
        {
            DisableInputActions();

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
        // INPUT SYSTEM INITIALIZATION
        // ====================================================================

        #region Input System

        /// <summary>
        /// Initialize the Input Action asset and find the OperatorScreens action map.
        /// </summary>
        private void InitializeInputActions()
        {
            // If no asset assigned in inspector, try to find it
            if (screenInputActions == null)
            {
                // Try loading from Resources
                screenInputActions = Resources.Load<InputActionAsset>("ScreenInputActions");
            }

            if (screenInputActions == null)
            {
                Debug.LogError("[ScreenManager] ScreenInputActions InputActionAsset not found! " +
                             "Assign it in the Inspector or place it in a Resources folder. " +
                             "Screen keyboard switching will NOT work.");
                return;
            }

            // Find the OperatorScreens action map
            _screenActionMap = screenInputActions.FindActionMap("OperatorScreens", throwIfNotFound: false);

            if (_screenActionMap == null)
            {
                Debug.LogError("[ScreenManager] 'OperatorScreens' action map not found in ScreenInputActions asset! " +
                             "Screen keyboard switching will NOT work.");
                return;
            }

            // Subscribe to each action's performed callback
            foreach (var action in _screenActionMap.actions)
            {
                // Capture action name for the lambda closure
                string actionName = action.name;
                action.performed += ctx => OnScreenActionPerformed(actionName);
            }

            if (debugLogging)
            {
                Debug.Log($"[ScreenManager] Input System initialized — {_screenActionMap.actions.Count} screen actions bound");
            }

            // Enable the action map now. OnEnable() runs before Start(), so
            // the map was still null when OnEnable() first fired. We must
            // enable it here after setup is complete.
            EnableInputActions();
        }

        /// <summary>
        /// Enable the OperatorScreens action map.
        /// </summary>
        private void EnableInputActions()
        {
            if (_screenActionMap != null && !_screenActionMap.enabled)
            {
                _screenActionMap.Enable();

                if (debugLogging)
                {
                    Debug.Log("[ScreenManager] OperatorScreens action map enabled");
                }
            }
        }

        /// <summary>
        /// Disable the OperatorScreens action map.
        /// </summary>
        private void DisableInputActions()
        {
            if (_screenActionMap != null && _screenActionMap.enabled)
            {
                _screenActionMap.Disable();
            }
        }

        /// <summary>
        /// Called when any screen action is performed (key pressed).
        /// Routes to the correct screen toggle.
        /// </summary>
        private void OnScreenActionPerformed(string actionName)
        {
            if (_actionNameToScreenIndex.TryGetValue(actionName, out int screenIndex))
            {
                ToggleScreen(screenIndex);
            }
            else
            {
                Debug.LogWarning($"[ScreenManager] Unknown screen action: {actionName}");
            }
        }

        #endregion

        // ====================================================================
        // SCREEN REGISTRATION
        // ====================================================================

        #region Screen Registration

        /// <summary>
        /// Discover and register OperatorScreen components on inactive GameObjects.
        /// Screens that start with SetActive(false) — such as those created by
        /// MultiScreenBuilder — never run Start(), so they never self-register.
        /// This method searches ALL root GameObjects and their full hierarchies
        /// (including inactive children) to find every OperatorScreen in the scene.
        /// </summary>
        private void RegisterInactiveScreens()
        {
            int found = 0;

            // Search every root GameObject in every loaded scene
            for (int s = 0; s < UnityEngine.SceneManagement.SceneManager.sceneCount; s++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    // GetComponentsInChildren with includeInactive=true finds
                    // OperatorScreen on any descendant, even if SetActive(false)
                    OperatorScreen[] screens = root.GetComponentsInChildren<OperatorScreen>(true);
                    foreach (OperatorScreen screen in screens)
                    {
                        if (!_screens.ContainsKey(screen.ScreenIndex))
                        {
                            RegisterScreen(screen);
                            found++;
                        }
                    }
                }
            }

            if (debugLogging)
            {
                Debug.Log($"[ScreenManager] RegisterInactiveScreens: scanned scene, found {found} new screen(s). " +
                          $"Total registered: {_screens.Count}");
            }
        }

        /// <summary>
        /// Register an operator screen with the manager.
        /// Called automatically by OperatorScreen.Start().
        /// </summary>
        /// <param name="screen">The screen to register</param>
        public void RegisterScreen(OperatorScreen screen)
        {
            if (screen == null)
            {
                Debug.LogError("[ScreenManager] Cannot register null screen");
                return;
            }

            int index = screen.ScreenIndex;

            // Check for duplicate registration
            if (_screens.ContainsKey(index))
            {
                if (_screens[index] == screen)
                {
                    // Already registered, ignore
                    return;
                }
                else
                {
                    Debug.LogWarning($"[ScreenManager] Screen index {index} already registered to '{_screens[index].ScreenName}'. " +
                                   $"Replacing with '{screen.ScreenName}'.");
                }
            }

            _screens[index] = screen;

            // Subscribe to screen visibility changes
            screen.OnVisibilityChanged += (visible) => OnScreenVisibilityChanged(screen, visible);

            if (debugLogging)
            {
                Debug.Log($"[ScreenManager] Registered screen {index}: '{screen.ScreenName}'");
            }

            OnScreenRegistered?.Invoke(screen);
        }

        /// <summary>
        /// Unregister an operator screen from the manager.
        /// Called automatically by OperatorScreen.OnDestroy().
        /// </summary>
        /// <param name="screen">The screen to unregister</param>
        public void UnregisterScreen(OperatorScreen screen)
        {
            if (screen == null) return;

            int index = screen.ScreenIndex;

            if (_screens.ContainsKey(index) && _screens[index] == screen)
            {
                _screens.Remove(index);

                // Clear active if this was the active screen
                if (_activeScreenIndex == index)
                {
                    _activeScreenIndex = -1;
                }

                if (debugLogging)
                {
                    Debug.Log($"[ScreenManager] Unregistered screen {index}: '{screen.ScreenName}'");
                }

                OnScreenUnregistered?.Invoke(screen);
            }
        }

        /// <summary>
        /// Get a registered screen by index.
        /// </summary>
        public OperatorScreen GetScreen(int index)
        {
            _screens.TryGetValue(index, out OperatorScreen screen);
            return screen;
        }

        /// <summary>
        /// Get a registered screen by name.
        /// </summary>
        public OperatorScreen GetScreenByName(string name)
        {
            foreach (var screen in _screens.Values)
            {
                if (screen.ScreenName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return screen;
                }
            }
            return null;
        }

        /// <summary>
        /// Check if a screen is registered.
        /// </summary>
        public bool IsScreenRegistered(int index)
        {
            return _screens.ContainsKey(index);
        }

        /// <summary>
        /// Get all registered screen indices.
        /// </summary>
        public IEnumerable<int> GetRegisteredScreenIndices()
        {
            return _screens.Keys;
        }

        #endregion

        // ====================================================================
        // SCREEN VISIBILITY CONTROL
        // ====================================================================

        #region Screen Visibility Control

        /// <summary>
        /// Show a screen by index. Hides current screen if mutual exclusion is enabled.
        /// </summary>
        /// <param name="index">Screen index to show</param>
        /// <returns>True if screen was shown successfully</returns>
        public bool ShowScreen(int index)
        {
            if (!_screens.TryGetValue(index, out OperatorScreen screen))
            {
                if (debugLogging)
                {
                    Debug.LogWarning($"[ScreenManager] Screen {index} not registered");
                }
                return false;
            }

            int oldIndex = _activeScreenIndex;

            // Hide current screen if mutual exclusion is enabled
            if (mutualExclusion && _activeScreenIndex >= 0 && _activeScreenIndex != index)
            {
                HideScreen(_activeScreenIndex);
            }

            // Show the new screen
            screen.Show();
            _activeScreenIndex = index;

            if (debugLogging && oldIndex != index)
            {
                Debug.Log($"[ScreenManager] Screen changed: {oldIndex} -> {index} ({screen.ScreenName})");
            }

            if (oldIndex != index)
            {
                OnActiveScreenChanged?.Invoke(oldIndex, index);
            }

            return true;
        }

        /// <summary>
        /// Hide a screen by index.
        /// </summary>
        /// <param name="index">Screen index to hide</param>
        /// <returns>True if screen was hidden successfully</returns>
        public bool HideScreen(int index)
        {
            if (!_screens.TryGetValue(index, out OperatorScreen screen))
            {
                return false;
            }

            screen.Hide();

            if (_activeScreenIndex == index)
            {
                int oldIndex = _activeScreenIndex;
                _activeScreenIndex = -1;

                if (debugLogging)
                {
                    Debug.Log($"[ScreenManager] Screen {index} ({screen.ScreenName}) hidden");
                }

                OnActiveScreenChanged?.Invoke(oldIndex, -1);
            }

            return true;
        }

        /// <summary>
        /// Toggle a screen's visibility.
        /// </summary>
        /// <param name="index">Screen index to toggle</param>
        /// <returns>True if screen is now visible, false if hidden</returns>
        public bool ToggleScreen(int index)
        {
            if (!_screens.TryGetValue(index, out OperatorScreen screen))
            {
                if (debugLogging)
                {
                    Debug.LogWarning($"[ScreenManager] Screen {index} not registered — toggle ignored");
                }
                return false;
            }

            if (screen.IsVisible)
            {
                // Currently visible - hide it (if allowed)
                if (allowNoScreen)
                {
                    HideScreen(index);
                    return false;
                }
                else
                {
                    // Don't allow hiding if no screen would be visible
                    return true;
                }
            }
            else
            {
                // Currently hidden - show it
                ShowScreen(index);
                return true;
            }
        }

        /// <summary>
        /// Hide all screens.
        /// </summary>
        public void HideAllScreens()
        {
            if (!allowNoScreen)
            {
                Debug.LogWarning("[ScreenManager] HideAllScreens called but allowNoScreen is false");
                return;
            }

            foreach (var screen in _screens.Values)
            {
                if (screen.IsVisible)
                {
                    screen.Hide();
                }
            }

            int oldIndex = _activeScreenIndex;
            _activeScreenIndex = -1;

            if (oldIndex >= 0)
            {
                OnActiveScreenChanged?.Invoke(oldIndex, -1);
            }

            if (debugLogging)
            {
                Debug.Log("[ScreenManager] All screens hidden");
            }
        }

        /// <summary>
        /// Show the next screen in sequence.
        /// </summary>
        public void ShowNextScreen()
        {
            if (_screens.Count == 0) return;

            // Get sorted list of indices
            List<int> indices = new List<int>(_screens.Keys);
            indices.Sort();

            int currentPos = indices.IndexOf(_activeScreenIndex);
            int nextPos = (currentPos + 1) % indices.Count;

            ShowScreen(indices[nextPos]);
        }

        /// <summary>
        /// Show the previous screen in sequence.
        /// </summary>
        public void ShowPreviousScreen()
        {
            if (_screens.Count == 0) return;

            // Get sorted list of indices
            List<int> indices = new List<int>(_screens.Keys);
            indices.Sort();

            int currentPos = indices.IndexOf(_activeScreenIndex);
            int prevPos = currentPos <= 0 ? indices.Count - 1 : currentPos - 1;

            ShowScreen(indices[prevPos]);
        }

        #endregion

        // ====================================================================
        // INTERNAL HANDLERS
        // ====================================================================

        #region Internal Handlers

        /// <summary>
        /// Called when a screen's visibility changes externally.
        /// Keeps our tracking in sync.
        /// </summary>
        private void OnScreenVisibilityChanged(OperatorScreen screen, bool visible)
        {
            int index = screen.ScreenIndex;

            if (visible)
            {
                // Screen became visible
                if (mutualExclusion && _activeScreenIndex >= 0 && _activeScreenIndex != index)
                {
                    // Another screen is active - hide it
                    if (_screens.TryGetValue(_activeScreenIndex, out OperatorScreen currentScreen))
                    {
                        currentScreen.SetVisible(false, silent: true);
                    }
                }

                _activeScreenIndex = index;
            }
            else
            {
                // Screen became hidden
                if (_activeScreenIndex == index)
                {
                    _activeScreenIndex = -1;
                }
            }
        }

        #endregion

        // ====================================================================
        // DEBUG / UTILITY
        // ====================================================================

        #region Debug / Utility

        /// <summary>
        /// Get a status string for debugging.
        /// </summary>
        public string GetStatusString()
        {
            string activeScreenName = ActiveScreen != null ? ActiveScreen.ScreenName : "None";
            string inputStatus = _screenActionMap != null ? "Active" : "NOT INITIALIZED";
            return $"ScreenManager: {_screens.Count} screens registered, Active: {activeScreenName} " +
                   $"(Index: {_activeScreenIndex}), Input: {inputStatus}";
        }

        /// <summary>
        /// Log all registered screens.
        /// </summary>
        [ContextMenu("Log Registered Screens")]
        public void LogRegisteredScreens()
        {
            Debug.Log("=== Registered Screens ===");
            foreach (var kvp in _screens)
            {
                string active = kvp.Key == _activeScreenIndex ? " [ACTIVE]" : "";
                Debug.Log($"  [{kvp.Key}] {kvp.Value.ScreenName}{active}");
            }
            Debug.Log($"Total: {_screens.Count} screens");
            Debug.Log($"Input System: {(_screenActionMap != null ? "Initialized" : "NOT INITIALIZED")}");
        }

        #endregion
    }
}
