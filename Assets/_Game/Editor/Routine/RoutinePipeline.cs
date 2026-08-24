using UnityEditor;

namespace FriWorld.Routine
{
    /// <summary>
    /// The order in which the object pipeline is run, and what each step is for.
    ///
    /// One list, two front ends: the Routine menu and the Object Pipeline window both read it,
    /// so the numbering in the menu and the descriptions in the window can never disagree.
    ///
    /// Nothing here does the work. Each step points at an existing menu item and the tools stay
    /// the single source of truth for their own behaviour.
    /// </summary>
    public static class RoutinePipeline
    {
        public struct Step
        {
            /// <summary>Shown in the window and, numbered, in the Routine menu.</summary>
            public string title;

            /// <summary>One or two sentences. Unity menus cannot show tooltips, so this is the
            /// only place the explanation can live.</summary>
            public string description;

            /// <summary>The menu item that actually does the work.</summary>
            public string menuPath;

            /// <summary>True when the tool reads Selection.gameObjects.</summary>
            public bool needsSelection;

            /// <summary>True for steps that are not part of every run.</summary>
            public bool optional;
        }

        public static readonly Step[] Steps =
        {
            new Step
            {
                title = "Report on selection",
                description = "Lists everything the registry cannot resolve in the selection: "
                            + "unknown types, types that are present but still blank, and names "
                            + "no prefix has stripped. Start here, and run it again after every "
                            + "other step until it says every object resolved to a decided type.",
                menuPath = "Tools/Object Registry/Report On Selection",
                needsSelection = true,
            },
            new Step
            {
                title = "Seed missing types",
                description = "Writes every unknown type key into ObjectTypes.json as an "
                            + "undecided entry. Fill in collider, layer and occluder by hand "
                            + "afterwards — until you do, those objects are left untouched. New "
                            + "entries are at the top of the file.",
                menuPath = "Tools/Object Registry/Seed Missing Types From Selection",
                needsSelection = true,
            },
            new Step
            {
                title = "Add prefixes",
                description = "Approves new container names as prefixes so their children's "
                            + "names can be stripped down to a type key, then reconciles "
                            + "RoomPlatforms.json with the areas that now exist. A prefix that "
                            + "would swallow a registered type is withheld and reported.",
                menuPath = "Tools/Object Registry/Add Prefixes From Selection",
                needsSelection = true,
            },
            new Step
            {
                title = "Sync room platforms",
                description = "Reconciles RoomPlatforms.json on its own, scanning the whole "
                            + "FriBuilding prefab rather than a selection. Only needed when "
                            + "containers were renamed or moved without any new prefix — the "
                            + "previous step already does this.",
                menuPath = "Tools/Object Registry/Sync Room Platforms",
                optional = true,
            },
            new Step
            {
                title = "Generate colliders",
                description = "Gives every object the collider its type asks for: none, box, "
                            + "sphere or mesh. An unknown or undecided type is skipped and "
                            + "listed rather than guessed at.",
                menuPath = "Tools/Colliders/Generate From Registry",
                needsSelection = true,
            },
            new Step
            {
                title = "Assign layers and static flags",
                description = "Sets the layer, the static flags, the Door tag and the NavMesh "
                            + "modifier from the type's layer and occluder fields. UNO and UYO "
                            + "in an object's name still override the registry.",
                menuPath = "Tools/Layers/Assign Layers And Static From Registry",
                needsSelection = true,
            },
            new Step
            {
                title = "Setup interactables",
                description = "Attaches the behaviour named in the type's script field, such as "
                            + "Door, together with its animator controller. Runs after layers "
                            + "because door_frame sits on the Interactable layer too and must "
                            + "not become an openable door.",
                menuPath = "Tools/Interactables/Setup From Registry",
                needsSelection = true,
            },
            new Step
            {
                title = "Room gates",
                description = "Writes the platform gates into the FriBuilding prefab from "
                            + "RoomPlatforms.json: a PlatformGate on each room container under "
                            + "Objects, and a ComponentGate on each door under fri_building. "
                            + "Preview first — a second preview after writing must report zero.",
                menuPath = "Tools/Feature Flags/Room Gates",
            },
        };

        /// <summary>
        /// Occlusion data is baked, so it cannot follow the registry. Shown at the end of the
        /// window because forgetting it is silent: the build looks right and culls wrong.
        /// </summary>
        public const string AfterwardsNote =
            "If any object's occluder setting changed, rebake occlusion culling. Nothing in this "
            + "list can do that for you, and a stale bake fails quietly.";

        /// <summary>
        /// Runs the tool behind a step. Straight through, not deferred: an earlier version routed
        /// this via EditorApplication.delayCall to keep the modal Room Gates window out of a
        /// repaint, and the callback never fired — every Routine item reported success and did
        /// nothing at all. A silent no-op is far worse than the reentrancy it was avoiding.
        /// </summary>
        public static void Run(Step step) => EditorApplication.ExecuteMenuItem(step.menuPath);
    }
}
