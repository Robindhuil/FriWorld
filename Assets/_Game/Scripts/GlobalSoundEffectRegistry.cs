using System;
using System.Collections.Generic;
using UnityEngine;

public enum SoundEffectId
{
    UI_Click,
    UI_Key,
    UI_Button,
    Foley_Grab,
    Foley_JumpStart,
    Foley_JumpEnd,
    Foley_Walk,
    Foley_Sprint,
    Interactable_Secret,
    Interactable_DoorClose,
    Interactable_DoorOpen,
    Interactable_DoorSliderClose,
    Interactable_DoorSliderOpen,
}

public class GlobalSoundEffectRegistry : MonoBehaviour
{
    [Serializable]
    private class UiSounds
    {
        public AudioClip[] clickSounds;
        public AudioClip[] keySounds;
        public AudioClip[] buttonSounds;
    }

    [Serializable]
    private class FoleySounds
    {
        public AudioClip[] grab;
        public AudioClip[] jumpStart;
        public AudioClip[] jumpEnd;
        public AudioClip[] walkSound;
        public AudioClip[] sprintSound;
    }

    [Serializable]
    private class InteractableSounds
    {
        public AudioClip[] secretSound;
        public AudioClip[] doorClose;
        public AudioClip[] doorOpen;
        public AudioClip[] doorSliderClose;
        public AudioClip[] doorSliderOpen;
    }

    public static GlobalSoundEffectRegistry Instance { get; private set; }

    //bro
    [Header("UI")]
    [SerializeField]
    private UiSounds ui = new UiSounds();

    [Header("Foley")]
    [SerializeField]
    private FoleySounds foley = new FoleySounds();

    [Header("Interactables")]
    [SerializeField]
    private InteractableSounds interactables = new InteractableSounds();

    private readonly Dictionary<SoundEffectId, AudioClip[]> clipsById =
        new Dictionary<SoundEffectId, AudioClip[]>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[GlobalSoundEffectRegistry] Duplicate instance found. Destroying the new one."
            );
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildLookup();
    }

    private void OnValidate()
    {
        BuildLookup();
    }

    public AudioClip GetClip(SoundEffectId id)
    {
        return GetClip(id, 0);
    }

    public AudioClip GetClip(SoundEffectId id, int variationIndex)
    {
        if (!clipsById.TryGetValue(id, out AudioClip[] clips) || clips == null || clips.Length == 0)
        {
            return null;
        }

        if (variationIndex < 0 || variationIndex >= clips.Length)
        {
            return null;
        }

        return clips[variationIndex];
    }

    public AudioClip GetRandomClip(SoundEffectId id)
    {
        if (!clipsById.TryGetValue(id, out AudioClip[] clips) || clips == null || clips.Length == 0)
        {
            return null;
        }

        int randomStart = UnityEngine.Random.Range(0, clips.Length);
        for (int i = 0; i < clips.Length; i++)
        {
            int index = (randomStart + i) % clips.Length;
            if (clips[index] != null)
            {
                return clips[index];
            }
        }

        return null;
    }

    public static AudioClip Get(SoundEffectId id)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[GlobalSoundEffectRegistry] No instance found in scene.");
            return null;
        }

        return Instance.GetClip(id);
    }

    public static AudioClip GetRandom(SoundEffectId id)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[GlobalSoundEffectRegistry] No instance found in scene.");
            return null;
        }

        return Instance.GetRandomClip(id);
    }

    private void BuildLookup()
    {
        clipsById.Clear();

        clipsById[SoundEffectId.UI_Click] = ui.clickSounds;
        clipsById[SoundEffectId.UI_Key] = ui.keySounds;
        clipsById[SoundEffectId.UI_Button] = ui.buttonSounds;

        clipsById[SoundEffectId.Foley_Grab] = foley.grab;
        clipsById[SoundEffectId.Foley_JumpStart] = foley.jumpStart;
        clipsById[SoundEffectId.Foley_JumpEnd] = foley.jumpEnd;
        clipsById[SoundEffectId.Foley_Walk] = foley.walkSound;
        clipsById[SoundEffectId.Foley_Sprint] = foley.sprintSound;

        clipsById[SoundEffectId.Interactable_Secret] = interactables.secretSound;
        clipsById[SoundEffectId.Interactable_DoorClose] = interactables.doorClose;
        clipsById[SoundEffectId.Interactable_DoorOpen] = interactables.doorOpen;
        clipsById[SoundEffectId.Interactable_DoorSliderClose] = interactables.doorSliderClose;
        clipsById[SoundEffectId.Interactable_DoorSliderOpen] = interactables.doorSliderOpen;
    }
}
