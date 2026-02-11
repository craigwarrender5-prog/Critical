// ============================================================================
// CRITICAL: Master the Atom - UI Component (Tab Event Log Partial)
// HeatupValidationVisual.TabEventLog.cs - Tab 6: Event Log
// ============================================================================
//
// PURPOSE:
//   Renders the full Event Log tab — complete annunciator tile grid (all 26
//   tiles) in the upper region and full-width scrollable event log in the
//   lower region with severity filtering (ALL/INFO/ALERT/ALARM).
//
//   Layout (2-region, full width):
//     ┌─────────────────────────────────────────────────┐
//     │ ANNUNCIATOR TILES (full grid, all 26 tiles)     │
//     │ (top 35% of area)                               │
//     ├─────────────────────────────────────────────────┤
//     │ [ALL] [INFO] [ALERT] [ALARM]  severity filter   │
//     │ EVENT LOG (scrollable, full width)               │
//     │ (bottom 65% of area)                            │
//     └─────────────────────────────────────────────────┘
//
// READS FROM:
//   Delegates rendering to existing partial methods:
//     - Annunciators partial: DrawAnnunciatorContent(), DrawEventLogContent()
//   Plus new severity filter buttons (tab-local state).
//
// REFERENCE:
//   NRC HRTD 19.0 — Plant operations monitoring
//   Westinghouse main control board annunciator conventions
//
// ARCHITECTURE:
//   Partial class of HeatupValidationVisual. Implements:
//     - DrawEventLogTab(Rect) — dispatched from Core tab switch
//
// GOLD STANDARD: Yes
// v5.0.0: New file — Event Log tab for multi-tab dashboard redesign
// ============================================================================

using UnityEngine;
using Critical.Physics;

public partial class HeatupValidationVisual
{
    // ========================================================================
    // EVENT LOG TAB LAYOUT CONSTANTS
    // ========================================================================

    const float EVLOG_TILES_FRAC = 0.35f;   // Top region for annunciator tiles
    const float EVLOG_FILTER_H = 26f;        // Severity filter bar height

    // Severity filter state (0=ALL, 1=INFO, 2=ALERT, 3=ALARM)
    private int _eventLogFilter = 0;
    private readonly string[] _filterLabels = { "ALL", "INFO", "ALERT", "ALARM" };

    // Separate scroll state for filtered event log
    private Vector2 _filteredLogScroll;

    // ========================================================================
    // TAB IMPLEMENTATION
    // ========================================================================

    partial void DrawEventLogTab(Rect area)
    {
        if (engine == null) return;

        // TOP REGION: Annunciator Tiles
        float tilesH = area.height * EVLOG_TILES_FRAC;
        Rect tilesArea = new Rect(area.x, area.y, area.width, tilesH);
        DrawAnnunciatorContent(tilesArea);

        // BOTTOM REGION: Severity filter + Event Log
        float logY = area.y + tilesH + 4f;
        float logH = area.height - tilesH - 4f;
        Rect logRegion = new Rect(area.x, logY, area.width, logH);
        DrawFilteredEventLog(logRegion);
    }

    // ========================================================================
    // FILTERED EVENT LOG
    // Severity filter buttons + scrollable event log that respects filter.
    // ========================================================================

    private void DrawFilteredEventLog(Rect area)
    {
        float pad = 4f;
        float x0 = area.x + pad;
        float y0 = area.y + pad;
        float w = area.width - pad * 2f;

        // Section header
        DrawSectionHeader(new Rect(x0, y0, w, GAUGE_GROUP_HEADER_H), "OPERATIONS LOG");
        y0 += GAUGE_GROUP_HEADER_H + 2f;

        // Severity filter buttons
        float btnW = 70f;
        float btnGap = 4f;
        for (int i = 0; i < _filterLabels.Length; i++)
        {
            Rect btnRect = new Rect(x0 + i * (btnW + btnGap), y0, btnW, EVLOG_FILTER_H - 2f);

            // Highlight active filter
            bool isActive = (_eventLogFilter == i);
            Color btnBg = isActive ? _cBgHeader : _cBgPanel;
            Color btnText = isActive ? _cTextPrimary : _cTextSecondary;

            DrawFilledRect(btnRect, btnBg);
            var prev = GUI.contentColor;
            GUI.contentColor = btnText;
            if (GUI.Button(btnRect, _filterLabels[i], _statusLabelStyle))
            {
                _eventLogFilter = i;
                _filteredLogScroll = Vector2.zero;  // Reset scroll on filter change
            }
            GUI.contentColor = prev;
        }
        y0 += EVLOG_FILTER_H;

        // Filtered log area
        float logAreaH = area.height - GAUGE_GROUP_HEADER_H - EVLOG_FILTER_H - pad * 2f - 4f;
        if (logAreaH < 20f) return;

        var log = engine.eventLog;
        if (log == null || log.Count == 0)
        {
            GUI.Label(new Rect(x0, y0, w, 20f), "  No events yet.", _statusLabelStyle);
            return;
        }

        float entryH = 14f;

        // If filter is ALL (0), delegate to existing DrawEventLogContent for efficiency
        if (_eventLogFilter == 0)
        {
            Rect delegateArea = new Rect(area.x, y0 - GAUGE_GROUP_HEADER_H - 2f,
                area.width, logAreaH + GAUGE_GROUP_HEADER_H + 2f);
            // Draw directly since we already have the header
            DrawEventLogFiltered(x0, y0, w, logAreaH, log, null);
            return;
        }

        // Filtered: determine target severity
        HeatupSimEngine.EventSeverity? targetSev = null;
        switch (_eventLogFilter)
        {
            case 1: targetSev = HeatupSimEngine.EventSeverity.INFO; break;
            case 2: targetSev = HeatupSimEngine.EventSeverity.ALERT; break;
            case 3: targetSev = HeatupSimEngine.EventSeverity.ALARM; break;
        }

        DrawEventLogFiltered(x0, y0, w, logAreaH, log, targetSev);
    }

    /// <summary>
    /// Render event log entries with optional severity filter.
    /// Uses visible-only rendering for performance (same approach as v0.9.6).
    /// </summary>
    private void DrawEventLogFiltered(float x0, float y0, float w, float logAreaH,
        System.Collections.Generic.List<HeatupSimEngine.EventLogEntry> log,
        HeatupSimEngine.EventSeverity? filterSev)
    {
        float entryH = 14f;

        // Count matching entries and build index map
        // For ALL filter, total = log.Count
        int totalVisible;
        if (filterSev == null)
        {
            totalVisible = log.Count;
        }
        else
        {
            totalVisible = 0;
            for (int i = 0; i < log.Count; i++)
            {
                if (log[i].Severity == filterSev.Value)
                    totalVisible++;
            }
        }

        if (totalVisible == 0)
        {
            GUI.Label(new Rect(x0, y0, w, 20f), "  No matching events.", _statusLabelStyle);
            return;
        }

        float contentH = totalVisible * entryH;
        float scrollW = w - 16f;

        // Auto-scroll to bottom
        if (contentH > logAreaH)
            _filteredLogScroll.y = contentH - logAreaH;

        Rect scrollOuter = new Rect(x0, y0, w, logAreaH);
        _filteredLogScroll = GUI.BeginScrollView(scrollOuter, _filteredLogScroll,
            new Rect(0, 0, scrollW, contentH));

        // Calculate visible range
        int firstVisRow = Mathf.Max(0, (int)(_filteredLogScroll.y / entryH) - 5);
        int lastVisRow = Mathf.Min(totalVisible - 1,
            (int)((_filteredLogScroll.y + logAreaH) / entryH) + 5);

        if (filterSev == null)
        {
            // Unfiltered — direct index access (fast path)
            for (int i = firstVisRow; i <= lastVisRow; i++)
            {
                var entry = log[i];
                float ey = i * entryH;
                Color sevColor = GetEventSeverityColor(entry.Severity);
                var prev = GUI.contentColor;
                GUI.contentColor = sevColor;
                GUI.Label(new Rect(0, ey, scrollW, entryH), entry.FormattedLine, _eventLogStyle);
                GUI.contentColor = prev;
            }
        }
        else
        {
            // Filtered — scan through log mapping filtered index to actual index
            int visIdx = 0;
            for (int i = 0; i < log.Count && visIdx <= lastVisRow; i++)
            {
                if (log[i].Severity != filterSev.Value) continue;

                if (visIdx >= firstVisRow)
                {
                    float ey = visIdx * entryH;
                    Color sevColor = GetEventSeverityColor(log[i].Severity);
                    var prev = GUI.contentColor;
                    GUI.contentColor = sevColor;
                    GUI.Label(new Rect(0, ey, scrollW, entryH), log[i].FormattedLine, _eventLogStyle);
                    GUI.contentColor = prev;
                }
                visIdx++;
            }
        }

        GUI.EndScrollView();
    }

    /// <summary>
    /// Map event severity to display color.
    /// </summary>
    private Color GetEventSeverityColor(HeatupSimEngine.EventSeverity sev)
    {
        switch (sev)
        {
            case HeatupSimEngine.EventSeverity.ALARM:  return _cAlarmRed;
            case HeatupSimEngine.EventSeverity.ALERT:  return _cWarningAmber;
            case HeatupSimEngine.EventSeverity.ACTION: return _cCyanInfo;
            default: return _cTextSecondary;
        }
    }
}
