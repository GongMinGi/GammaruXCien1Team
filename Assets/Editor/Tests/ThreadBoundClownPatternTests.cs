using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class ThreadBoundClownPatternTests
{
    // ---------- 수레바퀴 ----------

    [Test]
    public void Wheel_RotateWalksEightCellsClockwiseOnce()
    {
        int[] expected = { 8, 9, 6, 3, 2, 1, 4, 7 };

        int cell = 7;
        for (int step = 0; step < expected.Length; step++)
        {
            cell = ClownWheel.Rotate(cell);
            Assert.AreEqual(expected[step], cell,
                $"{step + 1}번째 회전이 어긋남");
        }

        Assert.AreEqual(7, cell, "8칸을 돌면 제자리로 와야 한다");
    }

    [Test]
    public void Wheel_OppositeIsPointSymmetric()
    {
        Assert.AreEqual(9, ClownWheel.Opposite(1));
        Assert.AreEqual(1, ClownWheel.Opposite(9));
        Assert.AreEqual(8, ClownWheel.Opposite(2));
        Assert.AreEqual(7, ClownWheel.Opposite(3));
        Assert.AreEqual(6, ClownWheel.Opposite(4));
        Assert.AreEqual(4, ClownWheel.Opposite(6));

        foreach (int cell in new[] { 1, 2, 3, 4, 6, 7, 8, 9 })
        {
            Assert.AreEqual(-ClownWheel.ToDirection(cell),
                ClownWheel.ToDirection(ClownWheel.Opposite(cell)));
        }
    }

    [Test]
    public void Wheel_ColumnCellsAreThreeLinesOnDiagonalAndTwoOnOrthogonal()
    {
        foreach (int diagonal in new[] { 1, 3, 7, 9 })
        {
            Vector2Int[] cells = ClownWheel.ColumnCells(diagonal);
            Assert.AreEqual(15, cells.Length, $"칸 {diagonal}");
            CollectionAssert.AreEquivalent(
                new[] { -2, 0, 2 }, cells.Select(c => c.x).Distinct().ToArray());
        }

        foreach (int orthogonal in new[] { 2, 4, 6, 8 })
        {
            Vector2Int[] cells = ClownWheel.ColumnCells(orthogonal);
            Assert.AreEqual(10, cells.Length, $"칸 {orthogonal}");
            CollectionAssert.AreEquivalent(
                new[] { -1, 1 }, cells.Select(c => c.x).Distinct().ToArray());
        }
    }

    [Test]
    public void Wheel_BombCellsAreAlwaysNineAndInsideGrid()
    {
        foreach (int cell in new[] { 1, 2, 3, 4, 6, 7, 8, 9 })
        {
            Vector2Int[] cells = ClownWheel.BombCells(cell);
            Assert.AreEqual(9, cells.Length, $"칸 {cell}의 3×3이 잘렸다");
            Assert.Contains(ClownWheel.ToDirection(cell), cells);
            AssertInsideGrid(cells);
        }
    }

    [Test]
    public void Wheel_PickTangleCenterStaysInOppositeQuadrant()
    {
        System.Random rng = new(1234);

        // 대각 구역: 반대편 1곳 확정
        Assert.AreEqual(new Vector2Int(-1, -1),
            ClownWheel.PickTangleCenter(new Vector2Int(2, 1), rng));
        Assert.AreEqual(new Vector2Int(1, 1),
            ClownWheel.PickTangleCenter(new Vector2Int(-1, -2), rng));

        // 3번째 가로줄: x는 반대편 확정, y만 랜덤
        HashSet<Vector2Int> rowResults = new();
        for (int i = 0; i < 200; i++)
            rowResults.Add(ClownWheel.PickTangleCenter(new Vector2Int(2, 0), rng));
        CollectionAssert.AreEquivalent(
            new[] { new Vector2Int(-1, 1), new Vector2Int(-1, -1) }, rowResults);

        // 3번째 세로줄: y는 반대편 확정, x만 랜덤
        HashSet<Vector2Int> columnResults = new();
        for (int i = 0; i < 200; i++)
            columnResults.Add(ClownWheel.PickTangleCenter(new Vector2Int(0, -2), rng));
        CollectionAssert.AreEquivalent(
            new[] { new Vector2Int(1, 1), new Vector2Int(-1, 1) }, columnResults);

        // 정중앙: 4곳 전부
        HashSet<Vector2Int> centerResults = new();
        for (int i = 0; i < 400; i++)
            centerResults.Add(ClownWheel.PickTangleCenter(Vector2Int.zero, rng));
        Assert.AreEqual(4, centerResults.Count);
        foreach (Vector2Int center in centerResults)
            Assert.AreEqual(1, Mathf.Abs(center.x) * Mathf.Abs(center.y));
    }

    // ---------- 실타래 ----------

    [Test]
    public void TangleField_SharedCellRemovesBothOverlappingTangles()
    {
        GameObject go = new GameObject("TangleTest");
        TangleField field = go.AddComponent<TangleField>();

        Assert.IsTrue(field.Spawn(new Vector2Int(-1, 1)));
        Assert.IsTrue(field.Spawn(new Vector2Int(1, 1)));
        Assert.IsFalse(field.Spawn(new Vector2Int(1, 1)), "같은 중심은 중복 생성되지 않는다");
        Assert.AreEqual(2, field.Count);

        // 공유 칸 (0,+1) — 두 십자가 모두 포함한다
        Assert.IsTrue(field.Contains(new Vector2Int(0, 1)));
        field.RemoveContaining(new Vector2Int(0, 1));
        Assert.AreEqual(0, field.Count, "겹친 실타래는 함께 사라진다");

        // 겹치지 않는 칸을 제거하면 나머지는 남는다
        field.Spawn(new Vector2Int(-1, 1));
        field.Spawn(new Vector2Int(1, 1));
        field.RemoveContaining(new Vector2Int(-2, 1));
        Assert.AreEqual(1, field.Count);
        Assert.IsTrue(field.Contains(new Vector2Int(1, 1)));
        Assert.IsFalse(field.Contains(new Vector2Int(-1, 1)));

        field.ClearAll();
        Assert.AreEqual(0, field.Count);

        Object.DestroyImmediate(go);
    }

    // ---------- 패턴 ----------

    [Test]
    public void BuildPattern_AllPhasesStayInsideTimelineAndTargetCells()
    {
        var definition =
            ScriptableObject.CreateInstance<ThreadBoundClownPatternDefinition>();

        // 1페이즈 패턴 3종
        for (int index = 0; index < 3; index++)
            AssertPlanIsPlayable(definition.BuildPattern(0, index, WanderingPositions()));

        // 2·3페이즈: t 12턴 × II/II,II 2종 × 두 위치 시나리오
        for (int turn = 0; turn < 12; turn++)
        {
            int phase = turn < 6 ? 1 : 2;
            definition.SelectPatternIndex(phase, turn);

            for (int index = 0; index < 2; index++)
            {
                AssertPlanIsPlayable(definition.BuildPattern(phase, index, WanderingPositions()));
                AssertPlanIsPlayable(definition.BuildPattern(phase, index, CornerPositions()));
            }
        }

        Object.DestroyImmediate(definition);
    }

    [Test]
    public void BuildPattern_PhaseTwoSlotEightFollowsFourTurnCycle()
    {
        var definition =
            ScriptableObject.CreateInstance<ThreadBoundClownPatternDefinition>();
        definition.ResetRuntimeState();

        int[][] expected =
        {
            new[] { 7 },     // t=1  VII
            new[] { 1, 1 },  // t=2  I, I
            new[] { 8 },     // t=3  VIII
            new[] { 1 }      // t=4  I
        };

        for (int turn = 0; turn < 8; turn++)
        {
            definition.SelectPatternIndex(1, turn);
            BossPatternPlan plan = definition.BuildPattern(1, 0, WanderingPositions());

            BossIntent slotEight = plan.Intents.Single(i => i.timingSlot == 7);
            CollectionAssert.AreEqual(expected[turn % 4], slotEight.arcanaIds,
                $"t={turn + 1}의 슬롯 8 구성이 4턴 주기와 어긋남");
        }

        Object.DestroyImmediate(definition);
    }

    [Test]
    public void BuildPattern_PhaseThreeAddsFallingKnifeEveryThirdTurn()
    {
        var definition =
            ScriptableObject.CreateInstance<ThreadBoundClownPatternDefinition>();
        definition.ResetRuntimeState();

        for (int turn = 1; turn <= 7; turn++)
        {
            definition.SelectPatternIndex(2, turn - 1);
            BossPatternPlan plan = definition.BuildPattern(2, 0, WanderingPositions());

            bool hasXii = plan.Intents.Any(i => i.arcanaIds.SequenceEqual(new[] { 12 }));
            Assert.AreEqual(turn % 3 == 1, hasXii, $"u={turn}");

            if (hasXii)
            {
                BossIntent xii = plan.Intents.Single(i => i.timingSlot == 0);
                // 시전 1 → 타격 6번째 슬롯 (인덱스 5)
                Assert.IsTrue(plan.Actions.Any(a => a.timingSlot == 5 && a.ignoresDodge));
                Assert.AreEqual(0, xii.timingSlot);
            }
        }

        Object.DestroyImmediate(definition);
    }

    [Test]
    public void TurnEndArcanaIds_AnnounceExactlyWhatTheWheelDoes()
    {
        var definition =
            ScriptableObject.CreateInstance<ThreadBoundClownPatternDefinition>();
        definition.ResetRuntimeState();

        // 1페이즈: 턴 종료 0만
        CollectionAssert.AreEqual(new[] { 0 }, definition.TurnEndArcanaIds);

        int[][] expected =
        {
            new[] { 5, 0 },  // t=1  V + 0
            new[] { 0 },     // t=2  0
            new[] { 4 },     // t=3  IV
            new[] { 0 }      // t=4  0
        };

        for (int turn = 1; turn <= 8; turn++)
        {
            definition.SelectPatternIndex(1, turn - 1);

            int[] announced = definition.TurnEndArcanaIds;
            CollectionAssert.AreEqual(expected[(turn - 1) % 4], announced,
                $"t={turn}의 턴 종료 예고가 4턴 주기와 어긋남");

            // 예고한 아르카나가 실제 수레바퀴 이동과 일치해야 한다
            int before = definition.WheelPosition;
            int after = definition.ApplyTurnEndWheel();

            int predicted;
            if (announced.SequenceEqual(new[] { 4 }))
                predicted = 4;
            else if (announced.SequenceEqual(new[] { 5, 0 }))
                predicted = ClownWheel.Rotate(ClownWheel.Opposite(before));
            else
                predicted = ClownWheel.Rotate(before);

            Assert.AreEqual(predicted, after,
                $"t={turn}: 예고({string.Join("+", announced)})와 실제 이동이 다름");
        }

        Object.DestroyImmediate(definition);
    }

    [Test]
    public void TurnEndArcanaIds_IsEmptyForBossesWithoutTurnEndCasts()
    {
        var golem =
            ScriptableObject.CreateInstance<AbandonedMagicGolemPatternDefinition>();

        Assert.IsEmpty(golem.TurnEndArcanaIds,
            "골렘은 턴 종료 카드가 붙지 않아야 한다");

        Object.DestroyImmediate(golem);
    }

    [Test]
    public void BuildPattern_IsPureAcrossRepeatedCalls()
    {
        var definition =
            ScriptableObject.CreateInstance<ThreadBoundClownPatternDefinition>();
        definition.ResetRuntimeState();
        definition.SelectPatternIndex(1, 0);

        BossPatternPlan first = definition.BuildPattern(1, 0, WanderingPositions());
        BossPatternPlan second = definition.BuildPattern(1, 0, WanderingPositions());

        Assert.AreEqual(first.Actions.Length, second.Actions.Length);
        for (int i = 0; i < first.Actions.Length; i++)
        {
            Assert.AreEqual(first.Actions[i].timingSlot, second.Actions[i].timingSlot);
            CollectionAssert.AreEquivalent(
                first.Actions[i].targetCells, second.Actions[i].targetCells);
        }

        Object.DestroyImmediate(definition);
    }

    // ---------- 헬퍼 ----------

    /// BossAI.ValidatePlan과 같은 규칙 — 여기서 걸리면 전투가 중단된다
    private static void AssertPlanIsPlayable(BossPatternPlan plan)
    {
        Assert.IsNotNull(plan);
        Assert.IsNotEmpty(plan.Intents);
        Assert.IsNotEmpty(plan.Actions);

        foreach (BossIntent intent in plan.Intents)
        {
            Assert.GreaterOrEqual(intent.timingSlot, 0);
            Assert.Less(intent.timingSlot, ActionBar.SlotCount);
            Assert.IsNotEmpty(intent.arcanaIds);
        }

        foreach (BossAction action in plan.Actions)
        {
            Assert.GreaterOrEqual(action.timingSlot, 0);
            Assert.Less(action.timingSlot, ActionBar.SlotCount);
            Assert.IsNotNull(action.targetCells);
            Assert.IsNotEmpty(action.targetCells);
            Assert.GreaterOrEqual(action.baseDamage, 0);
            AssertInsideGrid(action.targetCells);

            if (action.wheelTargeting != WheelTargeting.None)
            {
                Assert.GreaterOrEqual(action.castSlot, 0);
                Assert.Less(action.castSlot, ActionBar.SlotCount);
            }
        }
    }

    private static void AssertInsideGrid(Vector2Int[] cells)
    {
        foreach (Vector2Int cell in cells)
        {
            Assert.IsTrue(
                cell.x >= -GridManager.Columns / 2 && cell.x <= GridManager.Columns / 2 &&
                cell.y >= -GridManager.Rows / 2 && cell.y <= GridManager.Rows / 2,
                $"{cell}이 그리드를 벗어남");
        }
    }

    /// 슬롯마다 다른 칸을 밟는 플레이어 (PredictWheel과 슬롯별 조준을 흔들어 본다)
    private static Vector2Int[] WanderingPositions()
    {
        Vector2Int[] positions = new Vector2Int[ActionBar.SlotCount];
        for (int slot = 0; slot < positions.Length; slot++)
            positions[slot] = new Vector2Int(slot % 5 - 2, (slot * 3) % 5 - 2);
        return positions;
    }

    /// 계속 구석에 붙어 있는 플레이어 (그리드 클리핑 경로)
    private static Vector2Int[] CornerPositions()
    {
        Vector2Int[] positions = new Vector2Int[ActionBar.SlotCount];
        for (int slot = 0; slot < positions.Length; slot++)
            positions[slot] = new Vector2Int(2, 2);
        return positions;
    }
}
