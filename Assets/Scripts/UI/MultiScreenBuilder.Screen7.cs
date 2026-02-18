// CRITICAL: Master the Atom - Multi-Screen Builder
// MultiScreenBuilder.Screen7.cs
//
// File: Assets/Scripts/UI/MultiScreenBuilder.Screen7.cs
// Module: Critical.UI.MultiScreenBuilder
// Responsibility: Screen 7 - Secondary Systems construction logic.
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

        #region Screen 7 - Secondary Systems

        private static void CreateSecondarySystemsScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 7 â€” Secondary Systems...");

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
            Debug.Log("[MultiScreenBuilder] Screen 7 â€” Secondary Systems â€” build complete");
        }

        // ----------------------------------------------------------------
        // SEC: Left Panel â€” 8 Feedwater Train Gauges
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
        // SEC: Center Panel â€” Secondary Cycle Flow Diagram
        // ----------------------------------------------------------------

        private static void BuildSecCenterPanel(Transform parent, SecondarySystemsScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "CenterPanel",
                new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f), COLOR_CENTER_BG);

            CreateTMPLabel(panelGO.transform, "ScreenTitle", "SECONDARY SYSTEMS", 16, COLOR_CYAN,
                new Vector2(0.02f, 0.93f), new Vector2(0.98f, 0.99f));
            CreateTMPLabel(panelGO.transform, "ScreenSubtitle",
                "FEEDWATER â€” MAIN STEAM â€” STEAM DUMP â€” CONDENSATE", 10, COLOR_TEXT_LABEL,
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
        // SEC: Right Panel â€” 8 Steam System Gauges
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
        // SEC: Bottom Panel â€” Steam Dump Controls, MSIV Controls, Alarms
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

    }
}
#endif


