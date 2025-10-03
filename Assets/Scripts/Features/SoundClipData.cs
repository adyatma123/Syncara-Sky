using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Defines a single playable sound element (e.g., "Explosion") and holds its audio clips.
/// </summary>
[Serializable]
public class SoundClipData
{
    [Tooltip("The unique name used to play this sound (e.g., 'Shooting').")]
    public string keyName;

    [Tooltip("Volume multiplier specific to this sound effect (0 to 1).")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("Array of randomized clips for this sound event.")]
    public AudioClip[] clips;
}
