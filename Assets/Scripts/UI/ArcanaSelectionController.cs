using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아르카나 표시 범위와 5~10장 선택 제한을 관리하고 View 및 전투 진입을 조정한다.
/// </summary>
public class ArcanaSelectionController : MonoBehaviour
{
    private const int FirstDisplayedArcanaId = 1;
    private const int MinimumSelectedCardCount = 5;
    private const int MaximumSelectedCardCount = 10;

    [SerializeField] private ArcanaCatalog arcanaCatalog;
    [SerializeField] private ArcanaSelectionView arcanaSelectionView;
    [SerializeField] private SceneTransitionController sceneTransitionController;
    [SerializeField] private TutorialController deckTutorialController;
    [SerializeField] private TutorialController battleReadyTutorialController;
    [SerializeField] private int maximumDisplayedArcanaId = 10;
    [SerializeField] private ArcanaDeckRowLayout deckRowLayout
        = ArcanaDeckRowLayout.TwoRows;

    private readonly List<ArcanaData> selectedArcanaCards = new List<ArcanaData>();

    /// <summary>
    /// 설정한 번호 범위에서 전투 풀에 들어갈 수 있는 아르카나 카드들을 생성한다.
    /// </summary>
    private void Start()
    {
        List<ArcanaData> selectableArcanaCards = new List<ArcanaData>();

        for (int arcanaId = FirstDisplayedArcanaId;
             arcanaId <= maximumDisplayedArcanaId;
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
            deckRowLayout,
            HandleLeftClick,
            HandleRightClick);
        UpdateBattleStartButtonState();
    }

    /// <summary>
    /// 아르카나 선택 화면을 표시한다.
    /// </summary>
    public void OpenArcanaSelection()
    {
        arcanaSelectionView.ShowSelection();

        // 첫 스테이지를 아직 깨지 않은 첫 판에서만 튜토리얼을 띄운다.
        if (!StageProgressData.IsFirstStageCleared)
        {
            deckTutorialController.StartTutorial();
        }
    }

    /// <summary>
    /// 좌클릭한 카드의 상세 정보를 표시하고 선택 제한 안에서 인벤토리로 옮긴다.
    /// </summary>
    private void HandleLeftClick(ArcanaCardView selectedArcanaCard)
    {
        arcanaSelectionView.ShowCardDetails(selectedArcanaCard.CardData);
        TutorialController.NotifyActionCompleted("ArcanaCardSelected");

        if (selectedArcanaCard.IsInDeck
            && selectedArcanaCards.Count < MaximumSelectedCardCount)
        {
            arcanaSelectionView.MoveCardToInventory(selectedArcanaCard);
            selectedArcanaCards.Add(selectedArcanaCard.CardData);
            UpdateBattleStartButtonState();

            // 수정구가 막 켜진 순간에 마지막 튜토리얼을 띄운다.
            if (selectedArcanaCards.Count == MinimumSelectedCardCount
                && !StageProgressData.IsFirstStageCleared)
            {
                battleReadyTutorialController.StartTutorial();
            }
        }
    }

    /// <summary>
    /// 우클릭한 카드의 상세 정보를 표시하고 선택된 카드는 더미로 되돌린다.
    /// </summary>
    private void HandleRightClick(ArcanaCardView selectedArcanaCard)
    {
        arcanaSelectionView.ShowCardDetails(selectedArcanaCard.CardData);
        TutorialController.NotifyActionCompleted("ArcanaCardReturned");

        if (!selectedArcanaCard.IsInDeck)
        {
            arcanaSelectionView.ReturnCardToDeck(selectedArcanaCard);
            selectedArcanaCards.Remove(selectedArcanaCard.CardData);
            UpdateBattleStartButtonState();
        }
    }

    /// <summary>
    /// 카드가 5장 이상 선택되었을 때 선택 목록을 저장하고 전투 씬 전환을 시작한다.
    /// </summary>
    public void StartBattle()
    {
        if (selectedArcanaCards.Count < MinimumSelectedCardCount)
        {
            return;
        }

        BattleLoadoutData.SaveSelectedArcanaCards(selectedArcanaCards.ToArray());
        sceneTransitionController.StartSceneTransition();
    }

    /// <summary>
    /// 현재 선택 수에 따라 전투 시작 버튼의 활성 상태를 갱신한다.
    /// </summary>
    private void UpdateBattleStartButtonState()
    {
        bool canStartBattle = selectedArcanaCards.Count
            >= MinimumSelectedCardCount;
        arcanaSelectionView.SetBattleStartButtonInteractable(canStartBattle);
    }
}
