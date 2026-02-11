// ============================================================================
// CRITICAL: Master the Atom - Instrument Material Setup
// InstrumentMaterialSetup.cs - Editor Script
// ============================================================================
//
// PURPOSE:
//   Creates TMP material presets for instrument display text.
//   These materials provide the phosphor glow, drop shadow, and color
//   effects that make the gauges look like real nuclear instrument readouts.
//
// USAGE:
//   Unity Menu: Critical > Setup Instrument Materials
//   Outputs to: Assets/Resources/Fonts & Materials/
//
// PREREQUISITE:
//   TextMesh Pro must be imported with Examples & Extras (for Electronic
//   Highway Sign SDF font asset).
//
// CREATED: v4.1.0
// ============================================================================

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;
using TMPro;

namespace Critical.UI.Editor
{
    public static class InstrumentMaterialSetup
    {
        // Output to the main TMP Resources folder so Resources.Load works
        private const string OUTPUT_PATH = "Assets/Resources/Fonts & Materials";

        // Font asset resource paths (relative to any Resources folder)
        private const string HIGHWAY_FONT_PATH = "Fonts & Materials/Electronic Highway Sign SDF";
        private const string LIBERATION_FONT_PATH = "Fonts & Materials/LiberationSans SDF";

        [MenuItem("Critical/Setup Instrument Materials")]
        public static void SetupAll()
        {
            Debug.Log("[MaterialSetup] Creating instrument TMP materials...");

            EnsureDirectory(OUTPUT_PATH);

            // Load font assets
            TMP_FontAsset highwayFont = Resources.Load<TMP_FontAsset>(HIGHWAY_FONT_PATH);
            TMP_FontAsset liberationFont = Resources.Load<TMP_FontAsset>(LIBERATION_FONT_PATH);

            if (highwayFont == null)
            {
                Debug.LogError("[MaterialSetup] Electronic Highway Sign SDF not found! " +
                    "Import TextMesh Pro Examples & Extras via Window > TextMeshPro > Import TMP Examples & Extras");
                return;
            }

            if (liberationFont == null)
            {
                Debug.LogError("[MaterialSetup] LiberationSans SDF not found! " +
                    "Import TextMesh Pro Essential Resources via Window > TextMeshPro > Import TMP Essential Resources");
                return;
            }

            // Create instrument readout materials (based on Electronic Highway Sign)
            CreateInstrumentMaterial(highwayFont, "Instrument_Green_Glow",
                faceColor: HexColor("#00FF88"),
                glowColor: HexColor("#00FF88"), glowPower: 0.25f, glowOffset: 0f, glowInner: 0.15f,
                underlayColor: HexColor("#000000"), underlayOffsetX: 0.4f, underlayOffsetY: -0.4f,
                underlayDilate: 0f, underlaySoftness: 0.3f);

            CreateInstrumentMaterial(highwayFont, "Instrument_Amber_Glow",
                faceColor: HexColor("#FFB830"),
                glowColor: HexColor("#FFB830"), glowPower: 0.25f, glowOffset: 0f, glowInner: 0.15f,
                underlayColor: HexColor("#000000"), underlayOffsetX: 0.4f, underlayOffsetY: -0.4f,
                underlayDilate: 0f, underlaySoftness: 0.3f);

            CreateInstrumentMaterial(highwayFont, "Instrument_Red_Glow",
                faceColor: HexColor("#FF3344"),
                glowColor: HexColor("#FF3344"), glowPower: 0.35f, glowOffset: 0f, glowInner: 0.2f,
                underlayColor: HexColor("#000000"), underlayOffsetX: 0.4f, underlayOffsetY: -0.4f,
                underlayDilate: 0f, underlaySoftness: 0.3f);

            CreateInstrumentMaterial(highwayFont, "Instrument_Cyan",
                faceColor: HexColor("#00CCFF"),
                glowColor: HexColor("#00CCFF"), glowPower: 0.2f, glowOffset: 0f, glowInner: 0.1f,
                underlayColor: HexColor("#000000"), underlayOffsetX: 0.3f, underlayOffsetY: -0.3f,
                underlayDilate: 0f, underlaySoftness: 0.2f);

            CreateInstrumentMaterial(highwayFont, "Instrument_White",
                faceColor: HexColor("#C8D0D8"),
                glowColor: HexColor("#C8D0D8"), glowPower: 0.1f, glowOffset: 0f, glowInner: 0.1f,
                underlayColor: HexColor("#000000"), underlayOffsetX: 0.3f, underlayOffsetY: -0.3f,
                underlayDilate: 0f, underlaySoftness: 0.2f);

            // Alarm pulsing material (higher glow power — animated at runtime)
            CreateInstrumentMaterial(highwayFont, "Alarm_Pulse_Red",
                faceColor: HexColor("#FF3344"),
                glowColor: HexColor("#FF0022"), glowPower: 0.5f, glowOffset: 0f, glowInner: 0.3f,
                underlayColor: HexColor("#330000"), underlayOffsetX: 0f, underlayOffsetY: 0f,
                underlayDilate: 0.2f, underlaySoftness: 0.5f);

            // Label materials (based on LiberationSans)
            CreateLabelMaterial(liberationFont, "Label_Standard",
                faceColor: HexColor("#8090A0"),
                underlayColor: HexColor("#000000"), underlayOffsetX: 0.3f, underlayOffsetY: -0.3f,
                underlaySoftness: 0.3f);

            CreateLabelMaterial(liberationFont, "Label_Section",
                faceColor: HexColor("#C8D0D8"),
                underlayColor: HexColor("#000000"), underlayOffsetX: 0.4f, underlayOffsetY: -0.4f,
                underlaySoftness: 0.3f);

            AssetDatabase.Refresh();

            Debug.Log("[MaterialSetup] All instrument materials created in " + OUTPUT_PATH);
        }

        // ================================================================
        // MATERIAL CREATORS
        // ================================================================

        private static void CreateInstrumentMaterial(TMP_FontAsset font, string name,
            Color faceColor,
            Color glowColor, float glowPower, float glowOffset, float glowInner,
            Color underlayColor, float underlayOffsetX, float underlayOffsetY,
            float underlayDilate, float underlaySoftness)
        {
            string path = $"{OUTPUT_PATH}/{name}.mat";

            // Create material based on the font's default material shader
            Material mat = new Material(font.material.shader);
            mat.CopyPropertiesFromMaterial(font.material);
            mat.name = name;

            // Face
            mat.SetColor("_FaceColor", faceColor);

            // Glow (TMP SDF shader keywords)
            mat.SetColor("_GlowColor", glowColor);
            mat.SetFloat("_GlowPower", glowPower);
            mat.SetFloat("_GlowOffset", glowOffset);
            mat.SetFloat("_GlowInner", glowInner);
            mat.SetFloat("_GlowOuter", 0.5f);

            // Underlay (drop shadow)
            mat.SetColor("_UnderlayColor", underlayColor);
            mat.SetFloat("_UnderlayOffsetX", underlayOffsetX);
            mat.SetFloat("_UnderlayOffsetY", underlayOffsetY);
            mat.SetFloat("_UnderlayDilate", underlayDilate);
            mat.SetFloat("_UnderlaySoftness", underlaySoftness);

            // Enable shader keywords
            mat.EnableKeyword("GLOW_ON");
            mat.EnableKeyword("UNDERLAY_ON");

            // Set atlas texture from font
            mat.SetTexture("_MainTex", font.atlasTexture);

            AssetDatabase.CreateAsset(mat, path);

            Debug.Log($"  [MaterialSetup] Created {name}.mat");
        }

        private static void CreateLabelMaterial(TMP_FontAsset font, string name,
            Color faceColor,
            Color underlayColor, float underlayOffsetX, float underlayOffsetY,
            float underlaySoftness)
        {
            string path = $"{OUTPUT_PATH}/{name}.mat";

            Material mat = new Material(font.material.shader);
            mat.CopyPropertiesFromMaterial(font.material);
            mat.name = name;

            // Face
            mat.SetColor("_FaceColor", faceColor);

            // Underlay (subtle drop shadow)
            mat.SetColor("_UnderlayColor", underlayColor);
            mat.SetFloat("_UnderlayOffsetX", underlayOffsetX);
            mat.SetFloat("_UnderlayOffsetY", underlayOffsetY);
            mat.SetFloat("_UnderlayDilate", 0f);
            mat.SetFloat("_UnderlaySoftness", underlaySoftness);

            // Enable underlay only (no glow for labels)
            mat.EnableKeyword("UNDERLAY_ON");

            // Set atlas texture from font
            mat.SetTexture("_MainTex", font.atlasTexture);

            AssetDatabase.CreateAsset(mat, path);

            Debug.Log($"  [MaterialSetup] Created {name}.mat");
        }

        // ================================================================
        // UTILITY
        // ================================================================

        private static Color HexColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color c))
                return c;
            return Color.white;
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}

#endif
