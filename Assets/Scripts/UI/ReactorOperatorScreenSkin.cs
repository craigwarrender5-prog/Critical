// ============================================================================
// CRITICAL: Master the Atom - Reactor Operator GUI
// ReactorOperatorScreenSkin.cs - Panel Artwork Skin Component
// ============================================================================
//
// PURPOSE:
//   Loads the Blender-rendered panel background texture and positions it
//   behind all existing UI elements. Makes existing flat-colored panel
//   backgrounds transparent so the 3D-rendered artwork shows through.
//
// USAGE:
//   Added automatically by OperatorScreenBuilder during screen creation.
//   Falls back gracefully if the texture file is not found (existing
//   colored panels remain visible as before).
//
// TEXTURE LOCATION:
//   Assets/Textures/ReactorOperatorPanel/panel_base_color
//
// ARCHITECTURE:
//   - Purely additive visual layer — no physics or logic changes
//   - If texture not found, does nothing (silent fallback)
//   - Implements IMosaicComponent for lifecycle management
//
// CREATED: v4.0.0
// ============================================================================

using UnityEngine;
using UnityEngine.UI;

namespace Critical.UI
{
    /// <summary>
    /// Applies the Blender-rendered panel artwork as a background texture
    /// on the Reactor Operator Screen.
    /// </summary>
    public class ReactorOperatorScreenSkin : MonoBehaviour
    {
        // ====================================================================
        // CONSTANTS
        // ====================================================================

        /// <summary>Resource path for the panel texture (without extension).</summary>
        private const string TEXTURE_RESOURCE_PATH = "ReactorOperatorPanel/panel_base_color";
        
        /// <summary>Alternative search paths for the texture asset.</summary>
        private static readonly string[] TEXTURE_SEARCH_PATHS = new string[]
        {
            "Textures/ReactorOperatorPanel/panel_base_color",
            "ReactorOperatorPanel/panel_base_color",
            "panel_base_color"
        };

        // ====================================================================
        // INSPECTOR FIELDS
        // ====================================================================

        #region Inspector Fields

        [Header("Panel Skin")]
        [Tooltip("The panel background texture (auto-loaded if null)")]
        public Texture2D PanelTexture;

        [Tooltip("Panels to make transparent when skin is active")]
        public Image[] TransparentPanels;

        [Header("Settings")]
        [Tooltip("Alpha for the background texture (1 = fully opaque)")]
        [Range(0f, 1f)]
        public float BackgroundAlpha = 1f;

        [Tooltip("Enable the skin (disable to revert to flat panels)")]
        public bool SkinEnabled = true;

        #endregion

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        #region Private Fields

        private RawImage _backgroundImage;
        private bool _textureLoaded = false;
        private Color[] _originalPanelColors;

        #endregion

        // ====================================================================
        // PROPERTIES
        // ====================================================================

        /// <summary>Is the skin texture loaded and active?</summary>
        public bool IsActive => _textureLoaded && SkinEnabled;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        #region Unity Lifecycle

        private void Awake()
        {
            TryLoadTexture();
        }

        private void Start()
        {
            if (_textureLoaded && SkinEnabled)
            {
                ApplySkin();
            }
        }

        private void OnEnable()
        {
            if (_textureLoaded && SkinEnabled && _backgroundImage != null)
            {
                _backgroundImage.enabled = true;
            }
        }

        private void OnDisable()
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.enabled = false;
            }
        }

        #endregion

        // ====================================================================
        // TEXTURE LOADING
        // ====================================================================

        #region Texture Loading

        private void TryLoadTexture()
        {
            // Already assigned in inspector?
            if (PanelTexture != null)
            {
                _textureLoaded = true;
                return;
            }

            // Try Resources.Load from various paths
            foreach (string path in TEXTURE_SEARCH_PATHS)
            {
                PanelTexture = Resources.Load<Texture2D>(path);
                if (PanelTexture != null)
                {
                    _textureLoaded = true;
                    Debug.Log($"[ScreenSkin] Panel texture loaded from Resources/{path}");
                    return;
                }
            }

            // Not found — this is OK, the flat panel colors will remain
            Debug.Log("[ScreenSkin] Panel texture not found — using flat panel colors. " +
                     "Place texture in Assets/Resources/ReactorOperatorPanel/panel_base_color.png " +
                     "or assign manually in Inspector.");
            _textureLoaded = false;
        }

        #endregion

        // ====================================================================
        // SKIN APPLICATION
        // ====================================================================

        #region Skin Application

        /// <summary>
        /// Apply the panel skin: add background image and make panels transparent.
        /// </summary>
        public void ApplySkin()
        {
            if (!_textureLoaded || PanelTexture == null) return;

            CreateBackgroundImage();
            MakePanelsTransparent();

            Debug.Log("[ScreenSkin] Panel skin applied successfully");
        }

        /// <summary>
        /// Remove the panel skin and restore original panel colors.
        /// </summary>
        public void RemoveSkin()
        {
            RestorePanelColors();

            if (_backgroundImage != null)
            {
                _backgroundImage.enabled = false;
            }

            Debug.Log("[ScreenSkin] Panel skin removed");
        }

        private void CreateBackgroundImage()
        {
            // Check if background already exists
            if (_backgroundImage != null) return;

            // Create a new GameObject for the background
            GameObject bgGO = new GameObject("PanelBackground");
            bgGO.transform.SetParent(transform, false);

            // Make it the first sibling so it renders behind everything
            bgGO.transform.SetAsFirstSibling();

            // Setup RectTransform to fill entire screen
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Add RawImage with the panel texture
            _backgroundImage = bgGO.AddComponent<RawImage>();
            _backgroundImage.texture = PanelTexture;
            _backgroundImage.color = new Color(1f, 1f, 1f, BackgroundAlpha);
            _backgroundImage.uvRect = new Rect(0, 0, 1, 1);

            // Ensure it doesn't block raycasts on interactive elements above it
            _backgroundImage.raycastTarget = false;
        }

        private void MakePanelsTransparent()
        {
            if (TransparentPanels == null || TransparentPanels.Length == 0) return;

            // Save original colors for restoration
            _originalPanelColors = new Color[TransparentPanels.Length];

            for (int i = 0; i < TransparentPanels.Length; i++)
            {
                if (TransparentPanels[i] != null)
                {
                    _originalPanelColors[i] = TransparentPanels[i].color;

                    // Make fully transparent — the Blender artwork provides the visual frame
                    Color c = TransparentPanels[i].color;
                    c.a = 0f;
                    TransparentPanels[i].color = c;
                }
            }
        }

        private void RestorePanelColors()
        {
            if (TransparentPanels == null || _originalPanelColors == null) return;

            for (int i = 0; i < TransparentPanels.Length && i < _originalPanelColors.Length; i++)
            {
                if (TransparentPanels[i] != null)
                {
                    TransparentPanels[i].color = _originalPanelColors[i];
                }
            }
        }

        #endregion

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        #region Public API

        /// <summary>
        /// Toggle the skin on/off.
        /// </summary>
        public void ToggleSkin()
        {
            SkinEnabled = !SkinEnabled;

            if (SkinEnabled)
                ApplySkin();
            else
                RemoveSkin();
        }

        /// <summary>
        /// Manually set the panel texture at runtime.
        /// </summary>
        public void SetTexture(Texture2D texture)
        {
            PanelTexture = texture;
            _textureLoaded = texture != null;

            if (_backgroundImage != null)
            {
                _backgroundImage.texture = texture;
            }
        }

        #endregion
    }
}
