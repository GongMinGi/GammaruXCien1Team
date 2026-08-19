using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아르카나 선택 목록과 11장 제한을 관리하고 View 및 전투 진입을 조정한다.
/// </summary>
public class ArcanaSelectionController : MonoBehaviour
{
    private const int MaximumSelectedCardCount = 11;

    [SerializeField] private ArcanaCatalog arcanaCatalog;
    [SerializeField] private ArcanaSelectionView arcanaSelectionView;
    [SerializeField] private SceneTransitionController sceneTransitionController;

    private readonly List<ArcanaData> selectedArcanaCards = new List<ArcanaData>();

    /// <summary>
    /// 전투 풀에 들어갈 수 있는 아르카나 카드들을 생성한다.
    /// </summary>
    private void Start()
    {
        List<ArcanaData> selectableArcanaCards = new List<ArcanaData>();

        for (int arcanaId = ArcanaCatalog.MinArcanaId;
             arcanaId <= ArcanaCatalog.MaxArcanaId;
             arcanaId++)
        {
            ArcanaData arcanaData = arcanaCatalog.GetById(arcanaId);

            if (arcanaData != null && arcanaData.CanEnterPool)
            {
                selectableArcanaCards.Add(arcanaData);
            }
        }

        arcanaSelectionView.CreateDeckCards(
            selectableArcanaCards.ToArray(),
            HandleLeftClick,
            HandleRightClick);
    }

    /// <summary>
    /// 아르카나 선택 화면을 표시한다.
    /// </summary>
    public void OpenArcanaSelection()
    {
        arcanaSelectionView.ShowSelection();
    }

    /// <summary>
    /// 좌클릭한 카드의 상세 정보를 표시하고 선택 제한 안에서 인벤토리로 옮긴다.
    /// </summary>
    private void HandleLeftClick(ArcanaCardView selectedArcanaCard)
    {
        arcanaSelectionView.ShowCardDetails(selectedArcanaCard.CardData);

        if (selectedArcanaCard.IsInDeck
            && selectedArcanaCards.Count < MaximumSelectedCardCount)
        {
            arcanaSelectionView.MoveCardToInventory(selectedArcanaCard);
            selectedArcanaCards.Add(selectedArcanaCard.CardData);
        }
    }

    /// <summary>
    /// 우클릭한 카드의 상세 정보를 표시하고 선택된 카드는 더미로 되돌린다.
    /// </summary>
    private void HandleRightClick(ArcanaCardView selectedArcanaCard)
    {
        arcanaSelectionView.ShowCardDetails(selectedArcanaCard.CardData);

        if (!selectedArcanaCard.IsInDeck)
        {
            arcanaSelectionView.ReturnCardToDeck(selectedArcanaCard);
            selectedArcanaCards.Remove(selectedArcanaCard.CardData);
        }
    }

    /// <summary>
    /// 카드가 정확히 11장 선택되었을 때 선택 목록을 저장하고 전투 씬 전환을 시작한다.
    /// </summary>
    public void StartBattle()
    {
        if (selectedArcanaCards.Count != MaximumSelectedCardCount)
        {
            return;
        }

        BattleLoadoutData.SaveSelectedArcanaCards(selectedArcanaCards.ToArray());
        sceneTransitionController.StartSceneTransition();
    }
}
