#!/usr/bin/env blender
# Day 5 — boat: old wooden rowboat + optional outboard, oars

import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__)))
import ll_pipeline as ll
import bpy, bmesh, math, random

def build():
    ll.clear_scene(); ll.set_scene_units()
    mat_hull = ll.make_pbr_material("LL_M_Hull", (0.42,0.32,0.22,1), roughness=0.88)
    mat_inner = ll.make_pbr_material("LL_M_HullInner", (0.52,0.42,0.28,1), roughness=0.90)
    mat_metal = ll.make_pbr_material("LL_M_BoatMetal", ll.PALETTE["metal_galv"], roughness=0.45, metallic=0.65)
    mat_rust  = ll.make_pbr_material("LL_M_BoatRust", ll.PALETTE["metal_rust"], roughness=0.78, metallic=0.30)

    bpy.ops.object.empty_add(location=(0,0,0)); root=bpy.context.view_layer.objects.active; root.name="LL_Boat"
    objs=[]
    def pr(o):
        o.parent=root; objs.append(o); return o

    # Hull: stretched cube with pointed bow
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0.32))
    hull=bpy.context.view_layer.objects.active; hull.name="LL_Boat_Hull"
    hull.scale=(4.2, 1.45, 0.52)
    ll.assign_material(hull, mat_hull); ll.ensure_uvs(hull); ll.add_bevel_modifier(hull, width=0.025, segments=1)
    # taper bow (front +X): scale top front edge inward
    bpy.ops.object.mode_set(mode='EDIT')
    bm=bmesh.from_edit_mesh(hull.data); bm.verts.ensure_lookup_table()
    for v in bm.verts:
        if v.co.x > 0.38:
            v.co.y *= 0.22
            v.co.z *= 0.85
    bmesh.update_edit_mesh(hull.data); bpy.ops.object.mode_set(mode='OBJECT')
    pr(hull)

    # Inner floor
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0.42))
    floor=bpy.context.view_layer.objects.active; floor.name="LL_Boat_Floor"
    floor.scale=(3.6, 1.05, 0.06); ll.assign_material(floor, mat_inner); ll.ensure_uvs(floor); pr(floor)

    # Seats (2)
    for x in (-0.9, 0.7):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x,0,0.62))
        s=bpy.context.view_layer.objects.active; s.name=f"LL_Boat_Seat_{int(x*10)}"
        s.scale=(0.18, 1.15, 0.06); ll.assign_material(s, mat_inner); ll.ensure_uvs(s); pr(s)

    # Transom
    bpy.ops.mesh.primitive_cube_add(size=1, location=(-2.02,0,0.42))
    trans=bpy.context.view_layer.objects.active; trans.name="LL_Boat_Transom"
    trans.scale=(0.08, 1.25, 0.42); ll.assign_material(trans, mat_hull); ll.ensure_uvs(trans); pr(trans)

    # Outboard bracket (so it reads as motorboat even without motor mesh)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(-2.14,0,0.42))
    br=bpy.context.view_layer.objects.active; br.name="LL_Boat_MotorBracket"
    br.scale=(0.22, 0.18, 0.22); ll.assign_material(br, mat_rust); ll.ensure_uvs(br); pr(br)
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.08, depth=0.35, location=(-2.28,0,0.44))
    shaft=bpy.context.view_layer.objects.active; shaft.name="LL_Boat_MotorShaft"
    shaft.rotation_euler=(0, math.radians(90), 0); ll.assign_material(shaft, mat_metal); ll.ensure_uvs(shaft); pr(shaft)

    # Oar locks
    for sx in (-0.15, 0.55):
        for sy in (-0.62, 0.62):
            bpy.ops.mesh.primitive_torus_add(major_radius=0.06, minor_radius=0.015, major_segments=8, minor_segments=6, location=(sx,sy,0.68))
            o=bpy.context.view_layer.objects.active; o.name=f"LL_Boat_OarLock_{int(sx*10)}_{int(sy*10)}"
            o.rotation_euler=(math.radians(90),0,0); ll.assign_material(o, mat_metal); ll.ensure_uvs(o); pr(o)

    # Oar (single, placed along gunwale)
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.03, depth=2.2, location=(0.1,0.78,0.62))
    oar=bpy.context.view_layer.objects.active; oar.name="LL_Boat_Oar"
    oar.rotation_euler=(0, math.radians(90), 0); ll.assign_material(oar, ll.make_pbr_material("LL_M_Oar", ll.PALETTE["wood_old"], roughness=0.88)); ll.ensure_uvs(oar); pr(oar)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(1.22,0.78,0.62))
    blade=bpy.context.view_layer.objects.active; blade.name="LL_Boat_OarBlade"
    blade.scale=(0.42,0.07,0.14); ll.assign_material(blade, mat_inner); ll.ensure_uvs(blade); pr(blade)

    tris=sum(ll.tri_count(o) for o in objs if o.type=='MESH')
    print(f"[BOAT] {tris} tris ({len(objs)} objs)"); ll.print_scene_stats()
    ll.screenshot_trio("Day5/LL_Boat", look_at=(0,0,0.42), lens=38)

    for o in objs:
        if o.type=='MESH':
            ll.select_only(o); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    ll.export_fbx([o for o in objs if o.type=='MESH'], "LL_Boat.fbx")
    print("[DONE] Boat")

if __name__=="__main__":
    build()
