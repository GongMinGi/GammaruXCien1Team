using System;
using UnityEngine;

public class BossStats : MonoBehaviour
{
    public event Action StatsChanged;

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
        currentHp = Mathf.Max(0, currentHp - amount);
        StatsChanged?.Invoke();
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
