using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FriWorld.Character.Editor
{
    /// <summary>
    /// Reads the base prefabs into the plain data CharacterValidation and the baker work on.
    ///
    /// LoadPrefabContents rather than opening the prefab in the stage: the isolated preview scene
    /// raises no modal dialogs, which is what makes this runnable from a script or over MCP.
    /// </summary>
    public static class CharacterScan
    {
        public const string MalePrefabPath = "Assets/_Game/Prefabs/npc/character_male.prefab";
        public const string FemalePrefabPath = "Assets/_Game/Prefabs/npc/character_female.prefab";

        public static string PathFor(Gender gender) =>
            gender == Gender.Male ? MalePrefabPath : FemalePrefabPath;

        /// <summary>Returns null when the prefab is not there, so the caller can say which one.</summary>
        public static ScannedBody Read(Gender gender)
        {
            string path = PathFor(gender);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return null;

            var body = new ScannedBody { gender = gender, prefabPath = path };
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var materialNames = new List<string>();
                    foreach (var material in renderer.sharedMaterials)
                        materialNames.Add(material != null ? material.name : string.Empty);

                    body.objects.Add(new ScannedObject
                    {
                        name = renderer.gameObject.name,
                        materialNames = materialNames.ToArray(),
                    });
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return body;
        }

        public static List<ScannedBody> ReadBoth(List<string> missing)
        {
            var bodies = new List<ScannedBody>();

            foreach (Gender gender in new[] { Gender.Male, Gender.Female })
            {
                var body = Read(gender);
                if (body == null) missing.Add(PathFor(gender));
                else bodies.Add(body);
            }

            return bodies;
        }
    }
}
