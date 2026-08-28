"""Replace one material with another on every selected object that uses it.

Run it from Blender's Scripting tab: set OLD_NAME and NEW_NAME below, select the
objects, press Run Script. The new material has to exist in the blend file already —
the script never creates one, because a typo would otherwise silently produce an empty
grey material instead of telling you the name was wrong.

Nothing is touched on objects that do not use the old material, so selecting the whole
scene is safe.
"""

import bpy

# ----------------------------------------------------------------------------------
OLD_NAME = "mt_wood_1"
NEW_NAME = "mt_wood_3"
# ----------------------------------------------------------------------------------


def replace(old_name, new_name):
    old = bpy.data.materials.get(old_name)
    new = bpy.data.materials.get(new_name)

    if old is None:
        return "no material called %r in this file" % old_name
    if new is None:
        return "no material called %r in this file" % new_name
    if old is new:
        return "%r and %r are the same material" % (old_name, new_name)

    selected = bpy.context.selected_objects
    if not selected:
        return "nothing selected"

    slots_changed = 0
    objects_changed = []
    # A mesh can be shared by several objects. Replacing a DATA-linked slot changes it
    # for all of them, including ones you did not select, so those get reported.
    shared_meshes = []

    for obj in selected:
        if not hasattr(obj, "material_slots"):
            continue

        touched = False
        for slot in obj.material_slots:
            if slot.material is not old:
                continue

            if slot.link == 'DATA' and obj.data is not None and obj.data.users > 1:
                others = [o.name for o in bpy.data.objects
                          if o.data is obj.data and o is not obj]
                shared_meshes.append((obj.name, obj.data.name, others))

            slot.material = new
            slots_changed += 1
            touched = True

        if touched:
            objects_changed.append(obj.name)

    lines = ["replaced %r with %r" % (old_name, new_name),
             "  %d slot(s) on %d of %d selected object(s)"
             % (slots_changed, len(objects_changed), len(selected))]

    if slots_changed == 0:
        lines.append("  nothing used it — check you selected the right objects")

    if shared_meshes:
        lines.append("  WARNING: %d slot(s) sit on a mesh shared with other objects,"
                     % len(shared_meshes))
        lines.append("           so those changed too:")
        for obj_name, mesh_name, others in shared_meshes[:10]:
            lines.append("             %s (mesh %s) also used by %s"
                         % (obj_name, mesh_name, ", ".join(others[:4])))
        if len(shared_meshes) > 10:
            lines.append("             ... and %d more" % (len(shared_meshes) - 10))

    return "\n".join(lines)


print(replace(OLD_NAME, NEW_NAME))
