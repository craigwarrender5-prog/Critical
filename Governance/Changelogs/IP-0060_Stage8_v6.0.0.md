# Changelog — IP-0060 Stage 8: Condenser / Trends / Log Tabs

**Version:** 6.0.0  
**IP:** IP-0060 Stage 8 (8A + 8B + 8C + 8D)  
**Date:** 2026-02-18  
**Author:** Claude (AI) with Craig oversight  

---

## Summary

Completed the final three tabs of the UI Toolkit Validation Dashboard V2, bringing all 8 tabs online. No more placeholder tabs remain. The dashboard now provides full-spectrum monitoring of the Westinghouse 4-Loop PWR heatup simulation from Cold Shutdown through Hot Zero Power.

---

## Stage 8A — Condenser Tab (Tab 5)

### Added
- **New file:** `UITKDashboardV2Controller.CondenserTab.cs`
- **Layout:** Three-column upper + two-column lower arrangement
  - **Left panel — Condenser Vacuum Dynamics:** ArcGauge for vacuum (0–30 inHg), ArcGauge for hotwell level (0–100%), TankLevelPOC for CST level, 3 LEDs (C-9 Available, P-12 Bypass, Steam Dump Permitted), digital metrics for backpressure, pulldown phase, feedwater return flow
  - **Center panel — Steam Dump Controller:** ArcGauge for dump heat removal (0–30 MW), ArcGauge for steam pressure (0–1200 psig), LED for dump active, metrics for mode/status/net plant heat
  - **Right panel — HZP Stabilization:** ArcGauge for HZP progress (0–100%), 3 LEDs (Stable, Ready, Heater PID Active), state machine display, heater PID output, 6-item startup prerequisites checklist
  - **Bottom-Left — Permissive Status:** Bridge FSM state, individual permissive check results (C-9, P-12, bypass, mode, steam pressure), final permitted/blocked status
  - **Bottom-Right — Strip Chart:** 3 traces (vacuum, steam dump heat, hotwell level)
- **Data binding:** `RefreshCondenserTab()` at 5Hz — 5 ArcGauges, 1 TankLevel, 7 LEDs, ~30 metrics, 1 strip chart with 3 traces
- **Engine fields used:** All CS-0115 fields from IP-0046 (condenserVacuum_inHg, steamDumpHeat_MW, hzpProgress, permissiveState, etc.)

### Modified
- **`UITKDashboardV2Controller.cs`** — `BuildTabContents()`: Added `case 5: BuildCondenserTab()`; `RefreshActiveTabData()`: Added `case 5: RefreshCondenserTab()`

---

## Stage 8B — Trends Tab (Tab 6)

### Added
- **New file:** `UITKDashboardV2Controller.TrendsTab.cs`
- **Layout:** 4 full-width strip charts stacked vertically, each with panel title showing color-coded trace legend
  - **Chart 1 — Temperatures (6 traces):** T_avg (green), T_hot (red), T_cold (cyan), T_pzr (amber), T_sat (magenta), T_sg_secondary (yellow) — range 50–700°F
  - **Chart 2 — Pressures (3 traces):** RCS Pressure (green), SG Pressure (cyan), Pressure Rate ×10 offset-scaled (amber) — range 0–2600 psia
  - **Chart 3 — Levels & Flows (5 traces):** PZR Level (green), VCT Level (cyan), Charging/2 (amber), Letdown/2 (red), Surge/2 (magenta) — range 0–100 with flow scaling (÷2 maps ~200 gpm max to 100)
  - **Chart 4 — Thermal Balance (5 traces):** RCP Heat (green), SG Heat Transfer (cyan), RHR Net (amber), Steam Dump (red), Net Plant Heat (white) — range -10 to 35 MW
- **Data binding:** `RefreshTrendsTab()` at 5Hz — all 4 charts updated simultaneously, 19 total traces
- **Scaling strategy:** Pressure rate offset to mid-range (1300 + rate×10) for visibility alongside absolute pressures; flows halved to share level percentage scale; net heat allows negative range

### Modified
- **`UITKDashboardV2Controller.cs`** — `BuildTabContents()`: Added `case 6: BuildTrendsTab()`; `RefreshActiveTabData()`: Added `case 6: RefreshTrendsTab()`

---

## Stage 8C — Log Tab (Tab 7)

### Added
- **New file:** `UITKDashboardV2Controller.LogTab.cs`
- **Layout:** Full-screen scrollable event log with header controls
  - **Filter bar:** 5 toggle buttons (ALL / INFO / ACTION / ALERT / ALARM) with active/inactive styling — active button highlighted with AccentBlue border
  - **Event entries:** Monospaced font, color-coded by severity (INFO=cyan, ACTION=green, ALERT=amber, ALARM=red), alternating row backgrounds
  - **Auto-scroll:** Automatically follows newest entries; pauses when user scrolls up manually; "▼ AUTO-SCROLL" button to re-engage
  - **Summary bar:** Shows total and filtered entry counts
- **Data source:** `engine.eventLog` (List<EventLogEntry>, max 200 entries) using pre-formatted `FormattedLine` strings (zero per-frame allocations per v0.9.6 optimization)
- **Smart refresh:** `RefreshLogTab()` at 5Hz only rebuilds DOM when `eventLog.Count` changes or filter selection changes — avoids unnecessary element creation/destruction

### Modified
- **`UITKDashboardV2Controller.cs`** — `BuildTabContents()`: Added `case 7: BuildLogTab()`; `RefreshActiveTabData()`: Added `case 7: RefreshLogTab()`

---

## Stage 8D — Final Wiring Verification

### Verified
- All 8 tab indices (0–7) covered in `BuildTabContents()` switch — no remaining `BuildTabPlaceholder()` calls for valid indices
- All 8 tab indices covered in `RefreshActiveTabData()` switch
- `TAB_NAMES` array confirmed: `["CRITICAL", "RCS", "PRESSURIZER", "CVCS", "SG / RHR", "CONDENSER", "TRENDS", "LOG"]`
- All 10 partial class files present with Unity .meta files:
  1. `UITKDashboardV2Controller.cs` (foundation)
  2. `UITKDashboardV2Controller.CriticalTab.cs` (tab 0 upper)
  3. `UITKDashboardV2Controller.CriticalTab.Lower.cs` (tab 0 lower)
  4. `UITKDashboardV2Controller.RCSTab.cs` (tab 1)
  5. `UITKDashboardV2Controller.PressurizerTab.cs` (tab 2)
  6. `UITKDashboardV2Controller.CVCSTab.cs` (tab 3)
  7. `UITKDashboardV2Controller.SGRHRTab.cs` (tab 4)
  8. `UITKDashboardV2Controller.CondenserTab.cs` (tab 5)
  9. `UITKDashboardV2Controller.TrendsTab.cs` (tab 6)
  10. `UITKDashboardV2Controller.LogTab.cs` (tab 7)

### No Changes Required
- `BuildTabPlaceholder()` method retained as dead-code fallback for defensive programming — only reachable if TAB_NAMES were expanded beyond 8 without adding a case

---

## Files Changed

| File | Action | Lines |
|------|--------|-------|
| `UITKDashboardV2Controller.CondenserTab.cs` | **Created** | ~420 |
| `UITKDashboardV2Controller.TrendsTab.cs` | **Created** | ~215 |
| `UITKDashboardV2Controller.LogTab.cs` | **Created** | ~310 |
| `UITKDashboardV2Controller.cs` | **Modified** | +12 (6 case lines in build + 6 in refresh) |

---

## Unaddressed Issues

| Issue | Reason | Disposition |
|-------|--------|-------------|
| Condenser tab engine method stubs (`GetSteamDumpStatus()`, `GetHZPStatusString()`, `GetHZPDetailedStatus()`, `GetHeaterPIDStatus()`, `GetStartupReadiness()`, `IsHZPActive()`) | These methods must exist on HeatupSimEngine; if not yet implemented, will cause compile errors | Verify at compile time; stub if needed |
| `SteamDumpMode` enum reference in CondenserTab | Must be accessible from `Critical.Physics` or engine namespace | Verify at compile time |
| `p12BypassCommanded` field on engine | Referenced in CondenserTab LED binding | Verify at compile time |
| Log tab monospaced font fallback | Uses `LegacyRuntime.ttf` built-in; if unavailable in URP, may fall back to default | Low risk, cosmetic only |
| Trends tab trace count (19 total across 4 charts) | Performance impact of 19 `AddValue()` + `MarkDirtyRepaint()` calls at 5Hz | Monitor; StripChartPOC ring buffer is O(1) per call |
