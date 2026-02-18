// CRITICAL: Master the Atom - Multi-Screen Builder
// MultiScreenBuilder.Infrastructure.cs
//
// File: Assets/Scripts/UI/MultiScreenBuilder.Infrastructure.cs
// Module: Critical.UI.MultiScreenBuilder
// Responsibility: Infrastructure construction logic.
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

        #region Infrastructure

        /// <summary>
        /// Find or create the shared OperatorScreensCanvas.
        /// All screens live under this single Canvas.
        /// </summary>
        private static Canvas FindOrCreateOperatorCanvas()
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (var c in canvases)
            {
                if (c.gameObject.name == "OperatorScreensCanvas")
                {
                    Debug.Log("[MultiScreenBuilder] Using existing OperatorScreensCanvas");
                    return c;
                }
            }

            GameObject canvasGO = new GameObject("OperatorScreensCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            Debug.Log("[MultiScreenBuilder] Created OperatorScreensCanvas");
            return canvas;
        }

        /// <summary>
        /// Ensure EventSystem exists with New Input System module.
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Debug.Log("[MultiScreenBuilder] Created EventSystem with InputSystemUIInputModule");
            }
        }

        /// <summary>
        /// Ensure ScreenManager singleton exists.
        /// Automatically wires the ScreenInputActions asset and disables allowNoScreen.
        /// </summary>
        private static void EnsureScreenManager()
        {
            ScreenManager existing = FindObjectOfType<ScreenManager>();
            ScreenManager mgr;

            if (existing != null)
            {
                Debug.Log("[MultiScreenBuilder] ScreenManager already exists â€” updating settings");
                mgr = existing;
            }
            else
            {
                GameObject go = new GameObject("ScreenManager");
                mgr = go.AddComponent<ScreenManager>();
                Debug.Log("[MultiScreenBuilder] Created ScreenManager");
            }

            // Wire ScreenInputActions asset automatically
            SerializedObject so = new SerializedObject(mgr);

            SerializedProperty inputProp = so.FindProperty("screenInputActions");
            if (inputProp != null && inputProp.objectReferenceValue == null)
            {
                // Search for the asset in the project
                string[] guids = AssetDatabase.FindAssets("ScreenInputActions t:InputActionAsset");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(path);
                    if (asset != null)
                    {
                        inputProp.objectReferenceValue = asset;
                        Debug.Log($"[MultiScreenBuilder] Wired ScreenInputActions from {path}");
                    }
                }
                else
                {
                    Debug.LogWarning("[MultiScreenBuilder] ScreenInputActions asset not found in project! " +
                                   "Create it at Assets/InputActions/ScreenInputActions.inputactions");
                }
            }

            // Disable allowNoScreen â€” pressing the same key should NOT hide the active screen
            SerializedProperty noScreenProp = so.FindProperty("allowNoScreen");
            if (noScreenProp != null)
            {
                noScreenProp.boolValue = false;
            }

            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// Ensure ScreenDataBridge singleton exists.
        /// </summary>
        private static void EnsureScreenDataBridge()
        {
            if (FindObjectOfType<ScreenDataBridge>() != null)
            {
                Debug.Log("[MultiScreenBuilder] ScreenDataBridge already exists");
                return;
            }

            GameObject go = new GameObject("ScreenDataBridge");
            go.AddComponent<ScreenDataBridge>();
            Debug.Log("[MultiScreenBuilder] Created ScreenDataBridge");
        }

        #endregion

    }
}
#endif


