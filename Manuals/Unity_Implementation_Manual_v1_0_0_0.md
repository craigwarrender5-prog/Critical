# Unity Implementation Manual v1.0.0.0

CRITICAL: Master the Atom

Unity Implementation Manual

Reactor Operator GUI

A step-by-step guide for building the Reactor Operator Screen

in Unity using uGUI, C# scripts, and the CRITICAL physics engine

Version 1.0.0.0 — February 2026

Assumes: Unity 2022.3+ LTS, no prior Unity UI experience

PART A

Unity Fundamentals You Need

Chapter 1: How Unity UI Works

1.1 The Canvas

Everything visible in Unity's UI system lives inside a Canvas. Think of the Canvas as an invisible rectangle that represents your game window. Every button, gauge, image, and text element must be a child (or grandchild) of a Canvas to appear on screen.

Your project already has Canvas creation in MosaicBoardBuilder.cs. The Reactor Operator Screen will use its own Canvas (or the same one).

Canvas Render Modes:

Screen Space - Overlay: The UI draws on top of everything. This is what you want for the reactor control screen.

Screen Space - Camera: UI rendered by a specific camera. Not needed here.

World Space: UI exists in the 3D world. Not needed here.

1.2 RectTransform: Anchors and Offsets

Every UI element has a RectTransform that controls position and size. The two most important concepts are anchors and offsets.

Anchors define where an element sits relative to its parent, using values from 0 to 1:

What You Want

Anchor Values

Meaning

Fill entire parent

min(0,0) max(1,1)

Element stretches to match parent exactly.

Left 15% of parent

min(0,0) max(0.15,1)

Fills left 15%. Stretches vertically with parent.

Bottom 25% of parent

min(0,0) max(1,0.25)

Fills bottom quarter. Stretches horizontally.

Center, fixed size

min(0.5,0.5) max(0.5,0.5)

Stays centered. Does not resize with parent.

IMPORTANT: Y=0 is the BOTTOM, Y=1 is the TOP. This is opposite to many graphics systems where Y goes downward.

Offsets add or subtract pixels after anchors define the region:

offsetMin = (left margin, bottom margin) in pixels. offsetMax = (right margin, top margin) — use negative values to shrink inward. Example: offsetMin=(4,4), offsetMax=(-4,-4) adds a 4px margin on all sides.

1.3 How to Set Anchors in the Inspector

Method 1: Anchor Presets (quick, limited):

Click the small square icon at the top-left of the RectTransform in the Inspector

Hold Alt+Shift and click a preset to set anchors, position, and pivot simultaneously

Good for common layouts (fill, center, corners) but cannot set values like 0.15

Method 2: Type Values Directly (precise):

Expand the RectTransform section to see Anchor Min X/Y and Anchor Max X/Y fields

Type your values: e.g., Min X=0.15, Min Y=0.26, Max X=0.65, Max Y=1.0

TIP: If you see Left/Right/Top/Bottom instead of Anchors, your element is in stretch mode. Those fields ARE the offsets. Click the anchor preset icon to view the actual anchor values.

Chapter 2: GameObjects, Components, and the Inspector

2.1 GameObjects and the Hierarchy

Everything in a Unity scene is a GameObject — an empty container with a name and a position. You add Components to give it behavior. GameObjects form a tree called the Hierarchy. Children inherit their parent's position and visibility.

Your Reactor Operator Screen hierarchy (simplified):

ReactorOperatorCanvas          ← Canvas (Screen Space Overlay)

└─ ReactorOperatorScreen      ← Master controller + dark background

├─ LeftGaugePanel          ← 9 gauges stacked vertically

├─ CoreMapPanel            ← 193-cell interactive core map

├─ RightGaugePanel         ← 8 gauges stacked vertically

├─ DetailPanel             ← Assembly detail (hidden until clicked)

└─ BottomPanel             ← Controls + alarms

2.2 Key Components You Will Use

Component

What It Does

Image

Draws a colored rectangle or sprite. Every visible panel/background uses this.

Text (Legacy)

Renders text on screen. Used for gauge readouts, labels, alarm names.

Button

Makes a GameObject clickable. Fires an OnClick event connected to your code.

Slider

A draggable control. Used for time compression.

CanvasScaler

Controls how UI scales at different resolutions. Set to 1920x1080 reference.

GraphicRaycaster

Enables mouse clicks on UI. Required on Canvas for any interactivity.

HorizontalLayoutGroup

Auto-arranges children left-to-right. Used for button rows and alarm strips.

VerticalLayoutGroup

Auto-arranges children top-to-bottom. Used for gauge columns.

2.3 Wiring References in the Inspector

Components often need references to other objects. For example, MosaicGauge has a "Value Text" field. You connect them by dragging:

Select the gauge GameObject in Hierarchy

In the Inspector, find the MosaicGauge component's "Value Text" slot (empty box)

Drag the child Text GameObject from the Hierarchy into this slot

The gauge now knows which Text to update with numbers

IMPORTANT: This drag-and-drop wiring is exactly what the Builder scripts do in code. When code says gauge.ValueText = valueText, it does the same thing as dragging in the Inspector. The Builder just automates it.

Chapter 3: How Your Existing Code Fits Together

3.1 The Three-Layer Architecture

CRITICAL has a clean separation. Understanding this is essential:

LAYER 1: Physics  (Pure C#, no Unity dependency)

PlantConstants, FuelAssembly, ControlRodBank, ReactorCore...

GOLD STANDARD — never modify these.

LAYER 2: Controller  (Unity MonoBehaviour)

ReactorController.cs

Owns physics objects, runs the simulation loop.

Exposes properties: .NeutronPower, .Tavg, .Boron_ppm...

Accepts commands: .WithdrawRods(), .Trip(), .SetBoron()...

LAYER 3: GUI  (Unity UI)  ← THIS IS WHAT WE BUILD

ReactorOperatorScreen, CoreMosaicMap, MosaicGauge...

READS data from ReactorController properties.

SENDS commands via ReactorController methods.

Never touches physics directly.

The key rule: the GUI never does physics. It reads values and displays them. When the player clicks a button, the GUI calls a ReactorController method which passes the command to physics.

3.2 Data Flow Trace: How Tavg Reaches a Gauge

ReactorCore (physics) calculates Tavg from heat balance equations every timestep

ReactorController.Update() calls _core.Update(dt) each frame

ReactorController exposes: public float Tavg => _core?.Tavg

MosaicBoard.Update() runs at 10 Hz, calls UpdateData() on registered components

MosaicBoard.GetValue(GaugeType.Tavg) reads ReactorController.Tavg

MosaicGauge.UpdateData() calls _board.GetValue(Type) and stores the result

MosaicGauge updates: ValueText.text = "588.5"

Unity renders the Text. Player sees 588.5 on screen.

This entire chain runs automatically once everything is wired. Your job is to create the visual structure (GameObjects, anchors, layout) and wire the references.

3.3 Existing UI Components to Reuse

Script

Role

MosaicBoard.cs

Central hub. Holds ReactorController reference. Provides data to gauges, manages alarm colors, value smoothing. ADD THIS to your screen panel.

MosaicGauge.cs

Displays one parameter. Set Type dropdown, wire ValueText and LabelText references. Auto-registers with MosaicBoard.

MosaicRodDisplay.cs

Draws 8 bank position bars. Auto-reads from ReactorController.

MosaicControlPanel.cs

Buttons for rod control, trip, boron, time. Sends commands to ReactorController.

MosaicAlarmPanel.cs

Alarm tile management. Monitors conditions, controls flashing.

MosaicBoardSetup.cs

Runtime initializer. Auto-creates ReactorController, sets initial state. ADD THIS to your screen panel.

PART B

Building the Reactor Operator Screen

Chapter 4: Project Setup

4.1 Open the Project

Open Unity Hub, open the CRITICAL project

Wait for compilation to finish (progress bar at bottom disappears)

Navigate to Assets/Scenes in the Project window

Right-click > Create > Scene. Name it "ReactorOperatorScreen"

Double-click to open the new scene

4.2 Set Game Window Resolution

Click the Game tab at top of the viewport

Click the resolution dropdown (may say "Free Aspect")

Click + to add custom: Label = "1920x1080", Type = Fixed Resolution, W=1920, H=1080

Select your new resolution

4.3 Set Build Resolution

Edit > Project Settings > Player

Default Screen Width = 1920, Default Screen Height = 1080

Uncheck Default Is Fullscreen for windowed mode

Chapter 5: Creating the Canvas and Master Layout

5.1 Create the Canvas

Right-click in Hierarchy > UI > Canvas

Rename to "ReactorOperatorCanvas"

In Inspector, Canvas component: Render Mode = "Screen Space - Overlay"

Canvas Scaler: UI Scale Mode = "Scale With Screen Size"

Reference Resolution: X=1920, Y=1080

Screen Match Mode: "Match Width Or Height"

Match slider: 0.5

TIP: "Scale With Screen Size" at 1920x1080 means you design at those coordinates and Unity auto-scales to other resolutions.

5.2 Create the Master Screen Panel

Right-click Canvas > UI > Image. Rename to "ReactorOperatorScreen"

Set RectTransform to stretch-fill: hold Alt+Shift, click bottom-right anchor preset

Set Image color: R=26, G=26, B=31 (hex #1A1A1F) — dark background

Add Component: MosaicBoard (search for it)

Add Component: MosaicBoardSetup

5.3 Create the Five Zone Panels

Create five child Images under ReactorOperatorScreen. For each, right-click ReactorOperatorScreen > UI > Image, rename, and set anchors:

Panel Name

AnchorMin

AnchorMax

Purpose

LeftGaugePanel

(0, 0.26)

(0.15, 1)

Left column: 9 nuclear instrument gauges

CoreMapPanel

(0.15, 0.26)

(0.65, 1)

Center: 193-assembly core mosaic map

RightGaugePanel

(0.65, 0.26)

(0.80, 1)

Right column: 8 thermal-hydraulic gauges

DetailPanel

(0.80, 0.26)

(1, 1)

Assembly detail (uncheck Active checkbox to hide initially)

BottomPanel

(0, 0)

(1, 0.26)

Controls, bank bars, trip, time, alarms

Set all panel colors to #1E1E28. Add 4px offset margins: offsetMin=(4,4), offsetMax=(-4,-4).

How to type anchor values:

Select the panel in Hierarchy

In Inspector, click the triangle next to the RectTransform's anchor preset icon to expand raw values

Type: Anchor Min X, Anchor Min Y, Anchor Max X, Anchor Max Y

Type: Left, Bottom (= offsetMin.x, offsetMin.y), Right, Top (use negative for offsetMax)

Chapter 6: Building the Core Mosaic Map

6.1 Concept

The core map displays 193 fuel assemblies as colored square cells in a 15x15 grid with corners removed. Each cell changes color based on reactor conditions and responds to mouse interaction.

The CoreMosaicMap script creates cells at runtime. Here is the process conceptually:

6.2 Setting Up the Map Container

Under CoreMapPanel, create child Image called "MapContainer"

Anchor it to the center area of CoreMapPanel (leave room for buttons above and below):

AnchorMin = (0.05, 0.08), AnchorMax = (0.95, 0.92)

Set Image color to #12121A (darker than panel, makes cells pop)

Add Component: CoreMosaicMap (a new script you will create)

6.3 How CoreMosaicMap Creates 193 Cells

In the CoreMosaicMap.Start() method, the script loops through the 15x15 grid and creates cells:

void Start() {

float containerW = GetComponent<RectTransform>().rect.width;

float cellSize = containerW / 15f - gap;

for (int row = 0; row < 15; row++) {

for (int col = 0; col < 15; col++) {

int gridVal = CoreMapData.CORE_GRID[row, col];

if (gridVal == -1) continue;  // skip empty corners

var cellGO = new GameObject($"Cell_{row}_{col}");

cellGO.transform.SetParent(transform, false);

var rect = cellGO.AddComponent<RectTransform>();

float x = col * (cellSize + gap);

float y = -(row * (cellSize + gap));

rect.anchoredPosition = new Vector2(x, y);

rect.sizeDelta = new Vector2(cellSize, cellSize);

var img = cellGO.AddComponent<Image>();

_cellImages[row, col] = img;  // store for color updates

// Make it interactive

var ac = cellGO.AddComponent<AssemblyCell>();

ac.Row = row; ac.Col = col;

ac.ParentMap = this;

}

}

}

TIP: The cells are created ONCE at startup and reused. Color updates just change img.color on existing objects — never create/destroy cells each frame.

6.4 Making Cells Interactive

Each cell has an AssemblyCell component implementing Unity's pointer interfaces:

using UnityEngine.EventSystems;

public class AssemblyCell : MonoBehaviour,

IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler

{

public int Row, Col;

public CoreMosaicMap ParentMap;

public void OnPointerEnter(PointerEventData d)

=> ParentMap.OnCellHover(this);

public void OnPointerExit(PointerEventData d)

=> ParentMap.OnCellHoverExit(this);

public void OnPointerClick(PointerEventData d)

=> ParentMap.OnCellClick(this);

}

These interfaces work automatically when the Canvas has a GraphicRaycaster and the cell Image has Raycast Target enabled (the default). No extra configuration needed.

6.5 Display Mode Buttons (Above Map)

Under CoreMapPanel, create "DisplayModeButtons" with AnchorMin=(0,0.93), AnchorMax=(1,1)

Add HorizontalLayoutGroup: Spacing=4, Child Force Expand Width=true

Create 4 child Buttons: POWER, FUEL TEMP, COOLANT TEMP, BANK MAP

Each button calls CoreMosaicMap.SetDisplayMode(mode) on click

6.6 Bank Filter Buttons (Below Map)

Under CoreMapPanel, create "BankFilterButtons" with AnchorMin=(0,0), AnchorMax=(1,0.07)

Add HorizontalLayoutGroup: Spacing=3, Child Force Expand Width=true

Create 11 buttons: ALL, SD (all shutdown), CTRL (all control), SA, SB, SC, SD, D, C, B, A

Each bank button toggles: CoreMosaicMap.ToggleBankFilter(bankIndex)

ALL/SD/CTRL are quick-select shortcuts

Chapter 7: Building the Gauge Columns

7.1 Using a VerticalLayoutGroup

Rather than positioning each gauge manually, the gauge columns use Unity's VerticalLayoutGroup to stack children automatically:

Select LeftGaugePanel in Hierarchy

Add Component > Vertical Layout Group

Settings: Spacing=3, Padding=4 all sides, Child Force Expand Width=true, Child Force Expand Height=true

Now every child added is automatically stacked vertically with equal spacing

With 9 children and ~785px height, each gauge gets ~85px. With 8 children (right column), each gets ~96px.

7.2 Creating a Single Gauge (Step by Step)

Right-click LeftGaugePanel > UI > Image. Name: "NeutronPowerGauge"

Set Image color: #14141A (dark gauge background)

Add Component: MosaicGauge

In MosaicGauge component, set Type = NeutronPower

Right-click NeutronPowerGauge > UI > Legacy > Text. Name: "Value"

Font Size=20, Alignment=Center, Color=#00FF88, Anchor=fill upper 60%

Drag Value text into MosaicGauge's "Value Text" slot in Inspector

Create another Text child: "Label", Font Size=10, Color=#8090A0, text="NEUTRON POWER"

Drag Label into "Label Text" slot

Repeat for all 9 gauges in the left column

7.3 Left Column: 9 Gauges (Top to Bottom)

#

Name

GaugeType

Range / Notes

1

NeutronPowerGauge

NeutronPower

0–120%

2

ThermalPowerGauge

ThermalPower

0–3800 MWt

3

StartupRateGauge

StartupRate

-1 to +1 DPM

4

PeriodGauge

ReactorPeriod

-999 to +999 sec

5

ReactivityGauge

TotalReactivity

-10000 to +1000 pcm

6

KeffGauge

(custom)

0.900–1.100

7

BoronGauge

Boron

0–2500 ppm

8

XenonGauge

Xenon

0–4000 pcm

9

FlowGauge

FlowFraction

0–120%

7.4 Right Column: 8 Gauges

#

Name

GaugeType

Range / Notes

1

TavgGauge

Tavg

500–650°F

2

ThotGauge

Thot

500–650°F

3

TcoldGauge

Tcold

500–650°F

4

DeltaTGauge

DeltaT

0–80°F

5

FuelCenterlineGauge

FuelCenterline

500–4800°F

6

HotChannelGauge

(custom)

Hot channel fuel centerline

7

PressureGauge

(custom)

0–2500 psig (needs PZR model)

8

PZRLevelGauge

(custom)

0–100% (needs PZR model)

Chapter 8: Building the Bottom Control Panel

8.1 Subdivide into Groups

The BottomPanel contains 5 control groups arranged horizontally plus an alarm strip. Use anchor ranges:

Group

AnchorMin X

AnchorMax X

Contains

Rod Control

0

0.20

WITHDRAW, INSERT, STOP, Bank Select, AUTO/MAN

Bank Position

0.20

0.55

MosaicRodDisplay (8 vertical bars)

Boron Control

0.55

0.70

BORATE, DILUTE, ppm readout

Trip Controls

0.70

0.85

TRIP (with cover), RESET, status light

Time Controls

0.85

1.0

Compression slider, PAUSE, sim clock

For each group, use Y anchors from 0.15 to 1.0 (leaving bottom 15% for the alarm strip).

8.2 Creating Buttons

Right-click parent group > UI > Button (Legacy)

Rename (e.g., "WithdrawBtn"), set Image color (#2A3A2A for green-tinted dark)

Select child Text, set label ("WITHDRAW"), Font Size=12, Bold, Color=#00FF88

Connecting to code:

In Button component, find On Click () section at bottom

Click + to add event

Drag the MosaicControlPanel GameObject into the Object slot

Select function: MosaicControlPanel > WithdrawRods

Now clicking this button calls WithdrawRods() on MosaicControlPanel, which calls ReactorController, which tells physics to start withdrawing.

8.3 The Bank Position Display

In the Bank Position group, create a child Image that fills the area

Add Component: MosaicRodDisplay

MosaicRodDisplay reads bank positions from MosaicBoard.Instance automatically

8.4 Trip Button with Safety Cover

Two overlapping buttons — cover on top, actual trip button beneath:

Create "TripCover" button: semi-transparent red, text="LIFT TO ARM"

Create "TripActual" button behind it: bright red, text="TRIP". Start with Active=unchecked

TripCover onClick: hide itself, show TripActual

TripActual onClick: call ReactorController.Trip()

After 5 seconds idle or after trip, cover reactivates

8.5 Time Compression

Add a Slider component to a child object

Set Min Value=0, Max Value=10 (10 discrete notches)

In the OnValueChanged event, map the slider value to time compression:

float[] tcValues = {0, 1, 2, 5, 10, 50, 100, 500, 1000, 5000, 10000};

Add a Text showing current rate ("100x") and a sim clock ("02:15:30")

Chapter 9: Alarm Annunciator Strip

9.1 Layout

Under BottomPanel, create "AlarmStrip" with AnchorMin=(0,0), AnchorMax=(1,0.15)

Add HorizontalLayoutGroup: Spacing=3, Child Force Expand=true

Create 10 child Image+Text pairs (alarm tiles)

9.2 Alarm Tile States

Inactive: dark grey #2A2A2A, grey text — normal condition

Flashing: alternates alarm color / dark every 500ms — new unacknowledged alarm

Steady: constant alarm color — acknowledged but condition still active

MosaicAlarmPanel manages this state machine. New alarm starts flashing, ACK button sets to steady, condition clear extinguishes tile.

Chapter 10: Assembly Detail Panel

10.1 Visibility

The DetailPanel starts with its Active checkbox unchecked (hidden). When the player clicks an assembly cell, CoreMosaicMap shows it:

DetailPanel.gameObject.SetActive(true);

Clicking the same cell again or clicking empty space hides it.

10.2 Content Layout

Add VerticalLayoutGroup to DetailPanel

Child elements, top to bottom:

Header Text: grid position ("H-8, Assembly #97")

Type label: "FUEL" or "RCCA - Bank D"

Divider: thin Image, height=2, color=#444444

Power readout: "Relative Power: 1.05"

Fuel CL temp: "Fuel CL: 2850°F"

Coolant outlet temp: "Coolant Out: 615°F"

If RCCA: Rod position, bank worth, motion status

PART C

Wiring It All Together

Chapter 11: Connecting GUI to Physics

11.1 The Connection Hub: MosaicBoard

MosaicBoard is the central hub connecting UI to ReactorController. Add it to the ReactorOperatorScreen panel. All MosaicGauge, MosaicIndicator, and MosaicAlarmPanel components auto-register with MosaicBoard.Instance.

11.2 Wiring Checklist

MosaicBoard.Reactor must reference the ReactorController (auto-wired by MosaicBoardSetup)

Each MosaicGauge: ValueText and LabelText slots connected, Type dropdown set

MosaicRodDisplay: just needs to be a child/descendant of MosaicBoard's object

MosaicControlPanel: same auto-registration via MosaicBoard.Instance

MosaicAlarmPanel: same auto-registration

CoreMosaicMap: reads ReactorController directly for 193-cell updates (new wiring)

11.3 MosaicBoardSetup Does the Heavy Lifting

Add MosaicBoardSetup alongside MosaicBoard. In the Inspector it shows:

Start State dropdown: Cold Shutdown, Hot Zero Power, or Power Operation

Initial Power slider (for Power Operation)

Initial Boron ppm

Auto Start checkbox (starts simulation immediately)

At runtime, it automatically creates ReactorController and ReactorSimEngine if they do not exist in the scene, then initializes the reactor to your chosen state.

Chapter 12: Input Handling

12.1 Screen Toggle (Key 1)

In your ReactorOperatorScreen.Update() method:

void Update() {

if (Input.GetKeyDown(KeyCode.Alpha1)) {

screenPanel.SetActive(!screenPanel.activeSelf);

}

}

GetKeyDown fires once on keypress. SetActive toggles visibility of the panel and all children.

12.2 Mouse Tooltips

Create a small panel that follows the mouse cursor when hovering over assembly cells:

void UpdateTooltip() {

if (hoveredCell != null) {

tooltip.SetActive(true);

tooltip.transform.position = Input.mousePosition + new Vector3(15,-15,0);

tooltipText.text = GetAssemblyInfo(hoveredCell);

} else {

tooltip.SetActive(false);

}

}

Chapter 13: The Builder Script

13.1 Pattern

OperatorScreenBuilder follows the same pattern as your existing MosaicBoardBuilder:

#if UNITY_EDITOR

[MenuItem("Critical/Create Operator Screen")]

public static void Create() {

// 1. Create Canvas (or find existing)

// 2. Create master panel with MosaicBoard + MosaicBoardSetup

// 3. Create left gauge column (VerticalLayoutGroup + 9 gauges)

// 4. Create core map area + CoreMosaicMap component

// 5. Create right gauge column (8 gauges)

// 6. Create detail panel (hidden)

// 7. Create bottom panel (5 control groups + alarm strip)

// 8. Wire all references

}

#endif

13.2 Running It

Save all scripts (Ctrl+S)

Return to Unity, wait for compilation

Menu bar: Critical > Create Operator Screen

Entire hierarchy appears. Press Play to test.

PART D

Testing and Troubleshooting

Chapter 14: Running and Verifying

14.1 Verification Steps

Press Play. Reactor initializes (check Console for setup messages).

Gauges: Neutron Power ~0%, Tavg ~557°F, Boron ~1500 ppm at HZP.

Core map: 193 cells in cross-pattern (corners empty, NOT a full square).

Hover cells: tooltip shows assembly data.

Click cell: detail panel appears with assembly info.

Bank filter buttons: highlight correct RCCA positions.

WITHDRAW: rod bars move up, reactivity changes.

TRIP (lift cover, press): all bars drop to zero, REACTOR TRIP alarm flashes.

Time compression: simulation speeds up, xenon builds after trip.

Key 1: screen toggles without affecting simulation.

14.2 Console Messages

"Created ReactorController" — normal, auto-created

"Initialized to HotZeroPower" — normal startup

NullReferenceException — a reference not wired. Check script name and line number.

MissingComponentException — component not on the GameObject you expected

Chapter 15: Common Problems and Fixes

Problem

Fix

UI elements invisible

Check: Canvas render mode set? Element Active? Image alpha > 0? Not behind another element? Inside the Canvas?

Wrong position

Y=0 is bottom, Y=1 is top. AnchorMin is bottom-left. Check offsets are not pushing off-screen.

Text not showing

Check: color alpha > 0? Font assigned? Font size not too large? Overflow set to Overflow not Truncate?

Buttons not clickable

Canvas needs GraphicRaycaster? Scene has EventSystem? Image Raycast Target = true? Nothing blocking on top?

Gauge shows 0

MosaicBoard.Reactor assigned? ReactorController initialized? Gauge Type set? ValueText wired?

Layout Group children wrong size

Check Child Force Expand and Control Child Size. Check LayoutElement on children if present.

Core map is full square (not cross)

Corner cells (CORE_GRID == -1) not being skipped. Check rendering loop.

NullReferenceException

Read error for script + line number. That variable is null — a reference was not dragged in Inspector.

Low FPS

Core map updates at 2 Hz max. Gauge updates at 10 Hz. Throttle with Time.time checks.

APPENDICES

Appendix A: Complete GameObject Hierarchy

ReactorOperatorCanvas [Canvas, CanvasScaler, GraphicRaycaster]

EventSystem [EventSystem, StandaloneInputModule]

ReactorController [ReactorController]

ReactorSimEngine [ReactorSimEngine]

ReactorOperatorScreen [Image, MosaicBoard, MosaicBoardSetup]

LeftGaugePanel [Image, VerticalLayoutGroup]

NeutronPowerGauge [Image, MosaicGauge] > Value [Text], Label [Text]

ThermalPowerGauge, StartupRateGauge, PeriodGauge,

ReactivityGauge, KeffGauge, BoronGauge, XenonGauge, FlowGauge

CoreMapPanel [Image]

DisplayModeButtons [HLayoutGroup] > PowerBtn, FuelTempBtn...

MapContainer [Image, CoreMosaicMap] > Cell_R_C [Image, AssemblyCell] x193

BankFilterButtons [HLayoutGroup] > AllBtn, SA..A Btns

RightGaugePanel [Image, VerticalLayoutGroup]

TavgGauge, ThotGauge, TcoldGauge, DeltaTGauge,

FuelCenterlineGauge, HotChannelGauge, PressureGauge, PZRLevelGauge

DetailPanel [Image, AssemblyDetailPanel] (inactive)

HeaderText, TypeLabel, Divider, Readouts...

BottomPanel [Image]

RodControlGroup > WithdrawBtn, InsertBtn, StopBtn, BankSelect

BankPositionDisplay [MosaicRodDisplay]

BoronControlGroup > BorateBtn, DiluteBtn, BoronReadout

TripControlGroup > TripCover, TripActual, ResetBtn, StatusLight

TimeControlGroup > TimeSlider, PauseBtn, SimClock, RateDisplay

AlarmStrip [HLayoutGroup] > 10 alarm tiles

Appendix B: Anchor Quick Reference

Goal

AnchorMin

AnchorMax

Offsets

Fill entire parent

(0, 0)

(1, 1)

(0,0) (0,0)

Left 15%

(0, 0)

(0.15, 1)

(4,4) (-2,-4)

Center 50%

(0.15, 0)

(0.65, 1)

(2,4) (-2,-4)

Right 20%

(0.80, 0)

(1, 1)

(2,4) (-4,-4)

Bottom 25%

(0, 0)

(1, 0.25)

(4,4) (-4,-2)

Top 15% strip

(0, 0.85)

(1, 1)

(0,0) (0,0)

Fixed 200x100 at center

(0.5, 0.5)

(0.5, 0.5)

sizeDelta=(200,100)

Appendix C: Color Palette Reference

Element

Hex

Usage

Screen background

#1A1A1F

Darkest layer. Main screen fill.

Panel background

#1E1E28

Zone panels (gauge columns, bottom panel).

Gauge background

#14141A

Individual gauge dark wells.

Map background

#12121A

Core map container behind cells.

Panel border

#2A2A35

Subtle edge delineation.

Normal text

#C8D0D8

Primary text color.

Label text

#8090A0

Secondary labels, units.

Green accent

#00FF88

Normal values, healthy status.

Amber accent

#FFB830

Warnings, approaching limits.

Red accent

#FF3344

Alarms, trip state.

Cyan accent

#00CCFF

Selection highlight, interactive focus.

Core cold (blue)

#0044AA

Low relative power on core map.

Core hot (red)

#FF2200

High relative power on core map.

Alarm tile inactive

#2A2A2A

Dark grey when no alarm.

— End of Implementation Manual —
