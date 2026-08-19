using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [SerializeField] private BossAI bossAI;
    [SerializeField] private BossCardDisplay bossCardDisplay;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ArcanaCatalog arcanaCatalog;
    [SerializeField] private TangleField tangleField;  // 광대 보스일 때만 연결

    private BossAction[] currentPattern;

    public IReadOnlyList<BossAction> LockedPattern => currentPattern;

    public bool ValidateReferences()
    {
        if (bossAI == null || bossCardDisplay == null ||
            gridManager == null || arcanaCatalog == null)
        {
            Debug.LogError("BossController references are not assigned.", this);
            return false;
        }

        return bossAI.ValidateReferences();
    }

    public bool GenerateAndShow(Vector2Int playerPosition)
    {
        BossPatternPlan plan = bossAI.GeneratePattern(playerPosition);
        if (plan == null || plan.Actions == null || plan.Intents == null)
        {
            currentPattern = null;
            Debug.LogError("BossController failed to generate a pattern.", this);
            return false;
        }

        currentPattern = plan.Actions;

        int[] turnEndArcanaIds = bossAI.TurnEndArcanaIds;
        bool hasTurnEnd = turnEndArcanaIds != null && turnEndArcanaIds.Length > 0;

        BossAction[] displayActions =
            new BossAction[plan.Intents.Length + (hasTurnEnd ? 1 : 0)];
        for (int i = 0; i < plan.Intents.Length; i++)
        {
            BossIntent intent = plan.Intents[i];
            displayActions[i] = new BossAction
            {
                timingSlot = intent.timingSlot,
                arcanaIds = intent.arcanaIds,
                isInstantKill = intent.isInstantKill
            };
        }

        if (hasTurnEnd)
        {
            // 타임라인 슬롯을 쓰지 않는 턴 종료 시전(0 / V+0 / IV).
            // timingSlot -1은 BossCardDisplay가 "종료" 라벨로 읽는 표시 전용 값이다.
            displayActions[^1] = new BossAction
            {
                timingSlot = -1,
                arcanaIds = turnEndArcanaIds
            };
        }

        bossCardDisplay.ShowPattern(displayActions, arcanaCatalog);
        return true;
    }

    private static readonly Color WarningBlinkOn = new Color(1f, 0.3f, 0.3f, 0.5f);

    public void ShowTargetsAtSlot(int slotIndex)
    {
        gridManager.ClearAllHighlights();

        if (currentPattern == null || slotIndex < 0 ||
            slotIndex >= ActionBar.SlotCount)
            return;

        foreach (BossAction action in currentPattern)
        {
            if (action.timingSlot != slotIndex || action.targetCells == null)
                continue;

            foreach (Vector2Int cell in action.targetCells)
            {
                GridCell gridCell = gridManager.GetCell(cell.x, cell.y);
                gridCell?.StartBlink(WarningBlinkOn, gridCell.OriginalColor);
            }
        }
    }


    public void UpdatePlanningPreview(
        IReadOnlyList<PlannedAction> plannedActions,
        Vector2Int startPosition,
        int previewSlot)
    {
        Vector2Int[] positions =
            BuildPlayerPositionsBySlot(plannedActions, startPosition);
        if (positions == null)
        {
            currentPattern = null;
            gridManager.ClearAllHighlights();
            Debug.LogError("Failed to simulate planned player positions.", this);
            return;
        }

        BossPatternPlan plan = bossAI.RebuildCurrentPattern(positions);
        if (plan == null)
        {
            currentPattern = null;
            gridManager.ClearAllHighlights();
            Debug.LogError("Failed to rebuild boss targets.", this);
            return;
        }

        currentPattern = plan.Actions;
        ShowTargetsAtSlot(previewSlot);
    }


    private Vector2Int[] BuildPlayerPositionsBySlot(
        IReadOnlyList<PlannedAction> plannedActions,
        Vector2Int startPosition)
    {
        if (plannedActions == null)
            return null;

        Vector2Int[] positions = new Vector2Int[ActionBar.SlotCount];
        Vector2Int currentPosition = startPosition;
        int slotCursor = 0;

        foreach (PlannedAction action in plannedActions)
        {
            if (action.Type == ActionType.MergeCards ||
                action.Type == ActionType.UseObservationCard)
                continue;

            if (action.Type == ActionType.UseInstantCard)
            {
                continue;
            }

            if (action.Cost <= 0 ||
                slotCursor + action.Cost > ActionBar.SlotCount)
                return null;

            bool onTangle =
                tangleField != null && tangleField.Contains(currentPosition);

            if (action.Type == ActionType.Move)
            {
                if (!onTangle)
                    currentPosition += action.Direction;
            }
            else if (action.Type == ActionType.UseCard &&
                     action.Direction != Vector2Int.zero &&
                     action.CardData != null &&
                     action.CardData.EffectDefinition != null)
            {
                currentPosition = SimulateCardMovement(
                    currentPosition, action.Direction, action.CardData);
                if (action.DuplicatedDirection != Vector2Int.zero)
                    currentPosition = SimulateCardMovement(
                        currentPosition, action.DuplicatedDirection, action.CardData);
            }

            for (int i = 0; i < action.Cost; i++)
                positions[slotCursor + i] = currentPosition;

            slotCursor += action.Cost;
        }

        for (int slot = slotCursor; slot < positions.Length; slot++)
            positions[slot] = currentPosition;

        return positions;
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

                // 실타래 위에서는 이동이 막힌다 — 예고 위치가 실행과 어긋나지 않게 한다
                if (tangleField != null && tangleField.Contains(currentPos))
                    continue;

                int dist = effect.BaseValue > 0 ? effect.BaseValue : 1;
                Vector2Int target = currentPos;
                for (int step = 1; step <= dist; step++)
                {
                    Vector2Int check = currentPos + direction * step;
                    if (check.x < -GridManager.Columns / 2 ||
                        check.x > GridManager.Columns / 2 ||
                        check.y < -GridManager.Rows / 2 ||
                        check.y > GridManager.Rows / 2)
                        break;
                    target = check;
                }
                currentPos = target;
            }
        }

        return currentPos;
    }
}
