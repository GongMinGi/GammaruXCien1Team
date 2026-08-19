using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [SerializeField] private BossAI bossAI;
    [SerializeField] private BossCardDisplay bossCardDisplay;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ArcanaCatalog arcanaCatalog;

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

        return true;
    }

    public void GenerateAndShow()
    {
        currentPattern = bossAI.GeneratePattern();
        bossCardDisplay.ShowPattern(currentPattern, arcanaCatalog);
        ShowBossTargetsOnGrid();
    }

    private void ShowBossTargetsOnGrid()
    {
        gridManager.ClearAllHighlights();

        foreach (BossAction action in currentPattern)
        {
            if (action.targetCells == null)
                continue;

            foreach (Vector2Int cell in action.targetCells)
            {
                GridCell gridCell = gridManager.GetCell(cell.x, cell.y);
                gridCell?.SetHighlight(new Color(1f, 0.3f, 0.3f, 0.5f));
            }
        }
    }
}
