// ============================================================================
// CRITICAL: Master the Atom - Window Focus Manager
// WindowFocusManager.cs - Prevents Desktop Flicker and Focus Loss
// ============================================================================
//
// PURPOSE:
//   Prevents the application from losing focus and showing the desktop.
//   Specifically addresses the 60-second flicker issue in fullscreen window mode.
//
// FIXES:
//   - Prevents Windows from stealing focus during periodic system events
//   - Locks cursor when application has focus
//   - Forces application to stay on top
//   - Detects and logs focus loss events for debugging
//
// USAGE:
//   1. Attach to a persistent GameObject (or ScreenManager)
//   2. Application will automatically maintain focus
//   3. Check console for focus loss warnings
//
// VERSION: 1.0.0
// DATE: 2026-02-10
// ============================================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Critical.Core
{
    /// <summary>
    /// Manages application window focus to prevent desktop flicker.
    /// </summary>
    public class WindowFocusManager : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Focus Management")]
        [Tooltip("Force application to stay on top of other windows")]
        [SerializeField] private bool forceTopmost = true;

        [Tooltip("Lock cursor to application window when focused")]
        [SerializeField] private bool lockCursorWhenFocused = false;

        [Tooltip("Prevent system from entering sleep/screensaver")]
        [SerializeField] private bool preventSystemSleep = true;

        [Header("Debug")]
        [Tooltip("Log focus events to console")]
        [SerializeField] private bool debugLogging = true;

        #endregion

        #region Windows API Imports

#if UNITY_STANDALONE_WIN
        // Window positioning constants
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        // Power management constants
        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint esFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);
#endif

        #endregion

        #region Private Fields

        private bool _hasFocus = true;
        private float _lastFocusTime = 0f;
        private float _focusLostTime = 0f;
        private int _focusLossCount = 0;
        private float _simulationTimeAtLastFocus = 0f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Ensure application runs in background
            Application.runInBackground = true;

            if (debugLogging)
            {
                Debug.Log("[WindowFocusManager] Initialized - preventing focus loss");
            }
        }

        private void Start()
        {
            // Prevent system sleep/screensaver
            if (preventSystemSleep)
            {
                PreventSystemSleep();
            }

            // Set window to topmost
            if (forceTopmost)
            {
                SetWindowTopmost(true);
            }

            _lastFocusTime = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            // Monitor for periodic focus loss
            CheckPeriodicFocusLoss();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            float currentTime = Time.realtimeSinceStartup;
            float focusDuration = currentTime - _focusLostTime;

            if (!hasFocus)
            {
                // Lost focus
                _hasFocus = false;
                _focusLostTime = currentTime;
                _focusLossCount++;

                if (debugLogging)
                {
                    Debug.LogWarning($"[WindowFocusManager] FOCUS LOST! " +
                                   $"Count: {_focusLossCount}, " +
                                   $"Sim Time: {Time.time:F1}s, " +
                                   $"Real Time: {currentTime:F1}s");
                }

                // Try to immediately reclaim focus
                ReclaimFocus();
            }
            else
            {
                // Gained focus
                _hasFocus = true;
                _lastFocusTime = currentTime;

                if (debugLogging && _focusLossCount > 0)
                {
                    Debug.Log($"[WindowFocusManager] Focus regained after {focusDuration:F3}s");
                }

                // Lock cursor if configured
                if (lockCursorWhenFocused)
                {
                    Cursor.lockState = CursorLockMode.Confined;
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (debugLogging)
            {
                Debug.LogWarning($"[WindowFocusManager] Application Pause: {pauseStatus}");
            }

            // Prevent actual pausing
            if (pauseStatus)
            {
                Application.runInBackground = true;
                ReclaimFocus();
            }
        }

        private void OnDestroy()
        {
            // Reset execution state
#if UNITY_STANDALONE_WIN
            if (preventSystemSleep)
            {
                SetThreadExecutionState(ES_CONTINUOUS);
            }
#endif

            if (lockCursorWhenFocused)
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }

        #endregion

        #region Focus Management

        /// <summary>
        /// Detect if focus loss is happening periodically (like every 60 seconds)
        /// </summary>
        private void CheckPeriodicFocusLoss()
        {
            // Check if we're approaching a 60-second boundary
            float simTime = Time.time;
            float timeMod60 = simTime % 60f;

            // If we're within 0.1 seconds of a 60-second mark, be extra vigilant
            if (timeMod60 < 0.1f || timeMod60 > 59.9f)
            {
                if (!_hasFocus)
                {
                    if (debugLogging)
                    {
                        Debug.LogWarning($"[WindowFocusManager] Periodic focus loss detected at sim time {simTime:F1}s!");
                    }
                    ReclaimFocus();
                }
            }
        }

        /// <summary>
        /// Attempt to reclaim window focus
        /// </summary>
        private void ReclaimFocus()
        {
#if UNITY_STANDALONE_WIN
            try
            {
                IntPtr windowHandle = GetActiveWindow();
                if (windowHandle != IntPtr.Zero)
                {
                    SetFocus(windowHandle);
                    SetWindowTopmost(true);

                    if (debugLogging)
                    {
                        Debug.Log("[WindowFocusManager] Reclaimed focus");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[WindowFocusManager] Failed to reclaim focus: {e.Message}");
            }
#endif
        }

        /// <summary>
        /// Set window to topmost (always on top)
        /// </summary>
        private void SetWindowTopmost(bool topmost)
        {
#if UNITY_STANDALONE_WIN
            try
            {
                IntPtr windowHandle = GetActiveWindow();
                if (windowHandle != IntPtr.Zero)
                {
                    IntPtr topmostFlag = topmost ? HWND_TOPMOST : HWND_NOTOPMOST;
                    SetWindowPos(windowHandle, topmostFlag, 0, 0, 0, 0,
                               SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

                    if (debugLogging)
                    {
                        Debug.Log($"[WindowFocusManager] Window topmost: {topmost}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[WindowFocusManager] Failed to set topmost: {e.Message}");
            }
#endif
        }

        /// <summary>
        /// Prevent Windows from entering sleep or showing screensaver
        /// </summary>
        private void PreventSystemSleep()
        {
#if UNITY_STANDALONE_WIN
            uint result = SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);

            if (debugLogging)
            {
                if (result == 0)
                {
                    Debug.LogWarning("[WindowFocusManager] Failed to prevent system sleep");
                }
                else
                {
                    Debug.Log("[WindowFocusManager] System sleep prevention enabled");
                }
            }
#endif
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get focus loss statistics
        /// </summary>
        public string GetFocusStats()
        {
            return $"Focus Losses: {_focusLossCount}, " +
                   $"Current Focus: {_hasFocus}, " +
                   $"Last Focus: {_lastFocusTime:F1}s ago";
        }

        /// <summary>
        /// Manually force focus reclaim
        /// </summary>
        [ContextMenu("Force Reclaim Focus")]
        public void ForceReclaimFocus()
        {
            ReclaimFocus();
        }

        #endregion
    }
}
