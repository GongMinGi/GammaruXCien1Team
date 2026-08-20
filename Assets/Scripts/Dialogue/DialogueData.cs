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
        public string expressionName;
        [TextArea(2, 5)] public string text;
    }

    [SerializeField] private DialogueLine[] lines;

    public int LineCount { get { return lines.Length; } }

    public DialogueLine GetLine(int lineIndex)
    {
        return lines[lineIndex];
    }
}
