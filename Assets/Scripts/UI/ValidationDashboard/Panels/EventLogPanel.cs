// ============================================================================
// CRITICAL: Master the Atom - Event Log Panel Component
// EventLogPanel.cs - Scrollable Severity-Filtered Event Log
// ============================================================================
//
// PURPOSE:
//   Renders a scrollable, severity-filtered event log panel that reads
//   from HeatupSimEngine.eventLog (List<EventLogEntry>).
//   Supports severity filtering (ALL/INFO/ALERT/ALARM) with color coding.
//
// VISUAL DESIGN:
//   â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
//   â”‚  [ALL] [INFO] [ALERT] [ALARM]        27 events     â”‚
//   â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
//   â”‚  12:15:30 INFO   Phase â†’ HEATUP                    â”‚
//   â”‚  12:15:35 ALERT  PZR Level Low                     â”‚
//   â”‚  12:16:01 ALARM  Subcool < 20Â°F                    â”‚
//   â”‚  12:16:05 INFO   RCP #1 START COMMAND              â”‚
//   â”‚  12:16:10 ACTION HEATER MODE: STARTUP â†’ BUBBLE     â”‚
//   â”‚  ...                                     â–¼ scroll  â”‚
//   â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
//
// ARCHITECTURE:
//   Uses a pool of pre-created TextMeshProUGUI rows for visible-only
//   rendering. Scrolls by adjusting which entries are displayed, not by
//   moving a large content rect (same pattern as legacy system).
//   Zero-allocation per-frame update.
//
// VERSION: 1.0.0
// DATE: 2026-02-17
// IP: IP-0041 Stage 1
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Critical.Validation;

// Alias nested engine types for clean access
using EventSeverity = Critical.Validation.HeatupSimEngine.EventSeverity;
using EventLogEntry = Critical.Validation.HeatupSimEngine.EventLogEntry;
namespace Critical.UI.ValidationDashboard
{
    /// <summary>
    /// Scrollable event log panel with severity filtering.
    /// Reads from engine.eventLog (List of EventLogEntry).
    /// </summary>
    public class EventLogPanel : MonoBehaviour
    {
        // ====================================================================
        // SEVERITY FILTER
        // ====================================================================

        public enum FilterMode
        {
            All,
            Info,
            Alert,
            Alarm
        }

        // ====================================================================
        // CONFIGURATION
        // ====================================================================

        private const float ROW_HEIGHT = 16f;
        private const float FONT_SIZE = 11f;
        private const int MAX_VISIBLE_ROWS = 60;
        private const float SCROLL_SENSITIVITY = 3f;

        // ====================================================================
        // STATE
        // ====================================================================

        private FilterMode _filter = FilterMode.All;
        private List<EventLogEntry> _sourceLog;
        private readonly List<int> _filteredIndices = new List<int>(256);
        private int _scrollOffset;                // Index into _filteredIndices for top visible row
        private bool _autoScroll = true;          // Follow newest entries
        private int _lastKnownLogCount;

        // UI elements
        private TextMeshProUGUI[] _rowTexts;
        private RectTransform _contentArea;
        private TextMeshProUGUI _countLabel;
        private Image[] _filterButtons;
        private int _visibleRowCount;

        // Filter button references for highlighting
        private Image _btnAll, _btnInfo, _btnAlert, _btnAlarm;

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Set the event log source. Call once during setup.
        /// </summary>
        public void SetSource(List<EventLogEntry> log)
        {
            _sourceLog = log;
            _lastKnownLogCount = 0;
            RebuildFilteredIndices();
        }

        /// <summary>
        /// Set the severity filter.
        /// </summary>
        public void SetFilter(FilterMode filter)
        {
            _filter = filter;
            _scrollOffset = 0;
            _autoScroll = true;
            RebuildFilteredIndices();
            UpdateFilterButtonHighlight();
            RefreshDisplay();
        }

        /// <summary>
        /// Scroll to the bottom (newest entries).
        /// </summary>
        public void ScrollToBottom()
        {
            _autoScroll = true;
            int maxOffset = Mathf.Max(0, _filteredIndices.Count - _visibleRowCount);
            _scrollOffset = maxOffset;
            RefreshDisplay();
        }

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        void Update()
        {
            if (_sourceLog == null) return;

            // Check for new entries
            if (_sourceLog.Count != _lastKnownLogCount)
            {
                _lastKnownLogCount = _sourceLog.Count;
                RebuildFilteredIndices();

                if (_autoScroll)
                {
                    _scrollOffset = Mathf.Max(0, _filteredIndices.Count - _visibleRowCount);
                }

                RefreshDisplay();
            }

            // Update count label
            if (_countLabel != null)
            {
                _countLabel.text = $"{_filteredIndices.Count} events";
            }
        }

        // ====================================================================
        // SCROLL HANDLING
        // ====================================================================

        /// <summary>
        /// Handle scroll input. Called from parent or via scroll rect.
        /// </summary>
        public void OnScroll(float delta)
        {
            int scrollAmount = Mathf.RoundToInt(-delta * SCROLL_SENSITIVITY);
            int maxOffset = Mathf.Max(0, _filteredIndices.Count - _visibleRowCount);

            _scrollOffset = Mathf.Clamp(_scrollOffset + scrollAmount, 0, maxOffset);
            _autoScroll = (_scrollOffset >= maxOffset);
            RefreshDisplay();
        }

        // ====================================================================
        // FILTERING
        // ====================================================================

        void RebuildFilteredIndices()
        {
            _filteredIndices.Clear();
            if (_sourceLog == null) return;

            for (int i = 0; i < _sourceLog.Count; i++)
            {
                if (PassesFilter(_sourceLog[i].Severity))
                {
                    _filteredIndices.Add(i);
                }
            }
        }

        bool PassesFilter(EventSeverity severity)
        {
            switch (_filter)
            {
                case FilterMode.All:
                    return true;
                case FilterMode.Info:
                    return severity == EventSeverity.INFO || severity == EventSeverity.ACTION;
                case FilterMode.Alert:
                    return severity == EventSeverity.ALERT;
                case FilterMode.Alarm:
                    return severity == EventSeverity.ALARM;
                default:
                    return true;
            }
        }

        // ====================================================================
        // DISPLAY
        // ====================================================================

        void RefreshDisplay()
        {
            if (_rowTexts == null) return;

            for (int row = 0; row < _visibleRowCount; row++)
            {
                int filteredIdx = _scrollOffset + row;
                if (filteredIdx < 0 || filteredIdx >= _filteredIndices.Count)
                {
                    _rowTexts[row].text = "";
                    continue;
                }

                int sourceIdx = _filteredIndices[filteredIdx];
                if (sourceIdx < 0 || sourceIdx >= _sourceLog.Count)
                {
                    _rowTexts[row].text = "";
                    continue;
                }

                EventLogEntry entry = _sourceLog[sourceIdx];
                _rowTexts[row].text = entry.FormattedLine;
                _rowTexts[row].color = GetSeverityColor(entry.Severity);
            }
        }

        static Color GetSeverityColor(EventSeverity severity)
        {
            switch (severity)
            {
                case EventSeverity.INFO:
                    return ValidationDashboardTheme.TextSecondary;  // Gray
                case EventSeverity.ACTION:
                    return ValidationDashboardTheme.InfoCyan;       // Cyan
                case EventSeverity.ALERT:
                    return ValidationDashboardTheme.WarningAmber;   // Amber
                case EventSeverity.ALARM:
                    return ValidationDashboardTheme.AlarmRed;       // Red
                default:
                    return ValidationDashboardTheme.TextPrimary;
            }
        }

        void UpdateFilterButtonHighlight()
        {
            Color active = ValidationDashboardTheme.AccentBlue;
            Color inactive = ValidationDashboardTheme.BackgroundSection;

            if (_btnAll != null) _btnAll.color = (_filter == FilterMode.All) ? active : inactive;
            if (_btnInfo != null) _btnInfo.color = (_filter == FilterMode.Info) ? active : inactive;
            if (_btnAlert != null) _btnAlert.color = (_filter == FilterMode.Alert) ? active : inactive;
            if (_btnAlarm != null) _btnAlarm.color = (_filter == FilterMode.Alarm) ? active : inactive;
        }

        // ====================================================================
        // FACTORY METHOD
        // ====================================================================

        /// <summary>
        /// Create an EventLogPanel with filter buttons and scrollable row display.
        /// </summary>
        public static EventLogPanel Create(Transform parent, float width = 400f, float height = 300f)
        {
            // Container
            GameObject container = new GameObject("EventLogPanel");
            container.transform.SetParent(parent, false);
            RectTransform containerRT = container.AddComponent<RectTransform>();
            containerRT.sizeDelta = new Vector2(width, height);

            Image bg = container.AddComponent<Image>();
            bg.color = ValidationDashboardTheme.BackgroundPanel;
            bg.raycastTarget = true;    // For scroll events

            EventLogPanel panel = container.AddComponent<EventLogPanel>();

            // ---- Filter bar (top 24px) ----
            float filterBarHeight = 24f;
            GameObject filterBar = new GameObject("FilterBar");
            filterBar.transform.SetParent(container.transform, false);
            RectTransform filterBarRT = filterBar.AddComponent<RectTransform>();
            filterBarRT.anchorMin = new Vector2(0f, 1f);
            filterBarRT.anchorMax = new Vector2(1f, 1f);
            filterBarRT.offsetMin = new Vector2(0f, -filterBarHeight);
            filterBarRT.offsetMax = Vector2.zero;

            Image filterBarBg = filterBar.AddComponent<Image>();
            filterBarBg.color = ValidationDashboardTheme.BackgroundHeader;
            filterBarBg.raycastTarget = false;

            HorizontalLayoutGroup filterLayout = filterBar.AddComponent<HorizontalLayoutGroup>();
            filterLayout.spacing = 4f;
            filterLayout.padding = new RectOffset(4, 4, 2, 2);
            filterLayout.childAlignment = TextAnchor.MiddleLeft;
            filterLayout.childControlWidth = false;
            filterLayout.childControlHeight = true;
            filterLayout.childForceExpandWidth = false;
            filterLayout.childForceExpandHeight = true;

            panel._btnAll = CreateFilterButton(filterBar.transform, "ALL", 40f,
                () => panel.SetFilter(FilterMode.All));
            panel._btnInfo = CreateFilterButton(filterBar.transform, "INFO", 40f,
                () => panel.SetFilter(FilterMode.Info));
            panel._btnAlert = CreateFilterButton(filterBar.transform, "ALERT", 48f,
                () => panel.SetFilter(FilterMode.Alert));
            panel._btnAlarm = CreateFilterButton(filterBar.transform, "ALARM", 48f,
                () => panel.SetFilter(FilterMode.Alarm));

            // Count label (right side of filter bar)
            GameObject countGO = new GameObject("CountLabel");
            countGO.transform.SetParent(filterBar.transform, false);
            LayoutElement countLE = countGO.AddComponent<LayoutElement>();
            countLE.flexibleWidth = 1f;
            panel._countLabel = countGO.AddComponent<TextMeshProUGUI>();
            panel._countLabel.text = "0 events";
            panel._countLabel.fontSize = 10f;
            panel._countLabel.alignment = TextAlignmentOptions.MidlineRight;
            panel._countLabel.color = ValidationDashboardTheme.TextSecondary;

            // ---- Content area (below filter bar) ----
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(container.transform, false);
            RectTransform contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = Vector2.zero;
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.offsetMin = new Vector2(4f, 2f);
            contentRT.offsetMax = new Vector2(-4f, -filterBarHeight - 2f);
            panel._contentArea = contentRT;

            // Calculate visible rows
            float contentHeight = height - filterBarHeight - 4f;
            panel._visibleRowCount = Mathf.Min(MAX_VISIBLE_ROWS,
                Mathf.FloorToInt(contentHeight / ROW_HEIGHT));

            // Create row text objects (pooled â€” reused each frame)
            panel._rowTexts = new TextMeshProUGUI[panel._visibleRowCount];
            for (int i = 0; i < panel._visibleRowCount; i++)
            {
                GameObject rowGO = new GameObject($"Row_{i}");
                rowGO.transform.SetParent(contentGO.transform, false);
                RectTransform rowRT = rowGO.AddComponent<RectTransform>();
                rowRT.anchorMin = new Vector2(0f, 1f);
                rowRT.anchorMax = new Vector2(1f, 1f);
                rowRT.offsetMin = new Vector2(0f, -(i + 1) * ROW_HEIGHT);
                rowRT.offsetMax = new Vector2(0f, -i * ROW_HEIGHT);

                panel._rowTexts[i] = rowGO.AddComponent<TextMeshProUGUI>();
                panel._rowTexts[i].fontSize = FONT_SIZE;
                panel._rowTexts[i].alignment = TextAlignmentOptions.MidlineLeft;
                panel._rowTexts[i].color = ValidationDashboardTheme.TextSecondary;
                panel._rowTexts[i].enableWordWrapping = false;
                panel._rowTexts[i].overflowMode = TextOverflowModes.Truncate;
                panel._rowTexts[i].raycastTarget = false;

                // Use a monospace-friendly font style for log alignment
                panel._rowTexts[i].fontStyle = FontStyles.Normal;
            }

            panel.UpdateFilterButtonHighlight();
            return panel;
        }

        static Image CreateFilterButton(Transform parent, string label, float width,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGO = new GameObject($"Btn_{label}");
            btnGO.transform.SetParent(parent, false);

            LayoutElement le = btnGO.AddComponent<LayoutElement>();
            le.preferredWidth = width;

            Image btnImg = btnGO.AddComponent<Image>();
            btnImg.color = ValidationDashboardTheme.BackgroundSection;

            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(onClick);

            // Button navigation disabled (no tab navigation in dashboard)
            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 10f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = ValidationDashboardTheme.TextPrimary;
            text.raycastTarget = false;

            return btnImg;
        }
    }
}

