"""Copy a shape key from the face onto everything that sits on it.

Run it from Blender's Scripting tab: set KEY below, press Run Script.

The face is one mesh, but the brows, the beard, the eyes and the scalp are separate
objects because they are swappable presets. A shape key that widens the nose or the jaw
therefore only moves the face, and the overlays stay where they were — the beard lifts
off the chin, the brows float over the ridge. This copies the same deformation onto
them, so they follow.

For every vertex of a target it finds the nearest triangle on the face's undeformed
surface, reads how far that spot moves under the key, and shifts the vertex by the same
amount. The offset from the skin is preserved, which is what keeps a shell sitting
1.5 mm above the face still 1.5 mm above it afterwards.

It is safe to re-run: an existing key of the same name on a target is removed first, so
this is the step to repeat after every edit of the key on the face.
"""

import bpy
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

# ----------------------------------------------------------------------------------
KEY = "nose_wide"
SOURCE = "face_1"
TARGETS = [
    "cranium_1",
    "brow_1", "brow_2",
    "beard_1", "beard_2", "beard_none_1",
    "eye_brown_1", "eye_blue_1", "eye_green_1", "eye_hazel_1",
    "male_body_neck",
]
# ----------------------------------------------------------------------------------


def propagate(key_name, source_name, target_names):
    src = bpy.data.objects.get(source_name)
    if src is None:
        return ["no object called %r" % source_name]
    if src.data.shape_keys is None or key_name not in src.data.shape_keys.key_blocks:
        return ["%r has no shape key called %r" % (source_name, key_name)]

    sme = src.data
    smw = src.matrix_world
    basis = sme.shape_keys.key_blocks["Basis"]
    key = sme.shape_keys.key_blocks[key_name]

    sme.calc_loop_triangles()
    tris = [tuple(t.vertices) for t in sme.loop_triangles]
    base_pos = [smw @ basis.data[i].co for i in range(len(sme.vertices))]
    key_pos = [smw @ key.data[i].co for i in range(len(sme.vertices))]
    bvh = BVHTree.FromPolygons(base_pos, [list(t) for t in tris])

    lines = []
    for name in target_names:
        ob = bpy.data.objects.get(name)
        if ob is None:
            lines.append("%-14s skipped, not in this file" % name)
            continue

        me = ob.data
        if me.shape_keys is None:
            ob.shape_key_add(name="Basis", from_mix=False)
        for block in list(me.shape_keys.key_blocks):
            if block.name == key_name:
                ob.shape_key_remove(block)

        target = ob.shape_key_add(name=key_name, from_mix=False)
        target.slider_min, target.slider_max = -1.0, 1.0
        target_basis = me.shape_keys.key_blocks["Basis"]

        mw = ob.matrix_world
        mwi = mw.inverted()
        moved = 0
        largest = 0.0

        for i in range(len(me.vertices)):
            world = mw @ target_basis.data[i].co
            hit, _, tri_index, _ = bvh.find_nearest(world)
            if hit is None:
                continue
            a, b, c = tris[tri_index]
            delta = barycentric_transform(hit, base_pos[a], base_pos[b], base_pos[c],
                                          key_pos[a], key_pos[b], key_pos[c]) - hit
            if delta.length < 1e-6:
                continue
            target.data[i].co = mwi @ (world + delta)
            moved += 1
            largest = max(largest, delta.length)

        lines.append("%-14s %3d of %3d vertices moved, largest %.2f mm"
                     % (name, moved, len(me.vertices), largest * 1000))
    return lines


for line in propagate(KEY, SOURCE, TARGETS):
    print(line)
