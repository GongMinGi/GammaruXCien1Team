using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ModifiedAttackEffectDef",
    menuName = "Battle/Arcana Effects/Modified Attack Effect Def")]
public class ModifiedAttackEffectDef : ArcanaEffectDefinition
{
    [Header("최종 슬롯 데미지")]
    [SerializeField, Min(0)] private int baseValue;
    [SerializeField, Min(0f)] private float spellPowerCoefficient = 1.0f;
    [SerializeField] private int additionalEffectValue;

    [Header("시전 슬롯 수정자")]
    [SerializeField] private int incomingDamageModifier;

    [Header("카운터 스탠스 (선택)")]
    [SerializeField] private bool enableCounterStance;
    [SerializeField] private int counterStanceBonusPerHit;

    public override ScheduledEffect[][] Expand(ArcanaData card)
    {
        if (card == null)
            throw new ArgumentNullException(nameof(card));
        if (card.BaseCost <= 0)
            throw new InvalidOperationException(
                $"ModifiedAttackEffectDef({name}): BaseCost {card.BaseCost}. 1 이상이어야 함.");

        int cost = card.BaseCost;
        ScheduledEffect[][] effects = new ScheduledEffect[cost][];

        for (int i = 0; i < cost - 1; i++)
        {
            var slotEffects = new List<ScheduledEffect>();

            slotEffects.Add(new ScheduledEffect
            {
                Type = EffectType.Cast,
                SourceCard = card,
                Element = card.DefaultElement
            });

            if (incomingDamageModifier != 0)
            {
                slotEffects.Add(new ScheduledEffect
                {
                    Type = EffectType.IncomingDamageModifier,
                    BaseValue = incomingDamageModifier
                });
            }

            if (enableCounterStance)
            {
                slotEffects.Add(new ScheduledEffect
                {
                    Type = EffectType.CounterStance,
                    BaseValue = counterStanceBonusPerHit
                });
            }

            effects[i] = slotEffects.ToArray();
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
