using System.Collections.Generic;
using UnityEngine;

public class BossAI : MonoBehaviour
{
    [SerializeField] private BossStats bossStats;

    private int currentPhase;
    private int phaseTurnIndex;
    private int currentPatternIndex = -1;

    private void Awake()
    {
        if (bossStats == null)
            bossStats = GetComponent<BossStats>();
    }

    public bool ValidateReferences()
    {
        if (bossStats == null)
            bossStats = GetComponent<BossStats>();

        if (bossStats == null)
        {
            Debug.LogError("BossAI requires BossStats.", this);
            return false;
        }

        if (bossStats.Data == null || bossStats.Data.Pattern == null)
        {
            Debug.LogError("BossAI pattern definition is not assigned.", this);
            return false;
        }

        return bossStats.Data.Pattern.ValidateDefinition();
    }

    public BossPatternPlan GeneratePattern(Vector2Int playerPosition)
    {
        if (!ValidateReferences())
            return null;

        int resolvedPhase =
            bossStats.Data.Pattern.ResolvePhase(bossStats, currentPhase);
        if (resolvedPhase != currentPhase)
        {
            currentPhase = resolvedPhase;
            phaseTurnIndex = 0;
        }

        currentPatternIndex =
            bossStats.Data.Pattern.SelectPatternIndex(currentPhase, phaseTurnIndex);
        phaseTurnIndex++;

        return BuildCurrentPattern(
            CreateConstantPositions(playerPosition));
    }

    public BossPatternPlan RebuildCurrentPattern(
        IReadOnlyList<Vector2Int> playerPositionsBySlot)
    {
        if (!ValidateReferences() || currentPatternIndex < 0)
            return null;

        return BuildCurrentPattern(playerPositionsBySlot);
    }

    private BossPatternPlan BuildCurrentPattern(
        IReadOnlyList<Vector2Int> playerPositionsBySlot)
    {
        BossPatternPlan plan = bossStats.Data.Pattern.BuildPattern(
            currentPhase,
            currentPatternIndex,
            playerPositionsBySlot);

        return ValidatePlan(plan) ? plan : null;
    }

    private static Vector2Int[] CreateConstantPositions(Vector2Int position)
    {
        Vector2Int[] positions = new Vector2Int[ActionBar.SlotCount];
        for (int i = 0; i < positions.Length; i++)
            positions[i] = position;
        return positions;
    }

    private bool ValidatePlan(BossPatternPlan plan)
    {
        if (plan == null || plan.Intents == null || plan.Actions == null ||
            plan.Intents.Length == 0 || plan.Actions.Length == 0)
        {
            Debug.LogError("BossAI generated an empty pattern.", this);
            return false;
        }

        foreach (BossIntent intent in plan.Intents)
        {
            if (intent.timingSlot < 0 ||
                intent.timingSlot >= ActionBar.SlotCount ||
                intent.arcanaIds == null || intent.arcanaIds.Length == 0)
            {
                Debug.LogError("BossAI generated an invalid intent.", this);
                return false;
            }
        }

        foreach (BossAction action in plan.Actions)
        {
            if (action.timingSlot < 0 ||
                action.timingSlot >= ActionBar.SlotCount ||
                action.targetCells == null || action.targetCells.Length == 0 ||
                action.baseDamage < 0)
            {
                Debug.LogError("BossAI generated an invalid action.", this);
                return false;
            }
        }

        return true;
    }
}
