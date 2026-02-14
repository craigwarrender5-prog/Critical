# Implementation Plan v5.3.2 — Critical Tab uGUI Thermal Diagnostic Dashboard

**Version:** v5.3.2  
**Date:** 2026-02-12  
**Phase:** 0 — Thermal & Conservation Stability  
**Priority:** Blocked until v5.3.1 validation passes  
**Predecessor:** v5.3.1 (validation fix + logging refactor)  

---

## 0. Preconditions / Gate

**This plan DOES NOT implement any code changes.**

Before implementation may begin:

1. ✅ v5.3.1 validation must be complete with all acceptance tests (AT-1 through AT-5) passing
2. ✅ Craig must explicitly confirm validation completion
3. ✅ Changelog for v5.3.1 must exist in `Critical\Updates\Changelogs\`

**Implementation may NOT proceed until the gate is passed.**

---

## 1. Problem Statement

The current Critical tab (`HeatupValidationVisual.TabCritical.cs`) is a v5.2.0 IMGUI panel displaying text-based status readouts in a 2-row, 3+2 grid layout. While functional for basic at-a-glance monitoring, it has significant limitations:

### 1.1 Current State

- **Sparse visual presentation:** Large blocks of empty space between text labels
- **No temporal context:** All values are instantaneous snapshots with no history
- **Hidden thermal dynamics:** SG node stratification requires mental arithmetic to interpret
- **No visual T_sat relationship:** The critical "hottest node vs saturation" margin is a text number, not a visual indicator
- **Heat balance opaque:** Total SG Q vs Σ node Q discrepancies are invisible
- **Debugging is log-driven:** Identifying unstable physics (pressure stall, top-node pinning, heat scaling spikes) requires reading interval logs

### 1.2 Why This Matters

During heatup validation:
- **Pressure not rising** when it should is a symptom that takes minutes to identify via text
- **SG node temperature pinning** at T_sat is invisible without log analysis
- **Heat transfer spikes** at regime transitions are not evident in real-time
- **Energy/mass balance residuals** are not displayed, hiding conservation issues

The Critical tab should make unstable physics **obvious within seconds**.

---

## 2. Objectives

Convert the Critical tab into a real-time **Thermal Diagnostic Dashboard** that:

| Objective | Metric |
|-----------|--------|
| **O1:** Make thermal instabilities visually obvious | User identifies pressure stall or node pinning within 5 seconds of onset |
| **O2:** Show temporal evolution | Rolling strip charts with ≥60-second history |
| **O3:** Visualize SG stratification | Vertical node stack with saturation line overlay |
| **O4:** Display heat balance integrity | TotalQ vs ΣnodeQ discrepancy > 0.5 MW triggers visible warning |
| **O5:** Remain performant | No GC spikes, <1ms Update() overhead, ≥60 FPS maintained at 10× speed |
| **O6:** Decouple UI from physics | UI reads only from telemetry bus; never queries physics modules directly |

---

## 3. Non-Goals (Explicitly Out of Scope for v5.3.2)

| Non-Goal | Rationale |
|----------|-----------|
| Turbine/condenser modeling | Phase 4 work (v5.8.0) |
| SG physics refactor | This release is **observability/UI only** — no physics changes |
| New plant control logic | Steam dumps, feedwater, etc. remain unchanged |
| Redesign of other tabs | Only the Critical tab is affected |
| Full secondary thermal model | Planned for v6.0.0; this UI should *support* it when available |
| uGUI prefab architecture | Using programmatic uGUI construction for faster iteration; prefab conversion deferred |

---

## 4. Deliverables

### 4.A — New uGUI Critical Tab Layout (4 Zones)

Replace the current IMGUI 2×3+2 grid with a uGUI layout containing four functional zones:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          CRITICAL TAB HEADER                                 │
├────────────────────────────┬────────────────────┬───────────────────────────┤
│                            │                    │                           │
│   ZONE 1: RCS PRIMARY      │  ZONE 2: PZR STATE │  ZONE 3: SG THERMAL STACK │
│   HEALTH                   │                    │                           │
│                            │                    │                           │
│   • Arc gauges: P, T_avg   │  • Vertical level  │  • Vertical node temp     │
│   • Strip chart: T_avg, P  │    column          │    bands (5 segments)     │
│                            │  • Heater/spray    │  • T_sat overlay line     │
│                            │    bars            │  • Margin indicator       │
│                            │  • Bubble state    │  • Boiling shimmer        │
│                            │                    │                           │
├────────────────────────────┴────────────────────┴───────────────────────────┤
│                                                                             │
│                        ZONE 4: BOTTOM DEBUG GRAPHS                          │
│                                                                             │
│   [P_sec vs time] [T_sat vs time] [T_hot vs time] [TotalQ vs ΣnodeQ] [Res]  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### Zone 1 — RCS Primary Health
- **Arc gauges:** RCS Pressure (0–2500 psia), T_avg (100–600°F), selectable T_hot/T_cold
- **Strip chart:** Rolling 60s window showing T_avg (°F) and Pressure (psia) on dual Y-axis
- **Threshold coloring:** Normal (green), Warning (amber), Alarm (red) per existing PlantConstants

#### Zone 2 — Pressurizer State
- **Vertical level column:** Water level 0–100% with solid/bubble/two-phase visual overlay
- **Heater power bar:** 0–1400 kW horizontal bar with animation
- **Spray indicator:** Flow rate bar (when active) with animated fill
- **Bubble state indicator:** SOLID / FORMING / NORMAL (BUBBLE) with color coding

#### Zone 3 — Steam Generator Thermal Stack (Primary Feature)
- **Vertical node temperature stack:** 5 horizontal bands representing SG nodes (top to bottom)
- **Color gradient:** Blue (cold) → Orange (T_sat approach) → Red (boiling)
- **T_sat overlay line:** Horizontal line at current T_sat(P_sec), moves with pressure
- **Margin display:** Numeric (T_hot − T_sat) with approach warning pulse when margin < 20°F
- **Boiling shimmer:** Optional subtle animation effect when node is actively boiling

#### Zone 4 — Bottom Debug Graphs
- **P_sec vs time:** SG secondary pressure strip chart (rolling 60s)
- **T_sat vs time:** Saturation temperature strip chart
- **T_hot vs time:** Hottest node temperature (from SGMultiNodeState.TopNodeTemp_F)
- **TotalQ vs ΣnodeQ:** Two-line plot comparing `TotalHeatAbsorption_MW` vs sum of `NodeHeatRates[]`
- **Energy/mass residual:** Placeholder for conservation residual (displays "N/A" until v5.4.0+)

---

### 4.B — Telemetry Bus Layer

Create a centralized telemetry publisher that decouples the UI from physics modules:

**PlantTelemetry.cs** — ScriptableObject or MonoBehaviour singleton providing:

```csharp
public class PlantTelemetry : MonoBehaviour
{
    // RCS
    public float Pressure_psia;
    public float Tavg_F;
    public float Thot_F;
    public float Tcold_F;
    public float PressureRate_psihr;
    
    // PZR
    public float PZRPressure_psia;
    public float PZRLevel_pct;
    public float HeaterPower_kW;
    public float SprayFlow_gpm;
    public bool BubbleFormed;
    public bool SolidPressurizer;
    
    // SG
    public float SGSecondaryPressure_psia;
    public float SGTsat_F;
    public float SGBulkTemp_F;
    public float[] SGNodeTemps_F;       // [5] array
    public float[] SGNodeQ_BTUhr;       // [5] array
    public float SGTotalQ_MW;
    public bool SGBoilingActive;
    public float SGHottestNodeTemp_F;
    public bool[] SGNodeBoiling;        // [5] array
    
    // Future extensibility (v5.4.0+)
    public float SteamMass_lb;          // Placeholder
    public float LiquidMass_lb;         // Placeholder
    public float SteamQuality;          // Placeholder
    public float SteamSpaceVolumePct;   // Placeholder
    public float LatentSensibleSplit;   // Placeholder
}
```

**Design requirements:**
- Sim writes telemetry at **10 Hz** (every 0.1s) or at fixed physics timestep
- UI reads from telemetry only; UI **never** queries `HeatupSimEngine` directly
- Telemetry fields support future SG "full thermal" enhancements without breaking API

---

### 4.C — Reusable UI Components

Create minimal, allocation-free UI building blocks:

| Component | Description | Location |
|-----------|-------------|----------|
| **ArcGauge.cs** | Animated arc gauge using Image.fillAmount | `Scripts/UI/Critical/Components/` |
| **LevelGauge.cs** | Vertical bar with configurable fill, color zones, smoothing | `Scripts/UI/Critical/Components/` |
| **StripChart.cs** | Ring buffer + LineRenderer, dual Y-axis support | `Scripts/UI/Critical/Components/` |
| **ThermalStack.cs** | Vertical node bands + overlay line + shimmer effect | `Scripts/UI/Critical/Components/` |
| **WarningPulse.cs** | Reusable pulsing border/glow utility | `Scripts/UI/Shared/` |

All components must:
- Use `StringBuilder` for any text updates (no per-frame allocations)
- Cache all TMP_Text references
- Implement exponential smoothing for animated values
- Support inspector-configurable thresholds

---

## 5. Architecture Requirements

### 5.1 Telemetry Update Rate

- **Physics → Telemetry:** 10 Hz (or every physics timestep if faster)
- **Telemetry → UI:** Per-frame with interpolation/smoothing
- **Smoothing algorithm:** Critically damped spring or exponential smoothing (configurable τ)

### 5.2 Performance Constraints

| Constraint | Target |
|------------|--------|
| Per-frame allocations | 0 (no LINQ, no string formatting without StringBuilder) |
| Update() overhead | < 1 ms |
| Strip chart memory | Preallocated ring buffers (configurable length, default 600 samples = 60s @ 10 Hz) |
| Frame rate impact | No drop below 60 FPS at 10× simulation speed |

### 5.3 Extensibility

The telemetry bus must include placeholder fields for future SG "full thermal" data:
- Steam mass, liquid mass, quality
- Steam-space volume percentage
- Latent/sensible energy split

These fields remain unused (0 or NaN) until the physics model provides them (v6.0.0+).

---

## 6. Files & Locations

### 6.1 Existing Files to Modify

| File | Path | Changes |
|------|------|---------|
| `HeatupValidationVisual.TabCritical.cs` | `Assets/Scripts/Validation/` | **Replace IMGUI implementation** with call to new uGUI builder |
| `HeatupSimEngine.cs` | `Assets/Scripts/Validation/` | Add telemetry publishing call (10 Hz) |
| `ScreenDataBridge.cs` | `Assets/Scripts/UI/` | Add SG node array getters for telemetry |

### 6.2 New Files to Create

| File | Path | Purpose |
|------|------|---------|
| `PlantTelemetry.cs` | `Assets/Scripts/Telemetry/` | Centralized telemetry bus (singleton MonoBehaviour) |
| `CriticalTabBuilder.cs` | `Assets/Scripts/UI/Critical/` | uGUI Critical tab construction and update logic |
| `ArcGauge.cs` | `Assets/Scripts/UI/Critical/Components/` | Animated arc gauge component |
| `LevelGauge.cs` | `Assets/Scripts/UI/Critical/Components/` | Vertical bar gauge component |
| `StripChart.cs` | `Assets/Scripts/UI/Critical/Components/` | Rolling line chart with ring buffer |
| `ThermalStack.cs` | `Assets/Scripts/UI/Critical/Components/` | SG node visualization component |
| `WarningPulse.cs` | `Assets/Scripts/UI/Shared/` | Reusable warning pulse utility |
| `CriticalTabStyles.cs` | `Assets/Scripts/UI/Critical/` | Color constants, fonts, sizing (optional, can inline) |

### 6.3 Directory Structure (Create if Missing)

```
Assets/
└── Scripts/
    ├── Telemetry/           ← NEW
    │   └── PlantTelemetry.cs
    └── UI/
        ├── Critical/        ← NEW
        │   ├── CriticalTabBuilder.cs
        │   ├── CriticalTabStyles.cs
        │   └── Components/  ← NEW
        │       ├── ArcGauge.cs
        │       ├── LevelGauge.cs
        │       ├── StripChart.cs
        │       └── ThermalStack.cs
        └── Shared/          ← MAY EXIST
            └── WarningPulse.cs
```

---

## 7. Staged Implementation Plan

### Stage 1 — Telemetry Bus Scaffolding

**Summary:** Create the `PlantTelemetry` singleton and wire it to `HeatupSimEngine` for 10 Hz publishing.

**Files touched:**
- `Assets/Scripts/Telemetry/PlantTelemetry.cs` (NEW)
- `Assets/Scripts/Validation/HeatupSimEngine.cs` (MODIFY — add telemetry publish call)

**Implementation:**

1. Create directory `Assets/Scripts/Telemetry/` if not exists
2. Create `PlantTelemetry.cs`:
   - MonoBehaviour singleton pattern (consistent with `ScreenDataBridge`)
   - Public fields for all telemetry data (see Section 4.B)
   - `Initialize()` method to allocate arrays
   - No Update() — data is pushed from physics
3. In `HeatupSimEngine.cs`:
   - Add `[SerializeField] PlantTelemetry telemetry;` with auto-discovery fallback
   - Add `private float _telemetryTimer = 0f;`
   - In `FixedUpdate()` (or wherever physics step runs), call `UpdateTelemetry()` every 0.1s
   - `UpdateTelemetry()` copies all relevant state into telemetry fields

**Acceptance criteria:**
- [ ] `PlantTelemetry.Instance` returns valid singleton
- [ ] Telemetry fields update at 10 Hz during simulation
- [ ] SGNodeTemps_F array has 5 elements matching `sgState.NodeTemperatures[]`
- [ ] No new per-frame allocations (verify with Profiler)

**Validation steps:**
1. Enter Play mode with heatup scene
2. Inspect `PlantTelemetry` component in Hierarchy or via `PlantTelemetry.Instance`
3. Confirm values update every ~0.1s
4. Confirm `SGNodeTemps_F` array matches SGMultiNodeState

---

### Stage 2 — Critical Tab Layout Skeleton (uGUI)

**Summary:** Build the new uGUI hierarchy with panels, anchors, and scaling. No data binding yet.

**Files touched:**
- `Assets/Scripts/UI/Critical/CriticalTabBuilder.cs` (NEW)
- `Assets/Scripts/UI/Critical/CriticalTabStyles.cs` (NEW)
- `Assets/Scripts/Validation/HeatupValidationVisual.TabCritical.cs` (MODIFY — delegate to builder)

**Implementation:**

1. Create `CriticalTabStyles.cs` with static color constants:
   - `ColorNormalGreen`, `ColorWarningAmber`, `ColorAlarmRed`
   - `ColorTrace1..6` for chart lines
   - Font sizes, panel padding, gauge dimensions
2. Create `CriticalTabBuilder.cs`:
   - `public void BuildLayout(RectTransform parent)` — creates uGUI hierarchy
   - Creates 4 zone panels with LayoutGroups or explicit anchors
   - Placeholder Image/Text elements for each zone
   - Target resolutions: 1920×1080 (1080p) and 2560×1440 (1440p) — test both
3. Modify `HeatupValidationVisual.TabCritical.cs`:
   - In `DrawCriticalTab()`, check if uGUI is built; if not, build it
   - Disable old IMGUI rendering when uGUI is active
   - Call `CriticalTabBuilder.UpdateVisuals()` each frame (stub for now)

**Acceptance criteria:**
- [ ] New layout fills the Critical tab area without gaps
- [ ] All 4 zones are visible with labeled placeholders
- [ ] Layout scales correctly at 1080p and 1440p
- [ ] Old IMGUI content is disabled when uGUI is active

**Validation steps:**
1. Run simulation, switch to Critical tab
2. Verify 4 zones are visible with placeholder content
3. Resize window or test at 1440p — confirm no clipping or overlap

---

### Stage 3 — Thermal Stack Component

**Summary:** Implement the SG node temperature visualization with T_sat overlay.

**Files touched:**
- `Assets/Scripts/UI/Critical/Components/ThermalStack.cs` (NEW)
- `Assets/Scripts/UI/Critical/CriticalTabBuilder.cs` (MODIFY — wire ThermalStack)

**Implementation:**

1. Create `ThermalStack.cs`:
   - `public float[] NodeTemperatures` — set from telemetry
   - `public float SaturationTemp` — T_sat line position
   - `public float TRcs` — for margin calculation
   - Creates N horizontal bands (VerticalLayoutGroup or manual positioning)
   - Each band is an Image with color based on temperature gradient
   - T_sat overlay: thin horizontal line (Image, height ~2px) positioned by temperature
   - Margin display: TMP_Text showing (T_hot − T_sat) with color coding
   - Approach warning: when margin < 20°F, enable pulsing border
2. Integrate into Zone 3 of `CriticalTabBuilder`
3. In `CriticalTabBuilder.UpdateVisuals()`:
   - Read `PlantTelemetry.Instance.SGNodeTemps_F[]`
   - Read `PlantTelemetry.Instance.SGTsat_F`
   - Call `ThermalStack.UpdateDisplay()`

**Acceptance criteria:**
- [ ] 5 node bands are visible with distinct colors
- [ ] Top band color matches highest temperature
- [ ] T_sat line position corresponds to telemetry value
- [ ] Margin text updates in real-time
- [ ] Approach warning pulses when margin < 20°F

**Validation steps:**
1. Run heatup simulation into boiling regime
2. Observe node bands changing color as nodes heat
3. Verify T_sat line moves upward as P_sec rises
4. Verify margin warning triggers when nodes approach saturation

---

### Stage 4 — Strip Charts

**Summary:** Implement rolling strip charts for temporal data visualization.

**Files touched:**
- `Assets/Scripts/UI/Critical/Components/StripChart.cs` (NEW)
- `Assets/Scripts/UI/Critical/CriticalTabBuilder.cs` (MODIFY — wire charts)

**Implementation:**

1. Create `StripChart.cs`:
   - Ring buffer array (`float[] _buffer`, size configurable, default 600)
   - `public void PushValue(float value)` — adds sample, advances head
   - `public void PushDualValue(float v1, float v2)` — for two-line charts
   - Rendering via UI.Extensions.UILineRenderer or manual RawImage texture
   - Dual Y-axis support with independent scaling
   - Time axis label showing window duration (e.g., "60s")
2. Create 5 chart instances in Zone 4:
   - P_sec chart (single line, Y: 0–1200 psig)
   - T_sat chart (single line, Y: 200–600°F)
   - T_hot chart (single line, Y: 200–600°F)
   - TotalQ vs ΣnodeQ chart (two lines, Y: 0–25 MW)
   - Residual chart (single line, Y: -5 to +5 MW, centered at 0) — placeholder "N/A"
3. In `CriticalTabBuilder.UpdateVisuals()`:
   - Push new samples to charts at telemetry rate (not per-frame)
   - Use timestamp check to avoid duplicate samples

**Acceptance criteria:**
- [ ] P_sec chart shows rolling 60s pressure history
- [ ] T_sat and T_hot charts track correctly
- [ ] TotalQ vs ΣnodeQ chart shows two distinct lines
- [ ] No visible GC spikes when charts are updating
- [ ] Charts remain stable at 10× sim speed

**Validation steps:**
1. Run heatup from cold, observe P_sec and T_sat charts
2. Verify historical trend is visible (not just current value)
3. Profiler: confirm no allocations from chart updates

---

### Stage 5 — Arc Gauges + Level Column

**Summary:** Implement animated arc gauges and PZR level visualization.

**Files touched:**
- `Assets/Scripts/UI/Critical/Components/ArcGauge.cs` (NEW)
- `Assets/Scripts/UI/Critical/Components/LevelGauge.cs` (NEW)
- `Assets/Scripts/UI/Critical/CriticalTabBuilder.cs` (MODIFY — wire gauges)

**Implementation:**

1. Create `ArcGauge.cs`:
   - Uses Unity UI Image with `fillMethod = Radial360` or custom arc sprite
   - `public float Value`, `public float MinValue`, `public float MaxValue`
   - Smoothed animation via `Mathf.Lerp` or critically damped spring
   - Color zones: Green (normal), Amber (warning), Red (alarm)
   - Configurable threshold values
2. Create `LevelGauge.cs`:
   - Vertical bar using Image fill (Bottom-to-Top)
   - `public float Level_pct` (0–100)
   - Color overlay for solid/bubble/two-phase state
   - Smoothed animation
3. Wire into Zones 1 and 2:
   - Zone 1: Arc gauges for Pressure, T_avg
   - Zone 2: Level gauge for PZR, heater bar, spray bar
4. In `CriticalTabBuilder.UpdateVisuals()`:
   - Read telemetry, update gauge values
   - Apply smoothing factor (inspector-configurable)

**Acceptance criteria:**
- [ ] Pressure gauge needle moves smoothly
- [ ] T_avg gauge shows correct value with color zones
- [ ] PZR level bar tracks telemetry
- [ ] Heater bar shows proportional fill when heaters active
- [ ] Spray bar shows proportional fill when spray active

**Validation steps:**
1. Start heatup, observe gauge animations
2. Trigger spray (high pressure scenario) — verify spray bar responds
3. Confirm smoothing prevents jerky needle motion

---

### Stage 6 — Warning/Alert Polish

**Summary:** Implement consistent warning pulse system and optional boiling shimmer.

**Files touched:**
- `Assets/Scripts/UI/Shared/WarningPulse.cs` (NEW)
- `Assets/Scripts/UI/Critical/Components/ThermalStack.cs` (MODIFY — add shimmer)
- `Assets/Scripts/UI/Critical/CriticalTabBuilder.cs` (MODIFY — wire warnings)

**Implementation:**

1. Create `WarningPulse.cs`:
   - Attachable to any UI element
   - `public bool IsActive` — enables/disables pulse
   - Pulses alpha or outline color at configurable frequency (default 2 Hz)
   - No material changes — uses UI Image color modulation only
2. Add shimmer effect to ThermalStack:
   - When `SGNodeBoiling[i]` is true, apply subtle shimmer to that band
   - Implementation: alpha oscillation or small scale pulse
   - Must be visually subtle — not distracting
3. Wire warnings throughout:
   - Thermal Stack margin < 20°F → pulse
   - TotalQ vs ΣnodeQ discrepancy > 0.5 MW → chart border pulse
   - PZR level deviation > 10% from setpoint → level bar pulse

**Acceptance criteria:**
- [ ] Warning pulse is visible but not distracting
- [ ] Pulse activates/deactivates correctly based on conditions
- [ ] Boiling shimmer only appears on actively boiling nodes
- [ ] All animations remain smooth at 10× speed

**Validation steps:**
1. Run into boiling regime — verify shimmer on boiling nodes
2. Force thermal margin below 20°F — verify pulse activates
3. Restore normal conditions — verify pulse deactivates

---

### Stage 7 — QA + Tuning Hooks

**Summary:** Add inspector configurables, debug overlay, and final polish.

**Files touched:**
- `Assets/Scripts/UI/Critical/CriticalTabBuilder.cs` (MODIFY)
- `Assets/Scripts/UI/Critical/Components/*.cs` (MODIFY — add [SerializeField] for tuning)
- `Assets/Scripts/Telemetry/PlantTelemetry.cs` (MODIFY — add debug mode)

**Implementation:**

1. Add inspector tunables to all components:
   - Smoothing time constants (τ for gauges, charts)
   - Warning thresholds (margin, Q discrepancy)
   - Chart window duration (default 60s)
   - Telemetry update rate (default 10 Hz)
   - Pulse frequency
2. Add debug overlay toggle:
   - Hotkey (e.g., F10) toggles raw numeric overlay
   - Overlay shows: exact telemetry values, update timestamps, buffer fill levels
   - Overlay uses small monospace font, positioned in corner
3. Final visual polish:
   - Consistent font sizes across all zones
   - Alignment pass — verify nothing is misaligned at 1080p and 1440p
   - Color consistency with existing Critical tab palette

**Acceptance criteria:**
- [ ] All components have inspector-configurable thresholds
- [ ] Debug overlay toggles on/off with hotkey
- [ ] Debug overlay shows raw values matching physics state
- [ ] Visual appearance is consistent and professional
- [ ] No visible gaps or misalignments at target resolutions

**Validation steps:**
1. Adjust smoothing in inspector — verify immediate visual change
2. Toggle debug overlay — verify raw values display
3. Compare debug values to HeatupSimEngine inspector values

---

## 8. Validation Criteria (Explicit Pass/Fail)

All criteria must pass before v5.3.2 is considered complete.

| ID | Criterion | Pass Definition |
|----|-----------|-----------------|
| **V1** | SG stratification visible | User can identify hottest node visually within 2 seconds |
| **V2** | T_sat line tracks pressure | Line position changes within 1 frame when P_sec changes |
| **V3** | Margin warning functional | Pulse activates when (T_hot - T_sat) < 20°F |
| **V4** | Strip charts operational | All 5 charts show rolling history without artifacts |
| **V5** | TotalQ vs ΣnodeQ comparison | Two lines clearly distinguishable; discrepancy > 0.5 MW triggers warning |
| **V6** | No GC spikes | Profiler shows 0 allocations per frame from Critical tab |
| **V7** | Frame rate maintained | ≥60 FPS on dev machine at 10× sim speed with Critical tab active |
| **V8** | Telemetry decoupling | UI code contains zero direct references to `HeatupSimEngine` fields |
| **V9** | Debug overlay functional | F10 toggles raw value overlay with all telemetry fields |
| **V10** | Resolution scaling | Layout correct at both 1920×1080 and 2560×1440 |

---

## 9. Not Addressed / Future Work

| Item | Disposition | Target Version |
|------|-------------|----------------|
| Full thermal SG secondary model (pressure as state, steam-space mass, energy accounting) | **Out of scope** — UI designed to support it | v6.0.0 |
| Turbine/condenser visualization | **Out of scope** | v5.8.0 |
| Feedwater system display | **Out of scope** | v5.7.0+ |
| Per-loop instrumentation | **Out of scope** | v6.0.0 |
| Prefab-based architecture | Deferred for faster iteration | v5.5.0+ |
| Energy/mass residual chart | Placeholder "N/A" — requires conservation tracking | v5.4.0+ |

---

## 10. Rollback Notes

If implementation fails or causes regressions:

1. **Stage-level rollback:** Each stage is independently revertible
2. **Telemetry bus:** Can be disabled without breaking physics (HeatupSimEngine continues to function)
3. **IMGUI fallback:** Old `DrawCriticalTab()` IMGUI code is preserved (disabled, not deleted)
4. **Git reversion:** Each stage should be a separate commit for clean rollback

---

## 11. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| uGUI performance worse than IMGUI | Low | Medium | Profile early (Stage 2); fall back to IMGUI if needed |
| Strip chart memory pressure | Low | Low | Use fixed-size ring buffers; no dynamic allocation |
| Telemetry synchronization issues | Medium | Medium | Use monotonic timestamps; handle stale data gracefully |
| Visual clutter | Medium | Low | Iterative design; get feedback at Stage 3 before adding more |
| Late integration issues | Low | High | Wire telemetry in Stage 1 before any UI work |

---

## 12. Output Requirements

After completing each stage, output:

1. **Files modified** — Full paths
2. **Key code snippets** — Only edited sections (not full files)
3. **Validation steps performed** — What was tested and result
4. **Statement:** "Stage N complete. Awaiting approval to proceed."

---

## 13. Coding Rules

1. **Implement ONE STAGE per reply**
2. After completing each stage: show changed files, key code snippets, validation results
3. **Do NOT proceed to next stage** until current stage is explicitly approved
4. **Do NOT create a changelog** until all 7 stages are complete and validated
5. Keep behavior changes strictly contained to stage objectives
6. **No physics changes** — this is UI/observability only
7. Follow existing code conventions from `ScreenDataBridge.cs` and `HeatupValidationVisual.cs`

---

## 14. Success Criteria Summary

v5.3.2 is successful when:

1. ✅ All 7 implementation stages complete
2. ✅ All 10 validation criteria (V1–V10) pass
3. ✅ No regressions in existing functionality
4. ✅ Performance targets met (V6, V7)
5. ✅ Changelog created with evidence of all validations passing

---

*Prepared: 2026-02-12*  
*Status: PLAN COMPLETE — AWAITING v5.3.1 VALIDATION GATE*
