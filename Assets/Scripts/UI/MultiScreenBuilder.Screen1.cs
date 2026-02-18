// CRITICAL: Master the Atom - Multi-Screen Builder
// MultiScreenBuilder.Screen1.cs
//
// File: Assets/Scripts/UI/MultiScreenBuilder.Screen1.cs
// Module: Critical.UI.MultiScreenBuilder
// Responsibility: Screen 1 - Reactor Core construction logic.
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

        #region Screen 1 - Reactor Core

        /// <summary>
        /// Build the complete Reactor Core screen (Screen 1) UI hierarchy.
        /// This is a direct port of OperatorScreenBuilder.CreateOperatorScreen()
        /// adapted to live under the shared OperatorScreensCanvas.
        /// </summary>
        private static void CreateReactorCoreScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 1 â€” Reactor Core...");

            // Check if already built
            foreach (Transform child in canvasParent)
            {
                if (child.name == "ReactorOperatorScreen")
                {
                    Debug.LogWarning("[MultiScreenBuilder] ReactorOperatorScreen already exists under this canvas. Skipping.");
                    var existingScreen = child.GetComponent<ReactorOperatorScreen>();
                    if (existingScreen != null)
                    {
                        EnsureReactorScreenAdapter(existingScreen);
                    }
                    return;
                }
            }

            // Also check if Screen 1 exists elsewhere in the scene (from old builder)
            ReactorOperatorScreen existingAnywhere = FindObjectOfType<ReactorOperatorScreen>();
            if (existingAnywhere != null)
            {
                Debug.LogWarning("[MultiScreenBuilder] ReactorOperatorScreen found on a different canvas. " +
                               "Attaching adapter only. Move it under OperatorScreensCanvas for unified management.");
                EnsureReactorScreenAdapter(existingAnywhere);
                return;
            }

            // v4.1.0: Delegate to OperatorScreenBuilder.BuildScreen1() â€” single source of truth
            ReactorOperatorScreen screen = OperatorScreenBuilder.BuildScreen1(canvasParent);
            screen.StartVisible = true;

            // v4.0.0: Add panel skin component
            ReactorOperatorScreenSkin skin = screen.gameObject.AddComponent<ReactorOperatorScreenSkin>();
            var transparentPanels = new System.Collections.Generic.List<Image>();
            if (screen.LeftGaugePanel != null)
            {
                Image img = screen.LeftGaugePanel.GetComponent<Image>();
                if (img != null) transparentPanels.Add(img);
            }
            if (screen.CoreMapPanel != null)
            {
                Image img = screen.CoreMapPanel.GetComponent<Image>();
                if (img != null) transparentPanels.Add(img);
            }
            if (screen.RightGaugePanel != null)
            {
                Image img = screen.RightGaugePanel.GetComponent<Image>();
                if (img != null) transparentPanels.Add(img);
            }
            if (screen.DetailPanelArea != null)
            {
                Image img = screen.DetailPanelArea.GetComponent<Image>();
                if (img != null) transparentPanels.Add(img);
            }
            if (screen.BottomPanel != null)
            {
                Image img = screen.BottomPanel.GetComponent<Image>();
                if (img != null) transparentPanels.Add(img);
            }
            skin.TransparentPanels = transparentPanels.ToArray();

            // Attach ReactorScreenAdapter for ScreenManager integration
            EnsureReactorScreenAdapter(screen);

            Debug.Log("[MultiScreenBuilder] Screen 1 â€” Reactor Core â€” build complete");
        }

        /// <summary>
        /// Ensure ReactorScreenAdapter is attached to a ReactorOperatorScreen.
        /// </summary>
        private static void EnsureReactorScreenAdapter(ReactorOperatorScreen screen)
        {
            if (screen.GetComponent<ReactorScreenAdapter>() != null)
            {
                Debug.Log("[MultiScreenBuilder] ReactorScreenAdapter already attached");
                return;
            }

            if (screen.GetComponent<CanvasGroup>() == null)
            {
                screen.gameObject.AddComponent<CanvasGroup>();
            }

            screen.gameObject.AddComponent<ReactorScreenAdapter>();
            Debug.Log("[MultiScreenBuilder] ReactorScreenAdapter attached to Screen 1");
        }

        // v4.1.0: All BuildScreen1_* methods removed.
        // Screen 1 is now built by OperatorScreenBuilder.BuildScreen1() â€” single source of truth.
        // This eliminates ~350 lines of duplicated code and ensures all screens use
        // the same TMP fonts, materials, and sprite backgrounds.

        // BuildScreen1_* methods removed in v4.1.0 â€” now in OperatorScreenBuilder.BuildScreen1()

        #endregion

    }
}
#endif


