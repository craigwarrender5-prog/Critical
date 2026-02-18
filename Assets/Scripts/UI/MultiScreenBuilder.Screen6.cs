// CRITICAL: Master the Atom - Multi-Screen Builder
// MultiScreenBuilder.Screen6.cs
//
// File: Assets/Scripts/UI/MultiScreenBuilder.Screen6.cs
// Module: Critical.UI.MultiScreenBuilder
// Responsibility: Screen 6 - Turbine-Generator construction logic.
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

        #region Screen 6 - Turbine-Generator

        private static void CreateTurbineGeneratorScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 6 â€” Turbine-Generator...");

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
            Debug.Log("[MultiScreenBuilder] Screen 6 â€” Turbine-Generator â€” build complete");
        }

        // ----------------------------------------------------------------
        // TG: Left Panel â€” 8 Turbine Performance Gauges
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
        // TG: Center Panel â€” Shaft Train Diagram
        // ----------------------------------------------------------------

        private static void BuildTGCenterPanel(Transform parent, TurbineGeneratorScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "CenterPanel",
                new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f), COLOR_CENTER_BG);

            CreateTMPLabel(panelGO.transform, "ScreenTitle", "TURBINE-GENERATOR", 16, COLOR_CYAN,
                new Vector2(0.02f, 0.93f), new Vector2(0.98f, 0.99f));
            CreateTMPLabel(panelGO.transform, "ScreenSubtitle",
                "1150 MWe GROSS â€” 3600 RPM â€” 60 Hz", 10, COLOR_TEXT_LABEL,
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
        // TG: Right Panel â€” 8 Generator Output Gauges
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
        // TG: Bottom Panel â€” Controls, Breaker Status, Alarms
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

    }
}
#endif


