// ============================================================================
// CRITICAL: Master the Atom - Overview Section: Alarms
// OverviewSection_Alarms.cs - Alarm Summary Display
// ============================================================================
//
// PARAMETERS DISPLAYED:
//   - RCS Pressure High/Low
//   - PZR Level High/Low
//   - Subcooling Low
//   - VCT Level High/Low
//   - Mass Conservation Alarm
//
// VERSION: 1.0.0
// DATE: 2026-02-16
// IP: IP-0031 Stage 3
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Alarm summary section showing key annunciator states.
    /// </summary>
    public class OverviewSection_Alarms : OverviewSectionBase
    {
        // Alarm tile references
        private AlarmTile _pressHighTile;
        private AlarmTile _pressLowTile;
        private AlarmTile _pzrLevelHighTile;
        private AlarmTile _pzrLevelLowTile;
        private AlarmTile _subcoolLowTile;
        private AlarmTile _vctLevelTile;
        private AlarmTile _massConsTile;
        private AlarmTile _flowLowTile;

        protected override void BuildContent()
        {
            // Create a grid of alarm tiles
            // Using GridLayoutGroup for proper arrangement

            GameObject gridGO = new GameObject("AlarmGrid");
            gridGO.transform.SetParent(ContentRoot, false);

            RectTransform gridRT = gridGO.AddComponent<RectTransform>();
            gridRT.anchorMin = Vector2.zero;
            gridRT.anchorMax = Vector2.one;
            gridRT.offsetMin = Vector2.zero;
            gridRT.offsetMax = Vector2.zero;

            GridLayoutGroup grid = gridGO.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(70, 28);
            grid.spacing = new Vector2(4, 4);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            // Create alarm tiles
            _pressHighTile = AlarmTile.Create(gridGO.transform, "P HIGH");
            _pressLowTile = AlarmTile.Create(gridGO.transform, "P LOW");
            _pzrLevelHighTile = AlarmTile.Create(gridGO.transform, "LVL HI");
            _pzrLevelLowTile = AlarmTile.Create(gridGO.transform, "LVL LO");
            _subcoolLowTile = AlarmTile.Create(gridGO.transform, "SUBCOOL");
            _vctLevelTile = AlarmTile.Create(gridGO.transform, "VCT");
            _massConsTile = AlarmTile.Create(gridGO.transform, "MASS");
            _flowLowTile = AlarmTile.Create(gridGO.transform, "FLOW");
        }

        public override void UpdateData(HeatupSimEngine engine)
        {
            if (engine == null) return;

            // Update each alarm tile
            _pressHighTile.SetState(engine.pressureHigh);
            _pressLowTile.SetState(engine.pressureLow);
            _pzrLevelHighTile.SetState(engine.pzrLevelHigh);
            _pzrLevelLowTile.SetState(engine.pzrLevelLow);
            _subcoolLowTile.SetState(engine.subcoolingLow);
            
            // VCT can be high or low
            bool vctAlarm = engine.vctLevelHigh || engine.vctLevelLow;
            _vctLevelTile.SetState(vctAlarm);
            
            _massConsTile.SetState(engine.primaryMassAlarm);
            _flowLowTile.SetState(engine.rcsFlowLow);
        }

        public override void UpdateVisuals()
        {
            // Alarm tiles handle their own pulse animation
        }
    }

    // ========================================================================
    // ALARM TILE - Single Annunciator Display
    // ========================================================================

    /// <summary>
    /// Single alarm annunciator tile with pulse animation.
    /// </summary>
    public class AlarmTile : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI labelText;

        private bool _isAlarmed;
        private float _pulseTime;

        private static readonly Color NormalBg = new Color32(30, 33, 41, 255);
        private static readonly Color AlarmBg = new Color32(255, 46, 46, 255);
        private static readonly Color NormalText = new Color32(100, 105, 120, 255);
        private static readonly Color AlarmText = new Color32(20, 22, 28, 255);

        void Update()
        {
            if (_isAlarmed)
            {
                // Pulse animation
                _pulseTime += Time.deltaTime * 2f;
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
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = alarmed ? AlarmBg : NormalBg;
            }

            if (labelText != null)
            {
                labelText.color = alarmed ? AlarmText : NormalText;
            }
        }

        public static AlarmTile Create(Transform parent, string label)
        {
            GameObject tileGO = new GameObject($"AlarmTile_{label}");
            tileGO.transform.SetParent(parent, false);

            // Background
            Image bg = tileGO.AddComponent<Image>();
            bg.color = NormalBg;

            // Label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(tileGO.transform, false);

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

            AlarmTile tile = tileGO.AddComponent<AlarmTile>();
            tile.backgroundImage = bg;
            tile.labelText = labelTMP;

            return tile;
        }
    }
}
