using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 강조할 UI만 남기고 화면을 덮개 네 장으로 가리고 안내 문구를 표시한다.
/// </summary>
public class TutorialView : MonoBehaviour
{
    [SerializeField] private GameObject tutorialRoot;
    [SerializeField] private RectTransform leftCover;
    [SerializeField] private RectTransform rightCover;
    [SerializeField] private RectTransform topCover;
    [SerializeField] private RectTransform bottomCover;
    // 강조한 UI까지 눌리지 않게 막는 투명 판이다.
    [SerializeField] private GameObject clickBlocker;
    [SerializeField] private Text descriptionText;

    public void Open()
    {
        tutorialRoot.SetActive(true);
    }

    public void Close()
    {
        tutorialRoot.SetActive(false);
    }

    /// <summary>
    /// 강조할 UI의 화면 위치를 구해 덮개를 그 바깥으로 물리고 안내 문구를 바꾼다.
    /// 클릭으로 넘기는 단계에서는 강조한 UI까지 눌리지 않도록 막는다.
    /// </summary>
    public void ShowStep(
        RectTransform highlightTarget,
        string description,
        bool blocksHighlightClick)
    {
        Vector3[] targetCorners = new Vector3[4];
        highlightTarget.GetWorldCorners(targetCorners);

        Vector2 targetMin = new Vector2(
            targetCorners[0].x / Screen.width,
            targetCorners[0].y / Screen.height);
        Vector2 targetMax = new Vector2(
            targetCorners[2].x / Screen.width,
            targetCorners[2].y / Screen.height);

        SetCoverArea(leftCover, new Vector2(0f, 0f), new Vector2(targetMin.x, 1f));
        SetCoverArea(rightCover, new Vector2(targetMax.x, 0f), new Vector2(1f, 1f));
        SetCoverArea(topCover, new Vector2(targetMin.x, targetMax.y), new Vector2(targetMax.x, 1f));
        SetCoverArea(bottomCover, new Vector2(targetMin.x, 0f), new Vector2(targetMax.x, targetMin.y));

        descriptionText.text = description;
        clickBlocker.SetActive(blocksHighlightClick);
    }

    /// <summary>
    /// 덮개 한 장이 화면에서 차지할 영역을 화면 비율로 정한다.
    /// </summary>
    private void SetCoverArea(RectTransform cover, Vector2 areaMin, Vector2 areaMax)
    {
        cover.anchorMin = areaMin;
        cover.anchorMax = areaMax;
        cover.offsetMin = Vector2.zero;
        cover.offsetMax = Vector2.zero;
    }
}
