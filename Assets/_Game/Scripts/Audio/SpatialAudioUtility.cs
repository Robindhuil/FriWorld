using UnityEngine;

public enum EchoEnvironment
{
    None,
    Hall,
    Outside,
    Forest,
}

public static class SpatialAudioUtility
{
    public static void Configure3D(
        AudioSource source,
        float minDistance = 1f,
        float maxDistance = 10f,
        AudioRolloffMode rolloffMode = AudioRolloffMode.Linear,
        float spatialBlend = 1f,
        float dopplerLevel = 0f,
        bool loop = false,
        bool playOnAwake = false,
        EchoEnvironment echo = EchoEnvironment.None
    )
    {
        if (source == null)
            return;

        float clampedMinDistance = Mathf.Max(0.1f, minDistance);
        float clampedMaxDistance = Mathf.Max(clampedMinDistance + 0.1f, maxDistance);

        source.spatialBlend = Mathf.Clamp01(spatialBlend);
        source.rolloffMode = rolloffMode;
        source.minDistance = clampedMinDistance;
        source.maxDistance = clampedMaxDistance;
        source.dopplerLevel = Mathf.Max(0f, dopplerLevel);
        source.loop = loop;
        source.playOnAwake = playOnAwake;

        ConfigureReverb(source, echo);
    }

    private static void ConfigureReverb(AudioSource source, EchoEnvironment echo)
    {
        var reverb = source.gameObject.GetComponent<AudioReverbFilter>();

        if (echo == EchoEnvironment.None)
        {
            if (reverb != null)
                reverb.enabled = false;
            return;
        }

        if (reverb == null)
            reverb = source.gameObject.AddComponent<AudioReverbFilter>();

        reverb.enabled = true;
        reverb.reverbPreset = echo switch
        {
            EchoEnvironment.Hall => AudioReverbPreset.Auditorium,
            EchoEnvironment.Outside => AudioReverbPreset.Alley,
            EchoEnvironment.Forest => AudioReverbPreset.Forest,
            _ => AudioReverbPreset.Off,
        };
    }
}
