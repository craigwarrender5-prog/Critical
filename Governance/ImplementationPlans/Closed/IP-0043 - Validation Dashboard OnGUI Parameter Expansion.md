# IP-0043: Validation Dashboard — New OnGUI Dashboard

**Date:** 2026-02-17  
**Domain Plan:** DP-0008 (Operator Interface & Scenarios)  
**Predecessors:** IP-0042 (SUPERSEDED)  
**Status:** CLOSED  
**Closed Date:** 2026-02-17  
**Closure Note:** Completed and closed; CS-0107 archived as FIXED.  
**Priority:** High  
**Changelog Required:** Yes  
**Target Version:** v1.0.0.0  
**Changelog:** CL-0043-v1.0.0.0-FINAL.md

---

## 1. Executive Summary

Following the failure of IP-0042 (UI Toolkit rebuild) due to Unity 6's `Painter2D.Arc()` method not rendering correctly, this IP creates a **brand new OnGUI-based dashboard** designed from the ground up with comprehensive parameter coverage.

The existing `HeatupValidationVisual` dashboard remains untouched as a fallback. The new dashboard will be a separate system that can coexist with or replace the legacy dashboard.

---

## 2. Scene Integration

### 2.1 Existing Architecture

The simulator uses a two-scene architecture:

1. **MainScene** — Primary operator screen (build index 0, always loaded)
2. **Validator.unity** — Loaded additively via `SceneBridge.cs` when user presses **V** key

The `SceneBridge` component manages view switching:
- **V key** → Load Validator scene additively, hide OperatorScreensCanvas
- **1-8 / Tab / Esc** → Unload Validator scene, show OperatorScreensCanvas
- `HeatupSimEngine` persists via `DontDestroyOnLoad` regardless of active scene

### 2.2 New Dashboard Placement

The new `ValidationDashboard` will:
- Live in the **Validator.unity** scene (same as existing `HeatupValidationVisual`)
- Be toggled via **V key** (handled by existing `SceneBridge`)
- Find the persistent `HeatupSimEngine` via `FindObjectOfType` (same pattern as existing dashboard)
- Coexist with `HeatupValidationVisual` initially — a toggle (e.g., **F2**) can switch between old/new dashboards

### 2.3 File Structure

New files will be placed alongside existing validation dashboard files:

```
Assets/Scripts/Validation/
├── HeatupSimEngine.cs                   — (existing) Simulation engine
├── HeatupSimEngine.*.cs                 — (existing) Engine partials
├── HeatupValidationVisual.cs            — (existing) Legacy dashboard
├── HeatupValidationVisual.*.cs          — (existing) Legacy dashboard partials
├── ValidationDashboard.cs               — NEW: Main MonoBehaviour
├── ValidationDashboard.Layout.cs        — NEW: Screen layout calculations
├── ValidationDashboard.Styles.cs        — NEW: Colors, fonts, GUI styles
├── ValidationDashboard.Gauges.cs        — NEW: Arc gauge, bar gauge, LED rendering
├── ValidationDashboard.Panels.cs        — NEW: Panel/section rendering helpers
├── ValidationDashboard.Snapshot.cs      — NEW: Data snapshot class
├── Tabs/
│   ├── DashboardTab.cs                  — NEW: Base class for tabs
│   ├── OverviewTab.cs                   — NEW: Primary operations surface
│   ├── RCSTab.cs                        — NEW: RCS Primary details
│   ├── PressurizerTab.cs                — NEW: Pressurizer details
│   ├── CVCSTab.cs                       — NEW: CVCS/VCT details
│   ├── SGRHRTab.cs                      — NEW: SG and RHR details
│   ├── SystemsTab.cs                    — NEW: BRS, Orifices, Mass Conservation
│   ├── GraphsTab.cs                     — NEW: Strip chart trends
│   └── LogTab.cs                        — NEW: Event log and annunciators
```

---

## 3. Design Goals

1. **Comprehensive parameter coverage** — Display all ~130 parameters from requirements
2. **Primary operations surface** — All critical parameters visible without tab switching
3. **Professional control room aesthetic** — Nuclear instrument look and feel
4. **Clean architecture** — Modular, maintainable, well-documented code
5. **GOLD standard from day one** — Follow all project quality criteria
6. **Scene integration** — Work seamlessly with existing V-key toggle system

---

## 4. Architecture

### 4.1 Class Responsibilities

| Class | Responsibility |
|-------|----------------|
| `ValidationDashboard` | MonoBehaviour entry point, Update loop, keyboard input, tab switching |
| `ValidationDashboard.Layout` | Screen regions, margins, column calculations |
| `ValidationDashboard.Styles` | GUIStyle cache, color palette, fonts |
| `ValidationDashboard.Gauges` | Reusable gauge drawing (arc, bar, LED, digital readout) |
| `ValidationDashboard.Panels` | Section headers, bordered panels, parameter rows |
| `ValidationDashboard.Snapshot` | Data snapshot class for 10 Hz capture |
| `DashboardTab` | Abstract base with Draw() method, snapshot reference |
| Individual tabs | Specific parameter layouts for each system |

### 4.2 Dashboard Toggle (Old/New)

While both dashboards exist:
- **F1** — Toggle current dashboard visibility (existing behavior)
- **F2** — Switch between old (`HeatupValidationVisual`) and new (`ValidationDashboard`)

Only one dashboard renders at a time. Both read from the same `HeatupSimEngine`.

---

## 5. Primary Operations Surface (Overview Tab)

The Overview tab displays ALL critical parameters on a single 1920×1080 screen with NO scrolling.

### 5.1 Layout

```
┌────────────────────────────────────────────────────────────────────────────────┐
│ HEADER: Mode │ Phase │ Sim Time │ Wall Time │ Speed Controls │ Alarm Count    │
├────────────┬────────────┬────────────┬────────────┬────────────────────────────┤
│ RCS        │ PRESSURIZER│ CVCS/VCT   │ SG/RHR     │ TRENDS (8 sparklines)     │
│ PRIMARY    │            │            │            │                            │
│            │            │            │            │ • RCS Pressure             │
│ [ARC]      │ [ARC]      │ Chg ────── │ [ARC]      │ • PZR Level                │
│ T_avg      │ Pressure   │ Ltd ────── │ SG Press   │ • T_avg                    │
│            │            │ Net ══════ │            │ • Heatup Rate              │
│ T_hot      │ [ARC]      │            │ SG Temps   │ • Subcooling               │
│ T_cold     │ Level      │ [ARC]      │ RHR MW     │ • Net CVCS                 │
│ ΔT         │            │ VCT Level  │            │ • SG Pressure              │
│            │ Heater kW  │            │ [BAR]      │ • Net Heat                 │
│ [ARC]      │ Spray %    │ Mass Err   │ HZP Prog   │                            │
│ Subcool    │ Surge ±    │            │            │                            │
│            │            │ BRS Level  │            │                            │
│ Heatup °/hr│ Bubble ●   │            │ HZP Rdy ●  │                            │
│ Press psi/m│            │            │            │                            │
│            │            │            │            │                            │
│ RCPs ●●●●  │            │            │            │                            │
├────────────┴────────────┴────────────┴────────────┼────────────────────────────┤
│ ANNUNCIATORS (27 tiles in 3 rows)                 │ EVENT LOG (last 8 events)  │
│ [HTR][HTUP][BUBL][MODE][PLO][PHI][SCLO][FLOW]    │ 12:15:30 Phase → HEATUP    │
│ [LLO][LHI][RVLIS][CCW][CHG][LTD][SEAL][VLO]     │ 12:15:35 PZR Level Low     │
│ [VHI][DVRT][MKUP][RWST][R1][R2][R3][R4]...      │ 12:16:01 Subcool < 20°F    │
│                                    [ACK] [RESET] │                            │
└──────────────────────────────────────────────────┴────────────────────────────┘
```

### 5.2 Column Widths

| Column | Width % | Content |
|--------|---------|---------|
| RCS | 18% | T_avg arc, subcool arc, temps, rates, RCPs |
| PZR | 16% | Pressure arc, level arc, heater, spray, surge, bubble |
| CVCS | 16% | VCT arc, charging/letdown bars, mass conservation |
| SG/RHR | 16% | SG pressure arc, HZP bar, RHR status |
| Trends | 24% | 8 mini sparkline graphs |
| Footer | 100% | Annunciators (60%) + Event Log (40%) |

---

## 6. Complete Parameter Inventory

### 6.1 Overview Tab (Always Visible)

**RCS Column (18 parameters):**
- T_avg (arc gauge)
- T_hot, T_cold, Core ΔT (digital)
- T_pzr, T_sat (digital)
- Subcooling (arc gauge)
- Heatup rate °F/hr (digital, colored)
- Pressure rate psi/min (digital, colored)
- RCP status ×4 (LEDs)
- RCP count, RCP heat MW (digital)

**Pressurizer Column (16 parameters):**
- Pressure (arc gauge)
- Level (arc gauge)
- Water volume, Steam volume (digital)
- Heater power kW (bar gauge)
- Heater mode (text)
- Spray active (LED)
- Spray flow gpm (digital)
- Spray valve % (bar gauge)
- Surge flow ±gpm (bidirectional bar)
- Bubble state (LED + text)
- Pressure error, Level error (digital)

**CVCS Column (14 parameters):**
- VCT Level (arc gauge)
- Charging flow, Letdown flow (bar gauges)
- Net CVCS (bidirectional bar)
- Charging active, Letdown active (LEDs)
- VCT Makeup, VCT Divert (LEDs)
- Mass error lbm (digital, alarmed)
- BRS Holdup % (digital)
- Seal injection OK (LED)

**SG/RHR Column (12 parameters):**
- SG Secondary Pressure (arc gauge)
- SG Sat Temp, SG Bulk Temp (digital)
- SG Heat Transfer MW (digital)
- SG Boiling (LED)
- Steam Dump Active (LED)
- RHR Active (LED)
- RHR Mode (text)
- RHR Net Heat MW (digital)
- HZP Progress (bar gauge)
- HZP Ready (LED)

**Trends Column (8 sparklines):**
- RCS Pressure
- PZR Level
- T_avg / T_hot / T_cold
- Heatup Rate
- Subcooling
- Net CVCS
- SG Pressure
- Net Plant Heat

**Footer — Annunciators (27 tiles):**
All existing annunciators from legacy dashboard

**Footer — Event Log (8 entries):**
Scrollable with severity colors

### 6.2 Detail Tabs

Each detail tab expands on its system with:
- Larger gauges
- Full parameter list
- Stacked strip charts
- Diagnostic information

---

## 7. Implementation Stages

### Stage 1: Core Infrastructure
**Objective:** Create base classes and rendering primitives.

**Tasks:**
1. Create `ValidationDashboard.cs` — MonoBehaviour shell with engine binding
2. Create `ValidationDashboard.Layout.cs` — Screen region calculations with caching
3. Create `ValidationDashboard.Styles.cs` — Color palette, GUIStyle factory (all cached)
4. Create `ValidationDashboard.Gauges.cs` — Arc gauge, bar gauge, LED, digital readout
5. Create `ValidationDashboard.Panels.cs` — Section headers, bordered panels
6. Create `ValidationDashboard.Snapshot.cs` — Data snapshot class
7. Create `Tabs/` directory and `DashboardTab.cs` — Abstract base class
8. Test: Render a test pattern with each gauge type
9. **Performance gate:** Gauge rendering < 0.5ms for 10 gauges

**Deliverables:**
- Core partial classes
- Working gauge rendering
- Dashboard toggle (F2) between old/new

**Exit Criteria:** Can draw arc gauge, bar gauge, LED, and digital readout on screen. F2 toggles between old/new dashboard. Performance gate passed.

---

### Stage 2: Overview Tab — Layout Framework
**Objective:** Build the 5-column + footer layout structure.

**Tasks:**
1. Create `Tabs/OverviewTab.cs`
2. Implement 5-column layout with proper proportions
3. Implement footer split (annunciators left, log right)
4. Add placeholder content in each section
5. Verify layout at 1920×1080 and 2560×1440
6. Implement resolution-change detection and rect recalculation

**Deliverables:**
- `OverviewTab.cs` with complete layout

**Exit Criteria:** Layout renders correctly at both resolutions.

---

### Stage 3: Overview Tab — RCS and Pressurizer Columns
**Objective:** Populate RCS and PZR columns with live data.

**Tasks:**
1. RCS Column: T_avg arc, subcooling arc, all digital readouts, RCP LEDs
2. PZR Column: Pressure arc, level arc, heater bar, spray bar, surge bar, bubble LED
3. Wire all parameters to snapshot
4. Implement threshold coloring
5. Implement string preformatting in Update() (not OnGUI)

**Deliverables:**
- Complete RCS column
- Complete PZR column

**Exit Criteria:** 34 parameters updating live with correct coloring.

---

### Stage 4: Overview Tab — CVCS, SG/RHR, and Trends Columns
**Objective:** Complete remaining columns.

**Tasks:**
1. CVCS Column: VCT arc, charging/letdown bars, net CVCS bar, LEDs, mass error
2. SG/RHR Column: SG pressure arc, HZP bar, RHR status, LEDs
3. Trends Column: 8 mini sparklines using cached Texture2D approach
4. Wire all parameters to snapshot
5. **Performance gate:** Sparklines < 0.3ms for 8 charts

**Deliverables:**
- Complete CVCS column
- Complete SG/RHR column
- Complete Trends column

**Exit Criteria:** All 5 columns populated with live data. Sparkline performance gate passed.

---

### Stage 5: Overview Tab — Footer (Annunciators + Event Log)
**Objective:** Complete the footer with annunciators and event log.

**Tasks:**
1. Implement 27-tile annunciator grid (3 rows × 9 columns)
2. Implement annunciator state machine (ISA-18.1)
3. Implement click-to-acknowledge on individual ALERTING tiles
4. Implement ACK button (batch acknowledge all alerting)
5. Implement RESET button (clear all acknowledged that returned to normal)
6. Implement event log panel (last 8 entries)
7. Implement severity filtering buttons
8. **Performance gate:** Full Overview < 1.5ms

**Deliverables:**
- Complete annunciator panel with click-to-acknowledge
- Complete event log

**Exit Criteria:** Annunciators flash correctly, individual tile click acknowledges that tile, ACK button acknowledges all, log scrolls. Overview performance gate passed.

---

### Stage 6: Detail Tabs (RCS, PZR, CVCS, SG/RHR)
**Objective:** Create system detail tabs.

**Tasks:**
1. Create `Tabs/RCSTab.cs` — expanded RCS detail + TEMPS/PRESSURE graphs
2. Create `Tabs/PressurizerTab.cs` — expanded PZR detail + PRESSURE/RATES graphs
3. Create `Tabs/CVCSTab.cs` — expanded CVCS/VCT detail + CVCS/VCT graphs
4. Create `Tabs/SGRHRTab.cs` — expanded SG/RHR detail + SG/HZP graphs
5. Implement tab switching (keyboard Ctrl+1-4)

**Deliverables:**
- 4 detail tab classes

**Exit Criteria:** Each tab accessible and displays complete system data.

---

### Stage 7: Systems and Graphs Tabs
**Objective:** Create auxiliary tabs.

**Tasks:**
1. Create `Tabs/SystemsTab.cs` — BRS detail, Orifice status, Mass conservation audit
2. Create `Tabs/GraphsTab.cs` — Full-width tabbed strip charts (7 categories)
3. Create `Tabs/LogTab.cs` — Full annunciator grid + expanded event log
4. Implement keyboard shortcuts (Ctrl+5-7)

**Deliverables:**
- 3 auxiliary tab classes

**Exit Criteria:** All tabs accessible and functional.

---

### Stage 8: Polish and Integration
**Objective:** Final polish and switchover.

**Tasks:**
1. Implement all keyboard shortcuts (F1 toggle, F2 old/new switch, Ctrl+1-8 tabs, F5-F9 speed)
2. Add header with mode/phase/time display
3. Performance optimization pass
4. Test at multiple resolutions
5. Verify integration with SceneBridge (V key loads Validator scene)
6. **Final performance gate:** Full dashboard < 2.0ms
7. Write changelog

**Deliverables:**
- Complete, polished dashboard
- Seamless V-key scene integration
- CHANGELOG_v1.0.0.0.md

**Exit Criteria:** Dashboard is production-ready, integrates with V-key scene switching. Final performance gate passed.

---

## 8. Technical Specifications

### 8.1 Color Palette

| Use | Color | Hex |
|-----|-------|-----|
| Background | Dark blue-black | #0A0A12 |
| Panel | Dark panel | #141420 |
| Normal | Bright green | #00FF88 |
| Warning | Amber | #FFAA00 |
| Alarm | Red | #FF4444 |
| Info | Cyan | #66CCFF |
| Text | Light gray | #CCCCCC |
| Dim | Dim gray | #666666 |

### 8.2 Gauge Specifications

**Arc Gauge:**
- 180° sweep (9 o'clock to 3 o'clock)
- Configurable min/max
- Threshold color bands
- Animated needle (SmoothDamp)
- Digital value display below

**Bar Gauge:**
- Horizontal or vertical
- Configurable min/max
- Fill color based on thresholds
- Optional setpoint marker

**Bidirectional Bar:**
- Center-zero reference
- Positive/negative fill colors
- Value label

**LED Indicator:**
- On/Off/Warning/Alarm states
- Optional flash for alerting
- Label above or beside

**Digital Readout:**
- Configurable format string
- Threshold-based coloring
- Unit suffix

### 8.3 Performance Targets

- Update rate: 10 Hz
- Frame budget: < 2ms for OnGUI
- Memory: No per-frame allocations

### 8.4 Performance Architecture (Critical)

**Risk:** 60+ parameters at 10 Hz with < 2ms budget is aggressive. The following mitigations are **mandatory**.

#### 8.4.1 GUIStyle Caching

All GUIStyles created **once** in Awake(), never in OnGUI():

```csharp
// ValidationDashboard.Styles.cs
private static GUIStyle _labelNormal;
private static GUIStyle _labelWarning;
private static GUIStyle _labelAlarm;
private static GUIStyle _boxPanel;
private static bool _stylesInitialized = false;

public static void InitializeStyles()
{
    if (_stylesInitialized) return;
    _labelNormal = new GUIStyle(GUI.skin.label) { ... };
    // ... all styles
    _stylesInitialized = true;
}
```

#### 8.4.2 Rect Precomputation

Rects computed **once** per resolution change, not per frame:

```csharp
// ValidationDashboard.Layout.cs
private static Rect[] _columnRects = new Rect[5];
private static Rect[] _gaugeRects = new Rect[20];
private static Rect[] _annunciatorRects = new Rect[27];
private static int _lastScreenWidth;
private static int _lastScreenHeight;

public static void UpdateLayoutIfNeeded()
{
    if (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight)
        return; // No recalculation needed
    
    _lastScreenWidth = Screen.width;
    _lastScreenHeight = Screen.height;
    // ... compute all rects once
}
```

#### 8.4.3 String Preformatting (Zero Allocation)

Strings formatted in `Update()`, not in `OnGUI()`. Use preallocated char buffers:

```csharp
// Per-parameter cached strings
private string _tavgDisplay;
private string _pressureDisplay;
// ... etc

void Update()
{
    // Format once per update cycle using cached StringBuilder
    _tavgDisplay = FormatValue(engine.T_avg, "F1", " °F");
    _pressureDisplay = FormatValue(engine.pressure, "F0", " psia");
}

void OnGUI()
{
    // Just draw the pre-formatted string — no allocation
    GUI.Label(rect, _tavgDisplay, style);
}
```

#### 8.4.4 Sparkline Architecture

Sparklines use **fixed-size circular buffers** and **cached Texture2D**:

```csharp
public class SparklineRenderer
{
    private float[] _buffer;        // Fixed size (e.g., 256 points)
    private int _head;              // Circular write position
    private int _count;             // Valid entries
    private Texture2D _texture;     // Created once, updated in place
    private Color32[] _pixels;      // Preallocated pixel buffer
    
    public SparklineRenderer(int width, int height, int bufferSize)
    {
        _buffer = new float[bufferSize];
        _texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        _pixels = new Color32[width * height];
    }
    
    public void Push(float value)
    {
        _buffer[_head] = value;
        _head = (_head + 1) % _buffer.Length;
        if (_count < _buffer.Length) _count++;
    }
    
    public void UpdateTexture()
    {
        // Clear pixels (use Array.Clear or manual loop)
        // Draw line into _pixels array
        // Single SetPixels32 + Apply call
        _texture.SetPixels32(_pixels);
        _texture.Apply(false); // false = don't rebuild mipmaps
    }
    
    public void Draw(Rect rect)
    {
        GUI.DrawTexture(rect, _texture);
    }
}
```

**Key constraints:**
- **Never** allocate new Texture2D during OnGUI
- **Never** use GL.Begin/GL.End per point
- **Never** allocate new lists or arrays during rendering
- Use `SetPixels32` + `Apply(false)` for fastest texture updates

#### 8.4.5 Snapshot Isolation Rule

**The dashboard reads from a data snapshot updated at 10 Hz. The UI never queries live engine values during OnGUI.**

```csharp
// ValidationDashboard.Snapshot.cs
public class DashboardSnapshot
{
    // RCS
    public float T_avg;
    public float T_hot;
    public float T_cold;
    public float pressure;
    public float subcooling;
    // ... all parameters copied here
    
    public void CaptureFrom(HeatupSimEngine engine)
    {
        T_avg = engine.T_avg;
        T_hot = engine.T_hot;
        // ... single point-in-time capture
    }
}

private DashboardSnapshot _snapshot = new DashboardSnapshot();
private float _lastSnapshotTime;

void Update()
{
    if (Time.time - _lastSnapshotTime < 0.1f) return; // 10 Hz
    _lastSnapshotTime = Time.time;
    
    _snapshot.CaptureFrom(engine);  // Atomic snapshot
    PreformatAllStrings();          // Format from snapshot
}

void OnGUI()
{
    // ONLY reads from _snapshot, NEVER from engine directly
    DrawGauge(rect, _snapshot.T_avg, ...);
}
```

**This prevents:**
- Race conditions between simulation and rendering
- Partial-frame data inconsistencies (e.g., T_avg from frame N, pressure from frame N+1)
- UI logic accidentally influencing simulation timing
- Difficult-to-debug visual glitches

**This enables:**
- Clean profiling (simulation vs. rendering clearly separated)
- Future threading if needed (snapshot is thread-safe copy)
- Deterministic UI behavior for testing

#### 8.4.6 Performance Validation Gates

Each stage has a mandatory performance gate:

| Stage | Gate | Budget |
|-------|------|--------|
| 1 | 10 gauges rendering | < 0.5ms |
| 4 | 8 sparklines rendering | < 0.3ms |
| 5 | Full Overview tab | < 1.5ms |
| 8 | Full dashboard (any tab) | < 2.0ms |

**If any gate fails, stop and optimize before proceeding.**

---

## 9. File Manifest

### New Files

| File | Location | Purpose |
|------|----------|---------|
| `ValidationDashboard.cs` | `Assets/Scripts/Validation/` | Main MonoBehaviour |
| `ValidationDashboard.Layout.cs` | `Assets/Scripts/Validation/` | Layout calculations |
| `ValidationDashboard.Styles.cs` | `Assets/Scripts/Validation/` | GUI styles and colors |
| `ValidationDashboard.Gauges.cs` | `Assets/Scripts/Validation/` | Gauge rendering |
| `ValidationDashboard.Panels.cs` | `Assets/Scripts/Validation/` | Panel helpers |
| `ValidationDashboard.Snapshot.cs` | `Assets/Scripts/Validation/` | Data snapshot class |
| `DashboardTab.cs` | `Assets/Scripts/Validation/Tabs/` | Tab base class |
| `OverviewTab.cs` | `Assets/Scripts/Validation/Tabs/` | Primary surface |
| `RCSTab.cs` | `Assets/Scripts/Validation/Tabs/` | RCS detail |
| `PressurizerTab.cs` | `Assets/Scripts/Validation/Tabs/` | PZR detail |
| `CVCSTab.cs` | `Assets/Scripts/Validation/Tabs/` | CVCS detail |
| `SGRHRTab.cs` | `Assets/Scripts/Validation/Tabs/` | SG/RHR detail |
| `SystemsTab.cs` | `Assets/Scripts/Validation/Tabs/` | Auxiliary systems |
| `GraphsTab.cs` | `Assets/Scripts/Validation/Tabs/` | Strip charts |
| `LogTab.cs` | `Assets/Scripts/Validation/Tabs/` | Event log |

### Modified Files

| File | Changes |
|------|---------|
| `Validator.unity` | Add ValidationDashboard GameObject |

### Unchanged Files (Preserved as Fallback)

| File | Notes |
|------|-------|
| `HeatupValidationVisual.cs` | Legacy dashboard — remains functional |
| `HeatupValidationVisual.*.cs` | All legacy partials preserved |
| `SceneBridge.cs` | No changes needed — V key already works |

---

## 10. Success Criteria

- [x] Overview tab displays 60+ parameters without scrolling
- [x] All 8 tabs accessible and functional
- [x] 27 annunciator tiles with correct ISA-18.1 behavior
- [x] Click-to-acknowledge on individual ALERTING tiles
- [x] 8 sparkline trends on Overview
- [x] Full strip charts on Graphs tab
- [x] Event log with severity filtering
- [x] All keyboard shortcuts working (F1, F2, Ctrl+1-8, F5-F9)
- [x] V key loads Validator scene with new dashboard
- [x] F2 toggles between old/new dashboard (during transition period)
- [x] Performance < 2ms per frame (all gates passed)
- [x] Works at 1080p and 1440p

---

## 11. Risk Assessment

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Performance exceeds 2ms | Medium | Mandatory performance gates at each stage; stop and optimize if failed |
| Sparkline texture updates too slow | Medium | Use SetPixels32 + Apply(false); pre-allocated buffers |
| String allocation spikes | Low | All strings preformatted in Update(); no concatenation in OnGUI |
| Layout breaks at different resolutions | Low | Rect caching with resolution-change detection |
| GUIStyle creation in hot path | Low | Static initialization with guard flag |
| Scene integration issues | Low | Uses same pattern as existing HeatupValidationVisual |

---

## 12. Approval

- [x] **IP-0043 approved to begin** — Craig
- [x] **Stage 1 complete (Core Infrastructure)** — Craig
- [x] **Stage 2 complete (Layout Framework)** — Craig
- [x] **Stage 3 complete (RCS/PZR Columns)** — Merged into Stage 2
- [x] **Stage 4 complete (CVCS/SG/Trends)** — Craig
- [x] **Stage 5 complete (Footer)** — Craig
- [x] **Stage 6 complete (Detail Tabs)** — Craig
- [x] **Stage 7 complete (Aux Tabs)** — Craig
- [x] **Stage 8 complete (Polish)** — Craig
- [x] **IP-0043 closed** — Craig

---

*Implementation Plan prepared by Claude*  
*Completed: 2026-02-17*
