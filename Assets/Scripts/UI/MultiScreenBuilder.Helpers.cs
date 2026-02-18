// CRITICAL: Master the Atom - Multi-Screen Builder
// MultiScreenBuilder.Helpers.cs
//
// File: Assets/Scripts/UI/MultiScreenBuilder.Helpers.cs
// Module: Critical.UI.MultiScreenBuilder
// Responsibility: Shared UI helper construction methods used across screens.
// Standards: GOLD v1.0, SRP/SOLID
// Version: 1.0
// Last Updated: 2026-02-18

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace Critical.UI
{
    public partial class MultiScreenBuilder : MonoBehaviour
    {
        #region Helper Methods — Panels

        private static GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject panelGO = new GameObject(name);
            panelGO.transform.SetParent(parent, false);

            RectTransform rect = panelGO.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(2f, 2f);
            rect.offsetMax = new Vector2(-2f, -2f);

            Image image = panelGO.AddComponent<Image>();
            image.color = color;

            return panelGO;
        }

        #endregion

        // ====================================================================
        // SHARED HELPER METHODS — Legacy UI (for Screen 1 / MosaicGauge compat)
        // ====================================================================

        #region Helper Methods — Legacy UI

        /// <summary>
        /// Create a MosaicGauge with legacy Text elements (Screen 1 compatibility).
        /// </summary>
        private static MosaicGauge CreateMosaicGauge(Transform parent, string name, GaugeType type)
        {
            GameObject gaugeGO = new GameObject(name);
            gaugeGO.transform.SetParent(parent, false);

            gaugeGO.AddComponent<RectTransform>();

            Image bg = gaugeGO.AddComponent<Image>();
            bg.color = COLOR_GAUGE_BG;

            MosaicGauge gauge = gaugeGO.AddComponent<MosaicGauge>();
            gauge.Type = type;
            gauge.ShowAnalog = false;
            gauge.ShowDigital = true;
            gauge.Background = bg;

            // Value text (legacy Text for MosaicGauge compatibility)
            GameObject valueGO = new GameObject("Value");
            valueGO.transform.SetParent(gaugeGO.transform, false);

            RectTransform valueRect = valueGO.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0f, 0.35f);
            valueRect.anchorMax = new Vector2(1f, 0.85f);
            valueRect.offsetMin = new Vector2(4f, 0f);
            valueRect.offsetMax = new Vector2(-4f, 0f);

            TextMeshProUGUI valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.fontSize = 16;
            valueText.fontStyle = FontStyles.Bold;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = COLOR_GREEN;
            gauge.ValueText = valueText;

            // Label text
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(gaugeGO.transform, false);

            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0.35f);
            labelRect.offsetMin = new Vector2(2f, 0f);
            labelRect.offsetMax = new Vector2(-2f, 0f);

            TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.fontSize = 9;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = COLOR_TEXT_LABEL;
            gauge.LabelText = labelText;

            return gauge;
        }

        /// <summary>
        /// Create a legacy UI Button (for Screen 1 components using UnityEngine.UI.Text).
        /// </summary>
        private static Button CreateLegacyButton(Transform parent, string name, string text)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);

            btnGO.AddComponent<RectTransform>();

            Image bg = btnGO.AddComponent<Image>();
            bg.color = COLOR_BUTTON;

            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock colors = btn.colors;
            colors.normalColor = COLOR_BUTTON;
            colors.highlightedColor = COLOR_BUTTON_HOVER;
            colors.pressedColor = COLOR_CYAN;
            btn.colors = colors;

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(btnGO.transform, false);

            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelGO.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 10;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = COLOR_TEXT_NORMAL;

            return btn;
        }

        /// <summary>
        /// Create a legacy Button with explicit anchors.
        /// </summary>
        private static Button CreateLegacyButton(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            Button btn = CreateLegacyButton(parent, name, text);
            RectTransform rect = btn.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return btn;
        }

        /// <summary>
        /// Create a legacy section label at the top of a panel.
        /// </summary>
        private static void CreateLegacySectionLabel(Transform parent, string text)
        {
            GameObject labelGO = new GameObject("SectionLabel");
            labelGO.transform.SetParent(parent, false);

            RectTransform rect = labelGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.85f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text label = labelGO.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 11;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = COLOR_TEXT_LABEL;
        }

        /// <summary>
        /// Create a legacy digital readout display.
        /// </summary>
        private static Text CreateLegacyDigitalReadout(Transform parent, string name, string defaultText,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject readoutGO = new GameObject(name);
            readoutGO.transform.SetParent(parent, false);

            RectTransform rect = readoutGO.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = readoutGO.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f);

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(readoutGO.transform, false);

            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 2f);
            textRect.offsetMax = new Vector2(-4f, -2f);

            Text text = textGO.AddComponent<Text>();
            text.text = defaultText;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = COLOR_GREEN;

            return text;
        }

        /// <summary>
        /// Create a legacy indicator (for Screen 1 MosaicIndicator).
        /// </summary>
        private static void CreateLegacyIndicator(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject indicatorGO = new GameObject(name);
            indicatorGO.transform.SetParent(parent, false);

            RectTransform rect = indicatorGO.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = indicatorGO.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f);

            MosaicIndicator indicator = indicatorGO.AddComponent<MosaicIndicator>();
            indicator.Condition = IndicatorCondition.ReactorTripped;
            indicator.ColorType = AlarmState.Trip;
            indicator.FlashWhenActive = true;
            indicator.IndicatorImage = bg;
        }

        #endregion

        // ====================================================================
        // SHARED HELPER METHODS — Overview Gauge Items (for Plant Overview)
        // ====================================================================

        #region Helper Methods — Overview Gauges

        /// <summary>
        /// Create a compact gauge display item for the Plant Overview side panels.
        /// Returns the value TextMeshProUGUI reference. Uses a VerticalLayoutGroup
        /// child with label + value rows.
        /// </summary>
        private static TextMeshProUGUI CreateOverviewGaugeItem(Transform parent, string name, string label)
        {
            GameObject itemGO = new GameObject(name);
            itemGO.transform.SetParent(parent, false);

            itemGO.AddComponent<RectTransform>();

            Image bg = itemGO.AddComponent<Image>();
            bg.color = COLOR_GAUGE_BG;

            VerticalLayoutGroup vlg = itemGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 2, 2);
            vlg.spacing = 0f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            // Label row
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(itemGO.transform, false);
            labelGO.AddComponent<RectTransform>();

            TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 9;
            labelTMP.color = COLOR_TEXT_LABEL;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.fontStyle = FontStyles.Normal;

            // Value row
            GameObject valueGO = new GameObject("Value");
            valueGO.transform.SetParent(itemGO.transform, false);
            valueGO.AddComponent<RectTransform>();

            TextMeshProUGUI valueTMP = valueGO.AddComponent<TextMeshProUGUI>();
            valueTMP.text = "---";
            valueTMP.fontSize = 15;
            valueTMP.color = COLOR_GREEN;
            valueTMP.alignment = TextAlignmentOptions.Center;
            valueTMP.fontStyle = FontStyles.Bold;

            return valueTMP;
        }

        #endregion

        // ====================================================================
        // SHARED HELPER METHODS — Mimic Diagram Elements
        // ====================================================================

        #region Helper Methods — Mimic Diagram

        /// <summary>
        /// Create a mimic diagram box (equipment icon) with background color.
        /// Returns the Image component for runtime color updates.
        /// </summary>
        private static Image CreateMimicBox(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = go.AddComponent<Image>();
            img.color = color;

            return img;
        }

        /// <summary>
        /// Create a filled Image for level indicators (PZR, SG).
        /// Uses Image.Type.Filled with vertical fill from bottom.
        /// </summary>
        private static Image CreateMimicFill(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = go.AddComponent<Image>();
            img.color = color;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Vertical;
            img.fillOrigin = (int)Image.OriginVertical.Bottom;
            img.fillAmount = 0.5f;  // Default 50%

            return img;
        }

        #endregion

        // ====================================================================
        // SHARED HELPER METHODS — RCS Gauges (for Screen 2)
        // ====================================================================

        #region Helper Methods — RCS Gauges

        /// <summary>
        /// Create a MosaicGauge configured for RCS screen use with custom label.
        /// </summary>
        private static MosaicGauge CreateRCSGauge(Transform parent, string name,
            string label, string units)
        {
            MosaicGauge gauge = CreateMosaicGauge(parent, name, GaugeType.NeutronPower);
            gauge.CustomLabel = label;

            // Update the label text
            TextMeshProUGUI labelText = gauge.LabelText;
            if (labelText != null)
            {
                labelText.text = label;
            }

            // Slightly smaller font for RCS gauges (more gauges to fit)
            if (gauge.ValueText != null)
            {
                gauge.ValueText.fontSize = 15;
            }

            return gauge;
        }

        #endregion

        // ====================================================================
        // SHARED HELPER METHODS — TextMeshPro (for Screen 2+)
        // ====================================================================

        #region Helper Methods — TextMeshPro

        private static TextMeshProUGUI CreateTMPText(Transform parent, string name,
            string text, float fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return tmp;
        }

        /// <summary>
        /// Create a TMP label inside a VerticalLayoutGroup (with LayoutElement).
        /// </summary>
        private static TextMeshProUGUI CreateTMPLabel(Transform parent, string name,
            string text, float fontSize, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            go.AddComponent<RectTransform>();

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = fontSize + 6f;
            le.preferredHeight = fontSize + 8f;
            le.flexibleHeight = 0f;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;

            return tmp;
        }

        /// <summary>
        /// Create a TMP label with explicit anchors.
        /// </summary>
        private static TextMeshProUGUI CreateTMPLabel(Transform parent, string name,
            string text, float fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            TextMeshProUGUI tmp = CreateTMPText(parent, name, text, fontSize, color, anchorMin, anchorMax);
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static void CreateTMPSectionLabel(Transform parent, string text)
        {
            CreateTMPText(parent, "SectionLabel", text, 11, COLOR_TEXT_LABEL,
                new Vector2(0f, 0.88f), new Vector2(1f, 0.98f)).alignment = TextAlignmentOptions.Center;
        }

        #endregion

        // ====================================================================
        // SHARED HELPER METHODS — TMP Buttons (for Screen 2+)
        // ====================================================================

        #region Helper Methods — TMP Buttons

        private static Button CreateTMPButton(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);

            RectTransform rect = btnGO.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = btnGO.AddComponent<Image>();
            bg.color = bgColor;

            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.3f;
            colors.pressedColor = COLOR_CYAN;
            btn.colors = colors;

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(btnGO.transform, false);

            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 11;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = COLOR_TEXT_NORMAL;

            return btn;
        }

        #endregion
    }
}
#endif

