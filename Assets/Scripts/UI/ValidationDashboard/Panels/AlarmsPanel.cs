// ============================================================================
// CRITICAL: Master the Atom - Alarms Detail Panel
// AlarmsPanel.cs - Full Alarm Annunciator Grid
// ============================================================================
//
// TAB: 5 (ALARMS)
// VERSION: 1.0.0
// DATE: 2026-02-17
// IP: IP-0031 Stage 4
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Critical.UI.ValidationDashboard
{
    public class AlarmsPanel : ValidationPanelBase
    {
        public override string PanelName => "AlarmsPanel";
        public override int TabIndex => 5;

        // Alarm categories
        private Dictionary<string, AlarmTileExtended> _alarmTiles = new Dictionary<string, AlarmTileExtended>();

        protected override void OnInitialize()
        {
            BuildLayout();
        }

        private void BuildLayout()
        {
            // Main vertical layout
            VerticalLayoutGroup mainLayout = gameObject.AddComponent<VerticalLayoutGroup>();
            mainLayout.childAlignment = TextAnchor.UpperCenter;
            mainLayout.childControlWidth = true;
            mainLayout.childControlHeight = false;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = false;
            mainLayout.spacing = 16;
            mainLayout.padding = new RectOffset(8, 8, 8, 8);

            // RCS Alarms row
            CreateAlarmRow(transform, "RCS ALARMS", new string[] {
                "PRESS HI", "PRESS LO", "PRESS HI-HI", "PRESS LO-LO",
                "SUBCOOL LO", "FLOW LO", "T-AVG HI", "T-AVG LO"
            });

            // Pressurizer Alarms row
            CreateAlarmRow(transform, "PRESSURIZER ALARMS", new string[] {
                "PZR LVL HI", "PZR LVL LO", "PZR LVL HI-HI", "PZR LVL LO-LO",
                "HEATER FAIL", "SPRAY FAIL", "PORV OPEN", "SAFETY OPEN"
            });

            // CVCS Alarms row
            CreateAlarmRow(transform, "CVCS ALARMS", new string[] {
                "VCT LVL HI", "VCT LVL LO", "CHG FLOW LO", "LTD ISOLATED",
                "BORON HI", "BORON LO", "MAKEUP ACTIVE", "DIVERT ACTIVE"
            });

            // SG/RHR Alarms row
            CreateAlarmRow(transform, "SG / RHR ALARMS", new string[] {
                "SG PRESS HI", "SG PRESS LO", "SG LEVEL HI", "SG LEVEL LO",
                "RHR FLOW LO", "RHR TEMP HI", "BOILING", "RELIEF OPEN"
            });

            // System Alarms row
            CreateAlarmRow(transform, "SYSTEM ALARMS", new string[] {
                "MASS ERROR", "ENERGY ERROR", "TIMESTEP", "SIMULATION",
                "RCP TRIP", "TURB TRIP", "REACTOR TRIP", "EMER BORATE"
            });
        }

        private void CreateAlarmRow(Transform parent, string title, string[] alarmNames)
        {
            GameObject rowContainer = new GameObject(title.Replace(" ", ""));
            rowContainer.transform.SetParent(parent, false);

            LayoutElement rowLE = rowContainer.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 80;

            VerticalLayoutGroup rowVL = rowContainer.AddComponent<VerticalLayoutGroup>();
            rowVL.childAlignment = TextAnchor.UpperCenter;
            rowVL.childControlWidth = true;
            rowVL.childControlHeight = false;
            rowVL.spacing = 4;

            // Header
            GameObject headerGO = new GameObject("Header");
            headerGO.transform.SetParent(rowContainer.transform, false);

            LayoutElement headerLE = headerGO.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 20;

            TextMeshProUGUI headerText = headerGO.AddComponent<TextMeshProUGUI>();
            headerText.text = title;
            headerText.fontSize = 11;
            headerText.fontStyle = FontStyles.Bold;
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.color = ValidationDashboardTheme.TextSecondary;

            // Alarm tiles grid
            GameObject gridGO = new GameObject("Grid");
            gridGO.transform.SetParent(rowContainer.transform, false);

            LayoutElement gridLE = gridGO.AddComponent<LayoutElement>();
            gridLE.flexibleHeight = 1;

            GridLayoutGroup grid = gridGO.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(90, 32);
            grid.spacing = new Vector2(6, 4);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 8;

            foreach (string alarmName in alarmNames)
            {
                var tile = AlarmTileExtended.Create(gridGO.transform, alarmName);
                _alarmTiles[alarmName] = tile;
            }
        }

        protected override void OnUpdateData()
        {
            if (Engine == null) return;

            // RCS Alarms
            SetAlarm("PRESS HI", Engine.pressureHigh);
            SetAlarm("PRESS LO", Engine.pressureLow);
            SetAlarm("PRESS HI-HI", Engine.pressure > 2385f);
            SetAlarm("PRESS LO-LO", Engine.pressure < 1945f);
            SetAlarm("SUBCOOL LO", Engine.subcoolingLow);
            SetAlarm("FLOW LO", Engine.rcsFlowLow);
            SetAlarm("T-AVG HI", Engine.T_avg > 580f);
            SetAlarm("T-AVG LO", Engine.T_avg < 70f);

            // Pressurizer Alarms
            SetAlarm("PZR LVL HI", Engine.pzrLevelHigh);
            SetAlarm("PZR LVL LO", Engine.pzrLevelLow);
            SetAlarm("PZR LVL HI-HI", Engine.pzrLevel > 92f);
            SetAlarm("PZR LVL LO-LO", Engine.pzrLevel < 12f);
            SetAlarm("HEATER FAIL", false); // Future implementation
            SetAlarm("SPRAY FAIL", false);  // Future implementation
            SetAlarm("PORV OPEN", Engine.porvOpen);
            SetAlarm("SAFETY OPEN", Engine.safetyOpen);

            // CVCS Alarms
            SetAlarm("VCT LVL HI", Engine.vctLevelHigh);
            SetAlarm("VCT LVL LO", Engine.vctLevelLow);
            SetAlarm("CHG FLOW LO", Engine.chargingFlow < 20f && Engine.chargingActive);
            SetAlarm("LTD ISOLATED", Engine.letdownIsolatedFlag);
            SetAlarm("BORON HI", Engine.boronConcentration > 2000f);
            SetAlarm("BORON LO", Engine.boronConcentration < 100f);
            SetAlarm("MAKEUP ACTIVE", Engine.vctAutoMakeupActive);
            SetAlarm("DIVERT ACTIVE", Engine.vctDivertActive);

            // SG/RHR Alarms
            SetAlarm("SG PRESS HI", Engine.sgSecondaryPressure_psia > 1050f);
            SetAlarm("SG PRESS LO", Engine.sgSecondaryPressure_psia < 50f);
            SetAlarm("SG LEVEL HI", false); // Future implementation
            SetAlarm("SG LEVEL LO", false); // Future implementation
            SetAlarm("RHR FLOW LO", Engine.rhrActive && Engine.rhrFlow < 1000f);
            SetAlarm("RHR TEMP HI", Engine.rhrOutletTemp > 350f);
            SetAlarm("BOILING", Engine.sgBoilingActive);
            SetAlarm("RELIEF OPEN", false); // Future implementation

            // System Alarms
            SetAlarm("MASS ERROR", Engine.primaryMassAlarm);
            SetAlarm("ENERGY ERROR", Mathf.Abs(Engine.netPlantHeat_MW) > 10f);
            SetAlarm("TIMESTEP", false); // Future implementation
            SetAlarm("SIMULATION", false); // Future implementation
            SetAlarm("RCP TRIP", Engine.rcpCount == 0 && Engine.plantMode <= 3);
            SetAlarm("TURB TRIP", false); // Future implementation
            SetAlarm("REACTOR TRIP", false); // Future implementation
            SetAlarm("EMER BORATE", false); // Future implementation
        }

        private void SetAlarm(string name, bool active)
        {
            if (_alarmTiles.TryGetValue(name, out var tile))
                tile.SetState(active);
        }
    }

    // Extended alarm tile with more visual features
    public class AlarmTileExtended : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI labelText;

        private bool _isAlarmed;
        private float _pulseTime;
        private bool _acknowledged;

        private static readonly Color NormalBg = new Color32(25, 28, 36, 255);
        private static readonly Color NormalBorder = new Color32(50, 55, 70, 255);
        private static readonly Color AlarmBg = new Color32(255, 46, 46, 255);
        private static readonly Color AlarmBorder = new Color32(255, 120, 120, 255);
        private static readonly Color AckedBg = new Color32(180, 30, 30, 255);
        private static readonly Color NormalText = new Color32(100, 105, 120, 255);
        private static readonly Color AlarmText = new Color32(20, 22, 28, 255);

        void Update()
        {
            if (_isAlarmed && !_acknowledged)
            {
                _pulseTime += Time.unscaledDeltaTime * 2f;
                float pulse = (Mathf.Sin(_pulseTime * Mathf.PI * 2f) + 1f) * 0.5f;
                float alpha = Mathf.Lerp(0.6f, 1f, pulse);

                if (backgroundImage != null)
                {
                    Color c = AlarmBg;
                    c.a = alpha;
                    backgroundImage.color = c;
                }
            }
        }

        public void SetState(bool alarmed)
        {
            _isAlarmed = alarmed;

            if (!alarmed)
            {
                _pulseTime = 0f;
                _acknowledged = false;
            }

            if (backgroundImage != null)
                backgroundImage.color = alarmed ? AlarmBg : NormalBg;

            if (borderImage != null)
                borderImage.color = alarmed ? AlarmBorder : NormalBorder;

            if (labelText != null)
                labelText.color = alarmed ? AlarmText : NormalText;
        }

        public void Acknowledge()
        {
            if (_isAlarmed)
            {
                _acknowledged = true;
                if (backgroundImage != null)
                    backgroundImage.color = AckedBg;
            }
        }

        public static AlarmTileExtended Create(Transform parent, string label)
        {
            GameObject tileGO = new GameObject($"Alarm_{label.Replace(" ", "")}");
            tileGO.transform.SetParent(parent, false);

            // Border (background layer)
            Image border = tileGO.AddComponent<Image>();
            border.color = NormalBorder;

            // Inner background
            GameObject innerGO = new GameObject("Inner");
            innerGO.transform.SetParent(tileGO.transform, false);

            RectTransform innerRT = innerGO.AddComponent<RectTransform>();
            innerRT.anchorMin = Vector2.zero;
            innerRT.anchorMax = Vector2.one;
            innerRT.offsetMin = new Vector2(2, 2);
            innerRT.offsetMax = new Vector2(-2, -2);

            Image innerBg = innerGO.AddComponent<Image>();
            innerBg.color = NormalBg;

            // Label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(innerGO.transform, false);

            RectTransform labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(2, 2);
            labelRT.offsetMax = new Vector2(-2, -2);

            TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 9;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.color = NormalText;
            labelTMP.enableWordWrapping = true;

            AlarmTileExtended tile = tileGO.AddComponent<AlarmTileExtended>();
            tile.backgroundImage = innerBg;
            tile.borderImage = border;
            tile.labelText = labelTMP;

            return tile;
        }
    }
}
