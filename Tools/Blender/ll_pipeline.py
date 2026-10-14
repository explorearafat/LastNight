# ll_pipeline.py — Last Light Blender-to-Unity art pipeline
# Reusable module: asset creation helpers, PBR materials, UV, LODs, FBX export + screenshots.
# Every asset script imports this.  Run inside Blender 4.x/5.x background mode:
#   blender --background --python Tools/Blender/build_<asset>.py
#
# Conventions (strict):
#   - 1 unit = 1 metre.  Pivot at base (world origin on ground).
#   - Naming: LL_Prop_Name  (e.g. LL_Lighthouse_Tower, LL_Keeper_House)
#   - 1 material per mesh; atlas where noted.
#   - Clean UVs (Smart UV or manual seams), no overlapping islands that cause bleed.
#   - 2 LODs: LOD0 = full, LOD1 = decimated ~50%.

import bpy
import bmesh
import math
import os
import sys
import mathutils

BLENDER = "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe"
PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
GENERATED_DIR = os.path.join(PROJECT_ROOT, "Assets", "Environment", "Generated")
REVIEWS_DIR   = os.path.join(PROJECT_ROOT, "Docs", "AssetReviews")

# ── Scene helpers ──────────────────────────────────────────────────────────

def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    # purge orphan data
    for block in bpy.data.meshes: bpy.data.meshes.remove(block)
    for block in bpy.data.materials: bpy.data.materials.remove(block)
    for block in bpy.data.images: bpy.data.images.remove(block)

def set_scene_units():
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.length_unit = 'METERS'
    scene.unit_settings.scale_length = 1.0

def tri_count(obj):
    me = obj.data
    me.calc_loop_triangles()
    return len(me.loop_triangles)

def deselect_all():
    bpy.ops.object.select_all(action='DESELECT')

def select_only(obj):
    deselect_all()
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

# ── UV helpers ─────────────────────────────────────────────────────────────

def smart_uv(obj, angle_limit=66, island_margin=0.02):
    select_only(obj)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=angle_limit, island_margin=island_margin)
    bpy.ops.object.mode_set(mode='OBJECT')

def cube_project_uv(obj, cube_size=1.0):
    select_only(obj)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.cube_project(cube_size=cube_size)
    bpy.ops.object.mode_set(mode='OBJECT')

def ensure_uvs(obj):
    """Make sure the mesh has a UV layer; create Smart UV if missing."""
    if not obj.data.uv_layers:
        smart_uv(obj)

# ── PBR material helpers ───────────────────────────────────────────────────
# All materials use Principled BSDF → Material Output.
# Colors are chosen to match the North Atlantic palette:
#   white/red tower, wet dark stone, rusted metal, old wood, moss.

PALETTE = {
    "tower_white":   (0.88, 0.86, 0.82, 1.0),
    "tower_red":     (0.62, 0.14, 0.10, 1.0),
    "tower_red_dark":(0.42, 0.09, 0.06, 1.0),
    "stone_dark":    (0.22, 0.21, 0.20, 1.0),
    "stone_mid":     (0.38, 0.36, 0.34, 1.0),
    "stone_wet":     (0.15, 0.15, 0.16, 1.0),
    "wood_old":      (0.31, 0.22, 0.14, 1.0),
    "wood_dark":     (0.18, 0.12, 0.07, 1.0),
    "wood_grey":     (0.42, 0.40, 0.36, 1.0),
    "metal_rust":    (0.38, 0.18, 0.08, 1.0),
    "metal_iron":    (0.18, 0.18, 0.19, 1.0),
    "metal_galv":    (0.55, 0.56, 0.58, 1.0),
    "moss":          (0.22, 0.30, 0.14, 1.0),
    "paper":         (0.84, 0.80, 0.67, 1.0),
    "rope":          (0.52, 0.42, 0.28, 1.0),
}

def make_pbr_material(name, base_color, roughness=0.85, metallic=0.0, normal_strength=0.0):
    """Create (or reuse) a Principled BSDF material. Returns the Material."""
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
    else:
        mat.use_nodes = True
        mat.node_tree.nodes.clear()
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    out_node = nodes.new('ShaderNodeOutputMaterial')
    out_node.location = (400, 0)
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    bsdf.location = (0, 0)
    bsdf.inputs['Base Color'].default_value = base_color
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['Metallic'].default_value = metallic
    links.new(bsdf.outputs['BSDF'], out_node.inputs['Surface'])
    # Ensure viewport display color matches
    mat.diffuse_color = base_color
    return mat

def assign_material(obj, mat):
    if obj.data.materials:
        obj.data.materials[0] = mat
    else:
        obj.data.materials.append(mat)

# ── Vertex color helpers (wet / moss mask) ────────────────────────────────

def paint_vertex_color_gradient(obj, color_top=(0,0,0,1), color_bottom=(1,1,1,1), axis='Z'):
    """Fill vertex colors with a height gradient (for wet/moss blending)."""
    me = obj.data
    if not me.color_attributes:
        me.color_attributes.new(name="Col", domain='POINT', type='BYTE_COLOR')
    ca = me.color_attributes[0]
    # find bbox
    coords = [obj.matrix_world @ v.co for v in me.vertices]
    if not coords:
        return
    idx = {'X':0,'Y':1,'Z':2}[axis]
    vals = [c[idx] for c in coords]
    mn, mx = min(vals), max(vals)
    rng = max(mx - mn, 0.001)
    for i, v in enumerate(me.vertices):
        t = (coords[i][idx] - mn) / rng
        # bottom = wet/moss, top = dry
        col = [
            color_bottom[0]*(1-t)+color_top[0]*t,
            color_bottom[1]*(1-t)+color_top[1]*t,
            color_bottom[2]*(1-t)+color_top[2]*t,
            1.0
        ]
        ca.data[i].color = col

# ── Geometry helpers ───────────────────────────────────────────────────────

def add_bevel_modifier(obj, width=0.02, segments=1):
    mod = obj.modifiers.new(name="Bevel", type='BEVEL')
    mod.width = width
    mod.segments = segments
    mod.limit_method = 'ANGLE'
    mod.angle_limit = math.radians(30)
    return mod

def shade_smooth(obj):
    select_only(obj)
    bpy.ops.object.shade_smooth()

def shade_flat(obj):
    select_only(obj)
    bpy.ops.object.shade_auto_smooth(use_auto_smooth=False)
    # set flat
    for poly in obj.data.polygons:
        poly.use_smooth = False

# ── LOD generation ─────────────────────────────────────────────────────────

def make_lod(obj, ratio=0.5, name_suffix="_LOD1"):
    """Duplicate obj and add Decimate modifier. Returns the LOD object."""
    lod = obj.copy()
    lod.data = obj.data.copy()
    lod.name = obj.name + name_suffix
    bpy.context.collection.objects.link(lod)
    dec = lod.modifiers.new(name="Decimate", type='DECIMATE')
    dec.ratio = ratio
    dec.use_collapse_triangulate = True
    return lod

# ── Export ─────────────────────────────────────────────────────────────────

def ensure_dirs():
    os.makedirs(GENERATED_DIR, exist_ok=True)
    os.makedirs(REVIEWS_DIR, exist_ok=True)

def export_fbx(objects, filepath, apply_scale=True):
    """Export given objects as FBX to filepath (relative to project or absolute)."""
    ensure_dirs()
    if not os.path.isabs(filepath):
        filepath = os.path.join(GENERATED_DIR, filepath)
    # select only the requested objects
    deselect_all()
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0] if objects else None
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL' if apply_scale else 'FBX_SCALE_NONE',
        apply_unit_scale=True,
        bake_space_transform=True,
        object_types={'MESH', 'EMPTY'},
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
        add_leaf_bones=False,
        use_armature_deform_only=False,
        axis_forward='-Z',
        axis_up='Y',
    )
    tris = sum(tri_count(o) for o in objects)
    print(f"[EXPORT] {filepath} — {tris} tris ({len(objects)} objects)")
    return filepath

# ── Screenshots ────────────────────────────────────────────────────────────

def setup_preview_lighting():
    """Add a simple 3-point-ish light rig + world color for screenshots."""
    # world
    world = bpy.context.scene.world
    if world is None:
        world = bpy.data.worlds.new("PreviewWorld")
        bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.58, 0.66, 0.76, 1.0)  # overcast sky
        bg.inputs[1].default_value = 1.2
    # sun
    if "PreviewSun" not in bpy.data.objects:
        bpy.ops.object.light_add(type='SUN', location=(8, -10, 12))
        sun = bpy.context.view_layer.objects.active
        sun.name = "PreviewSun"
        sun.data.energy = 5.0
        sun.data.angle = math.radians(15)
        sun.rotation_euler = (math.radians(50), 0, math.radians(30))
    # fill
    if "PreviewFill" not in bpy.data.objects:
        bpy.ops.object.light_add(type='SUN', location=(-6, 4, 6))
        fill = bpy.context.view_layer.objects.active
        fill.name = "PreviewFill"
        fill.data.energy = 1.2
        fill.data.color = (0.8, 0.85, 1.0)
        fill.rotation_euler = (math.radians(40), 0, math.radians(-120))

def set_camera_view(location, look_at=(0, 1.0, 1.5), lens=50):
    """Position the active camera (create if needed) to look at a point."""
    cam = bpy.context.scene.camera
    if cam is None:
        bpy.ops.object.camera_add(location=location)
        cam = bpy.context.view_layer.objects.active
        bpy.context.scene.camera = cam
    cam.location = mathutils.Vector(location)
    cam.data.lens = lens
    # look-at
    direction = mathutils.Vector(look_at) - cam.location
    rot_quat = direction.to_track_quat('-Z', 'Y')
    cam.rotation_euler = rot_quat.to_euler()
    return cam

def render_screenshot(filepath, resolution=(1280, 960), samples=64):
    """Render current camera view to filepath."""
    ensure_dirs()
    if not os.path.isabs(filepath):
        filepath = os.path.join(REVIEWS_DIR, filepath)
    scene = bpy.context.scene
    scene.render.resolution_x = resolution[0]
    scene.render.resolution_y = resolution[1]
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGB'
    scene.render.filepath = filepath
    # use EEVEE if available else Cycles low samples
    try:
        scene.render.engine = 'BLENDER_EEVEE_NEXT'
    except:
        try:
            scene.render.engine = 'BLENDER_EEVEE'
        except:
            scene.render.engine = 'CYCLES'
            scene.cycles.samples = samples
    scene.eevee.taa_render_samples = samples
    bpy.ops.render.render(write_still=True)
    print(f"[SCREENSHOT] {filepath}")
    return filepath

def screenshot_trio(prefix, look_at=(0, 1.2, 1.5), lens=50):
    """Take 3 standard-angle screenshots: front, 3/4, top-down-ish. Returns list of paths."""
    setup_preview_lighting()
    views = [
        (f"{prefix}_01_front.png", (0, -10, 2.5)),
        (f"{prefix}_02_three_quarter.png", (7, -7, 4)),
        (f"{prefix}_03_top.png", (2, -2, 14)),
    ]
    paths = []
    for fname, loc in views:
        set_camera_view(loc, look_at=look_at, lens=lens)
        paths.append(render_screenshot(fname))
    return paths

# ── Naming helpers ─────────────────────────────────────────────────────────

def name_for(base):
    """Return LL_ prefixed name."""
    return base if base.startswith("LL_") else f"LL_{base}"

# ── Validation ─────────────────────────────────────────────────────────────

def validate_asset(obj, max_tris=8000, name_prefix="LL_"):
    """Print tri count and basic checks. Returns True if within budget."""
    tris = tri_count(obj)
    ok = True
    if tris > max_tris:
        print(f"[WARN] {obj.name}: {tris} tris > budget {max_tris}")
        ok = False
    else:
        print(f"[OK]   {obj.name}: {tris} tris / {max_tris}")
    if not obj.name.startswith(name_prefix):
        print(f"[WARN] {obj.name}: should start with {name_prefix}")
    if not obj.data.uv_layers:
        print(f"[WARN] {obj.name}: no UVs")
        ok = False
    # pivot check — should be near origin base
    if abs(obj.location.z) > 0.01 or abs(obj.location.x) > 0.01 or abs(obj.location.y) > 0.01:
        print(f"[INFO] {obj.name}: pivot at {obj.location} (ideally 0,0,0)")
    return ok

def print_scene_stats():
    print("\n── Scene stats ──")
    for obj in bpy.context.scene.objects:
        if obj.type == 'MESH':
            print(f"  {obj.name}: {tri_count(obj)} tris, {len(obj.data.vertices)} verts, {len(obj.data.materials)} mats")
    print("─────────────────\n")
