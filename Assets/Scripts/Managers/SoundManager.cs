using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Centralized audio manager handling all playback and mixer volume control.
/// Uses a Singleton pattern for easy access (SoundManager.Instance.PlaySFX("Key")).
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Mixer Settings")]
    [Tooltip("The master mixer asset (for volume control).")]
    public AudioMixer masterMixer;

    [Header("Audio Configuration")]
    [Tooltip("Define your main audio groups (Music, SFX, Voice) here.")]
    public AudioGroupData[] audioGroups;

    private Dictionary<string, (AudioClip[], AudioMixerGroup, float)> soundMap;
    private AudioSource sfxSource;
    private AudioSource musicSource;
    // --- NEW: Dedicated source for voice lines ---
    private AudioSource voiceSource;
    // ---------------------------------------------

    private void Awake()
    {
        // --- Singleton Setup ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // -------------------------

        InitializeAudioSources();
        BuildSoundMap();
    }

    /// <summary>
    /// Creates dedicated AudioSources for music, SFX, and Voice.
    /// </summary>
    private void InitializeAudioSources()
    {
        // Source dedicated to MUSIC (must loop)
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        // Source dedicated to SFX (for non-overlapping one-shot sounds)
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        // --- NEW: Source dedicated to VOICE (allows voice to queue/interrupt without affecting SFX) ---
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.loop = false;
        voiceSource.playOnAwake = false;
        // -----------------------------------------------------------------------------------------------
    }

    /// <summary>
    /// Builds a fast-lookup dictionary for all sound clips based on their keyName.
    /// </summary>
    private void BuildSoundMap()
    {
        soundMap = new Dictionary<string, (AudioClip[], AudioMixerGroup, float)>();

        foreach (var group in audioGroups)
        {
            if (group.mixerGroup == null)
            {
                Debug.LogError($"Audio Group '{group.groupName}' is missing its AudioMixerGroup assignment!");
                continue;
            }

            foreach (var clipData in group.soundClips)
            {
                if (soundMap.ContainsKey(clipData.keyName))
                {
                    Debug.LogWarning($"Duplicate audio key found: {clipData.keyName}. Ignoring duplicate.");
                    continue;
                }

                soundMap[clipData.keyName] = (clipData.clips, group.mixerGroup, clipData.volume);
            }
        }
    }

    // --- PUBLIC PLAYBACK METHODS ---

    /// <summary>
    /// Plays a sound clip by its unique key. Handles randomization and mixer routing.
    /// </summary>
    /// <param name="key">The keyName of the sound (e.g., 'Explosion').</param>
    public void PlaySFX(string key)
    {
        if (soundMap.TryGetValue(key, out var data))
        {
            AudioClip[] clips = data.Item1;
            AudioMixerGroup group = data.Item2;
            float volume = data.Item3;

            if (clips.Length > 0)
            {
                // Select a random clip if multiple exist
                AudioClip clipToPlay = clips[UnityEngine.Random.Range(0, clips.Length)];

                // Play the one-shot clip through the SFX source, routed to the correct group
                sfxSource.outputAudioMixerGroup = group;
                sfxSource.PlayOneShot(clipToPlay, volume);
            }
        }
        else
        {
            Debug.LogWarning($"Attempted to play unknown audio key: {key}");
        }
    }

    // --- NEW: Plays a voice clip using the dedicated voice source ---
    /// <summary>
    /// Plays a voice clip by its unique key. It uses a dedicated source 
    /// allowing multiple voice clips to queue or interrupt independently of SFX.
    /// </summary>
    /// <param name="key">The keyName of the voice clip (e.g., 'Warning').</param>
    public void PlayVoice(string key)
    {
        if (soundMap.TryGetValue(key, out var data))
        {
            AudioClip[] clips = data.Item1;
            AudioMixerGroup group = data.Item2;
            float volume = data.Item3;

            if (clips.Length > 0)
            {
                // Select a random clip if multiple exist
                AudioClip clipToPlay = clips[UnityEngine.Random.Range(0, clips.Length)];

                // Route to the correct mixer group (e.g., "Voice")
                voiceSource.outputAudioMixerGroup = group;

                // Stop any currently playing voice line and play the new one
                voiceSource.Stop();
                voiceSource.PlayOneShot(clipToPlay, volume);
            }
        }
        else
        {
            Debug.LogWarning($"Attempted to play unknown voice key: {key}");
        }
    }
    // ---------------------------------------------------------------

    /// <summary>
    /// Plays a looping music track by its unique key. Stops any current music first.
    /// </summary>
    /// <param name="key">The keyName of the music track (e.g., 'Hangar BGM').</param>
    public void PlayMusic(string key)
    {
        if (soundMap.TryGetValue(key, out var data))
        {
            AudioClip[] clips = data.Item1;
            AudioMixerGroup group = data.Item2;
            float volume = data.Item3;

            if (clips.Length > 0)
            {
                // Stop any current music
                musicSource.Stop();

                // Select the first clip (or random if intended)
                musicSource.clip = clips[0];
                musicSource.outputAudioMixerGroup = group;
                musicSource.volume = volume;
                musicSource.loop = true;

                musicSource.Play();
            }
        }
        else
        {
            Debug.LogWarning($"Attempted to play unknown music key: {key}");
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    // --- PUBLIC VOLUME CONTROL METHOD ---

    /// <summary>
    /// Sets the volume for an exposed Audio Mixer parameter.
    /// </summary>
    /// <param name="parameterName">The exposed parameter name (e.g., 'MasterVolume').</param>
    /// <param name="normalizedValue">The value from 0 (silent) to 1 (max).</param>
    public void SetVolume(string parameterName, float normalizedValue)
    {
        if (masterMixer == null)
        {
            Debug.LogError("Master Mixer is not assigned in the SoundManager Inspector.");
            return;
        }

        // Convert the linear slider value (0 to 1) to a logarithmic mixer volume (-80dB to 0dB)
        float volume = Mathf.Log10(Mathf.Max(normalizedValue, 0.0001f)) * 20;

        masterMixer.SetFloat(parameterName, volume);
    }
}
