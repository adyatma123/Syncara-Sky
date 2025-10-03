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
}
