// ============================================================================
// CRITICAL: Master the Atom — Edgewise Meter POC Controller
// EdgewiseMeterPOCController.cs
// ============================================================================
//
// PURPOSE:
//   MonoBehaviour controller for the Edgewise Meter POC scene.
//   Wires up interactive slider control for testing meter behavior.
//
// VERSION: 0.1.0-POC
// DATE: 2026-02-18
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    /// <summary>
    /// Controller for the Edgewise Meter POC demonstration.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class EdgewiseMeterPOCController : MonoBehaviour
    {
        // ====================================================================
        // REFERENCES
        // ====================================================================
        
        private UIDocument m_Document;
        private VisualElement m_Root;
        
        // Interactive elements
        private VisualElement m_PointerH;
        private VisualElement m_PointerV;
        private Slider m_ValueSlider;
        private Label m_ValueDisplay;
        
        // Scale constants (matching USS)
        private const float SCALE_START_PCT = 8f;
        private const float SCALE_END_PCT = 92f;
        
        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================
        
        private void OnEnable()
        {
            m_Document = GetComponent<UIDocument>();
            if (m_Document == null || m_Document.rootVisualElement == null)
            {
                Debug.LogError("[EdgewiseMeterPOC] UIDocument or root is null");
                return;
            }
            
            m_Root = m_Document.rootVisualElement;
            
            // Query interactive elements
            m_PointerH = m_Root.Q<VisualElement>("pointer-interactive-h");
            m_PointerV = m_Root.Q<VisualElement>("pointer-interactive-v");
            m_ValueSlider = m_Root.Q<Slider>("value-slider");
            m_ValueDisplay = m_Root.Q<Label>("value-display");
            
            // Wire up slider
            if (m_ValueSlider != null)
            {
                m_ValueSlider.RegisterValueChangedCallback(OnSliderChanged);
            }
            
            Debug.Log("[EdgewiseMeterPOC] Initialized");
            LogStatus();
        }
        
        private void OnDisable()
        {
            if (m_ValueSlider != null)
            {
                m_ValueSlider.UnregisterValueChangedCallback(OnSliderChanged);
            }
        }
        
        // ====================================================================
        // EVENT HANDLERS
        // ====================================================================
        
        private void OnSliderChanged(ChangeEvent<float> evt)
        {
            float value = evt.newValue;
            
            // Update display
            if (m_ValueDisplay != null)
            {
                string sign = value >= 0 ? "+" : "";
                m_ValueDisplay.text = $"{sign}{value:F1}";
                
                // Color based on deviation
                if (Mathf.Abs(value) > 15f)
                    m_ValueDisplay.style.color = new Color(1f, 0.27f, 0.27f); // Red
                else if (Mathf.Abs(value) > 10f)
                    m_ValueDisplay.style.color = new Color(1f, 0.78f, 0f); // Amber
                else
                    m_ValueDisplay.style.color = new Color(0.18f, 0.85f, 0.25f); // Green
            }
            
            // Update horizontal pointer
            if (m_PointerH != null)
            {
                float normalizedH = (value - (-20f)) / 40f; // -20 to +20 range
                float positionPctH = SCALE_START_PCT + normalizedH * (SCALE_END_PCT - SCALE_START_PCT);
                m_PointerH.style.left = Length.Percent(positionPctH);
            }
            
            // Update vertical pointer (map -20/+20 to 0-100% for level display)
            if (m_PointerV != null)
            {
                float normalizedV = (value - (-20f)) / 40f;
                float positionPctV = SCALE_START_PCT + normalizedV * (SCALE_END_PCT - SCALE_START_PCT);
                m_PointerV.style.bottom = Length.Percent(positionPctV);
            }
        }
        
        // ====================================================================
        // DEBUG
        // ====================================================================
        
        private void LogStatus()
        {
            Debug.Log($"  - Pointer H: {(m_PointerH != null ? "OK" : "MISSING")}");
            Debug.Log($"  - Pointer V: {(m_PointerV != null ? "OK" : "MISSING")}");
            Debug.Log($"  - Slider: {(m_ValueSlider != null ? "OK" : "MISSING")}");
            Debug.Log($"  - Display: {(m_ValueDisplay != null ? "OK" : "MISSING")}");
        }
        
        // ====================================================================
        // CONTEXT MENU TESTS
        // ====================================================================
        
        [ContextMenu("Animate Sweep")]
        public void AnimateSweep()
        {
            StartCoroutine(SweepCoroutine());
        }
        
        private System.Collections.IEnumerator SweepCoroutine()
        {
            if (m_ValueSlider == null) yield break;
            
            float duration = 3f;
            
            // Sweep from min to max
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                m_ValueSlider.value = Mathf.Lerp(-20f, 20f, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Sweep back
            elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                m_ValueSlider.value = Mathf.Lerp(20f, -20f, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Return to center
            m_ValueSlider.value = 0f;
        }
        
        [ContextMenu("Simulate Deviation Alarm")]
        public void SimulateDeviationAlarm()
        {
            StartCoroutine(DeviationAlarmCoroutine());
        }
        
        private System.Collections.IEnumerator DeviationAlarmCoroutine()
        {
            if (m_ValueSlider == null) yield break;
            
            // Start at normal
            m_ValueSlider.value = 2f;
            yield return new WaitForSeconds(1f);
            
            // Drift into warning
            float target = 12f;
            float start = m_ValueSlider.value;
            float duration = 2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                m_ValueSlider.value = Mathf.Lerp(start, target, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // Spike into alarm
            target = 18f;
            start = m_ValueSlider.value;
            duration = 0.5f;
            elapsed = 0f;
            
            while (elapsed < duration)
            {
                m_ValueSlider.value = Mathf.Lerp(start, target, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(1f);
            
            // Recovery
            target = 0f;
            start = m_ValueSlider.value;
            duration = 3f;
            elapsed = 0f;
            
            while (elapsed < duration)
            {
                m_ValueSlider.value = Mathf.Lerp(start, target, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            m_ValueSlider.value = 0f;
        }
    }
}
