"""Strip every material slot from the selected objects.

Run it from Blender's Scripting tab: select the objects, press Run Script. Leave
DRY_RUN on for the first pass — it prints exactly what would go and touches nothing.

Slots are removed, not just emptied. Setting a slot to None leaves an empty slot behind,
and an object exported to Unity with an empty slot still arrives carrying a renderer
material entry, which is the opposite of the point.

The materials themselves stay in the blend file. They lose a user, so they disappear on
the next save-and-reopen if nothing else holds them; keep one with a fake user (the shield
button in the material properties) if you want it to survive.
"""

import bpy

# ----------------------------------------------------------------------------------
DRY_RUN = True

# A mesh can be shared by several objects. Its slots are usually DATA-linked, so removing
# them strips the material from every object using that mesh, including ones you did not
# select. Left False, those objects are reported and skipped instead.
INCLUDE_SHARED_MESHES = False
# ----------------------------------------------------------------------------------


def clear_materials(dry_run, include_shared):
    selected = bpy.context.selected_objects
    if not selected:
        return "nothing selected"

    cleared = []      # (object name, slots removed)
    skipped_shared = []
    without_slots = 0

    for obj in selected:
        if not hasattr(obj, "material_slots"):
            continue

        count = len(obj.material_slots)
        if count == 0:
            without_slots += 1
            continue

        data = obj.data
        shared = data is not None and data.users > 1
        if shared and not include_shared:
            others = [o.name for o in bpy.data.objects if o.data is data and o is not obj]
            skipped_shared.append((obj.name, data.name, others))
            continue

        if not dry_run:
            # Clearing the data array drops the slots with it. Object-linked slots have no
            # entry there, so they are emptied first and then removed by the same clear.
            for slot in obj.material_slots:
                if slot.link == 'OBJECT':
                    slot.material = None
            if data is not None and hasattr(data, "materials"):
                data.materials.clear()
            else:
                # Objects whose data carries no material array — the operator is the only
                # way in, and it needs the object to be the active one.
                view = bpy.context.view_layer
                previous = view.objects.active
                view.objects.active = obj
                while obj.material_slots:
                    bpy.ops.object.material_slot_remove()
                view.objects.active = previous

        cleared.append((obj.name, count))

    slots = sum(c for _, c in cleared)
    head = "would remove" if dry_run else "removed"
    lines = ["%s %d slot(s) from %d of %d selected object(s)"
             % (head, slots, len(cleared), len(selected))]

    for name, count in cleared[:20]:
        lines.append("  %s — %d slot(s)" % (name, count))
    if len(cleared) > 20:
        lines.append("  ... and %d more" % (len(cleared) - 20))

    if without_slots:
        lines.append("  %d selected object(s) had no slots to begin with" % without_slots)

    if skipped_shared:
        lines.append("  SKIPPED: %d object(s) sit on a mesh shared with objects you did not"
                     % len(skipped_shared))
        lines.append("           select, so stripping them would strip those too:")
        for obj_name, mesh_name, others in skipped_shared[:10]:
            lines.append("             %s (mesh %s) also used by %s"
                         % (obj_name, mesh_name, ", ".join(others[:4])))
        if len(skipped_shared) > 10:
            lines.append("             ... and %d more" % (len(skipped_shared) - 10))
        lines.append("           Set INCLUDE_SHARED_MESHES = True if that is what you want.")

    if dry_run:
        lines.append("  DRY_RUN is on — nothing was changed. Set DRY_RUN = False to apply.")

    return "\n".join(lines)


print(clear_materials(DRY_RUN, INCLUDE_SHARED_MESHES))
