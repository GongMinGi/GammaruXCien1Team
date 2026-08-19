using UnityEngine;

public class CombatResolver
{
    public int ResolveBossDamage(BossAction action)
    {
        return DamageCalculator.CalculateBossDamage(action.baseDamage);
    }

    public int ResolvePlayerDamage(
        ScheduledEffect effect,
        int spellPower,
        DamageElement bossWeakness)
    {
        return DamageCalculator.CalculatePlayerDamage(
            effect.BaseValue,
            effect.SpellPowerCoefficient,
            effect.AdditionalEffectValue,
            spellPower,
            effect.Element,
            bossWeakness);
    }

    public bool IsHit(
        Vector2Int playerPos,
        Vector2Int[] targetCells,
        bool isDodging,
        bool ignoresDodge = false,
        bool blockedByTangle = false,
        bool playerOnTangle = false)
    {
        if (isDodging && !ignoresDodge)
            return false;

        // 실타래 안에 있으면 회피 무시 공격도 막힌다
        if (blockedByTangle && playerOnTangle)
            return false;

        if (targetCells == null)
            return false;

        foreach (Vector2Int cell in targetCells)
        {
            if (cell == playerPos)
                return true;
        }

        return false;
    }
}
