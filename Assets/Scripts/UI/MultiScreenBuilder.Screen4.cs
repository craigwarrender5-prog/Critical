// CRITICAL: Master the Atom - Multi-Screen Builder
// MultiScreenBuilder.Screen4.cs
//
// File: Assets/Scripts/UI/MultiScreenBuilder.Screen4.cs
// Module: Critical.UI.MultiScreenBuilder
// Responsibility: Screen 4 - CVCS construction logic.
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

        #region Screen 4 - CVCS

        private static void CreateCVCSScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 4 â€” CVCS...");

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
            Debug.Log("[MultiScreenBuilder] Screen 4 â€” CVCS â€” build complete");
        }

        // ----------------------------------------------------------------
        // CVCS: Left Panel â€” 8 Flow/Control Gauges
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
        // CVCS: Center Panel â€” 2D Flow Diagram
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
        // CVCS: Right Panel â€” 8 Chemistry/Boron Gauges
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
        // CVCS: Bottom Panel â€” CCP Controls, Boration/Dilution, Alarms
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

    }
}
#endif


