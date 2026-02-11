# Section 7: Guidelines and Quick Reference

Part 8: Universal Integration Guidelines

8.1 Standard FBX Export Settings

Setting

Value

Scale

0.01

Apply Scalings

FBX All

Forward Axis

-Z Forward

Up Axis

Y Up

Apply Modifiers

Checked

Smoothing

Face

8.2 Unity Layer Architecture

Layer

Purpose

GaugeAnalog

Analog round gauges (companion manual)

GaugeEdgewise

Vertical edgewise meters

GaugeBarGraph

Vertical bar graph displays

GaugeStripChart

Strip chart recorders

GaugeAnnunciator

Annunciator tile arrays

GaugeStatusLamp

Indicator lamp / mimic panels

GaugeDigital

Digital numeric readouts

Each layer has a dedicated Orthographic camera rendering only that layer to its own RenderTexture. Clear Flags = Solid Color (black). Depth value must not conflict with main camera.

8.3 RenderTexture Sizing

Gauge Type

Recommended RT Size

Analog Round Gauge

512 × 512 px

Vertical Edgewise Meter

256 × 512 px

Vertical Bar Graph

256 × 512 px

Strip Chart Recorder

512 × 512 px

Annunciator Grid (per row of 8)

1024 × 128 px

Indicator Lamp (single)

128 × 128 px

Indicator Lamp (mimic panel)

1024 × 512 px

Digital Readout

256 × 128 px

8.4 Performance Optimization

Group gauges that share one RenderTexture where possible (e.g., 4 edgewise meters in a bank).

Use MaterialPropertyBlock instead of unique Material instances per gauge.

Render gauge cameras on-demand (camera.Render()) for slow-changing values; every frame for strip charts.

Use GPU instancing on annunciator tile mesh renderers.

8.5 Lighting Standard

Primary: Area Light, 50W, 4000–5000K warm white, above-front.

Fill: Point/Area Light, 10–15W, below-front to soften bezel shadows.

Ambient: Low warm gray (#2A2520) simulating control room wall reflections.

No directional light (avoid harsh shadows on panel instruments).

8.6 NUREG-0700 Color Standards

Color

Meaning

Red

Danger, abnormal, trip, alarm, safety concern

Green

Normal, safe, running, open

Amber/Yellow

Caution, warning, intermediate state

White

Neutral status, information only

Blue

Bypass, suppressed, maintenance

Part 9: Quick Reference and Checklists

9.1 Parameter-to-Gauge Selection Guide

Parameter

Recommended Indicator(s)

Temperatures (T-hot/T-cold/T-avg)

Analog round gauge OR edgewise + strip chart

Pressurizer Pressure

Analog round + digital readout + strip chart

Pressurizer Level

Bar graph + strip chart

SG Water Levels

Bar graph + edgewise backup + strip chart

Reactor Power (%RTP)

Digital readout + analog round gauge

Rod Position (steps)

Digital readout (dedicated)

Flow Rates (RCS, CVCS)

Edgewise meter OR bar graph

Component Status

Indicator lamp (binary on/off)

Alarms

Annunciator window tile

Nuclear Instrumentation

Edgewise meter (log scale) + strip chart

9.2 File Naming Convention

Asset Type

Pattern

Blender source

GaugeType_ParamName.blend

FBX export

GaugeType_ParamName.fbx

Scale texture

GaugeType_Scale_Range.png

Alarm text texture

Annun_System_AlarmText.png

Unity prefab

GaugeType_ParamName.prefab

C# driver

GaugeTypeDriver.cs

RenderTexture

RT_GaugeType_ParamName

9.3 Implementation Checklist

Identify real-world gauge type and parameter range from this manual.

Model in Blender 5.0 per type-specific instructions.

Apply materials with Principled BSDF and emission nodes where needed.

Set up orthographic camera and standard lighting in Blender.

Export FBX with standardized settings (Section 8.1).

Import into Unity 6.3 with correct settings.

Assign to dedicated layer.

Create dedicated orthographic camera for that layer.

Create RenderTexture at correct size (Section 8.3).

Assign RenderTexture to camera Target Texture.

Attach C# driver script to animated element.

Wire SetValue() to the simulation physics engine.

Add RawImage on GUI canvas, assign RenderTexture.

Test full parameter range for correct behavior.

Verify visual consistency with other panel gauges.

--- End of Manual ---