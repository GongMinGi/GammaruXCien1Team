using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "RepeatingAttackEffectDef",
    menuName = "Battle/Arcana Effects/Repeating Attack Effect Def")]
public class RepeatingAttackEffectDef : ArcanaEffectDefinition
{
    [Header("첫 슬롯")]
    [SerializeField] private EffectType firstSlotEffect = EffectType.Cast;

    [Header("타격 데미지 (각 회)")]
    [SerializeField, Min(0)] private int baseValue;
    [SerializeField, Min(0f)] private float spellPowerCoefficient = 1.0f;
    [SerializeField] private int additionalEffectValue;

    [Header("타격당 부가 효과")]
    [SerializeField] private bool healPerHit;
    [SerializeField, Min(0)] private int healPerHitValue;
    [SerializeField] private bool burnPerHit;
    [SerializeField, Min(0)] private int burnPerHitStacks;

    public override ScheduledEffect[][] Expand(ArcanaData card)
    {
        if (card == null)
            throw new ArgumentNullException(nameof(card));
        if (card.BaseCost < 2)
            throw new InvalidOperationException(
                $"RepeatingAttackEffectDef({name}): BaseCost {card.BaseCost}. 2 이상이어야 함.");

        int cost = card.BaseCost;
        ScheduledEffect[][] effects = new ScheduledEffect[cost][];

        effects[0] = new[] { new ScheduledEffect
        {
            Type = firstSlotEffect,
            SourceCard = card,
            Element = card.DefaultElement
        }};

        for (int i = 1; i < cost; i++)
        {
            var slotEffects = new List<ScheduledEffect>();

            slotEffects.Add(new ScheduledEffect
            {
                Type = EffectType.DealDamage,
                SourceCard = card,
                Element = card.DefaultElement,
                BaseValue = baseValue,
                SpellPowerCoefficient = spellPowerCoefficient,
                AdditionalEffectValue = additionalEffectValue
            });

            if (healPerHit)
            {
                slotEffects.Add(new ScheduledEffect
                {
                    Type = EffectType.Heal,
                    SourceCard = card,
                    BaseValue = healPerHitValue
                });
            }

            if (burnPerHit)
            {
                slotEffects.Add(new ScheduledEffect
                {
                    Type = EffectType.ApplyBurn,
                    SourceCard = card,
                    BaseValue = burnPerHitStacks
                });
            }

            effects[i] = slotEffects.ToArray();
        }

        return effects;
    }
}
