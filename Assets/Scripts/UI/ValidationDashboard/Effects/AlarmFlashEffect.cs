// ============================================================================
// CRITICAL: Master the Atom - Alarm Flash Effect
// AlarmFlashEffect.cs - Pulsing/Flashing Effect for Alarm States
// ============================================================================

using UnityEngine;
using UnityEngine.UI;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Flashing/pulsing effect for alarm indicators.
    /// </summary>
    public class AlarmFlashEffect : MonoBehaviour
    {
        [Header("Flash Settings")]
        [SerializeField] private float flashFrequency = 2f;
        [SerializeField] private float onDuration = 0.7f;
        [SerializeField] private Color alarmColor = new Color32(255, 46, 46, 255);
        [SerializeField] private Color offColor = new Color32(80, 30, 30, 255);

        [Header("Target")]
        [SerializeField] private Graphic targetGraphic;

        private bool _isActive;
        private float _timer;

        public bool IsActive => _isActive;

        void Update()
        {
            if (!_isActive || targetGraphic == null) return;

            _timer += Time.unscaledDeltaTime * flashFrequency;
            float phase = _timer % 1f;
            targetGraphic.color = phase < onDuration ? alarmColor : offColor;
        }

        public void Activate()
        {
            _isActive = true;
            _timer = 0f;
        }

        public void Deactivate()
        {
            _isActive = false;
            if (targetGraphic != null)
                targetGraphic.color = offColor;
        }

        public void SetColors(Color alarm, Color off)
        {
            alarmColor = alarm;
            offColor = off;
        }

        public static AlarmFlashEffect AddTo(Graphic target, float frequency = 2f)
        {
            AlarmFlashEffect effect = target.gameObject.AddComponent<AlarmFlashEffect>();
            effect.targetGraphic = target;
            effect.flashFrequency = frequency;
            return effect;
        }
    }
}
