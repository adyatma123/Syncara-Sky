using UnityEngine;

// Enum for character disposition
public enum CharacterDisposition { Friendly, Enemy, Neutral }

/// <summary>
/// Defines the speaking character's profile (name, faction, disposition).
/// This asset is created via: Assets/Create/Dialogue System/Dialogue Profile
/// </summary>
[CreateAssetMenu(fileName = "NewProfile", menuName = "Dialogue System/Dialogue Profile", order = 1)]
public class DialogueProfileSO : ScriptableObject
{
    [Header("Profile Identity")]
    [Tooltip("The name of the character speaking.")]
    public string profileName = "New Character";

    [Tooltip("The general attitude of the character toward the player/protagonist.")]
    public CharacterDisposition disposition = CharacterDisposition.Neutral;

    [Tooltip("The group or organization the character belongs to.")]
    public string faction = "None";

    [Header("Visuals (Optional)")]
    [Tooltip("Optional image or portrait of the character.")]
    public Sprite characterPortrait;
}
