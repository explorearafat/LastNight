#!/usr/bin/env blender
import sys,os; sys.path.insert(0, os.path.join(os.path.dirname(__file__)))
import ll_pipeline as ll
import bpy, bmesh, math

def build_lantern():
    ll.clear_scene(); ll.set_scene_units()
    mat_brass = ll.make_pbr_material("LL_M_LanternBrass", (0.55,0.42,0.18,1), roughness=0.45, metallic=0.75)
    mat_rust = ll.make_pbr_material("LL_M_LanternRust", ll.PALETTE["metal_rust"], roughness=0.78, metallic=0.3)
    mat_iron = ll.make_pbr_material("LL_M_LanternIron", ll.PALETTE["metal_iron"], roughness=0.65, metallic=0.2)
    mat_glass = bpy.data.materials.new("LL_M_LanternGlass")
    mat_glass.use_nodes=True; ng=mat_glass.node_tree.nodes; lk=mat_glass.node_tree.links; ng.clear()
    out=ng.new('ShaderNodeOutputMaterial'); out.location=(400,0)
    bsdf=ng.new('ShaderNodeBsdfPrincipled'); bsdf.location=(0,0)
    bsdf.inputs['Base Color'].default_value=(0.85,0.82,0.70,1.0)
    bsdf.inputs['Roughness'].default_value=0.18
    try: bsdf.inputs['Transmission Weight'].default_value=0.6
    except: pass
    lk.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    mat_glass.blend_method='BLEND'

    bpy.ops.object.empty_add(location=(0,0,0)); root=bpy.context.view_layer.objects.active; root.name="LL_Lantern"
    objs=[];
    def pr(o): o.parent=root; objs.append(o); return o

    # Base
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.18, depth=0.04, location=(0,0,0.04))
    base=bpy.context.view_layer.objects.active; base.name="LL_Lantern_Base"
    ll.assign_material(base, mat_iron); ll.ensure_uvs(base); pr(base)

    # Glass body (hex)
    bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.15, depth=0.42, location=(0,0,0.28))
    glass=bpy.context.view_layer.objects.active; glass.name="LL_Lantern_Glass"
    ll.assign_material(glass, mat_glass); ll.ensure_uvs(glass); ll.shade_smooth(glass)
    pr(glass)

    # Vertical frame bars (6)
    for k in range(6):
        ang = k/6*math.pi*2
        x=math.cos(ang)*0.15; y=math.sin(ang)*0.15
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x,y,0.28))
        bar=bpy.context.view_layer.objects.active; bar.name=f"LL_Lantern_Bar_{k}"
        bar.scale=(0.012,0.012,0.42)
        bar.rotation_euler=(0,0,ang)
        ll.assign_material(bar, mat_brass); ll.ensure_uvs(bar)
        pr(bar)

    # Top cap
    bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.17, depth=0.05, location=(0,0,0.50))
    top=bpy.context.view_layer.objects.active; top.name="LL_Lantern_Top"
    ll.assign_material(top, mat_brass); ll.ensure_uvs(top); pr(top)

    # Top handle arch
    bpy.ops.mesh.primitive_torus_add(major_radius=0.07, minor_radius=0.012, major_segments=10, minor_segments=6, location=(0,0,0.57))
    handle=bpy.context.view_layer.objects.active; handle.name="LL_Lantern_Handle"
    handle.rotation_euler=(math.radians(90),0,0)
    ll.assign_material(handle, mat_brass); ll.ensure_uvs(handle)
    # cut bottom half — scale
    pr(handle)

    total=sum(ll.tri_count(o) for o in objs)
    print(f"[LANTERN] {total} tris (budget 800)"); ll.print_scene_stats()
    ll.screenshot_trio("LL_Lantern", look_at=(0,0,0.28), lens=55)
    for o in objs: ll.select_only(o); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ll.export_fbx(objs, "LL_Lantern.fbx")

def build_rocks():
    ll.clear_scene(); ll.set_scene_units()
    mat_stone = ll.make_pbr_material("LL_M_RockStone", ll.PALETTE["stone_dark"], roughness=0.94)
    mat_moss = ll.make_pbr_material("LL_M_RockMoss", ll.PALETTE["moss"], roughness=0.92)
    import random; random.seed(42)
    bpy.ops.object.empty_add(location=(0,0,0)); root=bpy.context.view_layer.objects.active; root.name="LL_Rocks"
    all_objs=[]
    # 4 variants spaced apart
    for idx in range(4):
        scales = [(1.6,1.2,0.9),(1.0,0.85,1.35),(2.0,1.4,0.65),(0.7,0.6,0.55)]
        sx,sy,sz = scales[idx]
        # Use icosphere subdiv 1 + displace
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.0, location=(idx*3.5, 0, 0))
        rock=bpy.context.view_layer.objects.active; rock.name=f"LL_Rock_0{idx+1}"
        rock.scale=(sx,sy,sz)
        rock.parent=root
        # Random displace
        bpy.ops.object.mode_set(mode='EDIT')
        bm=bmesh.from_edit_mesh(rock.data); bm.verts.ensure_lookup_table()
        for v in bm.verts:
            n = v.normal
            disp = (random.random()-0.5)*0.18
            v.co += n*disp
        bmesh.update_edit_mesh(rock.data); bpy.ops.object.mode_set(mode='OBJECT')
        # Flatten bottom
        bpy.ops.object.mode_set(mode='EDIT')
        bm=bmesh.from_edit_mesh(rock.data); bm.verts.ensure_lookup_table()
        minz=min(v.co.z for v in bm.verts)
        for v in bm.verts:
            if v.co.z < minz + 0.15:
                v.co.z = minz
        bmesh.update_edit_mesh(rock.data); bpy.ops.object.mode_set(mode='OBJECT')
        ll.assign_material(rock, mat_stone if idx!=1 else mat_moss)
        ll.shade_smooth(rock); ll.ensure_uvs(rock)
        ll.add_bevel_modifier(rock, width=0.04, segments=1)
        all_objs.append(rock)

    total=sum(ll.tri_count(o) for o in all_objs)
    print(f"[ROCKS] {total} tris total, avg {total//4} per rock (budget 800 each)"); ll.print_scene_stats()
    ll.screenshot_trio("LL_Rocks", look_at=(5,0,0.5), lens=45)
    for o in all_objs: ll.select_only(o); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    # Export each separately + as set
    for o in all_objs:
        ll.export_fbx([o], f"{o.name}.fbx")
    ll.export_fbx(all_objs, "LL_Rocks_Set.fbx")

def build_driftwood():
    ll.clear_scene(); ll.set_scene_units()
    mat_wood = ll.make_pbr_material("LL_M_DriftWood", ll.PALETTE["wood_grey"], roughness=0.88)
    import random; random.seed(7)
    bpy.ops.object.empty_add(location=(0,0,0)); root=bpy.context.view_layer.objects.active; root.name="LL_Driftwood"
    objs=[];
    def pr(o): o.parent=root; objs.append(o); return o
    # Curved log via subdivided cube + bend
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0.18))
    log=bpy.context.view_layer.objects.active; log.name="LL_Driftwood_Log"
    log.scale=(1.8,0.22,0.22)
    # Subdivide
    bpy.ops.object.mode_set(mode='EDIT')
    bm=bmesh.from_edit_mesh(log.data)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=6)
    # Bend
    bm.verts.ensure_lookup_table()
    for v in bm.verts:
        bend = math.sin((v.co.x+0.9)/1.8*math.pi)*0.18
        v.co.y += bend
        v.co.z += (random.random()-0.5)*0.04
    bmesh.update_edit_mesh(log.data); bpy.ops.object.mode_set(mode='OBJECT')
    ll.assign_material(log, mat_wood); ll.ensure_uvs(log); ll.shade_smooth(log)
    pr(log)
    # Broken branch stub
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.065, depth=0.32, location=(0.55,0.12,0.22))
    stub=bpy.context.view_layer.objects.active; stub.name="LL_Driftwood_Stub"
    stub.rotation_euler=(math.radians(35), math.radians(20),0)
    ll.assign_material(stub, mat_wood); ll.ensure_uvs(stub)
    pr(stub)

    total=sum(ll.tri_count(o) for o in objs)
    print(f"[DRIFTWOOD] {total} tris (budget 600)"); ll.print_scene_stats()
    ll.screenshot_trio("LL_Driftwood", look_at=(0,0,0.22), lens=55)
    for o in objs: ll.select_only(o); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ll.export_fbx(objs, "LL_Driftwood.fbx")

if __name__=="__main__":
    build_lantern()
    build_rocks()
    build_driftwood()
    print("[DONE] Lantern + Rocks + Driftwood exported")
