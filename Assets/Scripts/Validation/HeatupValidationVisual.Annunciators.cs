// ============================================================================
// CRITICAL: Master the Atom - UI Component (Annunciators Partial)
// HeatupValidationVisual.Annunciators.cs - Alarm Tiles + Event Log
// ============================================================================
//
// PURPOSE:
//   Renders the footer region of the heatup validation dashboard:
//   Left 45%: Annunciator tile grid (nuclear I&C alarm panel convention)
//   Right 55%: Scrollable operations event log with severity color-coding
//
// READS FROM:
//   HeatupSimEngine — all bool annunciator fields, eventLog list
//
// REFERENCE:
//   Westinghouse 4-Loop PWR main control board annunciator panel:
//     - Tiles dark when inactive, illuminated when alarmed
//     - Color: Green=normal, Amber=warning, Red=alarm, Dim=off
//     - Event log follows plant computer printout format
//   NRC HRTD Section 19 — Plant Operations monitoring
//
// ARCHITECTURE:
//   Partial class of HeatupValidationVisual. Implements:
//     - DrawAnnunciatorContent(Rect) — tile grid rendering
//     - DrawEventLogContent(Rect) — scrollable event log
//
// GOLD STANDARD: Yes
// v0.9.6 PERF FIX: Visible-only event log rendering eliminates 72,000 allocs/sec
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using Critical.Physics;

public partial class HeatupValidationVisual
{
    // ========================================================================
    // ANNUNCIATOR TILE DESCRIPTOR
    // ========================================================================

    struct AnnunciatorTile
    {
        public string Label;
        public bool Active;
        public bool IsAlarm;  // true=red, false=amber when active

        public AnnunciatorTile(string label, bool active, bool isAlarm)
        {
            Label = label;
            Active = active;
            IsAlarm = isAlarm;
        }
    }

    // v0.9.4: Cached inactive border color to prevent per-frame allocation
    private static readonly Color _cTileBorderInactive = new Color(0.2f, 0.22f, 0.26f, 1f);

    // v0.9.4: Reusable tile array to prevent per-frame allocation
    private AnnunciatorTile[] _cachedTiles;
    private const int TILE_COUNT = 27;

    // ========================================================================
    // PARTIAL METHOD — Annunciator Tiles (left footer)
    // ========================================================================

    partial void DrawAnnunciatorContent(Rect area)
    {
        if (engine == null) return;

        float pad = 4f;
        float x0 = area.x + pad;
        float y0 = area.y + pad;
        float availW = area.width - pad * 2f;

        // Section header
        DrawSectionHeader(new Rect(x0, y0, availW, GAUGE_GROUP_HEADER_H), "ANNUNCIATOR PANEL");
        y0 += GAUGE_GROUP_HEADER_H + 2f;

        // v0.9.4: Update cached tiles instead of allocating new array every frame
        UpdateAnnunciatorTiles();

        // Calculate grid layout
        int cols = Mathf.Max(1, Mathf.FloorToInt(availW / (ANN_TILE_W + ANN_TILE_GAP)));
        float tileW = (availW - (cols - 1) * ANN_TILE_GAP) / cols;

        for (int i = 0; i < _cachedTiles.Length; i++)
        {
            int col = i % cols;
            int row = i / cols;

            float tx = x0 + col * (tileW + ANN_TILE_GAP);
            float ty = y0 + row * (ANN_TILE_H + ANN_TILE_GAP);

            Rect tileRect = new Rect(tx, ty, tileW, ANN_TILE_H);
            DrawTile(tileRect, _cachedTiles[i]);
        }
    }

    // ========================================================================
    // BUILD TILE ARRAY — Maps engine booleans to display tiles
    // ========================================================================

    /// <summary>
    /// v0.9.4: Initialize the cached tile array once, then update values each frame.
    /// This prevents allocating a new array every frame.
    /// </summary>
    void UpdateAnnunciatorTiles()
    {
        // Allocate only once
        if (_cachedTiles == null || _cachedTiles.Length != TILE_COUNT)
        {
            _cachedTiles = new AnnunciatorTile[TILE_COUNT];
            // Initialize labels (these never change)
            _cachedTiles[0].Label = "PZR HTRS\nON";       _cachedTiles[0].IsAlarm = false;
            _cachedTiles[1].Label = "HEATUP\nIN PROG";    _cachedTiles[1].IsAlarm = false;
            _cachedTiles[2].Label = "STEAM\nBUBBLE OK";   _cachedTiles[2].IsAlarm = false;
            _cachedTiles[3].Label = "MODE\nPERMISSIVE";   _cachedTiles[3].IsAlarm = false;
            _cachedTiles[4].Label = "PRESS\nLOW";         _cachedTiles[4].IsAlarm = true;
            _cachedTiles[5].Label = "PRESS\nHIGH";        _cachedTiles[5].IsAlarm = true;
            _cachedTiles[6].Label = "SUBCOOL\nLOW";       _cachedTiles[6].IsAlarm = true;
            _cachedTiles[7].Label = "RCS FLOW\nLOW";      _cachedTiles[7].IsAlarm = true;
            _cachedTiles[8].Label = "PZR LVL\nLOW";       _cachedTiles[8].IsAlarm = true;
            _cachedTiles[9].Label = "PZR LVL\nHIGH";      _cachedTiles[9].IsAlarm = false;
            _cachedTiles[10].Label = "RVLIS\nLOW";        _cachedTiles[10].IsAlarm = true;
            _cachedTiles[11].Label = "CCW\nRUNNING";      _cachedTiles[11].IsAlarm = false;
            _cachedTiles[12].Label = "CHARGING\nACTIVE";  _cachedTiles[12].IsAlarm = false;
            _cachedTiles[13].Label = "LETDOWN\nACTIVE";   _cachedTiles[13].IsAlarm = false;
            _cachedTiles[14].Label = "SEAL INJ\nOK";      _cachedTiles[14].IsAlarm = false;
            _cachedTiles[15].Label = "VCT LVL\nLOW";      _cachedTiles[15].IsAlarm = true;
            _cachedTiles[16].Label = "VCT LVL\nHIGH";     _cachedTiles[16].IsAlarm = false;
            _cachedTiles[17].Label = "VCT\nDIVERT";       _cachedTiles[17].IsAlarm = false;
            _cachedTiles[18].Label = "VCT\nMAKEUP";       _cachedTiles[18].IsAlarm = false;
            _cachedTiles[19].Label = "RWST\nSUCTION";     _cachedTiles[19].IsAlarm = true;
            _cachedTiles[20].Label = "RCP #1\nRUN";       _cachedTiles[20].IsAlarm = false;
            _cachedTiles[21].Label = "RCP #2\nRUN";       _cachedTiles[21].IsAlarm = false;
            _cachedTiles[22].Label = "RCP #3\nRUN";       _cachedTiles[22].IsAlarm = false;
            _cachedTiles[23].Label = "RCP #4\nRUN";       _cachedTiles[23].IsAlarm = false;
            _cachedTiles[24].Label = "SMM LOW\nMARGIN";   _cachedTiles[24].IsAlarm = false;
            _cachedTiles[25].Label = "SMM NO\nMARGIN";    _cachedTiles[25].IsAlarm = true;
            _cachedTiles[26].Label = "SG PRESS\nHIGH";    _cachedTiles[26].IsAlarm = true;
        }

        // Update only the Active states (these change each frame)
        _cachedTiles[0].Active = engine.pzrHeatersOn;
        _cachedTiles[1].Active = engine.heatupInProgress;
        _cachedTiles[2].Active = engine.steamBubbleOK;
        _cachedTiles[3].Active = engine.modePermissive;
        _cachedTiles[4].Active = engine.pressureLow;
        _cachedTiles[5].Active = engine.pressureHigh;
        _cachedTiles[6].Active = engine.subcoolingLow;
        _cachedTiles[7].Active = engine.rcsFlowLow;
        _cachedTiles[8].Active = engine.pzrLevelLow;
        _cachedTiles[9].Active = engine.pzrLevelHigh;
        _cachedTiles[10].Active = engine.rvlisLevelLow;
        _cachedTiles[11].Active = engine.ccwRunning;
        _cachedTiles[12].Active = engine.chargingActive;
        _cachedTiles[13].Active = engine.letdownActive;
        _cachedTiles[14].Active = engine.sealInjectionOK;
        _cachedTiles[15].Active = engine.vctLevelLow;
        _cachedTiles[16].Active = engine.vctLevelHigh;
        _cachedTiles[17].Active = engine.vctDivertActive;
        _cachedTiles[18].Active = engine.vctMakeupActive;
        _cachedTiles[19].Active = engine.vctRWSTSuction;
        _cachedTiles[20].Active = engine.rcpRunning[0];
        _cachedTiles[21].Active = engine.rcpRunning[1];
        _cachedTiles[22].Active = engine.rcpRunning[2];
        _cachedTiles[23].Active = engine.rcpRunning[3];
        _cachedTiles[24].Active = engine.smmLowMargin;
        _cachedTiles[25].Active = engine.smmNoMargin;
        _cachedTiles[26].Active = engine.sgSecondaryPressureHigh;
    }

    // ========================================================================
    // DRAW SINGLE TILE — Illuminated alarm tile
    // ========================================================================

    void DrawTile(Rect rect, AnnunciatorTile tile)
    {
        Color bgColor;
        Color textColor;

        if (tile.Active)
        {
            if (tile.IsAlarm)
            {
                bgColor = _cAnnAlarm;
                textColor = _cAlarmRed;
            }
            else
            {
                // Status indicators use green, warnings use amber
                bool isNormalStatus = tile.Label.Contains("OK") ||
                                     tile.Label.Contains("RUN") ||
                                     tile.Label.Contains("ACTIVE") ||
                                     tile.Label.Contains("ON") ||
                                     tile.Label.Contains("PROG") ||
                                     tile.Label.Contains("PERMISSIVE") ||
                                     tile.Label.Contains("BUBBLE");
                if (isNormalStatus)
                {
                    bgColor = _cAnnNormal;
                    textColor = _cNormalGreen;
                }
                else
                {
                    bgColor = _cAnnWarning;
                    textColor = _cWarningAmber;
                }
            }
        }
        else
        {
            bgColor = _cAnnOff;
            textColor = _cTextSecondary;
        }

        // Draw tile background
        DrawFilledRect(rect, bgColor);

        // Draw border
        // v0.9.4: Use cached color instead of new Color() every frame
        float b = 1f;
        Color borderC = tile.Active ? textColor : _cTileBorderInactive;
        DrawFilledRect(new Rect(rect.x, rect.y, rect.width, b), borderC);
        DrawFilledRect(new Rect(rect.x, rect.yMax - b, rect.width, b), borderC);
        DrawFilledRect(new Rect(rect.x, rect.y, b, rect.height), borderC);
        DrawFilledRect(new Rect(rect.xMax - b, rect.y, b, rect.height), borderC);

        // Label
        var prev = GUI.contentColor;
        GUI.contentColor = textColor;
        GUI.Label(rect, tile.Label, _annTileStyle);
        GUI.contentColor = prev;
    }

    // ========================================================================
    // PARTIAL METHOD — Event Log (right footer)
    // v0.9.6 PERF FIX: Only draw visible entries, use pre-formatted strings
    // ========================================================================

    partial void DrawEventLogContent(Rect area)
    {
        if (engine == null) return;

        float pad = 4f;
        float x0 = area.x + pad;
        float y0 = area.y + pad;
        float w = area.width - pad * 2f;

        // Section header
        DrawSectionHeader(new Rect(x0, y0, w, GAUGE_GROUP_HEADER_H), "OPERATIONS LOG");
        y0 += GAUGE_GROUP_HEADER_H + 2f;

        float logAreaH = area.height - GAUGE_GROUP_HEADER_H - pad * 2f - 2f;
        if (logAreaH < 20f) return;

        var log = engine.eventLog;
        if (log == null || log.Count == 0)
        {
            GUI.Label(new Rect(x0, y0, w, 20f), "  No events yet.", _statusLabelStyle);
            return;
        }

        // v0.9.6 PERF FIX: Calculate visible range to avoid drawing all 200 entries
        float entryH = 14f;
        float contentH = log.Count * entryH;
        
        // Calculate which entries are actually visible in the scroll window
        // Add buffer of 5 entries above/below for smooth scrolling
        int firstVisible = Mathf.Max(0, (int)(_eventLogScroll.y / entryH) - 5);
        int lastVisible = Mathf.Min(log.Count - 1, 
            (int)((_eventLogScroll.y + logAreaH) / entryH) + 5);

        // Scrollable log area (newest at bottom, auto-scroll)
        Rect scrollOuter = new Rect(x0, y0, w, logAreaH);
        float scrollW = w - 16f;

        // Auto-scroll to bottom
        if (contentH > logAreaH)
            _eventLogScroll.y = contentH - logAreaH;

        _eventLogScroll = GUI.BeginScrollView(scrollOuter, _eventLogScroll,
            new Rect(0, 0, scrollW, contentH));

        // v0.9.6 PERF FIX: Only draw visible entries (typically 10-20 instead of all 200)
        // This eliminates ~95% of GUI.Label calls and string allocations
        for (int i = firstVisible; i <= lastVisible; i++)
        {
            var entry = log[i];
            float ey = i * entryH;

            // Severity color
            Color sevColor;
            switch (entry.Severity)
            {
                case HeatupSimEngine.EventSeverity.ALARM:
                    sevColor = _cAlarmRed;
                    break;
                case HeatupSimEngine.EventSeverity.ALERT:
                    sevColor = _cWarningAmber;
                    break;
                case HeatupSimEngine.EventSeverity.ACTION:
                    sevColor = _cCyanInfo;
                    break;
                default:
                    sevColor = _cTextSecondary;
                    break;
            }

            // v0.9.6 PERF FIX: Use pre-formatted line from struct (NO string allocations!)
            // The timestamp formatting now happens ONCE when the entry is created,
            // not every frame for every entry
            var prev = GUI.contentColor;
            GUI.contentColor = sevColor;
            GUI.Label(new Rect(0, ey, scrollW, entryH), entry.FormattedLine, _eventLogStyle);
            GUI.contentColor = prev;
        }

        GUI.EndScrollView();
    }
}
