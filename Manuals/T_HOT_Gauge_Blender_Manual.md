# T-HOT Gauge Blender Manual

CRITICAL: Master the Atom

Westinghouse 4-Loop PWR Simulator

2.5D Temperature Gauge

Blender 5.0 to Unity 6 Instruction Manual

T_HOT — Hot Leg Temperature Indicator

Range: 100°F – 650°F

Revision 1.0 — February 2026

For use with Blender 5.0.1 and Unity 6.3 (6000.1.x)

Table of Contents

Part 0 — Overview and Context

0.1 What We Are Building

This manual guides you through creating a 2.5D temperature gauge in Blender 5.0 and importing it into Unity 6 for use on the Reactor Manual Operation GUI (Key 1). The gauge replicates the T_HOT (Hot Leg Temperature) indicator found on a real Westinghouse 4-Loop PWR main control board.

The term “2.5D” means the gauge has real 3D geometry with depth, bevels, and dimensionality, but is designed to be viewed from a fixed front-facing camera angle. This gives it a premium, physically realistic look compared to a flat 2D texture, while remaining lightweight enough for a UI overlay.

0.2 What the Real Gauge Looks Like

On a Westinghouse 4-Loop PWR main control board, temperature indicators for T_HOT, T_COLD, and T_AVG are typically round analog gauges, approximately 6 inches in diameter with the following features:

•  A dark (usually black or dark grey) face plate with white scale markings and numerals.

•  A chrome or brushed-metal circular bezel (the outer ring).

•  A red or orange needle/pointer that sweeps from roughly the 7 o’clock position (minimum) to the 5 o’clock position (maximum), covering approximately 270 degrees of arc.

•  Scale range for T_HOT: 100°F to 650°F, with major divisions every 50°F and minor tick marks every 10°F.

•  A label reading “T-HOT °F” centred below the needle pivot.

•  A glass cover with subtle reflections.

0.3 Parts We Will Model

Part

Geometry

Material

Notes

Bezel (outer ring)

Torus / extruded circle

Brushed chrome metal

Gives the gauge its frame

Face plate

Cylinder (very thin)

Dark matte grey/black

Background behind markings

Scale markings

Thin rectangular prisms

White emissive

Major + minor tick marks

Numerals

3D text or texture

White emissive

100, 150, 200 ... 650

Needle

Thin tapered wedge

Red/orange emissive

This is the animated part

Needle hub

Small cylinder

Chrome metal

Centre cap over the needle pivot

Glass cover

Slightly convex disc

Transparent + glossy

Adds realism with reflection

Back plate

Cylinder

Dark metal

Seals the back, adds depth

0.4 Software Requirements

Software

Version

Download

Blender

5.0.1 or later

blender.org/download

Unity

6.3 (6000.1.x) or later

unity.com/download

Unity FBX Importer

Built-in (Package Manager)

Included with Unity

Part 1 — Blender 5.0 Essentials for Beginners

1.1 First Launch

Step 1: Download and install Blender 5.0.1 from blender.org/download.

Step 2: Launch Blender. You will see a splash screen. Click anywhere outside the splash to dismiss it.

Step 3: You are now in the default scene containing a cube, a camera, and a light. We will delete all three shortly.

1.2 Understanding the Interface

Blender’s interface has several key areas. Here is what you see on the default screen:

Area

Location

Purpose

3D Viewport

Centre (largest area)

Where you model, rotate, and view your 3D objects

Outliner

Top-right

Lists every object in your scene (like a file explorer for 3D objects)

Properties Panel

Bottom-right

Settings for the selected object: materials, physics, render, etc.

Timeline

Bottom strip

Used for animation keyframes (we will use this later)

Header / Top Bar

Very top

File menu, workspace tabs (Layout, Modeling, Sculpting, etc.)

Toolbar

Left side of 3D Viewport

Quick-access tools (move, rotate, scale, etc.)

1.3 Essential Navigation (Mouse + Keyboard)

Blender relies heavily on keyboard shortcuts. These are the ones you absolutely must know:

Viewport Navigation

Middle Mouse Button (MMB) drag — Orbit (rotate) the view around the scene

Shift + MMB drag — Pan (slide) the view left/right/up/down

Scroll wheel — Zoom in and out

Numpad 1 — Front view (looking at the front of your object)

Numpad 3 — Right side view

Numpad 7 — Top-down view

Numpad 5 — Toggle perspective / orthographic view

Numpad 0 — Camera view (what the camera sees)

Home key — Zoom to fit everything in the viewport

TIP: If you don’t have a numpad, go to Edit > Preferences > Input and check “Emulate Numpad”. This maps numpad keys to the number row.

Object Operations

Left click — Select an object

A — Select all / Deselect all (toggle)

X or Delete — Delete selected object (confirm in popup)

G — Grab (move) — press X, Y, or Z after to constrain to an axis

R — Rotate — press X, Y, or Z after to constrain to an axis

S — Scale — press X, Y, or Z after to constrain to an axis

Shift + A — Add menu (add new objects: mesh, curve, text, etc.)

Tab — Toggle between Object Mode and Edit Mode

Ctrl + Z — Undo (works for almost everything)

Ctrl + S — Save your file

Edit Mode Operations

1, 2, 3 (top row) — Switch between Vertex, Edge, Face select mode

E — Extrude selected faces/edges/vertices

I — Inset faces (creates a smaller face inside a selected face)

Ctrl + R — Loop cut (adds a ring of edges around geometry)

S then number — Scale by exact amount (e.g., S then 0.5 then Enter = half size)

WARNING: Always make sure you know which MODE you are in. The mode selector is in the top-left of the 3D Viewport. “Object Mode” is for moving whole objects. “Edit Mode” is for modifying the mesh geometry of a single object.

1.4 Clean Up the Default Scene

Step 1: Press A to select all objects (cube, camera, light).

Step 2: Press X and confirm “Delete” in the popup.

Step 3: You now have a completely empty scene. This is our starting point.

Step 4: Press Ctrl + S and save the file as T_HOT_Gauge.blend in your project folder.

TIP: Save frequently. Blender can crash, and losing work is painful. Use Ctrl+S often.

Part 2 — Modelling the Gauge in Blender

2.1 Set Up Your Workspace

Step 1: Press Numpad 1 to switch to Front View.

Step 2: Press Numpad 5 to switch to Orthographic mode (removes perspective distortion; good for precision work).

Step 3: We will model everything centred at the origin (0, 0, 0). The gauge face will be on the XY plane, with depth going along the Z axis (towards you).

2.2 Create the Back Plate

The back plate is a thin cylinder that forms the rear of the gauge housing.

Step 1: Press Shift + A to open the Add menu.

Step 2: Navigate to Mesh > Cylinder.

Step 3: IMMEDIATELY after adding the cylinder, look at the bottom-left of the 3D viewport. You will see a small floating panel labelled “Add Cylinder”. Click it to expand if needed.

Step 4: Set the following values in that panel:

Parameter

Value

Why

Vertices

64

Smooth circular shape (default 32 looks slightly faceted)

Radius

1.0

This defines our gauge as 2 Blender units in diameter

Depth

0.05

Very thin disc — this is the back plate

Location Z

-0.05

Pushes it slightly behind the origin

Step 5: In the Outliner (top-right), double-click on the object name “Cylinder” and rename it to BackPlate.

TIP: Always name your objects as you create them. With 8+ objects, unnamed “Cylinder.001”, “Cylinder.002” etc. becomes confusing fast.

2.3 Create the Face Plate

The face plate is a slightly thinner disc that sits in front of the back plate. This is the dark surface behind the scale markings.

Step 1: Press Shift + A > Mesh > Cylinder.

Step 2: In the Add Cylinder panel, set:

Parameter

Value

Vertices

64

Radius

0.92

Depth

0.02

Location Z

0.0

Step 3: Rename this object to FacePlate.

The face plate has a smaller radius (0.92 vs 1.0) so there is a visible ring of the back plate / bezel around it.

2.4 Create the Bezel (Outer Ring)

The bezel is the chrome ring around the outside edge of the gauge. We will use a Torus for this.

Step 1: Press Shift + A > Mesh > Torus.

Step 2: In the Add Torus panel, set:

Parameter

Value

Why

Major Segments

64

Smooth circle

Minor Segments

16

Smooth cross-section of the ring tube

Major Radius

1.0

Matches the overall gauge diameter

Minor Radius

0.08

Thickness of the bezel ring tube

Step 3: The torus is created lying flat. We need it to sit at Z = 0.01 (just in front of the face plate). Press G, then Z, then type 0.01, then Enter.

Step 4: Rename this object to Bezel.

2.5 Create the Scale Markings (Tick Marks)

This is the most involved step. We need to create tick marks arranged in an arc from approximately 225° (7 o’clock) to -45° (5 o’clock), covering 270° of arc. There are 11 major marks (every 50°F from 100 to 650) and 55 minor marks (every 10°F).

2.5.1 Create the Major Tick Template

Step 1: Press Shift + A > Mesh > Cube.

Step 2: Press S to scale, then type 0.01 and press Enter. This makes a tiny cube.

Step 3: Now we will scale it non-uniformly. Press S, then X, then type 3, then Enter. This stretches it into a thin rectangle along the X axis.

Step 4: Press S, then Y, then type 0.3, then Enter. This flattens it in Y.

Step 5: Press S, then Z, then type 0.5, then Enter. This gives it a bit of depth.

Step 6: You should now have a small elongated rectangular shape — about 0.06 units long in X, thin in Y and Z. This is one major tick mark.

Step 7: Rename it to MajorTick.

2.5.2 Position the First Tick

The tick marks live near the rim of the face plate. The first tick (100°F) needs to be at the 225° position (7 o’clock). The centre of the gauge is at the origin.

Step 1: Move the tick to the rim: Press G, then X, then type 0.78, then Enter. This places it near the inner edge of the bezel.

Step 2: We now need to rotate it around the gauge centre. The 225° position means we rotate the tick 225° around the Z axis, but using the 3D cursor as pivot.

Step 3: First, make sure the 3D cursor is at the origin. Press Shift + S and choose “Cursor to World Origin”.

Step 4: Set the pivot point to 3D Cursor: In the header of the 3D Viewport, find the pivot point dropdown (it shows a dot icon by default, labelled “Median Point”). Change it to “3D Cursor”.

Step 5: With MajorTick selected, press R, then Z, then type 225, then Enter. The tick mark is now at the 7 o’clock position, 0.78 units from centre.

Step 6: The tick should also be oriented radially (pointing towards centre). Press R, then Z, then type 225, then Enter, but this time make sure you are in Object Mode and using Individual Origins as pivot. Actually, a simpler approach: select the tick and press Ctrl + A > Rotation to apply the rotation.

TIP: This is getting complex for a first-timer. An easier approach follows below using the Array + Empty method.

2.5.3 Easier Method: Array Modifier with Empty

Blender has a powerful trick for arranging objects in a circle: the Array modifier combined with an Empty object set to rotate. This lets you create all tick marks from a single object.

Step 1: First, undo everything from 2.5.1 and 2.5.2 (Ctrl+Z until you are back to just the FacePlate, BackPlate, and Bezel).

We will start fresh with a cleaner method:

Step 2: Create the Empty (rotation driver): Press Shift + A > Empty > Plain Axes. Rename it to TickRotator. Make sure it is at the origin (0, 0, 0).

Step 3: We need 56 tick marks total (55 minor intervals across 270°). Each interval is 270/55 = 4.909°. Set the TickRotator’s Z rotation: In the Properties panel on the right, expand the “Object Properties” tab (orange square icon). Under Transform > Rotation Z, type: 4.909

Step 4: Create the tick mark: Press Shift + A > Mesh > Cube.

Step 5: Scale it into a thin rectangle: Press S, then type 0.006, then Enter. Then press S, X, type 4, Enter. Then press S, Z, type 0.3, Enter. You now have a thin elongated bar.

Step 6: Move it to the rim: Press G, then Y, then type 0.8, then Enter. The tick is now 0.8 units above centre.

Step 7: Rename it to TickMark.

Step 8: Apply the Array modifier: With TickMark selected, go to the Properties panel > Modifiers tab (wrench icon). Click “Add Modifier” > Array.

Step 9: In the Array modifier settings, set:

Setting

Value

Fit Type

Fixed Count

Count

56

Relative Offset

UNCHECK this (turn it off)

Object Offset

CHECK this (turn it on)

Object

TickRotator (select from the dropdown)

Step 10: You should now see 56 tick marks arranged in a circular arc. They start from the top (12 o’clock) and sweep 270° around.

Step 11: Rotate the whole thing so tick #1 starts at 7 o’clock (225°): Select the TickMark object. Set the pivot point to “3D Cursor” (which should be at origin). Press R, Z, type 135, Enter. This rotates the starting position to approximately the 7 o’clock position.

Step 12: Apply the Array modifier: With TickMark selected, hover over the Array modifier in the Properties panel and press Ctrl + A (or click the dropdown arrow > Apply). The tick marks are now real geometry.

NOTE: After applying the Array modifier, all 56 ticks are part of one mesh. Every 5th tick is a major mark (corresponding to 50°F intervals). We will differentiate major vs minor ticks using materials or by scaling them after the fact. For now, this gives you the correct positioning.

2.5.4 Differentiate Major and Minor Ticks

Step 1: With TickMark selected, press Tab to enter Edit Mode.

Step 2: Press 3 (top row) to switch to Face Select mode.

Step 3: You need to select every 5th tick (these are the major marks at 100, 150, 200... 650°F). Hold Ctrl and click on the faces of every 5th tick mark (the 1st, 6th, 11th, 16th, 21st, 26th, 31st, 36th, 41st, 46th, 51st ticks — 11 total).

Step 4: Once selected, press S, then the axis perpendicular to the face length (this will be context-dependent on orientation), then type 1.8, then Enter. This scales those ticks to be ~1.8x longer than the minor ticks.

Step 5: Press Tab to return to Object Mode.

TIP: If selecting every 5th tick manually feels tedious, you can instead create two separate tick arrays: one with 11 ticks at 27° spacing (majors) and one with 44 ticks at the remaining positions (minors), each with different lengths. Use the same TickRotator Empty approach but with different rotation values.

2.6 Create the Numerals

The numerals (100, 150, 200... 650) sit just inside the major tick marks. There are 11 numerals. We will use Blender’s Text object and then convert to mesh.

Step 1: Switch to Front View (Numpad 1) and Orthographic (Numpad 5).

Step 2: Press Shift + A > Text.

Step 3: A text object labelled “Text” appears. Press Tab to enter Edit Mode for the text.

Step 4: Select all the default text and delete it. Type: 100

Step 5: Press Tab to exit Edit Mode.

Step 6: With the text selected, go to Properties panel > Object Data Properties (the “A” icon that looks like a font character).

Step 7: Under Font, you can leave the default (BFont) or load a cleaner sans-serif font. Under Geometry > Extrude, set to 0.005 to give the text slight depth.

Step 8: Set the text Size to 0.08 in the same panel.

Step 9: Under Paragraph > Alignment, set Horizontal to “Center”.

Step 10: Position this text at the correct location for the “100” label: move it to approximately (X: -0.48, Y: -0.48, Z: 0.01), which is the inner portion near the 7 o’clock major tick.

Step 11: Rotate the text to face outward/be readable: Press R, Z, and rotate to match the angle of the 100°F tick mark.

Step 12: Repeat steps 2–11 for each numeral: 150, 200, 250, 300, 350, 400, 450, 500, 550, 600, 650. Position each at the corresponding major tick mark location.

TIP: This is 11 text objects to create and position manually. It is tedious but straightforward. Once you have all 11, select them all, right-click and choose “Convert To > Mesh”. This turns the text curves into mesh geometry, which exports more reliably to FBX.

Step 13: Add the label. Create one more text object reading “T-HOT °F”. Set size to 0.06, centre it horizontally, and position it at approximately (0, -0.35, 0.01) — centred below the needle pivot. Rename it to Label_T_HOT.

2.7 Create the Needle

The needle is the part that will be animated in Unity. It must be a separate object with its pivot (origin) at the centre of the gauge.

Step 1: Press Shift + A > Mesh > Cube.

Step 2: Press Tab to enter Edit Mode.

Step 3: We need a tapered shape — wide at the pivot and thin at the tip. Press 1 to switch to Vertex Select mode.

Step 4: The default cube has 8 vertices. Select the top 4 vertices (the ones at +Y). Press S, X, type 0.3, Enter. This narrows the top, creating a taper.

Step 5: Press Tab to return to Object Mode.

Step 6: Now scale the whole object: Press S, X, type 0.015, Enter. Press S, Y, type 0.7, Enter. Press S, Z, type 0.008, Enter.

Step 7: This gives you a thin, flat, tapered pointer about 0.7 units long.

Step 8: Move the needle so its BASE (wide end) is at the origin and it extends upward: Press G, Y, type 0.35, Enter.

Step 9: CRITICAL: Set the origin to the base of the needle. Go to the header menu: Object > Set Origin > Origin to 3D Cursor. Since the 3D cursor is at the world origin (0,0,0), and the base of the needle is near the origin, this sets the pivot point at the gauge centre. The needle will rotate around this point.

WARNING: The needle’s origin (pivot point) MUST be at the centre of the gauge (world origin), not at the centre of the needle mesh. If you get this wrong, the needle will orbit weirdly in Unity instead of sweeping like a real gauge pointer.

Step 10: Move the needle slightly forward in Z so it sits above the face plate: Press G, Z, type 0.015, Enter.

Step 11: Rename this object to Needle.

2.8 Create the Needle Hub (Centre Cap)

Step 1: Press Shift + A > Mesh > Cylinder.

Step 2: Set: Vertices: 32, Radius: 0.05, Depth: 0.03, Location Z: 0.02.

Step 3: Rename to NeedleHub.

2.9 Create the Glass Cover

The glass gives the gauge a realistic look — a slightly convex transparent disc in front of everything.

Step 1: Press Shift + A > Mesh > UV Sphere.

Step 2: Set: Segments: 32, Rings: 16, Radius: 0.95.

Step 3: This creates a full sphere. We only want the front cap. Press Tab to enter Edit Mode.

Step 4: Press 3 for Face Select. Press A to select all faces.

Step 5: Switch to Right view (Numpad 3). Use Box Select (B key, then drag) to deselect the front-facing cap. We want to DELETE everything except the front hemisphere/cap.

Step 6: Actually, an easier approach: switch back to Front view. Press A to deselect all. Then use Circle Select (C key) or Box Select (B key) to select only the back half of the sphere (everything at Z < 0). Press X > Faces to delete them.

Step 7: Now flatten the remaining cap: press Tab to exit Edit Mode. Press S, Z, type 0.15, Enter. This squishes the hemisphere into a subtle dome.

Step 8: Position it at Z = 0.025 (just in front of everything else): Press G, Z, type 0.025, Enter.

Step 9: Rename to GlassCover.

TIP: If the sphere approach is too complex, you can also just use a cylinder with Vertices: 64, Radius: 0.94, Depth: 0.005 and skip the dome. A flat glass disc still looks good.

2.10 Verify Your Object List

Check the Outliner panel. You should have these objects:

Object Name

Type

BackPlate

Cylinder

FacePlate

Cylinder

Bezel

Torus

TickMark

Cube (arrayed, applied)

(11 numeral objects)

Text converted to Mesh

Label_T_HOT

Text converted to Mesh

Needle

Cube (tapered)

NeedleHub

Cylinder

GlassCover

Sphere (modified)

TickRotator

Empty (can delete now)

Step 1: You can safely delete the TickRotator empty now — select it and press X.

Step 2: Press Ctrl + S to save.

Part 3 — Materials and Appearance

3.1 How Materials Work in Blender

Each object can have one or more materials that define its colour, shininess, transparency, etc. When we export to Unity, these materials come along and we can either use them or replace them with Unity materials.

To add a material to an object: Select the object. Go to the Properties panel > Material Properties tab (the sphere icon). Click “New”.

3.2 Material Assignments

Create and assign the following materials. For each one, select the object, create a New material, rename it, and set the values in the “Surface” section (which defaults to Principled BSDF):

Object

Material Name

Base Color

Metallic

Roughness

Other

BackPlate

MAT_DarkMetal

Hex: 1A1A1A

0.9

0.5

—

FacePlate

MAT_GaugeFace

Hex: 0D0D0D

0.0

0.8

Nearly pure black, matte

Bezel

MAT_Chrome

Hex: C0C0C0

1.0

0.15

Mirror-like chrome

TickMark

MAT_WhiteEmissive

Hex: FFFFFF

0.0

0.5

Emission: 0.5 (faint glow)

Numerals

MAT_WhiteEmissive

Same as ticks

—

—

Reuse the same material

Label_T_HOT

MAT_WhiteEmissive

Same as ticks

—

—

Reuse the same material

Needle

MAT_NeedleRed

Hex: CC2200

0.0

0.4

Emission: 1.0 (glowing red)

NeedleHub

MAT_Chrome

Same as Bezel

—

—

Reuse

GlassCover

MAT_Glass

Hex: FFFFFF

0.0

0.05

Alpha: 0.08, see below

3.2.1 Setting Up the Glass Material

Step 1: Select the GlassCover object.

Step 2: In Material Properties, click New. Rename to MAT_Glass.

Step 3: Set Base Color to white (FFFFFF).

Step 4: Set Roughness to 0.05 (very glossy).

Step 5: Set Alpha to 0.08 (nearly fully transparent).

Step 6: CRITICAL: Under the material’s Settings section (scroll down in the material panel), find “Blend Mode” and change it from “Opaque” to “Alpha Blend”.

Step 7: Also in Settings, check “Backface Culling” so you only see the front face of the glass.

3.3 Preview Your Gauge

Step 1: In the 3D Viewport header, find the viewport shading buttons (four circles in the top-right of the viewport). Click the rightmost one — “Material Preview” (or press Z and select Material Preview).

Step 2: You should now see your gauge with colours and materials applied. The black face plate with white ticks, chrome bezel, and red needle should be clearly visible.

Step 3: Orbit around (MMB drag) to verify everything looks correct from the front.

Step 4: Press Ctrl + S to save.

Part 4 — Final Cleanup Before Export

4.1 Smooth Shading

Cylinders and tori look faceted by default. We need to set Smooth Shading on the curved objects.

Step 1: Select BackPlate. Right-click > Shade Smooth.

Step 2: Repeat for FacePlate, Bezel, NeedleHub, and GlassCover.

Step 3: Do NOT smooth-shade the TickMark or Needle objects — they should remain flat-shaded (sharp edges look correct for these).

4.2 Apply All Transforms

WARNING: This step is CRITICAL for a clean export to Unity. If you skip this, your objects may appear at wrong scales, rotations, or positions in Unity.

Step 1: Press A to select all objects.

Step 2: Press Ctrl + A and choose “All Transforms”. This bakes the current position, rotation, and scale into the mesh data and resets the object transforms to identity (Location 0, Rotation 0, Scale 1).

4.3 Parent All to an Empty (Gauge Root)

For clean organisation in Unity, we want all gauge parts to be children of a single parent object.

Step 1: Press Shift + A > Empty > Plain Axes. Rename it to GaugeRoot. Position at (0, 0, 0).

Step 2: Select ALL gauge objects (BackPlate, FacePlate, Bezel, TickMark, all numerals, Label, Needle, NeedleHub, GlassCover) but NOT the GaugeRoot empty yet.

Step 3: Now ALSO select GaugeRoot (Shift + click it). It must be the LAST thing selected — this makes it the parent.

Step 4: Press Ctrl + P > Object. This parents all selected objects to the GaugeRoot empty.

Step 5: In the Outliner, you should now see GaugeRoot with all parts nested under it as children. The Needle should be a direct child of GaugeRoot (not nested under anything else) because Unity will rotate the Needle independently.

Step 6: Press Ctrl + S to save.

Part 5 — Exporting from Blender 5.0

5.1 FBX Export Settings

Step 1: Go to File > Export > FBX (.fbx).

Step 2: In the file browser that opens, navigate to your Unity project’s Assets folder. Create a subfolder called Models if one doesn’t exist. Example path: C:\Users\craig\Projects\Critical\Assets\Models\

Step 3: Set the filename to T_HOT_Gauge.fbx.

Step 4: In the export settings panel on the right side, configure:

Setting

Value

Why

Selected Objects

Check this ON

Only export our gauge, not hidden junk

Scale

1.0

Keeps Blender units = Unity units (1 = 1 metre)

Apply Scalings

FBX All

Bakes all scale transforms into the FBX

Forward

-Z Forward

Corrects Blender’s Z-up to Unity’s coordinate system

Up

Y Up

Unity uses Y as the up axis

Apply Transform

Check this ON

Critical: prevents rotation issues in Unity

Mesh > Smoothing

Face

Preserves our smooth/flat shading choices

Mesh > Apply Modifiers

Check this ON

Bakes any remaining modifiers

Armature

Uncheck (not needed)

We have no skeleton/bones

Animation

Uncheck for now

We will animate in Unity, not Blender

Step 5: Click “Export FBX”.

WARNING: Before clicking Export, make sure you have all objects selected in the viewport (press A), because we checked “Selected Objects”. If nothing is selected, nothing will export.

5.2 Save an Operator Preset

So you do not have to set all these options again every time:

Step 1: Before clicking Export, click the + button next to the presets dropdown at the top of the export panel.

Step 2: Name it “Unity FBX Export”.

Step 3: Next time you export, just select this preset and all settings will be pre-filled.

Part 6 — Importing into Unity 6

6.1 Import the FBX

Step 1: Open your Unity project (Critical).

Step 2: If you exported directly into Assets/Models/, Unity will have already detected the file. Switch to Unity and wait for the import progress bar to finish.

Step 3: If you exported elsewhere, drag and drop the T_HOT_Gauge.fbx file into the Assets/Models/ folder in the Unity Project window.

Step 4: Click on the imported T_HOT_Gauge asset in the Project window. The Inspector will show Model Import Settings.

6.2 Import Settings

Step 1: In the Model tab of Import Settings:

Setting

Value

Why

Scale Factor

1

If your gauge appears tiny, try 100

Convert Units

Checked

Converts Blender metres to Unity units

Import BlendShapes

Unchecked

Not needed

Import Visibility

Checked

Preserves visibility states

Import Cameras

Unchecked

We deleted the camera in Blender

Import Lights

Unchecked

We deleted the light in Blender

Step 2: Click Apply at the bottom of the Inspector.

6.3 Extract and Fix Materials

Step 1: Click the Materials tab in the Import Settings.

Step 2: Under Material Creation Mode, select “Import via MaterialDescription”.

Step 3: Click “Extract Materials” and save them to a Materials subfolder (e.g., Assets/Models/Materials/).

Step 4: Now select each extracted material in the Project window and adjust its Shader and properties in the Inspector. Unity’s default URP Lit shader works well:

Blender Material

Unity Shader

Key Settings

MAT_Chrome

URP/Lit

Metallic: 1.0, Smoothness: 0.85, Color: C0C0C0

MAT_DarkMetal

URP/Lit

Metallic: 0.9, Smoothness: 0.5, Color: 1A1A1A

MAT_GaugeFace

URP/Lit

Metallic: 0, Smoothness: 0.2, Color: 0D0D0D

MAT_WhiteEmissive

URP/Lit

Emission: FFFFFF at intensity 0.5

MAT_NeedleRed

URP/Lit

Emission: CC2200 at intensity 1.0, Color: CC2200

MAT_Glass

URP/Lit

Surface Type: Transparent, Alpha: 0.08, Smoothness: 0.95

Step 5: Click Apply on each material after editing.

6.4 Place the Gauge in the Scene

Step 1: Drag the T_HOT_Gauge prefab from the Project window into the Hierarchy or Scene view.

Step 2: Verify the gauge appears correctly. If it is rotated oddly, select the root object and set Rotation to (0, 0, 0).

Step 3: If scale is wrong (too large or too small), adjust the Scale Factor in the Import Settings and click Apply again. A common fix is to set Scale Factor to 100 if the gauge appears as a tiny dot.

Part 7 — Animating the Needle in Unity

7.1 Understanding the Animation Approach

The needle needs to rotate around the Z axis (the axis pointing out of the gauge face) based on the T_HOT temperature value from the simulation. We have two choices:

•  Script-driven rotation (recommended): A C# script reads the current T_HOT value and sets the needle’s rotation. This is simple, direct, and integrates naturally with your existing simulation engine.

•  Unity Animator with animation clips: Useful for pre-baked visual flourishes (startup sweep, oscillation) but overkill for a value-driven indicator.

We will use the script-driven approach as it connects directly to your physics engine’s output values.

7.2 The Rotation Mapping

The needle sweeps 270° of arc across the temperature range 100°F to 650°F:

Temperature (°F)

Needle Angle (° from 12 o’clock)

Gauge Position

100 (minimum)

225° (or -135°)

7 o’clock

375 (midpoint)

90° (or -270°)

12 o’clock (top centre)

650 (maximum)

-45° (or 315°)

5 o’clock

The mapping formula is:

float fraction = (temperature - 100f) / (650f - 100f);   // 0 to 1

float angleDeg = 225f - (fraction * 270f);               // 225° to -45°

needleTransform.localRotation = Quaternion.Euler(0, 0, angleDeg);

7.3 Create the GaugeNeedleDriver Script

Create a new C# script in your Unity project. Place it in Assets/Scripts/UI/ (or wherever your GUI scripts live).

Here is the complete script:

using UnityEngine;

/// <summary>

/// Drives a 2.5D gauge needle rotation based on a float value.

/// Attach to the Needle child object of the imported gauge.

/// </summary>

public class GaugeNeedleDriver : MonoBehaviour

{

[Header("Gauge Range")]

[Tooltip("Minimum value on the gauge scale")]

public float minValue = 100f;

[Tooltip("Maximum value on the gauge scale")]

public float maxValue = 650f;

[Header("Sweep Angles (degrees from 12 o'clock, clockwise positive)")]

[Tooltip("Angle at minimum value (7 o'clock = 225)")]

public float angleAtMin = 225f;

[Tooltip("Angle at maximum value (5 o'clock = -45)")]

public float angleAtMax = -45f;

[Header("Smoothing")]

[Tooltip("How quickly the needle tracks the target (higher = faster)")]

[Range(1f, 20f)]

public float smoothSpeed = 5f;

// Current displayed value (smoothed)

private float _currentAngle;

/// <summary>

/// Call this from your simulation to update the gauge.

/// </summary>

public void SetValue(float temperature)

{

float clamped = Mathf.Clamp(temperature, minValue, maxValue);

float fraction = (clamped - minValue) / (maxValue - minValue);

float targetAngle = Mathf.Lerp(angleAtMin, angleAtMax, fraction);

_currentAngle = Mathf.Lerp(_currentAngle, targetAngle,

Time.deltaTime * smoothSpeed);

transform.localRotation = Quaternion.Euler(0f, 0f, _currentAngle);

}

private void Start()

{

_currentAngle = angleAtMin; // Start at minimum

transform.localRotation = Quaternion.Euler(0f, 0f, _currentAngle);

}

}

7.4 Attach the Script

Step 1: In the Hierarchy, expand the T_HOT_Gauge object until you find the Needle child object.

Step 2: Select the Needle object.

Step 3: Drag the GaugeNeedleDriver script onto it (or use Add Component > GaugeNeedleDriver).

Step 4: In the Inspector, verify the default values: Min Value: 100, Max Value: 650, Angle At Min: 225, Angle At Max: -45, Smooth Speed: 5.

7.5 Test It

To test without the full simulation running, you can add a temporary test script or modify GaugeNeedleDriver to include an Update test:

// Add this temporarily to GaugeNeedleDriver for testing:

[Header("Debug")]

public bool testMode = false;

[Range(100f, 650f)]

public float testTemperature = 100f;

private void Update()

{

if (testMode) SetValue(testTemperature);

}

Check “Test Mode” in the Inspector, enter Play mode, and drag the Test Temperature slider. The needle should sweep smoothly across the gauge face.

Part 8 — Integrating with the Reactor GUI

8.1 Placement on the Manual Operation Panel

The Reactor Manual Operation GUI is activated by pressing Key 1. The gauge needs to appear as part of this panel’s visual layout.

8.1.1 Option A: World-Space UI (Recommended for 2.5D)

Since the gauge is a 3D model, it looks best rendered in 3D space rather than as a flat UI overlay. The approach is:

Step 1: Position the gauge as a child of the panel’s 3D layout (if the GUI exists as a 3D panel in world space), or place it in front of a dedicated camera.

Step 2: Create a dedicated Render Texture camera that views only the gauge (using Layer filtering).

Step 3: Display the Render Texture on a Raw Image UI element on the reactor GUI canvas.

8.1.2 Option B: Direct 3D Placement

If your reactor panel is a 3D object in the scene, you can simply parent the gauge to the panel and position it at the appropriate location (where the T_HOT indicator sits on the control board).

8.2 Render Texture Approach (Detailed Steps)

Step 1: Create a Render Texture: In the Project window, right-click > Create > Render Texture. Name it RT_GaugeT_HOT. Set resolution to 512 x 512 (sufficient for a gauge).

Step 2: Create a Gauge Camera: In the Hierarchy, right-click > Camera. Name it GaugeCamera_T_HOT.

Step 3: Position the camera directly in front of the gauge, looking at it. Set Projection to Orthographic. Adjust Orthographic Size so the gauge fills the camera view with a small margin.

Step 4: Set the camera’s Target Texture to RT_GaugeT_HOT.

Step 5: Set the camera’s Culling Mask to a dedicated layer (e.g., create a layer called “GaugeRender”). Move all gauge objects to this layer. This prevents the gauge from appearing in the main camera and the main scene from appearing in the gauge camera.

Step 6: Set the Background Type to Solid Color and make it transparent (RGBA: 0, 0, 0, 0) so the gauge has no background.

Step 7: On the Reactor GUI Canvas, add a UI > Raw Image element where the T_HOT gauge should appear. Set its Texture to RT_GaugeT_HOT.

Step 8: Size the Raw Image to match the gauge’s desired screen size on the panel.

8.3 Connecting to the Simulation

Your existing simulation engine updates temperature values every frame. You need to call GaugeNeedleDriver.SetValue() with the current T_HOT value.

In whatever script manages the GUI updates (likely your ReactorGUIManager or ManualOperationPanel), add a reference to the gauge:

[SerializeField] private GaugeNeedleDriver tHotGauge;

Then, in the update loop where you already update text displays, add:

tHotGauge.SetValue(currentState.T_hot);

Where currentState.T_hot is whatever field holds the hot leg temperature in your current simulation state struct or class.

8.4 Lighting the Gauge

For the gauge to look good in isolation (rendered by its own camera), add a small lighting setup:

Step 1: Add a Point Light or Spot Light near the gauge camera, aimed at the gauge face. Set intensity to something subtle. Set this light to the same “GaugeRender” layer so it only affects the gauge.

Step 2: A second fill light at lower intensity from the side adds depth and makes the chrome bezel gleam.

Step 3: Experiment with light colour — a slightly warm white (hex: FFF5E0) mimics the incandescent control room lighting found in real Westinghouse control rooms.

Part 9 — Polish and Realism Details

9.1 Add Needle Oscillation (Optional)

Real gauge needles have a slight oscillation due to sensor noise and fluid turbulence. You can add this for realism:

// In GaugeNeedleDriver.SetValue(), add noise:

float noise = Mathf.PerlinNoise(Time.time * 2f, 0f) - 0.5f;

float noiseAmplitude = 0.3f; // degrees

_currentAngle += noise * noiseAmplitude;

9.2 Add a Red Danger Zone

On a real T_HOT gauge, the region above ~620°F is often marked with a red arc to indicate approach to design limits. You can model this as a thin red arc mesh in Blender, or add it as a texture on the face plate.

9.3 Make It Reusable for Other Gauges

The same gauge model and GaugeNeedleDriver script can be reused for T_COLD, T_AVG, T_PZR, and pressure gauges by simply changing the minValue, maxValue, and label. To make variants:

Step 1: Duplicate the gauge prefab in Unity.

Step 2: Change the GaugeNeedleDriver values on the new instance.

Step 3: For the label, either swap the Label_T_HOT text mesh for a new one, or use a TextMeshPro component in Unity positioned over the gauge face.

Gauge

Min Value

Max Value

Unit

T_HOT (Hot Leg)

100

650

°F

T_COLD (Cold Leg)

100

650

°F

T_AVG (Average)

100

650

°F

T_PZR (Pressurizer)

100

700

°F

P_PZR (PZR Pressure)

0

2500

psig

PZR Level

0

100

%

9.4 Performance Considerations

Each Render Texture camera adds a draw call. For a panel with 6–8 gauges, this is negligible on modern hardware. The mesh itself is very low-poly (a few hundred triangles at most) and will have zero performance impact.

Part 10 — Troubleshooting

Problem

Likely Cause

Fix

Gauge appears as tiny dot in Unity

Scale mismatch

Set Scale Factor to 100 in Unity Import Settings

Gauge is rotated 90° in Unity

Coordinate system difference

In Blender export: set Forward: -Z, Up: Y, Apply Transform: ON

Materials are pink/missing

Shader not set for your render pipeline

Re-assign materials using URP/Lit shader

Needle rotates on wrong axis

Axis confusion between Blender and Unity

Try Quaternion.Euler(0, 0, angle) vs (angle, 0, 0) vs (0, angle, 0)

Needle rotates from its centre, not its base

Origin not set to base in Blender

In Blender: select Needle, set 3D Cursor to origin, Object > Set Origin > Origin to 3D Cursor

Glass is fully opaque

Blend mode not set

Select MAT_Glass in Unity, set Surface Type to Transparent

Ticks/numerals not visible

Emission not enabled

Enable Emission on the material and set colour to white

FBX export fails or is empty

Objects not selected

Press A to select all before exporting with Selected Objects checked

Smooth shading looks weird on ticks

Smooth shade on flat objects

Right-click > Shade Flat on tick/needle objects

Appendix A — Quick Reference Cheat Sheet

Blender Keyboard Shortcuts Used in This Guide

Shortcut

Action

Shift + A

Add object menu

Tab

Toggle Object / Edit Mode

G

Grab (move)

R

Rotate

S

Scale

X / Delete

Delete selection

A

Select all / Deselect all

Ctrl + A

Apply transforms menu

Ctrl + P

Parent menu

Ctrl + R

Loop cut

Ctrl + Z

Undo

Ctrl + S

Save

Numpad 1 / 3 / 7

Front / Right / Top view

Numpad 5

Toggle Perspective / Orthographic

Numpad 0

Camera view

MMB drag

Orbit view

Shift + MMB drag

Pan view

Scroll wheel

Zoom

Shift + S

Snap / Cursor menu

Z

Shading mode pie menu

B

Box select

C

Circle select

1, 2, 3 (Edit Mode)

Vertex, Edge, Face select

E

Extrude

I

Inset faces

Unity GaugeNeedleDriver API

Member

Type

Description

minValue

float

Minimum gauge scale value (default: 100)

maxValue

float

Maximum gauge scale value (default: 650)

angleAtMin

float

Rotation angle at minimum (default: 225°)

angleAtMax

float

Rotation angle at maximum (default: -45°)

smoothSpeed

float

Lerp speed for needle movement (default: 5)

SetValue(float)

method

Call each frame with current temperature