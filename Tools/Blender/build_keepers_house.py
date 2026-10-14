#!/usr/bin/env blender
# Build Keeper's House — small stone cottage near the lighthouse
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__)))
import ll_pipeline as ll
import bpy, bmesh, math, mathutils

OUT_FBX = "LL_Keepers_House.fbx"
PREFIX = "LL_Keepers_House"

def build():
    ll.clear_scene(); ll.set_scene_units()
    mat_stone = ll.make_pbr_material("LL_M_HouseStone", ll.PALETTE["stone_mid"], roughness=0.93)
    mat_stone_dark = ll.make_pbr_material("LL_M_HouseStoneDark", ll.PALETTE["stone_dark"], roughness=0.96)
    mat_wood = ll.make_pbr_material("LL_M_HouseWood", ll.PALETTE["wood_old"], roughness=0.86)
    mat_wood_grey = ll.make_pbr_material("LL_M_HouseWoodGrey", ll.PALETTE["wood_grey"], roughness=0.88)
    mat_roof = ll.make_pbr_material("LL_M_HouseRoof", (0.22,0.22,0.24,1), roughness=0.78)
    mat_metal = ll.make_pbr_material("LL_M_HouseMetal", ll.PALETTE["metal_rust"], roughness=0.72, metallic=0.3)
    mat_glass = bpy.data.materials.new("LL_M_HouseGlass")
    mat_glass.use_nodes = True; ng=mat_glass.node_tree.nodes; lk=mat_glass.node_tree.links; ng.clear()
    out=ng.new('ShaderNodeOutputMaterial'); out.location=(400,0)
    bsdf=ng.new('ShaderNodeBsdfPrincipled'); bsdf.location=(0,0)
    bsdf.inputs['Base Color'].default_value=(0.7,0.78,0.85,1.0)
    bsdf.inputs['Roughness'].default_value=0.15
    try: bsdf.inputs['Transmission Weight'].default_value=0.85
    except: pass
    lk.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    mat_glass.blend_method='BLEND'

    bpy.ops.object.empty_add(location=(0,0,0))
    root = bpy.context.view_layer.objects.active
    root.name = "LL_Keepers_House"
    objs=[] ;
    def pr(o): o.parent=root; objs.append(o); return o

    # Main box 8 x 6 x 2.8m walls
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,1.4))
    walls = bpy.context.view_layer.objects.active
    walls.name="LL_House_Walls"
    walls.scale=(8,6,2.8)
    ll.assign_material(walls, mat_stone); ll.ensure_uvs(walls)
    pr(walls)

    # Foundation strip
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,0.22))
    found=bpy.context.view_layer.objects.active
    found.name="LL_House_Foundation"
    found.scale=(8.5,6.5,0.4)
    ll.assign_material(found, mat_stone_dark); ll.ensure_uvs(found)
    pr(found)

    # Pitched roof (prism)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,3.55))
    roof=bpy.context.view_layer.objects.active
    roof.name="LL_House_Roof"
    roof.scale=(8.4,6.2,0.12)
    # Replace flat roof with gable — delete the placeholder cube
    bpy.ops.object.delete(use_global=False)
    for side in [-1,1]:
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0, side*1.4, 3.65))
        half=bpy.context.view_layer.objects.active
        half.name=f"LL_House_Roof_{'N' if side<0 else 'S'}"
        half.scale=(8.6, 3.3, 0.14)
        half.rotation_euler=(math.radians(28*side),0,0)
        ll.assign_material(half, mat_roof); ll.ensure_uvs(half)
        pr(half)
    # Ridge cap
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,0,4.18))
    ridge=bpy.context.view_layer.objects.active
    ridge.name="LL_House_Ridge"
    ridge.scale=(8.7,0.28,0.14)
    ll.assign_material(ridge, mat_roof); ll.ensure_uvs(ridge)
    pr(ridge)

    # Chimney
    bpy.ops.mesh.primitive_cube_add(size=1, location=(3.2,0,3.9))
    chim=bpy.context.view_layer.objects.active
    chim.name="LL_House_Chimney"
    chim.scale=(0.85,0.85,2.1)
    ll.assign_material(chim, mat_stone_dark); ll.ensure_uvs(chim)
    pr(chim)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(3.2,0,5.05))
    chimcap=bpy.context.view_layer.objects.active
    chimcap.name="LL_House_ChimneyCap"
    chimcap.scale=(1.1,1.1,0.14)
    ll.assign_material(chimcap, mat_stone_dark); ll.ensure_uvs(chimcap)
    pr(chimcap)

    # Door
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,3.06,1.25))
    door=bpy.context.view_layer.objects.active
    door.name="LL_House_Door"
    door.scale=(1.05,0.12,1.9)
    ll.assign_material(door, mat_wood); ll.ensure_uvs(door)
    pr(door)
    # door handle
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.05, depth=0.12, location=(0.35,3.12,1.2))
    h=bpy.context.view_layer.objects.active
    h.name="LL_House_DoorHandle"
    h.rotation_euler=(math.radians(90),0,0)
    ll.assign_material(h, mat_metal); ll.ensure_uvs(h)
    pr(h)

    # Windows x3
    for i, (x,z) in enumerate([( -2.4,1.45),(2.4,1.45),(3.8,1.45)]):
        y = 3.06 if i<2 else 0 # side window
        wx, wy = (x,y) if i<2 else (4.06, x+0.5)
        bpy.ops.mesh.primitive_cube_add(size=1, location=(wx,wy,z))
        wf=bpy.context.view_layer.objects.active
        wf.name=f"LL_House_WindowFrame_{i}"
        wf.scale=(0.95,0.14,0.85) if i<2 else (0.14,0.95,0.85)
        ll.assign_material(wf, mat_wood_grey); ll.ensure_uvs(wf)
        pr(wf)
        bpy.ops.mesh.primitive_cube_add(size=1, location=(wx,wy+ (0.04 if i<2 else 0),z))
        wg=bpy.context.view_layer.objects.active
        wg.name=f"LL_House_WindowGlass_{i}"
        wg.scale=(0.78,0.04,0.68) if i<2 else (0.04,0.78,0.68)
        ll.assign_material(wg, mat_glass); ll.ensure_uvs(wg)
        pr(wg)

    # Small porch step
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0,3.45,0.18))
    step=bpy.context.view_layer.objects.active
    step.name="LL_House_Step"
    step.scale=(1.6,0.55,0.12)
    ll.assign_material(step, mat_stone); ll.ensure_uvs(step)
    pr(step)

    total=sum(ll.tri_count(o) for o in objs)
    print(f"[HOUSE] {total} tris (budget 8000) — {len(objs)} objs")
    ll.print_scene_stats()
    ll.screenshot_trio(PREFIX, look_at=(0,0,1.6), lens=42)
    for o in objs:
        ll.select_only(o); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ll.export_fbx(objs, OUT_FBX)
    print(f"[DONE] Keepers House exported")

if __name__=="__main__":
    build()
