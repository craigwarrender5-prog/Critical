# Section 2: Vertical Bar Graph

Part 3: Vertical Bar Graph Display

3.1 Real-World Reference

Vertical bar graph displays (Weschler BG-252 series) combine a 101-segment vertical LED bar with a digital numeric readout. Green segments indicate normal range, amber for caution, red for alarm. Programmable setpoint markers (triangular LEDs) show Hi/Lo thresholds. Trend arrows indicate rising/falling values.

On Westinghouse 4-Loop PWR panels, bar graphs monitor: pressurizer level (0–100%, setpoints at 17% Lo and 92% Hi), SG narrow-range levels, RCS flow rates, CVCS charging and letdown flows, and boric acid concentrations.

3.2 Specifications

Property

Specification

Overall Dimensions

2.5" W × 6.0" H (same panel cutout as edgewise)

Bar Column

101 LED segments, green/amber/red zones

Digital Display

3.5 or 4.5 digit LED below bar column

Setpoint Markers

Triangular LED indicators at Hi/Lo thresholds

Trend Arrows

↑↓ LEDs above digital display

Housing

Black plastic, flush panel mount

Update Rate

4 Hz (250 ms refresh)

3.3 Blender 5.0 Modeling

Housing and Bar Column

Add Cube: X=6.35cm, Y=0.5cm, Z=15.24cm. Edit front face: inset 0.25cm, extrude back -0.15cm. Name "BarGraphBody". Black plastic material.

Add Plane for bar column background: 1.2cm × 10.5cm, positioned inside recess. Name "BarColumnBG". Very dark material (#0D0D0D).

Duplicate as "BarFill": position slightly forward (Y+0.01). Set origin to bottom edge (pivot for vertical scaling). Apply emissive green material: Base Color #00CC44, Emission #00CC44, Strength 3.0.

TIP: For segmented LED appearance: create a 32×512px PNG with thin dark horizontal lines every 5px. Apply as alpha-cutout texture over the emissive material.

Setpoints and Digital Area

Create two small triangles (3-vertex circles, ~0.3cm) on bar edge at Hi/Lo positions. Emissive amber material (#FFAA00, Strength 2.0). Name "SetpointHi"/"SetpointLo".

Add small Plane below bar column for digital readout: 4.0cm × 2.0cm. Dark background (#0A0A0A). Name "DigitalReadout". This displays the numeric value via TextMeshPro in Unity.

3.4 Export and Unity Setup

Export as BarGraphDisplay.fbx with standard settings. In Unity, use the same layer/camera/RenderTexture pattern (256×512px). The BarFill object is scaled on Z by the driver script. TextMeshPro overlays the digital value.

3.5 C# Driver Script

using UnityEngine;

using TMPro;

public class BarGraphDriver : MonoBehaviour

{

[Header("Scale")]

public float scaleMin = 0f, scaleMax = 100f;

public string units = "%";

[Header("Setpoints")]

public float setpointLo = 17f, setpointHi = 92f;

[Header("References")]

public Transform barFill;

public Renderer barFillRenderer;

public TextMeshPro digitalReadout;

[Header("Colors")]

public Color normalColor = new Color(0f, 0.8f, 0.27f);

public Color cautionColor = new Color(1f, 0.67f, 0f);

public Color alarmColor = new Color(1f, 0.15f, 0.15f);

public float damping = 10f;

private float _targetScale, _currentValue;

private MaterialPropertyBlock _mpb;

void Awake() { _mpb = new MaterialPropertyBlock(); }

public void SetValue(float value)

{

_currentValue = Mathf.Clamp(value, scaleMin, scaleMax);

_targetScale = Mathf.InverseLerp(scaleMin, scaleMax, _currentValue);

}

void Update()

{

Vector3 s = barFill.localScale;

s.z = Mathf.Lerp(s.z, _targetScale, Time.deltaTime * damping);

barFill.localScale = s;

Color c = (_currentValue <= setpointLo || _currentValue >= setpointHi)

? alarmColor

: (_currentValue <= setpointLo + 5f || _currentValue >= setpointHi - 5f)

? cautionColor : normalColor;

barFillRenderer.GetPropertyBlock(_mpb);

_mpb.SetColor("_EmissionColor", c * 3f);

_mpb.SetColor("_BaseColor", c);

barFillRenderer.SetPropertyBlock(_mpb);

if (digitalReadout)

digitalReadout.text = _currentValue.ToString("F1") + " " + units;

}

}