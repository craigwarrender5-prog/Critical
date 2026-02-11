// ============================================================================
// CRITICAL: Master the Atom - Reactor Screen Adapter
// ReactorScreenAdapter.cs - Bridges GOLD STANDARD Screen 1 to ScreenManager
// ============================================================================
//
// PURPOSE:
//   The ReactorOperatorScreen (Screen 1) is GOLD STANDARD and does NOT inherit
//   from OperatorScreen. It manages its own visibility via legacy Input.GetKeyDown()
//   which is non-functional under the New Input System (activeInputHandler: 1).
//
//   This adapter sits on the SAME GameObject as ReactorOperatorScreen and:
//   1. Registers Screen 1 with ScreenManager (using OperatorScreen API)
//   2. Bridges Show()/Hide() calls to ReactorOperatorScreen's GameObject.SetActive()
//   3. ReactorOperatorScreen.cs now uses its own New Input System InputAction
//      for the '1' key toggle (migrated from legacy Input.GetKeyDown()).
//
//   Both ReactorOperatorScreen's own InputAction and ScreenManager's action map
//   can trigger screen 1 toggle. ScreenManager coordinates mutual exclusion
//   across all screens.
//
// PLACEMENT:
//   Attach this MonoBehaviour to the SAME GameObject that has ReactorOperatorScreen.
//   The adapter reads ScreenID and ToggleKey from the existing component.
//
// GOLD STANDARD: ReactorOperatorScreen.cs is NOT modified.
//
// VERSION: 2.0.0
// DATE: 2026-02-10
// CLASSIFICATION: UI — Adapter / Integration
// ============================================================================

using UnityEngine;

namespace Critical.UI
{
    /// <summary>
    /// Adapter that registers the GOLD STANDARD ReactorOperatorScreen with
    /// the ScreenManager system without modifying the original class.
    /// Inherits from OperatorScreen to satisfy ScreenManager's registration API.
    /// </summary>
    [RequireComponent(typeof(ReactorOperatorScreen))]
    public class ReactorScreenAdapter : OperatorScreen
    {
        // ====================================================================
        // ABSTRACT PROPERTY IMPLEMENTATIONS
        // ====================================================================

        #region OperatorScreen Implementation

        public override KeyCode ToggleKey => KeyCode.Alpha1;
        public override string ScreenName => "REACTOR CORE";
        public override int ScreenIndex => 1;

        #endregion

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        #region Private Fields

        /// <summary>
        /// Reference to the GOLD STANDARD ReactorOperatorScreen on this GameObject.
        /// </summary>
        private ReactorOperatorScreen _reactorScreen;

        #endregion

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        #region Unity Lifecycle

        protected override void Awake()
        {
            // Get reference to the GOLD STANDARD screen
            _reactorScreen = GetComponent<ReactorOperatorScreen>();

            if (_reactorScreen == null)
            {
                Debug.LogError("[ReactorScreenAdapter] ReactorOperatorScreen not found on this GameObject! " +
                             "This adapter must be on the same GameObject as ReactorOperatorScreen.");
                enabled = false;
                return;
            }

            // Cache our own RectTransform and CanvasGroup
            // Note: We do NOT call base.Awake() because it would try to get its own
            // Image component and apply backgroundColor, which could conflict with
            // ReactorOperatorScreen's own Image setup.
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            // Ensure CanvasGroup exists (ReactorOperatorScreen may not require one)
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            Debug.Log("[ReactorScreenAdapter] Adapter initialized for GOLD STANDARD ReactorOperatorScreen");
        }

        protected override void Start()
        {
            // Register with ScreenManager (this calls base.Start() behavior manually)
            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.RegisterScreen(this);
                Debug.Log("[ReactorScreenAdapter] Screen 1 (Reactor Core) registered with ScreenManager via adapter");
            }
            else
            {
                Debug.LogWarning("[ReactorScreenAdapter] ScreenManager not found. Adapter registration deferred.");
            }

            // Sync our visibility state with the ReactorOperatorScreen's initial state
            isVisible = _reactorScreen.StartVisible;
        }

        protected override void Update()
        {
            // Do NOT call base.Update() — we don't want the OperatorScreen's
            // fallback keyboard handling or status bar updates.
            // ScreenManager handles all input for this screen via new Input System.

            // Keep our visibility state in sync with the actual GameObject state
            // in case something else toggles it.
            if (_reactorScreen != null)
            {
                bool currentlyActive = _reactorScreen.gameObject.activeSelf;
                if (isVisible != currentlyActive)
                {
                    isVisible = currentlyActive;
                }
            }
        }

        protected override void OnDestroy()
        {
            // Unregister from ScreenManager
            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.UnregisterScreen(this);
            }
        }

        #endregion

        // ====================================================================
        // VISIBILITY CONTROL — Delegates to ReactorOperatorScreen's GameObject
        // ====================================================================

        #region Visibility Control

        /// <summary>
        /// Override SetVisible to control the ReactorOperatorScreen's GameObject.
        /// </summary>
        public override void SetVisible(bool visible, bool silent = false)
        {
            if (_reactorScreen == null) return;
            if (isVisible == visible) return;

            isVisible = visible;

            // Control the actual ReactorOperatorScreen's visibility
            _reactorScreen.gameObject.SetActive(visible);

            // Update CanvasGroup if present
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            if (!silent)
            {
                FireVisibilityChanged(visible);

                if (visible)
                {
                    FireScreenShown();
                }
                else
                {
                    FireScreenHidden();
                }

                Debug.Log($"[ReactorScreenAdapter] Screen 1 visibility: {visible}");
            }
        }

        #endregion
    }
}
