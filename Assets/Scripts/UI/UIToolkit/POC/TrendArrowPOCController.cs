// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// TrendArrowPOCController.cs — Test harness for TrendArrowPOC
// ============================================================================
//
// PURPOSE:
//   Demonstrates the TrendArrowPOC element with an animated sinusoidal
//   rate value. Shows the arrow responding to positive, negative, and
//   near-zero rates with appropriate direction, size, and color changes.
//
// SETUP:
//   1. Create empty scene (or use existing POC scene)
//   2. GameObject → UI Toolkit → UI Document
//   3. Add this script to the UIDocument GameObject
//   4. Press Play
//
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    [RequireComponent(typeof(UIDocument))]
    public class TrendArrowPOCController : MonoBehaviour
    {
        // ====================================================================
        // INSPECTOR
        // ====================================================================

        [Header("Animation")]
        [SerializeField] private bool animateValue = true;
        [SerializeField] private float animationSpeed = 0.5f;

        [Tooltip("Peak magnitude of the animated sine wave")]
        [SerializeField] private float animationAmplitude = 120f;

        [Header("Arrow Configuration")]
        [SerializeField] private float maxMagnitude = 100f;
        [SerializeField] private float deadband = 0.5f;
        [SerializeField] private float elevatedThreshold = 50f;
        [SerializeField] private float alarmThreshold = 80f;

        // ====================================================================
        // PRIVATE FIELDS
        // ====================================================================

        private UIDocument _uiDocument;
        private TrendArrowPOC _arrow;
        private Label _valueLabel;
        private Label _directionLabel;
        private float _time;
        private bool _uiBuilt = false;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();

            if (_uiDocument == null)
            {
                Debug.LogError("[TrendArrowPOC] UIDocument component not found!");
                return;
            }

            if (_uiDocument.panelSettings == null)
            {
                Debug.LogError("[TrendArrowPOC] Panel Settings is NULL! Assign a Panel Settings asset.");
                return;
            }
        }

        private void Start()
        {
            BuildUI();
        }

        private void OnEnable()
        {
            if (!_uiBuilt && _uiDocument != null)
                BuildUI();
        }

        private void Update()
        {
            if (!animateValue || _arrow == null)
                return;

            _time += Time.deltaTime * animationSpeed;
            float rate = Mathf.Sin(_time) * animationAmplitude;

            _arrow.value = rate;

            if (_valueLabel != null)
                _valueLabel.text = $"{rate:+0.0;-0.0;0.0} psi/hr";

            if (_directionLabel != null)
            {
                string dir = Mathf.Abs(rate) <= deadband ? "STEADY" :
                             rate > 0 ? "RISING" : "FALLING";
                _directionLabel.text = dir;
            }
        }

        // ====================================================================
        // UI CONSTRUCTION
        // ====================================================================

        private void BuildUI()
        {
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[TrendArrowPOC] rootVisualElement is NULL! Assign Panel Settings.");
                return;
            }

            _uiBuilt = true;
            root.Clear();

            // Root styling
            root.style.flexGrow = 1;
            root.style.backgroundColor = new Color(0.05f, 0.05f, 0.1f, 1f);
            root.style.flexDirection = FlexDirection.Column;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;

            // Title
            var title = new Label("UI Toolkit Trend Arrow POC");
            title.style.fontSize = 24;
            title.style.color = Color.green;
            title.style.marginBottom = 30;
            root.Add(title);

            // Main demo area — arrow flanked by a mock gauge placeholder
            var demoRow = new VisualElement();
            demoRow.style.flexDirection = FlexDirection.Row;
            demoRow.style.alignItems = Align.Center;
            demoRow.style.justifyContent = Justify.Center;
            root.Add(demoRow);

            // Mock gauge placeholder (dark circle)
            var gaugePlaceholder = new VisualElement();
            gaugePlaceholder.style.width = 140;
            gaugePlaceholder.style.height = 140;
            gaugePlaceholder.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            gaugePlaceholder.style.borderTopLeftRadius = 70;
            gaugePlaceholder.style.borderTopRightRadius = 70;
            gaugePlaceholder.style.borderBottomLeftRadius = 70;
            gaugePlaceholder.style.borderBottomRightRadius = 70;
            demoRow.Add(gaugePlaceholder);

            var gaugeLabel = new Label("GAUGE");
            gaugeLabel.style.color = new Color(0.4f, 0.4f, 0.45f, 1f);
            gaugeLabel.style.fontSize = 12;
            gaugeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            gaugeLabel.style.marginTop = 55;
            gaugePlaceholder.Add(gaugeLabel);

            // Trend Arrow — the actual POC element
            var arrowContainer = new VisualElement();
            arrowContainer.style.width = 36;
            arrowContainer.style.height = 80;
            arrowContainer.style.marginLeft = 8;
            arrowContainer.style.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            arrowContainer.style.borderTopLeftRadius = 4;
            arrowContainer.style.borderTopRightRadius = 4;
            arrowContainer.style.borderBottomLeftRadius = 4;
            arrowContainer.style.borderBottomRightRadius = 4;
            demoRow.Add(arrowContainer);

            _arrow = new TrendArrowPOC();
            _arrow.style.width = new Length(100, LengthUnit.Percent);
            _arrow.style.height = new Length(100, LengthUnit.Percent);
            _arrow.maxMagnitude = maxMagnitude;
            _arrow.deadband = deadband;
            _arrow.elevatedThreshold = elevatedThreshold;
            _arrow.alarmThreshold = alarmThreshold;
            _arrow.value = 0f;
            arrowContainer.Add(_arrow);

            // Value readout
            _valueLabel = new Label("+0.0 psi/hr");
            _valueLabel.style.fontSize = 18;
            _valueLabel.style.color = Color.white;
            _valueLabel.style.marginTop = 20;
            _valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(_valueLabel);

            // Direction label
            _directionLabel = new Label("STEADY");
            _directionLabel.style.fontSize = 14;
            _directionLabel.style.color = new Color(0.6f, 0.6f, 0.65f, 1f);
            _directionLabel.style.marginTop = 8;
            _directionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(_directionLabel);

            // Size comparison row — show multiple arrows at different sizes
            var sizeTitle = new Label("Size Comparison (fixed values)");
            sizeTitle.style.fontSize = 14;
            sizeTitle.style.color = new Color(0.5f, 0.5f, 0.55f, 1f);
            sizeTitle.style.marginTop = 40;
            sizeTitle.style.marginBottom = 10;
            root.Add(sizeTitle);

            var compRow = new VisualElement();
            compRow.style.flexDirection = FlexDirection.Row;
            compRow.style.alignItems = Align.FlexEnd;
            compRow.style.justifyContent = Justify.Center;
            root.Add(compRow);

            // Static arrows at various magnitudes
            float[] sampleValues = { -100f, -60f, -20f, 0f, 20f, 60f, 100f };
            foreach (float sv in sampleValues)
            {
                var col = new VisualElement();
                col.style.alignItems = Align.Center;
                col.style.marginLeft = 6;
                col.style.marginRight = 6;
                compRow.Add(col);

                var sArrowContainer = new VisualElement();
                sArrowContainer.style.width = 28;
                sArrowContainer.style.height = 60;
                sArrowContainer.style.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
                sArrowContainer.style.borderTopLeftRadius = 3;
                sArrowContainer.style.borderTopRightRadius = 3;
                sArrowContainer.style.borderBottomLeftRadius = 3;
                sArrowContainer.style.borderBottomRightRadius = 3;
                col.Add(sArrowContainer);

                var sArrow = new TrendArrowPOC();
                sArrow.style.width = new Length(100, LengthUnit.Percent);
                sArrow.style.height = new Length(100, LengthUnit.Percent);
                sArrow.maxMagnitude = maxMagnitude;
                sArrow.deadband = deadband;
                sArrow.elevatedThreshold = elevatedThreshold;
                sArrow.alarmThreshold = alarmThreshold;
                sArrow.value = sv;
                sArrowContainer.Add(sArrow);

                var sLabel = new Label($"{sv:+0;-0;0}");
                sLabel.style.fontSize = 10;
                sLabel.style.color = new Color(0.5f, 0.5f, 0.55f, 1f);
                sLabel.style.marginTop = 4;
                sLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                col.Add(sLabel);
            }

            // Instructions
            var instructions = new Label("Arrow animates through full sine cycle — watch direction, size, and color");
            instructions.style.fontSize = 11;
            instructions.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            instructions.style.marginTop = 30;
            root.Add(instructions);

            Debug.Log("[TrendArrowPOC] UI build complete");
        }
    }
}
