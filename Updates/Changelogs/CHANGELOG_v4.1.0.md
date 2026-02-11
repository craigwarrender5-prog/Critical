# CHANGELOG v4.1.0 — Mosaic Board Visual Upgrade

## Version: 4.1.0
## Date: 2026-02-11
## Scope: Screen 1 (Reactor Operator Screen) — Full TMP Font & Visual Upgrade

---

## Summary

Complete visual overhaul of all text rendering, gauge displays, core map cells,
buttons, readouts, and alarm indicators on the Reactor Operator Screen (Screen 1).
Replaced all legacy `UnityEngine.UI.Text` components with `TextMeshProUGUI` using
purpose-built instrument font materials with phosphor glow effects. Added procedural
sprite backgrounds for gauges, readouts, buttons, and core map cells. Added fill bar
indicators and glow images to all gauges.

---

## Stage 1: Font & Material Setup (Editor Scripts)

### Files Created

- **`Assets/Scripts/UI/Editor/InstrumentSpriteGenerator.cs`** (NEW)
  - Editor menu: Critical > Generate Instrument Sprites
  - Generates 6 procedural sprite PNGs to `Assets/Resources/Sprites/`:
    - `gauge_bg.png` (256×64) — Dark recessed gauge background with inner shadow
    - `cell_bg.png` (32×32) — Beveled cell with 1px border, highlight/shadow edges, 9-slice
    - `button_bg.png` (64×32) — Rounded rect with top highlight, bottom shadow, 9-slice
    - `readout_bg.png` (128×32) — Deep recessed readout with inner shadow, 9-slice
    - `fill_bar.png` (256×16) — Horizontal fill with vertical gradient and rounded left cap
    - `glow_soft.png` (64×64) — Soft radial gaussian glow for behind value text

- **`Assets/Scripts/UI/Editor/InstrumentMaterialSetup.cs`** (NEW)
  - Editor menu: Critical > Setup Instrument Materials
  - Creates 8 TMP material presets to `Assets/Resources/Fonts & Materials/`:
    - `Instrument_Green_Glow.mat` — Electronic Highway Sign SDF, #00FF88, glow 0.25
    - `Instrument_Amber_Glow.mat` — Electronic Highway Sign SDF, #FFB830, glow 0.25
    - `Instrument_Red_Glow.mat` — Electronic Highway Sign SDF, #FF3344, glow 0.35
    - `Instrument_Cyan.mat` — Electronic Highway Sign SDF, #00CCFF, glow 0.2
    - `Instrument_White.mat` — Electronic Highway Sign SDF, #C8D0D8, glow 0.1
    - `Alarm_Pulse_Red.mat` — Electronic Highway Sign SDF, #FF3344, glow 0.5 (alarm flash)
    - `Label_Standard.mat` — LiberationSans SDF, #8090A0, subtle drop shadow
    - `Label_Section.mat` — LiberationSans SDF, #C8D0D8, dark outline

- **`Assets/Scripts/UI/Editor/InstrumentSpriteImporter.cs`** (NEW)
  - Asset postprocessor: auto-configures sprite import settings and 9-slice borders
    when PNGs are created in `Assets/Resources/Sprites/`

### Directories Created

- `Assets/Scripts/UI/Editor/`
- `Assets/Resources/Sprites/`

---

## Stage 2: MosaicGauge Visual Upgrade

### Files Modified — GOLD Standard

- **`Assets/Scripts/UI/MosaicGauge.cs`** (GOLD)
  - Added `using TMPro;`
  - Changed field types: `Text ValueText` → `TextMeshProUGUI ValueText`,
    `Text LabelText` → `TextMeshProUGUI LabelText`,
    `Text UnitsText` → `TextMeshProUGUI UnitsText`
  - Added fields: `Image FillBarIndicator`, `Image GlowImage`
  - Added private material cache: `_matGreen`, `_matAmber`, `_matRed`, `_matCyan`
  - Added `LoadInstrumentMaterials()` — loads TMP material presets from Resources at Awake
  - Added `UpdateFillBarIndicator()` — scales fill bar anchor to normalized value
  - Added `UpdateGlowEffect()` — tints glow image to alarm color with variable alpha
  - Added `UpdateValueMaterial()` — swaps TMP material preset on alarm state change
    (only when state transitions, avoids per-frame material assignment)
  - Updated `UpdateDigitalDisplay()` — skips `.color` when TMP materials are loaded

- **`Assets/Scripts/UI/ReactorOperatorScreen.cs`** (GOLD)
  - Added `using TMPro;`
  - Changed 4 status display fields: `Text ScreenTitleText`, `Text SimTimeText`,
    `Text TimeCompressionText`, `Text ReactorModeText` → `TextMeshProUGUI`

### Files Modified — Non-GOLD

- **`Assets/Scripts/UI/OperatorScreenBuilder.cs`**
  - Added `using TMPro;`
  - `CreateGauge()` — complete rewrite:
    - Background: `gauge_bg` sprite (9-sliced) instead of flat color
    - Fill bar: `fill_bar` sprite child, anchored left, width controlled by normalized value
    - Glow: `glow_soft` sprite child, centered behind value text
    - Value text: `TextMeshProUGUI` with Electronic Highway Sign SDF font
    - Label text: `TextMeshProUGUI` with LiberationSans SDF + `Label_Standard` material
  - `CreateDigitalReadout()` — return type `Text` → `TextMeshProUGUI`:
    - Background: `readout_bg` sprite (9-sliced)
    - Text: Electronic Highway Sign SDF + `Instrument_Green_Glow` material
  - `CreateSectionLabel()` — TMP with LiberationSans SDF + `Label_Section` material
  - `CreateButton()` — button_bg sprite (9-sliced), TMP label with LiberationSans SDF
  - Rod control section: all `Text` → `TextMeshProUGUI` with appropriate fonts/materials
    - Selected bank text: Electronic Highway Sign + Instrument_Cyan material
    - Steps label: LiberationSans + Label_Standard material
    - Motion status: Electronic Highway Sign
    - Bank selector button labels: `GetComponentInChildren<TextMeshProUGUI>()`
    - Command button labels: `GetComponentInChildren<TextMeshProUGUI>()`

- **`Assets/Scripts/UI/RodControlPanel.cs`**
  - Added `using TMPro;`
  - Changed 3 display fields: `Text SelectedBankText`, `Text StepPositionText`,
    `Text MotionStatusText` → `TextMeshProUGUI`

---

## Stage 3: Core Map Cell Visual Upgrade

### Files Modified — GOLD Standard

- **`Assets/Scripts/UI/CoreMosaicMap.cs`** (GOLD)
  - Added `using TMPro;`
  - Changed `Text TooltipText` → `TextMeshProUGUI TooltipText`
  - Changed `Text[] _cellLabels` → `TextMeshProUGUI[] _cellLabels`
  - `CreateCells()` — array type updated
  - `CreateCell()`:
    - Cell background: `cell_bg` sprite (9-sliced) for beveled appearance
    - RCCA bank labels: `TextMeshProUGUI` with Electronic Highway Sign SDF, fontSize 7

- **`Assets/Scripts/UI/AssemblyDetailPanel.cs`** (GOLD)
  - Added `using TMPro;`
  - Changed all 15 `public Text` fields → `TextMeshProUGUI`:
    - Header: HeaderText, CoordinateText, TypeText
    - Power: PowerLabel, PowerValue
    - Fuel Temp: FuelTempLabel, FuelTempValue
    - Coolant Temp: CoolantTempLabel, CoolantTempValue
    - RCCA: RCCABankLabel, RCCABankValue, RCCAPositionLabel, RCCAPositionValue,
      RCCAInsertionLabel, RCCAInsertionValue
  - `CreateText()` helper — returns `TextMeshProUGUI`:
    - Auto-selects font: Electronic Highway Sign SDF for Bold values,
      LiberationSans SDF for Normal/Italic labels
    - Converts legacy `FontStyle` → TMP `FontStyles` (Bold, Italic, BoldAndItalic, Normal)
    - Converts legacy `TextAnchor` → TMP `TextAlignmentOptions` (9 alignment options)
  - Close button "✕" text: converted to TMP with LiberationSans SDF

### Files Modified — Non-GOLD (Builder)

- **`Assets/Scripts/UI/OperatorScreenBuilder.cs`**
  - `CreateTooltip()` — tooltip text converted to `TextMeshProUGUI` with LiberationSans SDF

---

## Stage 4: Button & Readout Visual Upgrade

Completed within Stage 2. All button and readout upgrades were implemented as part
of the `CreateButton()`, `CreateDigitalReadout()`, and rod control section rewrites.

---

## Stage 5: Alarm Visual Enhancement

### Files Modified — GOLD Standard

- **`Assets/Scripts/UI/MosaicGauge.cs`** (GOLD)
  - `OnAlarmFlash()` — added TMP glow power pulsing: when alarm state >= Alarm,
    animates `_GlowPower` shader property between 0.15 (dim) and 0.6 (bright)
    on the value text font material. Creates double-layer alarm effect combined
    with existing glow Image alpha pulsing.

### Files Modified — Non-GOLD

- **`Assets/Scripts/UI/MosaicAlarmPanel.cs`**
  - Added `using TMPro;`
  - Changed `Text CurrentAlarmText` → `TextMeshProUGUI CurrentAlarmText`
  - Changed `Text AlarmCountText` → `TextMeshProUGUI AlarmCountText`
  - `AlarmEntryUI.text` → `TextMeshProUGUI`
  - `CreateAlarmEntry()` — uses `TextMeshProUGUI` with Electronic Highway Sign SDF
  - `GetComponent<Text>()` → `GetComponent<TextMeshProUGUI>()` in entry creation

---

## Stage 6: Polish & Final Verification

### Files Modified — Non-GOLD (Builder)

- **`Assets/Scripts/UI/OperatorScreenBuilder.cs`**
  - Fixed bank selector button labels: `GetComponentInChildren<Text>()` →
    `GetComponentInChildren<TextMeshProUGUI>()`
  - Fixed command button labels (WITHDRAW, STOP, INSERT): same fix
  - Fixed `FontStyle.Bold` → `FontStyles.Bold` on STOP button label

---

## GOLD Standard Files Modified (Complete List)

| File | Changes |
|------|---------|
| `MosaicGauge.cs` | Text→TMP (3 fields), +FillBarIndicator, +GlowImage, +material cache, +3 visual methods, +alarm glow pulse |
| `ReactorOperatorScreen.cs` | Text→TMP (4 fields) |
| `CoreMosaicMap.cs` | Text→TMP (1 public + 1 array), +cell_bg sprite, +TMP bank labels |
| `AssemblyDetailPanel.cs` | Text→TMP (15 fields), CreateText() rewrite, close button TMP |
| `MosaicBoard.cs` | NOT MODIFIED — no Text fields |

**Nature of GOLD changes**: Type-only field changes (`Text` → `TextMeshProUGUI`) and additive
visual fields. No physics logic, data flow, or behavioral modifications. `TextMeshProUGUI`
is a drop-in replacement for `Text` in Unity UI — implements the same `ILayoutElement`
interfaces and works identically within Canvas rendering.

---

## Deferred to Future Versions

- **v4.2.0+**: Screens 2-8 TMP visual upgrade (same treatment, one screen at a time)
- **v4.2.0+**: Analog dial needles on gauges (requires gauge face sprite design)
- **v4.2.0+**: CRT scanline overlay shader (pure cosmetic)
- **v4.2.0+**: Per-assembly fuel temperature gradient in core cells

---

## Activation Steps (Required in Unity Editor)

1. Open Unity project
2. Menu: **Critical > Generate Instrument Sprites** (creates 6 PNGs)
3. Menu: **Critical > Setup Instrument Materials** (creates 8 .mat files)
4. Copy `panel_base_color.png` to `Assets/Resources/ReactorOperatorPanel/` (if not already done from v4.0.0)
5. Delete existing Reactor Operator Screen hierarchy in scene
6. Menu: **Critical > Create Operator Screen**
7. Press Play to verify
