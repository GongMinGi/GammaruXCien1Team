using System;
using NUnit.Framework;
using UnityEngine;

public class AttackEffectDefTests
{
    [Test]
    public void Expand_ProducesCorrectEffectSequence()
    {
        AttackEffectDef def = ScriptableObject.CreateInstance<AttackEffectDef>();
        SetField(def, "baseValue", 5);
        SetField(def, "spellPowerCoefficient", 1.5f);
        SetField(def, "additionalEffectValue", 2);

        ArcanaData card = ScriptableObject.CreateInstance<ArcanaData>();
        SetField(card, "id", 8);
        SetField(card, "baseCost", 2);
        SetField(card, "defaultElement", DamageElement.Neutral);

        ScheduledEffect[][] effects = def.Expand(card);

        Assert.AreEqual(2, effects.Length);

        Assert.AreEqual(1, effects[0].Length);
        Assert.AreEqual(EffectType.Cast, effects[0][0].Type);
        Assert.AreEqual(card, effects[0][0].SourceCard);
        Assert.AreEqual(DamageElement.Neutral, effects[0][0].Element);

        Assert.AreEqual(1, effects[1].Length);
        Assert.AreEqual(EffectType.DealDamage, effects[1][0].Type);
        Assert.AreEqual(card, effects[1][0].SourceCard);
        Assert.AreEqual(DamageElement.Neutral, effects[1][0].Element);
        Assert.AreEqual(5, effects[1][0].BaseValue);
        Assert.AreEqual(1.5f, effects[1][0].SpellPowerCoefficient);
        Assert.AreEqual(2, effects[1][0].AdditionalEffectValue);

        UnityEngine.Object.DestroyImmediate(def);
        UnityEngine.Object.DestroyImmediate(card);
    }

    [Test]
    public void Expand_NullCard_ThrowsArgumentNullException()
    {
        AttackEffectDef def = ScriptableObject.CreateInstance<AttackEffectDef>();

        Assert.Throws<ArgumentNullException>(() => def.Expand(null));

        UnityEngine.Object.DestroyImmediate(def);
    }

    [Test]
    public void Expand_ZeroCost_ThrowsInvalidOperationException()
    {
        AttackEffectDef def = ScriptableObject.CreateInstance<AttackEffectDef>();
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
