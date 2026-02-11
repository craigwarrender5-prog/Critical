# ============================================================================
# CRITICAL: Master the Atom - Blender Arc Gauge Generator
# BlenderArcGauge_v1.py - Parametric Arc Gauge Creation Script
# ============================================================================
#
# PURPOSE:
#   Creates professional arc-style gauges for the nuclear reactor simulator.
#   Generates complete gauge assemblies with bezels, faces, scales, needles,
#   and color zones ready for export to Unity.
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
#   Gauge_[Name]
#   ├── Bezel          (outer ring)
#   ├── Face           (background plate)
#   ├── Scale
#   │   ├── MajorTicks (large tick marks)
#   │   ├── MinorTicks (small tick marks)
#   │   ├── Numbers    (value labels)
#   │   └── Zones      (color bands - normal/warning/danger)
#   ├── Needle         (indicator needle - separate for animation)
#   ├── NeedleHub      (center cap)
#   ├── Label          (gauge name text)
#   └── Units          (unit label text)
#
# MATERIALS CREATED:
#   - MAT_Gauge_Bezel      (metallic ring)
#   - MAT_Gauge_Face       (dark background)
#   - MAT_Gauge_Tick       (white tick marks)
#   - MAT_Gauge_Needle     (red/orange needle)
#   - MAT_Gauge_Zone_Normal    (green zone)
#   - MAT_Gauge_Zone_Warning   (yellow zone)
#   - MAT_Gauge_Zone_Danger    (red zone)
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

class GaugeConfig:
    """Configuration parameters for gauge generation."""
    
    # --- IDENTIFICATION ---
    name = "Temperature"          # Gauge name (used in hierarchy)
    label = "T-HOT"               # Display label on gauge face
    units = "°F"                  # Unit label
    
    # --- DIMENSIONS ---
    radius = 1.0                  # Outer radius (Blender units)
    depth = 0.08                  # Total depth/thickness
    bezel_width = 0.08            # Width of bezel ring
    
    # --- SCALE ---
    min_value = 100.0             # Minimum scale value
    max_value = 700.0             # Maximum scale value
    major_tick_interval = 100.0   # Interval between major ticks
    minor_tick_interval = 20.0    # Interval between minor ticks
    
    # --- SWEEP ---
    sweep_angle = 270.0           # Total sweep angle in degrees
    start_angle = 225.0           # Start angle (0 = right, 90 = top, etc.)
    
    # --- NEEDLE ---
    needle_length = 0.75          # Length as fraction of radius
    needle_width = 0.03           # Width at base
    needle_pivot_offset = 0.0     # Z offset for needle pivot
    
    # --- COLOR ZONES ---
    # Each zone is (start_value, end_value)
    # Leave empty list for no zones
    zone_normal = (100.0, 550.0)    # Green zone
    zone_warning = (550.0, 620.0)   # Yellow zone  
    zone_danger = (620.0, 700.0)    # Red zone
    zone_width = 0.06               # Width of zone bands
    
    # --- TEXT ---
    label_size = 0.12             # Size of label text
    units_size = 0.08             # Size of units text
    number_size = 0.06            # Size of scale numbers
    
    # --- COLORS (RGBA) ---
    color_bezel = (0.3, 0.3, 0.32, 1.0)
    color_face = (0.1, 0.1, 0.12, 1.0)
    color_tick = (0.9, 0.9, 0.9, 1.0)
    color_needle = (0.9, 0.2, 0.1, 1.0)
    color_hub = (0.2, 0.2, 0.22, 1.0)
    color_text = (0.9, 0.9, 0.9, 1.0)
    color_zone_normal = (0.2, 0.7, 0.3, 1.0)
    color_zone_warning = (0.9, 0.8, 0.2, 1.0)
    color_zone_danger = (0.9, 0.2, 0.2, 1.0)


# ============================================================================
# MATERIAL CREATION
# ============================================================================

def create_material(name, color, metallic=0.0, roughness=0.5, emission=0.0):
    """Create a PBR material compatible with Unity URP."""
    
    # Check if material already exists
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)
    
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    
    # Clear existing nodes
    nodes.clear()
    
    # Create Principled BSDF
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.location = (0, 0)
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = roughness
    
    # Add emission if specified
    if emission > 0:
        bsdf.inputs['Emission Color'].default_value = color
        bsdf.inputs['Emission Strength'].default_value = emission
    
    # Create output node
    output = nodes.new('ShaderNodeOutputMaterial')
    output.location = (300, 0)
    
    # Link nodes
    links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    
    return mat


def create_gauge_materials(config):
    """Create all materials needed for the gauge."""
    
    materials = {}
    
    materials['bezel'] = create_material(
        f"MAT_Gauge_Bezel_{config.name}",
        config.color_bezel,
        metallic=0.8,
        roughness=0.3
    )
    
    materials['face'] = create_material(
        f"MAT_Gauge_Face_{config.name}",
        config.color_face,
        metallic=0.0,
        roughness=0.8
    )
    
    materials['tick'] = create_material(
        f"MAT_Gauge_Tick_{config.name}",
        config.color_tick,
        metallic=0.0,
        roughness=0.5
    )
    
    materials['needle'] = create_material(
        f"MAT_Gauge_Needle_{config.name}",
        config.color_needle,
        metallic=0.1,
        roughness=0.4,
        emission=0.3
    )
    
    materials['hub'] = create_material(
        f"MAT_Gauge_Hub_{config.name}",
        config.color_hub,
        metallic=0.6,
        roughness=0.4
    )
    
    materials['text'] = create_material(
        f"MAT_Gauge_Text_{config.name}",
        config.color_text,
        metallic=0.0,
        roughness=0.5
    )
    
    materials['zone_normal'] = create_material(
        f"MAT_Gauge_Zone_Normal_{config.name}",
        config.color_zone_normal,
        metallic=0.0,
        roughness=0.6,
        emission=0.1
    )
    
    materials['zone_warning'] = create_material(
        f"MAT_Gauge_Zone_Warning_{config.name}",
        config.color_zone_warning,
        metallic=0.0,
        roughness=0.6,
        emission=0.2
    )
    
    materials['zone_danger'] = create_material(
        f"MAT_Gauge_Zone_Danger_{config.name}",
        config.color_zone_danger,
        metallic=0.0,
        roughness=0.6,
        emission=0.3
    )
    
    return materials


# ============================================================================
# GEOMETRY CREATION
# ============================================================================

def value_to_angle(value, config):
    """Convert a gauge value to an angle in radians."""
    normalized = (value - config.min_value) / (config.max_value - config.min_value)
    # Clamp to 0-1 range
    normalized = max(0.0, min(1.0, normalized))
    # Convert to angle (start_angle going counter-clockwise by sweep_angle)
    angle_deg = config.start_angle - (normalized * config.sweep_angle)
    return math.radians(angle_deg)


def create_arc_mesh(name, inner_radius, outer_radius, start_angle, end_angle, segments=64, depth=0.01):
    """Create an arc/ring segment mesh."""
    
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    
    bm = bmesh.new()
    
    # Calculate angle step
    angle_range = end_angle - start_angle
    angle_step = angle_range / segments
    
    # Create vertices for inner and outer edges, front and back
    verts_inner_front = []
    verts_outer_front = []
    verts_inner_back = []
    verts_outer_back = []
    
    half_depth = depth / 2
    
    for i in range(segments + 1):
        angle = start_angle + (i * angle_step)
        cos_a = math.cos(angle)
        sin_a = math.sin(angle)
        
        # Front face vertices
        verts_inner_front.append(bm.verts.new((
            inner_radius * cos_a,
            inner_radius * sin_a,
            half_depth
        )))
        verts_outer_front.append(bm.verts.new((
            outer_radius * cos_a,
            outer_radius * sin_a,
            half_depth
        )))
        
        # Back face vertices
        verts_inner_back.append(bm.verts.new((
            inner_radius * cos_a,
            inner_radius * sin_a,
            -half_depth
        )))
        verts_outer_back.append(bm.verts.new((
            outer_radius * cos_a,
            outer_radius * sin_a,
            -half_depth
        )))
    
    # Create faces
    for i in range(segments):
        # Front face
        bm.faces.new([
            verts_inner_front[i],
            verts_outer_front[i],
            verts_outer_front[i + 1],
            verts_inner_front[i + 1]
        ])
        
        # Back face
        bm.faces.new([
            verts_inner_back[i + 1],
            verts_outer_back[i + 1],
            verts_outer_back[i],
            verts_inner_back[i]
        ])
        
        # Outer edge
        bm.faces.new([
            verts_outer_front[i],
            verts_outer_back[i],
            verts_outer_back[i + 1],
            verts_outer_front[i + 1]
        ])
        
        # Inner edge
        bm.faces.new([
            verts_inner_front[i + 1],
            verts_inner_back[i + 1],
            verts_inner_back[i],
            verts_inner_front[i]
        ])
    
    # End caps
    # Start cap
    bm.faces.new([
        verts_inner_front[0],
        verts_inner_back[0],
        verts_outer_back[0],
        verts_outer_front[0]
    ])
    
    # End cap
    bm.faces.new([
        verts_outer_front[-1],
        verts_outer_back[-1],
        verts_inner_back[-1],
        verts_inner_front[-1]
    ])
    
    bm.to_mesh(mesh)
    bm.free()
    
    return obj


def create_disk(name, radius, segments=64, depth=0.01):
    """Create a filled disk mesh."""
    
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    
    bm = bmesh.new()
    
    half_depth = depth / 2
    
    # Center vertex
    center_front = bm.verts.new((0, 0, half_depth))
    center_back = bm.verts.new((0, 0, -half_depth))
    
    # Edge vertices
    verts_front = []
    verts_back = []
    
    for i in range(segments):
        angle = (i / segments) * 2 * math.pi
        x = radius * math.cos(angle)
        y = radius * math.sin(angle)
        
        verts_front.append(bm.verts.new((x, y, half_depth)))
        verts_back.append(bm.verts.new((x, y, -half_depth)))
    
    # Create faces
    for i in range(segments):
        next_i = (i + 1) % segments
        
        # Front face (triangle fan)
        bm.faces.new([center_front, verts_front[i], verts_front[next_i]])
        
        # Back face
        bm.faces.new([center_back, verts_back[next_i], verts_back[i]])
        
        # Edge
        bm.faces.new([
            verts_front[i],
            verts_back[i],
            verts_back[next_i],
            verts_front[next_i]
        ])
    
    bm.to_mesh(mesh)
    bm.free()
    
    return obj


def create_tick_mark(name, inner_radius, outer_radius, angle, width=0.01, depth=0.005):
    """Create a single tick mark."""
    
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    
    bm = bmesh.new()
    
    half_width = width / 2
    half_depth = depth / 2
    
    cos_a = math.cos(angle)
    sin_a = math.sin(angle)
    
    # Perpendicular direction
    perp_x = -sin_a
    perp_y = cos_a
    
    # Create box vertices
    verts = []
    for r in [inner_radius, outer_radius]:
        for w in [-half_width, half_width]:
            for d in [-half_depth, half_depth]:
                x = r * cos_a + w * perp_x
                y = r * sin_a + w * perp_y
                z = d
                verts.append(bm.verts.new((x, y, z)))
    
    # Create faces (box)
    # Front (z+)
    bm.faces.new([verts[1], verts[3], verts[7], verts[5]])
    # Back (z-)
    bm.faces.new([verts[0], verts[4], verts[6], verts[2]])
    # Top (outer radius)
    bm.faces.new([verts[4], verts[5], verts[7], verts[6]])
    # Bottom (inner radius)
    bm.faces.new([verts[0], verts[2], verts[3], verts[1]])
    # Left
    bm.faces.new([verts[0], verts[1], verts[5], verts[4]])
    # Right
    bm.faces.new([verts[2], verts[6], verts[7], verts[3]])
    
    bm.to_mesh(mesh)
    bm.free()
    
    return obj


def create_needle(name, length, width, depth=0.02):
    """Create a needle/pointer mesh."""
    
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    
    bm = bmesh.new()
    
    half_width = width / 2
    half_depth = depth / 2
    
    # Needle vertices (tapered shape)
    # Base (wider)
    base_width = width
    # Tip (pointed)
    
    verts = [
        # Base front
        bm.verts.new((-base_width / 2, -0.05, half_depth)),   # 0
        bm.verts.new((base_width / 2, -0.05, half_depth)),    # 1
        # Base back
        bm.verts.new((-base_width / 2, -0.05, -half_depth)),  # 2
        bm.verts.new((base_width / 2, -0.05, -half_depth)),   # 3
        # Tip front
        bm.verts.new((0, length, half_depth)),                 # 4
        # Tip back
        bm.verts.new((0, length, -half_depth)),                # 5
    ]
    
    # Create faces
    # Front
    bm.faces.new([verts[0], verts[1], verts[4]])
    # Back
    bm.faces.new([verts[3], verts[2], verts[5]])
    # Left side
    bm.faces.new([verts[0], verts[4], verts[5], verts[2]])
    # Right side
    bm.faces.new([verts[1], verts[3], verts[5], verts[4]])
    # Base
    bm.faces.new([verts[0], verts[2], verts[3], verts[1]])
    
    bm.to_mesh(mesh)
    bm.free()
    
    # Set origin to base (pivot point)
    obj.location = (0, 0, 0)
    
    return obj


def create_text_object(name, text, size, depth=0.005):
    """Create a 3D text object."""
    
    # Create text curve
    curve = bpy.data.curves.new(name=name, type='FONT')
    curve.body = text
    curve.size = size
    curve.extrude = depth / 2
    curve.align_x = 'CENTER'
    curve.align_y = 'CENTER'
    
    obj = bpy.data.objects.new(name, curve)
    
    return obj


# ============================================================================
# GAUGE ASSEMBLY
# ============================================================================

def create_gauge(config):
    """Create the complete gauge assembly."""
    
    print(f"Creating gauge: {config.name}")
    
    # Create materials
    materials = create_gauge_materials(config)
    
    # Create parent empty
    root = bpy.data.objects.new(f"Gauge_{config.name}", None)
    bpy.context.collection.objects.link(root)
    root.empty_display_type = 'ARROWS'
    root.empty_display_size = config.radius * 0.5
    
    # --- BEZEL ---
    bezel = create_arc_mesh(
        "Bezel",
        config.radius - config.bezel_width,
        config.radius,
        0, 2 * math.pi,
        segments=64,
        depth=config.depth
    )
    bpy.context.collection.objects.link(bezel)
    bezel.parent = root
    bezel.data.materials.append(materials['bezel'])
    
    # --- FACE ---
    face = create_disk(
        "Face",
        config.radius - config.bezel_width - 0.005,
        segments=64,
        depth=config.depth * 0.5
    )
    bpy.context.collection.objects.link(face)
    face.parent = root
    face.location.z = config.depth * 0.25
    face.data.materials.append(materials['face'])
    
    # --- SCALE (parent for ticks and numbers) ---
    scale_empty = bpy.data.objects.new("Scale", None)
    bpy.context.collection.objects.link(scale_empty)
    scale_empty.parent = root
    scale_empty.empty_display_type = 'PLAIN_AXES'
    scale_empty.empty_display_size = 0.1
    
    # --- MAJOR TICKS ---
    major_ticks_empty = bpy.data.objects.new("MajorTicks", None)
    bpy.context.collection.objects.link(major_ticks_empty)
    major_ticks_empty.parent = scale_empty
    
    tick_inner = config.radius - config.bezel_width - 0.12
    tick_outer = config.radius - config.bezel_width - 0.02
    
    value = config.min_value
    tick_index = 0
    while value <= config.max_value + 0.001:
        angle = value_to_angle(value, config)
        
        tick = create_tick_mark(
            f"MajorTick_{tick_index}",
            tick_inner,
            tick_outer,
            angle,
            width=0.015,
            depth=0.008
        )
        bpy.context.collection.objects.link(tick)
        tick.parent = major_ticks_empty
        tick.location.z = config.depth * 0.5 + 0.002
        tick.data.materials.append(materials['tick'])
        
        value += config.major_tick_interval
        tick_index += 1
    
    # --- MINOR TICKS ---
    minor_ticks_empty = bpy.data.objects.new("MinorTicks", None)
    bpy.context.collection.objects.link(minor_ticks_empty)
    minor_ticks_empty.parent = scale_empty
    
    tick_inner_minor = config.radius - config.bezel_width - 0.08
    tick_outer_minor = config.radius - config.bezel_width - 0.02
    
    value = config.min_value
    tick_index = 0
    while value <= config.max_value + 0.001:
        # Skip if this is a major tick position
        if abs((value - config.min_value) % config.major_tick_interval) > 0.001:
            angle = value_to_angle(value, config)
            
            tick = create_tick_mark(
                f"MinorTick_{tick_index}",
                tick_inner_minor,
                tick_outer_minor,
                angle,
                width=0.008,
                depth=0.006
            )
            bpy.context.collection.objects.link(tick)
            tick.parent = minor_ticks_empty
            tick.location.z = config.depth * 0.5 + 0.002
            tick.data.materials.append(materials['tick'])
            
            tick_index += 1
        
        value += config.minor_tick_interval
    
    # --- NUMBERS ---
    numbers_empty = bpy.data.objects.new("Numbers", None)
    bpy.context.collection.objects.link(numbers_empty)
    numbers_empty.parent = scale_empty
    
    number_radius = config.radius - config.bezel_width - 0.20
    
    value = config.min_value
    num_index = 0
    while value <= config.max_value + 0.001:
        angle = value_to_angle(value, config)
        
        # Format number (remove decimal if whole number)
        if value == int(value):
            text = str(int(value))
        else:
            text = f"{value:.1f}"
        
        num_obj = create_text_object(
            f"Number_{num_index}",
            text,
            config.number_size,
            depth=0.004
        )
        bpy.context.collection.objects.link(num_obj)
        num_obj.parent = numbers_empty
        
        # Position at angle
        num_obj.location.x = number_radius * math.cos(angle)
        num_obj.location.y = number_radius * math.sin(angle)
        num_obj.location.z = config.depth * 0.5 + 0.003
        
        # Rotate to be readable (facing up)
        num_obj.rotation_euler.x = math.radians(90)
        
        # Convert to mesh and apply material
        # (Text objects need special handling for materials in Unity)
        
        value += config.major_tick_interval
        num_index += 1
    
    # --- COLOR ZONES ---
    zones_empty = bpy.data.objects.new("Zones", None)
    bpy.context.collection.objects.link(zones_empty)
    zones_empty.parent = scale_empty
    
    zone_inner = config.radius - config.bezel_width - config.zone_width - 0.02
    zone_outer = config.radius - config.bezel_width - 0.02
    
    # Normal zone (green)
    if config.zone_normal and config.zone_normal[0] < config.zone_normal[1]:
        start_angle = value_to_angle(config.zone_normal[1], config)
        end_angle = value_to_angle(config.zone_normal[0], config)
        
        zone_normal = create_arc_mesh(
            "Zone_Normal",
            zone_inner,
            zone_outer,
            start_angle,
            end_angle,
            segments=32,
            depth=0.004
        )
        bpy.context.collection.objects.link(zone_normal)
        zone_normal.parent = zones_empty
        zone_normal.location.z = config.depth * 0.5 + 0.001
        zone_normal.data.materials.append(materials['zone_normal'])
    
    # Warning zone (yellow)
    if config.zone_warning and config.zone_warning[0] < config.zone_warning[1]:
        start_angle = value_to_angle(config.zone_warning[1], config)
        end_angle = value_to_angle(config.zone_warning[0], config)
        
        zone_warning = create_arc_mesh(
            "Zone_Warning",
            zone_inner,
            zone_outer,
            start_angle,
            end_angle,
            segments=32,
            depth=0.004
        )
        bpy.context.collection.objects.link(zone_warning)
        zone_warning.parent = zones_empty
        zone_warning.location.z = config.depth * 0.5 + 0.001
        zone_warning.data.materials.append(materials['zone_warning'])
    
    # Danger zone (red)
    if config.zone_danger and config.zone_danger[0] < config.zone_danger[1]:
        start_angle = value_to_angle(config.zone_danger[1], config)
        end_angle = value_to_angle(config.zone_danger[0], config)
        
        zone_danger = create_arc_mesh(
            "Zone_Danger",
            zone_inner,
            zone_outer,
            start_angle,
            end_angle,
            segments=32,
            depth=0.004
        )
        bpy.context.collection.objects.link(zone_danger)
        zone_danger.parent = zones_empty
        zone_danger.location.z = config.depth * 0.5 + 0.001
        zone_danger.data.materials.append(materials['zone_danger'])
    
    # --- NEEDLE ---
    needle_length = config.radius * config.needle_length
    needle = create_needle(
        "Needle",
        needle_length,
        config.needle_width,
        depth=0.015
    )
    bpy.context.collection.objects.link(needle)
    needle.parent = root
    needle.location.z = config.depth * 0.5 + 0.01
    needle.data.materials.append(materials['needle'])
    
    # Set needle to mid-range initially
    mid_value = (config.min_value + config.max_value) / 2
    mid_angle = value_to_angle(mid_value, config)
    needle.rotation_euler.z = mid_angle - math.radians(90)  # Adjust for needle pointing up
    
    # --- NEEDLE HUB ---
    hub = create_disk(
        "NeedleHub",
        config.needle_width * 2,
        segments=32,
        depth=0.02
    )
    bpy.context.collection.objects.link(hub)
    hub.parent = root
    hub.location.z = config.depth * 0.5 + 0.015
    hub.data.materials.append(materials['hub'])
    
    # --- LABEL TEXT ---
    label = create_text_object(
        "Label",
        config.label,
        config.label_size,
        depth=0.005
    )
    bpy.context.collection.objects.link(label)
    label.parent = root
    label.location.y = -config.radius * 0.35
    label.location.z = config.depth * 0.5 + 0.005
    label.rotation_euler.x = math.radians(90)
    
    # --- UNITS TEXT ---
    units = create_text_object(
        "Units",
        config.units,
        config.units_size,
        depth=0.004
    )
    bpy.context.collection.objects.link(units)
    units.parent = root
    units.location.y = -config.radius * 0.55
    units.location.z = config.depth * 0.5 + 0.005
    units.rotation_euler.x = math.radians(90)
    
    print(f"Gauge '{config.name}' created successfully!")
    print(f"  - Hierarchy root: Gauge_{config.name}")
    print(f"  - Needle object: Needle (animate rotation_euler.z)")
    print(f"  - Value range: {config.min_value} to {config.max_value}")
    print(f"  - Sweep: {config.sweep_angle}° starting at {config.start_angle}°")
    
    return root


# ============================================================================
# PRESET CONFIGURATIONS
# ============================================================================

def create_temperature_gauge():
    """Create a temperature gauge preset."""
    config = GaugeConfig()
    config.name = "Temperature"
    config.label = "T-HOT"
    config.units = "°F"
    config.min_value = 100.0
    config.max_value = 700.0
    config.major_tick_interval = 100.0
    config.minor_tick_interval = 20.0
    config.zone_normal = (100.0, 550.0)
    config.zone_warning = (550.0, 620.0)
    config.zone_danger = (620.0, 700.0)
    return create_gauge(config)


def create_flow_gauge():
    """Create a flow rate gauge preset."""
    config = GaugeConfig()
    config.name = "Flow"
    config.label = "FLOW"
    config.units = "K gpm"
    config.min_value = 0.0
    config.max_value = 120.0
    config.major_tick_interval = 20.0
    config.minor_tick_interval = 5.0
    config.zone_danger = (0.0, 60.0)
    config.zone_warning = (60.0, 70.0)
    config.zone_normal = (70.0, 120.0)
    return create_gauge(config)


def create_pressure_gauge():
    """Create a pressure gauge preset."""
    config = GaugeConfig()
    config.name = "Pressure"
    config.label = "RCS PRESS"
    config.units = "psia"
    config.min_value = 0.0
    config.max_value = 2500.0
    config.major_tick_interval = 500.0
    config.minor_tick_interval = 100.0
    config.zone_danger = (0.0, 2000.0)
    config.zone_warning = (2000.0, 2185.0)
    config.zone_normal = (2185.0, 2285.0)
    # Add upper warning/danger
    return create_gauge(config)


def create_power_gauge():
    """Create a power gauge preset (low range for heatup)."""
    config = GaugeConfig()
    config.name = "Power"
    config.label = "CORE PWR"
    config.units = "MW"
    config.min_value = 0.0
    config.max_value = 50.0
    config.major_tick_interval = 10.0
    config.minor_tick_interval = 2.0
    config.zone_normal = (0.0, 30.0)
    config.zone_warning = (30.0, 40.0)
    config.zone_danger = (40.0, 50.0)
    return create_gauge(config)


# ============================================================================
# MAIN EXECUTION
# ============================================================================

def main():
    """Main execution - create gauge with current configuration."""
    
    # Clear existing objects (optional - comment out to keep existing)
    # bpy.ops.object.select_all(action='SELECT')
    # bpy.ops.object.delete()
    
    # Create gauge with default config
    # Modify GaugeConfig class above to customize, or use presets:
    
    # Option 1: Use default config
    gauge = create_gauge(GaugeConfig())
    
    # Option 2: Use preset (uncomment one)
    # gauge = create_temperature_gauge()
    # gauge = create_flow_gauge()
    # gauge = create_pressure_gauge()
    # gauge = create_power_gauge()
    
    # Select the created gauge
    bpy.context.view_layer.objects.active = gauge
    gauge.select_set(True)
    
    print("\n=== EXPORT INSTRUCTIONS ===")
    print("1. Select the gauge root object (Gauge_*)")
    print("2. File > Export > FBX (.fbx)")
    print("3. Settings:")
    print("   - Scale: 1.0")
    print("   - Apply Transform: checked")
    print("   - Forward: -Z Forward")
    print("   - Up: Y Up")
    print("   - Only Selected: checked")
    print("   - Apply Modifiers: checked")
    print("4. In Unity, set scale to 1 and materials to URP/Lit")


# Run when script is executed
if __name__ == "__main__":
    main()
