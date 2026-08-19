using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 계산된 위치에 스테이지 카드와 연결선을 표시한다.
/// </summary>
public class StageMapView : MonoBehaviour
{
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform linesRoot;
    [SerializeField] private RectTransform cardsRoot;
    [SerializeField] private RectTransform cardPrefab;
    [SerializeField] private RectTransform linePrefab;

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
    /// 두 카드 중심을 잇는 UI 연결선을 생성한다.
    /// </summary>
    public void CreateLine(Vector2 start, Vector2 end)
    {
        RectTransform line = Instantiate(linePrefab, linesRoot, false);
        Vector2 direction = end - start;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        line.anchoredPosition = (start + end) * 0.5f;
        line.sizeDelta = new Vector2(direction.magnitude, line.sizeDelta.y);
        line.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    /// <summary>
    /// 지정한 번호와 위치, 클릭 처리 메서드로 스테이지 카드를 생성한다.
    /// </summary>
    public void CreateCard(
        int stageNumber,
        Vector2 position,
        UnityAction cardClickHandler)
    {
        RectTransform card = Instantiate(cardPrefab, cardsRoot, false);
        Text numberText = card.GetComponentInChildren<Text>(true);
        Button cardButton = card.GetComponent<Button>();

        card.anchoredPosition = position;
        numberText.text = stageNumber.ToString();
        cardButton.onClick.AddListener(cardClickHandler);
    }
}
