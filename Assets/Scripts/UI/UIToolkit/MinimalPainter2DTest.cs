// ============================================================================
// CRITICAL: Minimal UI Toolkit Test
// MinimalPainter2DTest.cs — Just draws a simple shape to verify Painter2D works
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.UIToolkit
{
    /// <summary>
    /// Minimal test - draws directly on root to verify Painter2D works in Unity 6.
    /// Add this to the same GameObject as UIDocument.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MinimalPainter2DTest : MonoBehaviour
    {
        void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null || doc.rootVisualElement == null)
            {
                Debug.LogError("[MinimalTest] No UIDocument or root element!");
                return;
            }
            
            // Create a simple test element
            var testElement = new TestDrawingElement();
            testElement.style.width = 300;
            testElement.style.height = 200;
            testElement.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            testElement.style.marginTop = 50;
            testElement.style.marginLeft = 50;
            
            doc.rootVisualElement.Add(testElement);
            
            Debug.Log("[MinimalTest] Added TestDrawingElement to root");
        }
    }
    
    /// <summary>
    /// Simple element that draws a colored arc and line.
    /// </summary>
    public class TestDrawingElement : VisualElement
    {
        public TestDrawingElement()
        {
            generateVisualContent += OnGenerateVisualContent;
            Debug.Log("[TestDrawingElement] Constructor called, generateVisualContent registered");
        }
        
        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            float w = contentRect.width;
            float h = contentRect.height;
            
            Debug.Log($"[TestDrawingElement] OnGenerateVisualContent called! Size: {w} x {h}");
            
            if (w < 1 || h < 1)
            {
                Debug.LogWarning("[TestDrawingElement] Content rect too small, skipping draw");
                return;
            }
            
            var painter = mgc.painter2D;
            
            // Draw a green arc
            painter.strokeColor = Color.green;
            painter.lineWidth = 8f;
            painter.lineCap = LineCap.Round;
            
            float cx = w / 2f;
            float cy = h * 0.6f;
            float radius = Mathf.Min(w, h) * 0.35f;
            
            painter.BeginPath();
            painter.Arc(new Vector2(cx, cy), radius, Mathf.PI, 0f, ArcDirection.CounterClockwise);
            painter.Stroke();
            
            Debug.Log($"[TestDrawingElement] Drew arc at ({cx}, {cy}) radius {radius}");
            
            // Draw a white needle line
            painter.strokeColor = Color.white;
            painter.lineWidth = 4f;
            
            painter.BeginPath();
            painter.MoveTo(new Vector2(cx, cy));
            painter.LineTo(new Vector2(cx + radius * 0.7f, cy - radius * 0.5f));
            painter.Stroke();
            
            // Draw a red circle
            painter.fillColor = Color.red;
            painter.BeginPath();
            painter.Arc(new Vector2(cx, cy), 10f, 0f, Mathf.PI * 2f, ArcDirection.Clockwise);
            painter.ClosePath();
            painter.Fill();
            
            Debug.Log("[TestDrawingElement] Drawing complete!");
        }
    }
}
