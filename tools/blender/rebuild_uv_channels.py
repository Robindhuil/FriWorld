"""Rebuild the UV channels of the selected meshes to exactly two, in Unity's order.

Run it from Blender's Scripting tab: set the config block below, select the objects,
press Run Script.

Why it wipes instead of adding what is missing:

Unity does not read UV channel names. It reads their order — Blender channel 0 becomes
mesh.uv, channel 1 becomes mesh.uv2, and mesh.uv2 is the lightmap UV. A script that only
adds a channel when the name is absent appends it at the end, so a mesh that arrived with
["UVMap", "UVMap.001", "Atlas"] ends up with "Lightmap" at index 3 and Unity bakes into
whatever junk sits at index 1. The name is right and the result is nonsense. Blender has
no way to reorder channels, so the only way to guarantee the order is to remove them all
and create them again.

Channels are addressed by index, never by a held reference. Entering and leaving edit
mode converts Mesh to BMesh and back, which reallocates the custom data layers; a
MeshUVLoopLayer fetched before the switch dangles afterwards and foreach_get on it
crashes Blender outright.

DO_MAIN_UV = False keeps the texture UV: it is read out before the wipe and written back
into channel 0. Tiling walls and floors need that — unwrapping them destroys the tiling.
"""

import bpy
import math

# ----------------------------------------------------------------------------------
ISLAND_MARGIN    = 0.03
LIGHTMAP_MARGIN  = 0.04
ANGLE_LIMIT_DEG  = 66.0
DO_MAIN_UV       = True     # False = tiling walls keep their UV0
MAIN_UV_NAME     = "UVMap"
LIGHTMAP_UV_NAME = "Lightmap"
# ----------------------------------------------------------------------------------


def read_uv(layer):
    buf = [0.0] * (len(layer.data) * 2)
    layer.data.foreach_get("uv", buf)
    return buf


def rebuild_channels(me):
    """Remove every channel, then create two: index 0 = main, index 1 = lightmap."""
    keep = None
    if not DO_MAIN_UV and me.uv_layers:
        # what Blender itself renders with; failing that, whatever sits at index 0
        src = next((l for l in me.uv_layers if l.active_render), me.uv_layers[0])
        keep = read_uv(src)

    while me.uv_layers:
        me.uv_layers.remove(me.uv_layers[0])

    # do_init=False matters: the default copies the active channel into the new one, so a
    # smart_project that quietly fails leaves a copy of the tiling UV in the lightmap
    # channel. That reads as valid and bakes as light smeared across the whole object.
    me.uv_layers.new(name=MAIN_UV_NAME, do_init=False)
    if keep is not None:
        me.uv_layers[0].data.foreach_set("uv", keep)
    me.uv_layers.new(name=LIGHTMAP_UV_NAME, do_init=False)
    me.uv_layers[0].active_render = True


def unwrap_channel(me, index, margin):
    me.uv_layers.active_index = index
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(
        angle_limit=math.radians(ANGLE_LIMIT_DEG),
        island_margin=margin,
        area_weight=0.0,
        correct_aspect=True,
        scale_to_bounds=False,
    )
    bpy.ops.object.mode_set(mode='OBJECT')


def is_sane(me, index):
    """A lightmap UV has to sit inside 0..1 and cover some area."""
    buf = read_uv(me.uv_layers[index])
    if not buf:
        return False
    lo, hi = min(buf), max(buf)
    return hi - lo > 1e-4 and lo > -0.001 and hi < 1.001


orig_selected = list(bpy.context.selected_objects)
orig_active = bpy.context.view_layer.objects.active

targets = [o for o in orig_selected if o.type == 'MESH']
if not targets and orig_active and orig_active.type == 'MESH':
    targets = [orig_active]

if not targets:
    print("Žiadny mesh objekt nie je vybraný.")
else:
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')

    done_meshes = set()      # shared mesh data gets unwrapped once, not once per instance
    skipped, broken, ok = [], [], 0

    for obj in targets:
        me = obj.data
        if me.name in done_meshes:
            continue
        done_meshes.add(me.name)

        if not me.polygons:
            skipped.append(obj.name + " (mesh bez plôch)")
            continue

        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj

        rebuild_channels(me)

        if DO_MAIN_UV:
            unwrap_channel(me, 0, ISLAND_MARGIN)
        unwrap_channel(me, 1, LIGHTMAP_MARGIN)

        me.uv_layers.active_index = 0

        if is_sane(me, 1):
            ok += 1
        else:
            broken.append(obj.name)

    bpy.ops.object.select_all(action='DESELECT')
    for o in orig_selected:
        o.select_set(True)
    bpy.context.view_layer.objects.active = orig_active

    print("Hotovo. Meshov: %d, v poriadku: %d" % (len(done_meshes), ok))
    if skipped:
        print("Preskočené (%d): %s" % (len(skipped), ", ".join(skipped)))
    if broken:
        print("ZLÁ LIGHTMAP UV (%d): %s" % (len(broken), ", ".join(broken)))
