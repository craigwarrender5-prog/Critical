// ============================================================================
// CRITICAL: Master the Atom - UI Component (Gauges Partial)
// HeatupValidationVisual.Gauges.cs - Instrument Gauge Panels
// ============================================================================
//
// PURPOSE:
//   Renders 5 gauge groups (~20 individual instruments) in the left column
//   of the heatup validation dashboard. Each group contains 2–3 arc gauges
//   for primary parameters and mini-bar gauges for secondary parameters.
//   All ranges, setpoints, and alarm thresholds are sourced from
//   PlantConstants to match Westinghouse 4-Loop PWR specifications.
//
// READS FROM:
//   HeatupSimEngine — T_avg, T_hot, T_cold, T_pzr, T_sat, subcooling,
//     heatupRate, pressure, pzrLevel, pzrHeaterPower, pressureRate,
//     chargingFlow, letdownFlow, surgeFlow, rcpCount, rcpHeat,
//     effectiveRCPHeat, rcpContribution, vctState, brsState,
//     gridEnergy, solidPressurizer, bubbleFormed
//
// GAUGE GROUPS:
//   1. TEMPERATURES — T_avg, T_hot, T_cold, T_pzr, Subcooling, Heatup Rate
//   2. PRESSURIZER  — RCS Pressure, PZR Level, Heater Power, Pressure Rate
//   3. CVCS FLOWS   — Charging, Letdown, Surge, Net CVCS, Seal Injection
//   4. VCT & BRS    — VCT Level, BRS Holdup, BRS Flow (bidirectional)
//   5. RCP / HEAT   — RCP Heat, Coupling α, Flow Fraction, Grid Energy
//   6. HZP STAB     — v1.1.0 Steam Dump Heat, HZP Progress, Steam Press, PID Output
//
// REFERENCE:
//   NRC HRTD Sections 4.1, 6.1, 10.2, 10.3, 19.0, 19.2
//   Westinghouse 4-Loop FSAR — instrumentation ranges
//
// ARCHITECTURE:
//   Partial class of HeatupValidationVisual. Implements:
//     - DrawGaugeColumnContent() — dispatched from Core
//     - GetGaugeContentHeightPartial() — tells scroll view total height
//     - Per-group drawing methods (DrawTemperatureGauges, etc.)
//     - DrawGaugeArcBidirectional() — center-zero bidirectional arc gauge
//
// VERSION: 0.9.1 — Consolidated VCT & BRS into single group with bidirectional flow gauge
// GOLD STANDARD: Yes
// ============================================================================

using UnityEngine;
using Critical.Physics;

public partial class HeatupValidationVisual
{
    // ========================================================================
    // GAUGE GROUP HEIGHTS — Pre-calculated for scroll view sizing
    // Each group = header + arc rows + mini-bar rows + gap
    // ========================================================================

    // Heights per group (arc rows hold 2-3 gauges side by side)
    // v0.8.0: Added T_SG mini bar to temperature group
    // v0.9.1: Consolidated VCT + BRS into single LIQUID_INV group
    // v1.1.0: Added HZP group for steam dump and HZP systems
    const float TEMP_GROUP_H       = GAUGE_GROUP_HEADER_H + GAUGE_ROW_H * 2 + 20f * 3 + GAUGE_GROUP_GAP; // 2 arc rows + 3 bars
    const float PZR_GROUP_H        = GAUGE_GROUP_HEADER_H + GAUGE_ROW_H + 20f * 3 + GAUGE_GROUP_GAP;     // 1 arc row + 3 bars
    const float CVCS_GROUP_H       = GAUGE_GROUP_HEADER_H + GAUGE_ROW_H + 20f * 4 + GAUGE_GROUP_GAP;     // 1 arc row + 4 bars
    const float LIQUID_INV_GROUP_H = GAUGE_GROUP_HEADER_H + GAUGE_ROW_H + 20f * 2 + GAUGE_GROUP_GAP;     // 1 arc row (3 gauges) + 2 bars
    const float RCP_GROUP_H        = GAUGE_GROUP_HEADER_H + GAUGE_ROW_H + 20f * 3 + GAUGE_GROUP_GAP;     // 1 arc row + 3 bars
    const float HZP_GROUP_H        = GAUGE_GROUP_HEADER_H + GAUGE_ROW_H + 20f * 3 + GAUGE_GROUP_GAP;     // v1.1.0: 1 arc row + 3 bars

    const float TOTAL_GAUGE_H = TEMP_GROUP_H + PZR_GROUP_H + CVCS_GROUP_H
                                + LIQUID_INV_GROUP_H + RCP_GROUP_H + HZP_GROUP_H + 20f;

    // ========================================================================
    // PARTIAL METHOD IMPLEMENTATIONS — Called by Core
    // ========================================================================

    partial void GetGaugeContentHeightPartial(ref float height)
    {
        height = TOTAL_GAUGE_H;
    }

    partial void DrawGaugeColumnContent(Rect area)
    {
        if (engine == null) return;

        float y = area.y;
        float w = area.width;

        DrawTemperatureGauges(area.x, ref y, w);
        DrawPressurizerGauges(area.x, ref y, w);
        DrawCVCSFlowGauges(area.x, ref y, w);
        DrawLiquidInventoryGauges(area.x, ref y, w);  // v0.9.1: Combined VCT + BRS
        DrawRCPHeatGauges(area.x, ref y, w);
        DrawHZPGauges(area.x, ref y, w);  // v1.1.0: HZP stabilization
    }

    // ========================================================================
    // GROUP 1: TEMPERATURES
    //   Arc gauges: T_avg, T_hot, T_cold (row 1), T_pzr, Subcooling (row 2)
    //   Mini bars: Heatup Rate, PZR-RCS Delta
    // ========================================================================

    void DrawTemperatureGauges(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x, y, w, GAUGE_GROUP_HEADER_H), "TEMPERATURES");
        y += GAUGE_GROUP_HEADER_H;

        float arcR = GAUGE_ARC_SIZE / 2f;
        float cellW = w / 3f;

        // ROW 1: T_avg, T_hot, T_cold
        {
            float rowY = y;
            // T_avg
            Color tavgC = GetThresholdColor(engine.T_avg, 150f, 560f, 100f, 580f,
                _cNormalGreen, _cWarningAmber, _cAlarmRed);
            DrawGaugeArc(
                new Vector2(x + cellW * 0.5f, rowY + arcR + 14f), arcR,
                engine.T_avg, 50f, 600f, tavgC,
                "T-AVG", $"{engine.T_avg:F1}", "°F");

            // T_hot
            Color thotC = GetHighThresholdColor(engine.T_hot, 600f, 630f,
                _cNormalGreen, _cWarningAmber, _cAlarmRed);
            DrawGaugeArc(
                new Vector2(x + cellW * 1.5f, rowY + arcR + 14f), arcR,
                engine.T_hot, 50f, 650f, thotC,
                "T-HOT", $"{engine.T_hot:F1}", "°F");

            // T_cold
            DrawGaugeArc(
                new Vector2(x + cellW * 2.5f, rowY + arcR + 14f), arcR,
                engine.T_cold, 50f, 600f, _cTrace3,
                "T-COLD", $"{engine.T_cold:F1}", "°F");

            y += GAUGE_ROW_H;
        }

        // ROW 2: T_pzr, Subcooling
        {
            float rowY = y;
            float cell2W = w / 2f;

            // T_pzr
            Color tpzrC = _cTrace4;
            DrawGaugeArc(
                new Vector2(x + cell2W * 0.5f, rowY + arcR + 14f), arcR,
                engine.T_pzr, 50f, 700f, tpzrC,
                "T-PZR", $"{engine.T_pzr:F1}", "°F");

            // Subcooling — LOW is bad
            Color scC = GetLowThresholdColor(engine.subcooling, 30f, 15f,
                _cNormalGreen, _cWarningAmber, _cAlarmRed);
            DrawGaugeArc(
                new Vector2(x + cell2W * 1.5f, rowY + arcR + 14f), arcR,
                engine.subcooling, 0f, 200f, scC,
                "SUBCOOLING", $"{engine.subcooling:F1}", "°F");

            y += GAUGE_ROW_H;
        }

        // Mini bars: Heatup Rate, PZR-RCS Delta, RCS-SG Delta (v0.8.0)
        {
            float barH = 18f;

            // Heatup Rate — HIGH is bad (Tech Spec limit 50 °F/hr)
            Color rateC = GetHighThresholdColor(Mathf.Abs(engine.heatupRate), 40f, 50f,
                _cNormalGreen, _cWarningAmber, _cAlarmRed);
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "HEATUP RATE", engine.heatupRate, 0f, 60f, rateC, "F1", "°F/hr");
            y += barH + 2f;

            // PZR-RCS Delta T
            float deltaT = engine.T_pzr - engine.T_rcs;
            Color dtC = Mathf.Abs(deltaT) > 200f ? _cAlarmRed :
                        Mathf.Abs(deltaT) > 100f ? _cWarningAmber : _cCyanInfo;
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "PZR-RCS ΔT", deltaT, -50f, 350f, dtC, "F1", "°F");
            y += barH + 2f;

            // v0.8.0: RCS-SG Secondary Delta T (thermal lag indicator)
            float deltaTsg = engine.T_rcs - engine.T_sg_secondary;
            // Healthy thermal lag is 10-20°F when RCPs running; warn if >30°F or <5°F (poor coupling)
            Color sgDtC;
            if (engine.rcpCount == 0)
                sgDtC = _cTextSecondary;  // Inactive when no RCPs
            else if (deltaTsg > 30f || deltaTsg < 5f)
                sgDtC = _cWarningAmber;
            else
                sgDtC = _cCyanInfo;
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "RCS-SG ΔT", deltaTsg, 0f, 50f, sgDtC, "F1", "°F");
            y += barH + 2f;
        }

        y += GAUGE_GROUP_GAP;
    }

    // ========================================================================
    // GROUP 2: PRESSURIZER
    //   Arc gauges: RCS Pressure, PZR Level
    //   Mini bars: Heater Power, Pressure Rate, Heater Mode
    // ========================================================================

    void DrawPressurizerGauges(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x, y, w, GAUGE_GROUP_HEADER_H), "PRESSURIZER");
        y += GAUGE_GROUP_HEADER_H;

        float arcR = GAUGE_ARC_SIZE / 2f;
        float cell2W = w / 2f;

        // ROW: Pressure, PZR Level
        {
            float rowY = y;

            // RCS Pressure — range depends on operating phase
            float pMax = engine.solidPressurizer ? 600f : 2500f;
            Color pC;
            if (engine.solidPressurizer)
            {
                // Solid plant: 320-450 psig band (334.7-464.7 psia)
                pC = GetThresholdColor(engine.pressure,
                    PlantConstants.SOLID_PLANT_P_LOW_PSIA, PlantConstants.SOLID_PLANT_P_HIGH_PSIA,
                    PlantConstants.SOLID_PLANT_P_LOW_PSIA - 20f, PlantConstants.SOLID_PLANT_P_HIGH_PSIA + 20f,
                    _cNormalGreen, _cWarningAmber, _cAlarmRed);
            }
            else
            {
                pC = GetThresholdColor(engine.pressure, 300f, 2300f, 200f, 2400f,
                    _cNormalGreen, _cWarningAmber, _cAlarmRed);
            }
            DrawGaugeArc(
                new Vector2(x + cell2W * 0.5f, rowY + arcR + 14f), arcR,
                engine.pressure, 0f, pMax, pC,
                "RCS PRESS", $"{engine.pressure:F0}", "psia");

            // PZR Level — setpoint-relative coloring
            float lvlSetpoint = engine.pzrLevelSetpointDisplay;
            Color lvlC;
            float lvlErr = engine.pzrLevel - lvlSetpoint;
            if (Mathf.Abs(lvlErr) > 15f) lvlC = _cAlarmRed;
            else if (Mathf.Abs(lvlErr) > 8f) lvlC = _cWarningAmber;
            else lvlC = _cNormalGreen;

            DrawGaugeArc(
                new Vector2(x + cell2W * 1.5f, rowY + arcR + 14f), arcR,
                engine.pzrLevel, 0f, 100f, lvlC,
                "PZR LEVEL", $"{engine.pzrLevel:F1}", "%");

            y += GAUGE_ROW_H;
        }

        // Mini bars
        {
            float barH = 18f;

            // PZR Heater Power
            Color htrC = engine.pzrHeatersOn ? _cOrangeAccent : _cTextSecondary;
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "HTR POWER", engine.pzrHeaterPower * 1000f, 0f, 1800f, htrC, "F0", "kW");
            y += barH + 2f;

            // Pressure Rate
            Color prC = Mathf.Abs(engine.pressureRate) > 100f ? _cWarningAmber : _cCyanInfo;
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "PRESS RATE", engine.pressureRate, -200f, 200f, prC, "F1", "psi/hr");
            y += barH + 2f;

            // PZR Level Setpoint (reference line)
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "LVL SETPT", lvlSetpoint, 0f, 100f, _cTextSecondary, "F1", "%");
            y += barH + 2f;
        }

        y += GAUGE_GROUP_GAP;
    }

    // Cached for reuse in PZR group
    private float lvlSetpoint => engine != null ? engine.pzrLevelSetpointDisplay : 25f;

    // ========================================================================
    // GROUP 3: CVCS FLOWS
    //   Arc gauges: Charging, Letdown
    //   Mini bars: Surge Flow, Net CVCS, Seal Injection, Letdown Path
    // ========================================================================

    void DrawCVCSFlowGauges(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x, y, w, GAUGE_GROUP_HEADER_H), "CVCS FLOWS");
        y += GAUGE_GROUP_HEADER_H;

        float arcR = GAUGE_ARC_SIZE / 2f;
        float cell2W = w / 2f;

        // ROW: Charging, Letdown
        {
            float rowY = y;

            // Charging Flow
            Color chgC = engine.chargingActive ? _cNormalGreen : _cTextSecondary;
            DrawGaugeArc(
                new Vector2(x + cell2W * 0.5f, rowY + arcR + 14f), arcR,
                engine.chargingFlow, 0f, 120f, chgC,
                "CHARGING", $"{engine.chargingFlow:F1}", "gpm");

            // Letdown Flow
            Color ldC = engine.letdownActive ? _cBlueAccent : _cTextSecondary;
            if (engine.letdownIsolatedFlag) ldC = _cAlarmRed;
            DrawGaugeArc(
                new Vector2(x + cell2W * 1.5f, rowY + arcR + 14f), arcR,
                engine.letdownFlow, 0f, 120f, ldC,
                "LETDOWN", $"{engine.letdownFlow:F1}", "gpm");

            y += GAUGE_ROW_H;
        }

        // Mini bars
        {
            float barH = 18f;

            // Surge Flow (positive = outsurge, negative = insurge)
            Color surgeC = Mathf.Abs(engine.surgeFlow) > 20f ? _cWarningAmber : _cCyanInfo;
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "SURGE FLOW", engine.surgeFlow, -50f, 50f, surgeC, "F1", "gpm");
            y += barH + 2f;

            // Net CVCS (charging - letdown)
            float netCVCS = engine.chargingFlow - engine.letdownFlow;
            Color netC = netCVCS > 0 ? _cNormalGreen : _cWarningAmber;
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "NET CVCS", netCVCS, -50f, 50f, netC, "F1", "gpm");
            y += barH + 2f;

            // Seal Injection
            float sealInj = engine.rcpCount * PlantConstants.SEAL_INJECTION_PER_PUMP_GPM;
            Color sealC = engine.sealInjectionOK ? _cNormalGreen : _cAlarmRed;
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "SEAL INJ", sealInj, 0f, 40f, sealC, "F0", "gpm");
            y += barH + 2f;

            // Letdown Path indicator (text row)
            string pathStr = engine.letdownIsolatedFlag ? "ISOLATED" :
                             engine.letdownViaRHR ? "RHR XCONN" : "ORIFICE";
            Color pathC = engine.letdownIsolatedFlag ? _cAlarmRed :
                          engine.letdownViaRHR ? _cCyanInfo : _cNormalGreen;
            GUI.Label(new Rect(x + 2f, y, 90f, barH), "LD PATH", _statusLabelStyle);
            var prev = GUI.contentColor;
            GUI.contentColor = pathC;
            GUI.Label(new Rect(x + 94f, y, w - 96f, barH), pathStr, _statusValueStyle);
            GUI.contentColor = prev;
            y += barH + 2f;
        }

        y += GAUGE_GROUP_GAP;
    }

    // ========================================================================
    // GROUP 4: VCT & BRS — LIQUID INVENTORY (v0.9.1 Combined)
    //   Arc gauges: VCT Level, BRS Holdup, BRS Flow (bidirectional center-zero)
    //   Mini bars: VCT Boron, Mass Conservation Error
    //
    // The BRS Flow gauge uses a bidirectional arc with center-zero:
    //   - Needle at 12 o'clock (straight up) = 0 gpm (no flow)
    //   - Needle deflects RIGHT = positive flow (inflow to BRS from VCT divert)
    //   - Needle deflects LEFT = negative flow (outflow from BRS to VCT/plant)
    //
    // Per NRC HRTD 4.1: BRS inflow (divert) and outflow (return) are mutually
    // exclusive — VCT cannot be >70% (divert) and <27% (makeup) simultaneously.
    // ========================================================================

    void DrawLiquidInventoryGauges(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x, y, w, GAUGE_GROUP_HEADER_H),
            "VCT & BRS — LIQUID INVENTORY");
        y += GAUGE_GROUP_HEADER_H;

        float arcR = GAUGE_ARC_SIZE / 2f;
        float cell3W = w / 3f;

        // ROW: VCT Level (left), BRS Holdup (center), BRS Flow (right, bidirectional)
        {
            float rowY = y;

            // VCT Level — band-based coloring per NRC HRTD 4.1 setpoints
            float vl = engine.vctState.Level_percent;
            Color vlC;
            if (vl < PlantConstants.VCT_LEVEL_LOW_LOW || vl > PlantConstants.VCT_LEVEL_HIGH_HIGH)
                vlC = _cAlarmRed;
            else if (vl < PlantConstants.VCT_LEVEL_LOW || vl > PlantConstants.VCT_LEVEL_HIGH)
                vlC = _cWarningAmber;
            else if (vl >= PlantConstants.VCT_LEVEL_NORMAL_LOW && vl <= PlantConstants.VCT_LEVEL_NORMAL_HIGH)
                vlC = _cNormalGreen;
            else
                vlC = _cCyanInfo;

            DrawGaugeArc(
                new Vector2(x + cell3W * 0.5f, rowY + arcR + 14f), arcR,
                vl, 0f, 100f, vlC,
                "VCT LEVEL", $"{vl:F1}", "%");

            // BRS Holdup Level
            float holdupPct = BRSPhysics.GetHoldupLevelPercent(engine.brsState);
            Color huC;
            if (holdupPct > PlantConstants.BRS_HOLDUP_HIGH_LEVEL_PCT)
                huC = _cWarningAmber;
            else if (holdupPct < PlantConstants.BRS_HOLDUP_LOW_LEVEL_PCT && engine.brsState.ProcessingActive)
                huC = _cWarningAmber;
            else if (engine.brsState.ProcessingActive)
                huC = _cOrangeAccent;
            else
                huC = _cCyanInfo;

            DrawGaugeArc(
                new Vector2(x + cell3W * 1.5f, rowY + arcR + 14f), arcR,
                holdupPct, 0f, 100f, huC,
                "BRS HOLDUP", $"{holdupPct:F1}", "%");

            // BRS Flow — Bidirectional center-zero gauge
            // Positive = inflow to BRS (divert from VCT)
            // Negative = outflow from BRS (return to VCT/plant)
            float brsNetFlow = engine.brsState.InFlow_gpm - engine.brsState.ReturnFlow_gpm;

            DrawGaugeArcBidirectional(
                new Vector2(x + cell3W * 2.5f, rowY + arcR + 14f), arcR,
                brsNetFlow, -40f, 40f,
                _cOrangeAccent,   // Positive (inflow) color
                _cBlueAccent,     // Negative (outflow) color
                "BRS FLOW", brsNetFlow, "gpm");

            y += GAUGE_ROW_H;
        }

        // Mini bars
        {
            float barH = 18f;

            // VCT Boron concentration
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "VCT BORON", engine.vctState.BoronConcentration_ppm, 0f, 3000f,
                _cCyanInfo, "F0", "ppm");
            y += barH + 2f;

            // Mass Conservation Error
            Color consC = engine.massConservationError < 10f ? _cNormalGreen :
                          engine.massConservationError < 50f ? _cWarningAmber : _cAlarmRed;
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "MASS CONS", engine.massConservationError, 0f, 100f, consC, "F1", "gal");
            y += barH + 2f;
        }

        y += GAUGE_GROUP_GAP;
    }

    // ========================================================================
    // GROUP 5: RCP / HEAT SOURCES
    //   Arc gauge: Effective RCP Heat (MW)
    //   Mini bars: Coupling Factor α, Total Flow Fraction, Grid Energy
    // ========================================================================

    void DrawRCPHeatGauges(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x, y, w, GAUGE_GROUP_HEADER_H), "RCP / HEAT SOURCES");
        y += GAUGE_GROUP_HEADER_H;

        float arcR = GAUGE_ARC_SIZE / 2f;

        // ROW: Effective RCP Heat (centered)
        {
            float rowY = y;

            float effHeat = engine.effectiveRCPHeat;
            float maxHeat = PlantConstants.RCP_HEAT_MW;  // 21 MW
            Color heatC;
            if (engine.rcpCount == 0)
                heatC = _cTextSecondary;
            else if (engine.rcpContribution.AllFullyRunning)
                heatC = _cNormalGreen;
            else
                heatC = _cCyanInfo;  // Ramping

            string heatLabel = engine.rcpCount > 0
                ? $"{effHeat:F1} / {engine.rcpCount * PlantConstants.RCP_HEAT_MW_EACH:F1}"
                : "0.0";

            DrawGaugeArc(
                new Vector2(x + w * 0.5f, rowY + arcR + 14f), arcR,
                effHeat, 0f, maxHeat + 4f, heatC,
                $"RCP HEAT ({engine.rcpCount}/4)", heatLabel, "MW");

            y += GAUGE_ROW_H;
        }

        // Mini bars
        {
            float barH = 18f;

            // Coupling Factor α
            float alpha = Mathf.Min(1.0f, engine.rcpContribution.TotalFlowFraction);
            Color alphaC = alpha > 0.99f ? _cNormalGreen :
                           alpha > 0.01f ? _cCyanInfo : _cTextSecondary;
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "COUPLING α", alpha, 0f, 1f, alphaC, "F2", "");
            y += barH + 2f;

            // Total Flow Fraction (sum across all pumps, 0.0–4.0)
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "FLOW FRAC", engine.rcpContribution.TotalFlowFraction, 0f, 4f,
                engine.rcpCount > 0 ? _cBlueAccent : _cTextSecondary, "F2", "");
            y += barH + 2f;

            // Grid Energy (cumulative MWh)
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "GRID ENERGY", engine.gridEnergy, 0f, 500f,
                _cTextPrimary, "F0", "MWh");
            y += barH + 2f;
        }

        y += GAUGE_GROUP_GAP;
    }

    // ========================================================================
    // GROUP 6: HZP STABILIZATION — v1.1.0
    //   Arc gauges: Steam Dump Heat (MW), HZP Progress (%)
    //   Mini bars: Steam Pressure, Heater PID Output, HZP State
    // ========================================================================

    void DrawHZPGauges(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x, y, w, GAUGE_GROUP_HEADER_H), "HZP STABILIZATION (v1.1.0)");
        y += GAUGE_GROUP_HEADER_H;

        float arcR = GAUGE_ARC_SIZE / 2f;
        float cell2W = w / 2f;

        // ROW: Steam Dump Heat (left), HZP Progress (right)
        {
            float rowY = y;

            // Steam Dump Heat Removal (MW)
            Color sdHeatC;
            if (!engine.IsHZPActive())
                sdHeatC = _cTextSecondary;
            else if (engine.steamDumpHeat_MW > 0.1f)
                sdHeatC = _cOrangeAccent;
            else
                sdHeatC = _cCyanInfo;

            DrawGaugeArc(
                new Vector2(x + cell2W * 0.5f, rowY + arcR + 14f), arcR,
                engine.steamDumpHeat_MW, 0f, 25f, sdHeatC,
                "STM DUMP", $"{engine.steamDumpHeat_MW:F1}", "MW");

            // HZP Progress (%)
            Color hzpProgC;
            if (!engine.IsHZPActive())
                hzpProgC = _cTextSecondary;
            else if (engine.hzpProgress >= 100f)
                hzpProgC = _cNormalGreen;
            else if (engine.hzpProgress >= 50f)
                hzpProgC = _cCyanInfo;
            else
                hzpProgC = _cWarningAmber;

            DrawGaugeArc(
                new Vector2(x + cell2W * 1.5f, rowY + arcR + 14f), arcR,
                engine.hzpProgress, 0f, 100f, hzpProgC,
                "HZP PROG", $"{engine.hzpProgress:F0}", "%");

            y += GAUGE_ROW_H;
        }

        // Mini bars
        {
            float barH = 18f;

            // Steam Pressure (psig)
            Color spC = engine.steamPressure_psig > 1000f ? _cNormalGreen :
                        engine.steamPressure_psig > 500f ? _cCyanInfo : _cTextSecondary;
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "STM PRESS", engine.steamPressure_psig, 0f, 1200f, spC, "F0", "psig");
            y += barH + 2f;

            // Heater PID Output (%)
            float pidPct = engine.heaterPIDOutput * 100f;
            Color pidC = engine.heaterPIDActive ? _cNormalGreen : _cTextSecondary;
            DrawMiniBar(new Rect(x + 2f, y, w - 4f, barH),
                "HTR PID", pidPct, 0f, 100f, pidC, "F0", "%");
            y += barH + 2f;

            // HZP State indicator (text row)
            string stateStr = engine.GetHZPStatusString();
            Color stateC;
            if (engine.hzpReadyForStartup)
                stateC = _cNormalGreen;
            else if (engine.hzpStable)
                stateC = _cCyanInfo;
            else if (engine.IsHZPActive())
                stateC = _cWarningAmber;
            else
                stateC = _cTextSecondary;

            GUI.Label(new Rect(x + 2f, y, 70f, barH), "STATE", _statusLabelStyle);
            var prev = GUI.contentColor;
            GUI.contentColor = stateC;
            // Truncate state string if too long
            if (stateStr.Length > 25) stateStr = stateStr.Substring(0, 25) + "...";
            GUI.Label(new Rect(x + 74f, y, w - 76f, barH), stateStr, _statusValueStyle);
            GUI.contentColor = prev;
            y += barH + 2f;
        }

        y += GAUGE_GROUP_GAP;
    }

    // ========================================================================
    // BIDIRECTIONAL ARC GAUGE — Center-zero with left/right deflection
    //
    // Draws an arc gauge where:
    //   - Zero value = needle pointing straight UP (12 o'clock)
    //   - Positive values = needle deflects RIGHT (clockwise from 12 o'clock)
    //   - Negative values = needle deflects LEFT (counter-clockwise from 12 o'clock)
    //
    // The arc spans 270° total: from bottom-left (min) through top (zero)
    // to bottom-right (max). This provides intuitive visual feedback for
    // bidirectional quantities like flow direction.
    //
    // Used for: BRS Flow (inflow to BRS vs outflow from BRS)
    // ========================================================================

    /// <summary>
    /// Draw a bidirectional arc gauge with center-zero.
    /// Needle at 12 o'clock = zero, right = positive, left = negative.
    /// </summary>
    /// <param name="center">Center point of the gauge arc</param>
    /// <param name="radius">Arc radius in pixels</param>
    /// <param name="value">Current value (can be negative)</param>
    /// <param name="minValue">Minimum value (negative, e.g., -40)</param>
    /// <param name="maxValue">Maximum value (positive, e.g., +40)</param>
    /// <param name="positiveColor">Color for positive values (right deflection)</param>
    /// <param name="negativeColor">Color for negative values (left deflection)</param>
    /// <param name="label">Gauge label text</param>
    /// <param name="valueNum">Numeric value for formatting</param>
    /// <param name="unitText">Unit text</param>
    internal void DrawGaugeArcBidirectional(Vector2 center, float radius, float value,
        float minValue, float maxValue, Color positiveColor, Color negativeColor,
        string label, float valueNum, string unitText)
    {
        if (Event.current.type != EventType.Repaint) return;

        // Normalise value to 0-1 range where 0.5 = zero
        // minValue maps to 0, zero maps to 0.5, maxValue maps to 1
        float zeroPoint = -minValue / (maxValue - minValue);  // Should be 0.5 for symmetric range
        float normalised = Mathf.Clamp01((value - minValue) / (maxValue - minValue));

        // Determine needle color based on value sign
        Color needleColor;
        Color arcFillColor;
        if (value > 0.5f)
        {
            needleColor = positiveColor;
            arcFillColor = positiveColor;
        }
        else if (value < -0.5f)
        {
            needleColor = negativeColor;
            arcFillColor = negativeColor;
        }
        else
        {
            needleColor = _cTextSecondary;  // Gray for zero/near-zero
            arcFillColor = _cTextSecondary;
        }

        // Draw arc background (full 270° sweep)
        // Arc goes from 225° (bottom-left, min) through 90° (top, zero) to -45° (bottom-right, max)
        DrawArcSegmentBidirectional(center, radius, 0f, 1f, _cGaugeArcBg, 3f);

        // Draw filled arc from zero to current value
        if (Mathf.Abs(value) > 0.5f)
        {
            if (value > 0f)
            {
                // Positive: fill from center (0.5) to current position
                DrawArcSegmentBidirectional(center, radius, zeroPoint, normalised, arcFillColor, 3f);
            }
            else
            {
                // Negative: fill from current position to center (0.5)
                DrawArcSegmentBidirectional(center, radius, normalised, zeroPoint, arcFillColor, 3f);
            }
        }

        // Calculate needle angle
        // normalised 0 = 225° (bottom-left), 0.5 = 90° (top), 1 = -45° (bottom-right)
        // Using radians: 225° = 5π/4, 90° = π/2, -45° = -π/4
        float needleAngle = Mathf.Lerp(225f * Mathf.Deg2Rad, -45f * Mathf.Deg2Rad, normalised);

        // Draw needle
        Vector2 needleTip = center + new Vector2(
            Mathf.Cos(needleAngle) * radius * 0.85f,
            -Mathf.Sin(needleAngle) * radius * 0.85f);
        DrawLine(center, needleTip, needleColor, 2f);

        // Center dot
        DrawFilledRect(new Rect(center.x - 2f, center.y - 2f, 4f, 4f), needleColor);

        // Draw center tick mark at 12 o'clock (zero position)
        float zeroAngle = 90f * Mathf.Deg2Rad;
        Vector2 zeroTickInner = center + new Vector2(
            Mathf.Cos(zeroAngle) * (radius - 4f),
            -Mathf.Sin(zeroAngle) * (radius - 4f));
        Vector2 zeroTickOuter = center + new Vector2(
            Mathf.Cos(zeroAngle) * (radius + 2f),
            -Mathf.Sin(zeroAngle) * (radius + 2f));
        DrawLine(zeroTickInner, zeroTickOuter, _cTextPrimary, 2f);

        // Label above arc
        Rect labelRect = new Rect(center.x - radius, center.y - radius - 14f,
            radius * 2f, 14f);
        GUI.Label(labelRect, label, _gaugeLabelStyle);

        // Value readout below arc — show sign explicitly
        string valueText;
        if (valueNum > 0.5f)
            valueText = $"+{valueNum:F1}";
        else if (valueNum < -0.5f)
            valueText = $"{valueNum:F1}";  // Negative sign automatic
        else
            valueText = "0.0";

        Rect valRect = new Rect(center.x - radius, center.y + 2f,
            radius * 2f, 16f);
        var prev = GUI.contentColor;
        GUI.contentColor = needleColor;
        GUI.Label(valRect, valueText, _gaugeValueStyle);
        GUI.contentColor = prev;

        // Units
        Rect unitRect = new Rect(center.x - radius, center.y + 16f,
            radius * 2f, 12f);
        GUI.Label(unitRect, unitText, _gaugeUnitStyle);
    }

    /// <summary>
    /// Draw a 270° arc segment for bidirectional gauges.
    /// Arc spans from bottom-left (225°) through top (90°) to bottom-right (-45°).
    /// </summary>
    internal static void DrawArcSegmentBidirectional(Vector2 center, float radius,
        float startFrac, float endFrac, Color color, float thickness)
    {
        GetGLMaterial().SetPass(0);
        GL.PushMatrix();
        GL.LoadPixelMatrix();

        int segments = 32;

        // Arc angles: 225° (5π/4) to -45° (-π/4), total 270° sweep
        // startFrac=0 → 225°, startFrac=0.5 → 90° (top), startFrac=1 → -45°
        float arcStartDeg = 225f;
        float arcEndDeg = -45f;

        float startAngle = Mathf.Lerp(arcStartDeg, arcEndDeg, startFrac) * Mathf.Deg2Rad;
        float endAngle = Mathf.Lerp(arcStartDeg, arcEndDeg, endFrac) * Mathf.Deg2Rad;

        // Draw thick arc as multiple offset lines
        for (float offset = -thickness / 2f; offset <= thickness / 2f; offset += 1f)
        {
            GL.Begin(GL.LINE_STRIP);
            GL.Color(color);
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = Mathf.Lerp(startAngle, endAngle, t);
                float r = radius + offset;
                float px = center.x + Mathf.Cos(angle) * r;
                float py = center.y - Mathf.Sin(angle) * r;
                GL.Vertex3(px, py, 0);
            }
            GL.End();
        }

        GL.PopMatrix();
    }
}
