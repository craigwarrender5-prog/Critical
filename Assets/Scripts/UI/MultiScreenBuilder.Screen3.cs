// CRITICAL: Master the Atom - Multi-Screen Builder
// MultiScreenBuilder.Screen3.cs
//
// File: Assets/Scripts/UI/MultiScreenBuilder.Screen3.cs
// Module: Critical.UI.MultiScreenBuilder
// Responsibility: Screen 3 - Pressurizer construction logic.
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

        #region Screen 3 - Pressurizer

        /// <summary>
        /// Build the complete Pressurizer screen (Key 3) UI hierarchy.
        /// </summary>
        private static void CreatePressurizerScreen(Transform canvasParent)
        {
            Debug.Log("[MultiScreenBuilder] Building Screen 3 â€” Pressurizer...");

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

            Debug.Log("[MultiScreenBuilder] Screen 3 â€” Pressurizer â€” build complete");
        }

        // ----------------------------------------------------------------
        // Pressurizer: Left Panel â€” 8 Pressure Gauges
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
        // Pressurizer: Center Panel â€” 2D Vessel Cutaway
        // ----------------------------------------------------------------

        private static void BuildPZRCenterPanel(Transform parent, PressurizerScreen screen)
        {
            GameObject panelGO = CreatePanel(parent, "CenterPanel",
                new Vector2(0.15f, 0.26f), new Vector2(0.65f, 1f), COLOR_CENTER_BG);

            // Title
            CreateTMPLabel(panelGO.transform, "ScreenTitle", "PRESSURIZER", 16, COLOR_CYAN,
                new Vector2(0.02f, 0.93f), new Vector2(0.98f, 0.99f));

            CreateTMPLabel(panelGO.transform, "ScreenSubtitle",
                "WESTINGHOUSE 4-LOOP PWR â€” 1800 FT\u00B3 / 2235 PSIA", 10, COLOR_TEXT_LABEL,
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
        // Pressurizer: Right Panel â€” 8 Level/Volume Gauges
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
        // Pressurizer: Bottom Panel â€” Controls, Valve Status, Alarms
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

    }
}
#endif


