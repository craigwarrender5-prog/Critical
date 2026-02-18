// CRITICAL: Master the Atom - Multi-Screen Builder
// MultiScreenBuilder.OverviewTab.cs
//
// File: Assets/Scripts/UI/MultiScreenBuilder.OverviewTab.cs
// Module: Critical.UI.MultiScreenBuilder
// Responsibility: Screen Tab - Plant Overview construction logic.
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

        #region Screen Tab - Plant Overview

        /// <summary>
        /// Build the complete Plant Overview screen (Tab key) UI hierarchy.
        /// </summary>
        private static void CreatePlantOverviewScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen Tab â€” Plant Overview...");

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

            Debug.Log("[MultiScreenBuilder] Screen Tab â€” Plant Overview â€” build complete");
        }

        // ----------------------------------------------------------------
        // Plant Overview: Left Panel â€” 8 Nuclear/Primary Gauges (TMP)
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
        // Plant Overview: Center Panel â€” Mimic Diagram
        // ----------------------------------------------------------------

        private static void BuildOverviewCenterPanel(Transform parent, PlantOverviewScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "CenterPanel",
                new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f), COLOR_CENTER_BG);

            // Title
            CreateTMPLabel(panelGO.transform, "ScreenTitle", "PLANT OVERVIEW", 16, COLOR_CYAN,
                new Vector2(0.02f, 0.93f), new Vector2(0.98f, 0.99f));

            CreateTMPLabel(panelGO.transform, "ScreenSubtitle",
                "WESTINGHOUSE 4-LOOP PWR â€” 3411 MWt / 1150 MWe", 10, COLOR_TEXT_LABEL,
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

                // Hot leg line (reactor right side â†’ SG left side)
                Image hotLeg = CreateMimicBox(mimicGO.transform, $"HotLeg_{i + 1}",
                    new Vector2(0.22f, yMid + 0.04f), new Vector2(0.40f, yMid + 0.07f),
                    hotLegColor);
                hotLegsProp.GetArrayElementAtIndex(i).objectReferenceValue = hotLeg;

                // Cold leg line (SG right side â†’ reactor left side via bottom)
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
        // Plant Overview: Right Panel â€” 8 Secondary/Output Gauges (TMP)
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
        // Plant Overview: Bottom Panel â€” Status, RCP indicators, Alarms
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

    }
}
#endif


