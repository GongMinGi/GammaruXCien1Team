using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DodgeEffectDef",
    menuName = "Battle/Arcana Effects/Dodge Effect Def")]
public class DodgeEffectDef : ArcanaEffectDefinition
{
    public override ScheduledEffect[][] Expand(ArcanaData card)
    {
        if (card == null)
            throw new ArgumentNullException(nameof(card));
        if (card.BaseCost <= 0)
            throw new InvalidOperationException(
                $"DodgeEffectDef({name}): BaseCost {card.BaseCost}. 1 이상이어야 함.");

        int cost = card.BaseCost;
        ScheduledEffect[][] effects = new ScheduledEffect[cost][];

        for (int i = 0; i < cost; i++)
        {
            effects[i] = new[] { new ScheduledEffect
            {
                Type = EffectType.Dodge,
                SourceCard = card
            }};
        }

        return effects;
    }
}
