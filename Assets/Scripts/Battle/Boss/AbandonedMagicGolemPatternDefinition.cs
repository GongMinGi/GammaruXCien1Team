using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AbandonedMagicGolemPattern",
    menuName = "Battle/Boss Patterns/Abandoned Magic Golem")]
public class AbandonedMagicGolemPatternDefinition : BossPatternDefinition
{
    private enum AttackKind
    {
        I, II, IThenII, IIThenI, VII, VIIThenI, VIIThenII, VIII
    }

    [Header("Attack Damage")]
    [SerializeField, Min(0)] private int punchDamage = 10;
    [SerializeField, Min(0)] private int explosionDamage = 10;
    [SerializeField, Min(0)] private int crossDamage = 10;
    [SerializeField, Min(0)] private int diagonalDamage = 10;
    [SerializeField, Min(0)] private int diamondDamage = 10;
    [SerializeField, Min(0)] private int shockwaveDamage = 10;
    [SerializeField] private DamageElement attackElement = DamageElement.Neutral;

    public override int ResolvePhase(BossStats stats, int currentPhase)
    {
        if (currentPhase >= 1)
            return 1;

        return (long)stats.CurrentHp * 2 <= stats.MaxHp ? 1 : 0;
    }

    public override int SelectPatternIndex(int phase, int phaseTurnIndex)
    {
        if (phaseTurnIndex < 5)
            return phaseTurnIndex;

        return phase == 0 ? Random.Range(2, 5) : Random.Range(0, 5);
    }

    public override BossPatternPlan BuildPattern(
        int phase,
        int patternIndex,
        IReadOnlyList<Vector2Int> playerPositionsBySlot)
    {
        if (playerPositionsBySlot == null ||
            playerPositionsBySlot.Count != ActionBar.SlotCount)
            return null;

        List<BossIntent> intents = new();
        List<BossAction> actions = new();

        if (phase == 0)
            BuildPhaseOne(patternIndex, playerPositionsBySlot, intents, actions);
        else
            BuildPhaseTwo(patternIndex, playerPositionsBySlot, intents, actions);

        return new BossPatternPlan(intents.ToArray(), actions.ToArray());
    }

    private void BuildPhaseOne(int index,
        IReadOnlyList<Vector2Int> positions,
        List<BossIntent> intents,
        List<BossAction> actions)
    {
        switch (index)
        {
            case 0:
                AddCast(2, AttackKind.I, positions, intents, actions);
                AddCast(6, AttackKind.I, positions, intents, actions);
                break;
            case 1:
                AddCast(3, AttackKind.I, positions, intents, actions);
                AddCast(8, AttackKind.II, positions, intents, actions);
                break;
            case 2:
                AddCast(2, AttackKind.I, positions, intents, actions);
                AddCast(5, AttackKind.II, positions, intents, actions);
                AddCast(8, AttackKind.IThenII, positions, intents, actions);
                break;
            case 3:
                AddCast(2, AttackKind.II, positions, intents, actions);
                AddCast(5, AttackKind.I, positions, intents, actions);
                AddCast(8, AttackKind.IIThenI, positions, intents, actions);
                break;
            case 4:
                AddCast(1, AttackKind.IThenII, positions, intents, actions);
                AddCast(5, AttackKind.I, positions, intents, actions);
                AddCast(8, AttackKind.IIThenI, positions, intents, actions);
                break;
        }
    }

    private void BuildPhaseTwo(int index,
        IReadOnlyList<Vector2Int> positions,
        List<BossIntent> intents,
        List<BossAction> actions)
    {
        switch (index)
        {
            case 0:
                AddCast(1, AttackKind.VIII, positions, intents, actions);
                AddCast(6, AttackKind.IIThenI, positions, intents, actions);
                break;
            case 1:
                AddCast(2, AttackKind.VII, positions, intents, actions);
                AddCast(7, AttackKind.IThenII, positions, intents, actions);
                break;
            case 2:
                AddCast(1, AttackKind.IThenII, positions, intents, actions);
                AddCast(5, AttackKind.VIIThenI, positions, intents, actions);
                break;
            case 3:
                AddCast(1, AttackKind.IIThenI, positions, intents, actions);
                AddCast(5, AttackKind.VIIThenII, positions, intents, actions);
                break;
            case 4:
                AddCast(2, AttackKind.VIIThenI, positions, intents, actions);
                AddCast(6, AttackKind.VIIThenII, positions, intents, actions);
                break;
        }
    }

    private void AddCast(int startSlotOneBased, AttackKind kind,
        IReadOnlyList<Vector2Int> positions,
        List<BossIntent> intents,
        List<BossAction> actions)
    {
        int startSlot = startSlotOneBased - 1;
        Vector2Int playerPosition = positions[startSlot];

        intents.Add(new BossIntent
        {
            timingSlot = startSlot,
            arcanaIds = GetArcanaIds(kind),
            isInstantKill = false
        });

        switch (kind)
        {
            case AttackKind.I:
                AddHit(actions, startSlot + 1,
                    BossTargetResolver.Center(playerPosition), punchDamage);
                break;
            case AttackKind.II:
                AddHit(actions, startSlot + 1,
                    BossTargetResolver.CardinalNeighbors(playerPosition), explosionDamage);
                break;
            case AttackKind.IThenII:
                AddHit(actions, startSlot + 1,
                    BossTargetResolver.Center(playerPosition), punchDamage);
                AddHit(actions, startSlot + 2,
                    BossTargetResolver.CardinalNeighbors(playerPosition), explosionDamage);
                break;
            case AttackKind.IIThenI:
                AddHit(actions, startSlot + 1,
                    BossTargetResolver.CardinalNeighbors(playerPosition), explosionDamage);
                AddHit(actions, startSlot + 2,
                    BossTargetResolver.Center(playerPosition), punchDamage);
                break;
            case AttackKind.VII:
                AddHit(actions, startSlot + 2,
                    BossTargetResolver.Cross(), crossDamage);
                break;
            case AttackKind.VIIThenI:
                AddHit(actions, startSlot + 2,
                    BossTargetResolver.DiagonalCross(), diagonalDamage);
                break;
            case AttackKind.VIIThenII:
                AddHit(actions, startSlot + 2,
                    BossTargetResolver.DiamondPerimeter(), diamondDamage);
                break;
            case AttackKind.VIII:
                AddHit(actions, startSlot + 3,
                    BossTargetResolver.Center(Vector2Int.zero), shockwaveDamage);
                AddHit(actions, startSlot + 4,
                    BossTargetResolver.ManhattanRing(Vector2Int.zero, 1), shockwaveDamage);
                AddHit(actions, startSlot + 5,
                    BossTargetResolver.ManhattanRing(Vector2Int.zero, 2), shockwaveDamage);
                break;
        }
    }

    private void AddHit(List<BossAction> actions, int timingSlot,
        Vector2Int[] cells, int damage)
    {
        actions.Add(new BossAction
        {
            timingSlot = timingSlot,
            arcanaIds = System.Array.Empty<int>(),
            targetCells = cells,
            baseDamage = damage,
            isInstantKill = false,
            element = attackElement
        });
    }

    private static int[] GetArcanaIds(AttackKind kind)
    {
        switch (kind)
        {
            case AttackKind.I: return new[] { 1 };
            case AttackKind.II: return new[] { 2 };
            case AttackKind.IThenII: return new[] { 1, 2 };
            case AttackKind.IIThenI: return new[] { 2, 1 };
            case AttackKind.VII: return new[] { 7 };
            case AttackKind.VIIThenI: return new[] { 7, 1 };
            case AttackKind.VIIThenII: return new[] { 7, 2 };
            case AttackKind.VIII: return new[] { 8 };
            default: return System.Array.Empty<int>();
        }
    }
}
