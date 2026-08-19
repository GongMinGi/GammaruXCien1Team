using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public event Action StatsChanged;
    public event Action<int> DamageTaken;

    [SerializeField, Min(1)] private int maxHp = 100;
    [SerializeField, Min(0)] private int spellPower = 10;

    private int currentHp;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public int SpellPower => spellPower;
    public bool IsDead => currentHp <= 0;

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

    public void InstantKill()
    {
        int appliedDamage = currentHp;
        currentHp = 0;
        StatsChanged?.Invoke();
        if (appliedDamage > 0)
            DamageTaken?.Invoke(appliedDamage);
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        StatsChanged?.Invoke();
    }
}
