using UnityEngine;

/// <summary>
/// Scriptable Object yang menyimpan satu item Trivia (Nama dan Konten).
/// Digunakan untuk membuat aset trivia yang dapat diedit di Unity Editor.
/// </summary>
[CreateAssetMenu(fileName = "NewTriviaData", menuName = "Trivia/New Trivia Item")]
public class TriviaData : ScriptableObject
{
    [Tooltip("Nama/Judul singkat untuk trivia (opsional, untuk organisasi).")]
    public string TriviaName;

    [Tooltip("Konten lengkap dari trivia yang akan ditampilkan di layar loading.")]
    [TextArea(3, 10)] // Membuat field input yang lebih besar di Inspector
    public string TriviaContent;
}