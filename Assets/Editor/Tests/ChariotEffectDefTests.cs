using System;
using NUnit.Framework;
using UnityEngine;

public class ChariotEffectDefTests
{
    [Test]
    public void Expand_Cost2_CastThenMove()
    {
        ChariotEffectDef def = ScriptableObject.CreateInstance<ChariotEffectDef>();
        SetField(def, "rushDistance", 3);

        ArcanaData card = ScriptableObject.CreateInstance<ArcanaData>();
        SetField(card, "id", 7);
        SetField(card, "baseCost", 2);

        ScheduledEffect[][] effects = def.Expand(card);

        Assert.AreEqual(2, effects.Length);

        Assert.AreEqual(1, effects[0].Length);
        Assert.AreEqual(EffectType.Cast, effects[0][0].Type);

        Assert.AreEqual(1, effects[1].Length);
        Assert.AreEqual(EffectType.Move, effects[1][0].Type);
        Assert.AreEqual(3, effects[1][0].BaseValue);

        UnityEngine.Object.DestroyImmediate(def);
        UnityEngine.Object.DestroyImmediate(card);
    }

    [Test]
    public void RequiresDirection_ReturnsTrue()
    {
        ChariotEffectDef def = ScriptableObject.CreateInstance<ChariotEffectDef>();
        Assert.IsTrue(def.RequiresDirection);
        Assert.AreEqual(4, def.AllowedDirections);
        UnityEngine.Object.DestroyImmediate(def);
    }

    [Test]
    public void Expand_NullCard_ThrowsArgumentNullException()
    {
        ChariotEffectDef def = ScriptableObject.CreateInstance<ChariotEffectDef>();
        Assert.Throws<ArgumentNullException>(() => def.Expand(null));
        UnityEngine.Object.DestroyImmediate(def);
    }

    [Test]
    public void Expand_Cost1_ThrowsInvalidOperationException()
    {
        ChariotEffectDef def = ScriptableObject.CreateInstance<ChariotEffectDef>();
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
