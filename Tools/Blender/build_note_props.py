#!/usr/bin/env blender
import sys,os; sys.path.insert(0, os.path.join(os.path.dirname(__file__)))
import ll_pipeline as ll
import bpy, bmesh, math, random

def build_note_props():
    ll.clear_scene(); ll.set_scene_units()
    mat_paper = ll.make_pbr_material("LL_M_Paper", ll.PALETTE["paper"], roughness=0.92)
    mat_paper_dark = ll.make_pbr_material("LL_M_PaperDark", (0.72,0.66,0.52,1), roughness=0.90)
    mat_wood = ll.make_pbr_material("LL_M_NoteWood", ll.PALETTE["wood_dark"], roughness=0.85)
    bpy.ops.object.empty_add(location=(0,0,0)); root=bpy.context.view_layer.objects.active; root.name="LL_Note_Props"
    objs=[];
    def pr(o): o.parent=root; objs.append(o); return o

    # Note paper (slightly curled plane)
    bpy.ops.mesh.primitive_plane_add(size=1, location=(-0.55,0,0.02))
    paper=bpy.context.view_layer.objects.active; paper.name="LL_Note_Paper"
    paper.scale=(0.42,0.58,1)
    # Curl
    bpy.ops.object.mode_set(mode='EDIT')
    bm=bmesh.from_edit_mesh(paper.data); bm.verts.ensure_lookup_table()
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=4)
    bm.verts.ensure_lookup_table()
    for v in bm.verts:
        v.co.z += math.sin((v.co.x+0.21)/0.42*math.pi)*0.025
    bmesh.update_edit_mesh(paper.data); bpy.ops.object.mode_set(mode='OBJECT')
    ll.assign_material(paper, mat_paper); ll.ensure_uvs(paper); pr(paper)

    # Folded note variant
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.30,0,0.04))
    folded=bpy.context.view_layer.objects.active; folded.name="LL_Note_Folded"
    folded.scale=(0.32,0.42,0.04)
    ll.assign_material(folded, mat_paper_dark); ll.ensure_uvs(folded); pr(folded)

    # Clipboard / board
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.95,0,0.06))
    board=bpy.context.view_layer.objects.active; board.name="LL_Note_Board"
    board.scale=(0.48,0.66,0.025)
    ll.assign_material(board, mat_wood); ll.ensure_uvs(board); pr(board)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.95,0.26,0.08))
    clip=bpy.context.view_layer.objects.active; clip.name="LL_Note_Clip"
    clip.scale=(0.16,0.06,0.03)
    mat_metal=ll.make_pbr_material("LL_M_NoteMetal", ll.PALETTE["metal_galv"], roughness=0.45, metallic=0.7)
    ll.assign_material(clip, mat_metal); ll.ensure_uvs(clip); pr(clip)

    total=sum(ll.tri_count(o) for o in objs)
    print(f"[NOTES] {total} tris (budget 800)"); ll.print_scene_stats()
    ll.screenshot_trio("LL_Note_Props", look_at=(0.15,0,0.06), lens=55)
    for o in objs: ll.select_only(o); bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ll.export_fbx(objs, "LL_Note_Props.fbx")
    ll.export_fbx([paper], "LL_Note_Paper.fbx")

if __name__=="__main__":
    build_note_props()
    print("[DONE] Note props exported")
