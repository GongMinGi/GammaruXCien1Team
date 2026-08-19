using UnityEngine;

public enum EffectType
{
    Move,
    Stay,
    DealDamage,
    Heal,
    Dodge,
    Cast,
    ApplyModifier
}

[System.Serializable]
public struct ScheduledEffect
{
    public EffectType Type;
    public Vector2Int Direction;
    public int BaseValue;
    public float SpellPowerCoefficient;
    public int AdditionalEffectValue;
    public DamageElement Element;
    public ArcanaData SourceCard;
}
