using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 계산된 위치에 스테이지 카드를 표시한다.
/// </summary>
public class StageMapView : MonoBehaviour
{
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform cardsRoot;
    [SerializeField] private RectTransform cardPrefab;
    [SerializeField] private float focusDuration = 0.35f;

    /// <summary>
    /// 지정한 콘텐츠 좌표가 화면 가운데 오도록 스크롤을 옮긴다.
    /// </summary>
    public void FocusOn(Vector2 contentPosition)
    {
        float viewportWidth = viewport.rect.width;
        float targetX = Mathf.Clamp(
            viewportWidth * 0.5f - contentPosition.x,
            viewportWidth - content.rect.width,
            0f);

        content.DOKill();
        content.DOAnchorPosX(targetX, focusDuration).SetEase(Ease.OutSine);
    }

    /// <summary>
    /// 스테이지 카드가 표시되는 영역의 높이를 반환한다.
    /// </summary>
    public float GetViewportHeight()
    {
        return viewport.rect.height;
    }

    /// <summary>
    /// 카드 프리팹의 크기를 반환한다.
    /// </summary>
    public Vector2 GetCardSize()
    {
        return cardPrefab.rect.size;
    }

    /// <summary>
    /// 모든 카드를 담을 수 있도록 스크롤 콘텐츠의 너비를 설정한다.
    /// </summary>
    public void SetContentWidth(float width)
    {
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    /// <summary>
    /// 지정한 번호와 위치, 클릭 처리 메서드로 스테이지 카드를 생성한다.
    /// </summary>
    public StageCardView CreateCard(
        int stageNumber,
        Vector2 position,
        Sprite cardImage,
        UnityAction<int> stageOpenHandler)
    {
        RectTransform card = Instantiate(cardPrefab, cardsRoot, false);
        StageCardView cardView = card.GetComponent<StageCardView>();

        card.anchoredPosition = position;
        cardView.Initialize(stageNumber, cardImage, stageOpenHandler);
        return cardView;
    }
}
