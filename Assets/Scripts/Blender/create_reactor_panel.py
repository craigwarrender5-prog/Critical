# ============================================================================
# CRITICAL: Master the Atom — Blender 5.0 Panel Creation Script
# create_reactor_panel.py
# ============================================================================
#
# PURPOSE:
#   Procedurally creates the Reactor Operator Screen mimic board panel model
#   in Blender 5.0. This creates the static "hardware" of the panel:
#   bezels, frames, recessed display areas, section dividers, and engraved
#   labels. Dynamic elements (gauges, core map, buttons) are handled in Unity.
#
# USAGE:
#   1. Open Blender 5.0
#   2. Switch to Scripting workspace
#   3. Delete default scene objects (A → X → Delete)
#   4. Open this script in the text editor
#   5. Run with Alt+P or the Play button
#   6. Save as ReactorOperatorPanel.blend
#
# LAYOUT (matches Unity OperatorScreenBuilder.cs anchors):
#   Left Gauge Panel:    0.00 - 0.15 x,  0.26 - 1.00 y  (9 bezels)
#   Core Map Panel:      0.15 - 0.65 x,  0.26 - 1.00 y  (central frame)
#   Right Gauge Panel:   0.65 - 0.80 x,  0.26 - 1.00 y  (8 bezels)
#   Detail Panel:        0.80 - 1.00 x,  0.26 - 1.00 y  (info area)
#   Bottom Panel:        0.00 - 1.00 x,  0.00 - 0.26 y  (controls)
#
# OUTPUT:
#   3D model suitable for rendering to texture via render_panel_textures.py
#
# CREATED: v4.0.0
# CHANGE: v4.2.2 — Fixed Bank Positions frame to upper row only (was full height,
#         overlapping alarm strip). Replaced split alarm strip with full-width
#         annunciator panel frame containing 4x4 tile recess grid with engraved labels.
# COMPATIBLE: Blender 5.0+ (uses stable bpy API only)
# ============================================================================

import bpy
import bmesh
import math
from mathutils import Vector

# ============================================================================
# CONFIGURATION
# ============================================================================

# Panel dimensions (in Blender units, proportional to 1920x1080)
# Using 1 Blender unit = 100 pixels for clean math
PANEL_WIDTH = 19.20    # 1920 px
PANEL_HEIGHT = 10.80   # 1080 px
PANEL_DEPTH = 0.15     # Thickness of the main panel

# Bezel dimensions
BEZEL_DEPTH = 0.08        # How far bezels protrude from panel
BEZEL_BEVEL = 0.02        # Bevel radius on bezel edges
RECESS_DEPTH = 0.06       # How deep display cutouts are recessed
SECTION_DIVIDER_WIDTH = 0.03

# Colors (linear RGB for Blender materials)
COL_PANEL_BASE = (0.025, 0.025, 0.032, 1.0)       # Dark gunmetal #1A1A1F approx
COL_BEZEL = (0.04, 0.04, 0.055, 1.0)               # Slightly lighter bezel
COL_RECESS = (0.012, 0.012, 0.018, 1.0)            # Very dark recess (display area)
COL_ACCENT_GREEN = (0.0, 0.15, 0.02, 1.0)          # Subtle green accent
COL_ACCENT_RED = (0.15, 0.02, 0.02, 1.0)           # Subtle red accent
COL_LABEL = (0.08, 0.09, 0.10, 1.0)                # Engraved label color
COL_DIVIDER = (0.05, 0.05, 0.065, 1.0)             # Section divider

# Text settings
LABEL_DEPTH = 0.005   # How deep labels are engraved


# ============================================================================
# UTILITY FUNCTIONS
# ============================================================================

def clear_scene():
    """Remove all objects from the scene."""
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    
    # Clean up orphan data
    for mesh in bpy.data.meshes:
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    for mat in bpy.data.materials:
        if mat.users == 0:
            bpy.data.materials.remove(mat)


def create_material(name, base_color, metallic=0.3, roughness=0.7, 
                    specular=0.5, normal_strength=0.0):
    """Create a PBR material with Principled BSDF."""
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    
    # Get the Principled BSDF node
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    
    if bsdf:
        bsdf.inputs["Base Color"].default_value = base_color
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        # Specular IOR Level replaces old Specular in Blender 4.0+
        if "Specular IOR Level" in bsdf.inputs:
            bsdf.inputs["Specular IOR Level"].default_value = specular
    
    return mat


def create_box(name, x, y, z, width, height, depth, material=None, 
               bevel_width=0.0, parent=None):
    """Create a box mesh at the specified position.
    
    x, y are in panel coordinates (0,0 = bottom-left of panel).
    z is the vertical offset from the panel surface.
    """
    bpy.ops.mesh.primitive_cube_add(
        size=1.0,
        location=(x + width/2, y + height/2, z + depth/2)
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (width, height, depth)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    
    # Apply bevel modifier if requested
    if bevel_width > 0:
        bevel = obj.modifiers.new(name="Bevel", type='BEVEL')
        bevel.width = bevel_width
        bevel.segments = 3
        bevel.limit_method = 'ANGLE'
        bevel.angle_limit = math.radians(60)
    
    # Assign material
    if material:
        obj.data.materials.append(material)
    
    # Parent
    if parent:
        obj.parent = parent
    
    return obj


def create_recessed_area(name, x, y, width, height, material=None, parent=None):
    """Create a recessed display area (cutout in the panel surface)."""
    return create_box(
        name, x, y, 
        -RECESS_DEPTH,  # Below panel surface
        width, height, RECESS_DEPTH,
        material=material,
        parent=parent
    )


def create_bezel(name, x, y, width, height, material=None, parent=None):
    """Create a raised bezel frame around a display area."""
    frame_w = 0.04  # Frame width (border thickness)
    
    # The bezel is a raised frame — 4 bars around a cutout
    bezel_mat = material
    recess_mat = mat_recess
    
    objs = []
    
    # Top bar
    objs.append(create_box(
        f"{name}_top", x, y + height - frame_w, 0,
        width, frame_w, BEZEL_DEPTH,
        material=bezel_mat, bevel_width=BEZEL_BEVEL, parent=parent
    ))
    
    # Bottom bar
    objs.append(create_box(
        f"{name}_bot", x, y, 0,
        width, frame_w, BEZEL_DEPTH,
        material=bezel_mat, bevel_width=BEZEL_BEVEL, parent=parent
    ))
    
    # Left bar
    objs.append(create_box(
        f"{name}_left", x, y, 0,
        frame_w, height, BEZEL_DEPTH,
        material=bezel_mat, bevel_width=BEZEL_BEVEL, parent=parent
    ))
    
    # Right bar
    objs.append(create_box(
        f"{name}_right", x + width - frame_w, y, 0,
        frame_w, height, BEZEL_DEPTH,
        material=bezel_mat, bevel_width=BEZEL_BEVEL, parent=parent
    ))
    
    # Recessed interior
    objs.append(create_recessed_area(
        f"{name}_display",
        x + frame_w, y + frame_w,
        width - 2*frame_w, height - 2*frame_w,
        material=recess_mat, parent=parent
    ))
    
    return objs


def create_gauge_bezel(name, x, y, width, height, parent=None):
    """Create a single gauge bezel with display recess."""
    return create_bezel(name, x, y, width, height, 
                       material=mat_bezel, parent=parent)


def create_section_divider(x, y, width, height, parent=None):
    """Create a subtle raised divider line."""
    return create_box(
        "Divider", x, y, 0,
        width, height, 0.02,
        material=mat_divider, bevel_width=0.005, parent=parent
    )


def create_text_label(name, text, x, y, z, size=0.15, parent=None):
    """Create an engraved text label on the panel surface.
    
    The text is created as a mesh object with slight depth.
    """
    bpy.ops.object.text_add(location=(x, y, z))
    txt_obj = bpy.context.active_object
    txt_obj.name = name
    txt_obj.data.body = text
    txt_obj.data.size = size
    txt_obj.data.extrude = LABEL_DEPTH
    txt_obj.data.align_x = 'CENTER'
    txt_obj.data.align_y = 'CENTER'
    
    # Set font (use default)
    # Blender's default font is Bfont which works fine for panel labels
    
    # Material
    txt_obj.data.materials.append(mat_label)
    
    if parent:
        txt_obj.parent = parent
    
    return txt_obj


def anchor_to_panel(ax, ay, aw=0, ah=0):
    """Convert Unity anchor coordinates (0-1) to panel coordinates.
    
    Returns (x, y, width, height) in Blender panel units.
    """
    x = ax * PANEL_WIDTH
    y = ay * PANEL_HEIGHT
    w = aw * PANEL_WIDTH
    h = ah * PANEL_HEIGHT
    return x, y, w, h


# ============================================================================
# MAIN PANEL CONSTRUCTION
# ============================================================================

def build_panel():
    """Build the complete Reactor Operator Screen panel."""
    
    print("=" * 60)
    print("  CRITICAL: Building Reactor Operator Panel")
    print("=" * 60)
    
    clear_scene()
    
    # ------------------------------------------------------------------
    # Create materials
    # ------------------------------------------------------------------
    global mat_panel, mat_bezel, mat_recess, mat_label, mat_divider
    global mat_accent_green, mat_accent_red
    
    mat_panel = create_material("Panel_Base", COL_PANEL_BASE, 
                                metallic=0.35, roughness=0.65)
    mat_bezel = create_material("Bezel", COL_BEZEL, 
                                metallic=0.5, roughness=0.5)
    mat_recess = create_material("Recess_Display", COL_RECESS, 
                                  metallic=0.1, roughness=0.9)
    mat_label = create_material("Label_Engraved", COL_LABEL,
                                 metallic=0.2, roughness=0.8)
    mat_divider = create_material("Divider", COL_DIVIDER,
                                   metallic=0.4, roughness=0.6)
    mat_accent_green = create_material("Accent_Green", COL_ACCENT_GREEN,
                                        metallic=0.3, roughness=0.6)
    mat_accent_red = create_material("Accent_Red", COL_ACCENT_RED,
                                      metallic=0.3, roughness=0.6)
    
    # ------------------------------------------------------------------
    # Create empty parent for organization
    # ------------------------------------------------------------------
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=(0, 0, 0))
    panel_root = bpy.context.active_object
    panel_root.name = "ReactorOperatorPanel"
    
    # ------------------------------------------------------------------
    # Main panel body (the flat background plate)
    # ------------------------------------------------------------------
    print("  Creating main panel body...")
    
    main_panel = create_box(
        "MainPanel", 0, 0, -PANEL_DEPTH,
        PANEL_WIDTH, PANEL_HEIGHT, PANEL_DEPTH,
        material=mat_panel, bevel_width=0.03, parent=panel_root
    )
    
    # ------------------------------------------------------------------
    # LEFT GAUGE PANEL — 9 nuclear instrumentation gauge bezels
    # Unity anchors: (0.00, 0.26) to (0.15, 1.00)
    # ------------------------------------------------------------------
    print("  Creating left gauge panel (9 bezels)...")
    
    lx, ly, lw, lh = anchor_to_panel(0.00, 0.26, 0.15, 0.74)
    
    # Section label
    create_text_label("Label_LeftPanel", "NUCLEAR INSTRUMENTATION",
                      lx + lw/2, ly + lh - 0.1, 0.001, size=0.12,
                      parent=panel_root)
    
    # 9 gauge bezels, evenly spaced vertically
    gauge_names_left = [
        "NeutronPower", "ThermalPower", "StartupRate", "Period",
        "Reactivity", "Keff", "Boron", "Xenon", "RCSFlow"
    ]
    
    gauge_padding = 0.08
    gauge_top_margin = 0.35   # Space for section label
    available_height = lh - gauge_top_margin - gauge_padding
    gauge_h = (available_height - gauge_padding * 8) / 9  # 9 gauges, 8 gaps
    gauge_w = lw - gauge_padding * 2
    
    for i, gname in enumerate(gauge_names_left):
        gx = lx + gauge_padding
        gy = ly + lh - gauge_top_margin - (i + 1) * (gauge_h + gauge_padding) + gauge_padding
        create_gauge_bezel(f"Gauge_L_{gname}", gx, gy, gauge_w, gauge_h,
                          parent=panel_root)
    
    # ------------------------------------------------------------------
    # CORE MAP PANEL — Central core mosaic display
    # Unity anchors: (0.15, 0.26) to (0.65, 1.00)
    # ------------------------------------------------------------------
    print("  Creating core map panel...")
    
    cx, cy, cw, ch = anchor_to_panel(0.15, 0.26, 0.50, 0.74)
    
    # Section label
    create_text_label("Label_CoreMap", "REACTOR CORE",
                      cx + cw/2, cy + ch - 0.1, 0.001, size=0.18,
                      parent=panel_root)
    
    # Display mode button housings (top row)
    # Unity anchors within core panel: y 0.90-0.98
    btn_row_y = cy + ch * 0.90
    btn_row_h = ch * 0.08
    btn_w = (cw * 0.90) / 4  # 4 buttons
    btn_start_x = cx + cw * 0.05
    
    mode_labels = ["POWER", "FUEL TEMP", "COOLANT", "ROD BANKS"]
    for i, label in enumerate(mode_labels):
        bx = btn_start_x + i * btn_w + 0.04 * i
        create_bezel(f"ModeBtn_{label.replace(' ', '')}", 
                    bx, btn_row_y, btn_w - 0.02, btn_row_h,
                    material=mat_bezel, parent=panel_root)
    
    # Main core map display area (large recessed area)
    # Unity anchors within core panel: (0.05, 0.12) to (0.95, 0.88)
    map_x = cx + cw * 0.05
    map_y = cy + ch * 0.12
    map_w = cw * 0.90
    map_h = ch * 0.76
    
    # Outer frame for core map
    create_bezel("CoreMapFrame", map_x, map_y, map_w, map_h,
                material=mat_bezel, parent=panel_root)
    
    # Bank filter button housings (bottom row)
    # Unity anchors within core panel: y 0.02-0.10
    filt_row_y = cy + ch * 0.02
    filt_row_h = ch * 0.08
    filt_btn_w = (cw * 0.90) / 9  # 9 buttons
    filt_start_x = cx + cw * 0.05
    
    bank_labels = ["ALL", "SA", "SB", "SC", "SD", "D", "C", "B", "A"]
    for i, label in enumerate(bank_labels):
        bx = filt_start_x + i * filt_btn_w + 0.02 * i
        create_bezel(f"BankBtn_{label}",
                    bx, filt_row_y, filt_btn_w - 0.01, filt_row_h,
                    material=mat_bezel, parent=panel_root)
    
    # ------------------------------------------------------------------
    # RIGHT GAUGE PANEL — 8 thermal-hydraulic gauge bezels
    # Unity anchors: (0.65, 0.26) to (0.80, 1.00)
    # ------------------------------------------------------------------
    print("  Creating right gauge panel (8 bezels)...")
    
    rx, ry, rw, rh = anchor_to_panel(0.65, 0.26, 0.15, 0.74)
    
    # Section label
    create_text_label("Label_RightPanel", "THERMAL-HYDRAULIC",
                      rx + rw/2, ry + rh - 0.1, 0.001, size=0.12,
                      parent=panel_root)
    
    # 8 gauge bezels
    gauge_names_right = [
        "Tavg", "Thot", "Tcold", "DeltaT",
        "FuelCenterline", "HotChannel", "Pressure", "PZRLevel"
    ]
    
    available_height_r = rh - gauge_top_margin - gauge_padding
    gauge_h_r = (available_height_r - gauge_padding * 7) / 8  # 8 gauges, 7 gaps
    gauge_w_r = rw - gauge_padding * 2
    
    for i, gname in enumerate(gauge_names_right):
        gx = rx + gauge_padding
        gy = ry + rh - gauge_top_margin - (i + 1) * (gauge_h_r + gauge_padding) + gauge_padding
        create_gauge_bezel(f"Gauge_R_{gname}", gx, gy, gauge_w_r, gauge_h_r,
                          parent=panel_root)
    
    # ------------------------------------------------------------------
    # DETAIL PANEL — Assembly detail area
    # Unity anchors: (0.80, 0.26) to (1.00, 1.00)
    # ------------------------------------------------------------------
    print("  Creating detail panel...")
    
    dx, dy, dw, dh = anchor_to_panel(0.80, 0.26, 0.20, 0.74)
    
    create_text_label("Label_Detail", "ASSEMBLY DETAIL",
                      dx + dw/2, dy + dh - 0.1, 0.001, size=0.12,
                      parent=panel_root)
    
    # Large recessed display area
    create_recessed_area("DetailDisplay",
                        dx + 0.06, dy + 0.06, dw - 0.12, dh - 0.45,
                        material=mat_recess, parent=panel_root)
    
    # ------------------------------------------------------------------
    # BOTTOM PANEL — Controls area
    # Unity anchors: (0.00, 0.00) to (1.00, 0.26)
    # ------------------------------------------------------------------
    print("  Creating bottom panel sections...")
    
    bx_base, by_base, bw_base, bh_base = anchor_to_panel(0.0, 0.0, 1.0, 0.26)
    
    # Horizontal divider between main area and bottom controls
    create_section_divider(0, bh_base, PANEL_WIDTH, SECTION_DIVIDER_WIDTH,
                          parent=panel_root)
    
    # --- Rod Control Section (0.01-0.15, 0.55-0.95 of bottom) ---
    rc_x = bx_base + bw_base * 0.01
    rc_y = by_base + bh_base * 0.55
    rc_w = bw_base * 0.14
    rc_h = bh_base * 0.40
    
    create_bezel("RodControlFrame", rc_x, rc_y, rc_w, rc_h,
                material=mat_bezel, parent=panel_root)
    
    create_text_label("Label_RodControl", "ROD CONTROL",
                      rc_x + rc_w/2, rc_y + rc_h + 0.04, 0.001, size=0.10,
                      parent=panel_root)
    
    # --- Bank Positions Section (0.16-0.45, 0.55-0.95 of bottom) ---
    # v4.2.2: Fixed to upper row only — was (0.05-0.95) which overlapped alarm strip
    bp_x = bx_base + bw_base * 0.16
    bp_y = by_base + bh_base * 0.55
    bp_w = bw_base * 0.29
    bp_h = bh_base * 0.40
    
    create_bezel("BankPositionsFrame", bp_x, bp_y, bp_w, bp_h,
                material=mat_bezel, parent=panel_root)
    
    create_text_label("Label_BankPos", "BANK POSITIONS",
                      bp_x + bp_w/2, bp_y + bp_h + 0.04, 0.001, size=0.10,
                      parent=panel_root)
    
    # Individual bar graph housings for 8 banks
    bar_w = (bp_w - 0.20) / 8
    for i in range(8):
        bar_x = bp_x + 0.10 + i * (bar_w + 0.005)
        create_recessed_area(f"BankBar_{BANK_NAMES_SHORT[i]}",
                            bar_x, bp_y + 0.10, bar_w, bp_h - 0.20,
                            material=mat_recess, parent=panel_root)
    
    # --- Boron Control Section (0.46-0.60, 0.55-0.95 of bottom) ---
    bc_x = bx_base + bw_base * 0.46
    bc_y = by_base + bh_base * 0.55
    bc_w = bw_base * 0.14
    bc_h = bh_base * 0.40
    
    create_bezel("BoronControlFrame", bc_x, bc_y, bc_w, bc_h,
                material=mat_bezel, parent=panel_root)
    
    create_text_label("Label_Boron", "BORON CONTROL",
                      bc_x + bc_w/2, bc_y + bc_h + 0.04, 0.001, size=0.10,
                      parent=panel_root)
    
    # Boron readout recess
    create_recessed_area("BoronReadout",
                        bc_x + 0.08, bc_y + 0.06, bc_w - 0.16, bc_h * 0.30,
                        material=mat_recess, parent=panel_root)
    
    # --- Trip Control Section (0.61-0.75, 0.55-0.95 of bottom) ---
    tc_x = bx_base + bw_base * 0.61
    tc_y = by_base + bh_base * 0.55
    tc_w = bw_base * 0.14
    tc_h = bh_base * 0.40
    
    create_bezel("TripControlFrame", tc_x, tc_y, tc_w, tc_h,
                material=mat_bezel, parent=panel_root)
    
    create_text_label("Label_Trip", "TRIP CONTROL",
                      tc_x + tc_w/2, tc_y + tc_h + 0.04, 0.001, size=0.10,
                      parent=panel_root)
    
    # Trip button housing (red accent ring)
    trip_btn_x = tc_x + tc_w * 0.15
    trip_btn_y = tc_y + tc_h * 0.25
    trip_btn_size = min(tc_w * 0.40, tc_h * 0.50)
    create_box("TripBtnRing", trip_btn_x, trip_btn_y, 0,
              trip_btn_size, trip_btn_size, BEZEL_DEPTH * 0.8,
              material=mat_accent_red, bevel_width=0.015, parent=panel_root)
    
    # --- Time Control Section (0.76-0.99, 0.55-0.95 of bottom) ---
    tm_x = bx_base + bw_base * 0.76
    tm_y = by_base + bh_base * 0.55
    tm_w = bw_base * 0.23
    tm_h = bh_base * 0.40
    
    create_bezel("TimeControlFrame", tm_x, tm_y, tm_w, tm_h,
                material=mat_bezel, parent=panel_root)
    
    create_text_label("Label_Time", "TIME CONTROL",
                      tm_x + tm_w/2, tm_y + tm_h + 0.04, 0.001, size=0.10,
                      parent=panel_root)
    
    # Time readout recesses
    create_recessed_area("SimTimeReadout",
                        tm_x + 0.06, tm_y + tm_h * 0.50, tm_w * 0.45, tm_h * 0.30,
                        material=mat_recess, parent=panel_root)
    create_recessed_area("TimeCompReadout",
                        tm_x + tm_w * 0.55, tm_y + tm_h * 0.50, tm_w * 0.35, tm_h * 0.30,
                        material=mat_recess, parent=panel_root)
    
    # --- Annunciator Panel (0.01-0.99, 0.05-0.50 of bottom) ---
    # v4.2.2: Full-width annunciator tile grid spanning under all upper sections
    al_x = bx_base + bw_base * 0.01
    al_y = by_base + bh_base * 0.05
    al_w = bw_base * 0.98
    al_h = bh_base * 0.45
    
    create_bezel("AnnunciatorFrame", al_x, al_y, al_w, al_h,
                material=mat_bezel, parent=panel_root)
    
    create_text_label("Label_Annunciator", "ANNUNCIATOR PANEL",
                      al_x + al_w/2, al_y + al_h + 0.04, 0.001, size=0.10,
                      parent=panel_root)
    
    # Annunciator tile recesses (4x4 grid within the frame)
    ann_frame_w = 0.04  # Match bezel frame width
    ann_inner_x = al_x + ann_frame_w + 0.06
    ann_inner_y = al_y + ann_frame_w + 0.04
    ann_inner_w = al_w - 2 * ann_frame_w - 0.12
    ann_inner_h = al_h - 2 * ann_frame_w - 0.08
    
    ann_cols = 8
    ann_rows = 2
    ann_gap = 0.03
    ann_tile_w = (ann_inner_w - (ann_cols - 1) * ann_gap) / ann_cols
    ann_tile_h = (ann_inner_h - (ann_rows - 1) * ann_gap) / ann_rows
    
    ann_labels = [
        "REACTOR\nTRIPPED", "NEUTRON\nPOWER HI", "STARTUP\nRATE HI", "ROD BOTTOM\nALARM",
        "PRESS\nLOW", "PRESS\nHIGH", "T-AVG\nLOW", "T-AVG\nHIGH",
        "SUBCOOL\nLOW", "PZR LVL\nLOW", "PZR LVL\nHIGH", "OVERPOWER\nDT",
        "REACTOR\nCRITICAL", "AUTO ROD\nCONTROL", "PZR HTRS\nON", "LOW\nFLOW"
    ]
    
    for row in range(ann_rows):
        for col in range(ann_cols):
            idx = row * ann_cols + col
            tile_x = ann_inner_x + col * (ann_tile_w + ann_gap)
            # Top row first (row 0 at top)
            tile_y = ann_inner_y + (ann_rows - 1 - row) * (ann_tile_h + ann_gap)
            
            create_recessed_area(f"AnnTile_{idx:02d}",
                                tile_x, tile_y, ann_tile_w, ann_tile_h,
                                material=mat_recess, parent=panel_root)
            
            # Tile label (engraved into the recess surround)
            label_text = ann_labels[idx].replace('\n', ' ')
            create_text_label(f"AnnLabel_{idx:02d}", label_text,
                              tile_x + ann_tile_w/2, tile_y + ann_tile_h/2,
                              -RECESS_DEPTH + 0.002, size=0.06,
                              parent=panel_root)
    
    # ------------------------------------------------------------------
    # Vertical section dividers between panels
    # ------------------------------------------------------------------
    print("  Adding section dividers...")
    
    # Between left gauges and core map
    div_x, _, _, _ = anchor_to_panel(0.15, 0.26, 0, 0.74)
    create_section_divider(div_x - SECTION_DIVIDER_WIDTH/2, 
                          anchor_to_panel(0, 0.26, 0, 0)[1],
                          SECTION_DIVIDER_WIDTH, 
                          anchor_to_panel(0, 0, 0, 0.74)[3],
                          parent=panel_root)
    
    # Between core map and right gauges
    div_x2, _, _, _ = anchor_to_panel(0.65, 0.26, 0, 0.74)
    create_section_divider(div_x2 - SECTION_DIVIDER_WIDTH/2,
                          anchor_to_panel(0, 0.26, 0, 0)[1],
                          SECTION_DIVIDER_WIDTH,
                          anchor_to_panel(0, 0, 0, 0.74)[3],
                          parent=panel_root)
    
    # Between right gauges and detail panel
    div_x3, _, _, _ = anchor_to_panel(0.80, 0.26, 0, 0.74)
    create_section_divider(div_x3 - SECTION_DIVIDER_WIDTH/2,
                          anchor_to_panel(0, 0.26, 0, 0)[1],
                          SECTION_DIVIDER_WIDTH,
                          anchor_to_panel(0, 0, 0, 0.74)[3],
                          parent=panel_root)
    
    # ------------------------------------------------------------------
    # Apply all bevel modifiers
    # ------------------------------------------------------------------
    print("  Applying modifiers...")
    
    for obj in bpy.data.objects:
        if obj.type == 'MESH' and obj.modifiers:
            bpy.context.view_layer.objects.active = obj
            for mod in obj.modifiers:
                try:
                    bpy.ops.object.modifier_apply(modifier=mod.name)
                except:
                    pass  # Some modifiers may not apply cleanly
    
    # ------------------------------------------------------------------
    # Setup lighting for render
    # ------------------------------------------------------------------
    print("  Setting up lighting...")
    
    # Key light (overhead, slightly forward)
    bpy.ops.object.light_add(type='AREA', location=(PANEL_WIDTH/2, PANEL_HEIGHT/2, 5.0))
    key_light = bpy.context.active_object
    key_light.name = "KeyLight"
    key_light.data.energy = 500
    key_light.data.size = 15.0
    key_light.data.color = (0.95, 0.95, 1.0)
    key_light.parent = panel_root
    
    # Fill light (lower, softer)
    bpy.ops.object.light_add(type='AREA', location=(PANEL_WIDTH/2, -2.0, 3.0))
    fill_light = bpy.context.active_object
    fill_light.name = "FillLight"
    fill_light.data.energy = 150
    fill_light.data.size = 10.0
    fill_light.data.color = (0.8, 0.85, 1.0)
    fill_light.parent = panel_root
    
    # Rim light (from behind/above for edge definition)
    bpy.ops.object.light_add(type='AREA', location=(PANEL_WIDTH/2, PANEL_HEIGHT + 2.0, 4.0))
    rim_light = bpy.context.active_object
    rim_light.name = "RimLight"
    rim_light.data.energy = 200
    rim_light.data.size = 12.0
    rim_light.data.color = (0.7, 0.8, 1.0)
    rim_light.parent = panel_root
    
    # ------------------------------------------------------------------
    # Done
    # ------------------------------------------------------------------
    
    # Select root for easy manipulation
    bpy.ops.object.select_all(action='DESELECT')
    panel_root.select_set(True)
    bpy.context.view_layer.objects.active = panel_root
    
    # Set viewport to front orthographic
    for area in bpy.context.screen.areas:
        if area.type == 'VIEW_3D':
            for region in area.regions:
                if region.type == 'WINDOW':
                    override = bpy.context.copy()
                    override['area'] = area
                    override['region'] = region
                    break
    
    print("")
    print("=" * 60)
    print("  Panel creation complete!")
    print(f"  Objects created: {len(bpy.data.objects)}")
    print(f"  Materials created: {len(bpy.data.materials)}")
    print("")
    print("  Next steps:")
    print("  1. Press Ctrl+S to save as ReactorOperatorPanel.blend")
    print("  2. Open and run render_panel_textures.py")
    print("=" * 60)


# ============================================================================
# Bank name constants (used in bottom panel)
# ============================================================================
BANK_NAMES_SHORT = ["SA", "SB", "SC", "SD", "D", "C", "B", "A"]


# ============================================================================
# RUN
# ============================================================================

if __name__ == "__main__":
    build_panel()
else:
    build_panel()
