"""Export the character to Unity as FBX, with only the bones that deform anything.

Run it from Blender's Scripting tab: press Run Script. It writes
Assets/3Dmodels/Npc/character_male.fbx and prints what went in.

The .blend itself lives one folder over, in Assets/3Dmodels/Npc~ — Unity ignores a folder
whose name ends in a tilde, so the working file stays in the project without being
imported twice alongside the .fbx it produces.

Why not let Unity read the .blend: Unity converts it with its own Unity-BlenderToFBX.py,
which calls the FBX exporter with a fixed argument list — no use_armature_deform_only and
no add_leaf_bones. The whole Rigify rig goes in, so every mesh ends up carrying 404 bind
poses: the brow, weighted to a single bone, hauls 404 matrices per frame. Of those 404
bones exactly 71 deform anything.

What goes in: the armature and every mesh bound to it. The female base, the control-shape
widgets and the hair curves stay out.
"""

import os

import bpy

# ----------------------------------------------------------------------------------
ARMATURE = "RIG-skeleton_male"
OUTPUT = "//../Npc/character_male.fbx"    # the .blend lives in Npc~, which Unity ignores
#                                         and the .fbx has to land in Npc, which it reads
# ----------------------------------------------------------------------------------


def collect(armature_name):
    rig = bpy.data.objects.get(armature_name)
    if rig is None:
        return None, [], ["no armature called %r" % armature_name]

    meshes, problems = [], []
    for ob in bpy.data.objects:
        if ob.type != 'MESH':
            continue
        if not any(m.type == 'ARMATURE' and m.object is rig for m in ob.modifiers):
            continue
        meshes.append(ob)

        # The exporter applies modifiers, and a mesh with shape keys cannot have them
        # applied — the keys would be dropped on the way out, silently.
        extra = [m.type for m in ob.modifiers if m.type != 'ARMATURE']
        if extra and ob.data.shape_keys:
            problems.append("%s carries %s and %d shape keys; the keys would not survive"
                            % (ob.name, ", ".join(extra), len(ob.data.shape_keys.key_blocks) - 1))

    return rig, sorted(meshes, key=lambda o: o.name), problems


def export(armature_name, output):
    rig, meshes, problems = collect(armature_name)
    if rig is None:
        return problems

    if problems:
        return problems + ["nothing exported"]

    if bpy.context.object is not None and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')

    selected = [o for o in bpy.context.selected_objects]
    active = bpy.context.view_layer.objects.active

    # Only one preset of each class is ever visible, and a hidden object cannot be selected
    # — nor does it reach the exporter at all when its collection is excluded. So the whole
    # export set is unhidden for the duration and put back exactly as it was.
    wanted = meshes + [rig]
    hidden = [(ob, ob.hide_get(), ob.hide_viewport) for ob in wanted]
    excluded = []

    def walk(layer):
        yield layer
        for child in layer.children:
            yield from walk(child)

    collections = {c for ob in wanted for c in ob.users_collection}
    for layer in walk(bpy.context.view_layer.layer_collection):
        if layer.collection in collections and (layer.exclude or layer.hide_viewport):
            excluded.append((layer, layer.exclude, layer.hide_viewport))
            layer.exclude = False
            layer.hide_viewport = False

    for ob, _, _ in hidden:
        ob.hide_viewport = False
        ob.hide_set(False)

    bpy.ops.object.select_all(action='DESELECT')
    for ob in wanted:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = rig

    path = bpy.path.abspath(output)
    bpy.ops.export_scene.fbx(
        filepath=path,
        check_existing=False,

        # exactly what is selected, nothing the scene happens to contain
        use_selection=True,
        use_visible=False,
        use_active_collection=False,
        object_types={'ARMATURE', 'MESH'},

        # Unity's own axis and scale convention, so the import needs no correction
        axis_forward='-Z',
        axis_up='Y',
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        global_scale=1.0,
        bake_space_transform=False,

        # Solidify on the garments has to be applied; the head meshes carry only Armature
        use_mesh_modifiers=True,
        mesh_smooth_type='OFF',          # the shading rides on custom normals, not on flags
        use_subsurf=False,
        use_mesh_edges=False,
        use_tspace=False,
        use_triangles=False,
        colors_type='NONE',
        prioritize_active_color=False,

        # the point of the whole file: 71 bones instead of 404, and no leaf bones on top
        use_armature_deform_only=True,
        add_leaf_bones=False,
        primary_bone_axis='Y',
        secondary_bone_axis='X',
        armature_nodetype='NULL',

        # there is no animation in this file, and baking one for 400 bones is pure weight
        bake_anim=False,

        use_custom_props=False,
        path_mode='AUTO',
        embed_textures=False,
        batch_mode='OFF',
    )

    bpy.ops.object.select_all(action='DESELECT')
    for ob, was_hidden, was_hidden_viewport in hidden:
        ob.hide_viewport = was_hidden_viewport
        ob.hide_set(was_hidden)
    for layer, was_excluded, was_hidden_viewport in excluded:
        layer.exclude = was_excluded
        layer.hide_viewport = was_hidden_viewport
    for ob in selected:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = active

    deform = [b.name for b in rig.data.bones if b.use_deform]
    keys = sum(1 for ob in meshes if ob.data.shape_keys)
    size = os.path.getsize(path) / 1e6 if os.path.exists(path) else 0.0

    return ["wrote %s  (%.1f MB)" % (os.path.basename(path), size),
            "%d meshes, %d of them with shape keys" % (len(meshes), keys),
            "%d bones of %d exported — the ones that deform something"
            % (len(deform), len(rig.data.bones))]


for line in export(ARMATURE, OUTPUT):
    print(line)
