using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "TowerEffectDef",
    menuName = "Battle/Arcana Effects/Tower Effect Def")]
public class TowerEffectDef : ArcanaEffectDefinition
{
    [Header("타워 보너스 피해")]
    [SerializeField, Min(0)] private int towerBonusDamage = 5;

    public override bool RequiresDirection => true;
    public override int AllowedDirections => 8;

    public override ScheduledEffect[][] Expand(ArcanaData card)
    {
        if (card == null)
            throw new ArgumentNullException(nameof(card));

        int cost = card.BaseCost;
        if (cost < 2)
            throw new InvalidOperationException(
                $"TowerEffectDef({name}): card {card.Id}({card.ArcanaName})의 " +
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
            Type = EffectType.PlaceTower,
            BaseValue = towerBonusDamage,
            SourceCard = card
        }};

        return effects;
    }
}
