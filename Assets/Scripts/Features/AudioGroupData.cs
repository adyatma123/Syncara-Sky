using UnityEngine;
using UnityEngine.Audio;
using System;

/// <summary>
/// Defines a top-level audio group (Music, SFX, Voice).
/// </summary>
[Serializable]
public class AudioGroupData
{
    [Tooltip("The unique name for this mixer group (e.g., 'SFX' or 'Music').")]
    public string groupName;

    [Tooltip("The Unity AudioMixerGroup this audio output will be routed through.")]
    public AudioMixerGroup mixerGroup;

    [Tooltip("All specific sound events within this group (e.g., 'HitSound', 'MissileLaunch').")]
    public SoundClipData[] soundClips;

    [Header("Fade Settings (Best used for Music)")]
    [Tooltip("If true, the volume will smoothly increase when playing a clip in this group.")]
    public bool useFadeIn = false;

    [Tooltip("If true, the volume will smoothly decrease before stopping a clip in this group.")]
    public bool useFadeOut = false;

    [Tooltip("The duration, in seconds, for the fade effect.")]
    [Range(0.1f, 5f)]
    public float fadeDuration = 1.0f;
}
