using System;
using NUnit.Framework;
using UnityEngine;

public class DodgeEffectDefTests
{
    [Test]
    public void Expand_AllSlotsDodge()
    {
        DodgeEffectDef def = ScriptableObject.CreateInstance<DodgeEffectDef>();

        ArcanaData card = ScriptableObject.CreateInstance<ArcanaData>();
        SetField(card, "id", 9);
        SetField(card, "baseCost", 3);

        ScheduledEffect[][] effects = def.Expand(card);

        Assert.AreEqual(3, effects.Length);
        for (int i = 0; i < 3; i++)
        {
            Assert.AreEqual(1, effects[i].Length);
            Assert.AreEqual(EffectType.Dodge, effects[i][0].Type);
            Assert.AreEqual(card, effects[i][0].SourceCard);
        }

        UnityEngine.Object.DestroyImmediate(def);
        UnityEngine.Object.DestroyImmediate(card);
    }

    [Test]
    public void Expand_NullCard_ThrowsArgumentNullException()
    {
        DodgeEffectDef def = ScriptableObject.CreateInstance<DodgeEffectDef>();
        Assert.Throws<ArgumentNullException>(() => def.Expand(null));
        UnityEngine.Object.DestroyImmediate(def);
    }

    [Test]
    public void Expand_ZeroCost_ThrowsInvalidOperationException()
    {
        DodgeEffectDef def = ScriptableObject.CreateInstance<DodgeEffectDef>();
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
