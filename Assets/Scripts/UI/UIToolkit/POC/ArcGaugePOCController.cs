// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// ArcGaugePOCController.cs — SIMPLIFIED test harness with better diagnostics
// ============================================================================
//
// SETUP:
//   1. Create empty scene
//   2. GameObject → UI Toolkit → UI Document (this auto-creates Panel Settings)
//   3. Add this script to the UIDocument GameObject
//   4. Press Play
//
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    [RequireComponent(typeof(UIDocument))]
    public class ArcGaugePOCController : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private bool animateValue = true;
        [SerializeField] private float animationSpeed = 25f;
        
        private UIDocument _uiDocument;
        private ArcGaugePOC _gauge;
        private Label _valueLabel;
        
        private float _currentValue = 50f;
        private float _animDirection = 1f;
        private bool _uiBuilt = false;
        
        private void Awake()
        {
            Debug.Log("[POC] Awake called");
            _uiDocument = GetComponent<UIDocument>();
            
            if (_uiDocument == null)
            {
                Debug.LogError("[POC] UIDocument component not found!");
                return;
            }
            
            if (_uiDocument.panelSettings == null)
            {
                Debug.LogError("[POC] Panel Settings is NULL! Please assign a Panel Settings asset to the UIDocument component.");
                return;
            }
            
            Debug.Log($"[POC] UIDocument found. Panel Settings: {_uiDocument.panelSettings.name}");
        }
        
        private void Start()
        {
            Debug.Log("[POC] Start called - attempting to build UI");
            BuildUI();
        }
        
        private void OnEnable()
        {
            Debug.Log("[POC] OnEnable called");
            
            // Try building UI here too in case Start already ran
            if (!_uiBuilt && _uiDocument != null)
            {
                BuildUI();
            }
        }
        
        private void Update()
        {
            if (!animateValue || _gauge == null) 
                return;
            
            _currentValue += _animDirection * animationSpeed * Time.deltaTime;
            
            if (_currentValue >= 100f)
            {
                _currentValue = 100f;
                _animDirection = -1f;
            }
            else if (_currentValue <= 0f)
            {
                _currentValue = 0f;
                _animDirection = 1f;
            }
            
            _gauge.value = _currentValue;
            
            if (_valueLabel != null)
                _valueLabel.text = $"Value: {_currentValue:F1}";
        }
        
        private void BuildUI()
        {
            if (_uiDocument == null)
            {
                Debug.LogError("[POC] Cannot build UI - UIDocument is null");
                return;
            }
            
            var root = _uiDocument.rootVisualElement;
            
            if (root == null)
            {
                Debug.LogError("[POC] rootVisualElement is NULL!");
                Debug.LogError("[POC] This usually means Panel Settings is not assigned.");
                Debug.LogError("[POC] Select the UIDocument GameObject and assign a Panel Settings asset.");
                return;
            }
            
            Debug.Log($"[POC] rootVisualElement found. Building UI...");
            Debug.Log($"[POC] Root size: {root.resolvedStyle.width} x {root.resolvedStyle.height}");
            
            _uiBuilt = true;
            root.Clear();
            
            // Make the root fill the screen and be visible
            root.style.flexGrow = 1;
            root.style.backgroundColor = new Color(0.05f, 0.05f, 0.1f, 1f);
            root.style.flexDirection = FlexDirection.Column;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;
            
            // Title
            var title = new Label("UI Toolkit Arc Gauge POC");
            title.style.fontSize = 24;
            title.style.color = Color.green;
            title.style.marginBottom = 20;
            root.Add(title);
            Debug.Log("[POC] Added title");
            
            // Gauge container - fixed size
            var container = new VisualElement();
            container.name = "gauge-container";
            container.style.width = 200;
            container.style.height = 200;
            container.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            root.Add(container);
            Debug.Log("[POC] Added container");
            
            // Arc Gauge
            _gauge = new ArcGaugePOC();
            _gauge.name = "arc-gauge";
            _gauge.style.width = new Length(100, LengthUnit.Percent);
            _gauge.style.height = new Length(100, LengthUnit.Percent);
            _gauge.minValue = 0f;
            _gauge.maxValue = 100f;
            _gauge.value = 50f;
            container.Add(_gauge);
            Debug.Log("[POC] Added ArcGaugePOC");
            
            // Value label
            _valueLabel = new Label("Value: 50.0");
            _valueLabel.style.fontSize = 18;
            _valueLabel.style.color = Color.white;
            _valueLabel.style.marginTop = 20;
            root.Add(_valueLabel);
            Debug.Log("[POC] Added value label");
            
            // Instructions
            var instructions = new Label("Check Console for debug output");
            instructions.style.fontSize = 12;
            instructions.style.color = Color.gray;
            instructions.style.marginTop = 30;
            root.Add(instructions);
            
            Debug.Log("[POC] UI Build complete!");
        }
    }
}
