using System;
using UnityEngine;

public class BossStats : MonoBehaviour
{
    public event Action StatsChanged;
    public event Action<int> DamageTaken;

    [SerializeField] private BossData bossData;

    private int currentHp;
    private int burnStacks;
    private int burnRemainingTurns;

    public BossData Data => bossData;
    public int MaxHp => bossData != null ? bossData.MaxHp : 1;
    public int CurrentHp => currentHp;
    public DamageElement Weakness =>
        bossData != null ? bossData.Weakness : DamageElement.Neutral;
    public bool IsDead => currentHp <= 0;
    public int BurnStacks => burnStacks;
    public int BurnRemainingTurns => burnRemainingTurns;

    private void Awake()
    {
        BossData transferredBossData = BattleLoadoutData.TakeSelectedBossData();

        if (transferredBossData != null)
            bossData = transferredBossData;

        if (bossData == null)
        {
            Debug.LogError("BossData is not assigned.", this);
            currentHp = 1;
            return;
        }
        currentHp = bossData.MaxHp;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        int previousHp = currentHp;
        currentHp = Mathf.Max(0, currentHp - amount);
        int appliedDamage = previousHp - currentHp;
        if (appliedDamage <= 0) return;
        StatsChanged?.Invoke();
        DamageTaken?.Invoke(appliedDamage);
    }

    public void AddBurnStacks(int stacks)
    {
        if (stacks <= 0) return;
        if (bossData == null) return;
        burnStacks += stacks;
        burnRemainingTurns = bossData.BurnDurationTurns;
        StatsChanged?.Invoke();
    }

    public void ProcessBurn()
    {
        if (burnStacks <= 0) return;
        int burnDamage = burnStacks *
                         (bossData != null ? bossData.BurnDamagePerStack : 0);
        TakeDamage(burnDamage);
    }

    public void TickBurnTimer()
    {
        if (burnRemainingTurns <= 0) return;
        burnRemainingTurns--;
        if (burnRemainingTurns <= 0)
            burnStacks = 0;
        StatsChanged?.Invoke();
    }
}
