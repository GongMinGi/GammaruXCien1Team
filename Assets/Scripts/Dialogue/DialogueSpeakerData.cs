using System;
using UnityEngine;

/// <summary>
/// 한 대화 상대의 이름과 표정별 일러스트를 보관한다.
/// </summary>
[CreateAssetMenu(fileName = "DialogueSpeakerData", menuName = "Dialogue/Speaker Data")]
public class DialogueSpeakerData : ScriptableObject
{
    [Serializable]
    public struct SpeakerExpression
    {
        public string expressionName;
        public Sprite illustration;
    }

    [SerializeField] private string speakerName;
    [SerializeField] private SpeakerExpression[] expressions;

    public string SpeakerName { get { return speakerName; } }

    /// <summary>
    /// 표정 이름과 같은 일러스트를 반환한다. 이름이 비어 있으면 첫 번째 일러스트를 쓴다.
    /// </summary>
    public Sprite GetIllustration(string expressionName)
    {
        for (int i = 0; i < expressions.Length; i++)
        {
            if (expressions[i].expressionName == expressionName)
            {
                return expressions[i].illustration;
            }
        }

        return expressions[0].illustration;
    }
}
