// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard
// UITKDashboardSceneSetup.cs — Scene Integration Helper
// ============================================================================
//
// PURPOSE:
//   Editor and runtime helper to properly configure a scene for the
//   UI Toolkit Validation Dashboard. Creates necessary GameObjects,
//   UIDocument, Panel Settings, and links everything together.
//
// USAGE:
//   1. Add this component to a GameObject in your scene
//   2. Click "Setup Dashboard" in the Inspector (Editor) or call Setup()
//   3. The dashboard will be configured and ready to use
//
// VERSION: 1.0.0
// DATE: 2026-02-18
// CS: CS-0127 Stage 1
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Critical.UI.UIToolkit.ValidationDashboard
{
    /// <summary>
    /// Helper script to set up the UI Toolkit Validation Dashboard in a scene.
    /// </summary>
    public class UITKDashboardSceneSetup : MonoBehaviour
    {
        [Header("Assets")]
        [Tooltip("Panel Settings asset for the dashboard")]
        [SerializeField] private PanelSettings panelSettings;
        
        [Tooltip("UXML source asset for the dashboard")]
        [SerializeField] private VisualTreeAsset sourceAsset;
        
        [Tooltip("USS stylesheet for the dashboard")]
        [SerializeField] private StyleSheet styleSheet;
        
        [Header("Runtime")]
        [Tooltip("Start visible when scene loads")]
        [SerializeField] private bool startVisible = true;
        
        [Tooltip("Sort order for the UIDocument")]
        [SerializeField] private float sortOrder = 100f;
        
        // Created components
        private UIDocument _uiDocument;
        private UITKDashboardController _controller;
        
        /// <summary>
        /// Reference to the created UIDocument.
        /// </summary>
        public UIDocument UIDocument => _uiDocument;
        
        /// <summary>
        /// Reference to the dashboard controller.
        /// </summary>
        public UITKDashboardController Controller => _controller;
        
        void Awake()
        {
            Setup();
        }
        
        /// <summary>
        /// Set up the dashboard in the current scene.
        /// </summary>
        public void Setup()
        {
            // Check for existing UIDocument
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                _uiDocument = gameObject.AddComponent<UIDocument>();
            }
            
            // Configure UIDocument
            if (panelSettings != null)
            {
                _uiDocument.panelSettings = panelSettings;
            }
            else
            {
                Debug.LogWarning("[UITKDashboardSetup] No PanelSettings assigned - dashboard may not render correctly");
            }
            
            if (sourceAsset != null)
            {
                _uiDocument.visualTreeAsset = sourceAsset;
            }
            else
            {
                Debug.LogWarning("[UITKDashboardSetup] No VisualTreeAsset assigned - using programmatic layout only");
            }
            
            _uiDocument.sortingOrder = sortOrder;
            
            // Apply stylesheet if provided
            if (styleSheet != null && _uiDocument.rootVisualElement != null)
            {
                _uiDocument.rootVisualElement.styleSheets.Add(styleSheet);
            }
            
            // Add controller
            _controller = GetComponent<UITKDashboardController>();
            if (_controller == null)
            {
                _controller = gameObject.AddComponent<UITKDashboardController>();
            }
            
            // Set initial visibility
            if (!startVisible && _uiDocument.rootVisualElement != null)
            {
                _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
            }
            
            Debug.Log("[UITKDashboardSetup] Dashboard setup complete");
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Editor button to set up the dashboard.
        /// </summary>
        [ContextMenu("Setup Dashboard")]
        public void EditorSetup()
        {
            Setup();
            EditorUtility.SetDirty(gameObject);
        }
        
        /// <summary>
        /// Create Panel Settings asset if needed.
        /// </summary>
        [ContextMenu("Create Panel Settings")]
        public void CreatePanelSettings()
        {
            string path = "Assets/UI/UIToolkit/ValidationDashboard/ValidationDashboard_PanelSettings.asset";
            
            // Check if exists
            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (existing != null)
            {
                panelSettings = existing;
                Debug.Log("[UITKDashboardSetup] Using existing Panel Settings");
                return;
            }
            
            // Create new
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.match = 0.5f;
            
            // Ensure directory exists
            string dir = System.IO.Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
            
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            
            panelSettings = settings;
            Debug.Log($"[UITKDashboardSetup] Created Panel Settings at {path}");
        }
#endif
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// Custom editor for the setup script.
    /// </summary>
    [CustomEditor(typeof(UITKDashboardSceneSetup))]
    public class UITKDashboardSceneSetupEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            var setup = (UITKDashboardSceneSetup)target;
            
            if (GUILayout.Button("Setup Dashboard", GUILayout.Height(30)))
            {
                setup.EditorSetup();
            }
            
            if (GUILayout.Button("Create Panel Settings Asset"))
            {
                setup.CreatePanelSettings();
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "1. Assign Panel Settings (or click 'Create Panel Settings')\n" +
                "2. Assign the UXML and USS assets\n" +
                "3. Click 'Setup Dashboard'\n" +
                "4. Enter Play Mode to test",
                MessageType.Info);
        }
    }
#endif
}
