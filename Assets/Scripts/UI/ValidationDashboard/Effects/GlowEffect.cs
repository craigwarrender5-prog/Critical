// ============================================================================
// CRITICAL: Master the Atom - Glow Effect Controller
// GlowEffect.cs - Runtime Glow/Bloom Effect for UI Elements
// ============================================================================

using UnityEngine;
using UnityEngine.UI;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Adds a soft glow effect behind UI elements using layered images.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class GlowEffect : MonoBehaviour
    {
        [Header("Glow Settings")]
        [SerializeField] private Color glowColor = new Color(0.2f, 0.85f, 0.25f, 0.4f);
        [SerializeField] private float glowSize = 8f;
        [SerializeField] private float glowIntensity = 0.5f;
        [SerializeField] private int glowLayers = 3;

        [Header("Animation")]
        [SerializeField] private bool enablePulse = false;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMinIntensity = 0.3f;
        [SerializeField] private float pulseMaxIntensity = 0.8f;

        private Image[] _glowImages;
        private RectTransform _rectTransform;
        private float _pulseTime;
        private float _currentIntensity;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            CreateGlowLayers();
        }

        void Update()
        {
            if (enablePulse)
            {
                _pulseTime += Time.unscaledDeltaTime * pulseSpeed;
                float pulse = (Mathf.Sin(_pulseTime * Mathf.PI * 2f) + 1f) * 0.5f;
                _currentIntensity = Mathf.Lerp(pulseMinIntensity, pulseMaxIntensity, pulse);
                UpdateGlowIntensity();
            }
        }

        private void CreateGlowLayers()
        {
            _glowImages = new Image[glowLayers];

            for (int i = 0; i < glowLayers; i++)
            {
                GameObject layerGO = new GameObject($"GlowLayer_{i}");
                layerGO.transform.SetParent(transform, false);
                layerGO.transform.SetAsFirstSibling();

                RectTransform layerRT = layerGO.AddComponent<RectTransform>();
                layerRT.anchorMin = Vector2.zero;
                layerRT.anchorMax = Vector2.one;

                float layerOffset = glowSize * (i + 1) / glowLayers;
                layerRT.offsetMin = new Vector2(-layerOffset, -layerOffset);
                layerRT.offsetMax = new Vector2(layerOffset, layerOffset);

                Image layerImg = layerGO.AddComponent<Image>();
                float layerAlpha = glowIntensity * (1f - (float)i / glowLayers);
                layerImg.color = new Color(glowColor.r, glowColor.g, glowColor.b, layerAlpha * glowColor.a);
                layerImg.raycastTarget = false;

                _glowImages[i] = layerImg;
            }
        }

        private void UpdateGlowIntensity()
        {
            if (_glowImages == null) return;

            for (int i = 0; i < _glowImages.Length; i++)
            {
                if (_glowImages[i] != null)
                {
                    float layerAlpha = _currentIntensity * (1f - (float)i / glowLayers);
                    _glowImages[i].color = new Color(glowColor.r, glowColor.g, glowColor.b, layerAlpha * glowColor.a);
                }
            }
        }

        public void SetColor(Color color)
        {
            glowColor = color;
            UpdateGlowIntensity();
        }

        public void SetIntensity(float intensity)
        {
            glowIntensity = intensity;
            _currentIntensity = intensity;
            UpdateGlowIntensity();
        }

        public void SetPulse(bool enable, float speed = 2f)
        {
            enablePulse = enable;
            pulseSpeed = speed;
            if (!enable) _currentIntensity = glowIntensity;
        }

        public static GlowEffect AddTo(GameObject target, Color color, float size = 8f, float intensity = 0.5f)
        {
            GlowEffect effect = target.AddComponent<GlowEffect>();
            effect.glowColor = color;
            effect.glowSize = size;
            effect.glowIntensity = intensity;
            return effect;
        }
    }
}
