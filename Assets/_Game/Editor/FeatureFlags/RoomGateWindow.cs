using System.Collections.Generic;
using FriWorld.ObjectRegistry;
using UnityEditor;
using UnityEngine;

namespace FriWorld.FeatureFlags
{
    /// <summary>
    /// Tools > Feature Flags > Room Gates.
    ///
    /// One window for both halves of the platform gating, because they are the same decision
    /// applied to two different trees and it is easier to reason about them side by side. You
    /// pick a branch and the window writes only that branch — but it always previews BOTH, so
    /// the counts and the warnings describe the whole building even when you are only fixing
    /// half of it.
    ///
    /// | branch        | what it writes                                                     |
    /// |---------------|--------------------------------------------------------------------|
    /// | Objects       | PlatformGate on the room container — the whole room's furniture    |
    /// | fri_building  | ComponentGate on each door — the room's walls and windows stay     |
    ///
    /// They are separate because they fail separately: a .blend reimport wipes the door gates
    /// inside fri_building and leaves the Objects branch untouched, so the fix should not have
    /// to rewrite Objects as well.
    /// </summary>
    public class RoomGateWindow : EditorWindow
    {
        enum Branch
        {
            Objects,
            Doors,
        }

        Branch branch = Branch.Objects;
        Vector2 scroll;
        string report = "";
        string status = "";
        MessageType statusKind = MessageType.Info;

        [MenuItem("Tools/Feature Flags/Room Gates")]
        static void Open()
        {
            var window = CreateInstance<RoomGateWindow>();
            window.titleContent = new GUIContent("Room Gates");
            window.minSize = new Vector2(560, 460);
            window.Preview();
            window.ShowModalUtility();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Source of truth", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(ObjectRegistryMenu.RoomPlatformsPath, EditorStyles.miniLabel);
            EditorGUILayout.LabelField(Summary(), EditorStyles.miniLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Which branch to write", EditorStyles.boldLabel);

            branch = (Branch)GUILayout.SelectionGrid((int)branch, new[]
            {
                "Objects — PlatformGate on whole room containers",
                "fri_building — ComponentGate on doors only",
            }, 1, EditorStyles.radioButton);

            EditorGUILayout.HelpBox(branch == Branch.Objects
                ? "Every room container under Objects whose area is desktopOnly or webOnly gets a "
                + "PlatformGate, and the whole container is stripped at build time. An area marked "
                + "all has its gate removed. Undecided areas are left alone."
                : "Every door inside a desktopOnly area gets a ComponentGate that strips its Door "
                + "script, Animator and AudioSource and moves it to the Obstacle layer — the door "
                + "stays visible and stops opening. Doors are taken from the type registry, so "
                + "door_frame and doorstep are never touched. Walls, ceilings and windows are "
                + "never stripped in this branch.",
                MessageType.None);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview", GUILayout.Height(28))) Preview();
                if (GUILayout.Button("Write " + BranchName(branch), GUILayout.Height(28))) Write();
            }

            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, statusKind);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Both branches, previewed", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (GUILayout.Button("Close")) Close();
        }

        static string BranchName(Branch b) => b == Branch.Objects ? "Objects" : "doors";

        string Summary()
        {
            var platforms = RoomPlatforms.Load(ObjectRegistryMenu.RoomPlatformsPath);
            int desktop = 0, web = 0, all = 0, undecided = 0;
            foreach (var entry in platforms.rooms)
            {
                if (entry == null) continue;
                if (entry.platform == RoomPlatforms.DesktopOnly) desktop++;
                else if (entry.platform == RoomPlatforms.WebOnly) web++;
                else if (entry.platform == RoomPlatforms.All) all++;
                else undecided++;
            }
            return platforms.rooms.Count + " areas — " + desktop + " desktopOnly, " + web
                 + " webOnly, " + all + " all, " + undecided + " undecided";
        }

        void Preview() => Run(writeObjects: false, writeDoors: false);

        void Write() => Run(writeObjects: branch == Branch.Objects,
                            writeDoors: branch == Branch.Doors);

        void Run(bool writeObjects, bool writeDoors)
        {
            bool writing = writeObjects || writeDoors;

            int obstacleLayer = LayerMask.NameToLayer(DoorComponentGates.ObstacleLayerName);
            if (obstacleLayer < 0)
            {
                Fail("The layer \"" + DoorComponentGates.ObstacleLayerName + "\" is not defined. "
                   + "Add it in Project Settings > Tags and Layers — without it a stripped door "
                   + "would stay on Interactable and still show the prompt.");
                return;
            }

            var platforms = RoomPlatforms.Load(ObjectRegistryMenu.RoomPlatformsPath);
            if (platforms.rooms.Count == 0)
            {
                Fail(ObjectRegistryMenu.RoomPlatformsPath + " is empty. Run "
                   + "Tools > Object Registry > Sync Room Platforms first, otherwise every area "
                   + "would be skipped as undecided.");
                return;
            }

            var prefixes = TypeRegistry.LoadPrefixes(ObjectRegistryMenu.PrefixesPath);
            var registry = TypeRegistry.Load(ObjectRegistryMenu.TypesPath);
            if (registry.types.Count == 0)
            {
                Fail(ObjectRegistryMenu.TypesPath + " is empty, so no object can be recognised "
                   + "as a door.");
                return;
            }

            ObjectsPlatformGates.Result objectsResult;
            DoorComponentGates.Result doorsResult;

            var contents = RoomGateScope.Open();
            bool closed = false;
            try
            {
                // Both branches always run. Only the selected one is allowed to mutate, so the
                // report describes the whole building even when you are writing half of it.
                objectsResult = ObjectsPlatformGates.Apply(
                    contents, prefixes, platforms, dryRun: !writeObjects);
                doorsResult = DoorComponentGates.Apply(
                    contents, prefixes, registry, platforms, obstacleLayer, dryRun: !writeDoors);

                if (writing) RoomGateScope.SaveAndClose(contents);
                else RoomGateScope.Close(contents);
                closed = true;
            }
            finally
            {
                if (!closed) RoomGateScope.Close(contents);
            }

            // No AssetDatabase.Refresh here on purpose. SaveAsPrefabAsset already writes and
            // reimports the asset, and an import kicked off from a modal window's OnGUI is the
            // kind of reentrancy that hangs the editor.
            report = RoomGateReport.Build(objectsResult, doorsResult, platforms);

            if (!writing)
            {
                status = "Preview only — the prefab was not written.";
                statusKind = MessageType.Info;
                return;
            }

            int changed = writeObjects
                ? objectsResult.added + objectsResult.retargeted + objectsResult.removed
                : doorsResult.added + doorsResult.reconfigured + doorsResult.removed;

            status = "Wrote " + changed + " change(s) to " + BranchName(branch) + " in "
                   + RoomGateScope.PrefabPath + ". Press Preview again — a second run must report "
                   + "zero for that branch.";
            statusKind = MessageType.Info;
            Debug.Log("[RoomGates] wrote " + BranchName(branch) + "\n" + report);
        }

        void Fail(string message)
        {
            status = message;
            statusKind = MessageType.Error;
            Debug.LogError("[RoomGates] " + message);
        }
    }
}
