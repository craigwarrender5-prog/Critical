// ============================================================================
// CRITICAL: Master the Atom - Multi-Screen Builder
// MultiScreenBuilder.cs - Editor Tool for Creating All Operator Screen UIs
// ============================================================================
//
// PURPOSE:
//   Creates the COMPLETE multi-screen operator interface hierarchy in a
//   single Unity scene. All screens share one Canvas, one ScreenManager,
//   and one ScreenDataBridge.
//
//   Menu: Critical > Create All Operator Screens
//
// ARCHITECTURE:
//   - Creates a single shared OperatorScreensCanvas with CanvasScaler (1920x1080)
//   - Creates ScreenManager and ScreenDataBridge singletons
//   - Builds Screen 1 (Reactor Core) with ReactorScreenAdapter for integration
//   - Each screen is a child panel with its own component
//   - Reuses the builder pattern from OperatorScreenBuilder.cs
//
// SCREENS BUILT:
//   - Screen 1 (Reactor Core) — GOLD STANDARD ReactorOperatorScreen
//   - Screen 2 (RCS Primary Loop) — RCSPrimaryLoopScreen
//   - Screen Tab (Plant Overview) — PlantOverviewScreen
//   - Screen 3 (Pressurizer) — PressurizerScreen
//   - Screen 4 (CVCS) — CVCSScreen
//   - Screen 5 (Steam Generators) — SteamGeneratorScreen
//   - Screen 6 (Turbine-Generator) — TurbineGeneratorScreen
//   - Screen 7 (Secondary Systems) — SecondarySystemsScreen
//   - Screen 8 (Auxiliary Systems) — AuxiliarySystemsScreen
//
// REPLACES:
//   - OperatorScreenBuilder.cs > "Critical > Create Operator Screen" (Screen 1 only)
//   - This builder creates ALL screens in one unified hierarchy.
//
// GOLD STANDARD:
//   ReactorOperatorScreen.cs is NOT modified. The builder creates GameObjects
//   and wires its public fields — exactly as OperatorScreenBuilder does.
//
// USAGE:
//   1. Menu: Critical > Create All Operator Screens
//   2. ScreenInputActions asset is auto-wired to ScreenManager
//   3. (Optional) Assign RCS model prefab to Screen 2 in Inspector
//   4. Press Play — keys 1/2/Tab switch screens
//
// VERSION: 2.0.9
// DATE: 2026-02-10
//
// SOURCES:
//   - IMPLEMENTATION_PLAN_v2.0.0_MultiScreenGUI.md
//   - Operator_Screen_Layout_Plan_v1_0_0.md
//   - OperatorScreenBuilder.cs (Screen 1 pattern reference)
//
// VERSION: 2.0.0
// DATE: 2026-02-10
// CLASSIFICATION: UI — Editor Tool
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Critical.UI
{
    /// <summary>
    /// Editor tool for creating the complete multi-screen operator interface.
    /// Builds all screen UIs programmatically with wired references in one Canvas.
    /// </summary>
    public class MultiScreenBuilder : MonoBehaviour
    {
        #if UNITY_EDITOR

        // ====================================================================
        // COLOR PALETTE (from Design Doc — shared across all screens)
        // ====================================================================

        #region Color Palette

        private static readonly Color COLOR_BACKGROUND = new Color(0.102f, 0.102f, 0.122f);      // #1A1A1F
        private static readonly Color COLOR_PANEL = new Color(0.118f, 0.118f, 0.157f);           // #1E1E28
        private static readonly Color COLOR_GAUGE_BG = new Color(0.078f, 0.078f, 0.102f);        // #14141A
        private static readonly Color COLOR_MAP_BG = new Color(0.071f, 0.071f, 0.102f);          // #12121A
        private static readonly Color COLOR_BORDER = new Color(0.165f, 0.165f, 0.208f);          // #2A2A35
        private static readonly Color COLOR_TEXT_NORMAL = new Color(0.784f, 0.816f, 0.847f);     // #C8D0D8
        private static readonly Color COLOR_TEXT_LABEL = new Color(0.502f, 0.565f, 0.627f);      // #8090A0
        private static readonly Color COLOR_GREEN = new Color(0f, 1f, 0.533f);                   // #00FF88
        private static readonly Color COLOR_AMBER = new Color(1f, 0.722f, 0.188f);               // #FFB830
        private static readonly Color COLOR_RED = new Color(1f, 0.2f, 0.267f);                   // #FF3344
        private static readonly Color COLOR_CYAN = new Color(0f, 0.8f, 1f);                      // #00CCFF
        private static readonly Color COLOR_BUTTON = new Color(0.2f, 0.2f, 0.25f);
        private static readonly Color COLOR_BUTTON_HOVER = new Color(0.25f, 0.25f, 0.3f);
        private static readonly Color COLOR_CENTER_BG = new Color(0.071f, 0.071f, 0.102f);      // #12121A
        private static readonly Color COLOR_PLACEHOLDER_MIMIC = new Color(0.4f, 0.4f, 0.5f);    // Placeholder text in mimic
        private static readonly Color COLOR_STOPPED = new Color(0.5f, 0.5f, 0.55f);              // Stopped / inactive state
        private static readonly Color COLOR_WARNING = new Color(1f, 0.722f, 0.188f);              // Warning (same as AMBER)

        #endregion

        // ====================================================================
        // MENU ITEMS
        // ====================================================================

        [MenuItem("Critical/Create All Operator Screens")]
        public static void CreateAllOperatorScreens()
        {
            Debug.Log("[MultiScreenBuilder] ===== Creating Multi-Screen Operator Interface =====");

            // 1. Create the shared Canvas
            Canvas canvas = FindOrCreateOperatorCanvas();

            // 2. Ensure EventSystem exists
            EnsureEventSystem();

            // 3. Create ScreenManager singleton
            EnsureScreenManager();

            // 4. Create ScreenDataBridge singleton
            EnsureScreenDataBridge();

            // 5. Build Screen 1 — Reactor Core (GOLD STANDARD)
            CreateReactorCoreScreen(canvas.transform);

            // 6. Build Screen 2 — RCS Primary Loop
            CreateRCSScreen(canvas.transform);

            // 7. Build Screen Tab — Plant Overview
            CreatePlantOverviewScreen(canvas.transform);

            // 8. Build Screen 3 — Pressurizer
            CreatePressurizerScreen(canvas.transform);

            // 9. Build Screen 4 — CVCS
            CreateCVCSScreen(canvas.transform);

            // 10. Build Screen 5 — Steam Generators
            CreateSteamGeneratorScreen(canvas.transform);

            // 11. Build Screen 6 — Turbine-Generator
            CreateTurbineGeneratorScreen(canvas.transform);

            // 12. Build Screen 7 — Secondary Systems
            CreateSecondarySystemsScreen(canvas.transform);

            // 13. Build Screen 8 — Auxiliary Systems
            CreateAuxiliarySystemsScreen(canvas.transform);

            Debug.Log("[MultiScreenBuilder] ===== Multi-Screen build complete =====");
            Debug.Log("  IMPORTANT: Assign ScreenInputActions asset to ScreenManager in Inspector");
            Debug.Log("  Press Play, then press 1/2/Tab to switch screens");
        }

        // ====================================================================
        // INFRASTRUCTURE SETUP
        // ====================================================================

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
                Debug.Log("[MultiScreenBuilder] ScreenManager already exists — updating settings");
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

            // Disable allowNoScreen — pressing the same key should NOT hide the active screen
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

        // ####################################################################
        //
        //  SCREEN 1 — REACTOR CORE (GOLD STANDARD)
        //
        //  Ported from OperatorScreenBuilder.cs. Creates the complete
        //  ReactorOperatorScreen hierarchy with MosaicBoard, CoreMosaicMap,
        //  gauges, rod display, controls, and alarms. Attaches
        //  ReactorScreenAdapter for ScreenManager integration.
        //
        //  GOLD STANDARD: ReactorOperatorScreen.cs is NOT modified.
        //
        // ####################################################################

        #region Screen 1 - Reactor Core

        /// <summary>
        /// Build the complete Reactor Core screen (Screen 1) UI hierarchy.
        /// This is a direct port of OperatorScreenBuilder.CreateOperatorScreen()
        /// adapted to live under the shared OperatorScreensCanvas.
        /// </summary>
        private static void CreateReactorCoreScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 1 — Reactor Core...");

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

            // v4.1.0: Delegate to OperatorScreenBuilder.BuildScreen1() — single source of truth
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

            Debug.Log("[MultiScreenBuilder] Screen 1 — Reactor Core — build complete");
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
        // Screen 1 is now built by OperatorScreenBuilder.BuildScreen1() — single source of truth.
        // This eliminates ~350 lines of duplicated code and ensures all screens use
        // the same TMP fonts, materials, and sprite backgrounds.

        // BuildScreen1_* methods removed in v4.1.0 — now in OperatorScreenBuilder.BuildScreen1()

        #endregion

        // ####################################################################
        //
        //  SCREEN 2 — RCS PRIMARY LOOP
        //
        // ####################################################################

        #region Screen 2 - RCS Primary Loop

        /// <summary>
        /// Build the complete RCS Primary Loop screen (Screen 2) UI hierarchy.
        /// </summary>
        private static void CreateRCSScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 2 — RCS Primary Loop...");

            foreach (Transform child in canvasParent)
            {
                if (child.name == "RCSPrimaryLoopScreen")
                {
                    Debug.LogWarning("[MultiScreenBuilder] RCSPrimaryLoopScreen already exists. Skipping.");
                    return;
                }
            }

            // --- Root panel ---
            GameObject screenGO = new GameObject("RCSPrimaryLoopScreen");
            screenGO.transform.SetParent(canvasParent, false);

            RectTransform rootRect = screenGO.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image rootBg = screenGO.AddComponent<Image>();
            rootBg.color = COLOR_BACKGROUND;

            screenGO.AddComponent<CanvasGroup>();

            RCSPrimaryLoopScreen screen = screenGO.AddComponent<RCSPrimaryLoopScreen>();

            // --- Build panels ---
            BuildRCSLeftPanel(screenGO.transform, screen);
            BuildRCSCenterPanel(screenGO.transform, screen);
            BuildRCSRightPanel(screenGO.transform, screen);
            BuildRCSBottomPanel(screenGO.transform, screen);

            // --- Start hidden (ScreenManager shows Screen 1 first) ---
            screenGO.SetActive(false);

            Debug.Log("[MultiScreenBuilder] Screen 2 — RCS Primary Loop — build complete");
        }

        // ----------------------------------------------------------------
        // Screen 2: Left Panel — 8 Temperature Gauges
        // ----------------------------------------------------------------

        private static void BuildRCSLeftPanel(Transform parent, RCSPrimaryLoopScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "LeftPanel",
                new Vector2(0f, 0.26f), new Vector2(0.15f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 3f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "LeftHeader", "LOOP TEMPERATURES", 11, COLOR_CYAN);

            var g1h = CreateRCSGauge(panelGO.transform, "Gauge_L1_THot", "LOOP 1 T-HOT", "°F");
            var g2h = CreateRCSGauge(panelGO.transform, "Gauge_L2_THot", "LOOP 2 T-HOT", "°F");
            var g3h = CreateRCSGauge(panelGO.transform, "Gauge_L3_THot", "LOOP 3 T-HOT", "°F");
            var g4h = CreateRCSGauge(panelGO.transform, "Gauge_L4_THot", "LOOP 4 T-HOT", "°F");
            var g1c = CreateRCSGauge(panelGO.transform, "Gauge_L1_TCold", "LOOP 1 T-COLD", "°F");
            var g2c = CreateRCSGauge(panelGO.transform, "Gauge_L2_TCold", "LOOP 2 T-COLD", "°F");
            var g3c = CreateRCSGauge(panelGO.transform, "Gauge_L3_TCold", "LOOP 3 T-COLD", "°F");
            var g4c = CreateRCSGauge(panelGO.transform, "Gauge_L4_TCold", "LOOP 4 T-COLD", "°F");

            SerializedObject so = new SerializedObject(screen);
            so.FindProperty("gauge_Loop1_THot").objectReferenceValue = g1h;
            so.FindProperty("gauge_Loop2_THot").objectReferenceValue = g2h;
            so.FindProperty("gauge_Loop3_THot").objectReferenceValue = g3h;
            so.FindProperty("gauge_Loop4_THot").objectReferenceValue = g4h;
            so.FindProperty("gauge_Loop1_TCold").objectReferenceValue = g1c;
            so.FindProperty("gauge_Loop2_TCold").objectReferenceValue = g2c;
            so.FindProperty("gauge_Loop3_TCold").objectReferenceValue = g3c;
            so.FindProperty("gauge_Loop4_TCold").objectReferenceValue = g4c;
            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // Screen 2: Center Panel — 2D RCS Mimic Schematic
        // ----------------------------------------------------------------

        private static void BuildRCSCenterPanel(Transform parent, RCSPrimaryLoopScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "CenterPanel",
                new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f), COLOR_CENTER_BG);

            CreateTMPLabel(panelGO.transform, "ScreenTitle", "RCS PRIMARY LOOP", 16, COLOR_CYAN,
                new Vector2(0.02f, 0.93f), new Vector2(0.98f, 0.99f));

            CreateTMPLabel(panelGO.transform, "ScreenSubtitle",
                "WESTINGHOUSE 4-LOOP PWR \u2014 REACTOR COOLANT SYSTEM", 10, COLOR_TEXT_LABEL,
                new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.93f));

            SerializedObject so = new SerializedObject(screen);

            // ---- Reactor Vessel (center) ----
            Image reactorVessel = CreateMimicBox(panelGO.transform, "ReactorVessel",
                new Vector2(0.42f, 0.25f), new Vector2(0.58f, 0.75f),
                new Color(0.3f, 0.15f, 0.15f));
            so.FindProperty("mimic_ReactorVessel").objectReferenceValue = reactorVessel;

            CreateTMPLabel(reactorVessel.transform, "RxLabel", "REACTOR", 9, COLOR_TEXT_LABEL,
                new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.95f));

            TextMeshProUGUI rxPowerText = CreateTMPText(reactorVessel.transform, "RxPowerText",
                "0.0%", 16, COLOR_GREEN,
                new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.75f));
            rxPowerText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("mimic_ReactorPowerText").objectReferenceValue = rxPowerText;

            TextMeshProUGUI rxTavgText = CreateTMPText(reactorVessel.transform, "RxTavgText",
                "--- \u00b0F", 12, COLOR_GREEN,
                new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.45f));
            rxTavgText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("mimic_ReactorTavgText").objectReferenceValue = rxTavgText;

            // ---- Pressurizer (above reactor, connected to Loop 2 hot leg) ----
            Image pzr = CreateMimicBox(panelGO.transform, "Pressurizer",
                new Vector2(0.60f, 0.72f), new Vector2(0.70f, 0.87f),
                new Color(0.25f, 0.15f, 0.30f));
            so.FindProperty("mimic_Pressurizer").objectReferenceValue = pzr;

            TextMeshProUGUI pzrText = CreateTMPText(pzr.transform, "PZRText",
                "PZR\n---", 8, COLOR_GREEN,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
            pzrText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("mimic_PZRText").objectReferenceValue = pzrText;

            // Surge line from PZR to Loop 2 hot leg
            CreateMimicBox(panelGO.transform, "SurgeLine",
                new Vector2(0.58f, 0.68f), new Vector2(0.62f, 0.72f),
                new Color(0.4f, 0.4f, 0.4f, 0.5f));

            // ---- 4 Loops at 90° intervals (arranged as quadrants) ----
            //
            //  Layout:   Loop 1 (top-right)    Loop 2 (bottom-right)
            //            Loop 4 (top-left)     Loop 3 (bottom-left)
            //
            //  Each loop: Hot Leg → SG → Cold Leg → RCP → Crossover → Vessel

            // Geometric positions for each loop
            // [hotLeg, SG, coldLeg, RCP, crossover] anchors
            float[][] loopData = new float[][] {
                // Loop 1 (top-right): HL goes right, CL returns right
                //   Hot leg:       vessel right → SG
                //   SG:            right of vessel, upper
                //   Cold leg:      SG → RCP
                //   RCP:           below SG
                //   Crossover:     RCP → vessel
                new float[] { 0.58f, 0.60f, 0.78f, 0.63f,   // Hot leg (xMin,yMin,xMax,yMax)
                              0.78f, 0.55f, 0.92f, 0.75f,   // SG
                              0.78f, 0.48f, 0.92f, 0.52f,   // Cold leg
                              0.80f, 0.37f, 0.90f, 0.47f,   // RCP
                              0.58f, 0.40f, 0.80f, 0.43f }, // Crossover

                // Loop 2 (bottom-right): HL goes right-low, CL returns
                new float[] { 0.58f, 0.37f, 0.78f, 0.40f,   // Hot leg
                              0.78f, 0.22f, 0.92f, 0.42f,   // SG
                              0.78f, 0.17f, 0.92f, 0.21f,   // Cold leg
                              0.80f, 0.07f, 0.90f, 0.17f,   // RCP
                              0.58f, 0.10f, 0.80f, 0.13f }, // Crossover

                // Loop 3 (bottom-left): HL goes left-low, CL returns
                new float[] { 0.22f, 0.37f, 0.42f, 0.40f,   // Hot leg
                              0.08f, 0.22f, 0.22f, 0.42f,   // SG
                              0.08f, 0.17f, 0.22f, 0.21f,   // Cold leg
                              0.10f, 0.07f, 0.20f, 0.17f,   // RCP
                              0.20f, 0.10f, 0.42f, 0.13f }, // Crossover

                // Loop 4 (top-left): HL goes left-high, CL returns
                new float[] { 0.22f, 0.60f, 0.42f, 0.63f,   // Hot leg
                              0.08f, 0.55f, 0.22f, 0.75f,   // SG
                              0.08f, 0.48f, 0.22f, 0.52f,   // Cold leg
                              0.10f, 0.37f, 0.20f, 0.47f,   // RCP
                              0.20f, 0.40f, 0.42f, 0.43f }, // Crossover
            };

            string[] sgNames = { "SG-A", "SG-B", "SG-C", "SG-D" };
            string[] loopLabels = { "LOOP 1", "LOOP 2", "LOOP 3", "LOOP 4" };

            // Serialized array properties
            SerializedProperty hotLegsProp = so.FindProperty("mimic_HotLegs");
            SerializedProperty coldLegsProp = so.FindProperty("mimic_ColdLegs");
            SerializedProperty crossoverProp = so.FindProperty("mimic_CrossoverLegs");
            SerializedProperty sgProp = so.FindProperty("mimic_SteamGenerators");
            SerializedProperty sgLabelProp = so.FindProperty("mimic_SGLabels");
            SerializedProperty rcpIconProp = so.FindProperty("mimic_RCPIcons");
            SerializedProperty rcpTextProp = so.FindProperty("mimic_RCPStatusTexts");
            SerializedProperty tHotTextProp = so.FindProperty("mimic_THotTexts");
            SerializedProperty tColdTextProp = so.FindProperty("mimic_TColdTexts");

            Color hotLegColor = new Color(0.8f, 0.3f, 0.2f);
            Color coldLegColor = new Color(0.2f, 0.4f, 0.8f);
            Color crossoverColor = new Color(0.4f, 0.3f, 0.5f);

            for (int i = 0; i < 4; i++)
            {
                float[] d = loopData[i];

                // Hot Leg
                Image hotLeg = CreateMimicBox(panelGO.transform, $"HotLeg_{i + 1}",
                    new Vector2(d[0], d[1]), new Vector2(d[2], d[3]), hotLegColor);
                hotLegsProp.GetArrayElementAtIndex(i).objectReferenceValue = hotLeg;

                // T-Hot label near hot leg
                TextMeshProUGUI tHotText = CreateTMPText(panelGO.transform, $"THot_{i + 1}",
                    "--- \u00b0F", 8, new Color(1f, 0.5f, 0.3f),
                    new Vector2(d[0], d[3]), new Vector2(d[2], d[3] + 0.05f));
                tHotText.alignment = TextAlignmentOptions.Center;
                tHotTextProp.GetArrayElementAtIndex(i).objectReferenceValue = tHotText;

                // Steam Generator
                Image sg = CreateMimicBox(panelGO.transform, $"SG_{sgNames[i]}",
                    new Vector2(d[4], d[5]), new Vector2(d[6], d[7]),
                    new Color(0.15f, 0.25f, 0.30f));
                sgProp.GetArrayElementAtIndex(i).objectReferenceValue = sg;

                TextMeshProUGUI sgLabel = CreateTMPText(sg.transform, "Label",
                    sgNames[i], 9, COLOR_TEXT_LABEL,
                    new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.95f));
                sgLabel.alignment = TextAlignmentOptions.Center;
                sgLabelProp.GetArrayElementAtIndex(i).objectReferenceValue = sgLabel;

                // Cold Leg (from SG output)
                Image coldLeg = CreateMimicBox(panelGO.transform, $"ColdLeg_{i + 1}",
                    new Vector2(d[8], d[9]), new Vector2(d[10], d[11]), coldLegColor);
                coldLegsProp.GetArrayElementAtIndex(i).objectReferenceValue = coldLeg;

                // T-Cold label near cold leg
                TextMeshProUGUI tColdText = CreateTMPText(panelGO.transform, $"TCold_{i + 1}",
                    "--- \u00b0F", 8, new Color(0.3f, 0.5f, 1f),
                    new Vector2(d[8], d[9] - 0.05f), new Vector2(d[10], d[9]));
                tColdText.alignment = TextAlignmentOptions.Center;
                tColdTextProp.GetArrayElementAtIndex(i).objectReferenceValue = tColdText;

                // RCP
                Image rcpIcon = CreateMimicBox(panelGO.transform, $"RCP_{i + 1}",
                    new Vector2(d[12], d[13]), new Vector2(d[14], d[15]),
                    new Color(0.35f, 0.35f, 0.35f));
                rcpIconProp.GetArrayElementAtIndex(i).objectReferenceValue = rcpIcon;

                TextMeshProUGUI rcpText = CreateTMPText(rcpIcon.transform, "StatusText",
                    "OFF", 8, COLOR_TEXT_LABEL,
                    new Vector2(0f, 0f), new Vector2(1f, 1f));
                rcpText.alignment = TextAlignmentOptions.Center;
                rcpTextProp.GetArrayElementAtIndex(i).objectReferenceValue = rcpText;

                // Crossover Leg (RCP → Vessel)
                Image crossover = CreateMimicBox(panelGO.transform, $"Crossover_{i + 1}",
                    new Vector2(d[16], d[17]), new Vector2(d[18], d[19]), crossoverColor);
                crossoverProp.GetArrayElementAtIndex(i).objectReferenceValue = crossover;

                // Loop label
                CreateTMPLabel(panelGO.transform, $"LoopLabel_{i + 1}", loopLabels[i], 8, COLOR_TEXT_LABEL,
                    new Vector2(d[4], d[7]), new Vector2(d[6], d[7] + 0.04f));
            }

            // Flow direction labels
            CreateTMPLabel(panelGO.transform, "FlowNote",
                "\u2192 HOT LEG    \u2190 COLD LEG    \u25cb RCP", 8, COLOR_TEXT_LABEL,
                new Vector2(0.15f, 0.02f), new Vector2(0.85f, 0.07f));

            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // Screen 2: Right Panel — 8 Flow/Power Gauges
        // ----------------------------------------------------------------

        private static void BuildRCSRightPanel(Transform parent, RCSPrimaryLoopScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "RightPanel",
                new Vector2(0.65f, 0.26f), new Vector2(1f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 3f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "RightHeader", "FLOW & POWER", 11, COLOR_CYAN);

            var gTotal = CreateRCSGauge(panelGO.transform, "Gauge_TotalFlow", "TOTAL RCS FLOW", "gpm");
            var gF1 = CreateRCSGauge(panelGO.transform, "Gauge_L1_Flow", "LOOP 1 FLOW", "gpm");
            var gF2 = CreateRCSGauge(panelGO.transform, "Gauge_L2_Flow", "LOOP 2 FLOW", "gpm");
            var gF3 = CreateRCSGauge(panelGO.transform, "Gauge_L3_Flow", "LOOP 3 FLOW", "gpm");
            var gF4 = CreateRCSGauge(panelGO.transform, "Gauge_L4_Flow", "LOOP 4 FLOW", "gpm");
            var gPower = CreateRCSGauge(panelGO.transform, "Gauge_CorePower", "CORE POWER", "MW");
            var gDT = CreateRCSGauge(panelGO.transform, "Gauge_CoreDeltaT", "CORE ΔT", "°F");
            var gTavg = CreateRCSGauge(panelGO.transform, "Gauge_AvgTavg", "AVG T-AVG", "°F");

            SerializedObject so = new SerializedObject(screen);
            so.FindProperty("gauge_TotalFlow").objectReferenceValue = gTotal;
            so.FindProperty("gauge_Loop1_Flow").objectReferenceValue = gF1;
            so.FindProperty("gauge_Loop2_Flow").objectReferenceValue = gF2;
            so.FindProperty("gauge_Loop3_Flow").objectReferenceValue = gF3;
            so.FindProperty("gauge_Loop4_Flow").objectReferenceValue = gF4;
            so.FindProperty("gauge_CorePower").objectReferenceValue = gPower;
            so.FindProperty("gauge_CoreDeltaT").objectReferenceValue = gDT;
            so.FindProperty("gauge_AverageTavg").objectReferenceValue = gTavg;
            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // Screen 2: Bottom Panel — 4 RCP Panels + Status + Alarms
        // ----------------------------------------------------------------

        private static void BuildRCSBottomPanel(Transform parent, RCSPrimaryLoopScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "BottomPanel",
                new Vector2(0f, 0f), new Vector2(1f, 0.26f), COLOR_PANEL);

            // 4 RCP control panels
            for (int i = 0; i < 4; i++)
            {
                float xMin = 0.01f + i * 0.15f;
                float xMax = xMin + 0.14f;

                RCPControlPanel rcpPanel = BuildSingleRCPPanel(panelGO.transform, i + 1,
                    new Vector2(xMin, 0.05f), new Vector2(xMax, 0.95f));

                SerializedObject so = new SerializedObject(screen);
                string fieldName = $"rcpPanel_{i + 1}";
                SerializedProperty prop = so.FindProperty(fieldName);
                if (prop != null)
                {
                    prop.objectReferenceValue = rcpPanel;
                    so.ApplyModifiedProperties();
                }
            }

            // Status section
            GameObject statusSection = CreatePanel(panelGO.transform, "StatusSection",
                new Vector2(0.61f, 0.05f), new Vector2(0.80f, 0.95f), COLOR_GAUGE_BG);

            CreateTMPSectionLabel(statusSection.transform, "STATUS");

            TextMeshProUGUI txtRCPCount = CreateTMPText(statusSection.transform, "RCPCountText",
                "RCPs: 0/4", 13, COLOR_GREEN,
                new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.80f));

            TextMeshProUGUI txtCircMode = CreateTMPText(statusSection.transform, "CircModeText",
                "FORCED CIRCULATION", 11, COLOR_TEXT_NORMAL,
                new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.58f));

            TextMeshProUGUI txtPlantMode = CreateTMPText(statusSection.transform, "PlantModeText",
                "MODE 5", 13, COLOR_AMBER,
                new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.38f));

            GameObject indicGO = new GameObject("NatCircIndicator");
            indicGO.transform.SetParent(statusSection.transform, false);
            RectTransform indicRect = indicGO.AddComponent<RectTransform>();
            indicRect.anchorMin = new Vector2(0.40f, 0.05f);
            indicRect.anchorMax = new Vector2(0.60f, 0.15f);
            indicRect.offsetMin = Vector2.zero;
            indicRect.offsetMax = Vector2.zero;
            Image indicImg = indicGO.AddComponent<Image>();
            indicImg.color = new Color(0.3f, 0.3f, 0.3f);

            SerializedObject soScreen = new SerializedObject(screen);
            soScreen.FindProperty("text_RCPCount").objectReferenceValue = txtRCPCount;
            soScreen.FindProperty("text_CirculationMode").objectReferenceValue = txtCircMode;
            soScreen.FindProperty("text_PlantMode").objectReferenceValue = txtPlantMode;
            soScreen.FindProperty("indicator_NaturalCirc").objectReferenceValue = indicImg;
            soScreen.ApplyModifiedProperties();

            // Alarm section
            GameObject alarmSection = CreatePanel(panelGO.transform, "AlarmSection",
                new Vector2(0.81f, 0.05f), new Vector2(0.99f, 0.95f), COLOR_GAUGE_BG);

            CreateTMPSectionLabel(alarmSection.transform, "ALARMS");

            GameObject alarmContainer = new GameObject("AlarmListContainer");
            alarmContainer.transform.SetParent(alarmSection.transform, false);
            RectTransform alarmContRect = alarmContainer.AddComponent<RectTransform>();
            alarmContRect.anchorMin = new Vector2(0.02f, 0.02f);
            alarmContRect.anchorMax = new Vector2(0.98f, 0.85f);
            alarmContRect.offsetMin = Vector2.zero;
            alarmContRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup alarmVLG = alarmContainer.AddComponent<VerticalLayoutGroup>();
            alarmVLG.padding = new RectOffset(2, 2, 2, 2);
            alarmVLG.spacing = 1f;
            alarmVLG.childAlignment = TextAnchor.UpperLeft;
            alarmVLG.childControlWidth = true;
            alarmVLG.childControlHeight = false;
            alarmVLG.childForceExpandWidth = true;
            alarmVLG.childForceExpandHeight = false;

            soScreen = new SerializedObject(screen);
            soScreen.FindProperty("alarmListContainer").objectReferenceValue = alarmContainer.transform;
            soScreen.ApplyModifiedProperties();
        }

        /// <summary>
        /// Build a single RCP control panel.
        /// </summary>
        private static RCPControlPanel BuildSingleRCPPanel(Transform parent, int pumpNumber,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject panelGO = new GameObject($"RCPPanel_{pumpNumber}");
            panelGO.transform.SetParent(parent, false);

            RectTransform rect = panelGO.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(2f, 2f);
            rect.offsetMax = new Vector2(-2f, -2f);

            Image bg = panelGO.AddComponent<Image>();
            bg.color = COLOR_GAUGE_BG;

            RCPControlPanel rcpPanel = panelGO.AddComponent<RCPControlPanel>();

            TextMeshProUGUI pumpLabel = CreateTMPText(panelGO.transform, "PumpLabel",
                $"RCP-{pumpNumber}", 13, COLOR_CYAN,
                new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.98f));
            pumpLabel.fontStyle = FontStyles.Bold;
            pumpLabel.alignment = TextAlignmentOptions.Center;

            // Status indicator
            GameObject statusIndGO = new GameObject("StatusIndicator");
            statusIndGO.transform.SetParent(panelGO.transform, false);
            RectTransform statusIndRect = statusIndGO.AddComponent<RectTransform>();
            statusIndRect.anchorMin = new Vector2(0.05f, 0.72f);
            statusIndRect.anchorMax = new Vector2(0.25f, 0.83f);
            statusIndRect.offsetMin = Vector2.zero;
            statusIndRect.offsetMax = Vector2.zero;
            Image statusIndImg = statusIndGO.AddComponent<Image>();
            statusIndImg.color = new Color(0.5f, 0.5f, 0.5f);

            TextMeshProUGUI statusText = CreateTMPText(panelGO.transform, "StatusText",
                "STOPPED", 10, COLOR_TEXT_NORMAL,
                new Vector2(0.28f, 0.72f), new Vector2(0.95f, 0.83f));

            TextMeshProUGUI speedText = CreateTMPText(panelGO.transform, "SpeedText",
                "Speed: 0 RPM", 10, COLOR_TEXT_NORMAL,
                new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.70f));

            TextMeshProUGUI flowText = CreateTMPText(panelGO.transform, "FlowText",
                "Flow: 0.0%", 10, COLOR_TEXT_NORMAL,
                new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.57f));

            TextMeshProUGUI ampsText = CreateTMPText(panelGO.transform, "AmpsText",
                "Amps: 0 A", 10, COLOR_TEXT_NORMAL,
                new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.44f));

            Button startBtn = CreateTMPButton(panelGO.transform, "StartBtn", "START",
                new Vector2(0.05f, 0.05f), new Vector2(0.47f, 0.28f),
                new Color(0.15f, 0.35f, 0.15f));

            Button stopBtn = CreateTMPButton(panelGO.transform, "StopBtn", "STOP",
                new Vector2(0.53f, 0.05f), new Vector2(0.95f, 0.28f),
                new Color(0.35f, 0.15f, 0.15f));

            SerializedObject so = new SerializedObject(rcpPanel);
            so.FindProperty("text_PumpLabel").objectReferenceValue = pumpLabel;
            so.FindProperty("text_Status").objectReferenceValue = statusText;
            so.FindProperty("text_Speed").objectReferenceValue = speedText;
            so.FindProperty("text_Flow").objectReferenceValue = flowText;
            so.FindProperty("text_Amps").objectReferenceValue = ampsText;

            SerializedProperty propStartBtn = so.FindProperty("button_Start");
            if (propStartBtn != null) propStartBtn.objectReferenceValue = startBtn;

            SerializedProperty propStopBtn = so.FindProperty("button_Stop");
            if (propStopBtn != null) propStopBtn.objectReferenceValue = stopBtn;

            SerializedProperty propStatusImg = so.FindProperty("image_StatusIndicator");
            if (propStatusImg != null) propStatusImg.objectReferenceValue = statusIndImg;

            so.ApplyModifiedProperties();

            return rcpPanel;
        }

        #endregion

        // ####################################################################
        //
        //  SCREEN TAB — PLANT OVERVIEW
        //
        //  Provides a high-level plant-wide view with a simplified
        //  mimic diagram showing reactor, RCS loops, pressurizer,
        //  steam generators, turbine-generator, and condenser.
        //
        // ####################################################################

        #region Screen Tab - Plant Overview

        /// <summary>
        /// Build the complete Plant Overview screen (Tab key) UI hierarchy.
        /// </summary>
        private static void CreatePlantOverviewScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen Tab — Plant Overview...");

            foreach (Transform child in canvasParent)
            {
                if (child.name == "PlantOverviewScreen")
                {
                    Debug.LogWarning("[MultiScreenBuilder] PlantOverviewScreen already exists. Skipping.");
                    return;
                }
            }

            // --- Root panel ---
            GameObject screenGO = new GameObject("PlantOverviewScreen");
            screenGO.transform.SetParent(canvasParent, false);

            RectTransform rootRect = screenGO.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image rootBg = screenGO.AddComponent<Image>();
            rootBg.color = COLOR_BACKGROUND;

            screenGO.AddComponent<CanvasGroup>();

            PlantOverviewScreen screen = screenGO.AddComponent<PlantOverviewScreen>();

            // --- Build panels ---
            BuildOverviewLeftPanel(screenGO.transform, screen);
            BuildOverviewCenterPanel(screenGO.transform, screen);
            BuildOverviewRightPanel(screenGO.transform, screen);
            BuildOverviewBottomPanel(screenGO.transform, screen);

            // --- Start hidden ---
            screenGO.SetActive(false);

            Debug.Log("[MultiScreenBuilder] Screen Tab — Plant Overview — build complete");
        }

        // ----------------------------------------------------------------
        // Plant Overview: Left Panel — 8 Nuclear/Primary Gauges (TMP)
        // ----------------------------------------------------------------

        private static void BuildOverviewLeftPanel(Transform parent, PlantOverviewScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "LeftPanel",
                new Vector2(0f, 0.26f), new Vector2(0.15f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "LeftHeader", "NUCLEAR / PRIMARY", 11, COLOR_CYAN);

            SerializedObject so = new SerializedObject(screen);

            // Each gauge: label row + value row in a sub-panel
            so.FindProperty("text_ReactorPower").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "ReactorPower", "REACTOR POWER");
            so.FindProperty("text_Tavg").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "Tavg", "T-AVG");
            so.FindProperty("text_RCSPressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "RCSPressure", "RCS PRESSURE");
            so.FindProperty("text_PZRLevel").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "PZRLevel", "PZR LEVEL");
            so.FindProperty("text_TotalRCSFlow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "TotalRCSFlow", "TOTAL RCS FLOW");
            so.FindProperty("text_RodPosition").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "RodPosition", "ROD POSITION (D)");
            so.FindProperty("text_BoronConc").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "BoronConc", "BORON CONC");
            so.FindProperty("text_XenonWorth").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "XenonWorth", "XENON WORTH");

            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // Plant Overview: Center Panel — Mimic Diagram
        // ----------------------------------------------------------------

        private static void BuildOverviewCenterPanel(Transform parent, PlantOverviewScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "CenterPanel",
                new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f), COLOR_CENTER_BG);

            // Title
            CreateTMPLabel(panelGO.transform, "ScreenTitle", "PLANT OVERVIEW", 16, COLOR_CYAN,
                new Vector2(0.02f, 0.93f), new Vector2(0.98f, 0.99f));

            CreateTMPLabel(panelGO.transform, "ScreenSubtitle",
                "WESTINGHOUSE 4-LOOP PWR — 3411 MWt / 1150 MWe", 10, COLOR_TEXT_LABEL,
                new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.93f));

            // Mimic diagram container
            GameObject mimicGO = new GameObject("MimicDiagram");
            mimicGO.transform.SetParent(panelGO.transform, false);

            RectTransform mimicRect = mimicGO.AddComponent<RectTransform>();
            mimicRect.anchorMin = new Vector2(0.02f, 0.02f);
            mimicRect.anchorMax = new Vector2(0.98f, 0.87f);
            mimicRect.offsetMin = Vector2.zero;
            mimicRect.offsetMax = Vector2.zero;

            SerializedObject so = new SerializedObject(screen);

            // --- Reactor Vessel (center-left) ---
            Image reactorVessel = CreateMimicBox(mimicGO.transform, "ReactorVessel",
                new Vector2(0.08f, 0.30f), new Vector2(0.22f, 0.80f),
                new Color(0.3f, 0.15f, 0.15f));
            so.FindProperty("mimic_ReactorVessel").objectReferenceValue = reactorVessel;

            CreateTMPLabel(reactorVessel.transform, "RxLabel", "REACTOR", 9, COLOR_TEXT_LABEL,
                new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.95f));

            TextMeshProUGUI rxPowerText = CreateTMPText(reactorVessel.transform, "RxPowerText",
                "0.0%", 18, COLOR_GREEN,
                new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.70f));
            rxPowerText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("mimic_ReactorPowerText").objectReferenceValue = rxPowerText;

            // --- Pressurizer (above reactor) ---
            Image pzr = CreateMimicBox(mimicGO.transform, "Pressurizer",
                new Vector2(0.24f, 0.72f), new Vector2(0.34f, 0.95f),
                new Color(0.25f, 0.15f, 0.30f));
            so.FindProperty("mimic_Pressurizer").objectReferenceValue = pzr;

            // PZR level fill bar
            Image pzrFill = CreateMimicFill(pzr.transform, "PZRFill",
                new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.60f),
                new Color(0.2f, 0.5f, 0.8f));
            so.FindProperty("mimic_PZRLevelFill").objectReferenceValue = pzrFill;

            TextMeshProUGUI pzrText = CreateTMPText(pzr.transform, "PZRText",
                "---\n---", 9, COLOR_GREEN,
                new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.95f));
            pzrText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("mimic_PZRText").objectReferenceValue = pzrText;

            // --- Hot Legs (4 lines from reactor to SGs) ---
            Color hotLegColor = new Color(0.8f, 0.3f, 0.2f);
            Color coldLegColor = new Color(0.2f, 0.4f, 0.8f);

            float[] sgYPositions = { 0.68f, 0.48f, 0.28f, 0.08f };
            float[] sgYTops = { 0.88f, 0.68f, 0.48f, 0.28f };

            SerializedProperty hotLegsProp = so.FindProperty("mimic_HotLegs");
            SerializedProperty coldLegsProp = so.FindProperty("mimic_ColdLegs");
            SerializedProperty sgProp = so.FindProperty("mimic_SteamGenerators");
            SerializedProperty sgFillProp = so.FindProperty("mimic_SGLevelFills");
            SerializedProperty sgLabelProp = so.FindProperty("mimic_SGLabels");

            for (int i = 0; i < 4; i++)
            {
                float yBot = sgYPositions[i];
                float yTop = sgYTops[i];
                float yMid = (yBot + yTop) * 0.5f;

                // Hot leg line (reactor right side → SG left side)
                Image hotLeg = CreateMimicBox(mimicGO.transform, $"HotLeg_{i + 1}",
                    new Vector2(0.22f, yMid + 0.04f), new Vector2(0.40f, yMid + 0.07f),
                    hotLegColor);
                hotLegsProp.GetArrayElementAtIndex(i).objectReferenceValue = hotLeg;

                // Cold leg line (SG right side → reactor left side via bottom)
                Image coldLeg = CreateMimicBox(mimicGO.transform, $"ColdLeg_{i + 1}",
                    new Vector2(0.22f, yMid - 0.02f), new Vector2(0.40f, yMid + 0.01f),
                    coldLegColor);
                coldLegsProp.GetArrayElementAtIndex(i).objectReferenceValue = coldLeg;

                // Steam Generator
                string sgLetter = ((char)('A' + i)).ToString();
                Image sg = CreateMimicBox(mimicGO.transform, $"SG_{sgLetter}",
                    new Vector2(0.40f, yBot), new Vector2(0.52f, yTop),
                    new Color(0.15f, 0.25f, 0.30f));
                sgProp.GetArrayElementAtIndex(i).objectReferenceValue = sg;

                // SG level fill
                Image sgFill = CreateMimicFill(sg.transform, $"SGFill_{sgLetter}",
                    new Vector2(0.1f, 0.05f), new Vector2(0.45f, 0.80f),
                    new Color(0.2f, 0.6f, 0.8f));
                sgFillProp.GetArrayElementAtIndex(i).objectReferenceValue = sgFill;

                // SG label
                TextMeshProUGUI sgLabel = CreateTMPText(sg.transform, $"SGLabel_{sgLetter}",
                    $"SG-{sgLetter}\n--- psig", 8, COLOR_TEXT_NORMAL,
                    new Vector2(0.48f, 0.10f), new Vector2(0.98f, 0.90f));
                sgLabel.alignment = TextAlignmentOptions.Left;
                sgLabelProp.GetArrayElementAtIndex(i).objectReferenceValue = sgLabel;
            }

            // --- Main Steam Line (SGs to Turbine) ---
            Image mainSteamLine = CreateMimicBox(mimicGO.transform, "MainSteamLine",
                new Vector2(0.52f, 0.45f), new Vector2(0.62f, 0.48f),
                new Color(0.7f, 0.7f, 0.7f));
            so.FindProperty("mimic_MainSteamLine").objectReferenceValue = mainSteamLine;

            // --- Turbine ---
            Image turbine = CreateMimicBox(mimicGO.transform, "Turbine",
                new Vector2(0.62f, 0.35f), new Vector2(0.78f, 0.60f),
                new Color(0.25f, 0.25f, 0.15f));
            so.FindProperty("mimic_Turbine").objectReferenceValue = turbine;

            CreateTMPLabel(turbine.transform, "TurbLabel", "TURBINE", 9, COLOR_TEXT_LABEL,
                new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.95f));

            TextMeshProUGUI turbText = CreateTMPText(turbine.transform, "TurbPowerText",
                "---\nMWt", 12, COLOR_PLACEHOLDER_MIMIC,
                new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.65f));
            turbText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("mimic_TurbineText").objectReferenceValue = turbText;

            // --- Generator ---
            Image generator = CreateMimicBox(mimicGO.transform, "Generator",
                new Vector2(0.80f, 0.35f), new Vector2(0.95f, 0.60f),
                new Color(0.20f, 0.25f, 0.20f));
            so.FindProperty("mimic_Generator").objectReferenceValue = generator;

            CreateTMPLabel(generator.transform, "GenLabel", "GENERATOR", 9, COLOR_TEXT_LABEL,
                new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.95f));

            TextMeshProUGUI genText = CreateTMPText(generator.transform, "GenPowerText",
                "---\nMWe", 12, COLOR_PLACEHOLDER_MIMIC,
                new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.65f));
            genText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("mimic_GeneratorText").objectReferenceValue = genText;

            // --- Condenser (below turbine) ---
            Image condenser = CreateMimicBox(mimicGO.transform, "Condenser",
                new Vector2(0.62f, 0.08f), new Vector2(0.78f, 0.30f),
                new Color(0.15f, 0.15f, 0.25f));
            so.FindProperty("mimic_Condenser").objectReferenceValue = condenser;

            CreateTMPLabel(condenser.transform, "CondLabel", "CONDENSER", 9, COLOR_TEXT_LABEL,
                new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.90f));

            // --- Feedwater Line (Condenser back to SGs) ---
            Image fwLine = CreateMimicBox(mimicGO.transform, "FeedwaterLine",
                new Vector2(0.40f, 0.02f), new Vector2(0.62f, 0.05f),
                new Color(0.2f, 0.5f, 0.3f));
            so.FindProperty("mimic_FeedwaterLine").objectReferenceValue = fwLine;

            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // Plant Overview: Right Panel — 8 Secondary/Output Gauges (TMP)
        // ----------------------------------------------------------------

        private static void BuildOverviewRightPanel(Transform parent, PlantOverviewScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "RightPanel",
                new Vector2(0.65f, 0.26f), new Vector2(1f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "RightHeader", "SECONDARY / OUTPUT", 11, COLOR_CYAN);

            SerializedObject so = new SerializedObject(screen);

            so.FindProperty("text_SGLevelAvg").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGLevelAvg", "SG LEVEL AVG");
            so.FindProperty("text_SteamPressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SteamPressure", "STEAM PRESSURE");
            so.FindProperty("text_FeedwaterFlow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "FeedwaterFlow", "FEEDWATER FLOW");
            so.FindProperty("text_TurbinePower").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "TurbinePower", "TURBINE POWER");
            so.FindProperty("text_GeneratorOutput").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "GeneratorOutput", "GENERATOR OUTPUT");
            so.FindProperty("text_CondenserVacuum").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "CondenserVacuum", "CONDENSER VACUUM");
            so.FindProperty("text_FeedwaterTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "FeedwaterTemp", "FEEDWATER TEMP");
            so.FindProperty("text_MainSteamFlow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "MainSteamFlow", "MAIN STEAM FLOW");

            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // Plant Overview: Bottom Panel — Status, RCP indicators, Alarms
        // ----------------------------------------------------------------

        private static void BuildOverviewBottomPanel(Transform parent, PlantOverviewScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "BottomPanel",
                new Vector2(0f, 0f), new Vector2(1f, 0.26f), COLOR_PANEL);

            SerializedObject so = new SerializedObject(screen);

            // --- Reactor Mode Section ---
            GameObject modeSection = CreatePanel(panelGO.transform, "ReactorModeSection",
                new Vector2(0.01f, 0.05f), new Vector2(0.15f, 0.95f), COLOR_GAUGE_BG);

            CreateTMPSectionLabel(modeSection.transform, "REACTOR MODE");

            TextMeshProUGUI modeText = CreateTMPText(modeSection.transform, "ModeText",
                "MODE 5", 16, COLOR_AMBER,
                new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.80f));
            modeText.alignment = TextAlignmentOptions.Center;
            modeText.fontStyle = FontStyles.Bold;
            so.FindProperty("text_ReactorMode").objectReferenceValue = modeText;

            // --- RCP Status Section ---
            GameObject rcpSection = CreatePanel(panelGO.transform, "RCPStatusSection",
                new Vector2(0.16f, 0.05f), new Vector2(0.38f, 0.95f), COLOR_GAUGE_BG);

            CreateTMPSectionLabel(rcpSection.transform, "RCP STATUS");

            SerializedProperty rcpIndProp = so.FindProperty("indicators_RCP");
            SerializedProperty rcpLblProp = so.FindProperty("labels_RCP");

            for (int i = 0; i < 4; i++)
            {
                float xMin = 0.05f + i * 0.24f;
                float xMax = xMin + 0.20f;

                // Indicator light
                GameObject indGO = new GameObject($"RCP{i + 1}_Indicator");
                indGO.transform.SetParent(rcpSection.transform, false);

                RectTransform indRect = indGO.AddComponent<RectTransform>();
                indRect.anchorMin = new Vector2(xMin, 0.50f);
                indRect.anchorMax = new Vector2(xMax, 0.75f);
                indRect.offsetMin = new Vector2(4f, 0f);
                indRect.offsetMax = new Vector2(-4f, 0f);

                Image indImg = indGO.AddComponent<Image>();
                indImg.color = new Color(0.4f, 0.4f, 0.4f);
                rcpIndProp.GetArrayElementAtIndex(i).objectReferenceValue = indImg;

                // Label
                TextMeshProUGUI rcpLabel = CreateTMPText(rcpSection.transform, $"RCP{i + 1}_Label",
                    $"RCP-{i + 1}", 10, COLOR_TEXT_LABEL,
                    new Vector2(xMin, 0.25f), new Vector2(xMax, 0.48f));
                rcpLabel.alignment = TextAlignmentOptions.Center;
                rcpLblProp.GetArrayElementAtIndex(i).objectReferenceValue = rcpLabel;
            }

            // --- Turbine/Generator Status ---
            GameObject tgSection = CreatePanel(panelGO.transform, "TurbGenSection",
                new Vector2(0.39f, 0.05f), new Vector2(0.55f, 0.95f), COLOR_GAUGE_BG);

            CreateTMPSectionLabel(tgSection.transform, "TURB / GEN");

            // Turbine indicator
            GameObject turbIndGO = new GameObject("TurbineIndicator");
            turbIndGO.transform.SetParent(tgSection.transform, false);
            RectTransform turbIndRect = turbIndGO.AddComponent<RectTransform>();
            turbIndRect.anchorMin = new Vector2(0.05f, 0.55f);
            turbIndRect.anchorMax = new Vector2(0.20f, 0.75f);
            turbIndRect.offsetMin = Vector2.zero;
            turbIndRect.offsetMax = Vector2.zero;
            Image turbIndImg = turbIndGO.AddComponent<Image>();
            turbIndImg.color = new Color(0.4f, 0.4f, 0.4f);
            so.FindProperty("indicator_TurbineStatus").objectReferenceValue = turbIndImg;

            TextMeshProUGUI turbStatusText = CreateTMPText(tgSection.transform, "TurbineStatusText",
                "TURBINE: OFF", 10, COLOR_TEXT_LABEL,
                new Vector2(0.22f, 0.55f), new Vector2(0.95f, 0.75f));
            so.FindProperty("text_TurbineStatus").objectReferenceValue = turbStatusText;

            // Generator breaker indicator
            GameObject genIndGO = new GameObject("GeneratorBreakerIndicator");
            genIndGO.transform.SetParent(tgSection.transform, false);
            RectTransform genIndRect = genIndGO.AddComponent<RectTransform>();
            genIndRect.anchorMin = new Vector2(0.05f, 0.28f);
            genIndRect.anchorMax = new Vector2(0.20f, 0.48f);
            genIndRect.offsetMin = Vector2.zero;
            genIndRect.offsetMax = Vector2.zero;
            Image genIndImg = genIndGO.AddComponent<Image>();
            genIndImg.color = new Color(0.4f, 0.4f, 0.4f);
            so.FindProperty("indicator_GeneratorBreaker").objectReferenceValue = genIndImg;

            TextMeshProUGUI genBkrText = CreateTMPText(tgSection.transform, "GenBreakerText",
                "GEN BKR: OPEN", 10, COLOR_TEXT_LABEL,
                new Vector2(0.22f, 0.28f), new Vector2(0.95f, 0.48f));
            so.FindProperty("text_GeneratorBreaker").objectReferenceValue = genBkrText;

            // --- Alarm Summary ---
            GameObject alarmSection = CreatePanel(panelGO.transform, "AlarmSummarySection",
                new Vector2(0.56f, 0.05f), new Vector2(0.78f, 0.95f), COLOR_GAUGE_BG);

            CreateTMPSectionLabel(alarmSection.transform, "ALARMS");

            GameObject alarmContainer = new GameObject("AlarmSummaryContainer");
            alarmContainer.transform.SetParent(alarmSection.transform, false);
            RectTransform alarmContRect = alarmContainer.AddComponent<RectTransform>();
            alarmContRect.anchorMin = new Vector2(0.02f, 0.02f);
            alarmContRect.anchorMax = new Vector2(0.98f, 0.85f);
            alarmContRect.offsetMin = Vector2.zero;
            alarmContRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup alarmVLG = alarmContainer.AddComponent<VerticalLayoutGroup>();
            alarmVLG.padding = new RectOffset(2, 2, 2, 2);
            alarmVLG.spacing = 1f;
            alarmVLG.childAlignment = TextAnchor.UpperLeft;
            alarmVLG.childControlWidth = true;
            alarmVLG.childControlHeight = false;
            alarmVLG.childForceExpandWidth = true;
            alarmVLG.childForceExpandHeight = false;

            so.FindProperty("alarmSummaryContainer").objectReferenceValue = alarmContainer.transform;

            // --- Time / Controls Section ---
            GameObject timeSection = CreatePanel(panelGO.transform, "TimeControlSection",
                new Vector2(0.79f, 0.05f), new Vector2(0.99f, 0.95f), COLOR_GAUGE_BG);

            CreateTMPSectionLabel(timeSection.transform, "SIMULATION");

            TextMeshProUGUI simTimeText = CreateTMPText(timeSection.transform, "SimTimeText",
                "00:00:00", 16, COLOR_GREEN,
                new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.82f));
            simTimeText.alignment = TextAlignmentOptions.Center;
            simTimeText.fontStyle = FontStyles.Bold;
            so.FindProperty("text_SimTime").objectReferenceValue = simTimeText;

            TextMeshProUGUI timeCompText = CreateTMPText(timeSection.transform, "TimeCompText",
                "1x", 13, COLOR_GREEN,
                new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.58f));
            timeCompText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("text_TimeCompression").objectReferenceValue = timeCompText;

            // Emergency buttons (visual only)
            Button rxTripBtn = CreateTMPButton(timeSection.transform, "RxTripBtn", "RX TRIP",
                new Vector2(0.05f, 0.05f), new Vector2(0.47f, 0.35f),
                new Color(0.5f, 0.1f, 0.1f));
            so.FindProperty("button_ReactorTrip").objectReferenceValue = rxTripBtn;

            Button turbTripBtn = CreateTMPButton(timeSection.transform, "TurbTripBtn", "TURB TRIP",
                new Vector2(0.53f, 0.05f), new Vector2(0.95f, 0.35f),
                new Color(0.4f, 0.25f, 0.1f));
            so.FindProperty("button_TurbineTrip").objectReferenceValue = turbTripBtn;

            so.ApplyModifiedProperties();
        }

        #endregion

        // ####################################################################
        //
        //  SCREEN 3 — PRESSURIZER
        //
        //  Displays pressurizer pressure, level, heater status, spray,
        //  PORV/SV indicators, and a 2D vessel cutaway visualization.
        //
        // ####################################################################

        #region Screen 3 - Pressurizer

        /// <summary>
        /// Build the complete Pressurizer screen (Key 3) UI hierarchy.
        /// </summary>
        private static void CreatePressurizerScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 3 — Pressurizer...");

            foreach (Transform child in canvasParent)
            {
                if (child.name == "PressurizerScreen")
                {
                    Debug.LogWarning("[MultiScreenBuilder] PressurizerScreen already exists. Skipping.");
                    return;
                }
            }

            // --- Root panel ---
            GameObject screenGO = new GameObject("PressurizerScreen");
            screenGO.transform.SetParent(canvasParent, false);

            RectTransform rootRect = screenGO.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image rootBg = screenGO.AddComponent<Image>();
            rootBg.color = COLOR_BACKGROUND;

            screenGO.AddComponent<CanvasGroup>();

            PressurizerScreen screen = screenGO.AddComponent<PressurizerScreen>();

            // --- Build panels ---
            BuildPZRLeftPanel(screenGO.transform, screen);
            BuildPZRCenterPanel(screenGO.transform, screen);
            BuildPZRRightPanel(screenGO.transform, screen);
            BuildPZRBottomPanel(screenGO.transform, screen);

            // --- Start hidden ---
            screenGO.SetActive(false);

            Debug.Log("[MultiScreenBuilder] Screen 3 — Pressurizer — build complete");
        }

        // ----------------------------------------------------------------
        // Pressurizer: Left Panel — 8 Pressure Gauges
        // ----------------------------------------------------------------

        private static void BuildPZRLeftPanel(Transform parent, PressurizerScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "LeftPanel",
                new Vector2(0f, 0.26f), new Vector2(0.15f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "LeftHeader", "PRESSURE", 11, COLOR_CYAN);

            SerializedObject so = new SerializedObject(screen);

            so.FindProperty("text_PZRPressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "PZRPressure", "PZR PRESSURE");
            so.FindProperty("text_PressureSetpoint").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "PressureSetpoint", "PRESSURE SP");
            so.FindProperty("text_PressureError").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "PressureError", "PRESSURE ERROR");
            so.FindProperty("text_PressureRate").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "PressureRate", "PRESSURE RATE");
            so.FindProperty("text_HeaterPower").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "HeaterPower", "HEATER POWER");
            so.FindProperty("text_SprayFlow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SprayFlow", "SPRAY FLOW");
            so.FindProperty("text_BackupHeaterStatus").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "BackupHeaterStatus", "BACKUP HTRS");
            so.FindProperty("text_PORVStatus").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "PORVStatus", "PORV STATUS");

            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // Pressurizer: Center Panel — 2D Vessel Cutaway
        // ----------------------------------------------------------------

        private static void BuildPZRCenterPanel(Transform parent, PressurizerScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "CenterPanel",
                new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f), COLOR_CENTER_BG);

            // Title
            CreateTMPLabel(panelGO.transform, "ScreenTitle", "PRESSURIZER", 16, COLOR_CYAN,
                new Vector2(0.02f, 0.93f), new Vector2(0.98f, 0.99f));

            CreateTMPLabel(panelGO.transform, "ScreenSubtitle",
                "WESTINGHOUSE 4-LOOP PWR — 1800 FT\u00B3 / 2235 PSIA", 10, COLOR_TEXT_LABEL,
                new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.93f));

            SerializedObject so = new SerializedObject(screen);

            // --- Vessel Shell ---
            Image vesselShell = CreateMimicBox(panelGO.transform, "VesselShell",
                new Vector2(0.30f, 0.08f), new Vector2(0.70f, 0.86f),
                new Color(0.25f, 0.25f, 0.30f));
            so.FindProperty("vessel_Shell").objectReferenceValue = vesselShell;

            // Vessel interior background
            CreateMimicBox(vesselShell.transform, "VesselInterior",
                new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.97f),
                new Color(0.10f, 0.10f, 0.15f));

            // --- Water Fill (vertical bottom fill) ---
            Image waterFill = CreateMimicFill(vesselShell.transform, "WaterFill",
                new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f),
                new Color(0.1f, 0.3f, 0.8f, 0.7f));
            so.FindProperty("vessel_WaterFill").objectReferenceValue = waterFill;

            // --- Steam Dome ---
            Image steamDome = CreateMimicBox(vesselShell.transform, "SteamDome",
                new Vector2(0.10f, 0.65f), new Vector2(0.90f, 0.93f),
                new Color(0.6f, 0.6f, 0.7f, 0.3f));
            so.FindProperty("vessel_SteamDome").objectReferenceValue = steamDome;

            CreateTMPLabel(steamDome.transform, "SteamLabel", "STEAM", 9, new Color(0.7f, 0.7f, 0.8f, 0.5f),
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));

            // --- Heater Bars (4 at bottom) ---
            SerializedProperty heaterBarsProp = so.FindProperty("vessel_HeaterBars");
            float[] heaterX = { 0.15f, 0.32f, 0.50f, 0.68f };
            for (int i = 0; i < 4; i++)
            {
                Image bar = CreateMimicBox(vesselShell.transform, $"HeaterBar_{i}",
                    new Vector2(heaterX[i], 0.04f), new Vector2(heaterX[i] + 0.14f, 0.18f),
                    new Color(0.2f, 0.15f, 0.1f));
                heaterBarsProp.GetArrayElementAtIndex(i).objectReferenceValue = bar;
            }

            // --- Spray Indicator (top of vessel) ---
            Image sprayInd = CreateMimicBox(panelGO.transform, "SprayIndicator",
                new Vector2(0.45f, 0.86f), new Vector2(0.55f, 0.92f),
                new Color(0.2f, 0.2f, 0.3f, 0.3f));
            so.FindProperty("vessel_SprayIndicator").objectReferenceValue = sprayInd;

            CreateTMPLabel(sprayInd.transform, "SprayLabel", "SPRAY", 7, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // --- Surge Line (bottom of vessel) ---
            Image surgeLine = CreateMimicBox(panelGO.transform, "SurgeLine",
                new Vector2(0.35f, 0.03f), new Vector2(0.65f, 0.07f),
                new Color(0.4f, 0.4f, 0.4f, 0.5f));
            so.FindProperty("vessel_SurgeLine").objectReferenceValue = surgeLine;

            CreateTMPLabel(surgeLine.transform, "SurgeLabel", "SURGE LINE", 7, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // --- PORV Indicators (2, right side of vessel) ---
            SerializedProperty porvProp = so.FindProperty("vessel_PORVIndicators");
            for (int i = 0; i < 2; i++)
            {
                float yPos = 0.72f + i * 0.08f;
                Image porv = CreateMimicBox(panelGO.transform, $"PORV_{i}",
                    new Vector2(0.71f, yPos), new Vector2(0.78f, yPos + 0.06f),
                    new Color(0.3f, 0.3f, 0.3f));
                porvProp.GetArrayElementAtIndex(i).objectReferenceValue = porv;

                CreateTMPLabel(porv.transform, "Label", $"P{i + 1}", 7, COLOR_TEXT_LABEL,
                    new Vector2(0f, 0f), new Vector2(1f, 1f));
            }

            // --- Safety Valve Indicators (3, left side of vessel) ---
            SerializedProperty svProp = so.FindProperty("vessel_SafetyValveIndicators");
            for (int i = 0; i < 3; i++)
            {
                float yPos = 0.66f + i * 0.08f;
                Image sv = CreateMimicBox(panelGO.transform, $"SV_{i}",
                    new Vector2(0.22f, yPos), new Vector2(0.29f, yPos + 0.06f),
                    new Color(0.3f, 0.3f, 0.3f));
                svProp.GetArrayElementAtIndex(i).objectReferenceValue = sv;

                CreateTMPLabel(sv.transform, "Label", $"S{i + 1}", 7, COLOR_TEXT_LABEL,
                    new Vector2(0f, 0f), new Vector2(1f, 1f));
            }

            // --- Overlay Text ---
            so.FindProperty("vessel_PressureText").objectReferenceValue =
                CreateTMPText(vesselShell.transform, "PressureOverlay", "--- psia", 14, COLOR_GREEN,
                    new Vector2(0.15f, 0.50f), new Vector2(0.85f, 0.62f));

            so.FindProperty("vessel_LevelText").objectReferenceValue =
                CreateTMPText(vesselShell.transform, "LevelOverlay", "--- %", 14, COLOR_GREEN,
                    new Vector2(0.15f, 0.38f), new Vector2(0.85f, 0.50f));

            so.FindProperty("vessel_TempText").objectReferenceValue =
                CreateTMPText(vesselShell.transform, "TempOverlay", "--- F", 12, COLOR_GREEN,
                    new Vector2(0.15f, 0.28f), new Vector2(0.85f, 0.38f));

            so.FindProperty("vessel_HeaterText").objectReferenceValue =
                CreateTMPText(vesselShell.transform, "HeaterOverlay", "--- kW", 12, COLOR_GREEN,
                    new Vector2(0.15f, 0.18f), new Vector2(0.85f, 0.28f));

            // --- Setpoint Reference Lines (labels on sides) ---
            CreateTMPText(panelGO.transform, "SP_Pressure", "SP: 2235 psia", 8, COLOR_TEXT_LABEL,
                new Vector2(0.72f, 0.55f), new Vector2(0.98f, 0.62f));
            CreateTMPText(panelGO.transform, "SP_Level", "SP: 60%", 8, COLOR_TEXT_LABEL,
                new Vector2(0.72f, 0.47f), new Vector2(0.98f, 0.54f));
            CreateTMPText(panelGO.transform, "PORV_SP", "PORV: 2335 psia", 8, COLOR_RED,
                new Vector2(0.72f, 0.39f), new Vector2(0.98f, 0.46f));
            CreateTMPText(panelGO.transform, "SV_SP", "SV: 2485 psia", 8, COLOR_RED,
                new Vector2(0.72f, 0.31f), new Vector2(0.98f, 0.38f));

            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // Pressurizer: Right Panel — 8 Level/Volume Gauges
        // ----------------------------------------------------------------

        private static void BuildPZRRightPanel(Transform parent, PressurizerScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "RightPanel",
                new Vector2(0.65f, 0.26f), new Vector2(1f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "RightHeader", "LEVEL / VOLUME", 11, COLOR_CYAN);

            SerializedObject so = new SerializedObject(screen);

            so.FindProperty("text_PZRLevel").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "PZRLevel", "PZR LEVEL");
            so.FindProperty("text_LevelSetpoint").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "LevelSetpoint", "LEVEL SP");
            so.FindProperty("text_LevelError").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "LevelError", "LEVEL ERROR");
            so.FindProperty("text_SurgeFlow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SurgeFlow", "SURGE FLOW");
            so.FindProperty("text_SteamVolume").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SteamVolume", "STEAM VOLUME");
            so.FindProperty("text_WaterVolume").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "WaterVolume", "WATER VOLUME");
            so.FindProperty("text_PZRWaterTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "PZRWaterTemp", "PZR WATER TEMP");
            so.FindProperty("text_Subcooling").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "Subcooling", "SUBCOOLING");

            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // Pressurizer: Bottom Panel — Controls, Valve Status, Alarms
        // ----------------------------------------------------------------

        private static void BuildPZRBottomPanel(Transform parent, PressurizerScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "BottomPanel",
                new Vector2(0f, 0f), new Vector2(1f, 0.26f), COLOR_PANEL);

            SerializedObject so = new SerializedObject(screen);

            // --- Heater Control Section ---
            GameObject heaterSection = CreatePanel(panelGO.transform, "HeaterControlSection",
                new Vector2(0.01f, 0.05f), new Vector2(0.18f, 0.95f), COLOR_GAUGE_BG);

            CreateTMPSectionLabel(heaterSection.transform, "HEATER CONTROL");

            Button propBtn = CreateTMPButton(heaterSection.transform, "PropHtrBtn", "PROP (660 kW)",
                new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.80f),
                new Color(0.15f, 0.30f, 0.15f));
            so.FindProperty("button_ProportionalHeaters").objectReferenceValue = propBtn;

            Button bkupBtn = CreateTMPButton(heaterSection.transform, "BackupHtrBtn", "BACKUP (1020 kW)",
                new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.45f),
                new Color(0.30f, 0.25f, 0.10f));
            so.FindProperty("button_BackupHeaters").objectReferenceValue = bkupBtn;

            // --- Spray Control Section ---
            GameObject spraySection = CreatePanel(panelGO.transform, "SprayControlSection",
                new Vector2(0.19f, 0.05f), new Vector2(0.33f, 0.95f), COLOR_GAUGE_BG);

            CreateTMPSectionLabel(spraySection.transform, "SPRAY CONTROL");

            Button sprayOpenBtn = CreateTMPButton(spraySection.transform, "SprayOpenBtn", "OPEN",
                new Vector2(0.05f, 0.50f), new Vector2(0.48f, 0.80f),
                new Color(0.15f, 0.30f, 0.15f));
            so.FindProperty("button_SprayOpen").objectReferenceValue = sprayOpenBtn;

            Button sprayCloseBtn = CreateTMPButton(spraySection.transform, "SprayCloseBtn", "CLOSE",
                new Vector2(0.52f, 0.50f), new Vector2(0.95f, 0.80f),
                new Color(0.30f, 0.15f, 0.15f));
            so.FindProperty("button_SprayClose").objectReferenceValue = sprayCloseBtn;

            CreateTMPText(spraySection.transform, "SprayNote", "(NOT MODELED)", 8, COLOR_PLACEHOLDER_MIMIC,
                new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.40f));

            // --- Valve Status Section ---
            GameObject valveSection = CreatePanel(panelGO.transform, "ValveStatusSection",
                new Vector2(0.34f, 0.05f), new Vector2(0.58f, 0.95f), COLOR_GAUGE_BG);

            CreateTMPSectionLabel(valveSection.transform, "RELIEF VALVES");

            // PORV-A
            BuildPZRValveIndicator(valveSection.transform, so, "indicator_PORV_A", "text_PORV_A",
                "PORV-A", new Vector2(0.03f, 0.55f), new Vector2(0.48f, 0.82f));

            // PORV-B
            BuildPZRValveIndicator(valveSection.transform, so, "indicator_PORV_B", "text_PORV_B",
                "PORV-B", new Vector2(0.52f, 0.55f), new Vector2(0.97f, 0.82f));

            // SV-1
            BuildPZRValveIndicator(valveSection.transform, so, "indicator_SV_1", "text_SV_1",
                "SV-1", new Vector2(0.03f, 0.15f), new Vector2(0.33f, 0.48f));

            // SV-2
            BuildPZRValveIndicator(valveSection.transform, so, "indicator_SV_2", "text_SV_2",
                "SV-2", new Vector2(0.35f, 0.15f), new Vector2(0.65f, 0.48f));

            // SV-3
            BuildPZRValveIndicator(valveSection.transform, so, "indicator_SV_3", "text_SV_3",
                "SV-3", new Vector2(0.67f, 0.15f), new Vector2(0.97f, 0.48f));

            // --- Status Section ---
            GameObject statusSection = CreatePanel(panelGO.transform, "StatusSection",
                new Vector2(0.59f, 0.05f), new Vector2(0.78f, 0.95f), COLOR_GAUGE_BG);

            CreateTMPSectionLabel(statusSection.transform, "STATUS");

            TextMeshProUGUI modeText = CreateTMPText(statusSection.transform, "ModeText",
                "MODE 5", 13, COLOR_AMBER,
                new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.85f));
            modeText.alignment = TextAlignmentOptions.Center;
            modeText.fontStyle = FontStyles.Bold;
            so.FindProperty("text_ReactorMode").objectReferenceValue = modeText;

            TextMeshProUGUI simTimeText = CreateTMPText(statusSection.transform, "SimTimeText",
                "00:00:00", 14, COLOR_GREEN,
                new Vector2(0.05f, 0.50f), new Vector2(0.60f, 0.68f));
            so.FindProperty("text_SimTime").objectReferenceValue = simTimeText;

            TextMeshProUGUI timeCompText = CreateTMPText(statusSection.transform, "TimeCompText",
                "1x", 14, COLOR_GREEN,
                new Vector2(0.62f, 0.50f), new Vector2(0.95f, 0.68f));
            so.FindProperty("text_TimeCompression").objectReferenceValue = timeCompText;

            TextMeshProUGUI pidText = CreateTMPText(statusSection.transform, "HeaterPIDText",
                "PID: ---", 11, COLOR_TEXT_NORMAL,
                new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.48f));
            so.FindProperty("text_HeaterPIDStatus").objectReferenceValue = pidText;

            TextMeshProUGUI hzpText = CreateTMPText(statusSection.transform, "HZPText",
                "HZP: ---", 11, COLOR_TEXT_NORMAL,
                new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.30f));
            so.FindProperty("text_HZPStatus").objectReferenceValue = hzpText;

            // --- Alarm Section ---
            GameObject alarmSection = CreatePanel(panelGO.transform, "AlarmSection",
                new Vector2(0.79f, 0.05f), new Vector2(0.99f, 0.95f), COLOR_GAUGE_BG);

            CreateTMPSectionLabel(alarmSection.transform, "ALARMS");

            GameObject alarmContainer = new GameObject("AlarmContainer");
            alarmContainer.transform.SetParent(alarmSection.transform, false);
            RectTransform alarmContRect = alarmContainer.AddComponent<RectTransform>();
            alarmContRect.anchorMin = new Vector2(0.02f, 0.02f);
            alarmContRect.anchorMax = new Vector2(0.98f, 0.85f);
            alarmContRect.offsetMin = Vector2.zero;
            alarmContRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup alarmVLG = alarmContainer.AddComponent<VerticalLayoutGroup>();
            alarmVLG.padding = new RectOffset(2, 2, 2, 2);
            alarmVLG.spacing = 1f;
            alarmVLG.childAlignment = TextAnchor.UpperLeft;
            alarmVLG.childControlWidth = true;
            alarmVLG.childControlHeight = false;
            alarmVLG.childForceExpandWidth = true;
            alarmVLG.childForceExpandHeight = false;

            so.FindProperty("alarmContainer").objectReferenceValue = alarmContainer.transform;

            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// Helper: Build a single valve indicator (Image + TMP label) for the bottom panel.
        /// </summary>
        private static void BuildPZRValveIndicator(Transform parent, SerializedObject so,
            string indicatorPropName, string textPropName, string valveName,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject container = new GameObject(valveName);
            container.transform.SetParent(parent, false);

            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Indicator light (left portion)
            GameObject indGO = new GameObject("Indicator");
            indGO.transform.SetParent(container.transform, false);

            RectTransform indRect = indGO.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0f, 0.2f);
            indRect.anchorMax = new Vector2(0.3f, 0.8f);
            indRect.offsetMin = new Vector2(2f, 0f);
            indRect.offsetMax = new Vector2(-2f, 0f);

            Image indImg = indGO.AddComponent<Image>();
            indImg.color = new Color(0.3f, 0.3f, 0.3f);
            so.FindProperty(indicatorPropName).objectReferenceValue = indImg;

            // Label text (right portion)
            TextMeshProUGUI label = CreateTMPText(container.transform, "Label",
                $"{valveName}: SHUT", 9, new Color(0f, 1f, 0.53f),
                new Vector2(0.32f, 0f), new Vector2(1f, 1f));
            label.alignment = TextAlignmentOptions.MidlineLeft;
            so.FindProperty(textPropName).objectReferenceValue = label;
        }

        #endregion

        // ####################################################################
        //
        //  SCREEN 4 — CVCS (Chemical and Volume Control System)
        //
        //  Displays charging/letdown flows, VCT level, boron concentration,
        //  seal injection, and a 2D CVCS flow diagram.
        //
        // ####################################################################

        #region Screen 4 - CVCS

        private static void CreateCVCSScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 4 — CVCS...");

            foreach (Transform child in canvasParent)
            {
                if (child.name == "CVCSScreen")
                {
                    Debug.LogWarning("[MultiScreenBuilder] CVCSScreen already exists. Skipping.");
                    return;
                }
            }

            GameObject screenGO = new GameObject("CVCSScreen");
            screenGO.transform.SetParent(canvasParent, false);

            RectTransform rootRect = screenGO.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image rootBg = screenGO.AddComponent<Image>();
            rootBg.color = COLOR_BACKGROUND;

            screenGO.AddComponent<CanvasGroup>();
            CVCSScreen screen = screenGO.AddComponent<CVCSScreen>();

            BuildCVCSLeftPanel(screenGO.transform, screen);
            BuildCVCSCenterPanel(screenGO.transform, screen);
            BuildCVCSRightPanel(screenGO.transform, screen);
            BuildCVCSBottomPanel(screenGO.transform, screen);

            screenGO.SetActive(false);
            Debug.Log("[MultiScreenBuilder] Screen 4 — CVCS — build complete");
        }

        // ----------------------------------------------------------------
        // CVCS: Left Panel — 8 Flow/Control Gauges
        // ----------------------------------------------------------------

        private static void BuildCVCSLeftPanel(Transform parent, CVCSScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "LeftPanel",
                new Vector2(0f, 0.26f), new Vector2(0.15f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "LeftHeader", "FLOW & CONTROL", 11, COLOR_CYAN);

            SerializedObject so = new SerializedObject(screen);
            so.FindProperty("text_ChargingFlow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "ChargingFlow", "CHARGING FLOW");
            so.FindProperty("text_LetdownFlow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "LetdownFlow", "LETDOWN FLOW");
            so.FindProperty("text_SealInjectionFlow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SealInjFlow", "SEAL INJ FLOW");
            so.FindProperty("text_NetInventoryChange").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "NetInventory", "NET INVENTORY");
            so.FindProperty("text_VCTLevel").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "VCTLevel", "VCT LEVEL");
            so.FindProperty("text_VCTTemperature").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "VCTTemp", "VCT TEMP");
            so.FindProperty("text_VCTPressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "VCTPressure", "VCT PRESSURE");
            so.FindProperty("text_CCPDischargePressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "CCPDischP", "CCP DISCH P");
            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // CVCS: Center Panel — 2D Flow Diagram
        // ----------------------------------------------------------------

        private static void BuildCVCSCenterPanel(Transform parent, CVCSScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "CenterPanel",
                new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f), COLOR_CENTER_BG);

            CreateTMPLabel(panelGO.transform, "ScreenTitle", "CVCS", 16, COLOR_CYAN,
                new Vector2(0.02f, 0.93f), new Vector2(0.98f, 0.99f));
            CreateTMPLabel(panelGO.transform, "ScreenSubtitle",
                "CHEMICAL AND VOLUME CONTROL SYSTEM", 10, COLOR_TEXT_LABEL,
                new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.93f));

            SerializedObject so = new SerializedObject(screen);

            // --- RCS Connection (right side) ---
            Image rcsConn = CreateMimicBox(panelGO.transform, "RCSConnection",
                new Vector2(0.82f, 0.35f), new Vector2(0.96f, 0.65f),
                new Color(0.3f, 0.15f, 0.15f));
            so.FindProperty("diagram_RCSConnection").objectReferenceValue = rcsConn;
            CreateTMPLabel(rcsConn.transform, "RCSLabel", "RCS", 11, COLOR_TEXT_LABEL,
                new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.90f));
            TextMeshProUGUI rcsBoronText = CreateTMPText(rcsConn.transform, "RCSBoronText",
                "--- ppm", 10, COLOR_GREEN,
                new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.50f));
            rcsBoronText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("diagram_RCSBoronText").objectReferenceValue = rcsBoronText;

            // --- Charging Line (VCT to RCS, upper path) ---
            Image chargingLine = CreateMimicBox(panelGO.transform, "ChargingLine",
                new Vector2(0.42f, 0.58f), new Vector2(0.82f, 0.62f),
                new Color(0.25f, 0.25f, 0.30f, 0.4f));
            so.FindProperty("diagram_ChargingLine").objectReferenceValue = chargingLine;

            TextMeshProUGUI chargingFlowText = CreateTMPText(panelGO.transform, "ChargingFlowText",
                "--- gpm", 9, COLOR_GREEN,
                new Vector2(0.55f, 0.62f), new Vector2(0.78f, 0.70f));
            so.FindProperty("diagram_ChargingFlowText").objectReferenceValue = chargingFlowText;

            CreateTMPLabel(panelGO.transform, "ChargingLabel", "CHARGING \u2192", 8, COLOR_TEXT_LABEL,
                new Vector2(0.43f, 0.62f), new Vector2(0.56f, 0.70f));

            // --- Letdown Line (RCS to VCT, lower path) ---
            Image letdownLine = CreateMimicBox(panelGO.transform, "LetdownLine",
                new Vector2(0.42f, 0.38f), new Vector2(0.82f, 0.42f),
                new Color(0.25f, 0.25f, 0.30f, 0.4f));
            so.FindProperty("diagram_LetdownLine").objectReferenceValue = letdownLine;

            TextMeshProUGUI letdownFlowText = CreateTMPText(panelGO.transform, "LetdownFlowText",
                "--- gpm", 9, COLOR_GREEN,
                new Vector2(0.55f, 0.30f), new Vector2(0.78f, 0.38f));
            so.FindProperty("diagram_LetdownFlowText").objectReferenceValue = letdownFlowText;

            CreateTMPLabel(panelGO.transform, "LetdownLabel", "\u2190 LETDOWN", 8, COLOR_TEXT_LABEL,
                new Vector2(0.43f, 0.30f), new Vector2(0.56f, 0.38f));

            // --- Letdown HX (on letdown path) ---
            Image letdownHX = CreateMimicBox(panelGO.transform, "LetdownHX",
                new Vector2(0.62f, 0.33f), new Vector2(0.72f, 0.47f),
                new Color(0.2f, 0.2f, 0.28f));
            so.FindProperty("diagram_LetdownHX").objectReferenceValue = letdownHX;
            CreateTMPLabel(letdownHX.transform, "HXLabel", "LTDN\nHX", 7, COLOR_TEXT_LABEL,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));

            // --- Demineralizer (on letdown path, after HX) ---
            Image demin = CreateMimicBox(panelGO.transform, "Demineralizer",
                new Vector2(0.50f, 0.33f), new Vector2(0.60f, 0.47f),
                new Color(0.2f, 0.2f, 0.28f));
            so.FindProperty("diagram_Demineralizer").objectReferenceValue = demin;
            CreateTMPLabel(demin.transform, "DeminLabel", "DEMIN", 7, COLOR_TEXT_LABEL,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));

            // --- VCT Tank (left-center) ---
            Image vctTank = CreateMimicBox(panelGO.transform, "VCTTank",
                new Vector2(0.08f, 0.22f), new Vector2(0.38f, 0.78f),
                new Color(0.18f, 0.22f, 0.30f));
            so.FindProperty("diagram_VCTTank").objectReferenceValue = vctTank;

            // VCT interior
            CreateMimicBox(vctTank.transform, "VCTInterior",
                new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.97f),
                new Color(0.10f, 0.10f, 0.15f));

            // VCT level fill
            Image vctFill = CreateMimicFill(vctTank.transform, "VCTFill",
                new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f),
                new Color(0.15f, 0.4f, 0.7f, 0.7f));
            so.FindProperty("diagram_VCTLevelFill").objectReferenceValue = vctFill;

            CreateTMPLabel(vctTank.transform, "VCTLabel", "VOLUME CONTROL TANK", 9, COLOR_TEXT_LABEL,
                new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.97f));

            TextMeshProUGUI vctLevelText = CreateTMPText(vctTank.transform, "VCTLevelText",
                "--- %", 14, COLOR_GREEN,
                new Vector2(0.15f, 0.45f), new Vector2(0.85f, 0.60f));
            vctLevelText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("diagram_VCTLevelText").objectReferenceValue = vctLevelText;

            TextMeshProUGUI vctBoronText = CreateTMPText(vctTank.transform, "VCTBoronText",
                "--- ppm", 11, COLOR_GREEN,
                new Vector2(0.15f, 0.30f), new Vector2(0.85f, 0.44f));
            vctBoronText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("diagram_VCTBoronText").objectReferenceValue = vctBoronText;

            // VCT reference labels
            CreateTMPText(vctTank.transform, "VCTCapLabel", "4500 gal", 7, COLOR_TEXT_LABEL,
                new Vector2(0.60f, 0.04f), new Vector2(0.95f, 0.15f));

            // --- CCPs (between VCT and charging line) ---
            float[] ccpY = { 0.68f, 0.56f, 0.44f };
            string[] ccpNames = { "CCP_A", "CCP_B", "CCP_C" };
            string[] ccpLabels = { "CCP-A", "CCP-B", "CCP-C" };
            string[] ccpDiagramProps = { "diagram_CCP_A", "diagram_CCP_B", "diagram_CCP_C" };

            for (int i = 0; i < 3; i++)
            {
                Image ccpIcon = CreateMimicBox(panelGO.transform, ccpNames[i],
                    new Vector2(0.40f, ccpY[i]), new Vector2(0.48f, ccpY[i] + 0.08f),
                    new Color(0.35f, 0.35f, 0.35f));
                so.FindProperty(ccpDiagramProps[i]).objectReferenceValue = ccpIcon;
                CreateTMPLabel(ccpIcon.transform, "Label", ccpLabels[i], 7, COLOR_TEXT_LABEL,
                    new Vector2(0f, 0f), new Vector2(1f, 1f));
            }

            // --- Seal Injection Lines (from CCP discharge to 4 RCPs) ---
            SerializedProperty sealProp = so.FindProperty("diagram_SealInjectionLines");
            for (int i = 0; i < 4; i++)
            {
                float yPos = 0.12f + i * 0.04f;
                Image sealLine = CreateMimicBox(panelGO.transform, $"SealInj_{i + 1}",
                    new Vector2(0.50f, yPos), new Vector2(0.82f, yPos + 0.02f),
                    new Color(0.25f, 0.25f, 0.30f, 0.4f));
                sealProp.GetArrayElementAtIndex(i).objectReferenceValue = sealLine;
            }
            CreateTMPLabel(panelGO.transform, "SealLabel", "SEAL INJ \u2192 RCPs", 7, COLOR_TEXT_LABEL,
                new Vector2(0.55f, 0.06f), new Vector2(0.80f, 0.12f));

            // --- Boration Path (from BAST to VCT, left side) ---
            Image borationPath = CreateMimicBox(panelGO.transform, "BorationPath",
                new Vector2(0.02f, 0.10f), new Vector2(0.08f, 0.22f),
                new Color(0.25f, 0.25f, 0.30f, 0.4f));
            so.FindProperty("diagram_BorationPath").objectReferenceValue = borationPath;
            CreateTMPLabel(panelGO.transform, "BASTLabel", "BAST\n21000 ppm", 7, new Color(0.8f, 0.4f, 0.1f, 0.7f),
                new Vector2(0.01f, 0.02f), new Vector2(0.12f, 0.10f));

            // --- Dilution Path (from RMWST to VCT, left side) ---
            Image dilutionPath = CreateMimicBox(panelGO.transform, "DilutionPath",
                new Vector2(0.02f, 0.78f), new Vector2(0.08f, 0.86f),
                new Color(0.25f, 0.25f, 0.30f, 0.4f));
            so.FindProperty("diagram_DilutionPath").objectReferenceValue = dilutionPath;
            CreateTMPLabel(panelGO.transform, "RMWSTLabel", "RMWST\n0 ppm", 7, new Color(0.3f, 0.7f, 1f, 0.7f),
                new Vector2(0.01f, 0.86f), new Vector2(0.12f, 0.93f));

            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // CVCS: Right Panel — 8 Chemistry/Boron Gauges
        // ----------------------------------------------------------------

        private static void BuildCVCSRightPanel(Transform parent, CVCSScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "RightPanel",
                new Vector2(0.65f, 0.26f), new Vector2(1f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "RightHeader", "CHEMISTRY & BORON", 11, COLOR_CYAN);

            SerializedObject so = new SerializedObject(screen);
            so.FindProperty("text_RCSBoronConc").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "RCSBoron", "RCS BORON");
            so.FindProperty("text_VCTBoronConc").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "VCTBoron", "VCT BORON");
            so.FindProperty("text_BorationFlow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "BorationFlow", "BORATION FLOW");
            so.FindProperty("text_DilutionFlow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "DilutionFlow", "DILUTION FLOW");
            so.FindProperty("text_BoronWorth").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "BoronWorth", "BORON WORTH");
            so.FindProperty("text_LetdownTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "LetdownTemp", "LETDOWN TEMP");
            so.FindProperty("text_ChargingTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "ChargingTemp", "CHARGING TEMP");
            so.FindProperty("text_PurificationFlow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "PurifFlow", "PURIF FLOW");
            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // CVCS: Bottom Panel — CCP Controls, Boration/Dilution, Alarms
        // ----------------------------------------------------------------

        private static void BuildCVCSBottomPanel(Transform parent, CVCSScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "BottomPanel",
                new Vector2(0f, 0f), new Vector2(1f, 0.26f), COLOR_PANEL);

            SerializedObject so = new SerializedObject(screen);

            // --- CCP Control Section ---
            GameObject ccpSection = CreatePanel(panelGO.transform, "CCPControlSection",
                new Vector2(0.01f, 0.05f), new Vector2(0.40f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(ccpSection.transform, "CHARGING PUMPS");

            BuildCVCSCCPControl(ccpSection.transform, so, "A",
                new Vector2(0.02f, 0.10f), new Vector2(0.33f, 0.85f));
            BuildCVCSCCPControl(ccpSection.transform, so, "B",
                new Vector2(0.34f, 0.10f), new Vector2(0.66f, 0.85f));
            BuildCVCSCCPControl(ccpSection.transform, so, "C",
                new Vector2(0.67f, 0.10f), new Vector2(0.98f, 0.85f));

            // --- Boration / Dilution Section ---
            GameObject boronSection = CreatePanel(panelGO.transform, "BorationSection",
                new Vector2(0.41f, 0.05f), new Vector2(0.60f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(boronSection.transform, "BORON CONTROL");

            Button borateBtn = CreateTMPButton(boronSection.transform, "BorateBtn", "BORATE",
                new Vector2(0.05f, 0.50f), new Vector2(0.48f, 0.80f),
                new Color(0.35f, 0.20f, 0.05f));
            so.FindProperty("button_Borate").objectReferenceValue = borateBtn;

            Button diluteBtn = CreateTMPButton(boronSection.transform, "DiluteBtn", "DILUTE",
                new Vector2(0.52f, 0.50f), new Vector2(0.95f, 0.80f),
                new Color(0.10f, 0.25f, 0.40f));
            so.FindProperty("button_Dilute").objectReferenceValue = diluteBtn;

            TextMeshProUGUI boronModeText = CreateTMPText(boronSection.transform, "BoronModeText",
                "MANUAL", 11, COLOR_STOPPED,
                new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.45f));
            boronModeText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("text_BoronMode").objectReferenceValue = boronModeText;

            // --- Status Section ---
            GameObject statusSection = CreatePanel(panelGO.transform, "StatusSection",
                new Vector2(0.61f, 0.05f), new Vector2(0.78f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(statusSection.transform, "STATUS");

            TextMeshProUGUI modeText = CreateTMPText(statusSection.transform, "ModeText",
                "MODE 5", 13, COLOR_AMBER,
                new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.82f));
            modeText.alignment = TextAlignmentOptions.Center;
            modeText.fontStyle = FontStyles.Bold;
            so.FindProperty("text_ReactorMode").objectReferenceValue = modeText;

            TextMeshProUGUI simTimeText = CreateTMPText(statusSection.transform, "SimTimeText",
                "00:00:00", 14, COLOR_GREEN,
                new Vector2(0.05f, 0.38f), new Vector2(0.60f, 0.58f));
            so.FindProperty("text_SimTime").objectReferenceValue = simTimeText;

            TextMeshProUGUI timeCompText = CreateTMPText(statusSection.transform, "TimeCompText",
                "1x", 14, COLOR_GREEN,
                new Vector2(0.62f, 0.38f), new Vector2(0.95f, 0.58f));
            so.FindProperty("text_TimeCompression").objectReferenceValue = timeCompText;

            // --- Alarm Section ---
            GameObject alarmSection = CreatePanel(panelGO.transform, "AlarmSection",
                new Vector2(0.79f, 0.05f), new Vector2(0.99f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(alarmSection.transform, "ALARMS");

            GameObject alarmContainer = new GameObject("AlarmContainer");
            alarmContainer.transform.SetParent(alarmSection.transform, false);
            RectTransform alarmContRect = alarmContainer.AddComponent<RectTransform>();
            alarmContRect.anchorMin = new Vector2(0.02f, 0.02f);
            alarmContRect.anchorMax = new Vector2(0.98f, 0.85f);
            alarmContRect.offsetMin = Vector2.zero;
            alarmContRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup alarmVLG = alarmContainer.AddComponent<VerticalLayoutGroup>();
            alarmVLG.padding = new RectOffset(2, 2, 2, 2);
            alarmVLG.spacing = 1f;
            alarmVLG.childAlignment = TextAnchor.UpperLeft;
            alarmVLG.childControlWidth = true;
            alarmVLG.childControlHeight = false;
            alarmVLG.childForceExpandWidth = true;
            alarmVLG.childForceExpandHeight = false;

            so.FindProperty("alarmContainer").objectReferenceValue = alarmContainer.transform;
            so.ApplyModifiedProperties();
        }

        private static void BuildCVCSCCPControl(Transform parent, SerializedObject so,
            string pumpLetter, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject container = new GameObject($"CCP_{pumpLetter}");
            container.transform.SetParent(parent, false);

            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(2f, 0f);
            rect.offsetMax = new Vector2(-2f, 0f);

            Image bg = container.AddComponent<Image>();
            bg.color = new Color(0.09f, 0.09f, 0.12f);

            // Pump label
            CreateTMPText(container.transform, "Label", $"CCP-{pumpLetter}", 11, COLOR_CYAN,
                new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.98f)).fontStyle = FontStyles.Bold;

            // Indicator
            GameObject indGO = new GameObject("Indicator");
            indGO.transform.SetParent(container.transform, false);
            RectTransform indRect = indGO.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0.10f, 0.62f);
            indRect.anchorMax = new Vector2(0.30f, 0.78f);
            indRect.offsetMin = Vector2.zero;
            indRect.offsetMax = Vector2.zero;
            Image indImg = indGO.AddComponent<Image>();
            indImg.color = new Color(0.4f, 0.4f, 0.4f);
            so.FindProperty($"indicator_CCP_{pumpLetter}").objectReferenceValue = indImg;

            // Status text
            TextMeshProUGUI statusText = CreateTMPText(container.transform, "StatusText",
                $"CCP-{pumpLetter}: STBY", 9, new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.32f, 0.62f), new Vector2(0.95f, 0.78f));
            so.FindProperty($"text_CCP_{pumpLetter}_Status").objectReferenceValue = statusText;

            // Start / Stop buttons
            Button startBtn = CreateTMPButton(container.transform, "StartBtn", "START",
                new Vector2(0.05f, 0.08f), new Vector2(0.48f, 0.38f),
                new Color(0.15f, 0.30f, 0.15f));
            so.FindProperty($"button_CCP_{pumpLetter}_Start").objectReferenceValue = startBtn;

            Button stopBtn = CreateTMPButton(container.transform, "StopBtn", "STOP",
                new Vector2(0.52f, 0.08f), new Vector2(0.95f, 0.38f),
                new Color(0.30f, 0.15f, 0.15f));
            so.FindProperty($"button_CCP_{pumpLetter}_Stop").objectReferenceValue = stopBtn;
        }

        #endregion

        // ####################################################################
        //
        //  SCREEN 5 — STEAM GENERATORS
        //
        //  Quad-SG 2x2 layout with U-tube schematics, level indicators,
        //  primary inlet/outlet temps, secondary level and steam pressure.
        //
        // ####################################################################

        #region Screen 5 - Steam Generators

        private static void CreateSteamGeneratorScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 5 — Steam Generators...");

            foreach (Transform child in canvasParent)
            {
                if (child.name == "SteamGeneratorScreen")
                {
                    Debug.LogWarning("[MultiScreenBuilder] SteamGeneratorScreen already exists. Skipping.");
                    return;
                }
            }

            GameObject screenGO = new GameObject("SteamGeneratorScreen");
            screenGO.transform.SetParent(canvasParent, false);

            RectTransform rootRect = screenGO.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image rootBg = screenGO.AddComponent<Image>();
            rootBg.color = COLOR_BACKGROUND;

            screenGO.AddComponent<CanvasGroup>();
            SteamGeneratorScreen screen = screenGO.AddComponent<SteamGeneratorScreen>();

            BuildSGLeftPanel(screenGO.transform, screen);
            BuildSGCenterPanel(screenGO.transform, screen);
            BuildSGRightPanel(screenGO.transform, screen);
            BuildSGBottomPanel(screenGO.transform, screen);

            screenGO.SetActive(false);
            Debug.Log("[MultiScreenBuilder] Screen 5 — Steam Generators — build complete");
        }

        // ----------------------------------------------------------------
        // SG: Left Panel — 8 Primary Side Temperature Gauges
        // ----------------------------------------------------------------

        private static void BuildSGLeftPanel(Transform parent, SteamGeneratorScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "LeftPanel",
                new Vector2(0f, 0.26f), new Vector2(0.15f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "LeftHeader", "PRIMARY SIDE", 11, COLOR_CYAN);

            SerializedObject so = new SerializedObject(screen);
            so.FindProperty("text_SGA_InletTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGAInlet", "SG-A INLET");
            so.FindProperty("text_SGB_InletTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGBInlet", "SG-B INLET");
            so.FindProperty("text_SGC_InletTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGCInlet", "SG-C INLET");
            so.FindProperty("text_SGD_InletTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGDInlet", "SG-D INLET");
            so.FindProperty("text_SGA_OutletTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGAOutlet", "SG-A OUTLET");
            so.FindProperty("text_SGB_OutletTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGBOutlet", "SG-B OUTLET");
            so.FindProperty("text_SGC_OutletTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGCOutlet", "SG-C OUTLET");
            so.FindProperty("text_SGD_OutletTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGDOutlet", "SG-D OUTLET");
            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // SG: Center Panel — Quad-SG 2x2 Layout
        // ----------------------------------------------------------------

        private static void BuildSGCenterPanel(Transform parent, SteamGeneratorScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "CenterPanel",
                new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f), COLOR_CENTER_BG);

            CreateTMPLabel(panelGO.transform, "ScreenTitle", "STEAM GENERATORS", 16, COLOR_CYAN,
                new Vector2(0.02f, 0.93f), new Vector2(0.98f, 0.99f));
            CreateTMPLabel(panelGO.transform, "ScreenSubtitle",
                "WESTINGHOUSE 4-LOOP PWR — MODEL F — 4 UNITS", 10, COLOR_TEXT_LABEL,
                new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.93f));

            SerializedObject so = new SerializedObject(screen);

            // 2x2 grid positions: [SG-A top-left, SG-B top-right, SG-C bottom-left, SG-D bottom-right]
            Vector2[] cellMins = {
                new Vector2(0.02f, 0.46f), new Vector2(0.51f, 0.46f),
                new Vector2(0.02f, 0.02f), new Vector2(0.51f, 0.02f)
            };
            Vector2[] cellMaxs = {
                new Vector2(0.49f, 0.87f), new Vector2(0.98f, 0.87f),
                new Vector2(0.49f, 0.43f), new Vector2(0.98f, 0.43f)
            };
            string[] sgNames = { "SG-A", "SG-B", "SG-C", "SG-D" };

            // Array properties
            SerializedProperty shellsProp = so.FindProperty("sg_Shells");
            SerializedProperty tubesProp = so.FindProperty("sg_TubeBundles");
            SerializedProperty fillsProp = so.FindProperty("sg_LevelFills");
            SerializedProperty domesProp = so.FindProperty("sg_SteamDomes");
            SerializedProperty hotLegProp = so.FindProperty("sg_HotLegInlets");
            SerializedProperty coldLegProp = so.FindProperty("sg_ColdLegOutlets");
            SerializedProperty lvlTextProp = so.FindProperty("sg_LevelTexts");
            SerializedProperty prsTextProp = so.FindProperty("sg_PressureTexts");
            SerializedProperty tmpTextProp = so.FindProperty("sg_TempTexts");

            for (int i = 0; i < 4; i++)
            {
                BuildSingleSGCell(panelGO.transform, so, sgNames[i], i,
                    cellMins[i], cellMaxs[i],
                    shellsProp, tubesProp, fillsProp, domesProp,
                    hotLegProp, coldLegProp, lvlTextProp, prsTextProp, tmpTextProp);
            }

            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// Build a single SG cell in the 2x2 grid with U-tube schematic.
        /// </summary>
        private static void BuildSingleSGCell(Transform parent, SerializedObject so,
            string sgName, int index, Vector2 cellMin, Vector2 cellMax,
            SerializedProperty shellsProp, SerializedProperty tubesProp,
            SerializedProperty fillsProp, SerializedProperty domesProp,
            SerializedProperty hotLegProp, SerializedProperty coldLegProp,
            SerializedProperty lvlTextProp, SerializedProperty prsTextProp,
            SerializedProperty tmpTextProp)
        {
            // Cell container
            GameObject cellGO = new GameObject(sgName);
            cellGO.transform.SetParent(parent, false);
            RectTransform cellRect = cellGO.AddComponent<RectTransform>();
            cellRect.anchorMin = cellMin;
            cellRect.anchorMax = cellMax;
            cellRect.offsetMin = Vector2.zero;
            cellRect.offsetMax = Vector2.zero;

            // SG name label
            CreateTMPLabel(cellGO.transform, "NameLabel", sgName, 12, COLOR_CYAN,
                new Vector2(0.02f, 0.88f), new Vector2(0.50f, 0.99f));

            // Shell (vessel outline)
            Image shell = CreateMimicBox(cellGO.transform, "Shell",
                new Vector2(0.15f, 0.05f), new Vector2(0.65f, 0.85f),
                new Color(0.20f, 0.22f, 0.28f));
            shellsProp.GetArrayElementAtIndex(index).objectReferenceValue = shell;

            // Interior background
            CreateMimicBox(shell.transform, "Interior",
                new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.97f),
                new Color(0.10f, 0.10f, 0.15f));

            // Tube bundle (mid-section of vessel)
            Image tubes = CreateMimicBox(shell.transform, "TubeBundle",
                new Vector2(0.10f, 0.15f), new Vector2(0.90f, 0.60f),
                new Color(0.30f, 0.25f, 0.20f, 0.6f));
            tubesProp.GetArrayElementAtIndex(index).objectReferenceValue = tubes;

            CreateTMPLabel(tubes.transform, "TubeLabel", "U-TUBES", 7,
                new Color(0.5f, 0.5f, 0.5f, 0.5f),
                new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.7f));

            // Water level fill (bottom fill)
            Image levelFill = CreateMimicFill(shell.transform, "LevelFill",
                new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f),
                new Color(0.15f, 0.4f, 0.7f, 0.7f));
            fillsProp.GetArrayElementAtIndex(index).objectReferenceValue = levelFill;

            // Steam dome (top of vessel)
            Image dome = CreateMimicBox(shell.transform, "SteamDome",
                new Vector2(0.10f, 0.65f), new Vector2(0.90f, 0.95f),
                new Color(0.6f, 0.6f, 0.7f, 0.3f));
            domesProp.GetArrayElementAtIndex(index).objectReferenceValue = dome;

            CreateTMPLabel(dome.transform, "SteamLabel", "STEAM", 7,
                new Color(0.6f, 0.6f, 0.7f, 0.5f),
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));

            // Hot leg inlet (left of vessel, lower)
            Image hotLeg = CreateMimicBox(cellGO.transform, "HotLegInlet",
                new Vector2(0.02f, 0.20f), new Vector2(0.15f, 0.35f),
                new Color(0.25f, 0.25f, 0.30f, 0.4f));
            hotLegProp.GetArrayElementAtIndex(index).objectReferenceValue = hotLeg;
            CreateTMPLabel(hotLeg.transform, "HLLabel", "HL", 7, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // Cold leg outlet (right of vessel, lower)
            Image coldLeg = CreateMimicBox(cellGO.transform, "ColdLegOutlet",
                new Vector2(0.65f, 0.20f), new Vector2(0.78f, 0.35f),
                new Color(0.25f, 0.25f, 0.30f, 0.4f));
            coldLegProp.GetArrayElementAtIndex(index).objectReferenceValue = coldLeg;
            CreateTMPLabel(coldLeg.transform, "CLLabel", "CL", 7, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // Feedwater inlet label (bottom)
            CreateTMPLabel(cellGO.transform, "FWLabel", "\u2191 FW", 7, COLOR_TEXT_LABEL,
                new Vector2(0.30f, 0.00f), new Vector2(0.50f, 0.06f));

            // Steam outlet label (top)
            CreateTMPLabel(cellGO.transform, "SteamOutLabel", "STM \u2191", 7, COLOR_TEXT_LABEL,
                new Vector2(0.30f, 0.85f), new Vector2(0.50f, 0.92f));

            // Overlay text — level, pressure, secondary temp (right side of cell)
            TextMeshProUGUI lvlText = CreateTMPText(cellGO.transform, "LevelText",
                "---% ", 10, new Color(0f, 1f, 0.53f),
                new Vector2(0.68f, 0.60f), new Vector2(0.98f, 0.72f));
            lvlText.alignment = TextAlignmentOptions.MidlineLeft;
            lvlTextProp.GetArrayElementAtIndex(index).objectReferenceValue = lvlText;

            CreateTMPLabel(cellGO.transform, "LvlLabel", "LVL", 7, COLOR_TEXT_LABEL,
                new Vector2(0.68f, 0.72f), new Vector2(0.98f, 0.80f));

            TextMeshProUGUI prsText = CreateTMPText(cellGO.transform, "PressText",
                "--- psig", 10, new Color(0f, 1f, 0.53f),
                new Vector2(0.68f, 0.42f), new Vector2(0.98f, 0.54f));
            prsText.alignment = TextAlignmentOptions.MidlineLeft;
            prsTextProp.GetArrayElementAtIndex(index).objectReferenceValue = prsText;

            CreateTMPLabel(cellGO.transform, "PrsLabel", "STM P", 7, COLOR_TEXT_LABEL,
                new Vector2(0.68f, 0.54f), new Vector2(0.98f, 0.62f));

            TextMeshProUGUI tmpText = CreateTMPText(cellGO.transform, "TempText",
                "--- F", 10, new Color(0f, 1f, 0.53f),
                new Vector2(0.68f, 0.24f), new Vector2(0.98f, 0.36f));
            tmpText.alignment = TextAlignmentOptions.MidlineLeft;
            tmpTextProp.GetArrayElementAtIndex(index).objectReferenceValue = tmpText;

            CreateTMPLabel(cellGO.transform, "TmpLabel", "SEC T", 7, COLOR_TEXT_LABEL,
                new Vector2(0.68f, 0.36f), new Vector2(0.98f, 0.44f));
        }

        // ----------------------------------------------------------------
        // SG: Right Panel — 8 Secondary Side Gauges
        // ----------------------------------------------------------------

        private static void BuildSGRightPanel(Transform parent, SteamGeneratorScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "RightPanel",
                new Vector2(0.65f, 0.26f), new Vector2(1f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "RightHeader", "SECONDARY SIDE", 11, COLOR_CYAN);

            SerializedObject so = new SerializedObject(screen);
            so.FindProperty("text_SGA_Level").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGALevel", "SG-A LEVEL");
            so.FindProperty("text_SGB_Level").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGBLevel", "SG-B LEVEL");
            so.FindProperty("text_SGC_Level").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGCLevel", "SG-C LEVEL");
            so.FindProperty("text_SGD_Level").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGDLevel", "SG-D LEVEL");
            so.FindProperty("text_SGA_SteamPressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGAPress", "SG-A STM P");
            so.FindProperty("text_SGB_SteamPressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGBPress", "SG-B STM P");
            so.FindProperty("text_SGC_SteamPressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGCPress", "SG-C STM P");
            so.FindProperty("text_SGD_SteamPressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SGDPress", "SG-D STM P");
            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // SG: Bottom Panel — Heat Removal, Flow, Status, Alarms
        // ----------------------------------------------------------------

        private static void BuildSGBottomPanel(Transform parent, SteamGeneratorScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "BottomPanel",
                new Vector2(0f, 0f), new Vector2(1f, 0.26f), COLOR_PANEL);

            SerializedObject so = new SerializedObject(screen);

            // --- Heat Transfer Section ---
            GameObject htSection = CreatePanel(panelGO.transform, "HeatTransferSection",
                new Vector2(0.01f, 0.05f), new Vector2(0.28f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(htSection.transform, "HEAT REMOVAL");

            TextMeshProUGUI totalHR = CreateTMPText(htSection.transform, "TotalHeatRemoval",
                "--- MW", 16, COLOR_GREEN,
                new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.78f));
            totalHR.alignment = TextAlignmentOptions.Center;
            totalHR.fontStyle = FontStyles.Bold;
            so.FindProperty("text_TotalHeatRemoval").objectReferenceValue = totalHR;

            TextMeshProUGUI steamingText = CreateTMPText(htSection.transform, "SteamingStatus",
                "SUBCOOLED", 11, COLOR_WARNING,
                new Vector2(0.05f, 0.22f), new Vector2(0.50f, 0.42f));
            steamingText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("text_SteamingStatus").objectReferenceValue = steamingText;

            TextMeshProUGUI sgSecTemp = CreateTMPText(htSection.transform, "SGSecTemp",
                "--- F", 11, COLOR_GREEN,
                new Vector2(0.50f, 0.22f), new Vector2(0.95f, 0.42f));
            sgSecTemp.alignment = TextAlignmentOptions.Center;
            so.FindProperty("text_SGSecondaryTemp").objectReferenceValue = sgSecTemp;

            TextMeshProUGUI circText = CreateTMPText(htSection.transform, "CircFraction",
                "---", 11, COLOR_GREEN,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.22f));
            circText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("text_CirculationFraction").objectReferenceValue = circText;

            // --- Flow Section ---
            GameObject flowSection = CreatePanel(panelGO.transform, "FlowSection",
                new Vector2(0.29f, 0.05f), new Vector2(0.50f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(flowSection.transform, "FLOW");

            TextMeshProUGUI fwFlow = CreateTMPText(flowSection.transform, "FWFlow",
                "FW: ---", 11, COLOR_PLACEHOLDER_MIMIC,
                new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.78f));
            fwFlow.alignment = TextAlignmentOptions.Center;
            so.FindProperty("text_FeedwaterFlow").objectReferenceValue = fwFlow;

            TextMeshProUGUI stmFlow = CreateTMPText(flowSection.transform, "SteamFlow",
                "STM: ---", 11, COLOR_PLACEHOLDER_MIMIC,
                new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.48f));
            stmFlow.alignment = TextAlignmentOptions.Center;
            so.FindProperty("text_SteamFlow").objectReferenceValue = stmFlow;

            CreateTMPText(flowSection.transform, "FlowNote", "(NOT MODELED)", 8, COLOR_PLACEHOLDER_MIMIC,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.22f));

            // --- Status Section ---
            GameObject statusSection = CreatePanel(panelGO.transform, "StatusSection",
                new Vector2(0.51f, 0.05f), new Vector2(0.78f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(statusSection.transform, "STATUS");

            TextMeshProUGUI modeText = CreateTMPText(statusSection.transform, "ModeText",
                "MODE 5", 13, COLOR_AMBER,
                new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.82f));
            modeText.alignment = TextAlignmentOptions.Center;
            modeText.fontStyle = FontStyles.Bold;
            so.FindProperty("text_ReactorMode").objectReferenceValue = modeText;

            TextMeshProUGUI simTimeText = CreateTMPText(statusSection.transform, "SimTimeText",
                "00:00:00", 14, COLOR_GREEN,
                new Vector2(0.05f, 0.38f), new Vector2(0.60f, 0.58f));
            so.FindProperty("text_SimTime").objectReferenceValue = simTimeText;

            TextMeshProUGUI timeCompText = CreateTMPText(statusSection.transform, "TimeCompText",
                "1x", 14, COLOR_GREEN,
                new Vector2(0.62f, 0.38f), new Vector2(0.95f, 0.58f));
            so.FindProperty("text_TimeCompression").objectReferenceValue = timeCompText;

            // Note about lumped model
            CreateTMPText(statusSection.transform, "LumpedNote",
                "LUMPED MODEL\nAll 4 SGs identical", 8, COLOR_PLACEHOLDER_MIMIC,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.35f));

            // --- Alarm Section ---
            GameObject alarmSection = CreatePanel(panelGO.transform, "AlarmSection",
                new Vector2(0.79f, 0.05f), new Vector2(0.99f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(alarmSection.transform, "ALARMS");

            GameObject alarmContainer = new GameObject("AlarmContainer");
            alarmContainer.transform.SetParent(alarmSection.transform, false);
            RectTransform acRect = alarmContainer.AddComponent<RectTransform>();
            acRect.anchorMin = new Vector2(0.02f, 0.02f);
            acRect.anchorMax = new Vector2(0.98f, 0.85f);
            acRect.offsetMin = Vector2.zero;
            acRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup aVLG = alarmContainer.AddComponent<VerticalLayoutGroup>();
            aVLG.padding = new RectOffset(2, 2, 2, 2);
            aVLG.spacing = 1f;
            aVLG.childAlignment = TextAnchor.UpperLeft;
            aVLG.childControlWidth = true;
            aVLG.childControlHeight = false;
            aVLG.childForceExpandWidth = true;
            aVLG.childForceExpandHeight = false;

            so.FindProperty("alarmContainer").objectReferenceValue = alarmContainer.transform;
            so.ApplyModifiedProperties();
        }

        #endregion

        // ####################################################################
        //
        //  SCREEN 6 — TURBINE-GENERATOR
        //
        //  Shaft train diagram, HP/LP turbine gauges, generator output.
        //  Almost entirely PLACEHOLDER — no turbine model exists.
        //
        // ####################################################################

        #region Screen 6 - Turbine-Generator

        private static void CreateTurbineGeneratorScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 6 — Turbine-Generator...");

            foreach (Transform child in canvasParent)
            {
                if (child.name == "TurbineGeneratorScreen")
                {
                    Debug.LogWarning("[MultiScreenBuilder] TurbineGeneratorScreen already exists. Skipping.");
                    return;
                }
            }

            GameObject screenGO = new GameObject("TurbineGeneratorScreen");
            screenGO.transform.SetParent(canvasParent, false);

            RectTransform rootRect = screenGO.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image rootBg = screenGO.AddComponent<Image>();
            rootBg.color = COLOR_BACKGROUND;

            screenGO.AddComponent<CanvasGroup>();
            TurbineGeneratorScreen screen = screenGO.AddComponent<TurbineGeneratorScreen>();

            BuildTGLeftPanel(screenGO.transform, screen);
            BuildTGCenterPanel(screenGO.transform, screen);
            BuildTGRightPanel(screenGO.transform, screen);
            BuildTGBottomPanel(screenGO.transform, screen);

            screenGO.SetActive(false);
            Debug.Log("[MultiScreenBuilder] Screen 6 — Turbine-Generator — build complete");
        }

        // ----------------------------------------------------------------
        // TG: Left Panel — 8 Turbine Performance Gauges
        // ----------------------------------------------------------------

        private static void BuildTGLeftPanel(Transform parent, TurbineGeneratorScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "LeftPanel",
                new Vector2(0f, 0.26f), new Vector2(0.15f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "LeftHeader", "TURBINE", 11, COLOR_CYAN);

            SerializedObject so = new SerializedObject(screen);
            so.FindProperty("text_HPInletPressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "HPInletP", "HP INLET P");
            so.FindProperty("text_HPInletTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "HPInletT", "HP INLET T");
            so.FindProperty("text_HPExhaustPressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "HPExhP", "HP EXHAUST P");
            so.FindProperty("text_LPExhaustPressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "LPExhP", "LP EXHAUST P");
            so.FindProperty("text_ThrottleSteamFlow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "ThrottleFlow", "THROTTLE FLOW");
            so.FindProperty("text_FirstStagePressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "1stStageP", "1ST STAGE P");
            so.FindProperty("text_MSRPressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "MSRP", "MSR PRESSURE");
            so.FindProperty("text_ReheatSteamTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "ReheatT", "REHEAT TEMP");
            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // TG: Center Panel — Shaft Train Diagram
        // ----------------------------------------------------------------

        private static void BuildTGCenterPanel(Transform parent, TurbineGeneratorScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "CenterPanel",
                new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f), COLOR_CENTER_BG);

            CreateTMPLabel(panelGO.transform, "ScreenTitle", "TURBINE-GENERATOR", 16, COLOR_CYAN,
                new Vector2(0.02f, 0.93f), new Vector2(0.98f, 0.99f));
            CreateTMPLabel(panelGO.transform, "ScreenSubtitle",
                "1150 MWe GROSS — 3600 RPM — 60 Hz", 10, COLOR_TEXT_LABEL,
                new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.93f));

            SerializedObject so = new SerializedObject(screen);

            // --- Shaft line (horizontal through all components) ---
            Image shaftLine = CreateMimicBox(panelGO.transform, "ShaftLine",
                new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.52f),
                new Color(0.4f, 0.4f, 0.4f, 0.5f));
            so.FindProperty("diagram_ShaftLine").objectReferenceValue = shaftLine;

            // --- Steam Admission (far left) ---
            Image steamAdm = CreateMimicBox(panelGO.transform, "SteamAdmission",
                new Vector2(0.02f, 0.55f), new Vector2(0.10f, 0.75f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_SteamAdmission").objectReferenceValue = steamAdm;
            CreateTMPLabel(steamAdm.transform, "Label", "STM\nINLET", 7, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            TextMeshProUGUI stmPText = CreateTMPText(panelGO.transform, "SteamPressureText",
                "--- psig", 9, new Color(0f, 1f, 0.53f),
                new Vector2(0.02f, 0.76f), new Vector2(0.14f, 0.84f));
            stmPText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("diagram_SteamPressureText").objectReferenceValue = stmPText;

            // --- HP Turbine ---
            Image hpTurb = CreateMimicBox(panelGO.transform, "HPTurbine",
                new Vector2(0.12f, 0.35f), new Vector2(0.27f, 0.65f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_HPTurbine").objectReferenceValue = hpTurb;
            CreateTMPLabel(hpTurb.transform, "Label", "HP\nTURBINE", 9, COLOR_TEXT_LABEL,
                new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f));

            // --- MSR ---
            Image msr = CreateMimicBox(panelGO.transform, "MSR",
                new Vector2(0.29f, 0.38f), new Vector2(0.40f, 0.62f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_MSR").objectReferenceValue = msr;
            CreateTMPLabel(msr.transform, "Label", "MSR", 8, COLOR_TEXT_LABEL,
                new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f));

            // --- LP Turbine A ---
            Image lpA = CreateMimicBox(panelGO.transform, "LPTurbineA",
                new Vector2(0.42f, 0.32f), new Vector2(0.57f, 0.68f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_LPTurbineA").objectReferenceValue = lpA;
            CreateTMPLabel(lpA.transform, "Label", "LP-A\nTURBINE", 9, COLOR_TEXT_LABEL,
                new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f));

            // --- LP Turbine B ---
            Image lpB = CreateMimicBox(panelGO.transform, "LPTurbineB",
                new Vector2(0.59f, 0.32f), new Vector2(0.74f, 0.68f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_LPTurbineB").objectReferenceValue = lpB;
            CreateTMPLabel(lpB.transform, "Label", "LP-B\nTURBINE", 9, COLOR_TEXT_LABEL,
                new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f));

            // --- Generator ---
            Image gen = CreateMimicBox(panelGO.transform, "Generator",
                new Vector2(0.76f, 0.35f), new Vector2(0.95f, 0.65f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_Generator").objectReferenceValue = gen;
            CreateTMPLabel(gen.transform, "Label", "GEN\n22 kV", 9, COLOR_TEXT_LABEL,
                new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f));

            // --- Condenser (below LP turbines) ---
            Image condenser = CreateMimicBox(panelGO.transform, "Condenser",
                new Vector2(0.40f, 0.05f), new Vector2(0.76f, 0.28f),
                new Color(0.12f, 0.15f, 0.20f));
            so.FindProperty("diagram_Condenser").objectReferenceValue = condenser;
            CreateTMPLabel(condenser.transform, "Label", "CONDENSER", 9, COLOR_TEXT_LABEL,
                new Vector2(0.10f, 0.40f), new Vector2(0.90f, 0.80f));

            TextMeshProUGUI condenserText = CreateTMPText(condenser.transform, "VacuumText",
                "--- in Hg", 9, new Color(0f, 1f, 0.53f),
                new Vector2(0.10f, 0.05f), new Vector2(0.90f, 0.35f));
            condenserText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("diagram_CondenserText").objectReferenceValue = condenserText;

            // --- RPM text (above shaft) ---
            TextMeshProUGUI rpmText = CreateTMPText(panelGO.transform, "RPMText",
                "--- RPM", 14, new Color(0f, 1f, 0.53f),
                new Vector2(0.35f, 0.70f), new Vector2(0.65f, 0.82f));
            rpmText.alignment = TextAlignmentOptions.Center;
            rpmText.fontStyle = FontStyles.Bold;
            so.FindProperty("diagram_RPMText").objectReferenceValue = rpmText;

            // --- Power text (near generator) ---
            TextMeshProUGUI powerText = CreateTMPText(panelGO.transform, "PowerText",
                "--- MWe", 14, new Color(0f, 1f, 0.53f),
                new Vector2(0.76f, 0.68f), new Vector2(0.98f, 0.80f));
            powerText.alignment = TextAlignmentOptions.Center;
            powerText.fontStyle = FontStyles.Bold;
            so.FindProperty("diagram_PowerText").objectReferenceValue = powerText;

            // "NOT MODELED" overlay
            CreateTMPLabel(panelGO.transform, "NotModeled",
                "TURBINE MODEL NOT IMPLEMENTED", 10, new Color(1f, 0.5f, 0.2f, 0.6f),
                new Vector2(0.15f, 0.82f), new Vector2(0.85f, 0.88f));

            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // TG: Right Panel — 8 Generator Output Gauges
        // ----------------------------------------------------------------

        private static void BuildTGRightPanel(Transform parent, TurbineGeneratorScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "RightPanel",
                new Vector2(0.65f, 0.26f), new Vector2(1f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "RightHeader", "GENERATOR", 11, COLOR_CYAN);

            SerializedObject so = new SerializedObject(screen);
            so.FindProperty("text_GeneratorOutput").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "GenOutput", "GEN OUTPUT");
            so.FindProperty("text_GrossOutput").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "GrossOutput", "GROSS OUTPUT");
            so.FindProperty("text_AuxLoad").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "AuxLoad", "AUX LOAD");
            so.FindProperty("text_NetOutput").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "NetOutput", "NET OUTPUT");
            so.FindProperty("text_GeneratorVoltage").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "GenVoltage", "GEN VOLTAGE");
            so.FindProperty("text_GeneratorCurrent").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "GenCurrent", "GEN CURRENT");
            so.FindProperty("text_PowerFactor").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "PowerFactor", "POWER FACTOR");
            so.FindProperty("text_GridFrequency").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "GridFreq", "GRID FREQ");
            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // TG: Bottom Panel — Controls, Breaker Status, Alarms
        // ----------------------------------------------------------------

        private static void BuildTGBottomPanel(Transform parent, TurbineGeneratorScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "BottomPanel",
                new Vector2(0f, 0f), new Vector2(1f, 0.26f), COLOR_PANEL);

            SerializedObject so = new SerializedObject(screen);

            // --- Turbine Controls ---
            GameObject turbSection = CreatePanel(panelGO.transform, "TurbineSection",
                new Vector2(0.01f, 0.05f), new Vector2(0.28f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(turbSection.transform, "TURBINE");

            Button tripBtn = CreateTMPButton(turbSection.transform, "TurbTripBtn", "TURBINE TRIP",
                new Vector2(0.05f, 0.50f), new Vector2(0.95f, 0.80f),
                new Color(0.40f, 0.15f, 0.10f));
            so.FindProperty("button_TurbineTrip").objectReferenceValue = tripBtn;

            // Trip indicator
            GameObject tripIndGO = new GameObject("TripIndicator");
            tripIndGO.transform.SetParent(turbSection.transform, false);
            RectTransform tripIndRect = tripIndGO.AddComponent<RectTransform>();
            tripIndRect.anchorMin = new Vector2(0.05f, 0.28f);
            tripIndRect.anchorMax = new Vector2(0.18f, 0.44f);
            tripIndRect.offsetMin = Vector2.zero;
            tripIndRect.offsetMax = Vector2.zero;
            Image tripIndImg = tripIndGO.AddComponent<Image>();
            tripIndImg.color = new Color(0.9f, 0.2f, 0.2f);
            so.FindProperty("indicator_TurbineTrip").objectReferenceValue = tripIndImg;

            TextMeshProUGUI tripStatusText = CreateTMPText(turbSection.transform, "TripStatus",
                "TRIPPED", 10, new Color(0.9f, 0.2f, 0.2f),
                new Vector2(0.20f, 0.28f), new Vector2(0.95f, 0.44f));
            tripStatusText.alignment = TextAlignmentOptions.MidlineLeft;
            so.FindProperty("text_TurbineTripStatus").objectReferenceValue = tripStatusText;

            // --- Generator Controls ---
            GameObject genSection = CreatePanel(panelGO.transform, "GeneratorSection",
                new Vector2(0.29f, 0.05f), new Vector2(0.58f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(genSection.transform, "GENERATOR BREAKER");

            Button closeBtn = CreateTMPButton(genSection.transform, "BkrCloseBtn", "CLOSE",
                new Vector2(0.05f, 0.50f), new Vector2(0.48f, 0.80f),
                new Color(0.15f, 0.30f, 0.15f));
            so.FindProperty("button_GenBreakerClose").objectReferenceValue = closeBtn;

            Button openBtn = CreateTMPButton(genSection.transform, "BkrOpenBtn", "OPEN",
                new Vector2(0.52f, 0.50f), new Vector2(0.95f, 0.80f),
                new Color(0.30f, 0.15f, 0.15f));
            so.FindProperty("button_GenBreakerOpen").objectReferenceValue = openBtn;

            // Breaker indicator
            GameObject bkrIndGO = new GameObject("BreakerIndicator");
            bkrIndGO.transform.SetParent(genSection.transform, false);
            RectTransform bkrIndRect = bkrIndGO.AddComponent<RectTransform>();
            bkrIndRect.anchorMin = new Vector2(0.05f, 0.28f);
            bkrIndRect.anchorMax = new Vector2(0.18f, 0.44f);
            bkrIndRect.offsetMin = Vector2.zero;
            bkrIndRect.offsetMax = Vector2.zero;
            Image bkrIndImg = bkrIndGO.AddComponent<Image>();
            bkrIndImg.color = new Color(0.4f, 0.4f, 0.4f);
            so.FindProperty("indicator_GenBreaker").objectReferenceValue = bkrIndImg;

            TextMeshProUGUI bkrStatusText = CreateTMPText(genSection.transform, "BreakerStatus",
                "OPEN", 10, new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.20f, 0.28f), new Vector2(0.95f, 0.44f));
            bkrStatusText.alignment = TextAlignmentOptions.MidlineLeft;
            so.FindProperty("text_GenBreakerStatus").objectReferenceValue = bkrStatusText;

            // --- Status Section ---
            GameObject statusSection = CreatePanel(panelGO.transform, "StatusSection",
                new Vector2(0.59f, 0.05f), new Vector2(0.78f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(statusSection.transform, "STATUS");

            TextMeshProUGUI modeText = CreateTMPText(statusSection.transform, "ModeText",
                "MODE 5", 13, COLOR_AMBER,
                new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.82f));
            modeText.alignment = TextAlignmentOptions.Center;
            modeText.fontStyle = FontStyles.Bold;
            so.FindProperty("text_ReactorMode").objectReferenceValue = modeText;

            TextMeshProUGUI simTimeText = CreateTMPText(statusSection.transform, "SimTimeText",
                "00:00:00", 14, COLOR_GREEN,
                new Vector2(0.05f, 0.38f), new Vector2(0.60f, 0.58f));
            so.FindProperty("text_SimTime").objectReferenceValue = simTimeText;

            TextMeshProUGUI timeCompText = CreateTMPText(statusSection.transform, "TimeCompText",
                "1x", 14, COLOR_GREEN,
                new Vector2(0.62f, 0.38f), new Vector2(0.95f, 0.58f));
            so.FindProperty("text_TimeCompression").objectReferenceValue = timeCompText;

            // --- Alarm Section ---
            GameObject alarmSection = CreatePanel(panelGO.transform, "AlarmSection",
                new Vector2(0.79f, 0.05f), new Vector2(0.99f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(alarmSection.transform, "ALARMS");

            GameObject alarmContainer = new GameObject("AlarmContainer");
            alarmContainer.transform.SetParent(alarmSection.transform, false);
            RectTransform acRect = alarmContainer.AddComponent<RectTransform>();
            acRect.anchorMin = new Vector2(0.02f, 0.02f);
            acRect.anchorMax = new Vector2(0.98f, 0.85f);
            acRect.offsetMin = Vector2.zero;
            acRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup aVLG = alarmContainer.AddComponent<VerticalLayoutGroup>();
            aVLG.padding = new RectOffset(2, 2, 2, 2);
            aVLG.spacing = 1f;
            aVLG.childAlignment = TextAnchor.UpperLeft;
            aVLG.childControlWidth = true;
            aVLG.childControlHeight = false;
            aVLG.childForceExpandWidth = true;
            aVLG.childForceExpandHeight = false;

            so.FindProperty("alarmContainer").objectReferenceValue = alarmContainer.transform;
            so.ApplyModifiedProperties();
        }

        #endregion

        // ####################################################################
        //
        //  SCREEN 7 — SECONDARY SYSTEMS
        //
        //  Secondary cycle flow diagram, feedwater train, steam dump,
        //  MSIVs, condensate/deaerator/FW heaters.
        //
        // ####################################################################

        #region Screen 7 - Secondary Systems

        private static void CreateSecondarySystemsScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 7 — Secondary Systems...");

            foreach (Transform child in canvasParent)
            {
                if (child.name == "SecondarySystemsScreen")
                {
                    Debug.LogWarning("[MultiScreenBuilder] SecondarySystemsScreen already exists. Skipping.");
                    return;
                }
            }

            GameObject screenGO = new GameObject("SecondarySystemsScreen");
            screenGO.transform.SetParent(canvasParent, false);

            RectTransform rootRect = screenGO.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image rootBg = screenGO.AddComponent<Image>();
            rootBg.color = COLOR_BACKGROUND;

            screenGO.AddComponent<CanvasGroup>();
            SecondarySystemsScreen screen = screenGO.AddComponent<SecondarySystemsScreen>();

            BuildSecLeftPanel(screenGO.transform, screen);
            BuildSecCenterPanel(screenGO.transform, screen);
            BuildSecRightPanel(screenGO.transform, screen);
            BuildSecBottomPanel(screenGO.transform, screen);

            screenGO.SetActive(false);
            Debug.Log("[MultiScreenBuilder] Screen 7 — Secondary Systems — build complete");
        }

        // ----------------------------------------------------------------
        // SEC: Left Panel — 8 Feedwater Train Gauges
        // ----------------------------------------------------------------

        private static void BuildSecLeftPanel(Transform parent, SecondarySystemsScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "LeftPanel",
                new Vector2(0f, 0.26f), new Vector2(0.15f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "LeftHeader", "FEEDWATER TRAIN", 11, COLOR_CYAN);

            SerializedObject so = new SerializedObject(screen);
            so.FindProperty("text_HotwellLevel").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "HotwellLvl", "HOTWELL LVL");
            so.FindProperty("text_CondPumpDischP").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "CondPDisch", "COND P DISCH");
            so.FindProperty("text_DeaeratorPressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "DeaerP", "DEAERATOR P");
            so.FindProperty("text_DeaeratorLevel").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "DeaerLvl", "DEAERATOR LVL");
            so.FindProperty("text_FWPumpSuctionP").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "FWPSuct", "FWP SUCTION P");
            so.FindProperty("text_FWPumpDischP").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "FWPDisch", "FWP DISCH P");
            so.FindProperty("text_FinalFWTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "FinalFWT", "FINAL FW TEMP");
            so.FindProperty("text_FWFlowTotal").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "FWFlow", "FW FLOW TOTAL");
            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // SEC: Center Panel — Secondary Cycle Flow Diagram
        // ----------------------------------------------------------------

        private static void BuildSecCenterPanel(Transform parent, SecondarySystemsScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "CenterPanel",
                new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f), COLOR_CENTER_BG);

            CreateTMPLabel(panelGO.transform, "ScreenTitle", "SECONDARY SYSTEMS", 16, COLOR_CYAN,
                new Vector2(0.02f, 0.93f), new Vector2(0.98f, 0.99f));
            CreateTMPLabel(panelGO.transform, "ScreenSubtitle",
                "FEEDWATER — MAIN STEAM — STEAM DUMP — CONDENSATE", 10, COLOR_TEXT_LABEL,
                new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.93f));

            SerializedObject so = new SerializedObject(screen);

            // Flow path: LEFT to RIGHT
            // Condenser -> Cond Pumps -> LP Heaters -> Deaerator -> FW Pumps -> HP Heaters -> SGs
            // SGs -> Main Steam Header -> Steam Dump / Turbine

            // --- Condenser (bottom left) ---
            Image condenser = CreateMimicBox(panelGO.transform, "Condenser",
                new Vector2(0.02f, 0.05f), new Vector2(0.14f, 0.25f),
                new Color(0.12f, 0.15f, 0.20f));
            so.FindProperty("diagram_Condenser").objectReferenceValue = condenser;
            CreateTMPLabel(condenser.transform, "Label", "COND", 8, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // --- Condensate Pumps ---
            Image condPumps = CreateMimicBox(panelGO.transform, "CondPumps",
                new Vector2(0.16f, 0.08f), new Vector2(0.24f, 0.22f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_CondPumps").objectReferenceValue = condPumps;
            CreateTMPLabel(condPumps.transform, "Label", "COND\nPUMP", 7, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // --- LP Heaters ---
            Image lpHeaters = CreateMimicBox(panelGO.transform, "LPHeaters",
                new Vector2(0.26f, 0.05f), new Vector2(0.38f, 0.25f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_LPHeaters").objectReferenceValue = lpHeaters;
            CreateTMPLabel(lpHeaters.transform, "Label", "LP FWH\n1-3", 8, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // --- Deaerator ---
            Image deaerator = CreateMimicBox(panelGO.transform, "Deaerator",
                new Vector2(0.40f, 0.05f), new Vector2(0.52f, 0.28f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_Deaerator").objectReferenceValue = deaerator;
            CreateTMPLabel(deaerator.transform, "Label", "DA\nTANK", 8, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // --- FW Pumps ---
            Image fwPumps = CreateMimicBox(panelGO.transform, "FWPumps",
                new Vector2(0.54f, 0.08f), new Vector2(0.62f, 0.22f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_FWPumps").objectReferenceValue = fwPumps;
            CreateTMPLabel(fwPumps.transform, "Label", "MFW\nPUMP", 7, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // --- HP Heaters ---
            Image hpHeaters = CreateMimicBox(panelGO.transform, "HPHeaters",
                new Vector2(0.64f, 0.05f), new Vector2(0.76f, 0.25f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_HPHeaters").objectReferenceValue = hpHeaters;
            CreateTMPLabel(hpHeaters.transform, "Label", "HP FWH\n4-6", 8, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // --- FW to SG Lines ---
            Image fwToSG = CreateMimicBox(panelGO.transform, "FWToSGLines",
                new Vector2(0.78f, 0.12f), new Vector2(0.98f, 0.16f),
                new Color(0.25f, 0.25f, 0.30f, 0.4f));
            so.FindProperty("diagram_FWToSGLines").objectReferenceValue = fwToSG;
            CreateTMPLabel(panelGO.transform, "FWtoSGLabel", "FW \u2192 SGs", 7, COLOR_TEXT_LABEL,
                new Vector2(0.78f, 0.17f), new Vector2(0.98f, 0.24f));

            TextMeshProUGUI fwFlowText = CreateTMPText(panelGO.transform, "FWFlowText",
                "FW: ---", 9, new Color(0.4f, 0.4f, 0.5f),
                new Vector2(0.78f, 0.24f), new Vector2(0.98f, 0.32f));
            so.FindProperty("diagram_FWFlowText").objectReferenceValue = fwFlowText;

            // --- Steam from SG Lines (upper path, right to left) ---
            Image steamFromSG = CreateMimicBox(panelGO.transform, "SteamFromSGLines",
                new Vector2(0.60f, 0.68f), new Vector2(0.98f, 0.72f),
                new Color(0.25f, 0.25f, 0.30f, 0.4f));
            so.FindProperty("diagram_SteamFromSGLines").objectReferenceValue = steamFromSG;
            CreateTMPLabel(panelGO.transform, "SteamFromSGLabel", "\u2190 STM from SGs", 7, COLOR_TEXT_LABEL,
                new Vector2(0.70f, 0.72f), new Vector2(0.98f, 0.80f));

            // --- MSIV Block ---
            Image msivBlock = CreateMimicBox(panelGO.transform, "MSIVBlock",
                new Vector2(0.48f, 0.62f), new Vector2(0.58f, 0.78f),
                new Color(0.2f, 0.3f, 0.2f));
            so.FindProperty("diagram_MSIVBlock").objectReferenceValue = msivBlock;
            CreateTMPLabel(msivBlock.transform, "Label", "MSIVs\nOPEN", 8, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // --- Main Steam Header ---
            Image mainSteam = CreateMimicBox(panelGO.transform, "MainSteamHeader",
                new Vector2(0.15f, 0.65f), new Vector2(0.46f, 0.75f),
                new Color(0.25f, 0.25f, 0.30f, 0.4f));
            so.FindProperty("diagram_MainSteamHeader").objectReferenceValue = mainSteam;

            TextMeshProUGUI stmPText = CreateTMPText(panelGO.transform, "SteamPressureText",
                "--- psig", 11, new Color(0f, 1f, 0.53f),
                new Vector2(0.15f, 0.76f), new Vector2(0.46f, 0.85f));
            stmPText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("diagram_SteamPressureText").objectReferenceValue = stmPText;

            CreateTMPLabel(panelGO.transform, "MSHLabel", "MAIN STEAM HEADER", 8, COLOR_TEXT_LABEL,
                new Vector2(0.15f, 0.56f), new Vector2(0.46f, 0.64f));

            // --- Steam Dump Valves (branch off main steam) ---
            Image steamDump = CreateMimicBox(panelGO.transform, "SteamDumpValves",
                new Vector2(0.04f, 0.55f), new Vector2(0.14f, 0.75f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_SteamDumpValves").objectReferenceValue = steamDump;
            CreateTMPLabel(steamDump.transform, "Label", "STEAM\nDUMP", 8, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            TextMeshProUGUI sdText = CreateTMPText(panelGO.transform, "SteamDumpText",
                "DUMP OFF", 9, new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.02f, 0.76f), new Vector2(0.18f, 0.84f));
            sdText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("diagram_SteamDumpText").objectReferenceValue = sdText;

            // "To Turbine" label from main steam
            CreateTMPLabel(panelGO.transform, "ToTurbineLabel", "\u2192 TURBINE", 8, COLOR_TEXT_LABEL,
                new Vector2(0.15f, 0.44f), new Vector2(0.35f, 0.54f));

            // "To Condenser" from steam dump
            CreateTMPLabel(panelGO.transform, "ToCond", "\u2193 COND", 7, COLOR_TEXT_LABEL,
                new Vector2(0.04f, 0.44f), new Vector2(0.14f, 0.54f));

            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // SEC: Right Panel — 8 Steam System Gauges
        // ----------------------------------------------------------------

        private static void BuildSecRightPanel(Transform parent, SecondarySystemsScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "RightPanel",
                new Vector2(0.65f, 0.26f), new Vector2(1f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "RightHeader", "STEAM SYSTEM", 11, COLOR_CYAN);

            SerializedObject so = new SerializedObject(screen);
            so.FindProperty("text_MainSteamPressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "MSHPress", "MS PRESSURE");
            so.FindProperty("text_SteamFlowToTurbine").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SteamToTurb", "STM TO TURB");
            so.FindProperty("text_SteamDumpFlow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SteamDump", "STEAM DUMP");
            so.FindProperty("text_MSIV_A").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "MSIVA", "MSIV-A");
            so.FindProperty("text_MSIV_B").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "MSIVB", "MSIV-B");
            so.FindProperty("text_MSIV_C").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "MSIVC", "MSIV-C");
            so.FindProperty("text_MSIV_D").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "MSIVD", "MSIV-D");
            so.FindProperty("text_TurbineBypass").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "TurbBypass", "TURB BYPASS");
            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // SEC: Bottom Panel — Steam Dump Controls, MSIV Controls, Alarms
        // ----------------------------------------------------------------

        private static void BuildSecBottomPanel(Transform parent, SecondarySystemsScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "BottomPanel",
                new Vector2(0f, 0f), new Vector2(1f, 0.26f), COLOR_PANEL);

            SerializedObject so = new SerializedObject(screen);

            // --- Steam Dump Controls ---
            GameObject sdSection = CreatePanel(panelGO.transform, "SteamDumpSection",
                new Vector2(0.01f, 0.05f), new Vector2(0.35f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(sdSection.transform, "STEAM DUMP");

            Button autoBtn = CreateTMPButton(sdSection.transform, "AutoBtn", "AUTO",
                new Vector2(0.05f, 0.55f), new Vector2(0.35f, 0.80f),
                new Color(0.15f, 0.30f, 0.15f));
            so.FindProperty("button_SteamDumpAuto").objectReferenceValue = autoBtn;

            Button manBtn = CreateTMPButton(sdSection.transform, "ManualBtn", "MANUAL",
                new Vector2(0.38f, 0.55f), new Vector2(0.68f, 0.80f),
                new Color(0.30f, 0.25f, 0.10f));
            so.FindProperty("button_SteamDumpManual").objectReferenceValue = manBtn;

            TextMeshProUGUI sdModeText = CreateTMPText(sdSection.transform, "SDMode",
                "OFF", 12, new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.70f, 0.55f), new Vector2(0.98f, 0.80f));
            sdModeText.alignment = TextAlignmentOptions.Center;
            sdModeText.fontStyle = FontStyles.Bold;
            so.FindProperty("text_SteamDumpMode").objectReferenceValue = sdModeText;

            TextMeshProUGUI sdDemandText = CreateTMPText(sdSection.transform, "SDDemand",
                "DEMAND: ---", 10, new Color(0f, 1f, 0.53f),
                new Vector2(0.05f, 0.28f), new Vector2(0.48f, 0.48f));
            so.FindProperty("text_SteamDumpDemand").objectReferenceValue = sdDemandText;

            TextMeshProUGUI sdHeatText = CreateTMPText(sdSection.transform, "SDHeat",
                "HEAT: ---", 10, new Color(0f, 1f, 0.53f),
                new Vector2(0.52f, 0.28f), new Vector2(0.98f, 0.48f));
            so.FindProperty("text_SteamDumpHeat").objectReferenceValue = sdHeatText;

            // --- MSIV Controls ---
            GameObject msivSection = CreatePanel(panelGO.transform, "MSIVSection",
                new Vector2(0.36f, 0.05f), new Vector2(0.58f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(msivSection.transform, "MSIVs");

            Button msivOpenBtn = CreateTMPButton(msivSection.transform, "MSIVOpen", "OPEN ALL",
                new Vector2(0.05f, 0.50f), new Vector2(0.48f, 0.80f),
                new Color(0.15f, 0.30f, 0.15f));
            so.FindProperty("button_MSIV_Open").objectReferenceValue = msivOpenBtn;

            Button msivCloseBtn = CreateTMPButton(msivSection.transform, "MSIVClose", "CLOSE ALL",
                new Vector2(0.52f, 0.50f), new Vector2(0.95f, 0.80f),
                new Color(0.30f, 0.15f, 0.15f));
            so.FindProperty("button_MSIV_Close").objectReferenceValue = msivCloseBtn;

            TextMeshProUGUI msivStatusText = CreateTMPText(msivSection.transform, "MSIVStatus",
                "ALL OPEN", 11, new Color(0.2f, 0.9f, 0.2f),
                new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.45f));
            msivStatusText.alignment = TextAlignmentOptions.Center;
            msivStatusText.fontStyle = FontStyles.Bold;
            so.FindProperty("text_MSIVStatus").objectReferenceValue = msivStatusText;

            // --- Status Section ---
            GameObject statusSection = CreatePanel(panelGO.transform, "StatusSection",
                new Vector2(0.59f, 0.05f), new Vector2(0.78f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(statusSection.transform, "STATUS");

            TextMeshProUGUI modeText = CreateTMPText(statusSection.transform, "ModeText",
                "MODE 5", 13, COLOR_AMBER,
                new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.82f));
            modeText.alignment = TextAlignmentOptions.Center;
            modeText.fontStyle = FontStyles.Bold;
            so.FindProperty("text_ReactorMode").objectReferenceValue = modeText;

            TextMeshProUGUI simTimeText = CreateTMPText(statusSection.transform, "SimTimeText",
                "00:00:00", 14, COLOR_GREEN,
                new Vector2(0.05f, 0.38f), new Vector2(0.60f, 0.58f));
            so.FindProperty("text_SimTime").objectReferenceValue = simTimeText;

            TextMeshProUGUI timeCompText = CreateTMPText(statusSection.transform, "TimeCompText",
                "1x", 14, COLOR_GREEN,
                new Vector2(0.62f, 0.38f), new Vector2(0.95f, 0.58f));
            so.FindProperty("text_TimeCompression").objectReferenceValue = timeCompText;

            // --- Alarm Section ---
            GameObject alarmSection = CreatePanel(panelGO.transform, "AlarmSection",
                new Vector2(0.79f, 0.05f), new Vector2(0.99f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(alarmSection.transform, "ALARMS");

            GameObject alarmContainer = new GameObject("AlarmContainer");
            alarmContainer.transform.SetParent(alarmSection.transform, false);
            RectTransform acRect = alarmContainer.AddComponent<RectTransform>();
            acRect.anchorMin = new Vector2(0.02f, 0.02f);
            acRect.anchorMax = new Vector2(0.98f, 0.85f);
            acRect.offsetMin = Vector2.zero;
            acRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup aVLG = alarmContainer.AddComponent<VerticalLayoutGroup>();
            aVLG.padding = new RectOffset(2, 2, 2, 2);
            aVLG.spacing = 1f;
            aVLG.childAlignment = TextAnchor.UpperLeft;
            aVLG.childControlWidth = true;
            aVLG.childControlHeight = false;
            aVLG.childForceExpandWidth = true;
            aVLG.childForceExpandHeight = false;

            so.FindProperty("alarmContainer").objectReferenceValue = alarmContainer.transform;
            so.ApplyModifiedProperties();
        }

        #endregion

        // ####################################################################
        //
        //  SCREEN 8 — AUXILIARY SYSTEMS
        //
        //  RHR, CCW, Service Water overview.
        //  Entirely PLACEHOLDER — no auxiliary system models exist.
        //
        // ####################################################################

        #region Screen 8 - Auxiliary Systems

        private static void CreateAuxiliarySystemsScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 8 — Auxiliary Systems...");

            foreach (Transform child in canvasParent)
            {
                if (child.name == "AuxiliarySystemsScreen")
                {
                    Debug.LogWarning("[MultiScreenBuilder] AuxiliarySystemsScreen already exists. Skipping.");
                    return;
                }
            }

            GameObject screenGO = new GameObject("AuxiliarySystemsScreen");
            screenGO.transform.SetParent(canvasParent, false);

            RectTransform rootRect = screenGO.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image rootBg = screenGO.AddComponent<Image>();
            rootBg.color = COLOR_BACKGROUND;

            screenGO.AddComponent<CanvasGroup>();
            AuxiliarySystemsScreen screen = screenGO.AddComponent<AuxiliarySystemsScreen>();

            BuildAuxLeftPanel(screenGO.transform, screen);
            BuildAuxCenterPanel(screenGO.transform, screen);
            BuildAuxRightPanel(screenGO.transform, screen);
            BuildAuxBottomPanel(screenGO.transform, screen);

            screenGO.SetActive(false);
            Debug.Log("[MultiScreenBuilder] Screen 8 — Auxiliary Systems — build complete");
        }

        // ----------------------------------------------------------------
        // AUX: Left Panel — 8 RHR System Gauges
        // ----------------------------------------------------------------

        private static void BuildAuxLeftPanel(Transform parent, AuxiliarySystemsScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "LeftPanel",
                new Vector2(0f, 0.26f), new Vector2(0.15f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "LeftHeader", "RHR SYSTEM", 11, COLOR_CYAN);

            SerializedObject so = new SerializedObject(screen);
            so.FindProperty("text_RHR_A_Flow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "RHRA_Flow", "RHR-A FLOW");
            so.FindProperty("text_RHR_B_Flow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "RHRB_Flow", "RHR-B FLOW");
            so.FindProperty("text_RHR_HXA_InletTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "HXA_In", "HX-A INLET T");
            so.FindProperty("text_RHR_HXA_OutletTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "HXA_Out", "HX-A OUTLET T");
            so.FindProperty("text_RHR_HXB_InletTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "HXB_In", "HX-B INLET T");
            so.FindProperty("text_RHR_HXB_OutletTemp").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "HXB_Out", "HX-B OUTLET T");
            so.FindProperty("text_RHR_SuctionPressure").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "RHR_Suct", "RHR SUCTION P");
            so.FindProperty("text_RHR_PumpStatus").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "RHR_Pump", "RHR PUMP");
            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // AUX: Center Panel — Auxiliary Systems Diagram
        // ----------------------------------------------------------------

        private static void BuildAuxCenterPanel(Transform parent, AuxiliarySystemsScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "CenterPanel",
                new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f), COLOR_CENTER_BG);

            CreateTMPLabel(panelGO.transform, "ScreenTitle", "AUXILIARY SYSTEMS", 16, COLOR_CYAN,
                new Vector2(0.02f, 0.93f), new Vector2(0.98f, 0.99f));
            CreateTMPLabel(panelGO.transform, "ScreenSubtitle",
                "RHR — CCW — SERVICE WATER", 10, COLOR_TEXT_LABEL,
                new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.93f));

            SerializedObject so = new SerializedObject(screen);

            // --- RCS Connection (top center) ---
            Image rcsConn = CreateMimicBox(panelGO.transform, "RCSConnection",
                new Vector2(0.35f, 0.75f), new Vector2(0.65f, 0.85f),
                new Color(0.25f, 0.25f, 0.30f, 0.4f));
            so.FindProperty("diagram_RCS_Connection").objectReferenceValue = rcsConn;
            CreateTMPLabel(rcsConn.transform, "Label", "RCS\n(<350\u00b0F, <425 psig)", 8, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // --- RHR Train A (left side) ---
            Image rhrPumpA = CreateMimicBox(panelGO.transform, "RHR_Pump_A",
                new Vector2(0.08f, 0.55f), new Vector2(0.22f, 0.70f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_RHR_Pump_A").objectReferenceValue = rhrPumpA;
            CreateTMPLabel(rhrPumpA.transform, "Label", "RHR\nPUMP A", 8, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            Image rhrHXA = CreateMimicBox(panelGO.transform, "RHR_HX_A",
                new Vector2(0.08f, 0.35f), new Vector2(0.22f, 0.52f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_RHR_HX_A").objectReferenceValue = rhrHXA;
            CreateTMPLabel(rhrHXA.transform, "Label", "RHR\nHX A", 8, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // --- RHR Train B (right side) ---
            Image rhrPumpB = CreateMimicBox(panelGO.transform, "RHR_Pump_B",
                new Vector2(0.78f, 0.55f), new Vector2(0.92f, 0.70f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_RHR_Pump_B").objectReferenceValue = rhrPumpB;
            CreateTMPLabel(rhrPumpB.transform, "Label", "RHR\nPUMP B", 8, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            Image rhrHXB = CreateMimicBox(panelGO.transform, "RHR_HX_B",
                new Vector2(0.78f, 0.35f), new Vector2(0.92f, 0.52f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_RHR_HX_B").objectReferenceValue = rhrHXB;
            CreateTMPLabel(rhrHXB.transform, "Label", "RHR\nHX B", 8, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // --- CCW Header (middle) ---
            Image ccwHeader = CreateMimicBox(panelGO.transform, "CCW_Header",
                new Vector2(0.25f, 0.22f), new Vector2(0.75f, 0.28f),
                new Color(0.25f, 0.25f, 0.30f, 0.4f));
            so.FindProperty("diagram_CCW_Header").objectReferenceValue = ccwHeader;
            CreateTMPLabel(panelGO.transform, "CCWHeaderLabel", "CCW HEADER", 8, COLOR_TEXT_LABEL,
                new Vector2(0.35f, 0.28f), new Vector2(0.65f, 0.34f));

            // --- CCW HXs ---
            Image ccwHXA = CreateMimicBox(panelGO.transform, "CCW_HX_A",
                new Vector2(0.28f, 0.08f), new Vector2(0.42f, 0.20f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_CCW_HX_A").objectReferenceValue = ccwHXA;
            CreateTMPLabel(ccwHXA.transform, "Label", "CCW\nHX A", 7, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            Image ccwHXB = CreateMimicBox(panelGO.transform, "CCW_HX_B",
                new Vector2(0.58f, 0.08f), new Vector2(0.72f, 0.20f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_CCW_HX_B").objectReferenceValue = ccwHXB;
            CreateTMPLabel(ccwHXB.transform, "Label", "CCW\nHX B", 7, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // --- CCW Pumps ---
            Image ccwPumps = CreateMimicBox(panelGO.transform, "CCW_Pumps",
                new Vector2(0.44f, 0.08f), new Vector2(0.56f, 0.20f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_CCW_Pumps").objectReferenceValue = ccwPumps;
            CreateTMPLabel(ccwPumps.transform, "Label", "CCW\nPUMPS", 7, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // --- SW Header ---
            Image swHeader = CreateMimicBox(panelGO.transform, "SW_Header",
                new Vector2(0.25f, 0.02f), new Vector2(0.75f, 0.06f),
                new Color(0.15f, 0.20f, 0.30f, 0.4f));
            so.FindProperty("diagram_SW_Header").objectReferenceValue = swHeader;

            // --- SW Pumps ---
            Image swPumps = CreateMimicBox(panelGO.transform, "SW_Pumps",
                new Vector2(0.02f, 0.02f), new Vector2(0.22f, 0.14f),
                new Color(0.15f, 0.15f, 0.18f));
            so.FindProperty("diagram_SW_Pumps").objectReferenceValue = swPumps;
            CreateTMPLabel(swPumps.transform, "Label", "SW PUMPS\n(LAKE/RIVER)", 7, COLOR_TEXT_LABEL,
                new Vector2(0f, 0f), new Vector2(1f, 1f));

            // Status overlay texts
            TextMeshProUGUI rhrText = CreateTMPText(panelGO.transform, "RHR_Status",
                "NOT MODELED", 10, new Color(0.4f, 0.4f, 0.5f),
                new Vector2(0.30f, 0.58f), new Vector2(0.70f, 0.68f));
            rhrText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("diagram_RHR_StatusText").objectReferenceValue = rhrText;

            TextMeshProUGUI ccwText = CreateTMPText(panelGO.transform, "CCW_Status",
                "NOT MODELED", 10, new Color(0.4f, 0.4f, 0.5f),
                new Vector2(0.30f, 0.38f), new Vector2(0.70f, 0.48f));
            ccwText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("diagram_CCW_StatusText").objectReferenceValue = ccwText;

            TextMeshProUGUI swText = CreateTMPText(panelGO.transform, "SW_Status",
                "NOT MODELED", 10, new Color(0.4f, 0.4f, 0.5f),
                new Vector2(0.30f, 0.15f), new Vector2(0.70f, 0.22f));
            swText.alignment = TextAlignmentOptions.Center;
            so.FindProperty("diagram_SW_StatusText").objectReferenceValue = swText;

            // "ALL SYSTEMS PLACEHOLDER" overlay
            CreateTMPLabel(panelGO.transform, "NotModeled",
                "AUXILIARY SYSTEMS NOT IMPLEMENTED", 10, new Color(1f, 0.5f, 0.2f, 0.6f),
                new Vector2(0.15f, 0.82f), new Vector2(0.85f, 0.88f));

            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // AUX: Right Panel — 8 Cooling Water Gauges
        // ----------------------------------------------------------------

        private static void BuildAuxRightPanel(Transform parent, AuxiliarySystemsScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "RightPanel",
                new Vector2(0.65f, 0.26f), new Vector2(1f, 1f), COLOR_PANEL);

            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            CreateTMPLabel(panelGO.transform, "RightHeader", "COOLING WATER", 11, COLOR_CYAN);

            SerializedObject so = new SerializedObject(screen);
            so.FindProperty("text_CCW_SupplyP").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "CCWSupply", "CCW SUPPLY P");
            so.FindProperty("text_CCW_ReturnP").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "CCWReturn", "CCW RETURN P");
            so.FindProperty("text_CCW_SurgeTankLevel").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "CCWSurge", "CCW SURGE LVL");
            so.FindProperty("text_CCW_Temperature").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "CCWTemp", "CCW TEMP");
            so.FindProperty("text_SW_Flow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SWFlow", "SW FLOW");
            so.FindProperty("text_SW_Temperature").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "SWTemp", "SW TEMP");
            so.FindProperty("text_RCP_ThermalBarrierFlow").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "TBFlow", "THERMAL BARRIER");
            so.FindProperty("text_CCW_HeatLoad").objectReferenceValue =
                CreateOverviewGaugeItem(panelGO.transform, "CCWHeat", "CCW HEAT LOAD");
            so.ApplyModifiedProperties();
        }

        // ----------------------------------------------------------------
        // AUX: Bottom Panel — Pump Controls, Status, Alarms
        // ----------------------------------------------------------------

        private static void BuildAuxBottomPanel(Transform parent, AuxiliarySystemsScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "BottomPanel",
                new Vector2(0f, 0f), new Vector2(1f, 0.26f), COLOR_PANEL);

            SerializedObject so = new SerializedObject(screen);

            // --- RHR Controls ---
            GameObject rhrSection = CreatePanel(panelGO.transform, "RHRSection",
                new Vector2(0.01f, 0.05f), new Vector2(0.35f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(rhrSection.transform, "RHR PUMPS");

            // Train A
            Button rhrAStart = CreateTMPButton(rhrSection.transform, "RHRA_Start", "A START",
                new Vector2(0.03f, 0.50f), new Vector2(0.25f, 0.80f),
                new Color(0.15f, 0.30f, 0.15f));
            so.FindProperty("button_RHR_A_Start").objectReferenceValue = rhrAStart;

            Button rhrAStop = CreateTMPButton(rhrSection.transform, "RHRA_Stop", "A STOP",
                new Vector2(0.27f, 0.50f), new Vector2(0.49f, 0.80f),
                new Color(0.30f, 0.15f, 0.15f));
            so.FindProperty("button_RHR_A_Stop").objectReferenceValue = rhrAStop;

            // Train A indicator
            GameObject indAGO = new GameObject("RHR_A_Indicator");
            indAGO.transform.SetParent(rhrSection.transform, false);
            RectTransform indARect = indAGO.AddComponent<RectTransform>();
            indARect.anchorMin = new Vector2(0.03f, 0.28f);
            indARect.anchorMax = new Vector2(0.10f, 0.44f);
            indARect.offsetMin = Vector2.zero;
            indARect.offsetMax = Vector2.zero;
            Image indAImg = indAGO.AddComponent<Image>();
            indAImg.color = new Color(0.4f, 0.4f, 0.4f);
            so.FindProperty("indicator_RHR_A").objectReferenceValue = indAImg;

            TextMeshProUGUI rhrAStatusText = CreateTMPText(rhrSection.transform, "RHRA_Status",
                "STOPPED", 9, new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.12f, 0.28f), new Vector2(0.49f, 0.44f));
            rhrAStatusText.alignment = TextAlignmentOptions.MidlineLeft;
            so.FindProperty("text_RHR_A_Status").objectReferenceValue = rhrAStatusText;

            // Train B
            Button rhrBStart = CreateTMPButton(rhrSection.transform, "RHRB_Start", "B START",
                new Vector2(0.51f, 0.50f), new Vector2(0.73f, 0.80f),
                new Color(0.15f, 0.30f, 0.15f));
            so.FindProperty("button_RHR_B_Start").objectReferenceValue = rhrBStart;

            Button rhrBStop = CreateTMPButton(rhrSection.transform, "RHRB_Stop", "B STOP",
                new Vector2(0.75f, 0.50f), new Vector2(0.97f, 0.80f),
                new Color(0.30f, 0.15f, 0.15f));
            so.FindProperty("button_RHR_B_Stop").objectReferenceValue = rhrBStop;

            // Train B indicator
            GameObject indBGO = new GameObject("RHR_B_Indicator");
            indBGO.transform.SetParent(rhrSection.transform, false);
            RectTransform indBRect = indBGO.AddComponent<RectTransform>();
            indBRect.anchorMin = new Vector2(0.51f, 0.28f);
            indBRect.anchorMax = new Vector2(0.58f, 0.44f);
            indBRect.offsetMin = Vector2.zero;
            indBRect.offsetMax = Vector2.zero;
            Image indBImg = indBGO.AddComponent<Image>();
            indBImg.color = new Color(0.4f, 0.4f, 0.4f);
            so.FindProperty("indicator_RHR_B").objectReferenceValue = indBImg;

            TextMeshProUGUI rhrBStatusText = CreateTMPText(rhrSection.transform, "RHRB_Status",
                "STOPPED", 9, new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.60f, 0.28f), new Vector2(0.97f, 0.44f));
            rhrBStatusText.alignment = TextAlignmentOptions.MidlineLeft;
            so.FindProperty("text_RHR_B_Status").objectReferenceValue = rhrBStatusText;

            // --- CCW/SW Controls ---
            GameObject cwSection = CreatePanel(panelGO.transform, "CWSection",
                new Vector2(0.36f, 0.05f), new Vector2(0.58f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(cwSection.transform, "CCW / SW");

            Button ccwBtn = CreateTMPButton(cwSection.transform, "CCW_Start", "CCW START",
                new Vector2(0.05f, 0.50f), new Vector2(0.48f, 0.80f),
                new Color(0.15f, 0.30f, 0.15f));
            so.FindProperty("button_CCW_Start").objectReferenceValue = ccwBtn;

            Button swBtn = CreateTMPButton(cwSection.transform, "SW_Start", "SW START",
                new Vector2(0.52f, 0.50f), new Vector2(0.95f, 0.80f),
                new Color(0.15f, 0.30f, 0.15f));
            so.FindProperty("button_SW_Start").objectReferenceValue = swBtn;

            // CCW indicator
            GameObject ccwIndGO = new GameObject("CCW_Indicator");
            ccwIndGO.transform.SetParent(cwSection.transform, false);
            RectTransform ccwIndRect = ccwIndGO.AddComponent<RectTransform>();
            ccwIndRect.anchorMin = new Vector2(0.05f, 0.28f);
            ccwIndRect.anchorMax = new Vector2(0.12f, 0.44f);
            ccwIndRect.offsetMin = Vector2.zero;
            ccwIndRect.offsetMax = Vector2.zero;
            Image ccwIndImg = ccwIndGO.AddComponent<Image>();
            ccwIndImg.color = new Color(0.4f, 0.4f, 0.4f);
            so.FindProperty("indicator_CCW").objectReferenceValue = ccwIndImg;

            TextMeshProUGUI ccwStatusText = CreateTMPText(cwSection.transform, "CCW_Status",
                "STOPPED", 9, new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.14f, 0.28f), new Vector2(0.48f, 0.44f));
            ccwStatusText.alignment = TextAlignmentOptions.MidlineLeft;
            so.FindProperty("text_CCW_Status").objectReferenceValue = ccwStatusText;

            // SW indicator
            GameObject swIndGO = new GameObject("SW_Indicator");
            swIndGO.transform.SetParent(cwSection.transform, false);
            RectTransform swIndRect = swIndGO.AddComponent<RectTransform>();
            swIndRect.anchorMin = new Vector2(0.52f, 0.28f);
            swIndRect.anchorMax = new Vector2(0.59f, 0.44f);
            swIndRect.offsetMin = Vector2.zero;
            swIndRect.offsetMax = Vector2.zero;
            Image swIndImg = swIndGO.AddComponent<Image>();
            swIndImg.color = new Color(0.4f, 0.4f, 0.4f);
            so.FindProperty("indicator_SW").objectReferenceValue = swIndImg;

            TextMeshProUGUI swStatusText = CreateTMPText(cwSection.transform, "SW_Status",
                "STOPPED", 9, new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.61f, 0.28f), new Vector2(0.95f, 0.44f));
            swStatusText.alignment = TextAlignmentOptions.MidlineLeft;
            so.FindProperty("text_SW_Status").objectReferenceValue = swStatusText;

            // --- Status Section ---
            GameObject statusSection = CreatePanel(panelGO.transform, "StatusSection",
                new Vector2(0.59f, 0.05f), new Vector2(0.78f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(statusSection.transform, "STATUS");

            TextMeshProUGUI modeText = CreateTMPText(statusSection.transform, "ModeText",
                "MODE 5", 13, COLOR_AMBER,
                new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.82f));
            modeText.alignment = TextAlignmentOptions.Center;
            modeText.fontStyle = FontStyles.Bold;
            so.FindProperty("text_ReactorMode").objectReferenceValue = modeText;

            TextMeshProUGUI simTimeText = CreateTMPText(statusSection.transform, "SimTimeText",
                "00:00:00", 14, COLOR_GREEN,
                new Vector2(0.05f, 0.38f), new Vector2(0.60f, 0.58f));
            so.FindProperty("text_SimTime").objectReferenceValue = simTimeText;

            TextMeshProUGUI timeCompText = CreateTMPText(statusSection.transform, "TimeCompText",
                "1x", 14, COLOR_GREEN,
                new Vector2(0.62f, 0.38f), new Vector2(0.95f, 0.58f));
            so.FindProperty("text_TimeCompression").objectReferenceValue = timeCompText;

            CreateTMPText(statusSection.transform, "RHRNote",
                "RHR: Mode 4-5 only\n(<350\u00b0F, <425 psig)", 8, new Color(0.4f, 0.4f, 0.5f),
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.35f));

            // --- Alarm Section ---
            GameObject alarmSection = CreatePanel(panelGO.transform, "AlarmSection",
                new Vector2(0.79f, 0.05f), new Vector2(0.99f, 0.95f), COLOR_GAUGE_BG);
            CreateTMPSectionLabel(alarmSection.transform, "ALARMS");

            GameObject alarmContainer = new GameObject("AlarmContainer");
            alarmContainer.transform.SetParent(alarmSection.transform, false);
            RectTransform acRect = alarmContainer.AddComponent<RectTransform>();
            acRect.anchorMin = new Vector2(0.02f, 0.02f);
            acRect.anchorMax = new Vector2(0.98f, 0.85f);
            acRect.offsetMin = Vector2.zero;
            acRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup aVLG = alarmContainer.AddComponent<VerticalLayoutGroup>();
            aVLG.padding = new RectOffset(2, 2, 2, 2);
            aVLG.spacing = 1f;
            aVLG.childAlignment = TextAnchor.UpperLeft;
            aVLG.childControlWidth = true;
            aVLG.childControlHeight = false;
            aVLG.childForceExpandWidth = true;
            aVLG.childForceExpandHeight = false;

            so.FindProperty("alarmContainer").objectReferenceValue = alarmContainer.transform;
            so.ApplyModifiedProperties();
        }

        #endregion

        // ====================================================================
        // SHARED HELPER METHODS — Panel Creation
        // ====================================================================

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

        #endif
    }
}
