using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Finds the Sfx group on the project's audio mixer and routes AudioSources into it.
///
/// An AudioSource with no output group plays straight into the AudioListener, so no mixer
/// parameter reaches it — the volume sliders in the settings menu do nothing to that sound. That
/// is what happened to the doors: routing lived only in the Interactable inspector, so it ran
/// when a human ticked "play sound effect" by hand and never for the 277 doors the generator
/// created. Fourteen were routed, and those fourteen were the hand-made ones.
///
/// Editor-only. Every AudioSource in the building is authored, not spawned, so it is enough to
/// get this right when the object is generated.
/// </summary>
public static class SfxMixerGroup
{
    public const string GroupName = "Sfx";

    static AudioMixerGroup cached;

    /// <summary>The Sfx group, or null when the project has no mixer that defines one.</summary>
    public static AudioMixerGroup Find()
    {
        if (cached != null) return cached;

        foreach (string guid in AssetDatabase.FindAssets("t:AudioMixer"))
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(AssetDatabase.GUIDToAssetPath(guid));
            if (mixer == null) continue;

            // FindMatchingGroups substring-matches the full path, so "Sfx" finds "Master/Sfx".
            var groups = mixer.FindMatchingGroups(GroupName);
            if (groups.Length == 0) continue;

            cached = groups[0];
            return cached;
        }

        return null;
    }

    /// <summary>
    /// Routes the source into the Sfx group when it has no group yet, and reports whether it
    /// changed anything.
    ///
    /// A source that already points somewhere is left alone: somebody put it on Music or on a
    /// group of their own on purpose, and silently dragging it to Sfx would undo that decision
    /// without saying so.
    /// </summary>
    public static bool Route(AudioSource source)
    {
        if (source == null || source.outputAudioMixerGroup != null) return false;

        var group = Find();
        if (group == null) return false;

        source.outputAudioMixerGroup = group;
        EditorUtility.SetDirty(source);
        return true;
    }
}
