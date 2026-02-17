// ============================================================================
// CRITICAL: Master the Atom - Gauge Animator Utilities
// GaugeAnimator.cs - Shared Animation and Interpolation Functions
// ============================================================================
//
// PURPOSE:
//   Provides shared animation utilities for all gauge components:
//   - Various easing functions (EaseOut, EaseIn, EaseInOut)
//   - Color interpolation with smooth transitions
//   - Value smoothing with configurable parameters
//   - Pulse/glow animation helpers
//
// USAGE:
//   Use static methods from GaugeAnimator for consistent animation behavior
//   across all gauge types. All gauges should use these shared utilities
//   rather than implementing their own animation logic.
//
// VERSION: 1.0.0
// DATE: 2026-02-16
// IP: IP-0031 Stage 2
// ============================================================================

using UnityEngine;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Static utility class providing animation functions for gauges.
    /// </summary>
    public static class GaugeAnimator
    {
        // ====================================================================
        // EASING FUNCTIONS
        // ====================================================================

        /// <summary>
        /// Quadratic ease-out: decelerating to zero velocity.
        /// </summary>
        public static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        /// <summary>
        /// Quadratic ease-in: accelerating from zero velocity.
        /// </summary>
        public static float EaseInQuad(float t)
        {
            return t * t;
        }

        /// <summary>
        /// Quadratic ease-in-out: acceleration until halfway, then deceleration.
        /// </summary>
        public static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        /// <summary>
        /// Cubic ease-out: stronger deceleration.
        /// </summary>
        public static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        /// <summary>
        /// Cubic ease-in: stronger acceleration.
        /// </summary>
        public static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        /// <summary>
        /// Cubic ease-in-out.
        /// </summary>
        public static float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        /// <summary>
        /// Sine ease-out: smooth sinusoidal deceleration.
        /// </summary>
        public static float EaseOutSine(float t)
        {
            return Mathf.Sin(t * Mathf.PI * 0.5f);
        }

        /// <summary>
        /// Sine ease-in-out: smooth sinusoidal acceleration/deceleration.
        /// </summary>
        public static float EaseInOutSine(float t)
        {
            return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
        }

        /// <summary>
        /// Exponential ease-out: very fast deceleration.
        /// </summary>
        public static float EaseOutExpo(float t)
        {
            return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
        }

        /// <summary>
        /// Spring-like overshoot ease-out.
        /// </summary>
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        // ====================================================================
        // VALUE INTERPOLATION
        // ====================================================================

        /// <summary>
        /// Smooth interpolation toward target with velocity tracking.
        /// More controlled than SmoothDamp, allows custom easing.
        /// </summary>
        public static float SmoothApproach(float current, float target, ref float velocity,
            float smoothTime, float deltaTime, float maxSpeed = float.MaxValue)
        {
            if (smoothTime <= 0f)
            {
                velocity = 0f;
                return target;
            }

            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float change = current - target;
            float originalTo = target;

            // Clamp maximum speed
            float maxChange = maxSpeed * smoothTime;
            change = Mathf.Clamp(change, -maxChange, maxChange);
            target = current - change;

            float temp = (velocity + omega * change) * deltaTime;
            velocity = (velocity - omega * temp) * exp;
            float output = target + (change + temp) * exp;

            // Prevent overshooting
            if ((originalTo - current > 0f) == (output > originalTo))
            {
                output = originalTo;
                velocity = (output - originalTo) / deltaTime;
            }

            return output;
        }

        /// <summary>
        /// Simple lerp with deltaTime-based smoothing.
        /// </summary>
        public static float SmoothLerp(float current, float target, float speed, float deltaTime)
        {
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * deltaTime));
        }

        /// <summary>
        /// Clamp value change per frame to prevent sudden jumps.
        /// </summary>
        public static float ClampedApproach(float current, float target, float maxDelta)
        {
            float delta = target - current;
            if (Mathf.Abs(delta) <= maxDelta)
                return target;
            return current + Mathf.Sign(delta) * maxDelta;
        }

        // ====================================================================
        // COLOR INTERPOLATION
        // ====================================================================

        /// <summary>
        /// Smooth color transition with deltaTime-based interpolation.
        /// </summary>
        public static Color SmoothColorLerp(Color current, Color target, float speed, float deltaTime)
        {
            float t = 1f - Mathf.Exp(-speed * deltaTime);
            return Color.Lerp(current, target, t);
        }

        /// <summary>
        /// Smooth color transition with easing function.
        /// </summary>
        public static Color EasedColorLerp(Color from, Color to, float t, System.Func<float, float> easeFunc)
        {
            float easedT = easeFunc(Mathf.Clamp01(t));
            return Color.Lerp(from, to, easedT);
        }

        /// <summary>
        /// Get interpolated color from a gradient based on normalized value.
        /// </summary>
        public static Color GetGradientColor(float normalizedValue, Color lowColor, Color midColor, Color highColor)
        {
            normalizedValue = Mathf.Clamp01(normalizedValue);
            
            if (normalizedValue < 0.5f)
            {
                return Color.Lerp(lowColor, midColor, normalizedValue * 2f);
            }
            else
            {
                return Color.Lerp(midColor, highColor, (normalizedValue - 0.5f) * 2f);
            }
        }

        // ====================================================================
        // PULSE / GLOW ANIMATION
        // ====================================================================

        /// <summary>
        /// Calculate pulse intensity (0-1) for pulsing animations.
        /// </summary>
        public static float GetPulseIntensity(float time, float frequency, float minIntensity = 0.3f, float maxIntensity = 1f)
        {
            float sin = Mathf.Sin(time * frequency * Mathf.PI * 2f);
            float normalized = (sin + 1f) * 0.5f; // Map -1..1 to 0..1
            return Mathf.Lerp(minIntensity, maxIntensity, normalized);
        }

        /// <summary>
        /// Calculate breathing/glow intensity with smooth ease-in-out.
        /// </summary>
        public static float GetBreathingIntensity(float time, float cycleDuration, float minIntensity = 0.4f, float maxIntensity = 0.9f)
        {
            float t = (time % cycleDuration) / cycleDuration;
            float eased = EaseInOutSine(t);
            // Create a full cycle (up then down)
            float cycleT = t < 0.5f ? t * 2f : (1f - t) * 2f;
            float intensity = EaseInOutSine(cycleT);
            return Mathf.Lerp(minIntensity, maxIntensity, intensity);
        }

        /// <summary>
        /// Calculate flash intensity for alarm states (sharp on/off).
        /// </summary>
        public static float GetFlashIntensity(float time, float frequency, float onDuration = 0.7f)
        {
            float period = 1f / frequency;
            float phaseInPeriod = (time % period) / period;
            return phaseInPeriod < onDuration ? 1f : 0.3f;
        }

        // ====================================================================
        // ANGLE INTERPOLATION
        // ====================================================================

        /// <summary>
        /// Smoothly interpolate angles, handling wraparound.
        /// </summary>
        public static float SmoothAngle(float current, float target, ref float velocity, float smoothTime, float deltaTime)
        {
            // Normalize angles to -180..180
            float diff = Mathf.DeltaAngle(current, target);
            float adjustedTarget = current + diff;
            return Mathf.SmoothDamp(current, adjustedTarget, ref velocity, smoothTime, float.MaxValue, deltaTime);
        }

        /// <summary>
        /// Lerp angle with shortest path.
        /// </summary>
        public static float LerpAngle(float current, float target, float t)
        {
            float diff = Mathf.DeltaAngle(current, target);
            return current + diff * t;
        }

        // ====================================================================
        // THRESHOLD UTILITIES
        // ====================================================================

        /// <summary>
        /// Get normalized position within thresholds for gradient coloring.
        /// Returns 0 at alarm low, 0.5 at normal, 1 at alarm high.
        /// </summary>
        public static float GetThresholdPosition(float value, float alarmLow, float warningLow,
            float warningHigh, float alarmHigh)
        {
            if (value <= alarmLow) return 0f;
            if (value >= alarmHigh) return 1f;
            
            if (value <= warningLow)
            {
                // Between alarm low and warning low (0 to 0.25)
                float t = (value - alarmLow) / (warningLow - alarmLow);
                return t * 0.25f;
            }
            else if (value <= warningHigh)
            {
                // Normal range (0.25 to 0.75)
                float t = (value - warningLow) / (warningHigh - warningLow);
                return 0.25f + t * 0.5f;
            }
            else
            {
                // Between warning high and alarm high (0.75 to 1)
                float t = (value - warningHigh) / (alarmHigh - warningHigh);
                return 0.75f + t * 0.25f;
            }
        }

        /// <summary>
        /// Determine if value is in alarm, warning, or normal zone.
        /// Returns: -2 = low alarm, -1 = low warning, 0 = normal, 1 = high warning, 2 = high alarm
        /// </summary>
        public static int GetThresholdZone(float value, float alarmLow, float warningLow,
            float warningHigh, float alarmHigh)
        {
            if (value <= alarmLow) return -2;
            if (value <= warningLow) return -1;
            if (value >= alarmHigh) return 2;
            if (value >= warningHigh) return 1;
            return 0;
        }
    }
}
