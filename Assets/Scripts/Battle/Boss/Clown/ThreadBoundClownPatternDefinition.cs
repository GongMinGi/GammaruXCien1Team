using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "실타래에 꿰인 광대" 패턴. 수레바퀴 런타임 상태를 들고 있으며,
/// 상태 변경은 SelectPatternIndex(턴당 1회)와 ClownBossMechanic의 실행 시점 호출에서만 일어난다.
/// BuildPattern은 순수하다 — 계획 중 매 입력마다 통째로 재실행된다.
/// </summary>
[CreateAssetMenu(
    fileName = "ThreadBoundClownPattern",
    menuName = "Battle/Boss Patterns/Thread Bound Clown")]
public class ThreadBoundClownPatternDefinition : BossPatternDefinition
{
    [Header("Attack Damage")]
    [SerializeField, Min(0)] private int knifeDamage = 8;
    [SerializeField, Min(0)] private int rowDamage = 5;
    [SerializeField, Min(0)] private int nailDamage = 15;
    [SerializeField, Min(0)] private int bombDamage = 15;
    [SerializeField, Min(0)] private int fallingKnifeDamage = 20;
    [SerializeField, Min(0)] private int wheelPunishDamage = 15;
    [SerializeField] private DamageElement attackElement = DamageElement.Neutral;

    [Header("Phase Thresholds (%)")]
    [SerializeField, Range(1, 99)] private int phaseTwoHpPercent = 80;
    [SerializeField, Range(1, 99)] private int phaseThreeHpPercent = 20;

    /// II 타격 가로줄 (y): 2·4번째 줄
    private static readonly int[] NarrowRows = { 1, -1 };
    /// II,II 타격 가로줄 (y): 1·3·5번째 줄
    private static readonly int[] WideRows = { 2, 0, -2 };

    // 수레바퀴 런타임 상태 — SO는 에디터 세션 간에 값이 남으므로 리셋 경로가 필요하다
    [System.NonSerialized] private int wheelPosition = ClownWheel.StartCell;
    [System.NonSerialized] private bool wheelEffectsEnabled;
    [System.NonSerialized] private int phaseTwoTurn;    // t: 2페이즈 진입 후 턴 번호
    [System.NonSerialized] private int phaseThreeTurn;  // u: 3페이즈 진입 후 턴 번호

    public int WheelPosition => wheelPosition;
    public bool WheelEffectsEnabled => wheelEffectsEnabled;
    public int WheelPunishDamage => wheelPunishDamage;
    public int PhaseTwoTurn => phaseTwoTurn;

    private void OnEnable()
    {
        ResetRuntimeState();
    }

    /// 전투 시작 시 ClownBossMechanic.Awake()에서 호출한다.
    public void ResetRuntimeState()
    {
        wheelPosition = ClownWheel.StartCell;
        wheelEffectsEnabled = false;
        phaseTwoTurn = 0;
        phaseThreeTurn = 0;
    }

    /// II 피격 시 1칸 우회전 (실행 중 호출)
    public void RotateWheel()
    {
        wheelPosition = ClownWheel.Rotate(wheelPosition);
    }

    private enum TurnEndOp { Rotate, OppositeThenRotate, ForceFour }

    /// 이번 턴 종료에 무엇이 시전되는지 — 실제 이동과 표시가 같은 곳을 보게 한다
    private TurnEndOp CurrentTurnEndOp
    {
        get
        {
            if (phaseTwoTurn % 2 == 0)      // 1페이즈 전체 및 t 짝수: 0
                return TurnEndOp.Rotate;
            if (phaseTwoTurn % 4 == 1)      // t = 1, 5, 9…: V + 0
                return TurnEndOp.OppositeThenRotate;
            return TurnEndOp.ForceFour;     // t = 3, 7, 11…: IV
        }
    }

    /// 턴 종료 시전 아르카나 — 타임라인에 올라가지 않아 카드로만 예고된다
    public override int[] TurnEndArcanaIds
    {
        get
        {
            switch (CurrentTurnEndOp)
            {
                case TurnEndOp.OppositeThenRotate: return new[] { 5, 0 };
                case TurnEndOp.ForceFour: return new[] { 4 };
                default: return new[] { 0 };
            }
        }
    }

    /// <summary>
    /// 턴 종료: IV / V / 0을 적용하고 최종 칸을 돌려준다.
    /// 효과(피해·실타래) 발동은 호출자(ClownBossMechanic)가 한다.
    /// </summary>
    public int ApplyTurnEndWheel()
    {
        switch (CurrentTurnEndOp)
        {
            case TurnEndOp.OppositeThenRotate:
                wheelPosition = ClownWheel.Rotate(ClownWheel.Opposite(wheelPosition));
                break;
            case TurnEndOp.ForceFour:
                wheelPosition = 4;
                break;
            default:
                wheelPosition = ClownWheel.Rotate(wheelPosition);
                break;
        }

        return wheelPosition;
    }

    public override int ResolvePhase(BossStats stats, int currentPhase)
    {
        if (currentPhase >= 2)
            return 2;

        if ((long)stats.CurrentHp * 100 <= (long)stats.MaxHp * phaseThreeHpPercent)
            return 2;

        if (currentPhase >= 1)
            return 1;

        return (long)stats.CurrentHp * 100 <= (long)stats.MaxHp * phaseTwoHpPercent ? 1 : 0;
    }

    public override int SelectPatternIndex(int phase, int phaseTurnIndex)
    {
        if (phase == 0)
            return Random.Range(0, 3);

        // X: 2페이즈 개막 — 수레바퀴를 1번으로 되돌리고 턴 종료 효과를 켠다.
        // (1페이즈에서 3페이즈로 한 번에 떨어져도 여기서 걸린다)
        if (!wheelEffectsEnabled)
        {
            wheelPosition = ClownWheel.StartCell;
            wheelEffectsEnabled = true;
        }

        phaseTwoTurn++;
        if (phase == 2)
            phaseThreeTurn++;

        return Random.Range(0, 2);  // 0 = II, 1 = II,II
    }

    public override BossPatternPlan BuildPattern(
        int phase,
        int patternIndex,
        IReadOnlyList<Vector2Int> playerPositionsBySlot)
    {
        if (playerPositionsBySlot == null ||
            playerPositionsBySlot.Count != ActionBar.SlotCount)
            return null;

        List<BossIntent> intents = new();
        List<BossAction> actions = new();

        if (phase == 0)
            BuildPhaseOne(patternIndex, playerPositionsBySlot, intents, actions);
        else
            BuildPhaseTwo(phase, patternIndex, playerPositionsBySlot, intents, actions);

        return new BossPatternPlan(intents.ToArray(), actions.ToArray());
    }

    /// 1페이즈: 2: I / 6: I, 3: II / 7: I, 3: II,II / 7: I 중 하나
    private void BuildPhaseOne(int index,
        IReadOnlyList<Vector2Int> positions,
        List<BossIntent> intents,
        List<BossAction> actions)
    {
        switch (index)
        {
            case 0:
                AddKnife(1, positions, intents, actions);
                AddKnife(5, positions, intents, actions);
                break;
            case 1:
                AddRows(2, false, intents, actions);
                AddKnife(6, positions, intents, actions);
                break;
            default:
                AddRows(2, true, intents, actions);
                AddKnife(6, positions, intents, actions);
                break;
        }
    }

    /// <summary>
    /// 2·3페이즈: 매 턴 2: I / 5: II(또는 II,II), 슬롯 8은 t의 4턴 주기.
    /// 3페이즈는 여기에 u 3턴 주기로 1: XII이 붙는다.
    /// </summary>
    private void BuildPhaseTwo(int phase, int index,
        IReadOnlyList<Vector2Int> positions,
        List<BossIntent> intents,
        List<BossAction> actions)
    {
        bool wideRows = index == 1;

        // 슬롯 순서대로 쌓아 인텐트 표시가 뒤섞이지 않게 한다
        if (phase == 2 && phaseThreeTurn % 3 == 1)
            AddFallingKnife(0, positions, intents, actions);   // 1: XII

        AddKnife(1, positions, intents, actions);              // 2: I
        AddRows(4, wideRows, intents, actions);                // 5: II / II,II

        int predictedWheel = PredictWheel(4, wideRows, positions);

        if (phaseTwoTurn % 2 == 1)
        {
            if (phaseTwoTurn % 4 == 1)
                AddNail(7, predictedWheel, intents, actions);      // 8: VII
            else
                AddBomb(7, predictedWheel, intents, actions);      // 8: VIII
        }
        else
        {
            if (phaseTwoTurn % 4 == 2)
                AddTripleKnife(7, positions, intents, actions);    // 8: I, I
            else
                AddKnife(7, positions, intents, actions);          // 8: I
        }
    }

    /// <summary>
    /// VII/VIII 목표의 계획 단계 예측값. 이번 턴 II 타격에 계획 위치가 겹치는 횟수만큼
    /// 미리 회전시킨다. 실행 시점에 ClownBossMechanic이 실제 위치로 덮어쓴다.
    /// </summary>
    private int PredictWheel(int rowStartSlot, bool wideRows,
        IReadOnlyList<Vector2Int> positions)
    {
        int[] rows = wideRows ? WideRows : NarrowRows;
        int wheel = wheelPosition;

        for (int offset = 1; offset <= 2; offset++)
        {
            if (System.Array.IndexOf(rows, positions[rowStartSlot + offset].y) >= 0)
                wheel = ClownWheel.Rotate(wheel);
        }

        return wheel;
    }

    /// I — 시전 슬롯의 플레이어 위치 1칸, 타격 +1
    private void AddKnife(int startSlot,
        IReadOnlyList<Vector2Int> positions,
        List<BossIntent> intents,
        List<BossAction> actions)
    {
        AddIntent(startSlot, new[] { 1 }, intents);
        actions.Add(NewAction(startSlot + 1,
            BossTargetResolver.Center(positions[startSlot]),
            knifeDamage, blockedByTangle: true));
    }

    /// I, I — 각 타격 슬롯(+0, +1, +2)의 플레이어 위치, 반드시 명중
    private void AddTripleKnife(int startSlot,
        IReadOnlyList<Vector2Int> positions,
        List<BossIntent> intents,
        List<BossAction> actions)
    {
        AddIntent(startSlot, new[] { 1, 1 }, intents);
        for (int offset = 0; offset <= 2; offset++)
        {
            actions.Add(NewAction(startSlot + offset,
                BossTargetResolver.Center(positions[startSlot + offset]),
                knifeDamage, ignoresDodge: true, blockedByTangle: true));
        }
    }

    /// XII — 타격 슬롯(+5)의 플레이어 위치, 반드시 명중, 큰 피해
    private void AddFallingKnife(int startSlot,
        IReadOnlyList<Vector2Int> positions,
        List<BossIntent> intents,
        List<BossAction> actions)
    {
        AddIntent(startSlot, new[] { 12 }, intents);
        actions.Add(NewAction(startSlot + 5,
            BossTargetResolver.Center(positions[startSlot + 5]),
            fallingKnifeDamage, ignoresDodge: true, blockedByTangle: true));
    }

    /// II / II,II — 가로줄 전체에 약한 피해 2번, 피격 시 수레바퀴 회전
    private void AddRows(int startSlot, bool wide,
        List<BossIntent> intents,
        List<BossAction> actions)
    {
        AddIntent(startSlot, wide ? new[] { 2, 2 } : new[] { 2 }, intents);

        Vector2Int[] cells = ClownWheel.RowCells(wide ? WideRows : NarrowRows);
        for (int offset = 1; offset <= 2; offset++)
            actions.Add(NewAction(startSlot + offset, cells, rowDamage,
                rotatesWheelOnHit: true));
    }

    /// VII — 세로줄, 시전 시작 시 수레바퀴 참조
    private void AddNail(int startSlot, int predictedWheel,
        List<BossIntent> intents,
        List<BossAction> actions)
    {
        AddIntent(startSlot, new[] { 7 }, intents);
        actions.Add(NewAction(startSlot + 2,
            ClownWheel.ColumnCells(predictedWheel), nailDamage,
            castSlot: startSlot, wheelTargeting: WheelTargeting.Column));
    }

    /// VIII — 3×3 폭탄, 시전 시작 시 수레바퀴 참조
    private void AddBomb(int startSlot, int predictedWheel,
        List<BossIntent> intents,
        List<BossAction> actions)
    {
        AddIntent(startSlot, new[] { 8 }, intents);
        actions.Add(NewAction(startSlot + 2,
            ClownWheel.BombCells(predictedWheel), bombDamage,
            castSlot: startSlot, wheelTargeting: WheelTargeting.Bomb));
    }

    private static void AddIntent(int timingSlot, int[] arcanaIds,
        List<BossIntent> intents)
    {
        intents.Add(new BossIntent
        {
            timingSlot = timingSlot,
            arcanaIds = arcanaIds,
            isInstantKill = false
        });
    }

    private BossAction NewAction(int timingSlot, Vector2Int[] cells, int damage,
        bool ignoresDodge = false,
        bool blockedByTangle = false,
        bool rotatesWheelOnHit = false,
        int castSlot = 0,
        WheelTargeting wheelTargeting = WheelTargeting.None)
    {
        return new BossAction
        {
            timingSlot = timingSlot,
            arcanaIds = System.Array.Empty<int>(),
            targetCells = cells,
            baseDamage = damage,
            isInstantKill = false,
            element = attackElement,
            castSlot = castSlot,
            ignoresDodge = ignoresDodge,
            blockedByTangle = blockedByTangle,
            rotatesWheelOnHit = rotatesWheelOnHit,
            wheelTargeting = wheelTargeting
        };
    }
}
