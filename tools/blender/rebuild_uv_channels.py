"""Rebuild the UV channels of the selected meshes to exactly two, in Unity's order.

Run it from Blender's Scripting tab: set the config block below, select the objects,
press Run Script. Start with DRY_RUN = True — it prints every channel it found without
changing anything, which is the only way to see what the older scripts left behind.

Why it wipes instead of adding what is missing:

Unity does not read UV channel names. It reads their order — Blender channel 0 becomes
mesh.uv, channel 1 becomes mesh.uv2, and mesh.uv2 is the lightmap UV. A script that only
adds a channel when the name is absent appends it at the end, so a mesh that arrived with
["UVMap", "UVMap.001", "Atlas"] ends up with "Lightmap" at index 3 and Unity bakes into
whatever junk sits at index 1. The name is right and the result is nonsense. Blender has
no way to reorder channels, so the only way to guarantee the order is to remove them all
and create them again.

DO_MAIN_UV = False keeps the texture UV: it is read out before the wipe and written back
into channel 0. Tiling walls and floors need that — unwrapping them destroys the tiling.
"""

import bpy
import math

# ----------------------------------------------------------------------------------
DRY_RUN          = True     # report only, change nothing. Run this first.

DO_MAIN_UV       = False    # True re-unwraps channel 0 and DESTROYS tiling UVs
DO_LIGHTMAP_UV   = True

ISLAND_MARGIN    = 0.03     # anti-bleed margin, as a fraction of the 0..1 map
LIGHTMAP_MARGIN  = 0.04
ANGLE_LIMIT_DEG  = 66.0

MAIN_UV_NAME     = "UVMap"
LIGHTMAP_UV_NAME = "Lightmap"
# ----------------------------------------------------------------------------------


def read_uv(layer):
    buf = [0.0] * (len(layer.data) * 2)
    layer.data.foreach_get("uv", buf)
    return buf


def write_uv(layer, buf):
    layer.data.foreach_set("uv", buf)


def describe(me):
    """One line per existing channel, so the wipe leaves a record of what was there."""
    out = []
    for i, layer in enumerate(me.uv_layers):
        marks = []
        if layer.active_render:
            marks.append("active_render")
        if me.uv_layers.active == layer:
            marks.append("active")
        out.append("[%d] %s%s" % (i, layer.name, (" <" + ",".join(marks) + ">") if marks else ""))
    return ", ".join(out) if out else "(žiadne)"


def pick_texture_uv(me):
    """Which of the existing channels is the real texture UV.

    active_render is what Blender itself renders with; index 0 is what Unity already
    reads as mesh.uv. When those two disagree the file is already inconsistent between
    the two programs, so the caller gets told about it.
    """
    if not me.uv_layers:
        return None, False
    rendered = next((l for l in me.uv_layers if l.active_render), None)
    if rendered is None:
        return me.uv_layers[0], False
    return rendered, rendered != me.uv_layers[0]


def rebuild_channels(me):
    """Exactly two channels: 0 = main, 1 = lightmap. Returns (main, lightmap, removed)."""
    keep = None
    if not DO_MAIN_UV:
        src, _ = pick_texture_uv(me)
        if src is not None:
            keep = read_uv(src)

    removed = [l.name for l in me.uv_layers]
    while me.uv_layers:
        me.uv_layers.remove(me.uv_layers[0])

    # do_init=False matters: the default copies the active channel into the new one, so a
    # smart_project that quietly fails leaves a copy of the tiling UV in the lightmap
    # channel. That reads as valid and bakes as light smeared across the whole object.
    main = me.uv_layers.new(name=MAIN_UV_NAME, do_init=False)
    if keep is not None:
        write_uv(main, keep)
    lightmap = me.uv_layers.new(name=LIGHTMAP_UV_NAME, do_init=False)
    main.active_render = True
    return main, lightmap, removed


def smart_project(margin):
    bpy.ops.uv.smart_project(
        angle_limit=math.radians(ANGLE_LIMIT_DEG),
        island_margin=margin,
        area_weight=0.0,
        correct_aspect=True,
        scale_to_bounds=False,
    )


def unwrap_into(obj, layer, margin):
    obj.data.uv_layers.active = layer
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    smart_project(margin)
    bpy.ops.object.mode_set(mode='OBJECT')


def is_sane(layer):
    """A lightmap UV has to sit inside 0..1 and cover some area."""
    buf = read_uv(layer)
    if not buf:
        return False
    lo, hi = min(buf), max(buf)
    return hi - lo > 1e-4 and lo > -0.001 and hi < 1.001


def main():
    orig_selected = list(bpy.context.selected_objects)
    orig_active = bpy.context.view_layer.objects.active

    targets = [o for o in orig_selected if o.type == 'MESH']
    if not targets and orig_active and orig_active.type == 'MESH':
        targets = [orig_active]
    if not targets:
        print("Žiadny mesh objekt nie je vybraný.")
        return

    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')

    seen = set()            # shared mesh data gets unwrapped once, not once per instance
    channel_counts = {}
    disagreed, skipped, broken, ok = [], [], [], 0

    for obj in targets:
        me = obj.data
        if me.name in seen:
            continue
        seen.add(me.name)

        count = len(me.uv_layers)
        channel_counts[count] = channel_counts.get(count, 0) + 1

        if DRY_RUN:
            print("%-40s %s" % (obj.name, describe(me)))
            _, mismatch = pick_texture_uv(me)
            if mismatch:
                disagreed.append(obj.name)
            continue

        if not me.polygons:
            skipped.append(obj.name + " (mesh bez plôch)")
            continue

        _, mismatch = pick_texture_uv(me)
        if mismatch:
            disagreed.append(obj.name)

        was_hidden, was_hidden_vp = obj.hide_get(), obj.hide_viewport
        obj.hide_viewport = False
        obj.hide_set(False)

        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj

        main_uv, lightmap, removed = rebuild_channels(me)
        if len(removed) > 2:
            print("%-40s zmazané: %s" % (obj.name, ", ".join(removed)))

        if DO_MAIN_UV:
            unwrap_into(obj, main_uv, ISLAND_MARGIN)
        if DO_LIGHTMAP_UV:
            unwrap_into(obj, lightmap, max(ISLAND_MARGIN, LIGHTMAP_MARGIN))

        me.uv_layers.active = main_uv

        if not DO_LIGHTMAP_UV or is_sane(lightmap):
            ok += 1
        else:
            broken.append(obj.name)

        obj.hide_viewport = was_hidden_vp
        obj.hide_set(was_hidden)

    if not DRY_RUN:
        bpy.ops.object.select_all(action='DESELECT')
        for o in orig_selected:
            o.select_set(True)
        bpy.context.view_layer.objects.active = orig_active

    print("")
    print("Meshov: %d (z %d vybraných objektov)" % (len(seen), len(targets)))
    for count in sorted(channel_counts):
        print("   %d UV kanálov: %d meshov%s"
              % (count, channel_counts[count], "   <-- Unity číta ako lightmapu kanál 1" if count > 2 else ""))
    if disagreed:
        print("active_render NIE JE kanál 0 (%d): %s"
              % (len(disagreed), ", ".join(disagreed[:10])))
        print("   Blender renderuje iným kanálom, než ktorý Unity berie ako mesh.uv.")
    if DRY_RUN:
        print("DRY_RUN — nič sa nezmenilo. Prepni na False a spusti znova.")
        return
    print("Lightmap UV v poriadku: %d" % ok)
    if skipped:
        print("Preskočené (%d): %s" % (len(skipped), ", ".join(skipped)))
    if broken:
        print("ZLÁ LIGHTMAP UV (%d): %s" % (len(broken), ", ".join(broken)))


main()
