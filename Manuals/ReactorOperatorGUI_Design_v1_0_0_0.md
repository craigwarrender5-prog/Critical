# Reactor Operator GUI Design v1.0.0.0

CRITICAL: Master the Atom

Reactor Operator GUI

Design Specification Document

Version 1.0.0.0

February 2026

Target: 1920 x 1080 Windowed | Unity uGUI

Westinghouse 4-Loop PWR (3411 MWt) Reference Plant

1. Design Overview

1.1 Purpose

The Reactor Operator GUI is the primary gameplay interface for CRITICAL: Master the Atom. It provides the player with a realistic control room experience modeled on authentic Westinghouse 4-Loop PWR operator interfaces. The screen is bound to keyboard key '1' and presents a unified view of reactor status, core conditions, and operator controls.

1.2 Design Philosophy

The GUI design draws from two complementary control room traditions:

Westinghouse PWR Control Board: The horseshoe-shaped instrumentation layout with annunciator panels, vertical rod position indicators, and analog/digital parameter displays. This provides the functional foundation.

RBMK Mosaic Mimic Board: The iconic illuminated core map with colored cells representing individual fuel channels. This provides the visual centerpiece — adapted from the RBMK channel grid to the Westinghouse 193-assembly cross-pattern.

The result is a hybrid aesthetic: the visual drama of the RBMK mosaic board with the engineering accuracy of a Westinghouse PWR control room.

1.3 Resolution and Layout Strategy

The target resolution is 1920 x 1080 pixels in a windowed display. The layout divides the screen into four functional zones:

Zone

Position

Content

Left Gauges

Left 15%

Nuclear instrumentation: power, reactivity, period, startup rate, boron, xenon

Core Mosaic Map

Center 50%

Interactive 193-assembly core visualization with RCCA overlays, color-coded power/temperature

Right Gauges

Right 15%

Thermal-hydraulic parameters: temperatures, pressure, flow, fuel temps

Bottom Panel

Bottom 25%

Control panel (rod controls, bank position bars, boron controls, trip, time compression) and alarm annunciator strip

1.4 Color Theme

Dark industrial theme reflecting actual control room ambient lighting:

Element

Color

Rationale

Background

#1A1A1F

Near-black with blue undertone. Reduces eye strain during extended play.

Panel borders

#2A2A35

Subtle panel delineation without harsh contrast.

Text (normal)

#C8D0D8

Slightly cool white. Readable without glare.

Text (labels)

#8090A0

Muted blue-grey for secondary labels and units.

Accent (green)

#00FF88

Primary healthy/normal indicator. Nuclear green.

Accent (amber)

#FFB830

Warning state. Approaching limits.

Accent (red)

#FF3344

Alarm/trip state. Immediate attention required.

Accent (cyan)

#00CCFF

Selected/highlighted items. Interactive focus.

Core cold (blue)

#0044AA

Low relative power/temperature on core map.

Core hot (red)

#FF2200

High relative power/temperature on core map.

2. Central Core Mosaic Map

The core mosaic map is the visual centerpiece of the GUI and the primary innovation beyond standard PWR control room displays. It renders all 193 fuel assemblies as interactive colored cells in the authentic Westinghouse cross-pattern, with RCCA positions overlaid as bank-colored markers.

2.1 Core Map: 193 Fuel Assembly Layout

The Westinghouse 4-Loop PWR contains 193 fuel assemblies arranged in a roughly circular cross-pattern within a 15x15 grid. The corners of the grid are unoccupied, yielding the characteristic octagonal core cross-section. Each assembly is a 17x17 lattice of 264 fuel rods, 24 guide thimbles, and 1 instrument tube.

Of the 193 assemblies, 53 contain Rod Cluster Control Assemblies (RCCAs) distributed across 8 banks. The remaining 140 assemblies contain either burnable poison rod assemblies (BPRAs), thimble plugs, or secondary neutron source assemblies.

2.1.1 Core Cross-Pattern (15x15 Grid Occupancy)

The following table shows the 15x15 grid occupancy. Each cell shows the assembly type: F = fuel-only, bank letter = RCCA location (SA, SB, SC, SD, D, C, B, A), dash = unoccupied corner position. Grid coordinates use Row (1-15, top to bottom) and Column (A-P, excluding I, left to right).

This layout is based on the standard Westinghouse 4-Loop core loading arrangement from NRC FSAR documentation. The RCCA bank assignments follow the typical pattern where shutdown banks (SA-SD) occupy peripheral and semi-peripheral positions, while control banks (D, C, B, A) occupy positions nearer the core center for effective power shaping.

The core occupancy follows quarter-core symmetry (octant symmetry for RCCA placement). The 15x15 grid has corner positions removed:

Row 1:  --  --  --  --  F   F   F   F   F   F   F  --  --  --  --

Row 2:  --  --  F   F   F   F   F   F   F   F   F   F   F  --  --

Row 3:  --  F   F   F   F   F   F   F   F   F   F   F   F   F  --

Row 4:  --  F   F   F   F   F   F   F   F   F   F   F   F   F  --

Row 5:  F   F   F   F   F   F   F   F   F   F   F   F   F   F   F

Row 6:  F   F   F   F   F   F   F   F   F   F   F   F   F   F   F

Row 7:  F   F   F   F   F   F   F   F   F   F   F   F   F   F   F

Row 8:  F   F   F   F   F   F   F   F   F   F   F   F   F   F   F

Row 9:  F   F   F   F   F   F   F   F   F   F   F   F   F   F   F

Row 10: F   F   F   F   F   F   F   F   F   F   F   F   F   F   F

Row 11: F   F   F   F   F   F   F   F   F   F   F   F   F   F   F

Row 12: --  F   F   F   F   F   F   F   F   F   F   F   F   F  --

Row 13: --  F   F   F   F   F   F   F   F   F   F   F   F   F  --

Row 14: --  --  F   F   F   F   F   F   F   F   F   F   F  --  --

Row 15: --  --  --  --  F   F   F   F   F   F   F  --  --  --  --

Total occupied positions: 193. Corner exclusions: 4 groups of 8 = 32 removed from 225 (15x15).

2.1.2 RCCA Bank Assignments (53 locations)

The 53 RCCA locations are distributed across 8 banks following standard Westinghouse practice. Bank assignments follow octant symmetry where possible:

Bank

Type

RCCAs

Function

SA

Shutdown

8

First withdrawn during startup. Provides shutdown margin.

SB

Shutdown

8

Second withdrawn. Peripheral core positions.

SC

Shutdown

4

Third withdrawn. Semi-peripheral positions.

SD

Shutdown

4

Last shutdown bank withdrawn. Provides scram reactivity.

D

Control

9

Lead control bank. Primary regulating bank for load follow and temperature control.

C

Control

9

Overlap bank. Supplements Bank D reactivity range.

B

Control

8

Deep power reduction bank. Moves only at lower power levels.

A

Control

4

Lead bank for power reduction. First control bank inserted on power decrease.

Total: 53 RCCAs (24 shutdown + 29 control). Some references cite 53 total for 4-Loop, others cite 57. Our simulation uses 53 per the standard Westinghouse Technology Manual.

Note: The remaining 140 fuel assembly positions contain no movable control elements and are occupied by fuel-only assemblies, BPRAs, thimble plugs, or neutron source assemblies.

2.2 Visual Rendering

2.2.1 Assembly Cell Rendering

Each fuel assembly is rendered as a square cell approximately 28x28 pixels (fitting the 15x15 grid into ~450x450 pixel area, with 2px gaps). The cell color encodes the primary display parameter selected by the operator:

Display Mode

Color Mapping

Data Source

Relative Power

Blue (#0044AA) to Red (#FF2200) continuous gradient

Assembly power fraction from ReactorCore (power distribution model)

Fuel Temperature

Blue to Yellow to Red (3-stop gradient)

FuelAssembly.CenterlineTemp_F mapped to range

Coolant Temperature

Cyan to Orange gradient

Assembly outlet temperature mapped to range

Rod Bank Overlay

Unique color per bank (see below), unoccupied = grey

ControlRodBank assignment map + position data

Default display mode is Relative Power, which provides the most operationally relevant information at a glance.

2.2.2 RCCA Overlay Rendering

The 53 RCCA locations are overlaid on the core map with bank-specific visual indicators. Each RCCA cell shows:

Bank letter label (SA, SB, SC, SD, D, C, B, A) in the cell corner, using bank color

Insertion indicator — a vertical fill bar within the cell showing rod position (0 = full bar from top = fully inserted, 228 = empty = fully withdrawn)

Motion indicator — subtle pulse animation when bank is actively moving (withdrawing or inserting)

Bank color assignments:

Bank

Hex Color

Description

Visual Notes

SA

#009688

Teal

Shutdown banks use cool tones

SB

#26A69A

Light Teal

Distinguishable from SA

SC

#66BB6A

Green

Fewer assemblies (4), needs visibility

SD

#43A047

Dark Green

Last shutdown bank, transition tone

D

#42A5F5

Blue

Lead control bank. Most visible — largest bank (9).

C

#7E57C2

Purple

Overlap bank, 9 assemblies

B

#EC407A

Pink

Deep reduction bank, 8 assemblies

A

#FFA726

Orange

Lead reduction bank, 4 assemblies. Warm tone for regulating bank.

2.2.3 Interactive Behavior

The core map supports the following interaction modes, designed to maximize information density within the limited 1920x1080 space:

Hover (Mouse Over Assembly):

Tooltip displays: Assembly grid position (e.g., H-8), relative power, fuel centerline temperature, coolant outlet temperature, and RCCA bank/position if applicable

Highlighted assembly cell gets a bright border outline (cyan)

Click (Select Assembly):

Locks the detail panel (see 2.3) to show expanded data for this assembly

If the assembly has an RCCA, highlights ALL assemblies in the same bank with a bank-colored border glow

Click again or click empty space to deselect

Bank Filter Buttons (Below Core Map):

Row of 8 bank toggle buttons: SA | SB | SC | SD | D | C | B | A

Clicking a bank button highlights all RCCA locations for that bank on the core map with bright bank-colored outlines

Non-selected bank assemblies dim slightly (60% opacity) to provide visual contrast

Multiple banks can be selected simultaneously (toggle behavior)

'ALL' button selects/deselects all banks. 'CTRL' button selects only control banks. 'SD' button selects only shutdown banks.

Display Mode Selector (Above Core Map):

Toggle buttons for display mode: POWER | FUEL TEMP | COOLANT TEMP | BANK MAP

Changes the color-coding basis for all 193 cells

BANK MAP mode shows assemblies colored by bank assignment (using bank colors above), fuel-only assemblies in neutral grey

2.3 Assembly Detail Panel

When an assembly is selected (clicked), a detail panel appears adjacent to the core map showing comprehensive data for that assembly. This panel replaces the need for a separate drill-down screen.

Panel contents:

Grid position and assembly index (e.g., H-8, Assembly #97)

Assembly type: Fuel, RCCA (with bank ID), BPRA, Source

Relative power factor

Fuel centerline temperature (°F)

Coolant outlet temperature (°F)

If RCCA: Rod position (steps), bank total worth (pcm), bank insertion reactivity (pcm), motion status

Mini radial temperature profile graphic: fuel centerline → pellet surface → gap → clad → coolant

3. Instrument Gauge Columns

3.1 Left Column — Nuclear Instrumentation

The left gauge column presents all nuclear/reactivity parameters. These are the parameters a reactor operator monitors to understand the neutronic state of the core. Arranged top-to-bottom by operational priority:

Parameter

Unit

Range

Source Property

Neutron Power

%

0 - 120%

ReactorController.NeutronPower * 100

Thermal Power

MWt

0 - 3800

ReactorController.ThermalPower_MWt

Startup Rate

DPM

-1 to +1

ReactorController.StartupRate_DPM

Reactor Period

sec

-999 to +999

ReactorController.ReactorPeriod (clamped)

Total Reactivity

pcm

-10000 to +1000

ReactorController.TotalReactivity

keff

0.900 - 1.100

ReactorController.Keff

Boron Concentration

ppm

0 - 2500

ReactorController.Boron_ppm

Xenon Worth

pcm

0 - 4000

ReactorController.Xenon_pcm (abs value)

RCS Flow

%

0 - 120%

ReactorController.FlowFraction * 100

Each gauge displays as a combined analog arc (upper 70% of gauge area) with a digital readout (lower 30%). The analog arc provides quick visual assessment while the digital readout provides precision. Gauge borders change color based on alarm state: green (normal), amber (warning), red (alarm).

3.2 Right Column — Thermal-Hydraulic Instrumentation

The right gauge column presents thermal-hydraulic parameters reflecting the heat removal and coolant state:

Parameter

Unit

Range

Source Property

Tavg

°F

500 - 650

ReactorController.Tavg

Thot

°F

500 - 650

ReactorController.Thot

Tcold

°F

500 - 650

ReactorController.Tcold

ΔT (Core)

°F

0 - 80

ReactorController.DeltaT

Fuel Centerline

°F

500 - 4800

ReactorController.FuelCenterline

Hot Ch. Centerline

°F

500 - 4800

ReactorController.HotChannelCenterline

RCS Pressure

psig

0 - 2500

PZR pressure minus 14.7 (when PZR model available)

PZR Level

%

0 - 100

PZR level percentage (when PZR model available)

4. Bottom Control Panel

The bottom 25% of the screen houses all operator controls and the alarm annunciator strip. It is subdivided into five functional groups arranged left to right:

4.1 Rod Control Group

WITHDRAW button — Starts sequential rod withdrawal (SA→A with overlap)

INSERT button — Starts sequential rod insertion (A→SA)

STOP button — Halts all rod motion immediately

BANK SELECT dropdown — Selects individual bank for manual single-bank control

AUTO/MANUAL toggle — Switches between automatic rod control (Tavg program) and manual mode

4.2 Bank Position Display

Eight vertical bar indicators showing positions of all rod banks (SA through A). Each bar:

Scale: 0 (bottom, fully inserted) to 228 (top, fully withdrawn)

Colored fill using bank color scheme from Section 2.2.2

Digital step count displayed below each bar

Bank label (SA, SB, SC, SD, D, C, B, A) above each bar

Motion indicator arrow (up/down) when bank is moving

Insertion limit line (dashed) at step 30 for Bank D (BANK_D_INSERTION_LIMIT)

4.3 Boron/Chemistry Control

BORATE button — Increases boron concentration (positive reactivity control)

DILUTE button — Decreases boron concentration (negative reactivity control)

Boron ppm readout — Current boron concentration with trend arrow

Rate selector — Adjusts boration/dilution rate

4.4 Trip and Safety Controls

TRIP button — Red, with safety cover toggle. Drops all rods. Requires cover lift before press (two-action safety).

RESET TRIP button — Resets trip condition after all rods confirmed at bottom. Grayed out unless trip conditions cleared.

TRIP STATUS indicator — Backlit panel: NORMAL (green) or TRIPPED (flashing red)

4.5 Time Compression Controls

Time compression slider — Logarithmic scale: 1x, 2x, 5x, 10x, 50x, 100x, 500x, 1000x, 5000x, 10000x

PAUSE button — Freezes simulation time (TimeCompression = 0)

Simulation clock display — Shows elapsed simulation time in HH:MM:SS format

Current compression readout — Shows active time compression factor (e.g., "100x")

4.6 Alarm Annunciator Strip

A horizontal strip of backlit alarm tiles spanning the full width of the bottom panel. Modeled after the Westinghouse annunciator window design:

Alarm Tile

Color (Active)

Trigger Condition

REACTOR TRIP

Magenta flash

ReactorController.IsTripped == true

OVERPOWER

Red flash

ThermalPower > 1.04 (104%)

HI NEUTRON FLUX

Red flash

NeutronPower > 1.09 (109% trip setpoint)

LO RCS FLOW

Red flash

FlowFraction < 0.87 (87% low flow trip)

TAVG HI

Amber flash

Tavg > 590°F (above HFP setpoint + deadband)

TAVG LO

Amber flash

Tavg < 555°F (below HZP setpoint)

ROD BOTTOM

Amber flash

Any control bank at step 0 (ControlRodBank.RodBottomAlarm)

ROD DEVIATION

Amber

Bank sequence violation detected

HI STARTUP RATE

Amber flash

StartupRate_DPM > 0.5 DPM

SHORT PERIOD

Red flash

ReactorPeriod > 0 AND < 30 seconds

Alarm behavior follows the standard Westinghouse annunciator sequence: new alarm flashes rapidly until acknowledged (ACK button), then burns steady until condition clears, then extinguishes. The ACK button clears the flash state but not the alarm itself.

5. Data Architecture and Module Integration

5.1 Data Flow

The GUI reads data exclusively from ReactorController (MonoBehaviour) which in turn reads from the physics modules. No physics calculations occur in the GUI layer. The data flow is strictly one-directional for display, with operator commands flowing back through ReactorController methods:

ReactorCore (physics) → ReactorController (coordinator) → GUI (display)

GUI (operator input) → ReactorController (commands) → ReactorCore (physics)

5.2 New Components Required

Component

Type

Purpose

ReactorOperatorScreen.cs

MonoBehaviour

Master screen controller. Manages toggle (key 1), layout, component wiring, update loop.

CoreMosaicMap.cs

MonoBehaviour

193-assembly interactive core map. Handles rendering, selection, hover, bank filtering.

CoreMapData.cs

Static Data

Assembly grid positions, RCCA bank assignments, coordinate lookup tables. Pure data, no MonoBehaviour.

AssemblyDetailPanel.cs

MonoBehaviour

Expandable detail panel for selected assembly. Shows radial temp profile, rod data, etc.

OperatorScreenBuilder.cs

Editor Tool

Menu tool: Critical > Create Operator Screen. Generates complete UI hierarchy.

Existing components to be reused as-is (these are GOLD STANDARD validated modules and must not be modified):

MosaicGauge.cs — Individual gauge rendering (analog arc + digital readout)

MosaicIndicator.cs — Binary status indicators with flash capability

MosaicRodDisplay.cs — Rod position visualization (vertical bars)

MosaicControlPanel.cs — Operator controls (rod, boron, trip, time)

MosaicAlarmPanel.cs — Alarm annunciator with scrolling list

MosaicTypes.cs — Shared enums (GaugeType, AlarmState, IndicatorCondition)

5.3 CoreMapData Static Layout

The CoreMapData class encodes the authentic Westinghouse 4-Loop core layout as static arrays. This data is derived from NRC FSAR documentation and the Westinghouse Technology Systems Manual.

Key data structures:

// 15x15 grid: -1 = empty (corner), 0 = fuel-only, 1-8 = RCCA bank index

public static readonly int[,] CORE_GRID = new int[15, 15] { ... };

// Assembly index (0-192) to grid position mapping

public static readonly (int row, int col)[] ASSEMBLY_POSITIONS = new (int,int)[193];

// RCCA bank to assembly indices mapping

public static readonly int[][] BANK_ASSEMBLIES = new int[8][];

This data must be validated against the Westinghouse core loading arrangement (NRC FSAR Figure 3.1-5 or equivalent). The exact RCCA positions within the 193-assembly grid must match the standard 4-Loop reference plant.

6. Implementation Plan

6.1 Implementation Phases

Step

Component

Deliverables

1

CoreMapData.cs

Static data class with authenticated 193-assembly grid layout, RCCA bank assignments, coordinate mappings. Full validation method.

2

CoreMosaicMap.cs

Core map MonoBehaviour: renders 193 cells, applies color-coding, handles hover/click selection, bank filtering. Uses Unity Image components in a Grid Layout.

3

AssemblyDetailPanel.cs

Floating detail panel that appears when an assembly is selected. Reads assembly-specific data from ReactorController/ReactorCore.

4

ReactorOperatorScreen.cs

Master screen controller: key 1 toggle, layout management, component orchestration, gauge data binding, alarm wiring.

5

OperatorScreenBuilder.cs

Editor tool to generate complete UI hierarchy. Menu: Critical > Create Operator Screen. Wires all references.

6

Integration + Test

Connect to ReactorController, verify data flow at HZP/50%/100% power states, validate alarm triggers, test interaction (hover, click, bank filter).

6.2 Technical Constraints

GOLD standard modules (ReactorCore, ControlRodBank, FuelAssembly, PlantConstants, all physics) must NOT be modified

GUI update rate: 10 Hz (100ms) for gauges, 2 Hz (500ms) for core map color updates (performance)

All rendering via Unity uGUI (Canvas, Image, Text, Button) — no custom shaders required for Phase 1

Core map uses 193 pre-instantiated Image GameObjects (not dynamically created each frame)

Tooltip and detail panel use object pooling (show/hide, not create/destroy)

All RCCA bank assignments and core geometry must be validated against Westinghouse 4-Loop PWR technical specifications

6.3 Acceptance Criteria

All 193 assembly cells render in correct Westinghouse cross-pattern with no gaps or misalignment

53 RCCA locations correctly identified and bank-assigned per reference plant data

Core map color-coding updates in real-time reflecting ReactorCore physics state

All 17 gauges (9 left + 8 right) display correct data from ReactorController with proper units and ranges

Rod control commands (withdraw, insert, stop, trip, reset) function correctly through GUI buttons

Bank position bars accurately track all 8 banks with correct bank colors and step counts

Alarm annunciator tiles trigger and flash correctly for all defined alarm conditions

Interactive core map: hover shows tooltip, click selects assembly, bank filter highlights correct positions

Screen toggle (key 1) shows/hides entire GUI without affecting simulation state

Time compression controls function correctly across full range (1x to 10000x)

7. Pixel Layout Specification (1920 x 1080)

Exact pixel regions for the four layout zones, accounting for 4px panel margins:

Zone

X

Y

Width

Height

Notes

Left Gauges

4

4

280

788

9 gauges @ ~85px each

Core Map Area

288

4

960

788

Map + mode buttons + bank buttons

Right Gauges

1252

4

280

788

8 gauges @ ~96px each

Detail Panel

1536

4

380

788

Assembly detail (when selected)

Bottom Panel

4

796

1912

280

Controls + alarms

The core map rendering area within the Core Map Zone is approximately 480 x 480 pixels (32px per cell x 15 cells), leaving room for mode selector buttons above and bank filter buttons below.

The detail panel on the far right is only visible when an assembly is selected. When no assembly is selected, the right gauge column extends to fill the available space, or the core map area widens slightly.

8. Technical References

NRC ML11223A212 — Westinghouse Technology Systems Manual, Section 3.1: Reactor Vessel and Internals (core loading arrangement, RCCA design parameters)

NRC ML11223A342 — Westinghouse Technology Systems Manual, Section 19.0: Plant Operations (startup/shutdown procedures, control room operations)

Westinghouse 4-Loop FSAR Chapter 4 — Reactor Design (fuel assembly specifications, RCCA bank assignments, core geometry)

NRC NUREG/CR-6042 — Control Room Design Review Guidelines (human factors engineering for nuclear plant control rooms)

MIT OCW 22.06 — Engineering of Nuclear Systems, PWR Description (Buongiorno): fuel assembly geometry, RCCA configuration, core parameters

Existing CRITICAL Codebase — PlantConstants.cs, ControlRodBank.cs, FuelAssembly.cs, ReactorController.cs, MosaicBoard.cs and related UI components

— End of Design Document —