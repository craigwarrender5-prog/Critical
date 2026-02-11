// CRITICAL: Master the Atom - Phase 2 Mosaic Board Scene Builder
// MosaicBoardBuilder.cs - Editor Tool for Scene Setup
//
// Creates complete Mosaic Board UI hierarchy in Unity scene.
// Menu: Critical > Create Mosaic Board

using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Critical.UI
{
    using Controllers;
    
    /// <summary>
    /// Builder for creating Mosaic Board UI in scene.
    /// </summary>
    public class MosaicBoardBuilder : MonoBehaviour
    {
        #if UNITY_EDITOR
        
        [MenuItem("Critical/Create Mosaic Board")]
        public static void CreateMosaicBoard()
        {
            // Create Canvas if needed
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("MosaicBoardCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            
            // Create main board
            var boardGO = new GameObject("MosaicBoard");
            boardGO.transform.SetParent(canvas.transform, false);
            
            var boardRect = boardGO.AddComponent<RectTransform>();
            boardRect.anchorMin = Vector2.zero;
            boardRect.anchorMax = Vector2.one;
            boardRect.offsetMin = Vector2.zero;
            boardRect.offsetMax = Vector2.zero;
            
            var boardImage = boardGO.AddComponent<Image>();
            boardImage.color = new Color(0.1f, 0.1f, 0.12f);
            
            var board = boardGO.AddComponent<MosaicBoard>();
            
            // Create sections
            CreatePowerSection(boardGO.transform, board);
            CreateTemperatureSection(boardGO.transform, board);
            CreateReactivitySection(boardGO.transform, board);
            CreateRodControlSection(boardGO.transform, board);
            CreateChemistrySection(boardGO.transform, board);
            CreateAlarmSection(boardGO.transform, board);
            CreateControlSection(boardGO.transform, board);
            CreateStatusSection(boardGO.transform, board);
            
            // Select the board
            Selection.activeGameObject = boardGO;
            
            Debug.Log("[MosaicBoardBuilder] Mosaic Board created successfully!");
        }
        
        private static void CreatePowerSection(Transform parent, MosaicBoard board)
        {
            var section = CreateSection(parent, "PowerSection", 
                new Vector2(0f, 0.7f), new Vector2(0.25f, 1f));
            board.PowerSection = section;
            
            CreateSectionLabel(section, "POWER");
            
            // Neutron Power gauge
            CreateGauge(section, "NeutronPowerGauge", GaugeType.NeutronPower, 
                new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.8f));
            
            // Thermal Power text
            CreateDigitalDisplay(section, "ThermalPowerDisplay", GaugeType.ThermalPower,
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.25f));
        }
        
        private static void CreateTemperatureSection(Transform parent, MosaicBoard board)
        {
            var section = CreateSection(parent, "TemperatureSection",
                new Vector2(0.25f, 0.7f), new Vector2(0.5f, 1f));
            board.TemperatureSection = section;
            
            CreateSectionLabel(section, "TEMPERATURE");
            
            // Tavg
            CreateGauge(section, "TavgGauge", GaugeType.Tavg,
                new Vector2(0.05f, 0.3f), new Vector2(0.35f, 0.8f));
            
            // Thot
            CreateGauge(section, "ThotGauge", GaugeType.Thot,
                new Vector2(0.35f, 0.3f), new Vector2(0.65f, 0.8f));
            
            // Tcold
            CreateGauge(section, "TcoldGauge", GaugeType.Tcold,
                new Vector2(0.65f, 0.3f), new Vector2(0.95f, 0.8f));
            
            // Delta-T
            CreateDigitalDisplay(section, "DeltaTDisplay", GaugeType.DeltaT,
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.25f));
        }
        
        private static void CreateReactivitySection(Transform parent, MosaicBoard board)
        {
            var section = CreateSection(parent, "ReactivitySection",
                new Vector2(0.5f, 0.7f), new Vector2(0.75f, 1f));
            board.ReactivitySection = section;
            
            CreateSectionLabel(section, "REACTIVITY");
            
            // Total Reactivity
            CreateGauge(section, "ReactivityGauge", GaugeType.TotalReactivity,
                new Vector2(0.05f, 0.3f), new Vector2(0.5f, 0.8f));
            
            // Startup Rate
            CreateGauge(section, "StartupRateGauge", GaugeType.StartupRate,
                new Vector2(0.5f, 0.3f), new Vector2(0.95f, 0.8f));
            
            // Period display
            CreateDigitalDisplay(section, "PeriodDisplay", GaugeType.ReactorPeriod,
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.25f));
        }
        
        private static void CreateRodControlSection(Transform parent, MosaicBoard board)
        {
            var section = CreateSection(parent, "RodControlSection",
                new Vector2(0.75f, 0.7f), new Vector2(1f, 1f));
            board.RodControlSection = section;
            
            CreateSectionLabel(section, "ROD CONTROL");
            
            // Rod display
            var rodDisplayGO = new GameObject("RodDisplay");
            rodDisplayGO.transform.SetParent(section, false);
            var rodRect = rodDisplayGO.AddComponent<RectTransform>();
            rodRect.anchorMin = new Vector2(0.05f, 0.15f);
            rodRect.anchorMax = new Vector2(0.95f, 0.85f);
            rodRect.offsetMin = Vector2.zero;
            rodRect.offsetMax = Vector2.zero;
            
            rodDisplayGO.AddComponent<MosaicRodDisplay>();
        }
        
        private static void CreateChemistrySection(Transform parent, MosaicBoard board)
        {
            var section = CreateSection(parent, "ChemistrySection",
                new Vector2(0f, 0.4f), new Vector2(0.25f, 0.7f));
            board.ChemistrySection = section;
            
            CreateSectionLabel(section, "CHEMISTRY");
            
            // Boron
            CreateDigitalDisplay(section, "BoronDisplay", GaugeType.Boron,
                new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.85f));
            
            // Xenon
            CreateDigitalDisplay(section, "XenonDisplay", GaugeType.Xenon,
                new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.45f));
        }
        
        private static void CreateAlarmSection(Transform parent, MosaicBoard board)
        {
            var section = CreateSection(parent, "AlarmSection",
                new Vector2(0.25f, 0.4f), new Vector2(0.75f, 0.7f));
            board.AlarmSection = section;
            
            CreateSectionLabel(section, "ALARMS");
            
            // Alarm panel
            var alarmGO = new GameObject("AlarmPanel");
            alarmGO.transform.SetParent(section, false);
            var alarmRect = alarmGO.AddComponent<RectTransform>();
            alarmRect.anchorMin = new Vector2(0.02f, 0.1f);
            alarmRect.anchorMax = new Vector2(0.98f, 0.85f);
            alarmRect.offsetMin = Vector2.zero;
            alarmRect.offsetMax = Vector2.zero;
            
            var alarmBg = alarmGO.AddComponent<Image>();
            alarmBg.color = new Color(0.15f, 0.1f, 0.1f);
            
            alarmGO.AddComponent<MosaicAlarmPanel>();
        }
        
        private static void CreateControlSection(Transform parent, MosaicBoard board)
        {
            var section = CreateSection(parent, "ControlSection",
                new Vector2(0.75f, 0.4f), new Vector2(1f, 0.7f));
            
            CreateSectionLabel(section, "CONTROLS");
            
            // Control panel
            var controlGO = new GameObject("ControlPanel");
            controlGO.transform.SetParent(section, false);
            var controlRect = controlGO.AddComponent<RectTransform>();
            controlRect.anchorMin = new Vector2(0.05f, 0.1f);
            controlRect.anchorMax = new Vector2(0.95f, 0.85f);
            controlRect.offsetMin = Vector2.zero;
            controlRect.offsetMax = Vector2.zero;
            
            controlGO.AddComponent<MosaicControlPanel>();
        }
        
        private static void CreateStatusSection(Transform parent, MosaicBoard board)
        {
            var section = CreateSection(parent, "StatusSection",
                new Vector2(0f, 0f), new Vector2(1f, 0.4f));
            
            CreateSectionLabel(section, "STATUS");
            
            // Status indicators row
            var indicatorRow = new GameObject("IndicatorRow");
            indicatorRow.transform.SetParent(section, false);
            var rowRect = indicatorRow.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.02f, 0.6f);
            rowRect.anchorMax = new Vector2(0.98f, 0.85f);
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;
            
            var hlg = indicatorRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            
            // Create indicators
            CreateIndicator(indicatorRow.transform, "TripIndicator", IndicatorCondition.ReactorTripped, AlarmState.Trip);
            CreateIndicator(indicatorRow.transform, "CriticalIndicator", IndicatorCondition.ReactorCritical, AlarmState.Normal);
            CreateIndicator(indicatorRow.transform, "SubcritIndicator", IndicatorCondition.ReactorSubcritical, AlarmState.Warning);
            CreateIndicator(indicatorRow.transform, "OverpowerIndicator", IndicatorCondition.Overpower, AlarmState.Alarm);
            CreateIndicator(indicatorRow.transform, "LowFlowIndicator", IndicatorCondition.LowFlow, AlarmState.Alarm);
            CreateIndicator(indicatorRow.transform, "RodBottomIndicator", IndicatorCondition.RodBottomAlarm, AlarmState.Warning);
            
            // Fuel temperature display
            CreateGauge(section, "FuelTempGauge", GaugeType.FuelCenterline,
                new Vector2(0.02f, 0.1f), new Vector2(0.32f, 0.55f));
            
            // Time display area
            var timeDisplay = CreatePanel(section, "TimeDisplay",
                new Vector2(0.35f, 0.1f), new Vector2(0.65f, 0.55f));
            
            var timeText = CreateText(timeDisplay, "SimTimeText", "00:00:00", 24);
            var tcText = CreateText(timeDisplay, "TimeCompText", "1x", 16);
            tcText.anchorMin = new Vector2(0f, 0f);
            tcText.anchorMax = new Vector2(1f, 0.4f);
            
            // Flow display
            CreateGauge(section, "FlowGauge", GaugeType.FlowFraction,
                new Vector2(0.68f, 0.1f), new Vector2(0.98f, 0.55f));
        }
        
        #region Helper Methods
        
        private static RectTransform CreateSection(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(5f, 5f);
            rect.offsetMax = new Vector2(-5f, -5f);
            
            var image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.12f, 0.15f);
            
            return rect;
        }
        
        private static void CreateSectionLabel(RectTransform section, string text)
        {
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(section, false);
            
            var rect = labelGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.9f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var label = labelGO.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 14;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.7f, 0.7f, 0.8f);
        }
        
        private static void CreateGauge(RectTransform section, string name, GaugeType type, 
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(section, false);
            
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.1f);
            
            var gauge = go.AddComponent<MosaicGauge>();
            gauge.Type = type;
            gauge.ShowAnalog = false;
            gauge.ShowDigital = true;
            
            // Value text
            var valueGO = new GameObject("Value");
            valueGO.transform.SetParent(go.transform, false);
            var valueRect = valueGO.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0f, 0.3f);
            valueRect.anchorMax = new Vector2(1f, 0.8f);
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
            
            var valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.fontSize = 18;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = new Color(0.2f, 0.8f, 0.2f);
            gauge.ValueText = valueText;
            
            // Label text
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0.25f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            
            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.fontSize = 10;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0.6f, 0.6f, 0.7f);
            gauge.LabelText = labelText;
        }
        
        private static void CreateDigitalDisplay(RectTransform section, string name, GaugeType type,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            CreateGauge(section, name, type, anchorMin, anchorMax);
        }
        
        private static void CreateIndicator(Transform parent, string name, IndicatorCondition condition, AlarmState colorType)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80f, 40f);
            
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f);
            
            var indicator = go.AddComponent<MosaicIndicator>();
            indicator.Condition = condition;
            indicator.ColorType = colorType;
            indicator.FlashWhenActive = (colorType >= AlarmState.Alarm);
            indicator.IndicatorImage = bg;
            
            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            
            var label = labelGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 10;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            indicator.LabelText = label;
        }
        
        private static RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.1f);
            
            return rect;
        }
        
        private static RectTransform CreateText(RectTransform parent, string name, string defaultText, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.4f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var text = go.AddComponent<Text>();
            text.text = defaultText;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.2f, 0.8f, 0.2f);
            
            return rect;
        }
        
        #endregion
        
        #endif
    }
}
