using System.Collections.Generic;
using UnityEngine;

public abstract class BossPatternDefinition : ScriptableObject
{
    public abstract int ResolvePhase(BossStats stats, int currentPhase);
    public abstract int SelectPatternIndex(int phase, int phaseTurnIndex);

    public abstract BossPatternPlan BuildPattern(
        int phase,
        int patternIndex,
        IReadOnlyList<Vector2Int> playerPositionsBySlot);

    public virtual bool ValidateDefinition()
    {
        return true;
    }
}
