using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [SerializeField] private AudioSource sfxSource; // For sound effects
    [SerializeField] private AudioSource musicSource; // For background music
    [SerializeField] private AudioClip[] initialMusicQueue; // Music queue set in Inspector
    [SerializeField] private AudioClip[] sfxBank; // Bank of sound effects set in Inspector

    private Queue<AudioClip> musicQueue = new Queue<AudioClip>(); // Runtime music queue
    private bool isMusicPlaying; // Track music playback state

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Populate initial music queue from Inspector
        foreach (var clip in initialMusicQueue)
        {
            if (clip != null)
            {
                musicQueue.Enqueue(clip);
            }
        }
    }

    private void Update()
    {
        // Handle music queue
        if (!musicSource.isPlaying && musicQueue.Count > 0 && !isMusicPlaying)
        {
            PlayNextMusic();
        }
    }

    // Queue a music track
    public void QueueMusic(AudioClip clip)
    {
        if (clip != null)
        {
            musicQueue.Enqueue(clip);
            if (!musicSource.isPlaying && !isMusicPlaying)
            {
                PlayNextMusic();
            }
        }
    }

    // Play the next music track in the queue
    private void PlayNextMusic()
    {
        if (musicQueue.Count > 0)
        {
            isMusicPlaying = true;
            AudioClip clip = musicQueue.Dequeue();
            musicSource.clip = clip;
            musicSource.Play();
            isMusicPlaying = false;
        }
    }

    // Play a music track immediately, clearing the queue
    public void PlayMusic(AudioClip clip)
    {
        if (clip != null)
        {
            musicQueue.Clear();
            musicSource.Stop();
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    // Play a sound effect immediately by AudioClip
    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // Play a sound effect immediately by index from the sfx bank
    public void PlaySoundByIndex(int index)
    {
        if (index >= 0 && index < sfxBank.Length && sfxBank[index] != null)
        {
            PlaySound(sfxBank[index]);
        }
        else
        {
            Debug.LogWarning($"Sound effect at index {index} is invalid or out of range.");
        }
    }

    // Clear music queue
    public void ClearMusicQueue()
    {
        musicQueue.Clear();
    }

    // Stop all music playback
    public void StopMusic()
    {
        musicSource.Stop();
        ClearMusicQueue();
    }

    // Stop all sfx playback
    public void StopSfx()
    {
        sfxSource.Stop();
    }
}