using System.Collections.Generic;
using FriWorld.ObjectRegistry;
using UnityEngine;

namespace FriWorld.FeatureFlags
{
    /// <summary>
    /// Brings the ComponentGate components on doors under FriBuilding/fri_building in line with
    /// RoomPlatforms.json.
    ///
    /// This branch never strips whole objects. The same container holds the room's walls,
    /// ceiling and windows, so removing it would leave a hole in the building. It strips only
    /// the door's behaviour — the Door script, its Animator and its AudioSource — and moves the
    /// object to Obstacle so the interaction raycast stops seeing it. The door stays visible and
    /// stops opening — the same configuration the hand-run DoorGateSetup used to produce, now
    /// derived from data instead of from the current Hierarchy selection.
    ///
    /// Doors come from the object type registry (script == "Door"), never from looking for
    /// "door" in the name. 808 object names contain it, but only the 283 that resolve to a Door
    /// type are doors — the rest are door_frame, door_frame_&lt;int&gt;_glass and doorstep, which
    /// carry no behaviour and have nothing to strip.
    ///
    /// Why a layer change and not a trigger collider: PlayerInteract raycasts with
    /// QueryTriggerInteraction.Ignore, so turning the door into a trigger would break interaction
    /// on every platform rather than only on web.
    /// </summary>
    public static class DoorComponentGates
    {
        public const string DoorScriptName = "Door";
        public const string ObstacleLayerName = "Obstacle";

        public class Result
        {
            public int added, reconfigured, removed, unchanged;

            /// <summary>Areas that own at least one door, whatever their decision.</summary>
            public readonly List<string> areasWithDoors = new List<string>();

            public readonly List<string> undecided = new List<string>();
            public readonly List<string> doorsWithNothingToStrip = new List<string>();
        }

        /// <summary>
        /// <paramref name="dryRun"/> counts what would change without touching anything, so the
        /// preview and the real run walk the same code and cannot drift apart.
        /// </summary>
        public static Result Apply(GameObject prefabRoot, IReadOnlyList<string> prefixes,
                                   TypeRegistry registry, RoomPlatforms platforms,
                                   int obstacleLayer, bool dryRun)
        {
            var result = new Result();
            var branch = RoomGateScope.Branch(prefabRoot, RoomGateScope.BuildingBranch);
            if (branch == null) return result;

            var areas = RoomGateScope.Match(branch, prefixes, platforms);

            var areaTransforms = new HashSet<Transform>();
            foreach (var area in areas) areaTransforms.Add(area.transform);

            foreach (var area in areas)
            {
                var doors = new List<Transform>();
                CollectDoors(area.transform, areaTransforms, prefixes, registry, doors);
                if (doors.Count > 0) result.areasWithDoors.Add(area.area);

                if (area.platform == null) { result.undecided.Add(area.area); continue; }

                // Only desktopOnly adds a door gate. Every other decided value means "no door
                // gate belongs here", which covers "all" and "webOnly" in one branch instead of
                // a special case for each.
                bool wantGate = area.platform == RoomPlatforms.DesktopOnly;

                foreach (var door in doors)
                {
                    var existing = door.GetComponent<ComponentGate>();

                    if (!wantGate)
                    {
                        if (existing == null) result.unchanged++;
                        else
                        {
                            if (!dryRun) Object.DestroyImmediate(existing);
                            result.removed++;
                        }
                        continue;
                    }

                    var components = new List<Component>();
                    var script = door.GetComponent<Door>();
                    var animator = door.GetComponent<Animator>();
                    var audio = door.GetComponent<AudioSource>();
                    if (script != null) components.Add(script);
                    if (animator != null) components.Add(animator);
                    if (audio != null) components.Add(audio);

                    if (components.Count == 0)
                    {
                        // A gate with an empty list does nothing but add clutter, and it hides
                        // the real problem: the Door behaviour was never attached. Run
                        // FriWorld > Generate > Layers And Static From Registry first.
                        result.doorsWithNothingToStrip.Add(RegistryScanner.PathOf(door));
                        if (existing != null)
                        {
                            if (!dryRun) Object.DestroyImmediate(existing);
                            result.removed++;
                        }
                        continue;
                    }

                    if (existing == null)
                    {
                        if (!dryRun)
                            Configure(door.gameObject.AddComponent<ComponentGate>(), components,
                                      obstacleLayer);
                        result.added++;
                    }
                    else if (NeedsReconfigure(existing, components, obstacleLayer))
                    {
                        if (!dryRun) Configure(existing, components, obstacleLayer);
                        result.reconfigured++;
                    }
                    else result.unchanged++;
                }
            }

            return result;
        }

        /// <summary>
        /// The doors belonging to this area. The walk stops at any nested area container, so a
        /// door is owned by exactly one area — otherwise rb_basement would claim the doors of all
        /// fourteen rooms inside it and decide on their behalf.
        /// </summary>
        static void CollectDoors(Transform areaRoot, HashSet<Transform> areaTransforms,
                                 IReadOnlyList<string> prefixes, TypeRegistry registry,
                                 List<Transform> acc)
        {
            foreach (Transform child in areaRoot)
            {
                if (areaTransforms.Contains(child)) continue;   // the inner area owns this

                var entry = registry.Find(ObjectTypeKey.Derive(child.name.Trim(), prefixes));
                if (entry != null && entry.script == DoorScriptName) acc.Add(child);

                CollectDoors(child, areaTransforms, prefixes, registry, acc);
            }
        }

        static void Configure(ComponentGate gate, List<Component> components, int obstacleLayer)
        {
            gate.target = PlatformGate.Target.DesktopOnly;
            gate.components = components;
            gate.changeLayerWhenStripped = true;
            gate.strippedLayer = obstacleLayer;
        }

        static bool NeedsReconfigure(ComponentGate gate, List<Component> want, int obstacleLayer)
        {
            if (gate.target != PlatformGate.Target.DesktopOnly) return true;
            if (!gate.changeLayerWhenStripped) return true;
            if (gate.strippedLayer != obstacleLayer) return true;
            if (gate.components == null || gate.components.Count != want.Count) return true;

            for (int i = 0; i < want.Count; i++)
                if (gate.components[i] != want[i]) return true;

            return false;
        }
    }
}
