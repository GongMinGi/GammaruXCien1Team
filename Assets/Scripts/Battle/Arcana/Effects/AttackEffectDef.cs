using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AttackEffectDef",
    menuName = "Battle/Arcana Effects/Attack Effect Def")]
public class AttackEffectDef : ArcanaEffectDefinition
{
    [Header("최종 슬롯 데미지 수치")]
    [SerializeField, Min(0)] private int baseValue;
    [SerializeField, Min(0f)] private float spellPowerCoefficient = 1.0f;
    [SerializeField] private int additionalEffectValue;

    public override ScheduledEffect[][] Expand(ArcanaData card)
    {
        if (card == null)
            throw new ArgumentNullException(nameof(card));

        int cost = card.BaseCost;
        if (cost <= 0)
            throw new InvalidOperationException(
                $"AttackEffectDef({name}): card {card.Id}({card.ArcanaName})의 " +
                $"BaseCost가 {cost}. 1 이상이어야 함.");

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
            Type = EffectType.DealDamage,
            SourceCard = card,
            Element = card.DefaultElement,
            BaseValue = baseValue,
            SpellPowerCoefficient = spellPowerCoefficient,
            AdditionalEffectValue = additionalEffectValue
        }};

        return effects;
    }
}
