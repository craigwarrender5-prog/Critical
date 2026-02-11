# ============================================================================
# CRITICAL: Master the Atom — Blender 5.0 Panel Render Script
# render_panel_textures.py
# ============================================================================
#
# PURPOSE:
#   Renders the Reactor Operator Panel model (created by create_reactor_panel.py)
#   to a high-resolution PNG texture for use as a UI background in Unity.
#
# PREREQUISITES:
#   - Must be run in the same Blender file where create_reactor_panel.py was run
#   - The panel model must exist in the scene
#
# OUTPUT:
#   - panel_base_color.png (3840x2160, RGBA with transparent display areas)
#
# USAGE:
#   1. Open the .blend file saved after running create_reactor_panel.py
#   2. Switch to Scripting workspace
#   3. Open this script
#   4. Run with Alt+P or Play button
#   5. Wait for render to complete (1-5 minutes)
#   6. Output saved automatically to Assets/Textures/ReactorOperatorPanel/
#
# CREATED: v4.0.0
# COMPATIBLE: Blender 5.0+
# ============================================================================

import bpy
import os
import math

# ============================================================================
# CONFIGURATION
# ============================================================================

# Panel dimensions (must match create_reactor_panel.py)
PANEL_WIDTH = 19.20
PANEL_HEIGHT = 10.80

# Render resolution (4K for crisp quality at 1080p)
RENDER_WIDTH = 3840
RENDER_HEIGHT = 2160

# Output path — adjust if your project is in a different location
# This tries to find the Unity project automatically
OUTPUT_DIR = None  # Will be auto-detected

# Render quality
RENDER_SAMPLES = 128     # Cycles samples (higher = better quality, slower)
RENDER_DEVICE = 'GPU'    # 'GPU' or 'CPU'
USE_DENOISING = True

# ============================================================================
# AUTO-DETECT OUTPUT DIRECTORY
# ============================================================================

def find_output_dir():
    """Try to find the Unity project Textures directory."""
    global OUTPUT_DIR
    
    # Common locations to check
    candidates = [
        r"C:\Users\craig\Projects\Critical\Assets\Resources\ReactorOperatorPanel",
        r"C:\Users\craig\Projects\Critical\Assets\Textures\ReactorOperatorPanel",
        os.path.join(os.path.expanduser("~"), "Projects", "Critical", 
                     "Assets", "Resources", "ReactorOperatorPanel"),
        os.path.join(os.path.expanduser("~"), "Projects", "Critical", 
                     "Assets", "Textures", "ReactorOperatorPanel"),
    ]
    
    # Also check relative to the .blend file location
    blend_path = bpy.data.filepath
    if blend_path:
        project_root = os.path.dirname(os.path.dirname(os.path.dirname(blend_path)))
        candidates.insert(0, os.path.join(project_root, "Textures", "ReactorOperatorPanel"))
    
    for path in candidates:
        if os.path.isdir(path):
            OUTPUT_DIR = path
            print(f"  Output directory found: {OUTPUT_DIR}")
            return True
        # Try to create it
        parent = os.path.dirname(path)
        if os.path.isdir(parent):
            try:
                os.makedirs(path, exist_ok=True)
                OUTPUT_DIR = path
                print(f"  Output directory created: {OUTPUT_DIR}")
                return True
            except:
                pass
    
    # Fallback: save next to .blend file
    if blend_path:
        OUTPUT_DIR = os.path.dirname(blend_path)
        print(f"  Output directory (fallback): {OUTPUT_DIR}")
        return True
    
    # Last resort: temp directory
    OUTPUT_DIR = os.path.join(os.path.expanduser("~"), "Desktop")
    print(f"  Output directory (desktop fallback): {OUTPUT_DIR}")
    return True


# ============================================================================
# RENDER SETUP
# ============================================================================

def setup_camera():
    """Create and position an orthographic camera facing the panel straight-on."""
    
    # Remove existing cameras
    for obj in bpy.data.objects:
        if obj.type == 'CAMERA':
            bpy.data.objects.remove(obj, do_unlink=True)
    
    # Create new camera
    bpy.ops.object.camera_add(location=(PANEL_WIDTH/2, PANEL_HEIGHT/2, 8.0))
    camera = bpy.context.active_object
    camera.name = "RenderCamera"
    
    # Set to orthographic
    camera.data.type = 'ORTHO'
    
    # Set orthographic scale to fit the panel exactly
    # The ortho_scale is the width of the camera view in Blender units
    # We need to account for the aspect ratio
    aspect = RENDER_WIDTH / RENDER_HEIGHT
    panel_aspect = PANEL_WIDTH / PANEL_HEIGHT
    
    if panel_aspect > aspect:
        # Panel is wider than render — fit to width
        camera.data.ortho_scale = PANEL_WIDTH * 1.01  # Tiny margin
    else:
        # Panel is taller than render — fit to height
        camera.data.ortho_scale = PANEL_HEIGHT * aspect * 1.01
    
    # Point straight down at panel (camera looks along -Z)
    camera.rotation_euler = (0, 0, 0)  # Facing -Z by default after camera_add
    
    # Actually, camera_add creates camera facing -Z in local space,
    # but we need it facing straight down at the panel (panel is on XY plane)
    # Camera at Z=8 looking down -Z will see the panel correctly
    camera.rotation_euler = (0, 0, 0)
    
    # Set as active camera
    bpy.context.scene.camera = camera
    
    print(f"  Camera: Orthographic, scale={camera.data.ortho_scale:.2f}")
    
    return camera


def setup_render_settings():
    """Configure Cycles render settings for high-quality output."""
    
    scene = bpy.context.scene
    
    # Set render engine to Cycles
    scene.render.engine = 'CYCLES'
    
    # Device
    cycles = scene.cycles
    cycles.device = RENDER_DEVICE
    
    # If GPU, try to enable it
    if RENDER_DEVICE == 'GPU':
        try:
            prefs = bpy.context.preferences.addons['cycles'].preferences
            # Try CUDA first, then OptiX, then HIP
            for compute_type in ['OPTIX', 'CUDA', 'HIP', 'METAL']:
                try:
                    prefs.compute_device_type = compute_type
                    prefs.get_devices()
                    for device in prefs.devices:
                        device.use = True
                    print(f"  GPU compute: {compute_type}")
                    break
                except:
                    continue
        except:
            print("  GPU not available, falling back to CPU")
            cycles.device = 'CPU'
    
    # Samples
    cycles.samples = RENDER_SAMPLES
    cycles.preview_samples = 32
    
    # Denoising
    if USE_DENOISING:
        cycles.use_denoising = True
        cycles.denoiser = 'OPENIMAGEDENOISE'
    
    # Resolution
    scene.render.resolution_x = RENDER_WIDTH
    scene.render.resolution_y = RENDER_HEIGHT
    scene.render.resolution_percentage = 100
    
    # Output format — PNG with alpha
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.image_settings.color_depth = '16'  # 16-bit for quality
    scene.render.image_settings.compression = 15     # PNG compression (0-100)
    
    # Transparent background (so display areas show as transparent)
    scene.render.film_transparent = True
    cycles.film_transparent_glass = True
    
    # Color management — standard sRGB for Unity compatibility
    scene.display_settings.display_device = 'sRGB'
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'None'
    
    print(f"  Render: {RENDER_WIDTH}x{RENDER_HEIGHT}, {RENDER_SAMPLES} samples")
    print(f"  Format: PNG RGBA 16-bit, transparent background")


def make_recess_materials_transparent():
    """Make the recess/display area materials use alpha transparency.
    
    This ensures that where dynamic Unity UI elements will overlay,
    the rendered texture is transparent.
    """
    
    mat = bpy.data.materials.get("Recess_Display")
    if mat and mat.use_nodes:
        nodes = mat.node_tree.nodes
        links = mat.node_tree.links
        
        bsdf = nodes.get("Principled BSDF")
        output = nodes.get("Material Output")
        
        if bsdf and output:
            # Make this material render as fully transparent
            # by mixing with a Transparent BSDF
            
            # Create Transparent BSDF
            trans = nodes.new(type='ShaderNodeBsdfTransparent')
            trans.location = (-200, -200)
            
            # Create Mix Shader
            mix = nodes.new(type='ShaderNodeMixShader')
            mix.location = (200, 0)
            mix.inputs[0].default_value = 0.85  # 85% transparent, 15% visible
            
            # Reconnect: BSDF → Mix input 1, Transparent → Mix input 2
            # Remove existing link from BSDF to Output
            for link in links:
                if link.to_node == output:
                    links.remove(link)
            
            links.new(bsdf.outputs[0], mix.inputs[1])
            links.new(trans.outputs[0], mix.inputs[2])
            links.new(mix.outputs[0], output.inputs[0])
            
            mat.blend_method = 'BLEND' if hasattr(mat, 'blend_method') else None
            
            print("  Recess materials set to semi-transparent")


# ============================================================================
# RENDER EXECUTION
# ============================================================================

def render_panel():
    """Execute the panel render."""
    
    print("=" * 60)
    print("  CRITICAL: Rendering Reactor Operator Panel Texture")
    print("=" * 60)
    
    # Verify panel exists
    panel_root = bpy.data.objects.get("ReactorOperatorPanel")
    if not panel_root:
        print("")
        print("  ERROR: Panel model not found in scene!")
        print("  Run create_reactor_panel.py first, then save the .blend file.")
        print("")
        return False
    
    # Find output directory
    if not find_output_dir():
        print("  ERROR: Could not find output directory!")
        return False
    
    # Setup
    print("\n  Setting up camera...")
    setup_camera()
    
    print("  Configuring render settings...")
    setup_render_settings()
    
    print("  Configuring transparent display areas...")
    make_recess_materials_transparent()
    
    # Output path
    output_path = os.path.join(OUTPUT_DIR, "panel_base_color.png")
    bpy.context.scene.render.filepath = output_path
    
    print(f"\n  Rendering to: {output_path}")
    print("  This may take 1-5 minutes depending on your hardware...")
    print("")
    
    # Render
    bpy.ops.render.render(write_still=True)
    
    print("")
    print("=" * 60)
    print("  Render complete!")
    print(f"  Output: {output_path}")
    print("")
    print("  Next steps:")
    print("  1. Switch to Unity")
    print("  2. Wait for auto-import (or right-click > Refresh in Project)")
    print("  3. Configure texture import settings (see Instruction Manual)")
    print("  4. Run: Critical > Apply Operator Screen Skin")
    print("=" * 60)
    
    return True


# ============================================================================
# RUN
# ============================================================================

if __name__ == "__main__":
    render_panel()
else:
    render_panel()
