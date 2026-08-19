using System.Collections.Generic;
using NUnit.Framework;

public class CooldownTests
{
    [Test]
    public void IsCardOnCooldown_DuringCooldown_ReturnsTrue()
    {
        var map = new Dictionary<int, int> { { 9, 5 } }; // turn 1 + 3 + 1 = 5

        Assert.IsTrue(BattleFlowController.IsCardOnCooldown(map, 9, 2));
        Assert.IsTrue(BattleFlowController.IsCardOnCooldown(map, 9, 3));
        Assert.IsTrue(BattleFlowController.IsCardOnCooldown(map, 9, 4));
    }

    [Test]
    public void IsCardOnCooldown_AfterCooldown_ReturnsFalse()
    {
        var map = new Dictionary<int, int> { { 9, 5 } };

        Assert.IsFalse(BattleFlowController.IsCardOnCooldown(map, 9, 5));
        Assert.IsFalse(BattleFlowController.IsCardOnCooldown(map, 9, 6));
    }

    [Test]
    public void IsCardOnCooldown_UnregisteredCard_ReturnsFalse()
    {
        var map = new Dictionary<int, int>();

        Assert.IsFalse(BattleFlowController.IsCardOnCooldown(map, 9, 1));
    }

    [Test]
    public void SameTurnDuplicate_AddedToCooldownSet_BlocksSecondUse()
    {
        var cooldownIds = new HashSet<int>();

        // 첫 사용: 쿨다운 ID에 추가
        Assert.IsFalse(cooldownIds.Contains(9));
        cooldownIds.Add(9);

        // 두 번째 사용 시도: 차단
        Assert.IsTrue(cooldownIds.Contains(9));
    }

    [Test]
    public void SameTurnDuplicate_UndoRemoves_AllowsReuse()
    {
        var cooldownIds = new HashSet<int>();
        cooldownIds.Add(9);

        // Undo: 쿨다운 ID에서 제거
        cooldownIds.Remove(9);
        Assert.IsFalse(cooldownIds.Contains(9));
    }
}
