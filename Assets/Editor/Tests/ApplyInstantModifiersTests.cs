using NUnit.Framework;

public class ApplyInstantModifiersTests
{
    private static ScheduledEffect[][] MakeAttackEffects()
    {
        return new[]
        {
            new[] { new ScheduledEffect { Type = EffectType.Cast } },
            new[] { new ScheduledEffect { Type = EffectType.Cast } },
            new[] { new ScheduledEffect
            {
                Type = EffectType.DealDamage,
                BaseValue = 5,
                Element = DamageElement.Neutral
            }}
        };
    }

    [Test]
    public void CostReduction_RemovesFirstSlot()
    {
        ScheduledEffect[][] input = MakeAttackEffects();

        ScheduledEffect[][] result = BattleFlowController.ApplyInstantModifiers(
            input, InstantModifierType.CostReduction, false, DamageElement.Neutral);

        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(EffectType.Cast, result[0][0].Type);
        Assert.AreEqual(EffectType.DealDamage, result[1][0].Type);
        Assert.AreEqual(5, result[1][0].BaseValue);
    }

    [Test]
    public void CostReduction_SingleSlot_NoChange()
    {
        ScheduledEffect[][] input = new[]
        {
            new[] { new ScheduledEffect { Type = EffectType.DealDamage, BaseValue = 5 } }
        };

        ScheduledEffect[][] result = BattleFlowController.ApplyInstantModifiers(
            input, InstantModifierType.CostReduction, false, DamageElement.Neutral);

        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(EffectType.DealDamage, result[0][0].Type);
    }

    [Test]
    public void EffectDuplication_DuplicatesLastSlot()
    {
        ScheduledEffect[][] input = MakeAttackEffects();

        ScheduledEffect[][] result = BattleFlowController.ApplyInstantModifiers(
            input, InstantModifierType.EffectDuplication, false, DamageElement.Neutral);

        Assert.AreEqual(4, result.Length);
        Assert.AreEqual(EffectType.Cast, result[0][0].Type);
        Assert.AreEqual(EffectType.Cast, result[1][0].Type);
        Assert.AreEqual(EffectType.DealDamage, result[2][0].Type);
        Assert.AreEqual(EffectType.DealDamage, result[3][0].Type);
        Assert.AreEqual(5, result[3][0].BaseValue);
    }

    [Test]
    public void ElementBuff_ChangesFirstDealDamageOnly()
    {
        ScheduledEffect[][] input = new[]
        {
            new[] { new ScheduledEffect { Type = EffectType.Cast } },
            new[] { new ScheduledEffect
            {
                Type = EffectType.DealDamage,
                Element = DamageElement.Neutral
            }},
            new[] { new ScheduledEffect
            {
                Type = EffectType.DealDamage,
                Element = DamageElement.Neutral
            }}
        };

        ScheduledEffect[][] result = BattleFlowController.ApplyInstantModifiers(
            input, InstantModifierType.None, true, DamageElement.Sun);

        Assert.AreEqual(DamageElement.Sun, result[1][0].Element);
        Assert.AreEqual(DamageElement.Neutral, result[2][0].Element);
    }

    [Test]
    public void ElementBuff_NoDealDamage_NoChange()
    {
        ScheduledEffect[][] input = new[]
        {
            new[] { new ScheduledEffect { Type = EffectType.Cast } },
            new[] { new ScheduledEffect { Type = EffectType.Dodge } }
        };

        ScheduledEffect[][] result = BattleFlowController.ApplyInstantModifiers(
            input, InstantModifierType.None, true, DamageElement.Moon);

        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(EffectType.Cast, result[0][0].Type);
        Assert.AreEqual(EffectType.Dodge, result[1][0].Type);
    }

    [Test]
    public void CostReduction_Plus_ElementBuff_BothApply()
    {
        ScheduledEffect[][] input = MakeAttackEffects();

        ScheduledEffect[][] result = BattleFlowController.ApplyInstantModifiers(
            input, InstantModifierType.CostReduction, true, DamageElement.Star);

        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(EffectType.Cast, result[0][0].Type);
        Assert.AreEqual(EffectType.DealDamage, result[1][0].Type);
        Assert.AreEqual(DamageElement.Star, result[1][0].Element);
    }

    [Test]
    public void EffectDuplication_Plus_ElementBuff_BothApply()
    {
        ScheduledEffect[][] input = MakeAttackEffects();

        ScheduledEffect[][] result = BattleFlowController.ApplyInstantModifiers(
            input, InstantModifierType.EffectDuplication, true, DamageElement.Moon);

        Assert.AreEqual(4, result.Length);
        Assert.AreEqual(EffectType.DealDamage, result[2][0].Type);
        Assert.AreEqual(DamageElement.Moon, result[2][0].Element);
        // 복제된 슬롯은 원본의 Element 유지 (ElementBuff는 첫 DealDamage만)
        Assert.AreEqual(DamageElement.Neutral, result[3][0].Element);
    }

    [Test]
    public void NoModifiers_ReturnsSameArray()
    {
        ScheduledEffect[][] input = MakeAttackEffects();

        ScheduledEffect[][] result = BattleFlowController.ApplyInstantModifiers(
            input, InstantModifierType.None, false, DamageElement.Neutral);

        Assert.AreSame(input, result);
    }
}
