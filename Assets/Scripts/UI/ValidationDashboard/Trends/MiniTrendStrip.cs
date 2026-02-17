// ============================================================================
// CRITICAL: Master the Atom - Mini Trend Strip
// MiniTrendStrip.cs - Compact Sparkline-Style Trend Display
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI.ValidationDashboard
{
    public class MiniTrendStrip : MaskableGraphic
    {
        [Header("Data")]
        [SerializeField] private int bufferSize = 120;
        [SerializeField] private Color lineColor = new Color32(46, 217, 64, 255);
        [SerializeField] private float lineWidth = 1.5f;

        [Header("Display")]
        [SerializeField] private bool showValue = true;
        [SerializeField] private string valueFormat = "F1";
        [SerializeField] private string unitSuffix = "";

        private TrendBuffer _buffer;
        private TextMeshProUGUI _valueText;
        private TextMeshProUGUI _labelText;
        private string _label;

        public TrendBuffer Buffer => _buffer;
        public string Label { get => _label; set { _label = value; if (_labelText != null) _labelText.text = value; } }

        protected override void Awake()
        {
            base.Awake();
            _buffer = new TrendBuffer(bufferSize);
        }

        public void AddDataPoint(float time, float value)
        {
            _buffer.Add(time, value);
            SetVerticesDirty();
            UpdateValueDisplay();
        }

        public void SetRange(float min, float max)
        {
            _buffer.SetRange(min, max);
            SetVerticesDirty();
        }

        public void SetColor(Color color)
        {
            lineColor = color;
            SetVerticesDirty();
        }

        public void SetValueText(TextMeshProUGUI text) => _valueText = text;
        public void SetLabelText(TextMeshProUGUI text) { _labelText = text; if (text != null && !string.IsNullOrEmpty(_label)) text.text = _label; }

        private void UpdateValueDisplay()
        {
            if (_valueText != null && _buffer.Count > 0)
            {
                _valueText.text = _buffer.GetLatest().ToString(valueFormat) + unitSuffix;
                _valueText.color = lineColor;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_buffer == null || _buffer.Count < 2) return;

            Rect rect = GetPixelAdjustedRect();
            int count = _buffer.Count;
            float xStep = rect.width / (bufferSize - 1);

            // Draw line segments
            for (int i = 1; i < count; i++)
            {
                float x0 = rect.xMin + (i - 1) * xStep;
                float x1 = rect.xMin + i * xStep;
                float y0 = rect.yMin + _buffer.GetNormalizedValue(i - 1) * rect.height;
                float y1 = rect.yMin + _buffer.GetNormalizedValue(i) * rect.height;

                DrawLineSegment(vh, new Vector2(x0, y0), new Vector2(x1, y1), lineWidth, lineColor);
            }
        }

        private void DrawLineSegment(VertexHelper vh, Vector2 p0, Vector2 p1, float width, Color c)
        {
            Vector2 dir = (p1 - p0).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x) * width * 0.5f;

            int idx = vh.currentVertCount;
            UIVertex v = UIVertex.simpleVert;
            v.color = c;

            v.position = p0 - perp; vh.AddVert(v);
            v.position = p0 + perp; vh.AddVert(v);
            v.position = p1 + perp; vh.AddVert(v);
            v.position = p1 - perp; vh.AddVert(v);

            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 2, idx + 3);
        }

        public static MiniTrendStrip Create(Transform parent, string label, float min, float max, Color color, string unit = "", string format = "F1")
        {
            GameObject container = new GameObject($"Trend_{label.Replace(" ", "")}");
            container.transform.SetParent(parent, false);

            LayoutElement le = container.AddComponent<LayoutElement>();
            le.preferredHeight = ValidationDashboardTheme.MiniTrendHeight + 16f;
            le.minHeight = ValidationDashboardTheme.MiniTrendHeight + 16f;

            Image bg = container.AddComponent<Image>();
            bg.color = ValidationDashboardTheme.BackgroundGraph;

            // Label at top
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(container.transform, false);
            RectTransform labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0, 1);
            labelRT.anchorMax = new Vector2(0.6f, 1);
            labelRT.pivot = new Vector2(0, 1);
            labelRT.sizeDelta = new Vector2(0, 12);
            labelRT.anchoredPosition = new Vector2(2, -1);

            TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 9;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            labelTMP.color = ValidationDashboardTheme.TextSecondary;

            // Value at top right
            GameObject valueGO = new GameObject("Value");
            valueGO.transform.SetParent(container.transform, false);
            RectTransform valueRT = valueGO.AddComponent<RectTransform>();
            valueRT.anchorMin = new Vector2(0.6f, 1);
            valueRT.anchorMax = new Vector2(1, 1);
            valueRT.pivot = new Vector2(1, 1);
            valueRT.sizeDelta = new Vector2(0, 12);
            valueRT.anchoredPosition = new Vector2(-2, -1);

            TextMeshProUGUI valueTMP = valueGO.AddComponent<TextMeshProUGUI>();
            valueTMP.text = "---";
            valueTMP.fontSize = 9;
            valueTMP.fontStyle = FontStyles.Bold;
            valueTMP.alignment = TextAlignmentOptions.MidlineRight;
            valueTMP.color = color;

            // Trend graph area
            GameObject graphGO = new GameObject("Graph");
            graphGO.transform.SetParent(container.transform, false);
            RectTransform graphRT = graphGO.AddComponent<RectTransform>();
            graphRT.anchorMin = Vector2.zero;
            graphRT.anchorMax = Vector2.one;
            graphRT.offsetMin = new Vector2(2, 2);
            graphRT.offsetMax = new Vector2(-2, -14);

            // Ensure CanvasRenderer exists before MiniTrendStrip (MaskableGraphic).
            // Unity's [RequireComponent] should add it, but GraphicRaycaster can
            // race the registration and throw MissingComponentException.
            if (graphGO.GetComponent<CanvasRenderer>() == null)
                graphGO.AddComponent<CanvasRenderer>();

            MiniTrendStrip strip = graphGO.AddComponent<MiniTrendStrip>();
            strip.lineColor = color;
            strip.valueFormat = format;
            strip.unitSuffix = unit;
            strip._label = label;
            strip.SetValueText(valueTMP);
            strip.SetLabelText(labelTMP);
            strip.SetRange(min, max);

            return strip;
        }
    }
}
