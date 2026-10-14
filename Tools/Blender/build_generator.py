#!/usr/bin/env blender
# Build generator + fuel canister set
import sys,os; sys.path.insert(0, os.path.join(os.path.dirname(__file__)))
import ll_pipeline as ll
import bpy, bmesh, math, mathutils

def build_generator():
    ll.clear_scene(); ll.set_scene_units()
    mat_body = ll.make_pbr_material("LL_M_GenBody", ll.PALETTE["metal_galv"], roughness=0.55, metallic=0.6)
    mat_rust = ll.make_pbr_material("LL_M_GenRust", ll.PALETTE["metal_rust"], roughness=0.78, metallic=0.3)
    mat_iron = ll.make_pbr_material("LL_M_GenIron", ll.PALETTE["metal_iron"], roughness=0.7, metallic=0.15)
    mat_cable = ll.make_pbr_material("LL_M_Cable", (0.08,0.08,0.09,1), roughness=0.92)
    mat_gauge = ll.make_pbr_material("LL_M_Gauge", (0.92,0.92,0.90,1), roughness=0.35, metallic=0.1)

    bpy.ops.object.empty_add(location=(0,0,0)); root=bpy.context.view_layer.objects.active; root.name="LL_Generator"
    objs=[];
    def pr(o): o.parent=root; objs.append(o); return o

    # Main body 1.1 x 0.7 x 0.85
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0.52))
    body=bpy.context.view_layer.objects.active; body.name="LL_Generator_Body"; body.scale=(1.1,0.72,0.78)
    ll.assign_material(body, mat_body); ll.ensure_uvs(body)
    ll.add_bevel_modifier(body, width=0.025, segments=1)
    pr(body)

    # Fuel tank on top (cylinder along X)
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.22, depth=0.9, location=(0,0,1.08))
    tank=bpy.context.view_layer.objects.active; tank.name="LL_Generator_Tank"
    tank.rotation_euler=(0, math.radians(90),0)
    ll.assign_material(tank, mat_rust); ll.ensure_uvs(tank); ll.shade_smooth(tank)
    pr(tank)
    # tank caps
    for sx in [-0.45,0.45]:
        bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.22, depth=0.04, location=(sx,0,1.08))
        cap=bpy.context.view_layer.objects.active; cap.name=f"LL_Gen_TankCap_{'L' if sx<0 else 'R'}"
        cap.rotation_euler=(0, math.radians(90),0)
        ll.assign_material(cap, mat_iron); ll.ensure_uvs(cap)
        pr(cap)

    # Exhaust pipe
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.055, depth=0.55, location=(0.38,0,1.15))
    exh=bpy.context.view_layer.objects.active; exh.name="LL_Generator_Exhaust"
    ll.assign_material(exh, mat_iron); ll.ensure_uvs(exh)
    pr(exh)

    # Control panel bump
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0.38,0.58))
    panel=bpy.context.view_layer.objects.active; panel.name="LL_Generator_Panel"; panel.scale=(0.55,0.06,0.35)
    ll.assign_material(panel, mat_iron); ll.ensure_uvs(panel)
    pr(panel)

    # Gauge (small cylinder on panel)
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.09, depth=0.03, location=( -0.12,0.41,0.62))
    gauge=bpy.context.view_layer.objects.active; gauge.name="LL_Generator_Gauge"
    gauge.rotation_euler=(math.radians(90),0,0)
    ll.assign_material(gauge, mat_gauge); ll.ensure_uvs(gauge)
    pr(gauge)

    # Feet
    for sx,sy in [(-0.45,-0.28),(0.45,-0.28),(-0.45,0.28),(0.45,0.28)]:
        bpy.ops.mesh.primitive_cube_add(size=1, location=(sx,sy,0.06))
        foot=bpy.context.view_layer.objects.active; foot.name=f"LL_Gen_Foot_{sx:.1f}_{sy:.1f}"
        foot.scale=(0.14,0.14,0.10)
        ll.assign_material(foot, mat_iron); ll.ensure_uvs(foot)
        pr(foot)

    # Cables
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.03, depth=0.9, location=(0.55,0.35,0.35))
    cab=bpy.context.view_layer.objects.active; cab.name="LL_Generator_Cable"
    cab.rotation_euler=(math.radians(35),0,math.radians(25))
    ll.assign_material(cab, mat_cable); ll.ensure_uvs(cab)
    pr(cab)

    total=sum(ll.tri_count(o) for o in objs)
    print(f"[GENERATOR] {total} tris (budget 3000)"); ll.print_scene_stats()
    ll.screenshot_trio("LL_Generator", look_at=(0,0,0.55), lens=50)
    for o in objs: ll.select_only(o); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ll.export_fbx(objs, "LL_Generator.fbx")

def build_fuel_canister():
    ll.clear_scene(); ll.set_scene_units()
    mat_red = ll.make_pbr_material("LL_M_CanRed", ll.PALETTE["tower_red"], roughness=0.72)
    mat_metal = ll.make_pbr_material("LL_M_CanMetal", ll.PALETTE["metal_galv"], roughness=0.45, metallic=0.6)
    mat_rust = ll.make_pbr_material("LL_M_CanRust", ll.PALETTE["metal_rust"], roughness=0.8, metallic=0.25)

    bpy.ops.object.empty_add(location=(0,0,0)); root=bpy.context.view_layer.objects.active; root.name="LL_Fuel_Canister"
    objs=[];
    def pr(o): o.parent=root; objs.append(o); return o

    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0.32))
    body=bpy.context.view_layer.objects.active; body.name="LL_Canister_Body"; body.scale=(0.34,0.22,0.52)
    ll.assign_material(body, mat_red); ll.ensure_uvs(body)
    ll.add_bevel_modifier(body, width=0.015, segments=1)
    pr(body)

    # Top handle frame
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0.66))
    top=bpy.context.view_layer.objects.active; top.name="LL_Canister_Top"; top.scale=(0.34,0.22,0.06)
    ll.assign_material(top, mat_metal); ll.ensure_uvs(top)
    pr(top)
    # Handle
    bpy.ops.mesh.primitive_torus_add(major_radius=0.065, minor_radius=0.018, major_segments=12, minor_segments=6, location=(0,0,0.71))
    handle=bpy.context.view_layer.objects.active; handle.name="LL_Canister_Handle"
    handle.rotation_euler=(math.radians(90),0,0)
    ll.assign_material(handle, mat_metal); ll.ensure_uvs(handle)
    pr(handle)
    # Spout
    bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=0.035, depth=0.14, location=(0.12,0,0.62))
    spout=bpy.context.view_layer.objects.active; spout.name="LL_Canister_Spout"
    ll.assign_material(spout, mat_rust); ll.ensure_uvs(spout)
    pr(spout)
    # Cap
    bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=0.045, depth=0.03, location=(0.12,0,0.70))
    cap=bpy.context.view_layer.objects.active; cap.name="LL_Canister_Cap"
    ll.assign_material(cap, mat_metal); ll.ensure_uvs(cap)
    pr(cap)

    total=sum(ll.tri_count(o) for o in objs)
    print(f"[CANISTER] {total} tris (budget 800)"); ll.print_scene_stats()
    ll.screenshot_trio("LL_Fuel_Canister", look_at=(0,0,0.35), lens=55)
    for o in objs: ll.select_only(o); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ll.export_fbx(objs, "LL_Fuel_Canister.fbx")

if __name__=="__main__":
    build_generator()
    build_fuel_canister()
    print("[DONE] Generator + Canister exported")
