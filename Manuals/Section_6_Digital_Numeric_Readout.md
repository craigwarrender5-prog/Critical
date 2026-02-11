# Section 6: Digital Numeric Readout

Part 7: Digital Numeric Readout

7.1 Real-World Reference

Digital numeric readouts are 7-segment LED displays presenting precise numerical values. The most prominent on a Westinghouse 4-Loop PWR are the control rod position indicators, showing steps withdrawn (0–228) for each control bank (A, B, C, D) and shutdown banks in bright red 3-digit 7-segment displays. Other readouts show reactor power (% RTP), precise pressurizer pressure, and NIS channel readings.

A typical display has 3–5 digits with red, green, or amber LED segments. The housing is a black rectangular panel with a tinted lens. Unlit segments are faintly visible as dark ghost outlines – a characteristic visual detail that adds authenticity. Displays update at 1–4 Hz.

7.2 Specifications

Property

Specification

Digit Height

0.56" (14mm) or 1.0" (25mm) per digit

Digits

3, 4, or 5 digits + optional sign/decimal

Segment Type

7-segment LED

Colors

Red (most common), Green, Amber

Background

Black, non-reflective tinted lens

Update Rate

1–4 Hz

7.3 Parameter Formats

Parameter

Format

Rod Position (steps)

3 digits: 000–228 (red)

Reactor Power (%RTP)

4 digits: 000.0–120.0 (red/green)

Pressurizer Pressure

4 digits: 0000–2500 psig (green)

T-avg

4 digits: 000.0–650.0°F (green)

Delta-I

4 digits with sign: ±00.0 (red)

7.4 Blender 5.0 Modeling

Add Cube for housing. 3-digit (rod position): X=8.0cm, Y=0.8cm, Z=4.0cm. Black matte (#111111, Roughness 0.8). Inset front face 0.3cm, extrude back -0.2cm. Name "DigitalBody".

Add Plane for digit window: X=6.8cm, Z=3.0cm, inside recess at Y=0.15cm. Dark tinted material (#0A0503, Roughness 0.3). Name "DigitWindow". UV Unwrap.

Optionally create ghost-segment texture: 256×128px showing "888" in very dark red (#1A0500) for the background layer.

Export as DigitalReadout.fbx. In Unity, use TextMeshPro with a 7-segment LED font (DSEG7-Classic recommended) for the digit display.

TIP: For maximum authenticity, use the free DSEG7-Classic font. Import as TMP font asset. Set HDR color with emission for the LED glow effect. Render ghost segments as a dim TMP text behind the active digits.

7.5 Unity Setup

Import FBX, create prefab. Add TextMeshPro 3D Text child positioned just in front of DigitWindow (+Y by 0.001).

TMP settings: Font=DSEG7-Classic, Alignment=Center/Middle, Color=Red HDR (intensity ~3.0).

Optionally add second TMP for ghost segments: text="888" (or "8888."), color = very dim red.

Render via dedicated camera to 256×128px RenderTexture.

7.6 C# Driver Script

using UnityEngine;

using TMPro;

public class DigitalReadoutDriver : MonoBehaviour

{

[Header("Display Config")]

public int digitCount = 3;

public int decimalPlaces = 0;

public bool leadingZeros = true;

public bool showSign = false;

[Header("Range")]

public float minValue = 0f, maxValue = 228f;

[Header("References")]

public TextMeshPro displayText;

public TextMeshPro ghostText;

[Header("Appearance")]

public Color digitColor = new Color(1f, 0.1f, 0.05f);

public float emissionIntensity = 3f;

private float _val;

void Start()

{

if (displayText) displayText.color = digitColor * emissionIntensity;

if (ghostText)

{

string g = new string('8', digitCount);

if (decimalPlaces > 0) g = g.Insert(digitCount-decimalPlaces, ".");

ghostText.text = g;

ghostText.color = digitColor * 0.08f;

}

}

public void SetValue(float value)

{

_val = Mathf.Clamp(value, minValue, maxValue);

if (!displayText) return;

string fmt = decimalPlaces > 0

? (leadingZeros ? new string('0', digitCount-decimalPlaces-1)+"0."+new string('0', decimalPlaces) : "F"+decimalPlaces)

: (leadingZeros ? new string('0', digitCount) : "F0");

string txt = Mathf.Abs(_val).ToString(fmt);

if (showSign) txt = (_val >= 0 ? "+" : "-") + txt;

displayText.text = txt;

}

}