using UnityEngine;

/// <summary>
/// Stores a single line of dialogue text and references the profile 
/// of the character speaking that line.
/// This asset is created via: Assets/Create/Dialogue System/Dialogue Text
/// </summary>
[CreateAssetMenu(fileName = "NewLine", menuName = "Dialogue System/Dialogue Text", order = 2)]
public class DialogueTextSO : ScriptableObject
{
    [Header("Speaker Reference")]
    [Tooltip("The ScriptableObject defining the speaker's profile.")]
    // This is the reference to the first SO you created.
    public DialogueProfileSO speakerProfile;

    [Header("Dialogue Content")]
    [Tooltip("The actual text to be displayed in the text box.")]
    [TextArea(3, 10)] // Makes the input field larger in the Inspector
    public string dialogueText;
}
