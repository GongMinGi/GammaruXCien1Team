using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

/// <summary>
/// 타이틀을 제외한 모든 씬에서 대화를 재생하는 창구다.
/// 마우스 클릭을 받아 대사 출력을 끝내거나 다음 대사로 넘긴다.
/// </summary>
public class DialogueController : MonoBehaviour
{
    private const string PrefabResourcePath = "DialogueManager";

    public static DialogueController Instance;

    [SerializeField] private DialogueView dialogueView;

    private DialogueData playingDialogueData;
    private int currentLineIndex;
    private UnityAction dialogueFinishedHandler;

    /// <summary>
    /// 어떤 씬에서 게임을 시작하든 대화 창구가 존재하도록 자동으로 생성한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstance()
    {
        GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
        Instantiate(prefab);
    }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        dialogueView.Close();
    }

    /// <summary>
    /// 대화를 처음부터 재생하고, 마지막 대사까지 끝나면 지정한 처리를 실행한다.
    /// </summary>
    public void PlayDialogue(DialogueData dialogueData, UnityAction finishedHandler)
    {
        playingDialogueData = dialogueData;
        dialogueFinishedHandler = finishedHandler;
        currentLineIndex = 0;
        dialogueView.Open();
        ShowCurrentLine();
    }

    /// <summary>
    /// 출력 중이면 대사를 한 번에 띄우고, 아니면 다음 대사로 넘어간다.
    /// </summary>
    private void Update()
    {
        if (playingDialogueData == null
            || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (dialogueView.IsTyping)
        {
            dialogueView.CompleteTyping();
            return;
        }

        currentLineIndex++;

        if (currentLineIndex < playingDialogueData.LineCount)
        {
            ShowCurrentLine();
            return;
        }

        FinishDialogue();
    }

    private void ShowCurrentLine()
    {
        DialogueData.DialogueLine line = playingDialogueData.GetLine(currentLineIndex);

        dialogueView.ShowLine(
            line.backgroundIllustration,
            line.speaker.GetIllustration(line.expressionName),
            line.hidesSpeakerIllustration,
            line.speaker.SpeakerName,
            line.text);
    }

    /// <summary>
    /// 대화창을 닫고 대화가 끝난 뒤 할 일을 실행한다.
    /// </summary>
    private void FinishDialogue()
    {
        playingDialogueData = null;
        dialogueView.Close();

        if (dialogueFinishedHandler != null)
        {
            dialogueFinishedHandler.Invoke();
        }
    }
}
