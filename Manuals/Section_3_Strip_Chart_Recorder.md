# Section 3: Strip Chart Recorder

Part 4: Strip Chart Recorder

4.1 Real-World Reference

Strip chart recorders provide continuous time-history traces of key parameters, enabling operators to detect slow drifts, monitor transients, and verify post-trip behavior. A typical Westinghouse 4-Loop PWR contains 15–25 strip chart recorders (Yokogawa, Honeywell, or ABB models). Multi-pen (2–4 pen) continuous paper recorders advance chart paper at 1–6 inches per hour (selectable), drawing colored traces on pre-printed grid paper.

Common applications: NR-45 (2-pen selectable NIS recorder), T-hot/T-cold trend recorders per loop, pressurizer pressure and level recorder, SG level trend recorders, containment pressure/temperature recorder, and steam/feedwater flow recorders. The visible chart window shows approximately 2–4 hours of recent data.

4.2 Specifications

Property

Specification

Visible Window

~6–8" W × 6–8" H (model dependent)

Chart Paper

Pre-printed grid, white, green/blue gridlines

Grid

10 major divisions, 5 minor subdivisions each

Pen Colors

Pen 1: Red, Pen 2: Blue, Pen 3: Green, Pen 4: Purple

Chart Speed

Selectable: 1, 2, 4, 6 in/hr (normal), up to 60 in/hr (fast)

Housing

Metal case, glass window, flush panel mount

4.3 Blender 5.0 Modeling

Add Cube for housing: X=20cm, Y=1cm, Z=20cm. Inset front face 0.8cm for frame, then 0.3cm for window bezel, extrude back -0.15cm. Dark gray metallic material (#2A2A2A, Roughness 0.5, Metallic 0.3). Name "RecorderBody".

Add Plane for chart paper: 17cm × 17cm inside window. White material (#FAFAF5, Roughness 0.95). Name "ChartPaper". UV Unwrap.

Create tileable grid texture: 512×512px, white background, light green (#C8E6C9) major gridlines every 51px, lighter green (#E8F5E9) minor lines every 10px.

Add label planes above (parameter name/legend) and on left/right edges (scale markings).

Pen traces are NOT modeled in Blender – they are generated dynamically in Unity (see C# script).

Export as StripChartRecorder.fbx. In Unity: dedicated layer, camera, 512×512px RenderTexture.

4.4 C# Driver Script

Creates a live scrolling strip chart by writing pen traces into a dynamic Texture2D:

using UnityEngine;

public class StripChartDriver : MonoBehaviour

{

[Header("Chart Config")]

public int texWidth = 512, texHeight = 512;

public float scrollSpeed = 2f;  // pixels/sec

public float sampleInterval = 0.5f;

[Header("Pen 1")]

public float pen1Min = 100f, pen1Max = 650f;

public Color pen1Color = Color.red;

[Header("Pen 2")]

public float pen2Min = 100f, pen2Max = 650f;

public Color pen2Color = Color.blue;

public Renderer chartRenderer;

public string texProperty = "_DetailAlbedoMap";

private Texture2D _tex;

private Color32[] _px;

private float _scrollAccum, _sampleTimer;

private float _v1, _v2;

public void SetPen1(float v) { _v1 = Mathf.Clamp(v, pen1Min, pen1Max); }

public void SetPen2(float v) { _v2 = Mathf.Clamp(v, pen2Min, pen2Max); }

void Start()

{

_tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);

_tex.filterMode = FilterMode.Point;

_px = new Color32[texWidth * texHeight];

var clr = new Color32(0,0,0,0);

for (int i = 0; i < _px.Length; i++) _px[i] = clr;

_tex.SetPixels32(_px); _tex.Apply();

chartRenderer.material.SetTexture(texProperty, _tex);

}

void Update()

{

_sampleTimer += Time.deltaTime;

_scrollAccum += scrollSpeed * Time.deltaTime;

while (_scrollAccum >= 1f) { _scrollAccum--; ScrollLeft(); }

if (_sampleTimer >= sampleInterval)

{

_sampleTimer = 0f;

DrawPen(texWidth-1, _v1, pen1Min, pen1Max, pen1Color);

DrawPen(texWidth-1, _v2, pen2Min, pen2Max, pen2Color);

_tex.SetPixels32(_px); _tex.Apply();

}

}

void ScrollLeft()

{

for (int y = 0; y < texHeight; y++)

for (int x = 0; x < texWidth-1; x++)

_px[y*texWidth+x] = _px[y*texWidth+x+1];

var c = new Color32(0,0,0,0);

for (int y = 0; y < texHeight; y++)

_px[y*texWidth+texWidth-1] = c;

}

void DrawPen(int col, float val, float mn, float mx, Color clr)

{

int row = Mathf.RoundToInt(Mathf.InverseLerp(mn, mx, val) * (texHeight-1));

row = Mathf.Clamp(row, 0, texHeight-1);

Color32 c = clr;

for (int d = -1; d <= 1; d++)

_px[Mathf.Clamp(row+d,0,texHeight-1)*texWidth+col] = c;

}

}