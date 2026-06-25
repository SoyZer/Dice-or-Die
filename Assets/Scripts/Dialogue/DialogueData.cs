using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [Tooltip("Clave de localización para el nombre del personaje (Ej: CHAR_CROUPIER)")]
    public string characterNameKey;

    [Tooltip("Clave de localización para el texto del diálogo (Ej: DIA_INTRO_01)")]
    public string textKey;
}

[System.Serializable]
public class DialogueSequence
{
    public DialogueLine[] lines;
}