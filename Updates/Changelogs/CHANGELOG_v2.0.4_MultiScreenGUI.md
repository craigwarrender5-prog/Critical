# Changelog — Multi-Screen GUI v2.0.4

## [2.0.4] — 2026-02-10

### Added — Stage 4: Pressurizer Screen (Key 3)

#### New File: `PressurizerScreen.cs`
- Pressurizer operator screen inheriting from `OperatorScreen`
- Toggle: Key 3, Index: 3, Name: "PRESSURIZER"
- **Left Panel (8 Pressure Gauges):**
  - PZR Pressure (psia) — alarm color coding at 2300/2185/2335 psia
  - Pressure Setpoint (fixed 2235 psia)
  - Pressure Error (actual - setpoint, warning at ±50 psi)
  - Pressure Rate (psi/hr, warning at ±100 psi/hr)
  - Heater Power (kW)
  - Spray Flow (PLACEHOLDER — no spray model)
  - Backup Heater Status (derived: OFF / STANDBY / ENERGIZED)
  - PORV Status (inferred from pressure ≥ 2335 psia)
- **Right Panel (8 Level/Volume Gauges):**
  - PZR Level (%) — alarm at 70% high / 17% low
  - Level Setpoint (fixed 60%)
  - Level Error (actual - setpoint, warning at ±10%)
  - Surge Flow (gpm, signed: + insurge / - outsurge)
  - Steam Volume (ft³)
  - Water Volume (ft³)
  - PZR Water Temperature (°F)
  - Subcooling Margin (°F) — green >20°F, warning 0-20°F, alarm <0°F
- **Center Panel (2D Vessel Cutaway):**
  - Vessel shell with interior background
  - Water level fill (Image.fillAmount, vertical bottom fill)
  - Water color: blue (cold) → purple (hot) interpolated by temperature
  - Steam dome (grey translucent, visible when steam volume > 0)
  - 4 heater glow bars (intensity proportional to heater power / 1680 kW)
  - Spray indicator (active when pressure > setpoint + 25 psi)
  - Surge line (warm orange for insurge, cool blue for outsurge, grey neutral)
  - 2 PORV indicators (red when pressure ≥ 2335 psia)
  - 3 Safety valve indicators (red when pressure ≥ 2485 psia)
  - Overlay text: pressure, level, temperature, heater power
  - Setpoint reference labels on side panel
- **Bottom Panel:**
  - Heater controls: Proportional (660 kW) and Backup (1020 kW) buttons (visual only)
  - Spray controls: Open/Close buttons (visual only, marked NOT MODELED)
  - Valve status: PORV-A, PORV-B, SV-1, SV-2, SV-3 with indicator lights and text
  - Status: Reactor mode, sim time, time compression, heater PID status, HZP status
  - Alarm container (VerticalLayoutGroup)
- Update throttling: 10 Hz gauges, 2 Hz vessel visualization
- NaN → "---" placeholder handling consistent with all screens

#### Modified File: `MultiScreenBuilder.cs` → v2.0.4
- Added `CreatePressurizerScreen(canvas.transform)` call in `CreateAllOperatorScreens()`
- Added Screen 3 builder region with methods:
  - `CreatePressurizerScreen()` — root panel, CanvasGroup, component attachment
  - `BuildPZRLeftPanel()` — 8 pressure gauges via `CreateOverviewGaugeItem()`
  - `BuildPZRCenterPanel()` — 2D vessel cutaway with all visual elements
  - `BuildPZRRightPanel()` — 8 level/volume gauges via `CreateOverviewGaugeItem()`
  - `BuildPZRBottomPanel()` — heater/spray controls, valve indicators, status, alarms
  - `BuildPZRValveIndicator()` — helper for indicator Image + TMP label pairs
- All SerializedObject wiring for PressurizerScreen fields including arrays
- Updated header: version, screens list

### Westinghouse 4-Loop PWR Constants Used
- Total volume: 1800 ft³
- Operating pressure: 2235 psia / Design: 2500 psia
- Normal level: 60%
- Proportional heaters: 660 kW / Backup: 1020 kW / Total: 1680 kW
- PORV setpoint: 2335 psia (2 PORVs)
- Safety valve setpoint: 2485 psia (3 SVs)
- Tsat at 2235 psia: 653°F
- Pressure alarms: High 2300 / Low 2185 psia
- Level alarms: High 70% / Low 17%

### Known Limitations
- Spray flow: PLACEHOLDER — no spray model in physics engine
- PORV/SV status: Inferred from pressure, not actual valve state model
- Level/pressure setpoints: Fixed nominal values, no dynamic control
- Controls: Visual only — no active control logic (per design)

---

## Previous Versions

### [2.0.3] — 2026-02-10
- Bugfixes: action map enable timing, Start() visibility race condition

### [2.0.2] — 2026-02-10
- Bugfixes: Input system auto-wiring, same-key toggle guard, legacy Input.mousePosition
- Fixed inactive screen registration, control scheme bindings

### [2.0.0] — 2026-02-10
- Initial multi-screen GUI: ScreenManager, OperatorScreen, ScreenDataBridge
- Screen 1 (Reactor Core), Screen 2 (RCS Primary Loop), Screen Tab (Plant Overview)
