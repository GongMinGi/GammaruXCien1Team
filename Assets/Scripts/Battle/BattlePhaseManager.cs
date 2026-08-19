using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 비활성화됨 — BattleExecutor로 대체
[System.Obsolete("BattleExecutor로 대체")]
public class BattlePhaseManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerDisplay playerDisplay;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private float slotDuration = 0.5f;
    [SerializeField] private Color attackHighlightColor = new Color(1f, 0.2f, 0.2f, 0.8f);

    public void StartBattlePhase(
        List<PlannedAction> actions,
        BossAction[] bossPattern,
        Vector2Int startPos)
    {
        StartCoroutine(ExecutePhase(actions, bossPattern, startPos));
    }

    private IEnumerator ExecutePhase(
        List<PlannedAction> actions,
        BossAction[] bossPattern,
        Vector2Int startPos)
    {
        Vector2Int currentPos = startPos;
        playerDisplay.UpdateGridPosition(startPos.x, startPos.y);
        yield return new WaitForSeconds(0.3f);

        int actionIndex = 0;
        int slotInAction = 0;

        while (actionIndex < actions.Count && actions[actionIndex].Cost == 0)
            actionIndex++;

        for (int slot = 0; slot < ActionBar.SlotCount; slot++)
        {
            if (actionIndex < actions.Count && slotInAction == 0)
            {
                PlannedAction action = actions[actionIndex];
                if (action.Type == ActionType.Move)
                {
                    currentPos += action.Direction;
                    playerDisplay.UpdateGridPosition(currentPos.x, currentPos.y);
                }
            }

            foreach (BossAction bossAction in bossPattern)
            {
                if (bossAction.timingSlot != slot) continue;

                bool hitByThis = false;
                foreach (Vector2Int cell in bossAction.targetCells)
                {
                    GridCell gridCell = gridManager.GetCell(cell.x, cell.y);
                    gridCell?.SetHighlight(attackHighlightColor);

                    if (cell == currentPos)
                        hitByThis = true;
                }

                if (hitByThis && playerStats != null)
                {
                    if (bossAction.isInstantKill)
                        playerStats.InstantKill();
                    else
                        playerStats.TakeDamage(
                            DamageCalculator.CalculateBossDamage(bossAction.baseDamage));
                }
            }

            yield return new WaitForSeconds(slotDuration);
            gridManager.ClearAllHighlights();

            if (actionIndex < actions.Count)
            {
                slotInAction++;
                if (slotInAction >= actions[actionIndex].Cost)
                {
                    actionIndex++;
                    slotInAction = 0;

                    while (actionIndex < actions.Count && actions[actionIndex].Cost == 0)
                        actionIndex++;
                }
            }
        }

        Debug.Log("전투 페이즈 종료");
    }
}
