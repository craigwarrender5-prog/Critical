# Section 4: Annunciator Window Tile

Part 5: Annunciator Window Tile

5.1 Real-World Reference

Annunciator windows are rectangular backlit alarm tiles in grid arrays across the top of every control panel section. They are the primary alarm notification system. When an alarm triggers, the window flashes rapidly with an audible horn. The operator acknowledges (ACK button) to stop flashing (steady-on, horn silenced). When the condition clears, the window slow-flashes until the operator resets it. A typical 4-Loop PWR has 500–1000 individual annunciator windows.

Each tile is a translucent plastic faceplate engraved with 2–3 lines of uppercase text (e.g., "PZR PRESS HI", "SG 1 LEVEL LO-LO"). The backlight is white or amber. The alarm sequence follows NUREG-0700 and ANSI/ISA-18.1: INACTIVE (dark) → ALERTING (fast flash + horn) → ACKNOWLEDGED (steady-on) → CLEARING (slow flash) → RESET (dark).

5.2 Specifications

Property

Specification

Tile Size

~2.75" W × 1.75" H per window

Grid Spacing

~0.125" gaps between tiles

Faceplate

Translucent white/amber plastic, engraved black text

Alert Flash Rate

2–4 Hz (fast flash)

Clear Flash Rate

0.5–1 Hz (slow flash)

Acknowledged

Steady-on (no flash)

Horn

Audible buzzer during ALERTING, silenced by ACK

5.3 Typical Annunciator Labels

Panel Section

Example Labels

Reactor / RCS

PZR PRESS HI, PZR LEVEL LO, T-AVG HI, RCS FLOW LO

Reactor Protection

REACTOR TRIP, ROD BOTTOM, NIS POWER RANGE HI

Steam Generators

SG 1 LEVEL HI, SG PRESS LO (SAFETY INJ)

CVCS

VCT LEVEL LO, CHARGING PUMP TRIP, LETDOWN ISOL

Containment

CNTMT PRESS HI-HI, CNTMT SUMP LEVEL HI

ESF

SAFETY INJECTION ACTUATED, CNTMT SPRAY ACTUATED

Electrical

4KV BUS UNDERVOLTAGE, DIESEL GEN START

5.4 Blender 5.0 Modeling

Model a single tile as a reusable prefab; instanced in Unity to build grid arrays.

Add Cube: X=7.0cm, Y=0.8cm, Z=4.45cm. Inset front face 0.15cm, extrude back -0.1cm. Dark frame material (#2A2A2A). Name "AnnunTile".

Add Plane faceplate: 6.5cm × 4.0cm, positioned at Y=0.32cm. Name "AnnunFace". UV Unwrap.

Create 256×128px alarm text texture: white background, black uppercase text, 2–3 lines. Apply to faceplate with Emission node (Strength=0.0 initial, driven by script).

Export as AnnunciatorTile.fbx.

TIP: Emission strength by state: INACTIVE=0.0, ALERTING/ACKNOWLEDGED=5.0–8.0, CLEARING oscillates 0.0–5.0.

5.5 Unity Setup

Import FBX, create prefab. Instantiate grid of tiles in a parent GameObject with 0.3cm gaps.

Each tile gets AnnunciatorTileDriver script linked to simulation alarm conditions.

Render via "AnnunCam" to 1024×256px RenderTexture (per row of ~8 tiles). Display on GUI canvas at top of panel.

5.6 C# Driver Script

using UnityEngine;

public enum AnnunciatorState { Inactive, Alerting, Acknowledged, Clearing }

public class AnnunciatorTileDriver : MonoBehaviour

{

public string alarmId;

public AnnunciatorState state = AnnunciatorState.Inactive;

public float alertFlashHz = 3f, clearFlashHz = 0.7f;

public float maxEmission = 6f;

public Color backlightColor = Color.white;

public Renderer faceplateRenderer;

public AudioSource hornAudio;

private MaterialPropertyBlock _mpb;

private float _t;

void Awake() { _mpb = new MaterialPropertyBlock(); }

public void TriggerAlarm()

{

if (state == AnnunciatorState.Inactive || state == AnnunciatorState.Clearing)

{ state = AnnunciatorState.Alerting; if (hornAudio) hornAudio.Play(); }

}

public void Acknowledge()

{

if (state == AnnunciatorState.Alerting)

{ state = AnnunciatorState.Acknowledged; if (hornAudio) hornAudio.Stop(); }

}

public void ConditionCleared()

{ if (state == AnnunciatorState.Acknowledged) state = AnnunciatorState.Clearing; }

public void Reset()

{ if (state == AnnunciatorState.Clearing) state = AnnunciatorState.Inactive; }

void Update()

{

_t += Time.deltaTime;

float em = 0f;

switch (state)

{

case AnnunciatorState.Inactive: em = 0f; break;

case AnnunciatorState.Alerting:

em = Mathf.Sin(_t * alertFlashHz * 2f * Mathf.PI) > 0 ? maxEmission : 0; break;

case AnnunciatorState.Acknowledged: em = maxEmission; break;

case AnnunciatorState.Clearing:

em = Mathf.Sin(_t * clearFlashHz * 2f * Mathf.PI) > 0 ? maxEmission : 0; break;

}

faceplateRenderer.GetPropertyBlock(_mpb);

_mpb.SetColor("_EmissionColor", backlightColor * em);

faceplateRenderer.SetPropertyBlock(_mpb);

}

}