# Section 1: Vertical Edgewise Meter

Part 2: Vertical Edgewise Meter

2.1 Real-World Reference

The vertical edgewise meter is the single most common indicator on a Westinghouse PWR control panel. The industry standard is the Weschler VX-252 and VC-252 series, designed and seismically qualified specifically for the nuclear power industry per IEEE Standard 344-1987 and IEEE Standard 420-1973. These meters occupy a narrow vertical profile approximately 2.5 inches wide by 6 inches tall, allowing operators to compare many parameters at a glance in dense side-by-side arrays.

A thin knife-edge pointer moves vertically along a printed linear scale on the left or right side of the face. The faceplate is white or off-white with black scale markings and a black pointer. On a Westinghouse 4-Loop PWR, edgewise meters monitor: RCS loop temperatures (T-hot and T-cold for all 4 loops), SG narrow-range levels, RCP motor currents, charging and letdown flow, main steam pressures, and containment sump levels.

2.2 Specifications

Property

Specification

Overall Dimensions

2.5" W × 6.0" H × 2.5" D (front face visible)

Faceplate

White/off-white background, matte finish

Scale

Vertical, linear, printed on left or right side

Major Graduations

Every 50 or 100 units, with numeric labels

Minor Graduations

Every 10 or 25 units

Pointer

Black knife-edge, translates vertically

Pointer Travel

Approximately 4.5" of vertical travel

Bezel

Black plastic rectangular frame

Accuracy

2% per ANSI C39.1, calibratable to 1%

Seismic Qualification

IEEE 344-1987

2.3 Parameter Ranges

Parameter

Edgewise Meter Range

T-hot / T-cold (per loop)

100–650°F

SG Narrow Range Level

0–100%

RCP Motor Current

0–500 A

Charging Flow

0–150 gpm

Letdown Flow

0–120 gpm

Main Steam Pressure

0–1200 psig

2.4 Blender 5.0 Modeling

Scene Setup

Open Blender 5.0, delete default cube. Set units: Metric, Unit Scale 0.01 (1 BU = 1 cm).

Set viewport background to neutral dark gray (#1A1A1A).

Meter Body

Add Cube. Dimensions: X=6.35cm, Y=0.5cm, Z=15.24cm. Name "EdgewiseBody".

Edit Mode: select front (+Y) face, Inset (I) by 0.2cm for bezel border, extrude inner face back -0.1cm for recess.

Apply black plastic material: Base Color #1A1A1A, Roughness 0.7, Metallic 0.0.

Faceplate

Add Plane, rotate R X 90. Dimensions: X=5.95cm, Z=14.64cm. Position Y=0.21cm. Name "EdgewiseFace".

UV Unwrap (U → Unwrap). Apply material with Image Texture for the scale.

TIP: Create scale texture in GIMP/Photoshop: 256×512px, white background, vertical scale line on left with major ticks every ~50px, minor ticks every ~10px. Black numerals in small sans-serif font. Export as PNG.

Pointer

Add Plane. Reshape in Edit Mode to a thin horizontal knife: ~3.0cm wide (X) × 0.15cm tall (Z). Merge two left vertices to form a pointed tip.

Position at Y=0.25cm (in front of faceplate). Set origin to bottom-center of the pointer (this is the slide axis anchor).

Name "EdgewisePointer". Apply solid black material (#0A0A0A, Roughness 0.3).

WARNING: Unlike the round gauge needle which rotates, the edgewise pointer translates vertically. The C# script moves this object along local Z. Ensure the origin is correct.

Camera and Lighting

Add Camera: Orthographic, position (0, 15, 7.5), rotation (90, 0, 0), Ortho Scale 18.

Add Area Light above-front: position (0, 10, 20), 50W, 5000K. Optional fill light from below at 10W.

2.5 FBX Export

Select all objects. File → Export → FBX: Scale=0.01, Apply Scalings=FBX All, Forward=-Z, Up=Y, Apply Modifiers=checked. Save as EdgewiseMeter.fbx.

2.6 Unity 6.3 Setup

Import FBX to Assets/Models/Gauges/Edgewise/. Scale Factor=1.0. Import textures to Assets/Textures/Gauges/.

Create layer "GaugeEdgewise". Assign meter prefab to this layer.

Create camera "EdgewiseCam": Orthographic, Culling Mask=GaugeEdgewise only.

Create RenderTexture 256×512px. Assign to EdgewiseCam Target Texture.

Add RawImage on GUI Canvas, assign the RenderTexture.

2.7 C# Driver Script

Attach to EdgewisePointer:

using UnityEngine;

public class EdgewiseMeterDriver : MonoBehaviour

{

[Header("Scale Configuration")]

public float scaleMin = 100f;

public float scaleMax = 650f;

[Header("Pointer Travel (local Z positions)")]

public float pointerZMin = 0.015f;  // Bottom of scale

public float pointerZMax = 0.130f;  // Top of scale

[Header("Dynamics")]

public float damping = 8.0f;

private float _targetZ;

public void SetValue(float value)

{

float v = Mathf.Clamp(value, scaleMin, scaleMax);

float t = Mathf.InverseLerp(scaleMin, scaleMax, v);

_targetZ = Mathf.Lerp(pointerZMin, pointerZMax, t);

}

void Update()

{

Vector3 pos = transform.localPosition;

pos.z = Mathf.Lerp(pos.z, _targetZ, Time.deltaTime * damping);

transform.localPosition = pos;

}

}