# ============================================================================
# CRITICAL: Master the Atom - Blender Temperature Gauge Generator
# BlenderTemperatureGauge_v1.py - Parametric Vertical Bar/Thermometer Gauge
# ============================================================================
#
# PURPOSE:
#   Creates professional vertical bar-style gauges (thermometer style) for
#   the nuclear reactor simulator. Generates complete gauge assemblies with
#   housing, tube, animated fill, scale, and color zones ready for Unity.
#
# REQUIREMENTS:
#   - Blender 4.0+ (tested on Blender 5.0)
#   - Run from Blender's Text Editor or Scripting workspace
#
# USAGE:
#   1. Open Blender
#   2. Go to Scripting workspace
#   3. Create new text file or open this script
#   4. Modify parameters in the CONFIGURATION section
#   5. Run script (Alt+P or Run Script button)
#   6. Gauge will be created at world origin
#   7. Export as FBX for Unity import
#
# OUTPUT HIERARCHY:
#   BarGauge_[Name]
#   ├── Housing        (outer frame/bezel)
#   ├── Background     (dark backing plate)
#   ├── Tube           (glass tube outline)
#   ├── Fill           (animated fill level - scale Y in Unity)
#   ├── Bulb           (bottom reservoir - optional thermometer style)
#   ├── Scale
#   │   ├── MajorTicks
#   │   ├── MinorTicks
#   │   └── Numbers
#   ├── Zones          (color bands on side)
#   ├── Label          (gauge name)
#   └── Units          (unit label)
#
# ANIMATION IN UNITY:
#   The Fill object should be animated by scaling its Y axis:
#   - fillObject.localScale.y = normalizedValue (0 to 1)
#   - Pivot is at bottom so it grows upward
#
# MATERIALS CREATED:
#   - MAT_BarGauge_Housing
#   - MAT_BarGauge_Background
#   - MAT_BarGauge_Tube (transparent glass)
#   - MAT_BarGauge_Fill (colored liquid)
#   - MAT_BarGauge_Bulb
#   - MAT_BarGauge_Tick
#   - MAT_BarGauge_Zone_Normal
#   - MAT_BarGauge_Zone_Warning
#   - MAT_BarGauge_Zone_Danger
#
# VERSION: 1.0.0
# DATE: 2026-02-09
# AUTHOR: Critical Project
# ============================================================================

import bpy
import bmesh
import math
from mathutils import Vector, Matrix

# ============================================================================
# CONFIGURATION - Modify these parameters as needed
# ============================================================================

class BarGaugeConfig:
    """Configuration parameters for bar gauge generation."""
    
    # --- IDENTIFICATION ---
    name = "Temperature"          # Gauge name (used in hierarchy)
    label = "TEMP"                # Display label
    units = "°F"                  # Unit label
    
    # --- DIMENSIONS ---
    height = 2.0                  # Total height (Blender units)
    width = 0.4                   # Total width
    depth = 0.15                  # Total depth/thickness
    
    # --- TUBE DIMENSIONS ---
    tube_width = 0.12             # Width of the indicator tube
    tube_margin = 0.08            # Margin from edges to tube
    
    # --- STYLE ---
    style = "bar"                 # "bar" = rectangular, "thermometer" = with bulb
    bulb_radius = 0.1             # Radius of bulb (thermometer style only)
    corner_radius = 0.02          # Rounded corners on housing
    
    # --- SCALE ---
    min_value = 100.0             # Minimum scale value
    max_value = 700.0             # Maximum scale value
    major_tick_interval = 100.0   # Interval between major ticks
    minor_tick_interval = 20.0    # Interval between minor ticks
    
    # --- COLOR ZONES ---
    # Each zone is (start_value, end_value)
    zone_normal = (100.0, 550.0)
    zone_warning = (550.0, 620.0)
    zone_danger = (620.0, 700.0)
    zone_width = 0.03             # Width of zone indicator bars
    
    # --- TEXT ---
    label_size = 0.08             # Size of label text
    units_size = 0.06             # Size of units text
    number_size = 0.05            # Size of scale numbers
    
    # --- INITIAL VALUE ---
    initial_fill = 0.5            # Initial fill level (0-1)
    
    # --- COLORS (RGBA) ---
    color_housing = (0.25, 0.25, 0.28, 1.0)
    color_background = (0.08, 0.08, 0.10, 1.0)
    color_tube = (0.6, 0.7, 0.8, 0.3)        # Transparent glass
    color_fill = (0.2, 0.6, 0.9, 1.0)        # Blue liquid (changes with temp)
    color_bulb = (0.8, 0.2, 0.2, 1.0)        # Red bulb
    color_tick = (0.85, 0.85, 0.85, 1.0)
    color_text = (0.9, 0.9, 0.9, 1.0)
    color_zone_normal = (0.2, 0.7, 0.3, 1.0)
    color_zone_warning = (0.9, 0.8, 0.2, 1.0)
    color_zone_danger = (0.9, 0.2, 0.2, 1.0)
    
    # --- FILL COLOR GRADIENT ---
    # Fill color changes based on value (cold to hot)
    fill_color_cold = (0.2, 0.4, 0.9, 1.0)   # Blue
    fill_color_mid = (0.2, 0.8, 0.4, 1.0)    # Green
    fill_color_hot = (0.9, 0.3, 0.1, 1.0)    # Red/Orange


# ============================================================================
# MATERIAL CREATION
# ============================================================================

def create_material(name, color, metallic=0.0, roughness=0.5, emission=0.0, alpha=1.0):
    """Create a PBR material compatible with Unity URP."""
    
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)
    
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    
    nodes.clear()
    
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.location = (0, 0)
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['Alpha'].default_value = alpha
    
    if emission > 0:
        bsdf.inputs['Emission Color'].default_value = color
        bsdf.inputs['Emission Strength'].default_value = emission
    
    output = nodes.new('ShaderNodeOutputMaterial')
    output.location = (300, 0)
    
    links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    
    # Enable transparency if alpha < 1
    if alpha < 1.0:
        mat.blend_method = 'BLEND'
        mat.shadow_method = 'HASHED'
    
    return mat


def create_bar_gauge_materials(config):
    """Create all materials needed for the bar gauge."""
    
    materials = {}
    
    materials['housing'] = create_material(
        f"MAT_BarGauge_Housing_{config.name}",
        config.color_housing,
        metallic=0.6,
        roughness=0.4
    )
    
    materials['background'] = create_material(
        f"MAT_BarGauge_Background_{config.name}",
        config.color_background,
        metallic=0.0,
        roughness=0.9
    )
    
    materials['tube'] = create_material(
        f"MAT_BarGauge_Tube_{config.name}",
        config.color_tube,
        metallic=0.0,
        roughness=0.1,
        alpha=config.color_tube[3]
    )
    
    materials['fill'] = create_material(
        f"MAT_BarGauge_Fill_{config.name}",
        config.color_fill,
        metallic=0.0,
        roughness=0.3,
        emission=0.2
    )
    
    materials['bulb'] = create_material(
        f"MAT_BarGauge_Bulb_{config.name}",
        config.color_bulb,
        metallic=0.0,
        roughness=0.3,
        emission=0.3
    )
    
    materials['tick'] = create_material(
        f"MAT_BarGauge_Tick_{config.name}",
        config.color_tick,
        metallic=0.0,
        roughness=0.5
    )
    
    materials['text'] = create_material(
        f"MAT_BarGauge_Text_{config.name}",
        config.color_text,
        metallic=0.0,
        roughness=0.5
    )
    
    materials['zone_normal'] = create_material(
        f"MAT_BarGauge_Zone_Normal_{config.name}",
        config.color_zone_normal,
        metallic=0.0,
        roughness=0.6,
        emission=0.15
    )
    
    materials['zone_warning'] = create_material(
        f"MAT_BarGauge_Zone_Warning_{config.name}",
        config.color_zone_warning,
        metallic=0.0,
        roughness=0.6,
        emission=0.2
    )
    
    materials['zone_danger'] = create_material(
        f"MAT_BarGauge_Zone_Danger_{config.name}",
        config.color_zone_danger,
        metallic=0.0,
        roughness=0.6,
        emission=0.3
    )
    
    return materials


# ============================================================================
# GEOMETRY CREATION
# ============================================================================

def create_box(name, width, height, depth, center_bottom=False):
    """Create a box mesh."""
    
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    
    bm = bmesh.new()
    
    hw = width / 2
    hh = height / 2
    hd = depth / 2
    
    # Offset for center_bottom (pivot at bottom center)
    y_offset = hh if center_bottom else 0
    
    # Create vertices
    verts = [
        bm.verts.new((-hw, -hh + y_offset, -hd)),  # 0: bottom-left-back
        bm.verts.new((hw, -hh + y_offset, -hd)),   # 1: bottom-right-back
        bm.verts.new((hw, hh + y_offset, -hd)),    # 2: top-right-back
        bm.verts.new((-hw, hh + y_offset, -hd)),   # 3: top-left-back
        bm.verts.new((-hw, -hh + y_offset, hd)),   # 4: bottom-left-front
        bm.verts.new((hw, -hh + y_offset, hd)),    # 5: bottom-right-front
        bm.verts.new((hw, hh + y_offset, hd)),     # 6: top-right-front
        bm.verts.new((-hw, hh + y_offset, hd)),    # 7: top-left-front
    ]
    
    # Create faces
    bm.faces.new([verts[0], verts[1], verts[2], verts[3]])  # Back
    bm.faces.new([verts[4], verts[7], verts[6], verts[5]])  # Front
    bm.faces.new([verts[0], verts[4], verts[5], verts[1]])  # Bottom
    bm.faces.new([verts[2], verts[6], verts[7], verts[3]])  # Top
    bm.faces.new([verts[0], verts[3], verts[7], verts[4]])  # Left
    bm.faces.new([verts[1], verts[5], verts[6], verts[2]])  # Right
    
    bm.to_mesh(mesh)
    bm.free()
    
    return obj


def create_rounded_box(name, width, height, depth, corner_radius, segments=4):
    """Create a box with rounded corners (XY plane)."""
    
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    
    bm = bmesh.new()
    
    hw = width / 2 - corner_radius
    hh = height / 2 - corner_radius
    hd = depth / 2
    
    # Create profile vertices (rounded rectangle in XY)
    profile_verts = []
    
    # Four corners with arcs
    corners = [
        (hw, hh, 0),           # Top-right
        (-hw, hh, math.pi/2),  # Top-left
        (-hw, -hh, math.pi),   # Bottom-left
        (hw, -hh, 3*math.pi/2) # Bottom-right
    ]
    
    for cx, cy, start_angle in corners:
        for i in range(segments + 1):
            angle = start_angle + (i / segments) * (math.pi / 2)
            x = cx + corner_radius * math.cos(angle)
            y = cy + corner_radius * math.sin(angle)
            profile_verts.append((x, y))
    
    # Create front and back faces
    front_verts = []
    back_verts = []
    
    for x, y in profile_verts:
        front_verts.append(bm.verts.new((x, y, hd)))
        back_verts.append(bm.verts.new((x, y, -hd)))
    
    n = len(profile_verts)
    
    # Front face
    bm.faces.new(front_verts)
    
    # Back face (reversed)
    bm.faces.new(back_verts[::-1])
    
    # Side faces
    for i in range(n):
        next_i = (i + 1) % n
        bm.faces.new([
            front_verts[i],
            back_verts[i],
            back_verts[next_i],
            front_verts[next_i]
        ])
    
    bm.to_mesh(mesh)
    bm.free()
    
    return obj


def create_cylinder(name, radius, height, segments=32, center_bottom=False):
    """Create a cylinder mesh."""
    
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    
    bm = bmesh.new()
    
    hh = height / 2
    y_offset = hh if center_bottom else 0
    
    # Create vertices
    top_center = bm.verts.new((0, hh + y_offset, 0))
    bottom_center = bm.verts.new((0, -hh + y_offset, 0))
    
    top_verts = []
    bottom_verts = []
    
    for i in range(segments):
        angle = (i / segments) * 2 * math.pi
        x = radius * math.cos(angle)
        z = radius * math.sin(angle)
        
        top_verts.append(bm.verts.new((x, hh + y_offset, z)))
        bottom_verts.append(bm.verts.new((x, -hh + y_offset, z)))
    
    # Create faces
    for i in range(segments):
        next_i = (i + 1) % segments
        
        # Top face
        bm.faces.new([top_center, top_verts[i], top_verts[next_i]])
        
        # Bottom face
        bm.faces.new([bottom_center, bottom_verts[next_i], bottom_verts[i]])
        
        # Side face
        bm.faces.new([
            top_verts[i],
            bottom_verts[i],
            bottom_verts[next_i],
            top_verts[next_i]
        ])
    
    bm.to_mesh(mesh)
    bm.free()
    
    return obj


def create_sphere(name, radius, segments=16, rings=8):
    """Create a sphere mesh."""
    
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    
    bm = bmesh.new()
    
    # Create vertices
    verts = []
    
    # Top pole
    top = bm.verts.new((0, radius, 0))
    verts.append([top])
    
    # Middle rings
    for ring in range(1, rings):
        ring_verts = []
        phi = (ring / rings) * math.pi  # 0 to pi
        y = radius * math.cos(phi)
        ring_radius = radius * math.sin(phi)
        
        for seg in range(segments):
            theta = (seg / segments) * 2 * math.pi
            x = ring_radius * math.cos(theta)
            z = ring_radius * math.sin(theta)
            ring_verts.append(bm.verts.new((x, y, z)))
        
        verts.append(ring_verts)
    
    # Bottom pole
    bottom = bm.verts.new((0, -radius, 0))
    verts.append([bottom])
    
    # Create faces
    # Top cap
    for i in range(segments):
        next_i = (i + 1) % segments
        bm.faces.new([verts[0][0], verts[1][i], verts[1][next_i]])
    
    # Middle bands
    for ring in range(1, rings - 1):
        for i in range(segments):
            next_i = (i + 1) % segments
            bm.faces.new([
                verts[ring][i],
                verts[ring + 1][i],
                verts[ring + 1][next_i],
                verts[ring][next_i]
            ])
    
    # Bottom cap
    last_ring = rings - 1
    for i in range(segments):
        next_i = (i + 1) % segments
        bm.faces.new([verts[last_ring][i], verts[-1][0], verts[last_ring][next_i]])
    
    bm.to_mesh(mesh)
    bm.free()
    
    return obj


def create_tube_outline(name, width, height, thickness, depth):
    """Create a hollow tube outline (frame)."""
    
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    
    bm = bmesh.new()
    
    hw_outer = width / 2
    hh_outer = height / 2
    hw_inner = hw_outer - thickness
    hh_inner = hh_outer - thickness
    hd = depth / 2
    
    # Create vertices for outer and inner rectangles, front and back
    def create_rect_verts(hw, hh, z):
        return [
            bm.verts.new((-hw, -hh, z)),
            bm.verts.new((hw, -hh, z)),
            bm.verts.new((hw, hh, z)),
            bm.verts.new((-hw, hh, z)),
        ]
    
    outer_front = create_rect_verts(hw_outer, hh_outer, hd)
    outer_back = create_rect_verts(hw_outer, hh_outer, -hd)
    inner_front = create_rect_verts(hw_inner, hh_inner, hd)
    inner_back = create_rect_verts(hw_inner, hh_inner, -hd)
    
    # Front face (with hole)
    for i in range(4):
        next_i = (i + 1) % 4
        bm.faces.new([
            outer_front[i],
            outer_front[next_i],
            inner_front[next_i],
            inner_front[i]
        ])
    
    # Back face (with hole)
    for i in range(4):
        next_i = (i + 1) % 4
        bm.faces.new([
            outer_back[next_i],
            outer_back[i],
            inner_back[i],
            inner_back[next_i]
        ])
    
    # Outer sides
    for i in range(4):
        next_i = (i + 1) % 4
        bm.faces.new([
            outer_front[i],
            outer_back[i],
            outer_back[next_i],
            outer_front[next_i]
        ])
    
    # Inner sides
    for i in range(4):
        next_i = (i + 1) % 4
        bm.faces.new([
            inner_front[next_i],
            inner_back[next_i],
            inner_back[i],
            inner_front[i]
        ])
    
    bm.to_mesh(mesh)
    bm.free()
    
    return obj


def create_tick_mark(name, x_pos, y_pos, length, thickness, depth):
    """Create a horizontal tick mark."""
    
    return create_box(name, length, thickness, depth)


def create_text_object(name, text, size, depth=0.005):
    """Create a 3D text object."""
    
    curve = bpy.data.curves.new(name=name, type='FONT')
    curve.body = text
    curve.size = size
    curve.extrude = depth / 2
    curve.align_x = 'CENTER'
    curve.align_y = 'CENTER'
    
    obj = bpy.data.objects.new(name, curve)
    
    return obj


def create_zone_bar(name, width, height, depth):
    """Create a colored zone indicator bar."""
    
    return create_box(name, width, height, depth)


# ============================================================================
# VALUE CONVERSION
# ============================================================================

def value_to_normalized(value, config):
    """Convert a gauge value to normalized 0-1 range."""
    normalized = (value - config.min_value) / (config.max_value - config.min_value)
    return max(0.0, min(1.0, normalized))


def value_to_y_position(value, config, tube_height):
    """Convert a gauge value to Y position on the tube."""
    normalized = value_to_normalized(value, config)
    return normalized * tube_height


# ============================================================================
# BAR GAUGE ASSEMBLY
# ============================================================================

def create_bar_gauge(config):
    """Create the complete bar gauge assembly."""
    
    print(f"Creating bar gauge: {config.name}")
    
    # Create materials
    materials = create_bar_gauge_materials(config)
    
    # Create parent empty
    root = bpy.data.objects.new(f"BarGauge_{config.name}", None)
    bpy.context.collection.objects.link(root)
    root.empty_display_type = 'ARROWS'
    root.empty_display_size = config.height * 0.25
    
    # Calculate dimensions
    tube_height = config.height - (config.tube_margin * 2)
    if config.style == "thermometer":
        tube_height -= config.bulb_radius
    
    tube_bottom_y = -config.height / 2 + config.tube_margin
    if config.style == "thermometer":
        tube_bottom_y += config.bulb_radius
    
    # --- HOUSING (outer frame) ---
    if config.corner_radius > 0.001:
        housing = create_rounded_box(
            "Housing",
            config.width,
            config.height,
            config.depth,
            config.corner_radius
        )
    else:
        housing = create_box("Housing", config.width, config.height, config.depth)
    
    bpy.context.collection.objects.link(housing)
    housing.parent = root
    housing.data.materials.append(materials['housing'])
    
    # --- BACKGROUND ---
    bg_margin = 0.02
    background = create_box(
        "Background",
        config.width - bg_margin * 2,
        config.height - bg_margin * 2,
        config.depth * 0.3
    )
    bpy.context.collection.objects.link(background)
    background.parent = root
    background.location.z = config.depth * 0.35
    background.data.materials.append(materials['background'])
    
    # --- TUBE (glass outline) ---
    tube_thickness = 0.01
    tube = create_tube_outline(
        "Tube",
        config.tube_width + tube_thickness * 2,
        tube_height + tube_thickness * 2,
        tube_thickness,
        config.depth * 0.2
    )
    bpy.context.collection.objects.link(tube)
    tube.parent = root
    tube.location.y = tube_bottom_y + tube_height / 2
    tube.location.z = config.depth * 0.5
    tube.data.materials.append(materials['tube'])
    
    # --- FILL (animated level) ---
    # Create fill with pivot at bottom for easy scaling
    fill_height = tube_height * config.initial_fill
    fill = create_box(
        "Fill",
        config.tube_width,
        tube_height,  # Full height - will be scaled
        config.depth * 0.15,
        center_bottom=True
    )
    bpy.context.collection.objects.link(fill)
    fill.parent = root
    fill.location.y = tube_bottom_y
    fill.location.z = config.depth * 0.5
    fill.scale.y = config.initial_fill  # Initial fill level
    fill.data.materials.append(materials['fill'])
    
    # --- BULB (thermometer style) ---
    if config.style == "thermometer":
        bulb = create_sphere("Bulb", config.bulb_radius)
        bpy.context.collection.objects.link(bulb)
        bulb.parent = root
        bulb.location.y = -config.height / 2 + config.tube_margin + config.bulb_radius * 0.3
        bulb.location.z = config.depth * 0.5
        bulb.data.materials.append(materials['bulb'])
    
    # --- SCALE (ticks and numbers) ---
    scale_empty = bpy.data.objects.new("Scale", None)
    bpy.context.collection.objects.link(scale_empty)
    scale_empty.parent = root
    
    # Major ticks
    major_ticks_empty = bpy.data.objects.new("MajorTicks", None)
    bpy.context.collection.objects.link(major_ticks_empty)
    major_ticks_empty.parent = scale_empty
    
    tick_x = config.width / 2 - config.tube_margin / 2
    major_tick_length = 0.06
    major_tick_thickness = 0.012
    
    value = config.min_value
    tick_idx = 0
    while value <= config.max_value + 0.001:
        y_pos = tube_bottom_y + value_to_y_position(value, config, tube_height)
        
        tick = create_box(
            f"MajorTick_{tick_idx}",
            major_tick_length,
            major_tick_thickness,
            config.depth * 0.1
        )
        bpy.context.collection.objects.link(tick)
        tick.parent = major_ticks_empty
        tick.location.x = tick_x
        tick.location.y = y_pos
        tick.location.z = config.depth * 0.55
        tick.data.materials.append(materials['tick'])
        
        value += config.major_tick_interval
        tick_idx += 1
    
    # Minor ticks
    minor_ticks_empty = bpy.data.objects.new("MinorTicks", None)
    bpy.context.collection.objects.link(minor_ticks_empty)
    minor_ticks_empty.parent = scale_empty
    
    minor_tick_length = 0.03
    minor_tick_thickness = 0.008
    
    value = config.min_value
    tick_idx = 0
    while value <= config.max_value + 0.001:
        # Skip major tick positions
        if abs((value - config.min_value) % config.major_tick_interval) > 0.001:
            y_pos = tube_bottom_y + value_to_y_position(value, config, tube_height)
            
            tick = create_box(
                f"MinorTick_{tick_idx}",
                minor_tick_length,
                minor_tick_thickness,
                config.depth * 0.08
            )
            bpy.context.collection.objects.link(tick)
            tick.parent = minor_ticks_empty
            tick.location.x = tick_x
            tick.location.y = y_pos
            tick.location.z = config.depth * 0.55
            tick.data.materials.append(materials['tick'])
            
            tick_idx += 1
        
        value += config.minor_tick_interval
    
    # Numbers
    numbers_empty = bpy.data.objects.new("Numbers", None)
    bpy.context.collection.objects.link(numbers_empty)
    numbers_empty.parent = scale_empty
    
    number_x = tick_x + major_tick_length / 2 + 0.04
    
    value = config.min_value
    num_idx = 0
    while value <= config.max_value + 0.001:
        y_pos = tube_bottom_y + value_to_y_position(value, config, tube_height)
        
        # Format number
        if value == int(value):
            text = str(int(value))
        else:
            text = f"{value:.0f}"
        
        num_obj = create_text_object(f"Number_{num_idx}", text, config.number_size)
        bpy.context.collection.objects.link(num_obj)
        num_obj.parent = numbers_empty
        num_obj.location.x = number_x
        num_obj.location.y = y_pos
        num_obj.location.z = config.depth * 0.55
        num_obj.rotation_euler.x = math.radians(90)
        
        value += config.major_tick_interval
        num_idx += 1
    
    # --- ZONES (color indicator bars) ---
    zones_empty = bpy.data.objects.new("Zones", None)
    bpy.context.collection.objects.link(zones_empty)
    zones_empty.parent = root
    
    zone_x = -config.width / 2 + config.tube_margin / 2 + config.zone_width / 2
    
    def create_zone(zone_name, zone_range, material):
        if zone_range and zone_range[0] < zone_range[1]:
            y_start = tube_bottom_y + value_to_y_position(zone_range[0], config, tube_height)
            y_end = tube_bottom_y + value_to_y_position(zone_range[1], config, tube_height)
            zone_height = y_end - y_start
            
            zone = create_box(
                zone_name,
                config.zone_width,
                zone_height,
                config.depth * 0.1
            )
            bpy.context.collection.objects.link(zone)
            zone.parent = zones_empty
            zone.location.x = zone_x
            zone.location.y = y_start + zone_height / 2
            zone.location.z = config.depth * 0.55
            zone.data.materials.append(material)
    
    create_zone("Zone_Normal", config.zone_normal, materials['zone_normal'])
    create_zone("Zone_Warning", config.zone_warning, materials['zone_warning'])
    create_zone("Zone_Danger", config.zone_danger, materials['zone_danger'])
    
    # --- LABEL ---
    label = create_text_object("Label", config.label, config.label_size)
    bpy.context.collection.objects.link(label)
    label.parent = root
    label.location.y = config.height / 2 - config.tube_margin * 0.7
    label.location.z = config.depth * 0.55
    label.rotation_euler.x = math.radians(90)
    
    # --- UNITS ---
    units = create_text_object("Units", config.units, config.units_size)
    bpy.context.collection.objects.link(units)
    units.parent = root
    units.location.y = -config.height / 2 + config.tube_margin * 0.5
    units.location.z = config.depth * 0.55
    units.rotation_euler.x = math.radians(90)
    
    print(f"Bar gauge '{config.name}' created successfully!")
    print(f"  - Hierarchy root: BarGauge_{config.name}")
    print(f"  - Fill object: Fill (animate scale.y from 0 to 1)")
    print(f"  - Value range: {config.min_value} to {config.max_value}")
    print(f"  - Style: {config.style}")
    
    return root


# ============================================================================
# PRESET CONFIGURATIONS
# ============================================================================

def create_temperature_bar_gauge():
    """Create a temperature bar gauge preset."""
    config = BarGaugeConfig()
    config.name = "Temperature"
    config.label = "TEMP"
    config.units = "°F"
    config.style = "thermometer"
    config.min_value = 100.0
    config.max_value = 700.0
    config.major_tick_interval = 100.0
    config.minor_tick_interval = 20.0
    config.zone_normal = (100.0, 550.0)
    config.zone_warning = (550.0, 620.0)
    config.zone_danger = (620.0, 700.0)
    config.initial_fill = 0.4
    return create_bar_gauge(config)


def create_level_bar_gauge():
    """Create a level indicator bar gauge preset."""
    config = BarGaugeConfig()
    config.name = "Level"
    config.label = "PZR LVL"
    config.units = "%"
    config.style = "bar"
    config.min_value = 0.0
    config.max_value = 100.0
    config.major_tick_interval = 20.0
    config.minor_tick_interval = 5.0
    config.zone_danger = (0.0, 15.0)
    config.zone_warning = (15.0, 20.0)
    config.zone_normal = (20.0, 70.0)
    # Upper warning/danger would need additional zones
    config.initial_fill = 0.5
    config.color_fill = (0.2, 0.6, 0.9, 1.0)  # Blue
    return create_bar_gauge(config)


def create_pressure_bar_gauge():
    """Create a pressure bar gauge preset."""
    config = BarGaugeConfig()
    config.name = "Pressure"
    config.label = "PRESS"
    config.units = "psia"
    config.style = "bar"
    config.height = 2.5
    config.min_value = 0.0
    config.max_value = 2500.0
    config.major_tick_interval = 500.0
    config.minor_tick_interval = 100.0
    config.zone_danger = (0.0, 2000.0)
    config.zone_warning = (2000.0, 2185.0)
    config.zone_normal = (2185.0, 2285.0)
    config.initial_fill = 0.9
    config.color_fill = (0.9, 0.5, 0.2, 1.0)  # Orange
    return create_bar_gauge(config)


def create_flow_bar_gauge():
    """Create a flow rate bar gauge preset."""
    config = BarGaugeConfig()
    config.name = "Flow"
    config.label = "FLOW"
    config.units = "K gpm"
    config.style = "bar"
    config.min_value = 0.0
    config.max_value = 120.0
    config.major_tick_interval = 20.0
    config.minor_tick_interval = 5.0
    config.zone_danger = (0.0, 60.0)
    config.zone_warning = (60.0, 70.0)
    config.zone_normal = (70.0, 120.0)
    config.initial_fill = 0.75
    config.color_fill = (0.3, 0.8, 0.9, 1.0)  # Cyan
    return create_bar_gauge(config)


# ============================================================================
# MAIN EXECUTION
# ============================================================================

def main():
    """Main execution - create gauge with current configuration."""
    
    # Create gauge with default config
    # Modify BarGaugeConfig class above to customize, or use presets:
    
    # Option 1: Use default config
    gauge = create_bar_gauge(BarGaugeConfig())
    
    # Option 2: Use preset (uncomment one)
    # gauge = create_temperature_bar_gauge()
    # gauge = create_level_bar_gauge()
    # gauge = create_pressure_bar_gauge()
    # gauge = create_flow_bar_gauge()
    
    # Select the created gauge
    bpy.context.view_layer.objects.active = gauge
    gauge.select_set(True)
    
    print("\n=== EXPORT INSTRUCTIONS ===")
    print("1. Select the gauge root object (BarGauge_*)")
    print("2. File > Export > FBX (.fbx)")
    print("3. Settings:")
    print("   - Scale: 1.0")
    print("   - Apply Transform: checked")
    print("   - Forward: -Z Forward")
    print("   - Up: Y Up")
    print("   - Only Selected: checked")
    print("4. In Unity:")
    print("   - Set scale to 1")
    print("   - Animate Fill.localScale.y (0 to 1) for level")
    print("   - Materials: URP/Lit, enable transparency for Tube")


# Run when script is executed
if __name__ == "__main__":
    main()
