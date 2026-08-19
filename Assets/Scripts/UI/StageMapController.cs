using System.Collections;
using UnityEngine;

/// <summary>
/// 스테이지 위치와 진행 상태를 계산해 View에 전달한다.
/// </summary>
public class StageMapController : MonoBehaviour
{
    [SerializeField] private StageMapData stageMapData;
    [SerializeField] private StageMapView stageMapView;
    [SerializeField] private ConstellationView constellationView;
    [SerializeField] private ArcanaSelectionController arcanaSelectionController;
    [SerializeField] private float sidePadding = 180f;
    [SerializeField] private float verticalJitter = 170f;
    [SerializeField] private float verticalPadding = 70f;
    // 카드 테두리에서 이만큼 떨어진 곳부터 별자리가 시작한다.
    [SerializeField] private float cardEdgePadding = 40f;
    // 별자리가 직선이 아니라 완만한 호를 그리도록 경로 길이에 비례해 밀어내는 비율.
    [SerializeField] private float constellationArcRate = 0.14f;
    // 별이 일정한 간격으로 늘어서지 않도록 진행 비율을 흔드는 폭.
    [SerializeField] private float starPositionJitter = 0.07f;
    // 별을 경로 좌우로 흩뜨리는 폭.
    [SerializeField] private float starSideJitter = 10f;

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
    /// 모든 카드의 위치를 계산하고 잠금 해제된 카드와 별자리 선을 생성한다.
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
            constellationView.ShowLink(BuildConstellationPoints(i));
        }

        // 첫 스테이지는 클리어 때와 같은 연출로 등장하고, 나머지는 바로 표시한다.
        for (int i = 1; i < unlockedStageCount; i++)
        {
            stageMapView.CreateCard(
                i + 1,
                stagePositions[i],
                stageMapData.GetCardImage(i),
                OpenStage);
        }

        StartCoroutine(RevealStageCardRoutine(0));
    }

    /// <summary>
    /// 두 스테이지를 잇는 별들의 위치와 크기 단계를 계산한다.
    /// 경로는 카드 안쪽이 아니라 카드 바깥에서 시작하고 끝난다.
    /// </summary>
    private StarPoint[] BuildConstellationPoints(int fromStageIndex)
    {
        Vector2 fromCenter = stagePositions[fromStageIndex];
        Vector2 toCenter = stagePositions[fromStageIndex + 1];
        Vector2 unitDirection = (toCenter - fromCenter).normalized;
        Vector2 startPoint = fromCenter + GetCardEdgeOffset(unitDirection);
        Vector2 endPoint = toCenter - GetCardEdgeOffset(unitDirection);
        Vector2 pathDirection = endPoint - startPoint;
        float arcHeight = pathDirection.magnitude * constellationArcRate;
        Vector2 sideDirection = new Vector2(-unitDirection.y, unitDirection.x);
        int starCount = Mathf.Max(2, stageMapData.StarNodeCountPerLink);
        float arcSign = fromStageIndex % 2 == 0 ? 1f : -1f;
        StarPoint[] starPoints = new StarPoint[starCount];
        Random.State previousRandomState = Random.state;

        // 스테이지마다 고정된 모양이 나오도록 씨앗을 정하고, 다른 무작위 연출에
        // 영향을 주지 않도록 계산이 끝나면 원래 상태로 되돌린다.
        Random.InitState(fromStageIndex + 1);

        for (int i = 0; i < starCount; i++)
        {
            float rate = (float)i / (starCount - 1);
            float sideDistance = 0f;

            // 양 끝 별은 카드 옆에 정확히 붙이고, 가운데 별만 흩뜨린다.
            if (i > 0 && i < starCount - 1)
            {
                rate = Mathf.Clamp01(
                    rate + Random.Range(-starPositionJitter, starPositionJitter));
                sideDistance = Mathf.Sin(rate * Mathf.PI) * arcHeight * arcSign
                    + Random.Range(-starSideJitter, starSideJitter);
            }

            starPoints[i] = new StarPoint(
                startPoint + pathDirection * rate + sideDirection * sideDistance,
                GetStarNodeSize(i, starCount));
        }

        Random.state = previousRandomState;
        return starPoints;
    }

    /// <summary>
    /// 카드 중심에서 진행 방향으로 카드 테두리 바깥까지의 거리를 구한다.
    /// </summary>
    private Vector2 GetCardEdgeOffset(Vector2 unitDirection)
    {
        Vector2 halfCardSize = stageCardSize * 0.5f;
        float horizontalDistance = float.MaxValue;
        float verticalDistance = float.MaxValue;

        if (Mathf.Abs(unitDirection.x) > Mathf.Epsilon)
        {
            horizontalDistance =
                (halfCardSize.x + cardEdgePadding) / Mathf.Abs(unitDirection.x);
        }

        if (Mathf.Abs(unitDirection.y) > Mathf.Epsilon)
        {
            verticalDistance =
                (halfCardSize.y + cardEdgePadding) / Mathf.Abs(unitDirection.y);
        }

        return unitDirection * Mathf.Min(horizontalDistance, verticalDistance);
    }

    /// <summary>
    /// 카드 옆 별은 크게, 가운데 한 곳은 중간, 나머지는 작게 정한다.
    /// </summary>
    private StarNodeSize GetStarNodeSize(int starIndex, int starCount)
    {
        if (starIndex == 0 || starIndex == starCount - 1)
        {
            return StarNodeSize.Large;
        }

        if (starIndex == starCount / 2)
        {
            return StarNodeSize.Medium;
        }

        return StarNodeSize.Small;
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
    /// 현재 마지막 스테이지의 클리어를 시험하고 다음 카드와 별자리 선을 생성한다.
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

        StartCoroutine(OpenNextStageRoutine(StageProgressData.UnlockedStageCount - 1));
    }

    /// <summary>
    /// 별자리 선, 카드 테두리, 반짝임, 뒷면 등장을 순서대로 진행한다.
    /// </summary>
    private IEnumerator OpenNextStageRoutine(int nextStageIndex)
    {
        UpdateContentWidth(nextStageIndex + 1);
        yield return constellationView.PlayLinkRoutine(
            BuildConstellationPoints(nextStageIndex - 1),
            false);
        yield return RevealStageCardRoutine(nextStageIndex);
    }

    /// <summary>
    /// 카드 테두리를 그린 뒤 반짝임과 함께 카드 뒷면을 띄운다.
    /// </summary>
    private IEnumerator RevealStageCardRoutine(int stageIndex)
    {
        yield return constellationView.PlayLinkRoutine(
            BuildCardBorderPoints(stageIndex),
            true);

        StageCardView cardView = stageMapView.CreateCard(
            stageIndex + 1,
            stagePositions[stageIndex],
            stageMapData.GetCardImage(stageIndex),
            OpenStage);
        yield return cardView.PlayRevealRoutine();
    }

    /// <summary>
    /// 카드 테두리를 왼쪽 위부터 시계 방향으로 도는 꼭짓점을 계산한다.
    /// </summary>
    private StarPoint[] BuildCardBorderPoints(int stageIndex)
    {
        Vector2 cardCenter = stagePositions[stageIndex];
        Vector2 halfCardSize = stageCardSize * 0.5f;
        StarPoint[] cornerPoints = new StarPoint[4];

        cornerPoints[0] = new StarPoint(
            cardCenter + new Vector2(-halfCardSize.x, halfCardSize.y),
            StarNodeSize.Large);
        cornerPoints[1] = new StarPoint(
            cardCenter + new Vector2(halfCardSize.x, halfCardSize.y),
            StarNodeSize.Large);
        cornerPoints[2] = new StarPoint(
            cardCenter + new Vector2(halfCardSize.x, -halfCardSize.y),
            StarNodeSize.Large);
        cornerPoints[3] = new StarPoint(
            cardCenter + new Vector2(-halfCardSize.x, -halfCardSize.y),
            StarNodeSize.Large);
        return cornerPoints;
    }

    /// <summary>
    /// 조절한 별자리 값을 Play Mode에서 곧바로 확인하도록 다시 그린다.
    /// </summary>
    public void RebuildConstellationForTest()
    {
        if (stagePositions == null)
        {
            return;
        }

        int unlockedStageCount = Mathf.Min(
            StageProgressData.UnlockedStageCount,
            stageMapData.StageCount);

        constellationView.Clear();

        for (int i = 0; i < unlockedStageCount - 1; i++)
        {
            constellationView.ShowLink(BuildConstellationPoints(i));
        }
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
