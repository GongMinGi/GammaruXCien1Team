using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlanningController : MonoBehaviour
{
    public event Action<IReadOnlyList<PlannedAction>, Vector2Int, int> PlanningStateChanged;

    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerDisplay playerDisplay;
    [SerializeField] private ActionBar actionBar;
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private ArcanaCatalog arcanaCatalog;
    [SerializeField] private HandDisplay handDisplay;
    [SerializeField] private TangleField tangleField;  // 광대 보스일 때만 연결

    private readonly List<PlannedAction> plannedActions = new();
    private int usedSlots;
    private Vector2Int playerGridPos;
    private Vector2Int battleStartPos;
    private bool planningActive;
    private int dragStartIndex = -1;

    private Action<List<PlannedAction>, Vector2Int> onPlanConfirmed;

    private InstantModifierType activeCostModifier = InstantModifierType.None;
    private bool activeElementBuff;
    private DamageElement activeElement;
    private bool elementSelectionActive;
    private int elementSelectionHandIndex;

    private bool directionSelectionActive;
    private int directionSelectionAllowedDirs;
    private PendingCardUse pendingCardUse;

    private ArcanaBag bag;
    private ArcanaData[] arcanaPool;
    private HashSet<int> cooldownCardIds;
    private bool transformTargetSelectionActive;
    private int observationSourceHandIndex;
    private bool zeroCostUsedThisCount;

    private HashSet<Vector2Int> existingTowerPositions;
    private Sprite cachedPreviewSprite;
    private Texture2D cachedPreviewTexture;

    private static readonly Color TowerPreviewColor = new Color(0.55f, 0.35f, 0.15f, 0.8f);
    private readonly List<GameObject> towerPreviewObjects = new();

    private GameObject elementPromptObject;
    private readonly List<GridCell> blinkingCells = new();

    private static readonly Vector2Int[] CardinalDirs = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };
    private static readonly Vector2Int[] DiagonalDirs = {
        new(-1, 1), new(1, 1), new(-1, -1), new(1, -1)
    };

    private struct PendingCardUse
    {
        public int HandIndex;
        public int EffectiveCost;
        public ArcanaData Card;
        public InstantModifierType ConsumedCost;
        public bool ConsumedElement;
        public DamageElement ConsumedElementValue;
        public bool NeedsTwoDirections;
        public Vector2Int FirstDirection;
        public Vector2Int FirstTowerPos;
        public GameObject FirstTowerPreview;
        public Vector2Int PreFirstMovePos;   // 첫 이동 시뮬 전 위치 (Move 카드용)
        public bool HasSimulatedFirstMove;
    }

    public bool ValidateReferences()
    {
        if (gridManager == null || playerDisplay == null || actionBar == null ||
            playerHand == null || arcanaCatalog == null || handDisplay == null)
        {
            Debug.LogError("PlanningController references are not assigned.", this);
            return false;
        }

        return true;
    }

    public void BeginPlanning(
        Vector2Int startPos,
        Action<List<PlannedAction>, Vector2Int> onConfirmed,
        ArcanaBag sourceBag = null,
        ArcanaData[] pool = null,
        HashSet<int> cooldownIds = null,
        HashSet<Vector2Int> existingTowers = null)
    {
        plannedActions.Clear();
        usedSlots = 0;
        battleStartPos = startPos;
        playerGridPos = startPos;
        actionBar.ClearAll();
        planningActive = true;
        onPlanConfirmed = onConfirmed;
        activeCostModifier = InstantModifierType.None;
        activeElementBuff = false;
        zeroCostUsedThisCount = false;
        elementSelectionActive = false;
        directionSelectionActive = false;
        transformTargetSelectionActive = false;
        bag = sourceBag;
        arcanaPool = pool;
        cooldownCardIds = cooldownIds;
        existingTowerPositions = existingTowers;
        HideElementPrompt();
        ClearDirectionTargets();
        ClearTowerPreviews();
        CancelCardDrag();
        NotifyPlanningSlotChanged();
    }

    private void Update()
    {
        if (!planningActive)
            return;

        if (elementSelectionActive || directionSelectionActive ||
            transformTargetSelectionActive)
        {
            if (directionSelectionActive)
                HandleDirectionClick();
            if (transformTargetSelectionActive && HandleTransformClick())
                return;
            HandleKeyboardInput();
            return;
        }

        if (HandleCardDrag())
            return;

        HandleKeyboardInput();
    }

    private bool HandleCardDrag()
    {
        if (Mouse.current == null)
            return false;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            int hovered = handDisplay.GetHoveredCardIndex();
            if (hovered >= 0)
            {
                dragStartIndex = hovered;
                handDisplay.SetDragSource(hovered);
                return true;
            }
        }

        if (dragStartIndex >= 0)
        {
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                int target = handDisplay.GetHoveredCardIndex();
                if (target >= 0 && target != dragStartIndex)
                    TryMerge(dragStartIndex, target);
                else if (target < 0)
                    QueueCardUse(dragStartIndex);

                CancelCardDrag();
            }
            return true;
        }

        return false;
    }

    private void CancelCardDrag()
    {
        dragStartIndex = -1;
        handDisplay.ClearDragSource();
    }

    private void TryMerge(int indexA, int indexB)
    {
        int lo = Mathf.Min(indexA, indexB);
        int hi = Mathf.Max(indexA, indexB);

        if (!playerHand.TryGetCard(lo, out ArcanaData sourceLo) ||
            !playerHand.TryGetCard(hi, out ArcanaData sourceHi))
            return;

        int sumId = sourceLo.Id + sourceHi.Id;

        const int minMergeResultId = 2;
        const int maxMergeResultId = 20;

        if (sumId < minMergeResultId || sumId > maxMergeResultId)
        {
            Debug.Log($"Merge rejected: result ID {sumId} out of range (valid: {minMergeResultId}~{maxMergeResultId}).");
            return;
        }

        ArcanaData result = arcanaCatalog.GetById(sumId);
        if (result == null)
        {
            Debug.Log($"Merge rejected: no arcana with ID {sumId}.");
            return;
        }

        if (!playerHand.TryMergeCards(lo, hi, result))
        {
            Debug.LogError("Failed to merge cards.");
            return;
        }

        plannedActions.Add(new PlannedAction
        {
            Type = ActionType.MergeCards,
            Cost = 0,
            CardData = result,
            MergeSource1 = sourceLo,
            MergeSourceIndex1 = lo,
            MergeSource2 = sourceHi,
            MergeSourceIndex2 = hi,
            MergeResultIndex = lo
        });

        Debug.Log($"Merged {sourceLo.DisplayNumber} + {sourceHi.DisplayNumber} = {result.DisplayNumber}");
    }

    private void HandleKeyboardInput()
    {
        if (Keyboard.current == null)
            return;

        Keyboard kb = Keyboard.current;

        if (transformTargetSelectionActive)
        {
            if (kb.digit1Key.wasPressedThisFrame) CompleteTransformCard(0);
            else if (kb.digit2Key.wasPressedThisFrame) CompleteTransformCard(1);
            else if (kb.digit3Key.wasPressedThisFrame) CompleteTransformCard(2);
            else if (kb.digit4Key.wasPressedThisFrame) CompleteTransformCard(3);
            else if (kb.digit5Key.wasPressedThisFrame) CompleteTransformCard(4);
            else if (kb.digit6Key.wasPressedThisFrame) CompleteTransformCard(5);
            else if (kb.digit7Key.wasPressedThisFrame) CompleteTransformCard(6);
            else if (kb.escapeKey.wasPressedThisFrame)
                CancelTransformCard();
            return;
        }

        if (directionSelectionActive)
        {
            bool allowsDiag = directionSelectionAllowedDirs >= 8;
            if (kb.wKey.wasPressedThisFrame)
                ConfirmDirectionSelection(Vector2Int.right);
            else if (kb.sKey.wasPressedThisFrame)
                ConfirmDirectionSelection(Vector2Int.left);
            else if (kb.aKey.wasPressedThisFrame)
                ConfirmDirectionSelection(Vector2Int.up);
            else if (kb.dKey.wasPressedThisFrame)
                ConfirmDirectionSelection(Vector2Int.down);
            else if (allowsDiag && kb.qKey.wasPressedThisFrame)
                ConfirmDirectionSelection(new Vector2Int(-1, 1));
            else if (allowsDiag && kb.eKey.wasPressedThisFrame)
                ConfirmDirectionSelection(new Vector2Int(1, 1));
            else if (allowsDiag && kb.zKey.wasPressedThisFrame)
                ConfirmDirectionSelection(new Vector2Int(-1, -1));
            else if (allowsDiag && kb.cKey.wasPressedThisFrame)
                ConfirmDirectionSelection(new Vector2Int(1, -1));
            else if (kb.escapeKey.wasPressedThisFrame)
                CancelDirectionSelection();
            return;
        }

        if (elementSelectionActive)
        {
            if (kb.digit1Key.wasPressedThisFrame)
                ConfirmElementSelection(DamageElement.Sun);
            else if (kb.digit2Key.wasPressedThisFrame)
                ConfirmElementSelection(DamageElement.Moon);
            else if (kb.digit3Key.wasPressedThisFrame)
                ConfirmElementSelection(DamageElement.Star);
            else if (kb.escapeKey.wasPressedThisFrame)
                CancelElementSelection();
            return;
        }

#if UNITY_EDITOR
        // Tab 언두는 개발용 — 빌드에는 이 블록이 컴파일되지 않는다.
        // QA 빌드에서도 쓰려면 조건을 UNITY_EDITOR || DEVELOPMENT_BUILD로 넓힌다.
        if (kb.tabKey.wasPressedThisFrame)
        {
            UndoLastAction();
            return;
        }
#endif

        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            ConfirmPlan();
        else if (kb.leftCtrlKey.wasPressedThisFrame || kb.rightCtrlKey.wasPressedThisFrame)
            QueueStay();
        else if (kb.spaceKey.wasPressedThisFrame)
            QueueCutTangle();
        else if (kb.digit1Key.wasPressedThisFrame)
            QueueCardUse(0);
        else if (kb.digit2Key.wasPressedThisFrame)
            QueueCardUse(1);
        else if (kb.digit3Key.wasPressedThisFrame)
            QueueCardUse(2);
        else if (kb.digit4Key.wasPressedThisFrame)
            QueueCardUse(3);
        else if (kb.digit5Key.wasPressedThisFrame)
            QueueCardUse(4);
        else if (kb.digit6Key.wasPressedThisFrame)
            QueueCardUse(5);
        else if (kb.digit7Key.wasPressedThisFrame)
            QueueCardUse(6);
        else if (kb.wKey.wasPressedThisFrame)
            QueueMove(Vector2Int.right);
        else if (kb.sKey.wasPressedThisFrame)
            QueueMove(Vector2Int.left);
        else if (kb.aKey.wasPressedThisFrame)
            QueueMove(Vector2Int.up);
        else if (kb.dKey.wasPressedThisFrame)
            QueueMove(Vector2Int.down);
    }

    private void QueueMove(Vector2Int direction)
    {
        if (usedSlots >= ActionBar.SlotCount)
            return;

        // 실타래 위에서는 이동이 불가능하다
        if (tangleField != null && tangleField.Contains(playerGridPos))
            return;

        Vector2Int newPos = playerGridPos + direction;

        if (!gridManager.IsValidCoordinate(newPos.x, newPos.y))
            return;

        if (IsTowerPositionOccupied(newPos))
            return;

        plannedActions.Add(new PlannedAction
        {
            Type = ActionType.Move,
            Direction = direction,
            Cost = 1
        });

        usedSlots++;
        zeroCostUsedThisCount = false;
        playerGridPos = newPos;
        playerDisplay.UpdateGridPosition(newPos.x, newPos.y);
        actionBar.FillSlot(usedSlots - 1, ActionType.Move);
        NotifyPlanningSlotChanged();

    }

    private void QueueStay()
    {
        if (usedSlots >= ActionBar.SlotCount)
            return;

        plannedActions.Add(new PlannedAction
        {
            Type = ActionType.Stay,
            Direction = Vector2Int.zero,
            Cost = 1
        });

        usedSlots++;
        zeroCostUsedThisCount = false;
        actionBar.FillSlot(usedSlots - 1, ActionType.Stay);
        NotifyPlanningSlotChanged();

    }

    /// 실타래 해제 (1코스트) — 계획상 현재 위치가 실타래 위일 때만 쓸 수 있다.
    private void QueueCutTangle()
    {
        if (usedSlots >= ActionBar.SlotCount)
            return;

        if (tangleField == null || !tangleField.Contains(playerGridPos))
            return;

        plannedActions.Add(new PlannedAction
        {
            Type = ActionType.CutTangle,
            Direction = Vector2Int.zero,
            Cost = 1
        });

        usedSlots++;
        actionBar.FillSlot(usedSlots - 1, ActionType.CutTangle);
        NotifyPlanningSlotChanged();
    }

    private void QueueCardUse(int handIndex)
    {
        if (!playerHand.TryGetCard(handIndex, out ArcanaData card))
            return;

        if (!card.CanPlaceOnTimeline)
        {
            if (card.UsageType == ArcanaUsageType.Instant)
            {
                QueueInstantUse(handIndex);
                return;
            }
            if (card.UsageType == ArcanaUsageType.Observation)
            {
                QueueObservationUse(handIndex);
                return;
            }
            Debug.Log($"{card.DisplayNumber} cannot be used during the Planning Phase.");
            return;
        }

        if (activeCostModifier != InstantModifierType.None && card.BlocksCostModifiers)
        {
            Debug.Log("절제/악마와 함께 사용할 수 없는 카드입니다.");
            return;
        }

        if (cooldownCardIds != null && cooldownCardIds.Contains(card.Id))
        {
            Debug.Log($"{card.KoreanName}은(는) 재사용 대기 중입니다.");
            return;
        }

        bool consumesHand = card.EffectDefinition != null && card.EffectDefinition.ConsumesHand;

        int effectiveCost = card.BaseCost;
        if (!consumesHand)
        {
            if (activeCostModifier == InstantModifierType.CostReduction)
                effectiveCost = Mathf.Max(1, effectiveCost - 1);
            else if (activeCostModifier == InstantModifierType.EffectDuplication)
                effectiveCost += 1;
        }

        if (usedSlots + effectiveCost > ActionBar.SlotCount)
            return;

        if (card.EffectDefinition != null && card.EffectDefinition.RequiresDirection)
        {
            bool needsTwo = activeCostModifier == InstantModifierType.EffectDuplication
                && HasDirectionalEffectInLastSlot(card);
            pendingCardUse = new PendingCardUse
            {
                HandIndex = handIndex,
                EffectiveCost = effectiveCost,
                Card = card,
                ConsumedCost = activeCostModifier,
                ConsumedElement = activeElementBuff,
                ConsumedElementValue = activeElement,
                NeedsTwoDirections = needsTwo
            };
            directionSelectionAllowedDirs = card.EffectDefinition.AllowedDirections;
            directionSelectionActive = true;
            ShowDirectionTargets();
            return;
        }

        FinalizeCardUse(handIndex, effectiveCost, card, Vector2Int.zero);
    }

    private void FinalizeCardUse(int handIndex, int effectiveCost,
        ArcanaData card, Vector2Int direction)
    {
        playerHand.TryTakeCard(handIndex, out _);

        ArcanaData[] consumedCards = null;
        int[] consumedIndices = null;
        if (card.EffectDefinition != null && card.EffectDefinition.ConsumesHand
            && playerHand.Cards.Count > 0)
        {
            int count = playerHand.Cards.Count;
            consumedCards = new ArcanaData[count];
            consumedIndices = new int[count];
            for (int i = 0; i < count; i++)
            {
                consumedCards[i] = playerHand.Cards[i];
                consumedIndices[i] = i;
            }
            playerHand.RemoveAllCards();
        }

        InstantModifierType consumedCost = activeCostModifier;
        bool consumedElement = activeElementBuff;
        DamageElement consumedElementValue = activeElement;

        activeCostModifier = InstantModifierType.None;
        activeElementBuff = false;

        Vector2Int prePos = playerGridPos;
        bool placedTower = false;
        Vector2Int towerPos = Vector2Int.zero;

        if (direction != Vector2Int.zero && card.EffectDefinition != null)
        {
            playerGridPos = SimulateCardMovement(playerGridPos, direction, card);
            if (playerGridPos != prePos)
                playerDisplay.UpdateGridPosition(playerGridPos.x, playerGridPos.y);

            towerPos = SimulateTowerPlacement(prePos, direction, card);
            if (towerPos != Vector2Int.zero)
            {
                placedTower = true;
                towerPreviewObjects.Add(CreateTowerPreview(towerPos));
            }
        }

        plannedActions.Add(new PlannedAction
        {
            Type = ActionType.UseCard,
            Direction = direction,
            Cost = effectiveCost,
            CardData = card,
            OriginalHandIndex = handIndex,
            ConsumedCostModifier = consumedCost,
            ConsumedElementBuff = consumedElement,
            ConsumedElement = consumedElementValue,
            PreCardPosition = prePos,
            PlacedTower = placedTower,
            TowerPlacedPosition = towerPos,
            ConsumedHandCards = consumedCards,
            ConsumedHandIndices = consumedIndices
        });

        if (card.CooldownTurns > 0)
            cooldownCardIds?.Add(card.Id);

        actionBar.FillRange(usedSlots, effectiveCost, card.TimelineColor);
        usedSlots += effectiveCost;
        zeroCostUsedThisCount = false;
        NotifyPlanningSlotChanged();
    }

    private void FinalizeCardUseWithTwoDirections(int handIndex, int effectiveCost,
        ArcanaData card, Vector2Int firstDirection, Vector2Int secondDirection)
    {
        playerHand.TryTakeCard(handIndex, out _);

        InstantModifierType consumedCost = activeCostModifier;
        bool consumedElement = activeElementBuff;
        DamageElement consumedElementValue = activeElement;

        activeCostModifier = InstantModifierType.None;
        activeElementBuff = false;

        bool isMove = HasMoveInLastSlot(card);
        // Move 카드: prePos는 첫 이동 시뮬 전 위치 (ConfirmDirectionSelection에서 저장)
        Vector2Int prePos = isMove ? pendingCardUse.PreFirstMovePos : playerGridPos;

        bool placedTower = false;
        Vector2Int towerPos = Vector2Int.zero;
        bool placedSecondTower = false;
        Vector2Int secondTowerPos = Vector2Int.zero;

        if (isMove)
        {
            // 첫 이동은 이미 시뮬됨 (ConfirmDirectionSelection에서)
            // 두 번째 이동 시뮬
            playerGridPos = SimulateCardMovement(playerGridPos, secondDirection, card);
            playerDisplay.UpdateGridPosition(playerGridPos.x, playerGridPos.y);
        }
        else
        {
            // 타워 카드: 첫 타워 프리뷰 이미 생성됨
            placedTower = true;
            towerPos = pendingCardUse.FirstTowerPos;

            Vector2Int secondTarget = prePos + secondDirection;
            if (gridManager.IsValidCoordinate(secondTarget.x, secondTarget.y))
            {
                placedSecondTower = true;
                secondTowerPos = secondTarget;
                towerPreviewObjects.Add(CreateTowerPreview(secondTowerPos));
            }
        }

        plannedActions.Add(new PlannedAction
        {
            Type = ActionType.UseCard,
            Direction = firstDirection,
            DuplicatedDirection = secondDirection,
            Cost = effectiveCost,
            CardData = card,
            OriginalHandIndex = handIndex,
            ConsumedCostModifier = consumedCost,
            ConsumedElementBuff = consumedElement,
            ConsumedElement = consumedElementValue,
            PreCardPosition = prePos,
            PlacedTower = placedTower,
            TowerPlacedPosition = towerPos,
            PlacedSecondTower = placedSecondTower,
            SecondTowerPlacedPosition = secondTowerPos
        });

        if (card.CooldownTurns > 0)
            cooldownCardIds?.Add(card.Id);

        actionBar.FillRange(usedSlots, effectiveCost, card.TimelineColor);
        usedSlots += effectiveCost;
        zeroCostUsedThisCount = false;
        NotifyPlanningSlotChanged();
    }

    private void UndoLastAction()
    {
        if (plannedActions.Count == 0)
            return;

        PlannedAction action = plannedActions[^1];
        plannedActions.RemoveAt(plannedActions.Count - 1);

        usedSlots -= action.Cost;
        zeroCostUsedThisCount = false;

        switch (action.Type)
        {
            case ActionType.Move:
                playerGridPos -= action.Direction;
                playerDisplay.UpdateGridPosition(playerGridPos.x, playerGridPos.y);
                actionBar.ClearSlot(usedSlots);
                break;
            case ActionType.Stay:
            case ActionType.CutTangle:
                actionBar.ClearSlot(usedSlots);
                break;
            case ActionType.UseCard:
                actionBar.ClearRange(usedSlots, action.Cost);
                if (action.ConsumedHandCards != null)
                    playerHand.RestoreCards(action.ConsumedHandCards, action.ConsumedHandIndices);
                playerHand.ReturnCard(action.OriginalHandIndex, action.CardData);
                if (action.PreCardPosition != playerGridPos)
                {
                    playerGridPos = action.PreCardPosition;
                    playerDisplay.UpdateGridPosition(playerGridPos.x, playerGridPos.y);
                }
                if (action.PlacedSecondTower && towerPreviewObjects.Count > 0)
                {
                    GameObject second = towerPreviewObjects[^1];
                    towerPreviewObjects.RemoveAt(towerPreviewObjects.Count - 1);
                    Destroy(second);
                }
                if (action.PlacedTower && towerPreviewObjects.Count > 0)
                {
                    GameObject first = towerPreviewObjects[^1];
                    towerPreviewObjects.RemoveAt(towerPreviewObjects.Count - 1);
                    Destroy(first);
                }
                if (action.ConsumedCostModifier != InstantModifierType.None)
                    activeCostModifier = action.ConsumedCostModifier;
                if (action.ConsumedElementBuff)
                {
                    activeElementBuff = true;
                    activeElement = action.ConsumedElement;
                }
                if (action.CardData != null && action.CardData.CooldownTurns > 0)
                    cooldownCardIds?.Remove(action.CardData.Id);
                break;
            case ActionType.UseInstantCard:
                playerHand.ReturnCard(action.OriginalHandIndex, action.CardData);
                switch (action.ModifierType)
                {
                    case InstantModifierType.CostReduction:
                    case InstantModifierType.EffectDuplication:
                        activeCostModifier = InstantModifierType.None;
                        break;
                    case InstantModifierType.ElementBuff:
                        activeElementBuff = false;
                        break;
                }
                if (ConsumesZeroCostLimit(action.CardData))
                    zeroCostUsedThisCount = false;
                break;
            case ActionType.UseObservationCard:
                if (action.ObservationAction == ObservationActionType.DrawCard)
                {
                    // 드로우된 카드를 인덱스로 제거 후 관측 카드 복원
                    int removeIndex = Mathf.Min(action.DrawnCardIndex, playerHand.Cards.Count - 1);
                    if (removeIndex >= 0 && playerHand.Cards[removeIndex] == action.DrawnCard)
                        playerHand.TryTakeCard(removeIndex, out _);
                    else
                    {
                        // 인덱스 밀림 — 폴백 탐색
                        for (int i = playerHand.Cards.Count - 1; i >= 0; i--)
                        {
                            if (playerHand.Cards[i] == action.DrawnCard)
                            {
                                playerHand.TryTakeCard(i, out _);
                                break;
                            }
                        }
                    }
                    playerHand.ReturnCard(action.OriginalHandIndex, action.CardData);
                }
                else if (action.ObservationAction == ObservationActionType.TransformCard)
                {
                    // 교체된 카드를 원본으로 복원 후 관측 카드 복원
                    if (action.TransformOriginal != null)
                        playerHand.ReplaceCard(action.TransformTargetIndex, action.TransformOriginal);
                    playerHand.ReturnCard(action.OriginalHandIndex, action.CardData);
                }
                if (ConsumesZeroCostLimit(action.CardData))
                    zeroCostUsedThisCount = false;
                break;
            case ActionType.MergeCards:
                playerHand.UndoMerge(
                    action.MergeResultIndex,
                    action.MergeSource1, action.MergeSourceIndex1,
                    action.MergeSource2, action.MergeSourceIndex2);
                break;
        }

        NotifyPlanningSlotChanged();

    }

    private void ConfirmPlan()
    {
        if (usedSlots != ActionBar.SlotCount)
            return;

        if (!planningActive)
            return;

        planningActive = false;
        ClearTowerPreviews();
        PlanningStateChanged?.Invoke(plannedActions, battleStartPos, -1);

        var confirmed = new List<PlannedAction>(plannedActions);
        plannedActions.Clear();
        onPlanConfirmed?.Invoke(confirmed, battleStartPos);
    }


    private void QueueObservationUse(int handIndex)
    {
        if (!playerHand.TryGetCard(handIndex, out ArcanaData card))
            return;

        if (ConsumesZeroCostLimit(card) && zeroCostUsedThisCount)
            return;

        switch (card.ObservationAction)
        {
            case ObservationActionType.DrawCard:
                playerHand.TryTakeCard(handIndex, out _);
                int countBefore = playerHand.Cards.Count;
                playerHand.DrawOne(bag);
                if (playerHand.Cards.Count <= countBefore)
                {
                    // 드로우 실패 — 관측 카드 복원
                    playerHand.ReturnCard(handIndex, card);
                    Debug.Log("드로우 실패: 백이 비어있습니다.");
                    return;
                }
                int drawnIndex = playerHand.Cards.Count - 1;
                ArcanaData drawn = playerHand.Cards[drawnIndex];
                plannedActions.Add(new PlannedAction
                {
                    Type = ActionType.UseObservationCard,
                    Cost = 0,
                    CardData = card,
                    OriginalHandIndex = handIndex,
                    ObservationAction = ObservationActionType.DrawCard,
                    DrawnCard = drawn,
                    DrawnCardIndex = drawnIndex
                });
                if (ConsumesZeroCostLimit(card))
                    zeroCostUsedThisCount = true;
                Debug.Log($"{card.DisplayNumber} {card.KoreanName}: 카드 1장 드로우.");
                break;

            case ObservationActionType.TransformCard:
                if (playerHand.Cards.Count <= 1)
                {
                    Debug.Log("변환할 대상 카드가 없습니다.");
                    return;
                }
                playerHand.TryTakeCard(handIndex, out _);
                observationSourceHandIndex = handIndex;
                transformTargetSelectionActive = true;
                plannedActions.Add(new PlannedAction
                {
                    Type = ActionType.UseObservationCard,
                    Cost = 0,
                    CardData = card,
                    OriginalHandIndex = handIndex,
                    ObservationAction = ObservationActionType.TransformCard
                });
                Debug.Log("변환할 카드를 선택하세요 (클릭 또는 숫자키). ESC: 취소.");
                break;

            default:
                return;
        }
    }

    private bool HandleTransformClick()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return false;

        int hovered = handDisplay.GetHoveredCardIndex();
        if (hovered >= 0)
        {
            CompleteTransformCard(hovered);
            return true;
        }
        return false;
    }

    private void CompleteTransformCard(int targetIndex)
    {
        if (!playerHand.TryGetCard(targetIndex, out ArcanaData targetCard))
            return;

        ArcanaData replacement = PickRandomFromPool();
        if (replacement == null)
        {
            transformTargetSelectionActive = false;
            return;
        }

        playerHand.ReplaceCard(targetIndex, replacement);
        transformTargetSelectionActive = false;

        // 마지막 PlannedAction에 변환 결과 기록
        PlannedAction last = plannedActions[^1];
        last.TransformTargetIndex = targetIndex;
        last.TransformOriginal = targetCard;
        last.TransformReplacement = replacement;
        plannedActions[^1] = last;

        if (ConsumesZeroCostLimit(last.CardData))
            zeroCostUsedThisCount = true;

        Debug.Log($"{targetCard.DisplayNumber} → {replacement.DisplayNumber} {replacement.KoreanName}");
    }

    private void CancelTransformCard()
    {
        transformTargetSelectionActive = false;
        if (plannedActions.Count > 0)
        {
            PlannedAction last = plannedActions[^1];
            if (last.Type == ActionType.UseObservationCard &&
                last.ObservationAction == ObservationActionType.TransformCard &&
                last.TransformOriginal == null)
            {
                plannedActions.RemoveAt(plannedActions.Count - 1);
                playerHand.ReturnCard(last.OriginalHandIndex, last.CardData);
            }
        }
    }

    private ArcanaData PickRandomFromPool()
    {
        if (arcanaPool == null || arcanaPool.Length == 0)
            return null;

        var handIds = new HashSet<int>();
        foreach (ArcanaData card in playerHand.Cards)
            handIds.Add(card.Id);

        var candidates = new List<ArcanaData>();
        foreach (ArcanaData card in arcanaPool)
        {
            if (!handIds.Contains(card.Id))
                candidates.Add(card);
        }

        if (candidates.Count == 0)
            return arcanaPool[UnityEngine.Random.Range(0, arcanaPool.Length)];

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private void QueueInstantUse(int handIndex)
    {
        if (!playerHand.TryGetCard(handIndex, out ArcanaData card))
            return;

        if (ConsumesZeroCostLimit(card) && zeroCostUsedThisCount)
            return;

        InstantModifierType modifier = GetModifierType(card);
        if (modifier == InstantModifierType.None)
            return;

        if ((modifier == InstantModifierType.CostReduction ||
             modifier == InstantModifierType.EffectDuplication) &&
            activeCostModifier != InstantModifierType.None)
            return;

        if (modifier == InstantModifierType.ElementBuff && activeElementBuff)
            return;

        if (modifier == InstantModifierType.DamageSpread &&
            ActionBar.SlotCount - usedSlots < 4)
            return;

        if (modifier == InstantModifierType.ElementBuff)
        {
            EnterElementSelection(handIndex);
            return;
        }

        ApplyInstantModifier(handIndex, modifier);
    }

    private void ApplyInstantModifier(int handIndex, InstantModifierType modifier)
    {
        playerHand.TryTakeCard(handIndex, out ArcanaData card);

        plannedActions.Add(new PlannedAction
        {
            Type = ActionType.UseInstantCard,
            Cost = 0,
            CardData = card,
            OriginalHandIndex = handIndex,
            ModifierType = modifier,
            SelectedElement = activeElement
        });

        switch (modifier)
        {
            case InstantModifierType.CostReduction:
            case InstantModifierType.EffectDuplication:
                activeCostModifier = modifier;
                break;
            case InstantModifierType.ElementBuff:
                activeElementBuff = true;
                break;
        }

        if (ConsumesZeroCostLimit(card))
            zeroCostUsedThisCount = true;

        NotifyPlanningSlotChanged();
    }

    private void EnterElementSelection(int handIndex)
    {
        elementSelectionActive = true;
        elementSelectionHandIndex = handIndex;
        ShowElementPrompt();
    }

    private void ConfirmElementSelection(DamageElement element)
    {
        HideElementPrompt();
        elementSelectionActive = false;
        activeElement = element;
        ApplyInstantModifier(elementSelectionHandIndex, InstantModifierType.ElementBuff);
    }

    private void CancelElementSelection()
    {
        HideElementPrompt();
        elementSelectionActive = false;
    }

    private void ConfirmDirectionSelection(Vector2Int direction)
    {
        Vector2Int target = playerGridPos + direction;
        if (!gridManager.IsValidCoordinate(target.x, target.y))
            return;

        if (IsTowerPositionOccupied(target))
            return;

        if (pendingCardUse.FirstDirection != Vector2Int.zero &&
            target == pendingCardUse.FirstTowerPos)
            return;

        if (pendingCardUse.NeedsTwoDirections &&
            pendingCardUse.FirstDirection == Vector2Int.zero)
        {
            pendingCardUse.FirstDirection = direction;

            if (HasMoveInLastSlot(pendingCardUse.Card))
            {
                // Move 카드: 첫 돌진 시뮬레이션 후 새 위치에서 두 번째 방향 선택
                pendingCardUse.PreFirstMovePos = playerGridPos;
                pendingCardUse.HasSimulatedFirstMove = true;
                playerGridPos = SimulateCardMovement(playerGridPos, direction,
                    pendingCardUse.Card);
                if (playerGridPos != pendingCardUse.PreFirstMovePos)
                    playerDisplay.UpdateGridPosition(playerGridPos.x, playerGridPos.y);
            }
            else
            {
                // 타워 카드: 첫 타워 프리뷰 생성
                pendingCardUse.FirstTowerPos = target;
                GameObject preview = CreateTowerPreview(target);
                pendingCardUse.FirstTowerPreview = preview;
                towerPreviewObjects.Add(preview);
            }

            ClearDirectionTargets();
            ShowDirectionTargets();
            return;
        }

        ClearDirectionTargets();
        directionSelectionActive = false;

        if (pendingCardUse.NeedsTwoDirections)
        {
            FinalizeCardUseWithTwoDirections(
                pendingCardUse.HandIndex,
                pendingCardUse.EffectiveCost,
                pendingCardUse.Card,
                pendingCardUse.FirstDirection,
                direction);
        }
        else
        {
            FinalizeCardUse(
                pendingCardUse.HandIndex,
                pendingCardUse.EffectiveCost,
                pendingCardUse.Card,
                direction);
        }
    }

    private bool IsTowerPositionOccupied(Vector2Int pos)
    {
        if (existingTowerPositions != null && existingTowerPositions.Contains(pos))
            return true;
        foreach (PlannedAction action in plannedActions)
        {
            if (action.PlacedTower && action.TowerPlacedPosition == pos)
                return true;
            if (action.PlacedSecondTower && action.SecondTowerPlacedPosition == pos)
                return true;
        }
        return false;
    }

    private void CancelDirectionSelection()
    {
        if (pendingCardUse.NeedsTwoDirections &&
            pendingCardUse.FirstDirection != Vector2Int.zero)
        {
            // 두 번째 선택 중 ESC: 첫 번째로 되돌리기
            if (pendingCardUse.FirstTowerPreview != null)
            {
                towerPreviewObjects.Remove(pendingCardUse.FirstTowerPreview);
                Destroy(pendingCardUse.FirstTowerPreview);
            }

            // Move 카드: 첫 이동 시뮬 되돌리기
            if (pendingCardUse.HasSimulatedFirstMove)
            {
                playerGridPos = pendingCardUse.PreFirstMovePos;
                playerDisplay.UpdateGridPosition(playerGridPos.x, playerGridPos.y);
                pendingCardUse.HasSimulatedFirstMove = false;
            }

            pendingCardUse.FirstDirection = Vector2Int.zero;
            pendingCardUse.FirstTowerPos = Vector2Int.zero;
            pendingCardUse.FirstTowerPreview = null;
            ClearDirectionTargets();
            ShowDirectionTargets();
            return;
        }

        ClearDirectionTargets();
        directionSelectionActive = false;
    }

    private GameObject CreateTowerPreview(Vector2Int gridPos)
    {
        EnsurePreviewSprite();

        GameObject obj = new GameObject("TowerPreview");
        obj.transform.SetParent(transform, false);
        obj.transform.position = gridManager.GridToWorldPosition(gridPos.x, gridPos.y);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(obj.transform, false);
        visual.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = cachedPreviewSprite;
        renderer.color = TowerPreviewColor;
        renderer.sortingOrder = 2;

        return obj;
    }

    private void EnsurePreviewSprite()
    {
        if (cachedPreviewSprite != null) return;
        int texSize = 16;
        cachedPreviewTexture = new Texture2D(texSize, texSize);
        cachedPreviewTexture.filterMode = FilterMode.Point;
        for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
                cachedPreviewTexture.SetPixel(x, y, Color.white);
        cachedPreviewTexture.Apply();
        cachedPreviewSprite = Sprite.Create(
            cachedPreviewTexture, new Rect(0, 0, texSize, texSize),
            new Vector2(0.5f, 0f), texSize);
    }

    private Vector2Int SimulateTowerPlacement(
        Vector2Int currentPos, Vector2Int direction, ArcanaData card)
    {
        ScheduledEffect[][] effects = card.EffectDefinition.Expand(card);
        if (effects == null)
            return Vector2Int.zero;

        foreach (ScheduledEffect[] slotEffects in effects)
        {
            if (slotEffects == null) continue;
            foreach (ScheduledEffect effect in slotEffects)
            {
                if (effect.Type != EffectType.PlaceTower)
                    continue;

                Vector2Int towerPos = currentPos + direction;
                if (gridManager.IsValidCoordinate(towerPos.x, towerPos.y))
                    return towerPos;
            }
        }

        return Vector2Int.zero;
    }

    private void ClearTowerPreviews()
    {
        foreach (GameObject obj in towerPreviewObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        towerPreviewObjects.Clear();
    }

    private Vector2Int SimulateCardMovement(
        Vector2Int currentPos, Vector2Int direction, ArcanaData card)
    {
        ScheduledEffect[][] effects = card.EffectDefinition.Expand(card);
        if (effects == null)
            return currentPos;

        foreach (ScheduledEffect[] slotEffects in effects)
        {
            if (slotEffects == null) continue;
            foreach (ScheduledEffect effect in slotEffects)
            {
                if (effect.Type != EffectType.Move)
                    continue;

                // 실타래 위에서는 카드 이동·전차 돌진도 막힌다
                if (tangleField != null && tangleField.Contains(currentPos))
                    continue;

                int dist = effect.BaseValue > 0 ? effect.BaseValue : 1;
                Vector2Int target = currentPos;
                for (int step = 1; step <= dist; step++)
                {
                    Vector2Int check = currentPos + direction * step;
                    if (!gridManager.IsValidCoordinate(check.x, check.y))
                        break;
                    if (IsTowerPositionOccupied(check))
                        break;
                    target = check;
                }
                currentPos = target;
            }
        }

        return currentPos;
    }

    private static bool ConsumesZeroCostLimit(ArcanaData card)
        => card.BaseCost == 0 && !card.ZeroCostUnlimited;

    private static bool HasDirectionalEffectInLastSlot(ArcanaData card)
    {
        if (card.EffectDefinition == null) return false;
        ScheduledEffect[][] effects = card.EffectDefinition.Expand(card);
        if (effects == null || effects.Length == 0) return false;
        ScheduledEffect[] last = effects[^1];
        if (last == null) return false;
        foreach (ScheduledEffect e in last)
            if (e.Type == EffectType.PlaceTower || e.Type == EffectType.Move)
                return true;
        return false;
    }

    private static bool HasMoveInLastSlot(ArcanaData card)
    {
        if (card.EffectDefinition == null) return false;
        ScheduledEffect[][] effects = card.EffectDefinition.Expand(card);
        if (effects == null || effects.Length == 0) return false;
        ScheduledEffect[] last = effects[^1];
        if (last == null) return false;
        foreach (ScheduledEffect e in last)
            if (e.Type == EffectType.Move) return true;
        return false;
    }

    private static InstantModifierType GetModifierType(ArcanaData card)
    {
        return card.Id switch
        {
            4 => InstantModifierType.ElementBuff,
            5 => InstantModifierType.DamageReduction,
            12 => InstantModifierType.DamageSpread,
            14 => InstantModifierType.CostReduction,
            15 => InstantModifierType.EffectDuplication,
            _ => InstantModifierType.None
        };
    }

    private void NotifyPlanningSlotChanged()
    {
        int slotIndex = usedSlots < ActionBar.SlotCount ? usedSlots : -1;
        PlanningStateChanged?.Invoke(plannedActions, battleStartPos, slotIndex);
    }

    private void HandleDirectionClick()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, 0f));

        if (!gridManager.TryWorldToGrid(worldPos, out int gx, out int gy))
            return;

        Vector2Int clickedDir = new Vector2Int(gx, gy) - playerGridPos;

        if (Mathf.Abs(clickedDir.x) > 1 || Mathf.Abs(clickedDir.y) > 1)
            return;
        if (clickedDir == Vector2Int.zero)
            return;

        bool isDiagonal = clickedDir.x != 0 && clickedDir.y != 0;
        if (isDiagonal && directionSelectionAllowedDirs < 8)
            return;

        ConfirmDirectionSelection(clickedDir);
    }

    private void ShowDirectionTargets()
    {
        ClearDirectionTargets();
        Color blinkA = new Color(0.3f, 0.9f, 0.3f, 0.8f);
        Color blinkB = new Color(0.3f, 0.9f, 0.3f, 0.2f);

        HashSet<Vector2Int> occupied = GetOccupiedTowerPositions();

        foreach (Vector2Int dir in CardinalDirs)
            TryBlinkCell(dir, blinkA, blinkB, occupied);

        if (directionSelectionAllowedDirs >= 8)
            foreach (Vector2Int dir in DiagonalDirs)
                TryBlinkCell(dir, blinkA, blinkB, occupied);
    }

    private HashSet<Vector2Int> GetOccupiedTowerPositions()
    {
        var occupied = new HashSet<Vector2Int>();
        if (existingTowerPositions != null)
            occupied.UnionWith(existingTowerPositions);
        foreach (PlannedAction action in plannedActions)
        {
            if (action.PlacedTower)
                occupied.Add(action.TowerPlacedPosition);
            if (action.PlacedSecondTower)
                occupied.Add(action.SecondTowerPlacedPosition);
        }
        if (pendingCardUse.FirstTowerPreview != null)
            occupied.Add(pendingCardUse.FirstTowerPos);
        return occupied;
    }

    private void TryBlinkCell(Vector2Int dir, Color a, Color b,
        HashSet<Vector2Int> occupied)
    {
        Vector2Int target = playerGridPos + dir;
        if (!gridManager.IsValidCoordinate(target.x, target.y))
            return;
        if (occupied.Contains(target))
            return;
        GridCell cell = gridManager.GetCell(target.x, target.y);
        if (cell == null) return;
        cell.StartBlink(a, b);
        blinkingCells.Add(cell);
    }

    private void ClearDirectionTargets()
    {
        foreach (GridCell cell in blinkingCells)
            cell.StopBlink();
        blinkingCells.Clear();
    }

    private void ShowElementPrompt()
    {
        HideElementPrompt();
        elementPromptObject = new GameObject("ElementPrompt");
        elementPromptObject.transform.SetParent(transform);
        Vector3 pos = gridManager.GridToWorldPosition(playerGridPos.x, playerGridPos.y);
        elementPromptObject.transform.position = pos + Vector3.up * 0.8f;

        TextMesh text = elementPromptObject.AddComponent<TextMesh>();
        text.text = "1:\ud574  2:\ub2ec  3:\ubcc4  ESC:\ucde8\uc18c";
        text.anchor = TextAnchor.MiddleCenter;
        text.fontSize = 32;
        text.characterSize = 0.06f;
        text.color = new Color(1f, 0.9f, 0.3f);

        MeshRenderer mr = elementPromptObject.GetComponent<MeshRenderer>();
        mr.sortingOrder = 20;
    }

    private void HideElementPrompt()
    {
        if (elementPromptObject != null)
        {
            Destroy(elementPromptObject);
            elementPromptObject = null;
        }
    }

    private void OnDestroy()
    {
        if (cachedPreviewSprite != null) Destroy(cachedPreviewSprite);
        if (cachedPreviewTexture != null) Destroy(cachedPreviewTexture);
    }
}
