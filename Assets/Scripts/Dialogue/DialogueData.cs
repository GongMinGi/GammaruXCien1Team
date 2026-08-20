using System;
using UnityEngine;

/// <summary>
/// 한 장면에서 순서대로 재생할 대사 묶음이다.
/// </summary>
[CreateAssetMenu(fileName = "DialogueData", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Serializable]
    public struct DialogueLine
    {
        public DialogueSpeakerData speaker;
        // 비워두면 배경 삽화 없이 대사만 표시한다.
        public Sprite backgroundIllustration;
        public string expressionName;
        // 체크하면 이 대사에서는 캐릭터 일러스트를 감춘다.
        public bool hidesSpeakerIllustration;
        [TextArea(2, 5)] public string text;
    }

    [SerializeField] private DialogueLine[] lines;

    public int LineCount { get { return lines.Length; } }

    public DialogueLine GetLine(int lineIndex)
    {
        return lines[lineIndex];
    }
}
