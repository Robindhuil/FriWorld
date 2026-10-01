using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FriWorld.Navigator.Editor
{
    /// <summary>
    /// Builds the Navigator for the web with its own build profile, into <see cref="OutputPath"/>.
    ///
    /// The profile is made active before the build, because URP strips shaders by the editor's
    /// active platform. Afterwards the editor is put back on the profile and platform it was on:
    /// left on web, FriWorld's play mode would strip desktop-only content as the web build does.
    /// What the Navigator never loads is then taken out of the output (<see cref="RemoveUnused"/>),
    /// and the room codes it knows are listed beside it (<see cref="WriteRooms"/>).
    /// The outcome is also written to a file, so whatever started the build can read it afterwards
    /// instead of watching the console.
    /// </summary>
    public static class NavigatorBuild
    {
        public const string ProfilePath = "Assets/_Navigator/Settings/NavigatorWeb.asset";
        public const string OutputPath = "Builds/Navigator/Web";
        public const string ResultPath = "Temp/navigator-build.txt";

        [MenuItem("Navigator/Build Web", priority = 20)]
        public static void BuildWeb()
        {
            if (File.Exists(ResultPath))
                File.Delete(ResultPath);

            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath);
            if (profile == null)
            {
                Debug.LogError($"[Navigator] Build profile {ProfilePath} is missing.");
                return;
            }

            BuildProfile previousProfile = BuildProfile.GetActiveBuildProfile();
            BuildTarget previousTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup previousGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            // URP picks the URP assets whose shader variants survive stripping by the editor's
            // active platform, not by the platform being built. Started from Windows, the build
            // kept the variants of the desktop quality levels, stripped every ForwardLit variant
            // RP_Web needs, and nothing Lit was drawn. So the editor goes to the web first.
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            BuildProfile.SetActiveBuildProfile(profile);

            BuildReport report = null;
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL && BuildProfile.GetActiveBuildProfile() == profile)
                report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions
                {
                    buildProfile = profile,
                    locationPathName = OutputPath,
                    options = BuildOptions.None,
                });

            // Null puts back the plain platform, after which the target can switch.
            BuildProfile.SetActiveBuildProfile(previousProfile);
            if (EditorUserBuildSettings.activeBuildTarget != previousTarget)
                EditorUserBuildSettings.SwitchActiveBuildTarget(previousGroup, previousTarget);

            if (report == null)
            {
                const string notSwitched = "Failed: the editor did not switch to the web profile, nothing was built";
                File.WriteAllText(ResultPath, notSwitched);
                Debug.LogError("[Navigator] Web build " + notSwitched);
                return;
            }

            BuildSummary summary = report.summary;
            string outcome = $"{summary.result} in {summary.totalTime.ToString(@"mm\:ss")}, "
                           + $"{summary.totalErrors} errors, {summary.totalWarnings} warnings";
            if (summary.result == BuildResult.Succeeded)
            {
                RemoveUnused(OutputPath);
                WriteRooms(OutputPath, AssetDatabase.LoadAssetAtPath<RoomAnchors>(NavigatorBake.AnchorsAssetPath));
                long bytes = new DirectoryInfo(OutputPath).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                outcome += $" -> {OutputPath}, {bytes / (1024f * 1024f):F1} MB";
            }

            File.WriteAllText(ResultPath, outcome);
            if (summary.result == BuildResult.Succeeded)
                Debug.Log("[Navigator] Web build " + outcome);
            else
                Debug.LogError("[Navigator] Web build " + outcome);
        }

        public const string RoomsFile = "rooms.json";

        /// <summary>
        /// Lists the room codes this build can fly to, as a JSON array beside it, so the page that
        /// offers the rooms shows exactly what the uploaded build knows.
        /// </summary>
        public static void WriteRooms(string outputPath, RoomAnchors anchors)
        {
            var codes = anchors.anchors.Select(a => "\"" + a.code + "\"").OrderBy(c => c, System.StringComparer.Ordinal);
            File.WriteAllText(Path.Combine(outputPath, RoomsFile), "[" + string.Join(",", codes) + "]");
        }

        /// <summary>
        /// Takes out of the build output what the Navigator never loads. StreamingAssets go into
        /// every build whole — a build profile cannot leave a folder out — and FriWorld keeps its
        /// memory-sign videos there, 200 MB the Navigator scene has no player for. Burst writes its
        /// debug symbols beside the build; they are never served. Only the output is touched.
        /// </summary>
        public static void RemoveUnused(string outputPath)
        {
            string streamingAssets = Path.Combine(outputPath, "StreamingAssets");
            string videos = Path.Combine(streamingAssets, "videos");
            if (Directory.Exists(videos))
                Directory.Delete(videos, true);
            if (Directory.Exists(streamingAssets) && !Directory.EnumerateFileSystemEntries(streamingAssets).Any())
                Directory.Delete(streamingAssets);

            foreach (string symbols in Directory.GetDirectories(outputPath, "*_BurstDebugInformation_DoNotShip"))
                Directory.Delete(symbols, true);
        }
    }
}
