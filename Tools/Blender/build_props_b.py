#!/usr/bin/env blender
import sys,os; sys.path.insert(0, os.path.join(os.path.dirname(__file__)))
import ll_pipeline as ll
import bpy, bmesh, math

def build_fence_dock():
    ll.clear_scene(); ll.set_scene_units()
    mat_wood = ll.make_pbr_material("LL_M_FenceWood", ll.PALETTE["wood_grey"], roughness=0.89)
    mat_wood_dark = ll.make_pbr_material("LL_M_FenceWoodDark", ll.PALETTE["wood_dark"], roughness=0.90)
    bpy.ops.object.empty_add(location=(0,0,0)); root=bpy.context.view_layer.objects.active; root.name="LL_Fence_Dock"
    objs=[] ;
    def pr(o): o.parent=root; objs.append(o); return o
    # Fence: 5m long, 4 posts + 2 rails + pickets
    length=5.0; posts=4
    for i in range(posts):
        x = -length/2 + i*(length/(posts-1))
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x,0,0.65))
        post=bpy.context.view_layer.objects.active; post.name=f"LL_Fence_Post_{i}"
        post.scale=(0.10,0.10,1.15)
        ll.assign_material(post, mat_wood_dark); ll.ensure_uvs(post); pr(post)
    for r in [0.42, 0.85]:
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,r))
        rail=bpy.context.view_layer.objects.active; rail.name=f"LL_Fence_Rail_{'Low' if r<0.5 else 'High'}"
        rail.scale=(5.0,0.06,0.09)
        ll.assign_material(rail, mat_wood); ll.ensure_uvs(rail); pr(rail)
    # Pickets (every 0.35m)
    n_pickets = int(length/0.35)-1
    for k in range(n_pickets):
        x = -length/2+0.22 + k*0.35
        # skip near posts
        near_post = any(abs(x - (-length/2 + j*(length/(posts-1)))) < 0.11 for j in range(posts))
        if near_post: continue
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x,0,0.62))
        pk=bpy.context.view_layer.objects.active; pk.name=f"LL_Fence_Picket_{k:02d}"
        pk.scale=(0.07,0.025,1.0)
        # slight height jitter
        import random; random.seed(k*17)
        h = 1.0 + (random.random()-0.5)*0.12
        pk.scale.z = h
        ll.assign_material(pk, mat_wood); ll.ensure_uvs(pk); pr(pk)

    # Dock section: 3 planks on 2 beams
    for b in [-0.85,0.85]:
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0,b,0.22))
        beam=bpy.context.view_layer.objects.active; beam.name=f"LL_Dock_Beam_{'N' if b<0 else 'S'}"
        beam.scale=(5.2,0.14,0.18)
        ll.assign_material(beam, mat_wood_dark); ll.ensure_uvs(beam); pr(beam)
    for p in range(9):
        x = -2.2 + p*0.55
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x,0,0.32))
        plank=bpy.context.view_layer.objects.active; plank.name=f"LL_Dock_Plank_{p:02d}"
        plank.scale=(0.42,1.75,0.05)
        ll.assign_material(plank, mat_wood); ll.ensure_uvs(plank); pr(plank)

    total=sum(ll.tri_count(o) for o in objs)
    print(f"[FENCE/DOCK] {total} tris (budget 2000)"); ll.print_scene_stats()
    ll.screenshot_trio("LL_Fence_Dock", look_at=(0,0,0.45), lens=45)
    for o in objs: ll.select_only(o); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ll.export_fbx(objs, "LL_Fence_Dock.fbx")

    # Also export fence alone (4m straight, for instancing)
    fence_only=[o for o in objs if "Fence" in o.name]
    ll.export_fbx(fence_only, "LL_Fence_4m.fbx")
    dock_only=[o for o in objs if "Dock" in o.name]
    ll.export_fbx(dock_only, "LL_Dock_5m.fbx")

def build_crates_barrels():
    ll.clear_scene(); ll.set_scene_units()
    mat_crate = ll.make_pbr_material("LL_M_Crate", ll.PALETTE["wood_old"], roughness=0.87)
    mat_crate2 = ll.make_pbr_material("LL_M_CrateDark", ll.PALETTE["wood_dark"], roughness=0.89)
    mat_barrel = ll.make_pbr_material("LL_M_Barrel", (0.42,0.30,0.18,1), roughness=0.80)
    mat_metal = ll.make_pbr_material("LL_M_BarrelHoop", ll.PALETTE["metal_rust"], roughness=0.70, metallic=0.35)

    bpy.ops.object.empty_add(location=(0,0,0)); root=bpy.context.view_layer.objects.active; root.name="LL_Crates_Barrels"
    objs=[];
    def pr(o): o.parent=root; objs.append(o); return o

    # Crate 1 (closed)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(-1.1,0,0.42))
    crate=bpy.context.view_layer.objects.active; crate.name="LL_Crate_Closed"
    crate.scale=(0.78,0.78,0.68)
    ll.assign_material(crate, mat_crate); ll.ensure_uvs(crate)
    ll.add_bevel_modifier(crate, width=0.018, segments=1)
    pr(crate)
    # crate slats detail (2 straps)
    for z in [0.22,0.62]:
        bpy.ops.mesh.primitive_cube_add(size=1, location=(-1.1,0,z))
        slat=bpy.context.view_layer.objects.active; slat.name=f"LL_Crate_Slat_{int(z*100)}"
        slat.scale=(0.80,0.80,0.06)
        ll.assign_material(slat, mat_crate2); ll.ensure_uvs(slat); pr(slat)

    # Crate 2 (open, half-height)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.05,0,0.28))
    crate2=bpy.context.view_layer.objects.active; crate2.name="LL_Crate_Open"
    crate2.scale=(0.85,0.85,0.42)
    ll.assign_material(crate2, mat_crate); ll.ensure_uvs(crate2); pr(crate2)

    # Barrel
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.38, depth=0.72, location=(1.18,0,0.38))
    barrel=bpy.context.view_layer.objects.active; barrel.name="LL_Barrel"
    ll.assign_material(barrel, mat_barrel); ll.ensure_uvs(barrel); ll.shade_smooth(barrel)
    pr(barrel)
    for z in [0.18,0.38,0.58]:
        bpy.ops.mesh.primitive_torus_add(major_radius=0.38, minor_radius=0.018, major_segments=16, minor_segments=6, location=(1.18,0,z))
        hoop=bpy.context.view_layer.objects.active; hoop.name=f"LL_Barrel_Hoop_{int(z*100)}"
        ll.assign_material(hoop, mat_metal); ll.ensure_uvs(hoop); pr(hoop)
    # lid
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.38, depth=0.035, location=(1.18,0,0.74))
    lid=bpy.context.view_layer.objects.active; lid.name="LL_Barrel_Lid"
    ll.assign_material(lid, mat_crate2); ll.ensure_uvs(lid); pr(lid)

    total=sum(ll.tri_count(o) for o in objs)
    print(f"[CRATES/BARRELS] {total} tris (budget 1500)"); ll.print_scene_stats()
    ll.screenshot_trio("LL_Crates_Barrels", look_at=(0,0,0.38), lens=50)
    for o in objs: ll.select_only(o); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ll.export_fbx(objs, "LL_Crates_Barrels.fbx")
    # Individual
    crate_set=[o for o in objs if "Crate" in o.name]
    barrel_set=[o for o in objs if "Barrel" in o.name]
    ll.export_fbx(crate_set, "LL_Crates.fbx")
    ll.export_fbx(barrel_set, "LL_Barrel.fbx")

def build_rusted_props():
    ll.clear_scene(); ll.set_scene_units()
    mat_rust = ll.make_pbr_material("LL_M_Rust", ll.PALETTE["metal_rust"], roughness=0.80, metallic=0.25)
    mat_iron = ll.make_pbr_material("LL_M_Iron", ll.PALETTE["metal_iron"], roughness=0.72, metallic=0.15)
    mat_galv = ll.make_pbr_material("LL_M_Galv", ll.PALETTE["metal_galv"], roughness=0.52, metallic=0.65)
    mat_rope = ll.make_pbr_material("LL_M_Rope", ll.PALETTE["rope"], roughness=0.92)
    bpy.ops.object.empty_add(location=(0,0,0)); root=bpy.context.view_layer.objects.active; root.name="LL_Rusted_Props"
    objs=[];
    def pr(o): o.parent=root; objs.append(o); return o

    # Anchor (small, decorative)
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.025, depth=0.65, location=(-1.2,0,0.42))
    shank=bpy.context.view_layer.objects.active; shank.name="LL_Anchor_Shank"
    ll.assign_material(shank, mat_rust); ll.ensure_uvs(shank); pr(shank)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.07, minor_radius=0.018, major_segments=10, minor_segments=6, location=(-1.2,0,0.76))
    ring=bpy.context.view_layer.objects.active; ring.name="LL_Anchor_Ring"
    ll.assign_material(ring, mat_galv); ll.ensure_uvs(ring); pr(ring)
    for s in [-1,1]:
        bpy.ops.mesh.primitive_cube_add(size=1, location=(-1.2+s*0.11,0,0.14))
        fluke=bpy.context.view_layer.objects.active; fluke.name=f"LL_Anchor_Fluke_{'L' if s<0 else 'R'}"
        fluke.scale=(0.12,0.04,0.22); fluke.rotation_euler=(0,0, math.radians(32*s))
        ll.assign_material(fluke, mat_rust); ll.ensure_uvs(fluke); pr(fluke)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(-1.2,0,0.12))
    stock=bpy.context.view_layer.objects.active; stock.name="LL_Anchor_Stock"
    stock.scale=(0.42,0.04,0.04)
    ll.assign_material(stock, mat_iron); ll.ensure_uvs(stock); pr(stock)

    # Buoy (rusted sphere + cone top)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=14, ring_count=10, radius=0.32, location=(0,0,0.34))
    buoy=bpy.context.view_layer.objects.active; buoy.name="LL_Buoy"
    ll.assign_material(buoy, mat_rust); ll.ensure_uvs(buoy); pr(buoy)
    bpy.ops.mesh.primitive_cone_add(vertices=14, radius1=0.10, radius2=0.02, depth=0.22, location=(0,0,0.68))
    buoy_top=bpy.context.view_layer.objects.active; buoy_top.name="LL_Buoy_Top"
    ll.assign_material(buoy_top, mat_iron); ll.ensure_uvs(buoy_top); pr(buoy_top)

    # Chain pile (coiled torus chain)
    for i in range(5):
        bpy.ops.mesh.primitive_torus_add(major_radius=0.07, minor_radius=0.018, major_segments=10, minor_segments=6, location=(1.15+ (i%2)*0.04, (i%2)*0.05, 0.05+i*0.04))
        link=bpy.context.view_layer.objects.active; link.name=f"LL_Chain_Link_{i}"
        link.rotation_euler=(math.radians(90) if i%2==0 else 0,0,0)
        ll.assign_material(link, mat_rust); ll.ensure_uvs(link); pr(link)

    total=sum(ll.tri_count(o) for o in objs)
    print(f"[RUSTED PROPS] {total} tris (budget 1500)"); ll.print_scene_stats()
    ll.screenshot_trio("LL_Rusted_Props", look_at=(0,0,0.32), lens=50)
    for o in objs: ll.select_only(o); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ll.export_fbx(objs, "LL_Rusted_Props.fbx")
    # Individual
    anchor=[o for o in objs if "Anchor" in o.name]
    buoy_set=[o for o in objs if "Buoy" in o.name]
    chain=[o for o in objs if "Chain" in o.name]
    ll.export_fbx(anchor, "LL_Anchor.fbx")
    ll.export_fbx(buoy_set, "LL_Buoy.fbx")
    ll.export_fbx(chain, "LL_Chain_Pile.fbx")

if __name__=="__main__":
    build_fence_dock()
    build_crates_barrels()
    build_rusted_props()
    print("[DONE] Fence/Dock + Crates/Barrels + Rusted Props exported")
