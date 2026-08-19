using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "JudgementEffectDef",
    menuName = "Battle/Arcana Effects/Judgement Effect Def")]
public class JudgementEffectDef : ArcanaEffectDefinition
{
    [Header("기본 피해")]
    [SerializeField, Min(0)] private int baseDamage = 30;
    [SerializeField, Min(0f)] private float spellPowerCoefficient = 1.5f;

    [Header("소거 보너스")]
    [SerializeField, Min(0)] private int bonusDamagePerCost = 5;

    public override bool ConsumesHand => true;
    public override int BonusDamagePerConsumedCost => bonusDamagePerCost;

    public override ScheduledEffect[][] Expand(ArcanaData card)
    {
        if (card == null)
            throw new ArgumentNullException(nameof(card));

        int cost = card.BaseCost;
        if (cost < 2)
            throw new InvalidOperationException(
                $"JudgementEffectDef({name}): card {card.Id}({card.ArcanaName})의 " +
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
            Type = EffectType.DealDamage,
            BaseValue = baseDamage,
            SpellPowerCoefficient = spellPowerCoefficient,
            Element = card.DefaultElement,
            SourceCard = card
        }};

        return effects;
    }
}
