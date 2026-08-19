using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하단 아르카나 카드 더미의 행 배치 방식을 나타낸다.
/// </summary>
public enum ArcanaDeckRowLayout
{
    OneRow = 1,
    TwoRows = 2
}

/// <summary>
/// 아르카나 카드 더미와 인벤토리, 카드 상세 정보를 화면에 표시한다.
/// </summary>
public class ArcanaSelectionView : MonoBehaviour
{
    private const string BookFlipAnimationStateName = "BookFlipAnim";
    private const int TwoRowReferenceCardCount = 5;
    private const int SelectedCardsPerRow = 5;

    [SerializeField] private GameObject selectionOverlay;
    [SerializeField] private RectTransform deckRoot;
    [SerializeField] private RectTransform selectedCardTopRow;
    [SerializeField] private RectTransform selectedCardBottomRow;
    [SerializeField] private ArcanaCardView arcanaCardPrefab;
    [SerializeField] private GameObject cardDetailPanel;
    [SerializeField] private Text detailTitleText;
    [SerializeField] private Text detailEffectText;
    [SerializeField] private Animator bookFlipAnimator;
    [SerializeField] private AnimationClip bookFlipAnimationClip;
    [SerializeField] private Button battleStartButton;
    [SerializeField] private Sprite crystalBallOnSprite;
    [SerializeField] private Sprite crystalBallOffSprite;
    [SerializeField] private float fanWidth = 1080f;
    [SerializeField] private float fanVerticalDrop = 70f;
    [SerializeField] private float maximumFanAngle = 24f;
    [SerializeField] private float twoRowFanWidth = 600f;
    [SerializeField] private float twoRowFanVerticalDrop = 28f;
    [SerializeField] private float twoRowMaximumFanAngle = 16f;
    [SerializeField] private float deckRowVerticalSpacing = 144f;

    /// <summary>
    /// 전달받은 아르카나 카드들을 설정한 행 수에 맞춰 부채꼴로 생성한다.
    /// </summary>
    public void CreateDeckCards(
        ArcanaData[] arcanaCards,
        ArcanaDeckRowLayout deckRowLayout,
        Action<ArcanaCardView> leftClickHandler,
        Action<ArcanaCardView> rightClickHandler)
    {
        int cardCount = arcanaCards.Length;
        int cardsPerRow = cardCount;
        float currentFanWidth = fanWidth;
        float currentFanVerticalDrop = fanVerticalDrop;
        float currentMaximumFanAngle = maximumFanAngle;

        if (deckRowLayout == ArcanaDeckRowLayout.TwoRows)
        {
            cardsPerRow = (cardCount + 1) / 2;
            currentFanWidth = twoRowFanWidth;
            currentFanVerticalDrop = twoRowFanVerticalDrop;
            currentMaximumFanAngle = twoRowMaximumFanAngle;
        }

        for (int cardIndex = 0; cardIndex < cardCount; cardIndex++)
        {
            ArcanaCardView cardView = Instantiate(
                arcanaCardPrefab,
                deckRoot,
                false);
            int rowIndex = cardIndex / cardsPerRow;
            int indexInCurrentRow = cardIndex % cardsPerRow;
            int cardsBeforeCurrentRow = rowIndex * cardsPerRow;
            int cardsInCurrentRow = Mathf.Min(
                cardsPerRow,
                cardCount - cardsBeforeCurrentRow);
            float currentRowFanWidth = currentFanWidth;
            float normalizedPosition = 0f;

            if (deckRowLayout == ArcanaDeckRowLayout.TwoRows)
            {
                currentRowFanWidth = Mathf.Min(
                    fanWidth,
                    twoRowFanWidth
                    * (cardsInCurrentRow - 1)
                    / (TwoRowReferenceCardCount - 1));
            }

            if (cardsInCurrentRow > 1)
            {
                normalizedPosition = (float)indexInCurrentRow
                    / (cardsInCurrentRow - 1)
                    * 2f
                    - 1f;
            }

            float positionX = normalizedPosition * currentRowFanWidth * 0.5f;
            float positionY = -rowIndex
                * deckRowVerticalSpacing
                - normalizedPosition
                * normalizedPosition
                * currentFanVerticalDrop;
            float rotationZ = -normalizedPosition * currentMaximumFanAngle;

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
    /// 클릭한 카드 정보를 준비하고 책장 애니메이션을 재생한다.
    /// </summary>
    public void ShowCardDetails(ArcanaData arcanaData)
    {
        StopAllCoroutines();
        detailTitleText.text = arcanaData.DisplayNumber
            + "\n"
            + arcanaData.ArcanaName;
        detailEffectText.text = arcanaData.EffectDescription;
        cardDetailPanel.SetActive(false);
        bookFlipAnimator.gameObject.SetActive(true);
        bookFlipAnimator.Play(BookFlipAnimationStateName, 0, 0f);
        StartCoroutine(ShowCardDetailsAfterBookFlip());
    }

    /// <summary>
    /// 책장 애니메이션이 끝나면 책을 숨기고 카드 상세 정보를 표시한다.
    /// </summary>
    private IEnumerator ShowCardDetailsAfterBookFlip()
    {
        yield return new WaitForSeconds(bookFlipAnimationClip.length);

        bookFlipAnimator.gameObject.SetActive(false);
        cardDetailPanel.SetActive(true);
    }

    /// <summary>
    /// 클릭한 더미 카드를 최대 5장씩 나뉜 선택 카드 행으로 옮긴다.
    /// </summary>
    public void MoveCardToInventory(ArcanaCardView arcanaCardView)
    {
        RectTransform targetRow = selectedCardTopRow;

        if (selectedCardTopRow.childCount >= SelectedCardsPerRow)
        {
            targetRow = selectedCardBottomRow;
        }

        arcanaCardView.MoveToInventory(targetRow);
    }

    /// <summary>
    /// 우클릭으로 선택을 취소한 카드를 더미로 되돌리고 두 행을 다시 채운다.
    /// </summary>
    public void ReturnCardToDeck(ArcanaCardView arcanaCardView)
    {
        arcanaCardView.ReturnToDeck(deckRoot);

        if (selectedCardTopRow.childCount < SelectedCardsPerRow
            && selectedCardBottomRow.childCount > 0)
        {
            selectedCardBottomRow
                .GetChild(0)
                .SetParent(selectedCardTopRow, false);
        }
    }

    /// <summary>
    /// 전투 시작 버튼의 상호작용 가능 상태와 수정구 스프라이트를 설정한다.
    /// </summary>
    public void SetBattleStartButtonInteractable(bool isInteractable)
    {
        battleStartButton.interactable = isInteractable;

        if (isInteractable)
        {
            battleStartButton.image.sprite = crystalBallOnSprite;
        }
        else
        {
            battleStartButton.image.sprite = crystalBallOffSprite;
        }
    }
}
