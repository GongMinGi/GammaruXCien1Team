using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerDisplay playerDisplay;
    [SerializeField] private ActionBar actionBar;
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private ArcanaCatalog arcanaCatalog;
    [SerializeField] private HandDisplay handDisplay;
    [SerializeField] private ArcanaData[] selectedArcanaPool;

    private readonly List<PlannedAction> plannedActions = new();
    private int usedSlots;
    private Vector2Int playerGridPos;
    private bool planningActive;
    private ArcanaBag bag;
    private int dragStartIndex = -1;

    private void Start()
    {
        if (!ValidateReferences() || !ValidatePool())
        {
            enabled = false;
            return;
        }

        bag = new ArcanaBag(selectedArcanaPool);
        playerHand.Initialize(bag);
        BeginPlanningPhase();
    }

    private bool ValidateReferences()
    {
        if (gridManager == null || playerDisplay == null || actionBar == null ||
            playerHand == null || arcanaCatalog == null || handDisplay == null)
        {
            Debug.LogError("TurnManager references are not assigned.", this);
            return false;
        }

        return true;
    }

    private bool ValidatePool()
    {
        if (selectedArcanaPool == null || selectedArcanaPool.Length == 0)
        {
            Debug.LogError("Arcana pool is empty.", this);
            return false;
        }

        HashSet<int> ids = new();
        foreach (ArcanaData a in selectedArcanaPool)
        {
            if (a == null)
            {
                Debug.LogError("Pool has null entry.", this);
                return false;
            }

            if (!a.CanEnterPool)
            {
                Debug.LogError($"Arcana {a.DisplayNumber} cannot enter pool.", this);
                return false;
            }

            if (!ids.Add(a.Id))
            {
                Debug.LogError($"Duplicate ID: {a.Id}", this);
                return false;
            }

            if (a.CanPlaceOnTimeline &&
                (a.BaseCost < 1 || a.BaseCost > ActionBar.SlotCount))
            {
                Debug.LogError($"Arcana {a.Id} invalid cost: {a.BaseCost}", this);
                return false;
            }

            ArcanaData catalogEntry = arcanaCatalog.GetById(a.Id);
            if (catalogEntry == null)
            {
                Debug.LogError($"Arcana {a.Id} missing from catalog.", this);
                return false;
            }

            if (catalogEntry != a)
            {
                Debug.LogError($"Pool Arcana {a.Id} doesn't match catalog asset.", this);
                return false;
            }
        }

        return true;
    }

    private void BeginPlanningPhase()
    {
        plannedActions.Clear();
        usedSlots = 0;
        playerGridPos = playerDisplay.GridPosition;
        actionBar.ClearAll();
        playerHand.DrawToCapacity();
        planningActive = true;
        CancelCardDrag();
    }

    private void Update()
    {
        if (!planningActive)
            return;

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

        if (kb.tabKey.wasPressedThisFrame)
            UndoLastAction();
        else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            ConfirmPlan();
        else if (kb.leftCtrlKey.wasPressedThisFrame || kb.rightCtrlKey.wasPressedThisFrame)
            QueueStay();
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
            QueueMove(Vector2Int.up);
        else if (kb.sKey.wasPressedThisFrame)
            QueueMove(Vector2Int.down);
        else if (kb.aKey.wasPressedThisFrame)
            QueueMove(Vector2Int.left);
        else if (kb.dKey.wasPressedThisFrame)
            QueueMove(Vector2Int.right);
    }

    private void QueueMove(Vector2Int direction)
    {
        if (usedSlots >= ActionBar.SlotCount)
            return;

        Vector2Int newPos = playerGridPos + direction;

        if (!gridManager.IsValidCoordinate(newPos.x, newPos.y))
            return;

        plannedActions.Add(new PlannedAction
        {
            Type = ActionType.Move,
            Direction = direction,
            Cost = 1
        });

        usedSlots++;
        playerGridPos = newPos;
        playerDisplay.UpdateGridPosition(newPos.x, newPos.y);
        actionBar.FillSlot(usedSlots - 1, ActionType.Move);
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
        actionBar.FillSlot(usedSlots - 1, ActionType.Stay);
    }

    private void QueueCardUse(int handIndex)
    {
        if (!playerHand.TryGetCard(handIndex, out ArcanaData card))
            return;

        if (!card.CanPlaceOnTimeline)
        {
            Debug.Log($"{card.DisplayNumber} cannot be used during the Planning Phase.");
            return;
        }

        if (usedSlots + card.BaseCost > ActionBar.SlotCount)
            return;

        playerHand.TryTakeCard(handIndex, out _);

        plannedActions.Add(new PlannedAction
        {
            Type = ActionType.UseCard,
            Direction = Vector2Int.zero,
            Cost = card.BaseCost,
            CardData = card,
            OriginalHandIndex = handIndex
        });

        actionBar.FillRange(usedSlots, card.BaseCost, card.TimelineColor);
        usedSlots += card.BaseCost;
    }

    private void UndoLastAction()
    {
        if (plannedActions.Count == 0)
            return;

        PlannedAction action = plannedActions[^1];
        plannedActions.RemoveAt(plannedActions.Count - 1);

        usedSlots -= action.Cost;

        switch (action.Type)
        {
            case ActionType.Move:
                playerGridPos -= action.Direction;
                playerDisplay.UpdateGridPosition(playerGridPos.x, playerGridPos.y);
                actionBar.ClearSlot(usedSlots);
                break;
            case ActionType.Stay:
                actionBar.ClearSlot(usedSlots);
                break;
            case ActionType.UseCard:
                actionBar.ClearRange(usedSlots, action.Cost);
                playerHand.ReturnCard(action.OriginalHandIndex, action.CardData);
                break;
            case ActionType.MergeCards:
                playerHand.UndoMerge(
                    action.MergeResultIndex,
                    action.MergeSource1, action.MergeSourceIndex1,
                    action.MergeSource2, action.MergeSourceIndex2);
                break;
        }
    }

    private void ConfirmPlan()
    {
        if (usedSlots != ActionBar.SlotCount)
            return;

        planningActive = false;
        Debug.Log("전투 페이즈 시작");
    }
}
