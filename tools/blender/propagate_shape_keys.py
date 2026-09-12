"""Copy every shape key from the face onto everything that sits on it.

Run it from Blender's Scripting tab: press Run Script. It takes the whole set, so there
is nothing to edit before a run — KEYS only exists to narrow a re-run to what changed.

The face is one mesh, but the brows, the beards, the freckles, the eyes and the scalp
are separate objects because they are swappable presets. A shape key that widens the jaw
or the cheek therefore only moves the face, and the overlays stay where they were — the
beard lifts off the chin, the brows float over the ridge. This copies the same
deformation onto them, so they follow.

For every vertex of a target it finds the nearest triangle on the face's undeformed
surface, reads how far that spot moves under each key, and shifts the vertex by the same
amount. The offset from the skin is preserved, which is what keeps a shell sitting
0.8 mm above the face still 0.8 mm above it afterwards. That spot is looked up once per
vertex and reused for every key — with twenty keys it is the difference between one BVH
query per vertex and twenty.

It is safe to re-run: an existing key of the same name on a target is removed first, so
this is the step to repeat after every edit of a key on the face.
"""

import bpy
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

# ----------------------------------------------------------------------------------
SOURCE = "face_1"

# Empty means every key on the face. Name a few to narrow a re-run to what changed.
KEYS = []

TARGETS = [
    "cranium_1",
    "brow_1", "brow_2", "brow_3", "brow_4",
    "beard_1", "beard_2", "beard_3", "beard_4", "beard_none_1",
    "eye_brown_1", "eye_blue_1", "eye_green_1", "eye_hazel_1",
    "freckle_1", "freckle_2", "freckle_3", "freckle_4", "freckle_none_1",
    "male_body_neck",
]

# The sliders on the targets get the same range as the ones on the face.
SLIDER_MIN, SLIDER_MAX = -3.0, 3.0
# ----------------------------------------------------------------------------------


def propagate(source_name, target_names, key_names=()):
    src = bpy.data.objects.get(source_name)
    if src is None:
        return ["no object called %r" % source_name]
    if src.data.shape_keys is None:
        return ["%r has no shape keys" % source_name]

    sme = src.data
    smw = src.matrix_world
    blocks = sme.shape_keys.key_blocks
    basis = blocks["Basis"]

    keys = [k for k in blocks if k.name != "Basis"]
    if key_names:
        unknown = [n for n in key_names if n not in blocks]
        if unknown:
            return ["%r has no key called %r" % (source_name, n) for n in unknown]
        keys = [k for k in keys if k.name in list(key_names)]
    if not keys:
        return ["%r has nothing to propagate" % source_name]

    sme.calc_loop_triangles()
    tris = [tuple(t.vertices) for t in sme.loop_triangles]
    base_pos = [smw @ basis.data[i].co for i in range(len(sme.vertices))]
    bvh = BVHTree.FromPolygons(base_pos, [list(t) for t in tris])
    key_pos = {k.name: [smw @ k.data[i].co for i in range(len(sme.vertices))] for k in keys}

    lines = []
    for name in target_names:
        ob = bpy.data.objects.get(name)
        if ob is None:
            lines.append("%-16s skipped, not in this file" % name)
            continue

        me = ob.data
        if me.shape_keys is None:
            ob.shape_key_add(name="Basis", from_mix=False)
        target_basis = me.shape_keys.key_blocks["Basis"]
        mw = ob.matrix_world
        mwi = mw.inverted()

        # where each vertex sits on the face, found once and reused for every key
        spots = []
        for i in range(len(me.vertices)):
            world = mw @ target_basis.data[i].co
            hit, _, tri_index, _ = bvh.find_nearest(world)
            if hit is not None:
                spots.append((i, world, hit, tris[tri_index]))

        touched, largest = 0, 0.0
        for key in keys:
            for block in list(me.shape_keys.key_blocks):
                if block.name == key.name:
                    ob.shape_key_remove(block)
            target = ob.shape_key_add(name=key.name, from_mix=False)
            target.slider_min, target.slider_max = SLIDER_MIN, SLIDER_MAX

            kp = key_pos[key.name]
            moved, biggest = 0, 0.0
            for i, world, hit, (a, b, c) in spots:
                delta = barycentric_transform(hit, base_pos[a], base_pos[b], base_pos[c],
                                              kp[a], kp[b], kp[c]) - hit
                if delta.length < 1e-6:
                    continue
                target.data[i].co = mwi @ (world + delta)
                moved += 1
                biggest = max(biggest, delta.length)
            if moved:
                touched += 1
                largest = max(largest, biggest)

        lines.append("%-16s %3d verts, %2d of %2d keys move it, up to %5.2f mm"
                     % (name, len(me.vertices), touched, len(keys), largest * 1000))
    return lines


for line in propagate(SOURCE, TARGETS, KEYS):
    print(line)
