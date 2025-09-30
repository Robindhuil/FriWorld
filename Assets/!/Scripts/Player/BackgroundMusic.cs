using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SongData
{
    public AudioClip clip;
    public string author;
    public string songName;
    public List<string> urls;
}

public class BackgroundMusic : MonoBehaviour
{
    [SerializeField] private List<SongData> songs;
    [SerializeField] private AudioSource audioSource;

    private SongData currentSong;

    public SongData CurrentSong => currentSong;

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        StartCoroutine(PlaySongsLoop());
    }

    private IEnumerator PlaySongsLoop()
    {
        while (true)
        {
            if (songs.Count == 0) yield break;

            currentSong = songs[Random.Range(0, songs.Count)];
            audioSource.clip = currentSong.clip;
            audioSource.Play();

            yield return new WaitForSeconds(currentSong.clip.length);
        }
    }
}
