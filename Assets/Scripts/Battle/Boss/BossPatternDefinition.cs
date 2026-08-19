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

    /// <summary>
    /// 이번 턴 종료에 시전되는 아르카나. 타임라인 슬롯을 쓰지 않아
    /// 인텐트로 만들 수 없는 것들을 표시용으로만 알려준다. 없으면 빈 배열.
    /// </summary>
    public virtual int[] TurnEndArcanaIds => System.Array.Empty<int>();

    public virtual bool ValidateDefinition()
    {
        return true;
    }
}
