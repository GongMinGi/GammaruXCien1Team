using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 한 장의 아르카나 카드 정보를 표시하고 포인터 입력을 전달한다.
/// </summary>
public class ArcanaCardView : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [SerializeField] private RectTransform cardRectTransform;
    [SerializeField] private Text arcanaNumberText;
    [SerializeField] private Text arcanaNameText;
    [SerializeField] private Text cardCostText;
    [SerializeField] private float hoverRise = 36f;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float hoverDuration = 0.15f;

    private ArcanaData cardData;
    private Action<ArcanaCardView> leftClickHandler;
    private Action<ArcanaCardView> rightClickHandler;
    private Vector2 deckAnchorMin;
    private Vector2 deckAnchorMax;
    private Vector2 deckPosition;
    private Vector3 deckScale;
    private Quaternion deckRotation;
    private int deckOrder;
    private int deckSiblingIndex;
    private bool isInDeck;
    private bool isDeckHovered;

    /// <summary>
    /// 이 View가 표시하는 아르카나 데이터를 반환한다.
    /// </summary>
    public ArcanaData CardData
    {
        get { return cardData; }
    }

    /// <summary>
    /// 카드가 하단 더미에 있는지 반환한다.
    /// </summary>
    public bool IsInDeck
    {
        get { return isInDeck; }
    }

    /// <summary>
    /// 카드 데이터와 좌우 클릭 처리 메서드를 연결하고 앞면 텍스트를 표시한다.
    /// </summary>
    public void Initialize(
        ArcanaData arcanaData,
        Action<ArcanaCardView> leftHandler,
        Action<ArcanaCardView> rightHandler)
    {
        cardData = arcanaData;
        leftClickHandler = leftHandler;
        rightClickHandler = rightHandler;
        arcanaNumberText.text = arcanaData.DisplayNumber;
        arcanaNameText.text = arcanaData.ArcanaName;
        cardCostText.text = "Cost " + arcanaData.BaseCost;
        isInDeck = true;
    }

    /// <summary>
    /// 카드의 기본 더미 기준점과 위치, 회전, 겹침 순서를 저장한다.
    /// </summary>
    public void SetDeckLayout(
        Vector2 position,
        float rotationZ,
        int cardOrder)
    {
        cardRectTransform.anchoredPosition = position;
        cardRectTransform.localEulerAngles = new Vector3(0f, 0f, rotationZ);
        deckAnchorMin = cardRectTransform.anchorMin;
        deckAnchorMax = cardRectTransform.anchorMax;
        deckPosition = position;
        deckScale = cardRectTransform.localScale;
        deckRotation = cardRectTransform.localRotation;
        deckOrder = cardOrder;
    }

    /// <summary>
    /// 더미 카드에 포인터가 올라오면 최상위로 올리고 확대한다.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInDeck)
        {
            return;
        }

        cardRectTransform.DOKill();
        isDeckHovered = true;
        deckSiblingIndex = cardRectTransform.GetSiblingIndex();
        cardRectTransform.SetAsLastSibling();
        cardRectTransform.DOAnchorPosY(
            deckPosition.y + hoverRise,
            hoverDuration);
        cardRectTransform.DOScale(
            deckScale * hoverScale,
            hoverDuration);
    }

    /// <summary>
    /// 포인터가 더미 카드를 벗어나면 기본 위치와 크기, 순서로 되돌린다.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isInDeck || !isDeckHovered)
        {
            return;
        }

        cardRectTransform.DOKill();
        isDeckHovered = false;
        cardRectTransform.SetSiblingIndex(deckSiblingIndex);
        cardRectTransform.DOAnchorPos(deckPosition, hoverDuration);
        cardRectTransform.DOScale(deckScale, hoverDuration);
    }

    /// <summary>
    /// 좌클릭과 우클릭을 구분해 연결된 처리 메서드에 이 View를 전달한다.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            leftClickHandler(this);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            rightClickHandler(this);
        }
    }

    /// <summary>
    /// 카드를 인벤토리 부모로 옮기고 더미 Hover 상태를 해제한다.
    /// </summary>
    public void MoveToInventory(RectTransform inventoryRoot)
    {
        cardRectTransform.DOKill();
        isInDeck = false;
        isDeckHovered = false;
        cardRectTransform.SetParent(inventoryRoot, false);
        cardRectTransform.localScale = Vector3.one;
        cardRectTransform.localRotation = Quaternion.identity;
        cardRectTransform.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// 선택을 취소한 카드를 원래 더미 위치와 겹침 순서로 되돌린다.
    /// </summary>
    public void ReturnToDeck(RectTransform deckRoot)
    {
        cardRectTransform.DOKill();
        isDeckHovered = false;

        int targetSiblingIndex = 0;

        for (int siblingIndex = 0;
             siblingIndex < deckRoot.childCount;
             siblingIndex++)
        {
            ArcanaCardView deckCardView = deckRoot
                .GetChild(siblingIndex)
                .GetComponent<ArcanaCardView>();

            if (deckCardView.deckOrder < deckOrder)
            {
                targetSiblingIndex++;
            }
        }

        cardRectTransform.SetParent(deckRoot, false);
        cardRectTransform.anchorMin = deckAnchorMin;
        cardRectTransform.anchorMax = deckAnchorMax;
        cardRectTransform.SetSiblingIndex(targetSiblingIndex);
        cardRectTransform.anchoredPosition = deckPosition;
        cardRectTransform.localScale = deckScale;
        cardRectTransform.localRotation = deckRotation;
        isInDeck = true;
    }

}
