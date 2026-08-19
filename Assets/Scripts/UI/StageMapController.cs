using UnityEngine;

/// <summary>
/// 스테이지 위치와 진행 상태를 계산해 View에 전달한다.
/// </summary>
public class StageMapController : MonoBehaviour
{
    [SerializeField] private StageMapData stageMapData;
    [SerializeField] private StageMapView stageMapView;
    [SerializeField] private ArcanaSelectionController arcanaSelectionController;
    [SerializeField] private float sidePadding = 180f;
    [SerializeField] private float verticalJitter = 170f;
    [SerializeField] private float verticalPadding = 70f;

    private Vector2[] stagePositions;
    private Vector2 stageCardSize;

    /// <summary>
    /// UI 크기가 계산된 뒤 스테이지 맵을 생성한다.
    /// </summary>
    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        BuildMap();
    }

    /// <summary>
    /// 모든 카드의 위치를 계산하고 잠금 해제된 카드와 연결선을 생성한다.
    /// </summary>
    private void BuildMap()
    {
        int stageCount = stageMapData.StageCount;
        int unlockedStageCount = Mathf.Min(
            StageProgressData.UnlockedStageCount,
            stageCount);
        stageCardSize = stageMapView.GetCardSize();
        stagePositions = new Vector2[stageCount];
        float availableY = stageMapView.GetViewportHeight() * 0.5f
            - stageCardSize.y * 0.5f
            - verticalPadding;
        float yRange = Mathf.Max(0f, Mathf.Min(verticalJitter, availableY));

        UpdateContentWidth(unlockedStageCount);

        for (int i = 0; i < stageCount; i++)
        {
            float x = sidePadding + stageCardSize.x * 0.5f
                + i * stageMapData.HorizontalSpacing;
            float y = stageMapData.GetVerticalRate(i) * yRange;
            stagePositions[i] = new Vector2(x, y);
        }

        for (int i = 0; i < unlockedStageCount - 1; i++)
        {
            stageMapView.CreateLine(stagePositions[i], stagePositions[i + 1]);
        }

        for (int i = 0; i < unlockedStageCount; i++)
        {
            stageMapView.CreateCard(
                i + 1,
                stagePositions[i],
                OpenStage);
        }
    }

    /// <summary>
    /// 선택한 스테이지를 저장하고 아르카나 선택창을 연다.
    /// </summary>
    private void OpenStage(int stageNumber)
    {
        StageProgressData.SelectStage(stageNumber, stageMapData.StageCount);
        arcanaSelectionController.OpenArcanaSelection();
    }

    /// <summary>
    /// 현재 마지막 스테이지의 클리어를 시험하고 다음 카드와 연결선을 생성한다.
    /// </summary>
    public void CompleteLatestStageForTest()
    {
        int unlockedStageCount = Mathf.Min(
            StageProgressData.UnlockedStageCount,
            stageMapData.StageCount);

        StageProgressData.SelectStage(
            unlockedStageCount,
            stageMapData.StageCount);

        if (!StageProgressData.CompleteSelectedStage())
        {
            return;
        }

        int nextStageIndex = StageProgressData.UnlockedStageCount - 1;
        UpdateContentWidth(StageProgressData.UnlockedStageCount);
        stageMapView.CreateLine(
            stagePositions[nextStageIndex - 1],
            stagePositions[nextStageIndex]);
        stageMapView.CreateCard(
            nextStageIndex + 1,
            stagePositions[nextStageIndex],
            OpenStage);
    }

    /// <summary>
    /// 표시된 스테이지를 담을 수 있도록 스크롤 콘텐츠의 너비를 갱신한다.
    /// </summary>
    private void UpdateContentWidth(int displayedStageCount)
    {
        float contentWidth = sidePadding * 2f;

        if (displayedStageCount > 0)
        {
            contentWidth += stageCardSize.x
                + (displayedStageCount - 1) * stageMapData.HorizontalSpacing;
        }

        stageMapView.SetContentWidth(contentWidth);
    }
}
