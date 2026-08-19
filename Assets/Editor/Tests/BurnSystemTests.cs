using NUnit.Framework;
using UnityEngine;

public class BurnSystemTests
{
    private GameObject go;
    private BossStats stats;
    private BossData data;

    [SetUp]
    public void SetUp()
    {
        data = ScriptableObject.CreateInstance<BossData>();
        SetField(data, "maxHp", 500);
        SetField(data, "burnDamagePerStack", 2);
        SetField(data, "burnDurationTurns", 3);

        go = new GameObject("BossStatsTest");
        stats = go.AddComponent<BossStats>();
        SetField(stats, "bossData", data);
        SetField(stats, "currentHp", 500);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(data);
    }

    [Test]
    public void TickBurnTimer_ThreeTimes_ClearsStacks()
    {
        stats.AddBurnStacks(2);

        stats.TickBurnTimer();
        stats.TickBurnTimer();
        Assert.AreEqual(2, stats.BurnStacks);

        stats.TickBurnTimer();
        Assert.AreEqual(0, stats.BurnStacks);
        Assert.AreEqual(0, stats.BurnRemainingTurns);
    }

    [Test]
    public void AddBurnStacks_ResetsBurnRemainingTurns()
    {
        stats.AddBurnStacks(1);
        Assert.AreEqual(3, stats.BurnRemainingTurns);

        stats.TickBurnTimer();
        Assert.AreEqual(2, stats.BurnRemainingTurns);

        stats.AddBurnStacks(1);
        Assert.AreEqual(3, stats.BurnRemainingTurns);
        Assert.AreEqual(2, stats.BurnStacks);
    }

    [Test]
    public void AddBurnStacks_MidTimer_AccumulatesAndResetsTimer()
    {
        stats.AddBurnStacks(2);
        stats.TickBurnTimer();
        stats.TickBurnTimer();
        Assert.AreEqual(1, stats.BurnRemainingTurns);

        stats.AddBurnStacks(3);
        Assert.AreEqual(5, stats.BurnStacks);
        Assert.AreEqual(3, stats.BurnRemainingTurns);
    }

    [Test]
    public void ProcessBurn_NoStacks_NoDamage()
    {
        stats.ProcessBurn();
        Assert.AreEqual(500, stats.CurrentHp);
    }

    [Test]
    public void ProcessBurn_WithStacks_DealsCorrectDamage_FiresDamageTakenOnce()
    {
        stats.AddBurnStacks(2);

        int damageTakenCount = 0;
        int lastDamage = 0;
        stats.DamageTaken += d => { damageTakenCount++; lastDamage = d; };

        stats.ProcessBurn();

        Assert.AreEqual(496, stats.CurrentHp);
        Assert.AreEqual(1, damageTakenCount);
        Assert.AreEqual(4, lastDamage);
    }

    [Test]
    public void ProcessBurn_AfterTimerExpires_NoDamage()
    {
        stats.AddBurnStacks(2);
        stats.TickBurnTimer();
        stats.TickBurnTimer();
        stats.TickBurnTimer();

        int damageTakenCount = 0;
        stats.DamageTaken += _ => damageTakenCount++;

        stats.ProcessBurn();

        Assert.AreEqual(500, stats.CurrentHp);
        Assert.AreEqual(0, damageTakenCount);
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
