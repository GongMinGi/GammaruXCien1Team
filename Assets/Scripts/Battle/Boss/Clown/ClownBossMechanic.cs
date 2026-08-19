using UnityEngine;

/// <summary>
/// "실타래에 꿰인 광대"의 실행 시점 권한.
/// BattleExecutor는 보스 종류를 모른 채 슬롯 시작·명중만 알려주고,
/// 수레바퀴 지식은 전부 여기에 있다.
/// </summary>
public class ClownBossMechanic : MonoBehaviour
{
    [SerializeField] private ThreadBoundClownPatternDefinition pattern;
    [SerializeField] private BattleExecutor battleExecutor;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerDisplay playerDisplay;
    [SerializeField] private TangleField tangleField;
    [SerializeField] private WheelDisplay wheelDisplay;

    private System.Random rng;
    private int shownWheelPosition = -1;

    private void Awake()
    {
        rng = new System.Random();

        // SO 런타임 상태는 에디터 세션 간에 남으므로 전투 시작 시 반드시 리셋한다
        if (pattern != null)
            pattern.ResetRuntimeState();
        if (tangleField != null)
            tangleField.ClearAll();

        RefreshDisplay();
    }

    private void OnEnable()
    {
        if (battleExecutor == null)
            return;

        battleExecutor.SlotStarted += OnSlotStarted;
        battleExecutor.BossActionHit += OnBossActionHit;
    }

    private void OnDisable()
    {
        if (battleExecutor == null)
            return;

        battleExecutor.SlotStarted -= OnSlotStarted;
        battleExecutor.BossActionHit -= OnBossActionHit;
    }

    /// 시전이 시작되는 슬롯에서 VII/VIII 목표를 현재 수레바퀴 위치로 덮어쓴다.
    private void OnSlotStarted(int slot, BossAction[] bossPattern)
    {
        if (pattern == null || bossPattern == null)
            return;

        for (int i = 0; i < bossPattern.Length; i++)
        {
            if (bossPattern[i].wheelTargeting == WheelTargeting.None ||
                bossPattern[i].castSlot != slot)
                continue;

            bossPattern[i].targetCells =
                bossPattern[i].wheelTargeting == WheelTargeting.Column
                    ? ClownWheel.ColumnCells(pattern.WheelPosition)
                    : ClownWheel.BombCells(pattern.WheelPosition);
        }
    }

    /// II 피격 → 수레바퀴 1칸 우회전
    private void OnBossActionHit(BossAction action)
    {
        if (pattern == null || !action.rotatesWheelOnHit)
            return;

        pattern.RotateWheel();
        RefreshDisplay();
    }

    /// <summary>
    /// 턴 종료 훅. IV/V/0을 적용한 뒤 수레바퀴 효과를 발동한다.
    /// BattleFlowController.OnExecutionComplete에서 호출한다.
    /// </summary>
    public void ApplyTurnEnd()
    {
        if (pattern == null)
            return;

        int cell = pattern.ApplyTurnEndWheel();
        RefreshDisplay();

        if (!pattern.WheelEffectsEnabled)
            return;

        if (cell == 3 || cell == 6)
        {
            if (playerStats != null)
                playerStats.TakeDamage(pattern.WheelPunishDamage);
        }
        else if (cell == 4 || cell == 7)
        {
            if (tangleField != null && playerDisplay != null)
                tangleField.Spawn(
                    ClownWheel.PickTangleCenter(playerDisplay.GridPosition, rng));
        }
        // 1, 2, 8, 9 — 효과 없음
    }

    /// <summary>
    /// 수레바퀴 위치는 여기 말고도 패턴 SO 안에서 바뀐다(X — 2페이즈 개막 리셋).
    /// 바뀔 때마다 알림을 받는 대신 표시가 모델을 따라가게 둔다.
    /// </summary>
    private void Update()
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (wheelDisplay == null || pattern == null)
            return;

        if (pattern.WheelPosition == shownWheelPosition)
            return;

        shownWheelPosition = pattern.WheelPosition;
        wheelDisplay.SetPosition(shownWheelPosition);
    }
}
