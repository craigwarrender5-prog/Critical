// ============================================================================
// CRITICAL: Master the Atom - UI Component (Panels Partial)
// HeatupValidationVisual.Panels.cs - Status Information Panels
// ============================================================================
//
// PURPOSE:
//   Renders structured status panels in the right column of the heatup
//   validation dashboard. Provides at-a-glance operational awareness for:
//     - Plant Overview (mode, phase, timing, targets)
//     - RCP Staged Ramp Grid (4 pumps, aggregate ramp progress)
//     - Bubble Formation State Machine (7-phase tracker with progress)
//     - RVLIS (3 ranges with validity indicators)
//     - System Inventory Conservation (RCS+PZR+VCT+BRS mass balance)
//
// READS FROM:
//   HeatupSimEngine â€” plantMode, heatupPhaseDesc, simTime, wallClockTime,
//     bubblePhase, bubblePhaseStartTime, rcpContribution, rcpRunning,
//     rcpCount, effectiveRCPHeat, rvlisDynamic/Full/Upper + validity,
//     vctState, brsState, totalSystemMass_lbm, massError_lbm,
//     massConservationError, T_avg, T_rcs, T_pzr, pressure, pzrLevel,
//     solidPressurizer, bubbleFormed, currentHeaterMode, timeToBubble
//
// REFERENCE:
//   NRC HRTD 19.2.2 â€” Bubble formation procedure phases
//   NRC HRTD 3.2 â€” RCP staged startup sequence
//   NRC HRTD 4.1 â€” RVLIS instrumentation ranges
//   NRC ML11223A342 â€” Plant mode definitions
//
// ARCHITECTURE:
//   Partial class of HeatupValidationVisual. Implements:
//     - DrawStatusColumnContent() â€” dispatched from Core
//     - GetStatusContentHeightPartial() â€” tells scroll view total height
//     - Per-panel drawing methods (DrawPlantOverview, DrawRCPGrid, etc.)
//
// GOLD STANDARD: Yes
// ============================================================================

using UnityEngine;
using Critical.Physics;


namespace Critical.Validation
{

public partial class HeatupValidationVisual
{
    // ========================================================================
    // STATUS PANEL HEIGHTS â€” Pre-calculated for scroll view
    // ========================================================================

    const float OVERVIEW_PANEL_H   = 220f;  // v0.9.4: Added memory tracking row
    const float RCP_PANEL_H        = 180f;
    const float BUBBLE_PANEL_H     = 200f;
    const float RVLIS_PANEL_H      = 130f;
    const float INVENTORY_PANEL_H  = 200f;  // v5.4.1: +20px for PZR STEAM row
    const float HEATER_PANEL_H     = 100f;
    const float HZP_PANEL_H        = 220f;  // v1.1.0: HZP stabilization panel
    const float SG_RHR_PANEL_H     = 200f;  // v4.3.0: SG pressure + RHR thermal balance

    const float TOTAL_STATUS_H = OVERVIEW_PANEL_H + RCP_PANEL_H + BUBBLE_PANEL_H
                                 + RVLIS_PANEL_H + INVENTORY_PANEL_H + HEATER_PANEL_H
                                 + HZP_PANEL_H + SG_RHR_PANEL_H
                                 + STATUS_SECTION_GAP * 8 + 40f;

    // ========================================================================
    // PARTIAL METHOD IMPLEMENTATIONS â€” Called by Core
    // ========================================================================

    partial void GetStatusContentHeightPartial(ref float height)
    {
        height = TOTAL_STATUS_H;
    }

    partial void DrawStatusColumnContent(Rect area)
    {
        if (engine == null) return;

        float y = area.y;
        float x = area.x + 4f;
        float w = area.width - 8f;

        DrawPlantOverview(x, ref y, w);
        y += STATUS_SECTION_GAP;
        DrawRCPGrid(x, ref y, w);
        y += STATUS_SECTION_GAP;
        DrawBubbleStatePanel(x, ref y, w);
        y += STATUS_SECTION_GAP;
        DrawHeaterModePanel(x, ref y, w);
        y += STATUS_SECTION_GAP;
        DrawRVLISPanel(x, ref y, w);
        y += STATUS_SECTION_GAP;
        DrawInventoryPanel(x, ref y, w);
        y += STATUS_SECTION_GAP;
        DrawSGRHRPanel(x, ref y, w);  // v4.3.0
        y += STATUS_SECTION_GAP;
        DrawHZPPanel(x, ref y, w);  // v1.1.0
    }

    // ========================================================================
    // PLANT OVERVIEW â€” Mode, phase, temperatures, targets
    // ========================================================================

    void DrawPlantOverview(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x - 4f, y, w + 8f, GAUGE_GROUP_HEADER_H), "PLANT OVERVIEW");
        y += GAUGE_GROUP_HEADER_H + 2f;

        // Mode with color
        Color modeC = engine.GetModeColor();
        string modeStr = $"MODE {engine.plantMode}";
        DrawStatusRow(ref y, x, w, "PLANT MODE", modeStr, modeC);

        // Phase
        DrawStatusRow(ref y, x, w, "PHASE", engine.heatupPhaseDesc, _cCyanInfo);

        // Times
        DrawStatusRow(ref y, x, w, "SIM TIME",
            TimeAcceleration.FormatTime(engine.simTime));
        DrawStatusRow(ref y, x, w, "WALL TIME",
            TimeAcceleration.FormatTime(engine.wallClockTime));

        // Current temperatures summary
        DrawStatusRow(ref y, x, w, "T_RCS", $"{engine.T_rcs:F1} Â°F",
            GetHighThresholdColor(engine.T_rcs, 545f, 570f,
                _cNormalGreen, _cWarningAmber, _cAlarmRed));
        DrawStatusRow(ref y, x, w, "T_PZR", $"{engine.T_pzr:F1} Â°F", _cTrace4);

        // Pressure
        DrawStatusRow(ref y, x, w, "RCS PRESS", $"{engine.pressure:F0} psia");

        // PZR state
        string pzrState = engine.solidPressurizer ? "SOLID (water-filled)" :
                          engine.bubbleFormed ? "NORMAL (steam bubble)" :
                          "FORMING BUBBLE";
        Color pzrStateC = engine.solidPressurizer ? _cWarningAmber :
                          engine.bubbleFormed ? _cNormalGreen : _cCyanInfo;
        DrawStatusRow(ref y, x, w, "PZR STATE", pzrState, pzrStateC);

        // Heatup target / progress
        float progress = (engine.T_rcs - engine.startTemperature) /
                         (engine.targetTemperature - engine.startTemperature) * 100f;
        progress = Mathf.Clamp(progress, 0f, 100f);
        DrawStatusRow(ref y, x, w, "HEATUP",
            $"{progress:F0}% ({engine.T_rcs:F0} â†’ {engine.targetTemperature:F0} Â°F)", _cNormalGreen);

        // Time to bubble (during solid PZR phase)
        if (engine.solidPressurizer && engine.timeToBubble > 0f)
        {
            DrawStatusRow(ref y, x, w, "EST BUBBLE",
                $"{engine.timeToBubble:F1} hr ({engine.timeToBubble * 60f:F0} min)", _cCyanInfo);
        }

        // v0.9.5: Memory usage monitoring (total including native and GPU)
        // GetTotalAllocatedMemoryLong only reports managed heap
        // Add reserved and graphics memory for total process memory estimate
        y += 4f;
        long reservedMem = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong();
        long graphicsMem = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver();
        float totalMB = (reservedMem + graphicsMem) / (1024f * 1024f);
        // Color code: green <100MB, amber 100-300MB, red >300MB (leak indicator)
        Color memC = totalMB < 100f ? _cNormalGreen :
                     totalMB < 300f ? _cWarningAmber : _cAlarmRed;
        DrawStatusRow(ref y, x, w, "MEMORY", $"{totalMB:F0} MB", memC);
    }

    // ========================================================================
    // RCP STAGED RAMP GRID â€” 4 pumps + aggregate status
    // Per NRC HRTD 3.2: Each pump ramps through 4 stages over ~40 min
    // ========================================================================

    void DrawRCPGrid(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x - 4f, y, w + 8f, GAUGE_GROUP_HEADER_H), "RCP STARTUP STATUS");
        y += GAUGE_GROUP_HEADER_H + 2f;

        // Per-pump status rows
        for (int i = 0; i < 4; i++)
        {
            string pumpLabel = $"RCP #{i + 1}";
            bool running = i < engine.rcpRunning.Length && engine.rcpRunning[i];
            string status;
            Color statusC;

            if (!running)
            {
                status = "OFF";
                statusC = _cTextSecondary;
            }
            else if (engine.rcpContribution.PumpsFullyRunning > i)
            {
                status = "RATED (100%)";
                statusC = _cNormalGreen;
            }
            else
            {
                status = "RAMPING...";
                statusC = _cCyanInfo;
            }

            DrawStatusRow(ref y, x, w, pumpLabel, status, statusC);
        }

        y += 4f;

        // Aggregate RCP data
        DrawStatusRow(ref y, x, w, "PUMPS RUNNING",
            $"{engine.rcpContribution.PumpsStarted} started, {engine.rcpContribution.PumpsFullyRunning} rated",
            engine.rcpCount > 0 ? _cNormalGreen : _cTextSecondary);

        DrawStatusRow(ref y, x, w, "EFF RCP HEAT",
            $"{engine.effectiveRCPHeat:F1} / {engine.rcpCount * PlantConstants.RCP_HEAT_MW_EACH:F1} MW",
            engine.rcpCount > 0 ? _cOrangeAccent : _cTextSecondary);

        DrawStatusRow(ref y, x, w, "FLOW FRACTION",
            $"{engine.rcpContribution.TotalFlowFraction:F2} ({engine.rcpContribution.EffectiveFlow_gpm:F0} gpm)",
            engine.rcpCount > 0 ? _cBlueAccent : _cTextSecondary);

        DrawStatusRow(ref y, x, w, "COUPLING Î±",
            $"{Mathf.Min(1f, engine.rcpContribution.TotalFlowFraction):F3}",
            engine.rcpContribution.AllFullyRunning ? _cNormalGreen : _cCyanInfo);
    }

    // ========================================================================
    // BUBBLE FORMATION STATE MACHINE â€” 7-phase tracker with progress
    // Per NRC HRTD 19.2.2
    // ========================================================================

    void DrawBubbleStatePanel(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x - 4f, y, w + 8f, GAUGE_GROUP_HEADER_H),
            "BUBBLE FORMATION (NRC HRTD 19.2.2)");
        y += GAUGE_GROUP_HEADER_H + 2f;

        // Phase display array
        string[] phaseNames = { "NONE", "DETECTION", "VERIFICATION", "DRAIN", "STABILIZE", "PRESSURIZE", "COMPLETE" };
        int currentPhaseIdx = (int)engine.bubblePhase;

        // Draw each phase as a status row with state indicator
        for (int i = 0; i < phaseNames.Length; i++)
        {
            Color phaseC;
            string indicator;

            if (i < currentPhaseIdx)
            {
                phaseC = _cNormalGreen;
                indicator = "DONE";
            }
            else if (i == currentPhaseIdx && engine.bubblePhase != HeatupSimEngine.BubbleFormationPhase.NONE
                     && engine.bubblePhase != HeatupSimEngine.BubbleFormationPhase.COMPLETE)
            {
                float elapsed = (engine.simTime - engine.bubblePhaseStartTime) * 60f;
                phaseC = _cCyanInfo;
                indicator = $"ACTIVE {elapsed:F0} min";
            }
            else if (i == currentPhaseIdx)
            {
                phaseC = engine.bubblePhase == HeatupSimEngine.BubbleFormationPhase.COMPLETE
                    ? _cNormalGreen : _cTextSecondary;
                indicator = engine.bubblePhase == HeatupSimEngine.BubbleFormationPhase.COMPLETE
                    ? "COMPLETE" : "--";
            }
            else
            {
                phaseC = _cTextSecondary;
                indicator = "--";
            }

            DrawStatusRow(ref y, x, w, $"  {i}. {phaseNames[i]}", indicator, phaseC);
        }

        y += 4f;

        // Key bubble parameters per active phase
        if (engine.bubblePhase == HeatupSimEngine.BubbleFormationPhase.DRAIN)
        {
            float drainProg = engine.bubbleDrainStartLevel > PlantConstants.PZR_LEVEL_AFTER_BUBBLE
                ? (engine.bubbleDrainStartLevel - engine.pzrLevel) /
                  (engine.bubbleDrainStartLevel - PlantConstants.PZR_LEVEL_AFTER_BUBBLE) * 100f
                : 100f;
            DrawStatusRow(ref y, x, w, "DRAIN PROGRESS",
                $"{drainProg:F0}% ({engine.pzrLevel:F1}% -> {PlantConstants.PZR_LEVEL_AFTER_BUBBLE:F0}%)", _cCyanInfo);

            string ccpStr = engine.ccpStarted ? $"ON (since {engine.ccpStartLevel:F1}%)" : "OFF (waiting)";
            DrawStatusRow(ref y, x, w, "CCP STATUS", ccpStr,
                engine.ccpStarted ? _cNormalGreen : _cWarningAmber);
        }
        else if (engine.bubblePhase == HeatupSimEngine.BubbleFormationPhase.PRESSURIZE)
        {
            float pPsig = engine.pressure - 14.7f;
            float pProg = pPsig / PlantConstants.MIN_RCP_PRESSURE_PSIG * 100f;
            DrawStatusRow(ref y, x, w, "PRESS PROGRESS",
                $"{pPsig:F0} / {PlantConstants.MIN_RCP_PRESSURE_PSIG:F0} psig ({pProg:F0}%)", _cCyanInfo);
        }
        else if (engine.bubblePhase == HeatupSimEngine.BubbleFormationPhase.VERIFICATION)
        {
            string sprayStr = engine.auxSprayActive ? "IN PROGRESS" :
                              engine.auxSprayTestPassed ? $"PASS (dP={engine.auxSprayPressureDrop:F1} psi)" :
                              "PENDING";
            DrawStatusRow(ref y, x, w, "AUX SPRAY TEST", sprayStr,
                engine.auxSprayTestPassed ? _cNormalGreen : _cCyanInfo);
        }
    }

    // ========================================================================
    // HEATER MODE STATUS â€” Current operating mode and power
    // Per NRC HRTD 6.1 / 10.2
    // ========================================================================

    void DrawHeaterModePanel(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x - 4f, y, w + 8f, GAUGE_GROUP_HEADER_H),
            "PZR HEATER CONTROL");
        y += GAUGE_GROUP_HEADER_H + 2f;

        // Heater mode
        string modeStr;
        Color modeC;
        switch (engine.currentHeaterMode)
        {
            case HeaterMode.STARTUP_FULL_POWER:
                modeStr = "STARTUP FULL (1800 kW)";
                modeC = _cOrangeAccent;
                break;
            case HeaterMode.BUBBLE_FORMATION_AUTO:
                modeStr = "BUBBLE AUTO (rate ctrl)";
                modeC = _cCyanInfo;
                break;
            case HeaterMode.PRESSURIZE_AUTO:
                modeStr = "PRESSURIZE AUTO";
                modeC = _cBlueAccent;
                break;
            case HeaterMode.AUTOMATIC_PID:
                modeStr = "PID AUTO";
                modeC = _cNormalGreen;
                break;
            case HeaterMode.OFF:
                modeStr = "OFF";
                modeC = _cTextSecondary;
                break;
            default:
                modeStr = "UNKNOWN";
                modeC = _cAlarmRed;
                break;
        }
        DrawStatusRow(ref y, x, w, "MODE", modeStr, modeC);

        // Power output
        float powerKW = engine.pzrHeaterPower * 1000f;
        float powerPct = powerKW / PlantConstants.HEATER_POWER_TOTAL * 100f;
        Color pwrC = engine.pzrHeatersOn ? _cOrangeAccent : _cTextSecondary;
        DrawStatusRow(ref y, x, w, "OUTPUT", $"{powerKW:F0} kW ({powerPct:F0}%)", pwrC);

        // Pressure rate feedback
        DrawStatusRow(ref y, x, w, "PRESS RATE",
            $"{engine.pressureRate:F1} psi/hr",
            Mathf.Abs(engine.pressureRate) > PlantConstants.HEATER_STARTUP_MAX_PRESSURE_RATE
                ? _cWarningAmber : _cNormalGreen);

        // Heater enable status
        DrawStatusRow(ref y, x, w, "ENABLED",
            engine.pzrHeatersOn ? "YES" : "NO -- INTERLOCK",
            engine.pzrHeatersOn ? _cNormalGreen : _cAlarmRed);
    }

    // ========================================================================
    // RVLIS â€” Reactor Vessel Level Indication System
    // Per NRC HRTD 4.1 â€” Three ranges with flow-dependent validity
    // ========================================================================

    void DrawRVLISPanel(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x - 4f, y, w + 8f, GAUGE_GROUP_HEADER_H),
            "RVLIS -- VESSEL LEVEL");
        y += GAUGE_GROUP_HEADER_H + 2f;

        // Dynamic Range (valid with RCPs running)
        Color dynC = engine.rvlisDynamicValid ? _cNormalGreen : _cTextSecondary;
        string dynVal = engine.rvlisDynamicValid
            ? $"{engine.rvlisDynamic:F1}%"
            : $"({engine.rvlisDynamic:F1}%) INVALID";
        DrawStatusRow(ref y, x, w, "DYNAMIC", dynVal, dynC);

        // Full Range (valid without RCPs â€” natural circ)
        Color fullC = engine.rvlisFullValid ? _cNormalGreen : _cTextSecondary;
        string fullVal = engine.rvlisFullValid
            ? $"{engine.rvlisFull:F1}%"
            : $"({engine.rvlisFull:F1}%) INVALID";
        DrawStatusRow(ref y, x, w, "FULL RANGE", fullVal, fullC);

        // Upper Range (valid without RCPs)
        Color upC = engine.rvlisUpperValid ? _cNormalGreen : _cTextSecondary;
        string upVal = engine.rvlisUpperValid
            ? $"{engine.rvlisUpper:F1}%"
            : $"({engine.rvlisUpper:F1}%) INVALID";
        DrawStatusRow(ref y, x, w, "UPPER RANGE", upVal, upC);

        // RVLIS low level alarm
        Color rvlLowC = engine.rvlisLevelLow ? _cAlarmRed : _cNormalGreen;
        DrawStatusRow(ref y, x, w, "LOW LEVEL", engine.rvlisLevelLow ? "ALARM" : "OK", rvlLowC);

        // Active range explanation
        string activeRange = engine.rcpCount > 0 ? "DYNAMIC (RCPs ON)" : "FULL/UPPER (No RCPs)";
        DrawStatusRow(ref y, x, w, "ACTIVE", activeRange, _cTextSecondary);
    }

    // ========================================================================
    // SYSTEM INVENTORY CONSERVATION â€” RCS + PZR + VCT + BRS mass balance
    // ========================================================================

    void DrawInventoryPanel(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x - 4f, y, w + 8f, GAUGE_GROUP_HEADER_H),
            "SYSTEM INVENTORY (MASS)");
        y += GAUGE_GROUP_HEADER_H + 2f;

        // v5.4.1 Fix B: Mass-based inventory display
        // RCS inventory
        DrawStatusRow(ref y, x, w, "RCS", $"{engine.rcsWaterMass:F0} lbm");

        // PZR inventory (water + steam)
        float pzrWaterDensity = WaterProperties.WaterDensity(engine.T_pzr, engine.pressure);
        float pzrSteamDensity = WaterProperties.SaturatedSteamDensity(engine.pressure);
        float pzrWaterMass = engine.pzrWaterVolume * pzrWaterDensity;
        float pzrSteamMass = engine.pzrSteamVolume * pzrSteamDensity;
        DrawStatusRow(ref y, x, w, "PZR WATER", $"{pzrWaterMass:F0} lbm ({engine.pzrLevel:F1}%)");
        DrawStatusRow(ref y, x, w, "PZR STEAM", $"{pzrSteamMass:F1} lbm");

        // VCT inventory
        float rhoVCT = WaterProperties.WaterDensity(100f, 14.7f);
        float vctMass = (engine.vctState.Volume_gal / PlantConstants.FT3_TO_GAL) * rhoVCT;
        DrawStatusRow(ref y, x, w, "VCT", $"{vctMass:F0} lbm ({engine.vctState.Level_percent:F1}%)");

        // BRS inventory
        float brsGal = engine.brsState.HoldupVolume_gal + engine.brsState.DistillateAvailable_gal
                         + engine.brsState.ConcentrateAvailable_gal;
        float brsMass = (brsGal / PlantConstants.FT3_TO_GAL) * rhoVCT;
        DrawStatusRow(ref y, x, w, "BRS TOTAL", $"{brsMass:F0} lbm");

        y += 4f;

        // Total system mass
        DrawStatusRow(ref y, x, w, "SYSTEM TOTAL",
            $"{engine.totalSystemMass_lbm:F0} lbm", _cTextPrimary);

        // Initial mass (reference)
        DrawStatusRow(ref y, x, w, "INITIAL",
            $"{engine.initialSystemMass_lbm:F0} lbm", _cTextSecondary);

        // Conservation error (mass-based thresholds: 100 lbm good, 500 lbm warn)
        Color errC = engine.massError_lbm < 100f ? _cNormalGreen :
                     engine.massError_lbm < 500f ? _cWarningAmber : _cAlarmRed;
        DrawStatusRow(ref y, x, w, "MASS ERROR",
            $"{engine.massError_lbm:F1} lbm", errC);

        // CVCS mass conservation (VCT-level tracking, retained)
        Color vctErrC = engine.massConservationError < 10f ? _cNormalGreen :
                        engine.massConservationError < 50f ? _cWarningAmber : _cAlarmRed;
        DrawStatusRow(ref y, x, w, "VCT CONS ERR",
            $"{engine.massConservationError:F1} gal", vctErrC);
    }

    // ========================================================================
    // SG PRESSURE + RHR THERMAL BALANCE â€” v4.3.0
    // Per NRC HRTD 19.2.2, 5.1
    // ========================================================================

    void DrawSGRHRPanel(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x - 4f, y, w + 8f, GAUGE_GROUP_HEADER_H),
            "SG / RHR THERMAL BALANCE (v4.3.0)");
        y += GAUGE_GROUP_HEADER_H + 2f;

        // SG section
        DrawStatusRow(ref y, x, w, "â” SG SECONDARY", "", _cTextPrimary);

        // SG pressure
        float sgP = engine.sgSecondaryPressure_psia;
        float sgP_psig = sgP - 14.7f;
        Color pC = sgP_psig > 1050f ? _cNormalGreen :
                   sgP_psig > 100f ? _cCyanInfo : _cTextSecondary;
        DrawStatusRow(ref y, x, w, "PRESSURE", $"{sgP_psig:F0} psig ({sgP:F0} psia)", pC);

        // T_sat and superheat
        DrawStatusRow(ref y, x, w, "T_SAT", $"{engine.sgSaturationTemp_F:F1} \u00b0F", _cCyanInfo);
        Color shC = engine.sgMaxSuperheat_F > 10f ? _cOrangeAccent :
                    engine.sgMaxSuperheat_F > 0f ? _cWarningAmber : _cTextSecondary;
        DrawStatusRow(ref y, x, w, "SUPERHEAT", $"{engine.sgMaxSuperheat_F:F1} \u00b0F", shC);

        // Boiling status
        string boilStr;
        Color boilC;
        if (engine.sgBoilingActive)
        {
            boilStr = $"BOILING ({engine.sgBoilingIntensity * 100f:F0}%)";
            boilC = engine.sgBoilingIntensity > 0.5f ? _cOrangeAccent : _cWarningAmber;
        }
        else
        {
            boilStr = "SUBCOOLED";
            boilC = _cTextSecondary;
        }
        DrawStatusRow(ref y, x, w, "BOILING", boilStr, boilC);

        // N2 blanket
        string n2Str = engine.sgNitrogenIsolated ? "ISOLATED" : "BLANKETED";
        Color n2C = engine.sgNitrogenIsolated ? _cWarningAmber : _cNormalGreen;
        DrawStatusRow(ref y, x, w, "N\u2082 BLANKET", n2Str, n2C);

        // SG heat absorption
        DrawStatusRow(ref y, x, w, "SG Q_TOTAL",
            $"{engine.sgHeatTransfer_MW:F2} MW",
            engine.sgHeatTransfer_MW > 0.1f ? _cOrangeAccent : _cTextSecondary);

        y += 4f;

        // RHR section
        DrawStatusRow(ref y, x, w, "â” RHR SYSTEM", "", _cTextPrimary);

        string rhrMode = engine.rhrModeString;
        Color rhrC = engine.rhrActive ? _cNormalGreen : _cTextSecondary;
        DrawStatusRow(ref y, x, w, "MODE", rhrMode, rhrC);

        if (engine.rhrActive)
        {
            DrawStatusRow(ref y, x, w, "NET HEAT",
                $"{engine.rhrNetHeat_MW:+0.000;-0.000} MW",
                engine.rhrNetHeat_MW > 0 ? _cOrangeAccent : _cBlueAccent);
            DrawStatusRow(ref y, x, w, "HX REMOVAL",
                $"{engine.rhrHXRemoval_MW:F3} MW", _cBlueAccent);
            DrawStatusRow(ref y, x, w, "PUMP HEAT",
                $"{engine.rhrPumpHeat_MW:F3} MW", _cOrangeAccent);
        }
        else
        {
            DrawStatusRow(ref y, x, w, "STATUS", "SECURED", _cTextSecondary);
        }
    }

    // ========================================================================
    // HZP STABILIZATION PANEL â€” v1.1.0
    // Steam dump, HZP state machine, heater PID, startup readiness
    // Per NRC HRTD 10.2, 11.2, 19.0
    // ========================================================================

    void DrawHZPPanel(float x, ref float y, float w)
    {
        DrawSectionHeader(new Rect(x - 4f, y, w + 8f, GAUGE_GROUP_HEADER_H),
            "HZP STABILIZATION (v1.1.0)");
        y += GAUGE_GROUP_HEADER_H + 2f;

        // HZP State
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
        DrawStatusRow(ref y, x, w, "HZP STATE", stateStr, stateC);

        // HZP Progress
        if (engine.IsHZPActive())
        {
            float prog = engine.hzpProgress;
            Color progC = prog >= 100f ? _cNormalGreen :
                          prog >= 50f ? _cCyanInfo : _cWarningAmber;
            DrawStatusRow(ref y, x, w, "PROGRESS", $"{prog:F0}%", progC);
        }
        else
        {
            DrawStatusRow(ref y, x, w, "PROGRESS", "--", _cTextSecondary);
        }

        y += 4f;

        // Steam Dump Section
        DrawStatusRow(ref y, x, w, "â” STEAM DUMP", "", _cTextPrimary);

        // Steam dump mode/status
        string sdStatus = engine.GetSteamDumpStatus();
        Color sdC = engine.steamDumpActive ? _cCyanInfo : _cTextSecondary;
        DrawStatusRow(ref y, x, w, "MODE", sdStatus, sdC);

        // Steam pressure
        DrawStatusRow(ref y, x, w, "STEAM PRESS",
            $"{engine.steamPressure_psig:F0} psig",
            engine.steamPressure_psig > 1000f ? _cNormalGreen : _cTextSecondary);

        // Steam dump heat removal
        Color sdHeatC = engine.steamDumpHeat_MW > 0.1f ? _cOrangeAccent : _cTextSecondary;
        DrawStatusRow(ref y, x, w, "HEAT REMOVAL",
            $"{engine.steamDumpHeat_MW:F1} MW", sdHeatC);

        y += 4f;

        // Heater PID Section
        DrawStatusRow(ref y, x, w, "â” HEATER PID", "", _cTextPrimary);

        // PID status
        string pidStatus = engine.GetHeaterPIDStatus();
        Color pidC = engine.heaterPIDActive ? _cNormalGreen : _cTextSecondary;
        DrawStatusRow(ref y, x, w, "STATUS", pidStatus, pidC);

        // PID output
        if (engine.heaterPIDActive)
        {
            float outPct = engine.heaterPIDOutput * 100f;
            Color outC = outPct > 80f ? _cOrangeAccent :
                         outPct > 20f ? _cNormalGreen : _cCyanInfo;
            DrawStatusRow(ref y, x, w, "OUTPUT", $"{outPct:F1}%", outC);
        }
        else
        {
            DrawStatusRow(ref y, x, w, "OUTPUT", "--", _cTextSecondary);
        }

        y += 4f;

        // Startup Readiness Section
        DrawStatusRow(ref y, x, w, "â” STARTUP READY", "", _cTextPrimary);

        // Individual prerequisites
        if (engine.IsHZPActive())
        {
            var prereqs = engine.GetStartupReadiness();

            Color tempC = prereqs.TemperatureOK ? _cNormalGreen : _cAlarmRed;
            DrawStatusRow(ref y, x, w, "T_AVG", prereqs.TemperatureStatus, tempC);

            Color pressC = prereqs.PressureOK ? _cNormalGreen : _cAlarmRed;
            DrawStatusRow(ref y, x, w, "PRESSURE", prereqs.PressureStatus, pressC);

            Color levelC = prereqs.LevelOK ? _cNormalGreen : _cAlarmRed;
            DrawStatusRow(ref y, x, w, "PZR LEVEL", prereqs.LevelStatus, levelC);

            Color rcpC = prereqs.RCPsOK ? _cNormalGreen : _cAlarmRed;
            DrawStatusRow(ref y, x, w, "RCPs", prereqs.RCPsStatus, rcpC);

            // Overall status
            y += 4f;
            Color allC = prereqs.AllMet ? _cNormalGreen : _cAlarmRed;
            string allStr = prereqs.AllMet ? "ALL PREREQUISITES MET" : "PREREQUISITES NOT MET";
            DrawStatusRow(ref y, x, w, "OVERALL", allStr, allC);
        }
        else
        {
            DrawStatusRow(ref y, x, w, "STATUS", "Not at HZP approach", _cTextSecondary);
        }
    }
}

}

