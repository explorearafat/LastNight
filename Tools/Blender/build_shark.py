#!/usr/bin/env blender
# Day 5 — shark fin/proxy (minimal geometry, only fins above water)

import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__)))
import ll_pipeline as ll
import bpy, bmesh, math

def build():
    ll.clear_scene(); ll.set_scene_units()
    mat_shark = ll.make_pbr_material("LL_M_Shark", (0.18,0.20,0.23,1), roughness=0.65)
    bpy.ops.object.empty_add(location=(0,0,0)); root=bpy.context.view_layer.objects.active; root.name="LL_Shark"
    objs=[]
    def pr(o):
        o.parent=root; objs.append(o); return o

    # Fin: triangulated prism
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0.28))
    fin=bpy.context.view_layer.objects.active; fin.name="LL_Shark_Fin"
    fin.scale=(0.55, 0.06, 0.52)
    # shear top edge to make triangle profile
    bpy.ops.object.mode_set(mode='EDIT')
    bm=bmesh.from_edit_mesh(fin.data); bm.verts.ensure_lookup_table()
    for v in bm.verts:
        if v.co.z > 0.4:
            v.co.x += 0.18
    bmesh.update_edit_mesh(fin.data); bpy.ops.object.mode_set(mode='OBJECT')
    ll.assign_material(fin, mat_shark); ll.ensure_uvs(fin); pr(fin)

    # Torpedo body (subtle, mostly underwater)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0.04))
    body=bpy.context.view_layer.objects.active; body.name="LL_Shark_Body"
    body.scale=(1.4, 0.42, 0.22); ll.assign_material(body, mat_shark); ll.ensure_uvs(body)
    bpy.ops.object.mode_set(mode='EDIT')
    bm=bmesh.from_edit_mesh(body.data); bm.verts.ensure_lookup_table()
    for v in bm.verts:
        if v.co.x > 0.3:
            v.co.y *= 0.25
            v.co.z *= 0.65
    bmesh.update_edit_mesh(body.data); bpy.ops.object.mode_set(mode='OBJECT')
    pr(body)

    tris=sum(ll.tri_count(o) for o in objs); print(f"[SHARK] {tris} tris"); ll.print_scene_stats()
    ll.screenshot_trio("Day5/LL_Shark", look_at=(0,0,0.18), lens=50)
    for o in objs: ll.select_only(o); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    ll.export_fbx(objs, "LL_Shark.fbx")
    print("[DONE] Shark")

if __name__=="__main__":
    build()
