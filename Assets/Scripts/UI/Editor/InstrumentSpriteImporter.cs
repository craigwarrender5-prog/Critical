// ============================================================================
// CRITICAL: Master the Atom - Sprite Import Postprocessor
// InstrumentSpriteImporter.cs - Editor Script
// ============================================================================
//
// PURPOSE:
//   Automatically configures import settings for generated instrument sprites.
//   Sets texture type to Sprite, configures 9-slice borders, and sets
//   appropriate compression.
//
// CREATED: v4.1.0
// ============================================================================

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace Critical.UI.Editor
{
    public class InstrumentSpriteImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            // Only process our instrument sprites
            if (!assetPath.StartsWith("Assets/Resources/Sprites/")) return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            // Set 9-slice borders based on sprite name
            string filename = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            switch (filename)
            {
                case "cell_bg":
                    importer.spriteBorder = new Vector4(2, 2, 2, 2);
                    break;
                case "button_bg":
                    importer.spriteBorder = new Vector4(5, 5, 5, 5);
                    break;
                case "readout_bg":
                    importer.spriteBorder = new Vector4(4, 4, 4, 4);
                    break;
                case "gauge_bg":
                    importer.spriteBorder = new Vector4(3, 3, 3, 3);
                    break;
                // fill_bar and glow_soft don't need 9-slice
            }
        }
    }
}

#endif
