// ============================================================================
// CRITICAL: Master the Atom — Analog Gauge POC Controller
// AnalogGaugePOCController.cs
// ============================================================================
//
// PURPOSE:
//   MonoBehaviour controller for the Analog Gauge POC scene.
//   Wires up the UI Toolkit demo with interactive slider control.
//
// USAGE:
//   Attach to a GameObject with a UIDocument component.
//   Set the UIDocument's source asset to AnalogGaugeDemo.uxml.
//
// VERSION: 0.1.0-POC
// DATE: 2026-02-18
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;
using Critical.UI.Elements;

namespace Critical.UI.POC
{
    /// <summary>
    /// Controller for the Analog Gauge POC demonstration.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class AnalogGaugePOCController : MonoBehaviour
    {
        // ====================================================================
        // REFERENCES
        // ====================================================================
        
        private UIDocument m_Document;
        private VisualElement m_Root;
        
        // Gauges
        private AnalogGaugeElement m_GaugeSmall;
        private AnalogGaugeElement m_GaugeStandard;
        private AnalogGaugeElement m_GaugeLarge;
        private AnalogGaugeElement m_GaugeCelsius;
        private AnalogGaugeElement m_GaugePressure;
        
        // Controls
        private Slider m_TempSlider;
        private Label m_TempValueLabel;
        
        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================
        
        private void OnEnable()
        {
            m_Document = GetComponent<UIDocument>();
            if (m_Document == null || m_Document.rootVisualElement == null)
            {
                Debug.LogError("[AnalogGaugePOC] UIDocument or root is null");
                return;
            }
            
            m_Root = m_Document.rootVisualElement;
            
            // Query gauge elements
            m_GaugeSmall = m_Root.Q<AnalogGaugeElement>("gauge-small");
            m_GaugeStandard = m_Root.Q<AnalogGaugeElement>("gauge-standard");
            m_GaugeLarge = m_Root.Q<AnalogGaugeElement>("gauge-large");
            m_GaugeCelsius = m_Root.Q<AnalogGaugeElement>("gauge-celsius");
            m_GaugePressure = m_Root.Q<AnalogGaugeElement>("gauge-pressure");
            
            // Query controls
            m_TempSlider = m_Root.Q<Slider>("temp-slider");
            m_TempValueLabel = m_Root.Q<Label>("temp-value");
            
            // Wire up slider
            if (m_TempSlider != null)
            {
                m_TempSlider.RegisterValueChangedCallback(OnSliderChanged);
            }
            
            LogStatus();
        }
        
        private void OnDisable()
        {
            if (m_TempSlider != null)
            {
                m_TempSlider.UnregisterValueChangedCallback(OnSliderChanged);
            }
        }
        
        // ====================================================================
        // EVENT HANDLERS
        // ====================================================================
        
        private void OnSliderChanged(ChangeEvent<float> evt)
        {
            float temp = evt.newValue;
            
            // Update all Fahrenheit gauges
            if (m_GaugeSmall != null) m_GaugeSmall.Value = temp;
            if (m_GaugeStandard != null) m_GaugeStandard.Value = temp;
            if (m_GaugeLarge != null) m_GaugeLarge.Value = temp;
            
            // Update Celsius gauge (convert F to C)
            if (m_GaugeCelsius != null)
            {
                float celsius = (temp - 32f) * 5f / 9f;
                m_GaugeCelsius.Value = Mathf.Clamp(celsius, 0f, 120f);
            }
            
            // Update value label
            if (m_TempValueLabel != null)
            {
                m_TempValueLabel.text = $"{temp:F1} °F";
            }
        }
        
        // ====================================================================
        // DEBUG
        // ====================================================================
        
        private void LogStatus()
        {
            Debug.Log($"[AnalogGaugePOC] Initialized");
            Debug.Log($"  - Gauge Small: {(m_GaugeSmall != null ? "OK" : "MISSING")}");
            Debug.Log($"  - Gauge Standard: {(m_GaugeStandard != null ? "OK" : "MISSING")}");
            Debug.Log($"  - Gauge Large: {(m_GaugeLarge != null ? "OK" : "MISSING")}");
            Debug.Log($"  - Gauge Celsius: {(m_GaugeCelsius != null ? "OK" : "MISSING")}");
            Debug.Log($"  - Gauge Pressure: {(m_GaugePressure != null ? "OK" : "MISSING")}");
            Debug.Log($"  - Slider: {(m_TempSlider != null ? "OK" : "MISSING")}");
        }
        
        // ====================================================================
        // PUBLIC API (for testing from Inspector or other scripts)
        // ====================================================================
        
        /// <summary>
        /// Set the temperature on all gauges.
        /// </summary>
        public void SetTemperature(float fahrenheit)
        {
            if (m_TempSlider != null)
            {
                m_TempSlider.value = fahrenheit;
            }
        }
        
        /// <summary>
        /// Animate temperature sweep for testing.
        /// </summary>
        [ContextMenu("Animate Temperature Sweep")]
        public void AnimateTemperatureSweep()
        {
            StartCoroutine(TemperatureSweepCoroutine());
        }
        
        private System.Collections.IEnumerator TemperatureSweepCoroutine()
        {
            float duration = 5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float temp = Mathf.Lerp(0f, 250f, t);
                SetTemperature(temp);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Sweep back down
            elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float temp = Mathf.Lerp(250f, 0f, t);
                SetTemperature(temp);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
