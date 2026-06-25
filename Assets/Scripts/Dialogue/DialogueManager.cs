using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using Unity.VisualScripting; // 🛠️ LIBRERÍA DE TEXTMESHPRO OBLIGATORIA

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Componentes de UI (TextMeshPro)")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI nameText; // 🛠️ CAMBIADO A TMP
    [SerializeField] private TextMeshProUGUI bodyText; // 🛠️ CAMBIADO A TMP

    [Header("Ajustes")]
    [SerializeField] private float typingSpeed = 0.03f;

    private Queue<DialogueLine> linesQueue = new Queue<DialogueLine>();
    private bool isTyping = false;
    private string currentFullText = "";
    private Action onDialogueCompleteCallback;

    public DialogueLine TEST_DIALOGUE_1;
    public DialogueLine TEST_DIALOGUE_2;
    public DialogueLine TEST_DIALOGUE_3;


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        DialogueSequence sequence = new DialogueSequence();
        sequence.lines = new DialogueLine[3];
        sequence.lines[0] = TEST_DIALOGUE_1;
        sequence.lines[1] = TEST_DIALOGUE_2;
        sequence.lines[2] = TEST_DIALOGUE_3;

        StartDialogue(sequence);
    }

    void Update()
    {
        if (dialoguePanel.activeSelf && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            if (isTyping)
            {
                StopAllCoroutines();
                bodyText.text = currentFullText;
                isTyping = false;
            }
            else
            {
                DisplayNextLine();
            }
        }
    }

    public void StartDialogue(DialogueSequence dialogue, Action onComplete = null)
    {
        onDialogueCompleteCallback = onComplete;
        linesQueue.Clear();

        foreach (DialogueLine line in dialogue.lines)
        {
            linesQueue.Enqueue(line);
        }

        dialoguePanel.SetActive(true);
        ToggleGameGameplay(false);

        DisplayNextLine();
    }

    private void DisplayNextLine()
    {
        if (linesQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine currentLine = linesQueue.Dequeue();

        string nombreTraducido = "";
        string textoTraducido = "";

        if (LocalizationManager.Instance != null)
        {
            nombreTraducido = LocalizationManager.Instance.GetTranslation(currentLine.characterNameKey);
            textoTraducido = LocalizationManager.Instance.GetTranslation(currentLine.textKey);
        }
        else
        {
            Debug.LogError("[DialogueManager] ¡No se encontró la instancia de LocalizationManager!");
            nombreTraducido = currentLine.characterNameKey;
            textoTraducido = currentLine.textKey;
        }

        nameText.text = nombreTraducido;
        currentFullText = textoTraducido;

        StartCoroutine(TypeText(currentFullText));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        bodyText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            bodyText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        ToggleGameGameplay(true);

        onDialogueCompleteCallback?.Invoke();
    }

    private void ToggleGameGameplay(bool enable)
    {
        MouseManager mouseManager = FindFirstObjectByType<MouseManager>();
        if (mouseManager != null) mouseManager.enabled = enable;
    }
}