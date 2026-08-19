using System;
using UnityEngine;

public class BossStats : MonoBehaviour
{
    public event Action StatsChanged;

    [SerializeField, Min(1)] private int maxHp = 500;
    [SerializeField] private DamageElement weakness = DamageElement.Neutral;

    private int currentHp;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public DamageElement Weakness => weakness;
    public bool IsDead => currentHp <= 0;

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
}
