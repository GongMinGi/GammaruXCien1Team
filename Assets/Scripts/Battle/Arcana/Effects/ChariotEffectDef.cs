using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ChariotEffectDef",
    menuName = "Battle/Arcana Effects/Chariot Effect Def")]
public class ChariotEffectDef : ArcanaEffectDefinition
{
    [Header("돌진 거리")]
    [SerializeField, Min(1)] private int rushDistance = 3;

    public override bool RequiresDirection => true;

    public override ScheduledEffect[][] Expand(ArcanaData card)
    {
        if (card == null)
            throw new ArgumentNullException(nameof(card));

        int cost = card.BaseCost;
        if (cost < 2)
            throw new InvalidOperationException(
                $"ChariotEffectDef({name}): card {card.Id}({card.ArcanaName})의 " +
                $"BaseCost가 {cost}. 2 이상이어야 함.");

        ScheduledEffect[][] effects = new ScheduledEffect[cost][];

        for (int i = 0; i < cost - 1; i++)
        {
            effects[i] = new[] { new ScheduledEffect
            {
                Type = EffectType.Cast,
                SourceCard = card
            }};
        }

        effects[cost - 1] = new[] { new ScheduledEffect
        {
            Type = EffectType.Move,
            BaseValue = rushDistance,
            SourceCard = card
        }};

        return effects;
    }
}
