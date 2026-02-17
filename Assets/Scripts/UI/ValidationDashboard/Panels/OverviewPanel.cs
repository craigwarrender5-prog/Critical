// ============================================================================
// CRITICAL: Master the Atom - Overview Panel
// OverviewPanel.cs - Main At-a-Glance Dashboard View
// ============================================================================
//
// PURPOSE:
//   The primary dashboard view showing all critical parameters at a glance
//   without requiring tab navigation. Displays:
//   - Global simulation health (mass/energy conservation)
//   - Reactor core temperatures (Tavg, Thot, Tcold, ΔT)
//   - RCS status (pressure, subcooling, RCP status)
//   - Pressurizer state (level, pressure, heater/spray)
//   - CVCS flows (charging, letdown, net)
//   - Steam generator status
//   - Alarm summary
//
// LAYOUT (IP-0040):
//   ┌──────────────────────────────────────────────────────────────────┐
//   │  REACTOR / RCS (1.2)  │  PRESSURIZER (1.0)  │  CVCS / SG (1.0)    │
//   │  [ArcGauges]          │  [ArcGauge]          │  [DigitalReadouts]   │
//   │  Press  Tavg           │  PZR Level           │  Charging  Letdown   │
//   │  Subcool  ΔT           │  [Status] [Status]   │  [BiGauge] Net CVCS  │
//   │  [RCP1][RCP2][3][4]   │  PZR Temp  Surge     │  SG Press  Net Heat  │
//   ├──────────────────────────────────────────────────────────────────┤
//   │  SYSTEM HEALTH (0.8)    │  ALARM ANNUNCIATOR (1.5)                    │
//   │  [Digital] Mass Error   │  ┌────┬────┬────┬────┬────┬────┐            │
//   │  [Status] Mass Cons     │  │PRHI│PRLO│LVHI│LVLO│SUBC│VCT │            │
//   │  [Status] SG Boiling    │  ├────┼────┼────┼────┼────┼────┤            │
//   │  [Status] RHR           │  │MASS│FLOW│SGP │HTRS│SPRY│BBLE│            │
//   └──────────────────────────────────────────────────────────────────┘
//
// VERSION: 2.0.0
// DATE: 2026-02-17
// IP: IP-0040 Stage 2
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Overview panel showing all critical parameters at a glance.
    /// </summary>
    public class OverviewPanel : ValidationPanelBase
    {
        // ====================================================================
        // PANEL IDENTITY
        // ====================================================================

        public override string PanelName => "OverviewPanel";
        public override int TabIndex => 0;  // First tab

        // ====================================================================
        // SECTION REFERENCES
        // ====================================================================

        [Header("Section Containers")]
        [SerializeField] private RectTransform topRow;
        [SerializeField] private RectTransform bottomRow;

        // Section scripts — IP-0040 merged layout
        private OverviewSection_ReactorRCS _reactorRcsSection;
        private OverviewSection_Pressurizer _pressurizerSection;
        private OverviewSection_CVCSG _cvcsgSection;
        private OverviewSection_SystemHealth _systemHealthSection;
        private OverviewSection_Alarms _alarmsSection;

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        protected override void OnInitialize()
        {
            // Build the panel layout if not already set up
            if (topRow == null || bottomRow == null)
            {
                BuildLayout();
            }

            // Initialize all sections
            InitializeSections();
        }

        private void BuildLayout()
        {
            // IP-0040: Proportional 3+2 layout
            // Top row (60%): Reactor/RCS, Pressurizer, CVCS/SG
            // Bottom row (40%): System Health, Alarm Annunciator Grid

            // Top row container — 60% of panel height
            GameObject topRowGO = new GameObject("TopRow");
            topRowGO.transform.SetParent(transform, false);
            topRow = topRowGO.AddComponent<RectTransform>();
            topRow.anchorMin = new Vector2(0, 0.40f);
            topRow.anchorMax = new Vector2(1, 1);
            topRow.offsetMin = new Vector2(0, 4);
            topRow.offsetMax = new Vector2(0, 0);

            HorizontalLayoutGroup topLayout = topRowGO.AddComponent<HorizontalLayoutGroup>();
            topLayout.childAlignment = TextAnchor.UpperLeft;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = true;
            topLayout.childForceExpandHeight = true;
            topLayout.spacing = 8;
            topLayout.padding = new RectOffset(6, 6, 4, 4);

            // Bottom row container — 40% of panel height
            GameObject bottomRowGO = new GameObject("BottomRow");
            bottomRowGO.transform.SetParent(transform, false);
            bottomRow = bottomRowGO.AddComponent<RectTransform>();
            bottomRow.anchorMin = new Vector2(0, 0);
            bottomRow.anchorMax = new Vector2(1, 0.40f);
            bottomRow.offsetMin = new Vector2(0, 0);
            bottomRow.offsetMax = new Vector2(0, -4);

            HorizontalLayoutGroup bottomLayout = bottomRowGO.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.childAlignment = TextAnchor.UpperLeft;
            bottomLayout.childControlWidth = true;
            bottomLayout.childControlHeight = true;
            bottomLayout.childForceExpandWidth = true;
            bottomLayout.childForceExpandHeight = true;
            bottomLayout.spacing = 8;
            bottomLayout.padding = new RectOffset(6, 6, 4, 4);
        }

        private void InitializeSections()
        {
            // IP-0040: 3+2 proportional section layout
            // Top row: Reactor/RCS (wider), Pressurizer, CVCS/SG
            _reactorRcsSection = CreateSection<OverviewSection_ReactorRCS>(topRow, "REACTOR / RCS", 1.2f);
            _pressurizerSection = CreateSection<OverviewSection_Pressurizer>(topRow, "PRESSURIZER", 1.0f);
            _cvcsgSection = CreateSection<OverviewSection_CVCSG>(topRow, "CVCS / SG", 1.0f);

            // Bottom row: System Health (narrow), Alarm Annunciator (wide)
            _systemHealthSection = CreateSection<OverviewSection_SystemHealth>(bottomRow, "SYSTEM HEALTH", 0.8f);
            _alarmsSection = CreateSection<OverviewSection_Alarms>(bottomRow, "ALARM ANNUNCIATOR", 1.5f);
        }

        private T CreateSection<T>(Transform parent, string title, float flexWeight) where T : OverviewSectionBase
        {
            GameObject sectionGO = new GameObject(typeof(T).Name);
            sectionGO.transform.SetParent(parent, false);

            // Layout element for flex sizing
            LayoutElement le = sectionGO.AddComponent<LayoutElement>();
            le.flexibleWidth = flexWeight;
            le.minWidth = 120;

            // Background
            Image bg = sectionGO.AddComponent<Image>();
            bg.color = ValidationDashboardTheme.BackgroundSection;

            // Vertical layout
            VerticalLayoutGroup layout = sectionGO.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4;
            layout.padding = new RectOffset(6, 6, 4, 6);

            // Section header
            GameObject headerGO = new GameObject("Header");
            headerGO.transform.SetParent(sectionGO.transform, false);

            LayoutElement headerLE = headerGO.AddComponent<LayoutElement>();
            headerLE.preferredHeight = ValidationDashboardTheme.SectionHeaderHeight;
            headerLE.minHeight = ValidationDashboardTheme.SectionHeaderHeight;

            TextMeshProUGUI headerText = headerGO.AddComponent<TextMeshProUGUI>();
            headerText.text = title;
            headerText.fontSize = 11;
            headerText.fontStyle = FontStyles.Bold;
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.color = ValidationDashboardTheme.TextSecondary;

            // Content container
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(sectionGO.transform, false);

            LayoutElement contentLE = contentGO.AddComponent<LayoutElement>();
            contentLE.flexibleHeight = 1;

            // Add the section component
            T section = sectionGO.AddComponent<T>();
            section.Initialize(contentGO.transform);

            return section;
        }

        // ====================================================================
        // DATA UPDATE
        // ====================================================================

        protected override void OnUpdateData()
        {
            if (Engine == null) return;

            _reactorRcsSection?.UpdateData(Engine);
            _pressurizerSection?.UpdateData(Engine);
            _cvcsgSection?.UpdateData(Engine);
            _systemHealthSection?.UpdateData(Engine);
            _alarmsSection?.UpdateData(Engine);
        }

        // ====================================================================
        // VISUAL UPDATE
        // ====================================================================

        protected override void OnUpdateVisuals()
        {
            _reactorRcsSection?.UpdateVisuals();
            _pressurizerSection?.UpdateVisuals();
            _cvcsgSection?.UpdateVisuals();
            _systemHealthSection?.UpdateVisuals();
            _alarmsSection?.UpdateVisuals();
        }
    }

    // ========================================================================
    // BASE CLASS FOR OVERVIEW SECTIONS
    // ========================================================================

    /// <summary>
    /// Base class for overview panel sections.
    /// </summary>
    public abstract class OverviewSectionBase : MonoBehaviour
    {
        protected Transform ContentRoot { get; private set; }
        protected List<ParameterRow> Rows { get; } = new List<ParameterRow>();

        public virtual void Initialize(Transform contentRoot)
        {
            ContentRoot = contentRoot;
            BuildContent();
        }

        protected abstract void BuildContent();

        public abstract void UpdateData(HeatupSimEngine engine);

        public virtual void UpdateVisuals()
        {
            foreach (var row in Rows)
            {
                row.UpdateVisuals();
            }
        }

        /// <summary>
        /// Create a parameter row with label and value.
        /// </summary>
        protected ParameterRow CreateRow(string label, string unit = "", string format = "F1")
        {
            var row = ParameterRow.Create(ContentRoot, label, unit, format);
            Rows.Add(row);
            return row;
        }

        /// <summary>
        /// Create a status row with label and indicator.
        /// </summary>
        protected StatusRow CreateStatusRow(string label)
        {
            return StatusRow.Create(ContentRoot, label);
        }
    }

    // ========================================================================
    // PARAMETER ROW - Label + Value display
    // ========================================================================

    /// <summary>
    /// A single parameter display row with label, value, and optional unit.
    /// </summary>
    public class ParameterRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI valueText;

        private float _targetValue;
        private float _displayValue;
        private Color _targetColor;
        private Color _displayColor;
        private string _format = "F1";
        private string _unit = "";
        private bool _useThresholds;
        private float _warnLow, _warnHigh, _alarmLow, _alarmHigh;

        public void SetValue(float value)
        {
            _targetValue = value;
            _displayValue = value; // Instant update for now
            UpdateColor();
            UpdateText();
        }

        public void SetValueWithThresholds(float value, float warnLow, float warnHigh, float alarmLow, float alarmHigh)
        {
            _useThresholds = true;
            _warnLow = warnLow;
            _warnHigh = warnHigh;
            _alarmLow = alarmLow;
            _alarmHigh = alarmHigh;
            SetValue(value);
        }

        public void SetText(string text)
        {
            if (valueText != null)
                valueText.text = text;
        }

        public void SetColor(Color color)
        {
            _useThresholds = false;
            _targetColor = color;
            _displayColor = color;
            if (valueText != null)
                valueText.color = color;
        }

        private void UpdateColor()
        {
            if (!_useThresholds)
            {
                _targetColor = ValidationDashboardTheme.TextPrimary;
            }
            else
            {
                _targetColor = ValidationDashboardTheme.GetThresholdColor(
                    _displayValue, _warnLow, _warnHigh, _alarmLow, _alarmHigh);
            }
            _displayColor = _targetColor;
        }

        private void UpdateText()
        {
            if (valueText != null)
            {
                valueText.text = _displayValue.ToString(_format) + _unit;
                valueText.color = _displayColor;
            }
        }

        public void UpdateVisuals()
        {
            // Future: smooth color transitions
        }

        public static ParameterRow Create(Transform parent, string label, string unit = "", string format = "F1")
        {
            GameObject rowGO = new GameObject($"Row_{label}");
            rowGO.transform.SetParent(parent, false);

            LayoutElement le = rowGO.AddComponent<LayoutElement>();
            le.preferredHeight = ValidationDashboardTheme.StatusRowHeight;
            le.minHeight = ValidationDashboardTheme.StatusRowHeight;

            HorizontalLayoutGroup layout = rowGO.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4;

            // Label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);

            LayoutElement labelLE = labelGO.AddComponent<LayoutElement>();
            labelLE.flexibleWidth = 1;

            TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 11;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            labelTMP.color = ValidationDashboardTheme.TextSecondary;

            // Value
            GameObject valueGO = new GameObject("Value");
            valueGO.transform.SetParent(rowGO.transform, false);

            LayoutElement valueLE = valueGO.AddComponent<LayoutElement>();
            valueLE.flexibleWidth = 1;
            valueLE.minWidth = 60;

            TextMeshProUGUI valueTMP = valueGO.AddComponent<TextMeshProUGUI>();
            valueTMP.text = "---";
            valueTMP.fontSize = 12;
            valueTMP.fontStyle = FontStyles.Bold;
            valueTMP.alignment = TextAlignmentOptions.MidlineRight;
            valueTMP.color = ValidationDashboardTheme.TextPrimary;

            ParameterRow row = rowGO.AddComponent<ParameterRow>();
            row.labelText = labelTMP;
            row.valueText = valueTMP;
            row._format = format;
            row._unit = unit;

            return row;
        }
    }

    // ========================================================================
    // STATUS ROW - Label + Status Indicator
    // ========================================================================

    /// <summary>
    /// A status display row with label and colored indicator.
    /// </summary>
    public class StatusRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Image statusImage;
        [SerializeField] private TextMeshProUGUI statusText;

        public void SetStatus(bool active, bool isAlarm = false)
        {
            Color c;
            string text;

            if (isAlarm && active)
            {
                c = ValidationDashboardTheme.AlarmRed;
                text = "ALARM";
            }
            else if (active)
            {
                c = ValidationDashboardTheme.NormalGreen;
                text = "OK";
            }
            else
            {
                c = ValidationDashboardTheme.Neutral;
                text = "OFF";
            }

            if (statusImage != null)
                statusImage.color = c;
            if (statusText != null)
            {
                statusText.text = text;
                statusText.color = c;
            }
        }

        public void SetStatus(string text, bool active)
        {
            Color c = active ? ValidationDashboardTheme.NormalGreen : ValidationDashboardTheme.WarningAmber;

            if (statusImage != null)
                statusImage.color = c;

            if (statusText != null)
            {
                statusText.text = text;
                statusText.color = c;
            }
        }

        public void SetStatusText(string text, Color color)
        {
            if (statusText != null)
            {
                statusText.text = text;
                statusText.color = color;
            }
            if (statusImage != null)
                statusImage.color = color;
        }

        public static StatusRow Create(Transform parent, string label)
        {
            GameObject rowGO = new GameObject($"StatusRow_{label}");
            rowGO.transform.SetParent(parent, false);

            LayoutElement le = rowGO.AddComponent<LayoutElement>();
            le.preferredHeight = ValidationDashboardTheme.StatusRowHeight;
            le.minHeight = ValidationDashboardTheme.StatusRowHeight;

            HorizontalLayoutGroup layout = rowGO.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4;

            // Label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);

            LayoutElement labelLE = labelGO.AddComponent<LayoutElement>();
            labelLE.flexibleWidth = 1;

            TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 11;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            labelTMP.color = ValidationDashboardTheme.TextSecondary;

            // Status indicator
            GameObject statusGO = new GameObject("Status");
            statusGO.transform.SetParent(rowGO.transform, false);

            LayoutElement statusLE = statusGO.AddComponent<LayoutElement>();
            statusLE.preferredWidth = 50;
            statusLE.minWidth = 50;

            Image statusImg = statusGO.AddComponent<Image>();
            statusImg.color = ValidationDashboardTheme.Neutral;

            // Status text (inside indicator)
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(statusGO.transform, false);

            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            TextMeshProUGUI textTMP = textGO.AddComponent<TextMeshProUGUI>();
            textTMP.text = "---";
            textTMP.fontSize = 9;
            textTMP.fontStyle = FontStyles.Bold;
            textTMP.alignment = TextAlignmentOptions.Center;
            textTMP.color = ValidationDashboardTheme.TextDark;

            StatusRow row = rowGO.AddComponent<StatusRow>();
            row.labelText = labelTMP;
            row.statusImage = statusImg;
            row.statusText = textTMP;

            return row;
        }
    }
}
