using System;
using NUnit.Framework;
using UnityEngine;

public class ModifiedAttackEffectDefTests
{
    [Test]
    public void Expand_Justice_DmgReductionAndCounterStance()
    {
        ModifiedAttackEffectDef def = ScriptableObject.CreateInstance<ModifiedAttackEffectDef>();
        SetField(def, "baseValue", 5);
        SetField(def, "spellPowerCoefficient", 1.0f);
        SetField(def, "additionalEffectValue", 0);
        SetField(def, "incomingDamageModifier", -5);
        SetField(def, "enableCounterStance", true);
        SetField(def, "counterStanceBonusPerHit", 3);

        ArcanaData card = ScriptableObject.CreateInstance<ArcanaData>();
        SetField(card, "id", 11);
        SetField(card, "baseCost", 3);
        SetField(card, "defaultElement", DamageElement.Neutral);

        ScheduledEffect[][] effects = def.Expand(card);

        Assert.AreEqual(3, effects.Length);

        for (int i = 0; i < 2; i++)
        {
            Assert.AreEqual(3, effects[i].Length);
            Assert.AreEqual(EffectType.Cast, effects[i][0].Type);
            Assert.AreEqual(EffectType.IncomingDamageModifier, effects[i][1].Type);
            Assert.AreEqual(-5, effects[i][1].BaseValue);
            Assert.AreEqual(EffectType.CounterStance, effects[i][2].Type);
            Assert.AreEqual(3, effects[i][2].BaseValue);
        }

        Assert.AreEqual(1, effects[2].Length);
        Assert.AreEqual(EffectType.DealDamage, effects[2][0].Type);
        Assert.AreEqual(5, effects[2][0].BaseValue);

        UnityEngine.Object.DestroyImmediate(def);
        UnityEngine.Object.DestroyImmediate(card);
    }

    [Test]
    public void Expand_Death_DmgIncreaseNoCounter()
    {
        ModifiedAttackEffectDef def = ScriptableObject.CreateInstance<ModifiedAttackEffectDef>();
        SetField(def, "baseValue", 8);
        SetField(def, "spellPowerCoefficient", 1.5f);
        SetField(def, "additionalEffectValue", 1);
        SetField(def, "incomingDamageModifier", 5);
        SetField(def, "enableCounterStance", false);

        ArcanaData card = ScriptableObject.CreateInstance<ArcanaData>();
        SetField(card, "id", 13);
        SetField(card, "baseCost", 3);
        SetField(card, "defaultElement", DamageElement.Neutral);

        ScheduledEffect[][] effects = def.Expand(card);

        Assert.AreEqual(3, effects.Length);

        for (int i = 0; i < 2; i++)
        {
            Assert.AreEqual(2, effects[i].Length);
            Assert.AreEqual(EffectType.Cast, effects[i][0].Type);
            Assert.AreEqual(EffectType.IncomingDamageModifier, effects[i][1].Type);
            Assert.AreEqual(5, effects[i][1].BaseValue);
        }

        Assert.AreEqual(1, effects[2].Length);
        Assert.AreEqual(EffectType.DealDamage, effects[2][0].Type);
        Assert.AreEqual(8, effects[2][0].BaseValue);
        Assert.AreEqual(1.5f, effects[2][0].SpellPowerCoefficient);
        Assert.AreEqual(1, effects[2][0].AdditionalEffectValue);

        UnityEngine.Object.DestroyImmediate(def);
        UnityEngine.Object.DestroyImmediate(card);
    }

    [Test]
    public void Expand_NullCard_ThrowsArgumentNullException()
    {
        ModifiedAttackEffectDef def = ScriptableObject.CreateInstance<ModifiedAttackEffectDef>();
        Assert.Throws<ArgumentNullException>(() => def.Expand(null));
        UnityEngine.Object.DestroyImmediate(def);
    }

    [Test]
    public void Expand_ZeroCost_ThrowsInvalidOperationException()
    {
        ModifiedAttackEffectDef def = ScriptableObject.CreateInstance<ModifiedAttackEffectDef>();
        ArcanaData card = ScriptableObject.CreateInstance<ArcanaData>();
        SetField(card, "baseCost", 0);

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
