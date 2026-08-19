using System;
using NUnit.Framework;
using UnityEngine;

public class TowerEffectDefTests
{
    [Test]
    public void Expand_Cost3_CastCastPlaceTower()
    {
        TowerEffectDef def = ScriptableObject.CreateInstance<TowerEffectDef>();
        SetField(def, "towerBonusDamage", 5);

        ArcanaData card = ScriptableObject.CreateInstance<ArcanaData>();
        SetField(card, "id", 16);
        SetField(card, "baseCost", 3);

        ScheduledEffect[][] effects = def.Expand(card);

        Assert.AreEqual(3, effects.Length);

        Assert.AreEqual(1, effects[0].Length);
        Assert.AreEqual(EffectType.Cast, effects[0][0].Type);

        Assert.AreEqual(1, effects[1].Length);
        Assert.AreEqual(EffectType.Cast, effects[1][0].Type);

        Assert.AreEqual(1, effects[2].Length);
        Assert.AreEqual(EffectType.PlaceTower, effects[2][0].Type);
        Assert.AreEqual(5, effects[2][0].BaseValue);
        Assert.AreEqual(card, effects[2][0].SourceCard);

        UnityEngine.Object.DestroyImmediate(def);
        UnityEngine.Object.DestroyImmediate(card);
    }

    [Test]
    public void RequiresDirection_ReturnsTrue_8Directions()
    {
        TowerEffectDef def = ScriptableObject.CreateInstance<TowerEffectDef>();
        Assert.IsTrue(def.RequiresDirection);
        Assert.AreEqual(8, def.AllowedDirections);
        UnityEngine.Object.DestroyImmediate(def);
    }

    [Test]
    public void Expand_NullCard_ThrowsArgumentNullException()
    {
        TowerEffectDef def = ScriptableObject.CreateInstance<TowerEffectDef>();
        Assert.Throws<ArgumentNullException>(() => def.Expand(null));
        UnityEngine.Object.DestroyImmediate(def);
    }

    [Test]
    public void Expand_Cost1_ThrowsInvalidOperationException()
    {
        TowerEffectDef def = ScriptableObject.CreateInstance<TowerEffectDef>();
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
