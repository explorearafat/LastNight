#!/usr/bin/env blender
# Build LL_Lighthouse_Tower — hero asset  (North Atlantic weathered, ~4-6k tris)
# Run: blender --background --python Tools/Blender/build_lighthouse.py

import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__)))
import ll_pipeline as ll
import bpy, bmesh, math, mathutils

OUT_FBX = "LL_Lighthouse_Tower.fbx"
REVIEW_PREFIX = "LL_Lighthouse_Tower"

def build():
    ll.clear_scene()
    ll.set_scene_units()

    # ── Materials ──
    mat_white = ll.make_pbr_material("LL_M_TowerWhite", ll.PALETTE["tower_white"], roughness=0.82, metallic=0.0)
    mat_red   = ll.make_pbr_material("LL_M_TowerRed",   ll.PALETTE["tower_red"],   roughness=0.78, metallic=0.0)
    mat_red_dark = ll.make_pbr_material("LL_M_TowerRedDark", ll.PALETTE["tower_red_dark"], roughness=0.85)
    mat_stone = ll.make_pbr_material("LL_M_StoneDark", ll.PALETTE["stone_dark"], roughness=0.95)
    mat_stone_mid = ll.make_pbr_material("LL_M_StoneMid", ll.PALETTE["stone_mid"], roughness=0.92)
    mat_metal_rust = ll.make_pbr_material("LL_M_MetalRust", ll.PALETTE["metal_rust"], roughness=0.75, metallic=0.35)
    mat_metal_galv = ll.make_pbr_material("LL_M_MetalGalv", ll.PALETTE["metal_galv"], roughness=0.45, metallic=0.75)
    mat_wood = ll.make_pbr_material("LL_M_WoodDark", ll.PALETTE["wood_dark"], roughness=0.88)
    # Glass for lens room — Principled with transmission
    mat_glass = bpy.data.materials.new("LL_M_Glass")
    mat_glass.use_nodes = True
    ng = mat_glass.node_tree.nodes
    lk = mat_glass.node_tree.links
    ng.clear()
    out = ng.new('ShaderNodeOutputMaterial'); out.location=(400,0)
    bsdf = ng.new('ShaderNodeBsdfPrincipled'); bsdf.location=(0,0)
    bsdf.inputs['Base Color'].default_value = (0.82, 0.88, 0.92, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.08
    bsdf.inputs['Metallic'].default_value = 0.0
    # Transmission for glass
    try:
        bsdf.inputs['Transmission Weight'].default_value = 0.95
        bsdf.inputs['IOR'].default_value = 1.45
    except: pass
    lk.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    mat_glass.blend_method = 'BLEND'
    mat_glass.diffuse_color = (0.8,0.85,0.9,0.4)

    # ── Root empty ──
    bpy.ops.object.empty_add(location=(0,0,0))
    root = bpy.context.view_layer.objects.active
    root.name = "LL_Lighthouse_Tower"
    root.empty_display_size = 0.5

    objs = []  # to export
    def parent_to_root(o):
        o.parent = root
        objs.append(o)
        return o

    # ── 1) Stone foundation (octagonal-ish, weathered) ──
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=3.2, depth=1.0, location=(0,0,0.5))
    base = bpy.context.view_layer.objects.active
    base.name = "LL_Lighthouse_Base"
    ll.assign_material(base, mat_stone)
    ll.shade_smooth(base)
    # slightly irregular top via scale
    base.scale = (1,1,1)
    ll.ensure_uvs(base)
    parent_to_root(base)

    # Bevel the base edge
    ll.add_bevel_modifier(base, width=0.05, segments=1)

    # ── 2) Tower body — tapered white cylinder (height 16m, base 2.4m, top 1.85m) ──
    # Use 24 sides, create via bmesh for taper
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=2.4, depth=16.0, location=(0,0,8.5))
    tower = bpy.context.view_layer.objects.active
    tower.name = "LL_Tower_White"
    # Taper: scale top verts inward
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(tower.data)
    bm.verts.ensure_lookup_table()
    top_z = max(v.co.z for v in bm.verts)
    for v in bm.verts:
        if v.co.z > top_z - 0.05:
            # top cap verts
            scale = 1.85/2.4
            v.co.x *= scale
            v.co.y *= scale
    bmesh.update_edit_mesh(tower.data)
    bpy.ops.object.mode_set(mode='OBJECT')
    ll.assign_material(tower, mat_white)
    ll.shade_smooth(tower)
    ll.ensure_uvs(tower)
    ll.add_bevel_modifier(tower, width=0.03, segments=1)
    parent_to_root(tower)

    # ── 3) Red band (middle, peeling) ──
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=2.18, depth=2.2, location=(0,0,9.0))
    red = bpy.context.view_layer.objects.active
    red.name = "LL_Tower_RedBand"
    # taper same ratio
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(red.data)
    bm.verts.ensure_lookup_table()
    for v in bm.verts:
        # interpolate taper by z
        t = (v.co.z + 1.1) / 2.2  # 0..1 within band
        # band sits at 8-10m height; tower taper from 2.4 to 1.85 over 16m -> ~ linear
        # approximate radius at band
        rscale = 1.0 - t*0.04
        v.co.x *= rscale
        v.co.y *= rscale
    bmesh.update_edit_mesh(red.data)
    bpy.ops.object.mode_set(mode='OBJECT')
    ll.assign_material(red, mat_red)
    ll.shade_smooth(red)
    ll.ensure_uvs(red)
    parent_to_root(red)

    # ── 4) Second thin red stripe near top (classic) ──
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=1.92, depth=0.55, location=(0,0,14.2))
    red2 = bpy.context.view_layer.objects.active
    red2.name = "LL_Tower_RedBand_Top"
    ll.assign_material(red2, mat_red_dark)
    ll.shade_smooth(red2)
    ll.ensure_uvs(red2)
    parent_to_root(red2)

    # ── 5) Door (recessed, rusted metal frame + wood door) ──
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 2.35, 1.55))
    door_frame = bpy.context.view_layer.objects.active
    door_frame.name = "LL_Door_Frame"
    door_frame.scale = (1.25, 0.22, 2.1)
    ll.assign_material(door_frame, mat_metal_rust)
    ll.ensure_uvs(door_frame)
    parent_to_root(door_frame)

    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 2.42, 1.55))
    door = bpy.context.view_layer.objects.active
    door.name = "LL_Door"
    door.scale = (0.95, 0.08, 1.85)
    mat_door = ll.make_pbr_material("LL_M_DoorWood", ll.PALETTE["wood_dark"], roughness=0.75)
    ll.assign_material(door, mat_door)
    ll.ensure_uvs(door)
    parent_to_root(door)

    # door handle
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.06, depth=0.14, location=(0.32, 2.48, 1.45))
    handle = bpy.context.view_layer.objects.active
    handle.name = "LL_Door_Handle"
    handle.rotation_euler = (math.radians(90),0,0)
    ll.assign_material(handle, mat_metal_galv)
    ll.ensure_uvs(handle)
    parent_to_root(handle)

    # ── 6) Windows (2, with frames) ──
    for i, (z, angle_deg) in enumerate([(5.2, 90), (11.0, -60)]):
        r = 2.15 if z < 8 else 1.95
        ang = math.radians(angle_deg)
        wx = math.sin(ang)*r
        wy = math.cos(ang)*r
        # frame
        bpy.ops.mesh.primitive_cube_add(size=1, location=(wx, wy, z))
        wf = bpy.context.view_layer.objects.active
        wf.name = f"LL_Window_{i}_Frame"
        wf.scale = (0.72, 0.18, 0.85)
        wf.rotation_euler = (0,0, -ang)
        ll.assign_material(wf, mat_metal_rust)
        ll.ensure_uvs(wf)
        parent_to_root(wf)
        # glass
        bpy.ops.mesh.primitive_cube_add(size=1, location=(wx+math.sin(ang)*0.06, wy+math.cos(ang)*0.06, z))
        wg = bpy.context.view_layer.objects.active
        wg.name = f"LL_Window_{i}_Glass"
        wg.scale = (0.58, 0.04, 0.68)
        wg.rotation_euler = (0,0, -ang)
        ll.assign_material(wg, mat_glass)
        ll.ensure_uvs(wg)
        parent_to_root(wg)

    # ── 7) Balcony (around top) ──
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=2.35, depth=0.18, location=(0,0,16.15))
    balcony = bpy.context.view_layer.objects.active
    balcony.name = "LL_Balcony_Floor"
    ll.assign_material(balcony, mat_stone_mid)
    ll.shade_smooth(balcony)
    ll.ensure_uvs(balcony)
    parent_to_root(balcony)

    # railing (torus-like via 16 posts + rail)
    rail_y = 16.15 + 0.55
    rail_radius = 2.35
    bpy.ops.mesh.primitive_torus_add(major_radius=rail_radius, minor_radius=0.05, major_segments=24, minor_segments=8, location=(0,0, rail_y+0.5))
    rail_top = bpy.context.view_layer.objects.active
    rail_top.name = "LL_Balcony_Rail_Top"
    ll.assign_material(rail_top, mat_metal_galv)
    ll.ensure_uvs(rail_top)
    parent_to_root(rail_top)

    # vertical posts
    for k in range(12):
        ang = (k/12)*math.pi*2
        px = math.cos(ang)*rail_radius
        py = math.sin(ang)*rail_radius
        bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.035, depth=1.0, location=(px, py, rail_y))
        post = bpy.context.view_layer.objects.active
        post.name = f"LL_Balcony_Post_{k:02d}"
        ll.assign_material(post, mat_metal_galv)
        ll.ensure_uvs(post)
        parent_to_root(post)

    # ── 8) Lens room — octagonal glass + metal frame ──
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=1.55, depth=2.2, location=(0,0,17.45))
    lens_frame = bpy.context.view_layer.objects.active
    lens_frame.name = "LL_LensRoom_Frame"
    # make it frame-like: scale inner? simpler: keep as cylinder and glass inset
    ll.assign_material(lens_frame, mat_metal_rust)
    # wireframe-ish via modifier? keep solid
    ll.shade_smooth(lens_frame)
    ll.ensure_uvs(lens_frame)
    parent_to_root(lens_frame)

    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=1.42, depth=2.05, location=(0,0,17.45))
    lens_glass = bpy.context.view_layer.objects.active
    lens_glass.name = "LL_LensRoom_Glass"
    ll.assign_material(lens_glass, mat_glass)
    ll.shade_smooth(lens_glass)
    ll.ensure_uvs(lens_glass)
    parent_to_root(lens_glass)

    # lens (Fresnel-like, emissive-ish)
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.55, depth=0.9, location=(0,0,17.5))
    lens = bpy.context.view_layer.objects.active
    lens.name = "LL_Lens"
    mat_lens = bpy.data.materials.new("LL_M_Lens")
    mat_lens.use_nodes = True
    ng = mat_lens.node_tree.nodes; lk = mat_lens.node_tree.links; ng.clear()
    out2 = ng.new('ShaderNodeOutputMaterial'); out2.location=(400,0)
    bsdf2 = ng.new('ShaderNodeBsdfPrincipled'); bsdf2.location=(0,0)
    bsdf2.inputs['Base Color'].default_value = (0.95, 0.92, 0.65, 1.0)
    bsdf2.inputs['Roughness'].default_value = 0.12
    try:
        bsdf2.inputs['Emission Color'].default_value = (1.0, 0.88, 0.45, 1.0)
        bsdf2.inputs['Emission Strength'].default_value = 1.5
    except: pass
    lk.new(bsdf2.outputs['BSDF'], out2.inputs['Surface'])
    ll.assign_material(lens, mat_lens)
    ll.shade_smooth(lens)
    ll.ensure_uvs(lens)
    parent_to_root(lens)

    # ── 9) Roof (copper patina, conical) ──
    bpy.ops.mesh.primitive_cone_add(vertices=16, radius1=1.75, radius2=0.15, depth=1.6, location=(0,0,19.05))
    roof = bpy.context.view_layer.objects.active
    roof.name = "LL_Roof"
    mat_roof = ll.make_pbr_material("LL_M_RoofCopper", (0.22, 0.42, 0.38, 1.0), roughness=0.65, metallic=0.4)
    ll.assign_material(roof, mat_roof)
    ll.shade_smooth(roof)
    ll.ensure_uvs(roof)
    parent_to_root(roof)

    # roof vent cap
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.18, depth=0.35, location=(0,0,19.92))
    vent = bpy.context.view_layer.objects.active
    vent.name = "LL_Roof_Vent"
    ll.assign_material(vent, mat_metal_galv)
    ll.ensure_uvs(vent)
    parent_to_root(vent)

    # ── 10) Interior spiral stairs (12 steps + central pole) ──
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.06, depth=15.5, location=(0,0,8.3))
    pole = bpy.context.view_layer.objects.active
    pole.name = "LL_Stairs_Pole"
    ll.assign_material(pole, mat_metal_rust)
    ll.ensure_uvs(pole)
    parent_to_root(pole)

    for i in range(14):
        frac = i / 14.0
        ang = frac * math.pi * 2 * 1.1  # slightly over one full turn
        h = 0.7 + frac * 14.2
        r = 0.85
        x = math.cos(ang)*r*0.55
        y = math.sin(ang)*r*0.55
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, h))
        step = bpy.context.view_layer.objects.active
        step.name = f"LL_Stair_{i:02d}"
        step.scale = (0.85, 0.32, 0.06)
        step.rotation_euler = (0,0, -ang)
        ll.assign_material(step, mat_wood)
        ll.ensure_uvs(step)
        parent_to_root(step)

    # ── Vertex wet gradient (base wetter) ──
    for o in objs:
        if "Base" in o.name or "Tower" in o.name:
            try:
                ll.paint_vertex_color_gradient(o, color_top=(0.2,0.2,0.2,1), color_bottom=(0.55,0.55,0.6,1))
            except: pass

    # ── Validate ──
    total_tris = sum(ll.tri_count(o) for o in objs)
    print(f"\n[LIGHTHOUSE] Total tris: {total_tris} (budget 25000 hero)")
    for o in objs:
        ll.validate_asset(o, max_tris=25000, name_prefix="LL_")
    ll.print_scene_stats()

    # ── Screenshots (critique loop will inspect) ──
    ll.ensure_dirs()
    # Look at tower mid-height
    ll.screenshot_trio(REVIEW_PREFIX, look_at=(0,0,8.5), lens=45)

    # ── Export ──
    # Ensure all objs have correct pivot (already at world origin)
    for o in objs:
        # Apply scale + rotation
        ll.select_only(o)
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    out_path = ll.export_fbx(objs, OUT_FBX)
    print(f"[DONE] Lighthouse exported → {out_path}")

if __name__ == "__main__":
    build()
