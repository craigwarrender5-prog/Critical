// CRITICAL: Master the Atom - Multi-Screen Builder
// MultiScreenBuilder.Screen2.cs
//
// File: Assets/Scripts/UI/MultiScreenBuilder.Screen2.cs
// Module: Critical.UI.MultiScreenBuilder
// Responsibility: Screen 2 - RCS Primary Loop construction logic.
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

        #region Screen 2 - RCS Primary Loop

        /// <summary>
        /// Build the complete RCS Primary Loop screen (Screen 2) UI hierarchy.
        /// </summary>
        private static void CreateRCSScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 2 â€” RCS Primary Loop...");

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

            Debug.Log("[MultiScreenBuilder] Screen 2 â€” RCS Primary Loop â€” build complete");
        }

        // ----------------------------------------------------------------
        // Screen 2: Left Panel â€” 8 Temperature Gauges
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

            var g1h = CreateRCSGauge(panelGO.transform, "Gauge_L1_THot", "LOOP 1 T-HOT", "Â°F");
            var g2h = CreateRCSGauge(panelGO.transform, "Gauge_L2_THot", "LOOP 2 T-HOT", "Â°F");
            var g3h = CreateRCSGauge(panelGO.transform, "Gauge_L3_THot", "LOOP 3 T-HOT", "Â°F");
            var g4h = CreateRCSGauge(panelGO.transform, "Gauge_L4_THot", "LOOP 4 T-HOT", "Â°F");
            var g1c = CreateRCSGauge(panelGO.transform, "Gauge_L1_TCold", "LOOP 1 T-COLD", "Â°F");
            var g2c = CreateRCSGauge(panelGO.transform, "Gauge_L2_TCold", "LOOP 2 T-COLD", "Â°F");
            var g3c = CreateRCSGauge(panelGO.transform, "Gauge_L3_TCold", "LOOP 3 T-COLD", "Â°F");
            var g4c = CreateRCSGauge(panelGO.transform, "Gauge_L4_TCold", "LOOP 4 T-COLD", "Â°F");

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
        // Screen 2: Center Panel â€” 2D RCS Mimic Schematic
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

            // ---- 4 Loops at 90Â° intervals (arranged as quadrants) ----
            //
            //  Layout:   Loop 1 (top-right)    Loop 2 (bottom-right)
            //            Loop 4 (top-left)     Loop 3 (bottom-left)
            //
            //  Each loop: Hot Leg â†’ SG â†’ Cold Leg â†’ RCP â†’ Crossover â†’ Vessel

            // Geometric positions for each loop
            // [hotLeg, SG, coldLeg, RCP, crossover] anchors
            float[][] loopData = new float[][] {
                // Loop 1 (top-right): HL goes right, CL returns right
                //   Hot leg:       vessel right â†’ SG
                //   SG:            right of vessel, upper
                //   Cold leg:      SG â†’ RCP
                //   RCP:           below SG
                //   Crossover:     RCP â†’ vessel
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

                // Crossover Leg (RCP â†’ Vessel)
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
        // Screen 2: Right Panel â€” 8 Flow/Power Gauges
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
            var gDT = CreateRCSGauge(panelGO.transform, "Gauge_CoreDeltaT", "CORE Î”T", "Â°F");
            var gTavg = CreateRCSGauge(panelGO.transform, "Gauge_AvgTavg", "AVG T-AVG", "Â°F");

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
        // Screen 2: Bottom Panel â€” 4 RCP Panels + Status + Alarms
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

    }
}
#endif


