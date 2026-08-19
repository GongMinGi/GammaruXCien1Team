using System;
using UnityEngine;

public class BossStats : MonoBehaviour
{
    public event Action StatsChanged;
    public event Action<int> DamageTaken;

    [SerializeField, Min(1)] private int maxHp = 500;
    [SerializeField] private DamageElement weakness = DamageElement.Neutral;
    [SerializeField, Min(0)] private int burnDamagePerStack = 2;

    private int currentHp;
    private int burnStacks;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public DamageElement Weakness => weakness;
    public bool IsDead => currentHp <= 0;
    public int BurnStacks => burnStacks;

    private void Awake()
    {
        currentHp = maxHp;
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
        burnStacks += stacks;
        StatsChanged?.Invoke();
    }

    public void ProcessBurn()
    {
        if (burnStacks <= 0) return;
        int burnDamage = burnStacks * burnDamagePerStack;
        TakeDamage(burnDamage);
    }

    public void ClearBurn()
    {
        burnStacks = 0;
    }
}
