using UnityEngine;

/// <summary>
/// 스테이지 데이터를 화면 안의 카드 위치로 계산해 View에 전달한다.
/// </summary>
public class StageMapController : MonoBehaviour
{
    [SerializeField] private StageMapData stageMapData;
    [SerializeField] private StageMapView stageMapView;
    [SerializeField] private ArcanaSelectionController arcanaSelectionController;
    [SerializeField] private float sidePadding = 180f;
    [SerializeField] private float verticalJitter = 170f;
    [SerializeField] private float verticalPadding = 70f;

    /// <summary>
    /// UI 크기가 계산된 뒤 스테이지 맵을 생성한다.
    /// </summary>
    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        BuildMap();
    }

    /// <summary>
    /// 모든 카드의 위치를 계산하고 인접 카드의 연결선과 카드를 생성한다.
    /// </summary>
    private void BuildMap()
    {
        int stageCount = stageMapData.StageCount;
        Vector2 cardSize = stageMapView.GetCardSize();
        Vector2[] positions = new Vector2[stageCount];
        float contentWidth = sidePadding * 2f;
        float availableY = stageMapView.GetViewportHeight() * 0.5f
            - cardSize.y * 0.5f
            - verticalPadding;
        float yRange = Mathf.Max(0f, Mathf.Min(verticalJitter, availableY));

        if (stageCount > 0)
        {
            contentWidth += cardSize.x
                + (stageCount - 1) * stageMapData.HorizontalSpacing;
        }

        stageMapView.SetContentWidth(contentWidth);

        for (int i = 0; i < stageCount; i++)
        {
            float x = sidePadding + cardSize.x * 0.5f
                + i * stageMapData.HorizontalSpacing;
            float y = stageMapData.GetVerticalRate(i) * yRange;
            positions[i] = new Vector2(x, y);
        }

        for (int i = 0; i < stageCount - 1; i++)
        {
            stageMapView.CreateLine(positions[i], positions[i + 1]);
        }

        for (int i = 0; i < stageCount; i++)
        {
            stageMapView.CreateCard(
                i + 1,
                positions[i],
                arcanaSelectionController.OpenArcanaSelection);
        }
    }
}
