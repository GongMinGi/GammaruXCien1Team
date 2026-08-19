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

    public bool IsHit(Vector2Int playerPos, Vector2Int[] targetCells, bool isDodging)
    {
        if (isDodging)
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
