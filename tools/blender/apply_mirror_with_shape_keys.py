"""Bake the Mirror modifier into the mesh and into every shape key.

Run it from Blender's Scripting tab: press Run Script.

Unity converts the .blend with use_mesh_modifiers=True, so a modifier that rewrites the
mesh is applied at export — and blend shapes do not survive that. Only ARMATURE may stay
on a mesh that carries keys; the reason is in
docs/decisions/2026-09-11-blend-shapes-a-modifiery.md.

Blender refuses to apply a modifier to a mesh that has shape keys, so the mirror is built
by hand instead: every vertex gets a twin at -x, every polygon a reversed twin, and every
key its own mirrored half. A vertex closer to the plane than the merge threshold is shared
rather than doubled, which is what the modifier does too.

The result is checked against what the modifier really produces: each object is evaluated
through the depsgraph with only the mirror enabled, and an object whose vertex count would
not match is skipped untouched rather than written wrong.

This replaces the mesh datablock, so undo is not to be relied on. The file must be saved
before the run; look at the model afterwards, then save again.
"""

import bpy
from mathutils import Vector

# ----------------------------------------------------------------------------------
# Empty means every mesh object that has a Mirror modifier.
OBJECTS = []

# Sliders on the rebuilt keys, matching the ones the face carries.
SLIDER_MIN, SLIDER_MAX = -3.0, 3.0

# A mirrored vertex group follows the side naming, as the modifier does.
SIDE_FLIP = {".L": ".R", ".R": ".L", "_L": "_R", "_R": "_L"}
# ----------------------------------------------------------------------------------


def flipped_group_name(name):
    for suffix, other in SIDE_FLIP.items():
        if name.endswith(suffix):
            return name[: -len(suffix)] + other
    return name


def evaluated_vertex_count(ob):
    """What the stack really produces, with everything but the mirror switched off."""
    states = [(m, m.show_viewport) for m in ob.modifiers]
    try:
        for m, _ in states:
            m.show_viewport = m.type == 'MIRROR'
        deps = bpy.context.evaluated_depsgraph_get()
        evaluated = ob.evaluated_get(deps)
        me = evaluated.to_mesh()
        count = len(me.vertices)
        evaluated.to_mesh_clear()
    finally:
        for m, state in states:
            m.show_viewport = state
    return count


def mirrored(co):
    return (-co[0], co[1], co[2])


def bake_mirror(ob):
    mirror = next((m for m in ob.modifiers if m.type == 'MIRROR'), None)
    if mirror is None:
        return "%-16s no mirror, left alone" % ob.name
    if list(mirror.use_axis) != [True, False, False]:
        return "%-16s SKIPPED, mirrors on %s and this only does X" % (
            ob.name, [a for a, on in zip("XYZ", mirror.use_axis) if on])
    if any(mirror.use_bisect_axis) or mirror.mirror_object is not None:
        return "%-16s SKIPPED, uses bisect or a mirror object" % ob.name

    old = ob.data
    n = len(old.vertices)
    threshold = mirror.merge_threshold if mirror.use_mirror_merge else 0.0

    base = [v.co.copy() for v in old.vertices]
    twin, extra = [0] * n, []
    for i, co in enumerate(base):
        # the modifier measures the gap between a vertex and its own image, which is 2x,
        # so a vertex 0.9 mm off the plane is still doubled at a 1 mm threshold
        if abs(co.x) * 2.0 <= threshold:
            twin[i] = i                       # sits on the plane, the halves share it
        else:
            twin[i] = n + len(extra)
            extra.append(i)

    verts = [tuple(co) for co in base] + [mirrored(base[i]) for i in extra]
    src_polys = [list(p.vertices) for p in old.polygons]
    polys = src_polys + [[twin[i] for i in reversed(p)] for p in src_polys]

    expected = evaluated_vertex_count(ob)
    if len(verts) != expected:
        return "%-16s SKIPPED, this would make %d verts, the modifier makes %d" % (
            ob.name, len(verts), expected)

    # everything is read off the old mesh before the swap
    keys = []
    if old.shape_keys:
        for kb in old.shape_keys.key_blocks:
            co = [kb.data[i].co.copy() for i in range(n)]
            keys.append((kb.name, [tuple(c) for c in co] + [mirrored(co[i]) for i in extra]))

    uvs = []
    for layer in old.uv_layers:
        uvs.append((layer.name, [tuple(d.uv) for d in layer.data]))

    custom_normals = None
    if old.has_custom_normals:
        custom_normals = [old.loops[li].normal.copy()
                          for p in old.polygons for li in p.loop_indices]

    group_names = [g.name for g in ob.vertex_groups]
    weights = {}
    for v in old.vertices:
        for g in v.groups:
            weights.setdefault(group_names[g.group], []).append((v.index, g.weight))

    # edge flags travel with the pair of vertices they join, twin included
    sharp, seams = set(), set()
    for e in old.edges:
        a, b = e.vertices
        if e.use_edge_sharp:
            sharp.add(tuple(sorted((a, b))))
            sharp.add(tuple(sorted((twin[a], twin[b]))))
        if e.use_seam:
            seams.add(tuple(sorted((a, b))))
            seams.add(tuple(sorted((twin[a], twin[b]))))

    # build
    new = bpy.data.meshes.new(old.name)
    new.from_pydata(verts, [], polys)
    new.update()

    for m in old.materials:
        new.materials.append(m)
    count = len(src_polys)
    for i, p in enumerate(old.polygons):
        for target in (new.polygons[i], new.polygons[count + i]):
            target.material_index = p.material_index
            target.use_smooth = p.use_smooth

    if sharp or seams:
        for e in new.edges:
            pair = tuple(sorted(e.vertices))
            if pair in sharp:
                e.use_edge_sharp = True
            if pair in seams:
                e.use_seam = True

    loop_of = [list(p.loop_indices) for p in old.polygons]
    for name, values in uvs:
        layer = new.uv_layers.get(name) or new.uv_layers.new(name=name)
        for i in range(count):
            src = loop_of[i]
            for k, li in enumerate(new.polygons[i].loop_indices):
                layer.data[li].uv = values[src[k]]
            # the twin polygon runs the other way round, so its loops do too
            for k, li in enumerate(new.polygons[count + i].loop_indices):
                layer.data[li].uv = values[src[len(src) - 1 - k]]

    normals = None
    if custom_normals is not None:
        normals = [None] * len(new.loops)
        for i in range(count):
            src = loop_of[i]
            for k, li in enumerate(new.polygons[i].loop_indices):
                normals[li] = custom_normals[src[k]]
            for k, li in enumerate(new.polygons[count + i].loop_indices):
                nrm = custom_normals[src[len(src) - 1 - k]]
                normals[li] = (-nrm.x, nrm.y, nrm.z)
        # a vertex the two halves share carries both its own normal and the mirror of it,
        # and their average lies in the plane — the modifier does the same, and without it
        # there is a crease down the middle of the face
        for li, nrm in enumerate(normals):
            vi = new.loops[li].vertex_index
            if vi < n and twin[vi] == vi:
                flat = Vector((0.0, nrm[1], nrm[2]))
                normals[li] = flat.normalized() if flat.length > 1e-9 else Vector((0.0, 0.0, 1.0))

    ob.data = new

    for name, coords in keys:
        kb = ob.shape_key_add(name=name, from_mix=False)
        kb.slider_min, kb.slider_max = SLIDER_MIN, SLIDER_MAX
        kb.value = 0.0
        for i, co in enumerate(coords):
            kb.data[i].co = co

    ob.vertex_groups.clear()
    groups = {}
    for name in group_names:
        groups[name] = ob.vertex_groups.new(name=name)
    for name in group_names:
        other = flipped_group_name(name)
        if other not in groups:
            groups[other] = ob.vertex_groups.new(name=other)
    for name, pairs in weights.items():
        for i, w in pairs:
            groups[name].add([i], w, 'REPLACE')
            if twin[i] != i:
                groups[flipped_group_name(name)].add([twin[i]], w, 'REPLACE')

    if normals is not None:
        ob.data.normals_split_custom_set(normals)

    ob.modifiers.remove(mirror)
    if old.users == 0:
        bpy.data.meshes.remove(old)

    return "%-16s %4d -> %4d verts, %3d -> %3d polys, %2d keys kept, mirror removed" % (
        ob.name, n, len(ob.data.vertices), count, len(ob.data.polygons),
        max(0, len(keys) - 1))


def run(names):
    if not bpy.data.filepath:
        return ["save the file first — this replaces mesh datablocks"]
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')

    targets = []
    for name in names:
        ob = bpy.data.objects.get(name)
        if ob is None:
            targets.append(name)
        else:
            targets.append(ob)
    if not names:
        targets = [o for o in bpy.data.objects
                   if o.type == 'MESH' and any(m.type == 'MIRROR' for m in o.modifiers)]

    lines = []
    for ob in targets:
        if isinstance(ob, str):
            lines.append("%-16s not in this file" % ob)
        else:
            lines.append(bake_mirror(ob))
    return lines


for line in run(OBJECTS):
    print(line)
