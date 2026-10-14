#!/usr/bin/env blender
# Day 5 — modular brick kit. One Blender scene containing ~16 kit pieces, exported as one FBX set + individual parts.
# 4-5 damage variants per type reuse the same mesh with vertex-jitter.

import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__)))
import ll_pipeline as ll
import bpy, bmesh, math, random, mathutils

OUT = "LL_BrickKit.fbx"
PREFIX = "Day5/LL_BrickKit"

BRICK      = (0.52, 0.24, 0.18, 1)  # old brick red-brown
MORTAR     = (0.76, 0.73, 0.68, 1)
STONE_DARK = (0.26, 0.25, 0.24, 1)
CONCRETE   = (0.62, 0.60, 0.58, 1)
WOOD_OLD   = (0.30, 0.22, 0.14, 1)
MOSS       = (0.22, 0.32, 0.16, 1)

def jitter_vertices(obj, amt=0.04):
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    for v in bm.verts:
        v.co += mathutils.Vector((random.uniform(-amt, amt), random.uniform(-amt, amt), random.uniform(-amt, amt)))
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.object.mode_set(mode='OBJECT')

def damage_variants(base_obj, count, amt_start=0.06):
    variants = []
    for i in range(count):
        dup = base_obj.copy(); dup.data = base_obj.data.copy()
        dup.name = f"{base_obj.name}_v{i+1}"
        bpy.context.collection.objects.link(dup)
        jitter_vertices(dup, amt_start + i*0.02)
        # chip off a corner on higher damage
        if i >= 2:
            bpy.context.view_layer.objects.active = dup
            bpy.ops.object.mode_set(mode='EDIT')
            bm = bmesh.from_edit_mesh(dup.data)
            bm.verts.ensure_lookup_table()
            if bm.verts:
                # move the top-right verts randomly
                for v in list(bm.verts)[:4]:
                    v.co += mathutils.Vector((random.uniform(-0.1,0.1),0,random.uniform(-0.15,0.05)))
                bmesh.update_edit_mesh(dup.data)
            bpy.ops.object.mode_set(mode='OBJECT')
        variants.append(dup)
    return variants

def brick_wall_block(w, d, h, name, mat):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0))
    o = bpy.context.view_layer.objects.active
    o.name = name; o.scale = (w,d,h)
    ll.assign_material(o, mat); ll.ensure_uvs(o)
    # give it brick subdivision via loop cuts visually (just bevel)
    ll.add_bevel_modifier(o, width=0.02, segments=1)
    return o

def build():
    random.seed(5)
    ll.clear_scene(); ll.set_scene_units()

    mat_brick  = ll.make_pbr_material("LL_M_Brick", BRICK, roughness=0.92)
    mat_mortar = ll.make_pbr_material("LL_M_Mortar", MORTAR, roughness=0.98)
    mat_stone  = ll.make_pbr_material("LL_M_StoneDark", STONE_DARK, roughness=0.95)
    mat_conc   = ll.make_pbr_material("LL_M_Concrete", CONCRETE, roughness=0.96)
    mat_moss   = ll.make_pbr_material("LL_M_Moss", MOSS, roughness=1.0)

    bpy.ops.object.empty_add(location=(0,0,0))
    root = bpy.context.view_layer.objects.active; root.name = "LL_BrickKit"
    objs = []
    def pr(o): o.parent=root; objs.append(o); return o

    # Layout grid so screenshots show the whole kit
    spacing = 3.0
    col = 0; row = 0
    def place(o, c, r):
        o.location.x = c*spacing; o.location.y = r*spacing
        # keep pivot at base -> offset geometry already at 0,0,0 base; root handles grid
        return o

    # 1 — Wall straight 4m x 3m x 0.4m
    w0 = brick_wall_block(4.0, 0.4, 3.0, "LL_Brick_Wall_Straight", mat_brick); pr(place(w0, 0, 0))
    for v in damage_variants(w0, 2): pr(v)

    # 2 — Wall corner L (3m x 3m)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0))
    wc = bpy.context.view_layer.objects.active; wc.name="LL_Brick_Wall_Corner"; wc.scale=(3.0,0.4,3.0)
    ll.assign_material(wc, mat_brick); ll.ensure_uvs(wc); ll.add_bevel_modifier(wc, width=0.02, segments=1)
    pr(place(wc, 1, 0))
    # corner perpendicular leg
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0))
    wc2=bpy.context.view_layer.objects.active; wc2.name="LL_Brick_Wall_Corner_Leg"
    wc2.scale=(0.4,3.0,3.0); wc2.parent=wc; ll.assign_material(wc2, mat_brick); ll.ensure_uvs(wc2)

    # 3 — Wall broken (ruined top, rubble at base)
    wb = brick_wall_block(4.0, 0.4, 1.8, "LL_Brick_Wall_Broken", mat_brick)
    jitter_vertices(wb, 0.08); pr(place(wb, 2, 0))
    for v in damage_variants(wb, 2): pr(v)

    # 4 — Wall arch (4m with arch hole via two pillars + lintel)
    bpy.ops.object.empty_add(location=(0,0,0)); arch_root=bpy.context.view_layer.objects.active
    arch_root.name="LL_Brick_Wall_Arch"; pr(place(arch_root, 3, 0))
    for sx in (-1.4, 1.4):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(sx,0,1.35)); p=bpy.context.view_layer.objects.active
        p.name=f"LL_Brick_Arch_Pillar_{'L' if sx<0 else 'R'}"; p.scale=(0.55,0.4,2.7); p.parent=arch_root
        ll.assign_material(p, mat_brick); ll.ensure_uvs(p)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,2.85)); lint=bpy.context.view_layer.objects.active
    lint.name="LL_Brick_Arch_Lintel"; lint.scale=(4.0,0.4,0.3); lint.parent=arch_root
    ll.assign_material(lint, mat_brick); ll.ensure_uvs(lint)
    # arch top (half cylinder)
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.9, depth=0.4, location=(0,0,2.65))
    at=bpy.context.view_layer.objects.active; at.name="LL_Brick_Arch_Top"; at.rotation_euler=(0, math.radians(90), 0)
    at.parent=arch_root; ll.assign_material(at, mat_brick); ll.ensure_uvs(at)

    # 5 — Window wall (4m with window opening)
    bpy.ops.object.empty_add(location=(0,0,0)); win_root=bpy.context.view_layer.objects.active
    win_root.name="LL_Brick_Wall_Window"; pr(place(win_root, 0, 1))
    for sx in (-1.35, 1.35):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(sx,0,1.5)); s=bpy.context.view_layer.objects.active
        s.name=f"LL_Brick_Window_Side_{'L' if sx<0 else 'R'}"; s.scale=(1.1,0.4,3.0); s.parent=win_root
        ll.assign_material(s, mat_brick); ll.ensure_uvs(s)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,2.55)); top=bpy.context.view_layer.objects.active
    top.name="LL_Brick_Window_Top"; top.scale=(4.0,0.4,0.45); top.parent=win_root
    ll.assign_material(top, mat_brick); ll.ensure_uvs(top)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0.35)); sill=bpy.context.view_layer.objects.active
    sill.name="LL_Brick_Window_Sill"; sill.scale=(1.6,0.45,0.15); sill.parent=win_root
    ll.assign_material(sill, mat_stone); ll.ensure_uvs(sill)

    # 6 — Door wall
    bpy.ops.object.empty_add(location=(0,0,0)); door_root=bpy.context.view_layer.objects.active
    door_root.name="LL_Brick_Wall_Door"; pr(place(door_root, 1, 1))
    for sx in (-1.45, 1.45):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(sx,0,1.5)); s=bpy.context.view_layer.objects.active
        s.name=f"LL_Brick_Door_Side_{'L' if sx<0 else 'R'}"; s.scale=(0.95,0.4,3.0); s.parent=door_root
        ll.assign_material(s, mat_brick); ll.ensure_uvs(s)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,2.65)); top2=bpy.context.view_layer.objects.active
    top2.name="LL_Brick_Door_Top"; top2.scale=(4.0,0.4,0.35); top2.parent=door_root
    ll.assign_material(top2, mat_brick); ll.ensure_uvs(top2)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,1.0)); door=bpy.context.view_layer.objects.active
    door.name="LL_Brick_Door_Leaf"; door.scale=(0.95,0.08,2.0); door.parent=door_root
    ll.assign_material(door, ll.make_pbr_material("LL_M_WoodDoor", WOOD_OLD, roughness=0.88)); ll.ensure_uvs(door)

    # 7 — Floor 4x4
    fl = brick_wall_block(4.0, 4.0, 0.2, "LL_Brick_Floor_4x4", mat_conc); pr(place(fl, 2, 1))

    # 8 — Stairs  (4 steps)
    bpy.ops.object.empty_add(location=(0,0,0)); stair_root=bpy.context.view_layer.objects.active
    stair_root.name="LL_Brick_Stairs"; pr(place(stair_root, 3, 1))
    for i in range(4):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0.6*i, 0.15+i*0.25)); st=bpy.context.view_layer.objects.active
        st.name=f"LL_Brick_Stair_{i}"; st.scale=(2.0,0.6,0.22); st.parent=stair_root
        ll.assign_material(st, mat_stone); ll.ensure_uvs(st)

    # 9 — Roof beam
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0)); beam=bpy.context.view_layer.objects.active
    beam.name="LL_Brick_Beam"; beam.scale=(4.2,0.22,0.22)
    ll.assign_material(beam, ll.make_pbr_material("LL_M_Beam", WOOD_OLD, roughness=0.88)); ll.ensure_uvs(beam)
    pr(place(beam, 0, 2))

    # 10 — Pillar
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,1.5)); pil=bpy.context.view_layer.objects.active
    pil.name="LL_Brick_Pillar"; pil.scale=(0.45,0.45,3.0)
    ll.assign_material(pil, mat_brick); ll.ensure_uvs(pil); pr(place(pil, 1, 2))

    # 11 — Rubble pile (cluster of small cubes/spheres)
    bpy.ops.object.empty_add(location=(0,0,0)); rub=bpy.context.view_layer.objects.active
    rub.name="LL_Brick_Rubble"; pr(place(rub, 2, 2))
    random.seed(11)
    for i in range(9):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(random.uniform(-0.7,0.7), random.uniform(-0.7,0.7), random.uniform(0.05,0.35)))
        r=bpy.context.view_layer.objects.active; r.name=f"LL_Rubble_{i}"; r.scale=(random.uniform(0.18,0.42), random.uniform(0.18,0.42), random.uniform(0.12,0.28))
        r.rotation_euler=(random.uniform(0, math.pi), random.uniform(0, math.pi), random.uniform(0, math.pi))
        r.parent=rub; m = mat_brick if i%2==0 else mat_stone; ll.assign_material(r, m); ll.ensure_uvs(r)
        jitter_vertices(r, 0.03)

    # 12 — Low wall / parapet
    low = brick_wall_block(4.0, 0.35, 1.0, "LL_Brick_Wall_Low", mat_brick); pr(place(low, 3, 2))

    # tris
    tris = sum(ll.tri_count(o) for o in objs if o.type=='MESH')
    # count empties' children too
    for o in bpy.data.objects:
        if o.type=='MESH' and o not in objs:
            tris += ll.tri_count(o)
    print(f"[BRICKKIT] ~{tris} tris total kit, {len([o for o in objs if o.type=='MESH'])} meshes (+ arch/window submeshes)")
    ll.print_scene_stats()
    # screenshots — wide view of the kit grid
    ll.screenshot_trio(PREFIX, look_at=(4.5, 3.0, 1.0), lens=38)

    for o in objs:
        if o.type=='MESH':
            ll.select_only(o); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    # also apply children of empties
    for o in list(bpy.data.objects):
        if o.type=='MESH' and o.parent and o.parent.name.startswith("LL_Brick_Wall_Arch"):
            ll.select_only(o); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    all_meshes = [o for o in bpy.data.objects if o.type=='MESH']
    ll.export_fbx(all_meshes, OUT)
    print(f"[DONE] BrickKit -> {OUT}")

if __name__=="__main__":
    build()
