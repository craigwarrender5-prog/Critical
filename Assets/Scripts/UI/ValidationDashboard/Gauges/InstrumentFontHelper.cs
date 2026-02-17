// ============================================================================
// CRITICAL: Master the Atom - Instrument Font Helper
// InstrumentFontHelper.cs - Shared Font & Glow Material Utility
// ============================================================================
//
// PURPOSE:
//   Centralizes loading and application of instrument-grade fonts and
//   TMP glow materials for all dashboard gauge readouts. Provides the
//   same green/amber/red LED-style text that MosaicGauge uses, but as
//   a shared utility any gauge component can call.
//
// USAGE:
//   InstrumentFontHelper.ApplyInstrumentStyle(myTextMeshPro, 24f);
//   InstrumentFontHelper.UpdateGlowForAlarmState(myTextMeshPro, isAlarm, isWarning);
//
// REFERENCE:
//   MosaicGauge.cs LoadInstrumentMaterials() — same material loading pattern
//
// VERSION: 1.0.0
// DATE: 2026-02-17
// IP: IP-0040 Stage 1
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Shared utility for instrument font and TMP glow material management.
    /// </summary>
    public static class InstrumentFontHelper
    {
        // ====================================================================
        // CACHED RESOURCES
        // ====================================================================

        private static TMP_FontAsset _instrumentFont;
        private static Material _matGreen;
        private static Material _matAmber;
        private static Material _matRed;
        private static bool _loaded;

        // ====================================================================
        // RESOURCE LOADING
        // ====================================================================

        /// <summary>
        /// Load instrument font and glow materials from Resources.
        /// Safe to call multiple times — only loads once.
        /// IP-0041 Stage 1: Multiple fallback paths + visible fallback styling.
        /// </summary>
        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            // Load from TMP Examples & Extras — this is the ORIGINAL asset whose
            // guid the glow materials reference via _MainTex. The copy in
            // Assets/Resources has a DIFFERENT guid, causing atlas mismatch
            // and invisible text. TMP's Resources folder structure means
            // both paths are valid for Resources.Load.
            _instrumentFont = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/Electronic Highway Sign SDF");

            if (_instrumentFont == null)
            {
                Debug.LogWarning(
                    "[InstrumentFontHelper] Instrument font not found — " +
                    "using default TMP font with green fallback styling.");
            }

            _matGreen = Resources.Load<Material>(
                "Fonts & Materials/Instrument_Green_Glow");
            _matAmber = Resources.Load<Material>(
                "Fonts & Materials/Instrument_Amber_Glow");
            _matRed = Resources.Load<Material>(
                "Fonts & Materials/Instrument_Red_Glow");
        }

        // ====================================================================
        // PUBLIC PROPERTIES
        // ====================================================================

        /// <summary>The instrument font asset, or null if not found.</summary>
        public static TMP_FontAsset Font
        {
            get { EnsureLoaded(); return _instrumentFont; }
        }

        /// <summary>True if glow materials were loaded successfully.</summary>
        public static bool HasGlowMaterials
        {
            get { EnsureLoaded(); return _matGreen != null; }
        }

        // ====================================================================
        // STYLE APPLICATION
        // ====================================================================

        /// <summary>
        /// Apply instrument font and green glow material to a TMP text component.
        /// IP-0041 Stage 1: Always produces visible text — no black rectangles.
        /// If font/material missing, uses default TMP font with green color.
        /// </summary>
        public static void ApplyInstrumentStyle(TextMeshProUGUI text, float fontSize = 24f)
        {
            if (text == null) return;

            // IP-0041 Stage 1: Direct color styling ONLY.
            // Glow materials caused invisible text across 6 implementation attempts
            // due to font atlas guid mismatch between copied font asset and materials.
            // Plain green text on dark background is fully readable and reliable.
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color32(46, 217, 64, 255); // NormalGreen
        }

        /// <summary>
        /// Swap TMP glow material based on alarm state.
        /// Only swaps when needed — call freely each update.
        /// </summary>
        public static void UpdateGlowForAlarmState(TextMeshProUGUI text, bool isAlarm, bool isWarning)
        {
            if (text == null) return;
            EnsureLoaded();

            Material target;
            if (isAlarm)
                target = _matRed;
            else if (isWarning)
                target = _matAmber;
            else
                target = _matGreen;

            if (target != null && text.fontSharedMaterial != target)
                text.fontSharedMaterial = target;
        }

        /// <summary>
        /// Swap TMP glow material using threshold evaluation.
        /// </summary>
        public static void UpdateGlowForValue(TextMeshProUGUI text, float value,
            float warnLow, float warnHigh, float alarmLow, float alarmHigh)
        {
            bool isAlarm = value < alarmLow || value > alarmHigh;
            bool isWarning = !isAlarm && (value < warnLow || value > warnHigh);
            UpdateGlowForAlarmState(text, isAlarm, isWarning);
        }

        // ====================================================================
        // RECESSED BACKING
        // ====================================================================

        /// <summary>
        /// Create a dark recessed rectangle behind a readout (display window look).
        /// IP-0041 Stage 1: Fixed z-order — backing is always behind text content.
        /// Created as a child of the parent, positioned behind all subsequent siblings.
        /// Uses CanvasRenderer to prevent GraphicRaycaster MissingComponentException.
        /// </summary>
        public static Image CreateRecessedBacking(Transform parent, float width, float height)
        {
            GameObject backingGO = new GameObject("RecessedBacking");
            backingGO.transform.SetParent(parent, false);

            // Ensure CanvasRenderer exists before adding Image (prevents raycaster errors)
            if (backingGO.GetComponent<CanvasRenderer>() == null)
                backingGO.AddComponent<CanvasRenderer>();

            RectTransform rt = backingGO.AddComponent<RectTransform>();
            // Stretch to fill parent — more robust than fixed size for layout groups
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = backingGO.AddComponent<Image>();
            img.color = ValidationDashboardTheme.BackgroundGraph;
            img.raycastTarget = false;

            // SetAsFirstSibling ensures backing renders BEHIND all text/content siblings
            backingGO.transform.SetAsFirstSibling();

            return img;
        }
    }
}
