# Changelog — IP-0040: Validation Dashboard Visual Overhaul

**Implementation Plan:** IP-0040 - Validation Dashboard Visual Overhaul  
**Version:** 1.0.0  
**Date:** 2026-02-17  
**Domain Plan:** DP-0008 (Operator Interface & Scenarios)  
**Parent IP:** IP-0031 (Validation Dashboard Visual Redesign)  

---

## Summary

Complete visual overhaul of the Validation Dashboard, replacing bare text rows with real instrument gauges, ISA-18.1 compliant annunciator tiles, instrument-grade digital fonts, and hero gauge panels across all detail tabs. No physics, engine, or GOLD module code was modified.

---

## Stage 1 — Theme & Annunciator Foundation

### Files Modified
- **ValidationDashboardTheme.cs** — Updated annunciator color palette to match MosaicAlarmPanel proven values. Added text/border active colors (AnnunciatorTextGreen, AnnunciatorTextAmber, AnnunciatorTextRed), inactive colors (AnnunciatorTextDim, AnnunciatorBorderDim), acknowledged colors (AnnunciatorAckBg, AnnunciatorAckText). Added ISA-18.1 flash rate constants (AnnunciatorAlertFlashHz=3.0, AnnunciatorClearFlashHz=0.7, AnnunciatorAutoResetDelay=5.0s).
- **ArcGauge.cs** — Create() factory updated: value text uses InstrumentFontHelper for Electronic Highway Sign SDF font + green glow material. Recessed dark backing added behind readout.
- **BidirectionalGauge.cs** — Same instrument font + recessed backing treatment in Create() factory.
- **DigitalReadout.cs** — Same instrument font + recessed backing treatment in Create() factory.
- **TabNavigationController.cs** — Added static `FadeCanvasGroup()` coroutine for tab transition animation.

### Files Created
- **DashboardAnnunciatorTile.cs** — ISA-18.1 state machine (INACTIVE→ALERTING→ACKNOWLEDGED→CLEARING→INACTIVE). Fast flash 3 Hz for new alarms, steady on for acknowledged, slow flash 0.7 Hz for clearing. MosaicAlarmPanel visual construction (4-edge 1px borders, instrument font, dim/lit color states). Factory Create() method. AnnunciatorState enum and AnnunciatorTileDescriptor struct.
- **InstrumentFontHelper.cs** — Shared utility for instrument font loading (Electronic Highway Sign SDF), TMP glow material management (Instrument_Green_Glow, Instrument_Amber_Glow, Instrument_Red_Glow), recessed backing rectangle creation. Thread-safe lazy loading with cache.

---

## Stage 2 — Overview Layout Restructure

### Files Modified
- **OverviewPanel.cs** — Rewritten to 3+2 proportional layout. Top row (60% height): Reactor/RCS (flex 1.2), Pressurizer (flex 1.0), CVCS/SG (flex 1.0). Bottom row (40% height): System Health (flex 0.8), Alarm Annunciator (flex 1.5). Section fields updated from 7 sections to 5 merged sections. Header comment updated with new layout diagram.

### Files Created (Stubs)
- **OverviewSection_ReactorRCS.cs** — Stub with ParameterRows (replaced in Stage 3).
- **OverviewSection_CVCSG.cs** — Stub with ParameterRows (replaced in Stage 4).
- **OverviewSection_SystemHealth.cs** — Stub with ParameterRows (replaced in Stage 5).

### Files Orphaned
- OverviewSection_ReactorCore.cs — No longer referenced.
- OverviewSection_RCS.cs — No longer referenced.
- OverviewSection_CVCS.cs — No longer referenced.
- OverviewSection_SGRHR.cs — No longer referenced.
- OverviewSection_GlobalHealth.cs — No longer referenced.

---

## Stage 3 — Reactor/RCS Section with Gauges

### Files Rewritten
- **OverviewSection_ReactorRCS.cs** — Replaced text rows with 4 ArcGauges in 2×2 GridLayoutGroup (RCS Pressure 0-2500 psia, T-avg 50-650°F, Subcooling 0-200°F, Delta-T 0-80°F) with heatup-appropriate threshold bands. Added 4 compact pill StatusIndicators for RCP-1 through RCP-4 in horizontal row.

---

## Stage 4 — Pressurizer & CVCS/SG Sections with Gauges

### Files Rewritten
- **OverviewSection_Pressurizer.cs** — ArcGauge for PZR Level (0-100%, warn 17/80, alarm 12/92). Two StatusIndicators (BUBBLE with 3-state logic: off/warning-forming/normal-formed, HEATERS on/off). Two DigitalReadouts (PZR Temp °F, Surge Flow gpm).
- **OverviewSection_CVCSG.cs** — Two DigitalReadouts (Charging gpm, Letdown gpm). BidirectionalGauge for Net CVCS (±75 gpm). Two DigitalReadouts (SG Pressure psia, Net Heat MW).

---

## Stage 5 — System Health & Alarm Annunciator Grid

### Files Rewritten
- **OverviewSection_SystemHealth.cs** — Two DigitalReadouts (Mass Error lbm with instrument font, Energy Balance MW). Three StatusIndicators (Mass Conservation with 3-tier threshold: <500 green/<1000 amber/else red, SG Boiling on/off, RHR Active on/off).
- **OverviewSection_Alarms.cs** — Complete rewrite. Old AlarmTile class removed. 12 DashboardAnnunciatorTiles in 6×2 GridLayoutGroup (72×36 cells, 3px spacing). Row 1: PRESS HIGH (alarm), PRESS LOW (alarm), LVL HIGH (warning), LVL LOW (alarm), SUBCOOL LOW (alarm), VCT LEVEL (warning). Row 2: MASS CONS (alarm), FLOW LOW (alarm), SG PRESS HIGH (warning), PZR HTRS ON (status/green), SPRAY ACTIVE (status/green), BUBBLE FORMED (status/green). ACK button acknowledges all alerting tiles.

---

## Stage 6 — Detail Tab Hero Gauges & Tab Transitions

### Files Modified
- **PrimaryLoopPanel.cs** — Added hero gauge row with 3 ArcGauges (RCS Pressure, T-avg, Subcooling) above existing 3-column detail layout.
- **PressurizerPanel.cs** — Added hero gauge row with 2 ArcGauges (PZR Level, PZR Pressure) above existing 3-column detail layout.
- **CVCSPanel.cs** — Added hero gauge row with 2 ArcGauges (Charging, Letdown) + 1 BidirectionalGauge (Net Flow) above existing 4-column detail layout.
- **SGRHRPanel.cs** — Added hero gauge row with 2 ArcGauges (SG Pressure, SG Heat Transfer) above existing 3-column detail layout.
- **ValidationDashboardController.cs** — SwitchToTab() now performs CanvasGroup fade transition (fade out 0.125s → swap panels → fade in 0.125s). Added early return for same-tab clicks. Added FadeTabTransition coroutine.

---

## Files Summary

| File | Action | Stage |
|------|--------|-------|
| ValidationDashboardTheme.cs | Modified | 1 |
| DashboardAnnunciatorTile.cs | Created | 1 |
| InstrumentFontHelper.cs | Created | 1 |
| ArcGauge.cs | Modified (Create factory) | 1 |
| BidirectionalGauge.cs | Modified (Create factory) | 1 |
| DigitalReadout.cs | Modified (Create factory) | 1 |
| TabNavigationController.cs | Modified | 1 |
| OverviewPanel.cs | Modified (layout + sections) | 2 |
| OverviewSection_ReactorRCS.cs | Created → Rewritten | 2, 3 |
| OverviewSection_CVCSG.cs | Created → Rewritten | 2, 4 |
| OverviewSection_SystemHealth.cs | Created → Rewritten | 2, 5 |
| OverviewSection_Pressurizer.cs | Rewritten | 4 |
| OverviewSection_Alarms.cs | Rewritten | 5 |
| PrimaryLoopPanel.cs | Modified | 6 |
| PressurizerPanel.cs | Modified | 6 |
| CVCSPanel.cs | Modified | 6 |
| SGRHRPanel.cs | Modified | 6 |
| ValidationDashboardController.cs | Modified | 6 |

---

## Modules NOT Touched

- HeatupSimEngine (all variants) — GOLD, read-only consumer
- All physics modules — No changes
- MosaicAlarmPanel.cs — Referenced for style, not modified
- MosaicGauge.cs — Referenced for font pattern, not modified
- Any .unity scene files — No scene modifications

---

## Orphaned Files (Safe to Delete)

These files are no longer referenced by any active code:

- `OverviewSection_ReactorCore.cs` (+ .meta)
- `OverviewSection_RCS.cs` (+ .meta)
- `OverviewSection_CVCS.cs` (+ .meta)
- `OverviewSection_SGRHR.cs` (+ .meta)
- `OverviewSection_GlobalHealth.cs` (+ .meta)
