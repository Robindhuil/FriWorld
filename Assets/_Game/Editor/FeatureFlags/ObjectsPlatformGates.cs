using System.Collections.Generic;
using FriWorld.ObjectRegistry;
using UnityEngine;

namespace FriWorld.FeatureFlags
{
    /// <summary>
    /// Brings the PlatformGate components under FriBuilding/Objects in line with
    /// RoomPlatforms.json. A whole room container is gated at once — everything the room holds
    /// goes with it.
    ///
    /// Reconciles rather than appends: it adds what is missing, retargets what is wrong and
    /// removes what the data says should not be there. That last part is why "all" and "no value
    /// at all" have to be different — only an explicit "all" gives it permission to delete.
    /// </summary>
    public static class ObjectsPlatformGates
    {
        public class Result
        {
            public int added, retargeted, removed, unchanged;

            /// <summary>Every area matched in this branch, whatever its decision.</summary>
            public readonly List<string> areasPresent = new List<string>();

            public readonly List<string> undecided = new List<string>();
            public readonly List<string> nested = new List<string>();
            public readonly List<string> badValues = new List<string>();
            public readonly List<string> orphanGates = new List<string>();
        }

        /// <summary>
        /// <paramref name="dryRun"/> counts what would change without touching anything, so the
        /// preview and the real run walk the same code and cannot drift apart.
        /// </summary>
        public static Result Apply(GameObject prefabRoot, IReadOnlyList<string> prefixes,
                                   RoomPlatforms platforms, bool dryRun)
        {
            var result = new Result();
            var branch = RoomGateScope.Branch(prefabRoot, RoomGateScope.ObjectsBranch);
            if (branch == null) return result;

            var areas = RoomGateScope.Match(branch, prefixes, platforms);

            var areaTransforms = new HashSet<Transform>();
            foreach (var area in areas)
            {
                areaTransforms.Add(area.transform);
                result.areasPresent.Add(area.area);
            }

            foreach (var area in areas)
            {
                if (area.platform == null) { result.undecided.Add(area.area); continue; }

                if (!RoomPlatforms.IsValidPlatform(area.platform))
                {
                    result.badValues.Add(area.area + " = \"" + area.platform + "\"");
                    continue;
                }

                var existing = area.transform.GetComponent<PlatformGate>();

                // "all" is a licence to delete, so it runs before the nesting guard: a stale gate
                // on a container of areas still has to come off.
                if (area.platform == RoomPlatforms.All)
                {
                    if (existing == null) result.unchanged++;
                    else
                    {
                        if (!dryRun) Object.DestroyImmediate(existing);
                        result.removed++;
                    }
                    continue;
                }

                // Gating a container of areas would strip them too and void their own decisions
                // without saying so.
                if (RoomGateScope.ContainsAnotherArea(area, areas))
                {
                    result.nested.Add(area.area);
                    continue;
                }

                var target = area.platform == RoomPlatforms.WebOnly
                    ? PlatformGate.Target.WebOnly
                    : PlatformGate.Target.DesktopOnly;

                if (existing == null)
                {
                    if (!dryRun)
                        area.transform.gameObject.AddComponent<PlatformGate>().target = target;
                    result.added++;
                }
                else if (existing.target != target)
                {
                    if (!dryRun) existing.target = target;
                    result.retargeted++;
                }
                else result.unchanged++;
            }

            // A gate somewhere that is not an area is somebody's hand-made exception. Report it,
            // do not delete it — removing another person's setup is not this tool's job.
            foreach (var gate in branch.GetComponentsInChildren<PlatformGate>(true))
                if (gate != null && !areaTransforms.Contains(gate.transform))
                    result.orphanGates.Add(RegistryScanner.PathOf(gate.transform));

            return result;
        }
    }
}
