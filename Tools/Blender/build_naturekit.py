#!/usr/bin/env blender
# Day 5 nature kit — rocks 6x, cliff pieces 2x, pines + scrub + grass/fern cards, driftwood, shells, seaweed/debris

import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__)))
import ll_pipeline as ll
import bpy, bmesh, math, random, mathutils

def jitter(obj, amt=0.06):
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(obj.data)
    for v in bm.verts:
        n = v.normal
        v.co += n * random.uniform(-amt, amt)
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.object.mode_set(mode='OBJECT')

def tree_trunk(h, r0, r1, verts=8):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r0, depth=h, location=(0,0,h*0.5))
    o=bpy.context.view_layer.objects.active
    # taper via bmesh scaling top ring
    bpy.ops.object.mode_set(mode='EDIT')
    bm=bmesh.from_edit_mesh(o.data); bm.verts.ensure_lookup_table()
    maxz=max(v.co.z for v in bm.verts)
    for v in bm.verts:
        if v.co.z > maxz-0.01:
            v.co.x*= r1/r0; v.co.y*= r1/r0
    bmesh.update_edit_mesh(o.data); bpy.ops.object.mode_set(mode='OBJECT')
    return o

def leaf_cone(h, r, verts=8):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=0.05, depth=h, location=(0,0,0))
    return bpy.context.view_layer.objects.active

def build():
    ll.clear_scene(); ll.set_scene_units()
    random.seed(5)
    mat_rock = ll.make_pbr_material("LL_M_Rock", ll.PALETTE["stone_mid"], roughness=0.96)
    mat_rock_dark = ll.make_pbr_material("LL_M_RockDark", ll.PALETTE["stone_dark"], roughness=0.97)
    mat_bark = ll.make_pbr_material("LL_M_Bark", ll.PALETTE["wood_dark"], roughness=0.95)
    mat_pine = ll.make_pbr_material("LL_M_Pine", (0.13,0.22,0.11,1), roughness=0.95)
    mat_scrub = ll.make_pbr_material("LL_M_Scrub", (0.22,0.28,0.16,1), roughness=0.95)
    mat_grass = ll.make_pbr_material("LL_M_GrassCard", (0.20,0.42,0.14,1), roughness=0.96)
    mat_fern  = ll.make_pbr_material("LL_M_FernCard", (0.22,0.36,0.18,1), roughness=0.96)
    mat_drift = ll.make_pbr_material("LL_M_Driftwood", ll.PALETTE["wood_grey"], roughness=0.96)
    mat_shell = ll.make_pbr_material("LL_M_Shell", (0.86,0.82,0.76,1), roughness=0.85)
    mat_sea   = ll.make_pbr_material("LL_M_Seaweed", (0.14,0.22,0.14,1), roughness=0.97)

    bpy.ops.object.empty_add(location=(0,0,0)); root=bpy.context.view_layer.objects.active; root.name="LL_NatureKit"
    objs=[]
    def pr(o):
        o.parent=root; objs.append(o); return o

    spacing=4.0; col= -2

    # Rocks 6 variants: ico spheres subdiv 1-2, displace
    for i in range(6):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1 if i<3 else 2, radius=0.7+random.uniform(-0.1,0.25))
        r=bpy.context.view_layer.objects.active; r.name=f"LL_Rock_{i+1:02d}"
        r.location = (i*1.8 - 4.5, 8, 0.35)
        r.scale = (random.uniform(0.85,1.25), random.uniform(0.85,1.25), random.uniform(0.5,0.85))
        ll.assign_material(r, mat_rock if i%2==0 else mat_rock_dark); ll.ensure_uvs(r); ll.shade_smooth(r)
        jitter(r, 0.09)
        # flatten bottom
        bpy.ops.object.mode_set(mode='EDIT'); bm=bmesh.from_edit_mesh(r.data)
        minz=min(v.co.z for v in bm.verts)
        for v in bm.verts:
            if v.co.z < minz+0.12: v.co.z = minz
        bmesh.update_edit_mesh(r.data); bpy.ops.object.mode_set(mode='OBJECT')
        pr(r)

    # Cliff pieces 2x — elongated cubes with cliff face
    for i in range(2):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(i*5-1, 4.5, 1.6))
        c=bpy.context.view_layer.objects.active; c.name=f"LL_Cliff_{i+1:02d}"
        c.scale=(6.0, 2.2, 3.2 if i==0 else 2.2)
        ll.assign_material(c, mat_rock_dark); ll.ensure_uvs(c)
        jitter(c, 0.12); pr(c)

    # Pine A tall (7m)
    bpy.ops.object.empty_add(location=(-5, 0, 0)); pineA=bpy.context.view_layer.objects.active; pineA.name="LL_Pine_Tall"; pineA.parent=root; objs.append(pineA)
    t=tree_trunk(7.0, 0.22, 0.08, 8); t.name="LL_Pine_Tall_Trunk"; t.location.z=0; t.parent=pineA; ll.assign_material(t, mat_bark); ll.ensure_uvs(t)
    for j, (h,r,y) in enumerate([(2.8,1.2,1.6),(2.4,1.0,3.2),(1.8,0.75,4.6)]):
        c=leaf_cone(h, r, 8); c.name=f"LL_Pine_Tall_Leaf{j}"; c.location=(0,0,y); c.parent=pineA; ll.assign_material(c, mat_pine); ll.ensure_uvs(c)

    # Pine B short (4.5m)
    bpy.ops.object.empty_add(location=(-2, 0, 0)); pineB=bpy.context.view_layer.objects.active; pineB.name="LL_Pine_Short"; pineB.parent=root; objs.append(pineB)
    t2=tree_trunk(4.5, 0.18, 0.06, 8); t2.name="LL_Pine_Short_Trunk"; t2.parent=pineB; ll.assign_material(t2, mat_bark); ll.ensure_uvs(t2)
    for j,(h,r,y) in enumerate([(2.2,0.95,1.2),(1.8,0.75,2.5)]):
        c=leaf_cone(h,r,8); c.name=f"LL_Pine_Short_Leaf{j}"; c.location=(0,0,y); c.parent=pineB; ll.assign_material(c, mat_pine); ll.ensure_uvs(c)

    # Scrub bush (sphere-ish)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.85); sc=bpy.context.view_layer.objects.active
    sc.name="LL_Scrub"; sc.location=(1.8,0,0.55); sc.scale=(1.0,0.95,0.75)
    sc.parent=root; objs.append(sc); ll.assign_material(sc, mat_scrub); ll.ensure_uvs(sc); ll.shade_smooth(sc); jitter(sc, 0.14)
    bpy.ops.object.mode_set(mode='EDIT'); bm=bmesh.from_edit_mesh(sc.data); minz=min(v.co.z for v in bm.verts)
    for v in bm.verts:
        if v.co.z < minz+0.15: v.co.z=minz
    bmesh.update_edit_mesh(sc.data); bpy.ops.object.mode_set(mode='OBJECT')

    # Grass card (crossed planes 1.2m)
    bpy.ops.object.empty_add(location=(5, -0.5, 0)); g=bpy.context.view_layer.objects.active; g.name="LL_Grass_Clump"; g.parent=root; objs.append(g)
    for k in range(2):
        bpy.ops.mesh.primitive_plane_add(size=1, location=(0,0,0.45))
        p=bpy.context.view_layer.objects.active; p.name=f"LL_Grass_Card_{k}"; p.scale=(1.1,1,0.9); p.rotation_euler=(math.radians(90),0, math.radians(90*k))
        p.parent=g; ll.assign_material(p, mat_grass); ll.ensure_uvs(p)

    # Fern card
    bpy.ops.object.empty_add(location=(6.8, -0.5, 0)); f=bpy.context.view_layer.objects.active; f.name="LL_Fern_Clump"; f.parent=root; objs.append(f)
    for k in range(2):
        bpy.ops.mesh.primitive_plane_add(size=1, location=(0,0,0.42)); p=bpy.context.view_layer.objects.active
        p.name=f"LL_Fern_Card_{k}"; p.scale=(1.2,1,0.75); p.rotation_euler=(math.radians(88),0, math.radians(90*k+15))
        p.parent=f; ll.assign_material(p, mat_fern); ll.ensure_uvs(p)

    # Driftwood
    bpy.ops.mesh.primitive_cube_add(size=1, location=(8.5,0,0.22)); dw=bpy.context.view_layer.objects.active
    dw.name="LL_Driftwood"; dw.scale=(2.0,0.35,0.32); dw.rotation_euler=(0, math.radians(8), math.radians(12))
    dw.parent=root; objs.append(dw); ll.assign_material(dw, mat_drift); ll.ensure_uvs(dw); jitter(dw, 0.03)

    # Shell pile (3 small)
    bpy.ops.object.empty_add(location=(9.5,-0.8,0)); sh=bpy.context.view_layer.objects.active; sh.name="LL_ShellPile"; sh.parent=root; objs.append(sh)
    for i in range(3):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=6, radius=0.14+random.uniform(-0.02,0.03))
        s=bpy.context.view_layer.objects.active; s.name=f"LL_Shell_{i}"; s.location=(random.uniform(-0.25,0.25), random.uniform(-0.2,0.2), 0.08); s.scale.z=0.55
        s.parent=sh; ll.assign_material(s, mat_shell); ll.ensure_uvs(s)
        jitter(s, 0.015)

    # Seaweed/debris card
    bpy.ops.mesh.primitive_plane_add(size=1, location=(9.8,0.9,0.18)); sw=bpy.context.view_layer.objects.active
    sw.name="LL_Seaweed"; sw.scale=(1.6,1,0.35); sw.rotation_euler=(math.radians(90),0, math.radians(-10))
    sw.parent=root; objs.append(sw); ll.assign_material(sw, mat_sea); ll.ensure_uvs(sw)

    tris = sum(ll.tri_count(o) for o in bpy.data.objects if o.type=='MESH')
    print(f"[NATUREKIT] {tris} tris total"); ll.print_scene_stats()
    ll.screenshot_trio("Day5/LL_NatureKit", look_at=(2.5,2.0,0.9), lens=36)

    for o in bpy.data.objects:
        if o.type=='MESH':
            ll.select_only(o); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    all_meshes=[o for o in bpy.data.objects if o.type=='MESH']
    ll.export_fbx(all_meshes, "LL_NatureKit.fbx")
    print("[DONE] NatureKit")

if __name__=="__main__":
    build()
