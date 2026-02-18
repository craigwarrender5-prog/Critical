// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Validation Dashboard V2
// UITKDashboardV2Controller.LogTab.cs — Full Event Log Tab (Tab 7)
// ============================================================================
//
// PURPOSE:
//   Builds the LOG Tab (Tab 7) — Full-screen scrollable event log viewer:
//     - Filter buttons: [ALL] [INFO] [ACTION] [ALERT] [ALARM]
//     - Color-coded entries by severity (cyan/green/amber/red)
//     - Monospaced font for alignment
//     - Auto-scroll with manual override (pauses when user scrolls up)
//     - Summary bar showing total/filtered counts
//
// DATA SOURCE:
//   engine.eventLog (List<EventLogEntry>, max 200 entries)
//   EventLogEntry.FormattedLine provides pre-formatted display string.
//
// DATA BINDING:
//   RefreshLogTab() at 5Hz when tab 7 is active.
//   Rebuilds visible entries only when log count changes or filter changes.
//
// IP: IP-0060 Stage 8C
// VERSION: 6.0.0
// DATE: 2026-02-18
// ============================================================================

using System.Collections.Generic;
using Critical.Validation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.UIToolkit.ValidationDashboard
{
    public partial class UITKDashboardV2Controller
    {
        // ====================================================================
        // LOG TAB — REFERENCES
        // ====================================================================

        private ScrollView _log_scrollView;
        private VisualElement _log_entryContainer;
        private Label _log_summaryLabel;

        // Filter state
        private enum LogFilter { All, Info, Action, Alert, Alarm }
        private LogFilter _log_activeFilter = LogFilter.All;
        private readonly List<Button> _log_filterButtons = new List<Button>(5);

        // Track log size to avoid unnecessary rebuilds
        private int _log_lastKnownCount;
        private LogFilter _log_lastAppliedFilter = LogFilter.All;

        // Auto-scroll state
        private bool _log_autoScroll = true;

        // Severity colors
        private static readonly Color LOG_COLOR_INFO   = new Color(0.4f, 0.85f, 1f, 1f);
        private static readonly Color LOG_COLOR_ACTION = new Color(0.3f, 0.9f, 0.4f, 1f);
        private static readonly Color LOG_COLOR_ALERT  = new Color(1f, 0.78f, 0.2f, 1f);
        private static readonly Color LOG_COLOR_ALARM  = new Color(1f, 0.45f, 0.45f, 1f);

        // Filter button colors
        private static readonly Color LOG_BTN_ACTIVE_BG   = new Color(0.15f, 0.22f, 0.35f, 1f);
        private static readonly Color LOG_BTN_INACTIVE_BG = new Color(0.06f, 0.075f, 0.11f, 1f);
        private static readonly Color LOG_BTN_ACTIVE_FG   = new Color(0.9f, 0.95f, 1f, 1f);
        private static readonly Color LOG_BTN_INACTIVE_FG = new Color(0.5f, 0.55f, 0.65f, 1f);

        // ====================================================================
        // BUILD
        // ====================================================================

        private VisualElement BuildLogTab()
        {
            var root = MakeTabRoot("v2-tab-log");

            // ── Header row: Title + Filter buttons + Summary ────────────
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 4f;
            headerRow.style.paddingLeft = 6f;
            headerRow.style.paddingRight = 6f;
            headerRow.style.flexShrink = 0f;

            var titleLabel = MakeLabel("EVENT LOG", 12f, FontStyle.Bold,
                new Color(0.73f, 0.8f, 0.9f, 1f));
            headerRow.Add(titleLabel);

            // ── Filter button group ─────────────────────────────────────
            var filterGroup = new VisualElement();
            filterGroup.style.flexDirection = FlexDirection.Row;
            filterGroup.style.alignItems = Align.Center;

            _log_filterButtons.Clear();
            filterGroup.Add(MakeLogFilterButton("ALL",    LogFilter.All));
            filterGroup.Add(MakeLogFilterButton("INFO",   LogFilter.Info));
            filterGroup.Add(MakeLogFilterButton("ACTION", LogFilter.Action));
            filterGroup.Add(MakeLogFilterButton("ALERT",  LogFilter.Alert));
            filterGroup.Add(MakeLogFilterButton("ALARM",  LogFilter.Alarm));
            headerRow.Add(filterGroup);

            // ── Summary label ───────────────────────────────────────────
            _log_summaryLabel = MakeLabel("0 entries", 10f, FontStyle.Normal,
                UITKDashboardTheme.TextSecondary);
            headerRow.Add(_log_summaryLabel);

            root.Add(headerRow);

            // ── Scroll view with event entries ──────────────────────────
            _log_scrollView = new ScrollView(ScrollViewMode.Vertical);
            _log_scrollView.style.flexGrow = 1f;
            _log_scrollView.style.backgroundColor = new Color(0.03f, 0.035f, 0.055f, 1f);
            _log_scrollView.style.borderTopWidth = 1f;
            _log_scrollView.style.borderBottomWidth = 1f;
            _log_scrollView.style.borderLeftWidth = 1f;
            _log_scrollView.style.borderRightWidth = 1f;
            var borderColor = new Color(0.118f, 0.173f, 0.271f, 1f);
            _log_scrollView.style.borderTopColor = borderColor;
            _log_scrollView.style.borderBottomColor = borderColor;
            _log_scrollView.style.borderLeftColor = borderColor;
            _log_scrollView.style.borderRightColor = borderColor;
            _log_scrollView.style.paddingTop = 4f;
            _log_scrollView.style.paddingBottom = 4f;
            _log_scrollView.style.paddingLeft = 6f;
            _log_scrollView.style.paddingRight = 6f;

            _log_entryContainer = new VisualElement();
            _log_entryContainer.style.flexDirection = FlexDirection.Column;
            _log_scrollView.Add(_log_entryContainer);

            // Detect manual scroll to pause auto-scroll
            _log_scrollView.verticalScroller.valueChanged += OnLogScrollChanged;

            root.Add(_log_scrollView);

            // ── Footer: auto-scroll indicator ───────────────────────────
            var footerRow = new VisualElement();
            footerRow.style.flexDirection = FlexDirection.Row;
            footerRow.style.justifyContent = Justify.FlexEnd;
            footerRow.style.alignItems = Align.Center;
            footerRow.style.marginTop = 2f;
            footerRow.style.paddingRight = 6f;
            footerRow.style.flexShrink = 0f;

            var scrollBtn = new Button(() =>
            {
                _log_autoScroll = true;
                ForceLogScrollToBottom();
            });
            scrollBtn.text = "▼ AUTO-SCROLL";
            scrollBtn.style.fontSize = 9f;
            scrollBtn.style.color = UITKDashboardTheme.InfoCyan;
            scrollBtn.style.backgroundColor = new Color(0.06f, 0.075f, 0.11f, 1f);
            scrollBtn.style.borderTopWidth = 1f;
            scrollBtn.style.borderBottomWidth = 1f;
            scrollBtn.style.borderLeftWidth = 1f;
            scrollBtn.style.borderRightWidth = 1f;
            scrollBtn.style.borderTopColor = new Color(0.15f, 0.22f, 0.35f, 1f);
            scrollBtn.style.borderBottomColor = new Color(0.15f, 0.22f, 0.35f, 1f);
            scrollBtn.style.borderLeftColor = new Color(0.15f, 0.22f, 0.35f, 1f);
            scrollBtn.style.borderRightColor = new Color(0.15f, 0.22f, 0.35f, 1f);
            scrollBtn.style.paddingLeft = 8f;
            scrollBtn.style.paddingRight = 8f;
            scrollBtn.style.paddingTop = 2f;
            scrollBtn.style.paddingBottom = 2f;
            SetCornerRadius(scrollBtn, 3f);
            footerRow.Add(scrollBtn);

            root.Add(footerRow);

            // Initialize tracking
            _log_lastKnownCount = 0;
            _log_lastAppliedFilter = LogFilter.All;
            _log_activeFilter = LogFilter.All;
            _log_autoScroll = true;
            UpdateFilterButtonStyles();

            return root;
        }

        // ====================================================================
        // FILTER BUTTON FACTORY
        // ====================================================================

        private Button MakeLogFilterButton(string label, LogFilter filter)
        {
            var btn = new Button(() =>
            {
                _log_activeFilter = filter;
                UpdateFilterButtonStyles();
                RebuildLogEntries();
            });
            btn.text = label;
            btn.style.fontSize = 9f;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.marginRight = 3f;
            btn.style.paddingLeft = 8f;
            btn.style.paddingRight = 8f;
            btn.style.paddingTop = 3f;
            btn.style.paddingBottom = 3f;
            btn.style.borderTopWidth = 1f;
            btn.style.borderBottomWidth = 1f;
            btn.style.borderLeftWidth = 1f;
            btn.style.borderRightWidth = 1f;
            SetCornerRadius(btn, 3f);

            btn.userData = filter;
            _log_filterButtons.Add(btn);
            return btn;
        }

        private void UpdateFilterButtonStyles()
        {
            foreach (var btn in _log_filterButtons)
            {
                if (btn.userData is LogFilter f && f == _log_activeFilter)
                {
                    btn.style.backgroundColor = LOG_BTN_ACTIVE_BG;
                    btn.style.color = LOG_BTN_ACTIVE_FG;
                    btn.style.borderTopColor = UITKDashboardTheme.AccentBlue;
                    btn.style.borderBottomColor = UITKDashboardTheme.AccentBlue;
                    btn.style.borderLeftColor = UITKDashboardTheme.AccentBlue;
                    btn.style.borderRightColor = UITKDashboardTheme.AccentBlue;
                }
                else
                {
                    btn.style.backgroundColor = LOG_BTN_INACTIVE_BG;
                    btn.style.color = LOG_BTN_INACTIVE_FG;
                    var bdr = new Color(0.15f, 0.2f, 0.3f, 1f);
                    btn.style.borderTopColor = bdr;
                    btn.style.borderBottomColor = bdr;
                    btn.style.borderLeftColor = bdr;
                    btn.style.borderRightColor = bdr;
                }
            }
        }

        // ====================================================================
        // LOG ENTRY REBUILD
        // ====================================================================

        private void RebuildLogEntries()
        {
            if (_log_entryContainer == null || engine == null) return;

            _log_entryContainer.Clear();

            var log = engine.eventLog;
            int total = log.Count;
            int shown = 0;

            for (int i = 0; i < total; i++)
            {
                var entry = log[i];

                if (!PassesFilter(entry.Severity))
                    continue;

                var line = new Label(entry.FormattedLine);
                line.style.fontSize = 11f;
                line.style.unityFontStyleAndWeight = FontStyle.Normal;
                line.style.unityFontDefinition = FontDefinition.FromFont(
                    Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
                line.style.color = GetSeverityColor(entry.Severity);
                line.style.marginBottom = 1f;
                line.style.paddingTop = 1f;
                line.style.paddingBottom = 1f;
                line.style.whiteSpace = WhiteSpace.NoWrap;

                // Subtle alternating row background
                if (shown % 2 == 1)
                    line.style.backgroundColor = new Color(0.04f, 0.047f, 0.07f, 1f);

                _log_entryContainer.Add(line);
                shown++;
            }

            _log_lastKnownCount = total;
            _log_lastAppliedFilter = _log_activeFilter;

            // Update summary
            if (_log_summaryLabel != null)
            {
                string filterText = _log_activeFilter == LogFilter.All
                    ? $"{total} entries"
                    : $"{shown} / {total} entries ({_log_activeFilter})";
                _log_summaryLabel.text = filterText;
            }

            // Auto-scroll to bottom after rebuild
            if (_log_autoScroll)
                ForceLogScrollToBottom();
        }

        // ====================================================================
        // DATA REFRESH — LOG TAB (5Hz)
        // ====================================================================

        private void RefreshLogTab()
        {
            if (engine == null) return;

            int currentCount = engine.eventLog.Count;

            // Only rebuild if entries changed or filter changed
            if (currentCount != _log_lastKnownCount ||
                _log_activeFilter != _log_lastAppliedFilter)
            {
                RebuildLogEntries();
            }
        }

        // ====================================================================
        // HELPERS
        // ====================================================================

        private bool PassesFilter(HeatupSimEngine.EventSeverity severity)
        {
            switch (_log_activeFilter)
            {
                case LogFilter.Info:   return severity == HeatupSimEngine.EventSeverity.INFO;
                case LogFilter.Action: return severity == HeatupSimEngine.EventSeverity.ACTION;
                case LogFilter.Alert:  return severity == HeatupSimEngine.EventSeverity.ALERT;
                case LogFilter.Alarm:  return severity == HeatupSimEngine.EventSeverity.ALARM;
                default:               return true; // All
            }
        }

        private static Color GetSeverityColor(HeatupSimEngine.EventSeverity severity)
        {
            switch (severity)
            {
                case HeatupSimEngine.EventSeverity.ACTION: return LOG_COLOR_ACTION;
                case HeatupSimEngine.EventSeverity.ALERT:  return LOG_COLOR_ALERT;
                case HeatupSimEngine.EventSeverity.ALARM:  return LOG_COLOR_ALARM;
                default:                                    return LOG_COLOR_INFO;
            }
        }

        private void OnLogScrollChanged(float value)
        {
            // If user scrolls away from bottom, pause auto-scroll
            if (_log_scrollView == null) return;

            float scrollMax = _log_scrollView.verticalScroller.highValue;
            float current = _log_scrollView.verticalScroller.value;

            // Consider "at bottom" if within 20px of max
            _log_autoScroll = (scrollMax - current) < 20f;
        }

        private void ForceLogScrollToBottom()
        {
            if (_log_scrollView == null) return;

            // Schedule for next frame to ensure layout is computed
            _log_scrollView.schedule.Execute(() =>
            {
                _log_scrollView.scrollOffset = new Vector2(
                    0f, _log_scrollView.contentContainer.layout.height);
            });
        }
    }
}
