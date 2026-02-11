// ============================================================================
// CRITICAL: Master the Atom - Reactor Operator GUI
// OperatorScreenBuilder.cs - Editor Tool for Scene Setup
// ============================================================================
//
// PURPOSE:
//   Creates the complete Reactor Operator GUI hierarchy in a Unity scene.
//   Menu: Critical > Create Operator Screen
//
// CREATES:
//   - Canvas with 1920x1080 reference resolution
//   - ReactorOperatorScreen master panel
//   - Left gauge panel (9 nuclear gauges)
//   - Core map panel (193-cell mosaic + buttons)
//   - Right gauge panel (8 thermal-hydraulic gauges)
//   - Detail panel (assembly info)
//   - Bottom panel (controls, rod display, alarms)
//   - All references wired automatically
//
// USAGE:
//   1. Open or create a scene
//   2. Menu: Critical > Create Operator Screen
//   3. Press Play to test
//
// SOURCES:
//   - ReactorOperatorGUI_Design_v1_0_0_0.md
//   - Unity_Implementation_Manual_v1_0_0_0.md
//
// CHANGE: v4.0.0 — CreateRodControlSection() now builds full rod control UI:
//         bank selector (8 buttons), WITHDRAW/INSERT/STOP commands,
//         step position readout, motion status indicator via RodControlPanel.
// CHANGE: v4.1.0 — All Text→TextMeshProUGUI. Added BuildScreen1() public entry
//         point for use by MultiScreenBuilder (eliminates code duplication).
// CHANGE: v4.2.2 — Fixed RodDisplaySection anchor overlap (was spanning full
//         bottom panel height over AlarmStrip). Replaced text-list alarm panel
//         with annunciator tile grid matching Heatup Visual standard.
//
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
    /// Editor tool for creating the Reactor Operator Screen UI hierarchy.
    /// </summary>
    public class OperatorScreenBuilder : MonoBehaviour
    {
        #if UNITY_EDITOR

        // ====================================================================
        // COLOR PALETTE (from Design Doc)
        // ====================================================================

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

        // ====================================================================
        // MENU ITEM
        // ====================================================================

        [MenuItem("Critical/Create Operator Screen")]
        public static void CreateOperatorScreen()
        {
            Debug.Log("[OperatorScreenBuilder] Creating Reactor Operator Screen...");

            // Create or find Canvas
            Canvas canvas = FindOrCreateCanvas();

            // Create EventSystem if needed
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
            }

            // Build Screen 1 under the canvas
            ReactorOperatorScreen screen = BuildScreen1(canvas.transform);
            GameObject screenGO = screen.gameObject;

            // v4.0.0: Add panel skin component
            ReactorOperatorScreenSkin skin = screenGO.AddComponent<ReactorOperatorScreenSkin>();
            
            // Collect panel backgrounds that should become transparent when skin is active
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

            // Select the created screen
            Selection.activeGameObject = screenGO;

            Debug.Log("[OperatorScreenBuilder] Reactor Operator Screen created successfully!");
            Debug.Log("  - Press Play to test");
            Debug.Log("  - Press '1' to toggle screen visibility");
            Debug.Log("  - Panel skin: Place texture in Assets/Resources/ReactorOperatorPanel/panel_base_color.png for auto-loading");
            Debug.Log("    OR assign texture manually to ScreenSkin.PanelTexture in Inspector");
        }

        /// <summary>
        /// Builds Screen 1 (Reactor Operator) under the given parent transform.
        /// Called by both the standalone menu item and MultiScreenBuilder.
        /// Returns the configured ReactorOperatorScreen component.
        /// </summary>
        public static ReactorOperatorScreen BuildScreen1(Transform parent)
        {
            // Create master screen panel
            GameObject screenGO = CreateScreenPanel(parent);
            ReactorOperatorScreen screen = screenGO.GetComponent<ReactorOperatorScreen>();

            // Create MosaicBoard on the screen
            MosaicBoard board = screenGO.AddComponent<MosaicBoard>();
            screen.Board = board;

            // Add MosaicBoardSetup for runtime initialization
            screenGO.AddComponent<MosaicBoardSetup>();

            // Create layout panels
            screen.LeftGaugePanel = CreateLeftGaugePanel(screenGO.transform, screen);
            screen.CoreMapPanel = CreateCoreMapPanel(screenGO.transform, screen);
            screen.RightGaugePanel = CreateRightGaugePanel(screenGO.transform, screen);
            screen.DetailPanelArea = CreateDetailPanel(screenGO.transform, screen);
            screen.BottomPanel = CreateBottomPanel(screenGO.transform, screen, board);

            // Create tooltip (shared by core map)
            CreateTooltip(screenGO.transform, screen.CoreMap);

            // Wire up remaining references
            if (screen.CoreMap != null)
            {
                screen.CoreMap.Board = board;
                screen.CoreMap.DetailPanel = screen.DetailPanel;
            }

            // Configure gauges
            screen.ConfigureGauges();

            return screen;
        }

        // ====================================================================
        // CANVAS SETUP
        // ====================================================================

        private static Canvas FindOrCreateCanvas()
        {
            // Look for existing canvas named ReactorOperatorCanvas
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (var c in canvases)
            {
                if (c.gameObject.name == "ReactorOperatorCanvas")
                {
                    return c;
                }
            }

            // Create new canvas
            GameObject canvasGO = new GameObject("ReactorOperatorCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        // ====================================================================
        // SCREEN PANEL
        // ====================================================================

        private static GameObject CreateScreenPanel(Transform parent)
        {
            GameObject screenGO = new GameObject("ReactorOperatorScreen");
            screenGO.transform.SetParent(parent, false);

            RectTransform rect = screenGO.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = screenGO.AddComponent<Image>();
            bg.color = COLOR_BACKGROUND;

            ReactorOperatorScreen screen = screenGO.AddComponent<ReactorOperatorScreen>();
            screen.BackgroundColor = COLOR_BACKGROUND;
            screen.PanelColor = COLOR_PANEL;
            screen.BorderColor = COLOR_BORDER;

            return screenGO;
        }

        // ====================================================================
        // LEFT GAUGE PANEL (9 Nuclear Gauges)
        // ====================================================================

        private static RectTransform CreateLeftGaugePanel(Transform parent, ReactorOperatorScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "LeftGaugePanel",
                new Vector2(0f, 0.26f), new Vector2(0.15f, 1f), COLOR_PANEL);

            RectTransform rect = panelGO.GetComponent<RectTransform>();

            // Add vertical layout
            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            // Create 9 gauges
            screen.NeutronPowerGauge = CreateGauge(panelGO.transform, "NeutronPowerGauge", GaugeType.NeutronPower);
            screen.ThermalPowerGauge = CreateGauge(panelGO.transform, "ThermalPowerGauge", GaugeType.ThermalPower);
            screen.StartupRateGauge = CreateGauge(panelGO.transform, "StartupRateGauge", GaugeType.StartupRate);
            screen.PeriodGauge = CreateGauge(panelGO.transform, "PeriodGauge", GaugeType.ReactorPeriod);
            screen.ReactivityGauge = CreateGauge(panelGO.transform, "ReactivityGauge", GaugeType.TotalReactivity);
            screen.KeffGauge = CreateGauge(panelGO.transform, "KeffGauge", GaugeType.NeutronPower); // Placeholder
            screen.BoronGauge = CreateGauge(panelGO.transform, "BoronGauge", GaugeType.Boron);
            screen.XenonGauge = CreateGauge(panelGO.transform, "XenonGauge", GaugeType.Xenon);
            screen.FlowGauge = CreateGauge(panelGO.transform, "FlowGauge", GaugeType.FlowFraction);

            return rect;
        }

        // ====================================================================
        // CORE MAP PANEL
        // ====================================================================

        private static RectTransform CreateCoreMapPanel(Transform parent, ReactorOperatorScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "CoreMapPanel",
                new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f), COLOR_PANEL);

            RectTransform rect = panelGO.GetComponent<RectTransform>();

            // Create display mode buttons at top
            CreateDisplayModeButtons(panelGO.transform, screen);

            // Create core map container
            GameObject mapContainer = new GameObject("MapContainer");
            mapContainer.transform.SetParent(panelGO.transform, false);

            RectTransform mapRect = mapContainer.AddComponent<RectTransform>();
            mapRect.anchorMin = new Vector2(0.05f, 0.12f);
            mapRect.anchorMax = new Vector2(0.95f, 0.88f);
            mapRect.offsetMin = Vector2.zero;
            mapRect.offsetMax = Vector2.zero;

            Image mapBg = mapContainer.AddComponent<Image>();
            mapBg.color = COLOR_MAP_BG;

            // Add CoreMosaicMap component
            CoreMosaicMap coreMap = mapContainer.AddComponent<CoreMosaicMap>();
            screen.CoreMap = coreMap;

            // Create bank filter buttons at bottom
            CreateBankFilterButtons(panelGO.transform, screen);

            return rect;
        }

        private static void CreateDisplayModeButtons(Transform parent, ReactorOperatorScreen screen)
        {
            GameObject buttonRow = new GameObject("DisplayModeButtons");
            buttonRow.transform.SetParent(parent, false);

            RectTransform rowRect = buttonRow.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.05f, 0.90f);
            rowRect.anchorMax = new Vector2(0.95f, 0.98f);
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup hlg = buttonRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            screen.PowerModeButton = CreateButton(buttonRow.transform, "PowerBtn", "POWER");
            screen.FuelTempModeButton = CreateButton(buttonRow.transform, "FuelTempBtn", "FUEL TEMP");
            screen.CoolantTempModeButton = CreateButton(buttonRow.transform, "CoolantBtn", "COOLANT");
            screen.RodBankModeButton = CreateButton(buttonRow.transform, "RodBankBtn", "ROD BANKS");
        }

        private static void CreateBankFilterButtons(Transform parent, ReactorOperatorScreen screen)
        {
            GameObject buttonRow = new GameObject("BankFilterButtons");
            buttonRow.transform.SetParent(parent, false);

            RectTransform rowRect = buttonRow.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.05f, 0.02f);
            rowRect.anchorMax = new Vector2(0.95f, 0.10f);
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup hlg = buttonRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            screen.BankAllButton = CreateButton(buttonRow.transform, "AllBtn", "ALL");
            screen.BankSAButton = CreateButton(buttonRow.transform, "SABtn", "SA");
            screen.BankSBButton = CreateButton(buttonRow.transform, "SBBtn", "SB");
            screen.BankSCButton = CreateButton(buttonRow.transform, "SCBtn", "SC");
            screen.BankSDButton = CreateButton(buttonRow.transform, "SDBtn", "SD");
            screen.BankDButton = CreateButton(buttonRow.transform, "DBtn", "D");
            screen.BankCButton = CreateButton(buttonRow.transform, "CBtn", "C");
            screen.BankBButton = CreateButton(buttonRow.transform, "BBtn", "B");
            screen.BankAButton = CreateButton(buttonRow.transform, "ABtn", "A");
        }

        // ====================================================================
        // RIGHT GAUGE PANEL (8 Thermal-Hydraulic Gauges)
        // ====================================================================

        private static RectTransform CreateRightGaugePanel(Transform parent, ReactorOperatorScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "RightGaugePanel",
                new Vector2(0.65f, 0.26f), new Vector2(0.80f, 1f), COLOR_PANEL);

            RectTransform rect = panelGO.GetComponent<RectTransform>();

            // Add vertical layout
            VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            // Create 8 gauges
            screen.TavgGauge = CreateGauge(panelGO.transform, "TavgGauge", GaugeType.Tavg);
            screen.ThotGauge = CreateGauge(panelGO.transform, "ThotGauge", GaugeType.Thot);
            screen.TcoldGauge = CreateGauge(panelGO.transform, "TcoldGauge", GaugeType.Tcold);
            screen.DeltaTGauge = CreateGauge(panelGO.transform, "DeltaTGauge", GaugeType.DeltaT);
            screen.FuelCenterlineGauge = CreateGauge(panelGO.transform, "FuelCenterlineGauge", GaugeType.FuelCenterline);
            screen.HotChannelGauge = CreateGauge(panelGO.transform, "HotChannelGauge", GaugeType.FuelCenterline); // Placeholder
            screen.PressureGauge = CreateGauge(panelGO.transform, "PressureGauge", GaugeType.NeutronPower); // Placeholder - needs PressureGaugeType
            screen.PZRLevelGauge = CreateGauge(panelGO.transform, "PZRLevelGauge", GaugeType.NeutronPower); // Placeholder

            return rect;
        }

        // ====================================================================
        // DETAIL PANEL
        // ====================================================================

        private static RectTransform CreateDetailPanel(Transform parent, ReactorOperatorScreen screen)
        {
            // Use the static creation helper from AssemblyDetailPanel
            AssemblyDetailPanel detailPanel = AssemblyDetailPanel.CreateDetailPanel(parent);
            screen.DetailPanel = detailPanel;

            return detailPanel.GetComponent<RectTransform>();
        }

        // ====================================================================
        // BOTTOM PANEL
        // ====================================================================

        private static RectTransform CreateBottomPanel(Transform parent, ReactorOperatorScreen screen, MosaicBoard board)
        {
            GameObject panelGO = CreatePanel(parent, "BottomPanel",
                new Vector2(0f, 0f), new Vector2(1f, 0.26f), COLOR_PANEL);

            RectTransform rect = panelGO.GetComponent<RectTransform>();

            // Create sub-sections
            CreateRodControlSection(panelGO.transform, screen);
            CreateRodDisplaySection(panelGO.transform, screen);
            CreateBoronControlSection(panelGO.transform, screen);
            CreateTripControlSection(panelGO.transform, screen);
            CreateTimeControlSection(panelGO.transform, screen);
            CreateAlarmStripSection(panelGO.transform, screen, board);

            return rect;
        }

        private static void CreateRodControlSection(Transform parent, ReactorOperatorScreen screen)
        {
            GameObject section = CreatePanel(parent, "RodControlSection",
                new Vector2(0.01f, 0.55f), new Vector2(0.15f, 0.95f), COLOR_GAUGE_BG);

            // Add label
            CreateSectionLabel(section.transform, "ROD CONTROL");

            // Add MosaicControlPanel (legacy — retains trip/boron/time wiring)
            GameObject controlGO = new GameObject("RodControls");
            controlGO.transform.SetParent(section.transform, false);

            RectTransform controlRect = controlGO.AddComponent<RectTransform>();
            controlRect.anchorMin = new Vector2(0.05f, 0.05f);
            controlRect.anchorMax = new Vector2(0.95f, 0.85f);
            controlRect.offsetMin = Vector2.zero;
            controlRect.offsetMax = Vector2.zero;

            MosaicControlPanel controlPanel = controlGO.AddComponent<MosaicControlPanel>();
            screen.ControlPanel = controlPanel;

            // ============================================================
            // v4.0.0: Add RodControlPanel with actual UI elements
            // ============================================================

            // Rod control panel component
            RodControlPanel rodPanel = controlGO.AddComponent<RodControlPanel>();

            // --- Bank Selector Row (top area) ---
            GameObject bankRow = new GameObject("BankSelectorRow");
            bankRow.transform.SetParent(controlGO.transform, false);

            RectTransform bankRowRect = bankRow.AddComponent<RectTransform>();
            bankRowRect.anchorMin = new Vector2(0f, 0.72f);
            bankRowRect.anchorMax = new Vector2(1f, 0.95f);
            bankRowRect.offsetMin = Vector2.zero;
            bankRowRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup bankHlg = bankRow.AddComponent<HorizontalLayoutGroup>();
            bankHlg.spacing = 2f;
            bankHlg.childAlignment = TextAnchor.MiddleCenter;
            bankHlg.childControlWidth = true;
            bankHlg.childControlHeight = true;
            bankHlg.childForceExpandWidth = true;
            bankHlg.childForceExpandHeight = true;
            bankHlg.padding = new RectOffset(1, 1, 0, 0);

            string[] bankNames = { "SA", "SB", "SC", "SD", "D", "C", "B", "A" };
            rodPanel.BankSelectorButtons = new Button[8];

            for (int i = 0; i < 8; i++)
            {
                Button bankBtn = CreateButton(bankRow.transform, $"Bank{bankNames[i]}Btn", bankNames[i]);
                // Make text smaller to fit
                TextMeshProUGUI btnLabel = bankBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (btnLabel != null) btnLabel.fontSize = 8;
                rodPanel.BankSelectorButtons[i] = bankBtn;
            }

            // --- Selected Bank + Step Position (middle area) ---
            GameObject infoRow = new GameObject("InfoRow");
            infoRow.transform.SetParent(controlGO.transform, false);

            RectTransform infoRect = infoRow.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0f, 0.42f);
            infoRect.anchorMax = new Vector2(1f, 0.70f);
            infoRect.offsetMin = Vector2.zero;
            infoRect.offsetMax = Vector2.zero;

            // Selected bank name (left half)
            GameObject bankNameGO = new GameObject("SelectedBankName");
            bankNameGO.transform.SetParent(infoRow.transform, false);

            RectTransform bankNameRect = bankNameGO.AddComponent<RectTransform>();
            bankNameRect.anchorMin = new Vector2(0.02f, 0.1f);
            bankNameRect.anchorMax = new Vector2(0.35f, 0.9f);
            bankNameRect.offsetMin = Vector2.zero;
            bankNameRect.offsetMax = Vector2.zero;

            Image bankNameBg = bankNameGO.AddComponent<Image>();
            bankNameBg.color = new Color(0.05f, 0.05f, 0.08f);

            GameObject bankNameTextGO = new GameObject("Text");
            bankNameTextGO.transform.SetParent(bankNameGO.transform, false);
            RectTransform bankNameTextRect = bankNameTextGO.AddComponent<RectTransform>();
            bankNameTextRect.anchorMin = Vector2.zero;
            bankNameTextRect.anchorMax = Vector2.one;
            bankNameTextRect.offsetMin = new Vector2(2f, 0f);
            bankNameTextRect.offsetMax = new Vector2(-2f, 0f);

            TextMeshProUGUI selectedBankText = bankNameTextGO.AddComponent<TextMeshProUGUI>();
            selectedBankText.text = "D";
            TMP_FontAsset instrFont = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/Electronic Highway Sign SDF");
            if (instrFont != null) selectedBankText.font = instrFont;
            selectedBankText.fontSize = 18;
            selectedBankText.fontStyle = FontStyles.Bold;
            selectedBankText.alignment = TextAlignmentOptions.Center;
            selectedBankText.color = COLOR_CYAN;
            selectedBankText.enableWordWrapping = false;
            selectedBankText.raycastTarget = false;
            Material cyanMat = Resources.Load<Material>("Fonts & Materials/Instrument_Cyan");
            if (cyanMat != null) selectedBankText.fontSharedMaterial = cyanMat;
            rodPanel.SelectedBankText = selectedBankText;

            // Step position readout (middle)
            rodPanel.StepPositionText = CreateDigitalReadout(infoRow.transform, "StepPosition", "228",
                new Vector2(0.38f, 0.1f), new Vector2(0.68f, 0.9f));

            // "STEPS" label (right)
            GameObject stepsLabelGO = new GameObject("StepsLabel");
            stepsLabelGO.transform.SetParent(infoRow.transform, false);

            RectTransform stepsLabelRect = stepsLabelGO.AddComponent<RectTransform>();
            stepsLabelRect.anchorMin = new Vector2(0.70f, 0.1f);
            stepsLabelRect.anchorMax = new Vector2(0.98f, 0.9f);
            stepsLabelRect.offsetMin = Vector2.zero;
            stepsLabelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI stepsLabel = stepsLabelGO.AddComponent<TextMeshProUGUI>();
            stepsLabel.text = "STEPS";
            TMP_FontAsset lblFont = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/LiberationSans SDF");
            if (lblFont != null) stepsLabel.font = lblFont;
            stepsLabel.fontSize = 9;
            stepsLabel.alignment = TextAlignmentOptions.Left;
            stepsLabel.color = COLOR_TEXT_LABEL;
            stepsLabel.enableWordWrapping = false;
            stepsLabel.raycastTarget = false;
            Material lblMat = Resources.Load<Material>("Fonts & Materials/Label_Standard");
            if (lblMat != null) stepsLabel.fontSharedMaterial = lblMat;

            // --- Motion Status ---
            GameObject statusGO = new GameObject("MotionStatus");
            statusGO.transform.SetParent(controlGO.transform, false);

            RectTransform statusRect = statusGO.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 0.30f);
            statusRect.anchorMax = new Vector2(1f, 0.42f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            TextMeshProUGUI motionStatusText = statusGO.AddComponent<TextMeshProUGUI>();
            motionStatusText.text = "STOPPED";
            TMP_FontAsset statusFont = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/Electronic Highway Sign SDF");
            if (statusFont != null) motionStatusText.font = statusFont;
            motionStatusText.fontSize = 10;
            motionStatusText.fontStyle = FontStyles.Bold;
            motionStatusText.alignment = TextAlignmentOptions.Center;
            motionStatusText.color = COLOR_TEXT_LABEL;
            motionStatusText.enableWordWrapping = false;
            motionStatusText.raycastTarget = false;
            rodPanel.MotionStatusText = motionStatusText;

            // --- Command Buttons Row (bottom area) ---
            GameObject cmdRow = new GameObject("CommandRow");
            cmdRow.transform.SetParent(controlGO.transform, false);

            RectTransform cmdRowRect = cmdRow.AddComponent<RectTransform>();
            cmdRowRect.anchorMin = new Vector2(0f, 0.02f);
            cmdRowRect.anchorMax = new Vector2(1f, 0.28f);
            cmdRowRect.offsetMin = Vector2.zero;
            cmdRowRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup cmdHlg = cmdRow.AddComponent<HorizontalLayoutGroup>();
            cmdHlg.spacing = 3f;
            cmdHlg.childAlignment = TextAnchor.MiddleCenter;
            cmdHlg.childControlWidth = true;
            cmdHlg.childControlHeight = true;
            cmdHlg.childForceExpandWidth = true;
            cmdHlg.childForceExpandHeight = true;
            cmdHlg.padding = new RectOffset(2, 2, 2, 2);

            // WITHDRAW button
            Button withdrawBtn = CreateButton(cmdRow.transform, "WithdrawBtn", "WITH\nDRAW");
            withdrawBtn.GetComponent<Image>().color = new Color(0.1f, 0.25f, 0.1f);
            TextMeshProUGUI withdrawLabel = withdrawBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (withdrawLabel != null) withdrawLabel.fontSize = 8;
            rodPanel.WithdrawButton = withdrawBtn;

            // STOP button
            Button stopBtn = CreateButton(cmdRow.transform, "StopBtn", "STOP");
            stopBtn.GetComponent<Image>().color = new Color(0.35f, 0.08f, 0.08f);
            TextMeshProUGUI stopLabel = stopBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (stopLabel != null) { stopLabel.fontSize = 9; stopLabel.fontStyle = FontStyles.Bold; }
            rodPanel.StopButton = stopBtn;

            // INSERT button
            Button insertBtn = CreateButton(cmdRow.transform, "InsertBtn", "IN\nSERT");
            insertBtn.GetComponent<Image>().color = new Color(0.3f, 0.25f, 0.05f);
            TextMeshProUGUI insertLabel = insertBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (insertLabel != null) insertLabel.fontSize = 8;
            rodPanel.InsertButton = insertBtn;

            // Wire the MosaicControlPanel rod buttons to same commands
            controlPanel.WithdrawButton = withdrawBtn;
            controlPanel.InsertButton = insertBtn;
            controlPanel.StopButton = stopBtn;
        }

        private static void CreateRodDisplaySection(Transform parent, ReactorOperatorScreen screen)
        {
            // v4.2.2: Fixed anchor — was (0.16, 0.05)→(0.45, 0.95) spanning full height,
            // which overlapped the AlarmStrip below. Now constrained to upper row only.
            GameObject section = CreatePanel(parent, "RodDisplaySection",
                new Vector2(0.16f, 0.55f), new Vector2(0.45f, 0.95f), COLOR_GAUGE_BG);

            // Add label
            CreateSectionLabel(section.transform, "BANK POSITIONS");

            // Add rod display
            GameObject rodGO = new GameObject("RodDisplay");
            rodGO.transform.SetParent(section.transform, false);

            RectTransform rodRect = rodGO.AddComponent<RectTransform>();
            rodRect.anchorMin = new Vector2(0.02f, 0.05f);
            rodRect.anchorMax = new Vector2(0.98f, 0.85f);
            rodRect.offsetMin = Vector2.zero;
            rodRect.offsetMax = Vector2.zero;

            MosaicRodDisplay rodDisplay = rodGO.AddComponent<MosaicRodDisplay>();
            screen.RodDisplay = rodDisplay;
        }

        private static void CreateBoronControlSection(Transform parent, ReactorOperatorScreen screen)
        {
            GameObject section = CreatePanel(parent, "BoronControlSection",
                new Vector2(0.46f, 0.55f), new Vector2(0.60f, 0.95f), COLOR_GAUGE_BG);

            CreateSectionLabel(section.transform, "BORON CONTROL");

            // Borate button
            CreateButton(section.transform, "BorateBtn", "BORATE",
                new Vector2(0.1f, 0.45f), new Vector2(0.45f, 0.75f));

            // Dilute button
            CreateButton(section.transform, "DiluteBtn", "DILUTE",
                new Vector2(0.55f, 0.45f), new Vector2(0.9f, 0.75f));

            // Boron readout
            CreateDigitalReadout(section.transform, "BoronReadout", "1500 ppm",
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.4f));
        }

        private static void CreateTripControlSection(Transform parent, ReactorOperatorScreen screen)
        {
            GameObject section = CreatePanel(parent, "TripControlSection",
                new Vector2(0.61f, 0.55f), new Vector2(0.75f, 0.95f), COLOR_GAUGE_BG);

            CreateSectionLabel(section.transform, "TRIP CONTROL");

            // Trip button (big red)
            Button tripBtn = CreateButton(section.transform, "TripBtn", "TRIP",
                new Vector2(0.15f, 0.3f), new Vector2(0.55f, 0.75f));
            tripBtn.GetComponent<Image>().color = new Color(0.5f, 0.1f, 0.1f);

            // Reset button
            CreateButton(section.transform, "ResetBtn", "RESET",
                new Vector2(0.6f, 0.3f), new Vector2(0.9f, 0.55f));

            // Status indicator
            CreateIndicator(section.transform, "TripIndicator",
                new Vector2(0.6f, 0.6f), new Vector2(0.9f, 0.75f));
        }

        private static void CreateTimeControlSection(Transform parent, ReactorOperatorScreen screen)
        {
            GameObject section = CreatePanel(parent, "TimeControlSection",
                new Vector2(0.76f, 0.55f), new Vector2(0.99f, 0.95f), COLOR_GAUGE_BG);

            CreateSectionLabel(section.transform, "TIME CONTROL");

            // Sim time display
            screen.SimTimeText = CreateDigitalReadout(section.transform, "SimTimeDisplay", "00:00:00",
                new Vector2(0.05f, 0.55f), new Vector2(0.55f, 0.8f));

            // Time compression display
            screen.TimeCompressionText = CreateDigitalReadout(section.transform, "TimeCompDisplay", "1x",
                new Vector2(0.6f, 0.55f), new Vector2(0.95f, 0.8f));

            // Pause button
            CreateButton(section.transform, "PauseBtn", "PAUSE",
                new Vector2(0.05f, 0.15f), new Vector2(0.45f, 0.45f));

            // Speed buttons
            CreateButton(section.transform, "Speed1xBtn", "1x",
                new Vector2(0.5f, 0.25f), new Vector2(0.65f, 0.45f));
            CreateButton(section.transform, "Speed10xBtn", "10x",
                new Vector2(0.67f, 0.25f), new Vector2(0.82f, 0.45f));
            CreateButton(section.transform, "Speed100xBtn", "100x",
                new Vector2(0.84f, 0.25f), new Vector2(0.99f, 0.45f));
        }

        // v4.2.2: Rebuilt as annunciator tile grid (replaces text-list alarm panel)
        private static void CreateAlarmStripSection(Transform parent, ReactorOperatorScreen screen, MosaicBoard board)
        {
            GameObject section = CreatePanel(parent, "AlarmStrip",
                new Vector2(0.01f, 0.05f), new Vector2(0.99f, 0.50f), COLOR_GAUGE_BG);

            CreateSectionLabel(section.transform, "ANNUNCIATOR PANEL");

            // Annunciator tile grid container
            GameObject alarmGO = new GameObject("AnnunciatorPanel");
            alarmGO.transform.SetParent(section.transform, false);

            RectTransform alarmRect = alarmGO.AddComponent<RectTransform>();
            alarmRect.anchorMin = new Vector2(0.01f, 0.02f);
            alarmRect.anchorMax = new Vector2(0.99f, 0.83f);
            alarmRect.offsetMin = Vector2.zero;
            alarmRect.offsetMax = Vector2.zero;

            // MosaicAlarmPanel builds its own tile grid dynamically on Initialize()
            MosaicAlarmPanel alarmPanel = alarmGO.AddComponent<MosaicAlarmPanel>();
            alarmPanel.GridColumns = 8;
            alarmPanel.TileSpacing = 3f;
            alarmPanel.TileContainer = alarmRect;  // Use self as container
            screen.AlarmPanel = alarmPanel;
            board.AlarmSection = section.GetComponent<RectTransform>();
        }

        // ====================================================================
        // TOOLTIP
        // ====================================================================

        private static void CreateTooltip(Transform parent, CoreMosaicMap coreMap)
        {
            GameObject tooltipGO = new GameObject("Tooltip");
            tooltipGO.transform.SetParent(parent, false);

            RectTransform rect = tooltipGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180f, 120f);
            rect.pivot = new Vector2(0f, 1f);

            Image bg = tooltipGO.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Tooltip text
            GameObject textGO = new GameObject("TooltipText");
            textGO.transform.SetParent(tooltipGO.transform, false);

            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 8f);
            textRect.offsetMax = new Vector2(-8f, -8f);

            // v4.1.0: TMP tooltip text
            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset tooltipFont = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/LiberationSans SDF");
            if (tooltipFont != null) text.font = tooltipFont;
            text.fontSize = 11;
            text.color = COLOR_TEXT_NORMAL;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = true;
            text.raycastTarget = false;

            // Wire to core map
            if (coreMap != null)
            {
                coreMap.TooltipPanel = tooltipGO;
                coreMap.TooltipText = text;
            }

            tooltipGO.SetActive(false);
        }

        // ====================================================================
        // HELPER METHODS
        // ====================================================================

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

        private static void CreateSectionLabel(Transform parent, string text)
        {
            GameObject labelGO = new GameObject("SectionLabel");
            labelGO.transform.SetParent(parent, false);

            RectTransform rect = labelGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.85f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // v4.1.0: TMP with section label material
            TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = text;
            TMP_FontAsset sectionFont = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/LiberationSans SDF");
            if (sectionFont != null) label.font = sectionFont;
            label.fontSize = 11;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = COLOR_TEXT_LABEL;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            Material sectionMat = Resources.Load<Material>("Fonts & Materials/Label_Section");
            if (sectionMat != null) label.fontSharedMaterial = sectionMat;
        }

        private static MosaicGauge CreateGauge(Transform parent, string name, GaugeType type)
        {
            GameObject gaugeGO = new GameObject(name);
            gaugeGO.transform.SetParent(parent, false);

            RectTransform rect = gaugeGO.AddComponent<RectTransform>();

            // v4.1.0: Use instrument sprite background instead of flat color
            Image bg = gaugeGO.AddComponent<Image>();
            Sprite gaugeBgSprite = Resources.Load<Sprite>("Sprites/gauge_bg");
            if (gaugeBgSprite != null)
            {
                bg.sprite = gaugeBgSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white; // Sprite provides its own colors
            }
            else
            {
                bg.color = COLOR_GAUGE_BG; // Fallback
            }

            MosaicGauge gauge = gaugeGO.AddComponent<MosaicGauge>();
            gauge.Type = type;
            gauge.ShowAnalog = false;
            gauge.ShowDigital = true;
            gauge.Background = bg;

            // v4.1.0: Fill bar indicator (behind value text, shows normalized position)
            GameObject fillGO = new GameObject("FillBar");
            fillGO.transform.SetParent(gaugeGO.transform, false);

            RectTransform fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0.35f);
            fillRect.anchorMax = new Vector2(0f, 0.82f); // Starts at 0 width, grows right
            fillRect.offsetMin = new Vector2(3f, 0f);
            fillRect.offsetMax = new Vector2(0f, 0f);

            Image fillImage = fillGO.AddComponent<Image>();
            Sprite fillSprite = Resources.Load<Sprite>("Sprites/fill_bar");
            if (fillSprite != null)
            {
                fillImage.sprite = fillSprite;
                fillImage.color = new Color(0f, 1f, 0.533f, 0.25f); // Green, low alpha
            }
            else
            {
                fillImage.color = new Color(0f, 1f, 0.533f, 0.15f);
            }
            fillImage.raycastTarget = false;
            gauge.FillBarIndicator = fillImage;

            // v4.1.0: Glow image (soft radial glow behind value text)
            GameObject glowGO = new GameObject("Glow");
            glowGO.transform.SetParent(gaugeGO.transform, false);

            RectTransform glowRect = glowGO.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.1f, 0.25f);
            glowRect.anchorMax = new Vector2(0.9f, 0.95f);
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;

            Image glowImage = glowGO.AddComponent<Image>();
            Sprite glowSprite = Resources.Load<Sprite>("Sprites/glow_soft");
            if (glowSprite != null)
            {
                glowImage.sprite = glowSprite;
            }
            glowImage.color = new Color(0f, 1f, 0.533f, 0.12f); // Subtle green glow
            glowImage.raycastTarget = false;
            gauge.GlowImage = glowImage;

            // v4.1.0: Value text — TMP with Electronic Highway Sign font
            GameObject valueGO = new GameObject("Value");
            valueGO.transform.SetParent(gaugeGO.transform, false);

            RectTransform valueRect = valueGO.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0f, 0.35f);
            valueRect.anchorMax = new Vector2(1f, 0.85f);
            valueRect.offsetMin = new Vector2(4f, 0f);
            valueRect.offsetMax = new Vector2(-4f, 0f);

            TextMeshProUGUI valueText = valueGO.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset instrumentFont = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/Electronic Highway Sign SDF");
            if (instrumentFont != null)
            {
                valueText.font = instrumentFont;
            }
            valueText.fontSize = 18;
            valueText.fontStyle = FontStyles.Bold;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = COLOR_GREEN;
            valueText.enableWordWrapping = false;
            valueText.overflowMode = TextOverflowModes.Truncate;
            valueText.raycastTarget = false;
            gauge.ValueText = valueText;

            // v4.1.0: Label text — TMP with LiberationSans font
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(gaugeGO.transform, false);

            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0.35f);
            labelRect.offsetMin = new Vector2(2f, 0f);
            labelRect.offsetMax = new Vector2(-2f, 0f);

            TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset labelFont = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/LiberationSans SDF");
            if (labelFont != null)
            {
                labelText.font = labelFont;
            }
            labelText.fontSize = 9;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = COLOR_TEXT_LABEL;
            labelText.enableWordWrapping = false;
            labelText.overflowMode = TextOverflowModes.Truncate;
            labelText.raycastTarget = false;

            // Apply label material with subtle shadow
            Material labelMat = Resources.Load<Material>("Fonts & Materials/Label_Standard");
            if (labelMat != null)
            {
                labelText.fontSharedMaterial = labelMat;
            }
            gauge.LabelText = labelText;

            return gauge;
        }

        private static Button CreateButton(Transform parent, string name, string text)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);

            RectTransform rect = btnGO.AddComponent<RectTransform>();

            // v4.1.0: Button sprite background with bevel
            Image bg = btnGO.AddComponent<Image>();
            Sprite btnSprite = Resources.Load<Sprite>("Sprites/button_bg");
            if (btnSprite != null)
            {
                bg.sprite = btnSprite;
                bg.type = Image.Type.Sliced;
            }
            bg.color = COLOR_BUTTON;

            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock colors = btn.colors;
            colors.normalColor = COLOR_BUTTON;
            colors.highlightedColor = COLOR_BUTTON_HOVER;
            colors.pressedColor = COLOR_CYAN;
            btn.colors = colors;

            // v4.1.0: TMP label with LiberationSans
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(btnGO.transform, false);

            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = text;
            TMP_FontAsset btnFont = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/LiberationSans SDF");
            if (btnFont != null) label.font = btnFont;
            label.fontSize = 10;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = COLOR_TEXT_NORMAL;
            label.enableWordWrapping = false;
            label.raycastTarget = false;

            return btn;
        }

        private static Button CreateButton(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            Button btn = CreateButton(parent, name, text);
            RectTransform rect = btn.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return btn;
        }

        private static TextMeshProUGUI CreateDigitalReadout(Transform parent, string name, string defaultText,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject readoutGO = new GameObject(name);
            readoutGO.transform.SetParent(parent, false);

            RectTransform rect = readoutGO.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // v4.1.0: Recessed readout background sprite
            Image bg = readoutGO.AddComponent<Image>();
            Sprite readoutSprite = Resources.Load<Sprite>("Sprites/readout_bg");
            if (readoutSprite != null)
            {
                bg.sprite = readoutSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.05f, 0.05f, 0.08f);
            }

            // v4.1.0: TMP text with instrument font
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(readoutGO.transform, false);

            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 2f);
            textRect.offsetMax = new Vector2(-4f, -2f);

            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = defaultText;
            TMP_FontAsset instrumentFont = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/Electronic Highway Sign SDF");
            if (instrumentFont != null)
            {
                text.font = instrumentFont;
            }
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = COLOR_GREEN;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;

            // Apply green glow material
            Material greenMat = Resources.Load<Material>("Fonts & Materials/Instrument_Green_Glow");
            if (greenMat != null)
            {
                text.fontSharedMaterial = greenMat;
            }

            return text;
        }

        private static void CreateIndicator(Transform parent, string name,
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

        #endif
    }
}
