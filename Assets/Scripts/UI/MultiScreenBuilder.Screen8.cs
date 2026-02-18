// CRITICAL: Master the Atom - Multi-Screen Builder
// MultiScreenBuilder.Screen8.cs
//
// File: Assets/Scripts/UI/MultiScreenBuilder.Screen8.cs
// Module: Critical.UI.MultiScreenBuilder
// Responsibility: Screen 8 - Auxiliary Systems construction logic.
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

        #region Screen 8 - Auxiliary Systems

        private static void CreateAuxiliarySystemsScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 8 â€” Auxiliary Systems...");

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
            Debug.Log("[MultiScreenBuilder] Screen 8 â€” Auxiliary Systems â€” build complete");
        }

        // ----------------------------------------------------------------
        // AUX: Left Panel â€” 8 RHR System Gauges
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
        // AUX: Center Panel â€” Auxiliary Systems Diagram
        // ----------------------------------------------------------------

        private static void BuildAuxCenterPanel(Transform parent, AuxiliarySystemsScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "CenterPanel",
                new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f), COLOR_CENTER_BG);

            CreateTMPLabel(panelGO.transform, "ScreenTitle", "AUXILIARY SYSTEMS", 16, COLOR_CYAN,
                new Vector2(0.02f, 0.93f), new Vector2(0.98f, 0.99f));
            CreateTMPLabel(panelGO.transform, "ScreenSubtitle",
                "RHR â€” CCW â€” SERVICE WATER", 10, COLOR_TEXT_LABEL,
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
        // AUX: Right Panel â€” 8 Cooling Water Gauges
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
        // AUX: Bottom Panel â€” Pump Controls, Status, Alarms
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

    }
}
#endif


