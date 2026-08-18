using UnityEngine;

public static class DamageCalculator
{
    public static int CalculateBossDamage(int baseDamage)
    {
        return CalculateBossDamage(baseDamage, Random.Range(0.98f, 1.02f));
    }

    public static int CalculateBossDamage(int baseDamage, float multiplier)
    {
        if (baseDamage <= 0) return 0;
        float clamped = Mathf.Clamp(multiplier, 0.98f, 1.02f);
        return Mathf.Max(0, Mathf.RoundToInt(baseDamage * clamped));
    }

    public static int CalculatePlayerDamage(
        int absoluteValue,
        float spellPowerCoefficient,
        int additionalEffectValue,
        int spellPower,
        DamageElement attackElement,
        DamageElement bossWeakness)
    {
        float raw = absoluteValue
            + spellPowerCoefficient * spellPower
            + additionalEffectValue * spellPower * 0.5f;

        if (attackElement != DamageElement.Neutral && attackElement == bossWeakness)
            raw *= 1.2f;

        return Mathf.Max(0, Mathf.RoundToInt(raw));
    }
}
