using System;
using NUnit.Framework;
using UnityEngine;

public class RepeatingAttackEffectDefTests
{
    [Test]
    public void Expand_Star_CastThenDealDamageWithHeal()
    {
        RepeatingAttackEffectDef def = ScriptableObject.CreateInstance<RepeatingAttackEffectDef>();
        SetField(def, "firstSlotEffect", EffectType.Cast);
        SetField(def, "baseValue", 3);
        SetField(def, "spellPowerCoefficient", 0.5f);
        SetField(def, "additionalEffectValue", 1);
        SetField(def, "healPerHit", true);
        SetField(def, "healPerHitValue", 5);
        SetField(def, "burnPerHit", false);

        ArcanaData card = ScriptableObject.CreateInstance<ArcanaData>();
        SetField(card, "id", 17);
        SetField(card, "baseCost", 4);
        SetField(card, "defaultElement", DamageElement.Neutral);

        ScheduledEffect[][] effects = def.Expand(card);

        Assert.AreEqual(4, effects.Length);

        Assert.AreEqual(1, effects[0].Length);
        Assert.AreEqual(EffectType.Cast, effects[0][0].Type);

        for (int i = 1; i < 4; i++)
        {
            Assert.AreEqual(2, effects[i].Length);
            Assert.AreEqual(EffectType.DealDamage, effects[i][0].Type);
            Assert.AreEqual(3, effects[i][0].BaseValue);
            Assert.AreEqual(EffectType.Heal, effects[i][1].Type);
            Assert.AreEqual(5, effects[i][1].BaseValue);
        }

        UnityEngine.Object.DestroyImmediate(def);
        UnityEngine.Object.DestroyImmediate(card);
    }

    [Test]
    public void Expand_Moon_DodgeThenDealDamageOnly()
    {
        RepeatingAttackEffectDef def = ScriptableObject.CreateInstance<RepeatingAttackEffectDef>();
        SetField(def, "firstSlotEffect", EffectType.Dodge);
        SetField(def, "baseValue", 3);
        SetField(def, "spellPowerCoefficient", 0.5f);
        SetField(def, "additionalEffectValue", 1);
        SetField(def, "healPerHit", false);
        SetField(def, "burnPerHit", false);

        ArcanaData card = ScriptableObject.CreateInstance<ArcanaData>();
        SetField(card, "id", 18);
        SetField(card, "baseCost", 4);
        SetField(card, "defaultElement", DamageElement.Neutral);

        ScheduledEffect[][] effects = def.Expand(card);

        Assert.AreEqual(4, effects.Length);

        Assert.AreEqual(1, effects[0].Length);
        Assert.AreEqual(EffectType.Dodge, effects[0][0].Type);

        for (int i = 1; i < 4; i++)
        {
            Assert.AreEqual(1, effects[i].Length);
            Assert.AreEqual(EffectType.DealDamage, effects[i][0].Type);
        }

        UnityEngine.Object.DestroyImmediate(def);
        UnityEngine.Object.DestroyImmediate(card);
    }

    [Test]
    public void Expand_Sun_CastThenDealDamageWithBurn()
    {
        RepeatingAttackEffectDef def = ScriptableObject.CreateInstance<RepeatingAttackEffectDef>();
        SetField(def, "firstSlotEffect", EffectType.Cast);
        SetField(def, "baseValue", 3);
        SetField(def, "spellPowerCoefficient", 0.5f);
        SetField(def, "additionalEffectValue", 1);
        SetField(def, "healPerHit", false);
        SetField(def, "burnPerHit", true);
        SetField(def, "burnPerHitStacks", 1);

        ArcanaData card = ScriptableObject.CreateInstance<ArcanaData>();
        SetField(card, "id", 19);
        SetField(card, "baseCost", 4);
        SetField(card, "defaultElement", DamageElement.Neutral);

        ScheduledEffect[][] effects = def.Expand(card);

        Assert.AreEqual(4, effects.Length);

        Assert.AreEqual(1, effects[0].Length);
        Assert.AreEqual(EffectType.Cast, effects[0][0].Type);

        for (int i = 1; i < 4; i++)
        {
            Assert.AreEqual(2, effects[i].Length);
            Assert.AreEqual(EffectType.DealDamage, effects[i][0].Type);
            Assert.AreEqual(EffectType.ApplyBurn, effects[i][1].Type);
            Assert.AreEqual(1, effects[i][1].BaseValue);
        }

        UnityEngine.Object.DestroyImmediate(def);
        UnityEngine.Object.DestroyImmediate(card);
    }

    [Test]
    public void Expand_NullCard_ThrowsArgumentNullException()
    {
        RepeatingAttackEffectDef def = ScriptableObject.CreateInstance<RepeatingAttackEffectDef>();
        Assert.Throws<ArgumentNullException>(() => def.Expand(null));
        UnityEngine.Object.DestroyImmediate(def);
    }

    [Test]
    public void Expand_CostLessThan2_ThrowsInvalidOperationException()
    {
        RepeatingAttackEffectDef def = ScriptableObject.CreateInstance<RepeatingAttackEffectDef>();
        ArcanaData card = ScriptableObject.CreateInstance<ArcanaData>();
        SetField(card, "baseCost", 1);

        Assert.Throws<InvalidOperationException>(() => def.Expand(card));

        UnityEngine.Object.DestroyImmediate(def);
        UnityEngine.Object.DestroyImmediate(card);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}");
        field.SetValue(target, value);
    }
}
