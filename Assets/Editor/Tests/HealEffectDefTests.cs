using System;
using NUnit.Framework;
using UnityEngine;

public class HealEffectDefTests
{
    [Test]
    public void Expand_ProducesCorrectEffectSequence()
    {
        HealEffectDef def = ScriptableObject.CreateInstance<HealEffectDef>();
        SetField(def, "baseHealValue", 20);

        ArcanaData card = ScriptableObject.CreateInstance<ArcanaData>();
        SetField(card, "id", 3);
        SetField(card, "baseCost", 2);
        SetField(card, "defaultElement", DamageElement.Neutral);

        ScheduledEffect[][] effects = def.Expand(card);

        Assert.AreEqual(2, effects.Length);

        Assert.AreEqual(1, effects[0].Length);
        Assert.AreEqual(EffectType.Cast, effects[0][0].Type);
        Assert.AreEqual(card, effects[0][0].SourceCard);

        Assert.AreEqual(1, effects[1].Length);
        Assert.AreEqual(EffectType.Heal, effects[1][0].Type);
        Assert.AreEqual(card, effects[1][0].SourceCard);
        Assert.AreEqual(20, effects[1][0].BaseValue);

        UnityEngine.Object.DestroyImmediate(def);
        UnityEngine.Object.DestroyImmediate(card);
    }

    [Test]
    public void Expand_NullCard_ThrowsArgumentNullException()
    {
        HealEffectDef def = ScriptableObject.CreateInstance<HealEffectDef>();
        Assert.Throws<ArgumentNullException>(() => def.Expand(null));
        UnityEngine.Object.DestroyImmediate(def);
    }

    [Test]
    public void Expand_ZeroCost_ThrowsInvalidOperationException()
    {
        HealEffectDef def = ScriptableObject.CreateInstance<HealEffectDef>();
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
