# ============================================================================
# CRITICAL: Master the Atom - RCS Primary Loop 3D Model Generator
# RCS_Primary_Loop_Blender.py - Blender 5.0 Python Script
# ============================================================================
#
# PURPOSE:
#   Generates a 3D model of a Westinghouse 4-Loop PWR Reactor Coolant System
#   for use in the Critical nuclear reactor simulator. The model is designed
#   for export to Unity with proper hierarchy, materials, and animation support.
#
# USAGE:
#   1. Open Blender 5.0
#   2. Go to Scripting workspace
#   3. Create new text file, paste this script
#   4. Run script (Alt+P or Run Script button)
#   5. Export as FBX for Unity
#
# FEATURES:
#   - Accurate proportions based on NRC HRTD specifications
#   - Proper hierarchy for Unity animation
#   - PBR materials compatible with Unity URP
#   - Separate meshes for animated components
#   - Flow arrow objects for animation
#
# SOURCES:
#   - NRC HRTD Section 3.2 - Reactor Coolant System
#   - Westinghouse 4-Loop PWR Design Parameters
#
# VERSION: 1.0.0
# DATE: 2026-02-09
# ============================================================================

import bpy
import bmesh
import math
from mathutils import Vector, Matrix

# ============================================================================
# CONFIGURATION - Adjust these for different scales/appearances
# ============================================================================

class Config:
    """Configuration parameters for the RCS model."""
    
    # Scale factor: 1 Blender unit = 1 foot (adjust for Unity import)
    SCALE = 1.0
    
    # Model detail level (higher = more subdivisions)
    DETAIL_LEVEL = 32  # Cylinder segments
    
    # Component sizes (in feet, will be scaled)
    # Based on NRC HRTD specifications
    
    # Reactor Vessel
    RV_RADIUS = 7.2  # ~173" ID / 2 / 12
    RV_HEIGHT = 40.0
    RV_WALL_THICKNESS = 0.7  # ~8.5" / 12
    
    # Hot Leg (29" ID)
    HOT_LEG_RADIUS = 1.21  # 29" / 2 / 12
    HOT_LEG_LENGTH = 27.0  # Approximate
    
    # Cold Leg (27.5" ID)
    COLD_LEG_RADIUS = 1.15  # 27.5" / 2 / 12
    COLD_LEG_LENGTH = 22.0
    
    # Crossover Leg (31" ID)
    CROSSOVER_RADIUS = 1.29  # 31" / 2 / 12
    CROSSOVER_LENGTH = 18.0
    
    # Steam Generator
    SG_LOWER_RADIUS = 5.6  # ~134" / 2 / 12
    SG_UPPER_RADIUS = 7.3  # ~175" / 2 / 12
    SG_HEIGHT = 67.75
    SG_TRANSITION_HEIGHT = 25.0  # Where it widens
    
    # Reactor Coolant Pump
    RCP_BODY_RADIUS = 4.0
    RCP_BODY_HEIGHT = 8.0
    RCP_MOTOR_RADIUS = 3.0
    RCP_MOTOR_HEIGHT = 20.0
    RCP_TOTAL_HEIGHT = 28.5
    
    # Pressurizer
    PZR_RADIUS = 3.5  # 84" / 2 / 12
    PZR_HEIGHT = 53.0
    
    # Surge Line (14" ID)
    SURGE_LINE_RADIUS = 0.58  # 14" / 2 / 12
    
    # Loop geometry (distance from center)
    LOOP_RADIUS = 35.0  # Distance from RV center to SG center
    
    # Elevation offsets
    RV_NOZZLE_HEIGHT = 10.0  # Height of nozzles above RV bottom
    SG_INLET_HEIGHT = 12.0
    RCP_HEIGHT = 8.0
    PZR_ELEVATION = 15.0
    
    # Flow arrow dimensions
    ARROW_LENGTH = 3.0
    ARROW_RADIUS = 0.3


# ============================================================================
# UTILITY FUNCTIONS
# ============================================================================

def clear_scene():
    """Remove all objects from the scene."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    
    # Clear orphan data
    for block in bpy.data.meshes:
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in bpy.data.materials:
        if block.users == 0:
            bpy.data.materials.remove(block)


def create_material(name, base_color, metallic=0.5, roughness=0.5, emission_color=None, emission_strength=0.0):
    """Create a PBR material compatible with Unity URP."""
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    
    # Clear default nodes
    nodes.clear()
    
    # Create Principled BSDF
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.location = (0, 0)
    bsdf.inputs['Base Color'].default_value = (*base_color, 1.0)
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = roughness
    
    if emission_color and emission_strength > 0:
        bsdf.inputs['Emission Color'].default_value = (*emission_color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = emission_strength
    
    # Create output node
    output = nodes.new('ShaderNodeOutputMaterial')
    output.location = (300, 0)
    
    # Link nodes
    links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    
    return mat


def create_cylinder(name, radius, height, segments=32, location=(0, 0, 0)):
    """Create a cylinder mesh."""
    bpy.ops.mesh.primitive_cylinder_add(
        radius=radius * Config.SCALE,
        depth=height * Config.SCALE,
        vertices=segments,
        location=(location[0] * Config.SCALE, 
                  location[1] * Config.SCALE, 
                  location[2] * Config.SCALE)
    )
    obj = bpy.context.active_object
    obj.name = name
    return obj


def create_sphere(name, radius, segments=32, location=(0, 0, 0)):
    """Create a UV sphere mesh."""
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=radius * Config.SCALE,
        segments=segments,
        ring_count=segments // 2,
        location=(location[0] * Config.SCALE,
                  location[1] * Config.SCALE,
                  location[2] * Config.SCALE)
    )
    obj = bpy.context.active_object
    obj.name = name
    return obj


def create_torus(name, major_radius, minor_radius, location=(0, 0, 0), rotation=(0, 0, 0)):
    """Create a torus (for pipe bends)."""
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius * Config.SCALE,
        minor_radius=minor_radius * Config.SCALE,
        major_segments=48,
        minor_segments=Config.DETAIL_LEVEL,
        location=(location[0] * Config.SCALE,
                  location[1] * Config.SCALE,
                  location[2] * Config.SCALE),
        rotation=rotation
    )
    obj = bpy.context.active_object
    obj.name = name
    return obj


def create_cone(name, radius1, radius2, height, location=(0, 0, 0)):
    """Create a cone/truncated cone."""
    bpy.ops.mesh.primitive_cone_add(
        radius1=radius1 * Config.SCALE,
        radius2=radius2 * Config.SCALE,
        depth=height * Config.SCALE,
        vertices=Config.DETAIL_LEVEL,
        location=(location[0] * Config.SCALE,
                  location[1] * Config.SCALE,
                  location[2] * Config.SCALE)
    )
    obj = bpy.context.active_object
    obj.name = name
    return obj


def create_arrow(name, length, radius, location=(0, 0, 0), rotation=(0, 0, 0)):
    """Create a flow direction arrow."""
    # Create arrow shaft
    bpy.ops.mesh.primitive_cylinder_add(
        radius=radius * Config.SCALE * 0.5,
        depth=length * Config.SCALE * 0.7,
        vertices=12,
        location=(0, 0, 0)
    )
    shaft = bpy.context.active_object
    shaft.name = f"{name}_shaft"
    
    # Create arrow head
    bpy.ops.mesh.primitive_cone_add(
        radius1=radius * Config.SCALE,
        radius2=0,
        depth=length * Config.SCALE * 0.3,
        vertices=12,
        location=(0, 0, length * Config.SCALE * 0.5)
    )
    head = bpy.context.active_object
    head.name = f"{name}_head"
    
    # Join shaft and head
    bpy.ops.object.select_all(action='DESELECT')
    shaft.select_set(True)
    head.select_set(True)
    bpy.context.view_layer.objects.active = shaft
    bpy.ops.object.join()
    
    arrow = bpy.context.active_object
    arrow.name = name
    
    # Apply rotation and location
    arrow.rotation_euler = rotation
    arrow.location = (location[0] * Config.SCALE,
                      location[1] * Config.SCALE,
                      location[2] * Config.SCALE)
    
    return arrow


def parent_objects(parent, children):
    """Parent multiple objects to a parent object."""
    for child in children:
        child.parent = parent
        child.matrix_parent_inverse = parent.matrix_world.inverted()


def create_empty(name, location=(0, 0, 0)):
    """Create an empty object for hierarchy organization."""
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=(
        location[0] * Config.SCALE,
        location[1] * Config.SCALE,
        location[2] * Config.SCALE
    ))
    empty = bpy.context.active_object
    empty.name = name
    return empty


# ============================================================================
# COMPONENT CREATION FUNCTIONS
# ============================================================================

def create_reactor_vessel():
    """Create the reactor vessel with inlet/outlet nozzles."""
    parts = []
    
    # Main cylindrical body
    body = create_cylinder(
        "RV_Body",
        Config.RV_RADIUS,
        Config.RV_HEIGHT - Config.RV_RADIUS * 2,  # Subtract for hemispherical heads
        Config.DETAIL_LEVEL,
        (0, 0, Config.RV_HEIGHT / 2)
    )
    parts.append(body)
    
    # Bottom hemispherical head
    bottom_head = create_sphere(
        "RV_BottomHead",
        Config.RV_RADIUS,
        Config.DETAIL_LEVEL,
        (0, 0, Config.RV_RADIUS)
    )
    # Cut to hemisphere (scale Z to 0.5 and adjust position)
    bottom_head.scale[2] = 0.5
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    parts.append(bottom_head)
    
    # Top hemispherical head (closure head)
    top_head = create_sphere(
        "RV_TopHead",
        Config.RV_RADIUS,
        Config.DETAIL_LEVEL,
        (0, 0, Config.RV_HEIGHT - Config.RV_RADIUS * 0.5)
    )
    top_head.scale[2] = 0.5
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    parts.append(top_head)
    
    # Create nozzle stubs (4 inlet, 4 outlet)
    for i in range(4):
        angle = math.radians(45 + i * 90)  # 45°, 135°, 225°, 315°
        
        # Outlet nozzle (hot leg) - slightly higher
        outlet_x = math.cos(angle) * Config.RV_RADIUS
        outlet_y = math.sin(angle) * Config.RV_RADIUS
        
        nozzle_out = create_cylinder(
            f"RV_Outlet_Nozzle_{i+1}",
            Config.HOT_LEG_RADIUS,
            3.0,
            16,
            (outlet_x + math.cos(angle) * 1.5, 
             outlet_y + math.sin(angle) * 1.5, 
             Config.RV_NOZZLE_HEIGHT + 2)
        )
        nozzle_out.rotation_euler[1] = math.pi / 2
        nozzle_out.rotation_euler[2] = angle
        parts.append(nozzle_out)
        
        # Inlet nozzle (cold leg) - slightly lower
        inlet_angle = angle + math.radians(45)  # Offset from outlet
        inlet_x = math.cos(inlet_angle) * Config.RV_RADIUS
        inlet_y = math.sin(inlet_angle) * Config.RV_RADIUS
        
        nozzle_in = create_cylinder(
            f"RV_Inlet_Nozzle_{i+1}",
            Config.COLD_LEG_RADIUS,
            3.0,
            16,
            (inlet_x + math.cos(inlet_angle) * 1.5,
             inlet_y + math.sin(inlet_angle) * 1.5,
             Config.RV_NOZZLE_HEIGHT)
        )
        nozzle_in.rotation_euler[1] = math.pi / 2
        nozzle_in.rotation_euler[2] = inlet_angle
        parts.append(nozzle_in)
    
    # Join all parts
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.join()
    
    rv = bpy.context.active_object
    rv.name = "ReactorVessel"
    
    # Apply material
    mat = create_material("MAT_ReactorVessel", (0.44, 0.5, 0.56), metallic=0.8, roughness=0.3)
    rv.data.materials.append(mat)
    
    return rv


def create_steam_generator(loop_number, angle):
    """Create a steam generator for the specified loop."""
    parts = []
    
    # Calculate position
    x = math.cos(angle) * Config.LOOP_RADIUS
    y = math.sin(angle) * Config.LOOP_RADIUS
    base_z = 0
    
    # Lower shell (narrower)
    lower_shell = create_cylinder(
        f"SG{loop_number}_LowerShell",
        Config.SG_LOWER_RADIUS,
        Config.SG_TRANSITION_HEIGHT,
        Config.DETAIL_LEVEL,
        (x, y, base_z + Config.SG_TRANSITION_HEIGHT / 2)
    )
    parts.append(lower_shell)
    
    # Transition cone
    transition = create_cone(
        f"SG{loop_number}_Transition",
        Config.SG_LOWER_RADIUS,
        Config.SG_UPPER_RADIUS,
        8.0,
        (x, y, base_z + Config.SG_TRANSITION_HEIGHT + 4)
    )
    parts.append(transition)
    
    # Upper shell (wider - steam drum)
    upper_height = Config.SG_HEIGHT - Config.SG_TRANSITION_HEIGHT - 8.0 - Config.SG_UPPER_RADIUS
    upper_shell = create_cylinder(
        f"SG{loop_number}_UpperShell",
        Config.SG_UPPER_RADIUS,
        upper_height,
        Config.DETAIL_LEVEL,
        (x, y, base_z + Config.SG_TRANSITION_HEIGHT + 8 + upper_height / 2)
    )
    parts.append(upper_shell)
    
    # Top dome
    top_dome = create_sphere(
        f"SG{loop_number}_TopDome",
        Config.SG_UPPER_RADIUS,
        Config.DETAIL_LEVEL,
        (x, y, base_z + Config.SG_HEIGHT - Config.SG_UPPER_RADIUS * 0.5)
    )
    top_dome.scale[2] = 0.5
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    parts.append(top_dome)
    
    # Bottom head
    bottom_head = create_sphere(
        f"SG{loop_number}_BottomHead",
        Config.SG_LOWER_RADIUS,
        Config.DETAIL_LEVEL,
        (x, y, base_z + Config.SG_LOWER_RADIUS * 0.5)
    )
    bottom_head.scale[2] = 0.5
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    parts.append(bottom_head)
    
    # Join all parts
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = lower_shell
    bpy.ops.object.join()
    
    sg = bpy.context.active_object
    sg.name = f"SteamGenerator_{loop_number}"
    
    # Apply material
    mat = create_material("MAT_SteamGenerator", (0.75, 0.75, 0.75), metallic=0.7, roughness=0.35)
    if f"MAT_SteamGenerator" not in bpy.data.materials:
        sg.data.materials.append(mat)
    else:
        sg.data.materials.append(bpy.data.materials["MAT_SteamGenerator"])
    
    return sg


def create_rcp(loop_number, angle):
    """Create a reactor coolant pump for the specified loop."""
    parts = []
    
    # Calculate position (offset from SG toward RV)
    sg_x = math.cos(angle) * Config.LOOP_RADIUS
    sg_y = math.sin(angle) * Config.LOOP_RADIUS
    
    # RCP is between SG and RV, offset perpendicular to loop radius
    rcp_radius = Config.LOOP_RADIUS * 0.6
    rcp_angle = angle + math.radians(25)  # Offset angle
    x = math.cos(rcp_angle) * rcp_radius
    y = math.sin(rcp_angle) * rcp_radius
    base_z = Config.RCP_HEIGHT
    
    # Pump casing (volute)
    casing = create_cylinder(
        f"RCP{loop_number}_Casing",
        Config.RCP_BODY_RADIUS,
        Config.RCP_BODY_HEIGHT,
        Config.DETAIL_LEVEL,
        (x, y, base_z + Config.RCP_BODY_HEIGHT / 2)
    )
    parts.append(casing)
    
    # Motor housing
    motor = create_cylinder(
        f"RCP{loop_number}_Motor",
        Config.RCP_MOTOR_RADIUS,
        Config.RCP_MOTOR_HEIGHT,
        Config.DETAIL_LEVEL,
        (x, y, base_z + Config.RCP_BODY_HEIGHT + Config.RCP_MOTOR_HEIGHT / 2)
    )
    parts.append(motor)
    
    # Flywheel at top
    flywheel = create_cylinder(
        f"RCP{loop_number}_Flywheel",
        Config.RCP_MOTOR_RADIUS * 1.3,
        1.5,
        Config.DETAIL_LEVEL,
        (x, y, base_z + Config.RCP_BODY_HEIGHT + Config.RCP_MOTOR_HEIGHT + 0.75)
    )
    parts.append(flywheel)
    
    # Join all parts
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = casing
    bpy.ops.object.join()
    
    rcp = bpy.context.active_object
    rcp.name = f"RCP_{loop_number}"
    
    # Apply material
    mat = create_material("MAT_RCP", (0.3, 0.3, 0.35), metallic=0.6, roughness=0.4)
    if "MAT_RCP" not in bpy.data.materials:
        rcp.data.materials.append(mat)
    else:
        rcp.data.materials.append(bpy.data.materials["MAT_RCP"])
    
    # Store RCP position for piping connections
    rcp["position_x"] = x
    rcp["position_y"] = y
    rcp["position_z"] = base_z
    
    return rcp


def create_pressurizer():
    """Create the pressurizer vessel."""
    parts = []
    
    # Position: Connected to Loop 2, offset from main loop
    # Loop 2 is at 135° (second quadrant)
    pzr_angle = math.radians(135 + 30)  # Offset from Loop 2
    pzr_radius = Config.LOOP_RADIUS * 0.5
    x = math.cos(pzr_angle) * pzr_radius
    y = math.sin(pzr_angle) * pzr_radius
    base_z = Config.PZR_ELEVATION
    
    # Main cylindrical body
    body = create_cylinder(
        "PZR_Body",
        Config.PZR_RADIUS,
        Config.PZR_HEIGHT - Config.PZR_RADIUS * 2,
        Config.DETAIL_LEVEL,
        (x, y, base_z + Config.PZR_HEIGHT / 2)
    )
    parts.append(body)
    
    # Bottom hemispherical head
    bottom_head = create_sphere(
        "PZR_BottomHead",
        Config.PZR_RADIUS,
        Config.DETAIL_LEVEL,
        (x, y, base_z + Config.PZR_RADIUS)
    )
    bottom_head.scale[2] = 0.5
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    parts.append(bottom_head)
    
    # Top hemispherical head
    top_head = create_sphere(
        "PZR_TopHead",
        Config.PZR_RADIUS,
        Config.DETAIL_LEVEL,
        (x, y, base_z + Config.PZR_HEIGHT - Config.PZR_RADIUS * 0.5)
    )
    top_head.scale[2] = 0.5
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    parts.append(top_head)
    
    # Join all parts
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.join()
    
    pzr = bpy.context.active_object
    pzr.name = "Pressurizer"
    
    # Apply material (gold/yellow tint)
    mat = create_material("MAT_Pressurizer", (0.85, 0.7, 0.2), metallic=0.7, roughness=0.3)
    pzr.data.materials.append(mat)
    
    # Store position for surge line
    pzr["position_x"] = x
    pzr["position_y"] = y
    pzr["position_z"] = base_z
    
    return pzr


def create_hot_leg(loop_number, angle):
    """Create a hot leg pipe from RV to SG."""
    # Start point: RV outlet nozzle
    rv_x = math.cos(angle) * (Config.RV_RADIUS + 2)
    rv_y = math.sin(angle) * (Config.RV_RADIUS + 2)
    rv_z = Config.RV_NOZZLE_HEIGHT + 2
    
    # End point: SG inlet (lower portion)
    sg_x = math.cos(angle) * Config.LOOP_RADIUS
    sg_y = math.sin(angle) * Config.LOOP_RADIUS
    sg_z = Config.SG_INLET_HEIGHT
    
    # Create straight pipe section
    # Calculate length and direction
    dx = sg_x - rv_x
    dy = sg_y - rv_y
    dz = sg_z - rv_z
    length = math.sqrt(dx*dx + dy*dy + dz*dz)
    
    # Create cylinder
    pipe = create_cylinder(
        f"HotLeg_{loop_number}",
        Config.HOT_LEG_RADIUS,
        length,
        Config.DETAIL_LEVEL,
        ((rv_x + sg_x) / 2, (rv_y + sg_y) / 2, (rv_z + sg_z) / 2)
    )
    
    # Calculate rotation to align with direction vector
    # Default cylinder is along Z axis, we need to rotate to (dx, dy, dz)
    direction = Vector((dx, dy, dz)).normalized()
    up = Vector((0, 0, 1))
    
    # Use quaternion rotation
    rot_quat = up.rotation_difference(direction)
    pipe.rotation_mode = 'QUATERNION'
    pipe.rotation_quaternion = rot_quat
    
    # Apply material (hot - red/orange)
    mat = create_material(f"MAT_HotLeg", (1.0, 0.27, 0.0), metallic=0.6, roughness=0.35)
    if "MAT_HotLeg" not in bpy.data.materials:
        pipe.data.materials.append(mat)
    else:
        pipe.data.materials.append(bpy.data.materials["MAT_HotLeg"])
    
    return pipe


def create_cold_leg(loop_number, angle, rcp_pos):
    """Create a cold leg pipe from RCP to RV."""
    # Start point: RCP discharge
    rcp_x = rcp_pos[0]
    rcp_y = rcp_pos[1]
    rcp_z = rcp_pos[2] + Config.RCP_BODY_HEIGHT / 2
    
    # End point: RV inlet nozzle
    inlet_angle = angle + math.radians(45)  # Inlet offset from outlet
    rv_x = math.cos(inlet_angle) * (Config.RV_RADIUS + 2)
    rv_y = math.sin(inlet_angle) * (Config.RV_RADIUS + 2)
    rv_z = Config.RV_NOZZLE_HEIGHT
    
    # Create straight pipe section
    dx = rv_x - rcp_x
    dy = rv_y - rcp_y
    dz = rv_z - rcp_z
    length = math.sqrt(dx*dx + dy*dy + dz*dz)
    
    pipe = create_cylinder(
        f"ColdLeg_{loop_number}",
        Config.COLD_LEG_RADIUS,
        length,
        Config.DETAIL_LEVEL,
        ((rcp_x + rv_x) / 2, (rcp_y + rv_y) / 2, (rcp_z + rv_z) / 2)
    )
    
    direction = Vector((dx, dy, dz)).normalized()
    up = Vector((0, 0, 1))
    rot_quat = up.rotation_difference(direction)
    pipe.rotation_mode = 'QUATERNION'
    pipe.rotation_quaternion = rot_quat
    
    # Apply material (cold - blue)
    mat = create_material("MAT_ColdLeg", (0.12, 0.56, 1.0), metallic=0.6, roughness=0.35)
    if "MAT_ColdLeg" not in bpy.data.materials:
        pipe.data.materials.append(mat)
    else:
        pipe.data.materials.append(bpy.data.materials["MAT_ColdLeg"])
    
    return pipe


def create_crossover_leg(loop_number, angle, rcp_pos):
    """Create a crossover leg pipe from SG to RCP."""
    # Start point: SG outlet (bottom)
    sg_x = math.cos(angle) * Config.LOOP_RADIUS
    sg_y = math.sin(angle) * Config.LOOP_RADIUS
    sg_z = Config.SG_LOWER_RADIUS + 2
    
    # End point: RCP suction
    rcp_x = rcp_pos[0]
    rcp_y = rcp_pos[1]
    rcp_z = rcp_pos[2]
    
    # Create pipe
    dx = rcp_x - sg_x
    dy = rcp_y - sg_y
    dz = rcp_z - sg_z
    length = math.sqrt(dx*dx + dy*dy + dz*dz)
    
    pipe = create_cylinder(
        f"CrossoverLeg_{loop_number}",
        Config.CROSSOVER_RADIUS,
        length,
        Config.DETAIL_LEVEL,
        ((sg_x + rcp_x) / 2, (sg_y + rcp_y) / 2, (sg_z + rcp_z) / 2)
    )
    
    direction = Vector((dx, dy, dz)).normalized()
    up = Vector((0, 0, 1))
    rot_quat = up.rotation_difference(direction)
    pipe.rotation_mode = 'QUATERNION'
    pipe.rotation_quaternion = rot_quat
    
    # Apply material (transition color - cyan)
    mat = create_material("MAT_CrossoverLeg", (0.0, 0.8, 0.8), metallic=0.6, roughness=0.35)
    if "MAT_CrossoverLeg" not in bpy.data.materials:
        pipe.data.materials.append(mat)
    else:
        pipe.data.materials.append(bpy.data.materials["MAT_CrossoverLeg"])
    
    return pipe


def create_surge_line(pzr_pos):
    """Create the surge line from pressurizer to Loop 2 hot leg."""
    # Start: Pressurizer bottom
    pzr_x = pzr_pos[0]
    pzr_y = pzr_pos[1]
    pzr_z = pzr_pos[2]
    
    # End: Loop 2 hot leg (at angle 135°)
    loop2_angle = math.radians(135)
    hl_x = math.cos(loop2_angle) * (Config.RV_RADIUS + 5)
    hl_y = math.sin(loop2_angle) * (Config.RV_RADIUS + 5)
    hl_z = Config.RV_NOZZLE_HEIGHT + 2
    
    # Create pipe
    dx = hl_x - pzr_x
    dy = hl_y - pzr_y
    dz = hl_z - pzr_z
    length = math.sqrt(dx*dx + dy*dy + dz*dz)
    
    pipe = create_cylinder(
        "SurgeLine",
        Config.SURGE_LINE_RADIUS,
        length,
        16,
        ((pzr_x + hl_x) / 2, (pzr_y + hl_y) / 2, (pzr_z + hl_z) / 2)
    )
    
    direction = Vector((dx, dy, dz)).normalized()
    up = Vector((0, 0, 1))
    rot_quat = up.rotation_difference(direction)
    pipe.rotation_mode = 'QUATERNION'
    pipe.rotation_quaternion = rot_quat
    
    # Apply material (same as hot leg - connected to hot leg)
    mat = bpy.data.materials.get("MAT_HotLeg")
    if mat:
        pipe.data.materials.append(mat)
    
    return pipe


def create_flow_arrows():
    """Create flow direction arrows for animation."""
    arrows = []
    
    # Create arrow material (bright green for visibility)
    arrow_mat = create_material("MAT_FlowArrow", (0.0, 1.0, 0.3), metallic=0.2, roughness=0.8,
                                 emission_color=(0.0, 1.0, 0.3), emission_strength=2.0)
    
    # Create arrows for each loop
    for i in range(4):
        angle = math.radians(45 + i * 90)
        
        # Hot leg arrow (pointing away from RV)
        hl_x = math.cos(angle) * (Config.RV_RADIUS + 10)
        hl_y = math.sin(angle) * (Config.RV_RADIUS + 10)
        hl_z = Config.RV_NOZZLE_HEIGHT + 2
        
        arrow = create_arrow(
            f"FlowArrow_HotLeg_{i+1}",
            Config.ARROW_LENGTH,
            Config.ARROW_RADIUS,
            (hl_x, hl_y, hl_z),
            (0, math.pi/2, angle)
        )
        arrow.data.materials.append(arrow_mat)
        arrows.append(arrow)
        
        # Cold leg arrow (pointing toward RV)
        inlet_angle = angle + math.radians(45)
        cl_x = math.cos(inlet_angle) * (Config.RV_RADIUS + 10)
        cl_y = math.sin(inlet_angle) * (Config.RV_RADIUS + 10)
        cl_z = Config.RV_NOZZLE_HEIGHT
        
        arrow = create_arrow(
            f"FlowArrow_ColdLeg_{i+1}",
            Config.ARROW_LENGTH,
            Config.ARROW_RADIUS,
            (cl_x, cl_y, cl_z),
            (0, math.pi/2, inlet_angle + math.pi)  # Point toward RV
        )
        arrow.data.materials.append(arrow_mat)
        arrows.append(arrow)
    
    return arrows


def create_loop_labels():
    """Create text labels for each loop."""
    labels = []
    
    for i in range(4):
        angle = math.radians(45 + i * 90)
        x = math.cos(angle) * (Config.LOOP_RADIUS + 10)
        y = math.sin(angle) * (Config.LOOP_RADIUS + 10)
        
        # Create text object
        bpy.ops.object.text_add(location=(x * Config.SCALE, y * Config.SCALE, 70 * Config.SCALE))
        text = bpy.context.active_object
        text.name = f"Label_Loop_{i+1}"
        text.data.body = f"LOOP {i+1}"
        text.data.size = 3.0
        text.data.align_x = 'CENTER'
        
        # Rotate to face outward
        text.rotation_euler[0] = math.pi / 2
        text.rotation_euler[2] = angle + math.pi / 2
        
        labels.append(text)
    
    return labels


# ============================================================================
# MAIN FUNCTION
# ============================================================================

def main():
    """Main function to create the complete RCS model."""
    print("=" * 60)
    print("CRITICAL: RCS Primary Loop Model Generator")
    print("Creating Westinghouse 4-Loop PWR Reactor Coolant System...")
    print("=" * 60)
    
    # Clear existing scene
    print("Clearing scene...")
    clear_scene()
    
    # Create root empty for hierarchy
    print("Creating hierarchy...")
    root = create_empty("RCS_Primary_Loop")
    
    # Create component groups
    components_group = create_empty("Components")
    piping_group = create_empty("Piping")
    arrows_group = create_empty("FlowArrows")
    labels_group = create_empty("Labels")
    
    components_group.parent = root
    piping_group.parent = root
    arrows_group.parent = root
    labels_group.parent = root
    
    # Create Reactor Vessel
    print("Creating Reactor Vessel...")
    rv = create_reactor_vessel()
    rv.parent = components_group
    
    # Create components for each loop
    rcp_positions = []
    
    for i in range(4):
        loop_num = i + 1
        angle = math.radians(45 + i * 90)  # 45°, 135°, 225°, 315°
        
        print(f"Creating Loop {loop_num} components...")
        
        # Steam Generator
        sg = create_steam_generator(loop_num, angle)
        sg.parent = components_group
        
        # Reactor Coolant Pump
        rcp = create_rcp(loop_num, angle)
        rcp.parent = components_group
        
        # Store RCP position for piping
        rcp_positions.append((rcp["position_x"], rcp["position_y"], rcp["position_z"]))
    
    # Create Pressurizer
    print("Creating Pressurizer...")
    pzr = create_pressurizer()
    pzr.parent = components_group
    pzr_pos = (pzr["position_x"], pzr["position_y"], pzr["position_z"])
    
    # Create piping for each loop
    print("Creating piping...")
    for i in range(4):
        loop_num = i + 1
        angle = math.radians(45 + i * 90)
        
        # Hot Leg
        hot_leg = create_hot_leg(loop_num, angle)
        hot_leg.parent = piping_group
        
        # Crossover Leg
        crossover = create_crossover_leg(loop_num, angle, rcp_positions[i])
        crossover.parent = piping_group
        
        # Cold Leg
        cold_leg = create_cold_leg(loop_num, angle, rcp_positions[i])
        cold_leg.parent = piping_group
    
    # Create Surge Line
    print("Creating Surge Line...")
    surge = create_surge_line(pzr_pos)
    surge.parent = piping_group
    
    # Create Flow Arrows
    print("Creating flow arrows...")
    arrows = create_flow_arrows()
    for arrow in arrows:
        arrow.parent = arrows_group
    
    # Create Labels
    print("Creating labels...")
    labels = create_loop_labels()
    for label in labels:
        label.parent = labels_group
    
    # Select root object
    bpy.ops.object.select_all(action='DESELECT')
    root.select_set(True)
    bpy.context.view_layer.objects.active = root
    
    # Set up camera and lighting
    print("Setting up scene...")
    
    # Add sun light
    bpy.ops.object.light_add(type='SUN', location=(50, 50, 100))
    sun = bpy.context.active_object
    sun.name = "Sun"
    sun.data.energy = 3.0
    
    # Add camera
    bpy.ops.object.camera_add(location=(100, -100, 80))
    camera = bpy.context.active_object
    camera.name = "Camera"
    camera.rotation_euler = (math.radians(60), 0, math.radians(45))
    bpy.context.scene.camera = camera
    
    print("=" * 60)
    print("RCS Primary Loop model created successfully!")
    print("")
    print("EXPORT INSTRUCTIONS:")
    print("1. Select 'RCS_Primary_Loop' in the Outliner")
    print("2. File > Export > FBX (.fbx)")
    print("3. Enable 'Selected Objects' in export settings")
    print("4. Set Scale to 1.0, Apply Scalings to 'FBX All'")
    print("5. Enable 'Bake Animation' if needed")
    print("6. Save as 'RCS_Primary_Loop.fbx'")
    print("=" * 60)


# ============================================================================
# RUN SCRIPT
# ============================================================================

if __name__ == "__main__":
    main()
