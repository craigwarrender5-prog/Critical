# CRITICAL: Master the Atom — Update v1.3.0.0

**Date:** 2026-02-06
**Type:** Minor Build (backwards compatible)
**Scope:** Reactor Operator GUI — Design Specification

---

## Summary

Created comprehensive design specification document for the Reactor Operator GUI — the primary gameplay interface for CRITICAL: Master the Atom. This is a design-only deliverable; no code changes were made to existing modules.

## Document Delivered

**ReactorOperatorGUI_Design_v1.0.0.0.docx** — 10-page design specification covering:

### 1. Design Overview
- Purpose and design philosophy (Westinghouse PWR + RBMK mosaic hybrid aesthetic)
- 1920×1080 windowed resolution target
- Four-zone layout strategy (Left Gauges, Core Map, Right Gauges, Bottom Panel)
- Dark industrial color theme with nuclear green/amber/red accent system

### 2. Central Core Mosaic Map (Centerpiece)
- 193 fuel assembly layout in authentic Westinghouse 15×15 cross-pattern
- 53 RCCA locations distributed across 8 banks (SA, SB, SC, SD, D, C, B, A)
- Bank count breakdown: 24 shutdown + 29 control = 53 total
- Four display modes: Relative Power, Fuel Temperature, Coolant Temperature, Bank Map
- RCCA overlay rendering with bank-specific colors and insertion indicators
- **Interactive behavior specification:**
  - Hover: tooltip with assembly data
  - Click: select assembly, highlight bank members
  - Bank filter buttons (8 individual + ALL/CTRL/SD quick-select)
  - Display mode toggle buttons
- Assembly detail panel design for drill-down data

### 3. Instrument Gauge Columns
- Left column: 9 nuclear instrumentation gauges (power, reactivity, period, SUR, keff, boron, xenon, flow)
- Right column: 8 thermal-hydraulic gauges (Tavg, Thot, Tcold, ΔT, fuel temps, pressure, PZR level)
- All gauge data sources mapped to ReactorController properties

### 4. Bottom Control Panel
- Rod control group (withdraw/insert/stop, bank select, auto/manual)
- 8-bank position bar display with bank colors and step counts
- Boron/chemistry controls (borate/dilute, rate selector)
- Trip and safety controls (trip button with safety cover, reset, status indicator)
- Time compression controls (1x–10000x, pause, sim clock)
- 10-tile alarm annunciator strip with Westinghouse-style flash/acknowledge behavior

### 5. Data Architecture
- Strict one-directional data flow: Physics → Controller → GUI
- 5 new components identified (ReactorOperatorScreen, CoreMosaicMap, CoreMapData, AssemblyDetailPanel, OperatorScreenBuilder)
- 6 existing GOLD standard components reused without modification
- CoreMapData static layout specification

### 6. Implementation Plan
- 6-step phased implementation (data → map → detail → screen → builder → test)
- Technical constraints (update rates, uGUI, object pooling, GOLD module protection)
- 10 acceptance criteria

### 7. Pixel Layout Specification
- Exact pixel regions for all zones at 1920×1080

## Files Changed

| File | Action | Description |
|------|--------|-------------|
| `Documentation/ReactorOperatorGUI_Design_v1.0.0.0.docx` | NEW | Design specification document |
| `Documentation/Updates/UPDATE-v1.3.0.0.md` | NEW | This changelog |

## Impact on Existing Code

**None.** This is a design document only. No existing GOLD standard modules were modified. All referenced code (PlantConstants, ControlRodBank, FuelAssembly, ReactorController, MosaicBoard, etc.) remains unchanged.

## Next Steps

Implementation of the 6-step plan outlined in Section 6 of the design document, beginning with CoreMapData.cs (authenticated 193-assembly layout data).
