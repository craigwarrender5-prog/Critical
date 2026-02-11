# Section 5: Indicator Lamp

Part 6: Indicator Lamp (Status Light)

6.1 Real-World Reference

Indicator lamps are the small round status lights embedded throughout every Westinghouse PWR control panel. The industry standard is the GE ET-16 indicating lamp, approximately 1.0–1.125 inches diameter, with a colored translucent dome lens (red, green, amber, white, or blue). Press-to-test capability verifies lamp function. Available in incandescent and LED retrofit models.

Lamps show binary component status: pump running (green) vs. stopped (red), valve open (green) vs. closed (red), breaker status, diesel generator status, ESF actuation. They are embedded in mimic bus diagrams – panel artwork showing simplified system schematics with lamps at component locations.

Color conventions per NUREG-0700: RED = danger, abnormal, trip; GREEN = normal, safe, running; AMBER = caution, transition; WHITE = neutral status; BLUE = bypass, maintenance.

6.2 Specifications

Property

Specification

Diameter

1.0–1.125" (25–29mm)

Shape

Round, domed translucent lens

Colors

Red, Green, Amber, White, Blue

Lamp Type

Incandescent (original) or LED (retrofit)

Push-to-Test

Press lens cap to verify function

Mounting

Panel cutout, snap-in/threaded collar

6.3 Blender 5.0 Modeling

Add Cylinder (32 vertices): X=2.86cm, Y=1.0cm, Z=2.86cm. Inset front face 0.15cm for bezel. Chrome material (#808080, Roughness 0.25, Metallic 1.0). Name "LampHousing".

Add UV Sphere, delete back hemisphere. Scale to ~2.5cm diameter dome. Position at Y=0.3cm. Name "LampLens".

Apply translucent material per color. Example RED: Base Color #CC0000, Roughness 0.15, Alpha 0.85, Blend Mode Alpha Blend. Emission Color #FF0000, Emission Strength 0.0 (driven by script).

Export as IndicatorLamp.fbx. Create Unity prefab variants for each color.

6.4 Unity Setup

Create prefab variants: RedLamp, GreenLamp, AmberLamp, WhiteLamp, BlueLamp.

For mimic panels: create 2D system schematic background sprite, place lamp prefabs at component locations.

Each lamp gets IndicatorLampDriver. Render via dedicated camera to RenderTexture (128×128 per lamp, or 1024×512 for a full mimic panel).

6.5 C# Driver Script

using UnityEngine;

public class IndicatorLampDriver : MonoBehaviour

{

public enum LampMode { Off, SteadyOn, Flashing }

public LampMode mode = LampMode.Off;

public Color lensColor = Color.red;

public float onEmission = 5f, flashHz = 2f;

public Renderer lensRenderer;

private MaterialPropertyBlock _mpb;

private float _t;

void Awake() { _mpb = new MaterialPropertyBlock(); }

public void SetState(bool on)

{ mode = on ? LampMode.SteadyOn : LampMode.Off; }

void Update()

{

_t += Time.deltaTime;

float em = mode switch

{

LampMode.Off => 0f,

LampMode.SteadyOn => onEmission,

LampMode.Flashing => Mathf.Sin(_t*flashHz*2f*Mathf.PI) > 0 ? onEmission : 0f,

_ => 0f

};

lensRenderer.GetPropertyBlock(_mpb);

_mpb.SetColor("_EmissionColor", lensColor * em);

lensRenderer.SetPropertyBlock(_mpb);

}

}