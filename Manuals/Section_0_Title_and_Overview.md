# Section 0: Title and Overview

WESTINGHOUSE 4-LOOP PWR

CONTROL ROOM INDICATOR TYPES

Blender 5.0 → Unity 6.3 Construction Manual

Companion Volume to the Analog Round Gauge Manual

Vertical Edgewise Meters • Vertical Bar Graphs • Strip Chart Recorders

Annunciator Windows • Indicator Lamps • Digital Numeric Readouts

Nuclear Reactor Simulator Project — February 2026

Table of Contents

Update this Table of Contents in Word/LibreOffice by right-clicking and selecting Update Field.

Part 1: Real-World PWR Control Room Indicator Types

A Westinghouse 4-Loop PWR main control room employs a diverse array of instrumentation displays across its vertical panels and benchboards. The control room is arranged in a horseshoe or wrap-around configuration, divided into functional sections: the reactor/RCS panel, steam generator and feedwater panel, turbine-generator panel, electrical distribution panel, and engineered safety features panel. Each section uses a specific mix of indicator types optimized for the information being conveyed.

The companion Analog Round Gauge Manual covered the classic Bourdon-tube style circular dial gauge in complete detail. This manual covers the six remaining major indicator types found on Westinghouse PWR control panels, each serving a distinct operational purpose within the control room human-system interface.

1.1 Complete Indicator Type Inventory

The following table catalogues every major indicator type found on a real Westinghouse 4-Loop PWR main control room panel, with typical parameters and panel locations.

Indicator Type

Typical Use

Panel Location

Analog Round Gauge

Temperatures, pressures, levels

Vertical panel faces, eye level

Vertical Edgewise Meter

Loop temps, SG levels, motor currents, flows

Dense vertical arrays on panel faces

Vertical Bar Graph Display

PZR level, SG levels, flow rates

Modernized panel sections

Strip Chart Recorder

Temperature/pressure/level/power trends

Benchboards and vertical faces

Annunciator Window Tile

All alarm conditions

Grid arrays at TOP of all panels

Indicator Lamp (Status Light)

Pump/valve/breaker component status

Embedded in mimic bus diagrams

Digital Numeric Readout

Rod position, power %, precise readings

Where high precision is needed

1.2 Design Philosophy

Each indicator type is modeled as a 2.5D asset in Blender 5.0, exported as FBX, imported into Unity 6.3, and rendered to a RenderTexture displayed on the GUI panel via a RawImage component. This provides photorealistic fidelity while maintaining excellent runtime performance. All dimensions, color schemes, scale markings, and behavioral characteristics are based on real Westinghouse 4-Loop PWR instrumentation as documented in NRC NUREG-0700, Weschler Instruments nuclear-grade specifications, and standard nuclear industry human factors engineering practice.