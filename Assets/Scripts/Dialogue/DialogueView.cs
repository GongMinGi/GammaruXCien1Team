using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 좌측 일러스트와 하단 대화창을 표시하고 대사를 한 자씩 출력한다.
/// </summary>
public class DialogueView : MonoBehaviour
{
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private Image backgroundIllustration;
    [SerializeField] private Image speakerIllustration;
    [SerializeField] private Text speakerNameText;
    [SerializeField] private Text dialogueBodyText;
    // 글자 하나가 출력되는 데 걸리는 시간이다.
    [SerializeField] private float secondsPerCharacter = 0.04f;

    private Tween typingTween;
    private bool isTyping;

    public bool IsTyping { get { return isTyping; } }

    public void Open()
    {
        dialogueRoot.SetActive(true);
    }

    public void Close()
    {
        dialogueRoot.SetActive(false);
    }

    /// <summary>
    /// 일러스트와 이름을 바꾸고 대사 본문을 한 자씩 출력한다.
    /// 배경 삽화와 캐릭터 일러스트는 대사마다 켜고 끌 수 있다.
    /// </summary>
    public void ShowLine(
        Sprite background,
        Sprite illustration,
        bool hidesIllustration,
        string speakerName,
        string bodyText)
    {
        backgroundIllustration.sprite = background;
        backgroundIllustration.gameObject.SetActive(background != null);
        speakerIllustration.sprite = illustration;
        speakerIllustration.gameObject.SetActive(!hidesIllustration);
        speakerNameText.text = speakerName;
        dialogueBodyText.text = "";
        isTyping = true;

        typingTween = dialogueBodyText
            .DOText(bodyText, bodyText.Length * secondsPerCharacter)
            .SetEase(Ease.Linear)
            .OnComplete(EndTyping);
    }

    /// <summary>
    /// 출력 중인 대사를 즉시 전부 표시한다.
    /// </summary>
    public void CompleteTyping()
    {
        typingTween.Complete();
    }

    private void EndTyping()
    {
        isTyping = false;
    }
}
