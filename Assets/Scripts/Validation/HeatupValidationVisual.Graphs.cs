// ============================================================================
// CRITICAL: Master the Atom - UI Component (Graphs Partial)
// HeatupValidationVisual.Graphs.cs - Trend Graph Rendering
// ============================================================================
//
// PURPOSE:
//   Renders time-series trend graphs in the center column of the heatup
//   validation dashboard. Six tabbed graph panels display rolling history
//   from the engine's 240-point buffers (1 sim-minute samples, 4-hour window).
//
// TABS:
//   0. TEMPS     — T_RCS, T_HOT, T_COLD, T_PZR, T_SAT, T_SG_SEC (6 traces, v0.9.0)
//   1. PRESSURE  — RCS Pressure + PZR Level (dual Y-axis)
//   2. CVCS      — Charging, Letdown, Surge (3 traces)
//   3. VCT/BRS   — VCT Level, BRS Holdup, BRS Distillate (3 traces)
//   4. RATES     — Heatup Rate, Pressure Rate, Subcooling (v0.9.0: added pressure rate)
//   5. RCP HEAT  — Heatup Rate replot with RCP count annotations
//   6. HZP       — v1.1.0: Steam dump heat, steam pressure, heater PID output, HZP progress
//
// READS FROM:
//   HeatupSimEngine — All history buffers including:
//     v0.9.0 NEW: tPzrHistory, tSatHistory, pressureRateHistory
//     Existing: tempHistory, tHotHistory, tColdHistory, tSgSecondaryHistory,
//       pressHistory, pzrLevelHistory, chargingHistory, letdownHistory, surgeFlowHistory,
//       vctLevelHistory, brsHoldupHistory, brsDistillateHistory,
//       heatRateHistory, subcoolHistory, timeHistory
//
// REFERENCE:
//   Control room chart recorder conventions:
//     - Dark background, bright traces
//     - Y-axis labels on left, time axis on bottom
//     - Grid lines at major divisions
//     - Color-coded legend
//
// ARCHITECTURE:
//   Partial class of HeatupValidationVisual. Implements:
//     - DrawGraphContent(Rect, int) — dispatched from Core per tab index
//     - DrawPlotArea() — shared graph renderer (axes, grid, traces, legend)
//     - Per-tab configuration methods defining traces and ranges
//
// v0.9.0 CHANGES:
//   - T_PZR and T_SAT now drawn as proper historical traces (were live annotations)
//   - Added Pressure Rate trace to RATES tab
//   - Removed DrawLiveAnnotation calls for T_PZR/T_SAT
//
// GOLD STANDARD: Yes
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using Critical.Physics;

public partial class HeatupValidationVisual
{
    // ========================================================================
    // GRAPH LAYOUT CONSTANTS
    // ========================================================================

    const float GRAPH_MARGIN_LEFT   = 60f;   // Y-axis labels
    const float GRAPH_MARGIN_RIGHT  = 16f;
    const float GRAPH_MARGIN_TOP    = 8f;
    const float GRAPH_MARGIN_BOTTOM = 28f;   // X-axis labels
    const float GRAPH_LEGEND_H      = 18f;   // Legend row height
    const float GRAPH_GRID_LINES_Y  = 5;     // Horizontal grid divisions
    const float GRAPH_GRID_LINES_X  = 6;     // Vertical grid divisions

    // Dual Y-axis right margin (when second axis is present)
    const float GRAPH_MARGIN_RIGHT_DUAL = 60f;

    // ========================================================================
    // TRACE DESCRIPTOR — Defines a single graph trace
    // ========================================================================

    struct TraceDescriptor
    {
        public string Label;
        public Color Color;
        public List<float> Data;
        public string Format;
        public string Unit;

        public TraceDescriptor(string label, Color color, List<float> data,
            string format = "F1", string unit = "")
        {
            Label = label;
            Color = color;
            Data = data;
            Format = format;
            Unit = unit;
        }
    }

    // ========================================================================
    // PARTIAL METHOD IMPLEMENTATION — Called by Core
    // ========================================================================

    partial void DrawGraphContent(Rect area, int tabIndex)
    {
        if (engine == null) return;

        var timeData = engine.timeHistory;
        // v0.9.5: Changed from < 2 to < 1 to show graph immediately with first data point
        if (timeData == null || timeData.Count < 1)
        {
            GUI.Label(area, "  Initializing...", _statusLabelStyle);
            return;
        }

        switch (tabIndex)
        {
            case 0: DrawTempsGraph(area, timeData); break;
            case 1: DrawPressureGraph(area, timeData); break;
            case 2: DrawCVCSGraph(area, timeData); break;
            case 3: DrawVCTBRSGraph(area, timeData); break;
            case 4: DrawRatesGraph(area, timeData); break;
            case 5: DrawRCPHeatGraph(area, timeData); break;
            case 6: DrawHZPGraph(area, timeData); break;  // v1.1.0
        }
    }

    // ========================================================================
    // TAB 0: TEMPERATURES — All 6 temperatures as historical traces
    // v0.9.0: T_PZR and T_SAT now use history buffers (no more live annotations)
    // ========================================================================

    void DrawTempsGraph(Rect area, List<float> timeData)
    {
        // v0.9.0: All 6 temperatures now have proper history buffers
        var traces = new TraceDescriptor[]
        {
            new TraceDescriptor("T_RCS",    _cTrace1, engine.tempHistory,           "F1", "°F"),
            new TraceDescriptor("T_HOT",    _cTrace2, engine.tHotHistory,           "F1", "°F"),
            new TraceDescriptor("T_COLD",   _cTrace3, engine.tColdHistory,          "F1", "°F"),
            new TraceDescriptor("T_PZR",    _cTrace4, engine.tPzrHistory,           "F1", "°F"),  // v0.9.0: Now from history!
            new TraceDescriptor("T_SAT",    _cTrace5, engine.tSatHistory,           "F1", "°F"),  // v0.9.0: Now from history!
            new TraceDescriptor("T_SG_SEC", _cTrace6, engine.tSgSecondaryHistory,   "F1", "°F"),
        };

        // Auto-range Y from all trace data
        float yMin, yMax;
        AutoRangeY(traces, out yMin, out yMax, 50f, 650f, 20f);
        
        // Clamp to absolute bounds
        yMin = Mathf.Max(yMin, 50f);
        yMax = Mathf.Min(yMax, 650f);

        DrawPlotArea(area, timeData, traces, yMin, yMax, "TEMP (°F)", false, 0f, 0f, "");
        
        // No more DrawLiveAnnotation needed - T_PZR and T_SAT are now proper traces!
    }

    // ========================================================================
    // TAB 1: PRESSURE — Dual Y-axis: RCS Pressure + PZR Level
    // ========================================================================

    void DrawPressureGraph(Rect area, List<float> timeData)
    {
        // Primary axis: Pressure
        var pressTrace = new TraceDescriptor[]
        {
            new TraceDescriptor("RCS PRESS", _cTrace1, engine.pressHistory, "F0", "psia"),
        };

        float pYMin, pYMax;
        AutoRangeY(pressTrace, out pYMin, out pYMax, 0f, 2500f, 50f);

        // Secondary axis: PZR Level
        var levelTrace = new TraceDescriptor[]
        {
            new TraceDescriptor("PZR LEVEL", _cTrace4, engine.pzrLevelHistory, "F1", "%"),
        };

        float lYMin = 0f, lYMax = 100f;

        DrawPlotArea(area, timeData, pressTrace, pYMin, pYMax, "PRESS (psia)",
            true, lYMin, lYMax, "LEVEL (%)");

        // Draw secondary traces manually on the dual axis
        DrawTracesOnSecondaryAxis(area, timeData, levelTrace, lYMin, lYMax);
    }

    // ========================================================================
    // TAB 2: CVCS — Charging, Letdown, Surge, Net
    // ========================================================================

    void DrawCVCSGraph(Rect area, List<float> timeData)
    {
        var traces = new TraceDescriptor[]
        {
            new TraceDescriptor("CHARGING", _cTrace1, engine.chargingHistory, "F1", "gpm"),
            new TraceDescriptor("LETDOWN",  _cTrace3, engine.letdownHistory,  "F1", "gpm"),
            new TraceDescriptor("SURGE",    _cTrace4, engine.surgeFlowHistory,"F1", "gpm"),
        };

        float yMin, yMax;
        AutoRangeY(traces, out yMin, out yMax, -50f, 150f, 10f);

        DrawPlotArea(area, timeData, traces, yMin, yMax, "FLOW (gpm)", false, 0f, 0f, "");

        // Zero reference line
        DrawHorizontalRef(area, timeData, yMin, yMax, 0f, _cTextSecondary, "ZERO");
    }

    // ========================================================================
    // TAB 3: VCT/BRS — VCT Level, BRS Holdup volume, BRS Distillate
    // ========================================================================

    void DrawVCTBRSGraph(Rect area, List<float> timeData)
    {
        // Primary: VCT Level (%)
        var vctTrace = new TraceDescriptor[]
        {
            new TraceDescriptor("VCT LEVEL", _cTrace1, engine.vctLevelHistory, "F1", "%"),
        };

        float vYMin = 0f, vYMax = 100f;

        // Secondary: BRS volumes (gal) — different scale
        var brsTraces = new TraceDescriptor[]
        {
            new TraceDescriptor("BRS HOLDUP",  _cOrangeAccent, engine.brsHoldupHistory,     "F0", "gal"),
            new TraceDescriptor("BRS DISTILL", _cCyanInfo,     engine.brsDistillateHistory,  "F0", "gal"),
        };

        float bYMin, bYMax;
        AutoRangeY(brsTraces, out bYMin, out bYMax, 0f, 50000f, 1000f);
        if (bYMax < 1000f) bYMax = 1000f;  // Minimum range for readability

        DrawPlotArea(area, timeData, vctTrace, vYMin, vYMax, "VCT (%)",
            true, bYMin, bYMax, "BRS (gal)");

        DrawTracesOnSecondaryAxis(area, timeData, brsTraces, bYMin, bYMax);

        // VCT setpoint bands
        DrawHorizontalRef(area, timeData, vYMin, vYMax,
            PlantConstants.VCT_LEVEL_HIGH, _cWarningAmber, "VCT HI");
        DrawHorizontalRef(area, timeData, vYMin, vYMax,
            PlantConstants.VCT_LEVEL_LOW, _cWarningAmber, "VCT LO");
    }

    // ========================================================================
    // TAB 4: RATES — Heatup Rate, Pressure Rate, Subcooling
    // v0.9.0: Added pressureRateHistory trace
    // ========================================================================

    void DrawRatesGraph(Rect area, List<float> timeData)
    {
        // Dual axis: Heatup Rate + Pressure Rate (left) + Subcooling (right)
        var rateTraces = new TraceDescriptor[]
        {
            new TraceDescriptor("HEATUP RATE", _cTrace1, engine.heatRateHistory,      "F1", "°F/hr"),
            new TraceDescriptor("PRESS RATE",  _cTrace4, engine.pressureRateHistory,  "F0", "psi/hr"),  // v0.9.0
        };

        float rYMin, rYMax;
        AutoRangeY(rateTraces, out rYMin, out rYMax, -50f, 100f, 10f);
        if (rYMax < 55f) rYMax = 55f;  // Always show Tech Spec limit
        if (rYMin > -10f) rYMin = -10f;  // Show zero line

        var scTrace = new TraceDescriptor[]
        {
            new TraceDescriptor("SUBCOOLING", _cTrace3, engine.subcoolHistory, "F1", "°F"),
        };

        float sYMin, sYMax;
        AutoRangeY(scTrace, out sYMin, out sYMax, 0f, 200f, 10f);

        DrawPlotArea(area, timeData, rateTraces, rYMin, rYMax, "RATE",
            true, sYMin, sYMax, "SUBCOOL (°F)");

        DrawTracesOnSecondaryAxis(area, timeData, scTrace, sYMin, sYMax);

        // Tech Spec 50 F/hr limit line (on primary axis)
        DrawHorizontalRef(area, timeData, rYMin, rYMax,
            50f, _cAlarmRed, "TECH SPEC 50°F/hr");
            
        // Zero reference line for rates
        DrawHorizontalRef(area, timeData, rYMin, rYMax,
            0f, _cTextSecondary, "ZERO");

        // Subcooling 30F warning on secondary axis
        Rect plotRect = GetPlotRect(area, true);
        float scWarnY = plotRect.yMax - ((30f - sYMin) / (sYMax - sYMin)) * plotRect.height;
        if (scWarnY >= plotRect.y && scWarnY <= plotRect.yMax)
        {
            // v0.9.4: Use DrawLineWithAlpha to avoid new Color allocation
            DrawLineWithAlpha(new Vector2(plotRect.x, scWarnY),
                     new Vector2(plotRect.xMax, scWarnY),
                     _cWarningAmber, 0.4f, 1f);
        }
        
        // Subcooling 15F alarm on secondary axis
        float scAlarmY = plotRect.yMax - ((15f - sYMin) / (sYMax - sYMin)) * plotRect.height;
        if (scAlarmY >= plotRect.y && scAlarmY <= plotRect.yMax)
        {
            // v0.9.4: Use DrawLineWithAlpha to avoid new Color allocation
            DrawLineWithAlpha(new Vector2(plotRect.x, scAlarmY),
                     new Vector2(plotRect.xMax, scAlarmY),
                     _cAlarmRed, 0.4f, 1f);
        }
    }

    // ========================================================================
    // TAB 5: RCP HEAT — Heatup Rate with RCP count context
    // ========================================================================

    void DrawRCPHeatGraph(Rect area, List<float> timeData)
    {
        var traces = new TraceDescriptor[]
        {
            new TraceDescriptor("HEATUP RATE", _cTrace1, engine.heatRateHistory, "F1", "F/hr"),
        };

        float yMin, yMax;
        AutoRangeY(traces, out yMin, out yMax, -10f, 60f, 5f);
        if (yMax < 55f) yMax = 55f;

        DrawPlotArea(area, timeData, traces, yMin, yMax, "RATE (F/hr)", false, 0f, 0f, "");

        // Tech Spec limit
        DrawHorizontalRef(area, timeData, yMin, yMax, 50f, _cAlarmRed, "LIMIT 50");

        // Live RCP status annotation in top-right of plot
        Rect plotRect = GetPlotRect(area, false);
        string rcpInfo = $"RCPs: {engine.rcpCount}/4  |  RCP Heat: {engine.effectiveRCPHeat:F1} MW" +
                         $"  |  Heaters: {engine.pzrHeaterPower * 1000f:F0} kW" +
                         $"  |  Alpha: {Mathf.Min(1f, engine.rcpContribution.TotalFlowFraction):F2}";
        var prev = GUI.contentColor;
        GUI.contentColor = _cCyanInfo;
        GUI.Label(new Rect(plotRect.x + 4f, plotRect.y + 2f, plotRect.width - 8f, 16f),
            rcpInfo, _graphLabelStyle);
        GUI.contentColor = prev;
    }

    // ========================================================================
    // SHARED GRAPH RENDERER — Axes, grid, traces, legend
    // ========================================================================

    void DrawPlotArea(Rect area, List<float> timeData, TraceDescriptor[] traces,
        float yMin, float yMax, string yLabel,
        bool dualAxis, float y2Min, float y2Max, string y2Label)
    {
        if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
            return;

        float rightMargin = dualAxis ? GRAPH_MARGIN_RIGHT_DUAL : GRAPH_MARGIN_RIGHT;

        // Calculate plot rectangle
        float legendH = (traces.Length > 0 ? GRAPH_LEGEND_H : 0f);
        Rect plotRect = new Rect(
            area.x + GRAPH_MARGIN_LEFT,
            area.y + GRAPH_MARGIN_TOP + legendH,
            area.width - GRAPH_MARGIN_LEFT - rightMargin,
            area.height - GRAPH_MARGIN_TOP - GRAPH_MARGIN_BOTTOM - legendH);

        if (plotRect.width < 50f || plotRect.height < 30f) return;

        // Background
        DrawFilledRect(plotRect, _cBgGraph);

        // v0.8.2: Fixed 240-minute (4-hour) rolling window
        // X-axis always shows -240 minutes to NOW regardless of how much data exists
        const float WINDOW_HOURS = 4.0f;  // 240 minutes
        float tMax = engine.simTime;       // Current time = right edge (NOW)
        float tMin = tMax - WINDOW_HOURS;  // 4 hours ago = left edge (-240 min)
        if (tMin < 0f) tMin = 0f;          // Don't go negative at simulation start

        // Grid lines
        DrawGridLines(plotRect, tMin, tMax, yMin, yMax);

        // Y-axis labels (left)
        DrawYAxisLabels(plotRect, yMin, yMax, yLabel, false);

        // Y-axis labels (right, if dual)
        if (dualAxis)
            DrawYAxisLabels(plotRect, y2Min, y2Max, y2Label, true);

        // X-axis labels (time)
        DrawXAxisLabels(plotRect, tMin, tMax);

        // Draw traces
        for (int t = 0; t < traces.Length; t++)
        {
            DrawTrace(plotRect, timeData, traces[t].Data, traces[t].Color,
                yMin, yMax, tMin, tMax);
        }

        // Legend (above plot area)
        DrawLegend(area, traces, dualAxis, y2Label);
    }

    // ========================================================================
    // GRID LINES
    // ========================================================================

    void DrawGridLines(Rect plotRect, float tMin, float tMax, float yMin, float yMax)
    {
        Color gridC = _cTraceGrid;

        // Horizontal grid
        int yDivs = (int)GRAPH_GRID_LINES_Y;
        for (int i = 0; i <= yDivs; i++)
        {
            float frac = (float)i / yDivs;
            float py = plotRect.yMax - frac * plotRect.height;
            DrawLine(new Vector2(plotRect.x, py), new Vector2(plotRect.xMax, py), gridC, 1f);
        }

        // Vertical grid
        int xDivs = (int)GRAPH_GRID_LINES_X;
        for (int i = 0; i <= xDivs; i++)
        {
            float frac = (float)i / xDivs;
            float px = plotRect.x + frac * plotRect.width;
            DrawLine(new Vector2(px, plotRect.y), new Vector2(px, plotRect.yMax), gridC, 1f);
        }
    }

    // ========================================================================
    // AXIS LABELS
    // ========================================================================

    void DrawYAxisLabels(Rect plotRect, float yMin, float yMax, string label, bool rightSide)
    {
        int divs = (int)GRAPH_GRID_LINES_Y;
        float labelW = GRAPH_MARGIN_LEFT - 6f;

        for (int i = 0; i <= divs; i++)
        {
            float frac = (float)i / divs;
            float val = yMin + frac * (yMax - yMin);
            float py = plotRect.yMax - frac * plotRect.height;

            string text;
            if (yMax - yMin > 500f)
                text = val.ToString("F0");
            else if (yMax - yMin > 50f)
                text = val.ToString("F0");
            else
                text = val.ToString("F1");

            Rect labelRect;
            // v0.9.4: Use cached styles instead of creating new GUIStyle each frame
            GUIStyle style;

            if (rightSide)
            {
                labelRect = new Rect(plotRect.xMax + 4f, py - 7f, GRAPH_MARGIN_RIGHT_DUAL - 8f, 14f);
                style = _graphAxisStyleRight;  // v0.9.4: Cached style
            }
            else
            {
                labelRect = new Rect(plotRect.x - labelW - 2f, py - 7f, labelW, 14f);
                style = _graphAxisStyle;
            }

            GUI.Label(labelRect, text, style);
        }

        // Axis title (rotated via vertical label placement)
        float titleY = plotRect.y + plotRect.height * 0.5f - 30f;
        if (rightSide)
        {
            Rect titleRect = new Rect(plotRect.xMax + 4f, plotRect.y - 14f,
                GRAPH_MARGIN_RIGHT_DUAL - 8f, 14f);
            GUI.Label(titleRect, label, _graphLabelStyle);
        }
        else
        {
            Rect titleRect = new Rect(plotRect.x - labelW - 2f, plotRect.y - 14f, labelW, 14f);
            GUI.Label(titleRect, label, _graphAxisStyle);
        }
    }

    void DrawXAxisLabels(Rect plotRect, float tMin, float tMax)
    {
        // v0.8.2: Show relative time from -240 to NOW (0)
        int divs = (int)GRAPH_GRID_LINES_X;
        float windowMinutes = (tMax - tMin) * 60f;  // Total window in minutes
        
        for (int i = 0; i <= divs; i++)
        {
            float frac = (float)i / divs;
            // Minutes ago: left edge = -windowMinutes, right edge = 0 (NOW)
            float minutesAgo = windowMinutes * (1f - frac);
            float px = plotRect.x + frac * plotRect.width;

            string text;
            if (minutesAgo < 0.5f)
                text = "NOW";
            else
                text = $"-{minutesAgo:F0}";

            Rect labelRect = new Rect(px - 20f, plotRect.yMax + 2f, 40f, 14f);
            // v0.9.4: Use cached style instead of creating new GUIStyle each frame
            GUI.Label(labelRect, text, _graphAxisStyleCenter);
        }

        // X-axis title
        Rect xTitleRect = new Rect(plotRect.x, plotRect.yMax + 14f, plotRect.width, 14f);
        // v0.9.4: Use cached style
        GUI.Label(xTitleRect, "TIME (minutes ago)", _graphLabelStyleCenter);
    }

    // ========================================================================
    // TRACE RENDERING — GL line strip for each data series
    // ========================================================================

    void DrawTrace(Rect plotRect, List<float> timeData, List<float> valueData,
        Color color, float yMin, float yMax, float tMin, float tMax)
    {
        if (valueData == null || valueData.Count < 2) return;

        int count = Mathf.Min(timeData.Count, valueData.Count);
        if (count < 2) return;

        float yRange = yMax - yMin;
        float tRange = tMax - tMin;
        if (yRange <= 0f || tRange <= 0f) return;

        GetGLMaterial().SetPass(0);
        GL.PushMatrix();
        GL.LoadPixelMatrix();
        GL.Begin(GL.LINE_STRIP);
        GL.Color(color);

        for (int i = 0; i < count; i++)
        {
            float tx = (timeData[i] - tMin) / tRange;
            float ty = (valueData[i] - yMin) / yRange;

            // Clamp to plot area
            tx = Mathf.Clamp01(tx);
            ty = Mathf.Clamp01(ty);

            float px = plotRect.x + tx * plotRect.width;
            float py = plotRect.yMax - ty * plotRect.height;

            GL.Vertex3(px, py, 0f);
        }

        GL.End();
        GL.PopMatrix();

        // Draw thicker by repeating with 1px offset
        GL.PushMatrix();
        GL.LoadPixelMatrix();
        GL.Begin(GL.LINE_STRIP);
        GL.Color(color);

        for (int i = 0; i < count; i++)
        {
            float tx = Mathf.Clamp01((timeData[i] - tMin) / tRange);
            float ty = Mathf.Clamp01((valueData[i] - yMin) / yRange);

            float px = plotRect.x + tx * plotRect.width;
            float py = plotRect.yMax - ty * plotRect.height + 1f;

            GL.Vertex3(px, py, 0f);
        }

        GL.End();
        GL.PopMatrix();
    }

    // ========================================================================
    // SECONDARY AXIS TRACES — Draw traces scaled to the right Y-axis
    // ========================================================================

    void DrawTracesOnSecondaryAxis(Rect area, List<float> timeData,
        TraceDescriptor[] traces, float y2Min, float y2Max)
    {
        Rect plotRect = GetPlotRect(area, true);
        
        // v0.8.2: Use same fixed 240-minute window as primary axis
        const float WINDOW_HOURS = 4.0f;
        float tMax = engine.simTime;
        float tMin = tMax - WINDOW_HOURS;
        if (tMin < 0f) tMin = 0f;

        for (int t = 0; t < traces.Length; t++)
        {
            DrawTrace(plotRect, timeData, traces[t].Data, traces[t].Color,
                y2Min, y2Max, tMin, tMax);
        }
    }

    // ========================================================================
    // LEGEND — Color-coded trace labels above the plot
    // ========================================================================

    void DrawLegend(Rect area, TraceDescriptor[] traces, bool dualAxis, string y2Label)
    {
        float x = area.x + GRAPH_MARGIN_LEFT;
        float y = area.y + GRAPH_MARGIN_TOP;
        float swatchW = 14f;
        float swatchH = 10f;
        float gap = 8f;

        for (int i = 0; i < traces.Length; i++)
        {
            // Color swatch
            DrawFilledRect(new Rect(x, y + 4f, swatchW, swatchH), traces[i].Color);
            x += swatchW + 3f;

            // Label with current value
            float currentVal = 0f;
            if (traces[i].Data != null && traces[i].Data.Count > 0)
                currentVal = traces[i].Data[traces[i].Data.Count - 1];

            string legendText = $"{traces[i].Label}: {currentVal.ToString(traces[i].Format)} {traces[i].Unit}";
            Vector2 textSize = _graphLabelStyle.CalcSize(new GUIContent(legendText));

            var prev = GUI.contentColor;
            GUI.contentColor = traces[i].Color;
            GUI.Label(new Rect(x, y, textSize.x + 4f, GRAPH_LEGEND_H), legendText, _graphLabelStyle);
            GUI.contentColor = prev;

            x += textSize.x + gap + 4f;
        }
    }

    // ========================================================================
    // REFERENCE LINES — Horizontal reference / limit lines
    // ========================================================================

    void DrawHorizontalRef(Rect area, List<float> timeData,
        float yMin, float yMax, float refValue, Color color, string label)
    {
        bool dual = false;  // Refs drawn on primary axis
        Rect plotRect = GetPlotRect(area, dual);

        float yRange = yMax - yMin;
        if (yRange <= 0f) return;

        float frac = (refValue - yMin) / yRange;
        if (frac < -0.05f || frac > 1.05f) return;

        float py = plotRect.yMax - Mathf.Clamp01(frac) * plotRect.height;

        // v0.9.4: Draw line with reduced alpha directly (no new Color allocation)
        // Use GL directly with alpha-modified color to avoid GetColorTex issue
        DrawLineWithAlpha(new Vector2(plotRect.x, py), new Vector2(plotRect.xMax, py), color, 0.6f, 1f);

        // Label
        var prev = GUI.contentColor;
        GUI.contentColor = color;
        GUI.Label(new Rect(plotRect.xMax - 120f, py - 14f, 116f, 14f), label, _graphLabelStyle);
        GUI.contentColor = prev;
    }

    // ========================================================================
    // LIVE ANNOTATION — Show current value as a horizontal marker for
    // parameters that lack history buffers (T_pzr, T_sat, etc.)
    // ========================================================================

    void DrawLiveAnnotation(Rect area, List<float> timeData,
        float yMin, float yMax, float currentValue, Color color, string label)
    {
        Rect plotRect = GetPlotRect(area, false);

        float yRange = yMax - yMin;
        if (yRange <= 0f) return;

        float frac = (currentValue - yMin) / yRange;
        if (frac < -0.05f || frac > 1.05f) return;

        float py = plotRect.yMax - Mathf.Clamp01(frac) * plotRect.height;

        // Short marker line on right edge
        // v0.9.4: Use DrawLineWithAlpha to avoid new Color allocation
        float markerLen = 24f;
        DrawLineWithAlpha(
            new Vector2(plotRect.xMax - markerLen, py),
            new Vector2(plotRect.xMax, py),
            color, 0.7f, 2f);

        // Diamond marker
        DrawLine(
            new Vector2(plotRect.xMax - markerLen - 4f, py),
            new Vector2(plotRect.xMax - markerLen, py),
            color, 2f);

        // Label with value
        var prev = GUI.contentColor;
        GUI.contentColor = color;
        string text = $"{label} {currentValue:F1}";
        GUI.Label(new Rect(plotRect.xMax - markerLen - 80f, py - 7f, 76f, 14f),
            text, _graphLabelStyle);
        GUI.contentColor = prev;
    }

    // ========================================================================
    // UTILITY — Plot rect calculation, auto-ranging
    // ========================================================================

    /// <summary>
    /// Calculate the inner plot rectangle from the outer area.
    /// </summary>
    Rect GetPlotRect(Rect area, bool dualAxis)
    {
        float rightM = dualAxis ? GRAPH_MARGIN_RIGHT_DUAL : GRAPH_MARGIN_RIGHT;
        float legendH = GRAPH_LEGEND_H;
        return new Rect(
            area.x + GRAPH_MARGIN_LEFT,
            area.y + GRAPH_MARGIN_TOP + legendH,
            area.width - GRAPH_MARGIN_LEFT - rightM,
            area.height - GRAPH_MARGIN_TOP - GRAPH_MARGIN_BOTTOM - legendH);
    }

    /// <summary>
    /// Auto-compute Y-axis range from trace data with padding.
    /// Falls back to absolute min/max if data is empty.
    /// </summary>
    void AutoRangeY(TraceDescriptor[] traces, out float yMin, out float yMax,
        float absMin, float absMax, float padding)
    {
        float dataMin = float.MaxValue;
        float dataMax = float.MinValue;
        bool hasData = false;

        for (int t = 0; t < traces.Length; t++)
        {
            var data = traces[t].Data;
            if (data == null) continue;
            for (int i = 0; i < data.Count; i++)
            {
                float v = data[i];
                if (v < dataMin) dataMin = v;
                if (v > dataMax) dataMax = v;
                hasData = true;
            }
        }

        if (!hasData)
        {
            yMin = absMin;
            yMax = absMax;
            return;
        }

        yMin = dataMin - padding;
        yMax = dataMax + padding;

        // Ensure minimum range
        if (yMax - yMin < padding * 2f)
        {
            float mid = (yMin + yMax) * 0.5f;
            yMin = mid - padding;
            yMax = mid + padding;
        }

        // Snap to nice round numbers
        yMin = SnapFloor(yMin, padding);
        yMax = SnapCeil(yMax, padding);

        // Clamp to absolute bounds
        yMin = Mathf.Max(yMin, absMin);
        yMax = Mathf.Min(yMax, absMax);

        if (yMax <= yMin) yMax = yMin + padding;
    }

    /// <summary>Snap value down to nearest multiple of step.</summary>
    static float SnapFloor(float value, float step)
    {
        if (step <= 0f) return value;
        return Mathf.Floor(value / step) * step;
    }

    /// <summary>Snap value up to nearest multiple of step.</summary>
    static float SnapCeil(float value, float step)
    {
        if (step <= 0f) return value;
        return Mathf.Ceil(value / step) * step;
    }

    // ========================================================================
    // TAB 6: HZP — v1.1.0 Steam Dump, Heater PID, HZP Progress
    // Dual Y-axis: Heat (MW) + Progress (%)
    // ========================================================================

    void DrawHZPGraph(Rect area, List<float> timeData)
    {
        // Primary axis: Heat removal (MW) + Heater PID output (scaled)
        var heatTraces = new TraceDescriptor[]
        {
            new TraceDescriptor("STEAM DUMP", _cOrangeAccent, engine.steamDumpHeatHistory, "F1", "MW"),
            new TraceDescriptor("HTR PID %",  _cTrace4,       engine.heaterPIDOutputHistory, "F2", ""),
        };

        // Note: Heater PID output is 0-1, we'll scale display to 0-100% on primary axis
        // Steam dump heat is 0-25 MW max, so we use a 0-25 range
        float hYMin = 0f, hYMax = 25f;

        // Secondary axis: HZP Progress (%) and Steam Pressure (scaled)
        var progressTraces = new TraceDescriptor[]
        {
            new TraceDescriptor("HZP PROG",    _cNormalGreen, engine.hzpProgressHistory,    "F0", "%"),
            new TraceDescriptor("STM PRESS/10", _cCyanInfo,    engine.steamPressureHistory, "F0", "psig/10"),
        };

        float pYMin = 0f, pYMax = 120f;  // 100% progress + headroom for steam pressure/10

        // Draw primary axis plot
        DrawPlotArea(area, timeData, heatTraces, hYMin, hYMax, "HEAT (MW)",
            true, pYMin, pYMax, "PROGRESS (%)");

        // Draw secondary traces (progress and scaled steam pressure)
        // Steam pressure needs to be scaled by /10 for display (1100 psig -> 110)
        DrawTracesOnSecondaryAxis(area, timeData, progressTraces, pYMin, pYMax);

        // Reference lines
        // 100% progress line on secondary axis
        Rect plotRect = GetPlotRect(area, true);
        float prog100Y = plotRect.yMax - ((100f - pYMin) / (pYMax - pYMin)) * plotRect.height;
        if (prog100Y >= plotRect.y && prog100Y <= plotRect.yMax)
        {
            DrawLineWithAlpha(new Vector2(plotRect.x, prog100Y),
                     new Vector2(plotRect.xMax, prog100Y),
                     _cNormalGreen, 0.4f, 1f);
        }

        // Live HZP status annotation in top-right of plot
        string hzpInfo = $"HZP: {engine.GetHZPStatusString()}";
        if (engine.IsHZPActive())
        {
            hzpInfo += $"  |  Steam Dump: {engine.steamDumpHeat_MW:F1} MW";
            hzpInfo += $"  |  PID: {engine.heaterPIDOutput * 100f:F0}%";
        }
        var prev = GUI.contentColor;
        Color infoC = engine.hzpReadyForStartup ? _cNormalGreen :
                      engine.IsHZPActive() ? _cCyanInfo : _cTextSecondary;
        GUI.contentColor = infoC;
        GUI.Label(new Rect(plotRect.x + 4f, plotRect.y + 2f, plotRect.width - 8f, 16f),
            hzpInfo, _graphLabelStyle);
        GUI.contentColor = prev;

        // Show startup readiness if at HZP
        if (engine.hzpReadyForStartup)
        {
            var prevC = GUI.contentColor;
            GUI.contentColor = _cNormalGreen;
            GUI.Label(new Rect(plotRect.x + 4f, plotRect.y + 18f, plotRect.width - 8f, 16f),
                "✓ READY FOR REACTOR STARTUP", _graphLabelStyle);
            GUI.contentColor = prevC;
        }
    }
}
