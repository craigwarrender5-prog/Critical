// CRITICAL: Master the Atom - Multi-Screen Builder
// MultiScreenBuilder.Screen5.cs
//
// File: Assets/Scripts/UI/MultiScreenBuilder.Screen5.cs
// Module: Critical.UI.MultiScreenBuilder
// Responsibility: Screen 5 - Steam Generators construction logic.
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

        #region Screen 5 - Steam Generators

        private static void CreateSteamGeneratorScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 5 â€” Steam Generators...");

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
            Debug.Log("[MultiScreenBuilder] Screen 5 â€” Steam Generators â€” build complete");
        }

        // ----------------------------------------------------------------
        // SG: Left Panel â€” 8 Primary Side Temperature Gauges
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
        // SG: Center Panel â€” Quad-SG 2x2 Layout
        // ----------------------------------------------------------------

        private static void BuildSGCenterPanel(Transform parent, SteamGeneratorScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "CenterPanel",
                new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f), COLOR_CENTER_BG);

            CreateTMPLabel(panelGO.transform, "ScreenTitle", "STEAM GENERATORS", 16, COLOR_CYAN,
                new Vector2(0.02f, 0.93f), new Vector2(0.98f, 0.99f));
            CreateTMPLabel(panelGO.transform, "ScreenSubtitle",
                "WESTINGHOUSE 4-LOOP PWR â€” MODEL F â€” 4 UNITS", 10, COLOR_TEXT_LABEL,
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

            // Overlay text â€” level, pressure, secondary temp (right side of cell)
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
        // SG: Right Panel â€” 8 Secondary Side Gauges
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
        // SG: Bottom Panel â€” Heat Removal, Flow, Status, Alarms
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

    }
}
#endif


