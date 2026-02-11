// ============================================================================
// CRITICAL: Master the Atom - Instrument Sprite Generator
// InstrumentSpriteGenerator.cs - Editor Script
// ============================================================================
//
// PURPOSE:
//   Procedurally generates UI sprite textures for the instrument panel
//   visual upgrade. Creates gauge backgrounds, cell bezels, button sprites,
//   readout backgrounds, and fill bar textures.
//
// USAGE:
//   Unity Menu: Critical > Generate Instrument Sprites
//   Outputs to: Assets/Resources/Sprites/
//
// CREATED: v4.1.0
// ============================================================================

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;

namespace Critical.UI.Editor
{
    public static class InstrumentSpriteGenerator
    {
        private const string OUTPUT_PATH = "Assets/Resources/Sprites";

        [MenuItem("Critical/Generate Instrument Sprites")]
        public static void GenerateAll()
        {
            Debug.Log("[SpriteGen] Generating instrument sprites...");

            EnsureDirectory(OUTPUT_PATH);

            GenerateGaugeBackground();
            GenerateCellBackground();
            GenerateButtonBackground();
            GenerateReadoutBackground();
            GenerateFillBar();
            GenerateGlowSprite();

            AssetDatabase.Refresh();

            Debug.Log("[SpriteGen] All instrument sprites generated in " + OUTPUT_PATH);
        }

        // ================================================================
        // GAUGE BACKGROUND — 256x64, dark with subtle inner shadow/gradient
        // ================================================================
        private static void GenerateGaugeBackground()
        {
            int w = 256, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color darkCenter = new Color(0.039f, 0.039f, 0.063f, 1f);    // #0A0A10
            Color darkEdge = new Color(0.055f, 0.055f, 0.078f, 1f);      // #0E0E14
            Color innerShadow = new Color(0.024f, 0.024f, 0.039f, 1f);   // #060610
            Color border = new Color(0.098f, 0.098f, 0.133f, 1f);        // #191922

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c;

                    // 1px border
                    if (x == 0 || x == w - 1 || y == 0 || y == h - 1)
                    {
                        c = border;
                    }
                    // 2px inner shadow (top and left darker)
                    else if (x <= 2 || y >= h - 3)
                    {
                        c = innerShadow;
                    }
                    // Inner highlight (bottom-left to top-right subtle gradient)
                    else if (y <= 2 || x >= w - 3)
                    {
                        c = darkEdge;
                    }
                    else
                    {
                        // Subtle radial gradient — slightly lighter in center
                        float nx = (float)(x - w / 2) / (w / 2);
                        float ny = (float)(y - h / 2) / (h / 2);
                        float dist = Mathf.Sqrt(nx * nx + ny * ny);
                        c = Color.Lerp(darkCenter, darkEdge, Mathf.Clamp01(dist * 0.6f));
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            SaveTexture(tex, "gauge_bg", w, h);
        }

        // ================================================================
        // CELL BACKGROUND — 32x32, square with border and inner bevel
        // ================================================================
        private static void GenerateCellBackground()
        {
            int w = 32, h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color border = new Color(0.039f, 0.039f, 0.059f, 1f);          // #0A0A0F
            Color innerHighlight = new Color(0.165f, 0.165f, 0.208f, 0.4f); // top/left edge
            Color innerShadow = new Color(0.020f, 0.020f, 0.031f, 0.6f);   // bottom/right edge
            Color fill = new Color(0.5f, 0.5f, 0.5f, 1f);                  // Neutral — tinted by Image.color

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c;

                    // 1px outer border
                    if (x == 0 || x == w - 1 || y == 0 || y == h - 1)
                    {
                        c = border;
                    }
                    // 1px inner highlight (top and left)
                    else if (x == 1 || y == h - 2)
                    {
                        c = Color.Lerp(fill, innerHighlight, 0.5f);
                    }
                    // 1px inner shadow (bottom and right)
                    else if (x == w - 2 || y == 1)
                    {
                        c = Color.Lerp(fill, innerShadow, 0.5f);
                    }
                    else
                    {
                        c = fill;
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            SaveTextureAs9Slice(tex, "cell_bg", w, h, 2);
        }

        // ================================================================
        // BUTTON BACKGROUND — 64x32, rounded corners with top highlight
        // ================================================================
        private static void GenerateButtonBackground()
        {
            int w = 64, h = 32;
            int r = 3; // corner radius
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color body = new Color(0.5f, 0.5f, 0.5f, 1f);               // Neutral — tinted
            Color highlight = new Color(0.7f, 0.7f, 0.7f, 1f);          // Top edge highlight
            Color shadow = new Color(0.25f, 0.25f, 0.25f, 1f);          // Bottom edge shadow
            Color border = new Color(0.15f, 0.15f, 0.20f, 1f);          // Border
            Color transparent = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Check if inside rounded rect
                    if (!IsInsideRoundedRect(x, y, w, h, r))
                    {
                        tex.SetPixel(x, y, transparent);
                        continue;
                    }

                    // Check if on border of rounded rect
                    if (!IsInsideRoundedRect(x, y, w, h, r, 1))
                    {
                        tex.SetPixel(x, y, border);
                        continue;
                    }

                    // Vertical gradient: highlight at top, shadow at bottom
                    float t = (float)y / h;
                    Color c;
                    if (t > 0.85f)
                        c = Color.Lerp(body, highlight, (t - 0.85f) / 0.15f * 0.4f);
                    else if (t < 0.15f)
                        c = Color.Lerp(body, shadow, (0.15f - t) / 0.15f * 0.4f);
                    else
                        c = body;

                    tex.SetPixel(x, y, c);
                }
            }

            SaveTextureAs9Slice(tex, "button_bg", w, h, r + 2);
        }

        // ================================================================
        // READOUT BACKGROUND — 128x32, recessed with inner shadow
        // ================================================================
        private static void GenerateReadoutBackground()
        {
            int w = 128, h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color darkFill = new Color(0.027f, 0.027f, 0.043f, 1f);      // #070711
            Color innerShadow = new Color(0.016f, 0.016f, 0.027f, 1f);   // #040407
            Color outerEdge = new Color(0.078f, 0.078f, 0.102f, 1f);     // #14141A
            Color borderDark = new Color(0.047f, 0.047f, 0.067f, 1f);    // #0C0C11

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c;

                    // 1px outer border (raised lip)
                    if (x == 0 || x == w - 1 || y == 0 || y == h - 1)
                    {
                        c = outerEdge;
                    }
                    // 1px inner border
                    else if (x == 1 || x == w - 2 || y == 1 || y == h - 2)
                    {
                        c = borderDark;
                    }
                    // 2px inner shadow (top and left — light comes from above-left)
                    else if (y >= h - 4 || x <= 3)
                    {
                        c = innerShadow;
                    }
                    else
                    {
                        c = darkFill;
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            SaveTextureAs9Slice(tex, "readout_bg", w, h, 4);
        }

        // ================================================================
        // FILL BAR — 256x16, horizontal gradient fill
        // ================================================================
        private static void GenerateFillBar()
        {
            int w = 256, h = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Vertical gradient for slight 3D feel
                    float vy = (float)y / h;
                    float brightness;
                    if (vy > 0.7f)
                        brightness = 0.8f + (vy - 0.7f) / 0.3f * 0.2f; // Top highlight
                    else if (vy < 0.3f)
                        brightness = 0.6f + vy / 0.3f * 0.2f;           // Bottom shadow
                    else
                        brightness = 0.8f;

                    // Rounded left cap (first 4 pixels)
                    float alpha = 1f;
                    if (x < 4)
                    {
                        float dx = 4f - x;
                        float dy = Mathf.Abs(y - h / 2f);
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        alpha = Mathf.Clamp01(1f - (dist - 3f));
                    }

                    // Color is neutral white — tinted by Image.color at runtime
                    Color c = new Color(brightness, brightness, brightness, alpha);
                    tex.SetPixel(x, y, c);
                }
            }

            SaveTexture(tex, "fill_bar", w, h);
        }

        // ================================================================
        // GLOW SPRITE — 64x64, soft radial glow for behind value text
        // ================================================================
        private static void GenerateGlowSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            float center = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Soft gaussian-like falloff
                    float alpha = Mathf.Exp(-dist * dist * 3f);
                    alpha = Mathf.Clamp01(alpha * 0.6f); // Peak at 60% opacity

                    Color c = new Color(1f, 1f, 1f, alpha); // White — tinted at runtime
                    tex.SetPixel(x, y, c);
                }
            }

            SaveTexture(tex, "glow_soft", size, size);
        }

        // ================================================================
        // UTILITY
        // ================================================================

        private static bool IsInsideRoundedRect(int x, int y, int w, int h, int r, int shrink = 0)
        {
            int sx = x + shrink;
            int sy = y + shrink;
            int sw = w - shrink * 2;
            int sh = h - shrink * 2;

            if (sx < 0 || sy < 0 || sx >= sw || sy >= sh) return false;

            // Check corners
            if (sx < r && sy < r)
                return (r - sx) * (r - sx) + (r - sy) * (r - sy) <= r * r;
            if (sx >= sw - r && sy < r)
                return (sx - (sw - r - 1)) * (sx - (sw - r - 1)) + (r - sy) * (r - sy) <= r * r;
            if (sx < r && sy >= sh - r)
                return (r - sx) * (r - sx) + (sy - (sh - r - 1)) * (sy - (sh - r - 1)) <= r * r;
            if (sx >= sw - r && sy >= sh - r)
                return (sx - (sw - r - 1)) * (sx - (sw - r - 1)) + (sy - (sh - r - 1)) * (sy - (sh - r - 1)) <= r * r;

            return true;
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void SaveTexture(Texture2D tex, string name, int w, int h)
        {
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            string fullPath = $"{OUTPUT_PATH}/{name}.png";
            File.WriteAllBytes(fullPath, png);

            Object.DestroyImmediate(tex);

            Debug.Log($"  [SpriteGen] Created {name}.png ({w}x{h})");
        }

        private static void SaveTextureAs9Slice(Texture2D tex, string name, int w, int h, int border)
        {
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            string fullPath = $"{OUTPUT_PATH}/{name}.png";
            File.WriteAllBytes(fullPath, png);

            Object.DestroyImmediate(tex);

            // Note: 9-slice border must be configured in Unity's Sprite Editor after import
            // The border value is documented here for reference
            Debug.Log($"  [SpriteGen] Created {name}.png ({w}x{h}, 9-slice border={border})");
        }
    }
}

#endif
