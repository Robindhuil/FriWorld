using System.Collections.Generic;
using System.Text;
using FriWorld.ObjectRegistry;

namespace FriWorld.FeatureFlags
{
    /// <summary>
    /// Formats what the two appliers found. Short on purpose: the moment this becomes a
    /// thousand-line dump it gets skipped, and then nothing is being caught at all.
    /// </summary>
    public static class RoomGateReport
    {
        public const int MaxListed = 20;

        public static string Build(ObjectsPlatformGates.Result objects,
                                   DoorComponentGates.Result doors,
                                   RoomPlatforms platforms)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Objects — PlatformGate on room containers");
            sb.AppendLine("    " + objects.added + " to add, " + objects.retargeted + " to retarget, "
                        + objects.removed + " to remove, " + objects.unchanged + " already correct");
            sb.AppendLine("fri_building — ComponentGate on doors");
            sb.AppendLine("    " + doors.added + " to add, " + doors.reconfigured + " to reconfigure, "
                        + doors.removed + " to remove, " + doors.unchanged + " already correct");

            Section(sb, "UNDECIDED areas (no platform in RoomPlatforms.json — left untouched)",
                    Union(objects.undecided, doors.undecided));
            Section(sb, "BAD VALUES (not all / desktopOnly / webOnly)", objects.badValues);
            Section(sb, "NESTED (contains another area — gating it would void the inner decisions)",
                    objects.nested);
            Section(sb, "NO EFFECT (desktopOnly, but the area has neither furniture nor doors)",
                    NoEffect(objects, doors, platforms));
            Section(sb, "ORPHAN gates (PlatformGate on something that is not an area — left alone)",
                    objects.orphanGates);
            Section(sb, "DOORS with nothing to strip (Door behaviour was never attached)",
                    doors.doorsWithNothingToStrip);

            return sb.ToString();
        }

        /// <summary>
        /// Areas marked desktopOnly whose decision currently does nothing, because they have no
        /// furniture container and no doors. Not an error — outside_gazebo is one today, and it
        /// starts working by itself the moment something is put there.
        /// </summary>
        static List<string> NoEffect(ObjectsPlatformGates.Result objects,
                                     DoorComponentGates.Result doors,
                                     RoomPlatforms platforms)
        {
            var inert = new List<string>();
            foreach (var entry in platforms.rooms)
            {
                if (entry == null || entry.platform != RoomPlatforms.DesktopOnly) continue;
                if (objects.areasPresent.Contains(entry.room)) continue;
                if (doors.areasWithDoors.Contains(entry.room)) continue;
                inert.Add(entry.room);
            }
            inert.Sort(string.CompareOrdinal);
            return inert;
        }

        static List<string> Union(List<string> a, List<string> b)
        {
            var all = new List<string>(a);
            foreach (var s in b) if (!all.Contains(s)) all.Add(s);
            all.Sort(string.CompareOrdinal);
            return all;
        }

        static void Section(StringBuilder sb, string title, List<string> items)
        {
            if (items == null || items.Count == 0) return;

            sb.AppendLine();
            sb.AppendLine(title + ": " + items.Count);
            for (int i = 0; i < items.Count && i < MaxListed; i++)
                sb.AppendLine("    " + items[i]);
            if (items.Count > MaxListed)
                sb.AppendLine("    … and " + (items.Count - MaxListed) + " more");
        }
    }
}
