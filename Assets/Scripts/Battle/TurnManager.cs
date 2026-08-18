using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerDisplay playerDisplay;
    [SerializeField] private ActionBar actionBar;

    private readonly List<PlannedAction> plannedActions = new();
    private int usedSlots;
    private Vector2Int playerGridPos;
    private bool planningActive;

    private void Start()
    {
        if (gridManager == null || playerDisplay == null || actionBar == null)
        {
            Debug.LogError("TurnManager references are not assigned.", this);
            enabled = false;
            return;
        }

        playerGridPos = playerDisplay.GridPosition;
        planningActive = true;
    }

    private void Update()
    {
        if (!planningActive || Keyboard.current == null)
            return;

        Keyboard kb = Keyboard.current;

        if (kb.tabKey.wasPressedThisFrame)
            UndoLastAction();
        else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            ConfirmPlan();
        else if (kb.leftCtrlKey.wasPressedThisFrame || kb.rightCtrlKey.wasPressedThisFrame)
            QueueStay();
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

    private void UndoLastAction()
    {
        if (plannedActions.Count == 0)
            return;

        PlannedAction action = plannedActions[^1];
        plannedActions.RemoveAt(plannedActions.Count - 1);

        usedSlots -= action.Cost;

        if (action.Type == ActionType.Move)
        {
            playerGridPos -= action.Direction;
            playerDisplay.UpdateGridPosition(playerGridPos.x, playerGridPos.y);
        }

        actionBar.ClearSlot(usedSlots);
    }

    private void ConfirmPlan()
    {
        if (usedSlots != ActionBar.SlotCount)
            return;

        planningActive = false;
        Debug.Log("전투 페이즈 시작");
    }
}
