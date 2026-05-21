using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections; // Required for Coroutines
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

    // The tuple now stores group data to easily access fade settings
    private Dictionary<string, (AudioClip[], AudioMixerGroup, float, AudioGroupData)> soundMap;
    private Dictionary<string, AudioGroupData> groupMap; // New map for quick group lookup
    private AudioSource sfxSource;
    private AudioSource musicSource;
    private AudioSource voiceSource;

    // Stores the current music fade coroutine to prevent starting multiple fades
    private Coroutine musicFadeCoroutine;

    // NEW: Public properties for debug overlay
    // If the source is playing, it returns the clip name; otherwise, it returns "None".
    public string CurrentMusicName => musicSource.isPlaying ? musicSource.clip?.name ?? "Playing (Name N/A)" : "None";
    public string CurrentVoiceName => voiceSource.isPlaying ? voiceSource.clip?.name ?? "Playing (Name N/A)" : "None";


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

        // Source dedicated to VOICE (allows voice to queue/interrupt without affecting SFX)
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.loop = false;
        voiceSource.playOnAwake = false;
    }

    /// <summary>
    /// Builds a fast-lookup dictionary for all sound clips based on their keyName.
    /// Also builds a map for quick AudioGroupData lookup.
    /// </summary>
    private void BuildSoundMap()
    {
        // Tuple format: (Clips, MixerGroup, Volume, ParentGroupData)
        soundMap = new Dictionary<string, (AudioClip[], AudioMixerGroup, float, AudioGroupData)>();
        groupMap = new Dictionary<string, AudioGroupData>();

        foreach (var group in audioGroups)
        {
            if (group.mixerGroup == null)
            {
                Debug.LogError($"Audio Group '{group.groupName}' is missing its AudioMixerGroup assignment!");
                continue;
            }

            // Store the group data for quick access
            groupMap[group.groupName] = group;

            foreach (var clipData in group.soundClips)
            {
                if (soundMap.ContainsKey(clipData.keyName))
                {
                    Debug.LogWarning($"Duplicate audio key found: {clipData.keyName}. Ignoring duplicate.");
                    continue;
                }

                soundMap[clipData.keyName] = (clipData.clips, group.mixerGroup, clipData.volume, group);
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
        // Item4 is the full AudioGroupData, but we don't use it for standard SFX one-shots
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

    /// <summary>
    /// Plays a voice clip using the dedicated voice source.
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
                voiceSource.clip = clipToPlay; // Store the clip for the debug accessor
                voiceSource.PlayOneShot(clipToPlay, volume);
            }
        }
        else
        {
            Debug.LogWarning($"Attempted to play unknown voice key: {key}");
        }
    }

    /// <summary>
    /// Plays a looping music track by its unique key, with optional fade-in.
    /// Stops any current music first, potentially with a fade-out.
    /// </summary>
    /// <param name="key">The keyName of the music track (e.g., 'Hangar BGM').</param>
    public void PlayMusic(string key)
    {
        if (soundMap.TryGetValue(key, out var data))
        {
            AudioClip[] clips = data.Item1;
            AudioMixerGroup group = data.Item2;
            float volume = data.Item3;
            AudioGroupData groupData = data.Item4;

            if (clips.Length > 0)
            {
                // 1. Stop any existing music transition
                if (musicFadeCoroutine != null)
                {
                    StopCoroutine(musicFadeCoroutine);
                }

                // 2. Configure the AudioSource
                musicSource.clip = clips[0]; // Store the clip for the debug accessor
                musicSource.outputAudioMixerGroup = group;
                musicSource.loop = true;

                // 3. Handle Fade-In or instant play
                if (groupData.useFadeIn)
                {
                    musicSource.volume = 0f; // Start at 0 volume
                    musicSource.Play();
                    musicFadeCoroutine = StartCoroutine(FadeVolume(musicSource, groupData.fadeDuration, volume, null));
                }
                else
                {
                    musicSource.volume = volume; // Instant max volume
                    musicSource.Play();
                }
            }
        }
        else
        {
            Debug.LogWarning($"Attempted to play unknown music key: {key}");
        }
    }

    /// <summary>
    /// Stops the music track, with optional fade-out.
    /// </summary>
    public void StopMusic()
    {
        if (musicSource.isPlaying && musicSource.clip != null)
        {
            // Find the AudioGroupData for the currently playing music
            AudioGroupData currentGroup = audioGroups.FirstOrDefault(g =>
                g.mixerGroup == musicSource.outputAudioMixerGroup &&
                g.soundClips.Any(sc => sc.clips.Contains(musicSource.clip)));

            if (currentGroup != null && currentGroup.useFadeOut)
            {
                // Stop any existing transition and start the fade-out
                if (musicFadeCoroutine != null)
                {
                    StopCoroutine(musicFadeCoroutine);
                }
                // Fade to 0, and stop the musicSource when done
                musicFadeCoroutine = StartCoroutine(FadeVolume(musicSource, currentGroup.fadeDuration, 0f, StopMusicSource));
            }
            else
            {
                // Instant stop
                musicSource.Stop();
            }
        }
    }

    /// <summary>
    /// Callback function to physically stop the AudioSource after a fade-out is complete.
    /// </summary>
    private void StopMusicSource()
    {
        musicSource.Stop();
        musicSource.clip = null; // Clear clip name for debug accessor
    }

    public void StopAllAudio()
    {
        // Stop coroutines first
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = null;
        }

        // Stop every source instantly
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }

        if (sfxSource != null)
        {
            sfxSource.Stop();
        }

        if (voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.clip = null;
        }
    }

    /// <summary>
    /// Pause all currently playing audio.
    /// </summary>
    public void PauseAllAudio()
    {
        AudioListener.pause = true;
    }

    /// <summary>
    /// Resume all paused audio.
    /// </summary>
    public void ResumeAllAudio()
    {
        AudioListener.pause = false;
    }

    // --- COROUTINE FOR FADING ---

    /// <summary>
    /// Coroutine to smoothly fade an AudioSource's volume over time.
    /// </summary>
    /// <param name="source">The AudioSource to fade.</param>
    /// <param name="duration">The duration of the fade in seconds.</param>
    /// <param name="targetVolume">The target volume (0.0 to 1.0).</param>
    /// <param name="onComplete">Action to execute once the fade is complete (optional).</param>
    private IEnumerator FadeVolume(AudioSource source, float duration, float targetVolume, System.Action onComplete)
    {
        float startVolume = source.volume;
        float startTime = Time.time;

        while (Time.time < startTime + duration)
        {
            float elapsed = Time.time - startTime;
            float t = elapsed / duration;
            source.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        // Ensure volume is exactly the target at the end
        source.volume = targetVolume;

        // Execute callback if provided
        onComplete?.Invoke();
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
