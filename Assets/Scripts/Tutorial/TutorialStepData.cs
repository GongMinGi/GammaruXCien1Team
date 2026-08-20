using System;
using UnityEngine;

/// <summary>
/// 튜토리얼 한 단계에서 강조할 UI와 안내 문구, 기다릴 동작 이름이다.
/// </summary>
[Serializable]
public class TutorialStepData
{
    public RectTransform highlightTarget;
    [TextArea(2, 4)] public string descriptionText;
    public string waitingActionName;
    // 기다릴 동작 없이 아무 곳이나 클릭하면 넘어가는 단계인지 정한다.
    public bool completesOnClick;
}
