using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "HealEffectDef",
    menuName = "Battle/Arcana Effects/Heal Effect Def")]
public class HealEffectDef : ArcanaEffectDefinition
{
    [Header("회복량")]
    [SerializeField, Min(0)] private int baseHealValue;

    public override ScheduledEffect[][] Expand(ArcanaData card)
    {
        if (card == null)
            throw new ArgumentNullException(nameof(card));
        if (card.BaseCost <= 0)
            throw new InvalidOperationException(
                $"HealEffectDef({name}): BaseCost {card.BaseCost}. 1 이상이어야 함.");

        int cost = card.BaseCost;
        ScheduledEffect[][] effects = new ScheduledEffect[cost][];

        for (int i = 0; i < cost - 1; i++)
        {
            effects[i] = new[] { new ScheduledEffect
            {
                Type = EffectType.Cast,
                SourceCard = card,
                Element = card.DefaultElement
            }};
        }

        effects[cost - 1] = new[] { new ScheduledEffect
        {
            Type = EffectType.Heal,
            SourceCard = card,
            BaseValue = baseHealValue
        }};

        return effects;
    }
}
