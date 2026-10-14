#!/usr/bin/env blender
# Re-render review screenshots for Lighthouse + House with tighter framing
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__)))
import ll_pipeline as ll
import bpy, math, mathutils

def rerender_lighthouse():
    ll.clear_scene(); ll.set_scene_units()
    # Rebuild quickly — import the FBX back
    fbx = os.path.join(ll.GENERATED_DIR, "LL_Lighthouse_Tower.fbx")
    bpy.ops.import_scene.fbx(filepath=fbx)
    # Fix materials after import — lighten issue is EEVEE exposure
    bpy.context.scene.view_settings.view_transform = 'Standard'
    ll.ensure_dirs()
    setup = ll.setup_preview_lighting
    setup()
    # Tweak world to darker overcast so white tower pops
    w = bpy.context.scene.world
    bg = w.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.32, 0.36, 0.45, 1.0)
        bg.inputs[1].default_value = 0.9
    # Boost sun
    sun = bpy.data.objects.get("PreviewSun")
    if sun: sun.data.energy = 8.0
    # Tighter framing — lighthouse is 20m tall, need closer camera
    views = [
        ("LL_Lighthouse_Tower_01_front.png", (0, -14, 9), (0,0,9), 35),
        ("LL_Lighthouse_Tower_02_three_quarter.png", (10, -10, 10), (0,0,9), 35),
        ("LL_Lighthouse_Tower_03_top.png", (4, -4, 22), (0,0,9), 32),
    ]
    for fname, loc, look, lens in views:
        ll.set_camera_view(loc, look_at=look, lens=lens)
        ll.render_screenshot(fname, samples=64)
        print(f"[RERENDER] {fname}")

def rerender_house():
    ll.clear_scene(); ll.set_scene_units()
    fbx = os.path.join(ll.GENERATED_DIR, "LL_Keepers_House.fbx")
    bpy.ops.import_scene.fbx(filepath=fbx)
    bpy.context.scene.view_settings.view_transform = 'Standard'
    ll.setup_preview_lighting()
    w = bpy.context.scene.world
    bg = w.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.32, 0.36, 0.45, 1.0)
        bg.inputs[1].default_value = 0.9
    sun = bpy.data.objects.get("PreviewSun")
    if sun: sun.data.energy = 8.0
    views = [
        ("LL_Keepers_House_01_front.png", (0, -10, 2.2), (0,0,1.6), 38),
        ("LL_Keepers_House_02_three_quarter.png", (7, -7, 3.0), (0,0,1.6), 38),
        ("LL_Keepers_House_03_top.png", (3, -3, 9), (0,0,1.6), 35),
    ]
    for fname, loc, look, lens in views:
        ll.set_camera_view(loc, look_at=look, lens=lens)
        ll.render_screenshot(fname, samples=64)
        print(f"[RERENDER] {fname}")

def rerender_generator():
    ll.clear_scene(); ll.set_scene_units()
    fbx = os.path.join(ll.GENERATED_DIR, "LL_Generator.fbx")
    bpy.ops.import_scene.fbx(filepath=fbx)
    bpy.context.scene.view_settings.view_transform = 'Standard'
    ll.setup_preview_lighting()
    w = bpy.context.scene.world
    bg = w.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.32, 0.36, 0.45, 1.0)
        bg.inputs[1].default_value = 0.9
    sun = bpy.data.objects.get("PreviewSun")
    if sun: sun.data.energy = 8.0
    views = [
        ("LL_Generator_01_front.png", (0, -2.2, 0.65), (0,0,0.55), 45),
        ("LL_Generator_02_three_quarter.png", (1.4, -1.4, 0.85), (0,0,0.55), 45),
        ("LL_Generator_03_top.png", (0.6, -0.6, 2.2), (0,0,0.55), 40),
    ]
    for fname, loc, look, lens in views:
        ll.set_camera_view(loc, look_at=look, lens=lens)
        ll.render_screenshot(fname, samples=64)
        print(f"[RERENDER] {fname}")

if __name__=="__main__":
    rerender_lighthouse()
    rerender_house()
    rerender_generator()
    print("[DONE] Re-renders")
