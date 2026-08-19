using UnityEngine;

[CreateAssetMenu(fileName = "BossData", menuName = "Battle/Boss Data")]
public class BossData : ScriptableObject
{
    [SerializeField] private string bossName;
    [SerializeField] private Sprite portrait;
    [SerializeField, Min(1)] private int maxHp = 500;
    [SerializeField] private DamageElement weakness = DamageElement.Neutral;
    [SerializeField, Min(0)] private int burnDamagePerStack = 2;
    [SerializeField, Min(1)] private int burnDurationTurns = 3;
    [SerializeField] private BossPatternDefinition pattern;

    public string BossName => bossName;
    public Sprite Portrait => portrait;
    public int MaxHp => maxHp;
    public DamageElement Weakness => weakness;
    public int BurnDamagePerStack => burnDamagePerStack;
    public int BurnDurationTurns => burnDurationTurns;
    public BossPatternDefinition Pattern => pattern;
}
