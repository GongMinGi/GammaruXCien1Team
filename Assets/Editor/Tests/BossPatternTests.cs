using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class BossPatternTests
{
    [Test]
    public void TargetResolver_BuildsExpectedBoardCenteredShapes()
    {
        Vector2Int[] cross = BossTargetResolver.Cross();
        Vector2Int[] diagonal = BossTargetResolver.DiagonalCross();
        Vector2Int[] diamond = BossTargetResolver.DiamondPerimeter();

        Assert.AreEqual(9, cross.Length);
        Assert.Contains(new Vector2Int(-2, 0), cross);
        Assert.Contains(new Vector2Int(0, 2), cross);

        Assert.AreEqual(9, diagonal.Length);
        Assert.Contains(new Vector2Int(-2, -2), diagonal);
        Assert.Contains(new Vector2Int(2, -2), diagonal);

        Assert.AreEqual(8, diamond.Length);
        Assert.Contains(new Vector2Int(0, 2), diamond);
        Assert.Contains(new Vector2Int(1, 1), diamond);
        Assert.IsFalse(diamond.Contains(Vector2Int.zero));
    }

    [Test]
    public void TargetResolver_ClipsPlayerCenteredTargetsToGrid()
    {
        Vector2Int[] cells =
            BossTargetResolver.CardinalNeighbors(new Vector2Int(2, 2));

        Assert.AreEqual(2, cells.Length);
        Assert.Contains(new Vector2Int(1, 2), cells);
        Assert.Contains(new Vector2Int(2, 1), cells);
    }

    [Test]
    public void GeneratePattern_FirstTurnMatchesPhaseOneOpening()
    {
        GameObject go = new GameObject("BossTest");
        BossStats stats = go.AddComponent<BossStats>();
        SetCurrentHp(stats);
        BossAI ai = CreateGolemAI(go, stats, out var definition, out var data);

        BossPatternPlan plan = ai.GeneratePattern(Vector2Int.zero);

        Assert.IsNotNull(plan);
        Assert.AreEqual(2, plan.Intents.Length);
        Assert.AreEqual(1, plan.Intents[0].timingSlot);
        CollectionAssert.AreEqual(new[] { 1 }, plan.Intents[0].arcanaIds);
        Assert.AreEqual(5, plan.Intents[1].timingSlot);
        CollectionAssert.AreEqual(new[] { 1 }, plan.Intents[1].arcanaIds);
        Assert.AreEqual(2, plan.Actions.Length);
        Assert.IsTrue(plan.Actions.All(action => action.baseDamage == 10));

        Object.DestroyImmediate(definition);
        Object.DestroyImmediate(data);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void GeneratePattern_AtHalfHpEntersPhaseTwoOnNextGeneration()
    {
        GameObject go = new GameObject("BossTest");
        BossStats stats = go.AddComponent<BossStats>();
        SetCurrentHp(stats);
        BossAI ai = CreateGolemAI(go, stats, out var definition, out var data);

        Assert.IsNotNull(ai.GeneratePattern(Vector2Int.zero));
        stats.TakeDamage(stats.MaxHp / 2);

        BossPatternPlan plan = ai.GeneratePattern(Vector2Int.zero);

        Assert.IsNotNull(plan);
        Assert.AreEqual(0, plan.Intents[0].timingSlot);
        CollectionAssert.AreEqual(new[] { 8 }, plan.Intents[0].arcanaIds);
        Assert.AreEqual(5, plan.Actions.Length);
        Assert.AreEqual(3, plan.Actions[0].timingSlot);
        Assert.AreEqual(1, plan.Actions[0].targetCells.Length);
        Assert.AreEqual(4, plan.Actions[1].targetCells.Length);
        Assert.AreEqual(8, plan.Actions[2].targetCells.Length);

        Object.DestroyImmediate(definition);
        Object.DestroyImmediate(data);
        Object.DestroyImmediate(go);
    }


    private static void SetCurrentHp(BossStats stats)
    {
        var field = typeof(BossStats).GetField("currentHp",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field);
        field.SetValue(stats, stats.MaxHp);
    }


    private static BossAI CreateGolemAI(
        GameObject go,
        BossStats stats,
        out AbandonedMagicGolemPatternDefinition definition,
        out BossData data)
    {
        BossAI ai = go.AddComponent<BossAI>();
        definition =
            ScriptableObject.CreateInstance<AbandonedMagicGolemPatternDefinition>();
        data = ScriptableObject.CreateInstance<BossData>();
        SetField(data, "maxHp", 500);
        SetField(data, "pattern", definition);
        SetField(stats, "bossData", data);
        SetCurrentHp(stats);
        return ai;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field);
        field.SetValue(target, value);
    }


    [Test]
    public void RebuildCurrentPattern_LocksTargetAtCastStartSlot()
    {
        GameObject go = new GameObject("BossTest");
        BossStats stats = go.AddComponent<BossStats>();
        SetCurrentHp(stats);
        BossAI ai = CreateGolemAI(go, stats, out var definition, out var data);

        Assert.IsNotNull(ai.GeneratePattern(Vector2Int.zero));

        Vector2Int[] positions = new Vector2Int[ActionBar.SlotCount];
        positions[0] = Vector2Int.zero;
        for (int slot = 1; slot < positions.Length; slot++)
            positions[slot] = new Vector2Int(1, 0);

        BossPatternPlan plan = ai.RebuildCurrentPattern(positions);

        Assert.IsNotNull(plan);
        Assert.Contains(new Vector2Int(1, 0), plan.Actions[0].targetCells);
        Assert.IsFalse(plan.Actions[0].targetCells.Contains(Vector2Int.zero));

        Object.DestroyImmediate(definition);
        Object.DestroyImmediate(data);
        Object.DestroyImmediate(go);
    }
}
