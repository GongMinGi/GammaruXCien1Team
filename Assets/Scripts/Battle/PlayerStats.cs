using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public event Action StatsChanged;

    [SerializeField, Min(1)] private int maxHp = 100;
    [SerializeField, Min(0)] private int spellPower = 10;

    private int currentHp;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public int SpellPower => spellPower;

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

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        StatsChanged?.Invoke();
    }
}
