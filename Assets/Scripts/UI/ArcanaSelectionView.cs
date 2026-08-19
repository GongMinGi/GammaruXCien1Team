using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아르카나 카드 더미와 인벤토리, 카드 상세 정보를 화면에 표시한다.
/// </summary>
public class ArcanaSelectionView : MonoBehaviour
{
    [SerializeField] private GameObject selectionOverlay;
    [SerializeField] private RectTransform deckRoot;
    [SerializeField] private RectTransform inventoryRoot;
    [SerializeField] private ArcanaCardView arcanaCardPrefab;
    [SerializeField] private GameObject cardDetailPanel;
    [SerializeField] private Text detailTitleText;
    [SerializeField] private float fanWidth = 1080f;
    [SerializeField] private float fanVerticalDrop = 70f;
    [SerializeField] private float maximumFanAngle = 24f;

    /// <summary>
    /// 전달받은 아르카나 카드들을 하단에 부채꼴로 생성한다.
    /// </summary>
    public void CreateDeckCards(
        ArcanaData[] arcanaCards,
        Action<ArcanaCardView> leftClickHandler,
        Action<ArcanaCardView> rightClickHandler)
    {
        int cardCount = arcanaCards.Length;

        for (int cardIndex = 0; cardIndex < cardCount; cardIndex++)
        {
            ArcanaCardView cardView = Instantiate(
                arcanaCardPrefab,
                deckRoot,
                false);
            float normalizedPosition = 0f;

            if (cardCount > 1)
            {
                normalizedPosition = (float)cardIndex / (cardCount - 1) * 2f - 1f;
            }

            float positionX = normalizedPosition * fanWidth * 0.5f;
            float positionY = -normalizedPosition
                * normalizedPosition
                * fanVerticalDrop;
            float rotationZ = -normalizedPosition * maximumFanAngle;

            cardView.Initialize(
                arcanaCards[cardIndex],
                leftClickHandler,
                rightClickHandler);
            cardView.SetDeckLayout(
                new Vector2(positionX, positionY),
                rotationZ,
                cardIndex);
        }
    }

    /// <summary>
    /// 아르카나 선택 화면을 활성화한다.
    /// </summary>
    public void ShowSelection()
    {
        selectionOverlay.SetActive(true);
    }

    /// <summary>
    /// 클릭한 카드의 아르카나 번호와 이름을 우측 상세 영역에 표시한다.
    /// </summary>
    public void ShowCardDetails(ArcanaData arcanaData)
    {
        detailTitleText.text = arcanaData.DisplayNumber
            + "\n"
            + arcanaData.ArcanaName;
        cardDetailPanel.SetActive(true);
    }

    /// <summary>
    /// 클릭한 더미 카드를 상단 인벤토리 영역으로 옮긴다.
    /// </summary>
    public void MoveCardToInventory(ArcanaCardView arcanaCardView)
    {
        arcanaCardView.MoveToInventory(inventoryRoot);
    }

    /// <summary>
    /// 우클릭으로 선택을 취소한 카드를 하단 더미로 되돌린다.
    /// </summary>
    public void ReturnCardToDeck(ArcanaCardView arcanaCardView)
    {
        arcanaCardView.ReturnToDeck(deckRoot);
    }
}
