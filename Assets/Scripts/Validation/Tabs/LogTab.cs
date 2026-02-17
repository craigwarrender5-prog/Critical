// ============================================================================
// CRITICAL: Master the Atom - Validation Dashboard v2
// LogTab.cs - Full Event Log and Annunciator Grid Tab
// ============================================================================
//
// PURPOSE:
//   Displays the complete event log with filtering options and an expanded
//   annunciator grid view. Provides detailed alarm history and status
//   for operator review.
//
// LAYOUT:
//   ┌─────────────────────────────────────────┬───────────────────────────────┐
//   │           ANNUNCIATOR GRID              │         EVENT LOG             │
//   │               (50%)                     │           (50%)               │
//   │                                         │                               │
//   │  ┌─────┬─────┬─────┬─────┬─────┬─────┐ │  [ALL] [INFO] [WARN] [ALARM]  │
//   │  │HTR  │HTUP │BUBL │MD5  │P LO │P HI │ │  ─────────────────────────────│
//   │  ├─────┼─────┼─────┼─────┼─────┼─────┤ │  12:15:30 HTR ON: PZR Heat... │
//   │  │SC LO│FLOW │MD4  │L LO │L HI │VCT  │ │  12:15:35 HEATUP: Heatup In...│
//   │  ├─────┼─────┼─────┼─────┼─────┼─────┤ │  12:16:01 P LO: RCS Pressur...│
//   │  │VCT  │CHG  │LTD  │SEAL │DVRT │MKUP │ │  12:16:30 BUBBLE: PZR Bubble..│
//   │  ├─────┼─────┼─────┼─────┼─────┼─────┤ │  12:17:00 MODE 4: Plant in ...│
//   │  │CCW  │RCP1 │RCP2 │RCP3 │RCP4 │SG P │ │  ...                          │
//   │  ├─────┼─────┼─────┼─────┼─────┼─────┤ │                               │
//   │  │RHR  │HZP  │PERM │     │     │     │ │                               │
//   │  └─────┴─────┴─────┴─────┴─────┴─────┘ │                               │
//   │                                         │                               │
//   │  ALERTING: 2  ACKED: 1  TOTAL: 3       │  Showing 24 of 48 events      │
//   │                      [ACK ALL] [RESET]  │                               │
//   └─────────────────────────────────────────┴───────────────────────────────┘
//
// REFERENCE:
//   ISA-18.1 Alarm Management
//   NRC HRTD alarm response procedures
//
// GOLD STANDARD: Yes
// VERSION: 1.0.0.0
// ============================================================================

using UnityEngine;
using System.Collections.Generic;

namespace Critical.Validation
{
    /// <summary>
    /// Log tab with expanded annunciator grid and filtered event log.
    /// </summary>
    public class LogTab : DashboardTab
    {
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public LogTab(ValidationDashboard dashboard) 
            : base(dashboard, "LOG", 7)
        {
        }

        // ====================================================================
        // FILTER STATE
        // ====================================================================

        private int _logFilter = 0; // 0=ALL, 1=INFO, 2=WARN, 3=ALARM
        private readonly string[] _filterLabels = new string[]
        {
            "ALL", "INFO", "WARN", "ALARM"
        };

        // ====================================================================
        // LAYOUT
        // ====================================================================

        private Rect _annunciatorArea;
        private Rect _logArea;

        private const float ANN_FRAC = 0.50f;
        private const float COL_GAP = 8f;
        private const float PAD = 8f;

        private float _cachedW;
        private float _cachedH;

        // Cached tile style
        private GUIStyle _cachedTileStyle;

        private void CalculateLayout(Rect area)
        {
            if (Mathf.Approximately(_cachedW, area.width) &&
                Mathf.Approximately(_cachedH, area.height))
                return;

            _cachedW = area.width;
            _cachedH = area.height;

            float availW = area.width - PAD * 2 - COL_GAP;
            float annW = availW * ANN_FRAC;
            float logW = availW - annW;

            float x = area.x + PAD;
            float y = area.y + PAD;
            float h = area.height - PAD * 2;

            _annunciatorArea = new Rect(x, y, annW, h);
            _logArea = new Rect(x + annW + COL_GAP, y, logW, h);
        }

        // ====================================================================
        // MAIN DRAW
        // ====================================================================

        public override void Draw(Rect area)
        {
            CalculateLayout(area);

            DrawAnnunciatorGrid();
            DrawEventLog();
        }

        // ====================================================================
        // ANNUNCIATOR GRID
        // ====================================================================

        private void DrawAnnunciatorGrid()
        {
            var d = Dashboard;
            var am = d.AnnunciatorManager;

            d.DrawColumnFrame(_annunciatorArea, "ANNUNCIATOR GRID");

            float y = _annunciatorArea.y + 26f;
            float areaW = _annunciatorArea.width - 8f;
            float areaH = _annunciatorArea.height - 26f - 60f; // Leave room for buttons and status

            // Calculate tile dimensions for expanded grid (6 columns)
            int cols = 6;
            int rows = 5; // 27 tiles = 5 rows of 6 (last row partial)
            float tileGap = 4f;
            float tileW = (areaW - (cols - 1) * tileGap) / cols;
            float tileH = (areaH - (rows - 1) * tileGap) / rows;
            tileW = Mathf.Min(tileW, 120f);
            tileH = Mathf.Min(tileH, 36f);

            // Draw tiles
            if (am != null && am.IsInitialized)
            {
                for (int i = 0; i < AnnunciatorManager.TILE_COUNT; i++)
                {
                    int row = i / cols;
                    int col = i % cols;

                    var tile = am.GetTile(i);
                    if (tile == null) continue;

                    float tileX = _annunciatorArea.x + 4f + col * (tileW + tileGap);
                    float tileY = y + row * (tileH + tileGap);
                    Rect tileRect = new Rect(tileX, tileY, tileW, tileH);

                    // Handle click-to-acknowledge
                    if (Event.current.type == EventType.MouseDown && 
                        tileRect.Contains(Event.current.mousePosition))
                    {
                        if (am.AcknowledgeTile(i))
                        {
                            Event.current.Use();
                        }
                    }

                    DrawExpandedTile(d, tileRect, tile);
                }
            }

            // Status line
            float statusY = _annunciatorArea.y + _annunciatorArea.height - 56f;
            int alerting = am?.AlertingCount ?? 0;
            int acked = am?.AcknowledgedCount ?? 0;
            int total = am?.TotalActiveCount ?? 0;

            string statusStr = $"ALERTING: {alerting}   ACKED: {acked}   TOTAL: {total}";
            GUI.contentColor = total > 0 ? ValidationDashboard._cWarningAmber : ValidationDashboard._cTextSecondary;
            GUI.Label(new Rect(_annunciatorArea.x + 8f, statusY, areaW, 20f),
                statusStr, d._statusLabelStyle);
            GUI.contentColor = Color.white;

            // Buttons
            float btnY = _annunciatorArea.y + _annunciatorArea.height - 30f;
            float btnW = 80f;
            float btnH = 24f;
            float btnGap = 8f;

            Rect ackRect = new Rect(_annunciatorArea.x + _annunciatorArea.width - btnW * 2 - btnGap - 8f, 
                btnY, btnW, btnH);
            Rect resetRect = new Rect(_annunciatorArea.x + _annunciatorArea.width - btnW - 4f, 
                btnY, btnW, btnH);

            GUI.enabled = alerting > 0;
            if (GUI.Button(ackRect, "ACK ALL", d._buttonStyle))
            {
                am?.AcknowledgeAll();
            }
            GUI.enabled = true;

            GUI.enabled = acked > 0;
            if (GUI.Button(resetRect, "RESET", d._buttonStyle))
            {
                am?.ResetAll();
            }
            GUI.enabled = true;
        }

        private void DrawExpandedTile(ValidationDashboard d, Rect tileRect, AnnunciatorTile tile)
        {
            // Get colors based on state
            Color bgColor;
            Color textColor;

            switch (tile.State)
            {
                case AnnunciatorState.Normal:
                    bgColor = ValidationDashboard._cAnnNormal;
                    textColor = ValidationDashboard._cTextPrimary;
                    break;
                case AnnunciatorState.Alerting:
                    bool flash = (Time.time % 0.5f) < 0.25f;
                    bgColor = flash ? ValidationDashboard._cAnnAlerting : ValidationDashboard._cAnnOff;
                    textColor = flash ? ValidationDashboard._cTextBright : ValidationDashboard._cTextSecondary;
                    break;
                case AnnunciatorState.Acknowledged:
                    bgColor = ValidationDashboard._cAnnAcked;
                    textColor = ValidationDashboard._cTextPrimary;
                    break;
                case AnnunciatorState.Alarm:
                    bool alarmFlash = (Time.time % 0.3f) < 0.15f;
                    bgColor = alarmFlash ? ValidationDashboard._cAnnAlarm : ValidationDashboard._cAnnOff;
                    textColor = ValidationDashboard._cTextBright;
                    break;
                default:
                    bgColor = tile.ConditionActive 
                        ? ValidationDashboard._cAnnNormal 
                        : ValidationDashboard._cAnnOff;
                    textColor = tile.ConditionActive 
                        ? ValidationDashboard._cTextPrimary 
                        : ValidationDashboard._cTextSecondary;
                    break;
            }

            // Draw background
            GUI.DrawTexture(tileRect, d.GetColorTex(bgColor));

            // Draw border
            ValidationDashboard.DrawLine(new Vector2(tileRect.x, tileRect.y),
                new Vector2(tileRect.x + tileRect.width, tileRect.y), 
                ValidationDashboard._cGaugeTick, 1f);
            ValidationDashboard.DrawLine(new Vector2(tileRect.x + tileRect.width, tileRect.y),
                new Vector2(tileRect.x + tileRect.width, tileRect.y + tileRect.height), 
                ValidationDashboard._cGaugeTick, 1f);
            ValidationDashboard.DrawLine(new Vector2(tileRect.x + tileRect.width, tileRect.y + tileRect.height),
                new Vector2(tileRect.x, tileRect.y + tileRect.height), 
                ValidationDashboard._cGaugeTick, 1f);
            ValidationDashboard.DrawLine(new Vector2(tileRect.x, tileRect.y + tileRect.height),
                new Vector2(tileRect.x, tileRect.y), 
                ValidationDashboard._cGaugeTick, 1f);

            // Cache tile style
            if (_cachedTileStyle == null)
            {
                _cachedTileStyle = new GUIStyle(d._gaugeLabelStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    fontSize = 9
                };
            }
            _cachedTileStyle.normal.textColor = textColor;

            // Draw label and description
            float labelH = tileRect.height * 0.5f;
            GUI.Label(new Rect(tileRect.x + 2f, tileRect.y + 2f,
                tileRect.width - 4f, labelH), tile.Label, _cachedTileStyle);

            // Draw description in smaller text if tile is large enough
            if (tileRect.height > 30f)
            {
                GUIStyle descStyle = new GUIStyle(_cachedTileStyle)
                {
                    fontSize = 7
                };
                descStyle.normal.textColor = ValidationDashboard._cTextSecondary;
                GUI.Label(new Rect(tileRect.x + 2f, tileRect.y + labelH,
                    tileRect.width - 4f, tileRect.height - labelH - 2f), 
                    tile.Description ?? "", descStyle);
            }
        }

        // ====================================================================
        // EVENT LOG
        // ====================================================================

        private void DrawEventLog()
        {
            var d = Dashboard;
            var am = d.AnnunciatorManager;

            d.DrawColumnFrame(_logArea, "EVENT LOG");

            float y = _logArea.y + 26f;
            float logW = _logArea.width - 8f;

            // Filter toolbar
            Rect filterRect = new Rect(_logArea.x + 4f, y, logW, 24f);
            _logFilter = GUI.Toolbar(filterRect, _logFilter, _filterLabels, d._tabStyle);
            y += 30f;

            // Get filtered events
            if (am == null || am.EventCount == 0)
            {
                GUI.contentColor = ValidationDashboard._cTextSecondary;
                GUI.Label(new Rect(_logArea.x + 4f, y, logW, 20f),
                    "No events recorded", d._statusLabelStyle);
                GUI.contentColor = Color.white;
                return;
            }

            var allEvents = am.GetEvents(AnnunciatorManager.EVENT_LOG_SIZE);
            var filteredEvents = FilterEvents(allEvents);

            // Draw events
            float logH = 18f;
            int maxVisible = (int)((_logArea.height - 26f - 60f) / logH);
            int shown = 0;

            foreach (var evt in filteredEvents)
            {
                if (shown >= maxVisible) break;

                // Format time
                int hours = (int)evt.SimTime;
                int minutes = (int)((evt.SimTime - hours) * 60f);
                int seconds = (int)((evt.SimTime * 60f - hours * 60f - minutes) * 60f);
                string timeStr = $"{hours:D2}:{minutes:D2}:{seconds:D2}";

                // Get severity color
                Color sevColor;
                switch (evt.Severity)
                {
                    case AlarmSeverity.Alarm:
                        sevColor = ValidationDashboard._cAlarmRed;
                        break;
                    case AlarmSeverity.Warning:
                        sevColor = ValidationDashboard._cWarningAmber;
                        break;
                    default:
                        sevColor = ValidationDashboard._cCyanInfo;
                        break;
                }

                // Draw time
                GUI.contentColor = ValidationDashboard._cTextSecondary;
                GUI.Label(new Rect(_logArea.x + 4f, y, 65f, logH),
                    timeStr, d._statusLabelStyle);

                // Draw message
                GUI.contentColor = sevColor;
                GUI.Label(new Rect(_logArea.x + 72f, y, logW - 72f, logH),
                    evt.Message, d._statusLabelStyle);

                y += logH;
                shown++;
            }

            GUI.contentColor = Color.white;

            // Status line
            float statusY = _logArea.y + _logArea.height - 26f;
            string statusStr = $"Showing {shown} of {filteredEvents.Count} events";
            GUI.contentColor = ValidationDashboard._cTextSecondary;
            GUI.Label(new Rect(_logArea.x + 4f, statusY, logW, 20f),
                statusStr, d._statusLabelStyle);
            GUI.contentColor = Color.white;
        }

        private List<EventLogEntry> FilterEvents(IReadOnlyList<EventLogEntry> events)
        {
            var result = new List<EventLogEntry>();

            foreach (var evt in events)
            {
                bool include = _logFilter switch
                {
                    0 => true,  // ALL
                    1 => evt.Severity == AlarmSeverity.Info,
                    2 => evt.Severity == AlarmSeverity.Warning,
                    3 => evt.Severity == AlarmSeverity.Alarm,
                    _ => true
                };

                if (include)
                    result.Add(evt);
            }

            return result;
        }
    }
}
