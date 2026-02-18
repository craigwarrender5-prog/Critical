// ============================================================================
// CRITICAL: Master the Atom - Multi-Screen Builder
// MultiScreenBuilder.cs - Editor Tool for Creating All Operator Screen UIs
// ============================================================================
//
// File: Assets/Scripts/UI/MultiScreenBuilder.cs
// Module: Critical.UI.MultiScreenBuilder
// Responsibility: Editor-time construction of unified operator screen hierarchy.
// Standards: GOLD v1.0, SRP/SOLID
// Version: 2.1
// Last Updated: 2026-02-17
// Changes:
//   - 2.1 (2026-02-17): Added GOLD metadata fields and bounded file-level change ledger.
//   - 2.0 (2026-02-10): Added unified all-screen builder flow and shared canvas wiring.
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
//   - Screen 1 (Reactor Core) â€” GOLD STANDARD ReactorOperatorScreen
//   - Screen 2 (RCS Primary Loop) â€” RCSPrimaryLoopScreen
//   - Screen Tab (Plant Overview) â€” PlantOverviewScreen
//   - Screen 3 (Pressurizer) â€” PressurizerScreen
//   - Screen 4 (CVCS) â€” CVCSScreen
//   - Screen 5 (Steam Generators) â€” SteamGeneratorScreen
//   - Screen 6 (Turbine-Generator) â€” TurbineGeneratorScreen
//   - Screen 7 (Secondary Systems) â€” SecondarySystemsScreen
//   - Screen 8 (Auxiliary Systems) â€” AuxiliarySystemsScreen
//
// REPLACES:
//   - OperatorScreenBuilder.cs > "Critical > Create Operator Screen" (Screen 1 only)
//   - This builder creates ALL screens in one unified hierarchy.
//
// GOLD STANDARD:
//   ReactorOperatorScreen.cs is NOT modified. The builder creates GameObjects
//   and wires its public fields â€” exactly as OperatorScreenBuilder does.
//
// USAGE:
//   1. Menu: Critical > Create All Operator Screens
//   2. ScreenInputActions asset is auto-wired to ScreenManager
//   3. (Optional) Assign RCS model prefab to Screen 2 in Inspector
//   4. Press Play â€” keys 1/2/Tab switch screens
//
// SOURCES:
//   - IMPLEMENTATION_PLAN_v2.0.0_MultiScreenGUI.md
//   - Operator_Screen_Layout_Plan_v1_0_0.md
//   - OperatorScreenBuilder.cs (Screen 1 pattern reference)
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
    public partial class MultiScreenBuilder : MonoBehaviour
    {
        #if UNITY_EDITOR

        // ====================================================================
        // COLOR PALETTE (from Design Doc â€” shared across all screens)
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

            // 5. Build Screen 1 â€” Reactor Core (GOLD STANDARD)
            CreateReactorCoreScreen(canvas.transform);

            // 6. Build Screen 2 â€” RCS Primary Loop
            CreateRCSScreen(canvas.transform);

            // 7. Build Screen Tab â€” Plant Overview
            CreatePlantOverviewScreen(canvas.transform);

            // 8. Build Screen 3 â€” Pressurizer
            CreatePressurizerScreen(canvas.transform);

            // 9. Build Screen 4 â€” CVCS
            CreateCVCSScreen(canvas.transform);

            // 10. Build Screen 5 â€” Steam Generators
            CreateSteamGeneratorScreen(canvas.transform);

            // 11. Build Screen 6 â€” Turbine-Generator
            CreateTurbineGeneratorScreen(canvas.transform);

            // 12. Build Screen 7 â€” Secondary Systems
            CreateSecondarySystemsScreen(canvas.transform);

            // 13. Build Screen 8 â€” Auxiliary Systems
            CreateAuxiliarySystemsScreen(canvas.transform);

            Debug.Log("[MultiScreenBuilder] ===== Multi-Screen build complete =====");
            Debug.Log("  IMPORTANT: Assign ScreenInputActions asset to ScreenManager in Inspector");
            Debug.Log("  Press Play, then press 1/2/Tab to switch screens");
        }

        // ====================================================================
        // INFRASTRUCTURE SETUP
        // ====================================================================

        // ####################################################################
        //
        //  SCREEN 1 â€” REACTOR CORE (GOLD STANDARD)
        //
        //  Ported from OperatorScreenBuilder.cs. Creates the complete
        //  ReactorOperatorScreen hierarchy with MosaicBoard, CoreMosaicMap,
        //  gauges, rod display, controls, and alarms. Attaches
        //  ReactorScreenAdapter for ScreenManager integration.
        //
        //  GOLD STANDARD: ReactorOperatorScreen.cs is NOT modified.
        //
        // ####################################################################

        // ####################################################################
        //
        //  SCREEN 2 â€” RCS PRIMARY LOOP
        //
        // ####################################################################

        // ####################################################################
        //
        //  SCREEN TAB â€” PLANT OVERVIEW
        //
        //  Provides a high-level plant-wide view with a simplified
        //  mimic diagram showing reactor, RCS loops, pressurizer,
        //  steam generators, turbine-generator, and condenser.
        //
        // ####################################################################

        // ####################################################################
        //
        //  SCREEN 3 â€” PRESSURIZER
        //
        //  Displays pressurizer pressure, level, heater status, spray,
        //  PORV/SV indicators, and a 2D vessel cutaway visualization.
        //
        // ####################################################################

        // ####################################################################
        //
        //  SCREEN 4 â€” CVCS (Chemical and Volume Control System)
        //
        //  Displays charging/letdown flows, VCT level, boron concentration,
        //  seal injection, and a 2D CVCS flow diagram.
        //
        // ####################################################################

        // ####################################################################
        //
        //  SCREEN 5 â€” STEAM GENERATORS
        //
        //  Quad-SG 2x2 layout with U-tube schematics, level indicators,
        //  primary inlet/outlet temps, secondary level and steam pressure.
        //
        // ####################################################################

        // ####################################################################
        //
        //  SCREEN 6 â€” TURBINE-GENERATOR
        //
        //  Shaft train diagram, HP/LP turbine gauges, generator output.
        //  Almost entirely PLACEHOLDER â€” no turbine model exists.
        //
        // ####################################################################

        // ####################################################################
        //
        //  SCREEN 7 â€” SECONDARY SYSTEMS
        //
        //  Secondary cycle flow diagram, feedwater train, steam dump,
        //  MSIVs, condensate/deaerator/FW heaters.
        //
        // ####################################################################

        // ####################################################################
        //
        //  SCREEN 8 â€” AUXILIARY SYSTEMS
        //
        //  RHR, CCW, Service Water overview.
        //  Entirely PLACEHOLDER â€” no auxiliary system models exist.
        //
        // ####################################################################

        // ====================================================================
        // SHARED HELPER METHODS â€” Panel Creation
        // ====================================================================

        // ====================================================================
        // SHARED HELPER METHODS â€” Legacy UI (for Screen 1 / MosaicGauge compat)
        // ====================================================================

        // ====================================================================
        // SHARED HELPER METHODS â€” Overview Gauge Items (for Plant Overview)
        // ====================================================================

        // ====================================================================
        // SHARED HELPER METHODS â€” Mimic Diagram Elements
        // ====================================================================

        // ====================================================================
        // SHARED HELPER METHODS â€” RCS Gauges (for Screen 2)
        // ====================================================================

        // ====================================================================
        // SHARED HELPER METHODS â€” TextMeshPro (for Screen 2+)
        // ====================================================================

        // ====================================================================
        // SHARED HELPER METHODS â€” TMP Buttons (for Screen 2+)
        // ====================================================================

        #endif
    }
}


